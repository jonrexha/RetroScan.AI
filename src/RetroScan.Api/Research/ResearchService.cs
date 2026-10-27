using Microsoft.EntityFrameworkCore;
using RetroScan.Api.Contracts;
using RetroScan.Api.Data;
using RetroScan.Api.LanguageModel;
using RetroScan.Api.Sources;

namespace RetroScan.Api.Research;

public sealed class ResearchService(
    RetroScanDbContext db,
    ILanguageModel languageModel,
    ISourceSearch sourceSearch,
    TimeProvider clock)
{
    public const int SourceLimit = 3;
    public const int TitleMaxLength = 80;

    public async Task<QuestionResponse?> AskAsync(
        Guid ownerId,
        Guid sessionId,
        string text,
        CancellationToken cancellationToken)
    {
        var session = await db.Sessions
            .Include(s => s.Questions.OrderByDescending(question => question.Id).Take(ResearchPrompts.HistoryTurns))
            .SingleOrDefaultAsync(s => s.Id == sessionId && s.OwnerId == ownerId, cancellationToken);
        if (session is null)
        {
            return null;
        }

        var askedAt = clock.UtcNowForDatabase();
        text = text.Trim();
        var history = session.Questions.OrderBy(question => question.Id).ToList();

        var searchQuery = ResearchPrompts.CleanSearchQuery(
            await languageModel.CompleteAsync(ResearchPrompts.ForSearchQuery(history, text), cancellationToken),
            fallback: text);
        var sources = await sourceSearch.SearchAsync(searchQuery, SourceLimit, cancellationToken);
        var answer = sources.Count == 0
            ? $"No Wikipedia articles matched \"{searchQuery}\". Try asking in different words."
            : ResearchPrompts.NormalizeCitations(
                await languageModel.CompleteAsync(ResearchPrompts.ForAnswer(history, text, sources), cancellationToken));

        var question = new Question
        {
            Text = text,
            SearchQuery = searchQuery,
            Answer = answer,
            AskedAt = askedAt,
            Sources = sources
                .Select((source, index) => new Source
                {
                    Number = index + 1,
                    Title = source.Title,
                    Url = source.Url,
                    Excerpt = source.Excerpt,
                })
                .ToList(),
        };

        session.Questions.Add(question);
        if (session.Title.Length == 0)
        {
            session.Title = TitleFrom(text);
        }

        session.UpdatedAt = clock.UtcNowForDatabase();
        await db.SaveChangesAsync(cancellationToken);
        return question.ToResponse();
    }

    public static string TitleFrom(string question) =>
        question.Length <= TitleMaxLength ? question : question[..(TitleMaxLength - 3)].TrimEnd() + "...";
}
