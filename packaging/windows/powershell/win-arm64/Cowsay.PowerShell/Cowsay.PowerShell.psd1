@{
    # Script module that manages COWPATH on import/remove
    RootModule        = 'Cowsay.PowerShell.psm1'

    # Binary module containing the Get-Cowsay and Get-Cowthink cmdlets
    NestedModules     = @('Cowsay.PowerShell.dll')

    ModuleVersion = '1.0.0'
    GUID              = '03ff6100-fafb-4c63-a1d6-73681bc4feef'
    Author            = 'Aldo Vilardy'
    CompanyName       = 'cowsay-dotnet'
    Copyright         = 'Copyright (c) 2026 Aldo Vilardy. Original cowsay Copyright 1999-2002 Tony Monroe. cowsay-org Copyright 2016-2024 Andrew Janke. Licensed under GPL-3.0-or-later.'
    Description       = 'A .NET reimplementation of the classic cowsay/cowthink utilities as PowerShell cmdlets, inspired by the original cowsay by Tony Monroe and the cowsay-org fork by Andrew Janke. Supports all original cowfiles, custom eyes/tongue, COWPATH, and both speech and thought bubbles. Licensed under GPL-3.0-or-later.'

    # Minimum version of PowerShell required (PowerShell 7+)
    PowerShellVersion = '7.0'

    # Cmdlets exported from the binary nested module
    CmdletsToExport   = @('Get-Cowsay', 'Get-Cowthink')

    # No functions, aliases, or variables exported
    FunctionsToExport = @()
    AliasesToExport   = @()
    VariablesToExport = @()

    PrivateData = @{
        PSData = @{
            Tags       = @('cowsay', 'cowthink', 'ascii-art', 'fun', 'dotnet')
            LicenseUri = 'https://www.gnu.org/licenses/gpl-3.0.en.html'
            ProjectUri = 'https://github.com/cowsay-dotnet/cowsay-dotnet'
        }
    }
}

