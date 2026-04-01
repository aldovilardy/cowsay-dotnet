using System.Management.Automation;

namespace Cowsay.PowerShell;

/// <summary>
/// Cmdlet to generate a Cowsay message (speech bubble).
/// </summary>
[Cmdlet(VerbsCommon.Get, "Cowsay")]
public class GetCowsayCommand : GetCowBaseCommand
{
    /// <inheritdoc />
    protected override bool IsThought => false;
}
