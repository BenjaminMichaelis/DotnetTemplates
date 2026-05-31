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
                    defaultValue = $false
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
                $defaultValue = if (-not [string]::IsNullOrWhiteSpace($symbol.defaultValue)) {
                    [string]$symbol.defaultValue
                } else {
                    $choices[0]
                }
                $result[$name] = @{
                    type = "choice"
                    values = $choices
                    defaultValue = $defaultValue
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
        [hashtable]$ParamDefs
    )

    $templateArgs = @()
    $variantParts = @()
    $tests = ""

    foreach ($key in ($Combo.Keys | Sort-Object)) {
        $def = $ParamDefs[$key]
        $value = $Combo[$key]

        if ($def.type -eq "bool") {
            if ([bool]$value) {
                $templateArgs += "--$key"
                $variantParts += $key
            }
            # false is the default — no arg needed
        } elseif ($def.type -eq "choice") {
            $strVal = [string]$value
            $defaultChoice = [string]$def.defaultValue
            if ($strVal -ne $defaultChoice) {
                $templateArgs += "--$key $strVal"
                if ($key -eq "tests" -and $strVal -eq "None") {
                    $variantParts += "no-tests"
                } else {
                    $variantParts += $strVal
                }
            }
            if ($key -eq "tests") {
                $tests = $strVal
            }
        }
    }

    $variantName = ($variantParts -join "-")
    if ([string]::IsNullOrWhiteSpace($variantName)) {
        $variantName = "default"
    }

    $reportTrxArgs = if ($tests -eq "None") {
        ""
    } elseif ($tests -eq "xunit") {
        "--report-xunit-trx --report-xunit-trx-filename tests.trx"
    } else {
        "--report-trx --report-trx-filename tests.trx"
    }

    return [ordered]@{
        variant       = $variantName
        args          = ($templateArgs -join " ")
        reportTrxArgs = $reportTrxArgs
    }
}

function Test-TruthyMsBuildValue {
    param([object]$Value)

    if ($null -eq $Value) {
        return $false
    }

    return ([string]$Value).Trim().Equals("true", [System.StringComparison]::OrdinalIgnoreCase)
}

function Test-FalsyMsBuildValue {
    param([object]$Value)

    if ($null -eq $Value) {
        return $false
    }

    return ([string]$Value).Trim().Equals("false", [System.StringComparison]::OrdinalIgnoreCase)
}

function Get-MsBuildPropertyValues {
    param(
        [xml]$ProjectXml,
        [string]$PropertyName
    )

    $values = @()
    foreach ($propertyGroup in @($ProjectXml.Project.PropertyGroup)) {
        if ($null -eq $propertyGroup) {
            continue
        }

        foreach ($property in @($propertyGroup.SelectNodes($PropertyName))) {
            if ($null -ne $property) {
                $values += @([string]$property.InnerText)
            }
        }
    }

    return $values
}

function Test-TemplatePrimaryProjectPackable {
    param(
        [string]$TemplateDir,
        [string]$SourceName
    )

    if ([string]::IsNullOrWhiteSpace($SourceName)) {
        $SourceName = "Template"
    }

    $primaryProject = Join-Path (Join-Path $TemplateDir $SourceName) "$SourceName.csproj"
    if (-not (Test-Path -Path $primaryProject -PathType Leaf)) {
        return $false
    }

    [xml]$projectXml = Get-Content -Path $primaryProject -Raw
    $sdk = [string]$projectXml.Project.GetAttribute("Sdk")

    $isPackableValues = @(Get-MsBuildPropertyValues -ProjectXml $projectXml -PropertyName "IsPackable")
    if ($isPackableValues | Where-Object { Test-FalsyMsBuildValue -Value $_ }) {
        return $false
    }

    $packAsToolValues = @(Get-MsBuildPropertyValues -ProjectXml $projectXml -PropertyName "PackAsTool")
    if ($packAsToolValues | Where-Object { Test-TruthyMsBuildValue -Value $_ }) {
        $runtimeIdentifierValues = @(
            Get-MsBuildPropertyValues -ProjectXml $projectXml -PropertyName "RuntimeIdentifier"
            Get-MsBuildPropertyValues -ProjectXml $projectXml -PropertyName "RuntimeIdentifiers"
            Get-MsBuildPropertyValues -ProjectXml $projectXml -PropertyName "ToolPackageRuntimeIdentifiers"
        )

        if ($runtimeIdentifierValues | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }) {
            return $false
        }

        return $true
    }

    if ($isPackableValues | Where-Object { Test-TruthyMsBuildValue -Value $_ }) {
        return $true
    }

    if ($sdk -match '(^|;)Aspire\.AppHost\.Sdk($|/|;)' -or $sdk -match '(^|;)Microsoft\.NET\.Sdk\.Web($|/|;)') {
        return $false
    }

    $outputTypeValues = @(Get-MsBuildPropertyValues -ProjectXml $projectXml -PropertyName "OutputType")
    if ($outputTypeValues | Where-Object { $_.Trim() -in @("Exe", "WinExe") }) {
        return $false
    }

    return ($sdk -match '(^|;)Microsoft\.NET\.Sdk($|/|;)')
}

$schemaPath = Join-Path ([System.IO.Path]::GetTempPath()) "dotnet-template-schema.json"
Invoke-WebRequest -Uri $SchemaUrl -OutFile $schemaPath

$templateFiles = Get-ChildItem -Path $TemplateRoot -Recurse -Filter "template.json" -Force | Where-Object { $_.FullName -match "[\\/]\.template\.config[\\/]template\.json$" }
if (-not $templateFiles) {
    throw "No template.json files found under '$TemplateRoot'."
}

$standardTemplates = @()

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
        $variants += @(New-StandardVariant -Combo $combo -ParamDefs $paramDefs)
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

    $packProject = Test-TemplatePrimaryProjectPackable -TemplateDir $templateDir -SourceName $sourceName
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

$standardTemplates = @($standardTemplates | Sort-Object job_name)
$standardJson = Get-CompactJson -Value $standardTemplates

if (-not $env:GITHUB_OUTPUT) {
    Write-Output "standard_templates=$standardJson"
    return
}

"standard_templates=$standardJson" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append
