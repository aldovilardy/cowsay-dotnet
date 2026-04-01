using CowSay.Core.Services;
using Shouldly;

namespace Cowsay.Core.Tests;

public class CowfilePreprocessorTests
{
    [Fact]
    public void Process_NoPreprocessingCode_ReturnsOriginalValues()
    {
        var template = "$the_cow = <<EOC;\n  art\nEOC\n";

        var (eyes, tongue) = CowfilePreprocessor.Process(template, "oo", "  ");

        eyes.ShouldBe("oo");
        tongue.ShouldBe("  ");
    }

    [Fact]
    public void Process_ChopEyes_RemovesLastCharAndReturnsIt()
    {
        // three-eyes.cow pattern: $extra = chop($eyes); then $eyes .= ($extra x 2)
        var template = "$extra = chop($eyes);\n$eyes .= ($extra x 2);\n$the_cow = <<EOC;\nEOC\n";

        var (eyes, tongue) = CowfilePreprocessor.Process(template, "oo", "  ");

        // chop("oo") removes 'o', eyes becomes "o", extra = "o"
        // then eyes .= "o" x 2 = "oo" → eyes = "o" + "oo" = "ooo"
        eyes.ShouldBe("ooo");
        tongue.ShouldBe("  ");
    }

    [Fact]
    public void Process_SimpleAssignment_SetsVariable()
    {
        var template = "$tongue = \"ZZ\";\n$the_cow = <<EOC;\nEOC\n";

        var (eyes, tongue) = CowfilePreprocessor.Process(template, "oo", "  ");

        tongue.ShouldBe("ZZ");
    }

    [Fact]
    public void Process_UnlessCondition_SetsWhenVarEmpty()
    {
        // "unless ($eyes)" means: only assign if eyes is empty
        var template = "$eyes = \"..\" unless ($eyes);\n$the_cow = <<EOC;\nEOC\n";

        var (eyes, _) = CowfilePreprocessor.Process(template, "", "  ");

        eyes.ShouldBe("..");
    }

    [Fact]
    public void Process_UnlessCondition_SkipsWhenVarSet()
    {
        var template = "$eyes = \"..\" unless ($eyes);\n$the_cow = <<EOC;\nEOC\n";

        var (eyes, _) = CowfilePreprocessor.Process(template, "oo", "  ");

        eyes.ShouldBe("oo");
    }

    [Fact]
    public void Process_ConcatAssignment_AppendsToExistingVar()
    {
        var template = "$eyes .= \"XY\";\n$the_cow = <<EOC;\nEOC\n";

        var (eyes, _) = CowfilePreprocessor.Process(template, "oo", "  ");

        eyes.ShouldBe("ooXY");
    }

    [Fact]
    public void Process_RepeatOperator_RepeatsString()
    {
        var template = "$eyes = ($eyes x 3);\n$the_cow = <<EOC;\nEOC\n";

        var (eyes, _) = CowfilePreprocessor.Process(template, "ab", "  ");

        eyes.ShouldBe("ababab");
    }

    [Fact]
    public void Process_CrlfLineEndings_HandledCorrectly()
    {
        var template = "$tongue = \"XX\";\r\n$the_cow = <<EOC;\r\nEOC\r\n";

        var (_, tongue) = CowfilePreprocessor.Process(template, "oo", "  ");

        tongue.ShouldBe("XX");
    }

    [Fact]
    public void Process_CommentsAndBlankLines_Ignored()
    {
        var template = "# this is a comment\n\n$tongue = \"YY\";\n$the_cow = <<EOC;\nEOC\n";

        var (_, tongue) = CowfilePreprocessor.Process(template, "oo", "  ");

        tongue.ShouldBe("YY");
    }

    [Fact]
    public void Process_StringInterpolation_ResolvesVarRefs()
    {
        var template = "$extra = \"X\";\n$eyes = \"$extra$extra\";\n$the_cow = <<EOC;\nEOC\n";

        var (eyes, _) = CowfilePreprocessor.Process(template, "oo", "  ");

        eyes.ShouldBe("XX");
    }
}
