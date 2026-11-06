# Remote Group 配置工具

Unity 菜单：`Tools > Remote System > Remote Group Config`。

## 启用方式

远端分组功能由 `BIZZA_REMOTEGROUPDATA` 编译符号控制。需要使用的目标平台在 Player Settings 中启用该符号并重新打包；未启用的平台继续使用原有本地配置。若项目使用商业化配置，同时保留 `BIZZA_REAL_WITHDRAW`。

框架不预设项目服务器地址。填写 `Remote Root Url` 后，点击 `Export Encrypted Config`，将加密的运行时配置保存到 `Assets/Resources/RemoteGroupRuntimeConfig.bytes`。也可以使用下方的一键导出按钮同时保存运行时配置与内容。

## 导出内容

在窗口中设置分组名与权重。需要远端商业化参数时，指定 `ChannelABConfig`，确保每个国家的配置数量与分组数量相同。商业化配置可不指定，此时只导出策略。

`Export Strategy` 和 `一键导出运行时配置与内容` 都导出到：

```text
Assets/Editor/RemoteContent/
├─ Common/group.bytes
├─ {Country}/group.bytes                  # 开启 Country Group Enabled 时
├─ commercial/ChannelConfig{index}.bytes  # ChannelABConfig 中的 None 配置
└─ {Country}/commercial/ChannelConfig{index}.bytes
```

一键导出按钮还更新 `Assets/Resources/RemoteGroupRuntimeConfig.bytes`。两个按钮均不会自动上传服务器，也不会自动复制到 `StreamingAssets`。若启用了国家分组但没有指定商业化配置，会为 `AccountModule.E_CountryType` 中的所有具体国家导出同一份策略；指定了商业化配置时，按其中的具体国家导出。

## 发布与本地兜底

- 服务器：把 `Assets/Editor/RemoteContent` **里面**的目录和 `.bytes` 文件上传到 `Remote Root Url` 指向的根目录。
- 包内：把相同内容复制到 `Assets/StreamingAssets/RemoteGroup`，保留目录结构，不复制 `.meta` 文件。客户端远端策略失败时从此处读策略；商业化参数优先使用远端或沙盒缓存，失败时读此处的文件。
- 兼容旧包内目录：客户端也尝试 `Assets/StreamingAssets/RemoteContent`，商业化参数最后还尝试旧的 `Assets/StreamingAssets/RemoteAdStatisticsDefaults/{Country}`。
- `Default.bytes` 是独立的本地关卡数据，放在 `Assets/StreamingAssets/RemoteGroup/Default.bytes`，不由上述按钮生成。非 Default 分组的关卡数据需放在服务器的 `Common/gamedata/{GroupName}.bytes` 或对应国家目录。框架只缓存这些二进制，不解析具体关卡格式。

`ChannelABConfig` 当前导出 CHAB v1。若业务扩展商业化字段，需同步更新导出器与 `RemoteAdStatisticsConfigBinarySerializer`。

可通过 `Tools > Remote System > Remote Group Test` 检查远端路径、初始化和缓存；测试窗口仍会使用项目根目录 `RemoteUpload` 创建测试资源，与正式导出目录分开。
