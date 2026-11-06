using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

internal static partial class Program
{
    private static int passed;
    private static AutoPopupQueue Queue => AutoPopupQueue.Shared;
    private static UIModule UI => UIModule.Instance;
    private static UserInfo Info(double rate = 0.0003, double balance = 100, double available = 3) =>
        new() { Os_Tra = rate, Balance = balance, Os_Ewl = available };
    private static AccountModule Reset()
    {
        Queue.Clear();
        Queue.ResourceTimeoutSeconds = 30; Queue.OpenTimeoutSeconds = 15; Queue.ContentTimeoutSeconds = 30;
        UIModule.Instance = new UIModule();
        AccountModule.Instance = new AccountModule();
        FlowModule.SetAutomaticPopupFlowBlocked(false);
        World.Current = new World();
        TransitionBlock.Instance = null;
        RewardItemCollectFlow.CanPresentUI = true;
        TeachUtil.IsTeach = false;
        Time.unscaledTime = 0;
        SaveDataUtils.GameData = new GameSaveData();
        SaveDataUtils.Saves = 0;
        Debug.Errors.Clear();
        TaskExtensions.Tasks.Clear();
        LoadingBlock.Reasons.Clear();
        AccountModule.CountryType = AccountModule.E_CountryType.US;
        AccountModule.Instance.Os_Current_Uso = Info();
        return AccountModule.Instance;
    }
    private static void Assert(bool value, string message)
    { if (!value) throw new Exception(message); }
    private static void Pass(string name) { passed++; Console.WriteLine("PASS " + name); }
    private static void Close(UIPageBase page)
    {
        UI.ClosePage(page);
        UI.FinishClose(page);
        UI.Tick();
    }

    private static async Task Main()
    {
        var account = Reset();
        account.Os_Current_Uso = Info();
        account.Os_Current_Uso = Info(0.0002);
        account.Os_Current_Uso = null;
        UI.Tick();
        Assert(UI.Attempts.Count == 0, "Initial/equal/lower/null responses must not open a popup.");
        Pass("initial login, equal/lower rate and null response");

        account = Reset();
        account.CheckPendingAutoPopups(); // GameStart occurs before the server response.
        account.Os_Current_Uso = Info(0.00035, 120, 4.2);
        Assert(UI.Attempts.Count == 1, "Late response waited for another GameStart.");
        var snapshot = (ExchangeRateInfo)UI.Attempts[0].data;
        Assert(snapshot.beforeRate == 0.0003 && snapshot.nowRate == 0.00035 &&
            snapshot.beforeBlance == 100 && snapshot.nowBlance == 120 &&
            snapshot.beforeClash == 3 && snapshot.nowClash == 4.2, "Incorrect value snapshot.");
        UI.Tick();
        Close(UI.LastPopup);
        account.Os_Current_Uso = Info(0.00035);
        Assert(UI.Attempts.Count == 1, "Same rate repeated after dismissal.");
        Pass("late response and immutable before/after values");

        var blockers = new (string name, Action block, Action release)[]
        {
            ("settlement/level transition", () => FlowModule.SetAutomaticPopupFlowBlocked(true),
                () => FlowModule.SetAutomaticPopupFlowBlocked(false)),
            ("ad/background", () => RewardItemCollectFlow.CanPresentUI = false, () => RewardItemCollectFlow.CanPresentUI = true),
            ("manual popup", () => UI.ManualPopup = true, () => UI.ManualPopup = false),
            ("page opening", () => UI.Opening = true, () => UI.Opening = false),
            ("tutorial", () => TeachUtil.IsTeach = true, () => TeachUtil.IsTeach = false),
            ("inactive game page", () => UI.GamePanel.gameObject.activeInHierarchy = false,
                () => UI.GamePanel.gameObject.activeInHierarchy = true),
            ("uninitialized UI", () => UI.Initialized = false, () => UI.Initialized = true)
        };
        foreach (var blocker in blockers)
        {
            account = Reset();
            blocker.block();
            account.Os_Current_Uso = Info(0.00035);
            UI.Tick();
            Assert(UI.Attempts.Count == 0, "Opened while blocked: " + blocker.name);
            blocker.release(); UI.Tick();
            Assert(UI.Attempts.Count == 1, "Pending request lost: " + blocker.name);
            Pass("wait and resume: " + blocker.name);
        }

        account = Reset();
        var ui = UI;
        UIModule.Instance = null;
        account.Os_Current_Uso = Info(0.00035);
        UIModule.Instance = ui;
        UI.Tick();
        Assert(UI.Attempts.Count == 1, "Response before UI creation was lost.");
        Pass("queue accepts responses before UI is available");

        Reset();
        int shown = 0;
        Assert(Queue.Enqueue("FutureOffer", key: "offer-42", onShown: () => shown++), "Initial enqueue rejected.");
        Assert(!Queue.Enqueue("FutureOffer", key: "offer-42"), "Pending key duplicated.");
        Queue.Enqueue("FutureNotice");
        UI.Tick();
        var first = UI.LastPopup;
        UI.Tick(); UI.Tick();
        Assert(shown == 1 && UI.Attempts.Count == 1, "Popups overlapped or confirmation repeated.");
        Assert(!Queue.Enqueue("FutureOffer", key: "offer-42"), "Visible key duplicated.");
        UI.ClosePage(first); // HasPopup is false while its closing animation is still visible.
        UI.Tick();
        Assert(UI.Attempts.Count == 1, "Opened over the preceding hide animation.");
        UI.FinishClose(first); UI.Tick();
        Assert(UI.Attempts.Count == 2 && UI.LastPopup.PageId == "FutureNotice", "FIFO did not advance.");
        Pass("future pages, deduplication, FIFO and complete hide animation");

        account = Reset();
        RewardItemCollectFlow.CanPresentUI = false;
        var before = account.Os_Current_Uso;
        var after = Info(0.00035, 120, 4.2);
        account.Os_Current_Uso = after;
        before.Os_Tra = 9; before.Balance = 9; after.Os_Tra = 8; after.Balance = 8;
        RewardItemCollectFlow.CanPresentUI = true; UI.Tick();
        snapshot = (ExchangeRateInfo)UI.Attempts[0].data;
        Assert(snapshot.beforeRate == 0.0003 && snapshot.nowRate == 0.00035 &&
            snapshot.beforeBlance == 100 && snapshot.nowBlance == 120, "Snapshot mutated while queued.");
        Pass("mutable account updates cannot change queued snapshots");

        Reset();
        var prepare = new TaskCompletionSource<bool>();
        UI.PrepareHandler = _ => prepare.Task;
        Queue.Enqueue("SlowPage"); UI.Tick(); UI.Tick();
        UI.ManualPopup = true;
        prepare.SetResult(true);
        await Task.WhenAll(TaskExtensions.Tasks.ToArray());
        Assert(UI.Attempts.Count == 0, "Opening ignored a blocker that appeared during preload.");
        UI.ManualPopup = false; UI.Tick();
        Assert(UI.Attempts.Count == 1, "Prepared page failed to resume.");
        Pass("recheck visibility after asynchronous asset loading");

        account = Reset();
        var open = new TaskCompletionSource<UIPageBase>();
        UI.OpenHandler = (_, _) => open.Task;
        account.Os_Current_Uso = Info(0.00035);
        account.Os_Current_Uso = Info(0.0004);
        UI.Tick(); UI.Tick();
        Assert(UI.Attempts.Count == 1, "Concurrent opens duplicated.");
        open.SetResult(new TestRatePage { RawPageId = UIPageIds.ExchangeRatePanel });
        await Task.WhenAll(TaskExtensions.Tasks.ToArray());
        UI.Tick(); UI.OpenHandler = UIModule.DefaultOpen;
        Close(UI.LastPopup);
        Assert(UI.Attempts.Count == 2 && ((ExchangeRateInfo)UI.Attempts[1].data).nowRate == 0.0004,
            "A later increase was consumed by an earlier request.");
        Pass("in-flight open retains subsequent increase");

        foreach (bool throws in new[] { false, true })
        {
            Reset();
            UI.OpenHandler = (id, _) => id == "Broken" ?
                (throws ? Task.FromException<UIPageBase>(new Exception("Simulated load error")) : Task.FromResult<UIPageBase>(null))
                : UIModule.DefaultOpen(id, null);
            Queue.Enqueue("Broken"); Queue.Enqueue("Healthy");
            UI.Tick(); UI.Tick();
            Assert(UI.Attempts.Count == 2 && UI.LastPopup.PageId == "Healthy", "A failed page blocked the queue.");
            UI.Tick(); Close(UI.LastPopup);
            UI.OpenHandler = UIModule.DefaultOpen; Time.unscaledTime = 1.1f; UI.Tick();
            Assert(UI.Attempts.Count == 3 && UI.LastPopup.PageId == "Broken", "Failed request was lost.");
            Pass(throws ? "exception retries without blocking other pages" : "null result retries without blocking other pages");
        }

        Reset();
        bool valid = true;
        prepare = new TaskCompletionSource<bool>();
        UI.PrepareHandler = _ => prepare.Task;
        Queue.Enqueue("Expiring", isValid: () => valid); UI.Tick(); valid = false;
        prepare.SetResult(true); await Task.WhenAll(TaskExtensions.Tasks.ToArray());
        Assert(UI.Attempts.Count == 0 && !Queue.HasWork, "Expired request opened after loading.");
        Pass("eligibility is rechecked after loading");

        Reset();
        prepare = new TaskCompletionSource<bool>();
        UI.PrepareHandler = _ => prepare.Task;
        Queue.Enqueue("OldAccount"); UI.Tick(); Queue.Clear();
        Queue.Enqueue("NewAccount");
        prepare.SetResult(true); await Task.WhenAll(TaskExtensions.Tasks.ToArray());
        UI.Tick();
        Assert(UI.Attempts.Count == 1 && UI.LastPopup.PageId == "NewAccount", "Stale completion crossed queue reset.");
        Pass("clear invalidates an old asynchronous attempt");

        Reset();
        Queue.Enqueue("CallbackThrows", onShown: () => throw new Exception("Simulated callback error"));
        UI.Tick(); UI.Tick(); Close(UI.LastPopup);
        Assert(UI.Attempts.Count == 1 && !Queue.HasWork, "Callback exception replayed a visible popup.");
        Pass("confirmation callback exception does not replay a shown page");

        account = Reset();
        account.Os_Current_Uso = Info(0.00035);
        account.TriggerDaily(); account.TriggerDaily();
        UI.Tick();
        Assert(UI.Attempts.Count == 1 && UI.LastPopup.PageId == UIPageIds.ExchangeRatePanel,
            "Daily popup overlapped the rate popup.");
        Close(UI.LastPopup);
        var daily = (DailyWithdrawPanel)UI.LastPopup;
        UI.Tick();
        Assert(SaveDataUtils.GameData.lastGetDailyRewardTime == DateTime.MinValue,
            "Daily flag was consumed before page content arrived.");
        account.RespondWithdrawal(true); UI.Tick();
        Assert(daily.ContentReady && SaveDataUtils.GameData.lastGetDailyRewardTime.Date == DateTime.Today,
            "Successful visible daily popup was not recorded.");
        Close(daily); account.TriggerDaily(); UI.Tick();
        Assert(UI.Attempts.Count == 2, "Daily popup repeated on the same day.");
        Pass("rate and daily serialize; daily commits only after successful content");

        account = Reset();
        account.TriggerDaily(); daily = (DailyWithdrawPanel)UI.LastPopup;
        account.RespondWithdrawal(false); UI.Tick();
        Assert(daily.ContentFailed && SaveDataUtils.GameData.lastGetDailyRewardTime == DateTime.MinValue,
            "Failed content consumed today's opportunity.");
        UI.FinishClose(daily); UI.Tick(); Time.unscaledTime = 1.1f; UI.Tick();
        Assert(UI.Attempts.Count == 2, "Failed daily page did not retry.");
        account.RespondWithdrawal(true); UI.Tick();
        Assert(SaveDataUtils.GameData.lastGetDailyRewardTime.Date == DateTime.Today, "Retry was not confirmed.");
        Pass("daily content failure retains today's opportunity and retries");

        account = Reset();
        account.TriggerDaily(); daily = (DailyWithdrawPanel)UI.LastPopup;
        UI.ClosePage(daily); account.RespondWithdrawal(true); UI.FinishClose(daily); UI.Tick();
        Assert(!daily.ContentReady && SaveDataUtils.GameData.lastGetDailyRewardTime == DateTime.MinValue,
            "A stale callback updated a closed daily page.");
        Pass("closed daily page ignores late HTTP callback");

        account = Reset();
        account.TriggerDaily(); account.RespondWithdrawal(true);
        RewardItemCollectFlow.CanPresentUI = false; UI.Tick();
        Assert(SaveDataUtils.GameData.lastGetDailyRewardTime == DateTime.MinValue, "Hidden page counted as shown.");
        RewardItemCollectFlow.CanPresentUI = true; UI.Tick();
        Assert(SaveDataUtils.GameData.lastGetDailyRewardTime.Date == DateTime.Today, "Foreground confirmation lost.");
        Pass("content ready behind ad/background does not commit until visible");

        account = Reset();
        UI.ManualPopup = true; account.TriggerDaily();
        account.Os_Current_Uso = Info(available: 0); UI.ManualPopup = false; UI.Tick();
        Assert(UI.Attempts.Count == 0 && !Queue.HasWork, "Daily eligibility was not rechecked before opening.");
        Pass("withdrawal balance change invalidates a queued daily popup");

        foreach (var country in new[] { AccountModule.E_CountryType.US, AccountModule.E_CountryType.BR, AccountModule.E_CountryType.ID })
        {
            account = Reset(); AccountModule.CountryType = country;
            account.Os_Current_Uso = Info(available: 0); account.TriggerDaily(); UI.Tick();
            Assert(UI.Attempts.Count == 0, "Below-threshold daily popup opened.");
            account.Os_Current_Uso = Info(available: 100); account.TriggerDaily();
            Assert(UI.Attempts.Count == 1, "Eligible country failed to enqueue.");
        }
        Pass("daily withdrawal thresholds for US, BR and ID");

        Assert(Debug.Errors.Count == 0, "Unexpected error in final scenario.");
        await RunReviewRegressions();
        await RunProjectRegressions();
        Console.WriteLine($"All {passed} automatic popup regressions passed.");
    }
}

// External dependencies only. Queue, account triggers, daily page and UIModule gates/update are production source.
public partial class AccountModule
{
    public static AccountModule Instance;
    public enum E_CountryType { US, BR, ID }
    public static E_CountryType CountryType;
    private const float UsMinWithdrawalValue = 0.2f, BRMinWithdrawalValue = 0.3f, IDMinWithdrawalValue = 50;
    private readonly Queue<Action<FailHttpResponse<OceanShineWithdrawalPageResponse>>> responses = new();
    public void TriggerDaily() => QueueDailyWithdrawal();
    public void Request_WithdrawalPageRequest(Action<FailHttpResponse<OceanShineWithdrawalPageResponse>> callback, bool block = true)
    {
        if (block) throw new Exception("Daily popup must own its loading mask independently of the HTTP request.");
        responses.Enqueue(callback);
    }
    public void RespondWithdrawal(bool success) => responses.Dequeue()(new() { success = success, data = success ? new() : null });
    public class OceanShineWithdrawalPageResponse
    {
        public List<WithdrawalPlatform> Os_Wwf = new();
        public class WithdrawalPlatform { public string Os_Cn = "paypal"; }
    }
}
public class FailHttpResponse<T> { public bool success; public T data; }
public class UserInfo
{
    public double Os_Tra, Os_Ewl, Balance;
    public int Os_Lgd, Os_Mny;
    public double GetBalance() => Balance;
    public string GetInfo() => "isolated test";
}
public partial class UIModule
{
    public static UIModule Instance;
    private List<UIPageBase> _pagesOpened = new();
    private List<UIPageBase> _pagePool = new();
    private int _uiLifecycleVersion;
    public bool Opening, ManualPopup;
    public bool HasPopup => ManualPopup || (_pagesOpened?.Any(p => p != GamePanel) ?? false);
    public RealGamePanel GamePanel = new();
    public UIPageBase LastPopup, LastAllocated, ReuseExisting;
    public bool Initialized { set => _pagesOpened = value ? new() { GamePanel } : null; }
    public readonly List<(PageId id, object data)> Attempts = new();
    public Func<PageId, Task<bool>> PrepareHandler = _ => Task.FromResult(true);
    public Func<PageId, object, Task<UIPageBase>> OpenHandler = DefaultOpen;
    public UIModule() { _pagesOpened.Add(GamePanel); }
    public static Task<UIPageBase> DefaultOpen(PageId id, object data) => Task.FromResult<UIPageBase>(
        id == UIPageIds.DailyWithdrawPanel ? new DailyWithdrawPanel { RawPageId = id, clashTxt = new(), balanceTxt = new(), withdrawalTxt = new() }
        : id == UIPageIds.ExchangeRatePanel ? new TestRatePage { RawPageId = id } : new TestPage { RawPageId = id });
    private async Task<bool> EnsurePageConfigLoaded(PageId id, int version, PageOpenAttempt attempt = null)
    {
        var result = await PrepareHandler(id).AttachExternalCancellation(attempt.Token);
        attempt.Token.ThrowIfCancellationRequested();
        return result && version == _uiLifecycleVersion;
    }
    public T GetPage<T>() where T : class => _pagesOpened.OfType<T>().FirstOrDefault();
    public UIPageBase FindLastOpenedPage() => _pagesOpened.LastOrDefault();
    // Only the resource/instance acquisition seam is simulated. Registration, activation,
    // argument dispatch, OnOpen/ShowPage, rollback and forced close execute production code.
    private async Task<UIPageBase> _OpenPage(PageId id, PageOpenAttempt attempt)
    {
        Attempts.Add((id, null));
        if (ReuseExisting != null) { attempt.Track(ReuseExisting, false); return ReuseExisting; }
        var page = await OpenHandler(id, null);
        attempt.Token.ThrowIfCancellationRequested(); // Match production's pre-registration guard.
        if (page == null) return null;
        LastAllocated = page;
        typeof(UIPageBase).GetField("m_animOnHide", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .SetValue(page, "test-hide");
        page.Layer = E_UILayer.PopupLayer;
        attempt.Track(page, true);
        InitPage(page, UIPageRuntimeConfig.FromPage(page), new RectTransform());
        LastPopup = page;
        return page;
    }
    private UIPageRuntimeConfig GetPageRuntimeConfig(UIPageBase page) => UIPageRuntimeConfig.FromPage(page);
    public bool Registered(UIPageBase page) => _pagesOpened.Contains(page);
    public void ClosePage_CallByFramework(UIPageBase page) { _pagesOpened?.Remove(page); page.gameObject.SetActive(false); }
    public void FinishClose(UIPageBase page) { page.ForceClosePage_CallByFramework(); page.gameObject.SetActive(false); }
    private static void Destroy(GameObject obj) => obj.SetActive(false);
    public void Tick() => Update();
    public void SimulatePause(bool value) => OnApplicationPause(value);
    public void SimulateFocus(bool value) => OnApplicationFocus(value);
    public void SimulateUIRelease() => _uiLifecycleVersion++;
    private void ExitCurrentView() { }
}
public class TestPage : UIPageBase
{
    public Action<TestPage> Initialize;
    public bool ThrowOnHide;
    public Action Closed;
    public int CloseCount;
    protected override void OnOpen() => Initialize?.Invoke(this);
    protected override void OnHide() { if (ThrowOnHide) throw new Exception("Simulated hide error"); }
    protected override void OnClose() { CloseCount++; Closed?.Invoke(); }
}
public class WaitingContentPage : TestPage, IAutoPopupContent
{
    public bool ContentReady => false;
    public bool ContentFailed => false;
}
public class TestRatePage : UIPageBase<ExchangeRateInfo>
{
    public Action<TestRatePage> Initialize;
    protected override void OnOpen(ExchangeRateInfo info)
    {
        int i = UIModule.Instance.Attempts.Count - 1;
        UIModule.Instance.Attempts[i] = (PageId, info);
        Initialize?.Invoke(this);
    }
    protected override void OnClose() { }
}
public class RealGamePanel : TestPage { }
public partial class UIPageIds
{
    public static readonly PageId ExchangeRatePanel = "ExchangeRatePanel";
    public static readonly PageId RealWithdrawPanel = "RealWithdrawPanel";
}
public enum E_GameModeType { None, MainMenu, GamePlay }
public class TestGameMode { public E_GameModeType GameModeType = E_GameModeType.GamePlay; }
public class World { public static World Current; public TestGameMode CurGameMode = new(); public bool IsPause; }
public class TransitionBlock { public static TransitionBlock Instance; public GameObject gameObject = new(); }
public static class RewardItemCollectFlow { public static bool CanPresentUI = true; public static int PresentationClockVersion; public static void CancelForPage(UIPageBase _) { } }
public static class TeachUtil { public static bool IsTeach; }
public static class Time { public static float unscaledTime; public static float realtimeSinceStartup => unscaledTime; }
public static class Mathf { public static float Max(float a, float b) => Math.Max(a, b); public static float Min(float a, float b) => Math.Min(a, b); public static float Pow(float a, float b) => (float)Math.Pow(a, b); }
public enum KeyCode { Escape }
public static class Input { public static bool GetKeyDown(KeyCode _) => false; }
public static class Debug
{
    public static readonly List<Exception> Errors = new();
    public static void Log(string _) { }
    public static void LogError(string _) { }
    public static void LogWarning(string _) { }
    public static void LogException(Exception exception) => Errors.Add(exception);
}
public static class LogLogger
{
    public static void LOGUserInfoAlter(string _) { }
    public static void LogVerbose(string _, string message) { }
}
public static class SaveDataUtils
{
    public static GameSaveData GameData = new();
    public static int Saves;
    public static void Save() => Saves++;
    public static SaveStrategy gameStrategy = new();
}
public class SaveStrategy { public void SaveData() => SaveDataUtils.Saves++; }
public class GameSaveData { public int todayAdTimes; public bool fakeWithdrawPanelFirstWithdraw; public DateTime lastGetDailyRewardTime; }
public static class BizzaEventSystem { public static void Emit(string _) { } public static void Emit(string _, PageId id) { } }
public static class EventDefine { public static class Frame { public const string OpenPage = "OpenPage", ClosePage = "ClosePage"; } public static class Item { public const string ItemChanged = "ItemChanged"; } }
public static class TaskExtensions
{
    public static readonly List<Task> Tasks = new();
    public static void Forget(this Task task) => Tasks.Add(task);
    public static Task<T> AttachExternalCancellation<T>(this Task<T> task, System.Threading.CancellationToken token) => task.WaitAsync(token);
}
public class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType _) { } }
public enum RuntimeInitializeLoadType { SubsystemRegistration }
public class HeaderAttribute : Attribute { public HeaderAttribute(string _) { } }
public class SerializeFieldAttribute : Attribute { }

public class BizzaButton { public ClickEvent onClick = new(); public class ClickEvent { public void AddListener(Action _) { } } }
public static class UIWithdrawalPanel { public const string paypalInfo = "paypal", pagBankInfo = "pagbank", danaInfo = "dana"; }
public static class LanguageUtils { public static string GetText(string _) => "$"; }
public static class WithdrawalUtil { public static string GetCustomizedValueByCountryType(float value) => value.ToString(); }
public static class BaseConst { public const string LOG_Game = "game"; }
namespace UnityEngine { }
namespace UnityEngine.UI { public static class LayoutRebuilder { public static void ForceRebuildLayoutImmediate(RectTransform _) { } } }
namespace TMPro { public class TMP_Text { public string text; } }
namespace Obfuz { public class ObfuzIgnoreAttribute : Attribute { } }
