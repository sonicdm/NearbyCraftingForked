# Changelog

## 1.4.5

- Prune destroyed containers from the trait cache on each nearby-container refresh.
- Station fuel/ore helpers use cached Harmony method delegates (no per-call `MethodInfo.Invoke` / `object[]` allocs).
- Quick Deposit equipment exclusions use `ItemType` enums (adds Hands + Trinket; avoids string drift).
- Removed unused `HaveAggregatedRequirements`.
- Cleaned Quick Deposit config indentation and simplified build `HaveRequirements` override logic.

## 1.4.4

- Show vanilla “no room” (`$msg_noroom`) when station fuel/ore assist finds the item in a chest but the player inventory cannot accept it.

## 1.4.3

- Station fuel/ore pull checks `CanAddItem` and rolls back to the chest if the player inventory cannot accept the item (avoids destroying a stack on a full inventory).
- Ore assist skips when the smelter/kiln queue is already full (same class of fix as fuel capacity in 1.4.2).
- `CraftingContext.Depth` pairs via Harmony `__state` so mid-craft disable/reload cannot leave consume intercept stuck on; shared `ResetRuntimeState()` on enable toggle, config reload, and destroy.
- Nested `HaveRequirementItems` no longer wipes outer `RecipeConsumptionRules`.
- Removed dead `Fireplace.UseItem` assist path (Interact handles empty-hand fueling).
- `package.ps1` asserts manifest version matches `PluginVersion` in source.

## 1.4.2

- Station fuel assist only pulls when the interaction will actually consume fuel (room for fuel; fireplace tap-to-toggle no longer pulls). Avoids stray fuel in inventory from rapid Use / macros on full or toggle-only lights.

## 1.4.1

- Fix station fuel assist for braziers, torches, campfires, and other `Fireplace` lights: chest lookup used `GetItem(..., isPrefabName: true)`, so shared names like `$item_resin` / `$item_coal` never matched. Charcoal kilns still worked because their wood input uses the ore/`FindCookableItem` path.
- Skip non-refillable / infinite-fuel fixtures.

## 1.4.0

- Station fuel assist: pull one required fuel item from nearby chests when interacting with smelters, kilns, blast furnaces, campfires, torches, braziers, cooking-station fuel switches, and shield generators (`[Station Fuel] Enabled`).
- Optional smelter/kiln ore (cookable input) assist (`EnableOreFromChests`).
- Fuel type comes from each prefab (`m_fuelItem` / `m_fuelItems`) — Wood, Resin, Coal, Greydwarf eye, etc.
- Intended to cover station fuel/ore from nearby chests so separate CraftFromChests-style fueling mods are unnecessary (those often collide on craft/build patches).

## 1.3.0

- Forked from supplied Nearby Crafting 1.2.1 decompile.
- New unique plugin GUID, name, namespace and assembly.
- Added configurable Quick Deposit exclusions for consumables, ammo, equipment and utility items.
- Added comma-separated custom ItemType exclusions.
- Added comma-separated specific item/prefab exclusions and allow-list exceptions (`AllowedItems`).
- Added `*` wildcard matching for item name lists.
- Added optional config auto-reload when the `.cfg` changes on disk.
- Exclusions affect Quick Deposit only.
- Thunderstore packaging via `package.ps1` (manifest, README, 256×256 icon, zip root layout).
- Obliterators (Incinerator) are excluded by default via `Containers.IgnoreObliterators` (configurable).
