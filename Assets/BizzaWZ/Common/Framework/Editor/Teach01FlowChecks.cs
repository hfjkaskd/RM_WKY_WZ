#if UNITY_EDITOR && BIZZA_REAL_WITHDRAW
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEditor;

/// <summary>运行生产流程，控制 HTTP/页面时序；不访问网络、玩家存档或真实奖励。</summary>
public static class Teach01FlowChecks
{
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
            host.GameTutorialCompleted = true; host.Pump();
            Check(!host.FrameworkBlocked && host.Requests == 1 && !host.Pages.ContainsKey(Teach01Page.Gift), "Tutorial completes before HTTP; gift waits for response");
            host.Reward.TrySetResult();
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

        for (int stage = 0; stage < 5; stage++)
        {
            var host = new Services();
            var flow = new Teach01Flow(host);
            var task = flow.RunAsync();
            if (stage >= 1) { host.GameReady = true; host.Pump(); }
            if (stage >= 2) { host.GameTutorialCompleted = true; host.Pump(); }
            if (stage >= 3) host.Reward.TrySetResult();
            if (stage >= 4) { host.Pages.Remove(Teach01Page.Gift); host.Pump(); }
            flow.Dispose(); host.Pump();
            Check(task.Status == UniTaskStatus.Succeeded && !task.GetAwaiter().GetResult(), "Cancellation terminates stage " + stage);
            Check(flow.CurrentStage == Teach01Flow.Stage.Cancelled && host.Saves == 0 &&
                  !host.Blocked && !host.FrameworkBlocked && host.Pages.Count == 0, "Cancellation cleans owned UI without completing save " + stage);
            host.Reward.TrySetResult(); host.Pump();
            Check(host.Pages.Count == 0, "Late reward cannot reopen cancelled UI " + stage);
            flow.Dispose();
        }

        {
            var host = new Services { GameReady = true, GameTutorialCompleted = true };
            using var flow = new Teach01Flow(host);
            var task = flow.RunAsync();
            Check(host.FrameworkBlockCalls == 0, "Completed game tutorial is never restarted");
            host.Reward.TrySetException(new InvalidOperationException("HTTP failed"));
            Check(!task.GetAwaiter().GetResult() && flow.CurrentStage == Teach01Flow.Stage.Failed &&
                  host.Saves == 0 && host.Pages.Count == 0 && !host.Blocked, "HTTP failure releases input without gift or completion save");
        }
        {
            var host = new Services { GameReady = true, GameTutorialCompleted = true };
            using var flow = new Teach01Flow(host);
            var task = flow.RunAsync(); host.Reward.TrySetResult();
            // Same page type reopens before polling; it is a different open, not the owned gift.
            var replacement = new object(); host.Pages[Teach01Page.Gift] = replacement; host.Pump();
            Check(flow.CurrentStage == Teach01Flow.Stage.Currency, "Reopened page identity cannot trap old gift wait");
            flow.Dispose(); host.Pump();
            Check(ReferenceEquals(host.Pages[Teach01Page.Gift], replacement), "Cleanup preserves unrelated replacement page");
            Check(!task.GetAwaiter().GetResult(), "Replacement scenario cancels cleanly");
        }
        {
            var host = new Services { GameReady = true, GameTutorialCompleted = true };
            using var flow = new Teach01Flow(host);
            var task = flow.RunAsync(); host.Reward.TrySetResult();
            host.Pages.Remove(Teach01Page.Gift); host.Pump();
            host.Pages.Remove(Teach01Page.Mask); host.Pump(); host.ExpirePageWait();
            Check(!task.GetAwaiter().GetResult() && flow.LastError is TimeoutException && host.Saves == 0,
                "Missing withdrawal page fails without infinite wait or false completion");
        }
        {
            var host = new Services { GameReady = true, GameTutorialCompleted = true, ThrowOnClose = true };
            using var flow = new Teach01Flow(host);
            var task = flow.RunAsync(); host.Reward.TrySetResult(); flow.Dispose(); host.Pump();
            Check(!task.GetAwaiter().GetResult() && !host.Blocked && !host.FrameworkBlocked, "Close callback exception cannot retain input locks");
        }
        UnityEngine.Debug.Log("Teach01 checks passed: " + passed.Count);
    }

    sealed class Services : ITeach01Services
    {
        public bool GameReady { get; set; }
        public bool GameTutorialCompleted { get; set; }
        public bool SingleCurrency { get; set; } = true;
        public bool IsUS { get; set; }
        public bool Blocked, FrameworkBlocked, ThrowOnClose;
        public int Requests, Saves, CurrencyMasks, WithdrawMasks, PopupCloses, FrameworkBlockCalls;
        public readonly Dictionary<Teach01Page, object> Pages = new();
        public readonly List<string> Tips = new();
        public readonly UniTaskCompletionSource Reward = new();
        readonly List<(Func<bool> condition, CancellationToken token, float timeout, UniTaskCompletionSource source)> waits = new();
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
        public UniTask RequestReward(CancellationToken token) { Requests++; return Reward.Task.AttachExternalCancellation(token); }
        public void ClosePopups() => PopupCloses++;
        UniTask<object> Open(Teach01Page page) { var owner = new object(); Pages[page] = owner; return UniTask.FromResult(owner); }
        public UniTask<object> OpenGift(CancellationToken token) => Open(Teach01Page.Gift);
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
