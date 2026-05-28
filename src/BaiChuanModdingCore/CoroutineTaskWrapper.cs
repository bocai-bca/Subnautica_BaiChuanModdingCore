// 由Deepseek提供
// 吗的自诩清高老资历的守旧派代码人面对协程难题终究还是自知不敌地向vibe coding低头了，作哀。

using System;
using System.Collections;
using UnityEngine;

public static class CoroutineTaskWrapper
{
	public static CoroutineTask<GameObject> Wrap(CoroutineTask<GameObject> original, Func<GameObject, GameObject> modifier)
	{
		var result = new TaskResult<GameObject>();
		IEnumerator Wrapper()
		{
			yield return original;
			var originalValue = original.GetResult();
			var modifiedValue = modifier(originalValue);
			result.Set(modifiedValue);
		}
		return new CoroutineTask<GameObject>(Wrapper(), result);
	}
}