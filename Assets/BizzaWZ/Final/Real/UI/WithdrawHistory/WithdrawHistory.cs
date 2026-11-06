#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections.Generic;
using Bizza;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
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
    [SerializeField] private Sprite retryButtonSprite;
    [SerializeField, Min(1f)] private float contentTimeoutSeconds = 20f;
    [SerializeField] private UIContentStateView contentView = new();

    private readonly List<WithdrawHistoryItem> items = new();
    private readonly UIContentLoadSession content = new();
    private ScrollRect scrollView;
    private CanvasGroup listGroup;
    private RectTransform statusRoot;
    private TMP_Text statusText;
    private BizzaButton retryButton;

    protected override void OnAwake()
    {
        base.OnAwake();
        closeButton.onClick.AddListener(CloseSelf);
        scrollView = root.GetComponentInParent<ScrollRect>();
        listGroup = scrollView.GetComponent<CanvasGroup>();
        if (listGroup == null) listGroup = scrollView.gameObject.AddComponent<CanvasGroup>();
        CreateStatusView();
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
        statusText.rectTransform.anchoredPosition = new Vector2(0, failed ? 28f : 0f);
        contentView.Show(state);
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

    private void CreateStatusView()
    {
        var template = emptyHint.GetComponent<TMP_Text>();
        var anchor = template.rectTransform;
        statusRoot = CreateRect("HistoryRequestStatus", anchor.parent, anchor.sizeDelta, anchor.anchoredPosition);
        statusRoot.anchorMin = anchor.anchorMin;
        statusRoot.anchorMax = anchor.anchorMax;
        statusRoot.pivot = anchor.pivot;
        statusText = CreateText("StatusText", statusRoot, new Vector2(anchor.rect.width - 60f, 120f), template);

        var buttonRect = CreateRect("RetryButton", statusRoot, new Vector2(520f, 116f), new Vector2(0f, -92f));
        var background = buttonRect.gameObject.AddComponent<Image>();
        background.sprite = retryButtonSprite;
        background.type = Image.Type.Sliced;
        retryButton = buttonRect.gameObject.AddComponent<BizzaButton>();
        retryButton.canDrag = false;
        retryButton.onClick = new UnityEvent();
        retryButton.onLongClick = new UnityEvent();
        retryButton.onClick.AddListener(Retry);
        var label = CreateText("RetryText", buttonRect, new Vector2(460f, 92f), template);
        label.color = Color.white;
        label.fontSize = label.fontSizeMax = 40f;
        label.text = LanguageUtils.GetText("UIContent_Retry");
    }

    private static RectTransform CreateRect(string objectName, Transform parent, Vector2 size, Vector2 position)
    {
        var rect = (RectTransform)new GameObject(objectName, typeof(RectTransform)).transform;
        rect.gameObject.layer = parent.gameObject.layer;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private static TMP_Text CreateText(string objectName, Transform parent, Vector2 size, TMP_Text template)
    {
        var rect = CreateRect(objectName, parent, size, Vector2.zero);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = template.font;
        text.fontSharedMaterial = template.fontSharedMaterial;
        text.fontSize = text.fontSizeMax = 36f;
        text.fontSizeMin = 24f;
        text.enableAutoSizing = true;
        text.alignment = TextAlignmentOptions.Center;
        text.color = template.color;
        text.raycastTarget = false;
        return text;
    }
}
#endif
