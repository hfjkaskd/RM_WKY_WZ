#if BIZZA_REAL_WITHDRAW
using System.ComponentModel;

public partial class SROptions
{
    [Category("游戏/777")]
    [DisplayName("777当前进度（0-5）")]
    public int SlotProgress
    {
        get => SlotProgressUtil.Current;
        set => SlotProgressUtil.SetProgress(value);
    }

    [Category("游戏/777")]
    [DisplayName("777进度：模拟过1关")]
    public void SlotProgress_AddLevel() => SlotProgressUtil.AddPassedLevel();

    [Category("游戏/777")]
    [DisplayName("777进度：补满5/5")]
    public void SlotProgress_Fill() => SlotProgressUtil.SetProgress(SlotProgressUtil.RequiredPassedLevels);

    [Category("游戏/777")]
    [DisplayName("777进度：重置0/5")]
    public void SlotProgress_Reset() => SlotProgressUtil.SetProgress(0);
}
#endif
