using UnityEngine;

namespace BaiChuanModdingCore.Patchers
{
	public class PrecursorIonBattery : PatcherBase
	{
		public override TechType GetTargetTechType()
		{
			return TechType.PrecursorIonBattery;
		}
		public override bool DoPatching(GameObject prefabGameObjects)
		{
			Battery battery = prefabGameObjects.GetComponent<Battery>();
			if (battery == null)
			{
				return false;
			}
			battery._charge = battery._capacity = 1500f;
			return true;
		}
	}
}