using System.Text.RegularExpressions;
namespace JobTracker.Helpers;

// Short text previews, e.g. a job's notes on its board card.
public static partial class TextDisplay
{
    // Longest preview shown on a board card, ellipsis included.
    public const int CardPreviewLength = 100;

    // Characters to fetch from the database for a preview: extra room, because collapsing
    // whitespace (blank lines, indentation) can shrink the text.
    public const int CardPreviewFetchLength = CardPreviewLength * 2;

    // One line of text, at most `maxLength` characters: line breaks and runs of spaces become
    // single spaces, and longer text is cut at a word boundary (when there's one nearby) with "…".
    public static string? Preview(string? text, int maxLength = CardPreviewLength)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        string flat = Whitespace().Replace(text, " ").Trim();
        if (flat.Length <= maxLength)
            return flat;

        string cut = flat[..(maxLength - 1)];   // Leave room for the ellipsis.
        int lastSpace = cut.LastIndexOf(' ');
        if (lastSpace >= maxLength / 2)          // Don't drop half the preview to avoid splitting one long word.
            cut = cut[..lastSpace];
        return cut.TrimEnd() + "…";
    }

    // Source-generated regex: compiled at build time instead of at runtime.
    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
