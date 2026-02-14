using System.Collections.Generic;
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
            var text = Normalize(HtmlEntity.DeEntitize(extrasNode.InnerText));

            // Match: Any text followed by time in parentheses (mm:ss) or (hh:mm:ss)
            // Examples: "Title" documentary (58:58), "Sub-title" featurette (5:26)
            var matches = Regex.Matches(
                text,
                @"(.*?)\s*\((\d{1,2}:\d{2}(?::\d{2})?)\)"
            );

            foreach (Match match in matches)
            {
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
    /// - Quote marks (single and double)
    /// - "featurette" label
    /// - Invalid filesystem characters (Windows, Linux, macOS compatible)
    /// </summary>
    public static string CleanupExtrasTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return string.Empty;

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
}