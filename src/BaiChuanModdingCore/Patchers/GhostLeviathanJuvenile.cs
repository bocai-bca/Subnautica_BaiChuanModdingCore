using UnityEngine;

namespace BaiChuanModdingCore.Patchers
{
	public class GhostLeviathanJuvenile : PatcherBase
	{
		public override TechType GetTargetTechType()
		{
			return TechType.GhostLeviathanJuvenile;
		}
		public override bool DoPatching(GameObject prefabGameObjects)
		{
			LiveMixin liveMixin = prefabGameObjects.GetComponent<LiveMixin>();
			if (liveMixin == null)
			{
				return false;
			}
			liveMixin.data.maxHealth = liveMixin.health = 315000f;
			return true;
		}
	}
}