using UnityEngine;

namespace BaiChuanModdingCore
{
	/// <summary>
	/// 补丁类型基类，所有的补丁都应该继承自本类型，并在实现后于PatchingEntry中添加实例到taskPatchers字段。子类名字可以随便起，该名字将被输出到日志
	/// </summary>
	public abstract class PatcherBase
	{
		/// <summary>
		/// 覆写此方法以指定要修补的目标TechType
		/// </summary>
		/// <returns>要修补其prefab的TechType</returns>
		public abstract TechType GetTargetTechType();

		/// <summary>
		/// 执行修补的入口方法
		/// </summary>
		/// <param name="prefabGameObjects">对应prefab的GameObject</param>
		/// <returns>补丁运作的成功与否</returns>
		public abstract bool DoPatching(GameObject prefabGameObjects);
	}
}