using CowSay.Core.Models;
using CowSay.Core.Services;
using Shouldly;
using Xunit;

namespace Cowsay.Tests;

public class FaceFactoryTests
{
    [Fact]
    public void Default_Mode_Returns_Standard_Eyes_And_Tongue()
    {
        var face = FaceFactory.Create(CowMode.Default);
        face.Eyes.ShouldBe("oo");
        face.Tongue.ShouldBe("  ");
    }

    [Theory]
    [InlineData(CowMode.Borg, "==", "  ")]
    [InlineData(CowMode.Dead, "xx", "U ")]
    [InlineData(CowMode.Greedy, "$$", "  ")]
    [InlineData(CowMode.Paranoid, "@@", "  ")]
    [InlineData(CowMode.Stoned, "**", "U ")]
    [InlineData(CowMode.Tired, "--", "  ")]
    [InlineData(CowMode.Wired, "LL", "  ")]
    [InlineData(CowMode.Youthful, "..", "  ")]
    public void Preset_Mode_Returns_Correct_Eyes_And_Tongue(CowMode mode, string expectedEyes, string expectedTongue)
    {
        var face = FaceFactory.Create(mode);
        face.Eyes.ShouldBe(expectedEyes);
        face.Tongue.ShouldBe(expectedTongue);
    }

    [Fact]
    public void Dead_And_Stoned_Tongues_Are_Two_Characters()
    {
        var dead = FaceFactory.Create(CowMode.Dead);
        var stoned = FaceFactory.Create(CowMode.Stoned);
        dead.Tongue.Length.ShouldBe(2);
        stoned.Tongue.Length.ShouldBe(2);
    }

    [Fact]
    public void Custom_Eyes_Uses_First_Two_Characters()
    {
        var face = FaceFactory.Create(CowMode.Default, eyes: "ABC");
        face.Eyes.ShouldBe("AB");
    }

    [Fact]
    public void Custom_Eyes_Single_Character_Kept()
    {
        var face = FaceFactory.Create(CowMode.Default, eyes: "X");
        face.Eyes.ShouldBe("X");
    }

    [Fact]
    public void Custom_Eyes_Two_Characters_Kept()
    {
        var face = FaceFactory.Create(CowMode.Default, eyes: "^^");
        face.Eyes.ShouldBe("^^");
    }

    [Fact]
    public void Custom_Tongue_Exactly_Two_Characters()
    {
        var face = FaceFactory.Create(CowMode.Default, tongue: "UU");
        face.Tongue.ShouldBe("UU");
    }

    [Fact]
    public void Custom_Tongue_Not_Two_Characters_Throws()
    {
        Should.Throw<ArgumentException>(() => FaceFactory.Create(CowMode.Default, tongue: "A"));
        Should.Throw<ArgumentException>(() => FaceFactory.Create(CowMode.Default, tongue: "ABC"));
    }

    [Fact]
    public void Preset_Mode_Overrides_Custom_Eyes_And_Tongue()
    {
        // Man page: "Any configuration done by -e and -T will be lost if one of the provided modes is used."
        var face = FaceFactory.Create(CowMode.Dead, eyes: "^^", tongue: "UU");
        face.Eyes.ShouldBe("xx");
        face.Tongue.ShouldBe("U ");
    }

    [Fact]
    public void Null_Eyes_Defaults_To_Oo()
    {
        var face = FaceFactory.Create(CowMode.Default, eyes: null);
        face.Eyes.ShouldBe("oo");
    }

    [Fact]
    public void Null_Tongue_Defaults_To_Spaces()
    {
        var face = FaceFactory.Create(CowMode.Default, tongue: null);
        face.Tongue.ShouldBe("  ");
    }

    [Fact]
    public void Empty_Eyes_Defaults_To_Oo()
    {
        var face = FaceFactory.Create(CowMode.Default, eyes: "");
        face.Eyes.ShouldBe("oo");
    }

    [Fact]
    public void Empty_Tongue_Defaults_To_Spaces()
    {
        var face = FaceFactory.Create(CowMode.Default, tongue: "");
        face.Tongue.ShouldBe("  ");
    }
}
