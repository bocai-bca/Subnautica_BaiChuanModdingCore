using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UWE;
using UnityEngine;

namespace BaiChuanModdingCore.Features.LeviathanListExpander
{
	/// <summary>
	/// 独立功能：替换 Socknautica 的利维坦权重表，并让非 GUID 的模组
	/// classId 能通过原版 SpawnPrefab 路径生成。
	///
	/// 优化点：
	///   - 启动探测从每帧改为每 0.5 秒
	///   - LootGeneration 类型/字段/方法缓存，不在每次回调里反复反射
	/// </summary>
	internal static class LeviathanListExpanderFeature
	{
		private static readonly string[] WeightedIds = BuildWeightedIds();

		private static ConfigEntry<bool> _enabled = null!;
		private static Harmony _harmony = null!;
		private static ManualLogSource? _log;
		private static MonoBehaviour? _host;

		private static Type? _lootGenerationType;
		private static FieldInfo? _leviathanClassIdsField;

		private static bool _patchedList;
		private static bool _patchedSpawn;

		private static Type? _modPrefabType;
		private static MethodInfo? _tryGetFromClassId;
		private static PropertyInfo? _techTypeProp;

		internal static void Initialize(ConfigFile config, Harmony harmony,
			ManualLogSource log, MonoBehaviour host)
		{
			_log = log;
			_host = host;
			_harmony = harmony;
			_enabled = config.Bind(
				"LeviathanListExpander.General",
				"Enable",
				true,
				"是否启用利维坦权重表替换。");

			_host.StartCoroutine(InitPatches());
		}

		private static IEnumerator InitPatches()
		{
			float waited = 0f;
			while (waited < 120f && (!_patchedList || !_patchedSpawn))
			{
				TryPatch();
				if (_patchedList && _patchedSpawn)
				{
					break;
				}
				yield return new WaitForSecondsRealtime(0.5f);
				waited += 0.5f;
			}

			if (_patchedList && _patchedSpawn)
			{
				_log?.LogInfo("[LeviathanList] patches installed (list=True spawn=True)");
			}
			else
			{
				_log?.LogWarning("[LeviathanList] timeout: list=" + _patchedList +
					" spawn=" + _patchedSpawn);
			}
		}

		private static void TryPatch()
		{
			try
			{
				if (_lootGenerationType == null)
				{
					_lootGenerationType = Type.GetType(
						"Socknautica.Mono.LootGeneration, Socknautica",
						false);
				}
				Type? type = _lootGenerationType;
				if (type == null)
				{
					return;
				}

				if (!_patchedList)
				{
					MethodInfo? method = type.GetMethod(
						"DetermineClassIDs",
						BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
					if (method != null)
					{
						_harmony.Patch(method, null, new HarmonyMethod(
							typeof(LeviathanListExpanderFeature).GetMethod(
								nameof(ListPostfix),
								BindingFlags.Static | BindingFlags.NonPublic)));
						_patchedList = true;
					}
				}

				if (!_patchedSpawn)
				{
					MethodInfo? method = type
						.GetMethods(BindingFlags.Instance | BindingFlags.Static |
							BindingFlags.Public | BindingFlags.NonPublic)
						.FirstOrDefault(x => x.Name == "SpawnPrefab" && IsStringVecQuatVec(x));
					if (method != null)
					{
						_harmony.Patch(method, new HarmonyMethod(
							typeof(LeviathanListExpanderFeature).GetMethod(
								nameof(SpawnPrefabPrefix),
								BindingFlags.Static | BindingFlags.NonPublic)));
						_patchedSpawn = true;
					}
				}
			}
			catch (Exception ex)
			{
				_log?.LogError("[LeviathanList] patch failed: " + ex);
			}
		}

		private static bool IsStringVecQuatVec(MethodInfo method)
		{
			ParameterInfo[] parameters = method.GetParameters();
			return parameters.Length == 4 &&
				parameters[0].ParameterType == typeof(string) &&
				parameters[1].ParameterType == typeof(Vector3) &&
				parameters[2].ParameterType == typeof(Quaternion) &&
				parameters[3].ParameterType == typeof(Vector3);
		}

		private static void ListPostfix()
		{
			if (!_enabled.Value)
			{
				return;
			}

			try
			{
				if (_lootGenerationType == null)
				{
					_lootGenerationType = Type.GetType(
						"Socknautica.Mono.LootGeneration, Socknautica",
						false);
				}
				if (_lootGenerationType == null)
				{
					return;
				}

				if (_leviathanClassIdsField == null)
				{
					_leviathanClassIdsField = _lootGenerationType.GetField(
						"leviathanClassIDs",
						BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				}
				if (_leviathanClassIdsField == null)
				{
					return;
				}

				if (_leviathanClassIdsField.GetValue(null) is List<string> list)
				{
					list.Clear();
					list.AddRange(WeightedIds);
				}
			}
			catch
			{
			}
		}

		private static bool SpawnPrefabPrefix(string __0, Vector3 __1,
			Quaternion __2, Vector3 __3, ref GameObject __result)
		{
			if (!_enabled.Value)
			{
				return true;
			}
			if (__0 == null || __0.Length == 36)
			{
				return true;
			}

			GameObject? prefab = ResolveModPrefab(__0);
			if (prefab == null)
			{
				return true;
			}

			GameObject spawned = UnityEngine.Object.Instantiate(prefab, __1, __2);
			spawned.transform.localScale = __3 == Vector3.zero ? Vector3.one : __3;
			spawned.SetActive(true);
			__result = spawned;
			return false;
		}

		private static GameObject? ResolveModPrefab(string classId)
		{
			try
			{
				if (PrefabDatabase.TryGetPrefab(classId, out GameObject prefab) &&
					prefab != null)
				{
					return prefab;
				}

				if (_modPrefabType == null)
				{
					_modPrefabType = Type.GetType(
						"SMLHelper.V2.Assets.ModPrefab, SMLHelper",
						false);
					if (_modPrefabType == null)
					{
						return null;
					}
					_tryGetFromClassId = _modPrefabType.GetMethod(
						"TryGetFromClassId",
						BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
					_techTypeProp = _modPrefabType.GetProperty("TechType");
				}

				if (_tryGetFromClassId == null || _techTypeProp == null)
				{
					return null;
				}

				object?[] args = { classId, null };
				if (!(bool)_tryGetFromClassId.Invoke(null, args) || args[1] == null)
				{
					return null;
				}

				object modPrefab = args[1]!;
				TechType techType = (TechType)_techTypeProp.GetValue(modPrefab, null)!;
				GameObject? byTechType = CraftData.GetPrefabForTechType(techType);
				if (byTechType != null)
				{
					return byTechType;
				}

				MethodInfo? getGameObject = _modPrefabType.GetMethod(
					"GetGameObject", Type.EmptyTypes);
				return getGameObject?.Invoke(modPrefab, null) as GameObject;
			}
			catch
			{
				return null;
			}
		}

		private static string[] BuildWeightedIds()
		{
			var list = new List<string>();
			Add(list, "5ea36b37-300f-4f01-96fa-003ae47c61e5", 9, "GhostLeviathanJuvenile");
			Add(list, "54701bfc-bb1a-4a84-8f79-ba4f76691bef", 10, "GhostLeviathan");
			Add(list, "54701bfc-bb1a-4a84-8f79-ba4f76691bef", 9, "VoidGhostLeviathan");
			Add(list, "f78942c3-87e7-4015-865a-5ae4d8bd9dcb", 10, "ReaperLeviathan");
			Add(list, "ff43eacd-1a9e-4182-ab7b-aa43c16d1e53", 10, "SeaDragon");
			Add(list, "8d3d3c8b-9290-444a-9fea-8e5493ecd6fe", 10, "Reefback");
			Add(list, "AbyssalBlaza", 10, "AbyssalBlaza");
			Add(list, "AncientBloop", 6, "AncientBloop");
			Add(list, "GulperLeviathan", 10, "GulperLeviathan");
			Add(list, "Mosasaurus", 10, "Mosasaurus");
			Add(list, "Kraken", 5, "Kraken");
			Add(list, "GargantuanLeviathan", 1, "GargantuanLeviathan");
			return list.ToArray();
		}

		private static void Add(List<string> list, string id, int weight, string name)
		{
			for (int i = 0; i < weight; i++)
			{
				list.Add(id);
			}
		}
	}
}
