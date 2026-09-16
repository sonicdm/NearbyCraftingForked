using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace NearbyCraftingForked
{
	[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
	public sealed class NearbyCraftingForkedPlugin : BaseUnityPlugin
	{
		public const string PluginGuid = "com.sonicdm.valheim.nearbycraftingforked";

		public const string PluginName = "Nearby Crafting Forked";

		public const string PluginVersion = "1.5.2";

		internal static ConfigEntry<float> Range;

		internal static ConfigEntry<bool> Enabled;

		internal static ConfigEntry<bool> EnableBuilding;

		internal static ConfigEntry<bool> IgnoreMovingContainers;

		internal static ConfigEntry<bool> IgnoreObliterators;

		internal static ConfigEntry<bool> RequirePlayerPlacedContainer;

		internal static ConfigEntry<bool> FixRequirementIndicator;

		internal static ConfigEntry<bool> BalrondCompatibility;

		internal static ConfigEntry<bool> DebugLogging;

		internal static ConfigEntry<bool> DebugContainerDetails;

		internal static ConfigEntry<bool> EnableMassDeposit;

		internal static ConfigEntry<KeyboardShortcut> MassDepositHotkey;

		internal static ConfigEntry<bool> QuickDepositHighlightChests;

		internal static ConfigEntry<string> QuickDepositHighlightColor;

		internal static ConfigEntry<float> QuickDepositHighlightAlpha;

		internal static ConfigEntry<float> QuickDepositHighlightDurationSeconds;

		internal static ConfigEntry<KeyboardShortcut> ReloadConfigHotkey;

		internal static ConfigEntry<bool> AutoReloadConfig;

		internal static ConfigEntry<bool> StationFuelEnabled;

		internal static ConfigEntry<bool> EnableOreFromChests;

		internal static ConfigEntry<bool> ItemLocateEnabled;

		internal static ConfigEntry<int> ItemLocateMaxHighlights;

		internal static ConfigEntry<float> ItemLocateDurationSeconds;

		internal static ConfigEntry<string> ItemLocateGlowColor;

		internal static ConfigEntry<float> ItemLocateGlowAlpha;

		internal static ConfigEntry<bool> QuickDepositExcludeConsumables;

		internal static ConfigEntry<bool> QuickDepositExcludeAmmo;

		internal static ConfigEntry<bool> QuickDepositExcludeEquipment;

		internal static ConfigEntry<bool> QuickDepositExcludeUtility;

		internal static ConfigEntry<string> QuickDepositExcludedItemTypes;

		internal static ConfigEntry<string> QuickDepositExcludedItems;

		internal static ConfigEntry<string> QuickDepositAllowedItems;

		internal static ManualLogSource ModLogger;

		private Harmony? _harmony;

		private DateTime _configLastWriteUtc;

		private float _configWatchNextCheck;

		private DateTime _configPendingWriteUtc;

		private float _configPendingSince = -1f;

		internal static bool DebugEnabled
		{
			get
			{
				if (DebugLogging != null)
				{
					return DebugLogging.Value;
				}
				return false;
			}
		}

		internal static bool ContainerDebugEnabled
		{
			get
			{
				if (DebugEnabled && DebugContainerDetails != null)
				{
					return DebugContainerDetails.Value;
				}
				return false;
			}
		}

		private void Awake()
		{
			//IL_0079: Unknown result type (might be due to invalid IL or missing references)
			//IL_0083: Expected O, but got Unknown
			//IL_0182: Unknown result type (might be due to invalid IL or missing references)
			//IL_0255: Unknown result type (might be due to invalid IL or missing references)
			//IL_025f: Expected O, but got Unknown
			//IL_034f: Unknown result type (might be due to invalid IL or missing references)
			ModLogger = Logger;
			Enabled = Config.Bind<bool>("General", "Enabled", true, "Enable Nearby Crafting.");
			EnableBuilding = Config.Bind<bool>("General", "EnableBuilding", true, "Allow building pieces to use materials from nearby containers.");
			Range = Config.Bind<float>("General", "ContainerRange", 20f, new ConfigDescription("Maximum distance in metres from the player to a container.", (AcceptableValueBase)(object)new AcceptableValueRange<float>(1f, 100f), Array.Empty<object>()));
			IgnoreMovingContainers = Config.Bind<bool>("Containers", "IgnoreMovingContainers", true, "Ignore ships, and ignore carts that are currently attached or in use. Parked/idle carts stay usable for craft, build, deposit, fuel, and locate.");
			IgnoreObliterators = Config.Bind<bool>("Containers", "IgnoreObliterators", true, "Ignore Obliterators (Incinerator). Prevents Quick Deposit and nearby crafting/building from using them.");
			RequirePlayerPlacedContainer = Config.Bind<bool>("Containers", "RequirePlayerPlacedContainer", true, "Only use containers attached to a Piece (normally player-built storage), avoiding most world loot chests.");
			FixRequirementIndicator = Config.Bind<bool>("UI", "FixRequirementIndicator", true, "Treat nearby-container materials as available when coloring crafting requirement indicators.");
			BalrondCompatibility = Config.Bind<bool>("Compatibility", "BalrondConstructions", true, "Recognize storage prefabs where Container and Piece are siblings under the same prefab root (used by BalrondConstructions and some other build mods).");
			DebugLogging = Config.Bind<bool>("Debug", "DebugLogging", false, "Write Nearby Crafting diagnostic information to BepInEx LogOutput.log.");
			DebugContainerDetails = Config.Bind<bool>("Debug", "DebugContainerDetails", false, "When DebugLogging is enabled, include per-container cache/scan details. This can be noisy.");
			EnableMassDeposit = Config.Bind<bool>("Quick Deposit", "Enabled", true, "Enable the nearby mass quick-deposit hotkey.");
			MassDepositHotkey = Config.Bind<KeyboardShortcut>("Quick Deposit", "Hotkey", new KeyboardShortcut((KeyCode)287, Array.Empty<KeyCode>()), "Hotkey used to quick-deposit matching inventory stacks into all eligible nearby containers. Default: F6.");
			QuickDepositHighlightChests = Config.Bind<bool>("Quick Deposit", "HighlightChests", false, "Glow chests that received items from Quick Deposit. Off by default. Color, opacity, and duration are HighlightColor / HighlightAlpha / HighlightDurationSeconds. Clear with 'nearby clear'.");
			QuickDepositHighlightColor = Config.Bind<string>("Quick Deposit", "HighlightColor", "#248038", "Glow color as hex (#248038) or RGB (36,128,56 or 0.14,0.50,0.22). rgb()/rgba() pastes work. Use HighlightAlpha for opacity.");
			QuickDepositHighlightAlpha = Config.Bind<float>("Quick Deposit", "HighlightAlpha", 1f, new ConfigDescription("Opacity of Quick Deposit chest glows. 0 is invisible, 1 is full intensity.", (AcceptableValueBase)(object)new AcceptableValueRange<float>(0.05f, 1f), Array.Empty<object>()));
			QuickDepositHighlightDurationSeconds = Config.Bind<float>("Quick Deposit", "HighlightDurationSeconds", 15f, new ConfigDescription("How long Quick Deposit chest glows last before auto-clear.", (AcceptableValueBase)(object)new AcceptableValueRange<float>(1f, 120f), Array.Empty<object>()));
			ReloadConfigHotkey = Config.Bind<KeyboardShortcut>("General", "ReloadConfigHotkey", KeyboardShortcut.Empty, "Optional hotkey to force-reload this mod's config from disk. Unset by default.");
			AutoReloadConfig = Config.Bind<bool>("General", "AutoReloadConfig", true, "Automatically reload this mod's config when the .cfg file changes on disk (edit in r2modman/Notepad, save, and it applies in-game).");
			QuickDepositExcludeConsumables = Config.Bind<bool>("Quick Deposit - Exclusions", "ExcludeConsumables", true, "Keep consumables such as food, meads and potions in the player inventory.");
			QuickDepositExcludeAmmo = Config.Bind<bool>("Quick Deposit - Exclusions", "ExcludeAmmo", true, "Keep ammunition such as arrows and bolts in the player inventory.");
			QuickDepositExcludeEquipment = Config.Bind<bool>("Quick Deposit - Exclusions", "ExcludeEquipment", true, "Keep weapons, armor, shields, tools, torches and other equipment in the player inventory.");
			QuickDepositExcludeUtility = Config.Bind<bool>("Quick Deposit - Exclusions", "ExcludeUtility", true, "Keep Utility item types in the player inventory.");
			QuickDepositExcludedItemTypes = Config.Bind<string>("Quick Deposit - Exclusions", "ExcludedItemTypes", "", "Additional ItemType names to exclude, comma-separated. Matching is case-insensitive. Example: Trophy,Misc");
			QuickDepositExcludedItems = Config.Bind<string>("Quick Deposit - Exclusions", "ExcludedItems", "", "Prefab, shared, or localized item names to exclude, comma-separated. Case-insensitive. * wildcards allowed. A leading * matches the display name and the first prefab word only — not descriptions or later words in a compound prefab (so *Berr* will not match Oatmeal). Example: Coins,DragonEgg,Oatmeal");
			QuickDepositAllowedItems = Config.Bind<string>("Quick Deposit - Exclusions", "AllowedItems", "", "Exceptions to exclusions: these items still quick-deposit even if their ItemType is excluded. Prefab, shared, or localized names, case-insensitive, * wildcards allowed. A leading * matches display name / first prefab word only (so *Berr* deposits berries, not Oatmeal). Example: Mushroom*,Honey,*Berr*");
			StationFuelEnabled = Config.Bind<bool>("Station Fuel", "Enabled", true, "Pull fuel (and smelter ore) from nearby eligible containers when interacting with smelters, kilns, fires, torches, braziers, and similar stations.");
			EnableOreFromChests = Config.Bind<bool>("Station Fuel", "EnableOreFromChests", true, "When Station Fuel is enabled, also pull smelter/kiln/blast-furnace cookable inputs (ore/scrap) from nearby containers.");
			ItemLocateEnabled = Config.Bind<bool>("Item Locate", "Enabled", true, "Enable the 'nearby' console/chat command to glow eligible chests that contain an item.");
			ItemLocateMaxHighlights = Config.Bind<int>("Item Locate", "MaxHighlights", 10, new ConfigDescription("Maximum chests to glow (nearest first; farther matches are culled).", (AcceptableValueBase)(object)new AcceptableValueRange<int>(1, 50), Array.Empty<object>()));
			ItemLocateDurationSeconds = Config.Bind<float>("Item Locate", "DurationSeconds", 15f, new ConfigDescription("How long chest glows last before auto-clear.", (AcceptableValueBase)(object)new AcceptableValueRange<float>(3f, 120f), Array.Empty<object>()));
			ItemLocateGlowColor = Config.Bind<string>("Item Locate", "GlowColor", "#8C731A", "Glow color as hex (#8C731A) or RGB (140,115,26 or 0.55,0.45,0.10). rgb()/rgba() pastes work. Use GlowAlpha for opacity.");
			ItemLocateGlowAlpha = Config.Bind<float>("Item Locate", "GlowAlpha", 1f, new ConfigDescription("Opacity of item-locate chest glows. 0 is invisible, 1 is full intensity.", (AcceptableValueBase)(object)new AcceptableValueRange<float>(0.05f, 1f), Array.Empty<object>()));
			Range.SettingChanged += OnContainerSettingChanged;
			IgnoreMovingContainers.SettingChanged += OnContainerSettingChanged;
			IgnoreObliterators.SettingChanged += OnContainerSettingChanged;
			RequirePlayerPlacedContainer.SettingChanged += OnContainerSettingChanged;
			BalrondCompatibility.SettingChanged += OnContainerSettingChanged;
			Enabled.SettingChanged += OnEnabledChanged;
			_configLastWriteUtc = GetConfigWriteTimeUtc();
			_configWatchNextCheck = 0f;
			_harmony = new Harmony(PluginGuid);
			try
			{
				_harmony.PatchAll();
				ItemLocate.RegisterCommand();
				Logger.LogInfo((object)PluginName + " " + PluginVersion + " loaded.");
				if (DebugEnabled)
				{
					Debug($"Config: Enabled={Enabled.Value}, EnableBuilding={EnableBuilding.Value}, Range={Range.Value:0.##}m, " + $"RequirePlayerPlacedContainer={RequirePlayerPlacedContainer.Value}, " + $"IgnoreMovingContainers={IgnoreMovingContainers.Value}, IgnoreObliterators={IgnoreObliterators.Value}, " + $"FixRequirementIndicator={FixRequirementIndicator.Value}, " + $"BalrondCompatibility={BalrondCompatibility.Value}, " + $"EnableMassDeposit={EnableMassDeposit.Value}, MassDepositHotkey={MassDepositHotkey.Value}, " + $"QDHighlight={QuickDepositHighlightChests.Value}, " + $"StationFuel={StationFuelEnabled.Value}, OreFromChests={EnableOreFromChests.Value}, " + $"ItemLocate={ItemLocateEnabled.Value}, " + $"DebugContainerDetails={DebugContainerDetails.Value}");
				}
			}
			catch (Exception ex)
			{
				try
				{
					_harmony.UnpatchSelf();
				}
				catch
				{
				}
				Enabled.Value = false;
				Logger.LogError((object)PluginName + " failed to apply its Harmony patches and has been disabled.");
				Logger.LogError((object)ex);
			}
		}

		private static void OnContainerSettingChanged(object? sender, EventArgs e)
		{
			NearbyContainers.InvalidateAll();
			if (DebugEnabled)
			{
				Debug("Container-related configuration changed; caches invalidated.");
			}
		}

		private static void OnEnabledChanged(object? sender, EventArgs e)
		{
			ResetRuntimeState();
		}

		internal static void ResetRuntimeState()
		{
			NearbyContainers.InvalidateAll();
			RequirementUiCache.Clear();
			BuildingPlacementState.Clear();
			RecipeRequirementContext.Clear();
			RecipeConsumptionRules.Clear();
			CraftingContext.Clear();
			ItemLocate.ClearHighlights();
		}

		internal static void Debug(string message)
		{
			if (DebugEnabled)
			{
				ModLogger.LogInfo((object)("[DEBUG] " + message));
			}
		}

		internal static void DebugContainer(string message)
		{
			if (ContainerDebugEnabled)
			{
				ModLogger.LogInfo((object)("[DEBUG-CONTAINER] " + message));
			}
		}

		private void Update()
		{
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			WatchConfigFileForChanges();
			ItemLocate.Tick();
			if (ReloadConfigHotkey != null)
			{
				KeyboardShortcut reloadShortcut = ReloadConfigHotkey.Value;
				if (!reloadShortcut.Equals(KeyboardShortcut.Empty) && reloadShortcut.IsDown())
				{
					ReloadConfigFromDisk(notifyPlayer: true);
				}
			}
			if (Enabled == null || !Enabled.Value || EnableMassDeposit == null || !EnableMassDeposit.Value || MassDepositHotkey == null)
			{
				return;
			}
			KeyboardShortcut value = MassDepositHotkey.Value;
			if (value.IsDown())
			{
				Player localPlayer = Player.m_localPlayer;
				if (!((Object)(object)localPlayer == (Object)null))
				{
					MassQuickDeposit(localPlayer);
				}
			}
		}

		private void WatchConfigFileForChanges()
		{
			if (AutoReloadConfig == null || !AutoReloadConfig.Value)
			{
				_configPendingSince = -1f;
				return;
			}
			float now = Time.unscaledTime;
			if (now < _configWatchNextCheck)
			{
				return;
			}
			_configWatchNextCheck = now + 1f;
			DateTime writeUtc = GetConfigWriteTimeUtc();
			if (writeUtc == DateTime.MinValue)
			{
				return;
			}
			if (writeUtc == _configLastWriteUtc)
			{
				_configPendingSince = -1f;
				return;
			}
			if (_configPendingSince < 0f || writeUtc != _configPendingWriteUtc)
			{
				_configPendingWriteUtc = writeUtc;
				_configPendingSince = now;
				return;
			}
			// Debounce ~0.75s so editors that rewrite the file mid-save don't reload a partial cfg.
			if (now - _configPendingSince < 0.75f)
			{
				return;
			}
			_configLastWriteUtc = writeUtc;
			_configPendingSince = -1f;
			ReloadConfigFromDisk(notifyPlayer: false);
		}

		private DateTime GetConfigWriteTimeUtc()
		{
			try
			{
				string path = Config.ConfigFilePath;
				if (string.IsNullOrEmpty(path) || !File.Exists(path))
				{
					return DateTime.MinValue;
				}
				return File.GetLastWriteTimeUtc(path);
			}
			catch
			{
				return DateTime.MinValue;
			}
		}

		private void ReloadConfigFromDisk(bool notifyPlayer)
		{
			try
			{
				Config.Reload();
				_configLastWriteUtc = GetConfigWriteTimeUtc();
				_configPendingSince = -1f;
				ResetRuntimeState();
				Logger.LogInfo((object)(PluginName + " config reloaded from disk."));
				if (notifyPlayer)
				{
					Player localPlayer = Player.m_localPlayer;
					if (!((Object)(object)localPlayer == (Object)null))
					{
						((Character)localPlayer).Message((MessageHud.MessageType)2, "Nearby Crafting: config reloaded", 0, (Sprite)null, false);
					}
				}
			}
			catch (Exception ex)
			{
				Logger.LogError((object)(PluginName + " failed to reload config from disk."));
				Logger.LogError((object)ex);
			}
		}

		private static void MassQuickDeposit(Player player)
		{
			Inventory inventory = ((Humanoid)player).GetInventory();
			if (inventory == null)
			{
				return;
			}
			List<Container> list = NearbyContainers.Get(player);
			if (list.Count == 0)
			{
				if (DebugEnabled)
				{
					Debug("Mass quick-deposit: no eligible nearby containers.");
				}
				return;
			}
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			List<Container> deposited = new List<Container>();
			for (int i = 0; i < list.Count; i++)
			{
				Container val = list[i];
				if ((Object)(object)val == (Object)null)
				{
					continue;
				}
				try
				{
					Inventory inventory2 = val.GetInventory();
					if (inventory2 != null && inventory2 != inventory)
					{
						int num4 = QuickDepositIntoContainer(inventory, inventory2);
						num++;
						num3 += num4;
						if (num4 > 0)
						{
							num2++;
							deposited.Add(val);
						}
						if (ContainerDebugEnabled)
						{
							DebugContainer($"Mass quick-deposit processed '{((Object)val).name}': moved={num4}.");
						}
					}
				}
				catch (Exception ex)
				{
					if (DebugEnabled)
					{
						Debug("Mass quick-deposit skipped '" + ((Object)val).name + "' after " + ex.GetType().Name + ": " + ex.Message);
					}
				}
			}
			NearbyContainers.InvalidateCounts();
			if (DebugEnabled)
			{
				Debug($"Mass quick-deposit completed across {num} eligible container(s); receiving={num2}; moved={num3} total item(s).");
			}
			string text = ((num3 > 0) ? string.Format("Quick Deposit: {0} item{1} deposited into {2} chest{3}", num3, (num3 == 1) ? "" : "s", num2, (num2 == 1) ? "" : "s") : "Quick Deposit: nothing to deposit");
			((Character)player).Message((MessageHud.MessageType)2, text, 0, (Sprite)null, false);
			if (QuickDepositHighlightChests != null && QuickDepositHighlightChests.Value && deposited.Count > 0)
			{
				ItemLocate.HighlightContainers(
					deposited,
					QuickDepositHighlightColor?.Value,
					new Color(0.14f, 0.50f, 0.22f, 1f),
					QuickDepositHighlightDurationSeconds != null ? QuickDepositHighlightDurationSeconds.Value : 15f,
					QuickDepositHighlightAlpha != null ? QuickDepositHighlightAlpha.Value : 1f);
			}
		}

		private static int QuickDepositIntoContainer(Inventory sourceInventory, Inventory targetInventory)
		{
			int num = 0;
			List<ItemDrop.ItemData> list = new List<ItemDrop.ItemData>(targetInventory.GetAllItems());
			List<ItemDrop.ItemData> list2 = new List<ItemDrop.ItemData>(sourceInventory.GetAllItems());
			for (int i = 0; i < list2.Count; i++)
			{
				ItemDrop.ItemData val = list2[i];
				if (val == null || val.m_shared == null || val.m_stack <= 0 || val.m_shared.m_maxStackSize <= 1)
				{
					continue;
				}
				if (IsQuickDepositExcluded(val))
				{
					if (ContainerDebugEnabled)
					{
						DebugContainer($"Quick-deposit excluded '{val.m_shared.m_name}' ({val.m_shared.m_itemType}).");
					}
					continue;
				}
				List<ItemDrop.ItemData> list3 = new List<ItemDrop.ItemData>();
				for (int j = 0; j < list.Count; j++)
				{
					ItemDrop.ItemData val2 = list[j];
					if (CanQuickStackTogether(val, val2))
					{
						list3.Add(val2);
					}
				}
				if (list3.Count == 0)
				{
					continue;
				}
				for (int k = 0; k < list3.Count; k++)
				{
					if (val.m_stack <= 0)
					{
						break;
					}
					ItemDrop.ItemData val3 = list3[k];
					if (val3 == null || val3.m_shared == null)
					{
						continue;
					}
					int num2 = val3.m_shared.m_maxStackSize - val3.m_stack;
					if (num2 > 0)
					{
						int num3 = Math.Min(val.m_stack, num2);
						int stack = val.m_stack;
						targetInventory.MoveItemToThis(sourceInventory, val, num3, val3.m_gridPos.x, val3.m_gridPos.y);
						int num4 = Math.Max(0, stack - val.m_stack);
						num += num4;
						if (ContainerDebugEnabled && num4 > 0)
						{
							DebugContainer($"Quick-deposit merged {num4}x '{val.m_shared.m_name}' into existing stack at {val3.m_gridPos.x},{val3.m_gridPos.y}.");
						}
					}
				}
				while (val.m_stack > 0)
				{
					int num5 = -1;
					int num6 = -1;
					for (int l = 0; l < targetInventory.GetHeight(); l++)
					{
						if (num5 >= 0)
						{
							break;
						}
						for (int m = 0; m < targetInventory.GetWidth(); m++)
						{
							if (targetInventory.GetItemAt(m, l) == null)
							{
								num5 = m;
								num6 = l;
								break;
							}
						}
					}
					if (num5 < 0 || num6 < 0)
					{
						break;
					}
					int num7 = Math.Min(val.m_stack, val.m_shared.m_maxStackSize);
					int stack2 = val.m_stack;
					targetInventory.MoveItemToThis(sourceInventory, val, num7, num5, num6);
					int num8 = Math.Max(0, stack2 - val.m_stack);
					if (num8 <= 0)
					{
						break;
					}
					num += num8;
					if (ContainerDebugEnabled)
					{
						DebugContainer($"Quick-deposit moved {num8}x '{val.m_shared.m_name}' into empty slot {num5},{num6}.");
					}
				}
			}
			return num;
		}


		private static bool IsQuickDepositExcluded(ItemDrop.ItemData item)
		{
			if (item == null || item.m_shared == null)
			{
				return false;
			}

			if (QuickDepositExcludedItems != null && ItemMatchesCsv(item, QuickDepositExcludedItems.Value))
			{
				return true;
			}

			if (QuickDepositAllowedItems != null && ItemMatchesCsv(item, QuickDepositAllowedItems.Value))
			{
				if (ContainerDebugEnabled)
				{
					DebugContainer($"Quick-deposit allowed by exception '{item.m_shared.m_name}' ({item.m_shared.m_itemType}).");
				}
				return false;
			}

			ItemDrop.ItemData.ItemType itemType = item.m_shared.m_itemType;

			if (QuickDepositExcludeConsumables != null && QuickDepositExcludeConsumables.Value &&
				itemType == ItemDrop.ItemData.ItemType.Consumable)
			{
				return true;
			}

			if (QuickDepositExcludeAmmo != null && QuickDepositExcludeAmmo.Value &&
				(itemType == ItemDrop.ItemData.ItemType.Ammo || itemType == ItemDrop.ItemData.ItemType.AmmoNonEquipable))
			{
				return true;
			}

			if (QuickDepositExcludeUtility != null && QuickDepositExcludeUtility.Value &&
				itemType == ItemDrop.ItemData.ItemType.Utility)
			{
				return true;
			}

			if (QuickDepositExcludeEquipment != null && QuickDepositExcludeEquipment.Value && IsEquipmentItemType(itemType))
			{
				return true;
			}

			if (QuickDepositExcludedItemTypes != null && CsvContains(QuickDepositExcludedItemTypes.Value, itemType.ToString()))
			{
				return true;
			}

			return false;
		}

		private static bool ItemMatchesCsv(ItemDrop.ItemData item, string csv)
		{
			if (string.IsNullOrWhiteSpace(csv) || item?.m_shared == null)
			{
				return false;
			}

			string[] patterns = csv.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < patterns.Length; i++)
			{
				string pattern = patterns[i].Trim();
				if (pattern.Length > 0 && ItemMatchesFilterPattern(item, pattern))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Quick Deposit name lists match identity fields only (localized display name, shared
		/// token, prefab). Item descriptions are never searched. Patterns that start with '*'
		/// (e.g. *Berr*) use the display name and the leading CamelCase prefab token so a
		/// compound id like OatmealLingonberryJam is not treated as a berry.
		/// </summary>
		private static bool ItemMatchesFilterPattern(ItemDrop.ItemData item, string pattern)
		{
			string sharedName = item.m_shared.m_name;
			string localized = ItemLocate.GetLocalizedName(sharedName);
			string prefabName = null;
			if ((Object)(object)item.m_dropPrefab != (Object)null)
			{
				prefabName = ((Object)item.m_dropPrefab).name;
			}

			string leadingPrefab = GetLeadingNameToken(prefabName);
			bool infixOrSuffix = pattern[0] == '*';

			if (!string.IsNullOrEmpty(localized) && !IsCompoundPrefabDump(localized, prefabName) && NameMatches(localized, pattern))
			{
				return true;
			}

			if (!string.IsNullOrEmpty(leadingPrefab) && NameMatches(leadingPrefab, pattern))
			{
				return true;
			}

			if (infixOrSuffix)
			{
				return false;
			}

			if (!string.IsNullOrEmpty(sharedName) && NameMatches(sharedName, pattern))
			{
				return true;
			}

			if (!string.IsNullOrEmpty(sharedName) && sharedName[0] == '$' && NameMatches(sharedName.Substring(1), pattern))
			{
				return true;
			}

			if (!string.IsNullOrEmpty(prefabName) && NameMatches(prefabName, pattern))
			{
				return true;
			}

			return false;
		}

		private static string GetLeadingNameToken(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return string.Empty;
			}

			int i = 1;
			while (i < name.Length)
			{
				char c = name[i];
				if (c == '_' || c == '-' || c == ' ')
				{
					break;
				}
				if (char.IsUpper(c))
				{
					break;
				}
				i++;
			}

			return name.Substring(0, i);
		}

		private static bool IsCompoundPrefabDump(string candidate, string prefabName)
		{
			if (string.IsNullOrEmpty(candidate) || string.IsNullOrEmpty(prefabName))
			{
				return false;
			}

			string leading = GetLeadingNameToken(prefabName);
			if (leading.Length >= prefabName.Length)
			{
				return false;
			}

			string compact = candidate.Replace(" ", string.Empty);
			return string.Equals(compact, prefabName, StringComparison.OrdinalIgnoreCase);
		}

		private static bool IsEquipmentItemType(ItemDrop.ItemData.ItemType itemType)
		{
			switch (itemType)
			{
				case ItemDrop.ItemData.ItemType.OneHandedWeapon:
				case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
				case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
				case ItemDrop.ItemData.ItemType.Bow:
				case ItemDrop.ItemData.ItemType.Shield:
				case ItemDrop.ItemData.ItemType.Helmet:
				case ItemDrop.ItemData.ItemType.Chest:
				case ItemDrop.ItemData.ItemType.Legs:
				case ItemDrop.ItemData.ItemType.Hands:
				case ItemDrop.ItemData.ItemType.Shoulder:
				case ItemDrop.ItemData.ItemType.Tool:
				case ItemDrop.ItemData.ItemType.Torch:
				case ItemDrop.ItemData.ItemType.Trinket:
					return true;
				default:
					return false;
			}
		}

		private static bool CsvContains(string csv, string value)
		{
			if (string.IsNullOrWhiteSpace(csv) || string.IsNullOrWhiteSpace(value))
			{
				return false;
			}

			string[] array = csv.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i].Trim();
				if (text.Length > 0 && NameMatches(value, text))
				{
					return true;
				}
			}
			return false;
		}

		internal static bool NameMatches(string value, string pattern)
		{
		    if (string.IsNullOrEmpty(pattern))
		    {
				return false;
		    }
		    if (pattern.IndexOf('*') < 0)
		    {
				return string.Equals(value, pattern, StringComparison.OrdinalIgnoreCase);
		    }

		    // Simple glob: * matches any sequence (including empty). Case-insensitive.
		    string[] parts = pattern.Split('*');
		    int index = 0;
		    for (int i = 0; i < parts.Length; i++)
		    {
				string part = parts[i];
				if (part.Length == 0)
				{
				    continue;
				}
				if (i == 0)
				{
				    if (!value.StartsWith(part, StringComparison.OrdinalIgnoreCase))
				    {
						return false;
				    }
				    index = part.Length;
				    continue;
				}
				bool isLast = i == parts.Length - 1;
				if (isLast && pattern[pattern.Length - 1] != '*')
				{
				    if (!value.EndsWith(part, StringComparison.OrdinalIgnoreCase))
				    {
						return false;
				    }
				    return value.Length - part.Length >= index;
				}
				int found = value.IndexOf(part, index, StringComparison.OrdinalIgnoreCase);
				if (found < 0)
				{
				    return false;
				}
				index = found + part.Length;
		    }
		    return true;
		}

		private static bool CanQuickStackTogether(ItemDrop.ItemData source, ItemDrop.ItemData target)
		{
			if (source == null || target == null || source.m_shared == null || target.m_shared == null)
			{
				return false;
			}
			if (source.m_shared.m_name != target.m_shared.m_name || source.m_quality != target.m_quality || source.m_variant != target.m_variant || source.m_worldLevel != target.m_worldLevel)
			{
				return false;
			}
			if (source.m_customData.Count != target.m_customData.Count)
			{
				return false;
			}
			foreach (KeyValuePair<string, string> customDatum in source.m_customData)
			{
				if (!target.m_customData.TryGetValue(customDatum.Key, out var value) || value != customDatum.Value)
				{
					return false;
				}
			}
			return true;
		}

		private void OnDestroy()
		{
			Range.SettingChanged -= OnContainerSettingChanged;
			IgnoreMovingContainers.SettingChanged -= OnContainerSettingChanged;
			IgnoreObliterators.SettingChanged -= OnContainerSettingChanged;
			RequirePlayerPlacedContainer.SettingChanged -= OnContainerSettingChanged;
			BalrondCompatibility.SettingChanged -= OnContainerSettingChanged;
			Enabled.SettingChanged -= OnEnabledChanged;
			ResetRuntimeState();
			Harmony? harmony = _harmony;
			if (harmony != null)
			{
				harmony.UnpatchSelf();
			}
		}
	}
	internal static class CraftingContext
	{
		internal static int Depth;

		internal static bool IsCrafting => Depth > 0;

		internal static void Clear()
		{
			Depth = 0;
		}
	}
	internal static class RecipeRequirementContext
	{
		internal static Player? Player;

		internal static Inventory? PlayerInventory;

		internal static int Depth;

		internal static bool IsActive
		{
			get
			{
				if (Depth > 0 && (Object)(object)Player != (Object)null)
				{
					return PlayerInventory != null;
				}
				return false;
			}
		}

		internal static void Begin(Player player, bool discover)
		{
			if (!discover && !((Object)(object)player == (Object)null))
			{
				if (Depth == 0)
				{
					Player = player;
					PlayerInventory = ((Humanoid)player).GetInventory();
				}
				Depth++;
			}
		}

		internal static void End()
		{
			if (Depth > 0)
			{
				Depth--;
				if (Depth == 0)
				{
					Player = null;
					PlayerInventory = null;
				}
			}
		}

		internal static void Clear()
		{
			Depth = 0;
			Player = null;
			PlayerInventory = null;
		}
	}
	internal readonly struct RecipeConsumptionKey : IEquatable<RecipeConsumptionKey>
	{
		internal readonly Piece.Requirement[] Requirements;

		internal readonly int QualityLevel;

		internal RecipeConsumptionKey(Piece.Requirement[] requirements, int qualityLevel)
		{
			Requirements = requirements;
			QualityLevel = qualityLevel;
		}

		public bool Equals(RecipeConsumptionKey other)
		{
			if (Requirements == other.Requirements)
			{
				return QualityLevel == other.QualityLevel;
			}
			return false;
		}

		public override bool Equals(object? obj)
		{
			if (obj is RecipeConsumptionKey other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (RuntimeHelpers.GetHashCode(Requirements) * 397) ^ QualityLevel;
		}
	}
	internal static class RecipeConsumptionRules
	{
		private static readonly Dictionary<RecipeConsumptionKey, HashSet<string>> AllowedNames = new Dictionary<RecipeConsumptionKey, HashSet<string>>();

		private static HashSet<string>? _currentNames;

		private static Piece.Requirement[]? _currentRequirements;

		private static int _currentQualityLevel;

		private static int _beginDepth;

		internal static void Begin(Recipe recipe, int qualityLevel)
		{
			if (_beginDepth > 0)
			{
				_beginDepth++;
				return;
			}

			_currentRequirements = recipe?.m_resources;
			_currentQualityLevel = qualityLevel;
			_currentNames = new HashSet<string>(StringComparer.Ordinal);
			_beginDepth = 1;
		}

		internal static void Record(string name)
		{
			if (_currentNames != null && !string.IsNullOrEmpty(name))
			{
				_currentNames.Add(name);
			}
		}

		internal static void Complete(bool success)
		{
			if (_beginDepth > 1)
			{
				_beginDepth--;
				return;
			}

			if (_beginDepth == 1 && success && _currentRequirements != null && _currentNames != null)
			{
				RecipeConsumptionKey key = new RecipeConsumptionKey(_currentRequirements, _currentQualityLevel);
				AllowedNames[key] = new HashSet<string>(_currentNames, StringComparer.Ordinal);
				if (NearbyCraftingForkedPlugin.DebugEnabled)
				{
					NearbyCraftingForkedPlugin.Debug($"Recorded {AllowedNames[key].Count} vanilla-consumed requirement name(s) " + $"for quality {_currentQualityLevel}.");
				}
			}
			_currentRequirements = null;
			_currentNames = null;
			_currentQualityLevel = 0;
			_beginDepth = 0;
		}

		internal static bool TryGet(Piece.Requirement[] requirements, int qualityLevel, out HashSet<string>? allowedNames)
		{
			if (requirements != null && AllowedNames.TryGetValue(new RecipeConsumptionKey(requirements, qualityLevel), out HashSet<string> value))
			{
				allowedNames = value;
				return true;
			}
			allowedNames = null;
			return false;
		}

		internal static void Clear()
		{
			AllowedNames.Clear();
			_currentRequirements = null;
			_currentNames = null;
			_currentQualityLevel = 0;
			_beginDepth = 0;
		}
	}
	internal static class BuildingPlacementState
	{
		private static Player? _lastPlacedPlayer;

		private static int _lastPlacedFrame = -1;

		internal static void MarkPlaced(Player player, bool placed)
		{
			if (placed && !((Object)(object)player == (Object)null))
			{
				_lastPlacedPlayer = player;
				_lastPlacedFrame = Time.frameCount;
				if (NearbyCraftingForkedPlugin.DebugEnabled)
				{
					NearbyCraftingForkedPlugin.Debug($"Successful build placement marked for frame {_lastPlacedFrame}.");
				}
			}
		}

		internal static bool WasPlacedThisFrame(Player player)
		{
			if ((Object)(object)player != (Object)null && (Object)(object)_lastPlacedPlayer == (Object)(object)player)
			{
				return _lastPlacedFrame == Time.frameCount;
			}
			return false;
		}

		internal static void Clear()
		{
			_lastPlacedPlayer = null;
			_lastPlacedFrame = -1;
		}
	}
	internal readonly struct ItemCountKey : IEquatable<ItemCountKey>
	{
		internal readonly string Name;

		internal readonly int Quality;

		internal ItemCountKey(string name, int quality)
		{
			Name = name;
			Quality = quality;
		}

		public bool Equals(ItemCountKey other)
		{
			if (Quality == other.Quality)
			{
				return string.Equals(Name, other.Name, StringComparison.Ordinal);
			}
			return false;
		}

		public override bool Equals(object? obj)
		{
			if (obj is ItemCountKey other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (((Name != null) ? StringComparer.Ordinal.GetHashCode(Name) : 0) * 397) ^ Quality;
		}
	}
	internal readonly struct CachedCount
	{
		internal readonly int Value;

		internal readonly float CreatedAt;

		internal readonly int ContainerGeneration;

		internal CachedCount(int value, float createdAt, int containerGeneration)
		{
			Value = value;
			CreatedAt = createdAt;
			ContainerGeneration = containerGeneration;
		}
	}
	internal readonly struct ContainerTraits
	{
		internal readonly Container Container;

		internal readonly bool HasPiece;

		internal readonly bool IsMoving;

		internal readonly bool IsIncinerator;

		internal ContainerTraits(Container container, bool hasPiece, bool isMoving, bool isIncinerator)
		{
			Container = container;
			HasPiece = hasPiece;
			IsMoving = isMoving;
			IsIncinerator = isIncinerator;
		}
	}
	internal readonly struct ContainerDistance
	{
		internal readonly Container Container;

		internal readonly float DistanceSq;

		internal ContainerDistance(Container container, float distanceSq)
		{
			Container = container;
			DistanceSq = distanceSq;
		}
	}
	internal static class NearbyContainers
	{
		private const float ContainerCacheLifetime = 0.5f;

		private const float CountCacheLifetime = 0.1f;

		private const float MovementRefreshDistanceSq = 1f;

		private static readonly List<Container> CachedContainers = new List<Container>();

		private static readonly List<ContainerDistance> ScanBuffer = new List<ContainerDistance>();

		private static readonly Dictionary<ItemCountKey, CachedCount> CountCache = new Dictionary<ItemCountKey, CachedCount>();

		private static readonly Dictionary<int, ContainerTraits> TraitCache = new Dictionary<int, ContainerTraits>();

		private static Player? _cachedPlayer;

		private static Vector3 _cachedPlayerPosition;

		private static float _containerCacheCreatedAt = -1000f;

		private static float _cachedRange = -1f;

		private static bool _cachedRequirePiece;

		private static bool _cachedIgnoreMoving;

		private static bool _cachedIgnoreObliterators;

		private static bool _cachedBalrondCompatibility;

		private static int _containerGeneration;

		internal static void InvalidateAll()
		{
			CachedContainers.Clear();
			ScanBuffer.Clear();
			CountCache.Clear();
			TraitCache.Clear();
			_cachedPlayer = null;
			_containerCacheCreatedAt = -1000f;
			_cachedRange = -1f;
			_containerGeneration++;
		}

		internal static void InvalidateCounts()
		{
			CountCache.Clear();
		}

		private static bool ContainerCacheIsValid(Player player, float now)
		{
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			if ((Object)(object)_cachedPlayer != (Object)(object)player)
			{
				return false;
			}
			if (now - _containerCacheCreatedAt >= 0.5f)
			{
				return false;
			}
			Vector3 val = ((Component)player).transform.position - _cachedPlayerPosition;
			if (val.sqrMagnitude >= 1f)
			{
				return false;
			}
			if (!Mathf.Approximately(_cachedRange, NearbyCraftingForkedPlugin.Range.Value))
			{
				return false;
			}
			if (_cachedRequirePiece != NearbyCraftingForkedPlugin.RequirePlayerPlacedContainer.Value || _cachedIgnoreMoving != NearbyCraftingForkedPlugin.IgnoreMovingContainers.Value || _cachedIgnoreObliterators != NearbyCraftingForkedPlugin.IgnoreObliterators.Value || _cachedBalrondCompatibility != NearbyCraftingForkedPlugin.BalrondCompatibility.Value)
			{
				return false;
			}
			return true;
		}

		internal static List<Container> Get(Player player)
		{
			if ((Object)(object)player == (Object)null)
			{
				return CachedContainers;
			}
			float realtimeSinceStartup = Time.realtimeSinceStartup;
			if (ContainerCacheIsValid(player, realtimeSinceStartup))
			{
				return CachedContainers;
			}
			RefreshContainerCache(player, realtimeSinceStartup);
			return CachedContainers;
		}

		private static void RefreshContainerCache(Player player, float now)
		{
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
			//IL_008e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0093: Unknown result type (might be due to invalid IL or missing references)
			//IL_0094: Unknown result type (might be due to invalid IL or missing references)
			//IL_0099: Unknown result type (might be due to invalid IL or missing references)
			//IL_0272: Unknown result type (might be due to invalid IL or missing references)
			//IL_0273: Unknown result type (might be due to invalid IL or missing references)
			CachedContainers.Clear();
			ScanBuffer.Clear();
			CountCache.Clear();
			PruneTraitCache();
			float num = Mathf.Max(0f, NearbyCraftingForkedPlugin.Range.Value);
			float num2 = num * num;
			Vector3 position = ((Component)player).transform.position;
			float num3 = (NearbyCraftingForkedPlugin.DebugEnabled ? Time.realtimeSinceStartup : 0f);
			Container[] array = Object.FindObjectsByType<Container>((FindObjectsSortMode)0);
			foreach (Container val in array)
			{
				if ((Object)(object)val == (Object)null || !((Behaviour)val).isActiveAndEnabled)
				{
					continue;
				}
				Vector3 val2 = ((Component)val).transform.position - position;
				float sqrMagnitude = val2.sqrMagnitude;
				if (sqrMagnitude > num2)
				{
					if (NearbyCraftingForkedPlugin.ContainerDebugEnabled)
					{
						NearbyCraftingForkedPlugin.DebugContainer($"Ignoring '{((Object)val).name}': outside {num:0.0}m range.");
					}
					continue;
				}
				ContainerTraits traits = GetTraits(val);
				if (NearbyCraftingForkedPlugin.IgnoreObliterators.Value && traits.IsIncinerator)
				{
					if (NearbyCraftingForkedPlugin.ContainerDebugEnabled)
					{
						NearbyCraftingForkedPlugin.DebugContainer("Ignoring '" + ((Object)val).name + "': Obliterator/Incinerator.");
					}
					continue;
				}
				if (NearbyCraftingForkedPlugin.RequirePlayerPlacedContainer.Value && !traits.HasPiece)
				{
					if (NearbyCraftingForkedPlugin.ContainerDebugEnabled)
					{
						NearbyCraftingForkedPlugin.DebugContainer("Ignoring '" + ((Object)val).name + "': not attached to a player-placeable Piece.");
					}
					continue;
				}
				if (ShouldIgnoreMovingContainer(val, traits))
				{
					if (NearbyCraftingForkedPlugin.ContainerDebugEnabled)
					{
						NearbyCraftingForkedPlugin.DebugContainer("Ignoring '" + ((Object)val).name + "': moving container in active use (or ship).");
					}
					continue;
				}
				try
				{
					if (val.GetInventory() == null)
					{
						continue;
					}
				}
				catch (Exception ex)
				{
					if (NearbyCraftingForkedPlugin.ContainerDebugEnabled)
					{
						NearbyCraftingForkedPlugin.DebugContainer("Skipping '" + ((Object)val).name + "': GetInventory threw " + ex.GetType().Name + ": " + ex.Message);
					}
					continue;
				}
				ScanBuffer.Add(new ContainerDistance(val, sqrMagnitude));
				if (NearbyCraftingForkedPlugin.ContainerDebugEnabled)
				{
					NearbyCraftingForkedPlugin.DebugContainer($"Eligible '{((Object)val).name}' at {Mathf.Sqrt(sqrMagnitude):0.0}m.");
				}
			}
			ScanBuffer.Sort((ContainerDistance a, ContainerDistance b) => a.DistanceSq.CompareTo(b.DistanceSq));
			for (int num4 = 0; num4 < ScanBuffer.Count; num4++)
			{
				CachedContainers.Add(ScanBuffer[num4].Container);
			}
			_cachedPlayer = player;
			_cachedPlayerPosition = position;
			_containerCacheCreatedAt = now;
			_cachedRange = NearbyCraftingForkedPlugin.Range.Value;
			_cachedRequirePiece = NearbyCraftingForkedPlugin.RequirePlayerPlacedContainer.Value;
			_cachedIgnoreMoving = NearbyCraftingForkedPlugin.IgnoreMovingContainers.Value;
			_cachedIgnoreObliterators = NearbyCraftingForkedPlugin.IgnoreObliterators.Value;
			_cachedBalrondCompatibility = NearbyCraftingForkedPlugin.BalrondCompatibility.Value;
			_containerGeneration++;
			if (NearbyCraftingForkedPlugin.DebugEnabled)
			{
				float num5 = (Time.realtimeSinceStartup - num3) * 1000f;
				NearbyCraftingForkedPlugin.Debug($"Container cache refreshed: scanned={array.Length}, eligible={CachedContainers.Count}, " + $"time={num5:0.00}ms.");
			}
		}

		private static void PruneTraitCache()
		{
			if (TraitCache.Count == 0)
			{
				return;
			}

			List<int>? deadKeys = null;
			foreach (KeyValuePair<int, ContainerTraits> pair in TraitCache)
			{
				if ((Object)(object)pair.Value.Container == (Object)null)
				{
					deadKeys ??= new List<int>();
					deadKeys.Add(pair.Key);
				}
			}

			if (deadKeys == null)
			{
				return;
			}

			for (int i = 0; i < deadKeys.Count; i++)
			{
				TraitCache.Remove(deadKeys[i]);
			}
		}

		/// <summary>
		/// When IgnoreMovingContainers is on: allow parked carts; skip carts that are attached/in use; always skip ships.
		/// </summary>
		private static bool ShouldIgnoreMovingContainer(Container container, ContainerTraits traits)
		{
			if (!traits.IsMoving || NearbyCraftingForkedPlugin.IgnoreMovingContainers == null || !NearbyCraftingForkedPlugin.IgnoreMovingContainers.Value)
			{
				return false;
			}

			try
			{
				Vagon? cart = ((Component)container).GetComponentInParent<Vagon>(true);
				if ((Object)(object)cart != (Object)null)
				{
					if (cart.InUse() || cart.IsAttached())
					{
						return true;
					}
					return false;
				}
			}
			catch
			{
			}

			// Ships and any other moving container types stay ignored while the setting is on.
			return true;
		}

		private static bool HasPlayerPlacedPiece(Container container)
		{
			try
			{
				if ((Object)(object)((Component)container).GetComponentInParent<Piece>() != (Object)null)
				{
					return true;
				}
				if (!NearbyCraftingForkedPlugin.BalrondCompatibility.Value)
				{
					return false;
				}
				Transform root = ((Component)container).transform.root;
				if ((Object)(object)root == (Object)null)
				{
					return false;
				}
				Piece[] componentsInChildren = ((Component)root).GetComponentsInChildren<Piece>(true);
				bool flag = componentsInChildren != null && componentsInChildren.Length != 0;
				if (flag && NearbyCraftingForkedPlugin.ContainerDebugEnabled)
				{
					NearbyCraftingForkedPlugin.DebugContainer("Accepted '" + ((Object)container).name + "' through sibling-Piece compatibility fallback (root='" + ((Object)root).name + "').");
				}
				return flag;
			}
			catch (Exception ex)
			{
				if (NearbyCraftingForkedPlugin.ContainerDebugEnabled)
				{
					NearbyCraftingForkedPlugin.DebugContainer("Piece-layout inspection failed for '" + ((Object)container).name + "': " + ex.GetType().Name + ": " + ex.Message);
				}
				return false;
			}
		}

		private static ContainerTraits GetTraits(Container container)
		{
			int instanceID = ((Object)container).GetInstanceID();
			if (TraitCache.TryGetValue(instanceID, out var value) && value.Container == container && (value.HasPiece || !NearbyCraftingForkedPlugin.BalrondCompatibility.Value))
			{
				return value;
			}
			bool hasPiece = HasPlayerPlacedPiece(container);
			bool isMoving = false;
			bool isIncinerator = false;
			try
			{
				if ((Object)(object)((Component)container).GetComponentInParent<Incinerator>(true) != (Object)null)
				{
					isIncinerator = true;
				}
			}
			catch
			{
			}
			try
			{
				Component[] componentsInParent = ((Component)container).GetComponentsInParent<Component>(true);
				for (int i = 0; i < componentsInParent.Length; i++)
				{
					string typeName = ((object)componentsInParent[i]).GetType().Name;
					switch (typeName)
					{
					case "Ship":
					case "Vagon":
					case "Wagon":
						isMoving = true;
						break;
					case "Incinerator":
						isIncinerator = true;
						break;
					}
					if (isMoving && isIncinerator)
					{
						break;
					}
				}
			}
			catch
			{
			}
			ContainerTraits containerTraits = new ContainerTraits(container, hasPiece, isMoving, isIncinerator);
			TraitCache[instanceID] = containerTraits;
			return containerTraits;
		}

		internal static int CountCached(Player player, string sharedName, int quality = -1)
		{
			List<Container> containers = Get(player);
			float realtimeSinceStartup = Time.realtimeSinceStartup;
			ItemCountKey key = new ItemCountKey(sharedName, quality);
			if (CountCache.TryGetValue(key, out var value) && value.ContainerGeneration == _containerGeneration && realtimeSinceStartup - value.CreatedAt < 0.1f)
			{
				return value.Value;
			}
			int num = CountAcross(player, containers, sharedName, quality);
			CountCache[key] = new CachedCount(num, realtimeSinceStartup, _containerGeneration);
			return num;
		}

		internal static int CountFresh(Player player, string sharedName, int quality = -1)
		{
			return CountAcross(player, Get(player), sharedName, quality);
		}

		private static int CountAcross(Player player, List<Container> containers, string sharedName, int quality)
		{
			Inventory inventory = ((Humanoid)player).GetInventory();
			int num = ((quality >= 0) ? inventory.CountItems(sharedName, quality, true) : inventory.CountItems(sharedName, -1, true));
			int num2 = num;
			for (int i = 0; i < containers.Count; i++)
			{
				Container val = containers[i];
				if ((Object)(object)val == (Object)null)
				{
					continue;
				}
				Inventory inventory2;
				try
				{
					inventory2 = val.GetInventory();
				}
				catch
				{
					continue;
				}
				if (inventory2 != null)
				{
					int num3 = ((quality >= 0) ? inventory2.CountItems(sharedName, quality, true) : inventory2.CountItems(sharedName, -1, true));
					num2 += num3;
					if (num3 > 0 && NearbyCraftingForkedPlugin.ContainerDebugEnabled)
					{
						NearbyCraftingForkedPlugin.DebugContainer($"Count '{sharedName}': '{((Object)val).name}' contributes {num3}.");
					}
				}
			}
			if (NearbyCraftingForkedPlugin.DebugEnabled)
			{
				NearbyCraftingForkedPlugin.Debug($"Count '{sharedName}' (quality={quality}): player={num}, " + $"nearby={num2 - num}, total={num2}.");
			}
			return num2;
		}

		internal static int CountNearbyOnly(Player player, string sharedName, int quality, bool matchWorldLevel)
		{
			if ((Object)(object)player == (Object)null || string.IsNullOrEmpty(sharedName))
			{
				return 0;
			}
			List<Container> list = Get(player);
			int num = 0;
			for (int i = 0; i < list.Count; i++)
			{
				Container val = list[i];
				if ((Object)(object)val == (Object)null)
				{
					continue;
				}
				Inventory inventory;
				try
				{
					inventory = val.GetInventory();
				}
				catch
				{
					continue;
				}
				if (inventory != null)
				{
					int num2 = inventory.CountItems(sharedName, quality, matchWorldLevel);
					num += num2;
					if (num2 > 0 && NearbyCraftingForkedPlugin.ContainerDebugEnabled)
					{
						NearbyCraftingForkedPlugin.DebugContainer($"Vanilla requirement count '{sharedName}': '{((Object)val).name}' contributes {num2}.");
					}
				}
			}
			return num;
		}

		private static Dictionary<string, int> AggregateRequirements(Piece.Requirement[] requirements, int qualityLevel, int multiplier, HashSet<string>? allowedNames = null)
		{
			Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.Ordinal);
			foreach (Piece.Requirement val in requirements)
			{
				if ((Object)(object)val?.m_resItem == (Object)null)
				{
					continue;
				}
				int num = val.GetAmount(qualityLevel) * multiplier;
				if (num <= 0)
				{
					continue;
				}
				string name = val.m_resItem.m_itemData.m_shared.m_name;
				int value;
				if (allowedNames != null && !allowedNames.Contains(name))
				{
					if (NearbyCraftingForkedPlugin.DebugEnabled)
					{
						NearbyCraftingForkedPlugin.Debug("Skipping conditional requirement '" + name + "' during nearby consumption.");
					}
				}
				else if (dictionary.TryGetValue(name, out value))
				{
					dictionary[name] = value + num;
				}
				else
				{
					dictionary.Add(name, num);
				}
			}
			return dictionary;
		}

		internal static bool CanPayPlayerOnly(Player player, Piece.Requirement[] requirements, int qualityLevel, int itemQuality, int multiplier, HashSet<string>? allowedNames = null)
		{
			Dictionary<string, int> dictionary = AggregateRequirements(requirements, qualityLevel, multiplier, allowedNames);
			Inventory inventory = ((Humanoid)player).GetInventory();
			foreach (KeyValuePair<string, int> item in dictionary)
			{
				if (((itemQuality >= 0) ? inventory.CountItems(item.Key, itemQuality, true) : inventory.CountItems(item.Key, -1, true)) < item.Value)
				{
					return false;
				}
			}
			return true;
		}

		internal static bool CanPayCombined(Player player, Piece.Requirement[] requirements, int qualityLevel, int itemQuality, int multiplier, HashSet<string>? allowedNames = null)
		{
			foreach (KeyValuePair<string, int> item in AggregateRequirements(requirements, qualityLevel, multiplier, allowedNames))
			{
				int num = CountFresh(player, item.Key, itemQuality);
				if (num < item.Value)
				{
					if (NearbyCraftingForkedPlugin.DebugEnabled)
					{
						NearbyCraftingForkedPlugin.Debug($"CanPayCombined=false for '{item.Key}': need={item.Value}, available={num}, " + $"qualityLevel={qualityLevel}, itemQuality={itemQuality}, multiplier={multiplier}.");
					}
					return false;
				}
				if (NearbyCraftingForkedPlugin.DebugEnabled)
				{
					NearbyCraftingForkedPlugin.Debug($"Combined requirement OK for '{item.Key}': need={item.Value}, available={num}.");
				}
			}
			return true;
		}

		internal static void Pay(Player player, Piece.Requirement[] requirements, int qualityLevel, int itemQuality, int multiplier, HashSet<string>? allowedNames = null)
		{
			Dictionary<string, int> dictionary = AggregateRequirements(requirements, qualityLevel, multiplier, allowedNames);
			List<Container> list = Get(player);
			Inventory inventory = ((Humanoid)player).GetInventory();
			foreach (KeyValuePair<string, int> item in dictionary)
			{
				string key = item.Key;
				int num = item.Value;
				int val = ((itemQuality >= 0) ? inventory.CountItems(key, itemQuality, true) : inventory.CountItems(key, -1, true));
				int num2 = Math.Min(num, val);
				if (num2 > 0)
				{
					inventory.RemoveItem(key, num2, itemQuality, true);
					num -= num2;
					if (NearbyCraftingForkedPlugin.DebugEnabled)
					{
						NearbyCraftingForkedPlugin.Debug($"Consumed {num2}x '{key}' from player inventory; remaining={num}.");
					}
				}
				for (int i = 0; i < list.Count && num > 0; i++)
				{
					Container val2 = list[i];
					if ((Object)(object)val2 == (Object)null)
					{
						continue;
					}
					Inventory inventory2;
					try
					{
						inventory2 = val2.GetInventory();
					}
					catch
					{
						continue;
					}
					if (inventory2 == null)
					{
						continue;
					}
					int val3 = ((itemQuality >= 0) ? inventory2.CountItems(key, itemQuality, true) : inventory2.CountItems(key, -1, true));
					int num3 = Math.Min(num, val3);
					if (num3 > 0)
					{
						inventory2.RemoveItem(key, num3, itemQuality, true);
						num -= num3;
						if (NearbyCraftingForkedPlugin.DebugEnabled)
						{
							NearbyCraftingForkedPlugin.Debug($"Consumed {num3}x '{key}' from container '{((Object)val2).name}'; remaining={num}.");
						}
					}
				}
				if (num > 0)
				{
					NearbyCraftingForkedPlugin.ModLogger.LogWarning((object)($"Resource consumption ended with {num}x '{key}' unpaid. " + "Container contents may have changed during crafting."));
				}
			}
			InvalidateCounts();
		}

		internal static bool HaveBuildRequirementsCombined(Player player, Piece piece)
		{
			if ((Object)(object)piece == (Object)null || piece.m_resources == null)
			{
				return false;
			}
			Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.Ordinal);
			for (int i = 0; i < piece.m_resources.Length; i++)
			{
				Piece.Requirement val = piece.m_resources[i];
				if (!((Object)(object)val?.m_resItem == (Object)null) && val.m_amount > 0)
				{
					string name = val.m_resItem.m_itemData.m_shared.m_name;
					if (dictionary.TryGetValue(name, out var value))
					{
						dictionary[name] = value + val.m_amount;
					}
					else
					{
						dictionary.Add(name, val.m_amount);
					}
				}
			}
			foreach (KeyValuePair<string, int> item in dictionary)
			{
				int num = CountCached(player, item.Key);
				if (num < item.Value)
				{
					if (NearbyCraftingForkedPlugin.DebugEnabled)
					{
						NearbyCraftingForkedPlugin.Debug("Build requirement failed for '" + ((Object)piece).name + "': '" + item.Key + "' " + $"needs={item.Value}, available={num}.");
					}
					return false;
				}
			}
			return true;
		}
	}
	internal readonly struct RequirementGraphicEntry
	{
		internal readonly Transform Root;

		internal readonly Graphic Graphic;

		internal RequirementGraphicEntry(Transform root, Graphic graphic)
		{
			Root = root;
			Graphic = graphic;
		}
	}
	internal static class RequirementUiCache
	{
		private static readonly Dictionary<int, RequirementGraphicEntry> Graphics = new Dictionary<int, RequirementGraphicEntry>();

		internal static Graphic? GetAmountGraphic(Transform elementRoot)
		{
			int instanceID = ((Object)elementRoot).GetInstanceID();
			if (Graphics.TryGetValue(instanceID, out var value) && value.Root == elementRoot && (Object)(object)value.Graphic != (Object)null)
			{
				return value.Graphic;
			}
			Transform val = elementRoot.Find("res_amount");
			if ((Object)(object)val == (Object)null)
			{
				return null;
			}
			Graphic component = ((Component)val).GetComponent<Graphic>();
			if ((Object)(object)component != (Object)null)
			{
				Graphics[instanceID] = new RequirementGraphicEntry(elementRoot, component);
			}
			return component;
		}

		internal static void Clear()
		{
			Graphics.Clear();
		}
	}
	[HarmonyPatch(typeof(InventoryGui), "DoCrafting")]
	[HarmonyAfter(new string[] { "balrond.astafaraios.BalrondConstructions" })]
	internal static class DoCraftingContextPatch
	{
		private static void Prefix(ref bool __state)
		{
			__state = false;
			if (NearbyCraftingForkedPlugin.Enabled.Value)
			{
				CraftingContext.Depth++;
				__state = true;
				if (NearbyCraftingForkedPlugin.DebugEnabled)
				{
					NearbyCraftingForkedPlugin.Debug($"DoCrafting entered. CraftingContext.Depth={CraftingContext.Depth}.");
				}
			}
		}

		private static void Finalizer(bool __state, Exception? __exception)
		{
			if (__exception != null)
			{
				NearbyCraftingForkedPlugin.ModLogger.LogError((object)("DoCrafting threw " + __exception.GetType().Name + ": " + __exception.Message));
			}
			if (__state && CraftingContext.Depth > 0)
			{
				CraftingContext.Depth--;
				if (NearbyCraftingForkedPlugin.DebugEnabled)
				{
					NearbyCraftingForkedPlugin.Debug($"DoCrafting exited. CraftingContext.Depth={CraftingContext.Depth}.");
				}
			}
		}
	}
	[HarmonyPatch(typeof(Player), "HaveRequirementItems", new Type[]
	{
		typeof(Recipe),
		typeof(bool),
		typeof(int),
		typeof(int)
	})]
	[HarmonyAfter(new string[] { "balrond.astafaraios.BalrondConstructions" })]
	internal static class HaveRequirementItemsPatch
	{
		[HarmonyPriority(800)]
		private static void Prefix(Player __instance, Recipe piece, bool discover, int qualityLevel, int amount)
		{
			if (!(!NearbyCraftingForkedPlugin.Enabled.Value || discover) && !((Object)(object)__instance == (Object)null) && !((Object)(object)piece == (Object)null))
			{
				RecipeRequirementContext.Begin(__instance, discover);
				RecipeConsumptionRules.Begin(piece, qualityLevel);
				if (NearbyCraftingForkedPlugin.DebugEnabled)
				{
					NearbyCraftingForkedPlugin.Debug("Vanilla recipe check with nearby counts enabled for '" + ((Object)piece).name + "' " + $"(qualityLevel={qualityLevel}, amount={amount}).");
				}
			}
		}

		[HarmonyPriority(0)]
		private static void Postfix(Recipe piece, bool discover, ref bool __result)
		{
			if (!(!NearbyCraftingForkedPlugin.Enabled.Value || discover) && !((Object)(object)piece == (Object)null))
			{
				RecipeConsumptionRules.Complete(__result);
				if (NearbyCraftingForkedPlugin.DebugEnabled)
				{
					NearbyCraftingForkedPlugin.Debug($"Vanilla recipe check result for '{((Object)piece).name}' with nearby counts: {__result}.");
				}
			}
		}

		private static Exception? Finalizer(Exception? __exception)
		{
			if (__exception != null)
			{
				RecipeConsumptionRules.Complete(success: false);
			}
			RecipeRequirementContext.End();
			return __exception;
		}
	}
	[HarmonyPatch(typeof(Inventory), "CountItems", new Type[]
	{
		typeof(string),
		typeof(int),
		typeof(bool)
	})]
	[HarmonyAfter(new string[] { "balrond.astafaraios.BalrondConstructions" })]
	internal static class InventoryCountItemsRequirementPatch
	{
		[HarmonyPriority(0)]
		private static void Postfix(Inventory __instance, string name, int quality, bool matchWorldLevel, ref int __result)
		{
			if (!NearbyCraftingForkedPlugin.Enabled.Value || !RecipeRequirementContext.IsActive || __instance == null || __instance != RecipeRequirementContext.PlayerInventory)
			{
				return;
			}
			Player player = RecipeRequirementContext.Player;
			if ((Object)(object)player == (Object)null)
			{
				return;
			}
			RecipeConsumptionRules.Record(name);
			int num = NearbyContainers.CountNearbyOnly(player, name, quality, matchWorldLevel);
			if (num > 0)
			{
				int num2 = __result;
				__result += num;
				if (NearbyCraftingForkedPlugin.DebugEnabled)
				{
					NearbyCraftingForkedPlugin.Debug($"Vanilla CountItems augmented for '{name}': player={num2}, " + $"nearby={num}, total={__result}, quality={quality}, " + $"matchWorldLevel={matchWorldLevel}.");
				}
			}
		}
	}
	[HarmonyPatch(typeof(Player), "TryPlacePiece", new Type[] { typeof(Piece) })]
	[HarmonyAfter(new string[] { "balrond.astafaraios.BalrondConstructions" })]
	internal static class TryPlacePiecePatch
	{
		private static void Postfix(Player __instance, bool __result)
		{
			if (NearbyCraftingForkedPlugin.Enabled.Value && NearbyCraftingForkedPlugin.EnableBuilding.Value)
			{
				BuildingPlacementState.MarkPlaced(__instance, __result);
			}
		}
	}
	[HarmonyPatch(typeof(Player), "HaveRequirements", new Type[]
	{
		typeof(Piece),
		typeof(Player.RequirementMode)
	})]
	[HarmonyAfter(new string[] { "balrond.astafaraios.BalrondConstructions" })]
	internal static class HaveBuildRequirementsPatch
	{
		[HarmonyPriority(0)]
		private static void Postfix(Player __instance, Piece piece, Player.RequirementMode mode, ref bool __result)
		{
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			//IL_0056: Unknown result type (might be due to invalid IL or missing references)
			if ((!NearbyCraftingForkedPlugin.Enabled.Value || !NearbyCraftingForkedPlugin.EnableBuilding.Value || __result)
				|| (Object)(object)__instance == (Object)null
				|| (Object)(object)piece == (Object)null
				|| (int)mode != 0
				|| ((Object)(object)piece.m_craftingStation != (Object)null
					&& !(bool)(Object)(object)CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name, ((Component)__instance).transform.position))
				|| !NearbyContainers.HaveBuildRequirementsCombined(__instance, piece))
			{
				return;
			}

			__result = true;
			if (NearbyCraftingForkedPlugin.DebugEnabled)
			{
				NearbyCraftingForkedPlugin.Debug("HaveRequirements overridden to TRUE for build piece '" + ((Object)piece).name + "'.");
			}
		}
	}
	[HarmonyPatch(typeof(Player), "ConsumeResources")]
	[HarmonyAfter(new string[] { "balrond.astafaraios.BalrondConstructions" })]
	internal static class ConsumeResourcesPatch
	{
		[HarmonyPriority(700)]
		private static bool Prefix(Player __instance, Piece.Requirement[] requirements, int qualityLevel, int itemQuality = -1, int multiplier = 1)
		{
			bool flag = NearbyCraftingForkedPlugin.EnableBuilding.Value && BuildingPlacementState.WasPlacedThisFrame(__instance);
			bool flag2 = CraftingContext.IsCrafting || flag;
			if (!NearbyCraftingForkedPlugin.Enabled.Value || !flag2 || (Object)(object)__instance == (Object)null || requirements == null)
			{
				return true;
			}
			HashSet<string> allowedNames = null;
			if (CraftingContext.IsCrafting)
			{
				if (RecipeConsumptionRules.TryGet(requirements, qualityLevel, out HashSet<string> allowedNames2) && allowedNames2 != null)
				{
					allowedNames = allowedNames2;
					if (NearbyCraftingForkedPlugin.DebugEnabled)
					{
						NearbyCraftingForkedPlugin.Debug($"Using vanilla-recorded requirement set for consumption: {allowedNames2.Count} name(s).");
					}
				}
				else if (NearbyCraftingForkedPlugin.DebugEnabled)
				{
					NearbyCraftingForkedPlugin.Debug("No vanilla-recorded requirement set found for this craft; using unfiltered fallback.");
				}
			}
			if (NearbyContainers.CanPayPlayerOnly(__instance, requirements, qualityLevel, itemQuality, multiplier, allowedNames))
			{
				if (NearbyCraftingForkedPlugin.DebugEnabled)
				{
					NearbyCraftingForkedPlugin.Debug("Player inventory can fully pay; using vanilla ConsumeResources (context=" + (flag ? "building" : "crafting") + ").");
				}
				return true;
			}
			if (NearbyCraftingForkedPlugin.DebugEnabled)
			{
				NearbyCraftingForkedPlugin.Debug("ConsumeResources needs nearby storage for " + string.Format("{0}: requirements={1}, ", flag ? "building" : "crafting", requirements.Length) + $"qualityLevel={qualityLevel}, itemQuality={itemQuality}, multiplier={multiplier}.");
			}
			if (!NearbyContainers.CanPayCombined(__instance, requirements, qualityLevel, itemQuality, multiplier, allowedNames))
			{
				if (NearbyCraftingForkedPlugin.DebugEnabled)
				{
					NearbyCraftingForkedPlugin.Debug("Combined pre-flight failed; falling back to vanilla without partial chest consumption.");
				}
				return true;
			}
			NearbyContainers.Pay(__instance, requirements, qualityLevel, itemQuality, multiplier, allowedNames);
			if (NearbyCraftingForkedPlugin.DebugEnabled)
			{
				NearbyCraftingForkedPlugin.Debug("Nearby Crafting completed resource consumption; skipping vanilla ConsumeResources.");
			}
			return false;
		}
	}
	[HarmonyPatch(typeof(InventoryGui), "SetupRequirement")]
	[HarmonyAfter(new string[] { "balrond.astafaraios.BalrondConstructions" })]
	internal static class SetupRequirementDisplayPatch
	{
		[HarmonyPriority(0)]
		private static void Postfix(Transform elementRoot, Piece.Requirement req, Player player, bool craft, int quality)
		{
			//IL_0051: Unknown result type (might be due to invalid IL or missing references)
			//IL_0056: Unknown result type (might be due to invalid IL or missing references)
			//IL_0057: Unknown result type (might be due to invalid IL or missing references)
			//IL_0058: Unknown result type (might be due to invalid IL or missing references)
			//IL_009a: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
			if (!NearbyCraftingForkedPlugin.Enabled.Value || !NearbyCraftingForkedPlugin.FixRequirementIndicator.Value || (Object)(object)elementRoot == (Object)null || (Object)(object)req?.m_resItem == (Object)null || (Object)(object)player == (Object)null)
			{
				return;
			}
			Graphic amountGraphic = RequirementUiCache.GetAmountGraphic(elementRoot);
			if ((Object)(object)amountGraphic == (Object)null)
			{
				return;
			}
			Color color = amountGraphic.color;
			if (color == Color.white)
			{
				return;
			}
			int amount = req.GetAmount(quality);
			if (amount <= 0)
			{
				return;
			}
			string name = req.m_resItem.m_itemData.m_shared.m_name;
			int num = NearbyContainers.CountCached(player, name);
			if (num >= amount)
			{
				amountGraphic.color = Color.white;
				if (NearbyCraftingForkedPlugin.DebugEnabled)
				{
					NearbyCraftingForkedPlugin.Debug($"UI indicator corrected for '{name}': need={amount}, available={num}, " + $"previousColor={color}.");
				}
			}
		}
	}

	internal static class StationChestAssist
	{
		internal static bool IsAssistEnabled
		{
			get
			{
				return NearbyCraftingForkedPlugin.Enabled != null
					&& NearbyCraftingForkedPlugin.Enabled.Value
					&& NearbyCraftingForkedPlugin.StationFuelEnabled != null
					&& NearbyCraftingForkedPlugin.StationFuelEnabled.Value;
			}
		}

		internal enum MoveToPlayerResult
		{
			Failed,
			InventoryFull,
			Moved
		}

		internal static void NotifyInventoryFull(Humanoid user)
		{
			if ((Object)(object)user == (Object)null)
			{
				return;
			}
			// Same key vanilla uses for "no room in inventory".
			((Character)user).Message((MessageHud.MessageType)2, "$msg_noroom", 0, (Sprite)null, false);
		}

		internal static MoveToPlayerResult TryMoveOneToPlayer(Inventory from, Inventory to, ItemDrop.ItemData item)
		{
			if (from == null || to == null || item == null || (Object)(object)item.m_dropPrefab == (Object)null)
			{
				return MoveToPlayerResult.Failed;
			}

			GameObject dropPrefab = item.m_dropPrefab;
			if (!to.CanAddItem(dropPrefab, 1))
			{
				return MoveToPlayerResult.InventoryFull;
			}

			if (!from.RemoveOneItem(item))
			{
				return MoveToPlayerResult.Failed;
			}

			if (!to.AddItem(dropPrefab, 1))
			{
				// Best-effort restore so a failed add does not destroy the chest stack.
				from.AddItem(dropPrefab, 1);
				return MoveToPlayerResult.InventoryFull;
			}

			return MoveToPlayerResult.Moved;
		}

		internal static bool TryPullOneToPlayer(Humanoid user, string sharedName)
		{
			if (!IsAssistEnabled || user == null || string.IsNullOrEmpty(sharedName))
			{
				return false;
			}

			Inventory playerInventory = user.GetInventory();
			if (playerInventory == null)
			{
				return false;
			}

			if (playerInventory.HaveItem(sharedName, true))
			{
				return false;
			}

			Player player = user as Player;
			if ((Object)(object)player == (Object)null)
			{
				player = Player.m_localPlayer;
			}
			if ((Object)(object)player == (Object)null || (Object)(object)player != (Object)(object)Player.m_localPlayer)
			{
				return false;
			}

			List<Container> containers = NearbyContainers.Get(player);
			for (int i = 0; i < containers.Count; i++)
			{
				Container container = containers[i];
				if ((Object)(object)container == (Object)null)
				{
					continue;
				}

				Inventory inventory;
				try
				{
					inventory = container.GetInventory();
				}
				catch
				{
					continue;
				}
				if (inventory == null)
				{
					continue;
				}

				// Third arg is isPrefabName (not matchWorldLevel). Shared names like $item_resin need false.
				ItemDrop.ItemData item = inventory.GetItem(sharedName, -1, false);
				if (item == null)
				{
					continue;
				}

				MoveToPlayerResult result = TryMoveOneToPlayer(inventory, playerInventory, item);
				if (result == MoveToPlayerResult.InventoryFull)
				{
					NotifyInventoryFull(user);
					return false;
				}
				if (result != MoveToPlayerResult.Moved)
				{
					continue;
				}

				NearbyContainers.InvalidateCounts();
				if (NearbyCraftingForkedPlugin.DebugEnabled)
				{
					NearbyCraftingForkedPlugin.Debug($"Station fuel assist pulled 1x '{sharedName}' from '{((Object)container).name}'.");
				}
				return true;
			}

			return false;
		}

		internal static readonly Func<Smelter, float>? SmelterGetFuel = CreateDelegate<Func<Smelter, float>>(typeof(Smelter), "GetFuel");
		internal static readonly Func<CookingStation, float>? CookingGetFuel = CreateDelegate<Func<CookingStation, float>>(typeof(CookingStation), "GetFuel");
		internal static readonly Func<ShieldGenerator, float>? ShieldGetFuel = CreateDelegate<Func<ShieldGenerator, float>>(typeof(ShieldGenerator), "GetFuel");
		internal static readonly Func<Smelter, int>? SmelterGetQueueSize = CreateDelegate<Func<Smelter, int>>(typeof(Smelter), "GetQueueSize");
		internal static readonly Func<Smelter, Inventory, ItemDrop.ItemData>? SmelterFindCookableItem =
			CreateDelegate<Func<Smelter, Inventory, ItemDrop.ItemData>>(typeof(Smelter), "FindCookableItem", typeof(Inventory));
		private static readonly FieldInfo? FireplaceNview = AccessTools.Field(typeof(Fireplace), "m_nview");

		private static TDelegate? CreateDelegate<TDelegate>(Type type, string name, params Type[] parameters) where TDelegate : Delegate
		{
			MethodInfo? method = parameters == null || parameters.Length == 0
				? AccessTools.Method(type, name)
				: AccessTools.Method(type, name, parameters);
			if (method == null)
			{
				return null;
			}
			return AccessTools.MethodDelegate<TDelegate>(method);
		}

		internal static bool TryPullFuelItemDrop(Humanoid user, ItemDrop? fuelItem)
		{
			if ((Object)(object)fuelItem == (Object)null || fuelItem.m_itemData?.m_shared == null)
			{
				return false;
			}
			return TryPullOneToPlayer(user, fuelItem.m_itemData.m_shared.m_name);
		}

		internal static float GetFireplaceFuel(Fireplace fireplace)
		{
			ZNetView? nview = FireplaceNview?.GetValue(fireplace) as ZNetView;
			if ((Object)(object)nview == (Object)null || !nview.IsValid())
			{
				return 0f;
			}
			return nview.GetZDO().GetFloat(ZDOVars.s_fuel, 0f);
		}
	}

	[HarmonyPatch(typeof(Smelter), "OnAddFuel")]
	internal static class SmelterOnAddFuelPatch
	{
		private static void Prefix(Smelter __instance, Humanoid user, ItemDrop.ItemData item)
		{
			if (!StationChestAssist.IsAssistEnabled || item != null || (Object)(object)__instance == (Object)null || (Object)(object)user == (Object)null)
			{
				return;
			}
			// Vanilla rejects when already full — don't pull into inventory first.
			Func<Smelter, float>? getFuel = StationChestAssist.SmelterGetFuel;
			if (getFuel != null && getFuel(__instance) > (float)(__instance.m_maxFuel - 1))
			{
				return;
			}
			StationChestAssist.TryPullFuelItemDrop(user, __instance.m_fuelItem);
		}
	}

	[HarmonyPatch(typeof(Smelter), "OnAddOre")]
	internal static class SmelterOnAddOrePatch
	{
		private static void Prefix(Smelter __instance, Humanoid user, ItemDrop.ItemData item)
		{
			Func<Smelter, Inventory, ItemDrop.ItemData>? findCookable = StationChestAssist.SmelterFindCookableItem;
			if (!StationChestAssist.IsAssistEnabled
				|| NearbyCraftingForkedPlugin.EnableOreFromChests == null
				|| !NearbyCraftingForkedPlugin.EnableOreFromChests.Value
				|| item != null
				|| (Object)(object)__instance == (Object)null
				|| (Object)(object)user == (Object)null
				|| findCookable == null)
			{
				return;
			}

			Func<Smelter, int>? getQueueSize = StationChestAssist.SmelterGetQueueSize;
			if (getQueueSize != null && getQueueSize(__instance) >= __instance.m_maxOre)
			{
				return;
			}

			Inventory playerInventory = user.GetInventory();
			if (playerInventory == null)
			{
				return;
			}

			if (findCookable(__instance, playerInventory) != null)
			{
				return;
			}

			Player player = user as Player;
			if ((Object)(object)player == (Object)null)
			{
				player = Player.m_localPlayer;
			}
			if ((Object)(object)player == (Object)null || (Object)(object)player != (Object)(object)Player.m_localPlayer)
			{
				return;
			}

			List<Container> containers = NearbyContainers.Get(player);
			for (int i = 0; i < containers.Count; i++)
			{
				Container container = containers[i];
				if ((Object)(object)container == (Object)null)
				{
					continue;
				}

				Inventory inventory;
				try
				{
					inventory = container.GetInventory();
				}
				catch
				{
					continue;
				}
				if (inventory == null)
				{
					continue;
				}

				ItemDrop.ItemData? ore = findCookable(__instance, inventory);
				if (ore == null)
				{
					continue;
				}

				StationChestAssist.MoveToPlayerResult result = StationChestAssist.TryMoveOneToPlayer(inventory, playerInventory, ore);
				if (result == StationChestAssist.MoveToPlayerResult.InventoryFull)
				{
					StationChestAssist.NotifyInventoryFull(user);
					return;
				}
				if (result != StationChestAssist.MoveToPlayerResult.Moved)
				{
					continue;
				}

				NearbyContainers.InvalidateCounts();
				if (NearbyCraftingForkedPlugin.DebugEnabled)
				{
					NearbyCraftingForkedPlugin.Debug($"Station ore assist pulled '{ore.m_shared?.m_name}' from '{((Object)container).name}'.");
				}
				break;
			}
		}
	}

	[HarmonyPatch(typeof(Fireplace), "Interact")]
	internal static class FireplaceInteractPatch
	{
		private static void Prefix(Fireplace __instance, Humanoid user, bool hold, bool alt)
		{
			// Tap on a turn-offable light with fuel left only toggles — do not pull.
			bool willAddFuel = FireplaceFuelAssist.WillInteractAddFuel(__instance, hold, alt);
			FireplaceFuelAssist.TryAssist(__instance, user, willAddFuel);
		}
	}

	internal static class FireplaceFuelAssist
	{
		internal static bool WillInteractAddFuel(Fireplace fireplace, bool hold, bool alt)
		{
			if ((Object)(object)fireplace == (Object)null || fireplace.m_infiniteFuel || !fireplace.m_canRefill)
			{
				return false;
			}

			float fuel = StationChestAssist.GetFireplaceFuel(fireplace);
			if ((float)Mathf.CeilToInt(fuel) >= fireplace.m_maxFuel)
			{
				return false;
			}

			// Matches Fireplace.Interact: brief Use toggles instead of refueling when fuel remains.
			if (fireplace.m_canTurnOff && !hold && !alt && fuel > 0f)
			{
				return false;
			}

			return true;
		}

		internal static void TryAssist(Fireplace fireplace, Humanoid user, bool willAddFuel)
		{
			if (!willAddFuel || !StationChestAssist.IsAssistEnabled || (Object)(object)fireplace == (Object)null || (Object)(object)user == (Object)null)
			{
				return;
			}
			StationChestAssist.TryPullFuelItemDrop(user, fireplace.m_fuelItem);
		}
	}

	[HarmonyPatch(typeof(CookingStation), "OnAddFuelSwitch")]
	internal static class CookingStationOnAddFuelSwitchPatch
	{
		private static void Prefix(CookingStation __instance, Humanoid user, ItemDrop.ItemData item)
		{
			if (!StationChestAssist.IsAssistEnabled || item != null || (Object)(object)__instance == (Object)null || (Object)(object)user == (Object)null)
			{
				return;
			}
			if (!__instance.m_useFuel)
			{
				return;
			}
			Func<CookingStation, float>? getFuel = StationChestAssist.CookingGetFuel;
			if (getFuel != null && getFuel(__instance) > (float)(__instance.m_maxFuel - 1))
			{
				return;
			}
			StationChestAssist.TryPullFuelItemDrop(user, __instance.m_fuelItem);
		}
	}

	[HarmonyPatch(typeof(ShieldGenerator), "OnAddFuel")]
	internal static class ShieldGeneratorOnAddFuelPatch
	{
		private static void Prefix(ShieldGenerator __instance, Humanoid user, ItemDrop.ItemData item)
		{
			if (!StationChestAssist.IsAssistEnabled || item != null || (Object)(object)__instance == (Object)null || (Object)(object)user == (Object)null)
			{
				return;
			}

			Func<ShieldGenerator, float>? getFuel = StationChestAssist.ShieldGetFuel;
			if (getFuel != null && getFuel(__instance) >= (float)__instance.m_maxFuel)
			{
				return;
			}

			List<ItemDrop>? fuelItems = __instance.m_fuelItems;
			if (fuelItems == null || fuelItems.Count == 0)
			{
				return;
			}

			Inventory playerInventory = user.GetInventory();
			if (playerInventory == null)
			{
				return;
			}

			for (int i = 0; i < fuelItems.Count; i++)
			{
				ItemDrop fuel = fuelItems[i];
				if ((Object)(object)fuel == (Object)null || fuel.m_itemData?.m_shared == null)
				{
					continue;
				}
				string name = fuel.m_itemData.m_shared.m_name;
				if (playerInventory.HaveItem(name, true))
				{
					return;
				}
			}

			for (int i = 0; i < fuelItems.Count; i++)
			{
				if (StationChestAssist.TryPullFuelItemDrop(user, fuelItems[i]))
				{
					return;
				}
			}
		}
	}

	internal static class ItemLocate
	{
		private const string EmissionColorProperty = "_EmissionColor";

		private static readonly List<HighlightedChest> Active = new List<HighlightedChest>();

		private static readonly MaterialPropertyBlock PropertyBlock = new MaterialPropertyBlock();

		private static bool _commandRegistered;

		private static float _activeUntil = -1f;

		private static Color _baseGlow = new Color(0.55f, 0.45f, 0.10f, 1f);

		private static float _glowAlpha = 1f;

		internal static void RegisterCommand()
		{
			if (_commandRegistered)
			{
				return;
			}
			_commandRegistered = true;
			RegisterOne(
				"nearby",
				"[item|clear] Highlight nearby eligible chests containing an item (or clear). No args uses held item.");
			RegisterOne(
				"locate",
				"[item|clear] Alias for nearby — highlight chests containing an item.");
		}

		private static void RegisterOne(string command, string description)
		{
			_ = new Terminal.ConsoleCommand(
				command,
				description,
				new Terminal.ConsoleEvent(OnCommand),
				false,
				false,
				false,
				false,
				false,
				false,
				null,
				false,
				false,
				false);
		}

		internal static void ClearHighlights()
		{
			for (int i = 0; i < Active.Count; i++)
			{
				Active[i].Restore();
			}
			Active.Clear();
			_activeUntil = -1f;
		}

		internal static void HighlightContainers(List<Container> containers, string? colorRaw, Color fallback, float durationSeconds, float alpha)
		{
			ClearHighlights();
			if (containers == null || containers.Count == 0)
			{
				return;
			}

			_baseGlow = ParseGlowColor(colorRaw, fallback);
			_glowAlpha = Mathf.Clamp01(alpha);
			float duration = Mathf.Clamp(durationSeconds, 1f, 120f);

			for (int i = 0; i < containers.Count; i++)
			{
				Container container = containers[i];
				if ((Object)(object)container == (Object)null)
				{
					continue;
				}

				HighlightedChest? highlight = HighlightedChest.TryCreate(container);
				if (highlight != null)
				{
					Active.Add(highlight);
				}
			}

			if (Active.Count == 0)
			{
				return;
			}

			_activeUntil = Time.realtimeSinceStartup + duration;
			Tick();

			if (NearbyCraftingForkedPlugin.DebugEnabled)
			{
				NearbyCraftingForkedPlugin.Debug($"Quick-deposit highlight: glowing={Active.Count}, duration={duration:0.#}s.");
			}
		}

		internal static void Tick()
		{
			if (Active.Count == 0)
			{
				return;
			}

			float now = Time.realtimeSinceStartup;
			if (now >= _activeUntil)
			{
				ClearHighlights();
				return;
			}

			float pulse = 0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(now * 5f));
			float intensity = pulse * _glowAlpha;
			Color glow = new Color(_baseGlow.r * intensity, _baseGlow.g * intensity, _baseGlow.b * intensity, 1f);
			for (int i = 0; i < Active.Count; i++)
			{
				Active[i].ApplyGlow(glow);
			}
		}

		private static void OnCommand(Terminal.ConsoleEventArgs args)
		{
			if (args.Length >= 2 && string.Equals(args[1], "clear", StringComparison.OrdinalIgnoreCase))
			{
				ClearHighlights();
				Say("Cleared nearby chest highlights.");
				return;
			}

			if (NearbyCraftingForkedPlugin.ItemLocateEnabled == null || !NearbyCraftingForkedPlugin.ItemLocateEnabled.Value)
			{
				Say("Item locate is disabled in config.");
				return;
			}

			Player? player = Player.m_localPlayer;
			if ((Object)(object)player == (Object)null)
			{
				Say("No local player.");
				return;
			}

			string pattern;
			string displayName;
			if (args.Length <= 1)
			{
				if (!TryGetHeldItemPattern(player, out pattern, out displayName))
				{
					Say("Hold an item or use: nearby <item name>");
					return;
				}
			}
			else
			{
				pattern = string.Join(" ", args.Args, 1, args.Length - 1).Trim();
				if (pattern.Length == 0)
				{
					Say("Usage: nearby [item|*pattern*|clear]");
					return;
				}
				displayName = pattern;
			}

			SearchAndHighlight(player, pattern, displayName);
		}

		private static bool TryGetHeldItemPattern(Player player, out string pattern, out string displayName)
		{
			pattern = string.Empty;
			displayName = string.Empty;
			Humanoid humanoid = (Humanoid)(object)player;
			ItemDrop.ItemData? item = humanoid.RightItem ?? humanoid.LeftItem;
			if (item?.m_shared == null)
			{
				return false;
			}

			string? prefabName = null;
			if ((Object)(object)item.m_dropPrefab != (Object)null)
			{
				prefabName = ((Object)item.m_dropPrefab).name;
			}

			if (!string.IsNullOrEmpty(prefabName))
			{
				pattern = prefabName!;
				displayName = prefabName!;
				return true;
			}

			pattern = item.m_shared.m_name;
			displayName = FormatDisplayName(item.m_shared.m_name);
			return !string.IsNullOrEmpty(pattern);
		}

		private static void SearchAndHighlight(Player player, string pattern, string displayName)
		{
			ClearHighlights();
			_baseGlow = ParseGlowColor(NearbyCraftingForkedPlugin.ItemLocateGlowColor?.Value, new Color(0.55f, 0.45f, 0.10f, 1f));
			_glowAlpha = NearbyCraftingForkedPlugin.ItemLocateGlowAlpha != null
				? Mathf.Clamp01(NearbyCraftingForkedPlugin.ItemLocateGlowAlpha.Value)
				: 1f;
			int maxHighlights = NearbyCraftingForkedPlugin.ItemLocateMaxHighlights != null
				? Mathf.Clamp(NearbyCraftingForkedPlugin.ItemLocateMaxHighlights.Value, 1, 50)
				: 10;
			float duration = NearbyCraftingForkedPlugin.ItemLocateDurationSeconds != null
				? Mathf.Clamp(NearbyCraftingForkedPlugin.ItemLocateDurationSeconds.Value, 3f, 120f)
				: 15f;

			List<Container> containers = NearbyContainers.Get(player);
			int matchCount = 0;
			List<string> matchedLabels = new List<string>();

			for (int i = 0; i < containers.Count; i++)
			{
				Container container = containers[i];
				if ((Object)(object)container == (Object)null)
				{
					continue;
				}

				Inventory? inventory;
				try
				{
					inventory = container.GetInventory();
				}
				catch
				{
					continue;
				}

				if (inventory == null || !InventoryContains(inventory, pattern, matchedLabels))
				{
					continue;
				}

				matchCount++;
				if (Active.Count < maxHighlights)
				{
					HighlightedChest? highlight = HighlightedChest.TryCreate(container);
					if (highlight != null)
					{
						Active.Add(highlight);
					}
				}
			}

			string label = matchedLabels.Count > 0
				? string.Join(", ", matchedLabels)
				: FormatDisplayName(displayName);
			if (matchCount == 0)
			{
				Say($"No nearby chests contain {FormatDisplayName(displayName)}.");
				return;
			}

			_activeUntil = Time.realtimeSinceStartup + duration;
			Tick();

			if (matchCount > Active.Count)
			{
				Say($"Found {label} in {matchCount} chests (showing nearest {Active.Count}).");
			}
			else
			{
				Say($"Found {label} in {matchCount} chests.");
			}

			if (NearbyCraftingForkedPlugin.DebugEnabled)
			{
				NearbyCraftingForkedPlugin.Debug($"Item locate '{pattern}': matches={matchCount}, glowing={Active.Count}, duration={duration:0.#}s.");
			}
		}

		private static bool InventoryContains(Inventory inventory, string pattern, List<string> matchedLabels)
		{
			List<ItemDrop.ItemData> items = inventory.GetAllItems();
			if (items == null || items.Count == 0)
			{
				return false;
			}

			bool found = false;
			for (int i = 0; i < items.Count; i++)
			{
				ItemDrop.ItemData item = items[i];
				if (item?.m_shared == null)
				{
					continue;
				}

				if (!ItemMatchesLocatePattern(item, pattern, out string matchedLabel))
				{
					continue;
				}

				found = true;
				AddUniqueLabel(matchedLabels, matchedLabel);
			}

			return found;
		}

		private static bool ItemMatchesLocatePattern(ItemDrop.ItemData item, string pattern, out string matchedLabel)
		{
			matchedLabel = string.Empty;
			string sharedName = item.m_shared.m_name;
			string localized = GetLocalizedName(sharedName);

			if (!string.IsNullOrEmpty(localized) && LocateNameMatches(localized, pattern))
			{
				matchedLabel = localized;
				return true;
			}

			if (!string.IsNullOrEmpty(sharedName) && LocateNameMatches(sharedName, pattern))
			{
				matchedLabel = !string.IsNullOrEmpty(localized) ? localized : FormatDisplayName(sharedName);
				return true;
			}

			// Also match token without '$' (e.g. item_seekerqueen_drop).
			if (!string.IsNullOrEmpty(sharedName) && sharedName[0] == '$')
			{
				string token = sharedName.Substring(1);
				if (LocateNameMatches(token, pattern))
				{
					matchedLabel = !string.IsNullOrEmpty(localized) ? localized : FormatDisplayName(sharedName);
					return true;
				}
			}

			if ((Object)(object)item.m_dropPrefab != (Object)null)
			{
				string prefabName = ((Object)item.m_dropPrefab).name;
				if (!string.IsNullOrEmpty(prefabName) && LocateNameMatches(prefabName, pattern))
				{
					matchedLabel = !string.IsNullOrEmpty(localized) ? localized : prefabName;
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Locate matching: QD-style globs, plus substring fallback so "surtling*" / "surtling"
		/// find both SurtlingCore and TrophySurtling. Also used for localized display names
		/// (e.g. "majestic carapace" → QueenDrop / $item_seekerqueen_drop).
		/// </summary>
		private static bool LocateNameMatches(string value, string pattern)
		{
			if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(pattern))
			{
				return false;
			}

			if (NearbyCraftingForkedPlugin.NameMatches(value, pattern))
			{
				return true;
			}

			string needle = pattern.Replace("*", string.Empty).Trim();
			if (needle.Length == 0)
			{
				return false;
			}

			return value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private static Func<string, string> _localizeName;

		private static bool _localizeResolved;

		internal static string GetLocalizedName(string sharedName)
		{
			if (string.IsNullOrEmpty(sharedName))
			{
				return sharedName;
			}

			EnsureLocalizeResolver();
			if (_localizeName == null)
			{
				return FormatDisplayName(sharedName);
			}

			try
			{
				string localized = _localizeName(sharedName);
				if (!string.IsNullOrEmpty(localized) && localized[0] != '$')
				{
					return localized;
				}
			}
			catch
			{
			}

			return FormatDisplayName(sharedName);
		}

		private static void EnsureLocalizeResolver()
		{
			if (_localizeResolved)
			{
				return;
			}

			_localizeResolved = true;
			try
			{
				Type locType = AccessTools.TypeByName("Localization");
				if (locType == null)
				{
					return;
				}

				MethodInfo getInstance = AccessTools.PropertyGetter(locType, "instance");
				MethodInfo localize = AccessTools.Method(locType, "Localize", new Type[] { typeof(string) });
				if (getInstance == null || localize == null)
				{
					localize = AccessTools.Method(locType, "Translate", new Type[] { typeof(string) });
				}

				if (getInstance == null || localize == null)
				{
					return;
				}

				_localizeName = (string token) =>
				{
					object instance = getInstance.Invoke(null, null);
					if (instance == null)
					{
						return token;
					}

					object result = localize.Invoke(instance, new object[] { token });
					return result as string ?? token;
				};
			}
			catch (Exception ex)
			{
				NearbyCraftingForkedPlugin.Debug("Item locate localization resolver failed: " + ex.Message);
			}
		}

		private static void AddUniqueLabel(List<string> labels, string label)
		{
			if (string.IsNullOrEmpty(label))
			{
				return;
			}

			for (int i = 0; i < labels.Count; i++)
			{
				if (string.Equals(labels[i], label, StringComparison.OrdinalIgnoreCase))
				{
					return;
				}
			}

			labels.Add(label);
		}

		private static string FormatDisplayName(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return name;
			}
			if (name.StartsWith("$item_", StringComparison.OrdinalIgnoreCase) && name.Length > 6)
			{
				string token = name.Substring(6);
				if (token.Length == 0)
				{
					return name;
				}
				return char.ToUpperInvariant(token[0]) + token.Substring(1);
			}
			if (name.StartsWith("$", StringComparison.Ordinal) && name.Length > 1)
			{
				return name.Substring(1);
			}
			return name;
		}

		private static Color ParseGlowColor(string? raw, Color fallback)
		{
			if (string.IsNullOrWhiteSpace(raw))
			{
				return fallback;
			}

			string text = StripRgbFunction(raw.Trim());
			if (text.IndexOf(',') < 0 && text.IndexOf(' ') < 0 && TryParseHexColor(text, out Color hex))
			{
				hex.a = 1f;
				return hex;
			}

			string[] parts = text.Split(new char[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length < 3)
			{
				return fallback;
			}

			if (!float.TryParse(parts[0].Trim('%'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float r)
				|| !float.TryParse(parts[1].Trim('%'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float g)
				|| !float.TryParse(parts[2].Trim('%'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float b))
			{
				return fallback;
			}

			if (r > 1f || g > 1f || b > 1f)
			{
				r /= 255f;
				g /= 255f;
				b /= 255f;
			}

			return new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b), 1f);
		}

		private static string StripRgbFunction(string text)
		{
			if (text.StartsWith("rgba", StringComparison.OrdinalIgnoreCase))
			{
				text = text.Substring(4).Trim();
			}
			else if (text.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
			{
				text = text.Substring(3).Trim();
			}

			return text.Trim().TrimStart('(').TrimEnd(')');
		}

		private static bool TryParseHexColor(string text, out Color color)
		{
			color = default;
			if (string.IsNullOrEmpty(text))
			{
				return false;
			}

			if (text[0] != '#')
			{
				text = "#" + text;
			}

			return ColorUtility.TryParseHtmlString(text, out color);
		}

		private static void Say(string message)
		{
			if ((Object)(object)Chat.instance != (Object)null)
			{
				Chat.instance.AddString(message);
			}
			else if (NearbyCraftingForkedPlugin.ModLogger != null)
			{
				NearbyCraftingForkedPlugin.ModLogger.LogInfo((object)message);
			}
		}

		private sealed class HighlightedChest
		{
			private readonly List<RendererGlow> _renderers = new List<RendererGlow>();

			internal static HighlightedChest? TryCreate(Container container)
			{
				Renderer[] renderers = ((Component)container).GetComponentsInChildren<Renderer>(true);
				if (renderers == null || renderers.Length == 0)
				{
					return null;
				}

				HighlightedChest highlight = new HighlightedChest();
				for (int i = 0; i < renderers.Length; i++)
				{
					Renderer renderer = renderers[i];
					if ((Object)(object)renderer == (Object)null || renderer is ParticleSystemRenderer)
					{
						continue;
					}

					RendererGlow? glow = RendererGlow.TryCreate(renderer);
					if (glow != null)
					{
						highlight._renderers.Add(glow);
					}
				}

				if (highlight._renderers.Count == 0)
				{
					return null;
				}

				return highlight;
			}

			internal void ApplyGlow(Color color)
			{
				for (int i = 0; i < _renderers.Count; i++)
				{
					_renderers[i].Apply(color);
				}
			}

			internal void Restore()
			{
				for (int i = 0; i < _renderers.Count; i++)
				{
					_renderers[i].Restore();
				}
				_renderers.Clear();
			}
		}

		private sealed class RendererGlow
		{
			private readonly Renderer _renderer;

			private readonly bool _usedMaterialInstances;

			private readonly Material[]? _originalSharedMaterials;

			private readonly Material[]? _instancedMaterials;

			private readonly Color[]? _originalEmission;

			private readonly bool[]? _hadEmissionKeyword;

			private RendererGlow(Renderer renderer, bool usedMaterialInstances, Material[]? originalSharedMaterials, Material[]? instancedMaterials, Color[]? originalEmission, bool[]? hadEmissionKeyword)
			{
				_renderer = renderer;
				_usedMaterialInstances = usedMaterialInstances;
				_originalSharedMaterials = originalSharedMaterials;
				_instancedMaterials = instancedMaterials;
				_originalEmission = originalEmission;
				_hadEmissionKeyword = hadEmissionKeyword;
			}

			internal static RendererGlow? TryCreate(Renderer renderer)
			{
				Material[] shared = renderer.sharedMaterials;
				if (shared == null || shared.Length == 0)
				{
					return null;
				}

				// Prefer MaterialPropertyBlock so we do not permanently alter shared materials.
				renderer.GetPropertyBlock(PropertyBlock);
				PropertyBlock.SetColor(EmissionColorProperty, Color.black);
				renderer.SetPropertyBlock(PropertyBlock);
				if (SupportsEmissionViaPropertyBlock(renderer))
				{
					return new RendererGlow(renderer, false, null, null, null, null);
				}

				Material[] originals = new Material[shared.Length];
				Material[] instances = new Material[shared.Length];
				Color[] originalEmission = new Color[shared.Length];
				bool[] hadKeyword = new bool[shared.Length];
				bool any = false;
				for (int i = 0; i < shared.Length; i++)
				{
					Material mat = shared[i];
					originals[i] = mat;
					if ((Object)(object)mat == (Object)null)
					{
						instances[i] = mat;
						continue;
					}

					Material instance = new Material(mat);
					instances[i] = instance;
					hadKeyword[i] = instance.IsKeywordEnabled("_EMISSION");
					if (instance.HasProperty(EmissionColorProperty))
					{
						originalEmission[i] = instance.GetColor(EmissionColorProperty);
						instance.EnableKeyword("_EMISSION");
						any = true;
					}
					else
					{
						originalEmission[i] = Color.black;
					}
				}

				if (!any)
				{
					for (int i = 0; i < instances.Length; i++)
					{
						if ((Object)(object)instances[i] != (Object)null && (Object)(object)instances[i] != (Object)(object)originals[i])
						{
							Object.Destroy(instances[i]);
						}
					}
					return null;
				}

				renderer.materials = instances;
				return new RendererGlow(renderer, true, originals, instances, originalEmission, hadKeyword);
			}

			private static bool SupportsEmissionViaPropertyBlock(Renderer renderer)
			{
				// Valheim chest shaders generally honor MPB emission when the keyword is on the material.
				Material[] shared = renderer.sharedMaterials;
				for (int i = 0; i < shared.Length; i++)
				{
					Material mat = shared[i];
					if ((Object)(object)mat != (Object)null && mat.HasProperty(EmissionColorProperty))
					{
						return true;
					}
				}
				return false;
			}

			internal void Apply(Color color)
			{
				if ((Object)(object)_renderer == (Object)null)
				{
					return;
				}

				if (!_usedMaterialInstances)
				{
					_renderer.GetPropertyBlock(PropertyBlock);
					PropertyBlock.SetColor(EmissionColorProperty, color);
					_renderer.SetPropertyBlock(PropertyBlock);
					return;
				}

				if (_instancedMaterials == null)
				{
					return;
				}

				for (int i = 0; i < _instancedMaterials.Length; i++)
				{
					Material mat = _instancedMaterials[i];
					if ((Object)(object)mat != (Object)null && mat.HasProperty(EmissionColorProperty))
					{
						mat.SetColor(EmissionColorProperty, color);
					}
				}
			}

			internal void Restore()
			{
				if ((Object)(object)_renderer == (Object)null)
				{
					return;
				}

				if (!_usedMaterialInstances)
				{
					_renderer.GetPropertyBlock(PropertyBlock);
					PropertyBlock.SetColor(EmissionColorProperty, Color.black);
					_renderer.SetPropertyBlock(PropertyBlock);
					return;
				}

				if (_originalSharedMaterials != null)
				{
					_renderer.sharedMaterials = _originalSharedMaterials;
				}

				if (_instancedMaterials != null)
				{
					for (int i = 0; i < _instancedMaterials.Length; i++)
					{
						Material mat = _instancedMaterials[i];
						if ((Object)(object)mat == (Object)null)
						{
							continue;
						}
						if (_originalEmission != null && mat.HasProperty(EmissionColorProperty))
						{
							mat.SetColor(EmissionColorProperty, _originalEmission[i]);
						}
						if (_hadEmissionKeyword != null && !_hadEmissionKeyword[i])
						{
							mat.DisableKeyword("_EMISSION");
						}
						Object.Destroy(mat);
					}
				}
			}
		}
	}
}
