using System.Net;
using System.Text.Json;
using RetroScan.Api.Errors;
using RetroScan.Api.Sources;

namespace RetroScan.Api.Tests.Sources;

public sealed class WikipediaSourceSearchTests
{
    private const string ThreePages = """
        {"batchcomplete":true,"query":{"pages":[
          {"pageid":2,"title":"Whale shark","fullurl":"https://en.wikipedia.org/wiki/Whale_shark","extract":"The whale shark is a filter feeder.","index":2},
          {"pageid":1,"title":"Shark","fullurl":"https://en.wikipedia.org/wiki/Shark","extract":"Sharks are a group of fishes.","index":1},
          {"pageid":3,"title":"Shark (disambiguation)","fullurl":"https://en.wikipedia.org/wiki/Shark_(disambiguation)","extract":"","index":3}
        ]}}
        """;

    private static WikipediaSourceSearch Search(StubHttpHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://en.wikipedia.test/") });

    [Fact]
    public async Task Returns_pages_in_search_rank_order_and_skips_empty_extracts()
    {
        var sources = await Search(new StubHttpHandler(HttpStatusCode.OK, ThreePages))
            .SearchAsync("shark", 3, CancellationToken.None);

        Assert.Equal(["Shark", "Whale shark"], sources.Select(source => source.Title));
        Assert.Equal("https://en.wikipedia.org/wiki/Shark", sources[0].Url);
        Assert.Equal("Sharks are a group of fishes.", sources[0].Excerpt);
    }

    [Fact]
    public async Task Escapes_the_query_and_asks_for_plain_text_introductions()
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK, ThreePages);

        await Search(handler).SearchAsync("sharks & rays", 3, CancellationToken.None);

        var url = Assert.Single(handler.Requests).RequestUri!.AbsoluteUri;
        Assert.StartsWith("https://en.wikipedia.test/w/api.php?", url);
        Assert.Contains("gsrsearch=sharks%20%26%20rays", url);
        Assert.Contains("gsrlimit=3", url);
        Assert.Contains("exintro=1", url);
        Assert.Contains("explaintext=1", url);
    }

    [Fact]
    public async Task Returns_nothing_when_the_search_has_no_hits()
    {
        var sources = await Search(new StubHttpHandler(HttpStatusCode.OK, """{"batchcomplete":true}"""))
            .SearchAsync("qwxzzy", 3, CancellationToken.None);

        Assert.Empty(sources);
    }

    [Fact]
    public async Task Shortens_long_extracts_at_a_word_boundary()
    {
        var extract = string.Concat(Enumerable.Repeat("cartilage ", 300));
        var page = JsonSerializer.Serialize(new
        {
            query = new { pages = new[] { new { title = "Shark", fullurl = "https://en.wikipedia.org/wiki/Shark", extract, index = 1 } } },
        });

        var source = Assert.Single(await Search(new StubHttpHandler(HttpStatusCode.OK, page))
            .SearchAsync("shark", 3, CancellationToken.None));

        Assert.True(source.Excerpt.Length <= WikipediaSourceSearch.ExcerptMaxLength + 3);
        Assert.EndsWith("cartilage...", source.Excerpt);
    }

    [Fact]
    public async Task Reports_an_upstream_failure_when_wikipedia_is_down()
    {
        var search = Search(new StubHttpHandler(HttpStatusCode.ServiceUnavailable, ""));

        var exception = await Assert.ThrowsAsync<UpstreamServiceException>(
            () => search.SearchAsync("shark", 3, CancellationToken.None));
        Assert.Equal("Wikipedia", exception.Service);
    }
}
