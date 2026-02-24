using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace BaiChuanModdingCore
{
	[BepInPlugin("net.bcasoft.baichuanmoddingcore", "BaiChuanModdingCore", "0.0.2.0")]
	public class BaiChuanModdingCore : BaseUnityPlugin
	{
		internal Harmony harmony = new Harmony("net.bcasoft.baichuanmoddingcore");
		
		private void Awake()
		{
			harmony.Patch(original: typeof(MainMenuMusic).GetMethod("Play"), postfix: new HarmonyMethod(typeof(PatchingEntry).GetMethod("StartPatching")));
			logger = Logger;
			logger.LogMessage("Loaded.");
		}

		internal static ManualLogSource logger;
	}
}