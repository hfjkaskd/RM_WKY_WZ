执行 IP检测 接入，直接修改目标 Unity 工程并完成验证。

源 SDK：`<源项目路径>`
目标工程：`<目标 Unity 项目路径>`
适配国家：`<目标 Unity 项目适配的国家>`

# 配置参数

- `AppId`：独立运行或公共 SDK 未提供应用标识时使用。组合接入时优先读取 `ChannelConfig.bytes` 注入 `DeviceInfoUtil` 的 AppId，不需要在初始化调用中重复传入。
- `WorkerUrl`：授权服务地址；默认：`https://iptokenclient.applebooks24.com/api/v1/decision`。
- `AuthTokenKey`：本地 Token 存储键，默认：`AUTH_TOKEN`。
- 提醒用户 安装好 UniTask、Newtonsoft.Json、Addressables、TextMeshPro、obfuz、UniTask.Addressables
- 用户提供的适配国家 AI后续要在 DeviceNativeBridge.cs 脚本的 SupportedCountryNames 字段中添加，保证国家的使用

# 工作流程

1. 先扫描目标工程，按以下顺序处理冲突：

   * `_Bizza_BuildTemplate` 和 `CommonPublic` 是允许直接复用的两个例外，不触发停止规则；
   * 如果目标项目中已经有 Assets 同级目录下的 `_Bizza_BuildTemplate` 文件夹，则复用现有模板并跳过 Java 脚本复制；
   * 如果目标项目中已经有 `CommonPublic` 文件夹，则复用现有目录并跳过 CommonPublic 复制；
   * 完成上述两个例外判断后，如果仍发现同一套 IP SDK、`UserInformationVerification` 程序集或其他同名脚本，立即停止并报告冲突，不覆盖。
   

2.  无冲突时完整迁移并保留全部 `.meta` 和 GUID：

   * `_Bizza_BuildTemplate/` → 目标根目录
   * `StreamingAssets/` → `Assets/StreamingAssets/`
   * 其余内容 → `Assets/SDK_UserVerification/`

   源包中的 `AuthConfig.bytes` 是配置模板，必须复制到目标工程并覆盖同名 `AuthConfig.bytes`。
   Unity 完成导入后，使用模块自带的 `AuthConfigEditorWindow` 或 `AuthConfigBinarySerializer` 读取该模板：

   * 组合接入时确认 `AppId` 与 `ChannelConfig.bytes` 中的应用标识一致；当前工程为 `coinstjam`；
   * 将 `WorkerUrl` 更新为本次用户提供的 HTTP 地址；用户没有提供时使用上面的默认地址；
   * 除 `AppId` 和 `WorkerUrl` 外，保留模板中的其他配置字段；
   * 使用 `AuthConfigBinarySerializer` 重新序列化并写回 `Assets/StreamingAssets/AuthConfig.bytes`，禁止把二进制文件当文本直接修改；
   * 写回后再次反序列化校验 `AppId` 和 `WorkerUrl`，校验失败则停止并报告错误。

具体参考
   ```
   目标项目/
├─ _Bizza_BuildTemplate/
│  └─ Android/
│     └─ unityLibrary/
├─ Assets/
│  ├─ StreamingAssets/
│  │  └─ AuthConfig.bytes
│  └─ SDK_UserVerification/
   ```

    需要对一个预制件 `CommonConfirmTipsPanel.prefab` 的 addressable key 进行补充为 "SDKPanel/CommonConfirmTipsPanel";


1. 程序集 `UserInformationVerification`，补齐必要引用；

    校验现有程序集名称；
    校验 Editor 和 Tests 引用的是 UserInformationVerification；
    禁止再创建第二个同名程序集。

2. Obfuz 缺失时通过 Git UPM 安装：`https://github.com/focus-creative-games/obfuz.git`

3. 默认按测试版接入，开启：

`DEBUG_MODE`
`USER_VERIFICATION_SDK`

默认关闭 `BIZZA_HTTP_AD`

6. 使用现有 `Boots_Flow.Init()` 初始化，只调用一次并等待完成。组合接入时先完成 WKY SDK 和公共 `DeviceInfoUtil` 初始化，再执行授权。授权 SDK 不再要求调用方传入 AppId。

参考调用方式
```
await Boots_Flow.Init();
```

   `Boots_Flow` 位于导出的 `CommonPublic` 中。目标项目不要直接调用 `AuthFlow`，也不要另建授权初始化入口。AppId 读取顺序为：宿主/公共 `DeviceInfoUtil` → `AuthConfig.bytes` 备用值。

7. 如果用户被拦截则不允许进入游戏，如果用户被允许进入游戏则允许进入游戏。

10. 文件迁移、依赖扫描和修改批量完成；全部修改后只启动一次 Unity，统一完成导入、依赖解析和编译验证。

 完成后报告：迁移文件、程序集、依赖、宏、初始化入口、编译结果和阻塞项。不要统计阶段耗时。
