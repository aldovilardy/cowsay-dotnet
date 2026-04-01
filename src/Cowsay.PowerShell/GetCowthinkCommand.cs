using System.Management.Automation;

namespace Cowsay.PowerShell;

/// <summary>
/// Cmdlet to generate a Cowthink message (thought bubble).
/// </summary>
[Cmdlet(VerbsCommon.Get, "Cowthink")]
public class GetCowthinkCommand : GetCowBaseCommand
{
    /// <inheritdoc />
    protected override bool IsThought => true;
}
