using System;
using System.Collections;
using System.Text;

namespace Whitebox.Foundation
{
    // BitArrayOperations.ToString0x2d7b500/0x2d7b558/0x2d7b5e8.
    // The original shared builder trims zero bytes at the end, then writes
    // signed decimal bytes separated by the literal section-sign delimiter.
    public static class OriginalUnlimitedBoosterSerialization
    {
        private static readonly StringBuilder builder = new StringBuilder();
        public static string Serialize(BitArray data)
        {
            if (data == null) throw new NullReferenceException();
            var bytes = new sbyte[(data.Length + 7) / 8];
            data.CopyTo(bytes, 0);
            builder.Clear();
            int count = bytes.Length;
            while (count > 0 && bytes[count - 1] == 0) count--;
            for (int i = 0; i < count; i++)
            {
                builder.Append(bytes[i]);
                if (i < count - 1) builder.Append('\u00a7');
            }
            return builder.ToString();
        }
    }
    public sealed partial class OriginalUserInventoryBalances
    {
        public void UpdateRocketEndTime(int value) // 0x28e7220; diagnostic logging omitted.
        { RocketEndTime = value; lifeValues.SetLong("RET", value); }
        public void UpdateTntEndTime(int value) // 0x28e7394.
        { TntEndTime = value; lifeValues.SetLong("TET", value); }
        public void UpdateLightballEndTime(int value) // 0x28e7508.
        { LightBallEndTime = value; lifeValues.SetLong("LET", value); }
        public void UpdateRemainingBoosterTimes(long value) // 0x28e70a8.
        { RemainingBoosterTimes = value; lifeValues.SetLong("RemainingBoosterTimes", value); }
        public void UpdateMultiplierEndTimes(long value) // 0x28e85d0.
        { MultiplierEndTimes = value; lifeValues.SetLong("MultiplierEndTimes", value); }
        public void ResetTimes() // 0x28e7184.
        {
            UpdateRocketEndTime(0);
            UpdateTntEndTime(0);
            UpdateLightballEndTime(0);
            UpdateChest(Chest & ~65535);
            UpdateRemainingBoosterTimes(0);
            // UpdateMultiplierEndTime(type0,0) clears inclusive bits31..61;
            // native UpdateLong preserves the other bits and does not clamp.
            UpdateMultiplierEndTimes(MultiplierEndTimes & ~(2147483647L << 31));
            // UpdateFeature2(value,false): the migration branch is not entered.
            Feature2 &= ~(65535L << 26);
            lifeValues.SetLong("Feature2", Feature2);
            ResetUnlimitedInGameBoosterTimes();
        }
        private void ResetUnlimitedInGameBoosterTimes() // 0x28e78d0.
        {
            var bits = UnlimitedBoosterInventory ?? throw new NullReferenceException();
            for (int i = 0; i < 192; i++) bits[i] = false;
            SaveUnlimitedBoosterDataWithoutSync();
        }
        private void SaveUnlimitedBoosterDataWithoutSync() // SaveAndSyncData(false)0x28ea568.
        {
            string serialized = OriginalUnlimitedBoosterSerialization.Serialize(UnlimitedBoosterInventory);
            if (serialized == UnlimitedBoosterInventoryString) return;
            UnlimitedBoosterInventoryString = serialized;
            lifeValues.SetString(OriginalLocalAccountLoad.UnlimitedBoosterKey, serialized);
        }
    }
}
