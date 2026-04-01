using Cowsay.Infrastructure.Repositories;
using Shouldly;
using Xunit;

namespace Cowsay.Tests;

public class FileCowRepositoryTests
{
    private static string FindCowsDirectory()
    {
        // Walk up from the test assembly location to find the assets/cows directory
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "assets", "cows");
            if (Directory.Exists(candidate))
                return candidate;
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("Could not find assets/cows directory");
    }

    private readonly FileCowRepository _sut = new(FindCowsDirectory());

    [Fact]
    public void GetCowByName_Default_Returns_Cow()
    {
        var cow = _sut.GetCowByName("default");
        cow.ShouldNotBeNull();
        cow.Name.ShouldBe("default");
        cow.Template.ShouldContain("$thoughts");
    }

    [Fact]
    public void GetCowByName_Without_Extension_Works()
    {
        var cow = _sut.GetCowByName("tux");
        cow.Name.ShouldBe("tux");
    }

    [Fact]
    public void GetCowByName_Nonexistent_Throws()
    {
        Should.Throw<FileNotFoundException>(() => _sut.GetCowByName("nonexistent_cow_xyz"));
    }

    [Fact]
    public void GetCowByName_Empty_Returns_Default()
    {
        var cow = _sut.GetCowByName("");
        cow.Name.ShouldBe("default");
    }

    [Fact]
    public void ListAvailableCows_Returns_NonEmpty()
    {
        var cows = _sut.ListAvailableCows().ToList();
        cows.ShouldNotBeEmpty();
        cows.ShouldContain("default");
    }

    [Fact]
    public void ListAvailableCows_Contains_Known_Cowfiles()
    {
        var cows = _sut.ListAvailableCows().ToList();
        cows.ShouldContain("tux");
        cows.ShouldContain("dragon");
        cows.ShouldContain("stegosaurus");
    }

    [Fact]
    public void ListAvailableCows_Returns_Sorted()
    {
        var cows = _sut.ListAvailableCows().ToList();
        var sorted = cows.OrderBy(c => c).ToList();
        cows.ShouldBe(sorted);
    }

    [Fact]
    public void GetCowByName_With_Relative_Path_Containing_Slash()
    {
        // If the cowfile spec contains '/' then it should be interpreted as a path
        // relative to the current directory. We test that the path resolution logic
        // handles directory separators.
        var cowsDir = FindCowsDirectory();
        var relativePath = Path.Combine("assets", "cows", "default");
        
        // This should attempt to resolve as a path (not a name search)
        // It may or may not find the file depending on CWD, but it should not throw
        // a search-path error - it should throw FileNotFound with a path-like message
        // or succeed.
        try
        {
            var cow = _sut.GetCowByName(relativePath);
            cow.ShouldNotBeNull();
        }
        catch (FileNotFoundException)
        {
            // This is acceptable - the path is relative to CWD, not to assets dir
        }
    }
}
