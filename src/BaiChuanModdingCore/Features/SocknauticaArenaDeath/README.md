# SocknauticaArenaDeath（核心功能）

玩家在 Socknautica 竞技场/Boss 战中死亡时，替换原版的制作组名单流程。

## 原版行为

Socknautica 的：

```text
Socknautica.Mono.PlayerPatches.PlayerOnKill
```

在：

```text
ArenaSpawner.main != null
```

时会启动：

```text
等待 5 秒
→ SceneManager.LoadSceneAsync("EndCreditsSceneCleaner", Single)
```

也就是播放制作组名单。

同时原版：

```text
Player.OnKill
→ ResetPlayerOnDeath
→ 5 秒后自动复活
```

会和 10 秒黑屏冲突。

## 现在行为

Harmony Prefix 直接拦截：

```text
Player.OnKill(DamageType)
```

当 `ArenaSpawner.main != null` 时：

`Player.OnKill` Prefix 使用：

```text
[HarmonyPriority(Priority.First)]
```

确保它先于其他死亡类 Prefix 执行，避免被其他 Prefix 提前返回 false 跳过。

```text
跳过原版死亡界面
跳过 ResetPlayerOnDeath
跳过 5 秒自动复活
跳过 Socknautica PlayerOnKill 的名单流程
```

另需拦截 Socknautica 的：

```text
Socknautica.Patches.PlayerPatches.PlayerOnKill
```

因为它本身是 `Player.OnKill` 的 Postfix。  
即使核心 Prefix 跳过了原版 `Player.OnKill`，  
Harmony 仍可能执行这个 Postfix，  
从而启动：

```text
KillCoroutine
→ EndCreditsSceneCleaner
```

现在死亡流程中该 Postfix 返回 false，  
不会启动制作组名单协程。

同时保留：

```text
Socknautica.Mono.Alien.BeatGameOnTouch.Touch
```

的拦截，防止其他结束触发物加载名单。

改为核心自己的 10 秒流程。

死亡流程进行中重复触发 `OnKill` 也返回 false，  
避免二次死亡/复活打断黑屏。

改为：

```text
0s   创建全屏黑屏
0s   写入死亡存档标记
1s   WarningUI.Show("葬身4546B", genericwarning, 8f)
9s   警告自动消失
10s  SceneCleaner.Open()
```

时间轴：

```text
0s ───────────── 10s  黑屏
     1s ───── 9s      葬身警告
```

`SceneCleaner.Open()` 会清理场景并返回主菜单。

## 黑屏与警告层级

先调用 `WarningUI.Show`，再创建黑屏。

如果 WarningUI 创建成功：

```text
找到 warning.transform.parent
创建全屏黑色 Image
SetParent 到同一个父节点
SetAsFirstSibling()
```

这样黑色 Image 和 WarningUI 处于同一层级，  
黑色背景永远画在警告之前，不会盖住警告文字。

如果没有可用的 WarningUI 父节点，则退回独立：

```text
ScreenSpaceOverlay Canvas
sortingOrder = -1000
```

黑屏对象不设置 DontDestroyOnLoad，  
`SceneCleaner` 切场景时会随旧场景销毁。

## Boss 战胜利

Socknautica 击败 Boss 后会调用：

```text
Socknautica.SaveWithoutSaving.OnDefeatBoss
```

并写入：

```text
SavedGames/<slot>/boss-completion.txt
```

核心在该方法 Postfix 中删除当前 slot 的葬身 sidecar。

同时 `IsMarked()` 会检查 `boss-completion.txt`，  
即使 sidecar 清除失败，已通关存档也不会再显示葬身4546B。

## 存档文字

死亡时读取：

```text
SaveLoadManager.main.currentSlot
```

写入该存档自己的 sidecar：

```text
SNAppData\SavedGames\<slot>\BaiChuan_4546B.json
```

内容：

```json
{
  "version": 1,
  "dead": true,
  "timestamp": "2026-11-28 23:04:00"
}
```

主菜单读取 sidecar 并验证：\n\n```text\ndead = true\n```\n\n写入使用临时文件 + File.Replace/Move，\n避免半截 JSON。\n\n主菜单只检查：

```text
当前 slot 目录下是否存在 BaiChuan_4546B.json
```

不再需要：

```text
fingerprint
startTicks
目录创建时间
全局标记文件
```

存档删除时整个 slot 目录删除，sidecar 自动消失。  
同一 slot 新建存档不会继承旧标记。

主菜单 `MainMenuLoadButton` 的：

```text
Start
onCursorEnter
onCursorLeave
```

Postfix 会检查 `saveGame` 对应的 sidecar 是否存在。

三个入口都重新应用红色，  
避免鼠标移出后颜色被原版恢复成白色。

匹配时修改文字和颜色：

```text
葬身4546B
红色
```

不锁定、不禁止读取、不修改其他按钮。

## 日志

正常：

```text
[SocknauticaArenaDeath] arena death hook installed
[SocknauticaArenaDeath] save slot hook installed
[SocknauticaArenaDeath] save marked: <slot>
```

异常：

```text
[SocknauticaArenaDeath] PlayerPatches.PlayerOnKill not found
[SocknauticaArenaDeath] black overlay failed
[SocknauticaArenaDeath] WarningUI.Show not found
[SocknauticaArenaDeath] write marker failed
```

## 边界

```text
只在 ArenaSpawner.main 存在时触发
一次死亡只执行一次
主菜单加载后重置，下一次 Boss 战死亡仍可触发
不修改存档内容
不修改读档逻辑
```













