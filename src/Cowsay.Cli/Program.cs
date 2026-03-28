using Cowsay.Application.DTOs;
using Cowsay.Application.Handlers;
using Cowsay.Infrastructure;
using Cowsay.Infrastructure.Repositories;
using CowSay.Core.Interfaces;
using CowSay.Core.Models;
using CowSay.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;

// 1. Dependency container configuration (DI)
var services = new ServiceCollection();

// Register Infrastructure
services.AddSingleton<ICowRepository>(new FileCowRepository(CowFilesDirectoryResolver.ResolveCowsDirectory()));

// Register Core Services
services.AddSingleton<IBubbleService, BubbleService>();
services.AddSingleton<ITemplateEngine, CowTemplateEngine>();

// Register Application Use Cases
services.AddTransient<GenerateCowSay>();

var serviceProvider = services.BuildServiceProvider();

// 2. CLI definition with System.CommandLine
var rootCommand = new RootCommand("Modern .NET clone of cowsay/cowthink - configurable speaking/thinking cow (and a bit more)");

// Classic Arguments and Options
Argument<string[]> messageArg = new("message")
{
    Description = "The message to be displayed by the cow",
    Arity = ArgumentArity.ZeroOrMore
};

Option<string?> cowOption = new("--file") { Description = "The name of the character (.cow)" };
cowOption.Aliases.Add("-f");

Option<string?> eyesOption = new("--eyes") { Description = "Customize the eyes" };
eyesOption.Aliases.Add("-e");

Option<string?> tongueOption = new("--tongue") { Description = "Customize the tongue" };
tongueOption.Aliases.Add("-T");

Option<bool> borgOption = new("-b") { Description = "Initiates Borg mode (eyes ==)" };
Option<bool> deadOption = new("-d") { Description = "Causes the cow to appear dead (eyes xx)" };
Option<bool> greedyOption = new("-g") { Description = "Invokes greedy mode (eyes $$)" };
Option<bool> paranoidOption = new("-p") { Description = "Causes a state of paranoia to come over the cow (eyes @@)" };
Option<bool> stonedOption = new("-s") { Description = "Makes the cow appear thoroughly stoned (eyes **)" };
Option<bool> tiredOption = new("-t") { Description = "Yields a tired cow (eyes --)" };
Option<bool> wiredOption = new("-w") { Description = "Is somewhat the opposite of -t, and initiates wired mode (eyes LL)" };
Option<bool> youthfulOption = new("-y") { Description = "Brings on the cow's youthful appearance (eyes ..)" };

Option<bool> noWrapOption = new("-n") { Description = "Do not word-wrap input; read message from standard input." };
noWrapOption.Aliases.Add("--no-wrap");

Option<int?> widthOption = new("-W") { Description = "Wrap width in columns (default: 40)" };
widthOption.Aliases.Add("--width");

Option<bool> listOption = new("-l") { Description = "List available cowfiles on the current search path." };
listOption.Aliases.Add("--list");

rootCommand.Arguments.Add(messageArg);
rootCommand.Options.Add(cowOption);
rootCommand.Options.Add(eyesOption);
rootCommand.Options.Add(tongueOption);

List<Option<bool>> modes = [borgOption, deadOption, greedyOption, paranoidOption, stonedOption, tiredOption, wiredOption, youthfulOption];
modes.ForEach(rootCommand.Options.Add);

rootCommand.Options.Add(noWrapOption);
rootCommand.Options.Add(widthOption);
rootCommand.Options.Add(listOption);

// 3. Handler: The bridge between the CLI and the Application Use Case
rootCommand.SetAction(parseResult =>
{
    try
    {
        var useCase = serviceProvider.GetRequiredService<GenerateCowSay>();

        if (parseResult.GetValue(listOption))
        {
            var repository = serviceProvider.GetRequiredService<ICowRepository>();
            Console.WriteLine(string.Join(" ", repository.ListAvailableCows()));
            return;
        }

        // Mapping options to a domain mode        
        var mode = modes.FirstOrDefault(opt => parseResult.GetValue(opt)) switch
        {
            var op when op == borgOption => CowMode.Borg,
            var op when op == deadOption => CowMode.Dead,
            var op when op == greedyOption => CowMode.Greedy,
            var op when op == paranoidOption => CowMode.Paranoid,
            var op when op == stonedOption => CowMode.Stoned,
            var op when op == tiredOption => CowMode.Tired,
            var op when op == wiredOption => CowMode.Wired,
            var op when op == youthfulOption => CowMode.Youthful,
            _ => CowMode.Default
        };

        var messageParts = parseResult.GetValue(messageArg) ?? [];
        var noWrap = parseResult.GetValue(noWrapOption);

        if (noWrap && messageParts.Length > 0)
            throw new ArgumentException("When using -n, provide the message via standard input only.");

        var message = messageParts.Length > 0
            ? string.Join(" ", messageParts)
            : ReadMessageFromStandardInput();

        var isThoughtMode = IsCowThinkInvocation();
        var wrapWidth = parseResult.GetValue(widthOption) ?? 40;

        // Create the request DTO
        CowRequest request = new(
            message,
            parseResult.GetValue(cowOption) ?? "default",
            mode,
            parseResult.GetValue(eyesOption),
            parseResult.GetValue(tongueOption),
            isThoughtMode,
            wrapWidth,
            noWrap);

        // Execute and display the result
        var result = useCase.Execute(request);
        Console.WriteLine(result);
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine($"Error: {ex.Message}");
        Console.ResetColor();
    }
});

// 4. Program execution
return await rootCommand.Parse(args).InvokeAsync();

/// <summary>
/// Reads the message from standard input if input is redirected.
/// </summary>
/// <returns>The message read from standard input, or an empty string if input is not redirected.</returns>
static string ReadMessageFromStandardInput() =>
    !Console.IsInputRedirected ? string.Empty : Console.In.ReadToEnd().TrimEnd('\r', '\n');

/// <summary>
/// Determines if the current process is a cowthink invocation.
/// </summary>
/// <returns>True if the process is cowthink, otherwise false.</returns>
static bool IsCowThinkInvocation() => 
    Path
    .GetFileNameWithoutExtension(Environment.ProcessPath ?? string.Empty)
    .Equals("cowthink", StringComparison.OrdinalIgnoreCase);