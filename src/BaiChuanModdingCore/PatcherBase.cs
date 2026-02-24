using System.Collections.Generic;
using UnityEngine;

namespace BaiChuanModdingCore
{
	/// <summary>
	/// 补丁类型基类，所有的补丁都应该继承自本类型，并在实现后于PatchingEntry中添加实例到taskPatchers字段。子类名字可以随便起，该名字将被输出到日志
	/// </summary>
	internal abstract class PatcherBase
	{
		/// <summary>
		/// 覆写此方法以指定要修补的目标TechType
		/// </summary>
		/// <returns>所有要获取prefab的TechType</returns>
		internal abstract List<TechType> GetTargetTechTypes();

		/// <summary>
		/// 执行修补的入口方法，将由PatchingEntry调用
		/// </summary>
		/// <param name="prefabGameObjects">所有由GetTargetTechTypes()给定的TechType的prefab，在列表中的顺序与GetTargetTechTypes()给定的顺序一致</param>
		/// <returns>补丁运作的成功与否</returns>
		internal abstract bool DoPatching(List<GameObject> prefabGameObjects);
	}
}