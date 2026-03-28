using CowSay.Core.Models;

namespace CowSay.Core.Interfaces;

/// <summary>
/// Defines a contract to merge the message with the ASCII cow character based on a given <see cref="Cow"/> and a <see cref="RenderContext"/>. 
/// This is responsible for processing the cow template and generating the final string output that represents the rendered cow character with the appropriate substitutions 
/// for eyes, tongue, and speech/thought character as specified in the rendering context.
/// </summary>
public interface ICowRenderer
{
    /// <summary>
    /// Renders a cow character based on the provided <see cref="Cow"/> and <see cref="RenderContext"/>.
    /// </summary>
    /// <param name="cow">The cow to render.</param>
    /// <param name="context">The rendering context.</param>
    /// <returns>
    /// A string representation of the rendered cow.
    /// </returns>
    string Render(Cow cow, RenderContext context);
}
