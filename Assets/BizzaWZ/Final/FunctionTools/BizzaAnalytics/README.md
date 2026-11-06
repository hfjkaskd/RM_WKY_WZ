# BizzaAnalytics 接入说明

## 1. 作用

`BizzaAnalytics` 是 Unity 底层上报 SDK。Agent 由运行时自动创建，不需要挂预制件或配置 Inspector。

所有运行参数（包括 `AppId`）都来自 `Assets/StreamingAssets` 下的加密配置。

## 2. 配置文件

在 Unity 菜单打开：

```text
Bizza → Analytics Config
```

填写 `App Id` 及其他参数后保存，生成唯一配置：

- `Assets/StreamingAssets/BizzaAnalytics.bytes`

运行时配置规则：

- 编辑器、测试包和正式包统一读取 `Assets/StreamingAssets/BizzaAnalytics.bytes`
- `DEBUG_MODE` 不再参与 BizzaAnalytics 配置选择；它仍可供项目其他 SDK 使用

当前工程配置：

| 配置 | AppId | reporting_ratio | verbose_log |
|---|---|---:|---|
| 统一 | `coinstjam` | `0.2` | `false` |

以上值只代表当前工程，迁移到其他产品时必须重新生成配置，禁止直接复用。

配置至少需要 `appId`、`ingestUrl`、`ingestToken`、`messageFrequency`、
`reportingRatio`；开启数据加密时还需要 `encryptionKeyBase64` 和 `encryptionKid`。

## 3. 自动初始化与上报

游戏最早期会自动记录一次 `game_start`，并开始读取配置。每次调用 `Track` 或 `UserProp`
时也会检查状态：未初始化则先启动初始化，已初始化则直接处理，因此业务层不需要反复判断。

```csharp
BizzaAnalyticsAgent.Track("event_name");
BizzaAnalyticsAgent.UserProp(properties);
```

SDK 不提供任何外部初始化接口。配置缺少或包含无效 AppId 时，自动初始化会直接失败。

初始化规则：

- `BeforeSplashScreen` 与首次上报触发的是同一套幂等初始化流程。
- AppId 缺失或格式无效时拒绝初始化并报错。
- 配置缺失、解密失败或字段无效时禁用 Analytics 并报错。
- 初始化完成前的事件最多缓存 256 条，成功后按原顺序处理。

## 4. 游戏进入漏斗

SDK 自动在 `BeforeSplashScreen` 阶段记录：

```text
game_start: launch_id, launch_time
```

每个真实加载阶段开始和结束时分别调用：

```csharp
string stageId = BizzaAnalyticsAgent.MarkGameLoadStageStart("user_profile");

BizzaAnalyticsAgent.MarkGameLoadStageComplete(
  stageId,
  success: true);
```

对应原子事件：

```text
game_load_stage_start:
  launch_id, stage_id, stage_name, stage_order, stage_start_time

game_load_stage_complete:
  launch_id, stage_id, stage_name, stage_order, stage_complete_time,
  stage_duration_ms, success, failure_reason（仅失败时）
```

`stageId` 必须使用开始接口的返回值，不能自行生成。支持并行阶段和同名阶段重复执行，单次启动
最多同时保留 64 个未完成阶段。建议使用稳定名称，例如 `user_profile`、`ip_check`、
`remote_resource_download`。

真实项目必须在“所有业务加载完成、玩家已经可以进入游戏”的唯一回调中调用一次：

```csharp
BizzaAnalyticsAgent.MarkGameLoadComplete();
```

对应事件为：

```text
game_load_complete: launch_id, load_complete_time, load_duration_ms
```

启动、阶段和最终完成事件使用同一个 `launch_id`。数据端据此计算进入成功率与加载耗时；客户端不直接上报比率。
存在未完成阶段时，最终完成事件会被拒绝；重复调用也不会产生重复事件。

## 5. 状态查看

```csharp
BizzaAnalyticsAgent.Instance.InitializationState;
BizzaAnalyticsAgent.Instance.IsReady;
BizzaAnalyticsAgent.Instance.IsReportingEnabled;
BizzaAnalyticsAgent.Instance.LastError;
```

Analytics 是辅助模块，不应阻塞登录或进入游戏等待 `Ready`。

## 6. 多项目迁移

迁移时复制整个 `Assets/BizzaAnalytics` 目录和对应 `.meta` 文件，然后为目标产品重新生成唯一的
加密配置 `BizzaAnalytics.bytes`。不要复用其他产品的 AppId、Token 或上报环境。

## 7. 安全说明

配置是加密二进制，主要用于避免明文配置和误操作。客户端中的 Token 仍可能被逆向提取。
