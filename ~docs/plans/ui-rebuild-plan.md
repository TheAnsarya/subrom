# Subrom UI Rebuild Plan

## Overview

Complete rebuild of the React frontend using modern Vite tooling and current best practices.

## Current Issues

- Outdated Yarn 3 causing network stream errors
- TypeScript configuration targeting ES5 with missing downlevelIteration
- Missing dependencies (web-vitals)
- Legacy React patterns and code structure
- Mix of old and new component patterns

## Target Architecture

### Technology Stack

| Component | Technology | Version |
|-----------|------------|---------|
| Build Tool | Vite | 6.x |
| Framework | React | 19.x |
| Language | TypeScript | 5.9+ |
| Routing | React Router | 7.x |
| State Management | Zustand | 5.x |
| HTTP Client | Native fetch + custom hooks |
| Real-time | SignalR (@microsoft/signalr) |
| Styling | CSS Modules + CSS Variables |
| Icons | FontAwesome 6 |
| Package Manager | Yarn 4.x |

### Project Structure

```
subrom-ui/
├── .editorconfig          # K&R braces, tabs
├── .yarnrc.yml            # Yarn 4 config
├── index.html             # Entry HTML
├── package.json
├── tsconfig.json
├── vite.config.ts
├── public/
│   └── favicon.ico
└── src/
	├── main.tsx           # Entry point
	├── App.tsx            # Root component
	├── vite-env.d.ts
	├── api/               # API client layer
	│   ├── client.ts      # Base fetch wrapper
	│   ├── dats.ts        # DAT file API
	│   ├── roms.ts        # ROM file API
	│   ├── scan.ts        # Scanning API
	│   └── verification.ts # Verification API
	├── components/        # Reusable components
	│   ├── Layout/
	│   │   ├── Layout.tsx
	│   │   ├── Layout.module.css
	│   │   ├── Sidebar.tsx
	│   │   └── Header.tsx
	│   ├── DataTable/
	│   │   ├── DataTable.tsx
	│   │   └── DataTable.module.css
	│   ├── FileUpload/
	│   ├── ProgressBar/
	│   └── Modal/
	├── hooks/             # Custom React hooks
	│   ├── useApi.ts
	│   ├── useSignalR.ts
	│   └── useScanProgress.ts
	├── pages/             # Page components
	│   ├── Dashboard/
	│   ├── DatManager/
	│   ├── RomFiles/
	│   ├── Verification/
	│   └── Settings/
	├── stores/            # Zustand stores
	│   ├── appStore.ts
	│   ├── datStore.ts
	│   └── scanStore.ts
	├── styles/            # Global styles
	│   ├── variables.css
	│   ├── global.css
	│   └── themes.css
	└── types/             # TypeScript types
	    ├── api.ts
	    ├── dat.ts
	    └── rom.ts
```

### Coding Standards

- **Indentation**: TABS only (enforced by .editorconfig)
- **Brace Style**: K&R (opening brace on same line)
- **Components**: Functional with hooks
- **State**: Zustand for global, useState for local
- **Types**: Strict TypeScript, no `any`
- **Imports**: Absolute paths via `@/` alias
- **CSS**: CSS Modules with BEM-like naming

### Key Features

1. **Dashboard** - Overview stats, recent activity, quick actions
2. **DAT Manager** - Import/view/delete DAT files
3. **ROM Files** - Browse scanned ROMs, filter/search
4. **Verification** - Match ROMs to DATs, show status
5. **Settings** - Configure scan paths, preferences

## Implementation Phases

### Phase 1: Project Setup (Epic #100)
- Create new Vite project
- Configure TypeScript, ESLint
- Set up .editorconfig
- Configure path aliases
- Create base styles/themes

### Phase 2: Core Components (Epic #101)
- Layout (sidebar, header, content)
- DataTable with sorting/filtering
- Modal dialogs
- File upload component
- Progress indicators

### Phase 3: API Integration (Epic #102)
- Fetch wrapper with error handling
- API hooks for each endpoint
- SignalR connection for real-time
- Loading/error states

### Phase 4: Pages (Epic #103)
- Dashboard with stats
- DAT Manager with import
- ROM Files browser
- Verification view
- Settings page

### Phase 5: Polish (Epic #104)
- Dark/light theme toggle
- Responsive design
- Keyboard shortcuts
- Error boundaries
- Loading skeletons

## Timeline

| Phase | Estimated Duration |
|-------|-------------------|
| Phase 1 | 1 session |
| Phase 2 | 1-2 sessions |
| Phase 3 | 1 session |
| Phase 4 | 2-3 sessions |
| Phase 5 | 1 session |

## Success Criteria

- [ ] `yarn install` works without errors
- [ ] `yarn build` produces production bundle
- [ ] `yarn dev` starts dev server
- [ ] All pages render without errors
- [ ] API calls work with backend
- [ ] SignalR real-time updates work
- [ ] Responsive on desktop/tablet
- [ ] Dark/light themes work

## Migration Notes

### Files to Preserve
- `public/favicon.ico` (if exists)
- Any custom assets

### Files to Remove
- Old `src/` directory (backup first)
- Old `node_modules/`
- Old `yarn.lock`
- `.yarn/cache/`

## References

- [Vite Documentation](https://vitejs.dev/)
- [React 19 Documentation](https://react.dev/)
- [Zustand](https://github.com/pmndrs/zustand)
- [React Router 7](https://reactrouter.com/)
