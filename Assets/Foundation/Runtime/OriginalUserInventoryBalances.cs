using System;
using System.Collections;

namespace Whitebox.Foundation
{
    public struct OriginalMadnessInventoryProgress
    {
        public int Step, Count, EventId;
        public OriginalMadnessInventoryProgress(long inventory) // 0x2f9645c.
        {
            Step = (int)((inventory >> 14) & 63);
            EventId = (int)((inventory >> 20) & 511);
            Count = (int)((inventory & 16383) + (((inventory >> 29) & 7) << 14));
        }
        public long UpdateLong(long inventory) // 0x2f96544: native unmasked step/event writes.
        {
            long value = (inventory & ~16383L) | (Count & 16383L);
            value = (value & ~(63L << 14)) | ((long)Step << 14);
            value = (value & ~(511L << 20)) | ((long)EventId << 20);
            return (value & ~(7L << 29)) | (((Count >> 14) & 7L) << 29);
        }
    }
    public interface IOriginalUserBalanceContext
    {
        void WriteCoin(long value);
        void WriteStar(int value);
        void WriteEventInventory(long value);
        void SetMissionProgress(int type, int amount);
        void TrackCoinTransaction(int amount);
    }
    // UserInventory's balances/event-inventory portion. Real storage/table and
    // analytics owners are required callbacks; this is not an alternate save path.
    public sealed partial class OriginalUserInventoryBalances : IOriginalWinInventory
    {
        private readonly IOriginalUserBalanceContext context;
        public int Coin { get; internal set; }
        public int Star { get; internal set; }
        public long EventInventory { get; internal set; }
        public int Inbox { get; internal set; }
        public int Chest { get; internal set; }
        public long Feature1 { get; internal set; }
        public long Feature2 { get; internal set; }
        public int RocketEndTime { get; internal set; }
        public int TntEndTime { get; internal set; }
        public int LightBallEndTime { get; internal set; }
        public long InGameInventory { get; internal set; }
        public long PreLevelInventory { get; internal set; }
        public long RemainingBoosterTimes { get; internal set; }
        public long MultiplierEndTimes { get; internal set; }
        public long DynamicDifficultyInventory { get; internal set; }
        public long Dialogs { get; internal set; }
        public OriginalLocalRoyalPassProgress RoyalPass { get; internal set; }
        public OriginalLocalTeamOfferProgress TeamOffer { get; internal set; }
        public string UnlimitedBoosterInventoryString { get; internal set; }
        public BitArray UnlimitedBoosterInventory { get; internal set; }
        public event Action OnCoinUpdated, OnStarsUpdated;
        public OriginalUserInventoryBalances(int coin, int star, long eventInventory, IOriginalUserBalanceContext context)
        { Coin = coin; Star = star; EventInventory = eventInventory; this.context = context; }
        public void UpdateCoins(int amount) // 0x28e4db0; diagnostic formatting omitted.
        {
            Coin = amount < 0 ? 0 : amount;
            context.WriteCoin(Coin);
            OnCoinUpdated?.Invoke();
        }
        public void UpdateStars(int amount) // 0x28e4bd8.
        {
            Star = amount < 0 ? 0 : amount;
            context.WriteStar(Star);
            OnStarsUpdated?.Invoke();
        }
        public void AddCoins(int amount, bool progressMission = true) // 0x28e4f3c.
        {
            UpdateCoins(unchecked(Coin + amount));
            if (progressMission) context.SetMissionProgress(21, amount);
            if (amount != 0) context.TrackCoinTransaction(amount);
        }
        public OriginalMadnessInventoryProgress GetMadness() => new OriginalMadnessInventoryProgress(EventInventory); // 0x28e5320.
        public void UpdateMadness(OriginalMadnessInventoryProgress progress) // 0x28e5354/0x28e5390.
        {
            long updated = progress.UpdateLong(EventInventory);
            // InitEventInventory0x28e55b8 restores native default step1 when
            // the entire packed value is zero. Other values are left intact.
            EventInventory = updated == 0 ? 16384L : updated;
            context.WriteEventInventory(EventInventory);
        }
    }
}
