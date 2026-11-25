# TooltipRainbowGradient（已并入 BaiChuanModdingCore）

原独立 BepInEx 插件已整合进核心，代码独立放在：

```text
Features/TooltipRainbowGradient/
```

## 入口

```text
BaiChuanModdingCore.Awake()
  → TooltipRainbowGradientFeature.Initialize(Config, Logger)

BaiChuanModdingCore.Update()
  → TooltipRainbowGradientFeature.Tick()
```

Harmony 补丁：

```text
Text.OnPopulateMesh(VertexHelper)
```

由核心现有的 `harmony.PatchAll()` 自动安装。

## 配置

```text
[BepInEx/config/BaiChuanModdingCore.cfg]

[TooltipRainbowGradient.General]
Enabled = true
Keywords = 寰宇,无尽

[TooltipRainbowGradient.Animation]
Animated = true
FlowSpeed = 0.12
RebuildIntervalFrames = 2

[TooltipRainbowGradient.Style]
Saturation = 0.90
Brightness = 1.00
HueSpan = 0.85
HueOffset = 0.00
```

旧独立插件的：

```text
com.baichuan.tooltiprainbow.cfg
```

不再读取。

## 行为

```text
只处理 uGUI_Tooltip 的标题 Text
第一行包含关键字才绘制彩虹
默认每 2 帧推进一次色相
关闭 Enabled 后恢复原色
```

## 边界

```text
不改提示框布局
不改其他 Text 的颜色
不影响物品/交互逻辑
```

## 合并时保留的优化

```text
Tooltip Text 缓存
关键字列表缓存
RebuildIntervalFrames 降频
只在目标 Text 的 Postfix 里做顶点重涂
```
