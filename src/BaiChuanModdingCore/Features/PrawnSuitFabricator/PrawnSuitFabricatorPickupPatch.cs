using System;
using HarmonyLib;

namespace BaiChuanModdingCore.Features.PrawnSuitFabricator
{
	/// <summary>
	/// 只影响 CrafterLogic.TryPickup，且仅在当前成品是海虾号时接管。
	/// 载具仓的 ConstructorInput 不走这里，因此原版载具仓路径保持不变。
	/// </summary>
	[HarmonyPatch(typeof(CrafterLogic), "TryPickup")]
	internal static class PrawnSuitFabricatorPickupPatch
	{
		[HarmonyPrefix]
		private static bool Prefix(CrafterLogic __instance)
		{
			try
			{
				if (__instance.currentTechType != TechType.Exosuit)
				{
					return true;
				}

				if (__instance.progress < 1f)
				{
					return true;
				}

				PrawnSuitFabricatorFeature.Log?.LogInfo("PrawnSuitFabricator：成品是海虾号，改为生成到逃生舱附近。");
				PrawnSuitFabricatorFeature.SpawnExosuit();
				__instance.Reset();
				return false;
			}
			catch (Exception ex)
			{
				PrawnSuitFabricatorFeature.Log?.LogError("PrawnSuitFabricator：收取海虾号时出错：" + ex);
				return true;
			}
		}
	}
}
