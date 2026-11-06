执行 WKY SDK 接入，直接修改目标 Unity 工程并完成验证。

源 SDK：`<源项目绝对路径，只读>`
目标工程：`<目标 Unity 工程绝对路径>`
原游戏：`<原游戏绝对路径，只读，用于核对国家和语言>`
applicationIdentifier：`<目标包名>`

【目标与最高优先级边界】

本任务只负责迁移现有 WKY SDK，并在目标项目中调用已有启动、广告和语言接口。不是 SDK 重构、SDK 修复或广告系统优化。

1. 源 SDK 全部只读，禁止修改。

2. 迁移到目标工程后的 SDK 也必须保持原样。唯一允许修改的 SDK 内容是：

   `AccountModule.E_CountryType`

   只允许追加原游戏支持、但该枚举缺少的国家；不得修改 `AccountModule` 的登录、请求、提现、回调及其他逻辑，不得重命名、删除、重排已有枚举值或改变已有数值。

3. 禁止修改：

   - `Boots_Flow`
   - `WKY_Flow`
   - 广告 SDK、聚合层和所有广告 Adapter
   - SDK 登录、账户、收益、提现、统计及回调机制
   - `DeviceNativeBridge`
   - `CommonConfirmTipsPanel` 内部逻辑
   - Android Java 脚本逻辑
   - SDK 配置格式和服务端参数

4. 如果编译或功能验证必须修改上述受保护内容才能继续，立即停止并报告原因、文件和报错，不得自行修复、绕过或另写替代实现。

【文件迁移】

1. 先扫描目标工程。若已有同一套 SDK、同名程序集或脚本，停止并报告冲突，不覆盖已有人工修改。

2. 无冲突时完整迁移并保留全部 `.meta` 和 GUID：

   - `_Bizza_BuildTemplate/` → 目标项目根目录
   - `Android/` → `Assets/Plugins/Android/`
   - `StreamingAssets/` → `Assets/StreamingAssets/`
   - 其余 SDK 内容 → `Assets/SDK_Cloud/`

   目录参考：

   目标项目/
   ├─ _Bizza_BuildTemplate/
   │  └─ Android/
   │     └─ unityLibrary/
   └─ Assets/
      ├─ Plugins/
      │  └─ Android/
      ├─ StreamingAssets/
      │  └─ ChannelConfig.bytes
      └─ SDK_Cloud/

3. `StreamingAssets` 使用迁移后的现有配置，同名文件按接入要求覆盖。禁止修改、猜测或编造 `ChannelConfig.bytes` 参数。

4. 为 `CommonConfirmTipsPanel.prefab` 配置 Addressable Key：

   `SDKPanel/CommonConfirmTipsPanel`

5. 创建目标工程运行时程序集 `WKY_SDK` 并补齐必要引用。程序集调整只用于让现有 SDK 正常编译，不得通过修改 SDK 脚本消除引用问题。

6. Obfuz 缺失时通过 Git UPM 安装：

   `https://github.com/focus-creative-games/obfuz.git`

   将宏 `WKY_SDK` 加入 Obfuz 设置，不修改 SDK 混淆标记和内部实现。

【依赖与项目配置】

1. 检查 Integration Manager 中是否存在：

   Bigo Ads、Bytedance、Fyber、InMobi、Mintegral、Moloco、Unity Ads、Vungle、Google。

   缺少或超出的渠道只在结果中报告，不擅自补装或删除。

2. 原样迁移 `AndroidManifest.xml`，仅将目标工程的 Android applicationIdentifier 设置为输入的目标包名。

3. 默认按测试版接入并开启：

   - `BIZZA_REAL_WITHDRAW`
   - `BIZZA_ENABLE_MAX`
   - `BIZZA_ENABLE_ADJUST`
   - `DEBUG_MODE`
   - `COMMONGAME`
   - `WKY_SDK`

   默认关闭：

   - `BIZZA_HTTP_AD`

【国家与语言同步】

1. 只读扫描原游戏现有国家枚举、国家配置和语言映射，整理原游戏实际支持的国家。

2. 对比目标工程中的 `AccountModule.E_CountryType`：

   - 已有国家保持不变。
   - 缺少的国家只追加到枚举末尾。
   - 不删除、重命名、重排已有成员。
   - 不修改 `AccountModule` 其他任何内容。

3. 不修改 `DeviceNativeBridge`。开启 `WKY_SDK` 后，现有逻辑应自动以 `AccountModule.E_CountryType` 为国家列表来源。

4. 服务器确认的 `AccountModule.CountryType` 是玩家国家的最终依据。不得在服务器结果返回前，仅根据设备、SIM、Locale 或原游戏本地配置确定最终游戏语言。

5. `await Boots_Flow.Init()` 成功完成后，读取服务器最终确定的 `AccountModule.CountryType`，复用原游戏已有的“国家 → 语言”映射和语言设置入口，把语言同步到目标游戏。

6. 国家不等于语言。不得直接把国家枚举名称当作语言代码；多个国家共用语言时，沿用原游戏现有映射，不新增臆测规则。

7. 如果 `CommonConfirmTipsPanel` 使用 `SelectedLanguage`，在目标游戏语言适配位置同步相同的语言代码，使游戏和公共提示面板保持一致；不得修改 `CommonConfirmTipsPanel` 脚本实现。

8. 服务器返回 `None`、未知国家或原游戏没有对应语言时，沿用目标游戏现有默认语言或既有兜底规则，并在结果中报告；不得修改 SDK 或虚构国家映射。

【启动流程接入】

1. 在目标项目现有启动或加载流程中只调用一次并等待：

   await Boots_Flow.Init();

2. 初始化顺序沿用 SDK 现有实现：

   配置 → 设备信息 → 登录/用户信息 → 广告/归因 → 主流程。

3. 禁止直接调用 `WKY_Flow.Start_Init_Flow()`，禁止另建 SDK 初始化入口，禁止修改 SDK 初始化顺序。

4. 玩法资源加载、主流程进入和游戏语言同步应放在现有框架接入位置，不得塞入 SDK 内部。

【广告接口接入】

1. 找到目标工程已有的统一广告控制器，只在目标工程业务层调用：

   - 激励广告：`BizzaSdk.Ad.ShowRewardAd`
   - 插屏广告：`BizzaSdk.Ad.ShowInterAd`

2. 根据目标工程现有代码选择正确重载和参数，保留原有广告位、埋点、奖励、收益和业务回调，不编造广告标识、奖励或触发规则。

3. 激励结果只在 SDK 现有成功回调后执行目标游戏的奖励、复活或道具效果；失败或取消时，只恢复目标游戏自身状态。

4. 重复点击、切关后的过期响应等，只允许在目标游戏业务接入层防护。禁止修改 SDK 回调派发、广告 Adapter、聚合逻辑、超时逻辑或收益逻辑。

5. 禁止直接调用或修改：

   - `AggregationAdSdk`
   - `OverseaPlatform.AdSdk`
   - MAX Adapter
   - HTTP Ad Adapter
   - 其他底层广告平台实现

【假网赚接口】

检查以下公开接口在迁移后仍然存在并保持公开：

- `OceanShineApplyWithdrawalRequestFake`
- `GetApplyWithdrawalRequestFake()`
- `Request_ApplyWithdrawalMoneyRollReqRequest()`

这些接口只允许由目标游戏业务层调用。不得修改、补写、删除、改为内部类型或调整其实现。

【验证与交付】

1. 文件迁移、依赖、宏、程序集和业务调用集中完成后，只启动一次 Unity，统一完成导入和编译验证。

2. 验证：

   - `Boots_Flow.Init()` 只执行一次并正常完成。
   - 服务器确认国家后，目标游戏语言正确同步。
   - `CommonConfirmTipsPanel` 与游戏使用同一语言。
   - 目标游戏通过公开接口调用激励和插屏广告。
   - 广告成功、失败和取消只影响目标游戏业务层，不修改 SDK。
   - 假网赚公开接口仍然存在。
   - Unity 编译无新增错误。

3. 开工前记录 Git 状态；完成后检查差异：

   - 源 SDK 不得出现任何修改。
   - 目标工程 SDK 内容除 `AccountModule.E_CountryType` 追加国家外，不得出现代码修改。
   - 不得覆盖目标工程已有人工修改。
   - 如果发现越界修改，停止并报告，不把任务标记为完成。

4. 最后简要报告：

   - 迁移文件
   - 依赖和程序集
   - 宏
   - 新增的国家枚举值
   - 服务器国家到游戏语言的映射位置
   - 初始化调用位置
   - 广告调用位置
   - applicationIdentifier
   - 编译及验证结果
   - 未验证项和阻塞项

不要统计阶段耗时，不要修改 SDK 来通过验收。