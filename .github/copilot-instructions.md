# GameInfo Project - AI Copilot Directives

## Project Overview

**Main Purpose:** ROM hacking toolkit with dual output targets:
1. **Dark Repos Wiki** - `*.wikitext` files (ROM/RAM maps, data structures, system descriptions) for games.darkrepos.com
2. **ROM Hacking Tools** - .NET 10 CLI + Python utilities + Blazor web editors

**Architecture:** Monorepo containing multiple tool categories, wiki content, and documentation for retro games (NES, SNES, GB, GBA).

**Home Folder:** `C:\Users\me\source\repos\GameInfo`

## Related Repositories

- `GameInfo` - Main repository (this one) - tools and wiki documentation
- `ffmq-info` - Final Fantasy Mystic Quest disassembly and documentation
- `dw4-info` / `dragon-warrior-4-info` - Dragon Warrior IV NES disassembly
- `dragon-warrior-info` - Dragon Warrior series documentation

## Key Architecture Concepts

### Dual Toolchain Strategy
- **C# .NET 10 Tools** (`src/GameInfoTools.*`) - Primary, cross-platform, high-performance
  - Modular library design: Core → Analysis/Graphics/Text/Rom → CLI
  - 993+ xUnit tests for reliability
  - Modern C# 14 features (pattern matching, spans, file-scoped namespaces)
- **Python Tools** (`tools/`) - Specialized analysis, rapid prototyping, legacy support
  - 148+ tools organized by category (analysis, graphics, text, data, etc.)
  - Many being migrated to C# equivalents (see `docs/python-csharp-mapping.md`)

### DarkRepos Integration
Three interconnected systems stored in `DarkRepos/`:
- **Wiki/** - MediaWiki `.wikitext` files for games.darkrepos.com
  - Organized as `{Platform}/{Game}/{SubPage}.wikitext`
  - Templates in `_meta/Templates/` (rommap, rammap, TBL, etc.)
- **Editor/** - Blazor WebAssembly + .NET backend (local-first web app, similar to Plex)
  - Runs as local service on `http://localhost:5280`
  - Editors for hex, graphics, maps, data tables
- **Web/** - Public documentation site

## Code Style & Formatting

### Indentation
- **ALWAYS use TABS, never spaces** - This is enforced by `.editorconfig`
- Tab width: 4 spaces equivalent
- Applies to: Python, JSON, Assembly, Markdown, YAML, all files

### Hexadecimal Values
- **Always lowercase** for hex values in code
- Correct: `$9d`, `0xca6e`, `$ff00`
- Incorrect: `$9D`, `0xCA6E`, `$FF00`

### Address Notation
- File addresses vs ROM/CPU addresses are NOT the same
- Always clarify which address space is being referenced
- Document header offsets when relevant

### C# Code Style
- **K&R brace style** - Opening braces on same line, not new line
- Use latest .NET (10) and C# (14) features
- Modern coding practices: pattern matching, spans, collection expressions
- Cross-platform compatible code
- All code must pass `dotnet format` with `.editorconfig`

### Technology Stack
- **C# .NET 10** for all production tools (not Python for new tools)
  - Blazor WebAssembly for web UI
  - xUnit for testing (993+ tests across solution)
  - Spectre.Console for rich CLI output
  - System.CommandLine for CLI argument parsing
  - EF Core + SQLite for database when needed
- **Python 3.11+** for specialized analysis tools and rapid prototyping
  - Legacy tools being migrated to C# equivalents
  - Use virtual environment (`.venv/`) for dependency isolation

### .NET Solution Structure (`src/`)
```
GameInfoTools.sln          # Main solution with 15+ projects
├── GameInfoTools.Core/    # Core types, RomFile, utilities
├── GameInfoTools.Analysis/ # ROM analysis, cross-references
├── GameInfoTools.Graphics/ # Tile graphics, CHR editing
├── GameInfoTools.Text/    # Text extraction, script tools
├── GameInfoTools.Rom/     # ROM manipulation, patching
├── GameInfoTools.Data/    # Game data tables, JSON export
├── GameInfoTools.Disassembly/ # CPU disassembly (6502/65816)
├── GameInfoTools.Audio/   # NSF/SPC/audio extraction
├── GameInfoTools.TasConvert/ # TAS replay converter (40+ formats)
├── GameInfoTools.Wiki/    # Wikitext generation
├── GameInfoTools.Cli/     # CLI entry point (git-style commands)
├── GameInfoTools.UI/      # Blazor WebAssembly UI
└── tests/                 # xUnit test projects
```

### Build & Test Commands
```bash
# Build entire solution
dotnet build GameInfoTools.sln

# Run all tests (993+ tests)
dotnet test GameInfoTools.sln

# Run CLI tool
dotnet run --project src/GameInfoTools.Cli -- <command>
# Examples:
#   rom info game.nes
#   text extract game.nes --tbl table.tbl
#   graphics chr game.nes --offset 0x10010
#   analysis map game.nes
```

## Documentation Structure

### `~docs/` (Tilde Docs - Development Documentation)
Documentation about *making* the project:
- `~docs/session-logs/` - AI session log files (markdown)
- `~docs/chat-logs/` - AI chat conversation logs (markdown)
- `~docs/plans/` - Planning documents
- Development notes, decisions, process documentation

### ⚠️ HANDS OFF FILES
**NEVER modify these files - they are manually edited by the user only:**
- `~docs/gameinfo-manual-prompts-log.txt` - User's personal prompt log
- Any file explicitly marked as "manual" or "user-edited"

### `docs/` (Project Documentation)
Documentation about the project itself:
- Organized into subfolders by subject/system
- All documentation reachable from `README.md` link tree
- Game-specific documentation
- Tool usage guides
- Format specifications

## Git Workflow

### Commit Strategy
- Commit in logical groups as work progresses
- Always commit at appropriate checkpoints
- Push commits regularly
- Use conventional commit messages:
  - `feat:` - New features
  - `fix:` - Bug fixes
  - `docs:` - Documentation
  - `style:` - Formatting/whitespace
  - `refactor:` - Code restructuring
  - `test:` - Tests
  - `chore:` - Maintenance

## GitHub Issue Management

### Issue Structure
- All issues tied to parent issues up to epics
- Use GitHub Projects with Kanban board
- Create epics for major work areas
- Break epics into detailed sub-issues
- Update issues as work completes

### Labels
- Use full descriptive labels, no abbreviations
- Labels should have clear meanings
- Categories: priority, type, component, status

### Project Board
- Always associate issues with the project board
- Use Kanban workflow: Backlog → In Progress → Done
- Track all planned and unplanned work

## Session Management

### Session Logs
- Create session logs in `~docs/session-logs/`
- Format: `YYYY-MM-DD-session-NN.md`
- Document work completed, decisions made, next steps

### Chat Logs
- Save chat logs in `~docs/chat-logs/`
- Format: `YYYY-MM-DD-chat-NN.md`
- Preserve context for future sessions

### End of Response
- Always include "What's Next" section
- List remaining work or suggested next prompts
- Track progress against todo lists

## Asset Management

### Extraction
- Extract all possible assets from ROMs
- Use `.include` directives for:
  - Graphics (export to PNG)
  - Stats and data tables (export to JSON)
  - Text and dialog
  - Data structures
  - Sound/music data

### Build Pipeline
- Support transformations: binary ↔ JSON/PNG/etc
- Maintain bidirectional conversion capability
- Document all data formats
- See [Build Pipeline Documentation](../docs/Build-Pipeline/README.md)

### Build Pipeline Flow
```
ROM File → Asset Extractor → Binary Assets
                               ↓
                           Converters
                               ↓
                    Editable Formats (JSON/PNG)
                               ↓ (Edit)
                           Converters
                               ↓
                        Binary Assets
                               ↓
   New ROM ← Assembler ← Source Code + Assets
```

## Python Tool Organization (`tools/`)

148+ Python tools organized into 11 categories:
- `analysis/` (22 tools) - ROM analysis, pattern detection, formula research
- `graphics/` (19 tools) - Tiles, sprites, palettes, animation
- `text/` (19 tools) - Text extraction, encoding, dialogue
- `data/` (29 tools) - Game data editing (monsters, items, spells)
- `rom/` (20 tools) - ROM operations (patches, checksums, headers)
- `debug/` (12 tools) - CDL tools, debug labels, memory tracing
- `assembly/` (12 tools) - Assembly formatting, labels, macros
- `converters/` (11 tools) - Format conversion, batch processing
- `maps/` (10 tools) - World maps, collision, warps
- `editors/` (7 tools) - Specialized editors
- `audio/` (6 tools) - Music extraction, NSF/SPC analysis

**Migration Strategy:** Many Python tools have or will have C# equivalents. Check `docs/python-csharp-mapping.md` before creating new Python tools.

## Output Formats

### Dark Repos Wikitext
- ROM maps
- RAM maps
- Data structures
- System descriptions
- Format specifications

### Markdown Documentation
- Game mechanics
- Tool usage
- Development guides
- Research notes

### Disassembly
- Annotated assembly source
- Symbol/label files
- Cross-references

## Token Usage

Maximize work per penny, save my money, that's what these polices are for: get as much as you can for as little as you can.

- **Maximize token usage per session**
- Continue implementing improvements until token limit
- Don't waste allocated resources
- Complete as much meaningful work as possible

## Todo List Management

- Create todo lists for all work
- Update status as tasks complete
- Use GitHub issues for tracking
- Maintain visibility of progress
