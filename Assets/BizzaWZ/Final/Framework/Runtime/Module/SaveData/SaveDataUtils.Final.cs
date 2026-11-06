#if BIZZA_REAL_WITHDRAW
public static partial class SaveDataUtils
{
    public static readonly DataStrategy<ChannelSaveData> channelStrategy = new();
    public static ChannelSaveData ChannelData => channelStrategy.Data;
}
#endif
