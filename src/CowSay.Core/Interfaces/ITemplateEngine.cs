using CowSay.Core.Models;

namespace CowSay.Core.Interfaces;

/// <summary>
/// The heart of the rendering process. 
/// Defines a contract for processing template strings by replacing placeholders with values from a Face object and a thought character.
/// </summary>
/// <remarks>
/// Responsible for injecting variable values into ASCII art templates, such as those used in .cow files. 
/// The interface is typically used to render dynamic text-based art by substituting placeholders with actual values.
/// </remarks>
public interface ITemplateEngine
{
    /// <summary>
    /// Processes the template by replacing placeholders $eyes, $tongue, and $thoughts 
    /// with actual values from the Face object and the thought character producing the final ASCII art.
    /// </summary>
    /// <param name="template">The character template raw string containing placeholders.</param>
    /// <param name="face">The Face object containing the values for eyes and tongue.</param>
    /// <param name="thoughtChar">The character representing thoughts (\ o o).</param>
    /// <returns>The processed template with placeholders replaced by actual values with the rendered character.</returns>
    string Process(string template, Face face, char thoughtChar);
}
