using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace BaiChuanModdingCore
{
	[BepInPlugin("net.bcasoft.baichuanmoddingcore", "BaiChuanModdingCore", "0.1.1.0")]
	public class BaiChuanModdingCore : BaseUnityPlugin
	{
		internal Harmony harmony = new Harmony("net.bcasoft.baichuanmoddingcore");
		
		private void Awake()
		{
			Instance = this;
			HarmonyMethod prefix = new HarmonyMethod(typeof(Patch_CraftData).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public));
			harmony.Patch(typeof(CraftData).GetMethod("GetPrefabForTechType", BindingFlags.Public | BindingFlags.Static), prefix,new HarmonyMethod(typeof(Patch_CraftData).GetMethod("Postfix", BindingFlags.Static | BindingFlags.Public)));
			harmony.Patch(typeof(CraftData).GetMethod("GetPrefabForTechTypeAsync", BindingFlags.Public | BindingFlags.Static, null, new []{typeof(TechType), typeof(bool)}, null), prefix,new HarmonyMethod(typeof(Patch_CraftData).GetMethod("PostfixAsync", BindingFlags.Static | BindingFlags.Public)));
			harmony.Patch(typeof(MainMenuMusic).GetMethod("Play", BindingFlags.Public | BindingFlags.Static), null, new HarmonyMethod(typeof(MainMenuMusicModded).GetMethod("PlayPostfix", BindingFlags.Static | BindingFlags.Public)));
			harmony.Patch(typeof(MainMenuMusic).GetMethod("Stop", BindingFlags.Public | BindingFlags.Static), null, new HarmonyMethod(typeof(MainMenuMusicModded).GetMethod("StopPostfix", BindingFlags.Static | BindingFlags.Public)));
			logger = Logger;
			if (!MainMenuMusicModded.LoadModMusic())
			{
				logger?.LogError("Failed to load mod music.");
			}
			logger?.LogMessage("Loaded.");
			MainMenuMusic.Stop();
			MainMenuMusic.Play();
		}

		internal static ManualLogSource? logger;

		public static BaiChuanModdingCore? Instance;
	}
}