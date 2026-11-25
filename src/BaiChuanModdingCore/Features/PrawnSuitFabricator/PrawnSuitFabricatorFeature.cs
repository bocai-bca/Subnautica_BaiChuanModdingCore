using System;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BaiChuanModdingCore.Features.PrawnSuitFabricator
{
	/// <summary>
	/// 单独功能：在逃生舱制造机里增加海虾号。
	/// 与 BaiChuanModdingCore 的其他补丁隔离，配置、状态、补丁类都放在本文件夹内。
	/// </summary>
	internal static class PrawnSuitFabricatorFeature
	{
		// 重载整合包的世界加载可能很慢，保留 5 分钟重试窗口；
		// 每秒最多一次，成功或超限后立即停止轮询。
		private const int MaxInjectionAttempts = 300;
		private const float SpawnDistance = 6f;

		private static string _categoryId = "Machines";
		private static bool _active;
		private static bool _injected;
		private static int _attempts;
		private static float _nextAttemptTime;
		private static bool _worldSeen;
		private static string? _lastError;

		internal static ManualLogSource? Log;

		internal static bool Active => _active;

		internal static void Initialize(ConfigFile config, ManualLogSource log)
		{
			Log = log;
			_categoryId = config.Bind(
				"PrawnSuitFabricator",
				"CategoryId",
				"Machines",
				"海虾号在制造机里挂到哪个分类节点。Machines = 可部署物品；其余可选 Resources / Survival / Personal / Tools。"
			).Value;

			if (string.IsNullOrEmpty(_categoryId))
			{
				_categoryId = "Machines";
			}

			_active = true;
			Log?.LogInfo("PrawnSuitFabricator 功能已初始化。");
		}

		internal static void Update()
		{
			if (!_active || _injected || Time.time < _nextAttemptTime)
			{
				return;
			}

			_nextAttemptTime = Time.time + 1f;

			if (LargeWorldStreamer.main == null)
			{
				return;
			}

			if (!_worldSeen)
			{
				_worldSeen = true;
				Log?.LogInfo("PrawnSuitFabricator：世界已加载，开始注入制造机合成树。");
			}

			if (_attempts >= MaxInjectionAttempts)
			{
				_active = false;
				Log?.LogError("PrawnSuitFabricator：注入制造机失败次数达到上限，停止重试。");
				return;
			}

			_attempts++;
			try
			{
				InjectIntoFabricator();
				_injected = true;
				_active = false;
				Log?.LogInfo("PrawnSuitFabricator：制造机合成树注入完成，停止更新轮询。");
			}
			catch (Exception ex)
			{
				string error = ex.GetType().Name + ": " + ex.Message;
				if (error != _lastError)
				{
					_lastError = error;
					Log?.LogWarning("PrawnSuitFabricator：第 " + _attempts + " 次注入合成树失败：" + error);
				}
			}
		}

		private static void InjectIntoFabricator()
		{
			CraftTree tree = CraftTree.GetTree(CraftTree.Type.Fabricator);
			if (tree == null)
			{
				throw new InvalidOperationException("Fabricator 合成树为 null");
			}

			CraftNode nodes = tree.nodes;
			if (nodes == null)
			{
				throw new InvalidOperationException("Fabricator 树根为 null");
			}

			if (nodes.FindNodeById("Exosuit", true) != null)
			{
				Log?.LogInfo("PrawnSuitFabricator：海虾号节点已存在，跳过注入。");
				return;
			}

			CraftNode? category = nodes.FindNodeById(_categoryId, true) as CraftNode;
			string categoryName;
			if (category == null)
			{
				categoryName = "（回退：第一个分类）";
				foreach (CraftNode node in nodes)
				{
					if (node is { action: TreeAction.Expand })
					{
						category = node;
						break;
					}
				}
			}
			else
			{
				categoryName = category.id;
			}

			if (category == null)
			{
				category = nodes;
				categoryName = "Root";
			}

			var exosuitNode = new CraftNode("Exosuit", TreeAction.Craft, TechType.Exosuit);
			category.AddNode(exosuitNode);
			Log?.LogInfo("PrawnSuitFabricator：海虾号已加入制造机，挂载节点：" + categoryName);
		}

		internal static void SpawnExosuit()
		{
			Quaternion rotation = Quaternion.identity;
			EscapePod? escapePod = EscapePod.main;
			Vector3 position;

			if (escapePod != null)
			{
				Transform podTransform = escapePod.transform;
				position = podTransform.position
					+ podTransform.right * SpawnDistance
					+ podTransform.forward * SpawnDistance;
				position.y = podTransform.position.y;
			}
			else
			{
				Player? player = Player.main;
				if (player == null)
				{
					Log?.LogError("PrawnSuitFabricator：找不到逃生舱或玩家，无法生成海虾号。");
					return;
				}

				Transform playerTransform = player.transform;
				position = playerTransform.position + playerTransform.forward * SpawnDistance;
				Log?.LogWarning("PrawnSuitFabricator：找不到逃生舱，改为在玩家附近生成。");
			}

			GameObject? exosuit = CraftData.InstantiateFromPrefab(TechType.Exosuit, false);
			if (exosuit == null)
			{
				Log?.LogError("PrawnSuitFabricator：海虾号预制件实例化失败。");
				return;
			}

			Transform exosuitTransform = exosuit.transform;
			exosuitTransform.position = position;
			exosuitTransform.rotation = rotation;
			LargeWorldEntity.Register(exosuit);
			Log?.LogInfo("PrawnSuitFabricator：海虾号已生成于 " + position);
			ErrorMessage.AddMessage("海虾号已在逃生舱附近生成。");
		}
	}
}
