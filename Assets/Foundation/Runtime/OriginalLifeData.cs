namespace Whitebox.Foundation
{
    public sealed partial class OriginalBasicLevelProgress
    {
        private OriginalUserKeyValue lifeValues;
        public void BindLifeStorage(OriginalUserKeyValue values) { lifeValues = values; }
        public void UpdateFullLivesTimeInMs(long value) // 0x2f8244c; diagnostic formatting omitted.
        {
            FullLivesTimeInMs = value;
            lifeValues.SetLong("FullLivesTimeInMs", value);
        }
        public void UpdateUnlimitedLivesEndTimeInMs(long value) // 0x2f822d0.
        {
            UnlimitedLivesEndTimeInMs = value;
            lifeValues.SetLong("UnlimitedLivesEndTimeInMs", value);
        }
        public void SubstractTimeFromFullLivesTimeInMs(long delta) // Native spelling, 0x2f825c8.
            => UpdateFullLivesTimeInMs(unchecked(FullLivesTimeInMs - delta));
    }

    public sealed partial class OriginalUserInventoryBalances
    {
        private OriginalUserKeyValue lifeValues;
        public void BindLifeStorage(OriginalUserKeyValue values) { lifeValues = values; }
        public int GetPausedUnlimitedLivesTimeInMinutes() => Chest & 65535; // 0x28e516c.
        public void UpdatePausedUnlimitedLivesTimeInMinutes(int value) // 0x28e5180.
            => UpdateChest((Chest & ~65535) | value); // Native UpdateInt does not mask the new value.
        public void UpdateChest(int value) // 0x28e51ac; diagnostic formatting omitted.
        {
            Chest = value;
            lifeValues.SetInt("Chest", value);
        }
    }
}
