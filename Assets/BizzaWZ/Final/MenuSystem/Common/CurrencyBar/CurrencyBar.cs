#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityExtensions;

public class CurrencyBar : MonoBehaviour
{
    public static Transform DollarTarget;
    public static Transform CoinTarget;

    public TMP_Text curLevelTxt;

    public TMP_Text coinTxt;
    public TMP_Text dollarTxt;
    public TMP_Text clashTxt;

    public TMP_Text addCoinTxt;
    public TMP_Text addDollarTxt;

    public BizzaButton coinBtn;
    public BizzaButton dollarBtn;

    public Image coinImg;
    public Image dollarImg;

    public BizzaButton settingBtn;

    void Awake()
    {
        settingBtn.onClick.AddListener(() =>
        {
            _ = UIManager.Instance.OpenPage(UIPageIds.PausePanel);
        });

        dollarBtn.onClick.AddListener(() =>
        {
            UIModule.Instance.OpenPage(UIPageIds.FakeWithdrawPanel);
        });

        coinBtn.onClick.AddListener(() =>
        {
            UIModule.Instance.OpenPage(UIPageIds.RealWithdrawPanel);
        });

        
    }

    void OnEnable()
    {
        // coinImg.sprite = ItemUtils.GetItemIcon(E_ItemType.Gold);
        // dollarImg.sprite = ItemUtils.GetItemIcon(E_ItemType.Dollar);
        OnItemChanged();

        addCoinTxt.DOFade(0, 0);
        addDollarTxt.DOFade(0, 0);

        curLevelTxt.text = SaveDataUtils.GameData.playerSelectedLv.ToString();

        BizzaEventSystem.Set(EventDefine.Item.ItemChanged, OnItemChanged, true);
        BizzaEventSystem.Set(EventDefine.Item.ItemChangedWithData, OnItemChangedWithData, true);
        BizzaEventSystem.Set(EventDefine.Frame.LanguageChange, OnLanguageChange, true);
        BizzaEventSystem.Set(EventDefine.Frame.ShowCurrencyBar, OnCurrencyShowHide, true);

        VFXUtils.itemFlyTarget[E_ItemType.Dollar] = dollarImg.transform;
        VFXUtils.itemFlyTarget[E_ItemType.Gold] = coinImg.transform;
    }

    void OnDisable()
    {
        BizzaEventSystem.Set(EventDefine.Item.ItemChanged, OnItemChanged, false);
        BizzaEventSystem.Set(EventDefine.Item.ItemChangedWithData, OnItemChangedWithData, false);
        BizzaEventSystem.Set(EventDefine.Frame.LanguageChange, OnLanguageChange, false);
        BizzaEventSystem.Set(EventDefine.Frame.ShowCurrencyBar, OnCurrencyShowHide, false);
        if (VFXUtils.itemFlyTarget.TryGetValue(E_ItemType.Dollar, out Transform dollar) && dollar == dollarImg.transform)
            VFXUtils.itemFlyTarget.Remove(E_ItemType.Dollar);
        if (VFXUtils.itemFlyTarget.TryGetValue(E_ItemType.Gold, out Transform coin) && coin == coinImg.transform)
            VFXUtils.itemFlyTarget.Remove(E_ItemType.Gold);
    }

    private void OnItemChanged()
    {
        if (AccountModule.Instance != null && AccountModule.Instance.Os_Current_Uso != null)
        {
            coinTxt.text = WithdrawalUtil.GetCustomizedValueByCountryType((float)AccountModule.Instance.Os_Current_Uso.GetBalance()); 
            dollarTxt.text = ItemUtils.GetItemText(E_ItemType.Dollar);
            string ewl = WithdrawalUtil.GetCustomizedValueByCountryType((float)AccountModule.Instance.Os_Current_Uso.Os_Ewl);
            clashTxt.text = $"{LanguageUtils.GetText("CurrencyToken")}{ewl}";
        }

    }

    private void OnLoadGame()
    {
        curLevelTxt.text = SaveDataUtils.GameData.playerSelectedLv.ToString();
        ResetAddTextState();
    }

    private void ResetAddTextState()
    {
        if (coinTween != null)
        {
            coinTween.Kill();
            coinTween = null;
        }

        if (dollarTween != null)
        {
            dollarTween.Kill();
            dollarTween = null;
        }

        DOTween.Kill(addCoinTxt);
        DOTween.Kill(addDollarTxt);
        addCoinTxt.DOFade(0f, 0f);
        addDollarTxt.DOFade(0f, 0f);
    }

    private void OnLanguageChange()
    {
        OnItemChanged();
    }

    private void OnCurrencyShowHide(bool value)
    {
        // if (value)
        // {
        //     Canvas overrideCanvas = gameObject.GetOrAddComponent<Canvas>();
        //     overrideCanvas.
        //     gameObject.GetOrAddComponent<GraphicRaycaster>();
        //     overrideCanvas.overrideSorting = value;

        //     var group = gameObject.GetOrAddComponent<CanvasGroup>();
        //     group.interactable = false;
        //     group.blocksRaycasts = false;
        // }
        // else
        // {
        //     var raycaster = gameObject.GetComponent<GraphicRaycaster>();

        //     if (raycaster != null) Destroy(raycaster);     // ✅ 先删它

        //     var canvas = gameObject.GetComponent<Canvas>();
        //     if (canvas != null) Destroy(canvas);

        //     var group = gameObject.GetOrAddComponent<CanvasGroup>();
        //     if (group != null) Destroy(group);
        // }
    }

    public static Dictionary<string, float> CurrentQueue = new();

    public static void OnEnQueue(string id, float value)
    {
        LogLogger.LogAdInfo("入队 " + id + " Value " + value);
        CurrentQueue[id] = value;
        // CurrentQueue.Enqueue(value);
    }

    public static float OnDequeue(string id)
    {
        if (CurrentQueue.TryGetValue(id, out float value))
        {
            LogLogger.LogAdInfo("收益id " + id + " Value " + value);
            CurrentQueue.Remove(id);
        }
        else
        {
            LogLogger.LogAdInfo("没有这个id " + id);
        }

        return value;
    }

    private Tween coinTween;
    private Tween dollarTween;
    private Tween _coinIncTween;
    private Tween _dollarIncTween;

    private void OnItemChangedWithData(ItemEntry prevItem, ItemEntry newItem)
    {
        var num = newItem.Count - prevItem.Count;
        num = (float)System.Math.Round(num, 2, System.MidpointRounding.AwayFromZero);
        if (num <= 0)
        {
            return;
        }

        var showItem = new ItemEntry()
        {
            Count = num,
            Type = prevItem.Type,
        };
        if (prevItem.Type == E_ItemType.Gold)
        {
            if (coinTween != null)
            {
                coinTween.Kill();
            }

            addCoinTxt.text = "+" + ItemUtils.GetItemText(showItem);
            var sequence = DOTween.Sequence();
            sequence.Append(addCoinTxt.DOFade(1f, 0.1f));
            sequence.AppendInterval(1);
            sequence.Append(addCoinTxt.DOFade(0f, 0.35f));
            coinTween = sequence;

            DoCoinAnim();
        }
        else if (prevItem.Type == E_ItemType.Dollar)
        {
            if (dollarTween != null)
            {
                dollarTween.Kill();
            }

            addDollarTxt.text = "+" + ItemUtils.GetItemText(showItem);
            var sequence = DOTween.Sequence();
            sequence.Append(addDollarTxt.DOFade(1f, 0.1f));
            sequence.AppendInterval(1);
            sequence.Append(addDollarTxt.DOFade(0f, 0.35f));
            dollarTween = sequence;

            DoDollarAnim();
        }
    }

    public Transform dollarBox;
    public Transform coinBox;
    private Tweener _dollarBoxTweener;
    private Tweener _coinBoxTweener;

    private float _lastDollarAnim;
    //
    // public void DoItemIncAnim(E_ItemType itemType, Tween tween)
    // {
    //
    //     _dollarIncTween.Kill();
    //     float target = ItemUtils.GetItemCount();
    //     //
    //     float currentValue;
    //     if (!float.TryParse(DollarText.text, NumberStyles.Float, CultureInfo.InvariantCulture, out currentValue))
    //         currentValue = prevNum;
    //
    //     //
    //     _dollarIncTween = DOTween.To(
    //             () => currentValue,
    //             x =>
    //             {
    //                 currentValue = x;
    //                 UpdateDollarText(x);
    //             },
    //             target,
    //             duration
    //         )
    //         .SetEase(Ease.OutQuad)
    //         .OnComplete(() =>
    //         {
    //             UpdateDollarText(PlayerDataManager.Instance.Dollar); // ׼
    //         });
    // }

    public void DoDollarAnim()
    {
        if (Time.realtimeSinceStartup - _lastDollarAnim < 0.2f) return;
        _lastDollarAnim = Time.realtimeSinceStartup;
        if (_dollarBoxTweener != null)
        {
            _dollarBoxTweener.Kill();
            _dollarBoxTweener = null;
        }
        dollarBox.transform.localScale = Vector3.one * 1.25f;
        _dollarBoxTweener = dollarBox.transform.DOScale(Vector3.one, 0.3f);
    }

    private float _lastCoinAnim;
    public void DoCoinAnim()
    {
        if (Time.realtimeSinceStartup - _lastCoinAnim < 0.2f) return;
        _lastCoinAnim = Time.realtimeSinceStartup;

        if (_coinBoxTweener != null)
        {
            _coinBoxTweener.Kill();
            _coinBoxTweener = null;
        }
        coinBox.transform.localScale = Vector3.one * 1.25f;
        _coinBoxTweener = coinBox.transform.DOScale(Vector3.one, 0.3f);
    }

}
#endif
