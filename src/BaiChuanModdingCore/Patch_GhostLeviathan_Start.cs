using HarmonyLib;
using UnityEngine;

namespace BaiChuanModdingCore
{
	[HarmonyPatch(typeof(GhostLeviathan), "Start")]
	public class Patch_GhostLeviathan_Start
	{
		public static void Postfix(GhostLeviathan __instance)
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
	}
}