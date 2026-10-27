using System.Text.Json;
using RetroScan.Api.Errors;

namespace RetroScan.Api.Sources;

public sealed class WikipediaSourceSearch(HttpClient http) : ISourceSearch
{
    public const int ExcerptMaxLength = 1200;

    public async Task<IReadOnlyList<SourceDocument>> SearchAsync(string query, int limit, CancellationToken cancellationToken)
    {
        var url = "w/api.php?action=query&format=json&formatversion=2&redirects=1"
                  + $"&generator=search&gsrsearch={Uri.EscapeDataString(query)}&gsrlimit={limit}"
                  + $"&prop=extracts%7Cinfo&exintro=1&explaintext=1&exlimit={limit}&inprop=url";

        SearchResponse? response;
        try
        {
            response = await http.GetFromJsonAsync<SearchResponse>(url, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException
                                          || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            throw new UpstreamServiceException("Wikipedia", exception.GetType().Name, exception);
        }

        return response?.Query?.Pages
            .Where(page => !string.IsNullOrWhiteSpace(page.Extract))
            .OrderBy(page => page.Index)
            .Select(page => new SourceDocument(page.Title, page.FullUrl, Shorten(page.Extract!)))
            .ToList() ?? [];
    }

    private static string Shorten(string extract)
    {
        extract = extract.Trim();
        if (extract.Length <= ExcerptMaxLength)
        {
            return extract;
        }

        var cut = extract.LastIndexOf(' ', ExcerptMaxLength);
        return string.Concat(extract.AsSpan(0, cut > 0 ? cut : ExcerptMaxLength), "...");
    }

    private sealed record SearchResponse(SearchQuery? Query);

    private sealed record SearchQuery(IReadOnlyList<Page> Pages);

    private sealed record Page(string Title, string FullUrl, string? Extract, int Index);
}
