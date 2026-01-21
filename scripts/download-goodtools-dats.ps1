# Download GoodTools DAT files
# Downloads GoodTools DAT files (if publicly available)
#
# NOTE: GoodTools DAT files are not officially distributed anymore.
# The GoodTools project is discontinued. Consider using:
# - No-Intro (authentication required, currently banned)
# - TOSEC (use download-tosec-dats.ps1)
# - Redump (use download-redump-dats.ps1)
#
# This script is a placeholder for future implementation if GoodTools
# DATs become available through an official source.

param(
	[string]$OutputPath = "C:\~reference-roms\dats\goodtools"
)

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Yellow
Write-Host "  GoodTools DAT Download Script" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Yellow
Write-Host ""
Write-Host "⚠️  GoodTools is DISCONTINUED" -ForegroundColor Yellow
Write-Host ""
Write-Host "The GoodTools project is no longer actively maintained." -ForegroundColor Gray
Write-Host "DAT files are not officially distributed." -ForegroundColor Gray
Write-Host ""
Write-Host "Alternative DAT sources:" -ForegroundColor Cyan
Write-Host "  • TOSEC:  .\scripts\download-tosec-dats.ps1" -ForegroundColor Gray
Write-Host "  • Redump: .\scripts\download-redump-dats.ps1" -ForegroundColor Gray
Write-Host ""
Write-Host "If you have GoodTools DAT files, place them in:" -ForegroundColor Gray
Write-Host "  $OutputPath" -ForegroundColor DarkGray
Write-Host ""

exit 0
