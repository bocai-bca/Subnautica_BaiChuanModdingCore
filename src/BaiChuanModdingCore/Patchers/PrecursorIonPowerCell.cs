using UnityEngine;

namespace BaiChuanModdingCore.Patchers
{
	public class PrecursorIonPowerCell : PatcherBase
	{
		public override TechType GetTargetTechType()
		{
			return TechType.PrecursorIonPowerCell;
		}
		public override bool DoPatching(GameObject prefabGameObjects)
		{
			Battery battery = prefabGameObjects.GetComponent<Battery>();
			if (battery == null)
			{
				return false;
			}
			battery._charge = battery._capacity = 3000f;
			return true;
		}
	}
}