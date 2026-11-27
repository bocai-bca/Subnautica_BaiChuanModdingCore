# AuroraSecondExplosion（已并入 BaiChuanModdingCore）

原独立 BepInEx 插件已整合进核心，代码独立放在：

```text
Features/AuroraSecondExplosion/
```

## 入口

```text
BaiChuanModdingCore.Awake()
  → AuroraSecondExplosionFeature.Initialize(Config, Logger, this)
```

没有 `Update()`。

## 优化

原版：

```text
每帧 Update
Time.time < _nextCheckTime 时直接 return
每秒最多检查一次
```

现在：

```text
每秒一次协程
没有每帧方法调用
```

行为不变。

## 配置

```text
[BepInEx/config/BaiChuanModdingCore.cfg]

[AuroraSecondExplosion.General]
DelayMinutes = 45

[AuroraSecondExplosion.Radiation]
RadiusMultiplier = 1.5
```

`DelayMinutes` 最小 1 分钟；`RadiusMultiplier` 最小 1.0。

旧独立插件的 `[General]` / `[Radiation]` 配置不再读取。

## 行为

```text
极光号第一次爆炸后
→ 计算已过时间
→ 如果超过延迟，本次不触发
→ 如果反应堆已修复，取消
→ 否则倒计时
→ 到点触发第二次爆炸
```

第二次爆炸：

```text
2000 伤害
500 半径
爆炸特效
镜头震动
爆炸推力
辐射上限放大
重新打开泄漏点
```

## 边界

```text
找不到 CrashedShipExploder 私有方法时只报错，不执行
没有 LeakingRadiation 时跳过辐射扩大
不改原版爆炸方法
```
