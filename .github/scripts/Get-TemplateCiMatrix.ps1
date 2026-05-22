param(
    [string]$TemplateRoot = "templates",
    [string]$SchemaUrl = "https://json.schemastore.org/template.json"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-CompactJson {
    param([object]$Value)
    return ($Value | ConvertTo-Json -Depth 100 -Compress)
}

function Get-TemplateParameter {
    param([object]$TemplateJson)

    $result = @{}
    if ($null -eq $TemplateJson.symbols) {
        return $result
    }

    foreach ($symbolProp in $TemplateJson.symbols.PSObject.Properties) {
        $name = $symbolProp.Name
        $symbol = $symbolProp.Value
        if ($null -eq $symbol -or $symbol.type -ne "parameter") {
            continue
        }

        $dataType = $symbol.datatype
        if ([string]::IsNullOrWhiteSpace($dataType)) {
            $dataType = $symbol.dataType
        }
        if ([string]::IsNullOrWhiteSpace($dataType)) {
            continue
        }

        switch ($dataType.ToLowerInvariant()) {
            "bool" {
                $result[$name] = @{
                    type = "bool"
                    values = @($false, $true)
                }
            }
            "choice" {
                $choices = @()
                if ($null -ne $symbol.choices) {
                    foreach ($choice in $symbol.choices) {
                        if ($null -ne $choice.choice) {
                            $choices += @([string]$choice.choice)
                        }
                    }
                }
                if (-not $choices) {
                    throw "Parameter '$name' is choice but no choices were found."
                }
                $result[$name] = @{
                    type = "choice"
                    values = $choices
                }
            }
        }
    }

    return $result
}

function Get-CartesianProduct {
    param([hashtable]$Domains)

    $keys = @($Domains.Keys | Sort-Object)
    $combinations = @(@{})

    foreach ($key in $keys) {
        $next = @()
        foreach ($combo in $combinations) {
            foreach ($value in $Domains[$key]) {
                $copy = @{}
                foreach ($entry in $combo.GetEnumerator()) {
                    $copy[$entry.Key] = $entry.Value
                }
                $copy[$key] = $value
                $next += @($copy)
            }
        }
        $combinations = $next
    }

    return $combinations
}

function New-NormalizedStandardCombinationKey {
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '')]
    [CmdletBinding()]
    param([hashtable]$Combo)

    $normalized = @{}
    foreach ($entry in $Combo.GetEnumerator()) {
        $normalized[$entry.Key] = $entry.Value
    }

    if ($normalized.ContainsKey("no-sln") -and [bool]$normalized["no-sln"]) {
        if ($normalized.ContainsKey("sln")) {
            $normalized["sln"] = $false
        }
    }

    if ($normalized.ContainsKey("tests") -and ($normalized["tests"] -eq "None")) {
        # Do not process tests further when None is selected
    }

    $parts = @()
    foreach ($key in @($normalized.Keys | Sort-Object)) {
        $parts += "$key=$($normalized[$key])"
    }
    return ($parts -join ";")
}

function New-StandardVariant {
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '')]
    [CmdletBinding()]
    param(
        [hashtable]$Combo,
        [hashtable]$Capabilities
    )

    $hasNoSln = $Combo.ContainsKey("no-sln")
    $hasSln = $Combo.ContainsKey("sln")
    $hasTests = $Combo.ContainsKey("tests")

    $noSln = $hasNoSln -and [bool]$Combo["no-sln"]
    $sln = $hasSln -and [bool]$Combo["sln"] -and -not $noSln
    $tests = if ($hasTests) { [string]$Combo["tests"] } else { "tunit" }
    if ([string]::IsNullOrWhiteSpace($tests)) {
        $tests = "tunit"
    }

    $templateArgs = @()
    if ($noSln) { $templateArgs += "--no-sln" }
    elseif ($sln) { $templateArgs += "--sln" }

    if ($tests -ne "tunit") { $templateArgs += "--tests $tests" }

    $variantParts = @()
    if ($sln) { $variantParts += "sln" }
    elseif ($noSln) { $variantParts += "no-sln" }

    if ($tests -eq "None") { $variantParts += "no-tests" }
    elseif ($tests -ne "tunit") { $variantParts += $tests }
    elseif (-not $sln -and -not $noSln) { $variantParts += "tunit" }

    # Handle arbitrary bool parameters (like Aspire's applicationInsights, integrationTests)
    $knownParams = @("no-sln", "sln", "tests")
    foreach ($key in ($Combo.Keys | Sort-Object)) {
        if ($key -notin $knownParams) {
            $val = [bool]$Combo[$key]
            if ($val) {
                $templateArgs += "--$key true"
                $variantParts += $key
            }
        }
    }

    $variantName = ($variantParts -join "-")
    if ([string]::IsNullOrWhiteSpace($variantName)) {
        $variantName = "default"
    }

    # For Aspire templates with integrationTests parameter, only expect tests when integrationTests=true
    $hasIntegrationTestsParam = $Combo.ContainsKey("integrationTests")
    $integrationTests = if ($hasIntegrationTestsParam) { [bool]$Combo["integrationTests"] } else { $false }
    $shouldExpectTests = ($tests -ne "None") -and (-not $hasIntegrationTestsParam -or $integrationTests)

    $result = [ordered]@{
        variant = $variantName
        args = ($templateArgs -join " ")
        expectSln = $sln
        expectSlnx = (-not $noSln -and -not $sln)
        expectTests = $shouldExpectTests
        reportTrxArgs = if ($tests -eq "None") { "" } elseif ($tests -eq "xunit") { "--report-xunit-trx --report-xunit-trx-filename tests.trx" } else { "--report-trx --report-trx-filename tests.trx" }
    }

    # Add capability flags
    if ($Capabilities) {
        $result.hasAspireHost = $Capabilities.hasAspireHost
        $result.hasEfMigrations = $Capabilities.hasEfMigrations
        $result.hasDockerfile = $Capabilities.hasDockerfile
    }

    return $result
}

function Get-TemplateCapabilities {
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '')]
    [CmdletBinding()]
    param([string]$TemplateDir)

    $capabilities = [ordered]@{
        hasAspireHost = $false
        hasEfMigrations = $false
        hasDockerfile = $false
    }

    $csprojFiles = @(Get-ChildItem -Path $TemplateDir -Recurse -Filter "*.csproj" -ErrorAction SilentlyContinue)
    foreach ($csproj in $csprojFiles) {
        $content = Get-Content -Path $csproj.FullName -Raw
        # Detect Aspire.AppHost.Sdk (with or without version suffix)
        if ($content -match 'Sdk\s*=\s*"Aspire\.AppHost\.Sdk(?:/[0-9.]+)?"' -or $content -match '<PackageReference\s+Include\s*=\s*"Aspire\.Hosting') {
            $capabilities.hasAspireHost = $true
        }
        # Detect EntityFrameworkCore.Design independently of Aspire
        if ($content -match '<PackageReference\s+Include\s*=\s*"Microsoft\.EntityFrameworkCore\.Design') {
            $capabilities.hasEfMigrations = $true
        }
    }

    $dockerfile = Get-ChildItem -Path $TemplateDir -Recurse -Filter "Dockerfile" -ErrorAction SilentlyContinue
    if ($dockerfile) {
        $capabilities.hasDockerfile = $true
    }

    return $capabilities
}

$schemaPath = Join-Path ([System.IO.Path]::GetTempPath()) "dotnet-template-schema.json"
Invoke-WebRequest -Uri $SchemaUrl -OutFile $schemaPath

$templateFiles = Get-ChildItem -Path $TemplateRoot -Recurse -Filter "template.json" -Force | Where-Object { $_.FullName -match "[\\/]\.template\.config[\\/]template\.json$" }
if (-not $templateFiles) {
    throw "No template.json files found under '$TemplateRoot'."
}

$standardTemplates = @()
$capabilityFlags = @{
    hasAspireHost = $false
    hasEfMigrations = $false
    hasDockerfile = $false
}

foreach ($file in $templateFiles) {
    $jsonText = Get-Content -Path $file.FullName -Raw
    $isValid = $jsonText | Test-Json -SchemaFile $schemaPath
    if (-not $isValid) {
        throw "Template schema validation failed: $($file.FullName)"
    }

    $template = $jsonText | ConvertFrom-Json -Depth 100
    if ([string]::IsNullOrWhiteSpace($template.shortName)) {
        throw "Template missing shortName: $($file.FullName)"
    }

    $shortName = [string]$template.shortName
    $paramDefs = Get-TemplateParameter -TemplateJson $template
    $templateDir = Split-Path -Parent $file.FullName | Split-Path -Parent

    # Detect template capabilities
    $capabilities = Get-TemplateCapabilities -TemplateDir $templateDir
    foreach ($flag in $capabilities.Keys) {
        if ($capabilities[$flag]) {
            $capabilityFlags[$flag] = $true
        }
    }

    # Allow arbitrary bool parameters (no longer restrict to standard set)
    $domains = @{}
    foreach ($entry in $paramDefs.GetEnumerator()) {
        $domains[$entry.Key] = $entry.Value.values
    }

    if ($domains.Count -eq 0) {
        $domains = @{ "__default" = @("default") }
    }

    $rawCombos = Get-CartesianProduct -Domains $domains
    $deduped = @{}
    foreach ($combo in $rawCombos) {
        if ($combo.ContainsKey("__default")) {
            $combo.Remove("__default")
        }
        $key = New-NormalizedStandardCombinationKey -Combo $combo
        if (-not $deduped.ContainsKey($key)) {
            $deduped[$key] = $combo
        }
    }

    $variants = @()
    foreach ($combo in $deduped.Values) {
        $variants += @(New-StandardVariant -Combo $combo -Capabilities $capabilities)
    }
    $variants = @($variants | Sort-Object variant)
    if (-not $variants) {
        throw "No generated variants for '$shortName'"
    }

    $sourceName = [string]$template.sourceName
    if ([string]::IsNullOrWhiteSpace($sourceName)) {
        $sourceName = "Template"
    }

    $projectName = $sourceName
    $outputDirPrefix = "Test$sourceName"
    $artifactSuffix = $shortName.Replace("bmichaelis.", "").Replace(".", "-")

    $postBuildCommand = ""
    if ($shortName -eq "bmichaelis.quickstart.benchmarkconsole") {
        $postBuildCommand = "benchmark-run"
    }

    $packProject = $shortName -in @("bmichaelis.nuget", "bmichaelis.quickstart.consoleapp", "bmichaelis.quickstart.benchmarkconsole")
    $jobName = $shortName.Replace("bmichaelis.", "")

    $standardTemplates += @([ordered]@{
            job_name = $jobName
            template_short_name = $shortName.Replace("bmichaelis.", "")
            project_name = $projectName
            output_dir_prefix = $outputDirPrefix
            variant_matrix_json = (Get-CompactJson -Value @{ include = $variants })
            post_build_command = $postBuildCommand
            pack_project = $packProject
            failed_playlist_artifact_prefix = "failed-tests-playlist-$artifactSuffix"
        })
}

# Log detected capabilities (for debugging)
$detectedCaps = @()
if ($capabilityFlags.hasAspireHost) { $detectedCaps += "Aspire" }
if ($capabilityFlags.hasEfMigrations) { $detectedCaps += "EF" }
if ($capabilityFlags.hasDockerfile) { $detectedCaps += "Docker" }

if ($detectedCaps.Count -gt 0) {
    Write-Host "Detected capabilities across templates: $($detectedCaps -join ', ')"
} else {
    Write-Host "No special capabilities detected in this template set."
}

$standardTemplates = @($standardTemplates | Sort-Object job_name)
$standardJson = Get-CompactJson -Value $standardTemplates

if (-not $env:GITHUB_OUTPUT) {
    Write-Output "standard_templates=$standardJson"
    return
}

"standard_templates=$standardJson" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append
