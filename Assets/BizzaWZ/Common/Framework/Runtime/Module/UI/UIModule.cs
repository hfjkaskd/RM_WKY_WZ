using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Obfuz;
using Sirenix.OdinInspector;
using UnityEngine;
#if BIZZA_REAL_WITHDRAW
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

 
public class UIModule : BaseGameModule<UIModule>
{
    private const string RewardItemLayerName = "RewardItemLayer";

    [SerializeField] private RectTransform m_pageRoot;
    [SerializeField] private UIAnimationConfig m_animationConfig;
    [SerializeField] private RectTransform rewardItemLayer;
    public Transform fxContainer;

    private readonly Dictionary<string, UIPageBase> _prefabsById = new(StringComparer.Ordinal);
    private readonly Dictionary<string, UIPageRuntimeConfig> _configById = new(StringComparer.Ordinal);

    private List<UIPageBase> _pagesOpened;
    private List<UIPageBase> _pagePool;
    private List<RectTransform> _pageLayerRoots;

    private int _openingCount;
    private int _uiLifecycleVersion;
    private Canvas _uiCanvas;
    private RectTransform _cachedRewardItemLayer;
    private IUIPageLoader _pageLoader;


    public bool Opening => _openingCount > 0;
    public Camera UICamera => GetUICanvas().GetComponentInChildren<Camera>();
    public Canvas UICanvas => GetUICanvas();
    public RectTransform RewardItemLayer => GetRewardItemLayer();

    [ShowInInspector]
    public bool HasPopup => HasPageInLayer((int)E_UILayer.PopupLayer);

    internal bool CanPresentAutomaticPopup
    {
        get
        {
            if (automaticPopupPaused || !automaticPopupFocused ||
                _pagesOpened == null || _pagesOpened.Count == 0 || Opening || HasPopup) return false;
#if BIZZA_REAL_WITHDRAW && !COMMONGAME
            if (!RewardItemCollectFlow.CanPresentUI || TeachUtil.IsTeach) return false;
            var panel = GetPage<RealGamePanel>();
            if (panel == null || !panel.gameObject.activeInHierarchy) return false;
            if (!IsAutomaticPopupGameplayReady || World.Current.IsPause) return false;
#endif
            if (HasClosingPopup()) return false;
            return true;
        }
    }

    internal bool CanConfirmAutomaticPopup(UIPageBase page)
    {
        if (automaticPopupPaused || !automaticPopupFocused ||
            _pagesOpened == null || FindLastOpenedPage() != page || Opening) return false;
#if BIZZA_REAL_WITHDRAW && !COMMONGAME
        if (!RewardItemCollectFlow.CanPresentUI || TeachUtil.IsTeach) return false;
        if (!IsAutomaticPopupGameplayReady) return false;
#endif
        if (HasClosingPopup(page)) return false;
        return true;
    }

    private bool automaticPopupPaused;
    private bool automaticPopupFocused = true;
    private int automaticPopupClockVersion;
    internal int AutomaticPopupClockVersion
    {
        get
        {
#if BIZZA_REAL_WITHDRAW && !COMMONGAME
            return unchecked(automaticPopupClockVersion + RewardItemCollectFlow.PresentationClockVersion);
#else
            return automaticPopupClockVersion;
#endif
        }
    }
    internal int AutomaticPopupLifecycleVersion => _uiLifecycleVersion;

    private void OnApplicationPause(bool paused)
    {
        automaticPopupPaused = paused;
        automaticPopupClockVersion++;
    }

    private void OnApplicationFocus(bool focused)
    {
        automaticPopupFocused = focused;
        automaticPopupClockVersion++;
    }

    internal bool CanAdvanceAutomaticPopup(UIPageBase ownPage)
    {
        if (automaticPopupPaused || !automaticPopupFocused || _pagesOpened == null) return false;
        if (ownPage == null ? HasPopup : FindLastOpenedPage() != ownPage) return false;
#if BIZZA_REAL_WITHDRAW && !COMMONGAME
        if (!RewardItemCollectFlow.CanPresentUI || TeachUtil.IsTeach) return false;
        var panel = GetPage<RealGamePanel>();
        if (panel == null || !panel.gameObject.activeInHierarchy) return false;
        if (!IsAutomaticPopupGameplayReady) return false;
#endif
        if (HasClosingPopup(ownPage)) return false;
        return true;
    }

#if BIZZA_REAL_WITHDRAW && !COMMONGAME
    private bool IsAutomaticPopupGameplayReady =>
        World.Current != null && World.Current.CurGameMode != null &&
        World.Current.CurGameMode.GameModeType == E_GameModeType.GamePlay &&
        !FlowModule.AutomaticPopupFlowBlocked &&
        (TransitionBlock.Instance == null || !TransitionBlock.Instance.gameObject.activeInHierarchy);
#endif

    // ClosePage removes registration before the hide animation ends. Keep other
    // popup animations as blockers until their actual instances become inactive.
    private readonly List<UIPageBase> closingPopupPages = new();
    private bool HasClosingPopup(UIPageBase ownPage = null)
    {
        bool blocked = false;
        for (int i = closingPopupPages.Count - 1; i >= 0; i--)
        {
            var page = closingPopupPages[i];
            if (page == null || !page.gameObject.activeInHierarchy || !page.IsClosing)
                closingPopupPages.RemoveAt(i);
            else if (page != ownPage) blocked = true;
        }
        return blocked;
    }

    internal UniTask<bool> PrepareAutomaticPopupAsync(PageId pageId, PageOpenAttempt attempt) =>
        EnsurePageConfigLoaded(pageId, _uiLifecycleVersion, attempt);

    // One scope per call, never keyed just by PageId: the same prefab can have several instances.
    internal sealed class PageOpenAttempt : IDisposable
    {
        private readonly CancellationTokenSource cancellation = new();
        internal CancellationToken Token { get; }
        internal UIPageBase Page { get; private set; }
        internal bool OwnsPage { get; private set; }
        private object previousOwner;
        internal bool IsCancelled => Token.IsCancellationRequested;
        private bool disposed;

        internal PageOpenAttempt() { Token = cancellation.Token; }
        internal void Track(UIPageBase page, bool ownsPage)
        {
            // Instantiate/Awake can reenter business code and cancel the attempt. A newly
            // allocated instance still needs an owner so finally can destroy it.
            if (!ownsPage) Token.ThrowIfCancellationRequested();
            Page = page;
            OwnsPage = ownsPage;
            previousOwner = ownsPage ? null : page.OpenOwner;
            page.OpenOwner = this;
            Token.ThrowIfCancellationRequested();
        }
        internal bool StillOwnsPage => OwnsPage && Page != null && ReferenceEquals(Page.OpenOwner, this);
        internal void Commit() { previousOwner = null; }
        internal void RestoreBorrowedOwner()
        {
            if (!OwnsPage && Page != null && ReferenceEquals(Page.OpenOwner, this))
                Page.OpenOwner = previousOwner;
        }
        internal void Cancel() { if (!disposed) cancellation.Cancel(); }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            cancellation.Dispose();
        }
    }

    internal void AbortPageOpen(PageOpenAttempt attempt)
    {
        if (attempt == null) return;
        attempt.Cancel();
        if (!attempt.StillOwnsPage)
        {
            attempt.RestoreBorrowedOwner();
            return;
        }
        var page = attempt.Page;
        // Revoke ownership before calling arbitrary close hooks (which may open other pages).
        page.OpenOwner = null;
        bool registered = _pagesOpened != null && _pagesOpened.Remove(page);
        _pagePool?.Remove(page);
        try
        {
#if BIZZA_REAL_WITHDRAW
            RewardItemCollectFlow.CancelForPage(page);
#endif
            page.ForceClosePage_CallByFramework();
        }
        catch (Exception exception) { Debug.LogException(exception); }
        finally
        {
            if (page != null)
            {
                page.gameObject.SetActive(false);
                Destroy(page.gameObject);
            }
        }
        if (registered)
        {
            try { BizzaEventSystem.Emit(EventDefine.Frame.ClosePage, page.PageId); }
            catch (Exception exception) { Debug.LogException(exception); }
        }
    }

    private int m_advertisticsLimit = 1;
    public int m_curadvertistics = 0;
    public bool isStatistics => m_curadvertistics > 0;

    public void SetPageLoader(IUIPageLoader pageLoader)
    {
        _pageLoader = pageLoader;
    }

    public override void InitGameModule()
    {
#if UNITY_EDITOR
        Debug.Log($"[WhiteBootstrap] UIModule.InitGameModule begin pageRoot:{(m_pageRoot != null)} animationConfig:{(m_animationConfig != null)}");
#endif
        _pagesOpened = new List<UIPageBase>();
        _pagePool = new List<UIPageBase>();
        _pageLayerRoots = new List<RectTransform>();
        _pageLoader ??= CreateDefaultPageLoader();
#if UNITY_EDITOR
        Debug.Log($"[WhiteBootstrap] UIModule.InitGameModule loader:{_pageLoader.GetType().Name}");
#endif

        BuildLayerRoots();

        m_loadingHandlerPool = new Pool<LoadingHandler>(CreateLoadingHandler);
        m_loadingShow = new Dictionary<PageId, LoadingHandler>();
        m_fullScreenHandler = new LoadingHandler();
        m_fullScreenHandler.PoolReset();
#if BIZZA_REAL_WITHDRAW
        RewardItemCollectFlow.Initialize(this);
#endif
#if UNITY_EDITOR
        Debug.Log($"[WhiteBootstrap] UIModule.InitGameModule end layerCount:{_pageLayerRoots.Count}");
#endif
        var tipsPrefab = Resources.Load<GameObject>("BroadCastBar");
        var tipsContainer = transform.Find("TipsContainer");
        
        if (tipsPrefab != null && tipsContainer != null)
        {
            GameObject.Instantiate(tipsPrefab, tipsContainer);
        }
    }

    public override void ReleaseGameModule()
    {
#if DEBUG_MODE
        DebugClosePage();
#endif
#if BIZZA_REAL_WITHDRAW
        RewardItemCollectFlow.CancelAll();
#endif
        _cachedRewardItemLayer = null;
        ReleaseAllPage();
    }

    private static IUIPageLoader CreateDefaultPageLoader()
    {
#if BIZZA_REAL_WITHDRAW
        return new AddressablesUIPageLoader();
#else
        return new ResourcesUIPageLoader();
#endif
    }

    public UIAnimationConfig.UIAnimationConfigData GetUIAnimationConfigById(int id)
    {
        foreach (var config in m_animationConfig.Datas)
        {
            if (config.Id == id)
            {
                return config;
            }
        }

        return null;
    }

    private void BuildLayerRoots()
    {
        _pageLayerRoots.Clear();
        if (m_pageRoot == null)
        {
            Debug.LogError("UI page root is not assigned on UIModule prefab.");
            return;
        }

        int layerCount = Enum.GetValues(typeof(E_UILayer)).Length;
        for (int i = 0; i < layerCount; i++)
        {
            string layerName = ((E_UILayer)i).ToString();
            var layer = m_pageRoot.Find(layerName);
            if (layer != null && layer.TryGetComponent<RectTransform>(out var existing))
            {
                _pageLayerRoots.Add(existing);
                continue;
            }

            Debug.LogError($"UI layer root '{layerName}' is missing under '{m_pageRoot.name}'. Configure it on the GameCanvas prefab.");
            _pageLayerRoots.Add(null);
        }
    }

    public int GetLayerByName(string layerName)
    {
        if (string.IsNullOrEmpty(layerName)) return -1;
        return Enum.TryParse<E_UILayer>(layerName, out var layer) ? (int)layer : -1;
    }

    public bool HasPageInLayer(int layerId)
    {
        if (_pagesOpened == null) return false;
        foreach (var page in _pagesOpened)
        {
            var config = GetPageRuntimeConfig(page);
            if ((int)config.Layer == layerId)
            {
                return true;
            }
        }

        return false;
    }

    public T GetPage<T>() where T : UIPageBase
    {
        foreach (var page in _pagesOpened)
        {
            if (page.GetType() == typeof(T))
            {
                return (T)page;
            }
        }

        return default;
    }

    public UIPageBase GetPage(PageId pageId)
    {
        if (pageId.IsEmpty) return null;
        var targetId = ResolveCanonicalPageId(pageId);
        foreach (var page in _pagesOpened)
        {
            if (page.PageId == targetId)
            {
                return page;
            }
        }

        return null;
    }

    private PageId ResolveCanonicalPageId(PageId pageId)
    {
        if (pageId.IsEmpty) return PageId.Empty;
        return _configById.TryGetValue(pageId.Value, out var config) ? config.PageId : pageId;
    }

    public void OpenPageSync(PageId pageId)
    {
        OpenPage(pageId).Forget();
    }

    public UniTask<UIPageBase> OpenPage(PageId pageId) =>
        OpenPageCore<UIPageBase>(pageId, page => page.OpenPage_CallByFramework(), 0);

#if DEBUG_MODE
    private UIPageBase _debugOpenedPage;
    private object _debugOpenedPageOwner;
    private GameObject _debugPreviewObject;
    private int _debugPreviewRequestVersion;

    // GM uses the normal page lifecycle when possible. Pages requiring business data
    // are displayed as prefabs so every registered UI can still be inspected.
    public async UniTask DebugOpenPage(PageId pageId)
    {
        if (pageId.IsEmpty)
        {
            Debug.LogWarning("[GM UI] 请输入页面 ID。");
            return;
        }

        int requestVersion = ++_debugPreviewRequestVersion;
        try
        {
            if (!await EnsurePageConfigLoaded(pageId, _uiLifecycleVersion) ||
                this == null || requestVersion != _debugPreviewRequestVersion)
            {
                return;
            }

            if (!_configById.TryGetValue(pageId.Value, out var config) ||
                !_prefabsById.TryGetValue(config.PageId.Value, out var prefab) || prefab == null)
            {
                Debug.LogWarning($"[GM UI] 页面不存在: {pageId}");
                return;
            }

            ClearDebugPreview();
            if (prefab.OpenArgumentCount == 0)
            {
                var existingPage = GetPage(config.PageId);
                if (existingPage != null)
                {
                    Debug.Log($"[GM UI] 页面已打开: {config.PageId}");
                    return;
                }

                try
                {
                    var openedPage = await OpenPage(config.PageId);
                    if (requestVersion != _debugPreviewRequestVersion)
                    {
                        if (openedPage != null) ClosePage(openedPage);
                        return;
                    }

                    if (openedPage != null)
                    {
                        _debugOpenedPage = openedPage;
                        _debugOpenedPageOwner = openedPage.OpenOwner;
                        Debug.Log($"[GM UI] 已打开: {config.PageId}");
                        return;
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"[GM UI] 正常打开失败，改用预制体预览: {config.PageId}\n{exception}");
                }
            }

            if (requestVersion != _debugPreviewRequestVersion ||
                !TryGetPageLayerRoot(E_UILayer.SystemLayer, out var previewRoot))
            {
                return;
            }

            _debugPreviewObject = Instantiate(prefab.gameObject, previewRoot, false);
            _debugPreviewObject.name = $"[GM Preview] {config.PageId}";
            _debugPreviewObject.transform.SetAsLastSibling();
            _debugPreviewObject.SetActive(true);
            Debug.Log($"[GM UI] 正在预览预制体: {config.PageId}（需要业务参数的内容可能未填充）");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    public void DebugClosePage()
    {
        ++_debugPreviewRequestVersion;
        ClearDebugPreview();
    }

    private void ClearDebugPreview()
    {
        if (_debugOpenedPage != null)
        {
            if (ReferenceEquals(_debugOpenedPage.OpenOwner, _debugOpenedPageOwner))
                ClosePage(_debugOpenedPage);
            _debugOpenedPage = null;
        }
        _debugOpenedPageOwner = null;

        if (_debugPreviewObject != null)
        {
            Destroy(_debugPreviewObject);
            _debugPreviewObject = null;
        }
    }
#endif

    internal UniTask<UIPageBase> OpenAutomaticPopup(PageId pageId, PageOpenAttempt attempt) =>
        OpenPageCore<UIPageBase>(pageId, page => page.OpenPage_CallByFramework(), 0, attempt);

    internal UniTask<UIPageBase<T>> OpenAutomaticPopup<T>(PageId pageId, T data, PageOpenAttempt attempt) =>
        OpenPageCore<UIPageBase<T>>(pageId, page => page.OpenPage_CallByFramework(data), 1, attempt);

    public UniTask<UIPageBase<A>> OpenPage<A>(PageId pageId, A a) =>
        OpenPageCore<UIPageBase<A>>(pageId, page => page.OpenPage_CallByFramework(a), 1);

    public UniTask<UIPageBase<A, B>> OpenPage<A, B>(PageId pageId, A a, B b) =>
        OpenPageCore<UIPageBase<A, B>>(pageId, page => page.OpenPage_CallByFramework(a, b), 2);

    public UniTask<UIPageBase<A, B, C>> OpenPage<A, B, C>(PageId pageId, A a, B b, C c) =>
        OpenPageCore<UIPageBase<A, B, C>>(pageId, page => page.OpenPage_CallByFramework(a, b, c), 3);

    public UniTask<UIPageBase<A, B, C, D>> OpenPage<A, B, C, D>(PageId pageId, A a, B b, C c, D d) =>
        OpenPageCore<UIPageBase<A, B, C, D>>(pageId, page => page.OpenPage_CallByFramework(a, b, c, d), 4);

    public UniTask<UIPageBase<A, B, C, D, E>> OpenPage<A, B, C, D, E>(PageId pageId, A a, B b, C c, D d, E e) =>
        OpenPageCore<UIPageBase<A, B, C, D, E>>(pageId, page => page.OpenPage_CallByFramework(a, b, c, d, e), 5);

    private async UniTask<TPage> OpenPageCore<TPage>(PageId pageId, Action<TPage> initialize,
        int argumentCount, PageOpenAttempt attempt = null) where TPage : UIPageBase
    {
        bool localAttempt = attempt == null;
        attempt ??= new PageOpenAttempt();
        bool initialized = false;
        try
        {
            var rawPage = await _OpenPage(pageId, attempt);
            attempt.Token.ThrowIfCancellationRequested();
            if (rawPage == null) return null;
            if (!(rawPage is TPage page) || rawPage.OpenArgumentCount != argumentCount)
                throw new InvalidOperationException($"Page {pageId} does not accept the supplied arguments.");
            initialize(page);
            attempt.Token.ThrowIfCancellationRequested();
            if (page == null || page.IsClosing || !page.gameObject.activeInHierarchy) return null;
            initialized = true;
            attempt.Commit();
            return page;
        }
        catch (OperationCanceledException) when (attempt.IsCancelled) { return null; }
        finally
        {
            // Includes mismatched arguments, InitPage/event failures, and OnOpen/ShowPage exceptions.
            if (!initialized) AbortPageOpen(attempt);
            if (localAttempt) attempt.Dispose();
        }
    }

    private bool LayerMultiPages(E_UILayer layer)
    {
        return layer is not E_UILayer.BaseLayer;
 //        return layer is E_UILayer.PopupLayer or E_UILayer.TopLayer or E_UILayer.SystemLayer or E_UILayer.TeachLayer;
    }

    private bool LayerIgnoreLowLayerManage(E_UILayer layer)
    {
        return layer == E_UILayer.SystemLayer;
    }

    private UIPageRuntimeConfig GetPageRuntimeConfig(UIPageBase page)
    {
        if (_configById.TryGetValue(page.PageId.Value, out var config))
        {
            return config;
        }

        return UIPageRuntimeConfig.FromPage(page);
    }

    private async UniTask<bool> EnsurePageConfigLoaded(PageId pageId, int expectedLifecycleVersion, PageOpenAttempt attempt = null)
    {
        attempt?.Token.ThrowIfCancellationRequested();
        if (pageId.IsEmpty) return false;
        if (_configById.ContainsKey(pageId.Value))
        {
#if UNITY_EDITOR
            Debug.Log($"[WhiteBootstrap] UIModule.EnsurePageConfigLoaded cached page:{pageId}");
#endif
            return true;
        }

#if UNITY_EDITOR
        Debug.Log($"[WhiteBootstrap] UIModule.EnsurePageConfigLoaded load begin page:{pageId}");
#endif
        var load = _pageLoader.LoadPagePrefabAsync(pageId);
        var prefab = attempt == null ? await load : await load.AttachExternalCancellation(attempt.Token);
        attempt?.Token.ThrowIfCancellationRequested();
        if (this == null || expectedLifecycleVersion != _uiLifecycleVersion)
        {
            return false;
        }

        if (prefab == null)
        {
            LogLogger.LogError($"加载页面失败: {pageId}");
#if UNITY_EDITOR
            Debug.LogError($"[WhiteBootstrap] UIModule.EnsurePageConfigLoaded load failed page:{pageId}");
#endif
            return false;
        }

        RegisterPrefab(pageId, prefab);
#if UNITY_EDITOR
        Debug.Log($"[WhiteBootstrap] UIModule.EnsurePageConfigLoaded load end page:{pageId} prefab:{prefab.name}");
#endif
        return true;
    }

    private void RegisterPrefab(PageId requestId, UIPageBase prefab)
    {
        var canonicalId = prefab.PageId.IsEmpty ? requestId : prefab.PageId;
        if (canonicalId.IsEmpty)
        {
            canonicalId = new PageId(prefab.name);
        }

        var config = UIPageRuntimeConfig.FromPage(prefab);
        config.PageId = canonicalId;

        _prefabsById[canonicalId.Value] = prefab;
        _configById[canonicalId.Value] = config;

        if (!requestId.IsEmpty && requestId != canonicalId)
        {
            _configById[requestId.Value] = config;
            _prefabsById[requestId.Value] = prefab;
        }
    }

    private bool TryGetPageLayerRoot(E_UILayer layer, out RectTransform layerRoot)
    {
        int layerIndex = (int)layer;
        if (_pageLayerRoots != null
            && layerIndex >= 0
            && layerIndex < _pageLayerRoots.Count
            && _pageLayerRoots[layerIndex] != null)
        {
            layerRoot = _pageLayerRoots[layerIndex];
            return true;
        }

        layerRoot = null;
        Debug.LogError($"UI layer root '{layer}' is not configured. Check the GameCanvas prefab.");
        return false;
    }

    private void InitPage(UIPageBase pageBaseClone, UIPageRuntimeConfig config, RectTransform layerRoot)
    {
        if (!pageBaseClone.PreserveRectTransformOnOpen)
        {
            pageBaseClone.RectTransform.anchoredPosition = Vector2.zero;
            pageBaseClone.RectTransform.localScale = Vector3.one;
        }

        if (config.ZIndex >= 1000)
        {
            pageBaseClone.RectTransform.SetAsLastSibling();
        }
        else
        {
            bool min = true;
            for (int i = layerRoot.childCount - 1; i > -1; i--)
            {
                var child = layerRoot.GetChild(i);
                if (!child.TryGetComponent<UIPageBase>(out var childPage) || childPage == pageBaseClone)
                {
                    continue;
                }

                var childConfig = GetPageRuntimeConfig(childPage);
                if (config.ZIndex >= childConfig.ZIndex)
                {
                    pageBaseClone.RectTransform.SetSiblingIndex(i + 1);
                    min = false;
                    break;
                }
            }

            if (min)
            {
                pageBaseClone.RectTransform.SetAsFirstSibling();
            }
        }

        _pagesOpened.Add(pageBaseClone);
        pageBaseClone.gameObject.SetActive(true);
        BizzaEventSystem.Emit(EventDefine.Frame.OpenPage, config.PageId);
    }

    private async UniTask<UIPageBase> _OpenPage(PageId pageId, PageOpenAttempt attempt)
    {
        int openVersion = _uiLifecycleVersion;
#if UNITY_EDITOR
        Debug.Log($"[WhiteBootstrap] UIModule._OpenPage begin page:{pageId}");
#endif
        if (!await EnsurePageConfigLoaded(pageId, openVersion, attempt))
        {
#if UNITY_EDITOR
            Debug.LogError($"[WhiteBootstrap] UIModule._OpenPage config load failed page:{pageId}");
#endif
            return null;
        }

        if (attempt.IsCancelled || this == null || openVersion != _uiLifecycleVersion)
        {
            return null;
        }

        if (!_configById.TryGetValue(pageId.Value, out var config))
        {
            return null;
        }

        _openingCount++;
        try
        {
#if UNITY_EDITOR
            Debug.Log($"[WhiteBootstrap] UIModule._OpenPage config page:{pageId} canonical:{config.PageId} layer:{config.Layer} multi:{config.MultiPages}");
#endif

            if (attempt.IsCancelled || this == null || openVersion != _uiLifecycleVersion)
            {
                return null;
            }

            if (!TryGetPageLayerRoot(config.Layer, out var layerRoot))
            {
#if UNITY_EDITOR
                Debug.LogError($"[WhiteBootstrap] UIModule._OpenPage missing layer root page:{pageId} layer:{config.Layer}");
#endif
                return null;
            }

            if (!LayerMultiPages(config.Layer) || !config.MultiPages)
            {
                for (int i = _pagesOpened.Count - 1; i > -1; i--)
                {
                    var page = _pagesOpened[i];
                    if (page == null)
                    {
                        _pagesOpened.RemoveAt(i);
                        continue;
                    }

                    var openedConfig = GetPageRuntimeConfig(page);
                    if (openedConfig.PageId == config.PageId)
                    {
#if UNITY_EDITOR
                        Debug.Log($"[WhiteBootstrap] UIModule._OpenPage reuse opened page:{pageId}");
#endif
                        attempt.Track(page, false);
                        return page;
                    }
                }
            }

            if (attempt.IsCancelled || this == null || openVersion != _uiLifecycleVersion)
            {
                return null;
            }

            UIPageBase pageBaseClone = null;
            for (int i = _pagePool.Count - 1; i > -1; i--)
            {
                var page = _pagePool[i];
                if (page == null)
                {
                    _pagePool.RemoveAt(i);
                    continue;
                }

                var poolConfig = GetPageRuntimeConfig(page);
                if (poolConfig.PageId == config.PageId)
                {
                    if (attempt.IsCancelled || this == null || openVersion != _uiLifecycleVersion)
                    {
                        return null;
                    }

                    pageBaseClone = page;
                    _pagePool.RemoveAt(i);
#if UNITY_EDITOR
                    Debug.Log($"[WhiteBootstrap] UIModule._OpenPage reuse pool page:{pageId}");
#endif
                    break;
                }
            }

            if (!pageBaseClone)
            {
                if (attempt.IsCancelled || this == null || openVersion != _uiLifecycleVersion)
                {
                    return null;
                }

                if (!_prefabsById.TryGetValue(config.PageId.Value, out var prefab) || prefab == null)
                {
                    return null;
                }

#if UNITY_EDITOR
                Debug.Log($"[WhiteBootstrap] UIModule._OpenPage instantiate page:{pageId} prefab:{prefab.name} layerRoot:{layerRoot.name}");
#endif
                pageBaseClone = Instantiate(prefab, layerRoot);
            }

            attempt.Track(pageBaseClone, true);

            if (!LayerMultiPages(config.Layer))
            {
                if (attempt.IsCancelled || this == null || openVersion != _uiLifecycleVersion)
                {
                    return null;
                }

                for (int i = _pagesOpened.Count - 1; i > -1; i--)
                {
                    if (attempt.IsCancelled || this == null || openVersion != _uiLifecycleVersion)
                    {
                        return null;
                    }

                    var page = _pagesOpened[i];
                    if (page == null)
                    {
                        _pagesOpened.RemoveAt(i);
                        continue;
                    }

                    var openedConfig = GetPageRuntimeConfig(page);
                    if (openedConfig.Layer == config.Layer)
                    {
                        ClosePage(page);
                        i = Mathf.Min(i, _pagesOpened.Count);
                    }
                    else if ((int)config.Layer < (int)openedConfig.Layer && !LayerIgnoreLowLayerManage(openedConfig.Layer))
                    {
                        ClosePage(page);
                        i = Mathf.Min(i, _pagesOpened.Count);
                    }
                }
            }

            if (attempt.IsCancelled || this == null || openVersion != _uiLifecycleVersion)
            {
                return null;
            }

            attempt.Token.ThrowIfCancellationRequested();
            InitPage(pageBaseClone, config, layerRoot);
            attempt.Token.ThrowIfCancellationRequested();
            if (pageBaseClone.Layer == E_UILayer.PopupLayer)
            {
                RecoverAdvertistics();
            }
#if UNITY_EDITOR
            Debug.Log($"[WhiteBootstrap] UIModule._OpenPage end page:{pageId} clone:{pageBaseClone.name}");
#endif
            return pageBaseClone;
        }
        finally
        {
            _openingCount--;
        }
    }

    public void RecoverAdvertistics()
    {
        m_curadvertistics = m_advertisticsLimit;
    } 

    public void ClosePage(UIPageBase pageBase)
    {
        if (pageBase == null)
        {
            Debug.LogError("试图关闭一个null的UIPage");
            return;
        }

        if (!_pagesOpened.Remove(pageBase))
        {
            return;
        }

        if (pageBase.Layer == E_UILayer.PopupLayer && !closingPopupPages.Contains(pageBase))
            closingPopupPages.Add(pageBase);

#if BIZZA_REAL_WITHDRAW
        RewardItemCollectFlow.CancelForPage(pageBase);
#endif
        pageBase.ClosePage_CallByFramework();
        BizzaEventSystem.Emit(EventDefine.Frame.ClosePage, pageBase.PageId);
    }

    public void ClosePage(PageId pageId, bool onlyLast = false)
    {
        if (pageId.IsEmpty) return;
        var targetId = ResolveCanonicalPageId(pageId);
        for (int i = _pagesOpened.Count - 1; i > -1; i--)
        {
            var page = _pagesOpened[i];
            if (page == null)
            {
                _pagesOpened.RemoveAt(i);
                continue;
            }

            if (page.PageId == targetId)
            {
                ClosePage(page);
                i = Mathf.Min(i, _pagesOpened.Count);
                if (onlyLast) break;
            }
        }
    }

    public void CloseLayerAllPage(int layer)
    {
        for (int i = _pagesOpened.Count - 1; i > -1; i--)
        {
            var page = _pagesOpened[i];
            if (page == null)
            {
                _pagesOpened.RemoveAt(i);
                continue;
            }

            var config = GetPageRuntimeConfig(page);
            if ((int)config.Layer == layer)
            {
                ClosePage(page);
                i = Mathf.Min(i, _pagesOpened.Count);
            }
        }
    }

    public void CloseAndDestroyLayerAllPage(int layer)
    {
        _uiLifecycleVersion++;
        var destroyTargets = new HashSet<GameObject>();
        for (int i = _pagesOpened.Count - 1; i > -1; i--)
        {
            var page = _pagesOpened[i];
            if (page == null)
            {
                _pagesOpened.RemoveAt(i);
                continue;
            }

            var config = GetPageRuntimeConfig(page);
            if ((int)config.Layer == layer)
            {
                _pagesOpened.RemoveAt(i);
                ForceCloseAndCollectDestroyTarget(page, destroyTargets, true);
                i = Mathf.Min(i, _pagesOpened.Count);
            }
        }

        for (int i = _pagePool.Count - 1; i > -1; i--)
        {
            var page = _pagePool[i];
            if (page == null)
            {
                _pagePool.RemoveAt(i);
                continue;
            }

            var config = GetPageRuntimeConfig(page);
            if ((int)config.Layer == layer)
            {
                _pagePool.RemoveAt(i);
                ForceCloseAndCollectDestroyTarget(page, destroyTargets, false);
                i = Mathf.Min(i, _pagePool.Count);
            }
        }

        if (_pageLayerRoots.TryGetItem(layer, out var root) && root != null)
        {
            CollectChildDestroyTargets(root, destroyTargets);
        }

        DestroyCollectedTargets(destroyTargets);
    }

    private void ForceCloseAndCollectDestroyTarget(
        UIPageBase page,
        HashSet<GameObject> destroyTargets,
        bool emitCloseEvent)
    {
        if (page == null)
        {
            return;
        }

        var pageId = page.PageId;
#if BIZZA_REAL_WITHDRAW
        RewardItemCollectFlow.CancelForPage(page);
#endif
        page.ForceClosePage_CallByFramework();
        destroyTargets.Add(page.gameObject);
        if (emitCloseEvent)
        {
            BizzaEventSystem.Emit(EventDefine.Frame.ClosePage, pageId);
        }
    }

    private static void CollectPageDestroyTarget(
        UIPageBase page,
        HashSet<GameObject> destroyTargets)
    {
        if (page != null)
        {
            destroyTargets.Add(page.gameObject);
        }
    }

    private static void CollectChildDestroyTargets(
        Transform root,
        HashSet<GameObject> destroyTargets)
    {
        if (root == null)
        {
            return;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            destroyTargets.Add(root.GetChild(i).gameObject);
        }
    }

    private static void DestroyCollectedTargets(HashSet<GameObject> destroyTargets)
    {
        foreach (var target in destroyTargets)
        {
            if (target != null)
            {
                UnityEngine.Object.Destroy(target);
            }
        }
    }

    public void CloseAllPage(PageId reShowPageId = default)
    {
        for (int i = 0; i < _pageLayerRoots.Count; i++)
        {
            CloseLayerAllPage(i);
        }

        if (!reShowPageId.IsEmpty)
        {
            OpenPageSync(reShowPageId);
        }
    }

    public void ClosePage_CallByFramework(UIPageBase uiPage)
    {
        if (uiPage == null)
        {
            return;
        }

        var config = GetPageRuntimeConfig(uiPage);
        uiPage.gameObject.SetActive(false);
        if (!config.Cache)
        {
            Destroy(uiPage.gameObject);
        }
        else if (!_pagePool.Contains(uiPage))
        {
            uiPage.PoolReset();
            _pagePool.Add(uiPage);
        }
    }

    public bool PageIsOpen(PageId pageId)
    {
        if (pageId.IsEmpty) return false;
        var targetId = ResolveCanonicalPageId(pageId);
        for (int i = _pagesOpened.Count - 1; i > -1; i--)
        {
            if (_pagesOpened[i].PageId == targetId)
            {
                return true;
            }
        }

        return false;
    }

    public UIPageBase FindOpenedPage(PageId pageId)
    {
        if (pageId.IsEmpty) return null;
        var targetId = ResolveCanonicalPageId(pageId);
        for (int i = _pagesOpened.Count - 1; i > -1; i--)
        {
            if (_pagesOpened[i].PageId == targetId)
            {
                return _pagesOpened[i];
            }
        }

        return null;
    }

    public UIPageBase FindLastOpenedPage()
    {
        return _pagesOpened.Count > 0 ? _pagesOpened[^1] : null;
    }

    public void ReleasePage(IEnumerable<PageId> pageIds)
    {
        foreach (var pageId in pageIds)
        {
            ReleasePage(pageId);
        }
    }

    public void ReleasePage(PageId pageId)
    {
        _uiLifecycleVersion++;
        if (pageId.IsEmpty) return;
        var targetId = ResolveCanonicalPageId(pageId);
        var destroyTargets = new HashSet<GameObject>();
        for (int i = _pagesOpened.Count - 1; i > -1; i--)
        {
            var page = _pagesOpened[i];
            if (page == null)
            {
                _pagesOpened.RemoveAt(i);
                continue;
            }

            if (page.PageId == targetId)
            {
                _pagesOpened.RemoveAt(i);
                CollectPageDestroyTarget(page, destroyTargets);
            }
        }

        for (int i = _pagePool.Count - 1; i > -1; i--)
        {
            var page = _pagePool[i];
            if (page == null)
            {
                _pagePool.RemoveAt(i);
                continue;
            }

            var config = GetPageRuntimeConfig(page);
            if (config.PageId == targetId)
            {
                _pagePool.RemoveAt(i);
                CollectPageDestroyTarget(page, destroyTargets);
            }
        }

        _prefabsById.Remove(pageId.Value);
        _configById.Remove(pageId.Value);
        _prefabsById.Remove(targetId.Value);
        _configById.Remove(targetId.Value);
        DestroyCollectedTargets(destroyTargets);
        _pageLoader?.ReleasePagePrefab(pageId);
        if (targetId != pageId)
        {
            _pageLoader?.ReleasePagePrefab(targetId);
        }
    }

    public void ReleaseAllPage()
    {
#if BIZZA_REAL_WITHDRAW
        RewardItemCollectFlow.CancelAll();
#endif
        _uiLifecycleVersion++;
        var destroyTargets = new HashSet<GameObject>();
        for (int i = _pagesOpened.Count - 1; i >= 0; i--)
        {
            var page = _pagesOpened[i];
            if (page != null)
            {
                CollectPageDestroyTarget(page, destroyTargets);
            }
        }

        for (int i = _pagePool.Count - 1; i >= 0; i--)
        {
            var page = _pagePool[i];
            if (page != null)
            {
                CollectPageDestroyTarget(page, destroyTargets);
            }
        }

        _pagesOpened.Clear();
        _pagePool.Clear();
        _prefabsById.Clear();
        _configById.Clear();
        DestroyCollectedTargets(destroyTargets);
        _pageLoader?.ReleaseAllPagePrefabs();
    }

    public Canvas GetUICanvas()
    {
        if (!_uiCanvas)
        {
            _uiCanvas = m_pageRoot.GetComponentInParent<Canvas>();
        }

        return _uiCanvas;
    }

    private RectTransform GetRewardItemLayer()
    {
        if (rewardItemLayer != null)
        {
            return rewardItemLayer;
        }

        if (_cachedRewardItemLayer != null)
        {
            return _cachedRewardItemLayer;
        }

        Transform layer = transform.Find(RewardItemLayerName);
        if (layer == null && m_pageRoot != null)
        {
            layer = m_pageRoot.Find(RewardItemLayerName);
        }

        _cachedRewardItemLayer = layer as RectTransform;
        if (_cachedRewardItemLayer != null)
        {
            return _cachedRewardItemLayer;
        }

        return fxContainer as RectTransform;
    }

    public float GetPixelSize(float sceneLength, bool isVertical = false)
    {
        var screenPositionA = Camera.main.WorldToScreenPoint(Vector3.zero);
        var screenPositionB = Camera.main.WorldToScreenPoint(new Vector3(sceneLength, 0, 0));
        return Vector3.Distance(screenPositionA, screenPositionB);
    }

    #region Loading Module

    private Pool<LoadingHandler> m_loadingHandlerPool;
    private Dictionary<PageId, LoadingHandler> m_loadingShow;
    private LoadingHandler m_fullScreenHandler;

    private LoadingHandler CreateLoadingHandler()
    {
        return new LoadingHandler();
    }

    public void HideLoading(PageId loadingType)
    {
        if (!m_loadingShow.TryGetValue(loadingType, out _))
        {
            return;
        }

        ClosePage(loadingType, true);
        m_loadingShow.Remove(loadingType);
    }

    #endregion

    private void Update()
    {
        if (AutoPopupQueue.Shared.HasWork) AutoPopupQueue.Shared.Tick(this);
        #if BIZZA_REAL_WITHDRAW
        if (TeachUtil.IsTeach)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitCurrentView();
        }
        #endif
    }

    private void ExitCurrentView()
    {
        int count = _pagesOpened.Count;
        if (count == 0) return;
        var page = _pagesOpened[count - 1];
        var config = GetPageRuntimeConfig(page);
        if (config.NativeClose)
        {
            ClosePage(page.PageId);
        }
    }
}

 
public class LoadingHandler : IPoolElement
{
    public PageId PageType { get; set; }
    public float Process { get; set; }

    public void PoolReset()
    {
        Process = 0;
        PageType = PageId.Empty;
    }
}

public interface IUIPageLoader
{
    UniTask<UIPageBase> LoadPagePrefabAsync(PageId pageId);
    void ReleasePagePrefab(PageId pageId);
    void ReleaseAllPagePrefabs();
}

public sealed class ResourcesUIPageLoader : IUIPageLoader
{
    private readonly string _rootPath;

    public ResourcesUIPageLoader(string rootPath = "Prefabs/UI")
    {
        _rootPath = rootPath?.Trim('/') ?? "Prefabs/UI";
    }

    public UniTask<UIPageBase> LoadPagePrefabAsync(PageId pageId)
    {
        if (pageId.IsEmpty)
        {
            return UniTask.FromResult<UIPageBase>(null);
        }

        string path = $"{_rootPath}/{pageId.Value}";
        var prefab = Resources.Load<GameObject>(path);
        var pagePrefab = prefab != null ? prefab.GetComponent<UIPageBase>() : null;
        if (pagePrefab != null)
        {
            return UniTask.FromResult(pagePrefab);
        }

#if UNITY_EDITOR
        pagePrefab = FindEditorPagePrefab(pageId);
        if (pagePrefab != null)
        {
            return UniTask.FromResult(pagePrefab);
        }

        Debug.LogError($"[WhiteBootstrap] UI prefab not found at Resources/{path} or editor assets");
#endif

        return UniTask.FromResult<UIPageBase>(null);
    }

    public void ReleasePagePrefab(PageId pageId)
    {
    }

    public void ReleaseAllPagePrefabs()
    {
    }

#if UNITY_EDITOR
    private static UIPageBase FindEditorPagePrefab(PageId pageId)
    {
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in guids)
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null || !prefab.TryGetComponent<UIPageBase>(out var page))
            {
                continue;
            }

            if (page.PageId == pageId)
            {
                return page;
            }
        }

        return null;
    }
#endif
}

#if BIZZA_REAL_WITHDRAW
public sealed class AddressablesUIPageLoader : IUIPageLoader
{
    private const string AddressablePrefix = "UIPanel/";
    private readonly Dictionary<string, AsyncOperationHandle<GameObject>> _handlesById = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _releaseVersionById = new(StringComparer.Ordinal);
    private int _releaseAllVersion;

    public async UniTask<UIPageBase> LoadPagePrefabAsync(PageId pageId)
    {
        if (pageId.IsEmpty)
        {
            return null;
        }

        if (_handlesById.TryGetValue(pageId.Value, out var cachedHandle)
            && cachedHandle.IsValid()
            && cachedHandle.Status == AsyncOperationStatus.Succeeded)
        {
            return cachedHandle.Result != null ? cachedHandle.Result.GetComponent<UIPageBase>() : null;
        }

        string pageKey = pageId.Value;
        if (_handlesById.ContainsKey(pageKey))
        {
            _handlesById.Remove(pageKey);
        }

        int loadReleaseAllVersion = _releaseAllVersion;
        int loadReleaseVersion = GetPageReleaseVersion(pageKey);
        string address = $"{AddressablePrefix}{pageKey}";
        var handle = Addressables.LoadAssetAsync<GameObject>(address);
        await handle.Task;

        if (loadReleaseAllVersion != _releaseAllVersion
            || loadReleaseVersion != GetPageReleaseVersion(pageKey))
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }

            return null;
        }

        if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }

            return null;
        }

        if (_handlesById.TryGetValue(pageKey, out cachedHandle)
            && cachedHandle.IsValid()
            && cachedHandle.Status == AsyncOperationStatus.Succeeded)
        {
            Addressables.Release(handle);
            return cachedHandle.Result != null ? cachedHandle.Result.GetComponent<UIPageBase>() : null;
        }

        _handlesById[pageKey] = handle;
        return handle.Result.GetComponent<UIPageBase>();
    }

    public void ReleasePagePrefab(PageId pageId)
    {
        if (pageId.IsEmpty)
        {
            return;
        }

        string pageKey = pageId.Value;
        _releaseVersionById[pageKey] = GetPageReleaseVersion(pageKey) + 1;
        if (_handlesById.TryGetValue(pageKey, out var handle) && handle.IsValid())
        {
            Addressables.Release(handle);
        }

        _handlesById.Remove(pageKey);
    }

    public void ReleaseAllPagePrefabs()
    {
        _releaseAllVersion++;
        foreach (var handle in _handlesById.Values)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }

        _handlesById.Clear();
        _releaseVersionById.Clear();
    }

    private int GetPageReleaseVersion(string pageKey)
    {
        return _releaseVersionById.TryGetValue(pageKey, out var version) ? version : 0;
    }
}
#endif

internal struct UIPageRuntimeConfig
{
    public PageId PageId;
    public E_UILayer Layer;
    public bool MultiPages;
    public bool Cache;
    public bool NativeClose;
    public int ZIndex;

    public static UIPageRuntimeConfig FromPage(UIPageBase page)
    {
        return new UIPageRuntimeConfig
        {
            PageId = page.PageId,
            Layer = page.Layer,
            MultiPages = page.MultiPages,
            Cache = page.Cache,
            NativeClose = page.NativeClose,
            ZIndex = page.ZIndex
        };
    }
}
