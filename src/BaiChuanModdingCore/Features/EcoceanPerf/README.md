# EcoceanPerf（已并入 BaiChuanModdingCore）

由原有两个独立 QMod 合并：

```text
EcoceanShieldGuard
EcoceanFinalTargetCache
```

统一放在：

```text
Features/EcoceanPerf/
```

## 入口

```text
BaiChuanModdingCore.Awake()
  → EcoceanPerfFeature.Initialize(harmony, Logger)
```

## 功能

### VoidTongueTag 目标缓存

反射查找：

```text
ReikaKalseki.Ecocean.VoidTongueTag.getPriorityTarget
```

缓存每个 `VoidTongueTag` 的目标 0.5 秒：

```text
0.5 秒内重复调用
→ 直接返回缓存 Transform
→ 跳过原方法
```

缓存使用 `ConditionalWeakTable`，不持有实例生命周期。

### SubRoot 护盾空引用保护

Patch：

```text
SubRoot.StartSubShielded
SubRoot.EndSubShielded
```

如果：

```text
SubRoot.live == null
```

直接跳过原方法，避免：

```text
Ecocean 在无 LiveMixin 的 SubRoot 上启停护盾
→ 原版访问 live 空引用
```

## 边界

```text
不修改 Ecocean 源码
不修改护盾数值
VoidTongueTag 不存在时只跳过目标缓存，护盾保护仍然安装
无每帧 Update
```

## 日志

```text
[EcoceanPerf] installed
```

异常：

```text
[EcoceanPerf] VoidTongueTag.getPriorityTarget not found; target cache skipped
[EcoceanPerf] SubRoot.StartSubShielded not found
[EcoceanPerf] SubRoot.EndSubShielded not found
```
