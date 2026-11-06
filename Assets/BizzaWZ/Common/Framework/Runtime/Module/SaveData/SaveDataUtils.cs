public static partial class SaveDataUtils
{
    public static readonly DataStrategy<GameSaveData> gameStrategy = new();
    public static GameSaveData GameData => gameStrategy.Data;
    public static readonly DataStrategy<SettingSaveData> settingStrategy = new();
    public static SettingSaveData SettingData => settingStrategy.Data;

    public static void Save()
    {
        SaveDataModule.Instance.SaveAllData();
    }
}
