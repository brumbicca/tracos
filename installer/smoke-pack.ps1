#Requires -Version 5.1
# Empacota o smoke test para máquina limpa (instalador + last-build + fixture + checklist).
# Pré-requisito: installer\publish.ps1 já executado (dist\Tracos3DStudio-setup.exe + last-build.txt).
$ErrorActionPreference = "Stop"

$root    = Split-Path -Parent $PSScriptRoot
$distDir = Join-Path $root "dist"
$stamp   = Join-Path $distDir "last-build.txt"
$setup   = Join-Path $distDir "Tracos3DStudio-setup.exe"
$fixture = Join-Path $root "fase-2-cozinha-L.tracos"
$checklist = Join-Path $root "docs\manual\escala\02-smoke-instalador-maquina-limpa.md"

foreach ($path in @($stamp, $setup, $fixture, $checklist)) {
    if (-not (Test-Path $path)) {
        Write-Error "Arquivo ausente: $path — execute dotnet test e installer\publish.ps1 antes."
    }
}

$versionLine = Get-Content $stamp | Where-Object { $_ -match "^Version=" } | Select-Object -First 1
$version = ($versionLine -replace "^Version=", "").Trim()
$zipName = "Tracos3DStudio-smoke-pack-$version.zip"
$zipPath = Join-Path $distDir $zipName

$staging = Join-Path $env:TEMP "tracos-smoke-pack-$version"
if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
New-Item -ItemType Directory -Path $staging | Out-Null

Copy-Item $setup $staging
Copy-Item $stamp $staging
Copy-Item $fixture $staging
Copy-Item $checklist $staging

if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path (Join-Path $staging "*") -DestinationPath $zipPath -CompressionLevel Optimal
Remove-Item $staging -Recurse -Force

$sizeMb = [math]::Round((Get-Item $zipPath).Length / 1MB, 1)
Write-Host ""
Write-Host "Smoke pack: $zipPath ($sizeMb MB)" -ForegroundColor Green
Write-Host "Conteúdo: setup.exe, last-build.txt, fase-2-cozinha-L.tracos, checklist 02-smoke" -ForegroundColor Cyan
Write-Host "Checklist: docs\manual\escala\02-smoke-instalador-maquina-limpa.md" -ForegroundColor Cyan
