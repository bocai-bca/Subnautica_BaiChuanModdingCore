using HarmonyLib;

namespace BaiChuanModdingCore
{
	[HarmonyPatch(typeof(uGUI_OptionsPanel), "OnEnable")]
	public class Patch_OptionsPanel_OnEnable
	{
		public static void Postfix(uGUI_OptionsPanel __instance)
		{
			__instance.transform.GetChild(2)?.GetChild(4)?.gameObject.SetActive(false);
			BaiChuanModdingCore.logger?.LogMessage("Hided Mods in options panel");
		}
	}
}