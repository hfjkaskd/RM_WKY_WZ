# 公共设备模块

该目录由 IP 检测工具和 Connect_SDK 共用，公共导出清单还会将同一层级的 UI、日志和 Android 构建依赖一起带出，包含：

- `DeviceInfoUtil`：首次注入 `appid/country/defaultCountry/fakeAndroidId`，之后统一复用单份设备快照；包含 GAID 单飞请求和 10 秒超时兜底。
- `DeviceNativeBridge`、`JavaBridgeUtils`、`DeviceUtils.java`：Android 设备采集链。
- `AndroidJavaMessageDispatcher`、事件基础类、`DeviceInfoLog`：回调与设备采集日志公共依赖。
- `CommonConfirmTipsPanel.cs` + `CommonConfirmTipsPanel.prefab`：SDK/IP 授权失败时共用的确认弹窗。
- `LogLogger.cs`：SDK/IP 共用日志入口；SDK 清单不再重复导出原位置的副本。
- `PostProcessBuild_Android.cs`：公共 Android 构建后处理；在 `BIZZA_REAL_WITHDRAW` 或 `USER_VERIFICATION_SDK` 的 Android 构建中生效。
- `_Bizza_BuildTemplate`：SDK 清单递归复制完整目录；IP 清单只复制非 Java 内容及 `BangsawanLog.java`、`DeviceUtils.java`、`UnityHelper.java`。

公共设备模块不读取宿主配置。定义 `WKY_SDK` 时，`DeviceNativeBridge` 通过无编译依赖的反射读取 `AccountModule.E_CountryType` 作为国家白名单；未定义时使用内置的完整 ISO 3166-1 alpha-2 国家/地区代码。Android Java 只接收最终白名单并保持原有判断逻辑。SDK 或独立 IP 在入口处调用一次 `DeviceInfoUtil.InitializeAsync(...)` 完成注入；后续模块只读取 `DeviceInfoUtil.Current`/`Data`。

`CommonPublicFileManifest.json` 中的 `@SHARED/...` 是公共目标标记：导出到同一个 Unity 项目时，SDK 与 IP 使用同一份公共目录，避免出现两份 `Bizza.Common.DeviceInfo` 程序集；导出到项目外的独立工具目录时，公共目录跟随当前目标目录，保证一个工具的全部内容都在目标目录内。复制窗口只清空当前功能目标目录；公共文件随当前清单整体复制并覆盖更新。
