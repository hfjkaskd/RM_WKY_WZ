using Newtonsoft.Json;
 
public partial class GameSaveData : ISaveData
{
    [JsonProperty] private int version;

    private int Version => 0;

    public int playerSelectedLv = 1;
    public int playerpassLevel => playerSelectedLv - 1;
    // public int playerSelectedLv = 1;

    public bool firstTimeMult = false;

    public float totalGameMinutes; // 总游戏时长分钟

    public int gameTimeSeconds;//游戏时长（秒）
    public int[] gameTimeSecondsArray = new int[7];

    public string gameBoardData;
    
    private void InitRes()
    {

    }

    private void AfterLoadRes()
    {

    }

    public void InitData()
    {
        InitRes();
    }

    public void AfterLoadData()
    {
        AfterLoadRes();
        CheckVersion();

        Login();
    }

    private void CheckVersion()
    {
        if (version < Version)
        {
            UpgradeVersion(version, Version);
            version = Version;
        }
        else if (version < Version)
        {
            LogLogger.LogError("存档版本高于当前版本");
        }
    }

    private void UpgradeVersion(int prev, int cur)
    {
    }

    void ISaveData.BeforeSave()
    {
    }
}
