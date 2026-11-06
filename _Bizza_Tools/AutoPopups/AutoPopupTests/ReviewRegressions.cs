using System;
using System.Linq;
using System.Threading.Tasks;

internal static partial class Program
{
    private static void ShortTimeouts()
    {
        Queue.ResourceTimeoutSeconds = 2;
        Queue.OpenTimeoutSeconds = 3;
        Queue.ContentTimeoutSeconds = 4;
    }
    private static Task Drain() => Task.WhenAll(TaskExtensions.Tasks.ToArray());

    private static async Task RunReviewRegressions()
    {
        foreach (bool typed in new[] { false, true })
        {
            Reset();
            int shown = 0;
            UIPageBase failed = null;
            UI.OpenHandler = (id, _) =>
            {
                if (id == "Healthy") return UIModule.DefaultOpen(id, null);
                if (typed)
                    failed = new TestRatePage { RawPageId = id, Initialize = p =>
                    {
                        Assert(UI.Registered(p) && p.gameObject.activeInHierarchy, "Typed OnOpen did not run after real InitPage registration.");
                        throw new Exception("Typed initialization failed after registration");
                    }};
                else
                    failed = new TestPage { RawPageId = id, Initialize = p =>
                    {
                        Assert(UI.Registered(p) && p.gameObject.activeInHierarchy, "OnOpen did not run after real InitPage registration.");
                        throw new Exception("Initialization failed after registration");
                    }};
                return Task.FromResult(failed);
            };
            if (typed) Queue.Enqueue("Broken", data: new ExchangeRateInfo(), onShown: () => shown++);
            else Queue.Enqueue("Broken", onShown: () => shown++);
            Queue.Enqueue("Healthy"); UI.Tick(); await Drain();
            Assert(failed != null && !UI.Registered(failed) && !failed.gameObject.activeInHierarchy && !UI.HasPopup,
                "Failed registered page leaked into the UI.");
            Assert(shown == 0 && SaveDataUtils.GameData.lastGetDailyRewardTime == DateTime.MinValue,
                "Initialization failure was confirmed.");
            UI.Tick(); UI.Tick();
            Assert(UI.LastPopup.PageId == "Healthy", "Failed initialized page blocked next popup.");
            Pass(typed ? "registered typed OnOpen exception rolls back its instance" : "registered untyped OnOpen exception is observable and rolled back");
        }

        foreach (bool missingArguments in new[] { false, true })
        {
            Reset(); int shown = 0;
            UI.OpenHandler = (id, _) => Task.FromResult<UIPageBase>(missingArguments
                ? new TestRatePage { RawPageId = id } : new TestPage { RawPageId = id });
            if (missingArguments) Queue.Enqueue("WrongSignature", onShown: () => shown++);
            else Queue.Enqueue("WrongSignature", data: new ExchangeRateInfo(), onShown: () => shown++);
            UI.Tick(); await Drain();
            Assert(!UI.HasPopup && !UI.Registered(UI.LastAllocated) && !UI.LastAllocated.gameObject.activeInHierarchy && shown == 0,
                "Mismatched/missing arguments left a registered page or confirmed it.");
            Pass(missingArguments ? "untyped call cannot silently open a typed page" : "argument type mismatch rolls back new registration");
        }

        Reset();
        var normal = await UI.OpenPage("Existing");
        var previousOwner = normal.OpenOwner;
        UI.ReuseExisting = normal;
        try { await UI.OpenPage<int>("Existing", 1); throw new Exception("Expected type error."); }
        catch (InvalidOperationException) { }
        Assert(UI.Registered(normal) && normal.gameObject.activeInHierarchy && ReferenceEquals(normal.OpenOwner, previousOwner),
            "Failed call destroyed or stole ownership from an unrelated existing page.");
        Pass("mismatched call preserves an already open instance");

        Reset();
        TestPage broken = null; UIPageBase other = null;
        UI.OpenHandler = (id, _) => Task.FromResult<UIPageBase>(broken = new TestPage
        {
            RawPageId = id,
            Initialize = p =>
            {
                UI.OpenHandler = UIModule.DefaultOpen;
                other = UI.OpenPage(id).GetAwaiter().GetResult();
                throw new Exception("Fail after another instance of same ID opened");
            }
        });
        Queue.Enqueue("SameId"); UI.Tick(); await Drain();
        Assert(!UI.Registered(broken) && !broken.gameObject.activeInHierarchy && UI.Registered(other) && other.gameObject.activeInHierarchy,
            "Rollback used the page ID instead of the owned instance.");
        Pass("rollback preserves a different instance with the same page ID");

        Reset();
        UI.OpenHandler = (id, _) => Task.FromResult<UIPageBase>(broken = new TestPage
        { RawPageId = id, ThrowOnHide = true, Initialize = _ => throw new Exception("Init failure") });
        Queue.Enqueue("BadCloseHooks"); UI.Tick(); await Drain();
        Assert(!UI.Registered(broken) && !broken.gameObject.activeInHierarchy && broken.CloseCount == 1,
            "Failing OnHide prevented final cleanup/OnClose.");
        Pass("rollback still invalidates and deactivates when OnHide throws");

        Reset(); ShortTimeouts(); int callbacks = 0;
        var load = new TaskCompletionSource<bool>();
        UI.PrepareHandler = id => id == "NeverLoads" ? load.Task : Task.FromResult(true);
        Queue.Enqueue("NeverLoads", onShown: () => callbacks++); Queue.Enqueue("Healthy");
        UI.Tick(); Time.unscaledTime = 2.1f; UI.Tick(); await Drain(); UI.Tick();
        Assert(UI.LastPopup.PageId == "Healthy" && callbacks == 0, "Resource timeout blocked the next popup.");
        load.SetResult(true); await Drain(); UI.Tick();
        Assert(UI.LastPopup.PageId == "Healthy" && callbacks == 0 && UI.Attempts.All(x => x.id != "NeverLoads"),
            "Late resource completion reopened a timed-out attempt.");
        Close(UI.LastPopup); Time.unscaledTime = 3.2f; UI.Tick(); UI.Tick();
        Assert(UI.LastPopup.PageId == "NeverLoads" && callbacks == 1, "Resource timeout did not recover through backoff retry.");
        Pass("never-ending resource load times out; late completion is ignored; retry succeeds");

        Reset(); ShortTimeouts(); callbacks = 0;
        var open = new TaskCompletionSource<UIPageBase>();
        UI.OpenHandler = (id, _) => id == "NeverOpens" ? open.Task : UIModule.DefaultOpen(id, null);
        Queue.Enqueue("NeverOpens", onShown: () => callbacks++); Queue.Enqueue("Healthy");
        UI.Tick(); Time.unscaledTime = 3.1f; UI.Tick(); UI.Tick();
        var healthy = UI.LastPopup;
        Assert(healthy.PageId == "Healthy" && callbacks == 0, "Opening timeout blocked queue.");
        var latePage = new TestPage { RawPageId = "NeverOpens" };
        open.SetResult(latePage); await Drain(); UI.Tick();
        Assert(!UI.Registered(latePage) && UI.LastPopup == healthy && callbacks == 0, "Late opening result entered the registry.");
        UI.OpenHandler = UIModule.DefaultOpen; Close(healthy); Time.unscaledTime = 4.2f; UI.Tick(); UI.Tick();
        Assert(callbacks == 1 && UI.LastPopup.PageId == "NeverOpens", "Opening timeout could not retry.");
        Pass("opening timeout invalidates late registration and preserves the new attempt");

        var account = Reset(); ShortTimeouts();
        account.TriggerDaily(); var oldDaily = (DailyWithdrawPanel)UI.LastPopup;
        Queue.Enqueue("Healthy"); Time.unscaledTime = 4.1f; UI.Tick(); UI.Tick();
        Assert(!UI.Registered(oldDaily) && !oldDaily.gameObject.activeInHierarchy && UI.LastPopup.PageId == "Healthy" &&
            SaveDataUtils.GameData.lastGetDailyRewardTime == DateTime.MinValue,
            "Content timeout left a blocking daily page or consumed today's opportunity.");
        Close(UI.LastPopup); Time.unscaledTime = 5.2f; UI.Tick();
        var newDaily = (DailyWithdrawPanel)UI.LastPopup;
        Assert(newDaily != oldDaily, "Retry reused a failed instance.");
        account.RespondWithdrawal(true); UI.Tick();
        Assert(!oldDaily.ContentReady && !newDaily.ContentReady && SaveDataUtils.GameData.lastGetDailyRewardTime == DateTime.MinValue,
            "Late content callback updated old/new page or committed daily marker.");
        account.RespondWithdrawal(true); UI.Tick();
        Assert(newDaily.ContentReady && SaveDataUtils.GameData.lastGetDailyRewardTime.Date == DateTime.Today,
            "Valid content retry failed to commit.");
        Pass("content timeout destroys failed daily instance; old callback cannot affect retry or daily marker");

        account = Reset(); ShortTimeouts();
        var unrelatedMask = new object(); LoadingBlock.Reasons.Add(unrelatedMask);
        account.TriggerDaily();
        Assert(LoadingBlock.Reasons.Count == 2, "Daily request did not own a separate loading mask.");
        Time.unscaledTime = 4.1f; UI.Tick();
        Assert(LoadingBlock.Reasons.SetEquals(new[] { unrelatedMask }), "Timeout leaked the daily mask or removed someone else's mask.");
        account.RespondWithdrawal(true); UI.Tick();
        Assert(LoadingBlock.Reasons.SetEquals(new[] { unrelatedMask }) && SaveDataUtils.GameData.lastGetDailyRewardTime == DateTime.MinValue,
            "Late callback changed mask ownership or daily opportunity.");
        Pass("daily timeout releases only its own network loading mask");

        account = Reset(); ShortTimeouts(); account.TriggerDaily();
        Time.unscaledTime = 4.1f; account.RespondWithdrawal(true); UI.Tick();
        Assert(SaveDataUtils.GameData.lastGetDailyRewardTime == DateTime.MinValue && !UI.HasPopup,
            "Success arriving past the content deadline was confirmed before timeout.");
        Pass("late content success is rejected even when it arrives before the next Tick");

        foreach (bool accountChange in new[] { false, true })
        {
            account = Reset(); ShortTimeouts(); account.TriggerDaily();
            oldDaily = (DailyWithdrawPanel)UI.LastPopup;
            if (accountChange) _ = new AccountModule(); else Queue.Clear();
            Queue.Enqueue("NewSession"); UI.Tick(); UI.Tick();
            account.RespondWithdrawal(true); UI.Tick();
            Assert(!oldDaily.ContentReady && !oldDaily.gameObject.activeInHierarchy && UI.LastPopup.PageId == "NewSession" &&
                SaveDataUtils.GameData.lastGetDailyRewardTime == DateTime.MinValue,
                "Clear/account change allowed old content to affect the new session.");
            Pass(accountChange ? "real AccountModule constructor invalidates old daily content" : "clear invalidates pending daily content");
        }

        foreach (bool accountChange in new[] { false, true })
        {
            Reset(); ShortTimeouts(); open = new TaskCompletionSource<UIPageBase>();
            UI.OpenHandler = (id, _) => id == "SameId" ? open.Task : UIModule.DefaultOpen(id, null);
            Queue.Enqueue("SameId", key: "same-event"); UI.Tick();
            Time.unscaledTime = 3.1f; UI.Tick();
            if (accountChange) _ = new AccountModule(); else Queue.Clear();
            UI.OpenHandler = UIModule.DefaultOpen;
            Queue.Enqueue("SameId", key: "same-event"); UI.Tick(); UI.Tick();
            var newPage = UI.LastPopup;
            open.SetResult(new TestPage { RawPageId = "SameId" }); await Drain(); UI.Tick();
            Assert(UI.LastPopup == newPage && UI.Registered(newPage) && newPage.gameObject.activeInHierarchy,
                "Old timeout completion interfered with reused event key in new session.");
            Pass(accountChange ? "timeout plus account switch isolates the same key/page ID" : "timeout plus clear isolates the same key/page ID");
        }

        foreach (string blocker in new[] { "ad", "settlement", "transition", "background" })
        {
            Reset(); ShortTimeouts(); load = new TaskCompletionSource<bool>();
            UI.PrepareHandler = _ => load.Task;
            Queue.Enqueue("Waiting"); UI.Tick(); Time.unscaledTime = 0.5f; UI.Tick();
            if (blocker == "ad") RewardItemCollectFlow.CanPresentUI = false;
            else if (blocker == "background") UI.SimulatePause(true);
            else FlowModule.SetAutomaticPopupFlowBlocked(true);
            UI.Tick(); Time.unscaledTime = 100; UI.Tick();
            Assert(Queue.HasWork && UI.Attempts.Count == 0, "Normal wait became a failure.");
            if (blocker == "ad") RewardItemCollectFlow.CanPresentUI = true;
            else if (blocker == "background") UI.SimulatePause(false);
            else FlowModule.SetAutomaticPopupFlowBlocked(false);
            UI.Tick(); load.SetResult(true); await Drain(); UI.Tick();
            Assert(UI.LastPopup?.PageId == "Waiting", "Paused work timed out after resuming.");
            Pass("resource clock pauses for " + blocker);
        }

        account = Reset(); ShortTimeouts(); account.TriggerDaily();
        UI.SimulatePause(true); Time.unscaledTime = 100; UI.SimulatePause(false); // No frames while suspended.
        UI.Tick(); account.RespondWithdrawal(true); UI.Tick();
        Assert(SaveDataUtils.GameData.lastGetDailyRewardTime.Date == DateTime.Today,
            "Background interval with no ticks was included in timeout.");
        Pass("application lifecycle clock revision excludes suspension without frames");

        account = Reset(); ShortTimeouts(); account.TriggerDaily();
        RewardItemCollectFlow.PresentationClockVersion++; // Ad displayed; no Unity frames during presentation.
        Time.unscaledTime = 100;
        RewardItemCollectFlow.PresentationClockVersion++; // Ad hidden.
        UI.Tick(); account.RespondWithdrawal(true); UI.Tick();
        Assert(SaveDataUtils.GameData.lastGetDailyRewardTime.Date == DateTime.Today,
            "Ad interval without frames was included in content timeout.");
        Pass("ad lifecycle clock revision excludes presentation without frames");

        Reset(); ShortTimeouts(); RewardItemCollectFlow.CanPresentUI = false;
        Queue.Enqueue("Pending"); Time.unscaledTime = 1000; UI.Tick();
        RewardItemCollectFlow.CanPresentUI = true; UI.Tick(); UI.Tick();
        Assert(UI.LastPopup.PageId == "Pending", "Time spent waiting in queue counted against loading.");
        Pass("pending requests have no loading deadline until work starts");

        Reset();
        var scope = new UIModule.PageOpenAttempt(); scope.Cancel();
        var allocatedDuringCancellation = new TestPage();
        try { scope.Track(allocatedDuringCancellation, true); }
        catch (OperationCanceledException) { }
        UI.AbortPageOpen(scope); scope.Dispose();
        Assert(!allocatedDuringCancellation.gameObject.activeInHierarchy,
            "Cancellation during allocation leaked the new instance before registration.");
        Pass("cancellation during allocation still gives rollback ownership of the new instance");

        foreach (bool replaceUI in new[] { false, true })
        {
            Reset(); load = new TaskCompletionSource<bool>();
            UI.PrepareHandler = _ => load.Task;
            Queue.Enqueue("OldUILoad"); UI.Tick();
            if (replaceUI) UIModule.Instance = new UIModule(); else UI.SimulateUIRelease();
            UI.PrepareHandler = _ => Task.FromResult(true);
            Queue.Enqueue("NewUILoad"); UI.Tick(); await Drain(); UI.Tick();
            Assert(UI.LastPopup.PageId == "NewUILoad", "Invalid UI context occupied the queue indefinitely.");
            load.SetResult(true); await Drain();
            Assert(UI.LastPopup.PageId == "NewUILoad", "Old UI resource completion altered the current UI.");
            Pass(replaceUI ? "UI replacement cancels old resource wait" : "UI release version cancels old resource wait");
        }

        Reset(); ShortTimeouts();
        UI.OpenHandler = (id, _) => id == "OldSession" ? Task.FromResult<UIPageBase>(new WaitingContentPage
        {
            RawPageId = id,
            Closed = () => { Queue.Clear(); Queue.Enqueue("NewSession"); }
        }) : UIModule.DefaultOpen(id, null);
        Queue.Enqueue("OldSession"); UI.Tick(); Time.unscaledTime = 4.1f; UI.Tick(); UI.Tick();
        Assert(UI.LastPopup.PageId == "NewSession", "Close hook could not submit new-session work.");
        Close(UI.LastPopup); Time.unscaledTime = 100; UI.Tick();
        Assert(UI.Attempts.Count == 2 && !Queue.HasWork, "Cleanup requeued old work after a reentrant Clear.");
        Pass("clear inside timeout close hook cannot requeue the old session");
    }
}
