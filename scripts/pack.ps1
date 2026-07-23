#!/usr/bin/env pwsh
# Builds a Velopack install package (Setup.exe + update package) for Markdown Studio.
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File scripts/pack.ps1                 # version read from AppVersion.cs
#   powershell -ExecutionPolicy Bypass -File scripts/pack.ps1 -Version 1.1.0  # override version
#
# Requires: .NET SDK, and the vpk tool (dotnet tool install -g vpk --version 1.2.0).
param(
    [string]$Version,
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$repo        = Split-Path -Parent $PSScriptRoot
$proj        = Join-Path $repo "src/MarkdownStudio/MarkdownStudio.csproj"
$publishDir  = Join-Path $repo "publish"
$releasesDir = Join-Path $repo "Releases"
$icon        = Join-Path $repo "src/MarkdownStudio/Assets/appicon.ico"

# Version comes from AppVersion.cs (single source of truth) unless passed explicitly.
if (-not $Version) {
    $appVer = Get-Content (Join-Path $repo "src/MarkdownStudio/AppVersion.cs") -Raw
    if ($appVer -match 'Current\s*=\s*"([^"]+)"') { $Version = $Matches[1] }
    else { throw "Could not extract version from AppVersion.cs" }
}

Write-Host "== Markdown Studio - Velopack pack ==" -ForegroundColor Cyan
Write-Host "Version : $Version"
Write-Host "Runtime : $Runtime`n"

# 1) Self-contained publish (no single-file - Velopack packs the output folder).
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
dotnet publish $proj -c Release -r $Runtime --self-contained true -o $publishDir /p:PublishSingleFile=false
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

# 2) Velopack pack -> Setup.exe + nupkg + RELEASES manifest.
#    For distribution to machines without WebView2, add: --framework webview2
vpk pack `
    --packId       MarkdownStudio `
    --packVersion  $Version `
    --packDir      $publishDir `
    --mainExe      MarkdownStudio.exe `
    --packTitle    "Markdown Studio" `
    --packAuthors  "ali7med" `
    --icon         $icon `
    --outputDir    $releasesDir
if ($LASTEXITCODE -ne 0) { throw "vpk pack failed" }

Write-Host "`nDone. Output in: $releasesDir" -ForegroundColor Green
Get-ChildItem $releasesDir | Select-Object Name, @{ n = "Size(MB)"; e = { [math]::Round($_.Length / 1MB, 2) } }
