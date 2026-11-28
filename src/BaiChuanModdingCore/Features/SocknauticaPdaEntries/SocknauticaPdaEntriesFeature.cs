using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BaiChuanModdingCore.Features.SocknauticaPdaEntries
{
	/// <summary>
	/// 补全 Socknautica 已有但未注册的 PDA 文本：
	///   - RocketSelfDestruct1 / 2 → 播放时写入 PDA 日志
	///   - Ency_SocknauticaLore1 → 注册为资料下载/数据和线索条目
	///
	/// 注意：这里只注册 EntryData，不默认解锁。
	/// RocketSelfDestruct1/2 在语音播放时写入日志；
	/// RocketSelfDestruct1 同时解锁“一封来自 Alterra 的信”。
	/// </summary>
	internal static class SocknauticaPdaEntriesFeature
	{
		internal static ManualLogSource? Log;
		private static readonly FieldInfo? PdaLogMappingField =
			AccessTools.Field(typeof(PDALog), "mapping");

		internal static void Initialize(Harmony harmony, ManualLogSource log)
		{
			Log = log;
			RegisterCustomEntries();
			PatchVoiceLine(harmony);
		}

		private static void RegisterCustomEntries()
		{
			try
			{
				Type? logHandler = AccessTools.TypeByName(
					"SMLHelper.V2.Handlers.PDALogHandler");
				MethodInfo? addLog = logHandler?.GetMethod(
					"AddCustomEntry",
					BindingFlags.Static | BindingFlags.Public,
					null,
					new[] { typeof(string), typeof(string), typeof(Sprite), typeof(FMODAsset) },
					null);
				if (addLog != null)
				{
					addLog.Invoke(null, new object?[] { "RocketSelfDestruct1", "RocketSelfDestruct1", null, null });
					addLog.Invoke(null, new object?[] { "RocketSelfDestruct2", "RocketSelfDestruct2", null, null });
					Log?.LogInfo("[SocknauticaPda] custom log entries registered");
				}
				else
				{
					Log?.LogWarning("[SocknauticaPda] SMLHelper PDALogHandler not found");
				}

				Type? encHandler = AccessTools.TypeByName(
					"SMLHelper.V2.Handlers.PDAEncyclopediaHandler");
				MethodInfo? addEnc = encHandler?.GetMethod(
					"AddCustomEntry",
					BindingFlags.Static | BindingFlags.Public,
					null,
					new[] { typeof(PDAEncyclopedia.EntryData) },
					null);
				if (addEnc != null)
				{
					var entry = new PDAEncyclopedia.EntryData
					{
						key = "SocknauticaLore1",
						path = "DownloadedData/Codes",
						nodes = new[] { "DownloadedData", "Codes" },
						// 不默认解锁，交给对应 DataTerminal / StoryGoal。
						unlocked = false
					};
					addEnc.Invoke(null, new object[] { entry });
					Log?.LogInfo("[SocknauticaPda] custom encyclopedia entry registered (locked)");
				}
				else
				{
					Log?.LogWarning("[SocknauticaPda] SMLHelper PDAEncyclopediaHandler not found");
				}
			}
			catch (Exception ex)
			{
				Log?.LogWarning("[SocknauticaPda] register custom entries failed: " + ex.Message);
			}
		}

		private static void PatchVoiceLine(Harmony harmony)
		{
			try
			{
				Type? type = AccessTools.TypeByName("Socknautica.CustomPDALinesManager");
				MethodInfo? method = type == null
					? null
					: AccessTools.Method(type, "PlayVoiceLine");
				if (method == null)
				{
					Log?.LogWarning("[SocknauticaPda] Socknautica.CustomPDALinesManager.PlayVoiceLine not found");
					return;
				}

				harmony.Patch(method, postfix: new HarmonyMethod(
					typeof(SocknauticaPdaEntriesFeature).GetMethod(
						nameof(VoicePostfix),
						BindingFlags.Static | BindingFlags.NonPublic)));
				Log?.LogInfo("[SocknauticaPda] voice log hook installed");
			}
			catch (Exception ex)
			{
				Log?.LogWarning("[SocknauticaPda] voice hook failed: " + ex.Message);
			}
		}

		private static void VoicePostfix(string id)
		{
			if (id == "RocketSelfDestruct1" || id == "RocketSelfDestruct2")
			{
				AddLog(id);
				AddEncyclopedia();
			}
		}

		private static bool AddEncyclopedia()
		{
			const string key = "SocknauticaLore1";
			try
			{
				if (PDAEncyclopedia.ContainsEntry(key))
				{
					return false;
				}

				PDAEncyclopedia.EntryData? added = PDAEncyclopedia.Add(key, false);
				if (added != null || PDAEncyclopedia.ContainsEntry(key))
				{
					ErrorMessage.AddMessage("你收到了一份阿尔特拉的信件");
					Log?.LogInfo("[SocknauticaPda] encyclopedia unlocked: " + key);
					return true;
				}

				Log?.LogWarning("[SocknauticaPda] encyclopedia Add returned null: " + key);
			}
			catch (Exception ex)
			{
				Log?.LogWarning("[SocknauticaPda] AddEncyclopedia failed: " + ex.Message);
			}
			return false;
		}

		private static void AddLog(string key)
		{
			try
			{
				if (PDALog.Contains(key))
				{
					return;
				}

				if (PdaLogMappingField?.GetValue(null) is not Dictionary<string, PDALog.EntryData> mapping)
				{
					Log?.LogWarning("[SocknauticaPda] PDALog.mapping unavailable");
					return;
				}

				if (!mapping.ContainsKey(key))
				{
					mapping[key] = new PDALog.EntryData
					{
						key = key,
						type = PDALog.EntryType.Default,
						icon = null,
						sound = null
					};
				}

				PDALog.EntryData? added = PDALog.Add(key, false);
				if (added != null || PDALog.Contains(key))
				{
					Log?.LogInfo("[SocknauticaPda] log entry unlocked: " + key);
					return;
				}

				Log?.LogWarning("[SocknauticaPda] log Add returned null: " + key);
			}
			catch (Exception ex)
			{
				Log?.LogWarning("[SocknauticaPda] AddLog(" + key + ") failed: " + ex.Message);
			}
		}
	}
}
