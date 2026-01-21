# Subrom Service Installer Script
# Run as Administrator

param(
	[Parameter(Mandatory=$false)]
	[ValidateSet('Install', 'Uninstall', 'Start', 'Stop', 'Status')]
	[string]$Action = 'Status'
)

$ServiceName = "SubromService"
$DisplayName = "Subrom ROM Manager Service"
$Description = "Background service for Subrom ROM management and verification"
$ExePath = Join-Path $PSScriptRoot "..\src\Subrom.Service\bin\Release\net10.0-windows\Subrom.Service.exe"

function Test-Administrator {
	$currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
	$principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
	return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Install-SubromService {
	if (-not (Test-Administrator)) {
		Write-Error "Administrator privileges required to install the service."
		return
	}

	if (-not (Test-Path $ExePath)) {
		Write-Error "Service executable not found at: $ExePath"
		Write-Host "Please build the project first: dotnet publish src/Subrom.Service -c Release"
		return
	}

	$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
	if ($service) {
		Write-Host "Service already installed. Updating..."
		sc.exe config $ServiceName binPath= "`"$ExePath`""
	} else {
		Write-Host "Installing Subrom Service..."
		New-Service -Name $ServiceName `
			-DisplayName $DisplayName `
			-Description $Description `
			-BinaryPathName $ExePath `
			-StartupType Automatic

		# Configure recovery options (restart on failure)
		sc.exe failure $ServiceName reset= 86400 actions= restart/5000/restart/10000/restart/30000
	}

	Write-Host "Service installed successfully." -ForegroundColor Green
	Write-Host "Run with -Action Start to start the service."
}

function Uninstall-SubromService {
	if (-not (Test-Administrator)) {
		Write-Error "Administrator privileges required to uninstall the service."
		return
	}

	$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
	if (-not $service) {
		Write-Host "Service is not installed."
		return
	}

	if ($service.Status -eq 'Running') {
		Write-Host "Stopping service..."
		Stop-Service -Name $ServiceName -Force
	}

	Write-Host "Removing service..."
	sc.exe delete $ServiceName

	Write-Host "Service uninstalled successfully." -ForegroundColor Green
}

function Start-SubromService {
	if (-not (Test-Administrator)) {
		Write-Error "Administrator privileges required to start the service."
		return
	}

	$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
	if (-not $service) {
		Write-Error "Service is not installed. Run with -Action Install first."
		return
	}

	if ($service.Status -eq 'Running') {
		Write-Host "Service is already running."
		return
	}

	Write-Host "Starting service..."
	Start-Service -Name $ServiceName
	Write-Host "Service started." -ForegroundColor Green
}

function Stop-SubromService {
	if (-not (Test-Administrator)) {
		Write-Error "Administrator privileges required to stop the service."
		return
	}

	$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
	if (-not $service) {
		Write-Error "Service is not installed."
		return
	}

	if ($service.Status -eq 'Stopped') {
		Write-Host "Service is already stopped."
		return
	}

	Write-Host "Stopping service..."
	Stop-Service -Name $ServiceName -Force
	Write-Host "Service stopped." -ForegroundColor Green
}

function Get-SubromServiceStatus {
	$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
	if (-not $service) {
		Write-Host "Service Status: Not Installed" -ForegroundColor Yellow
		return
	}

	$color = switch ($service.Status) {
		'Running' { 'Green' }
		'Stopped' { 'Red' }
		default { 'Yellow' }
	}

	Write-Host "Service Status: $($service.Status)" -ForegroundColor $color
	Write-Host "Display Name: $($service.DisplayName)"
	Write-Host "Startup Type: $($service.StartType)"
}

# Execute action
switch ($Action) {
	'Install' { Install-SubromService }
	'Uninstall' { Uninstall-SubromService }
	'Start' { Start-SubromService }
	'Stop' { Stop-SubromService }
	'Status' { Get-SubromServiceStatus }
}
