using CowSay.Core.Interfaces;
using CowSay.Core.Models;

namespace Cowsay.Infrastructure.Repositories;

/// <inheritdoc/>
public class FileCowRepository(string cowsDirectory) : ICowRepository
{
    private const string Extension = ".cow";
    private const string EnviromentVariableName = "COWPATH";

    private readonly string CowsDirectory = Directory.Exists(cowsDirectory)
            ? cowsDirectory
            : throw new DirectoryNotFoundException($"The cows directory was not found: {cowsDirectory}");

    private readonly string[] SearchDirectories = BuildSearchDirectories(cowsDirectory).ToArray();

    /// <inheritdoc/>
    public Cow GetCowByName(string name)
    {
        var fileName = string.IsNullOrWhiteSpace(name) ? "default" : name;

        var filePath = ResolveCowPath(fileName);

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"The character '{name}' does not exist in configured search paths.");

        return new Cow(Path.GetFileNameWithoutExtension(filePath), File.ReadAllText(filePath));
    }

    /// <inheritdoc/>
    public IEnumerable<string> ListAvailableCows() =>
        SearchDirectories
        .Where(Directory.Exists)
        .SelectMany(path => Directory.GetFiles(path, $"*{Extension}"))
        .Select(Path.GetFileNameWithoutExtension)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(n => n)!;

    /// <summary>
    /// Resolves the full file system path to a cow file based on the specified cow name or path.
    /// </summary>
    /// <remarks>
    /// If the specified cow name is a path (contains directory separators or is rooted), the method returns its absolute path. 
    /// Otherwise, it searches predefined directories for a matching file and falls back to a default directory if no match is found.
    /// </remarks>
    /// <param name="cowName">
    /// The name or path of the cow file to resolve. Can be a file name, a relative path, or an absolute path. Must not be null or empty.
    /// </param>
    /// <returns>
    /// The full path to the resolved cow file. 
    /// If the specified name is a path, returns its absolute path; otherwise, searches known directories and returns the first match or a default path if not found.
    /// </returns>
    private string ResolveCowPath(string cowName)
    {
        if (ContainsDirectorySeparator(cowName) || Path.IsPathRooted(cowName))
            return Path.GetFullPath(EnsureCowExtension(cowName), Directory.GetCurrentDirectory());

        var fileName = EnsureCowExtension(cowName);

        foreach (var directory in SearchDirectories)
        {
            var candidate = Path.Combine(directory, fileName);
            if (File.Exists(candidate))
                return candidate;
        }

        return Path.Combine(CowsDirectory, fileName);
    }

    /// <summary>
    /// Determines whether the specified string contains a directory separator character.
    /// </summary>
    /// <remarks>
    /// This method checks for both the primary and alternate directory separator characters as defined by the current platform. 
    /// Use this method to quickly determine if a path string includes any directory separators.
    /// </remarks>
    /// <param name="value">The string to search for directory separator characters. Cannot be null.</param>
    /// <returns>
    /// true if the string contains either the platform-specific directory separator character or the alternate directory separator character; otherwise, false.
    /// </returns>
    private static bool ContainsDirectorySeparator(string value) =>
        value.Contains(Path.DirectorySeparatorChar) || value.Contains(Path.AltDirectorySeparatorChar);

    /// <summary>
    /// Ensures that the specified file name ends with the required COW file extension.
    /// </summary>
    /// <remarks>The comparison for the file extension is case-insensitive. This method does not validate the
    /// file name for invalid characters.</remarks>
    /// <param name="fileName">The file name to check and update. Cannot be null.</param>
    /// <returns>A file name that ends with the required COW file extension. If the original file name already has the extension,
    /// it is returned unchanged; otherwise, the extension is appended.</returns>
    private static string EnsureCowExtension(string fileName) =>
        Path.GetExtension(fileName).Equals(Extension, StringComparison.OrdinalIgnoreCase)
            ? fileName
            : $"{fileName}{Extension}";

    /// <summary>
    /// Builds an ordered collection of directories to search for resources, including environment-specified, default, and local asset directories.
    /// </summary>
    /// <remarks>
    /// The search order prioritizes directories specified in the COWPATH environment variable, 
    /// followed by the provided default directory, and then a local 'assets/cows' directory if it exists in the current working directory. 
    /// Duplicate directories are removed in a case-insensitive manner.
    /// </remarks>
    /// <param name="defaultDirectory">The default directory to include in the search path. This directory is always added to the collection.</param>
    /// <returns>
    /// An enumerable collection of unique directory paths to be searched, in the order of environment variable entries, the default directory, 
    /// and a local assets directory if present.
    /// </returns>
    private static IEnumerable<string> BuildSearchDirectories(string defaultDirectory)
    {
        var list = new List<string>();
        var fromEnv = Environment.GetEnvironmentVariable(EnviromentVariableName);

        if (!string.IsNullOrWhiteSpace(fromEnv))
            list.AddRange(fromEnv
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(Directory.Exists));

        list.Add(defaultDirectory);

        var localAssets = Path.Combine(Directory.GetCurrentDirectory(), "assets", "cows");

        if (Directory.Exists(localAssets))
            list.Add(localAssets);

        return list
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }
}
