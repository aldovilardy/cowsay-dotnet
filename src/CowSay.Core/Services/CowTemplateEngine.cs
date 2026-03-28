using CowSay.Core.Interfaces;
using CowSay.Core.Models;
using System.Text.RegularExpressions;

namespace CowSay.Core.Services;

/// <inheritdoc/>
public class CowTemplateEngine : ITemplateEngine
{
    /// <inheritdoc/>
    public string Process(string template, Face face, char thoughtChar)
    {
        var content = ExtractAsciiContent(template);

        new List<KeyValuePair<string, string>>
        {
            new(@"\$eyes|\${eyes}", face.Eyes),
            new(@"\$tongue|\${tongue}", face.Tongue),
            new(@"\$thoughts|\${thoughts}", thoughtChar.ToString())
        }
        .ForEach(kv => content = Regex.Replace(content, kv.Key, kv.Value));

        return UnescapePerlStrings(content);
    }

    /// <summary>
    /// Extracts the ASCII content located between the first and last occurrence of the delimiter "EOC" in the specified template string.
    /// </summary>
    /// <remarks>
    /// The method performs a case-sensitive search for the delimiter "EOC". 
    /// If only one or no occurrences of the delimiter are found, the entire input string is returned unchanged.
    /// </remarks>
    /// <param name="template">
    /// The input string from which to extract ASCII content. The string is expected to contain at least two occurrences of the delimiter "EOC".
    /// </param>
    /// <returns>
    /// A substring containing the content found between the first and last "EOC" delimiters. 
    /// If the delimiters are not found in the expected order, returns the original template string.
    /// </returns>
    private static string ExtractAsciiContent(string template)
    {
        var isLongDelimiter = template.Contains("EOC\";");
        var start = isLongDelimiter ? template.IndexOf("EOC\";") : template.IndexOf("EOC;");
        var end = template.LastIndexOf("EOC");
        return start >= 0 && end > start ? template.Substring(start + (isLongDelimiter ? 5 : 4), end - start - (isLongDelimiter ? 5 : 4)) : template;
    }

    /// <summary>
    /// Replaces and protectsPerl-style escape sequences in the specified string with their unescaped character equivalents.
    /// </summary>
    /// <remarks>
    /// This method unescapes double backslashes (\\), as well as escaped at signs (\@) and dollar signs (\$), commonly used in Perl string literals. 
    /// The input string is not modified if it contains no recognized escape sequences.
    /// </remarks>
    /// <param name="input">The input string containing Perl-style escape sequences to be unescaped. Cannot be null.</param>
    /// <returns>A string with Perl-style escape sequences replaced by their corresponding unescaped characters.</returns>
    private static string UnescapePerlStrings(string input) =>
        input.Replace("\\\\", "\\").Replace("\\@", "@").Replace("\\$", "$");
}