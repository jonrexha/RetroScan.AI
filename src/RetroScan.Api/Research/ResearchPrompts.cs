using System.Text;
using System.Text.RegularExpressions;
using RetroScan.Api.Data;
using RetroScan.Api.LanguageModel;
using RetroScan.Api.Sources;

namespace RetroScan.Api.Research;

public static partial class ResearchPrompts
{
    public const int HistoryTurns = 3;

    private const string SearchQueryInstructions = """
        You turn a research question into a Wikipedia search query.
        Use the earlier questions only to resolve references such as "it" or "they".
        Reply with the search query alone: at most eight words, no quotes, no explanation.
        """;

    private const string AnswerInstructions = """
        You are RetroScan, a research assistant running in a retro computer terminal.
        Answer the question using only the numbered Wikipedia sources below.
        Cite each claim with the number of its source, like [1] or [2][3].
        If the sources do not answer the question, say so plainly instead of guessing.
        Write plain text without markdown, at most 180 words.
        """;

    public static IReadOnlyList<ChatMessage> ForSearchQuery(IReadOnlyList<Question> history, string question)
    {
        var prompt = new StringBuilder();
        if (history.Count > 0)
        {
            prompt.AppendLine("Earlier questions:");
            foreach (var earlier in history)
            {
                prompt.AppendLine($"- {earlier.Text}");
            }

            prompt.AppendLine();
        }

        prompt.Append($"Question: {question}");
        return [ChatMessage.System(SearchQueryInstructions), ChatMessage.User(prompt.ToString())];
    }

    public static IReadOnlyList<ChatMessage> ForAnswer(
        IReadOnlyList<Question> history,
        string question,
        IReadOnlyList<SourceDocument> sources)
    {
        var system = new StringBuilder(AnswerInstructions).AppendLine().Append("Sources:");
        for (var i = 0; i < sources.Count; i++)
        {
            system.AppendLine().AppendLine().AppendLine($"[{i + 1}] {sources[i].Title}").Append(sources[i].Excerpt);
        }

        var messages = new List<ChatMessage> { ChatMessage.System(system.ToString()) };
        foreach (var earlier in history)
        {
            messages.Add(ChatMessage.User(earlier.Text));
            // Earlier answers cite earlier source lists; their [n] would point at the wrong sources now.
            messages.Add(ChatMessage.Assistant(Citation().Replace(earlier.Answer, "")));
        }

        messages.Add(ChatMessage.User(question));
        return messages;
    }

    public static string CleanSearchQuery(string completion, string fallback)
    {
        var firstLine = completion
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? "";
        var query = firstLine.Trim('"', '\'', '`', '.', ' ');
        if (query.Length == 0)
        {
            query = fallback.Trim();
        }

        return query.Length <= Question.SearchQueryMaxLength ? query : query[..Question.SearchQueryMaxLength];
    }

    [GeneratedRegex(@"\s*\[\d+\]")]
    private static partial Regex Citation();
}
