# RM 与 WKY 接入记录

## 基线

| 项目 | 路径或提交 |
| --- | --- |
| 正式玩法来源 | F:/Coin Merge Fortune/RM/Recovery/UnityFoundation |
| 玩法 Git 基线 | 1a0a3686d87f29cad0dc38cf104346614be0114c |
| 框架来源 | C:/Users/pc/Desktop/WKY_FRAME/BizzaWZ |
| 框架 Git 基线 | 65a5160494926d5218b732a5088e734aa90653f6 |
| 指定目标目录 | F:/Coin Merge Fortune/RM |
| 目标远端基线 | 6dab85aed94b86c5a53cf84e65b641010de62ea9 |
| 工作分支 | codex/rm-wky-pure-real |

按本地玩法工作树复制目标基线。玩法来源包含未提交的脚本、配置和关卡引导 Prefab；E 盘独立备份保存工程目录和来源状态，源工作树保持原位。框架全部来源文件的相对路径、字节数和 SHA-256 单独存入暂存清单。

## 已确定的接入要求

- 接入方向为框架植入玩法。玩法来源是独立 Git 工程，指定目标目录中的其余恢复资料属于原有内容。
- 两个既有 Android 玩法 APK 的 aapt 结果均为 Royal Match Core，包名为 com.royalmatch.core.recovery。玩法 Player Settings 无平台显式包名；目标使用既有 Android 交付身份验证 WKY 登录。框架源 Android 包名为 com.bangsawan.oceanshine。
- 用户已选择纯真提现。源配置 singleCurrencyMode 为 false。纯真模式涉及单字段配置、三个页面 Prefab 与各自 .meta，以及相关非 SDK 资源注册、预加载和入口。待确认具体例外后实施。
- 玩法使用内置渲染管线，框架 Graphics 和 Quality 设置含 URP 引用，来源包清单未声明 URP。合并时保留玩法渲染，框架 UI 保持原 Overlay Canvas、相机条件和缩放配置，并实际验证材质、遮罩和键盘避让。
- 正式玩法当前支持第 1–100 关。正式入口已无体力门槛。普通库存接入保留锤子、箭矢、加农炮、小丑帽及其现有解锁条件、效果和局内限制。

## 当前验证

已做静态入口、包名与 Git 基线核对。尚待目标工程编译、运行、Android 构建及真机验证。初次设备检查未发现在线 ADB 设备。最终目录所在 F 盘空闲约 60 MiB，暂在 E 盘准备。

框架导入前已校验 GUID 和相对路径。两个工程无重复 GUID，唯一同路径文件是 Assets/Resources.meta，目标保留玩法版本。已对迁入的 6715 个框架来源文件执行 SHA-256 比对。框架本机构建报告工具设置未迁入。

Android 验证复用了玩法原构建的依赖筛选逻辑，选中 1726 个玩法资产、涵盖全部 100 关，序列化 Resources 路径无未解析项。验证工程另补加载页用到的原进度条图及其贴图。