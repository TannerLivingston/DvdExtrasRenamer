using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DvdExtrasRenamer.Models;

/// <summary>
/// Handles video file operations including discovery, duration extraction, and renaming.
/// Uses TagLibSharp for reliable metadata reading across multiple video formats.
/// </summary>
public class FileHandler
{
    private static readonly string[] VideoExtensions = { ".mp4", ".mkv", ".avi", ".mov", ".flv", ".wmv", ".webm" };
    private readonly Dictionary<string, double?> _durationCache = []; // Cache metadata to avoid re-reading

    /// <summary>
    /// Callback delegate for progress reporting during long-running operations.
    /// </summary>
    public delegate void ProgressCallback(string message, int videoCount, int progress);

    /// <summary>
    /// Matches video files in a directory to a list of DVD extras based on duration (±1 second tolerance).
    /// Reads metadata sequentially (caches to avoid redundant reads).
    /// Reports progress via callback and eliminates duplicate matches.
    /// </summary>
    public async Task<List<VideoMatch>> MatchVideoFilesToExtrasAsync(
        string directoryPath,
        List<DvdExtras> extras,
        ProgressCallback? onProgress = null,
        CancellationToken  cancellationToken = default)
    {
        var matches = new List<VideoMatch>();

        if (!Directory.Exists(directoryPath))
        {
            onProgress?.Invoke("❌ Directory does not exist.", 0, 0);
            return matches;
        }

        var directory = new DirectoryInfo(directoryPath);
        var videoFiles = directory.GetFiles()
            .Where(f => VideoExtensions.Contains(f.Extension.ToLower()))
            .ToList();

        onProgress?.Invoke($"🔍 Scanning directory for {videoFiles.Count} video file(s)...\n", videoFiles.Count, 0);
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Starting metadata extraction - {videoFiles.Count} files");

        var startTime = DateTime.Now;
        int processed = 0;
        foreach (var videoFile in videoFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            processed++;

            // Check cache first
            bool isFromCache = false;
            double? duration;

            if (_durationCache.TryGetValue(videoFile.FullName, out var cachedDuration))
            {
                duration = cachedDuration;
                isFromCache = true;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [CACHE] {videoFile.Name} → {duration:F1}s");
            }
            else
            {
                var extractStart = DateTime.Now;
                duration = await GetVideoDurationAsync(videoFile.FullName, cancellationToken);
                var elapsed = DateTime.Now - extractStart;

                // Cache the result
                _durationCache[videoFile.FullName] = duration;

                if (duration.HasValue)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [READ] {videoFile.Name} → {duration.Value:F1}s ({elapsed.TotalMilliseconds:F0}ms)");
                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [FAIL] {videoFile.Name} (could not read)");
                }


            }

            if (duration == null)
            {
                onProgress?.Invoke($"⏳ [{processed}/{videoFiles.Count}] {videoFile.Name} - ⚠️  Could not read duration, skipping\n", videoFiles.Count, processed);
                continue;
            }

            onProgress?.Invoke($"⏳ [{processed}/{videoFiles.Count}] {videoFile.Name} - Duration: {duration.Value:F1}s {(isFromCache ? "(cached)" : "")}", videoFiles.Count, processed);

            var candidateExtras = new List<(string Title, string Duration, double Diff)>();
            foreach (var extra in extras)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (TryParseDuration(extra.Duration, out var extraDuration))
                {
                    if (Math.Abs(duration.Value - extraDuration) <= 1.0)
                    {
                        var diff = Math.Abs(duration.Value - extraDuration);
                        candidateExtras.Add((extra.Title, extra.Duration, diff));
                    }
                }
            }

            if (candidateExtras.Count == 0)
            {
                onProgress?.Invoke($" - No matches found\n", 0, 0);
            }
            else
            {
                var titles = candidateExtras.Select(c => c.Title).ToList();
                var first = candidateExtras[0];
                var collisionNote = candidateExtras.Count > 1
                    ? $" ⚠️ COLLISION: {candidateExtras.Count} extras share this duration — pick one in the list below"
                    : "";
                onProgress?.Invoke($"\n    ✅ MATCH: {string.Join(", ", titles)} (diff: {first.Diff:F2}s){collisionNote}\n", videoFiles.Count, processed);
                matches.Add(new VideoMatch(
                    VideoFile: videoFile.Name,
                    CandidateExtraTitles: titles,
                    VideoDuration: duration.Value,
                    ExtraDuration: first.Duration,
                    DurationDifference: first.Diff,
                    FullPath: videoFile.FullName
                ));
            }
        }

        var totalElapsed = DateTime.Now - startTime;
        onProgress?.Invoke($"\n{'='*50}\n✅ Complete! Found {matches.Count} match(es) in {totalElapsed.TotalSeconds:F2}s.\n", videoFiles.Count, processed);
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Metadata extraction complete - {processed} files processed in {totalElapsed.TotalSeconds:F2}s");
        return matches;
    }

    /// <summary>
    /// Renames a video file to match the extra title.
    /// </summary>
    public static bool RenameVideoFile(string fullPath, string newTitle)
    {
        try
        {
            var fileInfo = new System.IO.FileInfo(fullPath);
            var extension = fileInfo.Extension;
            var directory = fileInfo.DirectoryName;
            var newFileName = $"{newTitle}{extension}";
            var newFullPath = System.IO.Path.Combine(directory ?? "", newFileName);

            // Avoid overwriting existing files
            if (System.IO.File.Exists(newFullPath) && newFullPath != fullPath)
            {
                throw new IOException($"File '{newFileName}' already exists.");
            }

            System.IO.File.Move(fullPath, newFullPath, overwrite: true);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error renaming file: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets the duration of a video file in seconds by reading its metadata.
    /// </summary>
    private static Task<double?> GetVideoDurationAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            // This operation can take a while and is called in a loop so we check first for the cancellation token
            cancellationToken.ThrowIfCancellationRequested();

            var file = TagLib.File.Create(filePath);
            if (file?.Properties?.Duration != null)
            {
                return Task.FromResult<double?>(file.Properties.Duration.TotalSeconds);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("File matching cancelled");

            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading duration for {filePath}: {ex.Message}");
        }

        return Task.FromResult<double?>(null);
    }

    /// <summary>
    /// Attempts to parse a duration string (e.g., "1:23:45" or "83:45") to seconds.
    /// </summary>
    private static bool TryParseDuration(string durationStr, out double seconds)
    {
        seconds = 0;

        if (string.IsNullOrEmpty(durationStr))
        {
            return false;
        }

        var parts = durationStr.Split(':');
        try
        {
            if (parts.Length == 2)
            {
                // MM:SS format
                var minutes = int.Parse(parts[0]);
                var secs = int.Parse(parts[1]);
                seconds = minutes * 60 + secs;
                return true;
            }
            else if (parts.Length == 3)
            {
                // HH:MM:SS format
                var hours = int.Parse(parts[0]);
                var minutes = int.Parse(parts[1]);
                var secs = int.Parse(parts[2]);
                seconds = hours * 3600 + minutes * 60 + secs;
                return true;
            }
        }
        catch (FormatException)
        {
            // Parsing failed
        }

        return false;
    }
}

public record VideoMatch(
    string VideoFile,
    List<string> CandidateExtraTitles,
    double VideoDuration,
    string ExtraDuration,
    double DurationDifference,
    string FullPath = ""
)
{
    /// <summary>First candidate title for display; use CandidateExtraTitles for full list and user choice.</summary>
    public string ExtraTitle => CandidateExtraTitles.Count > 0 ? CandidateExtraTitles[0] : string.Empty;

    public bool HasCollision => CandidateExtraTitles.Count > 1;

    public override string ToString()
    {
        var title = CandidateExtraTitles.Count == 1 ? CandidateExtraTitles[0] : $"[{CandidateExtraTitles.Count} options]";
        return $"✓ {VideoFile} → {title} (video: {VideoDuration:F1}s, extra: {ExtraDuration}, diff: {DurationDifference:F2}s)";
    }
}
