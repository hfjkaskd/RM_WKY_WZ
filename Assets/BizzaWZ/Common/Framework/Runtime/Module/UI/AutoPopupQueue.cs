using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Obfuz;
using UnityEngine;

/// <summary>Implement on a popup whose content loads after OnOpen.</summary>
public interface IAutoPopupContent
{
    bool ContentReady { get; }
    bool ContentFailed { get; }
}

/// <summary>
/// Serializes automatic popups on Unity's main thread. Keys are reserved while pending or visible.
/// UI availability is supplied by UIModule; business eligibility belongs to each request.
/// </summary>
[ObfuzIgnore(ObfuzScope.MethodBody)]
public sealed class AutoPopupQueue
{
    public static AutoPopupQueue Shared { get; } = new();

    private sealed class Request
    {
        internal PageId PageId;
        internal string Key;
        internal Func<UIModule, UIModule.PageOpenAttempt, UniTask<UIPageBase>> Open;
        internal Func<bool> IsValid;
        internal Action OnShown;
        internal float RetryAt;
        internal int Failures;
    }

    private readonly List<Request> pending = new();
    private readonly HashSet<string> keys = new(StringComparer.Ordinal);
    private Request current;
    private UIPageBase visiblePage;
    private UIModule presentingUI;
    private int presentingLifecycleVersion;
    private bool opening;
    private bool confirmed;
    private int generation;

    private UIModule.PageOpenAttempt openAttempt;
    private enum Phase { Resources, Opening, Content }
    private Phase phase;
    private float phaseElapsed, phaseLimit, lastClock;
    private bool clockRunning;
    private bool waitingForContent;
    private int clockVersion;
    private float resourceTimeout = 30f, openTimeout = 15f, contentTimeout = 30f;

    // Foreground work time, not time spent queued or behind ads/other gameplay flows.
    public float ResourceTimeoutSeconds { get => resourceTimeout; set => resourceTimeout = ValidateTimeout(value); }
    public float OpenTimeoutSeconds { get => openTimeout; set => openTimeout = ValidateTimeout(value); }
    public float ContentTimeoutSeconds { get => contentTimeout; set => contentTimeout = ValidateTimeout(value); }
    private static float ValidateTimeout(float value)
    {
        if (value <= 0f || float.IsNaN(value) || float.IsInfinity(value))
            throw new ArgumentOutOfRangeException(nameof(value), "Popup timeouts must be finite and positive.");
        return value;
    }

    public bool HasWork => current != null || pending.Count != 0;

    /// <param name="key">Business event ID; defaults to the page ID.</param>
    /// <param name="isValid">Rechecked before opening and confirming; false discards an expired request.</param>
    /// <param name="onShown">Called once after the page and its content are visible. Save one-time flags here.</param>
    public bool Enqueue(PageId pageId, string key = null, Func<bool> isValid = null, Action onShown = null)
    {
        return Add(pageId, key, (ui, attempt) => ui.OpenAutomaticPopup(pageId, attempt), isValid, onShown);
    }

    /// <summary>For UIPageBase&lt;T&gt;. Pass a value snapshot when data may change while waiting.</summary>
    public bool Enqueue<T>(PageId pageId, T data, string key = null,
        Func<bool> isValid = null, Action onShown = null)
    {
        return Add(pageId, key, async (ui, attempt) => await ui.OpenAutomaticPopup(pageId, data, attempt), isValid, onShown);
    }

    private bool Add(PageId pageId, string key, Func<UIModule, UIModule.PageOpenAttempt, UniTask<UIPageBase>> open,
        Func<bool> isValid, Action onShown)
    {
        if (pageId.IsEmpty) throw new ArgumentException("Automatic popups require a page ID.", nameof(pageId));
        key ??= pageId.Value;
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Popup keys cannot be empty.", nameof(key));
        if (!keys.Add(key)) return false;
        pending.Add(new Request { PageId = pageId, Key = key, Open = open, IsValid = isValid, OnShown = onShown });
        Debug.Log($"[AutoPopup] queued page={pageId}");
        return true;
    }

    public void Tick(UIModule ui)
    {
        if (current != null && (presentingUI == null || presentingUI != ui ||
            presentingUI.AutomaticPopupLifecycleVersion != presentingLifecycleVersion))
            FinishCurrent(!confirmed);
        if (opening && !CheckTimeout()) return;
        if (current != null)
        {
            if (visiblePage == null || !visiblePage.gameObject.activeInHierarchy)
            {
                FinishCurrent(!confirmed);
            }
            else if (!confirmed)
            {
                if (!ReferenceEquals(visiblePage.OpenOwner, openAttempt))
                {
                    // A newer open owns this instance now. Never confirm or clean up its UI.
                    FinishCurrent(false);
                }
                else if (!IsValid(current))
                {
                    // Expiry is terminal; keep ownership until the hide animation finishes.
                    confirmed = true;
                    CloseVisiblePage();
                }
                else if (waitingForContent && CheckTimeout()) { }
                else if (visiblePage.IsClosing ||
                    (visiblePage is IAutoPopupContent content && content.ContentFailed))
                {
                    FinishCurrent(true);
                }
                else if (visiblePage is IAutoPopupContent waiting && !waiting.ContentReady)
                {
                    // The content clock was sampled above, including a late successful callback.
                }
                else if (!visiblePage.IsClosing &&
                    (!(visiblePage is IAutoPopupContent ready) || ready.ContentReady) &&
                    ui != null && ui.CanConfirmAutomaticPopup(visiblePage))
                {
                    confirmed = true;
                    Debug.Log($"[AutoPopup] shown page={current.PageId}");
                    // Commit before calling business code: a callback failure must not duplicate the page.
                    try { current.OnShown?.Invoke(); }
                    catch (Exception exception) { Debug.LogException(exception); }
                }
            }
            if (current != null) return;
        }

        if (ui == null || !ui.CanPresentAutomaticPopup) return;
        for (int i = 0; i < pending.Count;)
        {
            var request = pending[i];
            if (!IsValid(request))
            {
                pending.RemoveAt(i);
                keys.Remove(request.Key);
                continue;
            }
            if (Time.unscaledTime < request.RetryAt) { i++; continue; }
            pending.RemoveAt(i);
            current = request;
            presentingUI = ui;
            presentingLifecycleVersion = ui.AutomaticPopupLifecycleVersion;
            openAttempt = new UIModule.PageOpenAttempt();
            OpenAsync(request, ui, openAttempt, generation).Forget();
            return;
        }
    }

    private bool IsCurrent(Request request, UIModule.PageOpenAttempt attempt, int version) =>
        version == generation && ReferenceEquals(current, request) && ReferenceEquals(openAttempt, attempt);

    private void BeginPhase(Phase next, float timeout)
    {
        phase = next;
        phaseLimit = timeout;
        phaseElapsed = 0f;
        lastClock = Time.realtimeSinceStartup;
        clockVersion = presentingUI != null ? presentingUI.AutomaticPopupClockVersion : 0;
        clockRunning = presentingUI != null && presentingUI.CanAdvanceAutomaticPopup(openAttempt?.Page);
    }

    private bool CheckTimeout()
    {
        if (current == null) return false;
        float now = Time.realtimeSinceStartup;
        bool allowed = presentingUI != null && presentingUI.CanAdvanceAutomaticPopup(openAttempt?.Page);
        int version = presentingUI != null ? presentingUI.AutomaticPopupClockVersion : clockVersion;
        if (allowed && clockRunning && version == clockVersion)
            phaseElapsed += Mathf.Max(0f, now - lastClock);
        lastClock = now;
        clockVersion = version;
        clockRunning = allowed;
        if (phaseElapsed < phaseLimit) return false;
        Debug.LogWarning($"[AutoPopup] timeout page={current.PageId}, phase={phase}, seconds={phaseLimit}");
        FinishCurrent(true);
        return true;
    }

    private async UniTask OpenAsync(Request request, UIModule ui, UIModule.PageOpenAttempt attempt, int version)
    {
        opening = true;
        BeginPhase(Phase.Resources, resourceTimeout);
        try
        {
            bool prepared = await ui.PrepareAutomaticPopupAsync(request.PageId, attempt);
            if (!IsCurrent(request, attempt, version) || CheckTimeout()) return;
            if (!IsValid(request)) { FinishCurrent(false); return; }
            if (!prepared || ui == null) { FinishCurrent(true); return; }
            if (!ui.CanPresentAutomaticPopup)
            {
                pending.Insert(0, request);
                ResetCurrent();
                return;
            }
            BeginPhase(Phase.Opening, openTimeout);
            var page = await request.Open(ui, attempt);
            // UI open scope prevents late registration and owns rollback; never close by PageId.
            if (!IsCurrent(request, attempt, version) || CheckTimeout()) return;
            visiblePage = page;
            if (page == null) { FinishCurrent(true); return; }
            BeginPhase(Phase.Content, contentTimeout);
            waitingForContent = page is IAutoPopupContent content && !content.ContentReady;
        }
        catch (OperationCanceledException) when (attempt.IsCancelled)
        {
            if (IsCurrent(request, attempt, version)) FinishCurrent(true);
        }
        catch (Exception exception)
        {
            if (IsCurrent(request, attempt, version))
            {
                FinishCurrent(true);
                Debug.LogException(exception);
            }
        }
        finally
        {
            if (IsCurrent(request, attempt, version)) opening = false;
        }
    }

    private static bool IsValid(Request request)
    {
        try { return request.IsValid?.Invoke() ?? true; }
        catch (Exception exception) { Debug.LogException(exception); return false; }
    }

    private void FinishCurrent(bool retry)
    {
        var request = current;
        int version = generation;
        ResetCurrent(abort: !confirmed);
        // Close hooks can clear the queue/change accounts and enqueue a new session's work.
        if (request == null || version != generation) return;
        bool valid = retry && IsValid(request);
        if (version != generation) return;
        if (valid)
        {
            // A failing page yields to other requests instead of blocking the whole queue.
            request.Failures++;
            request.RetryAt = Time.unscaledTime + Mathf.Min(30f, Mathf.Pow(2f, Mathf.Min(request.Failures - 1, 5)));
            pending.Add(request);
            Debug.LogWarning($"[AutoPopup] retry page={request.PageId}, attempt={request.Failures}");
        }
        else keys.Remove(request.Key);
    }

    private void ResetCurrent(bool abort = false)
    {
        var attempt = openAttempt;
        var ui = presentingUI;
        // Detach first: cancellation can synchronously resume an old async continuation.
        current = null;
        openAttempt = null;
        visiblePage = null;
        presentingUI = null;
        confirmed = false;
        opening = false;
        waitingForContent = false;
        if (attempt == null) return;
        try
        {
            if (abort && ui != null) ui.AbortPageOpen(attempt);
            else attempt.Cancel();
        }
        finally { attempt.Dispose(); }
    }

    private void CloseVisiblePage()
    {
        if (presentingUI != null && visiblePage != null && !visiblePage.IsClosing)
            presentingUI.ClosePage(visiblePage);
    }

    /// <summary>Clear on account replacement or logout, not on an ordinary level transition.</summary>
    public void Clear()
    {
        generation++;
        pending.Clear();
        keys.Clear();
        ResetCurrent(abort: true);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay() => Shared.Clear();
}
