using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BaiChuanModdingCore.Features.BlueprintTrackerPerf
{
	/// <summary>
	/// 独立功能：BlueprintTracker 性能补丁。
	/// 减少 Entry/Pin 的重复 Update、缓存 QuickSlots 查找，并把图标文本刷新集中到 1 秒一次。
	/// </summary>
	internal static class BlueprintTrackerPerfFeature
	{
		internal static ManualLogSource? Log;

		internal static void Initialize(Harmony harmony, ManualLogSource log)
		{
			Log = log;
			Patches.Install(harmony);
			IconUpdateManager.Create();
		}
	}

	internal static class Patches
	{
		private static readonly ConditionalWeakTable<object, EntryState> EntryStates =
			new ConditionalWeakTable<object, EntryState>();
		private static readonly ConditionalWeakTable<object, PinState> PinStates =
			new ConditionalWeakTable<object, PinState>();

		private static Func<object, bool>? _hover;
		private static Func<object, bool>? _down;
		private static Func<object, int>? _mode;
		private static bool _fieldsWarned;

		private static bool _hasShowState;
		private static bool _lastShow;

		internal static Type? IconType { get; private set; }

		internal static void Install(Harmony harmony)
		{
			Type? tracker = AccessTools.TypeByName("BlueprintTracker.BlueprintTracker");
			Type? entry = AccessTools.TypeByName("BlueprintTracker.BlueprintTrackerEntry");
			Type? pin = AccessTools.TypeByName("BlueprintTracker.PinButton");
			IconType = AccessTools.TypeByName("BlueprintTracker.BlueprintTrackerIcon");
			Type? remove = AccessTools.TypeByName("BlueprintTracker.BlueprintTrackerRemoveButton");

			if (tracker == null || entry == null || pin == null || IconType == null)
			{
				BlueprintTrackerPerfFeature.Log?.LogError(
					"[BlueprintTrackerPerf] BlueprintTracker types not found");
				return;
			}

			Patch(harmony, tracker, "ShowTrackers", nameof(ShowTrackersPrefix), null);
			Patch(harmony, tracker, "AddTracker", null, nameof(InvalidateShowState));
			Patch(harmony, tracker, "RemoveTracker", null, nameof(InvalidateShowState));
			Patch(harmony, entry, "Update", nameof(EntryUpdatePrefix), null);
			Patch(harmony, pin, "Update", nameof(PinUpdatePrefix), null);

			Patch(harmony, IconType, "OnEnable",
				nameof(IconEnablePrefix), nameof(IconEnablePostfix));
			Patch(harmony, IconType, "Init", null, nameof(IconInitPostfix));

			ResolvePinFields(pin);

			if (remove != null)
			{
				PatchTranspiler(harmony, remove, "Init", nameof(ReplaceQuickSlotsFind));
			}
			PatchTranspiler(harmony, IconType, "Init", nameof(ReplaceQuickSlotsFind));
			BlueprintTrackerPerfFeature.Log?.LogInfo("[BlueprintTrackerPerf] installed");
		}

		private static void Patch(Harmony harmony, Type type, string method,
			string? prefix, string? postfix)
		{
			MethodInfo? original = AccessTools.Method(type, method);
			if (original == null)
			{
				BlueprintTrackerPerfFeature.Log?.LogWarning(
					"[BlueprintTrackerPerf] missing method: " + type.Name + "." + method);
				return;
			}

			HarmonyMethod? pre = prefix == null ? null : new HarmonyMethod(
				typeof(Patches).GetMethod(prefix,
					BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
			HarmonyMethod? post = postfix == null ? null : new HarmonyMethod(
				typeof(Patches).GetMethod(postfix,
					BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));

			harmony.Patch(original, pre, post);
		}

		private static void PatchTranspiler(Harmony harmony, Type type, string method,
			string transpiler)
		{
			MethodInfo? original = AccessTools.Method(type, method);
			if (original == null)
			{
				BlueprintTrackerPerfFeature.Log?.LogWarning(
					"[BlueprintTrackerPerf] missing transpiler target: " + type.Name + "." + method);
				return;
			}

			harmony.Patch(original, transpiler: new HarmonyMethod(
				typeof(Patches).GetMethod(transpiler,
					BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)));
		}

		private static void ResolvePinFields(Type pin)
		{
			FieldInfo? hoverField = AccessTools.Field(pin, "hover");
			FieldInfo? downField = AccessTools.Field(pin, "down");
			FieldInfo? modeField = AccessTools.Field(pin, "mode");

			if (hoverField == null || downField == null || modeField == null)
			{
				BlueprintTrackerPerfFeature.Log?.LogWarning(
					"[BlueprintTrackerPerf] PinButton fields missing; pin optimization disabled");
				return;
			}

			try
			{
				_hover = BuildBoolGetter(hoverField);
				_down = BuildBoolGetter(downField);
				_mode = BuildIntGetter(modeField);
			}
			catch (Exception ex)
			{
				BlueprintTrackerPerfFeature.Log?.LogWarning(
					"[BlueprintTrackerPerf] PinButton delegate build failed: " + ex.Message);
			}
		}

		private static Func<object, bool> BuildBoolGetter(FieldInfo field)
		{
			ParameterExpression instance = Expression.Parameter(typeof(object), "instance");
			Expression access = Expression.Field(
				Expression.Convert(instance, field.DeclaringType!), field);
			return Expression.Lambda<Func<object, bool>>(
				Expression.Convert(access, typeof(bool)), instance).Compile();
		}

		private static Func<object, int> BuildIntGetter(FieldInfo field)
		{
			ParameterExpression instance = Expression.Parameter(typeof(object), "instance");
			Expression access = Expression.Field(
				Expression.Convert(instance, field.DeclaringType!), field);
			return Expression.Lambda<Func<object, int>>(
				Expression.Convert(access, typeof(int)), instance).Compile();
		}

		private static bool ShowTrackersPrefix(bool show)
		{
			if (_hasShowState && _lastShow == show)
			{
				return false;
			}

			_hasShowState = true;
			_lastShow = show;
			return true;
		}

		private static void InvalidateShowState()
		{
			_hasShowState = false;
		}

		private static bool EntryUpdatePrefix(object __instance)
		{
			if (Player.main == null)
			{
				return false;
			}

			PDA pda = Player.main.GetPDA();
			bool inUse = pda != null && pda.isInUse;
			bool journal = inUse && pda.ui != null && pda.ui.currentTabType == PDATab.Journal;

			if (inUse && pda.ui == null)
			{
				return false;
			}

			int signature = (pda == null ? 0 : 1) |
							(inUse ? 2 : 0) |
							(journal ? 4 : 0);

			EntryState state = EntryStates.GetValue(__instance, _ => new EntryState());
			if (state.Initialized && state.Signature == signature)
			{
				return false;
			}

			state.Initialized = true;
			state.Signature = signature;
			return true;
		}

		private static bool PinUpdatePrefix(object __instance)
		{
			if (_hover == null || _down == null || _mode == null)
			{
				if (!_fieldsWarned)
				{
					_fieldsWarned = true;
					BlueprintTrackerPerfFeature.Log?.LogWarning(
						"[BlueprintTrackerPerf] PinButton optimization inactive");
				}
				return true;
			}

			bool hover = _hover(__instance);
			bool down = _down(__instance);
			int mode = _mode(__instance);

			PinState state = PinStates.GetValue(__instance, _ => new PinState());
			if (state.Initialized && state.Hover == hover && state.Down == down &&
				state.Mode == mode)
			{
				return false;
			}

			state.Initialized = true;
			state.Hover = hover;
			state.Down = down;
			state.Mode = mode;
			return true;
		}

		private static bool IconEnablePrefix()
		{
			// 原版每个图标自带 UpdateRoutine，统一交给 IconUpdateManager。
			return false;
		}

		private static void IconEnablePostfix(object __instance)
		{
			IconUpdateManager.Register(__instance);
		}

		private static void IconInitPostfix(object __instance)
		{
			IconUpdateManager.Register(__instance);
		}

		private static IEnumerable<CodeInstruction> ReplaceQuickSlotsFind(
			IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo replacement = AccessTools.Method(typeof(QuickSlotsCache), "Get")!;
			foreach (CodeInstruction instruction in instructions)
			{
				MethodInfo? call = instruction.operand as MethodInfo;
				if (call != null &&
					call.Name == "FindObjectOfType" &&
					call.IsGenericMethod &&
					call.GetGenericArguments()[0] == typeof(uGUI_QuickSlots))
				{
					yield return new CodeInstruction(OpCodes.Call, replacement)
					{
						labels = instruction.labels,
						blocks = instruction.blocks
					};
					continue;
				}

				yield return instruction;
			}
		}

		private sealed class EntryState
		{
			internal bool Initialized;
			internal int Signature;
		}

		private sealed class PinState
		{
			internal bool Initialized;
			internal bool Hover;
			internal bool Down;
			internal int Mode;
		}
	}

	internal static class QuickSlotsCache
	{
		private static uGUI_QuickSlots? _quickSlots;

		internal static uGUI_QuickSlots? Get()
		{
			if (_quickSlots == null)
			{
				_quickSlots = UnityEngine.Object.FindObjectOfType<uGUI_QuickSlots>();
			}
			return _quickSlots;
		}
	}

	internal sealed class IconUpdateManager : MonoBehaviour
	{
		private static readonly HashSet<object> Icons = new HashSet<object>();
		private static IconUpdateManager? _instance;
		private static MethodInfo? _updateText;

		internal static void Create()
		{
			if (_instance != null || Patches.IconType == null)
			{
				return;
			}

			GameObject go = new GameObject("BlueprintTrackerPerf_IconUpdate");
			UnityEngine.Object.DontDestroyOnLoad(go);
			_instance = go.AddComponent<IconUpdateManager>();
			_updateText = AccessTools.Method(Patches.IconType, "UpdateText");
			if (_updateText == null)
			{
				BlueprintTrackerPerfFeature.Log?.LogWarning(
					"[BlueprintTrackerPerf] BlueprintTrackerIcon.UpdateText not found");
			}
		}

		internal static void Register(object icon)
		{
			if (icon != null)
			{
				Icons.Add(icon);
			}
		}

		private IEnumerator Start()
		{
			var wait = new WaitForSeconds(1f);
			while (true)
			{
				yield return wait;
				UpdateIcons();
			}
		}

		private static void UpdateIcons()
		{
			if (_updateText == null || Icons.Count == 0)
			{
				return;
			}

			var stale = new List<object>();
			foreach (object icon in Icons)
			{
				Behaviour? behaviour = icon as Behaviour;
				if (behaviour == null || !behaviour.isActiveAndEnabled)
				{
					if (behaviour == null)
					{
						stale.Add(icon);
					}
					continue;
				}

				try
				{
					_updateText.Invoke(icon, null);
				}
				catch (Exception ex)
				{
					BlueprintTrackerPerfFeature.Log?.LogWarning(
						"[BlueprintTrackerPerf] UpdateText failed: " + ex.Message);
					stale.Add(icon);
				}
			}

			foreach (object icon in stale)
			{
				Icons.Remove(icon);
			}
		}
	}
}
