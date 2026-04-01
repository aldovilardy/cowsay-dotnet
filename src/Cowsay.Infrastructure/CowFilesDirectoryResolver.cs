using System.Reflection;

namespace Cowsay.Infrastructure;

/// <summary>
/// Provides methods for resolving the directory path containing .cow character files used by the application.
/// </summary>
/// <remarks>
/// This static class searches multiple candidate locations for the .cow files required for operation:
/// <list type="number">
///   <item><description>Relative to the assembly location (supports deployed modules and standalone installs).</description></item>
///   <item><description>Walking up from the application's base directory (supports development-time scenarios).</description></item>
/// </list>
/// </remarks>
public static class CowFilesDirectoryResolver
{
    private static readonly string CowsSubPath = Path.Combine("assets", "cows");

    /// <summary>
    /// Resolves the directory path where the .cow character files are located.
    /// </summary>
    /// <remarks>
    /// Resolution order:
    /// <list type="number">
    ///   <item><description>
    ///     <c>assets/cows</c> relative to <see cref="AppContext.BaseDirectory"/>.
    ///     This handles single-file publish, normal execution, and most deployed scenarios.
    ///   </description></item>
    ///   <item><description>
    ///     <c>assets/cows</c> relative to the directory containing <c>Cowsay.Infrastructure.dll</c>.
    ///     This handles PowerShell module deployments where the assembly may be in a different
    ///     directory from the app base.
    ///   </description></item>
    ///   <item><description>
    ///     Walking up from <see cref="AppContext.BaseDirectory"/> checking for <c>assets/cows</c>
    ///     at each level. This handles development-time scenarios where the binary is nested
    ///     inside the repository tree.
    ///   </description></item>
    /// </list>
    /// </remarks>
    /// <returns>The full path to the directory containing the .cow files.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown if no valid .cow directory is found.</exception>
    public static string ResolveCowsDirectory()
    {
        // 1. Check relative to the app base directory (works for single-file publish and normal execution)
        var baseCandidate = Path.Combine(AppContext.BaseDirectory, CowsSubPath);
        if (Directory.Exists(baseCandidate))
            return baseCandidate;

        // 2. Check relative to the assembly location (deployed module / standalone install)
        //    Suppressed IL3000: we guard against the empty string that single-file returns.
#pragma warning disable IL3000
        var assemblyLocation = typeof(CowFilesDirectoryResolver).Assembly.Location;
#pragma warning restore IL3000
        if (!string.IsNullOrEmpty(assemblyLocation))
        {
            var assemblyDir = Path.GetDirectoryName(assemblyLocation);
            if (assemblyDir is not null)
            {
                var candidate = Path.Combine(assemblyDir, CowsSubPath);
                if (Directory.Exists(candidate))
                    return candidate;
            }
        }

        // 3. Walk up from AppContext.BaseDirectory (development-time fallback)
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        string[] candidatePaths =
        [
            CowsSubPath,
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
