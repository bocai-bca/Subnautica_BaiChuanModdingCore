using System;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BaiChuanModdingCore.Features.LookRotationGuard
{
	/// <summary>
	/// 独立功能：拦截 Quaternion.LookRotation 的零向量输入。
	/// 根因是 ECCLibrary 的 TrailManager 补丁在相邻尾迹段重合时传入零向量；
	/// Unity 原生代码会打印大量 "Look rotation viewing vector is zero" 并采集堆栈。
	/// 这里只在 |forward|² &lt; 1e-8 时返回 identity，其余输入完全放行。
	/// </summary>
	[HarmonyPatch(typeof(Quaternion), "LookRotation", new[] { typeof(Vector3), typeof(Vector3) })]
	internal static class LookRotationGuardPatch
	{
		private const float MinSqr = 1e-8f;
		private static int _blocked;
		private static ManualLogSource? _log;

		internal static void Initialize(ManualLogSource log)
		{
			_log = log;
			_log?.LogInfo("[LookRotationGuard] v1.1 ready (zero-vector LookRotation guarded, minSqr=1E-08)");
		}

		[HarmonyPrefix]
		private static bool Prefix(Vector3 forward, Vector3 upwards, ref Quaternion __result)
		{
			try
			{
				if (forward.sqrMagnitude >= MinSqr)
				{
					return true;
				}

				__result = Quaternion.identity;
				_blocked++;

				if (_blocked <= 10 || _blocked % 500 == 0)
				{
					string stack = Environment.StackTrace;
					if (stack.Length > 2000)
					{
						stack = stack.Substring(0, 2000);
					}

					_log?.LogInfo(
						"[LookRotationGuard] blocked zero-vector LookRotation #" + _blocked +
						" |forward|=" + Mathf.Sqrt(forward.sqrMagnitude).ToString("0.00000000") +
						"\n" + stack);
				}

				return false;
			}
			catch
			{
				return true;
			}
		}
	}
}
