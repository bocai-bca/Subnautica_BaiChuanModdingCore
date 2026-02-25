using UnityEngine;

namespace BaiChuanModdingCore
{
	public class Patch_CraftData
	{
		public static bool Prefix(ref TechType __state, TechType techType, bool verbose)
		{
			__state = techType;
			return true;
		}

		public static void Postfix(TechType __state, ref GameObject __result)
		{
			PatcherBase patcher = PatcherManager.GetPatcherForTechType(__state);
			if (patcher != null)
			{
				if (!patcher.DoPatching(__result))
				{
					BaiChuanModdingCore.logger.LogError("Failed to run patcher on TechType " + __state);
				}
				BaiChuanModdingCore.logger.LogMessage("Successed to run patcher on TechType " + __state);
				return;
			}
			//BaiChuanModdingCore.logger.LogInfo("No patcher was registed for TechType " + __state);
		}

		public static void PostfixAsync(TechType __state, ref CoroutineTask<GameObject> __result)
		{
			PatcherBase patcher = PatcherManager.GetPatcherForTechType(__state);
			if (patcher != null)
			{
				do
				{
					if (!patcher.DoPatching(__result.GetResult()))
					{
						BaiChuanModdingCore.logger.LogError("Failed to run patcher ASYNC on TechType " + __state);
					}
					BaiChuanModdingCore.logger.LogMessage("Successed to run patcher on TechType " + __state);
				} while (__result.MoveNext());
				return;
			}
			//BaiChuanModdingCore.logger.LogInfo("No patcher was registed for TechType " + __state);
		}
	}
}