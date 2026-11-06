# RM 与 WKY 接入记录

## 基线

| 项目 | 路径或提交 |
| --- | --- |
| 正式玩法来源 | F:/Coin Merge Fortune/RM/Recovery/UnityFoundation |
| 玩法 Git 基线 | 1a0a3686d87f29cad0dc38cf104346614be0114c |
| 框架来源 | C:/Users/pc/Desktop/WKY_FRAME/BizzaWZ |
| 框架 Git 基线 | 65a5160494926d5218b732a5088e734aa90653f6 |
| 最终交付目录 | C:/Users/pc/RM_WKY_WZ |
| 目标远端基线 | 6dab85aed94b86c5a53cf84e65b641010de62ea9 |
| 工作分支 | codex/rm-wky-pure-real |

按本地玩法工作树复制目标基线。玩法来源包含未提交的脚本、配置和关卡引导 Prefab；E 盘独立备份保存工程目录和来源状态，源工作树保持原位。框架全部来源文件的相对路径、字节数和 SHA-256 单独存入暂存清单。

## 已确定的接入要求

- 接入方向为框架植入玩法。玩法来源是独立 Git 工程，指定目标目录中的其余恢复资料属于原有内容。
- 两个既有 Android 玩法 APK 的 aapt 结果均为 Royal Match Core，包名为 com.royalmatch.core.recovery。玩法 Player Settings 无平台显式包名；目标使用既有 Android 交付身份验证 WKY 登录。框架源 Android 包名为 com.bangsawan.oceanshine。
- 用户确认目标保持单货币模式。使用原 SDK 序列化器将目标 singleCurrencyMode 从 false 改为 true，并回读验证。保留三个提现页面及其 .meta、资源注册和现有预加载。
- 玩法使用内置渲染管线，框架 Graphics 和 Quality 设置含 URP 引用，来源包清单未声明 URP。合并时保留玩法渲染，框架 UI 保持原 Overlay Canvas、相机条件和缩放配置，并实际验证材质、遮罩和键盘避让。
- 正式玩法当前支持第 1–100 关。正式入口已无体力门槛。普通库存接入保留锤子、箭矢、加农炮、小丑帽及其现有解锁条件、效果和局内限制。

## 当前验证

已做静态入口、包名与 Git 基线核对。使用原玩法 Android 资源筛选规则的独立验证工程通过 Unity 2022.3.62f3 Android 目标批处理导入及编译。完整工作树尚需运行验证，Android APK 和真机流程待验证。初次设备检查未发现在线 ADB 设备，后续检测到 Android 模拟器。起初 F 盘空闲约 60 MiB，用户随后选择 C 盘交付。

框架导入前已校验 GUID 和相对路径。两个工程无重复 GUID，唯一同路径文件是 Assets/Resources.meta，目标保留玩法版本。已对迁入的 6715 个框架来源文件执行 SHA-256 比对。框架本机构建报告工具设置未迁入。

Android 验证复用了玩法原构建的依赖筛选逻辑，选中 1726 个玩法资产、涵盖全部 100 关，序列化 Resources 路径无未解析项。验证工程另补加载页用到的原进度条图及其贴图。
## 桥接检查点

- 用原玩法的背景、Logo、MikadoBlack 字体和进度条素材适配框架加载 Prefab。淡入淡出以原加载器行为为参考，动画时长为本次适配值。启动、重试及切关复用同一页面，框架启动链仍调用一次 Boots_Flow.Init。
- 玩法桥接读取服务器当前关卡，并将局内存档绑定到关卡身份。接入了真实胜败和消除事件、四种道具的库存和激励广告入口、真实广告成功后的同棋盘单次复活。复活沿用玩法首档五步，重进时未收到广告成功回调的等待态按原失败结果收尾。
- InitWZ 使用同一常驻 EventSystem 服务两个 Canvas，保留玩法输入处理顺序。正式玩法场景关闭自身的重复 EventSystem。框架 RealGamePanel 保留业务按钮，移除样例道具入口，实际道具从原玩法 HUD 操作。框架 Canvas 渲染模式、参考分辨率和父级缩放未调整。弹窗键盘避让、安全区、不同宽高比和关闭防点穿仍需运行及真机验收。
- 复核 6715 个框架来源文件，其中 6708 个逐字节一致，另外七个为加载、复活入口、示例 UI、接入桥和启动场景适配。保留来源 Git 已跟踪的调试符号和旧安装包，保留 ChannelConfig.bytes、SDK 源码、程序集、原生文件和模板。
- Foundation 运行时和 Editor 程序集独立 Roslyn 编译通过。Unity Android 目标构建脚本程序集通过，实际 WKY SDK 和正式提现宏参与了该编译。现有框架附带 Newtonsoft 12，同时依赖 Newtonsoft 13，Unity 报告重复并选用 13。保留两个来源依赖，运行行为待验。
- 四种道具按锤子、箭矢、加农炮、小丑帽映射 GameProp_1 至 GameProp_4，解锁关卡分别为 8、14、17、19。原作用及免费教学保留。服务端对 GameProp_2 至 GameProp_4 广告位的实际响应，以及各道具库存、扣除、重复点击和跨关重进仍待验证。


## Unity 编辑器资源检查

Android 目标下的 Unity 2022.3.62f3 批处理检查通过。三个已启用场景按 InitWZ、GameScene、Empty 排序。InitWZ、GameScene 和加载 Prefab 均无 Missing Script。加载页的原背景、Logo、进度条素材和淡入淡出动画引用均已解析。InitWZ 常驻 EventSystem 启用，GameScene 内的重复实例禁用。验证使用编辑器静态场景加载，交互和真机仍待验收。
## 单货币及正式通关回执

- 目标配置已回读为 singleCurrencyMode=true。解密后 207 字节中只有零基偏移 79 的布尔值由 0 改为 1。原 SDK 序列化器对来源配置往返校验逐字节一致。三个提现 Prefab、对应 .meta、注册、加载以及页面脚本保留来源版本。框架原有单货币显示规则继续生效。
- 仅从 Android 宏列表移除 DEBUG_MODE，保留 WKY_SDK、BIZZA_REAL_WITHDRAW 及其他平台和 SDK 宏。
- 实际胜利继续由原胜利页调用通关接口。当前结算只允许认领一次请求，等待 SDK 应用并保存该次回执后，使用该回执中的服务器关卡号。服务器同号或回退均接受。失败和超时保持输入受限，不递增本地关卡或发起重复上报。跨进程中断后的服务端幂等性仍需实际联调。
- 对 6715 个框架来源文件重新计算 SHA-256。6705 个逐字节一致，另十个为已记录的七个界面/场景适配、两处结算接线和单货币配置。三个提现页面与对应 .meta 均在逐字节一致范围。
- 源码已在 C:/Users/pc/RM_WKY_WZ 检出，工作分支仍为 codex/rm-wky-pure-real。E 盘保留独立暂存及原玩法备份。正式 Android 复测沿用原框架的打包入口和玩法原资源筛选，构建及运行结果单独记录。