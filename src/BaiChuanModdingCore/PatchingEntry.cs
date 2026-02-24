using System.Collections.Generic;
using BaiChuanModdingCore.Patchers;
using UnityEngine;

namespace BaiChuanModdingCore
{
	public class PatchingEntry
	{
		/// <summary>
		/// 记录所有待修补的补丁类型实例，列表将在修补流程结束后清除
		/// </summary>
		private static List<PatcherBase> taskPatchers =  new List<PatcherBase>()
		{
			new PrecursorIonBattery(),
			new Patchers.ReaperLeviathan(),
			new Patchers.GhostLeviathan(),
			new Patchers.GhostLeviathanJuvenile(),
			new Patchers.SeaDragon(),
			new Patchers.SeaEmperorJuvenile(),
			new Patchers.Reefback(),
		};

		/// <summary>
		/// 执行所有修补行为的入口方法，应当在游戏的MainMenuMusic启动播放后调用，以确保执行时已完成所有游戏资源的载入
		/// </summary>
		// ReSharper disable once UnusedMember.Global
		public static void StartPatching()
		{
			if (patched)
			{
				return;
			}
			patched = true;
			BaiChuanModdingCore.logger.LogMessage("Starting patching process.");
			foreach (PatcherBase taskPatcher in taskPatchers)
			{
				bool wasFailed = false;
				List<GameObject> prefabGameObjects = new List<GameObject>();
				foreach (TechType targetTechType in taskPatcher.GetTargetTechTypes())
				{
					GameObject thisPrefab = CraftData.GetPrefabForTechType(targetTechType, false);
					if (thisPrefab == null)
					{
						BaiChuanModdingCore.logger.LogError("Could not found prefab of the target TechType " + targetTechType);
						wasFailed = true;
					}
					prefabGameObjects.Add(thisPrefab);
				}
				if (wasFailed)
				{
					continue;
				}
				string patcherName = taskPatcher.GetType().Name;
				BaiChuanModdingCore.logger.LogMessage("Starting patcher " + patcherName);
				if (taskPatcher.DoPatching(prefabGameObjects))
				{
					BaiChuanModdingCore.logger.LogMessage(patcherName + " run successfully.");
				}
				else
				{
					BaiChuanModdingCore.logger.LogError(patcherName + " run failed.");
				}
			}
		}
		public static bool patched;
	}
}