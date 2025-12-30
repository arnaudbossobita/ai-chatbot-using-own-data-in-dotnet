using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using ChatBot.Models;

namespace ChatBot.Services;

public partial class WikipediaClientChunk
{
    private static readonly HttpClient WikipediaHttpClient = new();

    static WikipediaClientChunk()
    {
        WikipediaHttpClient.DefaultRequestHeaders.UserAgent.Clear();
        WikipediaHttpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AICourseBot", "1.0")); // Application name and version
        WikipediaHttpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(contact:you@example.com)")); //This is to tell Wikipedia who we are, just being a good citizen
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Following are classes to deserialize Wikipedia API response
    private sealed class WikiApiResponse
    {
        [JsonPropertyName("query")]
        public WikiQuery? Query { get; set; }
    }

    private sealed class WikiQuery
    {
        [JsonPropertyName("pages")]
        public List<WikiPage> Pages { get; set; } = new();
    }

    private sealed class WikiPage
    {
        [JsonPropertyName("pageid")]
        public long? PageId { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("extract")]
        public string? Extract { get; set; }

        [JsonPropertyName("missing")]
        public bool? Missing { get; set; }
    }

    /// <summary>
    /// Creates a Wikipedia API URL to fetch the page extract for a given title.
    /// </summary>
    /// <param name="pageTitle"></param>
    /// <param name="full"></param>
    /// <returns></returns>
    static string CreateWikipediaUrl(string pageTitle, bool full)
    {
        var ub = new UriBuilder("https://en.wikipedia.org/w/api.php");
        var qs = new Dictionary<string, string>
        {
            ["action"] = "query",
            ["prop"] = "extracts",
            ["format"] = "json",
            ["formatversion"] = "2",
            ["redirects"] = "1",
            ["explaintext"] = "1",
            // Keep wiki-style headings like "== History =="
            ["exsectionformat"] = "wiki",
            ["titles"] = pageTitle
        };

        // If NOT full, only fetch the intro
        if (!full)
            qs["exintro"] = "1";

        ub.Query = string.Join("&", qs.Select(kv => $"{WebUtility.UrlEncode(kv.Key)}={WebUtility.UrlEncode(kv.Value)}"));
        return ub.ToString();
    }

    /// <summary>
    /// Fetches a Wikipedia page given its API URL and returns a Document object.
    /// </summary>
    /// <param name="url">The Wikipedia API URL.</param>
    /// <returns>A Document representing the Wikipedia page.</returns>
    static async Task<Document> GetWikipediaPage(string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await WikipediaHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<WikiApiResponse>(json, JsonOpts)
                  ?? throw new InvalidOperationException("Failed to deserialize Wikipedia response.");

        var firstPage = apiResponse.Query?.Pages?.FirstOrDefault();

        if (firstPage is null || firstPage.Missing is true)
            throw new Exception($"Could not find a Wikipedia page for {url}");

        if (string.IsNullOrWhiteSpace(firstPage.Title) || string.IsNullOrWhiteSpace(firstPage.Extract))
            throw new Exception($"Empty Wikipedia page returned for {url}");

        var title = firstPage.Title!;
        var content = firstPage.Extract!.Trim();

        var id = Utils.ToUrlSafeId(title);
        var pageUrl = $"https://en.wikipedia.org/wiki/{Uri.EscapeDataString(title.Replace(' ', '_'))}";

        return new Document(
            Id: id,
            Title: title,
            Content: content,
            PageUrl: pageUrl
        );
    }

    /// <summary>
    /// Fetches a Wikipedia page for a given title.
    /// </summary>
    /// <param name="title">The title of the Wikipedia page.</param>
    /// <param name="full">Whether to fetch the full page or just the introduction.</param>
    /// <returns>A Document representing the Wikipedia page.</returns>
    public Task<Document> GetWikipediaPageForTitle(string title, bool full = false)
    {
        var url = CreateWikipediaUrl(title, full);
        return GetWikipediaPage(url);
    }

    /// <summary>
    /// Regular expression to match Wikipedia-style headings (e.g., "== History ==").
    /// </summary>
    [GeneratedRegex(@"^\s*=+\s*(.+?)\s*=+\s*", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex HeadingRegex();

    /// <summary>
    /// Splits the article text into sections based on Wikipedia-style headings.
    /// </summary>
    /// <param name="articleText">The full text of the Wikipedia article.</param>
    /// <returns>An enumerable of section titles and their corresponding content.</returns>
    public IEnumerable<(string Title, string Content)> SplitIntoSections(string articleText)
    {
        var matches = HeadingRegex().Matches(articleText);

        if (matches.Count == 0)
        {
            yield return ("Introduction", articleText[..]);
            yield break;
        }

        // Returns any text before the first markdown heading as "Introduction"
        if (matches[0].Index > 0)
            yield return ("Introduction", articleText[..matches[0].Index]);

        for (int i = 0; i < matches.Count; i++)
        {
            var m = matches[i];
            string sectionName = m.Groups[1].Value.Trim();
            if (sectionName is "See also" or "References" or "External links" or "Notes")
                continue;

            int bodyStart = m.Index + m.Length;
            int bodyEnd = (i < matches.Count - 1) ? matches[i + 1].Index : articleText.Length;
            int length = bodyEnd - bodyStart;
            var content = articleText.Substring(bodyStart, length);
            yield return (sectionName, content);
        }
    }
}
