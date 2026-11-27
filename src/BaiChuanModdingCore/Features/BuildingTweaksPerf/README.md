# BuildingTweaksPerf（已并入 BaiChuanModdingCore）

原独立 QMod 已整合进核心，代码独立放在：

```text
Features/BuildingTweaksPerf/
```

## 入口

```text
BaiChuanModdingCore.Awake()
  → BuildingTweaksPerfFeature.Initialize(harmony, Logger)
```

不依赖 QModManager 加载顺序；BuildingTweaks 不存在时只记录错误。

## 目标

反射查找：

```text
BuildingTweaks.Patches.Player_Update_Patch.Postfix
BuildingTweaks.Patches.Builder_UpdateAllowed_Patch.Postfix
```

分别挂 Transpiler。

## 优化内容

### WaterPark 舱门查找

原版 Postfix：

```text
new Collider[0]
LINQ FirstOrDefault<UseableDiveHatch>
```

改为：

```text
Physics.OverlapSphereNonAlloc
固定 Collider[32] 缓冲
直接遍历取第一个 UseableDiveHatch
```

### Ghost 模型组件

原版每次：

```text
GetComponents<IBuilderGhostModel>()
```

改为按 GameObject 缓存：

```text
同一模型只取一次
模型变化才重新取
```

### 白名单

替换 BuildingTweaks 每次构造的 TechType 白名单 List。

当前白名单仍是原插件的硬编码 TechType：

```text
5501, 5505, 5514, 5504, 5524,
5515, 5528, 5529, 5525, 5522
```

## 边界

```text
只优化 BuildingTweaks 的两个 Postfix
不改白名单内容
不新增每帧 Update
Transpiler 模式匹配失败时保留原逻辑并写警告
```

## 可继续优化

```text
ParkBuffer 只有 32 个 Collider，密集水域可考虑增大
Whitelist 是硬编码 TechType ID，游戏枚举变化会失效
```

## 日志

```text
[BuildingTweaksPerf] player transpiler applied: waterpark, ghost=True
[BuildingTweaksPerf] whitelist transpiler applied
```

异常：

```text
[BuildingTweaksPerf] BuildingTweaks patch types not found
[BuildingTweaksPerf] WaterPark Collider[0] pattern not found
[BuildingTweaksPerf] whitelist pattern not found
```
