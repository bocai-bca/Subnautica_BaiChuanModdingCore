using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace BaiChuanModdingCore
{
	[BepInPlugin("net.bcasoft.baichuanmoddingcore", "BaiChuanModdingCore", "0.1.3.0")]
	public class BaiChuanModdingCore : BaseUnityPlugin
	{
		internal Harmony harmony = new("net.bcasoft.baichuanmoddingcore");
		
		private void Awake()
		{
			Instance = this;
			logger = Logger;
			try
			{
				HarmonyMethod prefix = new(typeof(Patch_CraftData).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public));
				harmony.Patch(typeof(CraftData).GetMethod("GetPrefabForTechType", BindingFlags.Public | BindingFlags.Static), prefix,new HarmonyMethod(typeof(Patch_CraftData).GetMethod("Postfix", BindingFlags.Static | BindingFlags.Public)));
				harmony.Patch(typeof(CraftData).GetMethod("GetPrefabForTechTypeAsync", BindingFlags.Public | BindingFlags.Static, null, [typeof(TechType), typeof(bool)], null), prefix,new HarmonyMethod(typeof(Patch_CraftData).GetMethod("PostfixAsync", BindingFlags.Static | BindingFlags.Public)));
				harmony.Patch(typeof(MainMenuMusic).GetMethod("Play", BindingFlags.Public | BindingFlags.Static), null, new HarmonyMethod(typeof(MainMenuMusicModded).GetMethod("PlayPostfix", BindingFlags.Static | BindingFlags.Public)));
				harmony.Patch(typeof(MainMenuMusic).GetMethod("Stop", BindingFlags.Public | BindingFlags.Static), null, new HarmonyMethod(typeof(MainMenuMusicModded).GetMethod("StopPostfix", BindingFlags.Static | BindingFlags.Public)));
				CreaturePatches.DoPatch(harmony);
				harmony.PatchAll();
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				throw;
			}
			if (!MainMenuMusicModded.LoadModMusic())
			{
				logger?.LogError("Failed to load mod music.");
			}
			dropAllOnDeath = Config.Bind("Switches", "DropAllOnDeath", false, "Control will the patch which is \"drop all items on player death\" work.");
			logger?.LogMessage("Loaded.");
		}

		private void Start()
		{
			Invoke(nameof(InitMusicPlay), 1f);
		}

		public void InitMusicPlay()
		{
			MainMenuMusic.Stop();
			MainMenuMusic.Play();
			logger?.LogMessage("Playing mod music manually.");
		}

		internal static ManualLogSource? logger;

		public static BaiChuanModdingCore? Instance;

		internal static ConfigEntry<bool>? dropAllOnDeath;
	}
}