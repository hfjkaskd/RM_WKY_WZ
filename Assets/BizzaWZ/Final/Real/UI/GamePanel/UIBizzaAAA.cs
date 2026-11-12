#if BIZZA_REAL_WITHDRAW
using Bizza.Channel;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Whitebox.Foundation;
using Random = UnityEngine.Random;

public class UIBizzaAAA : UIPageBase, IRoyalMatchBoardPointerBlocker
{
    [SerializeField] private Button btnFlowTreature;
    [SerializeField, Min(0f)] private float flowSpeed;
    [SerializeField, Min(0f)] private float boardEdgePadding;
    [SerializeField] private Vector2 flowHeadingDegrees;
    [SerializeField] private float firstFlowDelay = 30f;
    [SerializeField] private float flowShowTime = 60f;
    [SerializeField] private Vector2 flowCD;

    private const float BoundsRefreshInterval = 0.25f;
    private RectTransform flowRect;
    private RectTransform flowParent;
    private Canvas flowCanvas;
    private Rect movementBounds;
    private Vector2 flowDirection;
    private float flowCDTimer;
    private float nextBoundsRefresh;
    private int screenWidth;
    private int screenHeight;

    public ItemEntry thisTimeMoney;
    public float thisTimeRatio;

    private Camera EventCamera => flowCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : flowCanvas.worldCamera;

    public void Awake()
    {
        flowRect = (RectTransform)btnFlowTreature.transform;
        flowParent = (RectTransform)flowRect.parent;
        flowCanvas = flowRect.GetComponentInParent<Canvas>()?.rootCanvas;
        btnFlowTreature.onClick.AddListener(OnBtnFlowTreatureClick);
    }

    public void OnEnable()
    {
        flowCDTimer = Time.time + firstFlowDelay;
        nextBoundsRefresh = 0f;
        btnFlowTreature.gameObject.SetActive(false);
        RoyalMatchSessionBridge.BoardPointerBlocker = this;
    }

    public void OnDisable()
    {
        thisTimeMoney = ItemEntry.None;
        thisTimeRatio = 0;
        if (ReferenceEquals(RoyalMatchSessionBridge.BoardPointerBlocker, this))
            RoyalMatchSessionBridge.BoardPointerBlocker = null;
    }

    public bool BlocksBoardPointer(Vector2 screenPosition)
    {
        return flowCanvas != null && btnFlowTreature.isActiveAndEnabled && btnFlowTreature.IsInteractable() &&
            RectTransformUtility.RectangleContainsScreenPoint(flowRect, screenPosition, EventCamera);
    }

    private void LateUpdate()
    {
        float now = Time.time;
        if (!btnFlowTreature.gameObject.activeSelf)
        {
            TryShowTreasure(now);
            return;
        }

        if (now >= flowCDTimer)
        {
            flowCDTimer = now + Random.Range(flowCD.x, flowCD.y);
            btnFlowTreature.gameObject.SetActive(false);
            return;
        }

        if ((now >= nextBoundsRefresh || screenWidth != Screen.width || screenHeight != Screen.height) &&
            !TryRefreshBounds(now))
        {
            btnFlowTreature.gameObject.SetActive(false);
            return;
        }

        MoveTreasure(Time.deltaTime);
    }

    private void TryShowTreasure(float now)
    {
        if (now < flowCDTimer || now < nextBoundsRefresh || TeachUtil.IsTeach ||
            !SaveDataUtils.TeachData.IsCompleted("Teach_01") || !TryRefreshBounds(now))
            return;

        Vector3 position = flowRect.localPosition;
        position.x = Random.Range(movementBounds.xMin, movementBounds.xMax);
        position.y = Random.Range(movementBounds.yMin, movementBounds.yMax);
        flowRect.localPosition = position;
        float angle = Random.Range(flowHeadingDegrees.x, flowHeadingDegrees.y) * Mathf.Deg2Rad;
        flowDirection = new Vector2(Mathf.Cos(angle) * (Random.value < 0.5f ? -1f : 1f),
            Mathf.Sin(angle) * (Random.value < 0.5f ? -1f : 1f));
        flowCDTimer = now + flowShowTime;
        btnFlowTreature.gameObject.SetActive(true);
    }

    private bool TryRefreshBounds(float now)
    {
        nextBoundsRefresh = now + BoundsRefreshInterval;
        screenWidth = Screen.width;
        screenHeight = Screen.height;
        if (flowCanvas == null || !RoyalMatchSessionBridge.TryGetActiveBoardScreenRect(out Rect board))
            return false;

        Camera eventCamera = EventCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(flowParent, board.min, eventCamera, out Vector2 min) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(flowParent, board.max, eventCamera, out Vector2 max))
            return false;

        Rect button = flowRect.rect;
        Vector3 scale = flowRect.localScale;
        movementBounds = Rect.MinMaxRect(
            Mathf.Min(min.x, max.x) - button.xMin * scale.x + boardEdgePadding,
            Mathf.Min(min.y, max.y) - button.yMin * scale.y + boardEdgePadding,
            Mathf.Max(min.x, max.x) - button.xMax * scale.x - boardEdgePadding,
            Mathf.Max(min.y, max.y) - button.yMax * scale.y - boardEdgePadding);
        return movementBounds.width > 0f && movementBounds.height > 0f;
    }

    private void MoveTreasure(float deltaTime)
    {
        Vector3 position = flowRect.localPosition;
        position.x += flowDirection.x * flowSpeed * deltaTime;
        position.y += flowDirection.y * flowSpeed * deltaTime;
        if (position.x <= movementBounds.xMin) flowDirection.x = Mathf.Abs(flowDirection.x);
        else if (position.x >= movementBounds.xMax) flowDirection.x = -Mathf.Abs(flowDirection.x);
        if (position.y <= movementBounds.yMin) flowDirection.y = Mathf.Abs(flowDirection.y);
        else if (position.y >= movementBounds.yMax) flowDirection.y = -Mathf.Abs(flowDirection.y);
        position.x = Mathf.Clamp(position.x, movementBounds.xMin, movementBounds.xMax);
        position.y = Mathf.Clamp(position.y, movementBounds.yMin, movementBounds.yMax);
        flowRect.localPosition = position;
    }

    private void OnBtnFlowTreatureClick()
    {
        //UIController.ShowPage<UITreatureAds>();
        
        // AdModule.OpenRewardAds(E_AdPos.Flow, closeCallback: OnAdClaimSuccess);
        btnFlowTreature.gameObject.SetActive(false);
        // ItemUtils.ShowGetItemPanel(new ItemEntry()
        // {
        //     Count = thisTimeMoney,
        //     Type = E_ItemType.Dollar,
        // }, E_AdPos.Flow);

        #if BIZZA_REAL_WITHDRAW // 真网赚走这个流程
        Real_GetRewardPanelUtil.OpenGetRewardPanel(DoubleGetRewardPanel.E_UseScene.Bubble, null);
        return;
        #else
        var money = thisTimeMoney;
        if (money.Type == E_ItemType.None || money.Count <= 0)
        {
            money = ItemUtils.ToCorrect(new ItemEntry()
            {
                Type = E_ItemType.Dollar,
                Count = TableUtils.Global.FlowDollarNum,
            });
        }
            
        var coin = new ItemEntry();
        coin.Type = E_ItemType.Gold;
        coin.Count = TableUtils.Global.DefaultDollarNum;
        coin = ItemUtils.ToCorrect(coin);
        
        UIModule.Instance.OpenPage<ItemEntry, ItemEntry, DoubleGetRewardPanel.E_UseScene, Action<bool>>(UIPageIds.GetRewardPanel, coin, money, DoubleGetRewardPanel.E_UseScene.Bubble, null);
        #endif
        
        
    }

    protected override void OnOpen()
    {
        
    }

    protected override void OnClose()
    {
        
    }
}
#endif
