using HarmonyLib;
using UnityEngine;

namespace BaiChuanModdingCore
{
	[HarmonyPatch(typeof(ReaperLeviathan), "Awake")]
	public class Patch_ReaperLeviathan_Awake
	{
		public static void Postfix(ReaperLeviathan __instance)
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