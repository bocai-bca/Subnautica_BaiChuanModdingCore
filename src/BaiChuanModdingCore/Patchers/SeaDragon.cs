using System.Collections.Generic;
using UnityEngine;

namespace BaiChuanModdingCore.Patchers
{
	internal class SeaDragon : PatcherBase
	{
		internal override List<TechType> GetTargetTechTypes()
		{
			return new List<TechType>()
			{
				TechType.SeaDragon,
			};
		}
		internal override bool DoPatching(List<GameObject> prefabGameObjects)
		{
			LiveMixin liveMixin = prefabGameObjects[0].GetComponent<LiveMixin>();
			if (liveMixin == null)
			{
				return false;
			}
			liveMixin.data.maxHealth = liveMixin.health = 360000f;
			return true;
		}
	}
}