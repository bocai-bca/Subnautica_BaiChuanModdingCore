# MosasaurusSpawnPoints（已并入 BaiChuanModdingCore）

原独立 BepInEx 插件已整合进核心，代码独立放在：

```text
Features/MosasaurusSpawnPoints/
```

## 入口

```text
BaiChuanModdingCore.Awake()
  → MosasaurusSpawnPointsFeature.Initialize(Logger, this)
```

不占用核心 `Update()`，也没有每帧轮询。

## 行为

```text
立即尝试注册 9 个 Mosasaurus 定点
如果 TechType / ECCLibrary 还没就绪
每 0.5 秒重试一次，最多 30 秒
```

## 与旧插件的关系

旧 v1.4 插件：

```text
Awake
→ Main.RegisterPoints()
```

合并版保留同一行为。

群系随机刷新拦截仍然由：

```text
已修改的 AnthCreaturesMod.dll
```

负责，核心不重复加 Harmony 前缀。

## 定点列表

使用 v1.4 坐标：

```text
西蘑菇林、库什区、浮岛区、坠毁区、水雷区
北/西/东/南红藻区
```

## 反射边界

核心工程不额外引用：

```text
SMLHelper.dll
ECCLibrary.dll
```

通过反射调用：

```text
SMLHelper.V2.Handlers.TechTypeHandler.TryGetModdedTechType
ECCLibrary.StaticSpawn..ctor
ECCLibrary.StaticCreatureSpawns.RegisterStaticSpawn
```

只在启动阶段执行，避免给核心工程增加 QMods 引用路径。
