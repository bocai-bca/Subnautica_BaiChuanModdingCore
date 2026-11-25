using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BaiChuanModdingCore.Features.AutoCpuAffinity
{
	/// <summary>
	/// 独立功能：等待 PerformanceBooster 加载后，按 CPU 拓扑调整它的 CPUCores，
	/// 并防止 PerformanceBooster 自己再次覆盖 affinity。
	/// 本功能只操作 PerformanceBooster，不直接改游戏或核心其他模块。
	/// </summary>
	internal static class AutoCpuAffinityFeature
	{
		private const int RelationProcessorCore = 0;

		private static ConfigEntry<bool> _autoDetect = null!;
		private static ConfigEntry<int> _overrideCores = null!;
		internal static bool _weOwnAffinity;
		private static ManualLogSource? _log;

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool GetLogicalProcessorInformationEx(
			int relationship, IntPtr buffer, ref uint returnLength);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool SetProcessAffinityMask(IntPtr process, UIntPtr mask);

		[DllImport("kernel32.dll")]
		private static extern IntPtr GetCurrentProcess();

		internal static void Initialize(ConfigFile config, ManualLogSource log, MonoBehaviour host)
		{
			_log = log;
			_autoDetect = config.Bind(
				"AutoCpuAffinity",
				"AutoDetect",
				true,
				"自动检测 CPU 并设置 PerformanceBooster 的核心数。");
			_overrideCores = config.Bind(
				"AutoCpuAffinity",
				"OverrideCores",
				0,
				">0 时强制使用这个核心数（0 = 自动）。");

			host.StartCoroutine(Apply());
			_log?.LogInfo("AutoCpuAffinity 功能已初始化。");
		}

		private static IEnumerator Apply()
		{
			Type? mp = null;
			for (int i = 0; i < 600; i++)
			{
				if (mp != null)
				{
					break;
				}

				mp = FindType("PerformanceBooster.MainPatcher");
				if (mp == null)
				{
					yield return null;
				}
			}

			if (mp == null)
			{
				_log?.LogWarning("[AutoCpuAffinity] PerformanceBooster not found, skip");
				yield break;
			}

			FieldInfo? coresField = mp.GetField("CPUCores",
				BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			FieldInfo? enabledField = mp.GetField("affinityTweakEnabled",
				BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			MethodInfo? setAffinity = mp.GetMethod("SetAffinity",
				BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

			if (coresField == null || setAffinity == null)
			{
				_log?.LogWarning("[AutoCpuAffinity] members not found, skip");
				yield break;
			}

			if (!_autoDetect.Value && _overrideCores.Value <= 0)
			{
				_log?.LogInfo("[AutoCpuAffinity] AutoDetect=false, do nothing");
				yield break;
			}

			Detect(out int physical, out int smtCores, out int pCores,
				out int logical, out bool hybrid);

			int best = _overrideCores.Value > 0
				? _overrideCores.Value
				: (!hybrid || pCores <= 0 ? physical : pCores);

			if (best < 2)
			{
				best = 2;
			}
			if (best > 16)
			{
				best = 16;
			}
			if (logical > 0 && best * 2 - 1 > logical)
			{
				best = Math.Max(2, (logical + 1) / 2);
			}

			var sb = new StringBuilder(220);
			sb.Append("[AutoCpuAffinity] logical=").Append(logical);
			sb.Append(" physical=").Append(physical);
			sb.Append(" smtCores=").Append(smtCores);
			sb.Append(" pCores=").Append(pCores);
			sb.Append(" hybrid=").Append(hybrid ? "1" : "0");
			sb.Append(" -> best=").Append(best);
			_log?.LogInfo(sb.ToString());

			int current = (int)coresField.GetValue(null)!;
			_log?.LogInfo("[AutoCpuAffinity] PerformanceBooster.CPUCores currently = " + current);

			if (_overrideCores.Value <= 0 && current != 4)
			{
				_log?.LogInfo("[AutoCpuAffinity] user already customized (" + current + "), keep it");
				yield break;
			}

			coresField.SetValue(null, best);
			if (enabledField != null)
			{
				enabledField.SetValue(null, true);
			}

			try
			{
				setAffinity.Invoke(null, new object[] { best });
			}
			catch (Exception ex)
			{
				_log?.LogWarning("[AutoCpuAffinity] SetAffinity failed: " + ex.Message);
			}

			_log?.LogInfo("[AutoCpuAffinity] applied cores=" + best);
			_weOwnAffinity = true;
			TryGuardSetAffinity(mp);
		}

		private static void TryGuardSetAffinity(Type mp)
		{
			try
			{
				MethodInfo? method = mp.GetMethod("SetAffinity",
					BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				if (method == null)
				{
					_log?.LogWarning("[AutoCpuAffinity] SetAffinity not found for guard");
					return;
				}

				var harmony = new Harmony("com.baichuan.autocpuaffinity.guard");
				var prefix = new HarmonyMethod(
					typeof(AutoCpuAffinityFeature).GetMethod(nameof(SetAffinity_Prefix),
						BindingFlags.Static | BindingFlags.Public));
				harmony.Patch(method, prefix);
				_log?.LogInfo("[AutoCpuAffinity] guarded PerformanceBooster.SetAffinity");
			}
			catch (Exception ex)
			{
				_log?.LogWarning("[AutoCpuAffinity] guard failed: " + ex.Message);
			}
		}

		public static bool SetAffinity_Prefix()
		{
			if (_weOwnAffinity)
			{
				Debug.Log("[AutoCpuAffinity] blocked PerformanceBooster.SetAffinity (affinity owned by us)");
				return false;
			}

			return true;
		}

		private static Type? FindType(string fullName)
		{
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			for (int i = 0; i < assemblies.Length; i++)
			{
				string name;
				try
				{
					name = assemblies[i].GetName().Name;
				}
				catch
				{
					continue;
				}

				if (name != "PerformanceBooster")
				{
					continue;
				}

				try
				{
					return assemblies[i].GetType(fullName, false);
				}
				catch
				{
				}
			}

			return null;
		}

		private static void Detect(out int physical, out int smtCores, out int pCores,
			out int logical, out bool hybrid)
		{
			physical = 0;
			smtCores = 0;
			pCores = 0;
			logical = Environment.ProcessorCount;
			hybrid = false;
			var cores = new List<byte>();

			try
			{
				uint returnLength = 0;
				GetLogicalProcessorInformationEx(RelationProcessorCore, IntPtr.Zero, ref returnLength);
				if (returnLength == 0)
				{
					physical = Math.Max(1, logical / 2);
					pCores = physical;
					return;
				}

				IntPtr buffer = Marshal.AllocHGlobal((int)returnLength);
				try
				{
					if (!GetLogicalProcessorInformationEx(RelationProcessorCore, buffer, ref returnLength))
					{
						physical = Math.Max(1, logical / 2);
						pCores = physical;
						return;
					}

					int offset = 0;
					while (offset + 8 <= (int)returnLength)
					{
						int size = Marshal.ReadInt32(buffer, offset + 4);
						if (size <= 0)
						{
							break;
						}

						byte flags = Marshal.ReadByte(buffer, offset + 8);
						byte efficiencyClass = Marshal.ReadByte(buffer, offset + 9);
						physical++;
						if ((flags & 1u) != 0)
						{
							smtCores++;
						}
						if (!cores.Contains(efficiencyClass))
						{
							cores.Add(efficiencyClass);
						}

						offset += size;
					}
				}
				finally
				{
					Marshal.FreeHGlobal(buffer);
				}
			}
			catch
			{
				physical = Math.Max(1, logical / 2);
				pCores = physical;
				return;
			}

			hybrid = cores.Count > 1;
			pCores = smtCores > 0 ? smtCores : physical;
			if (physical <= 0)
			{
				physical = Math.Max(1, logical / 2);
			}
		}
	}
}
