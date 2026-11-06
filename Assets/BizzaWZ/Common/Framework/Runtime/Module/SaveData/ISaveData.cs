public interface ISaveData
{
    void InitData();
    void AfterLoadData();
    void BeforeSave();
}

public interface ILocalSaveData : ISaveData
{
    
}

public interface ITempSaveData : ISaveData
{
    
}
