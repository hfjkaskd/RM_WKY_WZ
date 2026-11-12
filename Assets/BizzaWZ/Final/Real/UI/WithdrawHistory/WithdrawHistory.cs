#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections.Generic;
using Bizza;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class UIPageIds
{
    public static readonly PageId WithdrawHistory = "WithdrawHistory";
}


public class WithdrawHistory : UIPageBase
{
    public WithdrawHistoryItem item;
    public Transform root;
    public GameObject emptyHint;

    public RectTransform rectTransform;

    [SerializeField] private BizzaButton closeButton;
    [SerializeField] private RectTransform statusRoot;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private BizzaButton retryButton;
    [SerializeField] private Vector2 loadingStatusPosition;
    [SerializeField, Min(24f)] private float loadingStatusFontSize = 36f;
    [SerializeField, Min(1f)] private float contentTimeoutSeconds = 20f;
    [SerializeField] private UIContentStateView contentView = new();

    private readonly List<WithdrawHistoryItem> items = new();
    private readonly UIContentLoadSession content = new();
    private ScrollRect scrollView;
    private CanvasGroup listGroup;
    private Vector2 failedStatusPosition;

    protected override void OnAwake()
    {
        base.OnAwake();
        closeButton.onClick.AddListener(CloseSelf);
        scrollView = root.GetComponentInParent<ScrollRect>();
        listGroup = scrollView.GetComponent<CanvasGroup>();
        if (listGroup == null) listGroup = scrollView.gameObject.AddComponent<CanvasGroup>();
        retryButton.onClick.AddListener(Retry);
        failedStatusPosition = statusText.rectTransform.anchoredPosition;
        contentView.Bind(statusText, retryButton);
        content.StateChanged += OnContentStateChanged;
        OnContentStateChanged(UIContentState.Closed);
    }

    protected override void OnOpen()
    {
        BizzaEventSystem.Set(EventDefine.Frame.LanguageChange, RefreshStatusText, true);
        OnRefresh();
    }

    private void OnRefresh()
    {
        if (IsClosing || !gameObject.activeInHierarchy) return;
        scrollView.StopMovement();
        int version = content.Begin(Time.unscaledTime, contentTimeoutSeconds);
        try
        {
            AccountModule.Instance.Request_WithdrawalRecordRequest(response => Refresh(version, response));
        }
        catch (Exception exception)
        {
            if (AcceptResponse(version)) content.Complete(version, UIContentState.Failed, Time.unscaledTime);
            Debug.LogException(exception);
        }
    }

    private bool AcceptResponse(int version) => this != null && !IsClosing && gameObject.activeInHierarchy &&
        content.Accept(version, Time.unscaledTime);

    private void Refresh(int version, FailHttpResponse<List<AccountModule.OceanShineWithdrawalRecord>> response)
    {
        if (!AcceptResponse(version)) return;
        // Only a successful empty array means there are no orders.
        if (!response.success || response.data == null || response.data.Exists(record => record == null))
        {
            content.Complete(version, UIContentState.Failed, Time.unscaledTime);
            return;
        }

        try
        {
            items.SetCmptListCount(item, root, response.data.Count);
            for (int i = 0; i < response.data.Count; i++)
                items[i].Init(response.data[i]);

            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            scrollView.verticalNormalizedPosition = 1f;
            content.Complete(version, response.data.Count == 0 ? UIContentState.Empty : UIContentState.Ready,
                Time.unscaledTime);
        }
        catch (Exception exception)
        {
            content.Complete(version, UIContentState.Failed, Time.unscaledTime);
            Debug.LogException(exception);
        }
    }

    public void Retry()
    {
        if (content.State == UIContentState.Failed) OnRefresh();
    }

    private void OnContentStateChanged(UIContentState state)
    {
        bool ready = state == UIContentState.Ready;
        bool failed = state == UIContentState.Failed;
        // Keep the list active for row initialization/layout, but hide stale rows and block their input.
        listGroup.alpha = ready ? 1f : 0f;
        listGroup.interactable = listGroup.blocksRaycasts = ready;
        emptyHint.SetActive(state == UIContentState.Empty);
        statusRoot.gameObject.SetActive(state == UIContentState.Loading || failed);
        statusText.rectTransform.anchoredPosition = failed ? failedStatusPosition : loadingStatusPosition;
        contentView.Show(state);
        if (state == UIContentState.Loading)
        {
            statusText.fontSizeMax = loadingStatusFontSize;
            statusText.fontSize = loadingStatusFontSize;
        }
        // History has no primary action. An empty result is valid and does not need retry.
        retryButton.interactable = failed;
        retryButton.gameObject.SetActive(failed);
    }

    private void RefreshStatusText() => OnContentStateChanged(content.State);

    private void Update()
    {
        if (!IsClosing) content.Tick(Time.unscaledTime);
    }

    protected override void OnClose() => ResetContent();

    private void OnDisable() => ResetContent();

    private void ResetContent()
    {
        content.Close();
        BizzaEventSystem.Set(EventDefine.Frame.LanguageChange, RefreshStatusText, false);
    }
}
#endif
