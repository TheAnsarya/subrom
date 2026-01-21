# Download Redump DAT files
# Downloads DAT files from Redump.org (disc preservation project)
#
# NOTE: Redump requires authentication to download DAT files.
# This script provides information on manual download process.

param(
	[string]$OutputPath = "C:\~reference-roms\dats\redump",
	[string]$Username = "",
	[string]$Password = ""
)

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "  Redump DAT File Download Script" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host ""
Write-Host "Output Directory: $OutputPath" -ForegroundColor Gray
Write-Host ""

if (-not (Test-Path $OutputPath)) {
	New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
	Write-Host "Created directory: $OutputPath" -ForegroundColor Green
	Write-Host ""
}

Write-Host "⚠️  AUTHENTICATION REQUIRED" -ForegroundColor Yellow
Write-Host ""
Write-Host "Redump.org requires a registered account to download DAT files." -ForegroundColor Gray
Write-Host ""
Write-Host "Manual Download Steps:" -ForegroundColor Cyan
Write-Host "  1. Register an account at: http://redump.org/signup/" -ForegroundColor Gray
Write-Host "  2. Log in at: http://redump.org/login/" -ForegroundColor Gray
Write-Host "  3. Visit Downloads: http://redump.org/downloads/" -ForegroundColor Gray
Write-Host "  4. Download desired system DAT files" -ForegroundColor Gray
Write-Host "  5. Save files to: $OutputPath" -ForegroundColor Gray
Write-Host ""
Write-Host "Available Systems:" -ForegroundColor Cyan
Write-Host "  • Sony PlayStation, PlayStation 2, PlayStation 3, PSP" -ForegroundColor DarkGray
Write-Host "  • Microsoft Xbox, Xbox 360, Xbox One" -ForegroundColor DarkGray
Write-Host "  • Nintendo GameCube, Wii, Wii U" -ForegroundColor DarkGray
Write-Host "  • Sega Dreamcast, Saturn, Mega CD" -ForegroundColor DarkGray
Write-Host "  • PC CD-ROM, DVD-ROM" -ForegroundColor DarkGray
Write-Host "  • And many more optical disc based systems" -ForegroundColor DarkGray
Write-Host ""
Write-Host "NOTE: Redump specializes in optical disc (CD/DVD/BD) preservation" -ForegroundColor Yellow
Write-Host "      For cartridge-based systems, use TOSEC or No-Intro" -ForegroundColor Yellow
Write-Host ""

if ($Username -and $Password) {
	Write-Host "⚠️  Automated download not yet implemented" -ForegroundColor Yellow
	Write-Host "   Username/Password parameters are reserved for future use" -ForegroundColor Gray
	Write-Host ""
}

Write-Host "For automated downloads, consider:" -ForegroundColor Cyan
Write-Host "  • TOSEC: .\scripts\download-tosec-dats.ps1" -ForegroundColor Gray
Write-Host "  • MAME:  .\scripts\download-mame-dats.ps1" -ForegroundColor Gray
Write-Host ""
