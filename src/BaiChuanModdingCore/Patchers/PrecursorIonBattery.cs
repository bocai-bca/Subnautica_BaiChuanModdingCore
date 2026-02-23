using System.Collections.Generic;
using UnityEngine;

namespace BaiChuanCustomCore.Patchers
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
		internal override void DoPatching(List<GameObject> prefabGameObjects)
		{
			Battery battery = prefabGameObjects[0].GetComponent<Battery>();
			if (battery == null)
			{
				return;
			}
			battery._charge = battery._capacity = 520f;
		}
	}
}