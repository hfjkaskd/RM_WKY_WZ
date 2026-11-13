#if UNITY_EDITOR && BIZZA_REAL_WITHDRAW
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEditor;

/// <summary>运行生产流程，控制 HTTP/页面时序；不访问网络、玩家存档或真实奖励。</summary>
[InitializeOnLoad]
public static class Teach01FlowChecks
{
    const string RequestFile = "Temp/Teach01FlowChecks.request.txt";
    const string ResultFile = "Temp/OfflineSyncChecks/teach-results.txt";
    static Teach01FlowChecks() => EditorApplication.update += Poll;
    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(RequestFile)) return;
        File.Delete(RequestFile);
        Directory.CreateDirectory(Path.GetDirectoryName(ResultFile));
        try { Run(); File.WriteAllText(ResultFile, "PASS\n" + string.Join("\n", passed)); }
        catch (Exception error) { File.WriteAllText(ResultFile, "FAIL\n" + error); }
    }

    static readonly List<string> passed = new();
    public static IReadOnlyList<string> Passed => passed;
    static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
        passed.Add(message);
    }

    [MenuItem("工具/教程/检查 Teach01 独立流程")]
    public static void Run()
    {
        passed.Clear();
        foreach (bool single in new[] { true, false })
        foreach (bool us in new[] { true, false })
        {
            var host = new Services { SingleCurrency = single, IsUS = us };
            using var flow = new Teach01Flow(host);
            var task = flow.RunAsync();
            Check(host.Blocked && host.Requests == 0, "Before game ready: block input without requesting reward");
            host.GameReady = true; host.Pump();
            Check(!host.Blocked && host.FrameworkBlocked && host.Requests == 0, "Game tutorial receives input before reward request");
            host.RewardState = NewbieGiftRequestState.NextLogin; host.Pump();
            Check(host.Requests == 1 && host.FrameworkBlocked, "First match starts HTTP while gameplay teaching remains protected");
            host.Reward.TrySetResult(true); host.Pump();
            Check(!host.Pages.ContainsKey(Teach01Page.Gift) && host.PopupCloses == 0, "Fast HTTP cannot interrupt gameplay teaching");
            host.BasicTutorialCompleted = true; host.Pump();
            Check(!host.FrameworkBlocked && host.Pages.Count == 0, "First selection releases HUD input but still waits for the 2x lesson");
            host.GameTutorialCompleted = true; host.Pump();
            Check(host.Requests == 1, "Further matches cannot repeat the reward request");
            Check(flow.CurrentStage == Teach01Flow.Stage.Gift && host.PopupCloses == 1, "Successful reward opens gift once");
            if (!single) host.Pages[Teach01Page.StarterWithdrawal] = new object();
            host.Pages.Remove(Teach01Page.Gift); host.Pump();
            if (!single)
            {
                Check(host.WithdrawMasks == (us ? 0 : 1), "US skips starter withdrawal target; other countries retain it");
                if (!us)
                {
                    host.Pages.Remove(Teach01Page.Mask);
                    host.Pages[Teach01Page.Withdrawal] = new object(); host.Pump();
                    Check(flow.IsRunning, "Wait for starter withdrawal dialog to close");
                    host.Pages.Remove(Teach01Page.Withdrawal); host.Pump();
                }
                host.Pages.Remove(Teach01Page.StarterWithdrawal); host.Pump();
            }
            Check(flow.CurrentStage == Teach01Flow.Stage.Currency && host.CurrencyMasks == 1, "Gift close reaches currency guide");
            Check(host.Tips.Contains(single ? "Teach_AllMoney:0:0.5" : "Teach_coin:-1:0.8"), "Keep branch text and position");
            host.Pages.Remove(Teach01Page.Mask);
            host.Pages[Teach01Page.RealWithdrawal] = new object(); host.Pump();
            if (single)
            {
                Check(flow.IsRunning && host.Saves == 0, "Single currency waits for real withdrawal close");
                host.Pages.Remove(Teach01Page.RealWithdrawal); host.Pump();
            }
            else Check(host.Pages.ContainsKey(Teach01Page.RealWithdrawal), "Dual currency leaves real withdrawal page open");
            Check(task.Status == UniTaskStatus.Succeeded && task.GetAwaiter().GetResult(), "All country/currency branches finish");
            Check(host.Saves == 1 && !flow.IsRunning && !host.Blocked && !host.FrameworkBlocked, "Completion saves once and restores input");
            Check(!flow.RunAsync().GetAwaiter().GetResult() && host.Requests == 1, "Repeated start does not request or award twice");
        }

        for (int stage = 0; stage < 8; stage++)
        {
            var host = new Services();
            var flow = new Teach01Flow(host);
            var task = flow.RunAsync();
            if (stage >= 1) { host.GameReady = true; host.Pump(); }
            if (stage >= 2) { host.RewardState = NewbieGiftRequestState.NextLogin; host.Pump(); }
            if (stage >= 3) host.Reward.TrySetResult(true);
            if (stage >= 4) { host.BasicTutorialCompleted = true; host.Pump(); }
            if (stage >= 5) { host.CanShowGift = false; host.GameTutorialCompleted = true; host.Pump(); }
            if (stage >= 6) { host.CanShowGift = true; host.Pump(); }
            if (stage >= 7) { host.Pages.Remove(Teach01Page.Gift); host.Pump(); }
            flow.Dispose(); host.Pump();
            Check(task.Status == UniTaskStatus.Succeeded && !task.GetAwaiter().GetResult(), "Cancellation terminates stage " + stage);
            Check(flow.CurrentStage == Teach01Flow.Stage.Cancelled && host.Saves == 0 &&
                  !host.Blocked && !host.FrameworkBlocked && host.Pages.Count == 0, "Cancellation cleans owned UI without completing save " + stage);
            host.Reward.TrySetResult(true); host.Pump();
            Check(host.Pages.Count == 0, "Late reward cannot reopen cancelled UI " + stage);
            flow.Dispose();
        }

        {
            var host = ReadyServices();
            using var flow = new Teach01Flow(host);
            var task = flow.RunAsync();
            Check(host.FrameworkBlockCalls == 0, "Completed game tutorial is never restarted");
            host.Reward.TrySetException(new InvalidOperationException("HTTP failed"));
            Check(!task.GetAwaiter().GetResult() && flow.CurrentStage == Teach01Flow.Stage.Failed &&
                  host.Saves == 0 && host.Pages.Count == 0 && !host.Blocked, "HTTP failure releases input without gift or completion save");
        }
        {
            var host = ReadyServices();
            using var flow = new Teach01Flow(host);
            var task = flow.RunAsync(); host.Reward.TrySetResult(true);
            // Same page type reopens before polling; it is a different open, not the owned gift.
            var replacement = new object(); host.Pages[Teach01Page.Gift] = replacement; host.Pump();
            Check(flow.CurrentStage == Teach01Flow.Stage.Currency, "Reopened page identity cannot trap old gift wait");
            flow.Dispose(); host.Pump();
            Check(ReferenceEquals(host.Pages[Teach01Page.Gift], replacement), "Cleanup preserves unrelated replacement page");
            Check(!task.GetAwaiter().GetResult(), "Replacement scenario cancels cleanly");
        }
        {
            var host = ReadyServices();
            using var flow = new Teach01Flow(host);
            var task = flow.RunAsync(); host.Reward.TrySetResult(true);
            host.Pages.Remove(Teach01Page.Gift); host.Pump();
            host.Pages.Remove(Teach01Page.Mask); host.Pump(); host.ExpirePageWait();
            Check(!task.GetAwaiter().GetResult() && flow.LastError is TimeoutException && host.Saves == 0,
                "Missing withdrawal page fails without infinite wait or false completion");
        }
        {
            var host = ReadyServices(); host.ThrowOnClose = true;
            using var flow = new Teach01Flow(host);
            var task = flow.RunAsync(); host.Reward.TrySetResult(true); flow.Dispose(); host.Pump();
            Check(!task.GetAwaiter().GetResult() && !host.Blocked && !host.FrameworkBlocked, "Close callback exception cannot retain input locks");
        }
        {
            var host = ReadyServices(); host.RewardState = NewbieGiftRequestState.NotTriggered;
            using var flow = new Teach01Flow(host);
            var task = flow.RunAsync();
            Check(host.Requests == 0 && !host.Blocked && !host.FrameworkBlocked, "Returning unfinished gift waits for first match without restarting gameplay teaching");
            host.RewardState = NewbieGiftRequestState.NextLogin; host.Pump();
            Check(host.Requests == 1 && host.Pages.Count == 0, "Completed tutorial still waits for slow HTTP response");
            host.Reward.TrySetResult(true); host.Pump();
            Check(host.Pages.ContainsKey(Teach01Page.Gift), "Slow HTTP opens gift after both prerequisites complete");
            flow.Dispose(); host.Pump();
            Check(!task.GetAwaiter().GetResult(), "Slow-response scenario cleans up");
        }
        foreach (bool rewardFirst in new[] { true, false })
        {
            var host = ReadyServices(); host.CanShowGift = false;
            using var flow = new Teach01Flow(host);
            var task = flow.RunAsync();
            if (rewardFirst) host.Reward.TrySetResult(true);
            host.Pump();
            Check(host.PopupCloses == 0 && host.Pages.Count == 0 && !host.FrameworkBlocked && !host.Blocked,
                "Victory settlement retains its UI and input while gift waits: " + rewardFirst);
            if (rewardFirst) { host.CanShowGift = true; host.Pump(); }
            else
            {
                host.CanShowGift = true; host.Pump();
                Check(host.PopupCloses == 0, "Victory can finish before delayed HTTP without triggering popup cleanup");
                // A new victory begins before the delayed response returns: re-evaluate the gate.
                host.CanShowGift = false; host.Reward.TrySetResult(true); host.Pump();
                Check(host.PopupCloses == 0, "Delayed HTTP rechecks current settlement state");
                host.CanShowGift = true; host.Pump();
            }
            Check(host.PopupCloses == 1 && host.Pages.ContainsKey(Teach01Page.Gift), "Gift resumes only after settlement and next gameplay are ready");
            flow.Dispose(); host.Pump();
            Check(!task.GetAwaiter().GetResult(), "Settlement scenario cleans up");
        }
        {
            var host = new Services { GameReady = true, RewardState = NewbieGiftRequestState.NextLogin };
            using var flow = new Teach01Flow(host);
            var task = flow.RunAsync();
            host.Reward.TrySetException(new TimeoutException("prefetch timeout")); host.Pump();
            Check(!task.GetAwaiter().GetResult() && !host.FrameworkBlocked && !host.Blocked && host.PendingWaits == 0,
                "Failed prefetch cancels the parallel tutorial wait and releases input");
            host.BasicTutorialCompleted = host.GameTutorialCompleted = true; host.Pump();
            Check(host.Pages.Count == 0 && host.Saves == 0 && host.Requests == 1, "Tutorial completion cannot revive a failed prefetch");
        }
        {
            var host = ReadyServices(); host.SettlementOnPopupClose = true;
            using var flow = new Teach01Flow(host);
            var task = flow.RunAsync(); host.Reward.TrySetResult(true); host.Pump();
            Check(host.PopupCloses == 1 && host.Pages.Count == 0 && flow.CurrentStage == Teach01Flow.Stage.WaitingForGiftWindow,
                "Popup close callback starting settlement postpones gift presentation");
            host.CanShowGift = true; host.Pump();
            Check(host.PopupCloses == 1 && host.Pages.ContainsKey(Teach01Page.Gift), "Resuming after close callback does not clear popups twice");
            flow.Dispose(); host.Pump();
            Check(!task.GetAwaiter().GetResult(), "Close-callback settlement scenario cleans up");
        }
        {
            var first = new Services { BasicTutorialCompleted = true, GameTutorialCompleted = true, GameReady = true, RewardState = NewbieGiftRequestState.NextLogin };
            using var firstFlow = new Teach01Flow(first);
            var firstTask = firstFlow.RunAsync();
            first.Reward.TrySetResult(false); first.Pump();
            Check(!firstTask.GetAwaiter().GetResult() && first.Requests == 1 && !first.GiftShown,
                "Online failure does not retry or show a gift in this session");
            var next = new Services { BasicTutorialCompleted = true, GameTutorialCompleted = true, GameReady = true, RewardState = first.RewardState };
            using var nextFlow = new Teach01Flow(next);
            var nextTask = nextFlow.RunAsync();
            Check(next.Requests == 1 && next.FrameworkBlockCalls == 0,
                "Next login automatically retries pending reward without a match");
            next.Reward.TrySetResult(true);
            Check(next.GiftShown && next.PopupCloses == 1, "Next-login success shows the normal gift");
        }
        {
            var host = new Services { BasicTutorialCompleted = true, GameTutorialCompleted = true, GameReady = true, CanRequestReward = false };
            using var flow = new Teach01Flow(host);
            var task = flow.RunAsync();
            host.RewardState = NewbieGiftRequestState.Background; host.Pump();
            Check(host.GiftShown && host.Requests == 0 && !host.FrameworkBlocked,
                "Offline first match shows gift once gameplay teaching is clear, without HTTP");
            host.Pages.Remove(Teach01Page.Gift); host.Pump();
            Check(flow.CurrentStage == Teach01Flow.Stage.RetryingReward && host.Tips.Count == 0 &&
                  host.CurrencyMasks == 0 && host.WithdrawMasks == 0 && host.Requests == 0,
                "Offline gift close skips money and withdrawal guides; waits for account");
            host.CanRequestReward = true; host.Pump();
            host.Reward.TrySetResult(false);
            host.Reward.TrySetResult(false);
            Check(host.Requests == 3 && host.PopupCloses == 1,
                "Offline pending reward retries sequentially after failures without redisplaying gift");
            host.Reward.TrySetResult(true);
            Check(task.GetAwaiter().GetResult() && host.RewardState == NewbieGiftRequestState.Succeeded &&
                  host.PopupCloses == 1 && host.Pages.Count == 0,
                "Background success completes with no gift replay");
        }
        {
            var host = new Services { BasicTutorialCompleted = true, GameTutorialCompleted = true, GameReady = true, RewardState = NewbieGiftRequestState.Background,
                GiftShown = true, CanRequestReward = false };
            using var flow = new Teach01Flow(host);
            var task = flow.RunAsync();
            Check(host.PopupCloses == 0 && host.Requests == 0 && host.FrameworkBlockCalls == 0,
                "Restored offline pending state requires neither a gift nor another match");
            host.CanRequestReward = true; host.Pump(); host.Reward.TrySetResult(true);
            Check(task.GetAwaiter().GetResult() && host.PopupCloses == 0,
                "Restored offline reward completes silently");
        }
        {
            var host = new Services { BasicTutorialCompleted = true, GameTutorialCompleted = true, GameReady = true, RewardState = NewbieGiftRequestState.Succeeded };
            using var flow = new Teach01Flow(host);
            var task = flow.RunAsync();
            Check(host.Requests == 0 && host.GiftShown,
                "Confirmed reward with unopened gift does not request the reward again");
        }
        foreach (bool inFlight in new[] { false, true })
        {
            var host = new Services { BasicTutorialCompleted = true, GameTutorialCompleted = true, GameReady = true, RewardState = NewbieGiftRequestState.Background,
                GiftShown = true, CanRequestReward = inFlight };
            using var flow = new Teach01Flow(host);
            var task = flow.RunAsync();
            host.IsSuspended = true;
            if (inFlight) host.Reward.TrySetResult(false);
            else host.Pump();
            Check(!task.GetAwaiter().GetResult() && flow.CurrentStage == Teach01Flow.Stage.Cancelled &&
                  host.Requests == (inFlight ? 1 : 0) && host.PopupCloses == 0,
                "Suspension stops background requests, including in-flight response: " + inFlight);
        }
        {
            var host = new Services { BasicTutorialCompleted = true, GameTutorialCompleted = true, GameReady = true, RewardState = NewbieGiftRequestState.Background,
                GiftShown = true };
            var flow = new Teach01Flow(host);
            var task = flow.RunAsync(); flow.Dispose(); host.Pump();
            host.Reward.TrySetResult(true);
            Check(!task.GetAwaiter().GetResult() && host.Requests == 1 && host.PopupCloses == 0,
                "Leaving during background request cannot restart requests or reopen gift");
        }
        {
            var saved = new TeachSaveData { newbieGiftRequest = NewbieGiftRequestState.Background, newbieGiftShown = true };
            saved.SetState(Teach01Flow.SaveKey, TeachUtil.FinishState);
            var restored = Newtonsoft.Json.JsonConvert.DeserializeObject<TeachSaveData>(Newtonsoft.Json.JsonConvert.SerializeObject(saved));
            Check(restored.newbieGiftRequest == NewbieGiftRequestState.Background && restored.newbieGiftShown &&
                  restored.GetState(Teach01Flow.SaveKey) == TeachUtil.FinishState,
                "Real teaching save serialization preserves pending reward, shown flag and completed teaching");
            var legacy = Newtonsoft.Json.JsonConvert.DeserializeObject<TeachSaveData>("{\"states\":{\"Teach_01\":100}}");
            Check(legacy.newbieGiftRequest == NewbieGiftRequestState.NotTriggered && !legacy.newbieGiftShown &&
                  legacy.GetState(Teach01Flow.SaveKey) == TeachUtil.FinishState,
                "Existing saves retain teaching completion and default the new fields safely");
        }
        {
            var host = new Services { GameReady = true, RewardState = NewbieGiftRequestState.Background, CanRequestReward = false };
            using var flow = new Teach01Flow(host);
            var task = flow.RunAsync();
            Check(host.Requests == 0 && !host.GiftShown && host.FrameworkBlocked,
                "Offline first match does not interrupt basic gameplay teaching");
            host.BasicTutorialCompleted = true; host.Pump();
            Check(!host.FrameworkBlocked && !host.GiftShown,
                "Offline gift still lets the native speed lesson finish");
            host.GameTutorialCompleted = true; host.CanShowGift = false; host.Pump();
            Check(!host.GiftShown && host.PopupCloses == 0,
                "Offline gift cannot replace a win settlement");
            host.CanShowGift = true; host.Pump();
            Check(host.GiftShown && host.Requests == 0, "Offline gift opens after native tutorials and settlement clear");
            host.IsSuspended = true; host.Pump();
            Check(!task.GetAwaiter().GetResult() && !host.Blocked && !host.FrameworkBlocked && host.Pages.Count == 0,
                "Suspension while an offline gift is open releases its owned UI");
        }
        {
            var host = ReadyServices(); host.RewardState = NewbieGiftRequestState.Succeeded;
            host.GiftShown = true;
            using var flow = new Teach01Flow(host);
            Check(flow.RunAsync().GetAwaiter().GetResult() && host.Requests == 0 && host.PopupCloses == 0 && host.CurrencyMasks == 0,
                "Already confirmed and shown reward never requests or grants another gift");
        }
        UnityEngine.Debug.Log("Teach01 checks passed: " + passed.Count);
    }

    static Services ReadyServices() => new()
    {
        GameReady = true, BasicTutorialCompleted = true, GameTutorialCompleted = true,
        RewardState = NewbieGiftRequestState.NextLogin
    };

    sealed class Services : ITeach01Services
    {
        public bool GameReady { get; set; }
        public NewbieGiftRequestState RewardState { get; set; }
        public bool GiftShown { get; set; }
        public bool CanRequestReward { get; set; } = true;
        public bool IsSuspended { get; set; }
        public bool BasicTutorialCompleted { get; set; }
        public bool GameTutorialCompleted { get; set; }
        public bool CanShowGift { get; set; } = true;
        public bool SingleCurrency { get; set; } = true;
        public bool IsUS { get; set; }
        public bool Blocked, FrameworkBlocked, ThrowOnClose, SettlementOnPopupClose;
        public int Requests, Saves, CurrencyMasks, WithdrawMasks, PopupCloses, FrameworkBlockCalls;
        public readonly Dictionary<Teach01Page, object> Pages = new();
        public readonly List<string> Tips = new();
        public UniTaskCompletionSource<bool> Reward = new();
        readonly List<(Func<bool> condition, CancellationToken token, float timeout, UniTaskCompletionSource source)> waits = new();
        public int PendingWaits => waits.Count;
        public void SetPreparationBlock(bool value) => Blocked = value;
        public void SetFrameworkInputBlocked(bool value) { FrameworkBlocked = value; if (value) FrameworkBlockCalls++; }
        public UniTask WaitUntil(Func<bool> condition, CancellationToken token, float timeoutSeconds = 0)
        {
            token.ThrowIfCancellationRequested();
            if (condition()) return UniTask.CompletedTask;
            var source = new UniTaskCompletionSource();
            waits.Add((condition, token, timeoutSeconds, source));
            return source.Task;
        }
        public void Pump()
        {
            for (int cycle = 0; cycle < 30; cycle++)
            {
                bool changed = false;
                foreach (var wait in waits.ToArray())
                {
                    if (!wait.token.IsCancellationRequested && !wait.condition()) continue;
                    waits.Remove(wait); changed = true;
                    if (wait.token.IsCancellationRequested) wait.source.TrySetCanceled(wait.token);
                    else wait.source.TrySetResult();
                }
                if (!changed) return;
            }
            throw new Exception("Unexpected wait loop");
        }
        public void ExpirePageWait()
        {
            foreach (var wait in waits.ToArray())
                if (wait.timeout > 0) { waits.Remove(wait); wait.source.TrySetException(new TimeoutException()); }
        }
        public UniTask Delay(float seconds, CancellationToken token) { token.ThrowIfCancellationRequested(); return UniTask.CompletedTask; }
        public async UniTask<bool> RequestReward(CancellationToken token)
        {
            Requests++;
            bool success = await Reward.Task.AttachExternalCancellation(token);
            Reward = new UniTaskCompletionSource<bool>();
            if (success) RewardState = NewbieGiftRequestState.Succeeded;
            return success;
        }
        public void ClosePopups() { PopupCloses++; if (SettlementOnPopupClose) CanShowGift = false; }
        UniTask<object> Open(Teach01Page page) { var owner = new object(); Pages[page] = owner; return UniTask.FromResult(owner); }
        public UniTask<object> OpenGift(CancellationToken token)
        {
            GiftShown = true;
            return Open(Teach01Page.Gift);
        }
        public UniTask<object> OpenMask(bool currency, CancellationToken token)
        { if (currency) CurrencyMasks++; else WithdrawMasks++; return Open(Teach01Page.Mask); }
        public UniTask<object> OpenTip(string text, int position, float height, CancellationToken token)
        { Tips.Add(text + ":" + position + ":" + height.ToString(System.Globalization.CultureInfo.InvariantCulture)); return Open(Teach01Page.Tip); }
        public object GetPageOwner(Teach01Page page) => Pages.TryGetValue(page, out var owner) ? owner : null;
        public void CloseOwnedPage(Teach01Page page, object owner)
        {
            if (ReferenceEquals(GetPageOwner(page), owner)) Pages.Remove(page);
            if (ThrowOnClose) throw new Exception("close failed");
        }
        public void SaveCompleted() => Saves++;
    }
}
#endif
