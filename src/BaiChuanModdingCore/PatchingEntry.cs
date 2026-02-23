using System.Collections.Generic;
using BaiChuanCustomCore.Patchers;
using UnityEngine;

namespace BaiChuanCustomCore
{
	public class PatchingEntry
	{
		/// <summary>
		/// 记录所有待修补的补丁类型实例，列表将在修补流程结束后清除
		/// </summary>
		private static List<PatcherBase> taskPatchers =  new List<PatcherBase>()
		{
			new PrecursorIonBattery(),
		};
		
		/// <summary>
		/// 执行所有修补行为的入口方法，应当在游戏的MainMenuMusic启动播放后调用，以确保执行时已完成所有游戏资源的载入
		/// </summary>
		// ReSharper disable once UnusedMember.Global
		internal static void StartPatching()
		{
			BaiChuanModdingCore.logger.LogInfo("BaiChuanModdingCore: Starting patching process.");
			foreach (PatcherBase taskPatcher in taskPatchers)
			{
				bool wasFailed = false;
				List<GameObject> prefabGameObjects = new List<GameObject>();
				foreach (TechType targetTechType in taskPatcher.GetTargetTechTypes())
				{
					GameObject thisPrefab = CraftData.GetPrefabForTechType(targetTechType);
					if (thisPrefab == null)
					{
						BaiChuanModdingCore.logger.LogError("BaiChuanModdingCore: Could not found prefab of the target TechType " + targetTechType);
						wasFailed = true;
					}
					prefabGameObjects.Add(thisPrefab);
				}
				if (wasFailed)
				{
					continue;
				}
				BaiChuanModdingCore.logger.LogInfo("BaiChuanModdingCore: Starting patcher " + taskPatcher.GetType().Name);
				taskPatcher.DoPatching(prefabGameObjects);
			}
			taskPatchers = null;
		}
	}
}