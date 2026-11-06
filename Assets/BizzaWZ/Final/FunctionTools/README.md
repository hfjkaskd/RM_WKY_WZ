# FunctionTools

这里放置按功能拆分的脚本复制工具。当前已创建：

- `SDK`：Connect_SDK
- `Analytics`：打点
- `RemoteConfig`：远端配置
- `IPBlock`：IP 检测及 SCB 屏幕采集保护

每个工具都可以从 Unity 菜单 `Bizza/功能工具` 打开。SDK 和 IP 使用同一套公共导出窗口逻辑，工具会记住自己的目标目录和 `.meta` 选项。导出会直接清空当前功能目标目录，再复制完整内容，不生成 `.FunctionCopyBackup`。公共清单中的 `@SHARED/...` 会写入共享公共根目录，不会被功能目录清空。

SDK 和 IP 的复制内容维护在 `Manifests` 目录中的 JSON 清单，后续框架更新后优先修改清单，不需要改窗口代码：

- `Manifests/CommonPublicFileManifest.json`：IP/SDK 共用清单，包含设备信息、日志、授权失败弹窗（脚本+预制件）、Android `mainTemplate.gradle` 和 `PostProcessBuild_Android`。
- `Manifests/SDKCommonFileManifest.json`：SDK 专用公共代码清单（日志、网络、旧设备工具和 SDK 事件）。
- `Manifests/SDKFileManifest.json`：SDK 功能代码、SDK 专属代码、完整 `_Bizza_BuildTemplate`、平台代码和项目配置，并引用 SDK 专用公共清单。
- `Manifests/IPBlockFileManifest.json`：IP 功能代码、SCB 屏幕采集保护、`AuthConfig.bytes` 配置模板和 IP 必需的 3 个 Java 文件，并引用公共清单；不会复制 SDK/广告扩展 Java。

`CommonPublic` 中的 `Boots_Flow` 是外部工程统一初始化入口：SDK 导出、IP 导出或两者组合导出后均使用 `await Boots_Flow.Init()`。本项目自己的 `BootsFlowTask` 依赖加载框架，不会复制到外部工程。

SDK 导出会递归保留 `AccountModule`，其中的 `OceanShineApplyWithdrawalRequestFake`、`GetApplyWithdrawalRequestFake()` 和 `Request_ApplyWithdrawalMoneyRollReqRequest()` 是假网赚游戏的公开提现接口，不得从 SDK 清单中排除。

清单还会单独校验上述关键入口文件。它们虽然由父目录递归复制，但任一源文件缺失都会让导出预览报错并禁止执行。

IP 核心不再编译 SDK 的网络、旧设备工具和业务事件代码。SDK 清单会从 `SDK_WKY` 中排除已经进入 `CommonPublic` 的日志、设备和 Android 后处理脚本。IP 只导出 `BangsawanLog.java`、`DeviceUtils.java`、`UnityHelper.java`，SDK 仍导出完整构建模板。SDK/IP 共用的弹窗脚本和预制件只由 `CommonPublicFileManifest` 导出一次。XXTEA 由 SDK 和 IP 各自的功能程序集按自身清单提供，避免跨程序集重复定义。

IP 模块运行时代码使用 `UserInformationVerification` 程序集；代码命名空间仍为 `Bizza.UserInformationVerification`。SDK/IP 共用的身份和日志接口位于 `CommonPublic/Contracts`，设备信息统一由 `Bizza.Common.DeviceInfo` 的 `DeviceInfoUtil` 提供。IP 程序集不引用 SDK 程序集。

只有新功能还没有清单时，才在对应的 `*FileCopyWindow.cs` 文件中的 `CreateCopyItems` 方法内添加，例如：

```csharp
return new[]
{
    new CopyItem("Analytics", "Assets/BizzaWZ/Final/Sdk/Analytics"),
    new CopyItem("AnalyticsHelper.cs", "Assets/BizzaWZ/Final/Sdk/Analytics/AnalyticsHelper.cs", false)
};
```

目录项会递归复制其全部文件；文件项只复制单个文件。默认同时复制对应的 Unity `.meta` 文件。Required 项缺失时不会执行导出；导出会直接清空目标目录后复制。

复制内容选择遵循父项优先：已选中的目录项会覆盖其目标路径下、且未被 `excludes` 排除的子项；子项会显示“父项已包含”并不会重复规划或复制。父项明确排除的子项仍会独立处理。

SDK 和 IP 同时导出到同一个目标工程时，公共契约及设备程序集只保留一份。目标项目如果已有旧的 `Assets/other` 公共脚本，应先移除或迁移旧副本，避免同名全局类型重复。
