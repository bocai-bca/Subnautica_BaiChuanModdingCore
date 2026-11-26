using System;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace BaiChuanModdingCore.Features.AchievementFontFix
{
	/// <summary>
	/// 独立功能：ArchitectsLibrary 的成就面板打开后，把面板里的 UI.Text
	/// 换成能显示中文的字体。
	///
	/// ArchitectsLibrary 是 QMod，类型不能编译期直接引用，因此运行时反射查找。
	/// </summary>
	internal static class AchievementFontFixFeature
	{
		private static ManualLogSource? _log;
		private static FieldInfo? _currentField;
		private static Font? _cachedFont;
		private static int _fixCount;
		private static bool _installed;

		internal static void Initialize(Harmony harmony, ManualLogSource log)
		{
			_log = log;
			if (_installed)
			{
				return;
			}

			try
			{
				Type? menuType = FindType("ArchitectsLibrary.Handlers.AchievementsMenu");
				if (menuType == null)
				{
					_log?.LogWarning("[AchievementFontFix] 未找到 AchievementsMenu，跳过");
					return;
				}

				MethodInfo? show = menuType.GetMethod(
					"Show",
					BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
					null,
					Type.EmptyTypes,
					null);
				if (show == null)
				{
					_log?.LogWarning("[AchievementFontFix] 未找到 AchievementsMenu.Show");
					return;
				}

				_currentField = menuType.GetField(
					"_current",
					BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				if (_currentField == null)
				{
					_log?.LogWarning("[AchievementFontFix] 未找到 AchievementsMenu._current");
					return;
				}

				var postfix = new HarmonyMethod(
					typeof(AchievementFontFixFeature).GetMethod(
						nameof(ShowPostfix),
						BindingFlags.Static | BindingFlags.NonPublic));
				harmony.Patch(show, null, postfix);
				_installed = true;
				_log?.LogInfo("[AchievementFontFix] 已挂 AchievementsMenu.Show → 中文字体修复");
			}
			catch (Exception ex)
			{
				_log?.LogError("[AchievementFontFix] Harmony 失败: " + ex.Message);
			}
		}

		private static void ShowPostfix()
		{
			if (_currentField == null)
			{
				return;
			}

			GameObject? current = _currentField.GetValue(null) as GameObject;
			if (current == null)
			{
				_log?.LogWarning("[AchievementFontFix] Show 之后 _current 为空");
				return;
			}

			Text[] texts = current.GetComponentsInChildren<Text>(true);
			if (texts.Length == 0)
			{
				_log?.LogWarning("[AchievementFontFix] 面板里没有 UI.Text");
				return;
			}

			Font? font = PickFont();
			if (font == null)
			{
				_log?.LogWarning("[AchievementFontFix] 找不到可用字体");
				return;
			}

			int replaced = 0;
			for (int i = 0; i < texts.Length; i++)
			{
				Text text = texts[i];
				if (text == null || text.font == font)
				{
					continue;
				}

				text.font = font;
				string oldText = text.text;
				text.text = string.Empty;
				text.text = oldText;
				replaced++;
			}

			if (_fixCount >= 6)
			{
				return;
			}

			_fixCount++;
			string sample = texts.Length > 0 && texts[0] != null ? texts[0].text : string.Empty;
			_log?.LogInfo(
				"[AchievementFontFix] 成就面板字体已处理：Text 共 " + texts.Length +
				" 个，替换字体 " + replaced +
				" 个，字体=" + font.name +
				" 样例文本=\"" + sample + "\"");
		}

		private static Font? PickFont()
		{
			if (_cachedFont != null)
			{
				return _cachedFont;
			}

			try
			{
				if (uGUI_MainMenu.main != null)
				{
					Text[] texts = uGUI_MainMenu.main.GetComponentsInChildren<Text>(true);
					for (int i = 0; i < texts.Length; i++)
					{
						Text text = texts[i];
						if (text == null || text.font == null)
						{
							continue;
						}
						if (text.font.HasCharacter('中'))
						{
							_cachedFont = text.font;
							return _cachedFont;
						}
					}

					for (int i = 0; i < texts.Length; i++)
					{
						Text text = texts[i];
						if (text == null || text.font == null)
						{
							continue;
						}
						_cachedFont = text.font;
						return _cachedFont;
					}
				}
			}
			catch (Exception ex)
			{
				_log?.LogWarning("[AchievementFontFix] 取主菜单字体失败: " + ex.Message);
			}

			try
			{
				_cachedFont = Font.CreateDynamicFontFromOSFont(
					new[] { "Microsoft YaHei", "微软雅黑", "SimHei", "SimSun", "Arial Unicode MS" },
					20);
			}
			catch (Exception ex)
			{
				_log?.LogWarning("[AchievementFontFix] 创建系统字体失败: " + ex.Message);
			}

			return _cachedFont;
		}

		private static Type? FindType(string fullName)
		{
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			for (int i = 0; i < assemblies.Length; i++)
			{
				try
				{
					Type? type = assemblies[i].GetType(fullName, false);
					if (type != null)
					{
						return type;
					}
				}
				catch
				{
				}
			}
			return null;
		}
	}
}
