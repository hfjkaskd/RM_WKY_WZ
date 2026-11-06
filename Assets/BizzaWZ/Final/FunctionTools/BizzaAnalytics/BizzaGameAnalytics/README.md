# BizzaGameAnalytics 接入说明

## 目标

`BizzaGameAnalytics` 是网赚/广告变现游戏的强类型业务打点层。它不使用反射或自建网络，
所有数据只通过底层 `BizzaAnalyticsAgent` 的公开接口处理：

- `BizzaAnalyticsAgent.Track`
- `BizzaAnalyticsAgent.UserProp`
- `BizzaAnalyticsAgent.MarkGameLoadStageStart`
- `BizzaAnalyticsAgent.MarkGameLoadStageComplete`
- `BizzaAnalyticsAgent.MarkGameLoadComplete`

SDK 不使用反射，也不直接发起网络请求。

配套文档：

- 真实游戏由 AI 实施接入：`Assets/BizzaAnalytics/BizzaGameAnalytics/AI接入文档.md`
- 用户正常体验、AI 分析 Editor 日志：`Assets/BizzaAnalytics/BizzaGameAnalytics/编辑器体验验收文档.md`

## 联调调用日志

Unity Editor 和勾选 `Development Build` 的测试包会输出轻量调用日志，正式 Release
构建会在编译时移除这些日志，不产生字符串拼接和 Console 开销。日志只包含事件名、
处理阶段、队列优先级及 `attempt_id` / `impression_id` / `request_id` 等关联 ID，
不会打印完整业务参数、配置 Token 或异常堆栈。

- `lifecycle_event=game_start/game_load_complete`：底层启动漏斗事件是否被接受及其 `launch_id`。
- `load_stage_event=game_load_stage_start/game_load_stage_complete`：加载阶段、顺序和配对 ID。
- `event_accepted`：事件已通过校验并进入封装内存队列，不代表服务器已收到。
- `event_handed_off`：事件已交给 `BizzaAnalytics`，最终网络结果由底层 SDK 负责。
- `event_sampled_out`：底层已 Ready，但当前设备被 `reporting_ratio` 采样排除。
- `event_rejected`：事件因参数、重复 ID 或队列已满而被拒绝。
- `submission_failed`：调用底层 API 时抛出异常。
- `bizza_state`：底层初始化状态或 `reporting_enabled` 发生变化。

## 最小接入

必须先通过 Unity 菜单 `Bizza → Analytics Config` 生成底层配置：

- 编辑器、测试包和正式包统一使用：`Assets/StreamingAssets/BizzaAnalytics.bytes`

当前工程统一配置 AppId 为 `coinstjam`。其他产品必须重新生成自己的配置。

AppId 已写入加密配置。SDK 不公开初始化接口：`BeforeSplashScreen` 自动启动底层；每次实际
调用 `Track` / `UserProp` 时也会检查状态，未初始化则内部启动，已初始化则直接处理。缺少对应
配置或配置无效时，底层会进入 `Disabled`，业务事件不会上报。

真实项目不要创建 Analytics Bootstrap，也不要调用初始化方法。默认性能采样周期为 60 秒。
所有接口必须从 Unity 主线程调用。`BizzaAnalytics` 不依赖项目的 `EventDefine` 或
`BizzaEventSystem`；`BIZZA_REAL_WITHDRAW` 是否定义由项目自身的业务代码决定，不是打点 SDK 的接入前置条件。

底层会在游戏最早期自动记录 `game_start`。每个加载阶段分别接入开始和完成：

```csharp
string profileStageId = BizzaGameAnalytics.TrackGameLoadStageStart("user_profile");
// 用户信息请求真实完成回调
BizzaGameAnalytics.TrackGameLoadStageComplete(profileStageId, success: true);

string resourceStageId = BizzaGameAnalytics.TrackGameLoadStageStart("remote_resource_download");
// 下载失败也要结束阶段，并传稳定错误码
BizzaGameAnalytics.TrackGameLoadStageComplete(
  resourceStageId,
  success: false,
  failureReason: "download_timeout");
```

开始接口返回的 `stageId` 必须保存并原样传给完成接口。并行加载使用各自的 `stageId`；
`stage_name` 使用稳定英文标识，不要拼接用户 ID、URL 或动态错误文本。

所有阶段结束、玩家可以进入游戏时，再在唯一回调中调用：

```csharp
BizzaGameAnalytics.TrackGameLoadComplete();
```

启动、阶段和最终完成事件共享 `launch_id`，数据端可据此计算玩家进入成功率和加载耗时。不要在登录开始、首场景
刚加载或任意单个资源完成时提前调用。仍存在未完成阶段时，最终完成调用会被拒绝。

网赚公共上下文在余额或提现阶段变化时更新，后续每个事件会自动携带：

```csharp
BizzaGameAnalytics.SetEconomyContext(
  currentBalance: 8.25,
  firstWithdrawalThreshold: 10.0,
  withdrawalStage: 0,
  balanceUnit: "COIN");
```

`balanceUnit` 同时适用于 `current_balance`、`first_withdrawal_threshold` 和
`stage_threshold_amount`，建议使用 `USD`、`CENT`、`COIN` 等稳定单位。

买量归因和 AB 分组：

```csharp
BizzaGameAnalytics.SetAttribution("mintegral", "campaign_2026_08", "US");
BizzaGameAnalytics.SetUserGroup("withdraw_v2");
```

## 常用业务调用

```csharp
BizzaGameAnalytics.TrackActivation();
BizzaGameAnalytics.TrackTutorialComplete();

string attemptId = BizzaGameAnalytics.TrackLevelStart("level_12", attempt: 2);
BizzaGameAnalytics.TrackLevelWin("level_12", attemptId);

BizzaGameAnalytics.TrackAdLoad(
  "level_complete", BizzaAdType.Rewarded, "max",
  BizzaAdLoadResult.Success, latencyMilliseconds: 320);
BizzaGameAnalytics.TrackAdShowRequest(
  "level_complete", BizzaAdType.Rewarded, "max", isReady: true);
BizzaGameAnalytics.TrackAdShow("level_complete", BizzaAdType.Rewarded, "max");
const string impressionId = "ad_impression_10001";
BizzaGameAnalytics.TrackAdImpression(
  "level_complete", BizzaAdType.Rewarded, "max", impressionId);
BizzaGameAnalytics.TrackAdRevenue(
  "level_complete", BizzaAdType.Rewarded, "max", 0.0123, "USD", impressionId);

BizzaGameAnalytics.TrackWithdrawalRequest("wd_10001", 10, "USD");
BizzaGameAnalytics.TrackWithdrawalResult("wd_10001", 10, "USD", success: true);
```

广告回调顺序通常为 `ad_load → ad_show_request → ad_show → ad_impression → ad_revenue`。
`ad_impression` 的 `impressionId` 必须来自广告 SDK 的同一展示实例且不能为空；
`ad_revenue` 在 SDK 无法提供稳定唯一 ID 时可以不传，提供 ID 时会按 ID 去重。
`currency` 必须是三位 ISO 4217 代码，`revenue` 使用该币种的主单位，不得传 eCPM。
提现 `requestId` 必须稳定且每次请求唯一，金额同样使用 `currency` 的主单位。

广告展示、收益、提现请求/结果及关卡结束接口返回 `bool`，仅表示本次数据通过校验并
进入本地队列，不代表服务端已经收到。SDK 会过滤当前进程中的近期重复 ID，服务端仍需
按 `impression_id`、`request_id` 做跨进程最终幂等。

## 事件清单

| 分类 | 事件 | 关键字段 |
|---|---|---|
| 进入漏斗 | `game_start` / `game_load_stage_start` / `game_load_stage_complete` / `game_load_complete` | `launch_id`、`stage_id`、阶段顺序/结果/耗时、总加载耗时 |
| 生命周期 | `app_start` | `session_id`、公共参数 |
| 会话 | `session_start` / `session_end` | `session_id`、时长、结束原因 |
| 首次漏斗 | `user_install` / `user_activate` / `tutorial_complete` | 安装时间、教程 ID |
| 页面/道具 | `page_open` / `page_close` / `item_use` | 页面、停留时长、道具、数量、来源 |
| 关卡 | `level_start` / `level_win` / `level_fail` / `level_quit` / `level_duration` | `attempt_id`、关卡、结果、时长 |
| 广告 | `ad_load` / `ad_show_request` / `ad_show` / `ad_impression` / `ad_revenue` | `impression_id`、广告位、类型、渠道、状态、收益 |
| 提现 | `withdrawal_stage_enter` / `withdrawal_request` / `withdrawal_result` | 阶段、金额、币种、结果 |
| 性能 | `fps` / `lag_state` / `crash` | FPS、卡顿分、慢帧率、异常摘要 |

自动用户属性：`game_version`、`device_info`、`country`、`install_time`、
`total_ipu`、`total_gametime`、`play_time`、`active_days`。归因接口设置
`network`、`campaign`，分组接口设置 `user_group`。

## 明确不由客户端上报的计算指标

以下指标由数据端基于原子事件计算：消耗、激活率、激活成本、ROI 与倍率、
留存、关卡通过率、平均通过次数、IPU、广告用户 IPU、eCPM、广告渗透率、
not-ready 比例、播放/加载比例、播放/展示比例、Crash 次数及 Crash-free 比例。

主要计算来源：

- DAU：按 `app_start` 用户去重；留存：`user_install` 分群后观察后续 `app_start`。
- 游戏进入成功率：同一时间窗口内，按 `launch_id` 匹配 `game_start` 与 `game_load_complete`；
  加载耗时使用 `game_load_complete.load_duration_ms`。
- 阶段成功率、失败位置和阶段耗时：按 `stage_name` 聚合，并使用 `stage_id` 配对阶段开始与完成；
  只有开始没有完成的阶段是潜在中断位置。
- 关卡通过率和尝试次数：使用 `level_start` 与结果事件；`level_duration` 只统计时长，不能再次计为结果。
- IPU/广告渗透：使用 `ad_impression`；not-ready：使用 `ad_show_request.is_ready=false`。
- 播放/加载和播放/展示：分别使用 `ad_show`、成功的 `ad_load`、`ad_impression`。
- 广告收入/eCPM：先按 `currency` 统一换算，再汇总 `ad_revenue.revenue` 与展示数。
- 启动次数和单次时长：分别使用 `session_start`、`session_end`。
- 托管异常的 Crash-free 会话率：使用所有事件携带的 `session_id`；原生 Crash 仍需原生方案。

## 性能策略

- 业务调用只写入内存队列；正常帧最多向 `BizzaAnalytics` 提交一个事件。
- 提现、广告收益/展示、关卡结果和崩溃使用高优先级队列。
- 暂停或退出时最多同步提交 32 条事件，避免在生命周期回调中形成大量磁盘 IO；恢复后继续提交剩余队列。
- FPS/卡顿默认 60 秒采样一次；慢帧阈值默认 50ms，卡顿分为
  `100 × (1 - 慢帧比例)`。
- 自动异常单次会话最多上报 3 条，消息最多 512 字符、堆栈最多 4096 字符。
- Unity 托管层无法保证原生崩溃退出前送达；完整原生 Crash 仍需平台崩溃方案。
- 操作系统在没有触发 Pause/Quit 回调时直接终止进程，可能丢失内存队列中的事件和最终 `session_end`。
