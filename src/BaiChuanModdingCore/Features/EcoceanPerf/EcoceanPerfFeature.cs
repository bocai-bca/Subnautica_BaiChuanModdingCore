using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BaiChuanModdingCore.Features.EcoceanPerf
{
	/// <summary>
	/// 独立功能：Ecocean 性能/稳定性补丁。
	/// 包含：
	///   - VoidTongueTag.getPriorityTarget 0.5 秒缓存
	///   - SubRoot.StartSubShielded / EndSubShielded 的 live 空引用保护
	/// </summary>
	internal static class EcoceanPerfFeature
	{
		internal static ManualLogSource? Log;

		internal static void Initialize(Harmony harmony, ManualLogSource log)
		{
			Log = log;
			Patches.Install(harmony);
		}
	}

	internal static class Patches
	{
		private sealed class Cache
		{
			internal Transform? Target;
			internal float Next;
		}

		private static readonly ConditionalWeakTable<object, Cache> Caches =
			new ConditionalWeakTable<object, Cache>();
		private static readonly FieldInfo? SubRootLive =
			AccessTools.Field(typeof(SubRoot), "live");

		internal static void Install(Harmony harmony)
		{
			Type? type = AccessTools.TypeByName("ReikaKalseki.Ecocean.VoidTongueTag");
			MethodInfo? target = type == null
				? null
				: AccessTools.Method(type, "getPriorityTarget");

			if (target == null)
			{
				EcoceanPerfFeature.Log?.LogError(
					"[EcoceanPerf] VoidTongueTag.getPriorityTarget not found; target cache skipped");
			}
			else
			{
				harmony.Patch(
					target,
					prefix: new HarmonyMethod(typeof(Patches).GetMethod(
						nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic)),
					postfix: new HarmonyMethod(typeof(Patches).GetMethod(
						nameof(Postfix), BindingFlags.Static | BindingFlags.NonPublic)));
			}

			PatchShield(harmony, "StartSubShielded");
			PatchShield(harmony, "EndSubShielded");
			EcoceanPerfFeature.Log?.LogInfo("[EcoceanPerf] installed");
		}

		private static void PatchShield(Harmony harmony, string methodName)
		{
			MethodInfo? method = AccessTools.Method(typeof(SubRoot), methodName);
			if (method == null)
			{
				EcoceanPerfFeature.Log?.LogWarning(
					"[EcoceanPerf] SubRoot." + methodName + " not found");
				return;
			}

			harmony.Patch(method, prefix: new HarmonyMethod(
				typeof(Patches).GetMethod(nameof(ShieldPrefix),
					BindingFlags.Static | BindingFlags.NonPublic)));
		}

		private static bool ShieldPrefix(object __instance)
		{
			// Ecocean 可能在无 LiveMixin 的 SubRoot 上调用护盾启停。
			// 原版直接访问 live，可能空引用。
			return __instance != null &&
				SubRootLive != null &&
				SubRootLive.GetValue(__instance) != null;
		}

		private static bool Prefix(object __instance, ref Transform __result,
			out bool __state)
		{
			__state = false;
			if (__instance == null)
			{
				return true;
			}

			if (Caches.TryGetValue(__instance, out Cache? cache) &&
				cache.Target != null &&
				Time.time < cache.Next)
			{
				__result = cache.Target;
				__state = true;
				return false;
			}

			return true;
		}

		private static void Postfix(object __instance, Transform __result,
			bool __state)
		{
			if (__state || __instance == null)
			{
				return;
			}

			Cache cache = Caches.GetValue(__instance, _ => new Cache());
			cache.Target = __result;
			cache.Next = Time.time + 0.5f;
		}
	}
}
