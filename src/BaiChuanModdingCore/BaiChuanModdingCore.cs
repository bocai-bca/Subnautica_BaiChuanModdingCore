using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace BaiChuanModdingCore
{
	[BepInPlugin("net.bcasoft.baichuanmoddingcore", "BaiChuanModdingCore", "0.1.0.0")]
	public class BaiChuanModdingCore : BaseUnityPlugin
	{
		internal Harmony harmony = new Harmony("net.bcasoft.baichuanmoddingcore");
		
		private void Awake()
		{
			Instance = this;
			HarmonyMethod prefix = new HarmonyMethod(typeof(Patch_CraftData).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public));
			harmony.Patch(typeof(CraftData).GetMethod("GetPrefabForTechType", BindingFlags.Public | BindingFlags.Static), prefix,new HarmonyMethod(typeof(Patch_CraftData).GetMethod("Postfix", BindingFlags.Static | BindingFlags.Public)));
			harmony.Patch(typeof(CraftData).GetMethod("GetPrefabForTechTypeAsync", BindingFlags.Public | BindingFlags.Static, null, new []{typeof(TechType), typeof(bool)}, null), prefix,new HarmonyMethod(typeof(Patch_CraftData).GetMethod("PostfixAsync", BindingFlags.Static | BindingFlags.Public)));
			logger = Logger;
			logger.LogMessage("Loaded.");
		}

		internal static ManualLogSource logger;

		public static BaiChuanModdingCore Instance;
	}
}