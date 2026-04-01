using CowSay.Core.Services;
using Shouldly;
using Xunit;

namespace Cowsay.Tests;

public class BubbleServiceTests
{
    private readonly BubbleService _sut = new();

    [Fact]
    public void Default_Wrap_At_40_Columns()
    {
        // 50-char message should wrap
        var message = new string('a', 20) + " " + new string('b', 20) + " " + new string('c', 20);
        var result = _sut.CreateBubble(message, isThought: false, wrapWidth: 40);

        // The bubble should contain multiple content lines
        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        // Top border + at least 2 content lines + bottom border = at least 4 lines
        lines.Length.ShouldBeGreaterThanOrEqualTo(4, $"Expected at least 4 lines, got {lines.Length}");
    }

    [Fact]
    public void Custom_Wrap_Width_Respected()
    {
        var message = "hello world foo bar";
        var result = _sut.CreateBubble(message, isThought: false, wrapWidth: 10);
        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        // Should wrap into multiple lines at width 10
        lines.Length.ShouldBeGreaterThanOrEqualTo(4, $"Expected at least 4 lines, got {lines.Length}");
    }

    [Fact]
    public void NoWrap_Preserves_Lines()
    {
        var message = "line one\nline two\nline three";
        var result = _sut.CreateBubble(message, isThought: false, wrapWidth: 40, noWrap: true);
        result.ShouldContain("line one");
        result.ShouldContain("line two");
        result.ShouldContain("line three");
    }

    [Fact]
    public void Single_Line_Speech_Uses_Angle_Brackets()
    {
        var result = _sut.CreateBubble("hello", isThought: false, wrapWidth: 40);
        result.ShouldContain("< hello >");
    }

    [Fact]
    public void Multi_Line_Speech_Uses_Slash_Pipe_Borders()
    {
        var message = "first line is here and second line follows after wrapping";
        var result = _sut.CreateBubble(message, isThought: false, wrapWidth: 20);
        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // First content line uses /  \
        // Middle content lines use |  |
        // Last content line uses \  /
        var contentLines = lines.Skip(1).Take(lines.Length - 2).ToArray();
        contentLines.Length.ShouldBeGreaterThanOrEqualTo(2);
        contentLines[0].TrimStart().ShouldStartWith("/");
        contentLines[^1].TrimStart().ShouldStartWith("\\");
    }

    [Fact]
    public void Thought_Mode_Uses_Parentheses()
    {
        var result = _sut.CreateBubble("hello", isThought: true, wrapWidth: 40);
        result.ShouldContain("( hello )");
    }

    [Fact]
    public void Thought_Mode_Multi_Line_Uses_Parentheses()
    {
        var message = "this is a thought that should wrap nicely";
        var result = _sut.CreateBubble(message, isThought: true, wrapWidth: 15);
        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var contentLines = lines.Skip(1).Take(lines.Length - 2).ToArray();

        foreach (var line in contentLines)
        {
            line.TrimStart().ShouldStartWith("(");
            line.TrimEnd().ShouldEndWith(")");
        }
    }

    [Fact]
    public void Empty_Message_Produces_Valid_Bubble()
    {
        var result = _sut.CreateBubble("", isThought: false, wrapWidth: 40);
        result.ShouldNotBeNull();
        result.ShouldContain("<");
        result.ShouldContain(">");
    }

    [Fact]
    public void Zero_WrapWidth_Falls_Back_To_40()
    {
        // wrapWidth <= 0 should default to 40
        var result = _sut.CreateBubble("hello", isThought: false, wrapWidth: 0);
        result.ShouldContain("< hello >");
    }

    [Fact]
    public void Long_Word_Gets_Split()
    {
        var longWord = new string('x', 60);
        var result = _sut.CreateBubble(longWord, isThought: false, wrapWidth: 40);
        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        // Should be split across multiple lines
        lines.Length.ShouldBeGreaterThanOrEqualTo(4);
    }
}
