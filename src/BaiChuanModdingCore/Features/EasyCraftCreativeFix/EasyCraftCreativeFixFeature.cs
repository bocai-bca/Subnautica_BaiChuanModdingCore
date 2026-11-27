using System;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace BaiChuanModdingCore.Features.EasyCraftCreativeFix
{
	/// <summary>
	/// 独立功能：EasyCraft 在创造模式下不使用材料，
	/// 但队列预检查仍可能按生存模式返回“材料不足”。
	/// 本功能只在 GameModeUtils.RequiresIngredients() 为 false 时
	/// 强制 _IsCraftRecipeFulfilledAdvanced 返回 true。
	/// </summary>
	internal static class EasyCraftCreativeFixFeature
	{
		internal static ManualLogSource? Log;
		internal static bool Logged;

		internal static void Initialize(Harmony harmony, ManualLogSource log)
		{
			Log = log;
			Type? type = AccessTools.TypeByName("EasyCraft.Main");
			MethodInfo? target = type == null
				? null
				: AccessTools.Method(type, "_IsCraftRecipeFulfilledAdvanced");
			if (target == null)
			{
				Log.LogWarning("[EasyCraftCreativeFix] EasyCraft.Main not found; skipped");
				return;
			}

			harmony.Patch(
				target,
				prefix: new HarmonyMethod(typeof(EasyCraftCreativePatch).GetMethod(
					nameof(EasyCraftCreativePatch.Prefix),
					BindingFlags.Static | BindingFlags.NonPublic)));
			Log.LogInfo("[EasyCraftCreativeFix] ready");
		}
	}
}
