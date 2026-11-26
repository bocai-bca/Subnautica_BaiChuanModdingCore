# CreatureHpFix（已并入 BaiChuanModdingCore）

原独立 BepInEx 插件已整合进核心，代码独立放在：

```text
Features/CreatureHpFix/
```

## 入口

```text
BaiChuanModdingCore.Awake()
  → CreatureHpFixFeature.Initialize(Logger, this)

Harmony 补丁由核心 PatchAll 自动安装
```

没有 `Update()`。

## 目标

```text
LiveMixin.Start
```

只处理：

```text
GhostLeviathan
ReaperLeviathan
SeaDragon
Reefback
SeaEmperorJuvenile
```

Warper 已按要求排除。

## 行为

```text
如果 health < maxHealth
→ 回填为 maxHealth

死亡个体（health <= 0.001）不处理
每个实例只处理一次
2 秒后再确认一次，处理旧存档延迟反序列化
```

## 优化点

```text
用 ConditionalWeakTable<LiveMixin, object> 去重
避免 int InstanceID 被 Unity 复用后误跳过新生物

Patch 只 GetComponent<Creature> 一次
并把 Creature 传入 TryFix，避免重复查找
```

## 边界

```text
不改 maxHealth（由核心 CreaturePatches 负责）
不处理 Warper
不复活死亡生物
不处理未列出的生物
```
