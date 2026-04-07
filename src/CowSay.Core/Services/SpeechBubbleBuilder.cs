using CowSay.Core.Models;
using System.Globalization;
using System.Text;

namespace CowSay.Core.Services;

public class SpeechBubbleBuilder
{
    private const int MaxWidth = 40;

    /// <summary>
    /// Builds a formatted speech or thought bubble containing the specified message.
    /// </summary>
    /// <remarks>
    /// The returned string includes borders and padding to visually represent either a speech or thought bubble, depending on the value of <paramref name="isThought"/>. 
    /// The formatting adjusts to fit the longest line in the message.
    /// </remarks>
    /// <param name="message">The text to display inside the bubble. May contain multiple lines.</param>
    /// <param name="isThought">
    /// A value indicating whether to format the bubble as a thought bubble (<see langword="true"/>) or a speech bubble (<see langword="false"/>).
    /// </param>
    /// <returns>A string representing the formatted speech or thought bubble with the message text.</returns>
    public static string Build(string message, bool isThought)
    {
        var bubbleType = isThought ? SpeechBubble.BubbleType.Thought : SpeechBubble.BubbleType.Speech;
        var bubble = new SpeechBubble(message, bubbleType);
        var lines = SplitMessage(bubble.Content);
        var maxLineLength = lines.Max(line => GetVisualLength(line));
        var borderChar = bubble.Type == SpeechBubble.BubbleType.Speech ? '\\' : 'o';
        var topBorder = $" {new string('_', maxLineLength + 2)} ";
        var bottomBorder = $" {new string('-', maxLineLength + 2)} ";
        var bubbleLines = lines.Select(line => $"| {PadToVisualLength(line, maxLineLength)} |").ToArray();

        return $"{topBorder}\n{string.Join("\n", bubbleLines)}\n{bottomBorder}";
    }

    /// <summary>
    /// Splits the specified message into multiple lines, ensuring that each line does not exceed the maximum allowed width.
    /// </summary>
    /// <remarks>
    /// Words longer than the maximum width are split across multiple lines. Leading and trailing whitespace in the input message is ignored. 
    /// Lines are constructed to avoid breaking words when possible.
    /// </remarks>
    /// <param name="message">The message to split into lines. Can be null, empty, or contain whitespace.</param>
    /// <returns>
    /// An array of strings, each representing a line of the original message. Returns an empty array if the input is null, empty, or consists only of whitespace.
    /// </returns>
    private static string[] SplitMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return [];

        var estimatedLines = message.Length / MaxWidth + 1;
        var lines = new List<string>(estimatedLines);
        var sb = new StringBuilder(MaxWidth);
        var span = message.AsSpan();
        int start = 0;

        while (start < span.Length)
        {
            // Skip spaces to find the start of the next word
            while (start < span.Length && char.IsWhiteSpace(span[start]))
                start++;

            if (start >= span.Length)
                break;

            // Find the end of the word
            int end = start;

            while (end < span.Length && !char.IsWhiteSpace(span[end]))
                end++;

            var word = span[start..end];

            // Handle words longer than MaxWidth by splitting them
            if (word.Length > MaxWidth)
            {
                if (sb.Length > 0)
                {
                    lines.Add(sb.ToString());
                    sb.Clear();
                }

                for (int i = 0; i < word.Length; i += MaxWidth)
                    lines.Add(word.Slice(i, Math.Min(MaxWidth, word.Length - i)).ToString());

                start = end;
                continue;
            }

            // Logic: If adding this word exceeds MaxWidth, flush the current buffer
            if (sb.Length > 0 && sb.Length + 1 + word.Length > MaxWidth)
            {
                lines.Add(sb.ToString());
                sb.Clear();
            }

            // Append the word (with a space if it's not the start of a line)
            if (sb.Length > 0)
                sb.Append(' ');

            sb.Append(word);

            start = end;
        }

        // Add the final remaining line
        if (sb.Length > 0)
            lines.Add(sb.ToString());

        return [.. lines];
    }

    /// <summary>
    /// Calculates the visual length of a string considering Unicode grapheme clusters (e.g., emojis).
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <returns>The visual length in text elements.</returns>
    private static int GetVisualLength(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;
        return new StringInfo(text).LengthInTextElements;
    }

    /// <summary>
    /// Pads a string to the specified visual length using spaces, supporting Unicode text elements like emojis.
    /// </summary>
    /// <param name="text">The text to pad.</param>
    /// <param name="totalVisualLength">The desired visual length.</param>
    /// <returns>The padded string.</returns>
    private static string PadToVisualLength(string text, int totalVisualLength)
    {
        var currentLength = GetVisualLength(text);
        var paddingNeeded = totalVisualLength - currentLength;
        if (paddingNeeded <= 0)
            return text;
        return text + new string(' ', paddingNeeded);
    }
}
