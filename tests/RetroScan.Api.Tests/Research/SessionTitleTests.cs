using RetroScan.Api.Research;

namespace RetroScan.Api.Tests.Research;

public sealed class SessionTitleTests
{
    [Theory]
    [InlineData(10, 10)]
    [InlineData(80, 80)]
    [InlineData(200, 80)]
    public void Titles_from_long_questions_are_cut_to_eighty_characters(int questionLength, int titleLength)
    {
        var title = ResearchService.TitleFrom(new string('q', questionLength));

        Assert.Equal(titleLength, title.Length);
        Assert.Equal(questionLength > 80, title.EndsWith("..."));
    }
}
