# AGENTS.md — Nearby Crafting Forked

Guidance for Cursor agents (and humans) working in this repo.

## What this is

Valheim **BepInEx** mod: fork of Nearby Crafting with Quick Deposit exclusions and station fuel/ore from nearby chests.

| | |
| --- | --- |
| GUID | `com.sonicdm.valheim.nearbycraftingforked` |
| Assembly | `NearbyCraftingForked.dll` |
| Source | `src/NearbyCraftingForked.cs` (single file) |
| GitHub | https://github.com/sonicdm/NearbyCraftingForked |

Do **not** tell users to disable a specific third-party mod by name in public docs. Prefer a general note that CraftFromChests-style plugins may break.

## Valheim references (local only)

Build needs Valheim/BepInEx managed DLLs. Default path:

```text
E:\Scripts\Valheim Mods\Reqs
```

Override with `-LibDir` on `build.ps1` / `package.ps1` / `release.ps1`.

- **Never** commit those DLLs or the Reqs folder.
- **Never** expect GitHub-hosted Actions to compile this mod.
- GitHub Actions only refreshes **release notes** from `CHANGELOG.md` on tag push (`.github/workflows/release.yml`). The Thunderstore zip is attached by the local `release.ps1`.

## Build / package (session habits)

```powershell
.\build.ps1                          # DLL → bin\Release\ (+ copy to dist\NearbyCraftingForked.dll)
.\package.ps1                        # Thunderstore zip → dist\NearbyCraftingForked-<ver>.zip
.\package.ps1 -SkipBuild             # zip from existing Release DLL
.\build.ps1 -Package                 # build then package
```

During an active coding session:

- Prefer **build only** when iterating (`.\build.ps1`).
- Do **not** run `package.ps1` / create a new versioned zip for every tiny change.
- Package once when the user asks, or right before a release.

`package.ps1` asserts `manifest.json` `version_number` == `PluginVersion` in source == csproj `<Version>`.

## Version bumps

When shipping a new version, update **all** of these to the same `MAJOR.MINOR.PATCH`:

1. `src/NearbyCraftingForked.cs` → `PluginVersion`
2. `NearbyCraftingForked.csproj` → `<Version>`, `<AssemblyVersion>`, `<FileVersion>`
3. `manifest.json` → `version_number`
4. `README.md` → Version row in the Identity table
5. `CHANGELOG.md` → new `## X.Y.Z` section at the top (this becomes the GitHub release body)

Working tree must be **clean** before `release.ps1` (commit first).

## Release flow (reproduce this)

GitHub-hosted runners cannot build. Releases are cut **locally**:

```text
1. Bump versions + write CHANGELOG ## X.Y.Z
2. Commit and push to main (if needed)
3. .\package.ps1                    # or use an existing zip
4. .\release.ps1 -SkipPackage       # if zip already exists
   # or: .\release.ps1             # package then release
```

What `release.ps1` does:

1. Validates version alignment (manifest / PluginVersion / csproj)
2. Ensures `dist\NearbyCraftingForked-<ver>.zip` exists
3. Extracts the matching `## <ver>` block from `CHANGELOG.md` for release notes
4. Creates annotated tag `v<ver>`, pushes branch + tag
5. Creates or updates the GitHub Release and uploads the zip

Useful flags:

```powershell
.\release.ps1 -DryRun          # validate + print only
.\release.ps1 -SkipPackage     # use existing dist zip
.\release.ps1 -SkipPush        # local tag only; print push/gh commands
```

Docs-only changes (README wording, etc.): **commit + push `main`**, do **not** cut a new release unless the user asks.

## Install path (local testing)

r2modman profile plugin folder (typical):

```text
%AppData%\r2modmanPlus-local\Valheim\profiles\Default\BepInEx\plugins\SonicDM-NearbyCraftingForked\
```

Copy `bin\Release\NearbyCraftingForked.dll` there after build. If Valheim is running, the DLL is often locked — close the game first.

## Code / product notes

- Station fuel assist: pull **one** item from nearby eligible chests into the player inventory, then let vanilla consume it. Only when the interaction will actually accept fuel/ore (not full, not fireplace tap-to-toggle).
- Inventory full → show vanilla `$msg_noroom`; never destroy chest stacks (`CanAddItem` + rollback).
- `Inventory.GetItem(name, quality, isPrefabName)` — third arg is **`isPrefabName`**, not world-level. Shared names like `$item_resin` need **`false`**.
- Quick Deposit exclusions are independent of craft/build nearby logic.
- Do not run alongside the original Nearby Crafting GUID.

## Scripts map

| Script | Role |
| --- | --- |
| `build.ps1` | `dotnet build` against local Reqs |
| `package.ps1` | Thunderstore zip + validation |
| `release.ps1` | Tag + GitHub Release + attach zip |
| `.github/workflows/release.yml` | On `v*` tag: changelog notes only |

## Git

- Default branch: `main`
- Release tags: `vMAJOR.MINOR.PATCH` (must match `manifest.json`)
- Do not commit: `bin/`, `obj/`, `dist/`, `.package/`, Valheim refs, `.cursor/`
