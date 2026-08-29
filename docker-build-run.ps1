<#
.SYNOPSIS
    Builds the Docker image for eShopPorted and runs it in a container.
.DESCRIPTION
    Builds the image from the root Dockerfile and runs it, mapping the container
    port 8080 to a host port. Requires Docker Desktop (Linux containers).
#>
param(
    [string]$ImageName = "eshopported:latest",
    [int]$HostPort = 8080
)

$ErrorActionPreference = "Stop"

Write-Host "Building image '$ImageName'..." -ForegroundColor Cyan
docker build -t $ImageName -f (Join-Path $PSScriptRoot "Dockerfile") $PSScriptRoot

Write-Host "Running container on http://localhost:$HostPort ..." -ForegroundColor Cyan
docker run --rm -p "${HostPort}:8080" $ImageName
