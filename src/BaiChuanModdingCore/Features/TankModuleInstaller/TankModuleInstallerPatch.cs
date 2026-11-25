using System;
using HarmonyLib;
using UnityEngine;

namespace BaiChuanModdingCore.Features.TankModuleInstaller
{
	/// <summary>
	/// 只拦截 Tank 的升级控制台点击；其他载具走原版界面。
	/// </summary>
	[HarmonyPatch(typeof(VehicleUpgradeConsoleInput), "OnHandClick")]
	internal static class TankModuleInstallerPatch
	{
		[HarmonyPrefix]
		private static bool Prefix(VehicleUpgradeConsoleInput __instance)
		{
			try
			{
				Vehicle? vehicle = __instance.GetComponentInParent<Vehicle>();
				if (vehicle == null)
				{
					return true;
				}
				if (vehicle.GetType().Name != "Tank")
				{
					return true;
				}

				TankModuleInstallerFeature.TryInstall(vehicle);
				return false;
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[TankModuleInstaller] " + ex.Message);
				return true;
			}
		}
	}
}
