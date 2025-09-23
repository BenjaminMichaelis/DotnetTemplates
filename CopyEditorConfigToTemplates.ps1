#!/usr/bin/env pwsh

# Script to copy the root .editorconfig to all template directories
# This should be run when the root .editorconfig is updated to ensure all templates get the latest settings

$rootEditorConfig = "./.editorconfig"
$templateDirs = Get-ChildItem -Path "./templates" -Recurse -Directory | Where-Object { $_.Name -match "^[A-Z]" -and (Test-Path (Join-Path $_.FullName ".template.config")) }

if (-not (Test-Path $rootEditorConfig)) {
    Write-Error "Root .editorconfig not found at $rootEditorConfig"
    exit 1
}

Write-Host "Found template directories:"
$templateDirs | ForEach-Object { Write-Host "  - $($_.FullName)" }
Write-Host ""

foreach ($templateDir in $templateDirs) {
    $targetPath = Join-Path $templateDir.FullName ".editorconfig"
    $hasCustomizations = $false
    
    # Check if template already has customizations
    if (Test-Path $targetPath) {
        $existingContent = Get-Content $targetPath -Raw
        $rootContent = Get-Content $rootEditorConfig -Raw
        
        if ($existingContent -ne $rootContent) {
            $hasCustomizations = $true
            Write-Warning "Template $($templateDir.Name) has customizations in its .editorconfig"
            Write-Host "  You may want to manually review and merge changes."
            Write-Host "  Backup created at: $targetPath.backup"
            Copy-Item $targetPath "$targetPath.backup" -Force
        }
    }
    
    if (-not $hasCustomizations) {
        Copy-Item $rootEditorConfig $targetPath -Force
        Write-Host "✓ Copied .editorconfig to $($templateDir.Name)"
    } else {
        Write-Host "⚠ Skipped $($templateDir.Name) due to customizations (backup created)"
    }
}

Write-Host ""
Write-Host "✓ Script completed. Remember to test the templates after updating .editorconfig files."