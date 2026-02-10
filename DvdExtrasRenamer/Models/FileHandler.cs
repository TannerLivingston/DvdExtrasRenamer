using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TagLib;

namespace DvdExtrasRenamer.Models;

/// <summary>
/// Handles video file operations including discovery, duration extraction, and renaming.
/// Uses TagLibSharp for reliable metadata reading across multiple video formats.
/// </summary>
public class FileHandler
{
    private static readonly string[] VideoExtensions = { ".mp4", ".mkv", ".avi", ".mov", ".flv", ".wmv", ".webm" };
    
    /// <summary>
    /// Callback delegate for progress reporting during long-running operations.
    /// </summary>
    public delegate void ProgressCallback(string message);

    /// <summary>
    /// Matches video files in a directory to a list of DVD extras based on duration (±1 second tolerance).
    /// Reports progress via callback and eliminates duplicate matches.
    /// </summary>
    public async Task<List<VideoMatch>> MatchVideoFilesToExtrasAsync(string directoryPath, List<DvdExtras> extras, ProgressCallback? onProgress = null)
    {
        var matches = new List<VideoMatch>();
        var seenMatches = new HashSet<string>(); // Track (VideoFile, ExtraTitle) tuples to eliminate duplicates

        if (!Directory.Exists(directoryPath))
        {
            onProgress?.Invoke("❌ Directory does not exist.");
            return matches;
        }

        var directory = new DirectoryInfo(directoryPath);
        var videoFiles = directory.GetFiles()
            .Where(f => VideoExtensions.Contains(f.Extension.ToLower()))
            .ToList();

        onProgress?.Invoke($"🔍 Scanning directory for {videoFiles.Count} video file(s)...\n");

        int processed = 0;
        foreach (var videoFile in videoFiles)
        {
            processed++;
            onProgress?.Invoke($"⏳ [{processed}/{videoFiles.Count}] Processing: {videoFile.Name}");
            
            var duration = await GetVideoDurationAsync(videoFile.FullName);
            if (duration == null)
            {
                onProgress?.Invoke($"  └─ ⚠️  Could not read duration, skipping\n");
                continue;
            }

            onProgress?.Invoke($"  └─ ✓ Duration: {duration.Value:F1}s, matching against extras...");

            var foundMatches = false;
            foreach (var extra in extras)
            {
                if (TryParseDuration(extra.Duration, out var extraDuration))
                {
                    if (Math.Abs(duration.Value - extraDuration) <= 1.0)
                    {
                        var matchKey = $"{videoFile.Name}|{extra.Title}";
                        if (!seenMatches.Contains(matchKey))
                        {
                            matches.Add(new VideoMatch(
                                VideoFile: videoFile.Name,
                                ExtraTitle: extra.Title,
                                VideoDuration: duration.Value,
                                ExtraDuration: extra.Duration,
                                DurationDifference: Math.Abs(duration.Value - extraDuration),
                                FullPath: videoFile.FullName
                            ));
                            seenMatches.Add(matchKey);
                            onProgress?.Invoke($"\n    ✅ MATCH: {extra.Title} (diff: {Math.Abs(duration.Value - extraDuration):F2}s)\n");
                            foundMatches = true;
                        }
                    }
                }
            }
            
            if (!foundMatches)
            {
                onProgress?.Invoke($" - No matches found\n");
            }
        }

        onProgress?.Invoke($"\n{'='*50}\n✅ Complete! Found {matches.Count} match(es).\n");
        return matches;
    }

    /// <summary>
    /// Renames a video file to match the extra title.
    /// </summary>
    public bool RenameVideoFile(string fullPath, string newTitle)
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
    private async Task<double?> GetVideoDurationAsync(string filePath)
    {
        try
        {
            var file = TagLib.File.Create(filePath);
            if (file?.Properties?.Duration != null)
            {
                return file.Properties.Duration.TotalSeconds;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading duration for {filePath}: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Attempts to parse a duration string (e.g., "1:23:45" or "83:45") to seconds.
    /// </summary>
    private bool TryParseDuration(string durationStr, out double seconds)
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
    string ExtraTitle,
    double VideoDuration,
    string ExtraDuration,
    double DurationDifference,
    string FullPath = ""
)
{
    public override string ToString()
    {
        return $"✓ {VideoFile} → {ExtraTitle} (video: {VideoDuration:F1}s, extra: {ExtraDuration}, diff: {DurationDifference:F2}s)";
    }
}