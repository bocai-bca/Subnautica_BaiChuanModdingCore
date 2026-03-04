using System.Collections.Generic;
using HarmonyLib;

namespace BaiChuanModdingCore
{
	[HarmonyPatch(typeof(Language), nameof(Language.TryGet))]
	public class Patch_Language_TryGet
	{
		public static Dictionary<string, string> OverrideTranslations = new()
		{
			{"Tooltip_StasisRifle", "通过改变磁场使物体在一段时间内处于停滞状态\n<color=#FF3030FF>射击遗迹和建筑会解除其静止状态</color>"},
			{"Tooltip_Battery", "便携能源\n<color=#FFA500>制作需要电池或含有电池的工具需要保持满电</color>"},
			{"Tooltip_SeamothTorpedoModule", "一个标准的水下发射装备，可以用于发射鱼雷\n<color=#FFA500>该模块只能装备在一到四号槽位</color>"},
			{"Tooltip_VehicleStorageModule", "额外增加的4x4储存空间，适用于海蛾号和海虾号动力机甲\n<color=#FFA500>该模块只能装备在一到四号槽位</color>"},
			{"SmallStorage", "白色潜影盒"},
			{"Tooltip_SmallStorage", "拥有8x10的储物空间,似乎不是此方世界的产物"},
			{"LuggageBag", "灰色潜影盒"},
			{"Tooltip_LuggageBag", "拥有8x10的储物空间,似乎不是此方世界的产物"}
		};

		public static void Prefix(string key, out string __state)
		{
			__state = key;
		}

		public static void Postfix(ref string result, string __state)
		{
			if (OverrideTranslations.TryGetValue(__state, out string? translationOverrided))
			{
				result = translationOverrided;
			}
		}
	}
}