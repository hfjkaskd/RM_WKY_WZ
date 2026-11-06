using System;
using Bizza;

public static partial class SaveDataUtils
{
    public static readonly DataStrategy<LoginSaveData> LoginStrategy = new();
    public static LoginSaveData LoginData => LoginStrategy.Data;
}

 
public class LoginSaveData : ILocalSaveData
{
    public long lastLoginTimeStamp;
    public static bool IsNewDay;
    
    public void InitData()
    {
    }

    public void AfterLoadData()
    {
        long today = DateTime.Today.GetTimeStamp();
        IsNewDay = today > lastLoginTimeStamp;
        lastLoginTimeStamp = DateTime.Now.GetTimeStamp();
        SaveDataUtils.LoginStrategy.SaveData();
    }

    public void BeforeSave()
    {
    }
}
