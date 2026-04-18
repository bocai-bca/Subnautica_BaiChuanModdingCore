using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace BaiChuanModdingCore;

/// <summary>
/// 物品掉落补丁，将在死前使所有物品变为可掉落状态，在游戏原生掉落物品后再强制执行掉落物品
/// </summary>
[HarmonyPatch(typeof(Inventory), nameof(Inventory.LoseItems))]
public class Patch_Inventory_LoseItems
{
	public static void Prefix(Inventory __instance)
	{
		if (BaiChuanModdingCore.dropAllOnDeath == null) return;
		if (!BaiChuanModdingCore.dropAllOnDeath.Value) return;
		BaiChuanModdingCore.logger?.LogMessage("De-secure inventory items.");
		foreach (InventoryItem item in Inventory.main.container)
		{
			item.item.destroyOnDeath = true;
		}
		foreach (InventoryItem item in (IItemsContainer)Inventory.main.equipment)
		{
			item.item.destroyOnDeath = true;
		}
	}

	public static void Postfix(Inventory __instance)
	{
		if (BaiChuanModdingCore.dropAllOnDeath == null) return;
		if (!BaiChuanModdingCore.dropAllOnDeath.Value) return;
		Player player = Player.main;
		/*bool shouldReturn = false;
		if (player.currentSub != null && player.currentWaterPark == null)
		{
			shouldReturn = true;
			BaiChuanModdingCore.logger?.LogWarning("DropAllOnDeath cancelled, Player.currentSub != null && Player.currentWaterPark == null.");
		}
		if (player.isPiloting)
		{
			shouldReturn = true;
			BaiChuanModdingCore.logger?.LogWarning("DropAllOnDeath cancelled, Player.isPiloting == true.");
		}
		if (shouldReturn)
		{
			return;
		}*/
		// 模拟丢物品
		BaiChuanModdingCore.logger?.LogMessage("Simulating death drop.");
		List<Pickupable> pickupables = [];
		pickupables.AddRange(Inventory.main.container.Select(item => item.item));
		foreach (Pickupable pickupable in pickupables)
		{
			Transform transform = MainCameraControl.main.transform;
			Vector3 dropPosition = Inventory.RayCast(transform.position, transform.forward, 10f, 0.75f, 1.5f);
			pickupable.Drop(dropPosition);
			if (pickupable.randomizeRotationWhenDropped)
			{
				pickupable.transform.rotation = Random.rotation;
			}
			SkyEnvironmentChanged.Send(pickupable.gameObject, Player.main.GetSkyEnvironment());
		}
	}
}