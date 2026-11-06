#if BIZZA_REAL_WITHDRAW
public static partial class EventDefine
{
    public static partial class Frame
    {
        public static readonly GameEvent<bool> FinishAd = new();
        public static readonly GameEvent<bool> ShowCurrencyBar = new();
    }
}
#endif
