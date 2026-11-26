using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Story;
using UnityEngine;

namespace BaiChuanModdingCore.Features.ArenaTeleportBlocker;

internal static class ArenaTeleportBlockerFeature
{
	internal static ConfigEntry<bool> Enabled;

	internal static ConfigEntry<float> Radius;

	internal static ConfigEntry<bool> Verbose;

	internal static ManualLogSource? Log;

	private static Harmony _harmony = null!;

	private static readonly HashSet<string> Done = new HashSet<string>();

	private static bool _patchedArenaSpawner;

	private static readonly HashSet<string> BlockLogged = new HashSet<string>();

	private static FieldInfo _arenaMainField;

	private static FieldInfo _arenaPosField;

	private static FieldInfo _arenaHeightField;

	internal static void Initialize(ConfigFile config, Harmony harmony, ManualLogSource log, MonoBehaviour host)
	{
		Log = log;
		_harmony = harmony;
		Enabled = config.Bind("ArenaTeleportBlocker.General", "Enable", defaultValue: true, "总开关");
		Radius = config.Bind("ArenaTeleportBlocker.General", "ArenaRadius", 400f, "竞技场判定半径（米）；中心取 Socknautica ArenaSpawner.arenaPos，高度取 arenaHeight/2");
		Verbose = config.Bind("ArenaTeleportBlocker.General", "VerboseLog", defaultValue: false, "每次拦截都写日志（排查用）");
		host.StartCoroutine(HookArenaSpawner());
		host.StartCoroutine(PatchTeleportsFallback());
	}

	private static IEnumerator HookArenaSpawner()
	{
		for (float waited = 0f; waited < 30f; waited += 0.25f)
		{
			if (_patchedArenaSpawner)
			{
				break;
			}
			try
			{
				Type type = Type.GetType("Socknautica.Mono.ArenaSpawner, Socknautica", throwOnError: false);
				if (type != null)
				{
					MethodInfo method = type.GetMethod("SpawnArena", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
					if (method != null)
					{
						_harmony.Patch(method, null, new HarmonyMethod(typeof(ArenaTeleportBlockerFeature).GetMethod("ArenaSpawn_Postfix", BindingFlags.Static | BindingFlags.NonPublic)));
						_patchedArenaSpawner = true;
						Log.LogInfo("[ArenaTeleportBlocker] 已挂 Boss 战入口 ArenaSpawner.SpawnArena()");
					}
				}
			}
			catch (Exception ex)
			{
				if (Verbose != null && Verbose.Value)
				{
					Log.LogError("[ArenaTeleportBlocker] 挂 ArenaSpawner 异常: " + ex);
				}
			}
			if (_patchedArenaSpawner)
			{
				break;
			}
			yield return new WaitForSeconds(0.25f);
		}
		if (!_patchedArenaSpawner)
		{
			Log.LogWarning("[ArenaTeleportBlocker] 未找到 ArenaSpawner.SpawnArena()，仅依赖启动兜底挂载");
		}
	}

	private static IEnumerator PatchTeleportsFallback()
	{
		for (float waited = 0f; waited < 15f; waited += 0.5f)
		{
			if (Done.Count >= 4)
			{
				break;
			}
			TryPatchAll();
			if (Done.Count >= 4)
			{
				break;
			}
			yield return new WaitForSeconds(0.5f);
		}
		if (Done.Count >= 4)
		{
			Log.LogInfo("[ArenaTeleportBlocker] 四个传送目标已全部挂载");
		}
		else
		{
			Log.LogWarning("[ArenaTeleportBlocker] 启动兜底结束，仍有目标未拦截（" + Done.Count + "/4）");
		}
	}

	internal static void TryPatchAll()
	{
		TryPatchVanilla();
		TryPatch("FCS_HomeSolutions.Mods.QuantumTeleporter.Mono.TeleportManager, FCS_HomeSolutions", "TeleportPlayer", 3, returnsEnumerator: false, "FCS 传送门 / PDA / 载具传送");
		TryPatch("RotA.Mono.Equipment.WarpCannon, ReturnOfTheAncients", "WarpToLastVisitedBase", 0, returnsEnumerator: true, "RoTA WarpCannon 回基地");
		TryPatch("WarpChip.WarpChipFunction, WarpChip", "ReturnToBase", 0, returnsEnumerator: false, "UltimateWarpChip 回基地");
	}

	private static void ArenaSpawn_Postfix()
	{
		try
		{
			TryPatchAll();
		}
		catch
		{
		}
	}

	private static void TryPatchVanilla()
	{
		if (Done.Contains("PrecursorTeleporter.BeginTeleportPlayer"))
		{
			return;
		}
		try
		{
			Type type = Type.GetType("PrecursorTeleporter, Assembly-CSharp", throwOnError: false);
			if (!(type == null))
			{
				MethodInfo method = type.GetMethod("BeginTeleportPlayer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (!(method == null))
				{
					_harmony.Patch(method, new HarmonyMethod(typeof(ArenaTeleportBlockerFeature).GetMethod("BlockVanilla", BindingFlags.Static | BindingFlags.NonPublic)));
					Done.Add("PrecursorTeleporter.BeginTeleportPlayer");
					Log.LogInfo("[ArenaTeleportBlocker] 已拦截 原版先驱者传送门（PrecursorTeleporter.BeginTeleportPlayer）");
				}
			}
		}
		catch (Exception ex)
		{
			if (Verbose != null && Verbose.Value)
			{
				Log.LogError("[ArenaTeleportBlocker] 原版传送门拦截异常: " + ex);
			}
		}
	}

	private static void TryPatch(string typeName, string methodName, int paramCount, bool returnsEnumerator, string label)
	{
		string item = typeName + "::" + methodName;
		if (Done.Contains(item))
		{
			return;
		}
		try
		{
			Type type = Type.GetType(typeName, throwOnError: false);
			if (!(type == null))
			{
				MethodInfo methodInfo = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault((MethodInfo x) => x.Name == methodName && x.GetParameters().Length == paramCount);
				if (!(methodInfo == null))
				{
					string text = (returnsEnumerator ? "BlockEnumerator" : "BlockVoid");
					_harmony.Patch(methodInfo, new HarmonyMethod(typeof(ArenaTeleportBlockerFeature).GetMethod(text, BindingFlags.Static | BindingFlags.NonPublic)));
					Done.Add(item);
					Log.LogInfo("[ArenaTeleportBlocker] 已拦截 " + label + "（" + type.Name + "." + methodInfo.Name + "）");
				}
			}
		}
		catch (Exception ex)
		{
			if (Verbose != null && Verbose.Value)
			{
				Log.LogError("[ArenaTeleportBlocker] 拦截失败 " + typeName + "." + methodName + ": " + ex);
			}
		}
	}

	private static void GetArenaBounds(out Vector3 center, out float halfHeight)
	{
		center = new Vector3(0f, -2000f, 0f);
		halfHeight = 200f;
		try
		{
			Type type = Type.GetType("Socknautica.Mono.ArenaSpawner, Socknautica", throwOnError: false);
			if (type == null)
			{
				return;
			}
			if (_arenaMainField == null)
			{
				_arenaMainField = type.GetField("main", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			object obj = ((_arenaMainField == null) ? null : _arenaMainField.GetValue(null));
			if (obj == null)
			{
				return;
			}
			if (_arenaPosField == null)
			{
				_arenaPosField = type.GetField("arenaPos", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			}
			if (_arenaHeightField == null)
			{
				_arenaHeightField = type.GetField("arenaHeight", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			}
			if (_arenaPosField != null)
			{
				center = (Vector3)_arenaPosField.GetValue(obj);
			}
			if (_arenaHeightField != null)
			{
				float num = (float)_arenaHeightField.GetValue(obj);
				if (num > 0f)
				{
					halfHeight = num * 0.5f;
				}
			}
		}
		catch
		{
		}
	}

	internal static bool ShouldBlock(MethodBase original)
	{
		if (Enabled == null || !Enabled.Value)
		{
			return false;
		}
		Player main = Player.main;
		if (main == null)
		{
			return false;
		}
		GetArenaBounds(out var center, out var halfHeight);
		float num = ((Radius == null) ? 400f : Radius.Value);
		Vector3 a = new Vector3(main.transform.position.x, center.y, main.transform.position.z);
		bool flag = Vector3.Distance(a, center) <= num && main.transform.position.y <= center.y + halfHeight;
		if (flag && original != null)
		{
			string text = original.DeclaringType.Name + "." + original.Name;
			if (BlockLogged.Add(text))
			{
				Debug.Log(string.Concat("[ArenaTeleportBlocker] 已拦截 ", text, "（竞技场内，位置=", main.transform.position, "）"));
			}
		}
		return flag;
	}

	private static bool BlockVoid(MethodBase __originalMethod)
	{
		return !ShouldBlock(__originalMethod);
	}

	private static bool BlockVanilla(MethodBase __originalMethod, PrecursorTeleporter __instance)
	{
		try
		{
			if (__instance != null && __instance.teleporterIdentifier == "ArenaTeleporter")
			{
				bool flag = false;
				try
				{
					if (StoryGoalManager.main != null)
					{
						flag = StoryGoalManager.main.IsGoalComplete("BeginBossFight");
					}
				}
				catch
				{
				}
				bool flag2 = Mathf.Approximately(__instance.warpToPos.x, 647f);
				if (flag || flag2)
				{
					if (BlockLogged.Add("PrecursorTeleporter.ArenaTeleporter"))
					{
						Debug.Log("[ArenaTeleportBlocker] 已拦截 竞技场传送门（ArenaTeleporter，" + (flag ? "Boss战已开启" : "出口目标 x=647") + "）");
					}
					return false;
				}
			}
		}
		catch
		{
		}
		return !ShouldBlock(__originalMethod);
	}

	private static bool BlockEnumerator(MethodBase __originalMethod, ref IEnumerator __result)
	{
		if (!ShouldBlock(__originalMethod))
		{
			return true;
		}
		__result = EmptyEnumerator();
		return false;
	}

	private static IEnumerator EmptyEnumerator()
	{
		yield break;
	}
}



