# BizzaGameAnalytics 真实项目 AI 接入文档

## 1. 文档用途

本文档用于让 AI 在正式 Unity 游戏中接入 `BizzaGameAnalytics`。接入目标是把游戏已有的
生命周期、关卡、广告、提现等真实业务回调连接到强类型接口，不重新设计统计系统。

接入完成不等于服务端已经收到数据。AI 必须完成代码和编译检查；Unity 真机运行、后台数据
核对仍需人工或具备对应环境的测试流程完成。

## 2. 接入边界

必须遵守：

- `Assets/BizzaAnalytics` 是完整打点 SDK 目录，接入时按第三方包只读处理。
- `Assets/BizzaAnalytics/BizzaGameAnalytics` 是业务打点封装，禁止为适配单个游戏而修改事件协议。
- 正式游戏只在现有业务代码或一个轻量适配类中调用 `BizzaGameAnalytics` 的公开接口。
- 禁止绕过封装层直接调用网络、写上报文件或接入另一套统计 SDK。
- 禁止使用反射、`dynamic`、字符串方法名或运行时扫描自动寻找业务回调。
- 禁止自行上报 ROI、留存率、通过率、IPU、eCPM 等计算指标。
- 不得虚构 `appId`、广告 `impressionId`、提现 `requestId`、币种或收益金额。
- 不为理论上的极端情况引入数据库、复杂持久化队列或后台线程。

封装层最终只使用以下底层接口：

- `BizzaAnalyticsAgent.Track`
- `BizzaAnalyticsAgent.UserProp`
- `BizzaAnalyticsAgent.MarkGameLoadComplete`

## 3. 需要带入真实项目的文件

将以下目录保持原结构复制到真实项目：

```text
Assets/BizzaAnalytics/
Assets/StreamingAssets/BizzaAnalytics.bytes
```

同时复制对应的 Unity `.meta` 文件，避免 GUID 变化。统一配置包含 AppId，不得使用其他产品的
AppId、环境或授权信息。

配置规则：

- 编辑器、测试包和正式包统一读取 `BizzaAnalytics.bytes`。
- `DEBUG_MODE` 不参与 BizzaAnalytics 配置选择。
- 缺少对应配置或配置无效时，底层会进入 `Disabled`，不能把这种情况当作上报成功。
- 当前模板工程统一 AppId 为 `coinstjam`；接入其他产品时必须替换为该产品配置，不能沿用模板值。
- `BizzaAnalytics` 不依赖项目的 `EventDefine` 或 `BizzaEventSystem`；`BIZZA_REAL_WITHDRAW` 仅由项目自身
  的业务代码决定，不是打点 SDK 的接入前置条件。

## 4. 自动启动和进入游戏接入

SDK 不公开初始化接口。底层在 Unity `BeforeSplashScreen` 阶段自动启动；每次实际调用
`Track` / `UserProp` 时也会内部检查状态，未初始化则启动初始化，已初始化则直接处理。
AppId 只能来自加密配置，真实项目不要创建 Analytics Bootstrap 或编写初始化调用。配置缺少
或包含无效 AppId 时，初始化直接失败。

SDK 会在 Unity `BeforeSplashScreen` 阶段自动产生一次 `game_start`。AI 必须定位游戏真正的
加载流程，并在每个阶段的真实开始和结束回调中配对调用：

```csharp
private string _profileLoadStageId;

public void OnProfileRequestStarted()
{
  _profileLoadStageId =
    BizzaGameAnalytics.TrackGameLoadStageStart("user_profile");
}

public void OnProfileRequestFinished(bool success, string errorCode)
{
  if (!string.IsNullOrEmpty(_profileLoadStageId))
  {
    BizzaGameAnalytics.TrackGameLoadStageComplete(
      _profileLoadStageId,
      success,
      success ? null : errorCode);
    _profileLoadStageId = null;
  }
}
```

至少检查用户信息、IP 检测、远端配置、账号登录、资源版本检查和远端资源下载等当前项目真实
存在的流程，不得为了凑事件虚构阶段。并行加载必须分别保存各自的 `stageId`；同一阶段重试时
重新调用开始接口，不能复用旧 ID。`stage_name` 使用稳定英文标识，错误原因使用稳定错误码。

随后定位游戏真正的“所有加载完成且玩家可以进入游戏”回调，并只调用一次：

```csharp
public void OnAllGameLoadingCompleted()
{
  BizzaGameAnalytics.TrackGameLoadComplete();
}
```

不要在启动场景开始、登录开始、某个资源包完成或进度条尚未结束时提前调用。存在未结束阶段
时最终完成调用会被拒绝。所有事件共享 `launch_id`，每个阶段通过 `stage_id` 配对；进入成功率、
中断阶段和加载耗时由数据端计算。

SDK 自动处理：

- `game_start`（最早期）；
- `user_install`；
- `app_start`；
- `session_start` / `session_end`；
- 初始及累计用户属性；
- FPS 和卡顿采样；
- 有数量限制的 Unity 托管异常采集；
- Pause、Resume、Quit 生命周期。

业务层不要重复实现或重复调用这些自动事件。

## 5. 公共属性和经济上下文

### 5.1 归因和分组

获得真实归因结果后调用；没有值的字段传 `null`，不要传 `unknown` 伪造数据。

```csharp
BizzaGameAnalytics.SetAttribution(
  network: attributionNetwork,
  campaign: campaignName,
  country: countryCode);

BizzaGameAnalytics.SetUserGroup(abGroupName);
```

### 5.2 余额和提现阶段

登录完成、余额变化或提现阶段变化后更新。`balanceUnit` 是游戏余额单位，可以是 `COIN`、
`CENT` 或 `USD`；它不一定等于提现法币 `currency`。

```csharp
bool accepted = BizzaGameAnalytics.SetEconomyContext(
  currentBalance: currentBalance,
  firstWithdrawalThreshold: firstWithdrawalThreshold,
  withdrawalStage: currentWithdrawalStage,
  balanceUnit: "COIN");
```

金额和门槛必须使用同一 `balanceUnit`。余额、门槛或阶段改变后应再次调用，避免后续事件携带
过期快照。

## 6. 首次漏斗接入

`TrackActivation` 应在产品定义的“用户首次有效启动/完成必要初始化”时调用。SDK 会保存激活
状态，重复调用会被拒绝。

```csharp
BizzaGameAnalytics.TrackActivation();
```

教程真正完成时调用一次。若游戏存在多个独立教程，使用稳定的不同 `tutorialId`。

```csharp
BizzaGameAnalytics.TrackTutorialComplete("main_tutorial");
```

业务层需要避免同一个教程完成回调重复调用。

## 7. 页面和道具接入

页面显示完成时调用 `TrackPageOpen`，页面隐藏或关闭时使用完全相同的 `pageName` 调用
`TrackPageClose`。

```csharp
BizzaGameAnalytics.TrackPageOpen("withdraw_page");
BizzaGameAnalytics.TrackPageClose("withdraw_page");
```

真实消耗道具后调用：

```csharp
BizzaGameAnalytics.TrackItemUse(
  itemId: itemId,
  itemType: itemType,
  quantity: quantity,
  source: source);
```

不要把仅仅选中、预览或购买但尚未消耗的动作记录为 `item_use`。

## 8. 关卡接入

关卡正式开始时保存返回的 `attemptId`。胜利、失败或退出必须带回同一个 ID，不能重新生成，
也不能使用关卡编号代替。

```csharp
private string _activeAttemptId;

public void OnLevelStarted(string levelId, int attemptNumber)
{
  _activeAttemptId = BizzaGameAnalytics.TrackLevelStart(levelId, attemptNumber);
}

public void OnLevelWon(string levelId)
{
  if (BizzaGameAnalytics.TrackLevelWin(levelId, _activeAttemptId))
  {
    _activeAttemptId = null;
  }
}

public void OnLevelFailed(string levelId, string reason)
{
  if (BizzaGameAnalytics.TrackLevelFail(levelId, _activeAttemptId, reason))
  {
    _activeAttemptId = null;
  }
}

public void OnLevelQuit(string levelId, string reason)
{
  if (BizzaGameAnalytics.TrackLevelQuit(levelId, _activeAttemptId, reason))
  {
    _activeAttemptId = null;
  }
}
```

接入要求：

- 一次开始只能对应一种结束结果。
- 延迟回调必须携带开始时保存的 `attemptId`。
- `TrackLevelStart` 返回空字符串时，不得继续伪造结束事件。
- 结果事件已经包含 `duration_seconds`；`level_duration` 仅用于时长统计，数据端不能再次把它
  当作胜负结果计数。

## 9. 广告接入

调用位置必须来自广告 SDK 的真实回调，不得根据按钮点击自行推断展示或收入。

推荐顺序：

```text
ad_load → ad_show_request → ad_show → ad_impression → ad_revenue
```

### 9.1 加载结果

```csharp
BizzaGameAnalytics.TrackAdLoad(
  placement,
  BizzaAdType.Rewarded,
  adNetwork,
  loadSucceeded ? BizzaAdLoadResult.Success : BizzaAdLoadResult.Failed,
  latencyMilliseconds,
  errorCode);
```

### 9.2 播放请求和开始播放

用户或业务尝试播放时，无论是否 Ready 都调用：

```csharp
BizzaGameAnalytics.TrackAdShowRequest(
  placement,
  BizzaAdType.Rewarded,
  adNetwork,
  isReady);
```

只有广告 SDK 确认开始播放后调用：

```csharp
BizzaGameAnalytics.TrackAdShow(
  placement,
  BizzaAdType.Rewarded,
  adNetwork);
```

### 9.3 展示和收益

`ad_impression` 的 `impressionId` 必须来自广告 SDK 的同一展示实例。收益回调能取得
同一 ID 时应保持一致；SDK 无法提供稳定唯一 ID 时，`ad_revenue` 可以省略该参数。

```csharp
BizzaGameAnalytics.TrackAdImpression(
  placement,
  BizzaAdType.Rewarded,
  adNetwork,
  impressionId);

BizzaGameAnalytics.TrackAdRevenue(
  placement,
  BizzaAdType.Rewarded,
  adNetwork,
  revenue,
  currency,
  impressionId);
```

接入要求：

- `revenue` 使用 `currency` 的主货币单位，不能传 eCPM。
- `currency` 使用广告 SDK 返回的三位大写币种代码，例如 `USD`。
- 不要用时间戳临时生成 `impressionId`；广告 SDK 没有提供时不发送 `ad_impression`，
  `ad_revenue` 则可以省略该参数。
- 同一个展示实例的 impression 和 revenue 回调各调用一次。
- 客户端只做当前进程近期去重，服务端仍需按 `impression_id` 最终幂等。

### 9.4 广告位字段一致性

广告接入必须区分业务广告位和 SDK 广告位：

- 业务广告位：调用方传入的 `ShowAdArgs.adPos`，例如 `ad.revive`，用于一次展示链路的业务归因。
- SDK 广告位：MAX 或其他 SDK 回调的 `AdInfo.Placement`、广告单元 ID 等，用于 SDK 加载和网络识别。

一次实际展示必须在适配器中保存业务广告位，不要在不同回调中重新从 SDK 回调对象解析：

```text
ad_show_request.placement = ad_show.placement
                       = ad_impression.placement
                       = ad_revenue.placement
```

当广告 SDK 能提供稳定唯一 ID 时，`ad_impression` 和 `ad_revenue` 必须使用同一个展示实例的
`impressionId`；无法提供时只跳过 `ad_impression`，收益仍可上报。预加载阶段没有业务
场景，`ad_load` 可以使用稳定的 SDK 广告位或广告单元 ID；不能用 `ad_load` 的值覆盖展示链路
中的业务广告位。编辑器中 `AdInfo.Placement` 为空是真实可能情况，真机中即使有值，也不能替代
业务广告位。

## 10. 提现接入

提现 `requestId` 必须来自业务后端或稳定的提现订单系统。请求和结果必须使用同一个
`requestId`、`amount` 和 `currency`。

```csharp
BizzaGameAnalytics.TrackWithdrawalStageEnter(
  stage: currentStage,
  stageThresholdAmount: currentStageThreshold);

BizzaGameAnalytics.TrackWithdrawalRequest(
  requestId,
  amount,
  currency);

BizzaGameAnalytics.TrackWithdrawalResult(
  requestId,
  amount,
  currency,
  success,
  failureReason);
```

接入要求：

- 进入提现阶段前先设置有效经济上下文。
- `currency` 是提现法币代码，不能传 `COIN` 等游戏余额单位。
- `amount` 使用法币主单位。
- 结果事件只能来自真实后端结果，不能在按钮点击时提前上报成功。
- 失败时传稳定、可聚合的失败原因或错误码，不要传大段服务器响应。
- 服务端必须按 `request_id` 做跨进程最终幂等和金额、币种一致性校验。

## 11. 异常接入

SDK 默认自动捕获有限数量的 Unity 托管异常。业务已经捕获且不会继续抛出的关键异常，可以
主动调用：

```csharp
try
{
  RunCriticalGameFlow();
}
catch (Exception exception)
{
  BizzaGameAnalytics.TrackException(exception, isFatal: false);
  throw;
}
```

不要同时在多个位置对同一异常重复上报。Unity 托管层无法保证原生崩溃、系统强杀或未触发
Pause/Quit 时的最终事件送达；完整原生崩溃统计仍需平台方案。

## 12. 返回值语义

- 打点接口返回 `true`：参数通过校验并进入封装队列。
- `TrackGameLoadStageStart` 返回非空字符串：阶段开始已接受，应保存为该次阶段的 `stageId`。
- `TrackGameLoadStageComplete == true`：阶段完成已接受，同一个 `stageId` 不能再次完成。
- `TrackLevelStart` 返回非空字符串：关卡开始事件进入队列，并生成有效 `attemptId`。
- 返回 `false` 或空字符串：本次调用未被接受，业务可以记录或排查，但不能无限重试。
- 以上返回值都不是服务端 ACK。

## 13. 联调日志

Unity Editor 和 Development Build 会输出：

- `lifecycle_event=game_start`
- `load_stage_event=game_load_stage_start`
- `load_stage_event=game_load_stage_complete`
- `lifecycle_event=game_load_complete`
- `bizza_state`
- `event_accepted`
- `event_handed_off`
- `event_sampled_out`
- `event_rejected`
- `submission_failed`

含义：

- `event_accepted`：已进入封装内存队列。
- `event_handed_off`：已交给底层 `BizzaAnalytics`，不代表服务器已接收。
- `event_sampled_out`：底层 Ready，但设备被 `reporting_ratio` 排除。
- `event_rejected`：参数、重复 ID 或队列问题导致拒绝。

广告联调时，应按同一次展示的业务广告位和 `impression_id` 检查字段一致性，不要只按日志行号
判断先后。关键事件可能因队列优先级在 `event_handed_off` 日志中顺序不同；这不代表业务回调
顺序改变。服务端是否收到数据仍需查看后台或网络结果。

正式 Release 中这些联调日志调用会被条件编译移除。不要为了看日志在正式包长期定义
`DEVELOPMENT_BUILD`。

## 14. AI 实施步骤

AI 接入真实项目时按以下顺序执行：

0. 先读取 `git status` 并保护用户已有修改；只有用户明确要求时，才在接入前提交本地内容。
1. 阅读本文件、封装层 `README.md` 和所有公开接口，禁止先改代码。
2. 扫描真实项目，列出启动加载阶段及其真实开始/结束回调，再定位总加载完成入口、关卡状态机、
   广告 SDK 回调、提现服务回调和页面管理器。
3. 先输出“业务回调 → 打点 API → 文件和方法”的接入映射。
4. 确认统一配置属于本产品且内置真实 AppId，并确认余额单位、法币单位和 ID 来源；未知信息不得猜测。
5. 优先在现有业务回调中增加直接调用；只有调用过于分散时才创建一个轻量适配类。
6. 为每个真实加载阶段配对接入 `TrackGameLoadStageStart/Complete`，保存各自 `stageId`；再把
   `TrackGameLoadComplete` 接到唯一的总加载完成回调。
7. 保存并传递关卡 `attemptId`、广告 `impressionId` 和提现 `requestId`。
8. 不修改事件名、参数名、底层 SDK 或封装 SDK。广告接入必须确认业务广告位从请求参数传到
   实际广告适配器，并在 show、impression、revenue 回调中复用；不得直接使用 SDK 的
   `AdInfo.Placement` 覆盖业务广告位。
9. 执行 C# 编译和项目已有测试。
10. 在 Editor/Development Build 根据调用日志检查事件路径、广告位一致性和 ID 复用；Editor
    验证通过后，仍需使用真机验证真实广告 SDK 回调。
11. 输出修改文件、调用映射、编译结果和尚未真机/后台验证的部分。

完成代码接入后，继续按照 `Assets/BizzaAnalytics/BizzaGameAnalytics/编辑器体验验收文档.md` 执行一次
Unity Editor 正常体验验收，由 AI 分析本轮 `Editor.log`，用户无需手动调用打点接口。

## 15. 真实项目验收清单

- [ ] 统一配置属于当前产品、内置正确 AppId 且可解析。
- [ ] 没有在业务代码中硬编码 AppId。
- [ ] 启动时自动产生一次 `game_start`。
- [ ] 每个真实加载阶段均产生配对的 start/complete，`stage_id` 一致且阶段耗时非负。
- [ ] 并行阶段分别保存 ID，重试不会复用已完成的旧 ID。
- [ ] 失败阶段上报 `success=false` 和稳定错误码。
- [ ] 所有加载完成后产生一次 `game_load_complete`，且与 `game_start` 的 `launch_id` 一致。
- [ ] `Loading → Ready` 可观察；采样排除能看到 `event_sampled_out`。
- [ ] 激活和教程完成使用真实业务时机。
- [ ] 页面打开和关闭名称一致。
- [ ] 每个关卡结果携带对应的 `attemptId`。
- [ ] 旧关卡延迟回调不会结束新 attempt。
- [ ] 广告事件来自真实 SDK 回调。
- [ ] `ad_show_request` 使用业务请求传入的广告位；预加载 `ad_load` 使用独立稳定的 SDK 广告位或广告单元 ID。
- [ ] 同一次展示的 `ad_show_request`、`ad_show`、`ad_impression`、`ad_revenue` 的业务 `placement` 一致。
- [ ] impression 和 revenue 使用同一个真实 `impressionId`。
- [ ] 广告收益不是 eCPM，币种和单位正确。
- [ ] 提现请求和结果使用同一个后端 `requestId`、金额和币种。
- [ ] 提现成功只在后端确认成功后上报。
- [ ] 没有直接上报 ROI、留存、通过率、IPU、eCPM 等计算指标。
- [ ] 没有修改 `Assets/BizzaAnalytics` 内的 SDK 实现和事件协议。
- [ ] 编译 0 错误，新增警告已解释或处理。
- [ ] Android 真机无明显打点相关主线程尖峰。
- [ ] Bizza 后台已核对至少一条关卡、广告展示、广告收益和提现完整链路。

## 16. 可直接交给 AI 的执行提示词

```text
请按照项目中的以下文档完成正式游戏打点接入：

Assets/BizzaAnalytics/BizzaGameAnalytics/AI接入文档.md
Assets/BizzaAnalytics/BizzaGameAnalytics/README.md

完整 SDK：Assets/BizzaAnalytics，接入时禁止修改。
业务封装：Assets/BizzaAnalytics/BizzaGameAnalytics，禁止为单个项目修改事件协议或内部实现。

先只读扫描项目并建立以下映射：
1. 用户信息、IP 检测、远端配置、登录、版本检查和资源下载等真实加载阶段的开始/结束回调，
   以及玩家可以进入游戏的唯一总完成回调；
2. 激活和教程完成时机；
3. 页面打开/关闭入口；
4. 道具真实消耗入口；
5. 关卡开始、胜利、失败、退出回调；
6. 广告加载、播放请求、开始播放、impression、revenue 回调；
7. 提现阶段、请求和后端结果回调；
8. 归因、AB 分组、余额和提现阶段数据来源。

建立映射后直接实施最小接入。确认正式/测试加密配置属于当前产品且包含正确 AppId；不得在
业务代码中硬编码或虚构 appId、impressionId、requestId、金额、币种或余额单位。发现这些
信息缺失时，完成其他可确定部分并明确列为阻塞项。

广告接入必须区分业务广告位和 SDK 广告位：`ShowAdArgs.adPos` 用于
`ad_show_request`、`ad_show`、`ad_impression`、`ad_revenue` 的同一次展示链路；MAX 或其他
SDK 的 `AdInfo.Placement`、广告单元 ID 仅用于加载和网络识别，不能覆盖业务广告位。
预加载没有业务场景时，`ad_load` 使用稳定的 SDK 广告位即可。`ad_impression` 和 `ad_revenue`
必须复用同一个真实 `impressionId`。

优先修改现有业务回调，只在必要时增加一个轻量适配类。禁止反射、直接联网、直接调用其他
统计 SDK、重复实现自动启动事件、修改 SDK、修改事件名或上报计算指标。必须为每个真实加载阶段
配对调用 BizzaGameAnalytics.TrackGameLoadStageStart/Complete，保存各自返回的 stageId；并把
BizzaGameAnalytics.TrackGameLoadComplete() 接到真实的全部加载完成回调，单次启动只调用一次。

完成后执行项目编译，并输出：
- 修改文件绝对路径；
- 每个业务回调对应的打点接口；
- 编译错误和警告数；
- Editor/Development Build 日志验证结果；
- 尚未完成的真机和 Bizza 后台验收项。
```
