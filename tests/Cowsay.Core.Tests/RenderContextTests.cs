using CowSay.Core.Models;
using Shouldly;

namespace Cowsay.Core.Tests;

public class RenderContextTests
{
    [Fact]
    public void Constructor_DefaultValues_AppliedCorrectly()
    {
        var ctx = new RenderContext("Hello");

        ctx.Message.ShouldBe("Hello");
        ctx.Eyes.ShouldBe("oo");
        ctx.Tongue.ShouldBe("  ");
        ctx.ThoughtCharacter.ShouldBe('\\');
    }

    [Fact]
    public void Constructor_CustomValues_SetCorrectly()
    {
        var ctx = new RenderContext("Test", "xx", "U ", 'o');

        ctx.Message.ShouldBe("Test");
        ctx.Eyes.ShouldBe("xx");
        ctx.Tongue.ShouldBe("U ");
        ctx.ThoughtCharacter.ShouldBe('o');
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var ctx1 = new RenderContext("Hello", "oo", "  ", '\\');
        var ctx2 = new RenderContext("Hello", "oo", "  ", '\\');

        ctx1.ShouldBe(ctx2);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var ctx1 = new RenderContext("Hello");
        var ctx2 = new RenderContext("World");

        ctx1.ShouldNotBe(ctx2);
    }
}
