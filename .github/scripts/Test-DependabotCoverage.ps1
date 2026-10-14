$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$dependabotPath = Join-Path $repoRoot ".github\dependabot.yml"
$dependabotContent = Get-Content -Path $dependabotPath -Raw
$coverage = @{}

function ConvertTo-DependabotDirectory([string]$Directory) {
  $normalized = $Directory.Trim().Trim('"').Trim("'").Replace("\", "/").TrimEnd("/")
  if ($normalized -eq "") {
    return "/"
  }

  return "/$($normalized.TrimStart('/'))"
}

$entryPattern = '(?ms)^  - package-ecosystem:[ \t]*["'']?([^"''\r\n]+)["'']?[ \t]*\r?\n(.*?)(?=^  - package-ecosystem:|\z)'
foreach ($entry in [regex]::Matches($dependabotContent, $entryPattern)) {
  $ecosystem = $entry.Groups[1].Value.Trim()
  if (-not $coverage.ContainsKey($ecosystem)) {
    $coverage[$ecosystem] = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
  }

  $block = $entry.Groups[2].Value
  $singleDirectory = [regex]::Match($block, '(?m)^    directory:\s*["'']?([^"''#\r\n]+)')
  if ($singleDirectory.Success) {
    [void]$coverage[$ecosystem].Add((ConvertTo-DependabotDirectory $singleDirectory.Groups[1].Value))
  }

  $inDirectories = $false
  foreach ($line in ($block -split '\r?\n')) {
    if ($line -match '^    directories:\s*$') {
      $inDirectories = $true
      continue
    }

    if ($inDirectories -and $line -match '^      -\s*["'']([^"''\r\n]+)["'']\s*$') {
      [void]$coverage[$ecosystem].Add((ConvertTo-DependabotDirectory $Matches[1]))
      continue
    }

    if ($inDirectories -and $line -match '^\S') {
      $inDirectories = $false
    }
  }
}

function Get-RelativeDependabotDirectory([string]$Path) {
  $relativePath = [System.IO.Path]::GetRelativePath($repoRoot, $Path).Replace("\", "/")
  if ($relativePath -eq ".") {
    return "/"
  }

  return "/$relativePath"
}

function Add-ExpectedDirectory([System.Collections.Generic.HashSet[string]]$Expected, [string]$Path) {
  [void]$Expected.Add((Get-RelativeDependabotDirectory $Path))
}

function Assert-DependabotCoverage([string]$Ecosystem, [System.Collections.Generic.HashSet[string]]$Expected) {
  $configured = $coverage[$Ecosystem]
  $missing = @($Expected | Where-Object { -not $configured -or -not $configured.Contains($_) } | Sort-Object)
  if ($missing.Count -gt 0) {
    throw "Dependabot '$Ecosystem' updates are missing directories:`n  $($missing -join "`n  ")"
  }

  Write-Output "Dependabot '$Ecosystem' covers $($Expected.Count) dependency location(s)."
}

$nugetDirectories = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$projectFiles = Get-ChildItem -Path $repoRoot -Recurse -File -Filter "*.csproj" |
  Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }
foreach ($project in $projectFiles) {
  $directory = $project.DirectoryName
  while ($directory -and $directory.StartsWith($repoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    if (Test-Path (Join-Path $directory "Directory.Packages.props")) {
      break
    }

    if ($directory -eq $repoRoot) {
      break
    }

    $directory = Split-Path -Parent $directory
  }

  Add-ExpectedDirectory $nugetDirectories $directory
}

Get-ChildItem -Path $repoRoot -Recurse -File -Filter "Directory.Packages.props" |
  Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } |
  ForEach-Object { Add-ExpectedDirectory $nugetDirectories $_.DirectoryName }

Get-ChildItem -Path $repoRoot -Recurse -File -Filter "dotnet-tools.json" |
  Where-Object { $_.FullName -match '[\\/]\.config[\\/]' -and $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } |
  ForEach-Object { Add-ExpectedDirectory $nugetDirectories $_.Directory.Parent.FullName }

$sdkDirectories = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
Get-ChildItem -Path $repoRoot -Recurse -File -Filter "global.json" |
  Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } |
  ForEach-Object { Add-ExpectedDirectory $sdkDirectories $_.DirectoryName }

$dockerDirectories = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
Get-ChildItem -Path $repoRoot -Recurse -File -Filter "Dockerfile" |
  Where-Object { $_.FullName -notmatch '[\\/](bin|obj|\.git)[\\/]' } |
  ForEach-Object { Add-ExpectedDirectory $dockerDirectories $_.DirectoryName }

$terraformDirectories = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$terraformMainFiles = Get-ChildItem -Path $repoRoot -Recurse -File -Filter "main.tf" |
  Where-Object { $_.FullName -notmatch '[\\/](bin|obj|\.git)[\\/]' }
foreach ($mainFile in $terraformMainFiles) {
  $directory = $mainFile.DirectoryName
  $parent = Split-Path -Parent $directory
  $coveredByParentRoot = $false
  while ($parent -and $parent.StartsWith($repoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    if (Test-Path (Join-Path $parent "main.tf")) {
      $coveredByParentRoot = $true
      break
    }

    if ($parent -eq $repoRoot) {
      break
    }

    $parent = Split-Path -Parent $parent
  }

  if (-not $coveredByParentRoot) {
    Add-ExpectedDirectory $terraformDirectories $directory
  }
}

$workflowDirectories = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
if (Get-ChildItem -Path (Join-Path $repoRoot ".github\workflows") -File |
  Where-Object { $_.Extension -in @(".yml", ".yaml") }) {
  Add-ExpectedDirectory $workflowDirectories $repoRoot
}

Get-ChildItem -Path (Join-Path $repoRoot "templates") -Recurse -File |
  Where-Object { $_.DirectoryName -match '[\\/]\.github[\\/]workflows$' -and $_.Extension -in @(".yml", ".yaml") } |
  ForEach-Object { Add-ExpectedDirectory $workflowDirectories $_.DirectoryName }

Assert-DependabotCoverage "nuget" $nugetDirectories
Assert-DependabotCoverage "dotnet-sdk" $sdkDirectories
Assert-DependabotCoverage "docker" $dockerDirectories
Assert-DependabotCoverage "terraform" $terraformDirectories
Assert-DependabotCoverage "github-actions" $workflowDirectories
