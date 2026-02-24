using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace BaiChuanModdingCore
{
	[BepInPlugin("net.bcasoft.baichuanmoddingcore", "BaiChuanModdingCore", "0.0.2.0")]
	public class BaiChuanModdingCore : BaseUnityPlugin
	{
		private const int PatchStartingTimer = 60;
		
		internal Harmony harmony = new Harmony("net.bcasoft.baichuanmoddingcore");
		
		private void Awake()
		{
			Instance = this;
			harmony.Patch(original: typeof(WaterSurface).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance), postfix: new HarmonyMethod(typeof(BaiChuanModdingCore).GetMethod("StartTiming", BindingFlags.Static | BindingFlags.Public)));
			logger = Logger;
			logger.LogMessage("Loaded.");
		}

		private void Update()
		{
			if (patchStartingTimer >= 0)
			{
				if (patchStartingTimer == 0)
				{
					PatchingEntry.StartPatching();
				}
				patchStartingTimer--;
			}
			else
			{
				enabled = false;
			}
		}

		public static void StartTiming()
		{
			patchStartingTimer = PatchStartingTimer;
			Instance.enabled = true;
		}

		internal static ManualLogSource logger;

		internal static int patchStartingTimer = -1;

		public static BaiChuanModdingCore Instance;
	}
}