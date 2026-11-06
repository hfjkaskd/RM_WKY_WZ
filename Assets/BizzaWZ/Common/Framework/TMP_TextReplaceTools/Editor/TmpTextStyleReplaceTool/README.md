# TMP Text Style Replace Tool

## 1. 功能说明
这是一个 Unity 编辑器工具，用来扫描指定目录下的 Prefab，找到所有 `TMP_Text`，根据它所在 GameObject 上挂载的“类型脚本”，批量替换：

- `TMP_Text.font`
- `TMP_Text.color`
- `TMP_Text.fontSharedMaterial`

支持四类文本：

- 一级标题
- 二级标题
- 三级标题
- 正文

## 2. 目录结构建议
把脚本放进你的 Unity 工程后，建议目录如下：

```text
Assets/
└── Editor/
    ├── TmpTextStyleReplaceWindow.cs
    ├── TmpTextStyleReplaceProcessor.cs
    └── TmpTextStyleReplaceData.cs
```

其中：

- `TmpTextStyleReplaceWindow.cs` 必须放在 `Editor` 目录
- `TmpTextStyleReplaceProcessor.cs` 建议也放 `Editor` 目录
- `TmpTextStyleReplaceData.cs` 当前实现里引用了 `UnityEditor.MonoScript`，所以也建议直接放 `Editor` 目录

最省事的做法，就是这三个脚本都丢进 `Assets/Editor/TmpTextStyleReplaceTool/`。

## 3. 使用前提
项目里需要已有：

- TextMeshPro
- 你的文本类型标记脚本，例如：
  - `TitleLevel1Tag`
  - `TitleLevel2Tag`
  - `TitleLevel3Tag`
  - `BodyTextTag`

这些脚本只要是挂在 `TMP_Text` 同一个 GameObject 上的 `MonoBehaviour`/`Component` 即可。

例如：

```csharp
// using UnityEngine;

// public class TitleLevel1Tag : MonoBehaviour
// {
// }
```

## 4. 打开方式
Unity 顶部菜单：

```text
Tools/TMP Style Replace Tool
```

## 5. 操作步骤
1. 打开工具窗口。
2. 选择 `Scan Folder`。
3. 配置四种文本类型脚本。
4. 配置四种样式预设：
   - Font Asset
   - Color
   - Material Preset
5. 点击 `开始扫描并替换`。
6. 查看 Console 日志和 Prefab 变更结果。

## 6. 规则说明

### 6.1 没挂任何类型脚本
- 跳过
- 默认不报日志，可勾选输出跳过日志

### 6.2 挂了多个类型脚本
- 打印 Warning
- 跳过，不替换

### 6.3 当前字体资源和目标字体资源不一致
- 自动替换字体
- 打印 Warning
- 继续替换颜色和 Material Preset

### 6.4 当前颜色不一致
- 直接替换
- 不警告

### 6.5 当前 Material Preset 不一致
- 直接替换
- 不警告

## 7. 日志示例

### 成功替换
```text
[TMP Style Replace][Info] Prefab: Assets/UI/Test.prefab, Object: Canvas/Title, 已应用样式：一级标题
```

### 多类型冲突
```text
[TMP Style Replace][Warning] Prefab: Assets/UI/Test.prefab, Object: Canvas/Title, TMP_Text 同时挂载多个类型脚本：一级标题, 正文，已跳过
```

### 字体不一致自动纠正
```text
[TMP Style Replace][Warning] Prefab: Assets/UI/Test.prefab, Object: Canvas/Title, TMP_Text 当前 FontAsset 与目标 FontAsset 不一致，已自动替换
```

### 汇总日志
```text
===== TMP Style Replace Result =====
Scanned Prefabs: 120
Scanned TMP_Text: 843
Replaced TMP_Text: 516
Skipped No Tag: 201
Skipped Multi Tags: 18
Font Mismatch Fixed: 67
Saved Prefabs: 74
====================================
```

## 8. 注意事项
1. `Material Preset` 最好使用和目标 `TMP_FontAsset` 匹配的材质预设。人类很喜欢把不匹配的字体材质硬绑上去，然后再问为什么描边发灰、字形发虚。
2. `Scan Folder` 必须是 `Assets` 下的目录。
3. 这个工具处理的是 Prefab，不处理场景中的对象。
4. 当前实现是强制覆盖模式，不提供“保留已有颜色/材质”的选项。
5. 如果某种类型没有配置样式预设，该类型会被跳过并打印 Warning。

## 9. 可继续扩展的方向
你后面要扩展也不难，至少比把逻辑全塞进一个窗口里强：

- 支持 ScriptableObject 保存配置
- 支持只扫描选中的 Prefab
- 支持 Dry Run 预览模式
- 支持字号、间距、对齐等更多样式项
- 支持场景对象批量处理
- 支持 Undo

## 10. 已知实现取舍
当前版本里，`TmpTextStyleReplaceConfig` 使用了 `MonoScript` 来让你在窗口里拖入脚本资源，再通过 `GetClass()` 判断 GameObject 是否挂了对应组件。

这个做法优点是：
- 直观
- 易配
- 不需要你再写额外的类型选择器

缺点也很明显：
- 配置是窗口内临时数据，关窗口就没了
- 依赖 `UnityEditor` 类型，不能当纯运行时数据类用

但这版目标本来就是“先能用”，不是“先把架构论文写完”。
