#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Whitebox.Foundation;

public class RealUiWidget : MonoBehaviour
{
    public BizzaButton taskBtn;
    public TMP_Text levelTxt;
    [SerializeField] private bool levelTextProvidedByCurrencyBar;
    [SerializeField] private CanvasGroup missionChestGroup;
    [SerializeField] private RectTransform topBar;

    private Canvas rootCanvas;
    private Vector2 topBarPrefabPosition;
    private Vector3 topBarPrefabScale;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;
    private float lastCanvasScale;


    public ItemForCountry itemForCountry;


    void Awake()
    {
        if (topBar != null)
        {
            topBarPrefabPosition = topBar.anchoredPosition;
            topBarPrefabScale = topBar.localScale;
            OnCanvasHierarchyChanged();
        }

        taskBtn.onClick.AddListener(() =>
        {
            if (!UIUtils.CheckWKYAccountReady()) return;
            _ = UIModule.Instance.OpenPage(UIPageIds.UI_DailyTaskPage);
        });
    }

    void OnEnable()
    {
        RefreshLevelText();
        itemForCountry.OnRefresh();
    }

    public void RefreshLevelText()
    {
        if (levelTextProvidedByCurrencyBar) return;
        levelTxt.text = FlowModule.CurrentLevel.ToString(); //LanguageUtils.GetFormatText("Menu_LevelBtn", SaveDataUtils.GameData.playerUnlockedLv);
    }

    private void OnCanvasHierarchyChanged()
    {
        if (topBar != null) rootCanvas = topBar.GetComponentInParent<Canvas>()?.rootCanvas;
    }

    private void LateUpdate()
    {
        bool chestVisible = !RoyalMatchSessionBridge.IsLevelQuitDialogOpen;
        if (missionChestGroup != null && missionChestGroup.blocksRaycasts != chestVisible)
        {
            missionChestGroup.alpha = chestVisible ? 1f : 0f;
            missionChestGroup.interactable = chestVisible;
            missionChestGroup.blocksRaycasts = chestVisible;
        }

        if (rootCanvas == null) return;

        Rect safeArea = Screen.safeArea;
        var screenSize = new Vector2Int(Screen.width, Screen.height);
        float canvasScale = rootCanvas.scaleFactor;
        if (safeArea == lastSafeArea && screenSize == lastScreenSize && canvasScale == lastCanvasScale) return;

        lastSafeArea = safeArea;
        lastScreenSize = screenSize;
        lastCanvasScale = canvasScale;

        // Convert the physical safe area to the scaled overlay canvas coordinates.
        float topInset = Mathf.Max(0f, screenSize.y - safeArea.yMax) / canvasScale;
        float centerOffset = (safeArea.center.x - screenSize.x * .5f) / canvasScale;
        float barWidth = topBar.rect.width * topBarPrefabScale.x;
        float fit = Mathf.Min(1f, safeArea.width / (barWidth * canvasScale));
        topBar.anchoredPosition = topBarPrefabPosition + new Vector2(centerOffset, -topInset);
        topBar.localScale = topBarPrefabScale * fit;
    }
}
#endif
