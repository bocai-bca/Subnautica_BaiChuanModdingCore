# TankModuleInstaller（已并入 BaiChuanModdingCore）

原独立 BepInEx 插件已整合进核心，代码独立放在：

```text
Features/TankModuleInstaller/
```

## 入口

```text
BaiChuanModdingCore.Awake()
  → TankModuleInstallerFeature.Initialize(Logger)
```

没有 `Update()`，只在点击 Tank 升级控制台时执行。

Harmony 补丁：

```text
VehicleUpgradeConsoleInput.OnHandClick
```

由核心现有 `harmony.PatchAll()` 自动安装。

## 行为

```text
只对类型名为 Tank 的载具生效
点击升级控制台时直接把背包模块装入 TankModule1..4
不打开原版升级界面
其他载具仍然走原版
```

允许：

```text
TankIonPiercer（震爆弹）      最多 1
VehiclePowerUpgradeModule     动力，计入 3 个上限
VehicleArmorPlating           装甲，计入 3 个上限
总槽位 4
```

## 反射边界

保留原 v1.2 反射方式：

```text
Equipment.slotMapping
Equipment.AddItem(string, InventoryItem, bool)
SMLHelper.V2.Handlers.TechTypeHandler.TryGetModdedTechType
```

没有让核心工程直接引用 SMLHelper。

## 相比原插件的改动

```text
SlotIds 改为静态数组，避免每次点击重复分配
TankIonPiercer TechType 查询结果缓存
日志统一走 BepInEx Logger
只保留实际执行的点击安装逻辑
```

## 边界

```text
不改 Tank 本体
不改 Equipment 的公开 API
不处理非 Tank 载具
```
