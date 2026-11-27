using System;
using System.Collections;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BaiChuanModdingCore.Features.AnthVoidTentacleFix
{
	/// <summary>
	/// 独立功能：修正 AnthCreatures 虚空触手生成条件。
	/// 只在玩家进入虚空/坐标超出范围时生成，离开或 Cthulhu 不活跃时清理。
	///
	/// 优化点：
	///   - 反射字段/方法只解析一次
	///   - CthulhuSpawner 搜索从每秒一次降到每 2 秒一次
	/// </summary>
	internal static class AnthVoidTentacleFixFeature
	{
		private const float VoidDistance = 2000f;
		private const int MaxTentacles = 6;
		private const float CthulhuScanInterval = 2f;

		private static Type? _tentacleSpawnerType;
		private static Type? _cthulhuSpawnerType;

		private static FieldInfo? _timeTrySpawnAgain;
		private static FieldInfo? _spawnedTentacles;
		private static FieldInfo? _spawnRadius;
		private static MethodInfo? _getRandomTentaclePosition;
		private static MethodInfo? _spawnTentacle;
		private static MethodInfo? _despawnTentacle;

		private static FieldInfo? _cthulhuTimerRunning;
		private static FieldInfo? _cthulhuCurrent;
		private static Component[] _cthulhuSpawners = Array.Empty<Component>();
		private static float _nextCthulhuScan;

		private static float _nextDiag;
		private static int _spawnCount;

		internal static ManualLogSource? Log;

		internal static void Initialize(Harmony harmony, ManualLogSource log)
		{
			Log = log;
			_tentacleSpawnerType = FindType("AnthCreaturesMod.Mono.IdleTentacleSpawner");
			_cthulhuSpawnerType = FindType("AnthCreaturesMod.Mono.CthulhuSpawner");

			if (_tentacleSpawnerType != null)
			{
				const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static |
					BindingFlags.Public | BindingFlags.NonPublic;
				_timeTrySpawnAgain = _tentacleSpawnerType.GetField("timeTrySpawnAgain", flags);
				_spawnedTentacles = _tentacleSpawnerType.GetField("spawnedTentacles", flags);
				_spawnRadius = _tentacleSpawnerType.GetField("spawnRadius", flags);
				_getRandomTentaclePosition = _tentacleSpawnerType.GetMethod("GetRandomTentaclePosition", flags);
				_spawnTentacle = _tentacleSpawnerType.GetMethod("SpawnTentacle", flags);
				_despawnTentacle = _tentacleSpawnerType.GetMethod("DespawnTentacle", flags);
			}

			if (_cthulhuSpawnerType != null)
			{
				const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static |
					BindingFlags.Public | BindingFlags.NonPublic;
				_cthulhuTimerRunning = _cthulhuSpawnerType.GetField("timerRunning", flags);
				_cthulhuCurrent = _cthulhuSpawnerType.GetField("currentCthulhu", flags);
			}

			bool patched = false;
			if (_tentacleSpawnerType != null)
			{
				MethodInfo? update = _tentacleSpawnerType.GetMethod(
					"Update",
					BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				if (update != null)
				{
					try
					{
						harmony.Patch(update, new HarmonyMethod(
							typeof(AnthVoidTentacleFixFeature).GetMethod(
								nameof(TentacleUpdate_Prefix),
								BindingFlags.Static | BindingFlags.NonPublic)));
						patched = true;
					}
					catch (Exception ex)
					{
						Log?.LogWarning("[AnthVoidTentacleFix] patch failed: " + ex.Message);
					}
				}
			}

			Log?.LogInfo("[AnthVoidTentacleFix] ready  tentacleGate=" + patched +
				"  voidDistance=" + VoidDistance +
				"  maxTentacles=" + MaxTentacles +
				"  spawnerType=" + (_tentacleSpawnerType != null) +
				"  cthulhuType=" + (_cthulhuSpawnerType != null));
		}

		private static bool TentacleUpdate_Prefix(object __instance)
		{
			try
			{
				if (__instance == null || _tentacleSpawnerType == null ||
					_timeTrySpawnAgain == null)
				{
					return true;
				}

				float next = (float)_timeTrySpawnAgain.GetValue(__instance);
				if (Time.time <= next)
				{
					return false;
				}
				_timeTrySpawnAgain.SetValue(__instance, Time.time + 1f);

				bool byCoord = false;
				bool byBiome = false;
				if (Player.main != null)
				{
					Vector3 position = Player.main.transform.position;
					byCoord = Mathf.Abs(position.x) > VoidDistance ||
						Mathf.Abs(position.z) > VoidDistance;
					byBiome = (Player.main.GetBiomeString() ?? string.Empty)
						.ToLower() == "void";
				}

				bool inVoid = byCoord || byBiome;
				bool cthulhuAlive = IsCthulhuAlive();
				IList? tentacles = _spawnedTentacles != null
					? _spawnedTentacles.GetValue(__instance) as IList
					: null;

				if (Time.time >= _nextDiag)
				{
					_nextDiag = Time.time + 5f;
					try
					{
						Vector3 position = Player.main != null
							? Player.main.transform.position
							: Vector3.zero;
						if (Mathf.Abs(position.x) > 1500f ||
							Mathf.Abs(position.z) > 1500f)
						{
							Log?.LogInfo(
								"[AnthVoidTentacleFix] diag pos=(" +
								position.x.ToString("0") + "," +
								position.y.ToString("0") + "," +
								position.z.ToString("0") + ") biome=" +
								(Player.main != null ? Player.main.GetBiomeString() : "?") +
								" inVoid=" + inVoid +
								" byCoord=" + byCoord +
								" byBiome=" + byBiome +
								" cthulhuActive=" + cthulhuAlive +
								" count=" + (tentacles?.Count ?? -1) +
								" radius=" + ReadRadius(__instance) +
								" spawned=" + _spawnCount);
						}
					}
					catch
					{
					}
				}

				if (inVoid && cthulhuAlive && tentacles != null &&
					tentacles.Count < MaxTentacles)
				{
					if (_getRandomTentaclePosition == null || _spawnTentacle == null)
					{
						Log?.LogWarning("[AnthVoidTentacleFix] missing spawn methods");
					}
					else
					{
						Vector3 position = (Vector3)_getRandomTentaclePosition.Invoke(__instance, null);
						_spawnTentacle.Invoke(__instance, new object[] { position });
						_spawnCount++;
						Log?.LogInfo("[AnthVoidTentacleFix] SPAWN tentacle #" +
							_spawnCount + " at (" +
							position.x.ToString("0") + "," +
							position.y.ToString("0") + "," +
							position.z.ToString("0") + ") count=" + tentacles.Count);
					}
				}

				if ((!inVoid || !cthulhuAlive) && tentacles != null &&
					tentacles.Count > 0 && _despawnTentacle != null)
				{
					object[] copy = new object[tentacles.Count];
					tentacles.CopyTo(copy, 0);
					for (int i = 0; i < copy.Length; i++)
					{
						try
						{
							_despawnTentacle.Invoke(__instance, new[] { copy[i] });
						}
						catch
						{
						}
					}
					tentacles.Clear();
					Log?.LogInfo("[AnthVoidTentacleFix] tentacles cleared (inVoid=" +
						inVoid + " cthulhu=" + cthulhuAlive + ")");
				}

				return false;
			}
			catch (Exception ex)
			{
				Log?.LogWarning("[AnthVoidTentacleFix] tentacle gate: " + ex.Message);
				return true;
			}
		}

		private static string ReadRadius(object spawner)
		{
			try
			{
				return _spawnRadius == null
					? "?"
					: ((float)_spawnRadius.GetValue(spawner)).ToString("0.0");
			}
			catch
			{
				return "?";
			}
		}

		private static bool IsCthulhuAlive()
		{
			try
			{
				if (_cthulhuSpawnerType == null)
				{
					return false;
				}

				if (Time.time >= _nextCthulhuScan)
				{
					_nextCthulhuScan = Time.time + CthulhuScanInterval;
					UnityEngine.Object[] objects =
						Resources.FindObjectsOfTypeAll(_cthulhuSpawnerType);
					var components = new System.Collections.Generic.List<Component>();
					for (int i = 0; i < objects.Length; i++)
					{
						if (objects[i] is Component component && component != null)
						{
							components.Add(component);
						}
					}
					_cthulhuSpawners = components.ToArray();
				}

				for (int i = 0; i < _cthulhuSpawners.Length; i++)
				{
					Component component = _cthulhuSpawners[i];
					if (component == null)
					{
						continue;
					}

					if (_cthulhuTimerRunning != null)
					{
						object value = _cthulhuTimerRunning.GetValue(component);
						if (value is bool running && running)
						{
							return true;
						}
					}

					if (_cthulhuCurrent != null)
					{
						GameObject? current =
							_cthulhuCurrent.GetValue(component) as GameObject;
						if (current != null)
						{
							return true;
						}
					}
				}

				return false;
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
