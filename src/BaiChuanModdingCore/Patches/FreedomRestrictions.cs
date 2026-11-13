using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace BaiChuanModdingCore
{
	/// <summary>
	/// 合并自 FreedomCommandBlocker：
	/// 禁用控制台 freedom 命令。
	/// </summary>
	[HarmonyPatch(typeof(GameModeConsoleCommands), "OnConsoleCommand_freedom")]
	internal static class FreedomCommandBlockerPatch
	{
		[HarmonyPrefix]
		private static bool Prefix()
		{
			ErrorMessage.AddMessage("无效的指令");
			return false;
		}
	}

	/// <summary>
	/// 合并自 FreedomModeDisable：
	/// 在主菜单禁用 Freedom 模式入口，并改为红色不可用状态。
	/// </summary>
	[HarmonyPatch(typeof(MainMenuRightSide), "Start")]
	internal static class FreedomModeDisablePatch
	{
		[HarmonyPostfix]
		private static void Postfix(MainMenuRightSide __instance)
		{
			Transform freedom = __instance.transform.Find("NewGame/NewGameOptions/2. Freedom");
			if (freedom == null)
				return;

			GameObject go = freedom.gameObject;

			Button button = go.GetComponent<Button>();
			if (button != null)
				UnityEngine.Object.Destroy(button);

			CanvasRenderer canvasRenderer = go.GetComponent<CanvasRenderer>();
			if (canvasRenderer != null)
				canvasRenderer.SetColor(new Color(1f, 0f, 0f, 1f));

			Transform title = go.transform.Find("ModeTitle");
			if (title != null)
			{
				Text text = title.GetComponent<Text>();
				if (text != null)
					text.text = "无效的游戏模式";
			}

			Transform description = go.transform.Find("ModeDescription");
			if (description != null)
			{
				Text text = description.GetComponent<Text>();
				if (text != null)
					text.text = "因部分游戏问题被百川归海夷灭并消逝";
			}
		}
	}
}
