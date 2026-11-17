
# Builds AutoAqua and creates dist\AutoAqua-<version>.zip, laid out so it can be
# extracted straight into the game folder:
#
#   Mods\AutoAqua.dll
#   AutoAqua-README.md
#   LICENSE
#
# Usage:  .\package.ps1 -Version 1.0.0
param([string]$Version = "1.0.0")

$ErrorActionPreference = "Stop"
Push-Location $PSScriptRoot
try {
    dotnet build -c Release | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "Build failed" }

    $dll = Join-Path $PSScriptRoot "bin\Release\net472\AutoAqua.dll"
    if (-not (Test-Path $dll)) { throw "Build output not found: $dll" }

    $stage = Join-Path $PSScriptRoot "dist\stage"
    if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
    New-Item -ItemType Directory -Force (Join-Path $stage "Mods") | Out-Null

    Copy-Item $dll (Join-Path $stage "Mods")
    Copy-Item (Join-Path $PSScriptRoot "README.md") (Join-Path $stage "AutoAqua-README.md")
    Copy-Item (Join-Path $PSScriptRoot "LICENSE") $stage

    $zip = Join-Path $PSScriptRoot "dist\AutoAqua-$Version.zip"
    if (Test-Path $zip) { Remove-Item $zip }

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory($stage, $zip)

    Write-Host ""
    Write-Host "Created $zip"
    Write-Host "Contents:"
    $archive = [System.IO.Compression.ZipFile]::OpenRead($zip)
    try { $archive.Entries | ForEach-Object { Write-Host "  $($_.FullName)" } }
    finally { $archive.Dispose() }
}
finally {
    Pop-Location
}
