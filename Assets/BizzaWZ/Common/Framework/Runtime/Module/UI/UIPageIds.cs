using Obfuz;

[ObfuzIgnore]
public static partial class UIPageIds
{
    public static readonly PageId None = PageId.Empty;

    public static readonly PageId LoadingPanel = nameof(LoadingPanel);
    public static readonly PageId TransitonPanel = nameof(TransitonPanel);
    public static readonly PageId PausePanel = nameof(PausePanel);
    public static readonly PageId RealGamePanel = nameof(RealGamePanel);

    // public static readonly PageId CommonConfirmTips = nameof(CommonConfirmTips);
}
