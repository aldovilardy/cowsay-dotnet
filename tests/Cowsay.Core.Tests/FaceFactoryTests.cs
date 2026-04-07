using CowSay.Core.Models;
using CowSay.Core.Services;
using Shouldly;

namespace Cowsay.Core.Tests;

public class FaceFactoryTests
{
    [Fact]
    public void Create_DefaultModeNoArgs_ReturnsDefaultFace()
    {
        var face = FaceFactory.Create(CowMode.Default);

        face.Eyes.ShouldBe("oo");
        face.Tongue.ShouldBe("  ");
    }

    [Theory]
    [InlineData(CowMode.Dead, "xx", "U ")]
    [InlineData(CowMode.Borg, "==", "  ")]
    [InlineData(CowMode.Greedy, "$$", "  ")]
    [InlineData(CowMode.Paranoid, "@@", "  ")]
    [InlineData(CowMode.Stoned, "**", "U ")]
    [InlineData(CowMode.Tired, "--", "  ")]
    [InlineData(CowMode.Wired, "LL", "  ")]
    [InlineData(CowMode.Youthful, "..", "  ")]
    public void Create_NonDefaultMode_ReturnsCorrectFace(CowMode mode, string expectedEyes, string expectedTongue)
    {
        var face = FaceFactory.Create(mode);

        face.Eyes.ShouldBe(expectedEyes);
        face.Tongue.ShouldBe(expectedTongue);
    }

    [Fact]
    public void Create_AllModes_ProduceTwoCharEyes()
    {
        foreach (var mode in Enum.GetValues<CowMode>())
        {
            var face = FaceFactory.Create(mode);
            face.Eyes.Length.ShouldBe(2, $"Mode {mode} should produce 2-char eyes");
        }
    }

    [Fact]
    public void Create_CustomEyesLongerThan2_TruncatedTo2()
    {
        var face = FaceFactory.Create(CowMode.Default, eyes: "ABC");

        face.Eyes.ShouldBe("AB");
    }

    [Fact]
    public void Create_CustomEyes1Char_KeptAs1Char()
    {
        var face = FaceFactory.Create(CowMode.Default, eyes: "X");

        face.Eyes.ShouldBe("X");
    }

    [Fact]
    public void Create_NullEyesNullTongue_Defaults()
    {
        var face = FaceFactory.Create(CowMode.Default, null, null);

        face.Eyes.ShouldBe("oo");
        face.Tongue.ShouldBe("  ");
    }

    [Fact]
    public void Create_CustomTongueExactly2_Accepted()
    {
        var face = FaceFactory.Create(CowMode.Default, tongue: "U ");

        face.Tongue.ShouldBe("U ");
    }

    [Fact]
    public void Create_CustomTongueNot2Chars_FallbackToDefault()
    {
        var face = FaceFactory.Create(CowMode.Default, tongue: "abc");
        face.Tongue.ShouldBe("  ");
    }

    [Fact]
    public void Create_EmptyTongue_DefaultsToSpaces()
    {
        var face = FaceFactory.Create(CowMode.Default, tongue: "");

        face.Tongue.ShouldBe("  ");
    }

    [Fact]
    public void Create_SpecialCharsInTongue_Accepted()
    {
        var face = FaceFactory.Create(CowMode.Default, tongue: "!@");

        face.Tongue.ShouldBe("!@");
    }

    [Fact]
    public void Create_SingleEmojiEyes_Accepted()
    {
        var face = FaceFactory.Create(CowMode.Default, eyes: "👀");

        face.Eyes.ShouldBe("👀");
    }

    [Fact]
    public void Create_TwoEmojiEyes_BothAccepted()
    {
        var face = FaceFactory.Create(CowMode.Default, eyes: "👀👂");

        face.Eyes.ShouldBe("👀👂");
    }

    [Fact]
    public void Create_ThreeEmojiEyes_TruncatedToTwo()
    {
        var face = FaceFactory.Create(CowMode.Default, eyes: "👀👂👃");

        face.Eyes.ShouldBe("👀👂");
    }

    [Fact]
    public void Create_EmojiTongue_Accepted()
    {
        var face = FaceFactory.Create(CowMode.Default, tongue: "👅");

        face.Tongue.ShouldBe("👅");
    }

    [Fact]
    public void Create_CombinedEmojiAndAsciiEyes_Accepted()
    {
        var face = FaceFactory.Create(CowMode.Default, eyes: "😎👌");

        face.Eyes.ShouldBe("😎👌");
    }

    [Fact]
    public void Create_InvalidTongueLength_FallbackToDefault()
    {
        var face = FaceFactory.Create(CowMode.Default, tongue: "👀👂👃");

        face.Tongue.ShouldBe("  ");
    }
}
