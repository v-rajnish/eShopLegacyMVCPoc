<#
.SYNOPSIS
    Builds and runs the modernized eShopPorted (.NET 8) application locally.
.DESCRIPTION
    Restores, builds, and runs the ASP.NET Core app. Works on Windows and Linux
    (PowerShell 7+). Uses mock data by default (see eShopPorted/appsettings.json).
#>
param(
    [string]$Configuration = "Release",
    [string]$Urls = "http://localhost:5080"
)

$ErrorActionPreference = "Stop"
$project = Join-Path $PSScriptRoot "eShopPorted/eShopPorted.csproj"

Write-Host "Restoring & building ($Configuration)..." -ForegroundColor Cyan
dotnet build $project -c $Configuration

Write-Host "Starting app on $Urls ..." -ForegroundColor Cyan
$env:ASPNETCORE_URLS = $Urls
dotnet run --project $project -c $Configuration --no-launch-profile
