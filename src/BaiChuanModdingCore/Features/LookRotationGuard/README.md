# LookRotationGuard（已并入 BaiChuanModdingCore）

原独立 BepInEx 插件已整合进核心，代码独立放在：

```text
Features/LookRotationGuard/
```

## 入口

```text
BaiChuanModdingCore.Awake()
  → LookRotationGuardPatch.Initialize(Logger)

Harmony 补丁由核心 PatchAll 自动安装
```

没有 `Update()`。

## 目标

```text
UnityEngine.Quaternion.LookRotation(Vector3 forward, Vector3 upwards)
```

单参重载会转调双参版本，所以一处补丁覆盖全部调用者。

## 行为

```text
forward.sqrMagnitude < 1e-8
→ 返回 Quaternion.identity
→ 不再进入 Unity 原生零向量警告路径
```

正常输入完全放行。

## 日志

```text
前 10 次
之后每 500 次
```

输出被拦截的零向量调用，堆栈截断到 2000 字符。

## 为什么需要

ECCLibrary 的 `TrailManagerUpdateTrailsPatch` 在尾迹段重合时会向
`Quaternion.LookRotation` 传入零向量，Unity 原生代码会打印：

```text
Look rotation viewing vector is zero
```

并采集堆栈，低频时无害，高频时会造成明显日志/性能开销。

## 边界

```text
只拦截零向量
返回值与 Unity 自身零向量时一致
不改 ECCLibrary
不影响正常 LookRotation
```
