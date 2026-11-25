using BepInEx.Logging;
using HarmonyLib;

namespace BaiChuanModdingCore.Features.ResourceTrackerGuard
{
	/// <summary>
	/// 独立功能：跳过没有 PrefabIdentifier 的 ResourceTracker.Start。
	/// Exscansion 的 ScannerFilteringHook2.Transpiler 会在 Start 中注入代码，
	/// 这类无效对象会触发 NullReferenceException。
	/// </summary>
	[HarmonyPatch(typeof(ResourceTracker), "Start")]
	internal static class ResourceTrackerGuardPatch
	{
		[HarmonyPriority(Priority.First)]
		[HarmonyPrefix]
		private static bool Prefix(ResourceTracker __instance)
		{
			if (__instance == null)
			{
				return false;
			}

			return __instance.prefabIdentifier != null;
		}

		internal static void Initialize(ManualLogSource log)
		{
			log?.LogInfo("[ResourceTrackerGuard] loaded: null prefabIdentifier will be skipped.");
		}
	}
}
