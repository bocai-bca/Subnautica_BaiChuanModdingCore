# EasyCraftCreativeFix（已并入 BaiChuanModdingCore）

原独立 QMod 已整合进核心，代码独立放在：

```text
Features/EasyCraftCreativeFix/
```

## 入口

```text
BaiChuanModdingCore.Awake()
  → EasyCraftCreativeFixFeature.Initialize(harmony, Logger)
```

启动时反射查找：

```text
EasyCraft.Main._IsCraftRecipeFulfilledAdvanced
```

找到后手动挂 Prefix；找不到只警告，不影响核心。

## 行为

```text
GameModeUtils.RequiresIngredients() == true
  → 原版逻辑

GameModeUtils.RequiresIngredients() == false（创造模式）
  → __result = true
  → 跳过原方法
```

首次绕过时输出一次：

```text
[EasyCraftCreativeFix] creative mode bypass active; queue checks return true
```

## 边界

```text
只改 EasyCraft 的队列材料预检查
不改 EasyCraft 其他逻辑
没有 Update
没有每帧开销
```
