using RetroScan.Api.LanguageModel;
using RetroScan.Api.Sources;

namespace RetroScan.Api.Tests.Api;

public sealed class FakeLanguageModel : ILanguageModel
{
    public const string SearchQuery = "shark";
    public const string Answer = "Sharks are cartilaginous fishes [1], and the largest ones filter feed [2].";

    public List<IReadOnlyList<ChatMessage>> Calls { get; } = [];
    public Exception? Failure { get; set; }

    public Task<string> CompleteAsync(IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken)
    {
        Calls.Add(messages);
        if (Failure is not null)
        {
            throw Failure;
        }

        var isAnswerPrompt = messages[0].Content.Contains("Sources:");
        return Task.FromResult(isAnswerPrompt ? Answer : SearchQuery);
    }

    public void Reset()
    {
        Calls.Clear();
        Failure = null;
    }
}

public sealed class FakeSourceSearch : ISourceSearch
{
    public static readonly IReadOnlyList<SourceDocument> Sharks =
    [
        new("Shark", "https://en.wikipedia.org/wiki/Shark", "Sharks are a group of cartilaginous fishes."),
        new("Whale shark", "https://en.wikipedia.org/wiki/Whale_shark", "The whale shark is a filter-feeding shark."),
    ];

    public List<string> Queries { get; } = [];
    public IReadOnlyList<SourceDocument> Results { get; set; } = Sharks;

    public Task<IReadOnlyList<SourceDocument>> SearchAsync(string query, int limit, CancellationToken cancellationToken)
    {
        Queries.Add(query);
        return Task.FromResult<IReadOnlyList<SourceDocument>>(Results.Take(limit).ToList());
    }

    public void Reset()
    {
        Queries.Clear();
        Results = Sharks;
    }
}
