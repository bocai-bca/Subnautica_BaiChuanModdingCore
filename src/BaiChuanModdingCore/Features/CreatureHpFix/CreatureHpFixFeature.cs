using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;
using BepInEx.Logging;
using UnityEngine;

namespace BaiChuanModdingCore.Features.CreatureHpFix
{
	/// <summary>
	/// 独立功能：修复旧存档里“maxHealth 已被补丁改大，但 health 仍是旧值”的生物。
	/// 只处理下列 5 种生物；Warper 已按要求不再修正。
	/// </summary>
	internal static class CreatureHpFixFeature
	{
		private static readonly ConditionalWeakTable<LiveMixin, object> Fixed =
			new ConditionalWeakTable<LiveMixin, object>();
		private static readonly string[] Watched =
		{
			"GhostLeviathan",
			"ReaperLeviathan",
			"SeaDragon",
			"Reefback",
			"SeaEmperorJuvenile",
		};

		internal static ManualLogSource? Log;
		internal static MonoBehaviour? Host;

		internal static void Initialize(ManualLogSource log, MonoBehaviour host)
		{
			Log = log;
			Host = host;
			Log?.LogInfo("[CreatureHpFix] ready (Warper excluded)");
		}

		internal static bool ShouldWatch(string typeName)
		{
			for (int i = 0; i < Watched.Length; i++)
			{
				if (Watched[i] == typeName)
				{
					return true;
				}
			}
			return false;
		}

		internal static void TryFix(LiveMixin liveMixin, Creature creature, string where)
		{
			if (liveMixin == null || creature == null)
			{
				return;
			}
			if (!ShouldWatch(creature.GetType().Name))
			{
				return;
			}

			if (Fixed.TryGetValue(liveMixin, out _))
			{
				return;
			}

			float max = liveMixin.maxHealth;
			if (max <= 0f)
			{
				return;
			}

			// 死亡个体不处理，避免读档时把尸体“复活”。
			if (liveMixin.health <= 0.001f)
			{
				Fixed.Add(liveMixin, string.Empty);
				return;
			}

			if (liveMixin.health >= max - 0.5f)
			{
				Fixed.Add(liveMixin, string.Empty);
				return;
			}

			float before = liveMixin.health;
			liveMixin.health = max;
			Fixed.Add(liveMixin, string.Empty);

			var builder = new StringBuilder(160);
			builder.Append("[CreatureHpFix] ").Append(where).Append(' ')
				.Append(creature.GetType().Name)
				.Append(" health ").Append(before.ToString("0"))
				.Append(" -> ").Append(max.ToString("0"));
			Log?.LogInfo(builder.ToString());
		}

		internal static IEnumerator Confirm(LiveMixin liveMixin, Creature creature, string where)
		{
			yield return new WaitForSeconds(2f);
			try
			{
				TryFix(liveMixin, creature, where + "+2s");
			}
			catch (Exception ex)
			{
				Log?.LogWarning("[CreatureHpFix] " + ex.Message);
			}
		}
	}
}
