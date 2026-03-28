using Cowsay.Application.DTOs;
using CowSay.Core.Interfaces;
using CowSay.Core.Services;

namespace Cowsay.Application.Handlers;

/// <summary>
/// Service responsible for generating CowSay messages.
/// </summary>
/// <param name="repository">The repository to fetch cow templates.</param>
/// <param name="bubbleService">The service to create message bubbles.</param>
/// <param name="templateEngine">The engine to process templates.</param>
public class GenerateCowSay(
    ICowRepository repository,
    IBubbleService bubbleService,
    ITemplateEngine templateEngine)
{
    /// <summary>
    /// Generates an ASCII art representation of a cow with a speech or thought bubble based on the specified request parameters.
    /// </summary>
    /// <remarks>
    /// The output is determined by the properties of the provided request, including the cow's appearance and the message to display. 
    /// The method supports both speech and thought bubbles, and allows customization of the cow's face and bubble formatting.
    /// </remarks>
    /// <param name="request">The request containing the cow's name, message, display mode, and formatting options. Cannot be null.</param>
    /// <returns>
    /// A string containing the formatted ASCII art of the cow with the generated bubble. The result includes both the bubble and the cow art.
    /// </returns>
    public string Execute(CowRequest request)
    {
        var cow = repository.GetCowByName(request.CowName);
        var face = FaceFactory.Create(request.Mode, request.Eyes, request.Tongue);
        var bubble = bubbleService.CreateBubble(request.Message, request.IsThought, request.WrapWidth, request.NoWrap);
        var cowAscii = templateEngine.Process(cow.Template, face, request.IsThought ? 'o' : '\\');

        return $"{bubble}\n{cowAscii}";
    }
}
