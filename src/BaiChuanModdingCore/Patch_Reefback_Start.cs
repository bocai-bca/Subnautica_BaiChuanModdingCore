using HarmonyLib;
using UnityEngine;

namespace BaiChuanModdingCore
{
	[HarmonyPatch(typeof(Reefback), "Start")]
	public class Patch_Reefback_Start
	{
		public static void Postfix(Reefback __instance)
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
	}
}