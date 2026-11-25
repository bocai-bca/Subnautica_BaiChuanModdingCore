using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using BepInEx.Logging;
using UnityEngine;

namespace BaiChuanModdingCore.Features.TankModuleInstaller
{
	internal delegate bool AddItemDelegate(Equipment equipment, string slot, InventoryItem item, bool forced);

	/// <summary>
	/// 独立功能：点击 Tank 的升级控制台时，直接把背包里的允许模块装进
	/// TankModule1..4，不打开原版升级界面。
	/// 逻辑来自已安装的 TankModuleInstaller v1.2（反射版），
	/// 不改 Tank 本体，也不碰其他载具。
	/// </summary>
	internal static class TankModuleInstallerFeature
	{
		private const int MaxVanillaModules = 3;
		private const int MaxOurModules = 1;
		private const int TotalSlots = 4;

		private static readonly string[] SlotIds =
		{
			"TankModule1",
			"TankModule2",
			"TankModule3",
			"TankModule4",
		};

		private static MethodInfo? _addMethod;
		private static AddItemDelegate? _addDelegate;
		private static bool _resolved;
		private static bool _slotMappingEnsured;
		private static MethodInfo? _tryGetModdedTechType;
		private static bool _techTypeLookupResolved;
		private static TechType? _tankIonPiercerTechType;
		private static ManualLogSource? _log;

		internal static void Initialize(ManualLogSource log)
		{
			_log = log;
			EnsureSlotMapping();
			_log?.LogInfo("[TankModuleInstaller] ready v7 (forced)");
		}

		private static void EnsureSlotMapping()
		{
			if (_slotMappingEnsured)
			{
				return;
			}
			_slotMappingEnsured = true;

			FieldInfo? field = typeof(Equipment).GetField(
				"slotMapping",
				BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if (field == null)
			{
				_log?.LogWarning("[TankModuleInstaller] slotMapping NOT found");
				return;
			}

			System.Collections.IDictionary? map = field.GetValue(null) as System.Collections.IDictionary;
			if (map == null)
			{
				_log?.LogWarning("[TankModuleInstaller] slotMapping NOT a dictionary");
				return;
			}

			var log = new StringBuilder(180);
			log.Append("[TankModuleInstaller] slotMapping: ");
			for (int i = 0; i < SlotIds.Length; i++)
			{
				bool existed = map.Contains(SlotIds[i]);
				if (!existed)
				{
					map[SlotIds[i]] = EquipmentType.VehicleModule;
				}
				log.Append(SlotIds[i]).Append(existed ? "=已在" : "=补登记").Append(' ');
			}
			_log?.LogInfo(log.ToString());
		}

		private static void ResolveAddItem()
		{
			if (_resolved)
			{
				return;
			}
			_resolved = true;

			MethodInfo[] methods = typeof(Equipment).GetMethods(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			for (int i = 0; i < methods.Length; i++)
			{
				MethodInfo method = methods[i];
				if (method.Name != "AddItem")
				{
					continue;
				}

				ParameterInfo[] parameters = method.GetParameters();
				if (parameters.Length != 3 ||
					parameters[0].ParameterType != typeof(string) ||
					parameters[1].ParameterType != typeof(InventoryItem) ||
					parameters[2].ParameterType != typeof(bool))
				{
					continue;
				}

				_addMethod = method;
				try
				{
					_addDelegate = (AddItemDelegate)Delegate.CreateDelegate(
						typeof(AddItemDelegate), method);
				}
				catch (Exception ex)
				{
					_log?.LogWarning("[TankModuleInstaller] delegate failed: " + ex.Message);
				}
				_log?.LogInfo("[TankModuleInstaller] resolved AddItem(string, InventoryItem, bool)");
				return;
			}

			_log?.LogWarning("[TankModuleInstaller] AddItem(string, InventoryItem, bool) NOT found");
		}

		private static TechType TechOf(InventoryItem item)
		{
			if (item == null || item.item == null)
			{
				return TechType.None;
			}

			TechTag? tag = item.item.GetComponent<TechTag>();
			if (tag != null)
			{
				return tag.type;
			}
			return CraftData.GetTechType(item.item.gameObject);
		}

		private static bool IsOurModule(TechType tech)
		{
			if (tech.ToString() == "TankIonPiercer")
			{
				return true;
			}

			if (!_techTypeLookupResolved)
			{
				_techTypeLookupResolved = true;
				Type? handler = FindType("SMLHelper.V2.Handlers.TechTypeHandler");
				_tryGetModdedTechType = handler?.GetMethod(
					"TryGetModdedTechType",
					BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
					null,
					new[] { typeof(string), typeof(TechType).MakeByRefType() },
					null);
				if (_tryGetModdedTechType == null)
				{
					_log?.LogWarning("[TankModuleInstaller] TechTypeHandler.TryGetModdedTechType NOT found");
				}
			}

			if (_tryGetModdedTechType == null)
			{
				return false;
			}

			if (_tankIonPiercerTechType == null)
			{
				object?[] args = { "TankIonPiercer", default(TechType) };
				try
				{
					object? result = _tryGetModdedTechType.Invoke(null, args);
					if (result is bool ok && ok && args[1] is TechType value)
					{
						_tankIonPiercerTechType = value;
					}
				}
				catch
				{
					return false;
				}
			}

			return _tankIonPiercerTechType != null && tech == _tankIonPiercerTechType.Value;
		}

		private static bool IsVanillaAllowed(TechType tech)
		{
			return tech == TechType.VehiclePowerUpgradeModule ||
				tech == TechType.VehicleArmorPlating;
		}

		private static string? FreeSlot(Equipment equipment)
		{
			for (int i = 0; i < SlotIds.Length; i++)
			{
				if (equipment.GetItemInSlot(SlotIds[i]) == null)
				{
					return SlotIds[i];
				}
			}
			return null;
		}

		private static void CountInstalled(Equipment equipment, out int ourCount,
			out int vanillaCount, out int used)
		{
			ourCount = 0;
			vanillaCount = 0;
			used = 0;
			if (equipment == null)
			{
				return;
			}

			for (int i = 0; i < SlotIds.Length; i++)
			{
				InventoryItem? item = equipment.GetItemInSlot(SlotIds[i]);
				if (item == null)
				{
					continue;
				}
				used++;
				TechType tech = TechOf(item);
				if (IsOurModule(tech))
				{
					ourCount++;
				}
				else if (IsVanillaAllowed(tech))
				{
					vanillaCount++;
				}
			}
		}

		internal static bool TryInstall(Vehicle tank)
		{
			if (tank == null || tank.modules == null)
			{
				return false;
			}

			ResolveAddItem();
			if (_addMethod == null || _addDelegate == null)
			{
				ErrorMessage.AddMessage("安装失败：找不到 Equipment.AddItem");
				return false;
			}

			Equipment equipment = tank.modules;
			CountInstalled(equipment, out int ourCount, out int vanillaCount, out int used);

			Inventory? inventory = Inventory.main;
			if (inventory == null)
			{
				return false;
			}

			var bag = new List<InventoryItem>();
			foreach (InventoryItem item in inventory.container)
			{
				if (item != null && item.item != null)
				{
					bag.Add(item);
				}
			}

			var head = new StringBuilder(170);
			head.Append("[TankModuleInstaller] bag=").Append(bag.Count);
			head.Append(" used=").Append(used);
			head.Append(" our=").Append(ourCount);
			head.Append(" vanilla=").Append(vanillaCount);
			_log?.LogInfo(head.ToString());

			int moved = 0;
			int ourAdded = 0;
			int vanillaAdded = 0;
			bool limit = false;

			for (int i = 0; i < bag.Count; i++)
			{
				if (used >= TotalSlots)
				{
					limit = true;
					break;
				}

				InventoryItem item = bag[i];
				TechType tech = TechOf(item);
				if (tech == TechType.None)
				{
					continue;
				}

				bool ours = IsOurModule(tech);
				bool vanilla = IsVanillaAllowed(tech);
				if (!ours && !vanilla)
				{
					_log?.LogInfo("[TankModuleInstaller] skip " + tech);
					continue;
				}
				if (ours && ourCount + ourAdded >= MaxOurModules)
				{
					limit = true;
					continue;
				}
				if (vanilla && vanillaCount + vanillaAdded >= MaxVanillaModules)
				{
					limit = true;
					continue;
				}

				string? slot = FreeSlot(equipment);
				if (string.IsNullOrEmpty(slot))
				{
					limit = true;
					break;
				}

				bool ok;
				try
				{
					ok = _addDelegate(equipment, slot!, item, true);
				}
				catch (Exception ex)
				{
					_log?.LogWarning("[TankModuleInstaller] AddItem threw: " + ex.Message);
					continue;
				}

				_log?.LogInfo("[TankModuleInstaller] add " + tech + " -> " + slot + " ok=" + (ok ? "1" : "0"));
				if (!ok)
				{
					continue;
				}

				used++;
				moved++;
				if (ours)
				{
					ourAdded++;
				}
				else
				{
					vanillaAdded++;
				}
			}

			var result = new StringBuilder(240);
			result.Append(moved > 0 ? "已安装 " + moved + " 个模块" : "没有安装任何模块");
			result.Append("（震爆弹 ").Append(ourCount + ourAdded).Append('/').Append(MaxOurModules);
			result.Append("，动力/装甲 ").Append(vanillaCount + vanillaAdded).Append('/').Append(MaxVanillaModules);
			result.Append("，占用 ").Append(used).Append('/').Append(TotalSlots).Append(" 槽）");
			ErrorMessage.AddMessage(result.ToString());
			if (limit)
			{
				ErrorMessage.AddMessage("已达上限：动力/装甲合计最多 3 个，震爆弹最多 1 个");
			}
			return moved > 0;
		}

		private static Type? FindType(string fullName)
		{
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			for (int i = 0; i < assemblies.Length; i++)
			{
				try
				{
					Type? type = assemblies[i].GetType(fullName, false);
					if (type != null)
					{
						return type;
					}
				}
				catch
				{
				}
			}
			return null;
		}
	}
}
