# ResourceTrackerGuard（已并入 BaiChuanModdingCore）

原独立 BepInEx 插件已整合进核心，代码独立放在：

```text
Features/ResourceTrackerGuard/
```

## 入口

```text
BaiChuanModdingCore.Awake()
  → ResourceTrackerGuardPatch.Initialize(Logger)

Harmony 补丁由核心 PatchAll 自动安装
```

没有 `Update()`。

## 目标

```text
ResourceTracker.Start()
```

## 行为

```text
HarmonyPriority(Priority.First)
__instance == null → 跳过
prefabIdentifier == null → 跳过
否则正常放行
```

## 为什么需要

`Exscansion.ScannerFilteringHook2.Transpiler` 会在
`ResourceTracker.Start` 里注入代码；没有 `PrefabIdentifier` 的模组对象
会在进入原方法后触发 NullReferenceException。

本补丁抢在 Exscansion 注入代码之前跳过无效对象，避免每局重复 NRE。

## 代价

```text
没有 PrefabIdentifier 的 ResourceTracker 不会初始化
```

这类对象本来就没有有效 prefab 标识，跳过比刷 NRE 更合理。

## 边界

```text
只影响 ResourceTracker.Start
不修改 Exscansion
不影响有 PrefabIdentifier 的正常对象
```
