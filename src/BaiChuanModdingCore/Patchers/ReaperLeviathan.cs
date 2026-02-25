using UnityEngine;

namespace BaiChuanModdingCore.Patchers
{
	public class ReaperLeviathan : PatcherBase
	{
		public override TechType GetTargetTechType()
		{
			return TechType.ReaperLeviathan;
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