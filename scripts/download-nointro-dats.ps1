# Download No-Intro DAT files
# Downloads all available No-Intro DAT files by scraping the download page
#
# ⚠️⚠️⚠️ SCRIPT SHELVED - BANNED FROM NO-INTRO ⚠️⚠️⚠️
# Automated scraping attempts resulted in IP ban from datomatic.no-intro.org
# Error: "Something went wrong with your client or another client on your network"
# Ban requires manual email contact to shippa6@hotmail.com to lift
#
# THIS SCRIPT IS NO LONGER FUNCTIONAL AND SHOULD NOT BE USED
#
# ALTERNATIVE APPROACHES:
# - Manual download from No-Intro website (requires credentials)
# - Use other DAT sources: TOSEC, Redump, GoodTools, MAME
# - Contact No-Intro for API access or bulk download permissions
#
# STATUS: SHELVED - DO NOT USE

param(
	[string]$OutputPath = "C:\~reference-roms\dats\nointro",
	[switch]$Force,
	[string]$BaseUrl = "https://datomatic.no-intro.org",
	[string]$DownloadPageUrl = "https://datomatic.no-intro.org/index.php?page=download&s=64",
	[int]$MaxConcurrent = 5
)

$ErrorActionPreference = "Stop"
$ProgressPreference = 'SilentlyContinue' # Faster downloads

# Ensure output directory exists
if (-not (Test-Path $OutputPath)) {
	New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
	Write-Host "Created directory: $OutputPath" -ForegroundColor Green
}

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "  No-Intro DAT File Download Script" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host ""
Write-Host "Output Directory: $OutputPath" -ForegroundColor Gray
Write-Host ""

try {
	Write-Host "Fetching No-Intro system list..." -ForegroundColor Cyan


# Fetch the download page HTML
	$response = Invoke-WebRequest -Uri $DownloadPageUrl -UseBasicParsing
	$html = $response.Content

	# Parse system links - look for pattern: ?page=download&op=dat&s=XX
	$systemLinks = [regex]::Matches($html, '\?page=download&op=dat&s=(\d+)')

	# Extract unique system IDs
	$systemIds = $systemLinks | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique | Sort-Object { [int]$_ }

	Write-Host "✓ Found $($systemIds.Count) No-Intro systems" -ForegroundColor Green
	Write-Host ""

	# Also extract system names from the table
	# Pattern: <a href="?page=download&op=dat&s=17">Nintendo - Game Boy</a>
	$systemInfo = @{}
	$nameMatches = [regex]::Matches($html, '<a[^>]*href="[^"]*\?page=download&op=dat&s=(\d+)"[^>]*>([^<]+)</a>')
	foreach ($match in $nameMatches) {
		$id = $match.Groups[1].Value
		$name = $match.Groups[2].Value.Trim()
		if (-not $systemInfo.ContainsKey($id)) {
			$systemInfo[$id] = $name
		}
	}

	# Download each DAT file
	$downloaded = 0
	$failed = 0
	$skipped = 0
	$totalSize = 0

	Write-Host "Downloading DAT files..." -ForegroundColor Cyan
	Write-Host ""

	foreach ($systemId in $systemIds) {
		$systemName = if ($systemInfo.ContainsKey($systemId)) { $systemInfo[$systemId] } else { "System $systemId" }

		# Sanitize filename
		$safeName = $systemName -replace '[\\/:*?"<>|]', '_'
		$datFile = Join-Path $OutputPath "$safeName.dat"

		# Skip if already exists and not forcing
		if ((Test-Path $datFile) -and -not $Force) {
			Write-Host "  ⊙ Skipped: $systemName (already exists)" -ForegroundColor DarkGray
			$skipped++
			continue
		}

		try {
			$downloadUrl = "$BaseUrl/index.php?page=download&op=dat&s=$systemId"

			# Download with timeout
			$webClient = New-Object System.Net.WebClient
			$webClient.DownloadFile($downloadUrl, $datFile)

		# Check if we got a valid DAT file (should be XML, not HTML)
		$firstLine = Get-Content $datFile -TotalCount 1 -ErrorAction SilentlyContinue
		if ($firstLine -like '<?xml*' -or $firstLine -like '<!DOCTYPE datafile*') {
				$fileSize = (Get-Item $datFile).Length
				$totalSize += $fileSize
				$downloaded++
				Write-Host "  ✓ Downloaded: $systemName ($([math]::Round($fileSize / 1KB, 1)) KB)" -ForegroundColor Green
			} else {
				# Not a valid DAT file
				Remove-Item $datFile -Force
				$failed++
				Write-Host "  ✗ Failed: $systemName (invalid format)" -ForegroundColor Red
			}

		} catch {
			$failed++
			Write-Host "  ✗ Failed: $systemName ($_)" -ForegroundColor Red
			if (Test-Path $datFile) {
				Remove-Item $datFile -Force
			}
		}

		# Small delay to avoid hammering the server
		Start-Sleep -Milliseconds 250
	}

	Write-Host ""
	Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
	Write-Host "  Download Summary" -ForegroundColor Cyan
	Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
	Write-Host ""
	Write-Host "  Downloaded: $downloaded DAT files" -ForegroundColor Green
	if ($skipped -gt 0) {
		Write-Host "  Skipped:    $skipped DAT files" -ForegroundColor DarkGray
	}
	if ($failed -gt 0) {
		Write-Host "  Failed:     $failed DAT files" -ForegroundColor Red
	}
	Write-Host "  Total size: $([math]::Round($totalSize / 1MB, 2)) MB" -ForegroundColor White
	Write-Host ""
	Write-Host "Location: $OutputPath" -ForegroundColor Gray
	Write-Host ""

} catch {
	Write-Host "✗ Error: $_" -ForegroundColor Red
	Write-Host $_.ScriptStackTrace -ForegroundColor DarkGray
	exit 1
}

Write-Host ""
