# Download TOSEC DAT files
# Downloads the latest TOSEC DAT file pack from tosecdev.org

param(
	[string]$OutputPath = "C:\~reference-roms\dats\tosec",
	[switch]$Force,
	[string]$BaseUrl = "https://www.tosecdev.org",
	[string]$DatfilesUrl = "https://www.tosecdev.org/downloads/category/22-datfiles"
)

$ErrorActionPreference = "Stop"
$ProgressPreference = 'SilentlyContinue' # Faster downloads

# Ensure output directory exists
if (-not (Test-Path $OutputPath)) {
	New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
	Write-Host "Created directory: $OutputPath" -ForegroundColor Green
}

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "  TOSEC DAT File Download Script" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host ""
Write-Host "Output Directory: $OutputPath" -ForegroundColor Gray
Write-Host ""

try {
	Write-Host "Fetching TOSEC release page..." -ForegroundColor Cyan
	
	# Fetch the datfiles category page
	$response = Invoke-WebRequest -Uri $DatfilesUrl -UseBasicParsing
	$html = $response.Content
	
	# Extract the latest release link (e.g., /downloads/category/59-2025-03-13)
	$releasePattern = 'href="(/downloads/category/\d+-[\d-]+)"'
	$releaseMatches = [regex]::Matches($html, $releasePattern)
	
	if ($releaseMatches.Count -eq 0) {
		Write-Host "✗ No release links found on datfiles page" -ForegroundColor Red
		exit 1
	}
	
	# Get the first (latest) release
	$latestReleasePath = $releaseMatches[0].Groups[1].Value
	$latestReleaseUrl = "$BaseUrl$latestReleasePath"
	
	# Extract release date from path
	$releaseDateMatch = [regex]::Match($latestReleasePath, '(\d{4}-\d{2}-\d{2})')
	$releaseDate = if ($releaseDateMatch.Success) { $releaseDateMatch.Groups[1].Value } else { "unknown" }
	
	Write-Host "✓ Found latest release: $releaseDate" -ForegroundColor Green
	Write-Host "  URL: $latestReleaseUrl" -ForegroundColor Gray
	Write-Host ""
	
	# Fetch the release page
	Write-Host "Fetching release page..." -ForegroundColor Cyan
	$releaseResponse = Invoke-WebRequest -Uri $latestReleaseUrl -UseBasicParsing
	$releaseHtml = $releaseResponse.Content
	
	# Find download link (usually a .zip or .7z file)
	# Pattern: "TOSEC - DAT Pack - Complete" followed by the filename
	$downloadPattern = 'href="([^"]*)" title="Download"'
	$downloadMatches = [regex]::Matches($releaseHtml, $downloadPattern)
	
	if ($downloadMatches.Count -eq 0) {
		Write-Host "✗ No download links found on release page" -ForegroundColor Red
		exit 1
	}
	
	Write-Host "✓ Found $($downloadMatches.Count) download file(s)" -ForegroundColor Green
	Write-Host ""
	
	$downloaded = 0
	$skipped = 0
	$totalSize = 0
	
	foreach ($match in $downloadMatches) {
		$downloadUrl = $match.Groups[1].Value
		
		# Make URL absolute if relative
		if (-not $downloadUrl.StartsWith("http")) {
			$downloadUrl = "$BaseUrl$downloadUrl"
		}
		
		# Extract filename
		$fileName = [System.IO.Path]::GetFileName($downloadUrl)
		$outFile = Join-Path $OutputPath $fileName
		
		# Skip if already exists and not forcing
		if ((Test-Path $outFile) -and -not $Force) {
			$fileSize = (Get-Item $outFile).Length
			$sizeMB = [math]::Round($fileSize / 1MB, 2)
			Write-Host "  ⊙ Skipped: $fileName ($sizeMB MB already exists)" -ForegroundColor DarkGray
			$skipped++
			continue
		}
		
		try {
			Write-Host "  → Downloading: $fileName" -ForegroundColor Cyan
			
			$webClient = New-Object System.Net.WebClient
			$webClient.DownloadFile($downloadUrl, $outFile)
			
			$fileSize = (Get-Item $outFile).Length
			$totalSize += $fileSize
			$downloaded++
			
			$sizeMB = [math]::Round($fileSize / 1MB, 2)
			Write-Host "  ✓ Downloaded: $fileName ($sizeMB MB)" -ForegroundColor Green
			
		} catch {
			Write-Host "  ✗ Failed: $fileName - $_" -ForegroundColor Red
			if (Test-Path $outFile) {
				Remove-Item $outFile -Force
			}
		}
	}
	
	Write-Host ""
	Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
	Write-Host "  Download Summary" -ForegroundColor Cyan
	Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
	Write-Host ""
	Write-Host "  Downloaded: $downloaded file(s)" -ForegroundColor Green
	if ($skipped -gt 0) {
		Write-Host "  Skipped:    $skipped file(s)" -ForegroundColor DarkGray
	}
	Write-Host "  Total size: $([math]::Round($totalSize / 1MB, 2)) MB" -ForegroundColor Gray
	Write-Host ""
	Write-Host "Location: $OutputPath" -ForegroundColor Gray
	Write-Host ""
	Write-Host "NOTE: TOSEC releases are typically compressed archives (.zip/.7z)" -ForegroundColor Yellow
	Write-Host "      Extract the archive to access individual DAT files." -ForegroundColor Yellow
	
} catch {
	Write-Host ""
	Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Red
	Write-Host "  Error" -ForegroundColor Red
	Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Red
	Write-Host ""
	Write-Host $_.Exception.Message -ForegroundColor Red
	Write-Host ""
	exit 1
}
