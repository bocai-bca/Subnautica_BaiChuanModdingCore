using UnityEngine;

namespace BaiChuanModdingCore.Patchers
{
	public class Warper : PatcherBase
	{
		public override TechType GetTargetTechType()
		{
			return TechType.Warper;
		}
		public override bool DoPatching(GameObject prefabGameObjects)
		{
			LiveMixin liveMixin = prefabGameObjects.GetComponent<LiveMixin>();
			if (liveMixin == null)
			{
				return false;
			}
			liveMixin.data.maxHealth = liveMixin.health = 8100f;
			return true;
		}
	}
}