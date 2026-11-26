# ArenaTeleportBlocker（已并入 BaiChuanModdingCore）

原独立 BepInEx 插件已整合进核心，代码独立放在：

```text
Features/ArenaTeleportBlocker/
```

## 入口

```text
BaiChuanModdingCore.Awake()
  → ArenaTeleportBlockerFeature.Initialize(Config, harmony, Logger, this)
```

没有 `Update()`。

两个启动协程：

```text
HookArenaSpawner       最多 30 秒，每 0.25 秒找一次 Socknautica.Mono.ArenaSpawner
PatchTeleportsFallback 最多 15 秒，每 0.5 秒尝试挂传送目标
```

## 配置

```text
[BepInEx/config/BaiChuanModdingCore.cfg]

[ArenaTeleportBlocker.General]
Enable = true
ArenaRadius = 400
VerboseLog = false
```

旧独立插件的 `[General]` 配置不再读取。

## 拦截目标

```text
原版 PrecursorTeleporter.BeginTeleportPlayer
FCS_HomeSolutions QuantumTeleporter.TeleportPlayer（3 参数）
RoTA WarpCannon.WarpToLastVisitedBase（返回 IEnumerator）
WarpChip.WarpChipFunction.ReturnToBase
```

## 判定

```text
只有玩家位于 Socknautica 竞技场范围内才拦截
中心：ArenaSpawner.arenaPos
高度：arenaHeight / 2
半径：ArenaRadius
```

特殊处理：

```text
ArenaTeleporter 指向 x=647 的出口
或 BeginBossFight 已完成 → 拦截
```

## 边界

```text
没有 Socknautica 时使用默认中心 (0,-2000,0)
只影响传送方法，不修改其他玩法
所有反射/补丁异常都有 try/catch
```
