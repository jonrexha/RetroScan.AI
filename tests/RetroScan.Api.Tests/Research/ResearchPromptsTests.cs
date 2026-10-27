using RetroScan.Api.Data;
using RetroScan.Api.Research;
using RetroScan.Api.Sources;

namespace RetroScan.Api.Tests.Research;

public sealed class ResearchPromptsTests
{
    private static readonly IReadOnlyList<SourceDocument> Sources =
    [
        new("Shark", "https://en.wikipedia.org/wiki/Shark", "Sharks are a group of fishes."),
        new("Whale shark", "https://en.wikipedia.org/wiki/Whale_shark", "The whale shark is a filter feeder."),
    ];

    private static readonly IReadOnlyList<Question> History =
        [new Question { Text = "What are sharks?", Answer = "Sharks are fishes [1] and some filter feed [2]." }];

    [Fact]
    public void Answer_prompt_numbers_the_sources_in_order()
    {
        var system = ResearchPrompts.ForAnswer([], "What are sharks?", Sources)[0];

        Assert.Equal("system", system.Role);
        Assert.Contains("[1] Shark\nSharks are a group of fishes.", system.Content.ReplaceLineEndings("\n"));
        Assert.Contains("[2] Whale shark\nThe whale shark is a filter feeder.", system.Content.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void Answer_prompt_replays_earlier_turns_without_their_old_citations()
    {
        var messages = ResearchPrompts.ForAnswer(History, "How long do they live?", Sources);

        Assert.Equal(["system", "user", "assistant", "user"], messages.Select(message => message.Role));
        Assert.Equal("What are sharks?", messages[1].Content);
        Assert.Equal("Sharks are fishes and some filter feed.", messages[2].Content);
        Assert.Equal("How long do they live?", messages[3].Content);
    }

    [Fact]
    public void Search_prompt_lists_earlier_questions_to_resolve_references()
    {
        var prompt = ResearchPrompts.ForSearchQuery(History, "How long do they live?")[1].Content;

        Assert.Contains("- What are sharks?", prompt);
        Assert.EndsWith("Question: How long do they live?", prompt);
    }

    [Fact]
    public void Search_prompt_for_a_first_question_is_just_the_question()
    {
        var prompt = ResearchPrompts.ForSearchQuery([], "What are sharks?")[1].Content;

        Assert.Equal("Question: What are sharks?", prompt);
    }

    [Theory]
    [InlineData("shark lifespan", "shark lifespan")]
    [InlineData("\"shark lifespan\"", "shark lifespan")]
    [InlineData("shark lifespan.\nI picked these words because...", "shark lifespan")]
    [InlineData(" \n ", "How long do sharks live?")]
    public void Clean_search_query_keeps_the_first_line_or_falls_back_to_the_question(string completion, string expected)
    {
        Assert.Equal(expected, ResearchPrompts.CleanSearchQuery(completion, "How long do sharks live?"));
    }

    [Theory]
    [InlineData("within the class Chondrichthyes【1】.", "within the class Chondrichthyes[1].")]
    [InlineData("about 272 to 510 years【2†L3-L8】【3】", "about 272 to 510 years[2][3]")]
    [InlineData("already plain [1].", "already plain [1].")]
    public void Model_specific_citations_become_square_brackets(string answer, string expected)
    {
        Assert.Equal(expected, ResearchPrompts.NormalizeCitations(answer));
    }

    [Fact]
    public void Clean_search_query_fits_the_column()
    {
        var query = ResearchPrompts.CleanSearchQuery(new string('a', 500), "fallback");

        Assert.Equal(Question.SearchQueryMaxLength, query.Length);
    }
}
