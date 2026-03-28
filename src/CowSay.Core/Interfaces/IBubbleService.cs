namespace CowSay.Core.Interfaces;

/// <summary>
/// Defines a service for generating the lines of a speech or thought bubble geometry based on a message, bubble type, and wrap width.
/// </summary>
/// <remarks>
/// Implementations of this interface are responsible for formatting the bubble's geometry, including adjusting borders and wrapping text as needed. 
/// The bubble style may differ depending on whether it represents speech or thought.
/// </remarks>
public interface IBubbleService
{
    /// <summary>
    /// Generates the lines of a speech or thought bubble (top, content, bottom) based on the provided message, type (speech or thought), and wrap width.
    /// </summary>
    /// <param name="message">The message to be displayed inside the bubble.</param>
    /// <param name="isThought">Indicates whether the bubble is a thought bubble (true) or a speech bubble (false).</param>
    /// <param name="wrapWidth">The maximum width of the bubble before wrapping the text.</param>
    /// <param name="noWrap">When true, preserves message lines and whitespace instead of word-wrapping.</param>
    /// <returns>A string representing the formatted bubble with the message.</returns>
    string CreateBubble(string message, bool isThought, int wrapWidth, bool noWrap = false);
}
