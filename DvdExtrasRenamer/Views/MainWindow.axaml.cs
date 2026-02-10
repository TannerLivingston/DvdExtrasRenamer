using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using DvdExtrasRenamer.ViewModels;

namespace DvdExtrasRenamer.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OpenFileButton_Clicked(object sender, RoutedEventArgs e)
    {
        // Get the TopLevel (Window) instance
        var topLevel = TopLevel.GetTopLevel(this);

        // Check if the storage provider is available
        if (topLevel?.StorageProvider is { } provider)
        {
            // Open folder picker dialog
            var folders = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Directory for Videos"
            });

            // Update the TextBlock with the selected directory path
            if (folders != null && folders.Any())
            {
                var selectedPath = folders.FirstOrDefault()?.Path?.LocalPath ?? folders.FirstOrDefault()?.Name ?? string.Empty;
                FilePathTextBlock.Text = selectedPath;
                
                // Extract the parent directory name and populate Title/Year
                if (this.DataContext is MainWindowViewModel viewModel)
                {
                    viewModel.SelectedDirectory = selectedPath;
                    
                    // Extract parent directory name (movie title)
                    var dirInfo = new System.IO.DirectoryInfo(selectedPath);
                    var parentDirName = dirInfo.Parent?.Name ?? dirInfo.Name;
                    
                    // Try to extract year from directory name (e.g., "The Northman (2022)" -> "2022")
                    var yearMatch = System.Text.RegularExpressions.Regex.Match(parentDirName, @"\((\d{4})\)");
                    if (yearMatch.Success)
                    {
                        var yearValue = yearMatch.Groups[1].Value;
                        viewModel.Year = yearValue;
                        
                        // Remove the year part from the title
                        viewModel.Title = System.Text.RegularExpressions.Regex.Replace(parentDirName, @"\s*\(\d{4}\)\s*$", "").Trim();
                    }
                    else
                    {
                        viewModel.Title = parentDirName;
                        viewModel.Year = "";
                    }
                }
            }
            else
            {
                FilePathTextBlock.Text = "No directory selected";
            }
        }
    }
}