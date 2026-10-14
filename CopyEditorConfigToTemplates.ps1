#!/usr/bin/env pwsh

# Script to copy the root .editorconfig to all template directories
# This should be run when the root .editorconfig or template override files are updated.
# Per-template overrides live in ".template.config/editorconfig.override".

$rootEditorConfig = "./.editorconfig"
$templatesRoot = (Resolve-Path "./templates").Path
$templateDirs = Get-ChildItem -Path "./templates" -Recurse -Directory | Where-Object { $_.Name -match "^[A-Z]" -and (Test-Path (Join-Path $_.FullName ".template.config")) }

if (-not (Test-Path $rootEditorConfig)) {
    Write-Error "Root .editorconfig not found at $rootEditorConfig"
    exit 1
}

$rootContent = Get-Content $rootEditorConfig -Raw
$lineEnding = if ($rootContent -match "`r`n") { "`r`n" } else { "`n" }

function Convert-ToNormalizedText {
    param (
        [string]$Content,
        [string]$TargetLineEnding
    )

    if ($null -eq $Content) {
        return ""
    }

    $normalized = ($Content -replace "`r`n", "`n" -replace "`r", "`n").TrimEnd("`n")
    if ([string]::IsNullOrEmpty($normalized)) {
        return ""
    }

    return ($normalized -replace "`n", $TargetLineEnding)
}

function Build-EditorConfigContent {
    param (
        [string]$BaseContent,
        [string]$OverrideContent,
        [string]$TargetLineEnding,
        [string]$OverridePath
    )

    $baseNormalized = Convert-ToNormalizedText -Content $BaseContent -TargetLineEnding $TargetLineEnding
    if ([string]::IsNullOrWhiteSpace($OverrideContent)) {
        return "$baseNormalized$TargetLineEnding"
    }

    $overrideNormalized = Convert-ToNormalizedText -Content $OverrideContent -TargetLineEnding $TargetLineEnding
    if ($overrideNormalized -match "(?m)^\s*root\s*=") {
        throw "Override file '$OverridePath' contains a disallowed root setting. Remove 'root = ...' from the override file."
    }

    return "$baseNormalized$TargetLineEnding$TargetLineEnding# Template-specific overrides$TargetLineEnding$overrideNormalized$TargetLineEnding"
}

$changedTemplates = New-Object System.Collections.Generic.List[string]
$templatesWithOverrides = New-Object System.Collections.Generic.List[string]

Write-Output "Found template directories:"
$templateDirs | ForEach-Object { Write-Output "  - $($_.FullName)" }
Write-Output ""

foreach ($templateDir in $templateDirs) {
    $targetPath = Join-Path $templateDir.FullName ".editorconfig"
    $overridePath = Join-Path $templateDir.FullName ".template.config/editorconfig.override"
    $relativeTemplatePath = [System.IO.Path]::GetRelativePath($templatesRoot, $templateDir.FullName).Replace("\", "/")

    $overrideContent = ""
    if (Test-Path $overridePath) {
        $overrideContent = Get-Content $overridePath -Raw
        if (-not [string]::IsNullOrWhiteSpace($overrideContent)) {
            $templatesWithOverrides.Add($relativeTemplatePath)
        }
    }

    $finalContent = Build-EditorConfigContent -BaseContent $rootContent -OverrideContent $overrideContent -TargetLineEnding $lineEnding -OverridePath $overridePath
    $existingContent = if (Test-Path $targetPath) { Get-Content $targetPath -Raw } else { "" }

    if ($existingContent -ne $finalContent) {
        Set-Content -Path $targetPath -Value $finalContent -NoNewline
        $changedTemplates.Add($relativeTemplatePath)
        Write-Output "✓ Updated .editorconfig for $relativeTemplatePath"
    } else {
        Write-Output "✓ No changes needed for $relativeTemplatePath"
    }
}

$changedTemplatesValue = (($changedTemplates | Sort-Object -Unique) -join ", ")
$templatesWithOverridesValue = (($templatesWithOverrides | Sort-Object -Unique) -join ", ")
$hasChanges = if ($changedTemplates.Count -gt 0) { "true" } else { "false" }

if ($env:GITHUB_OUTPUT) {
    "has_changes=$hasChanges" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append
    "changed_templates=$changedTemplatesValue" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append
    "templates_with_overrides=$templatesWithOverridesValue" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append
}

Write-Output ""
if ($changedTemplates.Count -gt 0) {
    Write-Output "✓ Script completed with updates: $changedTemplatesValue"
} else {
    Write-Output "✓ Script completed. All template .editorconfig files are up to date."
}
