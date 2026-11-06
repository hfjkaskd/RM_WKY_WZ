#if BIZZA_REAL_WITHDRAW
using System;
using Bizza.FlyMoney;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class RewardItemCollectFlow : MonoBehaviour
{
    public const int FreeConcurrencyLimit = 16;
    private static RewardItemCollectFlow instance;
    public static int Generation { get; private set; }
    private readonly Request[] requests = new Request[FlyMoneyPlayer.HardConcurrencyLimit];
    private readonly Request[] freeRequests = new Request[FreeConcurrencyLimit];
    private FlyMoneyPlayer player;
    private RewardFlyResources resources;
    private RectTransform rect;
    private Canvas canvas;
    private bool stopping;
    private bool background;
    private bool focused = true;
    private bool skipResumeFrame;
    private RewardAdVisibility adVisibility;
    private readonly PendingReward[] pendingRewards = new PendingReward[8];
    private int pendingCount;
    private int nextId;
    private int screenWidth;
    private int screenHeight;
    internal static int ActiveCount => ActiveAdCount + ActiveFreeCount;
    internal static int ActiveAdCount => instance == null ? 0 : CountActive(instance.requests);
    internal static int ActiveFreeCount => instance == null ? 0 : CountActive(instance.freeRequests);
    internal static int PendingCount => instance == null ? 0 : instance.pendingCount;
    private bool ForegroundBlocked => background || !focused;
    private bool PresentationBlocked => ForegroundBlocked || (adVisibility != null && adVisibility.IsShowing);
    internal static bool CanPresentUI => instance != null && instance.isActiveAndEnabled &&
        !instance.PresentationBlocked && !instance.skipResumeFrame;
    internal static int PresentationClockVersion => instance?.adVisibility?.Revision ?? 0;
    private bool IsBlocked(RewardCollectAnimation animation) =>
        animation == RewardCollectAnimation.Legacy ? ForegroundBlocked : PresentationBlocked;
    private Request[] GetRequests(RewardCollectAnimation animation) =>
        animation == RewardCollectAnimation.Legacy ? freeRequests : requests;

    private sealed class PendingReward
    {
        internal ItemEntry gold;
        internal ItemEntry dollar;
        internal ItemUtils.RewardPresentation first;
        internal ItemUtils.RewardPresentation second;
        internal float readyTime;
        internal void Complete() { first?.Complete(false); second?.Complete(false); }
    }

    internal sealed class Request
    {
        internal RewardItemCollectFlow flow;
        internal E_ItemType itemType;
        internal Transform target;
        internal Vector3 position;
        internal bool uiPosition;
        internal RewardCollectAnimation animation;
        internal RewardFxScope.Stamp scope;
        internal Action callback;
        internal int generation;
        internal int id;
        internal float delay;
        internal float elapsed;
        internal float lifetime;
        internal bool started;
        internal bool ended;
        internal bool notified;
        internal GameObject legacyRoot;
        internal bool native;
        internal bool IsValid => !ended && flow != null && flow.isActiveAndEnabled &&
            generation == Generation && target != null && target.gameObject.activeInHierarchy && scope.IsValid;

        internal void Notify(bool arrival)
        {
            if (notified) return;
            notified = true;
            Action complete = callback;
            callback = null;
            InvokeSafely(complete);
            if (arrival && IsValid)
            {
                try
                {
                    if (!native) VFXUtils.PlayItemCollectArriveFeedback();
                    else
                    {
                        VibrationUtils.VibrateStableClick(E_VibrateType.Light);
                        if (SoundManager.Instance != null) VFXUtils.PlayGetCoinSound(itemType);
                    }
                }
                catch (Exception exception) { Debug.LogException(exception); }
            }
        }

        internal void End(bool completed)
        {
            if (ended) return;
            // Release the budget before invoking arbitrary business code.
            ended = true;
            if (flow != null)
            {
                Request[] channel = flow.GetRequests(animation);
                for (int i = 0; i < channel.Length; i++)
                    if (ReferenceEquals(channel[i], this)) channel[i] = null;
            }
            VFXUtils.EndItemCollectFx(animation);
            try
            {
                if (legacyRoot != null) legacyRoot.SetActive(false);
                if (flow != null && flow.player != null) flow.player.Cancel(id);
            }
            catch (Exception exception) { Debug.LogException(exception); }
            finally { Notify(false); }
        }
    }

    public static void Initialize(UIModule ui)
    {
        if (instance != null || ui == null) return;
        RectTransform parent = ui.RewardItemLayer;
        if (parent == null) parent = ui.UICanvas.transform as RectTransform;
        if (parent == null) return;
        var go = new GameObject("Reward Collect Flow", typeof(RectTransform), typeof(Canvas));
        go.layer = parent.gameObject.layer;
        var transform = (RectTransform)go.transform;
        transform.SetParent(parent, false);
        transform.anchorMin = Vector2.zero;
        transform.anchorMax = Vector2.one;
        transform.offsetMin = Vector2.zero;
        transform.offsetMax = Vector2.zero;
        instance = go.AddComponent<RewardItemCollectFlow>();
        instance.rect = transform;
        instance.canvas = go.GetComponent<Canvas>();
        instance.player = go.AddComponent<FlyMoneyPlayer>();
        instance.player.AutoAdvance = false;
        instance.adVisibility = new RewardAdVisibility();
        instance.resources = Resources.Load<RewardFlyResources>("Bizza/RewardFlyResources");
        instance.screenWidth = Screen.width;
        instance.screenHeight = Screen.height;
        if (instance.resources != null)
        {
            var bank = instance.resources;
            instance.player.PrepareSprite(bank.paperBR);
            instance.player.PrepareSprite(bank.bundleBR);
            instance.player.PrepareSprite(bank.paperID);
            instance.player.PrepareSprite(bank.bundleID);
            instance.player.PrepareSprite(bank.paperUS);
            instance.player.PrepareSprite(bank.bundleUS);
            instance.player.PrepareSound(bank.burstSound);
        }
        BizzaEventSystem.On(EventDefine.RealWithdraw.PlayCurrentFly, instance.OnAdReward);
        SceneManager.sceneLoaded += instance.OnSceneLoaded;
        SceneManager.sceneUnloaded += instance.OnSceneUnloaded;
    }

    internal static void Play(E_ItemType type, Vector3 position, Action complete, bool uiPosition,
        Transform target, RewardCollectAnimation animation, float delay)
    {
        Request accepted = null;
        try
        {
            if (instance == null) Initialize(UIModule.Instance);
            if (instance == null || !instance.isActiveAndEnabled || instance.stopping)
            { InvokeSafely(complete); return; }
            if (target == null) VFXUtils.itemFlyTarget.TryGetValue(type, out target);
            if (target == null || !target.gameObject.activeInHierarchy ||
                !Finite(position.x) || !Finite(position.y) || !Finite(position.z))
            { InvokeSafely(complete); return; }
            int slot = -1;
            Request[] channel = instance.GetRequests(animation);
            for (int i = 0; i < channel.Length; i++)
                if (channel[i] == null) { slot = i; break; }
            if (slot < 0)
            { InvokeSafely(complete); return; }
            var request = new Request
            {
                flow = instance, itemType = type, position = position, uiPosition = uiPosition,
                target = target, scope = RewardFxScope.Capture(target), animation = animation,
                callback = complete, generation = Generation, id = ++instance.nextId,
                delay = Finite(delay) ? Mathf.Clamp(delay, 0f, 2f) : 0f
            };
            if (!VFXUtils.TryBeginItemCollectFx(type, string.Empty, animation))
            { InvokeSafely(complete); return; }
            accepted = request;
            if (request.id == 0) request.id = ++instance.nextId;
            channel[slot] = request;
            if (request.delay == 0f && !instance.IsBlocked(animation) && !instance.skipResumeFrame)
                instance.StartRequest(request);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (accepted != null) accepted.End(false);
            else InvokeSafely(complete);
        }
    }

    private void StartRequest(Request request)
    {
        if (!request.IsValid) { request.End(false); return; }
        if (IsBlocked(request.animation)) return;
        request.started = true;
        try
        {
            if (request.animation != RewardCollectAnimation.BurstCollect || !RewardItemCollectFxConfig.Instance.useNativeAdRewardFx)
            {
                VFXUtils.PlayLegacyItemCollectFx(request).Forget();
                return;
            }
            Sprite paper;
            Sprite bundle;
            bool cash = request.itemType == E_ItemType.Dollar || request.itemType == E_ItemType.WithDrawDanDollar ||
                (request.itemType == E_ItemType.Gold && Bizza.Sdk.ChannelConfig.Instance.real_CustomConfig.singleCurrencyMode);
            if (cash && resources != null)
                resources.Resolve(AccountModule.CountryType, out paper, out bundle);
            else
            {
                paper = bundle = VFXUtils.ResolveItemCollectSprite(request.itemType, request.target);
            }
            Vector2 origin = new Vector2(request.position.x, request.position.y);
            if ((!request.uiPosition && !VFXUtils.TryGetAnchoredPosition(request.position, rect, canvas, out origin)) ||
                !VFXUtils.TryGetAnchoredPosition(request.target.position, rect, canvas, out Vector2 destination))
            { request.End(false); return; }
            var config = RewardItemCollectFxConfig.Instance;
            request.native = true;
            // Keep this flight's endpoint fixed; currency-bar arrival tweens may move the icon.
            FlyMoneySettings settings = config.GetNativeSettings(AccountModule.CountryType, cash);
            player.concurrentLimit = config.MaxConcurrentFx;
            SyncSound();
            if (!player.TryPlay(paper, bundle, origin, destination, settings, (uint)request.id,
                    reason => request.End(reason == FlyMoneyEndReason.Completed), request.target,
                    () => request.Notify(true), request.id)) request.End(false);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            request.End(false);
        }
    }

    private void Update()
    {
        if (screenWidth != Screen.width || screenHeight != Screen.height)
        {
            screenWidth = Screen.width;
            screenHeight = Screen.height;
            CancelAll();
        }
        SyncSound();
        bool skip = skipResumeFrame;
        skipResumeFrame = false;
        float wallDelta = ForegroundBlocked || skip || !Finite(Time.unscaledDeltaTime) ? 0f : Mathf.Max(0f, Time.unscaledDeltaTime);
        float adDelta = PresentationBlocked ? 0f : wallDelta;
        if (adDelta > 0f) PresentPendingReward(adDelta);
        if (this == null || stopping || !isActiveAndEnabled) return;
        AdvanceRequests(freeRequests, wallDelta);
        if (this == null || stopping || !isActiveAndEnabled) return;
        AdvanceRequests(requests, PresentationBlocked ? 0f : adDelta);
        SyncSound();
        if (player != null && adDelta > 0f && !PresentationBlocked) player.Advance(Mathf.Min(adDelta, 0.1f));
    }

    private void AdvanceRequests(Request[] channel, float wallDelta)
    {
        float delta = Mathf.Min(wallDelta, 0.1f);
        for (int i = 0; i < channel.Length; i++)
        {
            Request request = channel[i];
            if (request == null) continue;
            if (!request.IsValid) { request.End(false); continue; }
            request.lifetime += wallDelta;
            if (request.lifetime > 8f) { request.End(false); continue; }
            if (delta <= 0f) continue;
            if (!request.started)
            {
                request.delay -= delta;
                if (request.delay <= 0f) StartRequest(request);
            }
            else request.elapsed += delta;
        }
    }

    private void SyncSound()
    {
        if (player == null) return;
        var sound = SoundManager.Instance;
        player.SoundEnabled = sound != null && !sound.IsSFXMuted();
        player.SoundVolume = sound != null ? sound.sfxVolume * 0.65f : 0f;
        player.Paused = PresentationBlocked;
    }

    public static void CancelAll()
    {
        Generation++;
        RewardItemCollectFlow current = instance;
        if (current == null || current.stopping) return;
        current.stopping = true;
        try
        {
            while (current.pendingCount > 0) current.TakePendingReward().Complete();
            for (int i = 0; i < current.requests.Length; i++) current.requests[i]?.End(false);
            for (int i = 0; i < current.freeRequests.Length; i++) current.freeRequests[i]?.End(false);
            if (current.player != null) current.player.StopAll();
        }
        finally { current.stopping = false; }
    }

    public static void CancelForPage(UIPageBase page)
    {
        if (page == null) return;
        RewardFxScope.Invalidate(page.gameObject);
        RewardItemCollectFlow current = instance;
        if (current == null) return;
        for (int i = 0; i < current.requests.Length; i++)
        {
            Request request = current.requests[i];
            if (request != null && request.target != null && request.target.IsChildOf(page.transform)) request.End(false);
        }
        for (int i = 0; i < current.freeRequests.Length; i++)
        {
            Request request = current.freeRequests[i];
            if (request != null && request.target != null && request.target.IsChildOf(page.transform)) request.End(false);
        }
    }

    private static int CountActive(Request[] channel)
    {
        int count = 0;
        for (int i = 0; i < channel.Length; i++) if (channel[i] != null) count++;
        return count;
    }

    private void OnApplicationPause(bool paused) { background = paused; skipResumeFrame = true; SyncSound(); }
    private void OnApplicationFocus(bool hasFocus) { focused = hasFocus; skipResumeFrame = true; SyncSound(); }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) { CancelAll(); }
    private void OnSceneUnloaded(Scene scene) { CancelAll(); }
    private void OnDisable() { if (instance == this) CancelAll(); }
    private void OnDestroy()
    {
        adVisibility?.Dispose();
        BizzaEventSystem.Off(EventDefine.RealWithdraw.PlayCurrentFly, OnAdReward);
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        if (instance == this) { CancelAll(); instance = null; }
    }

    private void OnAdReward(string id, double gold)
    {
        bool hasQueuedDollar = !string.IsNullOrEmpty(id) && CurrencyBar.CurrentQueue.ContainsKey(id);
        if (!hasQueuedDollar && !Bizza.Sdk.ChannelConfig.Instance.real_CustomConfig.singleCurrencyMode) return;
        float money = hasQueuedDollar ? CurrencyBar.OnDequeue(id) : 0f;
        var a = new ItemEntry { Type = E_ItemType.Gold, Count = (float)gold };
        var b = new ItemEntry { Type = E_ItemType.Dollar, Count = money };
        var parameters = new AddItemParam
        {
            playAnim = true, isAd = false, bUiPos = false,
            animation = RewardCollectAnimation.BurstCollect,
            rewardId = string.IsNullOrEmpty(id) ? null : "ad:" + id,
            source = DoubleGetRewardPanel.GetItemSource(DoubleGetRewardPanel.E_UseScene.Ad)
        };
        ItemUtils.RewardPresentation first = ItemUtils.GrantReward(a, parameters);
        ItemUtils.RewardPresentation second = ItemUtils.GrantReward(b, parameters);
        if (first == null && second == null) return;
        var pending = new PendingReward { gold = a, dollar = b, first = first, second = second };
        if (this == null || !isActiveAndEnabled || stopping || pendingCount == pendingRewards.Length)
        { pending.Complete(); return; }
        // Saving is independent of presentation. The bounded queue never holds SDK success/failure callbacks.
        pendingRewards[pendingCount++] = pending;
    }

    private PendingReward TakePendingReward()
    {
        PendingReward pending = pendingRewards[0];
        pendingCount--;
        Array.Copy(pendingRewards, 1, pendingRewards, 0, pendingCount);
        pendingRewards[pendingCount] = null;
        return pending;
    }

    private void PresentPendingReward(float delta)
    {
        if (pendingCount == 0) return;
        PendingReward pending = pendingRewards[0];
        pending.readyTime += delta;
        if (pending.readyTime > 8f) { TakePendingReward().Complete(); return; }
        int count = (pending.first != null ? 1 : 0) + (pending.second != null ? 1 : 0);
        if (!VFXUtils.CanBeginItemCollectFx(count)) return;
        try
        {
            var bar = BroadcastBarController.Instance;
            if (bar == null) { TakePendingReward().Complete(); return; }
            if (!bar.CanShowRewardImmediately) return;
            bool shown = bar.ShowRewardImmediately(pending.gold, pending.dollar, out Vector3 fromA, out Vector3 fromB);
            // Showing the UI can synchronously close a page or destroy this manager.
            if (!IsPending(pending) || !shown) return;
            TakePendingReward();
            pending.first?.Play(fromA, bar.RewardFlyDelay);
            pending.second?.Play(fromB, bar.RewardFlyDelay);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (IsPending(pending)) TakePendingReward();
            pending.Complete();
        }
    }

    private bool IsPending(PendingReward pending) => this != null && isActiveAndEnabled && !stopping &&
        pendingCount > 0 && ReferenceEquals(pendingRewards[0], pending);

    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    private static void InvokeSafely(Action action)
    {
        try { action?.Invoke(); }
        catch (Exception exception) { Debug.LogException(exception); }
    }
}

// Read-only presentation observer. It never grants rewards or changes an adapter's business callbacks.
internal sealed class RewardAdVisibility : IDisposable
{
    internal int Revision { get; private set; }
    private bool? reward;
    private bool? inter;
    private bool? splash;
    internal bool IsShowing => (reward ?? BizzaSdk.Ad.IsRewardShowing) ||
        (inter ?? BizzaSdk.Ad.IsInterShowing) || (splash ?? BizzaSdk.Ad.IsSplashShowing);

    internal RewardAdVisibility()
    {
        BizzaEventSystem.On(EventDefine.AdEvent.RewardAdStart, OnRewardStart);
        BizzaEventSystem.On(EventDefine.AdEvent.InterAdStart, OnInterStart);
        BizzaEventSystem.On(EventDefine.AdEvent.SplashAdStart, OnSplashStart);
#if BIZZA_ENABLE_MAX
        MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnRewardDisplayed;
        MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardHidden;
        MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardFailed;
        MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnInterDisplayed;
        MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterHidden;
        MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnInterFailed;
        MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent += OnSplashDisplayed;
        MaxSdkCallbacks.AppOpen.OnAdHiddenEvent += OnSplashHidden;
        MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent += OnSplashFailed;
#endif
    }

    private void OnRewardStart() { Revision++; reward = null; inter = null; }
    private void OnInterStart() { Revision++; inter = null; }
    private void OnSplashStart() { Revision++; splash = null; }
#if BIZZA_ENABLE_MAX
    private void OnRewardDisplayed(string id, MaxSdkBase.AdInfo info) { Revision++; reward = true; }
    private void OnRewardHidden(string id, MaxSdkBase.AdInfo info) { Revision++; reward = false; }
    private void OnRewardFailed(string id, MaxSdkBase.ErrorInfo error, MaxSdkBase.AdInfo info) { Revision++; reward = false; }
    private void OnInterDisplayed(string id, MaxSdkBase.AdInfo info) { Revision++; inter = true; }
    private void OnInterHidden(string id, MaxSdkBase.AdInfo info) { Revision++; inter = false; }
    private void OnInterFailed(string id, MaxSdkBase.ErrorInfo error, MaxSdkBase.AdInfo info) { Revision++; inter = false; }
    private void OnSplashDisplayed(string id, MaxSdkBase.AdInfo info) { Revision++; splash = true; }
    private void OnSplashHidden(string id, MaxSdkBase.AdInfo info) { Revision++; splash = false; }
    private void OnSplashFailed(string id, MaxSdkBase.ErrorInfo error, MaxSdkBase.AdInfo info) { Revision++; splash = false; }
#endif

    public void Dispose()
    {
        BizzaEventSystem.Off(EventDefine.AdEvent.RewardAdStart, OnRewardStart);
        BizzaEventSystem.Off(EventDefine.AdEvent.InterAdStart, OnInterStart);
        BizzaEventSystem.Off(EventDefine.AdEvent.SplashAdStart, OnSplashStart);
#if BIZZA_ENABLE_MAX
        MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent -= OnRewardDisplayed;
        MaxSdkCallbacks.Rewarded.OnAdHiddenEvent -= OnRewardHidden;
        MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent -= OnRewardFailed;
        MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent -= OnInterDisplayed;
        MaxSdkCallbacks.Interstitial.OnAdHiddenEvent -= OnInterHidden;
        MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent -= OnInterFailed;
        MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent -= OnSplashDisplayed;
        MaxSdkCallbacks.AppOpen.OnAdHiddenEvent -= OnSplashHidden;
        MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent -= OnSplashFailed;
#endif
    }
}
#endif
