# Plex-Like Architecture Plan

## Overview

Subrom will function as a **local server application** similar to Plex Media Server:
- Runs as a background service/process
- Accessible via web browser at `http://localhost:52100`
- System tray icon for quick access and control
- Optional Windows Service for auto-start

## User Experience Flow

```
┌─────────────────────────────────────────────────────────────────────┐
│                        User Journey                                  │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  1. INSTALL                                                          │
│     ├── Run installer                                                │
│     ├── Choose: Run as Service (auto-start) or Manual               │
│     └── Opens browser to http://localhost:52100                      │
│                                                                      │
│  2. FIRST RUN                                                        │
│     ├── Welcome wizard                                               │
│     ├── Add drives/folders to scan                                  │
│     ├── Import DAT files (or download from providers)               │
│     └── Initial scan starts                                         │
│                                                                      │
│  3. DAILY USE                                                        │
│     ├── System tray icon shows status                               │
│     ├── Click icon → Open web UI                                    │
│     ├── Right-click → Quick actions menu                            │
│     └── Background scanning when drives connect                     │
│                                                                      │
│  4. WEB UI                                                           │
│     ├── Dashboard with collection stats                             │
│     ├── Browse/search games and ROMs                                │
│     ├── View verification reports                                   │
│     ├── Manage drives and DAT files                                 │
│     └── Settings and preferences                                    │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

## Architecture Components

### 1. Subrom.Server (Core)

The main application host running ASP.NET Core:

```csharp
// Program.cs
var builder = Host.CreateApplicationBuilder(args);

// Determine hosting mode
var isService = args.Contains("--service");
var isTray = !isService && !args.Contains("--console");

builder.Services.AddSubromServer();
builder.Services.AddSubromWeb();
builder.Services.AddSubromSignalR();

if (OperatingSystem.IsWindows() && isService) {
`tbuilder.Services.AddWindowsService(options => {
`t    options.ServiceName = "Subrom";
`t});
}

var app = builder.Build();
app.MapSubromEndpoints();
await app.RunAsync();
```

### 2. Subrom.Tray (System Tray)

Windows Forms application for tray icon:

```csharp
public class TrayApplication : ApplicationContext {
`tprivate NotifyIcon _trayIcon;
`tprivate Process? _serverProcess;
`t
`tpublic TrayApplication() {
`t    _trayIcon = new NotifyIcon {
`t        Icon = Resources.SubromIcon,
`t        Text = "Subrom - ROM Manager",
`t        Visible = true,
`t        ContextMenuStrip = CreateContextMenu()
`t    };
`t    
`t    _trayIcon.DoubleClick += (s, e) => OpenWebUI();
`t    StartServer();
`t}
`t
`tprivate ContextMenuStrip CreateContextMenu() {
`t    var menu = new ContextMenuStrip();
`t    menu.Items.Add("Open Subrom", null, (s, e) => OpenWebUI());
`t    menu.Items.Add("-");
`t    menu.Items.Add("Start Scan", null, (s, e) => TriggerScan());
`t    menu.Items.Add("View Logs", null, (s, e) => OpenLogs());
`t    menu.Items.Add("-");
`t    menu.Items.Add("Settings", null, (s, e) => OpenSettings());
`t    menu.Items.Add("-");
`t    menu.Items.Add("Restart Server", null, (s, e) => RestartServer());
`t    menu.Items.Add("Exit", null, (s, e) => Exit());
`t    return menu;
`t}
`t
`tprivate void OpenWebUI() {
`t    Process.Start(new ProcessStartInfo {
`t        FileName = "http://localhost:52100",
`t        UseShellExecute = true
`t    });
`t}
}
```

### 3. Subrom.Service (Windows Service)

Windows Service wrapper for auto-start:

```csharp
// Uses Microsoft.Extensions.Hosting.WindowsServices
public class Program {
`tpublic static async Task Main(string[] args) {
`t    var builder = Host.CreateApplicationBuilder(args);
`t    
`t    builder.Services.AddWindowsService(options => {
`t        options.ServiceName = "Subrom";
`t    });
`t    
`t    builder.Services.AddHostedService<SubromServerService>();
`t    
`t    var host = builder.Build();
`t    await host.RunAsync();
`t}
}

public class SubromServerService : BackgroundService {
`tprotected override async Task ExecuteAsync(CancellationToken stoppingToken) {
`t    // Start the web server
`t    var webApp = CreateWebApplication();
`t    await webApp.RunAsync(stoppingToken);
`t}
}
```

## Port Configuration

| Port | Purpose |
|------|---------|
| 52100 | HTTP Web UI and API |
| 52101 | HTTPS (optional) |
| 52102 | SignalR WebSocket fallback |

Why 52100? Easy to remember, unlikely to conflict, similar to Plex (32400).

## System Tray Features

### Icon States
- 🟢 **Green** - Server running, all healthy
- 🟡 **Yellow** - Server running, scan in progress
- 🔴 **Red** - Server error or stopped
- ⚪ **Gray** - Starting up

### Context Menu
```
┌─────────────────────────┐
│ 📂 Open Subrom          │  ← Opens web UI
├─────────────────────────┤
│ 🔍 Start Quick Scan     │  ← Scan recent changes
│ 📊 View Statistics      │  ← Show stats popup
│ 📋 View Logs            │  ← Open log viewer
├─────────────────────────┤
│ ⚙️ Settings             │  ← Open settings dialog
├─────────────────────────┤
│ 🔄 Restart Server       │
│ ❌ Exit                 │
└─────────────────────────┘
```

### Notifications
- DAT import completed
- Scan completed with summary
- New drive detected
- Errors requiring attention

## Static File Serving

The React UI will be served directly by the server:

```csharp
// In production, serve React build from embedded resources or wwwroot
app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions {
`tFileProvider = new PhysicalFileProvider(
`t    Path.Combine(AppContext.BaseDirectory, "wwwroot")),
`tRequestPath = ""
});

// SPA fallback for client-side routing
app.MapFallbackToFile("index.html");
```

Build process:
```bash
# Build React UI
cd subrom-ui
yarn build

# Copy to server wwwroot
cp -r dist/* ../Subrom.Server/wwwroot/
```

## Installation Options

### 1. Portable Mode
- Extract ZIP anywhere
- Run `Subrom.exe`
- Data stored in application folder

### 2. Installed Mode (Recommended)
- MSI/MSIX installer
- Installs to Program Files
- Data in %LOCALAPPDATA%/Subrom
- Optional: Install as Windows Service
- Optional: Add to startup

### 3. Docker (Future)
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY . .
EXPOSE 52100
ENTRYPOINT ["dotnet", "Subrom.Server.dll"]
```

## Configuration Management

### Settings Storage
```
%LOCALAPPDATA%/Subrom/
├── settings.json        # User settings
├── subrom.db           # SQLite database
├── logs/               # Log files
│   ├── subrom-20260120.log
│   └── ...
└── cache/              # Temporary cache
```

### Settings Schema
```json
{
`t"server": {
`t"port": 52100,
`t"bindAddress": "localhost",
`t"enableHttps": false
`t},
`t"startup": {
`t"runOnStartup": true,
`t"startMinimized": true,
`t"checkForUpdates": true
`t},
`t"scanning": {
`t"autoScanOnDriveConnect": true,
`t"scanIntervalHours": 24,
`t"parallelHashingThreads": 4
`t},
`t"ui": {
`t"theme": "dark",
`t"language": "en"
`t},
`t"drives": [
`t{
`t  "id": "drive-1",
`t  "path": "E:\\ROMs",
`t  "label": "ROM Drive",
`t  "autoScan": true
`t}
`t]
}
```

## Development vs Production

### Development Mode
```bash
# Terminal 1: Run server with hot reload
cd Subrom.Server
dotnet watch run

# Terminal 2: Run React dev server
cd subrom-ui
yarn dev
```
React dev server proxies API calls to backend.

### Production Mode
```bash
# Build everything
dotnet publish Subrom.Server -c Release
cd subrom-ui && yarn build

# Run
./Subrom.Server
```
Server serves both API and static files.

## Cross-Platform Considerations

### Windows
- System tray with NotifyIcon
- Windows Service option
- MSI installer

### Linux (Future)
- Systemd service
- Desktop entry for autostart
- No tray (or use libappindicator)

### macOS (Future)
- LaunchAgent for autostart
- Menu bar app
- DMG installer

## Security Model

### Local-Only (Default)
- Binds to `localhost` only
- No authentication needed
- Firewall doesn't need configuration

### Network Access (Optional)
- Explicit setting to enable
- Binds to `0.0.0.0`
- Requires API key for access
- HTTPS recommended

```csharp
// When network access enabled
app.UseMiddleware<ApiKeyAuthMiddleware>();

public class ApiKeyAuthMiddleware {
`tpublic async Task InvokeAsync(HttpContext context) {
`t    if (context.Request.Host.Host != "localhost") {
`t        var apiKey = context.Request.Headers["X-Api-Key"];
`t        if (!ValidateApiKey(apiKey)) {
`t            context.Response.StatusCode = 401;
`t            return;
`t        }
`t    }
`t    await _next(context);
`t}
}
```

## Monitoring & Health

### Health Endpoint
```
GET /api/health
{
`t"status": "healthy",
`t"uptime": "2d 14h 32m",
`t"database": "ok",
`t"activeScanJobs": 0,
`t"lastScanAt": "2026-01-20T10:30:00Z",
`t"version": "1.0.0"
}
```

### Metrics (Optional)
- Prometheus endpoint at `/metrics`
- Grafana dashboard template

## Error Handling

### Server Crashes
- Tray app detects and offers restart
- Windows Service auto-restarts (recovery options)
- Crash logs saved

### Database Corruption
- Automatic backup before migrations
- Recovery mode with backup restore
- Export/import tools

## Update Mechanism (Future)

1. Check GitHub releases for new version
2. Download update package
3. Stop server gracefully
4. Apply update
5. Restart server
6. Migrate database if needed
