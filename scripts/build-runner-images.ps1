# build-runner-images.ps1
# PowerShell equivalent of build-runner-images.sh for Windows (no WSL/bash needed)
# Usage: .\scripts\build-runner-images.ps1
param(
    [string]$Tag = "latest",
    [string]$Registry = ""
)

$ErrorActionPreference = "Stop"
$ImagesDir = Resolve-Path (Join-Path $PSScriptRoot "..\infra\runner-images")

Write-Host "Building CodeArena runner sandbox images..." -ForegroundColor Yellow
Write-Host "Images dir: $ImagesDir`n"

$images = @(
    @{ Name = "python"; File = "Dockerfile.python" },
    @{ Name = "node"; File = "Dockerfile.node" },
    @{ Name = "c-cpp"; File = "Dockerfile.c_cpp" },
    @{ Name = "csharp"; File = "Dockerfile.csharp" }
)

foreach ($img in $images) {
    $fullName = "codearena-runner-$($img.Name):$Tag"
    $dockerfile = Join-Path $ImagesDir $img.File

    Write-Host "-> Building $fullName" -ForegroundColor Cyan
    docker build --no-cache -t $fullName -f $dockerfile "$ImagesDir"

    if ($LASTEXITCODE -ne 0) {
        Write-Error "docker build failed for $fullName"
        exit 1
    }

    if ($Registry -ne "") {
        docker tag $fullName "$Registry/$fullName"
        docker push "$Registry/$fullName"
    }

    Write-Host "OK $fullName`n" -ForegroundColor Green
}

Write-Host "All runner images built successfully." -ForegroundColor Green
