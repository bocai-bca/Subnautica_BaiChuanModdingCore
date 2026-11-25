# AutoCpuAffinity（已并入 BaiChuanModdingCore）

原独立 BepInEx 插件已整合进核心，代码独立放在：

```text
Features/AutoCpuAffinity/
```

## 入口

```text
BaiChuanModdingCore.Awake()
  → AutoCpuAffinityFeature.Initialize(Config, Logger, this)
  → StartCoroutine(Apply())
```

核心 `Update()` 不参与本功能。

## 配置

```text
[BepInEx/config/BaiChuanModdingCore.cfg]

[AutoCpuAffinity]
AutoDetect = true
OverrideCores = 0
```

旧独立插件的：

```text
com.baichuan.autocpuaffinity.cfg
```

不再读取。

## 它做什么

```text
等待 PerformanceBooster.MainPatcher
→ 检测逻辑核 / 物理核 / SMT / P核 E核
→ 修改 PerformanceBooster.CPUCores
→ 打开 affinityTweakEnabled
→ 调用 PerformanceBooster.SetAffinity
→ 安装 guard，阻止 PerformanceBooster 再次覆盖
```

## 边界

```text
只操作 PerformanceBooster
没有 PerformanceBooster 时跳过
不直接修改游戏、载具、制造或其他核心功能
```

## 合并时保留的行为

```text
最多等待 600 帧
AutoDetect=false 且 OverrideCores<=0 时不做任何事
用户已自定义 CPUCores（当前值不是 4）时保留
CPUCores 限制在 2-16
```

## 合并时没有改的逻辑

```text
CPU 拓扑检测
亲和性 guard
PerformanceBooster 反射调用
```
