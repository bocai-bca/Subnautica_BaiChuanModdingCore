# BlueprintTrackerPerf（已并入 BaiChuanModdingCore）

原独立 QMod 已整合进核心，代码独立放在：

```text
Features/BlueprintTrackerPerf/
```

## 入口

```text
BaiChuanModdingCore.Awake()
  → BlueprintTrackerPerfFeature.Initialize(harmony, Logger)
```

启动时反射查找：

```text
BlueprintTracker.BlueprintTracker
BlueprintTracker.BlueprintTrackerEntry
BlueprintTracker.PinButton
BlueprintTracker.BlueprintTrackerIcon
BlueprintTracker.BlueprintTrackerRemoveButton
```

找不到时只记录错误，不抛异常。

## 优化内容

### Entry.Update

按 PDA 状态签名缓存：

```text
PDA 是否存在
PDA 是否打开
是否在 Journal 页
```

签名不变时跳过 Update。

### PinButton.Update

缓存：

```text
hover
down
mode
```

三个字段都不变时跳过 Update。

### Icon.OnEnable / Init

原版每个图标自带 UpdateRoutine。

现在：

```text
OnEnable Prefix 阻止原版 UpdateRoutine
图标注册到 IconUpdateManager
每 1 秒统一调用 UpdateText
```

### QuickSlots 查找

Transpiler 替换：

```text
FindObjectOfType<uGUI_QuickSlots>()
```

为缓存版本：

```text
QuickSlotsCache.Get()
```

## 边界

```text
只处理 BlueprintTracker 的 UI 更新
不修改配方、追踪数据或存档
无每帧 Update
IconUpdateManager 每 1 秒一次
```

## 注意

`IconEnablePrefix` 会直接跳过原版 `BlueprintTrackerIcon.OnEnable`，  
依赖原版 OnEnable 做其他初始化的改动可能受影响。

当前保持原独立插件行为。

## 日志

正常：

```text
[BlueprintTrackerPerf] installed
```

异常：

```text
[BlueprintTrackerPerf] BlueprintTracker types not found
[BlueprintTrackerPerf] missing method: ...
[BlueprintTrackerPerf] PinButton fields missing; pin optimization disabled
[BlueprintTrackerPerf] BlueprintTrackerIcon.UpdateText not found
```
