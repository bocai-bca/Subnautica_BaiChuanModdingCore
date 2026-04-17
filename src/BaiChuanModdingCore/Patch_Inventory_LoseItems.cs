using HarmonyLib;

namespace BaiChuanModdingCore;

/// <summary>
/// 物品掉落补丁，将在死前使所有物品变为可掉落状态
/// </summary>
[HarmonyPatch(typeof(Inventory), nameof(Inventory.LoseItems))]
public class Patch_Inventory_LoseItems
{
	public static void Prefix(Inventory __instance)
	{
		if (BaiChuanModdingCore.dropAllOnDeath == null) return;
		if (!BaiChuanModdingCore.dropAllOnDeath.Value) return;
		foreach (InventoryItem item in Inventory.main.container)
		{
			item.item.destroyOnDeath = true;
		}
		foreach (InventoryItem item in (IItemsContainer)Inventory.main.equipment)
		{
			item.item.destroyOnDeath = true;
		}
	}
}