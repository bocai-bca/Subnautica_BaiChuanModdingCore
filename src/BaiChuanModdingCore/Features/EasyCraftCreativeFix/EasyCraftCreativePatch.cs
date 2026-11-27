using HarmonyLib;

namespace BaiChuanModdingCore.Features.EasyCraftCreativeFix
{
	internal static class EasyCraftCreativePatch
	{
		[HarmonyPrefix]
		internal static bool Prefix(ref bool __result)
		{
			if (GameModeUtils.RequiresIngredients())
			{
				return true;
			}

			__result = true;
			if (!EasyCraftCreativeFixFeature.Logged)
			{
				EasyCraftCreativeFixFeature.Logged = true;
				EasyCraftCreativeFixFeature.Log?.LogInfo(
					"[EasyCraftCreativeFix] creative mode bypass active; queue checks return true");
			}
			return false;
		}
	}
}
