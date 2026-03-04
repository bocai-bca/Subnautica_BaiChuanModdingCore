using System.Collections.Generic;
using HarmonyLib;

namespace BaiChuanModdingCore
{
	[HarmonyPatch(typeof(Language), nameof(Language.TryGet))]
	public class Patch_Language_TryGet
	{
		public static Dictionary<string, string> OverrideTranslations = new()
		{
			{"Quartz", "石英(翻译覆写测试)"},
		};

		public static void Prefix(string key, out string __state)
		{
			__state = key;
		}

		public static void Postfix(ref string result, string __state)
		{
			if (OverrideTranslations.TryGetValue(__state, out string? translationOverrided))
			{
				result = translationOverrided;
			}
		}
	}
}