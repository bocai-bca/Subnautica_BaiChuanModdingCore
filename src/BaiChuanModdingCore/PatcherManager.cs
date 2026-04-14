using System.Collections.Generic;
using System.Linq;
using BaiChuanModdingCore.Patchers;

namespace BaiChuanModdingCore
{
	public static class PatcherManager
	{
		/// <summary>
		/// 获取适用于给定TechType的补丁类
		/// </summary>
		/// <param name="techType">目标TechType</param>
		/// <returns>适用于给定TechType的补丁类，在未找到时返回null</returns>
		public static PatcherBase? GetPatcherForTechType(TechType techType)
		{
			return taskPatchers.FirstOrDefault(patcher => patcher.GetTargetTechType() == techType);
		}
		
		/// <summary>
		/// 记录所有待修补的补丁类型实例
		/// </summary>
		public static List<PatcherBase> taskPatchers = new List<PatcherBase>()
		{
			new PrecursorIonBattery(),
			new PrecursorIonPowerCell(),
		};
	}
}