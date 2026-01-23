# CI/CD Documentation

Subrom uses GitHub Actions for continuous integration and delivery. **All workflows are FREE for public repositories.**

## Workflows

### 1. CI (Continuous Integration)
**File:** `.github/workflows/ci.yml`  
**Triggers:** Push to `main`, Pull Requests to `main`

Runs automatically on every code change to ensure quality.

#### Jobs

| Job | Runner | Description |
|-----|--------|-------------|
| `build-and-test` | Ubuntu | Build solution, run 375 unit tests |
| `build-ui` | Ubuntu | Build React frontend with Yarn |
| `build-cross-platform` | Windows, Linux, macOS | Verify builds on all platforms |

#### Status Badge
```markdown
[![CI](https://github.com/TheAnsarya/subrom/actions/workflows/ci.yml/badge.svg)](https://github.com/TheAnsarya/subrom/actions/workflows/ci.yml)
```

---

### 2. Release (Build & Publish)
**File:** `.github/workflows/release.yml`  
**Triggers:** Push tags matching `v*` (e.g., `v1.2.0`)

Builds installers for all platforms and creates GitHub Release.

#### Jobs

| Job | Runner | Output |
|-----|--------|--------|
| `build-windows` | Windows | `Subrom-x.x.x-win-x64.zip` |
| `build-linux` | Ubuntu | `Subrom-x.x.x-linux-x64.tar.gz` |
| `build-macos` | macOS | `Subrom-x.x.x-osx-arm64.tar.gz`, `Subrom-x.x.x-osx-x64.tar.gz` |
| `release` | Ubuntu | Creates GitHub Release with all artifacts |

#### Creating a Release

```bash
# 1. Update version in CHANGELOG.md and version.json
# 2. Commit changes
git add .
git commit -m "chore: prepare release v1.2.0"

# 3. Create and push tag
git tag v1.2.0
git push origin main
git push origin v1.2.0
```

The workflow will automatically:
1. Build for all platforms
2. Create installers with install scripts
3. Upload to GitHub Releases
4. Generate release notes

---

### 3. Build Installers (Advanced)
**File:** `.github/workflows/build-installers.yml`  
**Triggers:** Push tags `v*`, Manual dispatch

More comprehensive installer builds including:
- Windows MSI (WiX Toolset)
- Linux DEB package
- Linux AppImage
- macOS PKG

> **Note:** Requires additional setup (WiX components, signing certificates).

---

## GitHub Actions Costs

### Public Repositories: **FREE** ✅
- Unlimited minutes for all workflows
- Free artifact storage (500 MB)
- No cost for any runner (Windows, Linux, macOS)

### Private Repositories
- 2,000 free minutes/month (Linux)
- macOS uses 10x minutes
- Windows uses 2x minutes

**Subrom is public, so all CI/CD is completely free!**

---

## Viewing Workflow Results

1. Go to [Actions tab](https://github.com/TheAnsarya/subrom/actions)
2. Click on a workflow run to see details
3. Download artifacts from completed builds

---

## Manual Workflow Triggers

Some workflows support manual dispatch:

1. Go to Actions → Select workflow
2. Click "Run workflow"
3. Fill in parameters (e.g., version number)
4. Click "Run workflow"

---

## Local Testing

Test the build locally before pushing:

```bash
# Run tests
dotnet test Subrom.sln

# Build for release
dotnet build Subrom.sln -c Release

# Build specific platform
dotnet publish src/Subrom.Server/Subrom.Server.csproj -c Release -r win-x64 --self-contained

# Build UI
cd subrom-ui
yarn install
yarn build
```

---

## Workflow Files Reference

| File | Purpose |
|------|---------|
| `.github/workflows/ci.yml` | Continuous integration (tests, builds) |
| `.github/workflows/release.yml` | Create releases with installers |
| `.github/workflows/build-installers.yml` | Advanced installer builds |

---

## Troubleshooting

### Workflow failed
1. Check the Actions tab for error logs
2. Click on the failed job
3. Expand the failed step to see details

### Build fails locally but passes in CI
- Different .NET SDK versions
- Different OS-specific behavior
- Missing dependencies

### Release not created
- Ensure tag matches `v*` pattern (e.g., `v1.2.0`)
- Check the `release` job for errors
- Verify repository permissions for creating releases
