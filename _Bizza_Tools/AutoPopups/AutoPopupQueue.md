# 自动弹窗接入

当前项目：`E:\Puzzle_Project\BizzaWZ\BizzaWZ`。

统一入口：`AutoPopupQueue.Shared.Enqueue(...)`。

已经接入 `ExchangeRatePanel` 和 `DailyWithdrawPanel`。游戏会在主界面可展示、广告结束、结算与切关完成、引导和其他弹窗关闭后，按入队顺序显示。前一个队列弹窗的关闭动画结束后，才会打开下一个。

## 当前项目的游戏流程

- 使用 `FlowModule` 的结算/切关状态、`World` 游戏模式与暂停状态、`TransitionBlock` 遮罩判断展示时机，不依赖其他游戏的适配器。
- `OpenGameWinPanel`、`OpenGameLosePanel`、`OpenGameRevivePanel` 和 `LoadGameLevel` 开始时阻止自动弹窗；`GameStart`、成功复活以及 `GameMode_GamePlay` 场景/UI 初始化完成后恢复。
- 新玩法仍应通过上述框架入口通知生命周期；如果自行实现异步关卡加载，应在加载前调用 `FlowModule.SetAutomaticPopupFlowBlocked(true)`，就绪后调用 `FlowModule.GameStart()`。
- `UIModule` 跟踪手动弹窗的关闭动画。页面从打开列表移除后，仍等实际实例停用才展示下一个自动弹窗。
- 弹窗本身造成的游戏暂停不会停掉自己的内容超时。后台、广告、结算和切关等等待仍暂停计时。

## 无参数页面

业务条件满足时提交一次请求即可，`UIModule.Update` 会持续处理，不需要自己加延时或每帧重复提交。

```csharp
AutoPopupQueue.Shared.Enqueue(
    UIPageIds.YourPanel,
    key: $"YourPanel:{eventId}",
    isValid: () => eventStillValid,
    onShown: () =>
    {
        // 在这里记录“今天已显示”“本次活动已提示”等标记并保存。
    });
```

- `key`：同一业务事件使用相同标识。请求等待、加载和显示期间都会去重；不同事件需要不同标识。省略时使用页面 ID。
- `isValid`：可省略。会在等待结束、预加载完成和确认显示前复查；返回 `false` 表示请求已过期，会丢弃。广告、切关等暂时等待条件由统一调度处理，不应写成过期条件。
- `onShown`：可省略。页面位于前台且内容加载成功后调用一次。记录展示次数或日期应放在这里。
- 去重覆盖请求的整个等待和展示过程。一次性、每日一次等跨请求规则仍由业务保存标记并在 `isValid` 中判断。

## 带参数页面

对应 `UIPageBase<T>`，参数名使用 `data`：

```csharp
AutoPopupQueue.Shared.Enqueue(
    UIPageIds.YourPanel,
    data: info,
    key: $"YourPanel:{eventId}");
```

若业务数据在等待期间可能被修改，请传入值类型快照或独立副本。比例提升弹窗使用 `ExchangeRateInfo` 值类型快照，保留触发时的新旧数值。

## 页面打开后还要请求数据

页面实现 `IAutoPopupContent`：

```csharp
public bool ContentReady { get; private set; }
public bool ContentFailed { get; private set; }
```

每次 `OnOpen` 重置两个状态；数据成功绑定到界面后设置 `ContentReady = true`；请求或显示失败时设置 `ContentFailed = true`。队列会关闭失败页面并保留请求重试，尚未成功时不会调用 `onShown`。页面自身需要忽略已经关闭或属于上一次打开的异步回调，可参考 `DailyWithdrawPanel` 的 `contentVersion`。

未实现此接口的页面，在打开成功且处于前台时确认显示。

## 失败与生命周期

- 资源加载失败、打开异常或内容失败后，会按 1、2、4、8、16、30 秒退避重试，上限 30 秒。正常等待根据实时界面状态恢复。
- 每次打开都有独立 `PageOpenAttempt`，记录本次持有的具体页面实例。注册后初始化异常、参数错误和超时都会注销并强制停用该实例；不会按页面 ID 批量关闭，也不会把故障页面放回对象池。已存在页面的调用失败会保留其原归属。
- 无参数页面的初始化异常不再被页面基类吞掉，统一由 `UIModule` 回滚；手动调用 `OpenPage` 的调用者也会收到初始化异常，应按现有异步调用方式观察异常。
- 重试等待中的请求会让其他已就绪的请求先显示，避免一个故障页面阻塞整个队列。
- 资源预加载完成后会重新检查展示条件，避免加载期间刚好开始广告或出现其他弹窗。
- `onShown` 抛出异常会记录日志，已显示的弹窗不会因此重新播放。
- 普通切关保留待展示请求。切换账号或退出账号时调用 `AutoPopupQueue.Shared.Clear()`；当前项目创建新 `AccountModule` 时已执行清理。
- UI 释放或替换会使旧 UI 上未完成的尝试失效，符合业务条件的请求仍可退避重试。清空队列和切换账号则丢弃旧请求。
- `DailyWithdrawPanel` 单独持有自己的加载遮罩，关闭、失败或超时都会释放。晚回调不能撤掉其他请求的遮罩。
- 队列保存在内存中，不跨应用退出恢复。日期等已展示标记由业务存档保存；此前已经被旧代码写入的“当天已触发”记录不会自动撤销。

## 可配置超时

```csharp
AutoPopupQueue.Shared.ResourceTimeoutSeconds = 30f;
AutoPopupQueue.Shared.OpenTimeoutSeconds = 15f;
AutoPopupQueue.Shared.ContentTimeoutSeconds = 30f;
```

上面也是默认值，必须为正的有限秒数，每一阶段开始时读取配置。资源加载、打开流程、内容等待分别计时；等待用户正常关闭已展示页面不计超时。计时只累计该请求可推进的时间，广告、后台、结算、切关、引导或被其他弹窗遮住时暂停。应用暂停/恢复及广告生命周期事件会更新时钟版本，避免没有帧更新时，将整段后台或广告时间计入超时。

超时会立即失效本次打开令牌、清理其具体实例，沿用退避重试。资源加载中的外部等待可取消；不支持底层取消的 Addressables/HTTP 任务可能继续完成，但旧结果不能重新注册或确认该弹窗。初始化、资源完成后的回调及每次重试均检查独立尝试身份。

Unity 同步 `OnOpen` 占用主线程时，计时检查也无法运行。如果主线程恢复，会在继续确认前检查超时；无法强行中断永不返回的同步代码。异步初始化应使用页面内容状态配合有界请求，避免阻塞主线程。

## 验证

运行 `_Bizza_Tools/AutoPopups/Test-AutoPopups.ps1`，可通过 `-OutputDirectory` 指定临时输出目录，默认输出到 `Library/AutoPopupTests`。脚本使用本机 .NET 8 SDK，直接编译生产队列、页面生命周期、账户触发、每日提现，以及从 `UIModule` 提取的注册、激活、初始化分发、关闭和回滚代码。测试会在 `OnOpen` 内断言页面已注册且处于激活状态，再抛异常验证清理。

Unity 渲染、资源/实例获取、SDK、网络和存档依赖使用替身；资源/实例获取替身在取得结果后检查取消令牌，再调用真实 `InitPage`。异步调度在隔离测试中使用 `Task`，不修改真实账号或存档。

2026-09-18 同步验证：保留来源项目的 52 项回归，新增 8 项本项目适配及边界测试，共 60 项通过。使用 Unity 2022.3.62f3 自带 Roslyn 和当前项目 Bee 编译参数，运行时及编辑器程序集均编译通过（0 错误，存在项目警告）。这属于编译与隔离测试验证，未进行真机联网、真实资源加载或运行中的 Unity UI 验证。
