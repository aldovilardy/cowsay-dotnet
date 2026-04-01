using CowSay.Core.Models;

namespace CowSay.Core.Services;

/// <summary>
/// Factory class to create Face objects based on CowMode or manual parameters.
/// </summary>
public class FaceFactory
{
    /// <summary>
    /// Creates a new Face instance with eyes and tongue configured according to the specified mode or provided values.
    /// </summary>
    /// <remarks>
    /// If either eyes or tongue is provided, those values are used directly and the mode is ignored for those features. 
    /// Otherwise, the mode determines the default face appearance.
    /// </remarks>
    /// <param name="mode">The mode that determines the default appearance of the face if custom eyes or tongue are not specified.</param>
    /// <param name="eyes">The string to use for the eyes. If null or empty, a default value is chosen based on the mode.</param>
    /// <param name="tongue">The string to use for the tongue. If null or empty, a default value is chosen based on the mode.</param>
    /// <returns>A Face instance with eyes and tongue set according to the specified parameters or the selected mode.</returns>
    public static Face Create(CowMode mode, string? eyes = null, string? tongue = null)
    {
        if (mode != CowMode.Default)
            return mode switch
            {
                CowMode.Dead => new Face("xx", "U "),
                CowMode.Borg => new Face("==", "  "),
                CowMode.Greedy => new Face("$$", "  "),
                CowMode.Paranoid => new Face("@@", "  "),
                CowMode.Stoned => new Face("**", "U "),
                CowMode.Tired => new Face("--", "  "),
                CowMode.Wired => new Face("LL", "  "),
                CowMode.Youthful => new Face("..", "  "),
                _ => new Face("oo", "  ")
            };

        var normalizedEyes = NormalizeEyes(eyes);
        var normalizedTongue = NormalizeTongue(tongue);

        return new Face(normalizedEyes, normalizedTongue);
    }

    /// <summary>
    /// Normalizes the input string representing eyes to a two-character value suitable for display.
    /// </summary>
    /// <param name="eyes">The input string representing eyes. Can be null or any length.</param>
    /// <returns>
    /// A two-character string representing eyes. 
    /// Returns "oo" if the input is null or empty; otherwise, 
    /// returns the first two characters if the input is longer than two characters, 
    /// or the input itself if it is one or two characters long.
    /// </returns>
    private static string NormalizeEyes(string? eyes) =>
        string.IsNullOrEmpty(eyes) ? "oo" : eyes.Length <= 2 ? eyes : eyes[..2];

    /// <summary>
    /// Normalizes the specified tongue code to a two-character string.
    /// </summary>
    /// <param name="tongue">The tongue code to normalize. Must be exactly two characters, or null or empty.</param>
    /// <returns>
    /// A two-character string representing the normalized tongue code. Returns two spaces if <paramref name="tongue"/> is null or empty.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="tongue"/> is not null or empty and does not have exactly two characters.</exception>
    private static string NormalizeTongue(string? tongue)
    {
        if (string.IsNullOrEmpty(tongue))
            return "  ";

        if (tongue.Length != 2)
            throw new ArgumentException("Tongue must be exactly 2 characters.", nameof(tongue));

        return tongue;
    }
}
