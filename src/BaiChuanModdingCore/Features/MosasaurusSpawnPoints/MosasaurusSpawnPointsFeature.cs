using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using BepInEx.Logging;
using UnityEngine;

namespace BaiChuanModdingCore.Features.MosasaurusSpawnPoints
{
	/// <summary>
	/// 独立功能：把 AnthCreatures 的 Mosasaurus 从随机群系刷新改为 9 个定点投放。
	/// 群系拦截由已修改的 AnthCreaturesMod.dll 负责；这里只注册定点。
	/// 通过反射调用 SMLHelper / ECCLibrary，避免核心工程额外引用 QMods 程序集。
	/// </summary>
	internal static class MosasaurusSpawnPointsFeature
	{
		private static readonly float[,] Points =
		{
			{ -774f, -112f, 515f, 400f, 0f },
			{ 1103f, -250f, 557f, 400f, 1f },
			{ -137f, -202f, 977f, 400f, 2f },
			{ 750f, -120f, -965f, 400f, 3f },
			{ -601f, -300f, -1886f, 400f, 4f },
			{ -274f, -60f, 465f, 400f, 5f },
			{ -601f, -60f, -305f, 400f, 6f },
			{ 334f, -60f, 96f, 400f, 7f },
			{ 57f, -60f, -672f, 400f, 8f },
		};

		private static ManualLogSource? _log;
		private static MonoBehaviour? _host;
		private static bool _registered;
		private static bool _retrying;

		internal static void Initialize(ManualLogSource log, MonoBehaviour host)
		{
			_log = log;
			_host = host;

			if (TryRegisterSafe())
			{
				return;
			}

			_log?.LogWarning("[MosasaurusSpawnPoints] 首次注册未就绪，启动延迟重试。");
			if (_host != null && !_retrying)
			{
				_host.StartCoroutine(Retry());
			}
		}

		private static IEnumerator Retry()
		{
			_retrying = true;
			for (int i = 0; i < 60; i++)
			{
				yield return new WaitForSecondsRealtime(0.5f);
				if (TryRegisterSafe())
				{
					_retrying = false;
					yield break;
				}
			}

			_retrying = false;
			_log?.LogError("[MosasaurusSpawnPoints] 等待 Mosasaurus TechType/ECC 接口超时，放弃注册。");
		}

		private static bool TryRegisterSafe()
		{
			try
			{
				return TryRegister();
			}
			catch (Exception ex)
			{
				_log?.LogError("[MosasaurusSpawnPoints] 注册异常：" + ex);
				return false;
			}
		}

		private static bool TryRegister()
		{
			if (_registered)
			{
				return true;
			}

			if (!TryGetTechType(out TechType techType))
			{
				return false;
			}

			Type? spawnType = FindType("ECCLibrary.StaticSpawn");
			Type? registryType = FindType("ECCLibrary.StaticCreatureSpawns");
			if (spawnType == null || registryType == null)
			{
				_log?.LogWarning("[MosasaurusSpawnPoints] ECCLibrary StaticSpawn/StaticCreatureSpawns 未找到。");
				return false;
			}

			ConstructorInfo? ctor = spawnType.GetConstructor(
				new[] { typeof(TechType), typeof(Vector3), typeof(string), typeof(float) });
			MethodInfo? register = registryType.GetMethod(
				"RegisterStaticSpawn",
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
				null,
				new[] { spawnType },
				null);

			if (ctor == null || register == null)
			{
				_log?.LogError("[MosasaurusSpawnPoints] ECCLibrary 注册接口签名不匹配。");
				return false;
			}

			for (int i = 0; i < Points.GetLength(0); i++)
			{
				Vector3 position = new Vector3(Points[i, 0], Points[i, 1], Points[i, 2]);
				float distance = Points[i, 3];
				int index = (int)Points[i, 4];
				string id = "MosasaurusStatic_" + index;

				object spawn = ctor.Invoke(new object[] { techType, position, id, distance });
				register.Invoke(null, new[] { spawn });

				var log = new StringBuilder(160);
				log.Append("[MosasaurusSpawnPoints] registered ").Append(id);
				log.Append(" @ (").Append(position.x.ToString("0")).Append(',');
				log.Append(position.y.ToString("0")).Append(',').Append(position.z.ToString("0")).Append(')');
				_log?.LogInfo(log.ToString());
			}

			_registered = true;
			_log?.LogInfo("[MosasaurusSpawnPoints] 9 个定点投放注册完成。");
			return true;
		}

		private static bool TryGetTechType(out TechType techType)
		{
			techType = default;
			Type? handler = FindType("SMLHelper.V2.Handlers.TechTypeHandler");
			if (handler == null)
			{
				return false;
			}

			MethodInfo? method = handler.GetMethod(
				"TryGetModdedTechType",
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
				null,
				new[] { typeof(string), typeof(TechType).MakeByRefType() },
				null);

			if (method == null)
			{
				return false;
			}

			object?[] args = { "Mosasaurus", default(TechType) };
			object? result = method.Invoke(null, args);
			if (result is bool ok && ok && args[1] is TechType value)
			{
				techType = value;
				return true;
			}

			return false;
		}

		private static Type? FindType(string fullName)
		{
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			for (int i = 0; i < assemblies.Length; i++)
			{
				try
				{
					Type? type = assemblies[i].GetType(fullName, false);
					if (type != null)
					{
						return type;
					}
				}
				catch
				{
				}
			}

			return null;
		}
	}
}
