using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BaiChuanModdingCore.Features.AquariumOverflowResearchStation
{
	/// <summary>
	/// 独立功能：大型水族馆 / 小水族馆 / 生物反应堆满员后，
	/// 把溢出的鱼送进 EquivalentExchange 的研究站容器。
	///
	/// 目标模组都是 QMod，核心在启动时反射解析并手动挂 Harmony。
	/// </summary>
	internal static class AquariumOverflowResearchStationFeature
	{
		internal static ManualLogSource? Log;

		private static Type? _stationType;
		private static FieldInfo? _fStorage;
		private static PropertyInfo? _pStorage;
		private static MethodInfo? _mIsAllowed;
		private static PropertyInfo? _pReactorContainer;

		private static WaterPark? _handledWp;
		private static bool _handledFlag;
		private static int _movedWp;
		private static int _movedAq;
		private static bool _warnedNoStation;
		private static bool _installed;

		private const float SubRootListTtl = 2f;
		private const float EmptyRetryCooldown = 3f;
		private const float ContainerTtl = 2f;
		private const float MissingPrefabRetry = 3f;

		private sealed class SubRootCache
		{
			internal BaseBioReactor[] Reactors = Array.Empty<BaseBioReactor>();
			internal Component[] Stations = Array.Empty<Component>();
			internal float ReactorsNextScan;
			internal float StationsNextScan;
		}

		private sealed class ContainerCache
		{
			internal StorageContainer? Storage;
			internal ItemsContainer? Container;
			internal float NextRefresh;
		}

		private sealed class PrefabCache
		{
			internal GameObject? Prefab;
			internal float NextRetry;
		}

		private static readonly Dictionary<SubRoot, SubRootCache> SubRootCaches =
			new Dictionary<SubRoot, SubRootCache>();
		private static readonly Dictionary<Component, ContainerCache> ContainerCaches =
			new Dictionary<Component, ContainerCache>();
		private static readonly Dictionary<TechType, PrefabCache> PrefabCaches =
			new Dictionary<TechType, PrefabCache>();

		internal static void Initialize(Harmony harmony, ManualLogSource log)
		{
			Log = log;
			if (_installed)
			{
				return;
			}

			_stationType = ResolveStationType();
			if (_stationType == null)
			{
				Log?.LogWarning("[AquariumOverflowResearchStation] 未找到 ItemResearchStation（EquivalentExchange 未安装？），功能禁用");
				return;
			}

			_fStorage = _stationType.GetField(
				"storageContainer",
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (_fStorage == null)
			{
				_pStorage = _stationType.GetProperty(
					"storageContainer",
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			}

			_mIsAllowed = _stationType.GetMethod(
				"IsAllowedToAdd",
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
				null,
				new[] { typeof(Pickupable), typeof(bool) },
				null);

			if (_fStorage == null && _pStorage == null)
			{
				Log?.LogWarning("[AquariumOverflowResearchStation] 研究站没有 storageContainer（字段/属性都没有），功能禁用");
				return;
			}

			MethodInfo? tryBreed = AccessTools.Method(
				typeof(WaterPark),
				"TryBreed",
				new[] { typeof(WaterParkCreature) });
			if (tryBreed != null)
			{
				harmony.Patch(tryBreed,
					new HarmonyMethod(typeof(AquariumOverflowResearchStationFeature).GetMethod(
						nameof(WaterParkTryBreed_Prefix),
						BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)));
			}

			Type? betterAcuType = FindType("BetterACU.Patches.WaterParkBreedPostfix");
			MethodInfo? betterAcuPostfix = betterAcuType != null
				? AccessTools.Method(betterAcuType, "Postfix")
				: null;
			if (betterAcuPostfix != null)
			{
				harmony.Patch(betterAcuPostfix,
					new HarmonyMethod(typeof(AquariumOverflowResearchStationFeature).GetMethod(
						nameof(BetterAcuOverflow_Prefix),
						BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)));
			}

			Type? aquariumOverflowType = FindType("AquariumOverflow.Main");
			MethodInfo? tryOverflow = aquariumOverflowType != null
				? AccessTools.Method(aquariumOverflowType, "TryOverflowIntoBioreactors")
				: null;
			if (tryOverflow != null)
			{
				harmony.Patch(tryOverflow,
					null,
					new HarmonyMethod(typeof(AquariumOverflowResearchStationFeature).GetMethod(
						nameof(AquariumOverflow_Postfix),
						BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)));
			}

			_installed = true;
			Log?.LogInfo(
				"[AquariumOverflowResearchStation] 已启用：" +
				"大型水族馆溢出 → 生物反应堆满 → 研究站（WaterPark=" +
				(tryBreed != null) + " BetterACU=" + (betterAcuPostfix != null) +
				" AO=" + (tryOverflow != null) + "）");
		}

		private static bool WaterParkTryBreed_Prefix(WaterPark __instance, WaterParkCreature creature)
		{
			try
			{
				if (__instance == null || creature == null)
				{
					return true;
				}

				bool hasSpace = true;
				try
				{
					hasSpace = __instance.HasFreeSpace();
				}
				catch
				{
				}

				if (hasSpace)
				{
					return true;
				}
				if (ReactorHasRoom(__instance))
				{
					return true;
				}
				if (TryStationOverflow(__instance, creature))
				{
					return false;
				}
				return true;
			}
			catch
			{
				return true;
			}
		}

		private static bool BetterAcuOverflow_Prefix(WaterPark __instance)
		{
			try
			{
				if (_handledFlag && ReferenceEquals(_handledWp, __instance))
				{
					_handledFlag = false;
					_handledWp = null;
					return false;
				}
			}
			catch
			{
			}
			return true;
		}

		private static SubRootCache GetSubRootCache(SubRoot subRoot)
		{
			if (SubRootCaches.Count > 64)
			{
				var dead = new List<SubRoot>();
				foreach (KeyValuePair<SubRoot, SubRootCache> pair in SubRootCaches)
				{
					if (pair.Key == null)
					{
						dead.Add(pair.Key!);
					}
				}
				for (int i = 0; i < dead.Count; i++)
				{
					SubRootCaches.Remove(dead[i]);
				}
			}

			if (!SubRootCaches.TryGetValue(subRoot, out SubRootCache? cache))
			{
				cache = new SubRootCache();
				SubRootCaches[subRoot] = cache;
			}
			return cache;
		}

		private static BaseBioReactor[] GetReactors(SubRoot subRoot)
		{
			SubRootCache cache = GetSubRootCache(subRoot);
			if (Time.time < cache.ReactorsNextScan &&
				(cache.Reactors.Length == 0 || HasLive(cache.Reactors)))
			{
				return cache.Reactors;
			}

			BaseBioReactor[] reactors = subRoot.GetComponentsInChildren<BaseBioReactor>(true);
			cache.Reactors = reactors ?? Array.Empty<BaseBioReactor>();
			cache.ReactorsNextScan = Time.time +
				(cache.Reactors.Length == 0 ? EmptyRetryCooldown : SubRootListTtl);
			return cache.Reactors;
		}

		private static Component[] GetStations(SubRoot subRoot)
		{
			if (_stationType == null)
			{
				return Array.Empty<Component>();
			}

			SubRootCache cache = GetSubRootCache(subRoot);
			if (Time.time < cache.StationsNextScan &&
				(cache.Stations.Length == 0 || HasLive(cache.Stations)))
			{
				return cache.Stations;
			}

			Component[] stations = subRoot.GetComponentsInChildren(_stationType, true);
			cache.Stations = stations ?? Array.Empty<Component>();
			cache.StationsNextScan = Time.time +
				(cache.Stations.Length == 0 ? EmptyRetryCooldown : SubRootListTtl);
			return cache.Stations;
		}

		private static bool HasLive<T>(T[] array) where T : UnityEngine.Object
		{
			if (array == null || array.Length == 0)
			{
				return false;
			}
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] != null)
				{
					return true;
				}
			}
			return false;
		}

		private static bool ReactorHasRoom(WaterPark waterPark)
		{
			try
			{
				SubRoot? subRoot = waterPark.GetComponentInParent<SubRoot>();
				if (subRoot == null)
				{
					return false;
				}

				BaseBioReactor[] reactors = GetReactors(subRoot);
				if (reactors.Length == 0)
				{
					return false;
				}

				for (int i = 0; i < reactors.Length; i++)
				{
					BaseBioReactor reactor = reactors[i];
					if (reactor == null)
					{
						continue;
					}

					ItemsContainer? container = null;
					try
					{
						if (_pReactorContainer == null)
						{
							_pReactorContainer = typeof(BaseBioReactor).GetProperty(
								"container",
								BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
						}
						if (_pReactorContainer != null)
						{
							container = _pReactorContainer.GetValue(reactor, null) as ItemsContainer;
						}
					}
					catch
					{
					}

					if (container != null && container.HasRoomFor(1, 1))
					{
						return true;
					}
				}
			}
			catch
			{
			}
			return false;
		}

		private static bool TryStationOverflow(WaterPark waterPark, WaterParkCreature creature)
		{
			if (_stationType == null)
			{
				return false;
			}

			try
			{
				TechType fishType = TechType.None;
				try
				{
					if (creature.pickupable != null)
					{
						fishType = creature.pickupable.GetTechType();
					}
				}
				catch
				{
				}
				if (fishType == TechType.None)
				{
					return false;
				}

				SubRoot? subRoot = waterPark.GetComponentInParent<SubRoot>();
				if (subRoot == null)
				{
					return false;
				}

				Component[] stations = GetStations(subRoot);
				if (stations.Length == 0)
				{
					if (!_warnedNoStation)
					{
						_warnedNoStation = true;
						Log?.LogWarning("[AquariumOverflowResearchStation] 反应堆已满，但基地内没有研究站可接收溢出鱼");
					}
					return false;
				}

				Vector2int itemSize = CraftData.GetItemSize(fishType);
				for (int i = 0; i < stations.Length; i++)
				{
					Component station = stations[i];
					if (station == null)
					{
						continue;
					}

					ItemsContainer? container = GetContainer(station);
					if (container == null || !container.HasRoomFor(itemSize.x, itemSize.y))
					{
						continue;
					}

					Pickupable? fish = SpawnFish(fishType);
					if (fish == null)
					{
						continue;
					}
					if (!AllowedPickupable(station, fish))
					{
						UnityEngine.Object.Destroy(fish.gameObject);
						continue;
					}

					InventoryItem? item = null;
					try
					{
						item = container.AddItem(fish);
					}
					catch
					{
					}
					if (item == null)
					{
						UnityEngine.Object.Destroy(fish.gameObject);
						continue;
					}

					try
					{
						creature.ResetBreedTime();
					}
					catch
					{
					}

					_handledWp = waterPark;
					_handledFlag = true;
					_movedWp++;
					if (_movedWp == 1 || _movedWp % 10 == 0)
					{
						Log?.LogInfo("[AquariumOverflowResearchStation] 反应堆满 → 研究站已接收 " +
							_movedWp + " 条溢出鱼（最近：" + fishType + "）");
					}
					return true;
				}
			}
			catch
			{
			}
			return false;
		}

		private static void AquariumOverflow_Postfix(SubRoot subRoot, TechType fishType, ref int breedCount)
		{
			try
			{
				if (breedCount <= 0 || subRoot == null || _stationType == null)
				{
					return;
				}

				Component[] stations = GetStations(subRoot);
				if (stations.Length == 0)
				{
					return;
				}

				Vector2int itemSize = CraftData.GetItemSize(fishType);
				for (int i = 0; i < stations.Length; i++)
				{
					Component station = stations[i];
					if (station == null || breedCount <= 0)
					{
						continue;
					}

					ItemsContainer? container = GetContainer(station);
					if (container == null)
					{
						continue;
					}

					while (breedCount > 0 && container.HasRoomFor(itemSize.x, itemSize.y))
					{
						Pickupable? fish = SpawnFish(fishType);
						if (fish == null)
						{
							break;
						}
						if (!AllowedPickupable(station, fish))
						{
							UnityEngine.Object.Destroy(fish.gameObject);
							break;
						}

						InventoryItem? item = null;
						try
						{
							item = container.AddItem(fish);
						}
						catch
						{
						}
						if (item == null)
						{
							UnityEngine.Object.Destroy(fish.gameObject);
							break;
						}

						breedCount--;
						_movedAq++;
						if (_movedAq == 1 || _movedAq % 10 == 0)
						{
							Log?.LogInfo("[AquariumOverflowResearchStation] 小水族馆溢出 → 研究站已接收 " +
								_movedAq + " 条（最近：" + fishType + "）");
						}
					}
				}
			}
			catch
			{
			}
		}

		private static bool AllowedPickupable(Component station, Pickupable pickupable)
		{
			if (_mIsAllowed == null)
			{
				return true;
			}

			try
			{
				try
				{
					return (bool)_mIsAllowed.Invoke(station, new object[] { pickupable, false });
				}
				catch
				{
					return false;
				}
			}
			catch
			{
				return true;
			}
		}

		private static ItemsContainer? GetContainer(Component station)
		{
			if (station == null)
			{
				return null;
			}

			try
			{
				if (ContainerCaches.TryGetValue(station, out ContainerCache? cache) &&
					cache.Container != null &&
					cache.Storage != null &&
					cache.Storage.container == cache.Container &&
					Time.time < cache.NextRefresh)
				{
					return cache.Container;
				}

				StorageContainer? storage = null;
				if (_fStorage != null)
				{
					storage = _fStorage.GetValue(station) as StorageContainer;
				}
				else if (_pStorage != null)
				{
					storage = _pStorage.GetValue(station, null) as StorageContainer;
				}

				if (storage == null)
				{
					storage = station.GetComponent<StorageContainer>();
				}
				if (storage == null)
				{
					storage = station.GetComponentInChildren<StorageContainer>(true);
				}

				if (cache == null)
				{
					cache = new ContainerCache();
					ContainerCaches[station] = cache;
				}
				cache.Storage = storage;
				cache.Container = storage != null ? storage.container : null;
				cache.NextRefresh = Time.time + ContainerTtl;

				if (ContainerCaches.Count > 128)
				{
					var dead = new List<Component>();
					foreach (KeyValuePair<Component, ContainerCache> pair in ContainerCaches)
					{
						if (pair.Key == null)
						{
							dead.Add(pair.Key!);
						}
					}
					for (int i = 0; i < dead.Count; i++)
					{
						ContainerCaches.Remove(dead[i]);
					}
				}

				return cache.Container;
			}
			catch
			{
				return null;
			}
		}

		private static Pickupable? SpawnFish(TechType fishType)
		{
			try
			{
				if (!PrefabCaches.TryGetValue(fishType, out PrefabCache? cache))
				{
					cache = new PrefabCache();
					PrefabCaches[fishType] = cache;
				}

				GameObject? prefab = cache.Prefab;
				if (prefab == null && Time.time >= cache.NextRetry)
				{
					prefab = CraftData.GetPrefabForTechType(fishType, false);
					cache.Prefab = prefab;
					cache.NextRetry = prefab != null ? 0f : Time.time + MissingPrefabRetry;
				}

				if (prefab == null)
				{
					return null;
				}

				GameObject gameObject = UnityEngine.Object.Instantiate(prefab);
				Pickupable? pickupable = gameObject.GetComponent<Pickupable>();
				if (pickupable == null)
				{
					pickupable = gameObject.AddComponent<Pickupable>();
				}
				pickupable.Pickup(false);
				return pickupable;
			}
			catch
			{
				return null;
			}
		}

		private static Type? ResolveStationType()
		{
			Type? type = FindType("EquivalentExchange.Monobehaviours.ItemResearchStation");
			if (type != null && HasStorage(type))
			{
				return type;
			}

			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			for (int i = 0; i < assemblies.Length; i++)
			{
				Assembly assembly = assemblies[i];
				string assemblyName;
				try
				{
					assemblyName = assembly.GetName().Name ?? string.Empty;
				}
				catch
				{
					continue;
				}

				// 兜底只扫可能包含 EquivalentExchange 的程序集，
				// 避免在 200+ 模组环境里对每个程序集做 GetTypes()。
				if (assemblyName.IndexOf("EquivalentExchange", StringComparison.OrdinalIgnoreCase) < 0 &&
					assemblyName.IndexOf("Exchange", StringComparison.OrdinalIgnoreCase) < 0 &&
					assemblyName.IndexOf("Research", StringComparison.OrdinalIgnoreCase) < 0)
				{
					continue;
				}

				Type[] types;
				try
				{
					types = assembly.GetTypes();
				}
				catch
				{
					continue;
				}

				for (int k = 0; k < types.Length; k++)
				{
					Type candidate = types[k];
					if (candidate != null &&
						candidate.Name.IndexOf("ItemResearchStation", StringComparison.Ordinal) >= 0 &&
						HasStorage(candidate))
					{
						return candidate;
					}
				}
			}

			return null;
		}

		private static bool HasStorage(Type type)
		{
			try
			{
				return type.GetField(
					"storageContainer",
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) != null ||
					type.GetProperty(
						"storageContainer",
						BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) != null;
			}
			catch
			{
				return false;
			}
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
