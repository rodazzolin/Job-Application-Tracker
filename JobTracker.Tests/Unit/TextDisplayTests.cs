using JobTracker.Helpers;
namespace JobTracker.Tests.Unit;

public class TextDisplayTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  \n\t ")]
    public void PreviewIsNullForBlankText(string? text)
    {
        Assert.Null(TextDisplay.Preview(text));
    }

    [Fact]
    public void ShortTextIsKeptButFlattenedToOneLine()
    {
        Assert.Equal("Recruiter call Tuesday. Ask about salary.",
            TextDisplay.Preview("  Recruiter call Tuesday.\n\n   Ask about salary.  "));
    }

    [Fact]
    public void TextAtExactlyTheLimitIsNotCut()
    {
        string text = new('a', TextDisplay.CardPreviewLength);
        Assert.Equal(text, TextDisplay.Preview(text));
    }

    [Fact]
    public void LongTextIsCutAtAWordWithEllipsis()
    {
        string text = string.Join(' ', Enumerable.Repeat("word", 50));   // 249 characters.
        string preview = TextDisplay.Preview(text)!;

        Assert.True(preview.Length <= TextDisplay.CardPreviewLength);
        Assert.EndsWith("word…", preview);   // Ends on a whole word, not "wo…".
    }

    [Fact]
    public void OneLongWordIsCutMidWord()
    {
        string preview = TextDisplay.Preview(new string('a', 300))!;

        Assert.Equal(TextDisplay.CardPreviewLength, preview.Length);
        Assert.EndsWith("a…", preview);
    }
}
