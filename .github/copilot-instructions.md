# Subrom Project - AI Copilot Directives

## Project Overview

**Main Purpose:** ROM management and verification toolkit
- **Backend:** .NET 10 Web API with SignalR for real-time progress
- **Frontend:** React + TypeScript + Vite UI (`subrom-ui/`)
- DAT file parsing and ROM verification against No-Intro, TOSEC, GoodTools catalogs

**Home Folder:** `C:\Users\me\source\repos\subrom`

## Key Architecture Concepts

### Technology Stack
- **Backend:** C# .NET 10 Web API
  - SignalR for real-time streaming progress
  - EF Core + SQLite for data persistence
  - Domain-driven design with Services layer
- **Frontend:** React 19 + TypeScript + Vite
  - Zustand for state management
  - react-window for virtualized lists (60K+ entries)
  - FontAwesome icons
  - CSS Modules for styling

### Frontend Package Manager
- **ALWAYS use Yarn, NEVER npm or npx**
- Vite is the build tool, yarn is the package manager
- Commands:
  - `yarn install` - Install dependencies
  - `yarn add <package>` - Add a dependency
  - `yarn add -D <package>` - Add a dev dependency
  - `yarn dev` - Start development server
  - `yarn build` - Build for production
  - `yarn preview` - Preview production build

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
├── Subrom/                # Web API project
├── Domain/                # Domain models (DAT files, ROMs, Games)
├── Services/              # Business logic services
├── Infrastructure/        # Shared utilities
├── Compression/           # 7-Zip compression support
└── ConsoleTesting/        # Console test harness

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
dotnet run --project Subrom

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
  - `feat:` - New features
  - `fix:` - Bug fixes
  - `docs:` - Documentation
  - `style:` - Formatting/whitespace
  - `refactor:` - Code restructuring
  - `test:` - Tests
  - `chore:` - Maintenance

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
