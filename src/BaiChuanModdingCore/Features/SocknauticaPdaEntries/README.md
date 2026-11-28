# SocknauticaPdaEntries（核心功能）

补全 Socknautica 已有文本、但没有注册进 PDA 的内容。

## 补入内容

### PDA 日志

注册：

```text
RocketSelfDestruct1
RocketSelfDestruct2
```

但**不默认解锁**。

只有对应语音真正播放时，才写入 PDA 日志：

```text
CustomPDALinesManager.PlayVoiceLine("RocketSelfDestruct1")
CustomPDALinesManager.PlayVoiceLine("RocketSelfDestruct2")
```

日志文本使用原有语言键：

```text
You have chosen not to come home to Alterra.
Detecting multiple energy signatures originating from the ecological dead zone...
```

### PDA 数据库

注册：

```text
SocknauticaLore1
```

路径：

```text
DownloadedData/Codes
```

对应中文分类：

```text
资料下载 → 数据和线索
```

标题和正文使用原有：

```text
Ency_SocknauticaLore1
EncyDesc_SocknauticaLore1
```

条目注册为：

```text
unlocked = false
```

不会默认出现在 PDA 数据库里。

解锁时机与自毁语音绑定：

```text
RocketSelfDestruct1 或 RocketSelfDestruct2 播放
→ 对应 PDA 日志解锁
→ 尝试 PDAEncyclopedia.Add("SocknauticaLore1")
```

因为这段 Lore 本身就是 Alterra 对“选择不回家”的回复，  
和火箭自毁语音属于同一段剧情。

`RocketSelfDestruct2` 作为 line1 失败兜底，  
百科已解锁时不会重复提示。

解锁成功会显示：

```text
你收到了一份阿尔特拉的信件
```

## 实现方式

Socknautica 只调用了：

```text
LanguageHandler.SetLanguageLine(...)
CustomSoundHandler.RegisterCustomSound(...)
```

没有调用：

```text
PDAHandler.AddLogEntry
PDAEncyclopedia.Add
```

核心在 `Awake` 阶段先通过 SMLHelper 注册自定义 EntryData：

```text
SMLHelper.V2.Handlers.PDALogHandler.AddCustomEntry
SMLHelper.V2.Handlers.PDAEncyclopediaHandler.AddCustomEntry
```

这样映射会在 `PDALog.Initialize` / `PDAEncyclopedia.Initialize` 时提前存在。

### 日志解锁

Harmony Postfix：

```text
Socknautica.CustomPDALinesManager.PlayVoiceLine
```

当 key 为：

```text
RocketSelfDestruct1
RocketSelfDestruct2
```

才执行：

```text
PDALog.Add(key, false)
```

### 百科解锁

核心不主动调用 `PDAEncyclopedia.Add`。

只注册：

```text
SocknauticaLore1
```

真正的解锁由 `CustomPDALinesManager.PlayVoiceLine` 的 Postfix 在自毁语音播放时完成。

## 日志

正常：

```text
[SocknauticaPda] custom log entries registered
[SocknauticaPda] custom encyclopedia entry registered (locked)
[SocknauticaPda] voice log hook installed
[SocknauticaPda] log entry unlocked: RocketSelfDestruct1
[SocknauticaPda] encyclopedia unlocked: SocknauticaLore1
[SocknauticaPda] log entry unlocked: RocketSelfDestruct2
```

异常：

```text
[SocknauticaPda] SMLHelper PDALogHandler not found
[SocknauticaPda] SMLHelper PDAEncyclopediaHandler not found
[SocknauticaPda] Socknautica.CustomPDALinesManager.PlayVoiceLine not found
[SocknauticaPda] AddLog(...) failed: ...
```

## 注意

日志条目没有独立标题，只显示原有语音文本。

百科条目在未触发对应终端前，状态是：

```text
mapping = True
entry = False
node = False
```

这是正常的“已注册但未解锁”，不是失败。

触发后才会变成：

```text
mapping = True
entry = True
node = True
```
