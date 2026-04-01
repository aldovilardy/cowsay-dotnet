using CowSay.Core.Models;
using CowSay.Core.Services;
using Shouldly;
using Xunit;

namespace Cowsay.Tests;

public class CowTemplateEngineTests
{
    private readonly CowTemplateEngine _sut = new();

    [Fact]
    public void Substitutes_Eyes_Tongue_Thoughts()
    {
        var template = "$the_cow = <<EOC;\n  $thoughts ($eyes) $tongue\nEOC";
        var face = new Face("oo", "  ");
        var result = _sut.Process(template, face, '\\');

        result.ShouldContain("\\");
        result.ShouldContain("oo");
    }

    [Fact]
    public void Substitutes_Braced_Variables()
    {
        var template = "$the_cow = <<EOC;\n  ${thoughts} (${eyes}) ${tongue}\nEOC";
        var face = new Face("^^", "UU");
        var result = _sut.Process(template, face, 'o');

        result.ShouldContain("o");
        result.ShouldContain("^^");
        result.ShouldContain("UU");
    }

    [Fact]
    public void Unescapes_Double_Backslash()
    {
        var template = "$the_cow = <<EOC;\n  $thoughts \\\\test\nEOC";
        var face = new Face("oo", "  ");
        var result = _sut.Process(template, face, '\\');

        result.ShouldContain("\\test");
        result.ShouldNotContain("\\\\test");
    }

    [Fact]
    public void Unescapes_At_Sign()
    {
        var template = "$the_cow = <<EOC;\n  $thoughts \\@test\nEOC";
        var face = new Face("oo", "  ");
        var result = _sut.Process(template, face, '\\');

        result.ShouldContain("@test");
    }

    [Fact]
    public void Unescapes_Dollar_Sign()
    {
        var template = "$the_cow = <<EOC;\n  $thoughts \\$test\nEOC";
        var face = new Face("oo", "  ");
        var result = _sut.Process(template, face, '\\');

        result.ShouldContain("$test");
    }

    [Fact]
    public void Handles_Quoted_Heredoc_Delimiter()
    {
        // Some cowfiles use <<"EOC"; instead of <<EOC;
        var template = "$the_cow = <<\"EOC\";\n  $thoughts ($eyes)\nEOC";
        var face = new Face("@@", "  ");
        var result = _sut.Process(template, face, '\\');

        result.ShouldContain("@@");
    }

    [Fact]
    public void Returns_Template_When_No_Heredoc()
    {
        var template = "  $thoughts ($eyes) $tongue";
        var face = new Face("oo", "  ");
        var result = _sut.Process(template, face, '\\');

        // Should still do substitution even without EOC delimiters
        result.ShouldContain("oo");
    }
}
