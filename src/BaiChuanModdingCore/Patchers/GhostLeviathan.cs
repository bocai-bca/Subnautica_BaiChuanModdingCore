using System.Collections.Generic;
using UnityEngine;

namespace BaiChuanModdingCore.Patchers
{
	public class GhostLeviathan : PatcherBase
	{
		public override TechType GetTargetTechType()
		{
			return TechType.GhostLeviathan;
		}
		public override bool DoPatching(GameObject prefabGameObjects)
		{
			LiveMixin liveMixin = prefabGameObjects.GetComponent<LiveMixin>();
			if (liveMixin == null)
			{
				return false;
			}
			liveMixin.data.maxHealth = liveMixin.health = 360000f;
			return true;
		}
	}
}