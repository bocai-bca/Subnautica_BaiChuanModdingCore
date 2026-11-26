using HarmonyLib;

namespace BaiChuanModdingCore.Features.CreatureHpFix
{
	[HarmonyPatch(typeof(LiveMixin), "Start")]
	internal static class CreatureHpFixPatch
	{
		[HarmonyPostfix]
		private static void Postfix(LiveMixin __instance)
		{
			try
			{
				if (__instance == null)
				{
					return;
				}

				Creature? creature = __instance.GetComponent<Creature>();
				if (creature == null ||
					!CreatureHpFixFeature.ShouldWatch(creature.GetType().Name))
				{
					return;
				}

				CreatureHpFixFeature.TryFix(__instance, creature, "Start");
				if (CreatureHpFixFeature.Host != null)
				{
					CreatureHpFixFeature.Host.StartCoroutine(
						CreatureHpFixFeature.Confirm(__instance, creature, "Start"));
				}
			}
			catch (System.Exception ex)
			{
				CreatureHpFixFeature.Log?.LogWarning("[CreatureHpFix] " + ex.Message);
			}
		}
	}
}
