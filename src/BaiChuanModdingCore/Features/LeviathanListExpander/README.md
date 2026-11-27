# LeviathanListExpander（已并入 BaiChuanModdingCore）

原独立 BepInEx 插件已整合进核心，代码独立放在：

```text
Features/LeviathanListExpander/
```

## 入口

```text
BaiChuanModdingCore.Awake()
  → LeviathanListExpanderFeature.Initialize(Config, harmony, Logger, this)
```

没有 `Update()`。

## 行为

```text
Socknautica.Mono.LootGeneration.DetermineClassIDs Postfix
→ 用加权利维坦 ID 表替换原列表

Socknautica.Mono.LootGeneration.SpawnPrefab Prefix
→ 非 GUID 的模组 classId 通过 PrefabDatabase / ModPrefab 解析后生成
```

权重表包含：

```text
GhostLeviathanJuvenile 9
GhostLeviathan 10
VoidGhostLeviathan 9
ReaperLeviathan 10
SeaDragon 10
Reefback 10
AbyssalBlaza 10
AncientBloop 6
GulperLeviathan 10
Mosasaurus 10
Kraken 5
GargantuanLeviathan 1
```

## 配置

```text
[BepInEx/config/BaiChuanModdingCore.cfg]

[LeviathanListExpander.General]
Enable = true
```

旧独立插件的 `[General]` 配置不再读取。

## 优化点

```text
启动探测：每帧 TryPatch → 每 0.5 秒一次，最多 120 秒
LootGeneration 类型缓存
leviathanClassIDs 字段缓存
SpawnPrefab 方法只在安装时查找一次
```

原版在类型未出现时每帧反射，  
`ListPostfix` 每次回调也重新 GetType/GetField。

## 边界

```text
没有 Socknautica 时超时警告并跳过
没有 SMLHelper ModPrefab 时只走 PrefabDatabase
不改 Socknautica / SMLHelper 源码
```
