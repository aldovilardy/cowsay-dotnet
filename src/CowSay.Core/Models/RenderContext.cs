namespace CowSay.Core.Models;

/// <summary>
/// Represents the context required to render a message with customizable eyes, tongue, and thought character, typically for ASCII art or similar output.
/// </summary>
/// <remarks>
/// This record is commonly used to encapsulate all parameters needed for dynamic rendering ASCII art figures, such as those in cowsay-style applications. 
/// All parameters are required for correct rendering, but default values are provided for eyes, tongue, and thought character to simplify typical usage.
/// </remarks>
/// <param name="Message">The message text to be rendered within the context.</param>
/// <param name="Eyes">The string representing the eyes to use in the rendered output. Defaults to "oo".</param>
/// <param name="Tongue">The string representing the tongue to use in the rendered output. Defaults to two spaces.</param>
/// <param name="ThoughtCharacter">The character used to represent the thought or speech bubble. Defaults to '\'.</param>
public record RenderContext(
    string Message,
    string Eyes = "oo",
    string Tongue = "  ",
    char ThoughtCharacter = '\\');