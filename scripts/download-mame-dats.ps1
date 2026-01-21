# Download MAME/MESS DAT files
# Downloads MAME and MESS (Multi Emulator Super System) DAT files

param(
	[string]$OutputPath = "C:\~reference-roms\dats\mame",
	[switch]$Force,
	[string]$MameUrl = "https://www.mamedev.org",
	[string]$DownloadsUrl = "https://www.mamedev.org/release.html"
)

$ErrorActionPreference = "Stop"
$ProgressPreference = 'SilentlyContinue'

# Ensure output directory exists
if (-not (Test-Path $OutputPath)) {
	New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
	Write-Host "Created directory: $OutputPath" -ForegroundColor Green
}

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "  MAME/MESS DAT File Download Script" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host ""
Write-Host "Output Directory: $OutputPath" -ForegroundColor Gray
Write-Host ""

Write-Host "NOTE: MAME DAT files are typically included with MAME releases" -ForegroundColor Yellow
Write-Host "      Download MAME from: https://www.mamedev.org/release.html" -ForegroundColor Yellow
Write-Host "      Extract and copy the 'hash' folder to the output directory" -ForegroundColor Yellow
Write-Host ""
Write-Host "      MAME includes:" -ForegroundColor Gray
Write-Host "        • Arcade game verification data" -ForegroundColor DarkGray
Write-Host "        • MESS (software list) for various systems" -ForegroundColor DarkGray
Write-Host "        • Hash files in XML format" -ForegroundColor DarkGray
Write-Host ""

# Try to get the latest MAME version
try {
	Write-Host "Checking latest MAME version..." -ForegroundColor Cyan
	$response = Invoke-WebRequest -Uri $DownloadsUrl -UseBasicParsing
	$html = $response.Content
	
	# Look for version number
	$versionPattern = 'MAME (\d+\.\d+)'
	$versionMatch = [regex]::Match($html, $versionPattern)
	
	if ($versionMatch.Success) {
		$latestVersion = $versionMatch.Groups[1].Value
		Write-Host "✓ Latest MAME version: $latestVersion" -ForegroundColor Green
		Write-Host ""
		Write-Host "Manual steps required:" -ForegroundColor Yellow
		Write-Host "  1. Download MAME $latestVersion from:" -ForegroundColor Gray
		Write-Host "     https://www.mamedev.org/release.html" -ForegroundColor DarkGray
		Write-Host "  2. Extract the archive" -ForegroundColor Gray
		Write-Host "  3. Copy the 'hash' folder to: $OutputPath" -ForegroundColor Gray
		Write-Host ""
	}
	
} catch {
	Write-Host "⚠️  Could not fetch MAME version automatically" -ForegroundColor Yellow
	Write-Host "   Visit: https://www.mamedev.org/release.html" -ForegroundColor Gray
	Write-Host ""
}

Write-Host "Script completed - manual download required" -ForegroundColor Cyan
