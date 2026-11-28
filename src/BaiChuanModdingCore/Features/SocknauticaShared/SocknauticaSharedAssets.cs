using System;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BaiChuanModdingCore.Features.SocknauticaShared
{
	/// <summary>
	/// Socknautica 多个功能共用的资源读取，避免重复加载 genericwarning。
	/// </summary>
	internal static class SocknauticaSharedAssets
	{
		private static Sprite? _genericWarning;

		internal static Sprite? GetGenericWarningSprite(ManualLogSource? log, string tag)
		{
			if (_genericWarning != null)
			{
				return _genericWarning;
			}

			try
			{
				Type? mainType = AccessTools.TypeByName("Socknautica.Main");
				FieldInfo? bundleField = mainType == null
					? null
					: AccessTools.Field(mainType, "assetBundle");
				if (bundleField?.GetValue(null) is AssetBundle bundle)
				{
					_genericWarning = bundle.LoadAsset<Sprite>("genericwarning");
				}
			}
			catch (Exception ex)
			{
				log?.LogWarning("[" + tag + "] genericwarning load failed: " + ex.Message);
			}

			return _genericWarning;
		}
	}
}
