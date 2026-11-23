#if BIZZA_REAL_WITHDRAW
using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Whitebox.Foundation;

public partial class UIPageIds
{
    public static readonly PageId BoosterPurchasePanel = "BoosterPurchasePanel";
}

public sealed class BoosterPurchasePanel : UIPageBase<BoosterPurchasePanel.Offer>
{
    public enum Choice
    {
        Cancel,
        Coins,
        Ad,
    }

    public readonly struct Offer
    {
        public OriginalBoosterType Type { get; }
        public Sprite Icon { get; }
        public int CoinPrice { get; }
        public int Balance { get; }
        public bool CanWatchAd { get; }
        public Action<Choice> Complete { get; }

        public Offer(OriginalBoosterType type, Sprite icon, int coinPrice, int balance,
            bool canWatchAd, Action<Choice> complete)
        {
            Type = type;
            Icon = icon;
            CoinPrice = coinPrice;
            Balance = balance;
            CanWatchAd = canWatchAd;
            Complete = complete;
        }
    }

    [SerializeField] private Button closeButton, buyButton, adButton;
    [SerializeField] private Image propIcon;
    [SerializeField] private TMP_Text titleText, propName, priceText, balanceText, buyText, adText;

    private Offer offer;
    private Choice choice;

    protected override void OnAwake()
    {
        closeButton.onClick.AddListener(CloseSelf);
        buyButton.onClick.AddListener(Buy);
        adButton.onClick.AddListener(WatchAd);
    }

    protected override void OnOpen(Offer purchase)
    {
        offer = purchase;
        choice = Choice.Cancel;
        propIcon.sprite = purchase.Icon;
        propIcon.enabled = purchase.Icon != null;
        BizzaEventSystem.Set(EventDefine.Frame.LanguageChange, RefreshLocalizedContent, true);
        RefreshLocalizedContent();
        adButton.gameObject.SetActive(purchase.CanWatchAd);
    }

    private void RefreshLocalizedContent()
    {
        var culture = CultureInfo.GetCultureInfo(LanguageUtils.SelectedLanguage);
        string price = offer.CoinPrice.ToString("N0", culture);
        string balance = offer.Balance.ToString("N0", culture);
        bool affordable = offer.CoinPrice > 0 && offer.Balance >= offer.CoinPrice;

        titleText.text = LanguageUtils.GetText("Booster_Title");
        propName.text = LanguageUtils.GetText(GetBoosterNameKey(offer.Type));
        priceText.text = LanguageUtils.GetFormatText("Booster_Price", price);
        balanceText.text = LanguageUtils.GetFormatText("Booster_Balance", balance);
        buyText.text = affordable
            ? LanguageUtils.GetText("Booster_Buy")
            : LanguageUtils.GetText("Booster_NotEnoughCoins");
        buyButton.interactable = affordable;
        adText.text = LanguageUtils.GetText("Booster_WatchAd");
    }

    private static string GetBoosterNameKey(OriginalBoosterType type) => type switch
    {
        OriginalBoosterType.Hammer => "Booster_Hammer",
        OriginalBoosterType.Arrow => "Booster_Arrow",
        OriginalBoosterType.Cannon => "Booster_Cannon",
        OriginalBoosterType.JesterHat => "Booster_Jester",
        _ => "Booster_Item",
    };

    private void Buy()
    {
        if (!buyButton.interactable || IsClosing) return;
        choice = Choice.Coins;
        CloseSelf();
    }

    private void WatchAd()
    {
        if (!offer.CanWatchAd || IsClosing) return;
        choice = Choice.Ad;
        CloseSelf();
    }

    protected override void OnClose()
    {
        BizzaEventSystem.Set(EventDefine.Frame.LanguageChange, RefreshLocalizedContent, false);
        var complete = offer.Complete;
        offer = default;
        complete?.Invoke(choice);
    }
}
#endif
