namespace CowSay.Core.Models;

/// <summary>
/// Represents a simple face with customizable eyes and tongue components.
/// </summary>
/// <param name="Eyes">The string representing the eyes of the face. Defaults to "oo" if not specified.</param>
/// <param name="Tongue">The string representing the tongue of the face. Defaults to two spaces if not specified.</param>
public record Face(string Eyes = "oo", string Tongue = "  ");
