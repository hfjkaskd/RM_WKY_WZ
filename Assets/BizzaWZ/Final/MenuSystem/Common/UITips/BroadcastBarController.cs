using System.Collections;
using System.Collections.Generic;
using Bizza.Sdk;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BroadcastBarController : BaseSingleton<BroadcastBarController>
{
    public RectTransform barTransform;

    public GameObject textObj;
    public TextMeshProUGUI textComponent;

    public GameObject entryObj;

    public GameObject entryAObj;
    public Image entryAImage;
    public TMP_Text entryAText;

    public GameObject entryBObj;
    public Image entryBImage;
    public TMP_Text entryBText;

    public float stayDuration = 1.5f;
    public float animDuration = 0.5f;
    public float minInterval = 0.3f; // 300ms 内禁止重复触发
    public float minTwoInterval = 0.3f; // 300ms 内禁止重复触发

    private Vector2 hiddenPos;
    private Vector2 visiblePos;
    private Sequence currentTween;
    private float txtlastShowTime = -10f;
    private float itemlastShowTime = -10f;
    private static readonly Vector3[] rectWorldCorners = new Vector3[4];


    protected override void Awake()
    {
        base.Awake();
    }

    void Start()
    {
        InitPositions();
        barTransform.anchoredPosition = visiblePos;
        gameObject.SetActive(false);
    }

    void InitPositions()
    {
        float barHeight = barTransform.rect.height;
        hiddenPos = new Vector2(0, barHeight);
        visiblePos = new Vector2(0, 0);
    }

    public void ShowMessage(string message)
    {
        if (Time.time - txtlastShowTime < minInterval)
            return;

        textObj.SetActive(true);
        entryObj.SetActive(false);

        txtlastShowTime = Time.time;

        if (currentTween != null && currentTween.IsActive())
            currentTween.Kill();

        InitPositions(); // 确保分辨率变化时位置正确
        textComponent.text = message;
        gameObject.SetActive(true);

        barTransform.anchoredPosition = visiblePos;

        CanvasGroup canvasGroup = GetOrAddCanvasGroup();
        canvasGroup.alpha = 0;

        currentTween = DOTween.Sequence().SetTarget(gameObject);
        currentTween.Append(canvasGroup.DOFade(1f, animDuration));
        currentTween.AppendInterval(stayDuration);
        currentTween.Append(canvasGroup.DOFade(0f, animDuration));
        currentTween.OnComplete(() => gameObject.SetActive(false)).SetTarget(gameObject);
    }

    public void ShowMessage(ItemEntry a, ItemEntry b, System.Action<Vector3, Vector3> onVisible)
    {
        bool isShown = ShowItemMessage(a, b);
        RefreshEntryLayoutAndGetPositions(out Vector3 pos1, out Vector3 pos2);
        if (!isShown)
        {
            onVisible?.Invoke(pos1, pos2);
            return;
        }

        GameUtils.DelayDo(() => { onVisible?.Invoke(pos1, pos2); }, RewardFlyDelay);
    }

    // Let the amount finish fading in and remain readable before spawning the money effects.
    public float RewardFlyDelay => animDuration + stayDuration;

    public bool CanShowRewardImmediately =>
        Time.time - itemlastShowTime >= Mathf.Max(minTwoInterval, RewardFlyDelay + animDuration);

    public bool ShowRewardImmediately(ItemEntry a, ItemEntry b, out Vector3 pos1, out Vector3 pos2)
    {
        pos1 = pos2 = Vector3.zero;
        if (!CanShowRewardImmediately || !ShowItemMessage(a, b)) return false;
        RefreshEntryLayoutAndGetPositions(out pos1, out pos2);
        return true;
    }

    public void ShowMessage(ItemEntry a, ItemEntry b)
    {
        ShowItemMessage(a, b);
    }

    private bool ShowItemMessage(ItemEntry a, ItemEntry b)
    {
        if (!CanShowRewardImmediately)
            return false;

        // 第一个是金币
        if (a.Type != E_ItemType.Gold)
        {
            var c = new ItemEntry();
            c = a;
            a = b;
            b = c;
        }
        itemlastShowTime = Time.time;

        textObj.SetActive(false);
        entryObj.SetActive(true);

        if (currentTween != null && currentTween.IsActive())
            currentTween.Kill();

        InitPositions(); // 确保分辨率变化时位置正确
        entryAObj.SetActive(false);
        if (!a.Equals(default(ItemEntry)))
        {
            entryAObj.gameObject.SetActive(true);
            // entryAImage.sprite = ItemUtils.GetItemIcon(a);
            entryAText.text = "+" + ItemUtils.GetItemText(a);
        }

        if (ChannelConfig.Instance.real_CustomConfig.singleCurrencyMode)
        {
            entryAImage.sprite = entryBImage.sprite;
            entryBObj.SetActive(false);
        }
        else
        {
            //entryBObj.SetActive(false);
            if (!b.Equals(default(ItemEntry)))
            {
                //entryBObj.gameObject.SetActive(true);
                // entryBImage.sprite = ItemUtils.GetItemIcon(b);
                entryBText.text = $"+{ItemUtils.GetItemText(b)}";
            }
            entryBObj.gameObject.SetActive(b.Count > 0);
        }

        gameObject.SetActive(true);

        barTransform.anchoredPosition = visiblePos;

        CanvasGroup canvasGroup = GetOrAddCanvasGroup();
        canvasGroup.alpha = 0;

        currentTween = DOTween.Sequence().SetTarget(gameObject);
        currentTween.Append(canvasGroup.DOFade(1f, animDuration));
        currentTween.AppendInterval(stayDuration);
        currentTween.Append(canvasGroup.DOFade(0f, animDuration));
        currentTween.OnComplete(() => gameObject.SetActive(false)).SetTarget(gameObject);
        return true;
    }

    private void RefreshEntryLayoutAndGetPositions(out Vector3 pos1, out Vector3 pos2)
    {
        Canvas.ForceUpdateCanvases();

        if (entryObj != null && entryObj.transform is RectTransform entryRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(entryRect);
        }

        if (barTransform != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(barTransform);
        }

        Canvas.ForceUpdateCanvases();

        pos1 = GetRectWorldCenter(entryAImage != null ? entryAImage.rectTransform : null);
        pos2 = GetRectWorldCenter(entryBImage != null ? entryBImage.rectTransform : null);
    }

    private static Vector3 GetRectWorldCenter(RectTransform rectTransform)
    {
        if (rectTransform == null)
        {
            return Vector3.zero;
        }

        rectTransform.GetWorldCorners(rectWorldCorners);
        return (rectWorldCorners[0] + rectWorldCorners[2]) * 0.5f;
    }

    private CanvasGroup GetOrAddCanvasGroup()
    {
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null)
            cg = gameObject.AddComponent<CanvasGroup>();
        return cg;
    }

}
