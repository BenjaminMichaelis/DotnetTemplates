param(
    [string[]]$Targets,
    [switch]$DiscoverFromCurrentDirectory,
    [switch]$RestoreTargets
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-DotNetFormatCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

if (-not $Targets -or $Targets.Count -eq 0) {
    if (-not $DiscoverFromCurrentDirectory) {
        throw 'Specify -Targets or pass -DiscoverFromCurrentDirectory.'
    }

    $solutionFile = Get-ChildItem -Path . -File | Where-Object { $_.Extension -eq '.sln' -or $_.Extension -eq '.slnx' } | Select-Object -First 1
    if ($solutionFile) {
        $Targets = @($solutionFile.FullName)
    } else {
        $Targets = @(Get-ChildItem -Path . -Recurse -Filter *.csproj | Select-Object -ExpandProperty FullName)
    }
}

if (-not $Targets -or $Targets.Count -eq 0) {
    throw 'No solution or project files found for format validation.'
}

foreach ($target in $Targets) {
    if ($RestoreTargets) {
        Invoke-DotNetFormatCommand -Arguments @('restore', $target)
    }

    Invoke-DotNetFormatCommand -Arguments @('format', 'whitespace', $target, '--verify-no-changes', '--no-restore')
    Invoke-DotNetFormatCommand -Arguments @('format', 'style', $target, '--verify-no-changes', '--no-restore', '--severity', 'warn')
    Invoke-DotNetFormatCommand -Arguments @('format', 'analyzers', $target, '--verify-no-changes', '--no-restore', '--severity', 'warn')
}
