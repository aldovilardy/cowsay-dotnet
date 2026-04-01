using Cowsay.Application.DTOs;
using Cowsay.Application.Handlers;
using Cowsay.Infrastructure;
using Cowsay.Infrastructure.Repositories;
using CowSay.Core.Interfaces;
using CowSay.Core.Models;
using CowSay.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Management.Automation;

namespace Cowsay.PowerShell;

/// <summary>
/// Base cmdlet that contains all shared parameters and logic for cowsay/cowthink.
/// Subclasses only need to specify <see cref="IsThought"/> and the <see cref="CmdletAttribute"/>.
/// </summary>
public abstract class GetCowBaseCommand : PSCmdlet
{
    /// <summary>
    /// When <c>true</c>, the cow "thinks" (thought bubble with <c>o</c> connectors);
    /// when <c>false</c>, the cow "says" (speech bubble with <c>\</c> connectors).
    /// </summary>
    protected abstract bool IsThought { get; }

    /// <summary>
    /// The message to display inside the cow bubble.
    /// </summary>
    [Parameter(Position = 0, Mandatory = false)]
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Name of the character (.cow) to use. Defaults to "default".
    /// </summary>
    [Parameter]
    public string Cow { get; init; } = "default";

    /// <summary>
    /// Custom eyes. The first 2 characters are used.
    /// </summary>
    [Parameter]
    public string? Eyes { get; init; }

    /// <summary>
    /// Custom tongue. Must be exactly 2 characters.
    /// </summary>
    [Parameter]
    public string? Tongue { get; init; }

    /// <summary>
    /// Wrap width in columns. Defaults to 40.
    /// </summary>
    [Parameter]
    public int Width { get; init; } = 40;

    /// <summary>
    /// Disables word wrapping and preserves message lines.
    /// </summary>
    [Parameter]
    public SwitchParameter NoWrap { get; init; }

    /// <summary>
    /// Lists available cowfiles.
    /// </summary>
    [Parameter]
    public SwitchParameter List { get; init; }

    /// <summary>
    /// Select Borg eye style ("==").
    /// </summary>
    [Parameter]
    public SwitchParameter Borg { get; init; }

    /// <summary>
    /// Select Dead eye style ("xx").
    /// </summary>
    [Parameter]
    public SwitchParameter Dead { get; init; }

    /// <summary>
    /// Select Greedy eye style ("$$").
    /// </summary>
    [Parameter]
    public SwitchParameter Greedy { get; init; }

    /// <summary>
    /// Select Paranoid eye style ("@@").
    /// </summary>
    [Parameter]
    public SwitchParameter Paranoid { get; init; }

    /// <summary>
    /// Select Stoned eye style ("**").
    /// </summary>
    [Parameter]
    public SwitchParameter Stoned { get; init; }

    /// <summary>
    /// Select Tired eye style ("--").
    /// </summary>
    [Parameter]
    public SwitchParameter Tired { get; init; }

    /// <summary>
    /// Select Wired eye style ("LL").
    /// </summary>
    [Parameter]
    public SwitchParameter Wired { get; init; }

    /// <summary>
    /// Select Youthful eye style ("..").
    /// </summary>
    [Parameter]
    public SwitchParameter Youthful { get; init; }

    /// <summary>
    /// Processes the current record by generating and writing a cowsay/cowthink ASCII-art string to the pipeline.
    /// </summary>
    /// <remarks>
    /// The cmdlet maps the provided switches to a single <see cref="CowMode"/> value 
    /// and then invokes the application use case <see cref="GenerateCowSay"/> to produce the output. 
    /// If multiple mode switches are provided, the following precedence is applied (highest first):
    /// <list type="bullet">
    /// <item><description><see cref="Borg"/></description></item>
    /// <item><description><see cref="Dead"/></description></item>
    /// <item><description><see cref="Greedy"/></description></item>
    /// <item><description><see cref="Paranoid"/></description></item>
    /// <item><description><see cref="Stoned"/></description></item>
    /// <item><description><see cref="Tired"/></description></item>
    /// <item><description><see cref="Wired"/></description></item>
    /// <item><description><see cref="Youthful"/></description></item>
    /// <item><description>Default: <see cref="CowMode.Default"/></description></item>
    /// </list>
    /// The cmdlet creates a short-lived service provider to resolve <see cref="GenerateCowSay"/>. 
    /// Callers should treat the cmdlet as a thin adapter; the heavy lifting is performed by the application layer.
    /// </remarks>
    protected override void ProcessRecord()
    {
        var provider = ConfigureServices();

        if (List)
        {
            WriteObject(string.Join(" ", provider.GetRequiredService<ICowRepository>().ListAvailableCows()));
            return;
        }

        var output = provider
            .GetRequiredService<GenerateCowSay>()
            .Execute(
                new CowRequest(
                    Message,
                    Cow,
                    Borg ? CowMode.Borg :
                    Dead ? CowMode.Dead :
                    Greedy ? CowMode.Greedy :
                    Paranoid ? CowMode.Paranoid :
                    Stoned ? CowMode.Stoned :
                    Tired ? CowMode.Tired :
                    Wired ? CowMode.Wired :
                    Youthful ? CowMode.Youthful : CowMode.Default,
                    Eyes,
                    Tongue,
                    IsThought,
                    Width,
                    NoWrap));

        WriteObject(output);
    }

    /// <summary>
    /// Configures and returns a service provider with required application services.
    /// </summary>
    /// <remarks>
    /// The returned <see cref="IServiceProvider"/> is created for the cmdlet invocation and contains the
    /// minimal registrations required to execute <see cref="GenerateCowSay"/>: an <see cref="ICowRepository"/>, 
    /// the bubble/templating core services, and the application use case. 
    /// The provider is not cached.
    /// </remarks>
    /// <returns>An <see cref="IServiceProvider"/> with the configured services.</returns>
    public static IServiceProvider ConfigureServices() =>
        new ServiceCollection()
            // Infrastructure
            .AddSingleton<ICowRepository>(new FileCowRepository(CowFilesDirectoryResolver.ResolveCowsDirectory()))
            // Core Services
            .AddSingleton<IBubbleService, BubbleService>()
            .AddSingleton<ITemplateEngine, CowTemplateEngine>()
            // Application Use Cases
            .AddTransient<GenerateCowSay>()
            .BuildServiceProvider();
}
