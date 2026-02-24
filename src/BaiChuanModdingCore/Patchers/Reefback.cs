using System.Collections.Generic;
using UnityEngine;

namespace BaiChuanModdingCore.Patchers
{
	internal class Reefback : PatcherBase
	{
		internal override List<TechType> GetTargetTechTypes()
		{
			return new List<TechType>()
			{
				TechType.Reefback,
			};
		}
		internal override bool DoPatching(List<GameObject> prefabGameObjects)
		{
			LiveMixin liveMixin = prefabGameObjects[0].GetComponent<LiveMixin>();
			if (liveMixin == null)
			{
				return false;
			}
			liveMixin.data.maxHealth = liveMixin.health = 450000f;
			return true;
		}
	}
}