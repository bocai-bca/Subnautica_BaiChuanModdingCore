# AchievementFontFix（已并入 BaiChuanModdingCore）

原独立 BepInEx 插件已整合进核心，代码独立放在：

```text
Features/AchievementFontFix/
```

## 入口

```text
BaiChuanModdingCore.Awake()
  → AchievementFontFixFeature.Initialize(harmony, Logger)
```

没有 `Update()`。

Harmony 补丁：

```text
ArchitectsLibrary.Handlers.AchievementsMenu.Show
```

由核心在启动时反射找到类型后手动安装。

## 行为

```text
成就面板 Show 后
→ 读取静态字段 _current
→ 遍历面板下所有 UI.Text
→ 选择能显示中文的字体
→ 替换字体并强制刷新文本
```

前 6 次会输出处理结果。

## 字体选择

优先：

```text
uGUI_MainMenu.main 下第一个 HasCharacter('中') 的字体
```

其次：

```text
uGUI_MainMenu.main 下第一个可用字体
```

最后：

```text
Font.CreateDynamicFontFromOSFont(
  Microsoft YaHei / 微软雅黑 / SimHei / SimSun / Arial Unicode MS
)
```

结果缓存在 `_cachedFont`。

## 边界

```text
只处理 ArchitectsLibrary 成就面板
不修改其他 UI
不改 ArchitectsLibrary 源码
找不到类型/方法时只记录警告并跳过
```
