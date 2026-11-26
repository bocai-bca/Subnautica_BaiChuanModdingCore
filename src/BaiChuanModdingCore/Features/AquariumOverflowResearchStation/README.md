# AquariumOverflowResearchStation（已并入 BaiChuanModdingCore）

原独立 BepInEx 插件已整合进核心，代码独立放在：

```text
Features/AquariumOverflowResearchStation/
```

## 入口

```text
BaiChuanModdingCore.Awake()
  → AquariumOverflowResearchStationFeature.Initialize(harmony, Logger)
```

没有 `Update()`。

## 依赖

必需：

```text
EquivalentExchange.ItemResearchStation
```

可选：

```text
BetterACU
AquariumOverflow
```

## 行为

```text
大型水族馆繁殖时：
  WaterPark 没空间
  → 检查基地里的生物反应堆
  → 反应堆也满
  → 把新鱼生成到研究站 storageContainer

小水族馆溢出：
  给 AquariumOverflow.TryOverflowIntoBioreactors 挂 Postfix
  → 把剩余的 breedCount 继续送进研究站

BetterACU：
  给它自己的 WaterParkBreedPostfix.Postfix 挂 Prefix
  → 避免同一帧重复处理
```

## 研究站解析

优先：

```text
EquivalentExchange.Monobehaviors.ItemResearchStation
```

找不到时只扫描程序集名包含：

```text
EquivalentExchange
Exchange
Research
```

的候选程序集，避免对全部 200+ 程序集执行 `GetTypes()`。

## 缓存与限流

```text
研究站容器：按 Component 缓存 2 秒
鱼 prefab：按 TechType 缓存，只缓存 prefab 引用
SubRoot 反应堆/研究站数组：缓存 2 秒
无反应堆/无研究站：失败后 3 秒内不重复扫描
```

缓存失效：

```text
对象被销毁 → 跳过/重扫
容器被替换 → StorageContainer.container 不一致时重建
研究站/反应堆列表为空 → 3 秒后再扫
```

## 边界

```text
启动时找不到研究站类型 → 本局禁用（不挂补丁）
基地里没有研究站 → 只警告一次，之后按 3 秒冷却继续尝试
没有生物反应堆时照常走原版
所有反射调用都有 try/catch
不改 EquivalentExchange / BetterACU / AquariumOverflow 源码
```
