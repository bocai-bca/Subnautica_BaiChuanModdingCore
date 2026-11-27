using System;
using System.Collections;
using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Logging;
using Story;
using UnityEngine;

namespace BaiChuanModdingCore.Features.AuroraSecondExplosion
{
	/// <summary>
	/// 独立功能：极光号第一次爆炸后，如果反应堆没有及时修复，
	/// 经过配置的延迟时间触发第二次爆炸并扩大辐射。
	/// 原版是每帧 Update + 1 秒节流；这里改为每秒一次协程检查。
	/// </summary>
	internal static class AuroraSecondExplosionFeature
	{
		private const float ExplosionDamage = 2000f;
		private const float ExplosionRadius = 500f;
		private const DamageType ExplosionType = DamageType.Explosive;
		private const float ExplodedOffset = 27f;
		private const BindingFlags PrivateInstance =
			BindingFlags.Instance | BindingFlags.NonPublic;

		private static MethodInfo? _playExplosionFX;
		private static MethodInfo? _shakePlayerCamera;
		private static MethodInfo? _createExplosiveForce;
		private static FieldInfo? _leaksField;

		private static float _delaySeconds = 2700f;
		private static float _radiationMultiplier = 1.5f;
		private static bool _decided;
		private static bool _armed;
		private static bool _fired;
		private static bool _hasOriginalMax;
		private static float _originalMaxRadius;

		internal static ManualLogSource? Log;

		internal static void Initialize(ConfigFile config, ManualLogSource log, MonoBehaviour host)
		{
			Log = log;

			int minutes = config.Bind(
				"AuroraSecondExplosion.General",
				"DelayMinutes",
				45,
				"极光号爆炸后经过多少分钟触发第二次爆炸。").Value;
			if (minutes < 1)
			{
				minutes = 1;
			}
			_delaySeconds = minutes * 60f;

			_radiationMultiplier = config.Bind(
				"AuroraSecondExplosion.Radiation",
				"RadiusMultiplier",
				1.5f,
				"第二次爆炸后辐射最大半径的倍数（1.0 = 不变）。").Value;
			if (_radiationMultiplier < 1f)
			{
				_radiationMultiplier = 1f;
			}

			Type type = typeof(CrashedShipExploder);
			_playExplosionFX = type.GetMethod("PlayExplosionFX", PrivateInstance);
			_shakePlayerCamera = type.GetMethod("ShakePlayerCamera", PrivateInstance);
			_createExplosiveForce = type.GetMethod("CreateExplosiveForce", PrivateInstance);
			_leaksField = typeof(LeakingRadiation).GetField("leaks", PrivateInstance);

			if (_playExplosionFX == null ||
				_shakePlayerCamera == null ||
				_createExplosiveForce == null)
			{
				Log?.LogError("[AuroraSecondExplosion] 反射失败：未找到 CrashedShipExploder 的私有爆炸方法，插件不会生效。");
				return;
			}

			host.StartCoroutine(CheckLoop());
			Log?.LogInfo("[AuroraSecondExplosion] loaded, delay=" + minutes +
				"min, radiationMultiplier=" + _radiationMultiplier);
		}

		private static IEnumerator CheckLoop()
		{
			while (!_fired)
			{
				yield return new WaitForSeconds(1f);
				try
				{
					Check();
				}
				catch (Exception ex)
				{
					Log?.LogWarning("[AuroraSecondExplosion] check: " + ex.Message);
				}
			}
		}

		private static void Check()
		{
			if (_fired || _playExplosionFX == null)
			{
				return;
			}

			CrashedShipExploder? main = CrashedShipExploder.main;
			if (main == null || DayNightCycle.main == null ||
				main.timeToStartCountdown < 100f)
			{
				return;
			}

			float elapsed = DayNightCycle.main.timePassedAsFloat -
				(main.timeToStartCountdown + ExplodedOffset);
			if (elapsed < 0f)
			{
				return;
			}

			if (!_decided)
			{
				Decide(main, elapsed);
			}
			else if (_armed && elapsed >= _delaySeconds)
			{
				_fired = true;
				TriggerSecondExplosion(main);
			}
		}

		private static void Decide(CrashedShipExploder exploder, float elapsed)
		{
			_decided = true;
			if (elapsed >= _delaySeconds)
			{
				_armed = false;
				Log?.LogInfo("[AuroraSecondExplosion] already " +
					(int)elapsed + "s since explosion; skip this save.");
				return;
			}

			if (IsReactorRepaired())
			{
				_armed = false;
				Log?.LogInfo("[AuroraSecondExplosion] reactor repaired; cancel.");
				ErrorMessage.AddMessage("极光号反应堆已在安全时间内修复，不会再发生爆炸。");
				return;
			}

			_armed = true;
			Log?.LogInfo("[AuroraSecondExplosion] armed; trigger in " +
				(int)(_delaySeconds - elapsed) + "s.");
			ErrorMessage.AddMessage("极光号爆炸了！请尽快前往动力室抢修，否则将发生第二次爆炸。");
		}

		private static bool IsReactorRepaired()
		{
			LeakingRadiation? main = LeakingRadiation.main;
			if (main == null)
			{
				return false;
			}
			if (main.radiationFixed)
			{
				return true;
			}
			if (main.leaksFixedGoal != null && StoryGoalManager.main != null)
			{
				string key = main.leaksFixedGoal.key;
				if (!string.IsNullOrEmpty(key) &&
					StoryGoalManager.main.IsGoalComplete(key))
				{
					return true;
				}
			}
			return false;
		}

		private static void TriggerSecondExplosion(CrashedShipExploder exploder)
		{
			try
			{
				if (exploder.shipExplodeSound != null)
				{
					exploder.shipExplodeSound.StartEvent();
				}

				_playExplosionFX?.Invoke(exploder, null);
				_shakePlayerCamera?.Invoke(exploder, null);
				_createExplosiveForce?.Invoke(exploder, null);

				DamageSystem.RadiusDamage(
					ExplosionDamage,
					exploder.transform.position,
					ExplosionRadius,
					ExplosionType,
					exploder.gameObject);

				ExpandRadiation();
				ErrorMessage.AddMessage("极光号发生了第二次爆炸！辐射范围大幅扩大，请立即抢修！");
				Log?.LogInfo("[AuroraSecondExplosion] second explosion triggered.");
			}
			catch (Exception ex)
			{
				Log?.LogError("[AuroraSecondExplosion] trigger failed: " + ex);
			}
		}

		private static void ExpandRadiation()
		{
			LeakingRadiation? main = LeakingRadiation.main;
			if (main == null)
			{
				Log?.LogWarning("[AuroraSecondExplosion] LeakingRadiation missing; skip radiation expand.");
				return;
			}

			if (!_hasOriginalMax)
			{
				_hasOriginalMax = true;
				_originalMaxRadius = main.kMaxRadius;
			}

			int reopened = 0;
			if (_leaksField != null &&
				_leaksField.GetValue(main) is IList leaks)
			{
				for (int i = 0; i < leaks.Count; i++)
				{
					RadiationLeak? leak = leaks[i] as RadiationLeak;
					if (leak == null)
					{
						continue;
					}

					LiveMixin? live = leak.GetComponent<LiveMixin>();
					if (live != null && live.GetHealthFraction() >= 1f)
					{
						live.health = live.maxHealth * 0.01f;
						reopened++;
					}
				}
			}

			main.kMaxRadius = _originalMaxRadius * _radiationMultiplier;
			main.currentRadius = _originalMaxRadius * _radiationMultiplier;
			main.radiationFixed = false;
			Log?.LogInfo("[AuroraSecondExplosion] radiation expanded: reopened=" +
				reopened + " max=" + _originalMaxRadius + " -> " + main.kMaxRadius);
		}
	}
}
