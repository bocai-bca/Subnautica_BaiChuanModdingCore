# PrawnSuitFabricator（已并入 BaiChuanModdingCore）

原独立 BepInEx 插件已反编译并整合进核心，代码独立放在：

```text
Features/PrawnSuitFabricator/
```

## 入口

```text
BaiChuanModdingCore.Awake()
  → PrawnSuitFabricatorFeature.Initialize(Config, Logger)

BaiChuanModdingCore.Update()
  → 仅在 PrawnSuitFabricatorFeature.Active 时调用 Update()
```

## 配置

```text
[BepInEx/config/BaiChuanModdingCore.cfg]

[PrawnSuitFabricator]
CategoryId = Machines
```

可选：

```text
Resources
Survival
Personal
Tools
Machines
```

旧独立插件的 `com.baichuan.prawnsuitfabricator.cfg` 不再读取。

## 相比原独立插件的改动

1. 注入成功后 `_active = false`，核心不再每帧调用本功能。
2. 注入重试上限 300 次（每秒最多一次），避免永久轮询。
3. 增加 `Player.main == null` 保护，避免找不到逃生舱且玩家未就绪时 NRE。
4. 所有状态、配置、Harmony 补丁独立在 `PrawnSuitFabricator` 命名空间内，不与其他功能混写。
5. 保留原有的延迟注入，避免提前触发 SMLHelper 的 CraftTree 初始化。

## 补丁边界

```text
HarmonyPatch: CrafterLogic.TryPickup
条件：currentTechType == TechType.Exosuit 且 progress >= 1
行为：生成到逃生舱附近，Reset，跳过原方法
```

不修改：

```text
CraftTree 以外的树
ConstructorInput / 载具仓
其他 TechType
```
