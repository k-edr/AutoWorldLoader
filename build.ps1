$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

$buildConfig = Get-Content (Join-Path $scriptDir "build-config.json") -Raw | ConvertFrom-Json
$seBin64    = $buildConfig.seBin64
$pluginsDir = Join-Path $seBin64 "Plugins"
$configXml  = Join-Path $pluginsDir "config.xml"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host " AutoWorldLoader - Build and Deploy" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path $seBin64))   { Write-Error "SE Bin64 not found: $seBin64"; exit 1 }
if (-not (Test-Path $configXml)) { Write-Error "config.xml not found: $configXml"; exit 1 }

# Kill game
$seProc = Get-Process -Name "SpaceEngineers" -ErrorAction SilentlyContinue
$launcherProc = Get-Process -Name "SpaceEngineersLauncher" -ErrorAction SilentlyContinue
if ($seProc -or $launcherProc) {
    Write-Host "[0] Game is running - stopping..." -ForegroundColor Magenta
    & cmd /c "taskkill /f /im SpaceEngineers.exe 2>nul" 2>$null
    & cmd /c "taskkill /f /im SpaceEngineersLauncher.exe 2>nul" 2>$null
    Start-Sleep -Seconds 3
}

# Find MSBuild
$msbuildPaths = @(
    "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles}\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
)
$msbuild = $null
foreach ($path in $msbuildPaths) {
    if (Test-Path $path) { $msbuild = $path; break }
}
if (-not $msbuild) { Write-Error "MSBuild not found."; exit 1 }

# Build
Write-Host "[1/3] Building AutoWorldLoader..." -ForegroundColor Yellow
$csproj = Join-Path $scriptDir "AutoWorldLoader.csproj"
$result = & $msbuild $csproj /p:Configuration=Release /t:Rebuild /v:minimal /nologo 2>&1
if ($LASTEXITCODE -ne 0) { Write-Host "BUILD FAILED" -ForegroundColor Red; Write-Host ($result -join "`n"); exit 1 }
Write-Host "  Build OK" -ForegroundColor Green

# Copy
Write-Host "[2/3] Copying DLL..." -ForegroundColor Yellow
$dll = Get-ChildItem -Path (Join-Path $scriptDir "bin\Release") -Recurse -Filter "AutoWorldLoader.dll" -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $dll) { Write-Error "DLL not found"; exit 1 }
$dest = Join-Path $pluginsDir "AutoWorldLoader.dll"
Copy-Item -Path $dll.FullName -Destination $dest -Force
Write-Host "  AutoWorldLoader.dll" -ForegroundColor Green

# Register in config.xml
Write-Host "[3/3] Updating PluginLoader config.xml..." -ForegroundColor Yellow
[xml]$config = Get-Content $configXml -Encoding UTF8
$pluginsNode = $config.PluginConfig.Plugins
if (-not $pluginsNode) {
    $pluginsNode = $config.CreateElement("Plugins")
    $config.PluginConfig.AppendChild($pluginsNode) | Out-Null
}
$alreadyExists = $false
foreach ($idNode in $pluginsNode.Id) {
    if ($idNode.'#text' -eq $dest) { $alreadyExists = $true; break }
}
if (-not $alreadyExists) {
    $newId = $config.CreateElement("Id")
    $newId.InnerText = $dest
    $pluginsNode.AppendChild($newId) | Out-Null
    $config.Save($configXml)
    Write-Host "  Registered: $dest" -ForegroundColor Green
} else {
    Write-Host "  Already registered" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Done!" -ForegroundColor Green
