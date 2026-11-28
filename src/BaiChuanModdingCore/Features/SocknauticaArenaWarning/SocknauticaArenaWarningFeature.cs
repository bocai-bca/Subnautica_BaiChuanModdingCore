using System;
using System.Reflection;
using BaiChuanModdingCore.Features.SocknauticaShared;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BaiChuanModdingCore.Features.SocknauticaArenaWarning
{
	/// <summary>
	/// 玩家通过传送门进入 Socknautica 竞技场、ArenaSpawner 开始生成时，
	/// 弹出“传送,复活与存档已被领域禁用”警告。
	/// </summary>
	internal static class SocknauticaArenaWarningFeature
	{
		private const string WarningText = "传送,复活与存档已被领域禁用";

		internal static ManualLogSource? Log;
		private static MethodInfo? _showMethod;

		internal static void Initialize(Harmony harmony, ManualLogSource log)
		{
			Log = log;

			try
			{
				Type? arenaType = AccessTools.TypeByName("Socknautica.Mono.ArenaSpawner");
				MethodInfo? spawnArena = arenaType == null
					? null
					: AccessTools.Method(arenaType, "SpawnArena");
				if (spawnArena == null)
				{
					Log?.LogWarning("[SocknauticaArenaWarning] ArenaSpawner.SpawnArena not found");
					return;
				}

				Type? warningType = AccessTools.TypeByName("Socknautica.Mono.WarningUI");
				_showMethod = warningType?.GetMethod(
					"Show",
					BindingFlags.Static | BindingFlags.Public,
					null,
					new[] { typeof(string), typeof(Sprite), typeof(float) },
					null);
				if (_showMethod == null)
				{
					Log?.LogWarning("[SocknauticaArenaWarning] WarningUI.Show not found");
				}

				harmony.Patch(spawnArena, postfix: new HarmonyMethod(
					typeof(SocknauticaArenaWarningFeature).GetMethod(
						nameof(SpawnArenaPostfix),
						BindingFlags.Static | BindingFlags.NonPublic)));
				Log?.LogInfo("[SocknauticaArenaWarning] arena warning hook installed");
			}
			catch (Exception ex)
			{
				Log?.LogWarning("[SocknauticaArenaWarning] init failed: " + ex.Message);
			}
		}

		private static void SpawnArenaPostfix()
		{
			// SpawnArena 每次进入竞技场只调用一次；不再做进程级一次性限制，
			// 否则回到主菜单开新游戏后第二次 Boss 战不会显示警告。
			ShowWarning();
		}

		private static void ShowWarning()
		{
			try
			{
				if (_showMethod == null)
				{
					Log?.LogWarning("[SocknauticaArenaWarning] WarningUI.Show not found");
					return;
				}

				Sprite? sprite = SocknauticaSharedAssets.GetGenericWarningSprite(
					Log, "SocknauticaArenaWarning");
				object? result = _showMethod.Invoke(
					null, new object?[] { WarningText, sprite, 10f });
				if (result != null)
				{
					Log?.LogInfo("[SocknauticaArenaWarning] warning shown");
				}
				else
				{
					Log?.LogWarning("[SocknauticaArenaWarning] WarningUI.Show returned null");
				}
			}
			catch (Exception ex)
			{
				Log?.LogWarning("[SocknauticaArenaWarning] show failed: " + ex.Message);
				ErrorMessage.AddMessage(WarningText);
			}
		}
	}
}

