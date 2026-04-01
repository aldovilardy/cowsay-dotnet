using Shouldly;

namespace Cowsay.Infrastructure.Tests;

public class CowFilesDirectoryResolverTests
{
    [Fact]
    public void ResolveCowsDirectory_ReturnsExistingDirectory()
    {
        var path = CowFilesDirectoryResolver.ResolveCowsDirectory();

        Directory.Exists(path).ShouldBeTrue();
    }

    [Fact]
    public void ResolveCowsDirectory_ContainsCowFiles()
    {
        var path = CowFilesDirectoryResolver.ResolveCowsDirectory();
        var cowFiles = Directory.GetFiles(path, "*.cow");

        cowFiles.ShouldNotBeEmpty();
    }

    [Fact]
    public void ResolveCowsDirectory_ContainsDefaultCow()
    {
        var path = CowFilesDirectoryResolver.ResolveCowsDirectory();
        var defaultCow = Path.Combine(path, "default.cow");

        File.Exists(defaultCow).ShouldBeTrue();
    }

    [Fact]
    public void ResolveCowsDirectory_IsDeterministic()
    {
        var path1 = CowFilesDirectoryResolver.ResolveCowsDirectory();
        var path2 = CowFilesDirectoryResolver.ResolveCowsDirectory();

        path1.ShouldBe(path2);
    }

    [Fact]
    public void ResolveCowsDirectory_PathEndsWithAssetsCows()
    {
        var path = CowFilesDirectoryResolver.ResolveCowsDirectory();

        path.ShouldEndWith(Path.Combine("assets", "cows"));
    }
}
