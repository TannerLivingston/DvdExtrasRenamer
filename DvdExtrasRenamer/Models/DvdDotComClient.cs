using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace DvdExtrasRenamer.Models;

/// <summary>
/// HTTP client for interacting with dvdcompare.net.
/// Provides methods to search for DVDs and fetch detailed information including extras.
/// </summary>
public class DvdDotComClient
{
    private HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="DvdDotComClient"/> class.
    /// Configures HTTP client with automatic decompression and cookie handling.
    /// </summary>
    public DvdDotComClient()
    {
        var handler = new HttpClientHandler
        {
            UseCookies = false,
            CookieContainer = new CookieContainer(),
            AutomaticDecompression = DecompressionMethods.All
        };
        
        _httpClient = new HttpClient(handler);
    }
    
    /// <summary>
    /// Searches dvdcompare.net for DVDs matching the specified criteria.
    /// </summary>
    /// <param name="title">DVD title to search for (required)</param>
    /// <param name="director">Director name (optional)</param>
    /// <param name="year">Release year (optional)</param>
    /// <param name="country">Country of origin (optional)</param>
    /// <param name="company">Production company (optional)</param>
    /// <param name="edition">Edition type (optional)</param>
    /// <param name="andOr">Search logic: "and" requires all criteria, "or" allows any match</param>
    /// <returns>HTML content of search results page</returns>
    public async Task<string> LookupDvdAsync(string title = "", string director = "", string year = "",
        string country = "", string company = "", string edition = "", string andOr = "and")
    {
        try
        {
            var content = new FormUrlEncodedContent([
                new KeyValuePair<string, string>("title_search", title),
                new KeyValuePair<string, string>("director_search", director),
                new KeyValuePair<string, string>("year_search", year),
                new KeyValuePair<string, string>("country_search", country),
                new KeyValuePair<string, string>("company_search", company),
                new KeyValuePair<string, string>("edition_search", edition),
                new KeyValuePair<string, string>("and_or", andOr)
            ]);

            using var client = new HttpClient();

            var response = await client.PostAsync(
                "https://www.dvdcompare.net/comparisons/adv_search_results.php",
                content
            );
            return await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine(e.Message);
            return string.Empty;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return string.Empty;
        }
    }

    /// <summary>
    /// Fetches detailed information for a specific DVD, including all extras and bonus features.
    /// </summary>
    /// <param name="href">Relative URL path to the DVD comparison page</param>
    /// <returns>HTML content of the DVD details page</returns>
    public async Task<string> FetchDvdDetailsAsync(string href)
    {
        Console.WriteLine($"Fetching DVD details with href  {href}");
        try
        {
            var fullUrl = $"https://www.dvdcompare.net/comparisons/{href}";
            var response = await _httpClient.GetAsync(fullUrl);
            return await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine(e.Message);
            return string.Empty;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return string.Empty;
        }
    }
}
