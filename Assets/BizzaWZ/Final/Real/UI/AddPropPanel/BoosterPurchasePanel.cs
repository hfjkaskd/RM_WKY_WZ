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

        bool portuguese = LanguageUtils.SelectedLanguage.StartsWith("pt", StringComparison.OrdinalIgnoreCase);
        var culture = CultureInfo.GetCultureInfo(portuguese ? "pt-BR" : "en-US");
        string price = purchase.CoinPrice.ToString("N0", culture);
        string balance = purchase.Balance.ToString("N0", culture);
        bool affordable = purchase.CoinPrice > 0 && purchase.Balance >= purchase.CoinPrice;

        titleText.text = portuguese ? "Comprar item" : "Buy Booster";
        propName.text = GetBoosterName(purchase.Type, portuguese);
        priceText.text = portuguese ? $"Preço: {price} moedas" : $"Price: {price} coins";
        balanceText.text = portuguese ? $"Saldo de moedas: {balance}" : $"Coin balance: {balance}";
        buyText.text = affordable
            ? (portuguese ? "Comprar" : "Buy")
            : (portuguese ? "Moedas insuficientes" : "Not enough coins");
        buyButton.interactable = affordable;
        adText.text = portuguese ? "Assistir vídeo" : "Watch video";
        adButton.gameObject.SetActive(purchase.CanWatchAd);
    }

    private static string GetBoosterName(OriginalBoosterType type, bool portuguese) => type switch
    {
        OriginalBoosterType.Hammer => portuguese ? "Martelo" : "Hammer",
        OriginalBoosterType.Arrow => portuguese ? "Flecha" : "Arrow",
        OriginalBoosterType.Cannon => portuguese ? "Canhão" : "Cannon",
        OriginalBoosterType.JesterHat => portuguese ? "Chapéu de bobo" : "Jester Hat",
        _ => portuguese ? "Item" : "Booster",
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
        var complete = offer.Complete;
        offer = default;
        complete?.Invoke(choice);
    }
}
#endif
