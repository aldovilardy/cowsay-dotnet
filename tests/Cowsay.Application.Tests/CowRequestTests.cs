using Cowsay.Application.DTOs;
using CowSay.Core.Models;
using Shouldly;

namespace Cowsay.Application.Tests;

public class CowRequestTests
{
    [Fact]
    public void Constructor_DefaultValues_AppliedCorrectly()
    {
        var request = new CowRequest("Hello");

        request.Message.ShouldBe("Hello");
        request.CowName.ShouldBe("default");
        request.Mode.ShouldBe(CowMode.Default);
        request.Eyes.ShouldBeNull();
        request.Tongue.ShouldBeNull();
        request.IsThought.ShouldBeFalse();
        request.WrapWidth.ShouldBe(40);
        request.NoWrap.ShouldBeFalse();
    }

    [Fact]
    public void Constructor_FullValues_SetCorrectly()
    {
        var request = new CowRequest(
            "Test",
            "tux",
            CowMode.Dead,
            "xx",
            "U ",
            true,
            72,
            true);

        request.Message.ShouldBe("Test");
        request.CowName.ShouldBe("tux");
        request.Mode.ShouldBe(CowMode.Dead);
        request.Eyes.ShouldBe("xx");
        request.Tongue.ShouldBe("U ");
        request.IsThought.ShouldBeTrue();
        request.WrapWidth.ShouldBe(72);
        request.NoWrap.ShouldBeTrue();
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var r1 = new CowRequest("Hello");
        var r2 = new CowRequest("Hello");

        r1.ShouldBe(r2);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var r1 = new CowRequest("Hello");
        var r2 = new CowRequest("World");

        r1.ShouldNotBe(r2);
    }

    [Fact]
    public void WithExpression_CreatesModifiedCopy()
    {
        var original = new CowRequest("Hello");
        var modified = original with { CowName = "tux", IsThought = true };

        modified.Message.ShouldBe("Hello");
        modified.CowName.ShouldBe("tux");
        modified.IsThought.ShouldBeTrue();
        original.CowName.ShouldBe("default"); // original unchanged
    }

    [Fact]
    public void ToString_ContainsTypeName()
    {
        var request = new CowRequest("Hello");

        request.ToString().ShouldContain("CowRequest");
    }
}
