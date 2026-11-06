#if BIZZA_REAL_WITHDRAW
using System.Collections.Generic;
public static partial class SaveDataUtils
{
    public static readonly DataStrategy<FakeWithDrawPanelSaveData> FakeWithDrawPanelStrategy = new();
    public static FakeWithDrawPanelSaveData FakeWithDrawPanelData => FakeWithDrawPanelStrategy.Data;
}
public class FakeWithDrawPanelSaveData : ISaveData
{
    public List<int> curStageList = new List<int>();
    public void AfterLoadData()
    {
    }

    public void BeforeSave()
    {
    }

    public void SetStage(int index, int stage)
    {
        curStageList[index] = stage;
        SaveDataUtils.FakeWithDrawPanelStrategy.SaveData();
    }

    public void InitData()
    {
        curStageList.Clear();
        curStageList.CompleteList(7, 0);
    }
}
#endif