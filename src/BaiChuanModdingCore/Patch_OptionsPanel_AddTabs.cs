using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace BaiChuanModdingCore
{
	[HarmonyPatch(typeof(uGUI_OptionsPanel), "AddTabs")]
	public class Patch_OptionsPanel_AddTabs
	{
		/*
		public static void Postfix(uGUI_OptionsPanel __instance)
		{
			for (int i = 0; i < __instance.tabs.Count; i++)
			{
				uGUI_TabbedControlsPanel.Tab thisTab = __instance.tabs[i];
				if (thisTab.tab.GetComponentInChildren<UnityEngine.UI.Text>(true).text != "Mods") continue;
				Object.Destroy(thisTab.pane);
				Object.Destroy(thisTab.tab);
				__instance.tabs.RemoveAt(i);
				BaiChuanModdingCore.logger?.LogMessage("Successed to remove Mods tab from OptionsPanel.");
				return;
			}
			BaiChuanModdingCore.logger?.LogWarning("Failed to find Mods tab from OptionsPanel.");
		}*/

		public static void Postfix(uGUI_OptionsPanel __instance)
		{
			Transform tabsHolder = __instance.transform.Find("TabsHolder");
			for (int i = 0; i < tabsHolder.childCount; i++)
			{
				Transform tab = tabsHolder.GetChild(i);
				Text text = tab.GetComponentInChildren<Text>();
				if (text == null) continue;
				if (text.text != "Mods") continue;
				tab.gameObject.SetActive(false);
				BaiChuanModdingCore.logger?.LogMessage("Successed to hide Mods tab from OptionsPanel.");
			}
			BaiChuanModdingCore.logger?.LogWarning("Failed to find Mods tab from OptionsPanel.");
		}
	}
}