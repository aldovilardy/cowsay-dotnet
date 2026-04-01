using Shouldly;

namespace Cowsay.Infrastructure.Tests;

public class CowOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var options = new CowOptions();

        options.BasePath.ShouldBe(string.Empty);
        options.DefaultCow.ShouldBe("default");
    }

    [Fact]
    public void BasePath_CanBeSet()
    {
        var options = new CowOptions { BasePath = "/custom/path" };

        options.BasePath.ShouldBe("/custom/path");
    }

    [Fact]
    public void DefaultCow_CanBeSet()
    {
        var options = new CowOptions { DefaultCow = "tux" };

        options.DefaultCow.ShouldBe("tux");
    }

    [Fact]
    public void AllProperties_CanBeSetTogether()
    {
        var options = new CowOptions
        {
            BasePath = "/my/path",
            DefaultCow = "cheese"
        };

        options.BasePath.ShouldBe("/my/path");
        options.DefaultCow.ShouldBe("cheese");
    }
}
