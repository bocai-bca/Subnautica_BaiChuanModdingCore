using UnityEngine;

namespace BaiChuanModdingCore.Patchers
{
	public class SeaEmperorJuvenile : PatcherBase
	{
		public override TechType GetTargetTechType()
		{
			return TechType.SeaEmperorJuvenile;
		}
		public override bool DoPatching(GameObject prefabGameObjects)
		{
			LiveMixin liveMixin = prefabGameObjects.GetComponent<LiveMixin>();
			if (liveMixin == null)
			{
				return false;
			}
			liveMixin.data.maxHealth = liveMixin.health = 225000f;
			return true;
		}
	}
}