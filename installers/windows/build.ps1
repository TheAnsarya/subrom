# Windows Installer Build Script
# Builds MSI using WiX Toolset v5

param(
	[string]$Configuration = "Release",
	[string]$Version = "1.2.0"
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent (Split-Path -Parent $ScriptDir)

Write-Host "Building Subrom Windows Installer v$Version" -ForegroundColor Cyan

# Build paths
$ServerProject = Join-Path $RootDir "src\Subrom.Server\Subrom.Server.csproj"
$ServiceProject = Join-Path $RootDir "src\Subrom.Service\Subrom.Service.csproj"
$TrayProject = Join-Path $RootDir "src\Subrom.Tray\Subrom.Tray.csproj"

$PublishDir = Join-Path $ScriptDir "publish"
$ServerPublish = Join-Path $PublishDir "Server"
$ServicePublish = Join-Path $PublishDir "Service"
$TrayPublish = Join-Path $PublishDir "Tray"

# Clean publish directory
if (Test-Path $PublishDir) {
	Remove-Item $PublishDir -Recurse -Force
}
New-Item -ItemType Directory -Path $PublishDir -Force | Out-Null

Write-Host "Publishing Server..." -ForegroundColor Yellow
dotnet publish $ServerProject `
	-c $Configuration `
	-r win-x64 `
	--self-contained true `
	-p:PublishSingleFile=false `
	-p:Version=$Version `
	-o $ServerPublish

Write-Host "Publishing Service..." -ForegroundColor Yellow
dotnet publish $ServiceProject `
	-c $Configuration `
	-r win-x64 `
	--self-contained true `
	-p:PublishSingleFile=true `
	-p:Version=$Version `
	-o $ServicePublish

Write-Host "Publishing Tray App..." -ForegroundColor Yellow
dotnet publish $TrayProject `
	-c $Configuration `
	-r win-x64 `
	--self-contained true `
	-p:PublishSingleFile=true `
	-p:Version=$Version `
	-o $TrayPublish

Write-Host "Building MSI with WiX..." -ForegroundColor Yellow
$WixProject = Join-Path $ScriptDir "Subrom.Installer.wixproj"

dotnet build $WixProject `
	-c $Configuration `
	-p:ProductVersion=$Version `
	-p:PublishDir=$ServerPublish `
	-p:ServicePublishDir=$ServicePublish `
	-p:TrayPublishDir=$TrayPublish

$MsiPath = Join-Path $ScriptDir "bin\$Configuration\Subrom-$Version-win-x64.msi"
if (Test-Path $MsiPath) {
	Write-Host "Successfully built: $MsiPath" -ForegroundColor Green
} else {
	Write-Host "Build failed - MSI not found" -ForegroundColor Red
	exit 1
}

Write-Host "Done!" -ForegroundColor Cyan
