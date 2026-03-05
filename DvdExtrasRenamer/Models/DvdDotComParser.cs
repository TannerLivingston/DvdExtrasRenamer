using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace DvdExtrasRenamer.Models;

/// <summary>
/// Parses HTML content from dvdcompare.net to extract DVD information and extras.
/// Handles title cleanup, deduplication, and character encoding.
/// </summary>
public class DvdDotComParser
{
    /// <summary>
    /// Extracts DVD search results from the search results HTML.
    /// </summary>
    /// <param name="dvdLookupHtml">HTML content from dvdcompare.net search results page</param>
    /// <returns>List of DVD comparison results with title, href, and format information</returns>
    public static IEnumerable<DvdCompareResult> GetLookupResults(string dvdLookupHtml)
    {
        var results = new List<DvdCompareResult>();
        
        var doc = new HtmlDocument();
        doc.LoadHtml(dvdLookupHtml);

        var resultItems = doc.DocumentNode
            .SelectNodes("//div[contains(@class,'col1-1')]//ul/li");

        if (resultItems == null)
        {
            return results;
        }
        
        foreach (var li in resultItems)
        {
            var linkNode = li.SelectSingleNode(".//a");
            var winnerNode = li.SelectSingleNode(".//i");

            var title = NormalizeWhitespace(HtmlEntity.DeEntitize(linkNode?.InnerText ?? "ERROR"));
            var href = linkNode?.GetAttributeValue("href", "ERROR") ?? "ERROR";
            var winner = winnerNode?.InnerText.Trim() ?? "ERROR";
            
            results.Add(new DvdCompareResult(title, href, winner));
        }

        return results;
    }

    /// <summary>
    /// Parses releases and their extras/bonus features from DVD details page HTML.
    /// Returns multiple releases if the page compares different editions, otherwise returns a single release.
    /// </summary>
    /// <param name="html">HTML content from the DVD details page</param>
    /// <returns>List of releases with their extracted extras</returns>
    public static List<DvdRelease> ParseReleases(string html)
    {
        var doc = new HtmlDocument
        {
            OptionDefaultStreamEncoding = Encoding.UTF8
        };
        doc.LoadHtml(html);

        var releases = new List<DvdRelease>();

        // DEBUG: Save HTML to file for inspection
        try
        {
            var debugPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "dvdcompare_debug.html");
            File.WriteAllText(debugPath, html);
            Console.WriteLine($"[DEBUG] Saved HTML to {debugPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Failed to save HTML: {ex.Message}");
        }

        // Strategy: Find all td elements with width="80%" that contain h3 headers
        // These are the main content cells in the comparison table
        var releaseTds = doc.DocumentNode.SelectNodes("//td[@width='80%']");
        Console.WriteLine($"[DEBUG] Found {releaseTds?.Count ?? 0} td[@width='80%'] elements");

        if (releaseTds != null && releaseTds.Count > 0)
        {
            // Filter to only td elements that contain h3 headers (actual release cells, not sidebars)
            foreach (var td in releaseTds)
            {
                var h3Header = td.SelectSingleNode(".//h3");
                if (h3Header == null)
                {
                    Console.WriteLine("[DEBUG] Skipping td - no h3 header found");
                    continue;
                }

                var headerText = h3Header.InnerText;
                Console.WriteLine($"[DEBUG] Found h3 header: {headerText}");
                
                // Filter out non-release headers (they don't contain format/location info)
                // Real releases contain patterns like "Blu-ray", "DVD", "4K", "Limited Edition", distributor names, years in brackets, etc.
                if (!Regex.IsMatch(headerText, @"(Blu-ray|DVD|UHD|4K|HD|Standard|Limited|Edition|Lionsgate|Second Sight|Warner|Paramount|Disney)", RegexOptions.IgnoreCase))
                {
                    Console.WriteLine("[DEBUG] Skipping - doesn't match release pattern");
                    continue;
                }

                var releaseName = CleanupReleaseName(headerText);
                Console.WriteLine($"[DEBUG] Processing release: {releaseName}");

                // Find all Extras sections within this td
                // They are in li elements that have a child div with class='label' containing 'Extras:'
                var extrasItems = td.SelectNodes(".//li[div[@class='label' and normalize-space()='Extras:']]");
                Console.WriteLine($"[DEBUG]   Found {extrasItems?.Count ?? 0} extras items");
                
                var allExtras = new List<DvdExtras>();

                if (extrasItems != null)
                {
                    foreach (var item in extrasItems)
                    {
                        var extrasDiv = item.SelectSingleNode(".//div[@class='description']");
                        if (extrasDiv != null)
                        {
                            var extras = ExtractExtrasFromSingleNode(extrasDiv);
                            Console.WriteLine($"[DEBUG]     Extracted {extras.Count} extras");
                            allExtras.AddRange(extras);
                        }
                    }
                }

                if (allExtras.Count > 0)
                {
                    releases.Add(new DvdRelease { Name = releaseName, Extras = allExtras });
                    Console.WriteLine($"[DEBUG]   Total extras for release: {allExtras.Count}");
                }
            }
        }

        // If we found multiple releases, return them
        if (releases.Count > 1)
        {
            Console.WriteLine($"[DEBUG] Successfully found {releases.Count} releases");
            return releases;
        }

        // Fallback: extract a single release with all extras
        Console.WriteLine("[DEBUG] Falling back to single-release parsing");
        var allParsedExtras = ParseExtras(html);
        if (allParsedExtras.Count > 0)
        {
            releases.Add(new DvdRelease { Name = "Release", Extras = allParsedExtras });
        }

        return releases;
    }

    /// <summary>
    /// Extracts extras from a list of nodes (used internally for release-specific parsing).
    /// </summary>
    private static List<DvdExtras> ParseExtrasFromNodes(HtmlNodeCollection extrasNodes)
    {
        static string Normalize(string s) =>
            Regex.Replace(s ?? "", @"\s+", " ").Trim();

        var extrasSet = new HashSet<string>();
        var results = new List<DvdExtras>();

        foreach (var extrasNode in extrasNodes)
        {
            var text = HtmlEntity.DeEntitize(extrasNode.InnerText);

            // Split by newlines to handle each item separately
            var lines = Regex.Split(text, @"\r\n|\r|\n");

            foreach (var line in lines)
            {
                var normalizedLine = Normalize(line);

                // Skip lines that don't contain a time format - these aren't videos
                if (!Regex.IsMatch(normalizedLine, @"\(\d{1,2}:\d{2}(?::\d{2})?\)"))
                    continue;

                var match = Regex.Match(
                    normalizedLine,
                    @"(.*?)\s*\((\d{1,2}:\d{2}(?::\d{2})?)\)"
                );

                if (!match.Success)
                    continue;

                var title = match.Groups[1].Value.Trim();
                var duration = match.Groups[2].Value;

                // Strip leading dashes if present
                title = Regex.Replace(title, @"^-+\s*", "").Trim();

                // Clean up the title for display and filesystem use
                title = CleanupExtrasTitle(title);

                // Skip empty titles
                if (string.IsNullOrWhiteSpace(title))
                    continue;

                // Add to results only if not a duplicate
                var key = $"{title}|{duration}";
                if (extrasSet.Add(key))
                {
                    results.Add(new DvdExtras(title, duration));
                }
            }
        }

        return results;
    }

    private static string CleanupReleaseName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Unknown Release";

        // Remove HTML tags and clean up whitespace
        name = Regex.Replace(name, "<[^>]+>", "");
        name = Regex.Replace(name, @"\s+", " ").Trim();

        // Remove trailing colons or dashes
        name = Regex.Replace(name, @"[-:]\s*$", "").Trim();

        return name;
    }

    /// <summary>
    /// Parses extras/bonus features from DVD details page HTML.
    /// Extracts titles and durations, removes duplicates, and cleans up titles for filesystem use.
    /// </summary>
    /// <param name="html">HTML content from the DVD details page</param>
    /// <returns>List of unique extras with cleaned titles and durations</returns>
    public static List<DvdExtras> ParseExtras(string html)
    {
        var doc = new HtmlDocument
        {
            OptionDefaultStreamEncoding = Encoding.UTF8
        };
        doc.LoadHtml(html);

        static string Normalize(string s) =>
            Regex.Replace(s ?? "", @"\s+", " ").Trim();

        var extrasSet = new HashSet<string>(); // Track unique extras to avoid duplicates
        var results = new List<DvdExtras>();

        // Get ALL Extras blocks (Disc One, Disc Two, etc.)
        var extrasNodes = doc.DocumentNode.SelectNodes(
            "//div[@class='label' and normalize-space()='Extras:']/following-sibling::div[@class='description']"
        );

        if (extrasNodes == null)
            return results;

        foreach (var extrasNode in extrasNodes)
        {
            var text = HtmlEntity.DeEntitize(extrasNode.InnerText);
            
            // Split by newlines to handle each item separately
            // This prevents grouping unrelated items together
            var lines = Regex.Split(text, @"\r\n|\r|\n");

            foreach (var line in lines)
            {
                var normalizedLine = Normalize(line);
                
                // Skip lines that don't contain a time format - these aren't videos
                // Examples to skip: "Design Gallery (17 pages)", "(5 photos)", etc.
                if (!Regex.IsMatch(normalizedLine, @"\(\d{1,2}:\d{2}(?::\d{2})?\)"))
                    continue;

                // Match: Any text followed by time in parentheses (mm:ss) or (hh:mm:ss)
                // Examples: "Title" documentary (58:58), "Sub-title" featurette (5:26)
                var match = Regex.Match(
                    normalizedLine,
                    @"(.*?)\s*\((\d{1,2}:\d{2}(?::\d{2})?)\)"
                );

                if (!match.Success)
                    continue;

                var title = match.Groups[1].Value.Trim();
                var duration = match.Groups[2].Value;

                // Strip leading dashes if present
                title = Regex.Replace(title, @"^-+\s*", "").Trim();

                // Clean up the title for display and filesystem use
                title = CleanupExtrasTitle(title);

                // Skip empty titles
                if (string.IsNullOrWhiteSpace(title))
                    continue;

                // Add to results only if not a duplicate
                var key = $"{title}|{duration}";
                if (extrasSet.Add(key))
                {
                    results.Add(new DvdExtras(title, duration));
                }
            }
        }

        return results;
    }
    
    private static string NormalizeWhitespace(string input)
    {
        return Regex.Replace(input, @"\s+", " ").Trim();
    }

    /// <summary>
    /// Cleans up extras titles for display and filesystem use by removing:
    /// - Non-video parentheses like "(17 pages)" or "(5 photos)"
    /// - Quote marks (single and double)
    /// - "featurette" label
    /// - Invalid filesystem characters (Windows, Linux, macOS compatible)
    /// </summary>
    public static string CleanupExtrasTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return string.Empty;

        // Remove non-year parentheses: anything in () that doesn't contain a 4-digit year
        // This filters out things like "(17 pages)", "(5 photos)", "(57:48)", etc., while preserving years like "(1981)"
        // Negative lookahead (?!\d{4}) ensures we skip year-format parentheses
        title = Regex.Replace(title, @"\((?!\d{4})([^)]*)\)", "");

        // Remove quote marks (including Unicode variants: regular, curly, guillemets, etc.)
        // Unicode quotes: U+2018 ('), U+2019 ('), U+201C ("), U+201D ("), U+00AB («), U+00BB (»)
        title = Regex.Replace(title, "[\"'\\u2018\\u2019\\u201C\\u201D\\u00AB\\u00BB]", "");

        // Remove "featurette" label only (case-insensitive)
        title = Regex.Replace(title, @"\s+featurette\b", "", RegexOptions.IgnoreCase);
        title = Regex.Replace(title, @"^featurette\s+", "", RegexOptions.IgnoreCase);

        // Remove invalid filesystem characters (Windows, Linux, macOS compatible)
        // Invalid for Windows: < > : " / \ | ? *
        // Invalid for Linux/macOS: / (null char not relevant here)
        title = Regex.Replace(title, "[<>:\\\"\\\\/?*|]", "");

        // Clean up excess whitespace
        title = Regex.Replace(title, @"\s+", " ").Trim();

        return title;
    }

    /// <summary>
    /// Extracts extras from a single HTML node (used for multi-release parsing).
    /// Reuses the logic from ParseExtrasFromNodes for a single node.
    /// </summary>
    private static List<DvdExtras> ExtractExtrasFromSingleNode(HtmlNode node)
    {
        static string Normalize(string s) =>
            Regex.Replace(s ?? "", @"\s+", " ").Trim();

        var extrasSet = new HashSet<string>();
        var results = new List<DvdExtras>();

        var text = HtmlEntity.DeEntitize(node.InnerText);
        var lines = Regex.Split(text, @"\r\n|\r|\n");

        foreach (var line in lines)
        {
            var normalizedLine = Normalize(line);
            
            if (!Regex.IsMatch(normalizedLine, @"\(\d{1,2}:\d{2}(?::\d{2})?\)"))
                continue;

            var match = Regex.Match(
                normalizedLine,
                @"(.*?)\s*\((\d{1,2}:\d{2}(?::\d{2})?)\)"
            );

            if (!match.Success)
                continue;

            var title = match.Groups[1].Value.Trim();
            var duration = match.Groups[2].Value;

            title = Regex.Replace(title, @"^-+\s*", "").Trim();
            title = CleanupExtrasTitle(title);

            if (string.IsNullOrWhiteSpace(title))
                continue;

            var key = $"{title}|{duration}";
            if (extrasSet.Add(key))
            {
                results.Add(new DvdExtras(title, duration));
            }
        }

        return results;
    }
}