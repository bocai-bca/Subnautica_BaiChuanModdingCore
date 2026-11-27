using System;
using System.Reflection;
using BaiChuanModdingCore.Features.AchievementFontFix;
using BaiChuanModdingCore.Features.AnthVoidTentacleFix;
using BaiChuanModdingCore.Features.AquariumOverflowResearchStation;
using BaiChuanModdingCore.Features.ArenaTeleportBlocker;
using BaiChuanModdingCore.Features.AuroraSecondExplosion;
using BaiChuanModdingCore.Features.AutoCpuAffinity;
using BaiChuanModdingCore.Features.BlueprintTrackerPerf;
using BaiChuanModdingCore.Features.BuildingTweaksPerf;
using BaiChuanModdingCore.Features.LeviathanListExpander;
using BaiChuanModdingCore.Features.CreatureHpFix;
using BaiChuanModdingCore.Features.EasyCraftCreativeFix;
using BaiChuanModdingCore.Features.LookRotationGuard;
using BaiChuanModdingCore.Features.MosasaurusSpawnPoints;
using BaiChuanModdingCore.Features.PrawnSuitFabricator;
using BaiChuanModdingCore.Features.ResourceTrackerGuard;
using BaiChuanModdingCore.Features.TankModuleInstaller;
using BaiChuanModdingCore.Features.TooltipRainbowGradient;
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
			PrawnSuitFabricatorFeature.Initialize(Config, Logger);
			AutoCpuAffinityFeature.Initialize(Config, Logger, this);
			MosasaurusSpawnPointsFeature.Initialize(Logger, this);
			TooltipRainbowGradientFeature.Initialize(Config, Logger);
			TankModuleInstallerFeature.Initialize(Logger);
			LookRotationGuardPatch.Initialize(Logger);
			ResourceTrackerGuardPatch.Initialize(Logger);
			AchievementFontFixFeature.Initialize(harmony, Logger);
			AquariumOverflowResearchStationFeature.Initialize(harmony, Logger);
			ArenaTeleportBlockerFeature.Initialize(Config, harmony, Logger, this);
			AuroraSecondExplosionFeature.Initialize(Config, Logger, this);
			LeviathanListExpanderFeature.Initialize(Config, harmony, Logger, this);
			CreatureHpFixFeature.Initialize(Logger, this);
			AnthVoidTentacleFixFeature.Initialize(harmony, Logger);
			EasyCraftCreativeFixFeature.Initialize(harmony, Logger);
			BuildingTweaksPerfFeature.Initialize(harmony, Logger);
			BlueprintTrackerPerfFeature.Initialize(harmony, Logger);
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
			if (!FrameDistributedTask_PlayerIntoVoid.LoadSound())
			{
				logger?.LogError("Failed to load SoundOnPlayerIntoVoid.");
			}
			if (!FrameDistributedTask_GargantuanMusic.LoadSound())
			{
				logger?.LogError("Failed to load GargantuanMusic.");
			}
			dropAllOnDeath = Config.Bind("Switches", "DropAllOnDeath", false, "Control will the patch which is \"drop all items on player death\" work.");
			logger?.LogMessage("Loaded.");
			NuclearReactorModify.DoModify();
		}

		private void Start()
		{
			Invoke(nameof(InitMusicPlay), 1f);
		}

		private void Update()
		{
			if (PrawnSuitFabricatorFeature.Active)
			{
				PrawnSuitFabricatorFeature.Update();
			}

			TooltipRainbowGradientFeature.Tick();
			FrameDistributedTaskManager.Update();
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