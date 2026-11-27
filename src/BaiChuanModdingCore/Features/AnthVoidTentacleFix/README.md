# AnthVoidTentacleFix（已并入 BaiChuanModdingCore）

原独立 BepInEx 插件已整合进核心，代码独立放在：

```text
Features/AnthVoidTentacleFix/
```

## 入口

```text
BaiChuanModdingCore.Awake()
  → AnthVoidTentacleFixFeature.Initialize(harmony, Logger)
```

没有 `Update()`。

Harmony 目标：

```text
AnthCreaturesMod.Mono.IdleTentacleSpawner.Update
```

## 行为

```text
玩家在虚空或坐标超出 ±2000
且 CthulhuSpawner 活跃
→ 最多生成 6 条虚空触手

玩家离开虚空或 Cthulhu 不活跃
→ 清理全部触手
```

内部仍然保留 1 秒生成检查节流。

## 优化点

```text
反射字段/方法只解析一次
  timeTrySpawnAgain
  spawnedTentacles
  spawnRadius
  GetRandomTentaclePosition
  SpawnTentacle
  DespawnTentacle
  CthulhuSpawner.timerRunning
  CthulhuSpawner.currentCthulhu

CthulhuSpawner 搜索从每秒一次降到每 2 秒一次
```

## 边界

```text
没有 AnthCreatures 时只警告并跳过
没有 CthulhuSpawner 时不生成触手
不改 AnthCreatures 源码
所有反射调用都有 try/catch
```
