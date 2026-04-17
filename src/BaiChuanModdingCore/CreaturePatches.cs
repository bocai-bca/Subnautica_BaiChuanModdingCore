using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace BaiChuanModdingCore
{
	/// <summary>
	/// 生物补丁，此类容纳一堆方法用于修补原版生物这种会在世界生成时预装非prefab提供的数据的傻逼玩意
	/// </summary>
	public class CreaturePatches
	{
		/// <summary>
		/// 修补任务列表，键为对应生物类型，值为对应生物的Start方法的Postfix方法
		/// </summary>
		public static Dictionary<Type, string[]> StartTasks = new()
		{
			{ typeof(Creature), [string.Empty, nameof(Creature_Start_Postfix)] },
		};
		
		/// <summary>
		/// 打入补丁，本方法自身不带异常捕捉
		/// </summary>
		/// <param name="harmony">修补用的harmony实例</param>
		public static void DoPatch(Harmony harmony)
		{
			BaiChuanModdingCore.logger?.LogMessage("Starting CreaturePatches patching.");
			foreach (KeyValuePair<Type, string[]> startPostfixTask in StartTasks)
			{
				MethodBase? originalMethod = startPostfixTask.Key.GetMethod("Start", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic); 
				HarmonyMethod? prefixMethod = startPostfixTask.Value[0] == string.Empty ? null : new HarmonyMethod(typeof(CreaturePatches).GetMethod(startPostfixTask.Value[0], BindingFlags.Static | BindingFlags.Public));
				HarmonyMethod? postfixMethod = startPostfixTask.Value[1] == string.Empty ? null : new HarmonyMethod(typeof(CreaturePatches).GetMethod(startPostfixTask.Value[1], BindingFlags.Static | BindingFlags.Public));
				if (originalMethod == null)
				{
					BaiChuanModdingCore.logger?.LogError("Failed to patch the type " + startPostfixTask.Key.Name + " of CreaturePatches, because it was failed to get original method.");
					continue;
				}
				harmony.Patch(
					original: originalMethod,
					prefix: prefixMethod,
					postfix: postfixMethod
				);
				BaiChuanModdingCore.logger?.LogMessage("The type " + startPostfixTask.Key.Name + " of CreaturePatches was patched.");
			}
			BaiChuanModdingCore.logger?.LogMessage("CreaturePatches patching end.");
		}

		public static void Creature_Start_Postfix(Creature __instance)
		{
			switch (__instance)
			{
				case GhostLeviathan ghostLeviathan:
					GhostLeviathan_Start_Postfix(ghostLeviathan);
					break;
				case ReaperLeviathan reaperLeviathan:
					ReaperLeviathan_Start_Postfix(reaperLeviathan);
					break;
				case SeaDragon seaDragon:
					SeaDragon_Start_Postfix(seaDragon);
					break;
				case Warper warper:
					Warper_Start_Postfix(warper);
					break;
				case Reefback reefback:
					Reefback_Start_Postfix(reefback);
					break;
				case SeaEmperorJuvenile seaEmperorJuvenile:
					SeaEmperorJuvenile_Start_Postfix(seaEmperorJuvenile);
					break;
			}
		}
		
		public static void GhostLeviathan_Start_Postfix(GhostLeviathan __instance)
		{
			LiveMixin liveMixin = __instance.GetComponent<LiveMixin>();
			if (liveMixin == null)
			{
				return;
			}
			// 检查如果最大血量为360000或315000则跳过修补
			if (Mathf.Approximately(liveMixin.data.maxHealth, 360000f) || Mathf.Approximately(liveMixin.data.maxHealth, 315000f))
			{
				return;
			}
			// 将血量改为360000
			liveMixin.data.maxHealth = liveMixin.health = 360000f;
			// 目前不知道怎么甄别未修补的成年幽灵和青年幽灵，所以索性将所有未修补的幽灵都改为成年血量了
		}
		
		public static void Reefback_Start_Postfix(Reefback __instance)
		{
			LiveMixin liveMixin = __instance.GetComponent<LiveMixin>();
			if (liveMixin == null)
			{
				return;
			}
			if (Mathf.Approximately(liveMixin.data.maxHealth, 450000f))
			{
				return;
			}
			liveMixin.data.maxHealth = liveMixin.health = 450000f;
		}
		
		public static void ReaperLeviathan_Start_Postfix(ReaperLeviathan __instance)
		{
			LiveMixin liveMixin = __instance.GetComponent<LiveMixin>();
			if (liveMixin == null)
			{
				return;
			}
			if (Mathf.Approximately(liveMixin.data.maxHealth, 225000f))
			{
				return;
			}
			liveMixin.data.maxHealth = liveMixin.health = 225000f;
		}
		
		public static void SeaDragon_Start_Postfix(SeaDragon __instance)
		{
			LiveMixin liveMixin = __instance.GetComponent<LiveMixin>();
			if (liveMixin == null)
			{
				return;
			}
			if (Mathf.Approximately(liveMixin.data.maxHealth, 360000f))
			{
				return;
			}
			liveMixin.data.maxHealth = liveMixin.health = 360000f;
		}
		
		public static void SeaEmperorJuvenile_Start_Postfix(SeaEmperorJuvenile __instance)
		{
			LiveMixin liveMixin = __instance.GetComponent<LiveMixin>();
			if (liveMixin == null)
			{
				return;
			}
			if (Mathf.Approximately(liveMixin.data.maxHealth, 225000f))
			{
				return;
			}
			liveMixin.data.maxHealth = liveMixin.health = 225000f;
		}
		
		public static void Warper_Start_Postfix(Warper __instance)
		{
			LiveMixin liveMixin = __instance.GetComponent<LiveMixin>();
			if (liveMixin == null)
			{
				return;
			}
			if (Mathf.Approximately(liveMixin.data.maxHealth, 8100f))
			{
				return;
			}
			liveMixin.data.maxHealth = liveMixin.health = 8100f;
		}
	}
}