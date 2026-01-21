# Download No-Intro DAT files
# Downloads all available DAT files from No-Intro for reference and testing

param(
	[string]$OutputPath = "C:\~reference-roms\dats\nointro",
	[switch]$Force
)

$ErrorActionPreference = "Stop"

# Ensure output directory exists
if (-not (Test-Path $OutputPath)) {
	New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
	Write-Host "Created directory: $OutputPath" -ForegroundColor Green
}

Write-Host "Downloading No-Intro DAT files to: $OutputPath" -ForegroundColor Cyan
Write-Host ""

# No-Intro Daily downloads page (contains links to all DAT files)
$baseUrl = "https://datomatic.no-intro.org/index.php?page=download&s=64"

try {
	Write-Host "Fetching No-Intro DAT catalog..." -ForegroundColor Yellow
	
	# Note: No-Intro requires authentication or provides DATs via Datomatic
	# This is a simplified version - in practice, you may need to use the Datomatic API
	
	# Known No-Intro system DAT files (based on common systems)
	$systems = @(
		@{Name = "Nintendo - Game Boy"; File = "Nintendo - Game Boy.dat"},
		@{Name = "Nintendo - Game Boy Color"; File = "Nintendo - Game Boy Color.dat"},
		@{Name = "Nintendo - Game Boy Advance"; File = "Nintendo - Game Boy Advance.dat"},
		@{Name = "Nintendo - Nintendo Entertainment System"; File = "Nintendo - Nintendo Entertainment System.dat"},
		@{Name = "Nintendo - Super Nintendo Entertainment System"; File = "Nintendo - Super Nintendo Entertainment System.dat"},
		@{Name = "Nintendo - Nintendo 64"; File = "Nintendo - Nintendo 64.dat"},
		@{Name = "Nintendo - GameCube"; File = "Nintendo - GameCube.dat"},
		@{Name = "Nintendo - Wii"; File = "Nintendo - Wii.dat"},
		@{Name = "Nintendo - Nintendo DS"; File = "Nintendo - Nintendo DS.dat"},
		@{Name = "Nintendo - Nintendo 3DS"; File = "Nintendo - Nintendo 3DS.dat"},
		@{Name = "Sega - Master System"; File = "Sega - Master System - Mark III.dat"},
		@{Name = "Sega - Mega Drive - Genesis"; File = "Sega - Mega Drive - Genesis.dat"},
		@{Name = "Sega - Game Gear"; File = "Sega - Game Gear.dat"},
		@{Name = "Sega - Saturn"; File = "Sega - Saturn.dat"},
		@{Name = "Sega - Dreamcast"; File = "Sega - Dreamcast.dat"},
		@{Name = "Sony - PlayStation"; File = "Sony - PlayStation.dat"},
		@{Name = "Sony - PlayStation 2"; File = "Sony - PlayStation 2.dat"},
		@{Name = "Sony - PlayStation Portable"; File = "Sony - PlayStation Portable.dat"},
		@{Name = "Atari - 2600"; File = "Atari - 2600.dat"},
		@{Name = "Atari - 5200"; File = "Atari - 5200.dat"},
		@{Name = "Atari - 7800"; File = "Atari - 7800.dat"},
		@{Name = "Atari - Lynx"; File = "Atari - Lynx.dat"},
		@{Name = "Atari - Jaguar"; File = "Atari - Jaguar.dat"},
		@{Name = "NEC - PC Engine - TurboGrafx 16"; File = "NEC - PC Engine - TurboGrafx-16.dat"},
		@{Name = "SNK - Neo Geo Pocket"; File = "SNK - Neo Geo Pocket.dat"},
		@{Name = "SNK - Neo Geo Pocket Color"; File = "SNK - Neo Geo Pocket Color.dat"},
		@{Name = "Bandai - WonderSwan"; File = "Bandai - WonderSwan.dat"},
		@{Name = "Bandai - WonderSwan Color"; File = "Bandai - WonderSwan Color.dat"}
	)

	Write-Host "⚠️  NOTE: No-Intro DATs require authentication from datomatic.no-intro.org" -ForegroundColor Yellow
	Write-Host "This script creates placeholder structure. You'll need to:" -ForegroundColor Yellow
	Write-Host "1. Visit https://datomatic.no-intro.org/" -ForegroundColor Cyan
	Write-Host "2. Register/login to access DAT downloads" -ForegroundColor Cyan
	Write-Host "3. Download the 'Daily' pack or individual system DATs" -ForegroundColor Cyan
	Write-Host "4. Extract to: $OutputPath" -ForegroundColor Cyan
	Write-Host ""

	# Create directory structure for expected DAT files
	foreach ($system in $systems) {
		$systemPath = Join-Path $OutputPath $system.Name
		if (-not (Test-Path $systemPath)) {
			New-Item -ItemType Directory -Path $systemPath -Force | Out-Null
		}
		
		$readmePath = Join-Path $systemPath "README.txt"
		$readme = @"
No-Intro DAT files for: $($system.Name)

Expected file: $($system.File)

To obtain this DAT file:
1. Visit https://datomatic.no-intro.org/
2. Navigate to Downloads → Daily
3. Download the complete Daily pack or individual system
4. Extract the DAT file to this directory

No-Intro provides verified ROM sets with:
- Accurate CRC32/MD5/SHA-1 hashes
- Complete game metadata
- Region and language information
- Parent/clone relationships
"@
		Set-Content -Path $readmePath -Value $readme -Force
	}

	Write-Host "✓ Created directory structure for $($systems.Count) systems" -ForegroundColor Green
	Write-Host ""
	Write-Host "Alternative: Use Datomatic API" -ForegroundColor Cyan
	Write-Host "You can also implement automated download via the Datomatic XML API:" -ForegroundColor Yellow
	Write-Host "  https://datomatic.no-intro.org/stuff/datinfo_xml.php" -ForegroundColor Gray
	Write-Host ""
	Write-Host "✓ Setup complete!" -ForegroundColor Green
	Write-Host "Directory: $OutputPath" -ForegroundColor Gray

} catch {
	Write-Host "✗ Error: $_" -ForegroundColor Red
	exit 1
}
