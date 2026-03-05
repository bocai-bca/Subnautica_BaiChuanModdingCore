using System.Collections.Generic;
using HarmonyLib;

namespace BaiChuanModdingCore
{
	[HarmonyPatch(typeof(Language), nameof(Language.TryGet))]
	public class Patch_Language_TryGet
	{
		public static Dictionary<string, string> OverrideTranslations = new()
		{
			{"Tooltip_StasisRifle", "通过改变磁场使物体在一段时间内处于停滞状态\n<color=#FF3030FF>射击遗迹和残骸会解除其静止状态</color>"},
			{"Tooltip_Battery", "便携能源\n<color=#FFA500>制作需要电池或含有电池的工具需要保持满电</color>"},
			{"Tooltip_SeamothTorpedoModule", "一个标准的水下发射装备，可以用于发射鱼雷\n<color=#FFA500>该模块只能装备在一到四号槽位</color>"},
			{"Tooltip_VehicleStorageModule", "额外增加的4x4储存空间，适用于海蛾号和海虾号动力机甲\n<color=#FFA500>该模块只能装备在一到四号槽位</color>"},
			{"SmallStorage", "白色潜影盒"},
			{"Tooltip_SmallStorage", "拥有8x10的储物空间,似乎不是此方世界的产物\n<color=#FF3030FF>建议使用鼠标中键打开，F键可能导致潜影盒卷入次元夹缝</color>\n<color=#FF3030FF>重载时会导致室外的潜影盒空间缩水</color>"},
			{"LuggageBag", "灰色潜影盒"},
			{"Tooltip_LuggageBag", "拥有8x10的储物空间,似乎不是此方世界的产物\n<color=#FF3030FF>建议使用鼠标中键打开，F键可能导致潜影盒卷入次元夹缝</color>\n<color=#FF3030FF>重载时会导致室外盒子空间缩水</color>"},
			{"Tooltip_Fabricator", "基础的工作台，将收集到的最基础的材料转化为有用的物品\n<color=#90EE90>批量制造绑定按键：鼠标滚轮</color>\n<color=#90EE90>拆解模式绑定按键：空格</color>"},
			{"Tooltip_Seaglide", "通过螺旋桨转动来产生水下推动力\n<color=#90EE90>开关灯光绑定按键：双击右键</color>\n<color=#90EE90>超频冲刺绑定按键：Shift</color>"},
			{"Tooltip_SmallLocker", "壁挂式储物柜，提供8x10的储物空间\n<color=#FF3030FF>重载时会导致空间缩水，建议使用其他柜子</color>"},
			{"Tooltip_Locker", "独立的8x10储藏室\n<color=#FF3030FF>重载时会导致空间缩水，建议使用其他柜子</color>"}
		};

		public static void Prefix(string key, out string? __state)
		{
			__state = key;
		}

		public static void Postfix(ref string result, string? __state)
		{
			if (__state == null) return;
			if (OverrideTranslations.TryGetValue(__state, out string? translationOverrided))
			{
				result = translationOverrided;
			}
		}
	}
}