namespace CowSay.Core.Models;

/// <summary>
/// Represents a cow character loaded from a .cow file content.
/// </summary>
/// <param name="Name">The name of the cow character.</param>
/// <param name="Template">The raw content that includes $eyes, $tongue and $thoughts variables.</param>
public record Cow(string Name, string Template);