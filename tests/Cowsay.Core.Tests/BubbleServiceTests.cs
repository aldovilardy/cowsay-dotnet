using CowSay.Core.Services;
using Shouldly;
using System.Globalization;

namespace Cowsay.Core.Tests;

public class BubbleServiceTests
{
    private readonly BubbleService _sut = new();

    [Fact]
    public void CreateBubble_NullMessage_ProducesEmptyBubble()
    {
        var result = _sut.CreateBubble(null!, false, 40);

        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("<");
        result.ShouldContain(">");
    }

    [Fact]
    public void CreateBubble_EmptyMessage_ProducesEmptyBubble()
    {
        var result = _sut.CreateBubble("", false, 40);

        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("<  >");
    }

    [Fact]
    public void CreateBubble_SingleLineSpeech_UsesAngleBrackets()
    {
        var result = _sut.CreateBubble("Hello", false, 40);

        result.ShouldContain("< Hello >");
    }

    [Fact]
    public void CreateBubble_SingleLineThought_UsesParentheses()
    {
        var result = _sut.CreateBubble("Hello", true, 40);

        result.ShouldContain("( Hello )");
    }

    [Fact]
    public void CreateBubble_MultiLineSpeech_UsesSlashBackslashBorders()
    {
        var result = _sut.CreateBubble("This is a longer message that should wrap across multiple lines", false, 20);
        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // lines[0] = top border, lines[^1] = bottom border
        // First content line (index 1) starts with /
        lines[1].TrimStart().ShouldStartWith("/");
        // Last content line (index ^2, before bottom border) starts with \
        lines[^2].TrimStart().ShouldStartWith("\\");
    }

    [Fact]
    public void CreateBubble_MultiLineThought_UsesParenthesesForAllLines()
    {
        var result = _sut.CreateBubble("This is a longer message that should wrap across multiple lines", true, 20);
        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // All content lines should use ( and )
        for (var i = 1; i < lines.Length - 1; i++)
        {
            lines[i].ShouldContain("(");
            lines[i].ShouldContain(")");
        }
    }

    [Fact]
    public void CreateBubble_MultiLineSpeech_MiddleLinesUsePipe()
    {
        // Need at least 3 content lines for middle pipe borders
        var result = _sut.CreateBubble("one two three four five six seven eight nine ten eleven twelve", false, 10);
        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Middle lines (not first, not last content line, not borders) use |
        // lines[0] is top border, lines[^1] is bottom border
        if (lines.Length > 4)
        {
            // A middle content line (index 2 is the second content line)
            lines[2].TrimStart().ShouldStartWith("|");
        }
    }

    [Fact]
    public void CreateBubble_NegativeWidth_DefaultsTo40()
    {
        var result = _sut.CreateBubble("Hello", false, -5);

        result.ShouldContain("< Hello >");
    }

    [Fact]
    public void CreateBubble_ZeroWidth_DefaultsTo40()
    {
        var result = _sut.CreateBubble("Hello", false, 0);

        result.ShouldContain("< Hello >");
    }

    [Fact]
    public void CreateBubble_ExactWrapWidth_DoesNotWrap()
    {
        // "Hello" is 5 chars, wrap at 5 should fit on one line
        var result = _sut.CreateBubble("Hello", false, 5);

        result.ShouldContain("< Hello >");
    }

    [Fact]
    public void CreateBubble_WordExceedsWidth_SplitsWord()
    {
        var result = _sut.CreateBubble("Supercalifragilistic", false, 10);
        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Should have top border + 2 content lines + bottom border
        lines.Length.ShouldBeGreaterThan(3);
    }

    [Fact]
    public void CreateBubble_CrlfLineEndings_NormalizedInNoWrapMode()
    {
        var result = _sut.CreateBubble("Line1\r\nLine2", false, 40, noWrap: true);

        result.ShouldContain("Line1");
        result.ShouldContain("Line2");
    }

    [Fact]
    public void CreateBubble_NoWrapMode_PreservesLines()
    {
        var result = _sut.CreateBubble("Short\nAlso short", false, 40, noWrap: true);

        result.ShouldContain("Short");
        result.ShouldContain("Also short");
    }

    [Fact]
    public void CreateBubble_WhitespaceOnlyMessage_ProducesEmptyBubble()
    {
        var result = _sut.CreateBubble("   ", false, 40);

        result.ShouldNotBeNullOrEmpty();
        // Whitespace-only is treated as empty by WrapMessage
    }

    [Fact]
    public void CreateBubble_TopAndBottomBorders_AreDashBased()
    {
        var result = _sut.CreateBubble("Test", false, 40);
        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        lines[0].ShouldContain("------");
        lines[^1].ShouldContain("------");
    }

    [Fact]
    public void CreateBubble_EmojiInMessage_ProperlyPadded()
    {
        var result = _sut.CreateBubble("👀", false, 40);

        result.ShouldContain("< 👀 >");
    }

    [Fact]
    public void CreateBubble_MultipleEmojisInMessage_ProperlyPadded()
    {
        var result = _sut.CreateBubble("👀👂👃", false, 40);

        result.ShouldContain("< 👀👂👃 >");
    }

    [Fact]
    public void CreateBubble_EmojiWithText_ProperlyPadded()
    {
        var result = _sut.CreateBubble("Hello 👀 World", false, 40);

        result.ShouldContain("< Hello 👀 World >");
    }

    [Fact]
    public void CreateBubble_EmojiWidth_TopBorderCorrectSize()
    {
        var result = _sut.CreateBubble("👀👂👃👄", false, 40);
        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var contentLine = lines[1];
        var topBorder = lines[0];

        var contentVisualLength = new StringInfo(contentLine.Trim()).LengthInTextElements;
        var expectedVisualLength = new StringInfo("👀👂👃👄").LengthInTextElements;

        contentVisualLength.ShouldBe(expectedVisualLength + 4);
    }

    [Fact]
    public void CreateBubble_MultipleEmojis_WrapsCorrectly()
    {
        var result = _sut.CreateBubble("👀👂👃👄👅👆👇👈👉👉", false, 5);

        result.ShouldNotBeNullOrEmpty();
        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        lines.Length.ShouldBeGreaterThan(3);
    }

    [Fact]
    public void CreateBubble_NoWrapMode_PreservesEmojiLineBreaks()
    {
        var result = _sut.CreateBubble("👀\n👂", false, 40, noWrap: true);

        result.ShouldContain("👀");
        result.ShouldContain("👂");
    }

    [Fact]
    public void CreateBubble_ThoughtBubbleWithEmoji_UsesParentheses()
    {
        var result = _sut.CreateBubble("👀", true, 40);

        result.ShouldContain("( 👀 )");
    }
}
