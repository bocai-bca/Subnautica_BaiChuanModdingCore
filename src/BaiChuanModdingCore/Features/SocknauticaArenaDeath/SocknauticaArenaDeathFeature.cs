using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BaiChuanModdingCore.Features.SocknauticaShared;
using BepInEx.Logging;
using HarmonyLib;
using UWE;
using UnityEngine;
using UnityEngine.UI;

namespace BaiChuanModdingCore.Features.SocknauticaArenaDeath
{
	/// <summary>
	/// 玩家在 Socknautica 竞技场/Boss 战中死亡时：
	///   1. 阻止原版 EndCreditsSceneCleaner 制作组名单
	///   2. 黑屏 10 秒；第 1 秒显示“葬身4546B”，第 9 秒消失
	///   3. 第 10 秒返回主菜单
	///   4. 在主菜单对应存档格以红色显示“葬身4546B”
	/// </summary>
	internal static class SocknauticaArenaDeathFeature
	{
		private const string DeathText = "葬身4546B";
		private const string MarkerFileName = "BaiChuan_4546B.json";

		internal static ManualLogSource? Log;
		private static bool _deathHandled;
		private static FieldInfo? _arenaMainField;
		private static readonly List<Text> ColorKeep = new List<Text>();
		private static readonly HashSet<string> LoggedMarkedSlots = new HashSet<string>();
		private static bool _colorKeepRunning;

		[Serializable]
		private sealed class DeathMarker
		{
			public int version = 1;
			public bool dead = true;
			public string timestamp = "";
		}

		internal static void Initialize(Harmony harmony, ManualLogSource log)
		{
			Log = log;
			PatchArenaDeath(harmony);
			PatchMainMenuSlot(harmony);
			PatchMainMenuColorKeep(harmony);
			PatchBeatGameCredits(harmony);
			PatchBossVictory(harmony);
		}

		private static void PatchArenaDeath(Harmony harmony)
		{
			try
			{
				Type? arenaType = AccessTools.TypeByName("Socknautica.Mono.ArenaSpawner");
				_arenaMainField = arenaType == null
					? null
					: AccessTools.Field(arenaType, "main");

				MethodInfo? onKill = AccessTools.Method(
					typeof(Player),
					"OnKill",
					new[] { typeof(DamageType) });
				if (onKill == null)
				{
					Log?.LogWarning("[SocknauticaArenaDeath] Player.OnKill not found");
					return;
				}

				harmony.Patch(onKill, prefix: new HarmonyMethod(
					typeof(SocknauticaArenaDeathFeature).GetMethod(
						nameof(OnKillPrefix),
						BindingFlags.Static | BindingFlags.NonPublic)));
				PatchSocknauticaCreditPostfix(harmony);
				Log?.LogInfo("[SocknauticaArenaDeath] arena death hook installed");
			}
			catch (Exception ex)
			{
				Log?.LogWarning("[SocknauticaArenaDeath] death hook failed: " + ex.Message);
			}
		}

		private static void PatchSocknauticaCreditPostfix(Harmony harmony)
		{
			try
			{
				Type? patches = AccessTools.TypeByName("Socknautica.Patches.PlayerPatches");
				MethodInfo? onKill = patches == null
					? null
					: AccessTools.Method(patches, "PlayerOnKill");
				if (onKill == null)
				{
					Log?.LogWarning("[SocknauticaArenaDeath] Socknautica PlayerOnKill not found");
					return;
				}

				harmony.Patch(onKill, prefix: new HarmonyMethod(
					typeof(SocknauticaArenaDeathFeature).GetMethod(
						nameof(BlockSocknauticaDeathCredits),
						BindingFlags.Static | BindingFlags.NonPublic)));
				Log?.LogInfo("[SocknauticaArenaDeath] Socknautica death-credit hook installed");
			}
			catch (Exception ex)
			{
				Log?.LogWarning("[SocknauticaArenaDeath] Socknautica credit hook failed: " +
					ex.Message);
			}
		}

		private static bool BlockSocknauticaDeathCredits()
		{
			if (!_deathHandled)
			{
				return true;
			}

			Log?.LogInfo("[SocknauticaArenaDeath] blocked Socknautica KillCoroutine credits");
			return false;
		}

		[HarmonyPriority(Priority.First)]
		private static bool OnKillPrefix()
		{
			if (!IsArenaActive())
			{
				return true;
			}

			// 死亡流程进行中，重复的 OnKill 也不能再触发原版死亡界面/复活。
			if (_deathHandled)
			{
				return false;
			}

			_deathHandled = true;
			Log?.LogInfo("[SocknauticaArenaDeath] arena death intercepted; skipping vanilla death screen/respawn");
			CoroutineHost.StartCoroutine(DeathRoutine());
			return false;
		}

		private static bool IsArenaActive()
		{
			try
			{
				return _arenaMainField?.GetValue(null) != null;
			}
			catch
			{
				return false;
			}
		}

		private static IEnumerator DeathRoutine()
		{
			// 黑屏 10 秒：
			//   0s 黑屏
			//   1s 显示警告
			//   9s 警告消失
			//  10s 回主菜单
			CreateBlackOverlayBeforeWarning();
			WriteDeathMarker();

			yield return new WaitForSeconds(1f);
			ShowDeathWarning(8f);
			yield return new WaitForSeconds(8f);
			yield return new WaitForSeconds(1f);

			SceneCleaner.Open();
		}

		private static Component? ShowDeathWarning(float duration)
		{
			try
			{
				Type? warningType = AccessTools.TypeByName("Socknautica.Mono.WarningUI");
				MethodInfo? show = warningType?.GetMethod(
					"Show",
					BindingFlags.Static | BindingFlags.Public,
					null,
					new[] { typeof(string), typeof(Sprite), typeof(float) },
					null);
				if (show == null)
				{
					Log?.LogWarning("[SocknauticaArenaDeath] WarningUI.Show not found");
					ErrorMessage.AddMessage(DeathText);
					return null;
				}

				object? result = show.Invoke(
					null,
					new object?[]
					{
						DeathText,
						SocknauticaSharedAssets.GetGenericWarningSprite(
							Log, "SocknauticaArenaDeath"),
						duration
					});
				Log?.LogInfo("[SocknauticaArenaDeath] warning shown");
				return result as Component;
			}
			catch (Exception ex)
			{
				Log?.LogWarning("[SocknauticaArenaDeath] warning failed: " + ex.Message);
				ErrorMessage.AddMessage(DeathText);
				return null;
			}
		}

		private static void CreateBlackOverlayBeforeWarning()
		{
			try
			{
				Transform? parent = null;
				Type? warningType = AccessTools.TypeByName("Socknautica.Mono.WarningUI");
				FieldInfo? prefabField = warningType?.GetField(
					"prefabObject",
					BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				if (prefabField?.GetValue(null) is GameObject prefab && prefab != null)
				{
					parent = prefab.transform.parent;
				}

				if (parent != null)
				{
					CreateBlackImage(parent);
					Log?.LogInfo("[SocknauticaArenaDeath] black before warning: parent=" +
						parent.name);
					return;
				}

				GameObject root = new GameObject("BaiChuan_4546B_Black");
				Canvas fallback = root.AddComponent<Canvas>();
				fallback.renderMode = RenderMode.ScreenSpaceOverlay;
				fallback.sortingOrder = -1000;
				CreateBlackImage(root.transform);
				Log?.LogWarning("[SocknauticaArenaDeath] warning parent unavailable; fallback black canvas");
			}
			catch (Exception ex)
			{
				Log?.LogWarning("[SocknauticaArenaDeath] black overlay failed: " + ex.Message);
			}
		}

		private static void CreateBlackImage(Transform parent)
		{
			GameObject imageObject = new GameObject("Black");
			imageObject.transform.SetParent(parent, false);
			imageObject.transform.SetAsFirstSibling();

			Image image = imageObject.AddComponent<Image>();
			image.color = Color.black;
			image.raycastTarget = false;

			RectTransform rect = image.rectTransform;
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.offsetMin = Vector2.zero;
			rect.offsetMax = Vector2.zero;
		}

		private static void PatchBeatGameCredits(Harmony harmony)
		{
			try
			{
				Type? type = AccessTools.TypeByName("Socknautica.Mono.Alien.BeatGameOnTouch");
				MethodInfo? touch = type == null
					? null
					: AccessTools.Method(type, "Touch");
				if (touch == null)
				{
					Log?.LogWarning("[SocknauticaArenaDeath] BeatGameOnTouch.Touch not found");
					return;
				}

				harmony.Patch(touch, prefix: new HarmonyMethod(
					typeof(SocknauticaArenaDeathFeature).GetMethod(
						nameof(BlockBeatGameCredits),
						BindingFlags.Static | BindingFlags.NonPublic)));
				Log?.LogInfo("[SocknauticaArenaDeath] BeatGame credits hook installed");
			}
			catch (Exception ex)
			{
				Log?.LogWarning("[SocknauticaArenaDeath] BeatGame credit hook failed: " + ex.Message);
			}
		}

		private static bool BlockBeatGameCredits()
		{
			if (!_deathHandled)
			{
				return true;
			}

			Log?.LogInfo("[SocknauticaArenaDeath] blocked BeatGameOnTouch credits");
			return false;
		}

		private static void PatchBossVictory(Harmony harmony)
		{
			try
			{
				Type? type = AccessTools.TypeByName("Socknautica.SaveWithoutSaving");
				MethodInfo? method = type == null
					? null
					: AccessTools.Method(type, "OnDefeatBoss");
				if (method == null)
				{
					Log?.LogWarning("[SocknauticaArenaDeath] SaveWithoutSaving.OnDefeatBoss not found");
					return;
				}

				harmony.Patch(method, postfix: new HarmonyMethod(
					typeof(SocknauticaArenaDeathFeature).GetMethod(
						nameof(OnDefeatBossPostfix),
						BindingFlags.Static | BindingFlags.NonPublic)));
				Log?.LogInfo("[SocknauticaArenaDeath] boss victory hook installed");
			}
			catch (Exception ex)
			{
				Log?.LogWarning("[SocknauticaArenaDeath] boss victory hook failed: " + ex.Message);
			}
		}

		private static void OnDefeatBossPostfix()
		{
			string? slot = CurrentSlot();
			if (string.IsNullOrEmpty(slot))
			{
				return;
			}

			ClearDeathMarker(slot);
			Log?.LogInfo("[SocknauticaArenaDeath] boss victory cleared death marker: " + slot);
		}

		private static void ClearDeathMarker(string slot)
		{
			try
			{
				File.Delete(MarkerPath(slot));
			}
			catch (Exception ex)
			{
				Log?.LogWarning("[SocknauticaArenaDeath] clear marker failed: " + ex.Message);
			}
		}

		private static void PatchMainMenuSlot(Harmony harmony)
		{
			try
			{
				HarmonyMethod postfix = new HarmonyMethod(
					typeof(SocknauticaArenaDeathFeature).GetMethod(
						nameof(SlotPostfix),
						BindingFlags.Static | BindingFlags.NonPublic));

				harmony.Patch(
					AccessTools.Method(typeof(MainMenuLoadButton), "Start"),
					postfix: postfix);
				harmony.Patch(
					AccessTools.Method(typeof(MainMenuLoadButton), "onCursorEnter"),
					postfix: postfix);
				harmony.Patch(
					AccessTools.Method(typeof(MainMenuLoadButton), "onCursorLeave"),
					postfix: postfix);
				harmony.Patch(
					AccessTools.Method(
						typeof(MainMenuLoadMenu), "UpdateLoadButtonState"),
					postfix: postfix);

				Log?.LogInfo("[SocknauticaArenaDeath] save slot hook installed");
			}
			catch (Exception ex)
			{
				Log?.LogWarning("[SocknauticaArenaDeath] slot hook failed: " + ex.Message);
			}
		}

		private static void PatchMainMenuColorKeep(Harmony harmony)
		{
			try
			{
				harmony.Patch(
					AccessTools.Method(typeof(MainMenuLoadMenu), "SelectItem"),
					postfix: new HarmonyMethod(
						typeof(SocknauticaArenaDeathFeature).GetMethod(
							nameof(MenuSelectPostfix),
							BindingFlags.Static | BindingFlags.NonPublic)));
				harmony.Patch(
					AccessTools.Method(typeof(MainMenuLoadMenu), "DeselectItem"),
					postfix: new HarmonyMethod(
						typeof(SocknauticaArenaDeathFeature).GetMethod(
							nameof(MenuDeselectPostfix),
							BindingFlags.Static | BindingFlags.NonPublic)));

				Log?.LogInfo("[SocknauticaArenaDeath] menu color hook installed");
			}
			catch (Exception ex)
			{
				Log?.LogWarning("[SocknauticaArenaDeath] menu color hook failed: " + ex.Message);
			}
		}

		private static void MenuSelectPostfix(object item)
		{
			ApplyColorKeep();
			GameObject go = (GameObject)item;
			MainMenuLoadButton? button = go.GetComponent<MainMenuLoadButton>();
			if (button != null)
			{
				SlotPostfix(button);
			}
		}

		private static void MenuDeselectPostfix()
		{
			ApplyColorKeep();
		}

		private static void ApplyColorKeep()
		{
			for (int i = ColorKeep.Count - 1; i >= 0; i--)
			{
				Text text = ColorKeep[i];
				if (text == null)
				{
					ColorKeep.RemoveAt(i);
					continue;
				}

				text.color = Color.red;
			}
		}

		private static Text? FindTarget(MainMenuLoadButton button)
		{
			// 存档格的实际显示文字固定在 load/SaveGameLength。
			if (button.load == null)
			{
				return null;
			}

			Transform length = button.load.transform.Find("SaveGameLength");
			return length == null ? null : length.GetComponent<Text>();
		}

		private static void SlotPostfix(MainMenuLoadButton __instance)
		{
			// 回到主菜单后允许下一次 Boss 战死亡再次触发。
			_deathHandled = false;

			if (__instance == null || string.IsNullOrEmpty(__instance.saveGame))
			{
				return;
			}

			if (!IsMarked(__instance.saveGame))
			{
				return;
			}

			if (LoggedMarkedSlots.Add(__instance.saveGame))
			{
				Log?.LogInfo("[SocknauticaArenaDeath] marked slot in menu: " +
					__instance.saveGame);
			}

			Text? target = FindTarget(__instance);
			if (target == null)
			{
				Log?.LogWarning("[SocknauticaArenaDeath] no target Text for saveGame: " +
					__instance.saveGame);
				return;
			}

			target.text = DeathText;
			target.color = Color.red;

			// MainMenuLoadMenu.SelectItem / DeselectItem 会再次把 Text 刷成黑/白。
			// 参考 SingularityFail 的 ColorKeep，持续把这一格压回红色。
			if (!ColorKeep.Contains(target))
			{
				ColorKeep.Add(target);
			}
			StartColorKeep();
		}

		private static void StartColorKeep()
		{
			if (_colorKeepRunning)
			{
				return;
			}

			_colorKeepRunning = true;
			CoroutineHost.StartCoroutine(ColorKeepLoop());
		}

		private static IEnumerator ColorKeepLoop()
		{
			while (ColorKeep.Count > 0)
			{
				yield return new WaitForSeconds(0.25f);

				for (int i = ColorKeep.Count - 1; i >= 0; i--)
				{
					Text text = ColorKeep[i];
					if (text == null)
					{
						ColorKeep.RemoveAt(i);
						continue;
					}

					Color color = text.color;
					if (color.r < 0.9f || color.g > 0.6f)
					{
						text.color = Color.red;
					}
				}
			}

			_colorKeepRunning = false;
		}

		private static void WriteDeathMarker()
		{
			try
			{
				string? slot = CurrentSlot();
				if (string.IsNullOrEmpty(slot))
				{
					Log?.LogWarning("[SocknauticaArenaDeath] current save slot unknown; marker skipped");
					return;
				}

				if (BossCompleted(slot))
				{
					Log?.LogInfo("[SocknauticaArenaDeath] boss already completed; death marker skipped");
					return;
				}

				string path = MarkerPath(slot);
				if (IsMarked(slot))
				{
					return;
				}

				string? directory = Path.GetDirectoryName(path);
				if (!string.IsNullOrEmpty(directory))
				{
					Directory.CreateDirectory(directory);
				}

				DeathMarker marker = new DeathMarker
				{
					version = 1,
					dead = true,
					timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
				};
				WriteAtomic(path, JsonUtility.ToJson(marker));
				Log?.LogInfo("[SocknauticaArenaDeath] save marked: " + path);
			}
			catch (Exception ex)
			{
				Log?.LogWarning("[SocknauticaArenaDeath] write marker failed: " + ex.Message);
			}
		}

		private static bool IsMarked(string slot)
		{
			try
			{
				if (string.IsNullOrEmpty(slot) || BossCompleted(slot))
				{
					return false;
				}

				string path = MarkerPath(slot);
				if (!File.Exists(path))
				{
					return false;
				}

				string json = File.ReadAllText(path);
				DeathMarker? marker = JsonUtility.FromJson<DeathMarker>(json);
				return marker != null && marker.dead;
			}
			catch
			{
				return false;
			}
		}

		private static void WriteAtomic(string path, string contents)
		{
			string temp = path + ".tmp";
			try
			{
				File.WriteAllText(temp, contents);
				if (File.Exists(path))
				{
					File.Replace(temp, path, null);
				}
				else
				{
					File.Move(temp, path);
				}
			}
			finally
			{
				if (File.Exists(temp))
				{
					try
					{
						File.Delete(temp);
					}
					catch
					{
					}
				}
			}
		}

		private static string? CurrentSlot()
		{
			try
			{
				if (SaveLoadManager.main == null)
				{
					return null;
				}

				Type type = typeof(SaveLoadManager);
				PropertyInfo? property = type.GetProperty(
					"currentSlot",
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (property?.GetValue(SaveLoadManager.main, null) is string propertySlot &&
					!string.IsNullOrEmpty(propertySlot))
				{
					return propertySlot;
				}

				FieldInfo? field = type.GetField(
					"currentSlot",
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (field?.GetValue(SaveLoadManager.main) is string fieldSlot &&
					!string.IsNullOrEmpty(fieldSlot))
				{
					return fieldSlot;
				}
			}
			catch
			{
			}

			return null;
		}
		private static bool BossCompleted(string slot)
		{
			try
			{
				if (string.IsNullOrEmpty(slot))
				{
					return false;
				}

				string path = Path.Combine(
					GameRoot(), "SNAppData", "SavedGames", slot,
					"boss-completion.txt");
				return File.Exists(path);
			}
			catch
			{
				return false;
			}
		}

		private static string GameRoot()
		{
			return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
		}

		private static string MarkerPath(string slot)
		{
			return Path.Combine(
				GameRoot(), "SNAppData", "SavedGames", slot, MarkerFileName);
		}

	}
}




