using CowSay.Core.Models;
using CowSay.Core.Services;
using Shouldly;

namespace Cowsay.Core.Tests;

public class CowTemplateEngineTests
{
    private readonly CowTemplateEngine _sut = new();

    [Fact]
    public void Process_SimpleTemplate_ReplacesEyesAndTongue()
    {
        var template = "$the_cow = <<EOC;\n        $thoughts\n        ($eyes)\n        $tongue\nEOC\n";
        var face = new Face("oo", "U ");

        var result = _sut.Process(template, face, '\\');

        result.ShouldContain("\\");
        result.ShouldContain("oo");
        result.ShouldContain("U ");
    }

    [Fact]
    public void Process_BraceVariables_ReplacesCorrectly()
    {
        var template = "$the_cow = <<EOC;\n        ${thoughts}\n        (${eyes})\n        ${tongue}\nEOC\n";
        var face = new Face("@@", "  ");

        var result = _sut.Process(template, face, 'o');

        result.ShouldContain("o");
        result.ShouldContain("@@");
    }

    [Fact]
    public void Process_ThoughtCharBackslash_ForSpeech()
    {
        var template = "$the_cow = <<EOC;\n  $thoughts\nEOC\n";
        var face = new Face();

        var result = _sut.Process(template, face, '\\');

        result.ShouldContain("\\");
    }

    [Fact]
    public void Process_ThoughtCharO_ForThought()
    {
        var template = "$the_cow = <<EOC;\n  $thoughts\nEOC\n";
        var face = new Face();

        var result = _sut.Process(template, face, 'o');

        result.ShouldContain("o");
    }

    [Fact]
    public void Process_DoubleBackslash_UnescapedToSingle()
    {
        var template = "$the_cow = <<EOC;\n  \\\\\nEOC\n";
        var face = new Face();

        var result = _sut.Process(template, face, '\\');

        result.ShouldContain("\\");
        result.ShouldNotContain("\\\\");
    }

    [Fact]
    public void Process_EscapedAtSign_UnescapedCorrectly()
    {
        var template = "$the_cow = <<EOC;\n  \\@array\nEOC\n";
        var face = new Face();

        var result = _sut.Process(template, face, '\\');

        result.ShouldContain("@array");
    }

    [Fact]
    public void Process_DollarSignInReplacement_PreservedCorrectly()
    {
        // The greedy mode has $$ eyes — verify $ in replacement doesn't break regex
        var template = "$the_cow = <<EOC;\n  ($eyes)\nEOC\n";
        var face = new Face("$$", "  ");

        var result = _sut.Process(template, face, '\\');

        result.ShouldContain("($$)");
    }

    [Fact]
    public void Process_NoEocDelimiter_ReturnsTemplateWithReplacements()
    {
        var template = "Just some text with $eyes in it";
        var face = new Face("OO", "  ");

        var result = _sut.Process(template, face, '\\');

        result.ShouldContain("OO");
    }
}
