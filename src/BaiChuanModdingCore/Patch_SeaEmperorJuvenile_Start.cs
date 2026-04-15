using HarmonyLib;
using UnityEngine;

namespace BaiChuanModdingCore
{
	[HarmonyPatch(typeof(SeaEmperorJuvenile), "Start")]
	public class Patch_SeaEmperorJuvenile_Start
	{
		public static void Postfix(SeaEmperorJuvenile __instance)
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
	}
}