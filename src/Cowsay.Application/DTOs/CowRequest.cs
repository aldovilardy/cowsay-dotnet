using CowSay.Core.Models;

namespace Cowsay.Application.DTOs;

/// <summary>
/// Represents a request to generate a cowsay message.
/// </summary>
/// <param name="Message">The message to be displayed by the cow.</param>
/// <param name="CowName">The name of the cow character.</param>
/// <param name="Mode">The mode of the cow (e.g., default, dead).</param>
/// <param name="Eyes">Custom eyes for the cow.</param>
/// <param name="Tongue">Custom tongue for the cow.</param>
/// <param name="IsThought">Indicates if the message is a thought.</param>
/// <param name="WrapWidth">The width at which the message should wrap.</param>
/// <param name="NoWrap">If true, disables word wrapping and preserves message lines as-is.</param>
public record CowRequest(
    string Message,
    string CowName = "default",
    CowMode Mode = CowMode.Default,
    string? Eyes = null,
    string? Tongue = null,
    bool IsThought = false,
    int WrapWidth = 40,
    bool NoWrap = false
);
