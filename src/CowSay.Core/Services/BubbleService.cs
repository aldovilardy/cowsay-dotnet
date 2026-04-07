using CowSay.Core.Interfaces;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace CowSay.Core.Services;

/// <inheritdoc/>
public class BubbleService : IBubbleService
{
    /// <inheritdoc/>
    public string CreateBubble(string message, bool isThought, int wrapWidth, bool noWrap = false)
    {
        var width = wrapWidth > 0 ? wrapWidth : 40;
        var lines = noWrap
            ? SplitPreservingLines(message ?? string.Empty)
            : WrapMessage(message ?? string.Empty, width);

        if (lines.Count == 0)
            lines.Add(string.Empty);

        var maxLineLength = lines.Max(line => GetVisualLength(line));
        var topBorder = $" {new string('-', maxLineLength + 2)} ";
        var bottomBorder = $" {new string('-', maxLineLength + 2)} ";

        var sb = new StringBuilder();
        sb.AppendLine(topBorder);

        if (lines.Count == 1)
            sb.AppendLine($"{GetLeftBorder(0, 1, isThought)} {PadToVisualLength(lines[0], maxLineLength)} {GetRightBorder(0, 1, isThought)}");
        else
            for (var i = 0; i < lines.Count; i++)
                sb.AppendLine($"{GetLeftBorder(i, lines.Count, isThought)} {PadToVisualLength(lines[i], maxLineLength)} {GetRightBorder(i, lines.Count, isThought)}");

        sb.Append(bottomBorder);
        return sb.ToString();
    }

    /// <summary>
    /// Splits the specified string into a list of lines, preserving empty lines and normalizing line endings.
    /// </summary>
    /// <remarks>
    /// Line endings are normalized to Unix-style (\n) before splitting. 
    /// The resulting list preserves the order and content of lines, including empty lines.
    /// </remarks>
    /// <param name="message">The string to split into lines. Can contain line breaks in either Windows (\r\n) or Unix (\n) format.</param>
    /// <returns>A list of strings, each representing a line from the input. The list contains a single empty string if the input is empty.</returns>
    private static List<string> SplitPreservingLines(string message) =>
        message.Length == 0 ? [string.Empty] : [.. message.Replace("\r\n", "\n").Split('\n')];

    /// <summary>
    /// Splits the specified message into multiple lines, ensuring that each line does not exceed the specified width.
    /// </summary>
    /// <remarks>
    /// Lines are broken at word boundaries when possible. 
    /// If a word exceeds the specified width, it is split across multiple lines. 
    /// Leading and trailing whitespace in the message is ignored.
    /// </remarks>
    /// <param name="message">
    /// The message to be wrapped into lines. If the message is null, empty, or consists only of whitespace, a single empty string is returned.
    /// </param>
    /// <param name="width">
    /// The maximum number of characters allowed in each line. Must be greater than zero.
    /// </param>
    /// <returns>
    /// A list of strings where each string represents a line of the wrapped message. Words longer than the specified width are split across lines.
    /// </returns>
    private static List<string> WrapMessage(string message, int width)
    {
        if (string.IsNullOrWhiteSpace(message))
            return [string.Empty];

        var words = message.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        var currentLine = new StringBuilder();
        var currentVisualLength = 0;

        foreach (var word in words)
        {
            var wordVisualLength = GetVisualLength(word);

            if (wordVisualLength > width)
            {
                if (currentLine.Length > 0)
                {
                    lines.Add(currentLine.ToString());
                    currentLine.Clear();
                    currentVisualLength = 0;
                }

                for (var i = 0; i < wordVisualLength; i += width)
                {
                    var chunkLength = Math.Min(width, wordVisualLength - i);
                    lines.Add(SubstringByVisualLength(word, i, chunkLength));
                }

                continue;
            }

            var proposedLength = currentVisualLength == 0
                ? wordVisualLength
                : currentVisualLength + 1 + wordVisualLength;

            if (proposedLength > width)
            {
                lines.Add(currentLine.ToString());
                currentLine.Clear();
                currentVisualLength = 0;
            }

            if (currentLine.Length > 0)
            {
                currentLine.Append(' ');
                currentVisualLength++;
            }

            currentLine.Append(word);
            currentVisualLength += wordVisualLength;
        }

        if (currentLine.Length > 0)
            lines.Add(currentLine.ToString());

        return lines;
    }

    /// <summary>
    /// Extracts a substring based on visual text elements (Unicode grapheme clusters).
    /// </summary>
    /// <param name="text">The input string from which to extract the substring.</param>
    /// <param name="start">The zero-based starting index of the substring in terms of visual text elements.</param>
    /// <param name="length">The number of visual text elements to include in the substring.</param>
    /// <returns>A substring containing the specified number of visual text elements starting from the specified index.</returns>
    private static string SubstringByVisualLength(string text, int start, int length)
    {
        var info = new StringInfo(text);
        var startIndex = Math.Min(start, info.LengthInTextElements);
        var remainingLength = info.LengthInTextElements - startIndex;
        var extractLength = Math.Min(length, remainingLength);

        return extractLength switch
        {
            <= 0 => string.Empty,
            _ => info.SubstringByTextElements(startIndex, extractLength)
        };
    }

    /// <summary>
    /// Determines the appropriate left border character for an item based on its position and context.
    /// </summary>
    /// <param name="index">
    /// The zero-based index of the item within the collection. Used to determine if the item is the first or last in the sequence.
    /// </param>
    /// <param name="count">The total number of items in the collection. Must be greater than zero.</param>
    /// <param name="isThought">A value indicating whether the item represents a thought. If <see langword="true"/>, a specific border character is used.</param>
    /// <returns>A character representing the left border for the item, based on its position and whether it is a thought.</returns>
    private static char GetLeftBorder(int index, int count, bool isThought) => 
        index switch
        {
            _ when isThought => '(',
            _ when count == 1 => '<',
            0 => '/',
            _ when index == count - 1 => '\\',
            _ => '|'
        };

    /// <summary>
    /// Determines the appropriate right border character for an item based on its position and context.
    /// </summary>
    /// <remarks>
    /// The returned character is intended for use in formatting or rendering scenarios where visual distinction of item borders is required. 
    /// The method selects different border characters for the first, last, and intermediate items, as well as for items marked as thoughts.
    /// </remarks>
    /// <param name="index">The zero-based index of the item within the collection. Must be greater than or equal to 0 and less than
    /// <paramref name="count"/>.</param>
    /// <param name="count">The total number of items in the collection. Must be greater than 0.</param>
    /// <param name="isThought">A value indicating whether the item represents a thought. If <see langword="true"/>, a parenthesis is used as the border.</param>
    /// <returns>
    /// A character representing the right border for the specified item. The character varies depending on the item's position and whether it is a thought.
    /// </returns>
    private static char GetRightBorder(int index, int count, bool isThought) => 
        index switch
        {
            _ when isThought => ')',
            _ when count == 1 => '>',
            0 => '\\',
            _ when index == count - 1 => '/',
            _ => '|'
        };

    /// <summary>
    /// Calculates the visual length of a string considering Unicode grapheme clusters (e.g., emojis).
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <returns>The visual length in text elements.</returns>
    private static int GetVisualLength(string text) => 
        string.IsNullOrEmpty(text) ? 0 : new StringInfo(text).LengthInTextElements;

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

        return paddingNeeded <= 0 ? text : $"{text}{new string(' ', paddingNeeded)}";
    }
}
