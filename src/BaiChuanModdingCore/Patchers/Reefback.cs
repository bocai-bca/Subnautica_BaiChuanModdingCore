using System.Collections.Generic;
using UnityEngine;

namespace BaiChuanModdingCore.Patchers
{
	public class Reefback : PatcherBase
	{
		public override TechType GetTargetTechType()
		{
			return TechType.Reefback;
		}
		public override bool DoPatching(GameObject prefabGameObjects)
		{
			LiveMixin liveMixin = prefabGameObjects.GetComponent<LiveMixin>();
			if (liveMixin == null)
			{
				return false;
			}
			liveMixin.data.maxHealth = liveMixin.health = 450000f;
			return true;
		}
	}
}