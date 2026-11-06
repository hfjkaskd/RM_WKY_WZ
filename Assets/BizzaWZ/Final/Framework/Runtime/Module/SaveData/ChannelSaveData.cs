#if BIZZA_REAL_WITHDRAW
using System;
using Bizza.Sdk;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
///
/// </summary>
[Obfuz.ObfuzIgnore]
public class ChannelSaveData : ILocalSaveData
{
    [JsonProperty]
    private string guid = string.Empty;
    [JsonProperty]
    private int userGroup = -1;

    public bool SideBarAward = false;

    public bool HasFirstSide;

    public bool HasFirstCdkey;

    public int UserGroup
    {
        get { return userGroup; }
        set
        {
            userGroup = value;
            // todo change userGroup
        }
    }

    public void AfterLoadData()
    {
    }

    public void InitData()
    {
        guid = Guid.NewGuid().ToString();
        LogLogger.LogInfo($"init guid:{guid}");
    }

    public void BeforeSave()
    {
    }
}
#endif
