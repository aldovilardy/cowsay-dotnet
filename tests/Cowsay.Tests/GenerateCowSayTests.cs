using Cowsay.Application.DTOs;
using Cowsay.Application.Handlers;
using Cowsay.Infrastructure.Repositories;
using CowSay.Core.Models;
using CowSay.Core.Services;
using Shouldly;
using Xunit;

namespace Cowsay.Tests;

public class GenerateCowSayTests
{
    private static string FindCowsDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "assets", "cows");
            if (Directory.Exists(candidate))
                return candidate;
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("Could not find assets/cows directory");
    }

    private readonly GenerateCowSay _sut;

    public GenerateCowSayTests()
    {
        var repo = new FileCowRepository(FindCowsDirectory());
        var bubble = new BubbleService();
        var engine = new CowTemplateEngine();
        _sut = new GenerateCowSay(repo, bubble, engine);
    }

    [Fact]
    public void Basic_Say_Output_Contains_Message_And_Cow()
    {
        var request = new CowRequest("Hello World");
        var result = _sut.Execute(request);

        result.ShouldContain("Hello World");
        result.ShouldContain("\\");  // trail character for say
        result.ShouldContain("^__^"); // default cow head
    }

    [Fact]
    public void Thought_Mode_Uses_Thought_Trail_And_Parentheses()
    {
        var request = new CowRequest("Thinking", IsThought: true);
        var result = _sut.Execute(request);

        result.ShouldContain("Thinking");
        result.ShouldContain("( Thinking )");
        // Thought trail uses 'o' character
        result.ShouldContain("o");
    }

    [Theory]
    [InlineData(CowMode.Borg, "==")]
    [InlineData(CowMode.Dead, "xx")]
    [InlineData(CowMode.Greedy, "$$")]
    [InlineData(CowMode.Paranoid, "@@")]
    [InlineData(CowMode.Stoned, "**")]
    [InlineData(CowMode.Tired, "--")]
    [InlineData(CowMode.Wired, "LL")]
    [InlineData(CowMode.Youthful, "..")]
    public void Preset_Mode_Renders_Correct_Eyes(CowMode mode, string expectedEyes)
    {
        var request = new CowRequest("Test", Mode: mode);
        var result = _sut.Execute(request);

        result.ShouldContain($"({expectedEyes})");
    }

    [Fact]
    public void Custom_Eyes_Appear_In_Output()
    {
        var request = new CowRequest("Test", Eyes: "^^");
        var result = _sut.Execute(request);

        result.ShouldContain("(^^)");
    }

    [Fact]
    public void Custom_Tongue_Appears_In_Output()
    {
        var request = new CowRequest("Test", Tongue: "UU");
        var result = _sut.Execute(request);

        result.ShouldContain("UU");
    }

    [Fact]
    public void NoWrap_Preserves_Message_Lines()
    {
        var request = new CowRequest("line1\nline2\nline3", NoWrap: true);
        var result = _sut.Execute(request);

        result.ShouldContain("line1");
        result.ShouldContain("line2");
        result.ShouldContain("line3");
    }

    [Fact]
    public void Custom_WrapWidth_Changes_Wrapping()
    {
        var message = "word1 word2 word3 word4 word5 word6 word7 word8";
        var narrow = _sut.Execute(new CowRequest(message, WrapWidth: 10));
        var wide = _sut.Execute(new CowRequest(message, WrapWidth: 80));

        // Narrow output should have more lines than wide output
        var narrowLines = narrow.Split('\n').Length;
        var wideLines = wide.Split('\n').Length;
        narrowLines.ShouldBeGreaterThan(wideLines, $"Narrow ({narrowLines} lines) should have more lines than wide ({wideLines} lines)");
    }

    [Fact]
    public void Different_Cowfile_Renders_Different_Art()
    {
        var defaultResult = _sut.Execute(new CowRequest("Test", CowName: "default"));
        var tuxResult = _sut.Execute(new CowRequest("Test", CowName: "tux"));

        // Both should have the message, but different art
        defaultResult.ShouldContain("Test");
        tuxResult.ShouldContain("Test");
        tuxResult.ShouldNotBe(defaultResult);
    }

    [Fact]
    public void Dead_Mode_Shows_Tongue_In_Output()
    {
        var request = new CowRequest("Test", Mode: CowMode.Dead);
        var result = _sut.Execute(request);

        // Dead mode should show the "U " tongue
        result.ShouldContain("U ");
    }

    [Fact]
    public void Stoned_Mode_Shows_Tongue_In_Output()
    {
        var request = new CowRequest("Test", Mode: CowMode.Stoned);
        var result = _sut.Execute(request);

        // Stoned mode should show the "U " tongue
        result.ShouldContain("U ");
    }
}
