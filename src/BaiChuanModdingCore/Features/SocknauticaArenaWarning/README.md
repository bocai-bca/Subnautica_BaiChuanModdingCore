# SocknauticaArenaWarning（核心功能）

玩家通过传送门进入 Socknautica 竞技场、  
`ArenaSpawner.SpawnArena()` 开始生成竞技场时，弹出警告。

## 显示内容

```text
传送,复活与存档已被领域禁用
```

参数：

```text
duration = 10 秒
背景图 = genericwarning
```

## 触发点

Harmony Postfix：

```text
Socknautica.Mono.ArenaSpawner.SpawnArena
```

这个静态方法由：

```text
Socknautica.Mono.InitiateArenaSpawnInRange
```

在玩家进入传送门落点范围时调用。

## 实现

通过反射调用：

```text
Socknautica.Mono.WarningUI.Show(
    "传送,复活与存档已被领域禁用",
    genericwarning,
    10f
)
```

`genericwarning` 从：

```text
Socknautica.Main.assetBundle
```

加载并缓存。

## 兜底

如果 `WarningUI.Show` 失败，例如：

```text
prefabObject 尚未初始化
```

则使用：

```text
ErrorMessage.AddMessage("传送,复活与存档已被领域禁用")
```

保证至少能看到文字提示。

## 日志

正常：

```text
[SocknauticaArenaWarning] arena warning hook installed
[SocknauticaArenaWarning] warning shown
```

异常：

```text
[SocknauticaArenaWarning] ArenaSpawner.SpawnArena not found
[SocknauticaArenaWarning] WarningUI.Show not found
[SocknauticaArenaWarning] genericwarning load failed
[SocknauticaArenaWarning] show failed
```

## 边界

```text
一次游戏进程只显示一次
不修改竞技场生成逻辑
不修改 Boss 数值或波次
```
