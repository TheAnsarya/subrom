# Subrom Project - AI Copilot Directives

## Project Overview

**Main Purpose:** ROM management and verification toolkit
- **Backend:** .NET 10 Web API with SignalR for real-time progress
- **Frontend:** React + TypeScript + Vite UI (`subrom-ui/`)
- DAT file parsing and ROM verification against No-Intro, TOSEC, GoodTools catalogs

**Home Folder:** `C:\Users\me\source\repos\subrom`

## ⛔ CRITICAL RESTRICTIONS

### No-Intro Website - DO NOT SCRAPE
**NEVER attempt to scrape or automate downloads from datomatic.no-intro.org**
- We were **INSTANTLY IP BANNED** when we attempted automated scraping
- The NoIntroProvider is currently DISABLED for downloads
- Users must manually download No-Intro DATs from the website
- Alternative methods needed: consider DAT-o-Matic API (requires auth), or community DAT packs
- Contact: shippa6@hotmail.com to request ban lift

## Key Architecture Concepts

### Technology Stack
- **Backend:** C# .NET 10 Web API
`t- SignalR for real-time streaming progress
`t- EF Core + SQLite for data persistence
`t- Domain-driven design with Services layer
- **Frontend:** React 19 + TypeScript + Vite
`t- Zustand for state management
`t- react-window for virtualized lists (60K+ entries)
`t- FontAwesome icons
`t- CSS Modules for styling

### Frontend Package Manager
- **ALWAYS use Yarn, NEVER npm or npx**
- Vite is the build tool, yarn is the package manager
- Commands:
`t- `yarn install` - Install dependencies
`t- `yarn add <package>` - Add a dependency
`t- `yarn add -D <package>` - Add a dev dependency
`t- `yarn dev` - Start development server
`t- `yarn build` - Build for production
`t- `yarn preview` - Preview production build

## Code Style & Formatting

### Indentation
- **ALWAYS use TABS, never spaces** - This is enforced by `.editorconfig`
- Tab width: 4 spaces equivalent
- Applies to: TypeScript, JSON, CSS, Markdown, YAML, all files

### Hexadecimal Values
- **Always lowercase** for hex values in code
- Correct: `0xca6e`, `0xff00`
- Incorrect: `0xCA6E`, `0xFF00`

### C# Code Style
- **K&R brace style** - Opening braces on same line, not new line
- Use latest .NET (10) and C# (14) features
- Modern coding practices: pattern matching, spans, collection expressions
- All code must pass `dotnet format` with `.editorconfig`

### TypeScript/React Code Style
- Functional components with hooks
- Named exports preferred
- Props interfaces defined inline or in same file
- Use `type` for object shapes, `interface` for extendable contracts

## Project Structure

```
Subrom.sln                 # Main .NET solution
├── src/
│   ├── Subrom.Domain/         # Domain models, value objects, aggregates
│   ├── Subrom.Application/    # Interfaces, DTOs, use cases
│   ├── Subrom.Infrastructure/ # EF Core, parsers, providers, services
│   └── Subrom.Server/         # ASP.NET Core Web API + SignalR
├── tests/
│   └── Subrom.Tests.Unit/     # Unit tests (xUnit)
├── scripts/                   # PowerShell automation scripts
├── docs/                      # User documentation
└── ~docs/                     # Development documentation

subrom-ui/                 # React + Vite frontend
├── src/
│   ├── components/        # React components
│   │   └── ui/            # Reusable UI components
│   ├── hooks/             # Custom React hooks
│   ├── stores/            # Zustand state stores
│   ├── types/             # TypeScript type definitions
│   └── pages/             # Page components
└── package.json           # Yarn dependencies
```

### Build & Run Commands
```bash
# Backend (.NET)
dotnet build Subrom.sln
dotnet run --project src/Subrom.Server

# Frontend (Vite + Yarn)
cd subrom-ui
yarn install
yarn dev      # Development server
yarn build    # Production build
```

## Documentation Structure

### `~docs/` (Tilde Docs - Development Documentation)
Documentation about *making* the project:
- `~docs/plans/` - Planning documents
- `~docs/issues/` - Issue tracking and epics
- Development notes, decisions, process documentation

### ⚠️ HANDS OFF FILES
**NEVER modify these files - they are manually edited by the user only:**
- `~docs/subrom-manual-prompts-log.txt` - User's personal prompt log
- Any file explicitly marked as "manual" or "user-edited"

## Git Workflow

### Commit Strategy
- Commit in logical groups as work progresses
- Always commit at appropriate checkpoints
- Push commits regularly
- Use conventional commit messages:
`t- `feat:` - New features
`t- `fix:` - Bug fixes
`t- `docs:` - Documentation
`t- `style:` - Formatting/whitespace
`t- `refactor:` - Code restructuring
`t- `test:` - Tests
`t- `chore:` - Maintenance

## Token Usage

Maximize work per penny, save my money, that's what these polices are for: get as much as you can for as little as you can.

- **Maximize token usage per session**
- Continue implementing improvements until token limit
- Don't waste allocated resources
- Complete as much meaningful work as possible

## Todo List Management

- Create todo lists for all work
- Update status as tasks complete
- Maintain visibility of progress
