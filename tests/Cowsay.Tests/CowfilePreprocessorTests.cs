using Shouldly;
using Xunit;
using CowSay.Core.Services;

namespace Cowsay.Tests;

public class CowfilePreprocessorTests
{
    [Fact]
    public void No_PreHeredoc_Code_Returns_Original_Values()
    {
        var template = "$the_cow = <<EOC;\n  $thoughts ($eyes)\nEOC";
        var (eyes, tongue) = CowfilePreprocessor.Process(template, "oo", "  ");
        eyes.ShouldBe("oo");
        tongue.ShouldBe("  ");
    }

    [Fact]
    public void Comments_Are_Ignored()
    {
        var template = "##\n## comment\n##\n$the_cow = <<EOC;\n  $thoughts ($eyes)\nEOC";
        var (eyes, tongue) = CowfilePreprocessor.Process(template, "oo", "  ");
        eyes.ShouldBe("oo");
        tongue.ShouldBe("  ");
    }

    [Fact]
    public void ThreeEyes_Cow_Produces_Three_Character_Eyes()
    {
        // three-eyes.cow logic:
        // $extra = chop($eyes);    -> removes last char of "oo", extra="o", eyes="o"
        // $eyes .= ($extra x 2);   -> eyes = "o" + "oo" = "ooo"
        var template = "##\n$extra = chop($eyes);\n$eyes .= ($extra x 2);\n$the_cow = <<EOC;\n  ($eyes)\nEOC";
        var (eyes, _) = CowfilePreprocessor.Process(template, "oo", "  ");
        eyes.ShouldBe("ooo");
    }

    [Fact]
    public void ThreeEyes_With_Custom_Eyes()
    {
        // With custom eyes "^^":
        // chop("^^") -> extra="^", eyes="^"
        // eyes .= ("^" x 2) -> eyes = "^" + "^^" = "^^^"
        var template = "##\n$extra = chop($eyes);\n$eyes .= ($extra x 2);\n$the_cow = <<EOC;\n  ($eyes)\nEOC";
        var (eyes, _) = CowfilePreprocessor.Process(template, "^^", "  ");
        eyes.ShouldBe("^^^");
    }

    [Fact]
    public void Udder_Cow_Produces_Spaced_Eyes()
    {
        // udder.cow logic:
        // $other_eye = chop($eyes);  -> other_eye="o", eyes="o"
        // $eyes .= " $other_eye";    -> eyes = "o" + " o" = "o o"
        var template = "##\n$other_eye = chop($eyes);\n$eyes .= \" $other_eye\";\n$the_cow = <<EOC;\n  ($eyes)\nEOC";
        var (eyes, _) = CowfilePreprocessor.Process(template, "oo", "  ");
        eyes.ShouldBe("o o");
    }

    [Fact]
    public void Small_Cow_Defaults_Eyes_When_Empty()
    {
        // small.cow logic:
        // $eyes = ".." unless ($eyes)  -> set eyes to ".." only if eyes is empty/falsy
        var template = "##\n$eyes = \"..\" unless ($eyes);\n$the_cow = <<EOC;\n  ($eyes)\nEOC";
        var (eyes, _) = CowfilePreprocessor.Process(template, "", "  ");
        eyes.ShouldBe("..");
    }

    [Fact]
    public void Small_Cow_Preserves_Eyes_When_Set()
    {
        // When eyes already has a value, unless should not overwrite
        var template = "##\n$eyes = \"..\" unless ($eyes);\n$the_cow = <<EOC;\n  ($eyes)\nEOC";
        var (eyes, _) = CowfilePreprocessor.Process(template, "oo", "  ");
        eyes.ShouldBe("oo");
    }

    [Fact]
    public void Chop_On_Empty_String_Returns_Empty()
    {
        var template = "$extra = chop($eyes);\n$the_cow = <<EOC;\n  ($eyes)\nEOC";
        var (eyes, _) = CowfilePreprocessor.Process(template, "", "  ");
        eyes.ShouldBe("");
    }

    [Fact]
    public void Chop_On_Single_Char_Returns_Char_And_Empties_Var()
    {
        var template = "$extra = chop($eyes);\n$the_cow = <<EOC;\n  ($eyes) $extra\nEOC";
        var (eyes, _) = CowfilePreprocessor.Process(template, "X", "  ");
        eyes.ShouldBe("");
    }
}
