namespace Cowsay.Infrastructure;

/// <summary>
/// Provides methods for resolving the directory path containing .cow character files used by the application.
/// </summary>
/// <remarks>
/// This static class searches for common candidate directories relative to the application's base directory 
/// and its parent directories to locate the .cow files required for operation.
/// </remarks>
public static class CowFilesDirectoryResolver
{
    ///<summary>
    /// Resolves the directory path where the .cow character files are located. 
    /// The method searches for common candidate paths relative to the application's base directory and its parent directories.
    /// </summary>
    /// <remarks>
    /// The method iterates through the application's base directory and its parent directories,
    /// checking each candidate path for the presence of .cow files.
    /// </remarks>
    /// <returns>The full path to the directory containing the .cow files.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown if no valid .cow directory is found.</exception>
    public static string ResolveCowsDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        string[] candidatePaths =
        [
            Path.Combine("assets", "cows"),
            Path.Combine("src", "Cowsay.Infrastructure", "assets", "cows")
        ];

        while (current is not null)
        {
            foreach (var candidatePath in candidatePaths)
            {
                var fullPath = Path.Combine(current.FullName, candidatePath);
                if (Directory.Exists(fullPath))
                    return fullPath;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("No .cow character directory found.");
    }
}
