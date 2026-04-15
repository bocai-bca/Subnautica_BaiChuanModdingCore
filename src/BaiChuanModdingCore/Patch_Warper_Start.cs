using HarmonyLib;
using UnityEngine;

namespace BaiChuanModdingCore
{
	[HarmonyPatch(typeof(Warper), "Start")]
	public class Patch_Warper_Start
	{
		public static void Postfix(Warper __instance)
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