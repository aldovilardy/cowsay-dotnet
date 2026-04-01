# Cowsay.PowerShell.psm1
# Manages the COWPATH environment variable so the bundled cow files are discoverable
# by both the PowerShell cmdlets and the CLI tools (cowsay/cowthink).

# Save the original COWPATH value so we can restore it when the module is removed.
$script:OriginalCowPath = $env:COWPATH

# Resolve the module's bundled cows directory (assets/cows alongside this script).
$script:ModuleCowsDir = Join-Path $PSScriptRoot 'assets' 'cows'

if (Test-Path $script:ModuleCowsDir -PathType Container) {
    if ([string]::IsNullOrEmpty($env:COWPATH)) {
        $env:COWPATH = $script:ModuleCowsDir
    }
    elseif ($env:COWPATH -split [IO.Path]::PathSeparator -notcontains $script:ModuleCowsDir) {
        # Prepend the module's cows so they are found first; user directories come after.
        $env:COWPATH = "$script:ModuleCowsDir$([IO.Path]::PathSeparator)$env:COWPATH"
    }
}

# Restore the original COWPATH when the module is removed (Remove-Module).
$MyInvocation.MyCommand.ScriptBlock.Module.OnRemove = {
    $env:COWPATH = $script:OriginalCowPath
}
