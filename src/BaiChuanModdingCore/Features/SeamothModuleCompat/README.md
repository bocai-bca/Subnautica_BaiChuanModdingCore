# SeamothModuleCompat（已并入 BaiChuanModdingCore）

原独立 QMod 已整合进核心，代码独立放在：

```text
Features/SeamothModuleCompat/
```

## 入口

```text
BaiChuanModdingCore.Awake()
  → SeamothModuleCompatFeature.Initialize(harmony, Logger)
```

## 模块兼容

启动时通过反射解析：

```text
VehiclePowerUpgradeModuleMK2
VehiclePowerUpgradeModuleMK3
SeamothHullModule4
SeamothHullModule5
```

使用 SMLHelper：

```text
SMLHelper.V2.Handlers.TechTypeHandler.TryGetModdedTechType
```

核心不直接引用 SMLHelper 程序集，只用反射。

## UpgradedVehicles 兼容

反射：

```text
UpgradedVehicles.VehicleUpgrader
```

接管：

```text
GetEfficiencyBonus
SeaMoth.OnUpgradeModuleChange
SeaMoth.Update
```

如果 UpgradedVehicles 尚未加载，会订阅：

```text
AppDomain.AssemblyLoad
```

加载后补挂。

## MK 模块效果

```text
MK2 电源升级：效率 +1.75
MK3 电源升级：效率 +2.5
HullModule4 / HullModule5：深度等级
```

没有 UpgradedVehicles 时使用 fallback：

```text
enginePowerRating = 1 + 普通电源模块 + MK2/MK3 加成
```

## SpeedController

每帧 Postfix 只在：

```text
SeaMoth 正在驾驶
```

时执行。

缓存：

```text
forwardForce
backwardForce
sidewardForce
verticalForce
SES gear/state
EldritchMoth stage
UpgradedVehicles multiplier
```

全部不变时直接返回，不重复写物理参数。

## 边界

```text
不修改模块数值表
只修改 SeaMoth 的升级变化响应和速度参数
无每帧强制写值（状态不变时跳过）
```

## 日志

```text
[SeamothModuleCompat] ready  MK2=True MK3=True MK4=True MK5=True
```

反射失败时 MK 标记为 False，但不影响启动。
