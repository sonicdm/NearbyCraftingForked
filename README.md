# Nearby Crafting Forked

Nearby Crafting Forked lets you craft and build using materials stored in nearby player-built containers, so you do not need to move resources into your inventory first.

This is a source fork of **Nearby Crafting 1.2.1** (original by IPA38 / mikeg) with Quick Deposit exclusions and fuel/ore assist from nearby chests for smelters, kilns, fires and lights.

## Identity

| | |
| --- | --- |
| Author | SonicDM |
| Plugin name | `Nearby Crafting Forked` |
| Plugin GUID | `com.sonicdm.valheim.nearbycraftingforked` |
| Assembly | `NearbyCraftingForked.dll` |
| Version | `1.4.5` |
| Config file | `BepInEx/config/com.sonicdm.valheim.nearbycraftingforked.cfg` |

Do **not** run this fork at the same time as the original Nearby Crafting plugin. Both patch the same Valheim methods; use one or the other.

This may break other CraftFromChests-style plugins that also patch crafting, building, or station fueling.

## Features

- Craft using materials from nearby eligible containers.
- Build using materials from nearby eligible containers.
- Mass Quick Deposit sends matching inventory items to nearby chests with one configurable hotkey.
- Quick Deposit can be enabled or disabled independently from Nearby Crafting.
- Quick Deposit exclusions keep consumables, ammo, equipment and utility items in your inventory by default.
- Per-item exclude / allow lists with `*` wildcards (for example deposit mushrooms while keeping other food).
- Fuel smelters, charcoal kilns, blast furnaces, campfires, torches, braziers and similar stations from nearby chests on interact (uses each prefab’s own fuel item: Wood, Resin, Coal, Greydwarf eye, etc.).
- Optionally pull smelter/kiln ore and other cookable inputs from nearby chests.
- Containers are processed nearest-first.
- Configurable container range and player-built / moving-container filters.
- Compatibility support for BalrondConstructions storage layouts.
- Optional auto-reload when the `.cfg` file changes on disk (edit, save, apply in-game).

## Mass Quick Deposit

Press the configured hotkey to deposit matching items into eligible nearby chests.

- An item is only deposited into a chest that **already contains that item type**.
- If one matching chest cannot accept the full amount, the remaining items can continue to another matching chest.
- A HUD message reports how many items were deposited and how many chests received items.
- Only stackable items are deposited (max stack size greater than 1).
- Exclusion rules apply only to Quick Deposit. Nearby crafting and building can still pull materials from chests.

Default Quick Deposit settings:

```ini
[Quick Deposit]
Enabled = true
Hotkey = F6
```

## Quick Deposit exclusions

Defaults are intentionally conservative so a combat/food loadout stays on you:

```ini
[Quick Deposit - Exclusions]
ExcludeConsumables = true
ExcludeAmmo = true
ExcludeEquipment = true
ExcludeUtility = true
ExcludedItemTypes =
ExcludedItems =
AllowedItems =
```

| Setting | Purpose |
| --- | --- |
| `ExcludeConsumables` | Keep food, meads, potions, etc. |
| `ExcludeAmmo` | Keep arrows, bolts, and non-equipable ammo |
| `ExcludeEquipment` | Keep weapons, armor, shields, tools, torches |
| `ExcludeUtility` | Keep Utility item types |
| `ExcludedItemTypes` | Extra `ItemType` names to exclude (comma-separated, case-insensitive) |
| `ExcludedItems` | Force-exclude specific items by prefab / shared name |
| `AllowedItems` | Exceptions: still deposit these even if their type is excluded |

### Matching rules

- `ExcludedItems` and `AllowedItems` match the item **prefab name** and internal shared name (case-insensitive).
- Prefer prefab-style names such as `MushroomYellow`.
- `*` wildcards are supported: `Mushroom*`, `*Egg`, `Cooked*Meat*`.
- If an item appears in both lists, **`ExcludedItems` wins**.
- Exclusions do not affect crafting or building material pulls.

### Useful examples

With `ExcludeConsumables = true`, cooked meals, cauldron food, and meads stay on you. Use `AllowedItems` only for **surplus forage** you want dumped into chests. Prefab IDs below match the [Valheim wiki Item IDs](https://valheim.fandom.com/wiki/Item_IDs) / [Food](https://valheim.fandom.com/wiki/Food) lists.

#### Usually keep on you (default exclusions already cover these)

These are the foods you typically want for combat and progression — leave them excluded:

- Cooked station food (`CookedMeat`, `CookedDeerMeat`, `FishCooked`, `SerpentMeatCooked`, …)
- Cauldron / oven meals (`QueensJam`, `CarrotSoup`, `Sausages`, `Bread`, `BloodPudding`, Mistlands/Ashlands dishes, …)
- Meads and barley wine (`MeadHealthMinor`, `MeadStaminaMedium`, `MeadFrostResist`, `BarleyWine`, …) — wildcard `Mead*` / `BarleyWine*` if you ever allow consumables broadly and need to re-exclude them
- Mead bases (`MeadBase*`) if those show up as depositable stacks in your setup

Raw butcher meats (`RawMeat`, etc.) are normally **Material**, not Consumable, so they already deposit without an allow-list entry.

#### Early game — Meadows / Black Forest forage

Safe surplus to deposit while exploring (weak foods; also crafting ingredients, so keep a small pile if you are brewing):

```ini
AllowedItems = Mushroom,MushroomYellow,MushroomBlue,Raspberry,Blueberries,Honey
```

Or shorter:

```ini
AllowedItems = Mushroom*,Raspberry,Blueberries,Honey
```

Optional farm surplus (also used in recipes — only allow-deposit if you have plenty):

```ini
AllowedItems = Mushroom*,Raspberry,Blueberries,Honey,Carrot
```

#### Mid game — Mountains / Plains

Add cloudberries (Plains) and optional onion surplus (Mountain farm crop / soup ingredient):

```ini
AllowedItems = Mushroom*,Raspberry,Blueberries,Cloudberry,Honey,Carrot,Onion
```

#### Late game — Mistlands / Ashlands forage

These mushrooms/plants are edible **and** key cooking/eitr ingredients. Allow-deposit only when you are stocked:

```ini
# Mistlands
AllowedItems = Mushroom*,Raspberry,Blueberries,Cloudberry,Honey,MushroomMagecap,MushroomJotunPuffs

# Ashlands (add when relevant)
AllowedItems = Mushroom*,Raspberry,Blueberries,Cloudberry,Honey,MushroomMagecap,MushroomJotunPuffs,SmokePuff,Fiddlehead,Vineberry*
```

#### Trash consumables

Bukeperries (`Pukeberries`) are usually worth dumping:

```ini
AllowedItems = Mushroom*,Raspberry,Blueberries,Cloudberry,Honey,Pukeberries
```

#### Ammo + forage

```ini
ExcludeAmmo = true
AllowedItems = Mushroom*,Raspberry,Blueberries,Cloudberry,Honey,ArrowWood,ArrowFlint
```

#### Valuables / name families

```ini
ExcludedItems = Coins,DragonEgg
```

```ini
ExcludedItems = *Trophy*,Dragon*,*Egg
```

```ini
ExcludedItemTypes = Trophy,Misc
```

#### Starter preset — deposit early forage, keep meals/meads/gear

```ini
[Quick Deposit - Exclusions]
ExcludeConsumables = true
ExcludeAmmo = true
ExcludeEquipment = true
ExcludeUtility = true
ExcludedItemTypes =
ExcludedItems = Coins,DragonEgg
AllowedItems = Mushroom*,Raspberry,Blueberries,Cloudberry,Honey,Pukeberries
```

Expand `AllowedItems` as you progress (Carrot/Onion → Magecap/Jotun → Ashlands forage) when those stacks become clutter instead of ingredients you still need on hand.

## Station fuel and ore

When you interact with a fuel switch / fire / light and you do **not** already have the required item, the mod can pull **one** matching item from a nearby eligible chest into your inventory so vanilla can consume it.

This covers Valheim’s shared components (fuel type comes from the prefab, not a hardcoded list):

| Structures | Component | Typical fuels / inputs |
| --- | --- | --- |
| Smelter, charcoal kiln, blast furnace, eitr refinery | `Smelter` | Coal / Wood (fuel) + ores/scrap (optional ore assist) |
| Campfire, hearth, wood/iron torches, braziers (incl. blue) | `Fireplace` | Wood, Resin, Coal, Greydwarf eye, etc. |
| Cooking stations that use their own fuel | `CookingStation` | Prefab `m_fuelItem` |
| Shield generator | `ShieldGenerator` | Prefab `m_fuelItems` list |

Non-refillable / infinite-fuel lanterns and wisplights are left to vanilla. There is no background “keep everything lit” auto-refuel (client-only limitation).

Assist only runs for the local player and only when the station will actually accept fuel/ore (not full, not fireplace tap-to-toggle). Chest takes are best-effort on the client — same pattern as nearby craft consume — and can desync on some dedicated-server setups if another player is using the same chest.

```ini
[Station Fuel]
Enabled = true
EnableOreFromChests = true
```

## Configuration

Nearby Crafting Forked includes options for:

- Enabling or disabling the mod.
- Enabling or disabling nearby-resource building.
- Changing the nearby-container range.
- Ignoring moving containers (carts/ships).
- Ignoring Obliterators (default on).
- Requiring containers to be player-built.
- Enabling or disabling the crafting requirement indicator fix.
- Enabling or disabling Mass Quick Deposit.
- Changing the Mass Quick Deposit hotkey.
- Quick Deposit type exclusions and per-item exclude/allow lists.
- Station fuel/ore assist from nearby chests.
- Auto-reloading config when the `.cfg` changes on disk.
- Optional manual config-reload hotkey (unset by default).
- BalrondConstructions compatibility.
- Debug logging.

The configuration file is generated at:

```text
BepInEx/config/com.sonicdm.valheim.nearbycraftingforked.cfg
```

Settings can also be changed through a compatible mod-config interface (for example Official BepInEx Configuration Manager).

### Config reload

```ini
[General]
AutoReloadConfig = true
ReloadConfigHotkey =
```

- `AutoReloadConfig` (default `true`) watches the `.cfg` and reloads shortly after you save it.
- `ReloadConfigHotkey` is optional and **unset by default** so it does not conflict with other mods. Assign a free key if you want a manual force-reload.
- Container caches are invalidated after reload.

## Installation

### Mod manager (r2modman / Thunderstore)

1. Disable or uninstall the original **Nearby Crafting** package if it is present.
2. Install **Nearby Crafting Forked** (or import the local package / DLL into the profile).
3. Launch once so the config file is created, then edit exclusions as needed.
4. With `AutoReloadConfig = true`, saving the cfg applies changes without restarting Valheim.

Recommended r2modman plugin layout (same pattern as other managed mods):

```text
BepInEx/plugins/SonicDM-NearbyCraftingForked/
  NearbyCraftingForked.dll
  manifest.json
  README.md
  CHANGELOG.md
  icon.png
```

### Manual

1. Install BepInExPack for Valheim.
2. Place `NearbyCraftingForked.dll` in `BepInEx/plugins/` (or a subfolder as above).
3. Remove any original `NearbyCrafting.dll` from the profile.

## Notes

- Nearby Crafting Forked uses Valheim's recipe requirement logic so conditional recipe requirements introduced in Valheim 1.0 are handled correctly.
- Resource consumption can be split between the player's inventory and multiple eligible nearby containers.
- Obliterators (Incinerator) are ignored by default via `Containers.IgnoreObliterators` (Quick Deposit, crafting/building, and station fuel/ore assist).
- This may break other CraftFromChests-style plugins that also patch crafting, building, or station fueling.
- For troubleshooting, enable debug logging in the mod configuration and check `BepInEx/LogOutput.log`.
- Enable `DebugContainerDetails` only when needed; it is noisy.

## Credits

- Author: **SonicDM**
- Original mod: **Nearby Crafting** by IPA38 / mikeg (`com.mikeg.valheim.nearbycrafting`).
- This fork adds Quick Deposit exclusion controls, allow-list exceptions, wildcards, live config reload, and station fuel/ore from chests under a separate plugin identity.

## Build (developers)

Default Valheim / BepInEx reference folder:

```text
E:\Scripts\Valheim Mods\Reqs
```

```powershell
.\build.ps1
.\build.ps1 -Package
.\package.ps1
.\package.ps1 -SkipBuild
.\build.ps1 -LibDir "D:\Somewhere\ValheimRefs"
```

`package.ps1` builds a Thunderstore-compatible zip under `dist/` per [Thunderstore package docs](https://thunderstore.io/package/create/docs/):

- `manifest.json`, `README.md`, `icon.png` (256×256), optional `CHANGELOG.md`, and `NearbyCraftingForked.dll` at the **zip root**
- Validates manifest fields, UTF-8 readme, and icon dimensions before packing

## Release (developers)

GitHub-hosted Actions **cannot** compile this mod (Valheim `Managed` DLLs stay local). Releases are cut from your machine:

1. Bump `PluginVersion`, csproj `<Version>`, and `manifest.json` `version_number` together.
2. Add a `## X.Y.Z` section to `CHANGELOG.md`.
3. Package locally (`.\package.ps1`), then publish:

```powershell
.\release.ps1 -SkipPackage   # use existing dist\NearbyCraftingForked-X.Y.Z.zip
# or
.\release.ps1                # package then release
```

`release.ps1` validates versions, tags `vX.Y.Z`, pushes, and creates a GitHub Release whose body is the matching CHANGELOG section, with the Thunderstore zip attached. The `Release` workflow on tag push refreshes those notes if the tag lands first.
