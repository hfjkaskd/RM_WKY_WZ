# RemoteGroup 系统

远端分组由 `BIZZA_REMOTEGROUPDATA` 控制，项目按需在目标平台启用。框架提供策略下载、随包策略回退、商业化配置缓存、随包商业化配置回退和关卡二进制缓存。框架不解析具体游戏的关卡格式，也不内置项目服务器地址。

## 运行时顺序

1. 从 `Resources/RemoteGroupRuntimeConfig.bytes` 加载地址、目录、国家开关、超时和文件名前缀。
2. 若存在已完成且可用的分组记录，直接使用该分组的本地或沙盒关卡二进制；商业化参数仍会从缓存读取并在后台尝试刷新。
3. 首次分组先请求远端 `{Country}/group.bytes` 或 `Common/group.bytes`。请求或解析失败时读取 `StreamingAssets/RemoteGroup` 中同结构的策略，再尝试兼容目录 `StreamingAssets/RemoteContent`。随包策略无法读取时使用本地 Default 兜底。
4. 按策略权重选组；未正式完成分组前，同一国家和策略内容下复用选组结果。策略变化时可重新随机，已正式完成的分组记录仍优先复用。分组为 Default 时读取 `StreamingAssets/RemoteGroup/Default.bytes`；其他分组请求对应的 `gamedata/{GroupName}.bytes`。只有关卡二进制可用时才写入正式分组完成记录。
5. 商业化参数优先使用可解析的沙盒缓存，后台获取新版；无缓存时同步请求远端。远端不可用时读取随包 `RemoteGroup/{Country}/commercial/ChannelConfig{index}.bytes`，其次读取公共与兼容目录，最后退回 `ChannelConfig` 本地数值。关卡二进制缺失不会阻止已取得的商业化参数生效。

开启 `CountryGroupEnabled` 或 `CountryLevelEnabled` 且国家仍为 `None` 时，不请求远端，使用本地 Default。两个开关都关闭时，`None` 使用公共策略。`CountryLevelEnabled` 仅控制非 Default 关卡文件是否使用国家目录。

## 文件布局

```text
Assets/Editor/RemoteContent/                # 编辑器导出，手工上传或复制
  Common/group.bytes
  {Country}/group.bytes
  commercial/ChannelConfig{index}.bytes
  {Country}/commercial/ChannelConfig{index}.bytes

Assets/StreamingAssets/RemoteGroup/          # 随包本地兜底
  Default.bytes                             # 业务自行提供的本地关卡数据
  Common/group.bytes
  {Country}/group.bytes
  commercial/ChannelConfig{index}.bytes
  {Country}/commercial/ChannelConfig{index}.bytes
```

服务器目录结构与 `Editor/RemoteContent` 的内部结构相同。非 Default 关卡二进制由业务导出到远端 `Common/gamedata/{GroupName}.bytes` 或 `{Country}/gamedata/{GroupName}.bytes`。下载后缓存于 `Application.persistentDataPath/RemoteGroup`。

编辑器导出与项目启用步骤见 [RemoteGroupConfigWindow.md](Editor/RemoteGroupConfigWindow.md)。测试入口为 `Tools > Remote System > Remote Group Test`；测试窗口生成的关卡样例只用于验证路径和缓存，不是正式业务关卡内容。
