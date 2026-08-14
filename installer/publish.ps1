#Requires -Version 5.1
$ErrorActionPreference = "Stop"

$root    = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "Tracos3DStudio\Tracos3DStudio.csproj"
$outDir  = Join-Path $root "publish\win-x64"
$distDir = Join-Path $root "dist"

$now = Get-Date

# Versão numérica Inno / assembly (yyyy.MM.dd.HHmm — única por minuto)
$version = $now.ToString("yyyy.MM.dd.HHmm")

# Legível em UI, desinstalador e propriedades do arquivo
$versionLabel = $now.ToString("dd/MM/yyyy HH:mm")
$versionDescription = "Software de projeto 3D para moveis planejados - build $versionLabel"

Write-Host "Versão do build: $version" -ForegroundColor Cyan
Write-Host "Descrição: $versionDescription" -ForegroundColor Cyan
Write-Host "Publicando Tracos 3D Studio (win-x64, self-contained)..." -ForegroundColor Cyan

dotnet publish $project `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:Version=$version `
    -p:FileVersion=$version `
    -p:InformationalVersion=$versionLabel `
    -p:Description=$versionDescription `
    -o $outDir

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "Publicação concluída: $outDir" -ForegroundColor Green
Write-Host "Executável: $(Join-Path $outDir 'Tracos3DStudio.exe')"

$iscc = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if ($iscc) {
    Write-Host ""
    Write-Host "Compilando instalador com Inno Setup..." -ForegroundColor Cyan
    & $iscc "/DMyAppVersion=$version" "/DMyVersionLabel=$versionLabel" (Join-Path $PSScriptRoot "Tracos3DStudio.iss")
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

  $installer = Join-Path $distDir "Tracos3DStudio-setup.exe"
    $stampFile = Join-Path $distDir "last-build.txt"

    New-Item -ItemType Directory -Force -Path $distDir | Out-Null
    @(
        "Build=$versionLabel"
        "Version=$version"
        "Installer=Tracos3DStudio-setup.exe"
    ) | Set-Content -Path $stampFile -Encoding UTF8

    Write-Host "Instalador: $installer" -ForegroundColor Green
    Write-Host "Versão gravada: $version ($versionLabel)" -ForegroundColor Green
    Write-Host "Registro: $stampFile" -ForegroundColor Green
}
else {
    Write-Host ""
    Write-Host "Inno Setup não encontrado. Instale em https://jrsoftware.org/isinfo.php" -ForegroundColor Yellow
    Write-Host "Depois execute: ISCC.exe /DMyAppVersion=$version /DMyVersionLabel=$versionLabel installer\Tracos3DStudio.iss"
}
