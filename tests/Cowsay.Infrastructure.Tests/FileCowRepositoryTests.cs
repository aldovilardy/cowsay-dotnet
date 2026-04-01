using Cowsay.Infrastructure.Repositories;
using Shouldly;

namespace Cowsay.Infrastructure.Tests;

public class FileCowRepositoryTests
{
    private static string GetCowsDirectory()
    {
        return CowFilesDirectoryResolver.ResolveCowsDirectory();
    }

    private readonly FileCowRepository _sut = new(GetCowsDirectory());

    [Fact]
    public void Constructor_NonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        Should.Throw<DirectoryNotFoundException>(() =>
            new FileCowRepository(@"C:\nonexistent\path\that\does\not\exist"));
    }

    [Fact]
    public void GetCowByName_Default_ReturnsDefaultCow()
    {
        var cow = _sut.GetCowByName("default");

        cow.ShouldNotBeNull();
        cow.Name.ShouldBe("default");
        cow.Template.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void GetCowByName_WithCowExtension_DoesNotDoubleExtension()
    {
        var cow = _sut.GetCowByName("default.cow");

        cow.Name.ShouldBe("default");
    }

    [Fact]
    public void GetCowByName_NullOrEmpty_FallsBackToDefault()
    {
        var cow = _sut.GetCowByName(null!);

        cow.Name.ShouldBe("default");
    }

    [Fact]
    public void GetCowByName_EmptyString_FallsBackToDefault()
    {
        var cow = _sut.GetCowByName("");

        cow.Name.ShouldBe("default");
    }

    [Fact]
    public void GetCowByName_NonExistentCow_ThrowsFileNotFoundException()
    {
        Should.Throw<FileNotFoundException>(() =>
            _sut.GetCowByName("this_cow_definitely_does_not_exist_xyz"));
    }

    [Fact]
    public void GetCowByName_Tux_ReturnsTuxCow()
    {
        var cow = _sut.GetCowByName("tux");

        cow.Name.ShouldBe("tux");
        cow.Template.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void GetCowByName_TemplateContainsEocDelimiter()
    {
        var cow = _sut.GetCowByName("default");

        cow.Template.ShouldContain("EOC");
    }

    [Fact]
    public void ListAvailableCows_ReturnsNonEmptyList()
    {
        var cows = _sut.ListAvailableCows().ToList();

        cows.ShouldNotBeEmpty();
    }

    [Fact]
    public void ListAvailableCows_ContainsDefaultCow()
    {
        var cows = _sut.ListAvailableCows().ToList();

        cows.ShouldContain("default");
    }

    [Fact]
    public void ListAvailableCows_ContainsTux()
    {
        var cows = _sut.ListAvailableCows().ToList();

        cows.ShouldContain("tux");
    }

    [Fact]
    public void ListAvailableCows_IsSorted()
    {
        var cows = _sut.ListAvailableCows().ToList();
        var sorted = cows.OrderBy(n => n).ToList();

        cows.ShouldBe(sorted);
    }

    [Fact]
    public void ListAvailableCows_NoDuplicates()
    {
        var cows = _sut.ListAvailableCows().ToList();
        var distinct = cows.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        cows.Count.ShouldBe(distinct.Count);
    }

    [Fact]
    public void ListAvailableCows_CountMatches48()
    {
        var cows = _sut.ListAvailableCows().ToList();

        cows.Count.ShouldBe(48);
    }

    [Theory]
    [InlineData("default")]
    [InlineData("tux")]
    [InlineData("bud-frogs")]
    [InlineData("cheese")]
    [InlineData("daemon")]
    [InlineData("elephant")]
    [InlineData("koala")]
    [InlineData("moose")]
    [InlineData("sheep")]
    [InlineData("turtle")]
    public void GetCowByName_KnownCows_LoadSuccessfully(string cowName)
    {
        var cow = _sut.GetCowByName(cowName);

        cow.ShouldNotBeNull();
        cow.Name.ShouldBe(cowName);
        cow.Template.ShouldNotBeNullOrEmpty();
    }
}
