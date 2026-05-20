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

function Get-TemplateParameters {
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

    if ($normalized.ContainsKey("no-tests") -and [bool]$normalized["no-tests"]) {
        if ($normalized.ContainsKey("tests")) {
            $normalized["tests"] = "tunit"
        }
    }

    $parts = @()
    foreach ($key in @($normalized.Keys | Sort-Object)) {
        $parts += "$key=$($normalized[$key])"
    }
    return ($parts -join ";")
}

function New-StandardVariant {
    param([hashtable]$Combo)

    $hasNoSln = $Combo.ContainsKey("no-sln")
    $hasSln = $Combo.ContainsKey("sln")
    $hasNoTests = $Combo.ContainsKey("no-tests")
    $hasTests = $Combo.ContainsKey("tests")

    $noSln = $hasNoSln -and [bool]$Combo["no-sln"]
    $sln = $hasSln -and [bool]$Combo["sln"] -and -not $noSln
    $noTests = $hasNoTests -and [bool]$Combo["no-tests"]
    $tests = if ($hasTests) { [string]$Combo["tests"] } else { "tunit" }
    if ([string]::IsNullOrWhiteSpace($tests)) {
        $tests = "tunit"
    }
    if ($tests -notin @("tunit", "xunit")) {
        throw "Unsupported test choice '$tests'. Update CI matrix generator handling."
    }

    $args = @()
    if ($noSln) { $args += "--no-sln" }
    elseif ($sln) { $args += "--sln" }

    if ($noTests) { $args += "--no-tests" }
    elseif ($tests -eq "xunit") { $args += "--tests xunit" }

    $variantParts = @()
    if ($sln) { $variantParts += "sln" }
    elseif ($noSln) { $variantParts += "no-sln" }

    if ($noTests) { $variantParts += "no-tests" }
    elseif ($tests -eq "xunit") { $variantParts += "xunit" }
    elseif (-not $sln -and -not $noSln) { $variantParts += "tunit" }

    $variantName = ($variantParts -join "-")
    if ([string]::IsNullOrWhiteSpace($variantName)) {
        $variantName = "default"
    }

    return [ordered]@{
        variant = $variantName
        args = ($args -join " ")
        expectSln = $sln
        expectSlnx = (-not $noSln -and -not $sln)
        expectTests = (-not $noTests)
        reportTrxArgs = if ($noTests) { "" } elseif ($tests -eq "xunit") { "--report-xunit-trx --report-xunit-trx-filename tests.trx" } else { "--report-trx --report-trx-filename tests.trx" }
    }
}

function New-AspireVariants {
    param([hashtable]$ParamDefinitions)

    $allowed = @("applicationInsights", "integrationTests")
    $unknown = @($ParamDefinitions.Keys | Where-Object { $_ -notin $allowed })
    if ($unknown.Count -gt 0) {
        throw "Unhandled Aspire parameters detected: $($unknown -join ', ')"
    }

    $domains = @{
        applicationInsights = @($false, $true)
        integrationTests = @($false, $true)
    }
    $combos = Get-CartesianProduct -Domains $domains
    $variants = @()
    foreach ($combo in $combos) {
        $args = @()
        if ([bool]$combo.applicationInsights) { $args += "--applicationInsights true" }
        if ([bool]$combo.integrationTests) { $args += "--integrationTests true" }

        $name = @()
        if ([bool]$combo.applicationInsights) { $name += "applicationInsights" } else { $name += "default" }
        if ([bool]$combo.integrationTests) { $name += "with-integration-tests" }
        $variantName = $name -join "-"

        $variants += @([ordered]@{
                variant = $variantName
                args = ($args -join " ")
            })
    }
    return @($variants | Sort-Object variant)
}

$schemaPath = Join-Path ([System.IO.Path]::GetTempPath()) "dotnet-template-schema.json"
Invoke-WebRequest -Uri $SchemaUrl -OutFile $schemaPath

$templateFiles = Get-ChildItem -Path $TemplateRoot -Recurse -Filter "template.json" | Where-Object { $_.FullName -match "[\\/]\.template\.config[\\/]template\.json$" }
if (-not $templateFiles) {
    throw "No template.json files found under '$TemplateRoot'."
}

$standardTemplates = @()
$aspireVariantMatrix = $null

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
    $paramDefs = Get-TemplateParameters -TemplateJson $template

    if ($shortName -eq "bmichaelis.aspire.minimalapi") {
        $aspireVariants = New-AspireVariants -ParamDefinitions $paramDefs
        $aspireVariantMatrix = @{ include = $aspireVariants }
        continue
    }

    $allowedStandard = @("no-sln", "sln", "no-tests", "tests")
    $unknownStandard = @($paramDefs.Keys | Where-Object { $_ -notin $allowedStandard })
    if ($unknownStandard.Count -gt 0) {
        throw "Unhandled standard template parameters for '$shortName': $($unknownStandard -join ', ')"
    }

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
        $variants += @(New-StandardVariant -Combo $combo)
    }
    $variants = @($variants | Sort-Object variant)
    if (-not $variants) {
        throw "No generated variants for '$shortName'"
    }

    $sourceName = [string]$template.sourceName
    if ([string]::IsNullOrWhiteSpace($sourceName)) {
        $sourceName = "Template"
    }

    $postBuildCommand = ""
    if ($shortName -eq "bmichaelis.quickstart.benchmarkconsole") {
        $postBuildCommand = "benchmark-run"
    }

    $packProject = $shortName -ne "bmichaelis.tool"
    $artifactSuffix = $shortName.Replace("bmichaelis.", "").Replace(".", "-")
    $jobName = $shortName.Replace("bmichaelis.", "")

    $standardTemplates += @([ordered]@{
            job_name = $jobName
            template_short_name = $shortName.Replace("bmichaelis.", "")
            project_name = $sourceName
            output_dir_prefix = "Test$sourceName"
            variant_matrix_json = (Get-CompactJson -Value @{ include = $variants })
            dotnet_version = "10.x"
            post_build_command = $postBuildCommand
            pack_project = $packProject
            failed_playlist_artifact_prefix = "failed-tests-playlist-$artifactSuffix"
        })
}

if ($null -eq $aspireVariantMatrix) {
    throw "Aspire template variants were not generated."
}

$standardTemplates = @($standardTemplates | Sort-Object job_name)
$standardJson = Get-CompactJson -Value $standardTemplates
$aspireJson = Get-CompactJson -Value $aspireVariantMatrix

if (-not $env:GITHUB_OUTPUT) {
    Write-Output "standard_templates=$standardJson"
    Write-Output "aspire_variants=$aspireJson"
    return
}

"standard_templates=$standardJson" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append
"aspire_variants=$aspireJson" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append
