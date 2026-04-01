using CowSay.Core.Services;
using Shouldly;

namespace Cowsay.Core.Tests;

public class SpeechBubbleBuilderTests
{
    [Fact]
    public void Build_SimpleMessage_ReturnsNonNull()
    {
        var result = SpeechBubbleBuilder.Build("Hello", false);

        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
    }

    [Fact]
    public void Build_SimpleMessage_ContainsMessage()
    {
        var result = SpeechBubbleBuilder.Build("Hello", false);

        result.ShouldContain("Hello");
    }

    [Fact]
    public void Build_Speech_UsesPipeBorders()
    {
        var result = SpeechBubbleBuilder.Build("Test", false);

        // SpeechBubbleBuilder always uses | borders (documented bug)
        result.ShouldContain("| Test |");
    }

    [Fact]
    public void Build_Thought_AlsoUsesPipeBorders()
    {
        // This documents the bug: thought mode also uses | instead of ( )
        var result = SpeechBubbleBuilder.Build("Test", true);

        result.ShouldContain("| Test |");
    }

    [Fact]
    public void Build_TopBorder_UsesUnderscores()
    {
        var result = SpeechBubbleBuilder.Build("Hi", false);
        var lines = result.Split('\n');

        lines[0].ShouldContain("_");
    }

    [Fact]
    public void Build_BottomBorder_UsesDashes()
    {
        var result = SpeechBubbleBuilder.Build("Hi", false);
        var lines = result.Split('\n');

        lines[^1].ShouldContain("-");
    }

    [Fact]
    public void Build_LongWord_SplitsAtMaxWidth()
    {
        var longWord = new string('A', 50); // Exceeds MaxWidth of 40
        var result = SpeechBubbleBuilder.Build(longWord, false);
        var lines = result.Split('\n');

        // Should have at least 4 lines: top border, 2 content lines, bottom border
        lines.Length.ShouldBeGreaterThanOrEqualTo(4);
    }

    [Fact]
    public void Build_WhitespaceMessage_ThrowsInvalidOperationException()
    {
        // SplitMessage returns empty array for whitespace-only,
        // which causes .Max() to fail — documenting this known behavior
        Should.Throw<InvalidOperationException>(() => SpeechBubbleBuilder.Build("  ", false));
    }

    [Fact]
    public void Build_MultipleWords_WrapsAt40Chars()
    {
        var message = string.Join(" ", Enumerable.Repeat("word", 20));
        var result = SpeechBubbleBuilder.Build(message, false);
        var lines = result.Split('\n').Where(l => l.StartsWith("|")).ToArray();

        // Each content line (between pipes) should be <= 40 chars of content
        foreach (var line in lines)
        {
            // Line format: "| content |" — content is between first | and last |
            var content = line.TrimStart('|').TrimEnd('|').Trim();
            content.Length.ShouldBeLessThanOrEqualTo(40);
        }
    }
}
