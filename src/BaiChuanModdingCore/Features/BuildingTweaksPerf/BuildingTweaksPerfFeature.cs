using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BaiChuanModdingCore.Features.BuildingTweaksPerf
{
	/// <summary>
	/// 独立功能：针对 BuildingTweaks 的两个高频 Postfix 做缓存/替换。
	/// 原来每个 Postfix 都做 LINQ/GetComponents，现在改为固定缓冲区和缓存。
	/// </summary>
	internal static class BuildingTweaksPerfFeature
	{
		internal static ManualLogSource? Log;

		internal static void Initialize(Harmony harmony, ManualLogSource log)
		{
			Log = log;
			Patches.Install(harmony);
		}
	}

	internal static class Patches
	{
		internal static void Install(Harmony harmony)
		{
			Type? playerPatch = AccessTools.TypeByName(
				"BuildingTweaks.Patches.Player_Update_Patch");
			Type? allowedPatch = AccessTools.TypeByName(
				"BuildingTweaks.Patches.Builder_UpdateAllowed_Patch");

			if (playerPatch == null || allowedPatch == null)
			{
				BuildingTweaksPerfFeature.Log?.LogError(
					"[BuildingTweaksPerf] BuildingTweaks patch types not found");
				return;
			}

			MethodInfo? playerPostfix = AccessTools.Method(playerPatch, "Postfix");
			MethodInfo? allowedPostfix = AccessTools.Method(allowedPatch, "Postfix");

			if (playerPostfix != null)
			{
				harmony.Patch(playerPostfix, transpiler: new HarmonyMethod(
					typeof(Patches).GetMethod(
						nameof(PlayerPostfixTranspiler),
						BindingFlags.Static | BindingFlags.NonPublic)));
			}
			else
			{
				BuildingTweaksPerfFeature.Log?.LogWarning(
					"[BuildingTweaksPerf] Player_Update_Patch.Postfix not found");
			}

			if (allowedPostfix != null)
			{
				harmony.Patch(allowedPostfix, transpiler: new HarmonyMethod(
					typeof(Patches).GetMethod(
						nameof(WhitelistTranspiler),
						BindingFlags.Static | BindingFlags.NonPublic)));
			}
			else
			{
				BuildingTweaksPerfFeature.Log?.LogWarning(
					"[BuildingTweaksPerf] Builder_UpdateAllowed_Patch.Postfix not found");
			}
		}

		private static IEnumerable<CodeInstruction> PlayerPostfixTranspiler(
			IEnumerable<CodeInstruction> instructions)
		{
			var output = new List<CodeInstruction>();
			var input = new List<CodeInstruction>(instructions);
			bool ghostReplaced = false;

			int start = -1;
			for (int i = 0; i < input.Count - 1; i++)
			{
				if (input[i].opcode == OpCodes.Ldc_I4_0 &&
					input[i + 1].opcode == OpCodes.Newarr &&
					input[i + 1].operand != null &&
					input[i + 1].operand.ToString().Contains("Collider"))
				{
					start = i;
					break;
				}
			}

			if (start < 0)
			{
				BuildingTweaksPerfFeature.Log?.LogWarning(
					"[BuildingTweaksPerf] WaterPark Collider[0] pattern not found");
				return input;
			}

			int end = -1;
			for (int i = start + 2; i < input.Count; i++)
			{
				string methodName = input[i].operand != null
					? input[i].operand.ToString()
					: string.Empty;
				if (methodName.Contains("FirstOrDefault") &&
					methodName.Contains("UseableDiveHatch"))
				{
					end = i + 1;
					break;
				}
			}

			if (end < 0 || input[end].opcode != OpCodes.Stloc_S)
			{
				BuildingTweaksPerfFeature.Log?.LogWarning(
					"[BuildingTweaksPerf] WaterPark LINQ end pattern not found");
				return input;
			}

			for (int i = 0; i < input.Count; i++)
			{
				if (i == start)
				{
					output.Add(new CodeInstruction(OpCodes.Call,
						AccessTools.Method(typeof(ParkBuffer), "FindHatch"))
					{
						labels = input[i].labels,
						blocks = input[i].blocks
					});
					continue;
				}

				if (i == end)
				{
					output.Add(input[i]);
					continue;
				}

				if (i > start && i < end)
				{
					continue;
				}

				string callName = input[i].operand != null
					? input[i].operand.ToString()
					: string.Empty;
				if (callName.Contains("GetComponents") &&
					callName.Contains("IBuilderGhostModel"))
				{
					output.Add(new CodeInstruction(OpCodes.Call,
						AccessTools.Method(typeof(GhostCache), "Get"))
					{
						labels = input[i].labels,
						blocks = input[i].blocks
					});
					ghostReplaced = true;
					continue;
				}

				output.Add(input[i]);
			}

			BuildingTweaksPerfFeature.Log?.LogInfo(
				"[BuildingTweaksPerf] player transpiler applied: waterpark, ghost=" +
				ghostReplaced);
			return output;
		}

		private static IEnumerable<CodeInstruction> WhitelistTranspiler(
			IEnumerable<CodeInstruction> instructions)
		{
			var output = new List<CodeInstruction>();
			var input = new List<CodeInstruction>(instructions);
			bool skipping = false;
			bool replaced = false;

			for (int i = 0; i < input.Count; i++)
			{
				CodeInstruction instruction = input[i];

				if (!skipping && IsWhitelistStart(input, i))
				{
					output.Add(new CodeInstruction(OpCodes.Call,
						AccessTools.Method(typeof(Whitelist), "Get"))
					{
						labels = instruction.labels,
						blocks = instruction.blocks
					});
					skipping = true;
					replaced = true;
					continue;
				}

				if (skipping)
				{
					if (instruction.opcode == OpCodes.Stloc_0)
					{
						output.Add(instruction);
						skipping = false;
					}
					continue;
				}

				output.Add(instruction);
			}

			if (!replaced)
			{
				BuildingTweaksPerfFeature.Log?.LogWarning(
					"[BuildingTweaksPerf] whitelist pattern not found");
			}
			else
			{
				BuildingTweaksPerfFeature.Log?.LogInfo(
					"[BuildingTweaksPerf] whitelist transpiler applied");
			}

			return output;
		}

		private static bool IsWhitelistStart(List<CodeInstruction> input, int index)
		{
			if (index + 3 >= input.Count)
			{
				return false;
			}
			if (input[index].opcode != OpCodes.Newobj ||
				input[index + 1].opcode != OpCodes.Dup ||
				input[index + 2].opcode != OpCodes.Ldc_I4 ||
				input[index + 3].opcode != OpCodes.Callvirt)
			{
				return false;
			}

			string add = input[index + 3].operand != null
				? input[index + 3].operand.ToString()
				: string.Empty;
			return add.Contains("Add") && add.Contains("TechType");
		}
	}

	internal static class ParkBuffer
	{
		private static readonly Collider[] Buffer = new Collider[32];

		internal static UseableDiveHatch? FindHatch()
		{
			if (Player.main == null)
			{
				return null;
			}

			int count = Physics.OverlapSphereNonAlloc(
				Player.main.transform.position,
				3f,
				Buffer,
				1,
				QueryTriggerInteraction.Ignore);

			for (int i = 0; i < count; i++)
			{
				Collider collider = Buffer[i];
				if (collider == null)
				{
					continue;
				}

				UseableDiveHatch hatch = collider.GetComponentInParent<UseableDiveHatch>();
				if (hatch != null)
				{
					return hatch;
				}
			}

			return null;
		}
	}

	internal static class GhostCache
	{
		private static GameObject? _model;
		private static IBuilderGhostModel[] _components =
			Array.Empty<IBuilderGhostModel>();

		internal static IBuilderGhostModel[] Get(GameObject model)
		{
			if (model == null)
			{
				return Array.Empty<IBuilderGhostModel>();
			}

			if (model != _model)
			{
				_model = model;
				_components = model.GetComponents<IBuilderGhostModel>();
			}

			return _components;
		}
	}

	internal static class Whitelist
	{
		private static readonly List<TechType> Values = new List<TechType>
		{
			(TechType)5501, (TechType)5505, (TechType)5514, (TechType)5504, (TechType)5524,
			(TechType)5515, (TechType)5528, (TechType)5529, (TechType)5525, (TechType)5522
		};

		internal static List<TechType> Get()
		{
			return Values;
		}
	}
}
