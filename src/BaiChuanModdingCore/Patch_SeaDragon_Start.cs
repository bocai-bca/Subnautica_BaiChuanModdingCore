using HarmonyLib;
using UnityEngine;

namespace BaiChuanModdingCore
{
	[HarmonyPatch(typeof(SeaDragon), "Start")]
	public class Patch_SeaDragon_Start
	{
		public static void Postfix(SeaDragon __instance)
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
	}
}