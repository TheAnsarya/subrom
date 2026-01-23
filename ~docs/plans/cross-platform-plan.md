# Cross-Platform Support Plan

**Created:** January 22, 2026  
**Target:** 1.0.0 Release  
**Priority:** CRITICAL - Required for parity

## Current State Analysis

### ✅ Already Cross-Platform (No Changes Needed)

| Component | Target Framework | Notes |
|-----------|------------------|-------|
| **Subrom.Server** | `net10.0` | ASP.NET Core is fully cross-platform |
| **Subrom.Domain** | `net10.0` | Pure domain models, no platform deps |
| **Subrom.Application** | `net10.0` | Application services, no platform deps |
| **Subrom.Infrastructure** | `net10.0` | EF Core SQLite, SharpCompress - cross-platform |

The **core server** (`Subrom.Server`) is already cross-platform! It can run on Windows, macOS, and Linux today.

### ❌ Windows-Only Components (Need Work)

| Component | Current TFM | Windows-Only Dependency | Issue |
|-----------|-------------|-------------------------|-------|
| **Subrom.Service** | `net10.0-windows` | `Microsoft.Extensions.Hosting.WindowsServices` | Windows Service SCM integration |
| **Subrom.Service** | `net10.0-windows` | `Serilog.Sinks.EventLog` | Windows Event Log |
| **Subrom.Tray** | `net10.0-windows` | WinForms (`NotifyIcon`, `ContextMenuStrip`) | System tray UI |

---

## Problem Breakdown

### 1. Windows Service (`Subrom.Service`)

**Current Implementation:**
- Uses `Microsoft.Extensions.Hosting.WindowsServices` for SCM integration
- Writes to Windows Event Log via Serilog
- Spawns `Subrom.Server` as a child process
- Auto-restarts if server crashes

**The Reality:**
This component exists to integrate with Windows' service infrastructure (SCM). On Linux/macOS, services work completely differently:
- **Linux:** systemd, init.d, supervisord
- **macOS:** launchd

**Solution Options:**

| Option | Complexity | Effort | Recommendation |
|--------|------------|--------|----------------|
| A) Remove Service project, run Server directly | Low | 1 day | ✅ **RECOMMENDED** |
| B) Create platform-specific service projects | High | 2 weeks | ❌ Overkill for 1.0 |
| C) Use `Microsoft.Extensions.Hosting.Systemd` for Linux | Medium | 3 days | ⚠️ Future consideration |

**Recommendation: Option A**

The `Subrom.Service` project is essentially a process supervisor that:
1. Starts the server
2. Monitors health
3. Restarts on failure

This can be done by:
- Running `Subrom.Server` directly via systemd/launchd/supervisor
- The server itself handles graceful shutdown
- External supervisors (systemd, launchd, Docker) handle restarts

### 2. System Tray (`Subrom.Tray`)

**Current Implementation:**
- WinForms `NotifyIcon` for system tray icon
- `ContextMenuStrip` for right-click menu
- `SettingsForm` for settings dialog
- `Process` management to spawn/kill server

**Platform-Specific Tray Technologies:**
| Platform | Tray Technology | UI Framework |
|----------|-----------------|--------------|
| Windows | WinForms NotifyIcon | WinForms |
| macOS | NSStatusBar | AppKit / .NET MAUI |
| Linux | libappindicator / GTK StatusIcon | GTK / Avalonia |

**Solution Options:**

| Option | Complexity | Effort | Recommendation |
|--------|------------|--------|----------------|
| A) Drop tray app, web UI only | Low | 0 days | ⚠️ Acceptable for 1.0 |
| B) Avalonia for cross-platform tray | Medium | 1 week | ✅ **RECOMMENDED** |
| C) Electron tray wrapper | Medium | 1 week | ❌ Heavy dependency |
| D) Platform-specific tray projects | High | 3 weeks | ❌ Too much work |

**Recommendation: Option A for 1.0.0, Option B for 1.1.0**

For 1.0.0:
- Make the tray app **optional** and Windows-only
- The web UI is the primary interface
- Users on Linux/macOS run the server directly or via systemd/launchd

For 1.1.0:
- Use **Avalonia** for a cross-platform tray application
- Avalonia supports tray icons on Windows, macOS, and Linux

---

## Recommended Architecture for 1.0.0

```
┌─────────────────────────────────────────────────────────────┐
│                    USER INTERFACE LAYER                      │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│   ┌─────────────────────────────────────────────────────┐   │
│   │              Web UI (React + Vite)                  │   │
│   │         ✅ Cross-Platform (Browser-based)           │   │
│   └─────────────────────────────────────────────────────┘   │
│                                                             │
│   ┌─────────────────────────────────────────────────────┐   │
│   │              System Tray (Subrom.Tray)              │   │
│   │         ⚠️ Windows-Only (Optional for 1.0)          │   │
│   │         🔜 Avalonia Cross-Platform (1.1.0)          │   │
│   └─────────────────────────────────────────────────────┘   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                      SERVER LAYER                            │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│   ┌─────────────────────────────────────────────────────┐   │
│   │              Subrom.Server (ASP.NET Core)           │   │
│   │         ✅ Cross-Platform (Windows/macOS/Linux)     │   │
│   │                                                     │   │
│   │   • REST API                                        │   │
│   │   • SignalR real-time updates                       │   │
│   │   • Static file serving for Web UI                  │   │
│   │   • Background services (scanning, hashing)         │   │
│   └─────────────────────────────────────────────────────┘   │
│                                                             │
│   ┌─────────────────────────────────────────────────────┐   │
│   │              Subrom.Service (Windows Service)       │   │
│   │         ⚠️ Windows-Only (SCM Integration)           │   │
│   │         📝 Use systemd/launchd on Linux/macOS       │   │
│   └─────────────────────────────────────────────────────┘   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## Implementation Plan

### Phase 1: Core Cross-Platform (1.0.0) - REQUIRED

#### 1.1 Update Project Targets

| Task | File | Change |
|------|------|--------|
| Remove Windows TFM from Service | `Subrom.Service.csproj` | Keep Windows-only but conditional |
| Keep Server cross-platform | `Subrom.Server.csproj` | Already `net10.0` ✅ |

#### 1.2 Add Platform Detection

Create `src/Subrom.Infrastructure/Platform/PlatformHelper.cs`:

```csharp
public static class PlatformHelper {
    public static bool IsWindows => OperatingSystem.IsWindows();
    public static bool IsMacOS => OperatingSystem.IsMacOS();
    public static bool IsLinux => OperatingSystem.IsLinux();
    
    public static string GetDataDirectory() {
        if (IsWindows)
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Subrom");
        if (IsMacOS)
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "Subrom");
        // Linux
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "subrom");
    }
    
    public static string GetLogDirectory() {
        if (IsWindows)
            return Path.Combine(GetDataDirectory(), "logs");
        if (IsMacOS)
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Logs", "Subrom");
        // Linux
        return Path.Combine(GetDataDirectory(), "logs");
    }
}
```

#### 1.3 Create Platform-Specific Service Configurations

**Linux (systemd):** Create `scripts/linux/subrom.service`
```ini
[Unit]
Description=Subrom ROM Manager
After=network.target

[Service]
Type=simple
User=subrom
WorkingDirectory=/opt/subrom
ExecStart=/opt/subrom/Subrom.Server
Restart=always
RestartSec=10
Environment=ASPNETCORE_URLS=http://localhost:52100

[Install]
WantedBy=multi-user.target
```

**macOS (launchd):** Create `scripts/macos/com.subrom.server.plist`
```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>com.subrom.server</string>
    <key>ProgramArguments</key>
    <array>
        <string>/Applications/Subrom/Subrom.Server</string>
    </array>
    <key>RunAtLoad</key>
    <true/>
    <key>KeepAlive</key>
    <true/>
    <key>StandardOutPath</key>
    <string>/tmp/subrom.log</string>
    <key>StandardErrorPath</key>
    <string>/tmp/subrom.err</string>
</dict>
</plist>
```

#### 1.4 Conditional Compilation for Windows-Only Features

Update `Subrom.Service.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk.Worker">
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <!-- Only build on Windows -->
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
  </PropertyGroup>
</Project>
```

Update solution to conditionally include:
```xml
<!-- In Subrom.sln -->
Project("{...}") = "Subrom.Service", "src\Subrom.Service\Subrom.Service.csproj", "{...}"
EndProject
<!-- Use conditional build in CI/CD -->
```

#### 1.5 Update Documentation

- README.md: Add Linux/macOS installation instructions
- Create platform-specific guides in `docs/`

---

### Phase 2: Cross-Platform Tray App (1.1.0) - PLANNED

#### 2.1 Avalonia Tray Implementation

Replace WinForms with Avalonia:
- Create `Subrom.Tray.Avalonia` project
- Use `Avalonia.Desktop` with tray icon support
- Single codebase for Windows, macOS, Linux

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Avalonia" Version="11.*" />
    <PackageReference Include="Avalonia.Desktop" Version="11.*" />
    <PackageReference Include="Avalonia.Themes.Fluent" Version="11.*" />
  </ItemGroup>
</Project>
```

---

## File System Considerations

### Database Location

| Platform | Default Location |
|----------|------------------|
| Windows | `%LOCALAPPDATA%\Subrom\subrom.db` |
| macOS | `~/Library/Application Support/Subrom/subrom.db` |
| Linux | `~/.config/subrom/subrom.db` |

### Log Location

| Platform | Default Location |
|----------|------------------|
| Windows | `%LOCALAPPDATA%\Subrom\logs\` |
| macOS | `~/Library/Logs/Subrom/` |
| Linux | `~/.config/subrom/logs/` |

### Configuration

| Platform | Default Location |
|----------|------------------|
| Windows | `%LOCALAPPDATA%\Subrom\appsettings.json` |
| macOS | `~/Library/Application Support/Subrom/appsettings.json` |
| Linux | `~/.config/subrom/appsettings.json` |

---

## Testing Matrix

| Scenario | Windows | macOS | Linux |
|----------|---------|-------|-------|
| Server starts | ✅ | 🧪 | 🧪 |
| Web UI accessible | ✅ | 🧪 | 🧪 |
| DAT parsing | ✅ | 🧪 | 🧪 |
| ROM scanning | ✅ | 🧪 | 🧪 |
| Archive extraction | ✅ | 🧪 | 🧪 |
| Database persistence | ✅ | 🧪 | 🧪 |
| SignalR real-time | ✅ | 🧪 | 🧪 |
| Tray app | ✅ | ❌ 1.1 | ❌ 1.1 |
| Windows Service | ✅ | N/A | N/A |
| systemd service | N/A | N/A | 🧪 |
| launchd service | N/A | 🧪 | N/A |

Legend: ✅ Tested | 🧪 Needs Testing | ❌ Not Available

---

## Summary for 1.0.0

### What Works Cross-Platform NOW

1. **Subrom.Server** - Full API, SignalR, web UI hosting
2. **Subrom.Domain** - All domain models
3. **Subrom.Application** - All application services
4. **Subrom.Infrastructure** - Database, parsers, archive handling
5. **subrom-ui** - React web frontend

### What's Windows-Only (Acceptable for 1.0.0)

1. **Subrom.Service** - Windows Service wrapper (use systemd/launchd instead)
2. **Subrom.Tray** - WinForms tray app (web UI is primary interface)

### What Needs to Be Done for 1.0.0

| Task | Effort | Priority |
|------|--------|----------|
| Create PlatformHelper class | 2 hours | HIGH |
| Update data/log directory resolution | 4 hours | HIGH |
| Create systemd service file | 1 hour | HIGH |
| Create launchd plist | 1 hour | HIGH |
| Update README with Linux/macOS instructions | 2 hours | HIGH |
| Test on Linux VM/Docker | 4 hours | HIGH |
| Test on macOS (if available) | 4 hours | MEDIUM |
| Update CI to build cross-platform | 2 hours | MEDIUM |

**Total Estimated Effort:** 2-3 days

---

## Risks and Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Path separators (`\` vs `/`) | Low | Medium | Use `Path.Combine()` consistently |
| File permissions on Linux | Medium | Low | Document required permissions |
| Case-sensitive file systems | Medium | Medium | Use consistent casing |
| Missing SharpCompress native deps | Low | High | Test on clean Linux/macOS |
| SQLite native library issues | Low | High | Include platform-specific SQLite |

---

## Next Steps

1. ✅ Create this plan document
2. 🔲 Add Epic #13.5: Cross-Platform Support to epics.md
3. 🔲 Implement PlatformHelper class
4. 🔲 Create service configuration files for Linux/macOS
5. 🔲 Update documentation
6. 🔲 Test on Linux (Docker)
