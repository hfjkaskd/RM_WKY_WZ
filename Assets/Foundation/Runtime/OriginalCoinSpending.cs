using System;

namespace Whitebox.Foundation
{
    public readonly struct OriginalSpendingData // SpendingData0x32b6304.
    {
        public int CoinAmount { get; }
        public string SpendingName { get; }
        public OriginalSpendingData(string spendingName, int coinAmount)
        { SpendingName = spendingName; CoinAmount = coinAmount; }
    }
    public interface IOriginalInventoryPackage { }
    public interface IOriginalHomeCoinView { void CoinViewForceUpdated(); }
    public interface IOriginalCoinSpendingContext
    {
        // Only a missing HomeContext permits null. A present context with
        // missing section/panel/view owners must fail in the provider.
        IOriginalHomeCoinView GetHomeCoinViewIfContextExists();
        OriginalActiveLevelAnimationValues GetCurrentAnimationData();
        OriginalUserInventoryBalances GetCurrentInventory();
        void TrackSpending(OriginalSpendingData spending, IOriginalInventoryPackage package, int egoWarningStep);
    }
    public sealed partial class OriginalUserInventoryBalances
    {
        public event Action OnCoinSpent;
        public bool HasEnoughCoins(int amount) => Coin >= amount; // 0x28e4da0.
        public bool SpendCoins(OriginalSpendingData spending, IOriginalCoinSpendingContext services,
            IOriginalInventoryPackage package, int egoWarningStep = -1) // 0x28e4f98.
        {
            // Native SUBS/B.GE compares signed operands, including overflow.
            // Do not test the wrapped subtraction result for negativity.
            if (Coin < spending.CoinAmount) return false;
            UpdateCoins(unchecked(Coin - spending.CoinAmount));
            OnCoinSpent?.Invoke();
            var owner = services ?? throw new NullReferenceException();
            owner.GetHomeCoinViewIfContextExists()?.CoinViewForceUpdated();
            var animation = owner.GetCurrentAnimationData() ?? throw new NullReferenceException();
            var inventory = owner.GetCurrentInventory() ?? throw new NullReferenceException();
            var coin = animation.Coin ?? throw new NullReferenceException();
            coin.Init(inventory.Coin);
            owner.TrackSpending(spending, package, egoWarningStep);
            return true;
        }
    }
}
