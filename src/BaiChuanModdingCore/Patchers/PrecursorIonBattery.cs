using System.Collections.Generic;
using UnityEngine;

namespace BaiChuanModdingCore.Patchers
{
	internal class PrecursorIonBattery : PatcherBase
	{
		internal override List<TechType> GetTargetTechTypes()
		{
			return new List<TechType>()
			{
				TechType.PrecursorIonBattery,
			};
		}
		internal override bool DoPatching(List<GameObject> prefabGameObjects)
		{
			Battery battery = prefabGameObjects[0].GetComponent<Battery>();
			if (battery == null)
			{
				return false;
			}
			battery ._charge = battery._capacity = 1500f;
			return true;
		}
	}
}