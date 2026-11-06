using System;
using System.Collections.Generic;

[Serializable]
public class LevelStatisticData
{
    public int levelPlayTimes;
    public int levelPlaySeconds;
}

public partial class GameSaveData
{
    public int totalLevelTimes;
    public Dictionary<int, LevelStatisticData> levelStatisticData = new();
}

public class StatisticModule : BaseGameModule<StatisticModule>, IUpdate
{
    public bool Enabled => true;

    public void OnUpdate(float delta)
    {
    }

    public void TeachEvent(int idx)
    {
    }

    public void OnLevelStart(int levelId)
    {
    }

    public void OnLevelFinish(int levelId, float progress, string endReason)
    {
    }

    public void BuyItem(ItemEntry costItem, ItemEntry getItem)
    {
    }

    public void UseItem(E_ItemType itemType, int levelId, int useTimes)
    {
    }
}
