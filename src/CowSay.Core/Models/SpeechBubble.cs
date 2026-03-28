using static CowSay.Core.Models.SpeechBubble;

namespace CowSay.Core.Models;

/// <summary>
/// Represents a speech bubble logic that can be used to display the content of a message in a specific style (speech or thought).
/// </summary>
/// <param name="Content">The content of the speech bubble.</param>
/// <param name="Type">The type of the speech bubble (speech or thought).</param>
public record SpeechBubble(string Content, BubbleType Type)
{
    /// <summary>
    /// Specifies the definition type of bubble used to display character speech (\) or thought (o).    
    /// </summary>
    /// <remarks>
    /// Use this enumeration to distinguish between speech bubbles (\), which represent spoken dialogue, and thought bubbles (o), which represent internal thoughts. 
    /// This can be useful for rendering different visual styles or behaviors based on the type of content being displayed.
    /// </remarks>
    public enum BubbleType
    {
        /// <summary>
        /// Represents a speech or spoken text to be processed or synthesized.
        /// </summary>
        Speech,

        /// <summary>
        /// Represents a single unit of thought or idea within the application.
        /// </summary>
        Thought
    }
}
