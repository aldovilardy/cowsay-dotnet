namespace Cowsay.Infrastructure;

/// <summary>
/// Represents configuration options for selecting and locating cow files.
/// </summary>
public class CowOptions
{
    /// <summary>
    /// Gets or sets the base directory path used for resolving relative file or resource locations.
    /// </summary>
    public string BasePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the default cow to use for output formatting.
    /// </summary>
    public string DefaultCow { get; set; } = "default";
}
