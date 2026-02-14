using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DvdExtrasRenamer.Models;

namespace DvdExtrasRenamer.ViewModels;

/// <summary>
/// Main view model for the DVD Extras Renamer application.
/// Manages the workflow of DVD lookup, video matching, and file renaming.
/// Implements MVVM pattern with observable properties and async relay commands.
/// </summary>
public partial class MainWindowViewModel(DvdDotComClient dvdDotComClient, FileHandler fileHandler) : ViewModelBase
{
    private readonly DvdDotComClient _dvdDotComClient = dvdDotComClient;
    private readonly FileHandler _fileHandler = fileHandler;
    private CancellationTokenSource? _matchVideosOperationCancellationTokenSource;
    private List<DvdExtras> _currentExtras = new();
    private List<VideoMatch> _currentMatches = new();
    [ObservableProperty] private string _lookupResult = string.Empty;

    [ObservableProperty] private string _title = string.Empty;

    [ObservableProperty] private string _year = string.Empty;

    [ObservableProperty] private string _director = string.Empty;

    [ObservableProperty] private ObservableCollection<DvdCompareResult> _items = new();

    [ObservableProperty] private string _matchResults = string.Empty;

    [ObservableProperty] private int _videoCount = int.MaxValue;

    [ObservableProperty] private string _selectedDirectory = string.Empty;

    [ObservableProperty] private string _selectedDvdTitle = string.Empty;

    [ObservableProperty] private string _selectedDvdHref = string.Empty;

    [ObservableProperty] private bool _isLookupInProgress = false;

    [ObservableProperty] private bool _isDvdSelected = false;

    [ObservableProperty] private bool _isCancelButtonEnabled = false;

    [ObservableProperty] private bool _isMatchingInProgress = false;

    [ObservableProperty] private int _selectedMatchIndex = 0;

    [ObservableProperty] private bool _isRenameInProgress = false;

    [ObservableProperty] private string _currentRenameStatus = string.Empty;

    [ObservableProperty] private ObservableCollection<VideoMatchViewModel> _matchList = new();

    partial void OnIsLookupInProgressChanged(bool value)
    {
        OnPropertyChanged(nameof(IsLookupNotInProgress));
    }

    partial void OnIsDvdSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(IsDvdNotSelected));
    }

    partial void OnIsRenameInProgressChanged(bool value)
    {
        OnPropertyChanged(nameof(IsRenameNotInProgress));
    }

    partial void OnIsMatchingInProgressChanged(bool value)
    {
        OnPropertyChanged(nameof(IsMatchingNotInProgress));
    }

    public bool IsLookupNotInProgress => !IsLookupInProgress;
    public bool IsDvdNotSelected => !IsDvdSelected;
    public bool IsRenameNotInProgress => !IsRenameInProgress;
    public bool IsMatchingNotInProgress => !IsMatchingInProgress;

    /// <summary>
    /// Searches dvdcompare.net for DVDs matching the Title, Director, and Year criteria.
    /// Automatically formats titles with articles (The, A, An) to match dvdcompare.net conventions.
    /// Results are displayed in the Items collection.
    /// </summary>
    [RelayCommand]
    private async Task PerformLookup()
    {
        if (string.IsNullOrEmpty(Title))
        {
            LookupResult = "❌ Please enter a title to lookup.";
            return;
        }

        IsLookupInProgress = true;
        LookupResult = "🔍 Searching for DVD...";

        var formattedTitle = FormatTitleForSearch(Title);
        var lookupResult = await _dvdDotComClient.LookupDvdAsync(formattedTitle, Director, Year);
        var options = DvdDotComParser.GetLookupResults(lookupResult);
        Items = new ObservableCollection<DvdCompareResult>(options.ToList());

        IsLookupInProgress = false;

        if (Items.Count == 0)
        {
            LookupResult = $"❌ No DVD results found for '{Title}'{(string.IsNullOrEmpty(Year) ? "" : $" ({Year})")}";
        }
        else
        {
            LookupResult = $"✅ Found {Items.Count} DVD result(s) for '{Title}'{(string.IsNullOrEmpty(Year) ? "" : $" ({Year})")}. Click a title to select it.";
        }
    }

    /// <summary>
    /// Selects a DVD from search results and fetches its extras/bonus features.
    /// Triggers loading of DVD details page and parsing of extras.
    /// Updates UI with selected DVD title and enables video matching.
    /// </summary>
    /// <param name="dvd">The DVD comparison result to select</param>
    [RelayCommand]
    private async Task SelectItem(DvdCompareResult? dvd)
    {
        if (dvd == null)
            return;

        // This command is fired when a button is clicked with the DVD info
        SelectedDvdTitle = dvd.Title;
        SelectedDvdHref = dvd.Href;
        IsLookupInProgress = true;
        LookupResult = "🔍 Loading DVD extras...";
        var details = await _dvdDotComClient.FetchDvdDetailsAsync(dvd.Href);
        var extras = DvdDotComParser.ParseExtras(details);
        _currentExtras = [.. extras];

        IsLookupInProgress = false;
        IsDvdSelected = _currentExtras.Count > 0;

        if (_currentExtras.Count > 0)
        {
            LookupResult = $"✅ Loaded {_currentExtras.Count} extra(s). You can now match videos.";
        }
        else
        {
            LookupResult = "❌ No extras found for this DVD.";
        }

        foreach (var item in _currentExtras)
        {
            Console.WriteLine(item);
        }
    }

    [RelayCommand]
    private void CancelMatchOperation()
    {
        IsCancelButtonEnabled = false;
        Console.WriteLine("Cancel Match Operation");
        MatchResults += "🛑 Stopping Match Operation...";
        _matchVideosOperationCancellationTokenSource?.Cancel();
    }

    /// <summary>
    /// Scans the selected directory for video files and matches them to DVD extras by duration.
    /// Uses ±1 second tolerance for matching. Results are displayed in MatchList.
    /// Runs on background thread to prevent UI blocking.
    /// </summary>
    /// <param name="directoryPath">Full path to the directory containing video files</param>
    [RelayCommand]
    private async Task MatchVideosInDirectory(string directoryPath)
    {
        if (string.IsNullOrEmpty(directoryPath) || _currentExtras.Count == 0)
        {
            MatchResults = "❌ Please select a DVD first and provide a directory path.";
            return;
        }

        _matchVideosOperationCancellationTokenSource = new CancellationTokenSource();

        IsMatchingInProgress = true;
        IsCancelButtonEnabled = true;
        VideoCount = int.MaxValue;
        SelectedMatchIndex = 0;
        SelectedDirectory = directoryPath;
        MatchList.Clear();
        _currentMatches.Clear();

        try
        {
            // Run matching on background thread to avoid blocking the UI
            var matches = await Task.Run(() =>
                _fileHandler.MatchVideoFilesToExtrasAsync(
                    directoryPath,
                    _currentExtras,
                    UpdateMatchProgress,
                    _matchVideosOperationCancellationTokenSource.Token
                )
            );

            _currentMatches = matches;

            if (matches.Count == 0)
            {
                MatchResults += "\n❌ No video files matched the DVD extras by duration.";
            }
            else
            {
                var collisionCount = matches.Count(m => m.HasCollision);
                if (collisionCount > 0)
                    MatchResults += $"\n⚠️ {collisionCount} match(es) have multiple title options (same duration). Pick the correct title from the list.";
                MatchResults += $"\n✅ Found {matches.Count} match(es)!";

                // Build match list for UI
                foreach (var match in matches)
                {
                    var fileExtension = System.IO.Path.GetExtension(match.VideoFile);
                    var candidatesWithExt = match.CandidateExtraTitles.Select(t => t + fileExtension).ToList();
                    var chosenWithExt = candidatesWithExt[0];

                    var vm = new VideoMatchViewModel
                    {
                        VideoFile = match.VideoFile,
                        ExtraTitle = chosenWithExt,
                        CandidateExtraTitles = new ObservableCollection<string>(candidatesWithExt),
                        SelectedCandidateTitle = chosenWithExt,
                        VideoDuration = match.VideoDuration,
                        ExtraDuration = match.ExtraDuration,
                        DurationDifference = match.DurationDifference,
                        FullPath = match.FullPath,
                        RenameCommand = RenameVideoCommand
                    };
                    vm.SelectCandidateTitleCommand = new RelayCommand<string?>(title =>
                    {
                        if (!string.IsNullOrEmpty(title))
                            vm.SelectedCandidateTitle = title;
                    });
                    MatchList.Add(vm);
                }
            }
        }
        catch (OperationCanceledException)
        {
            MatchResults += "\n⛔ Match operation cancelled.";
        }
        finally
        {
            IsMatchingInProgress = false;
            IsCancelButtonEnabled = false;
        }
    }

    /// <summary>
    /// Renames a video file to match its matched DVD extra title.
    /// Automatically preserves file extension.
    /// Updates UI with rename status and marks the match as renamed.
    /// Runs on background thread to prevent UI blocking.
    /// </summary>
    /// <param name="match">The video match containing the file path and new title</param>
    [RelayCommand]
    private async Task RenameVideo(VideoMatchViewModel? match)
    {
        if (match == null || string.IsNullOrEmpty(match.FullPath))
        {
            Console.WriteLine("❌ RenameVideo: Invalid match or FullPath");
            return;
        }

        IsRenameInProgress = true;
        CurrentRenameStatus = $"⏳ Processing: {match.VideoFile}...";
        Console.WriteLine($"🔄 Starting rename: {match.VideoFile} → {match.ExtraTitle}");

        try
        {
            // Strip extension from ExtraTitle before passing to RenameVideoFile
            // (since RenameVideoFile adds the extension automatically)
            var extraTitleWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(match.ExtraTitle);

            // Run the file operation on a background thread to avoid blocking the UI
            var success = await Task.Run(() =>
            {
                return FileHandler.RenameVideoFile(match.FullPath, extraTitleWithoutExtension);
            });

            if (success)
            {
                CurrentRenameStatus = $"✅ Renamed: {match.ExtraTitle}";
                var message = $"\n✅ SUCCESS: Renamed '{match.VideoFile}' to '{match.ExtraTitle}'";
                MatchResults += message;
                Console.WriteLine(message);
                match.IsRenamed = true;
            }
            else
            {
                CurrentRenameStatus = $"❌ Failed: {match.VideoFile}";
                var message = $"\n❌ FAILED: Could not rename '{match.VideoFile}'";
                MatchResults += message;
                Console.WriteLine(message);
            }
        }
        finally
        {
            IsRenameInProgress = false;
            CurrentRenameStatus = "";
        }
    }

    /// <summary>
    /// Opens the selected DVD's page on dvdcompare.net in the default web browser.
    /// </summary>
    [RelayCommand]
    private void OpenDvdLink()
    {
        if (string.IsNullOrEmpty(SelectedDvdHref))
            return;

        try
        {
            // Build full URL from relative path
            var fullUrl = $"https://www.dvdcompare.net/comparisons/{SelectedDvdHref}";

            // Open the URL in the default browser
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = fullUrl,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to open URL: {ex.Message}");
        }
    }

    private void UpdateMatchProgress(string message, int videoCount, int processed)
    {
        MatchResults += message;
        VideoCount = videoCount;
        SelectedMatchIndex = processed;
    }

    /// <summary>
    /// Formats a DVD title for searching on dvdcompare.net.
    /// Moves articles (The, A, An) to the end in parentheses to match dvdcompare.net conventions.
    /// Example: "The Northman" becomes "Northman (The)"
    /// </summary>
    /// <param name="title">The original title to format</param>
    /// <returns>Formatted title suitable for dvdcompare.net searches</returns>
    private static string FormatTitleForSearch(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return title;

        var trimmed = title.Trim();
        var lower = trimmed.ToLowerInvariant();

        // Check for articles at the beginning
        if (lower.StartsWith("the "))
        {
            return $"{trimmed.Substring(4).Trim()} (The)";
        }
        else if (lower.StartsWith("an "))
        {
            return $"{trimmed.Substring(3).Trim()} (An)";
        }
        else if (lower.StartsWith("a "))
        {
            return $"{trimmed.Substring(2).Trim()} (A)";
        }

        return trimmed;
    }
}

/// <summary>
/// Represents a matched video file and its corresponding DVD extra title(s).
/// When multiple extras share the same duration (collision), CandidateExtraTitles lists options for the user to pick.
/// </summary>
public class VideoMatchViewModel : ObservableObject
{
    private string _videoFile = string.Empty;
    public string VideoFile
    {
        get => _videoFile;
        set => SetProperty(ref _videoFile, value);
    }

    private string _extraTitle = string.Empty;
    public string ExtraTitle
    {
        get => _extraTitle;
        set => SetProperty(ref _extraTitle, value);
    }

    /// <summary>All candidate extra titles (with extension) when duration matches multiple extras or videos.</summary>
    private ObservableCollection<string> _candidateExtraTitles = new();
    public ObservableCollection<string> CandidateExtraTitles
    {
        get => _candidateExtraTitles;
        set
        {
            if (SetProperty(ref _candidateExtraTitles, value ?? new ObservableCollection<string>()))
                OnPropertyChanged(nameof(HasCollision));
        }
    }

    /// <summary>Selected candidate for binding to ComboBox; updates ExtraTitle when changed.</summary>
    private string? _selectedCandidateTitle;
    public string? SelectedCandidateTitle
    {
        get => _selectedCandidateTitle ?? ExtraTitle;
        set
        {
            if (SetProperty(ref _selectedCandidateTitle, value) && !string.IsNullOrEmpty(value))
                ExtraTitle = value;
        }
    }

    public bool HasCollision => CandidateExtraTitles.Count > 1;

    public double VideoDuration { get; set; }
    public string ExtraDuration { get; set; } = string.Empty;
    public double DurationDifference { get; set; }
    public string FullPath { get; set; } = string.Empty;

    private bool _isRenamed = false;
    public bool IsRenamed
    {
        get => _isRenamed;
        set
        {
            if (SetProperty(ref _isRenamed, value))
            {
                OnPropertyChanged(nameof(IsNotRenamed));
                OnPropertyChanged(nameof(ButtonVisibility));
            }
        }
    }

    public bool IsNotRenamed => !IsRenamed;
    public double ButtonVisibility => IsRenamed ? 0.3 : 1.0;

    public IAsyncRelayCommand<VideoMatchViewModel>? RenameCommand { get; set; }

    /// <summary>Sets the selected title when user picks from the dropdown list (collision case).</summary>
    public IRelayCommand<string?>? SelectCandidateTitleCommand { get; set; }
}
