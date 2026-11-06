using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static partial class SaveDataUtils
{
    public static readonly DataStrategy<TeachSaveData> TeachSaveData = new();
    public static TeachSaveData TeachData => TeachSaveData.Data;

}

public static class TeachUtil
{
    public const int FinishState = 100;

    public static bool IsTeach
    {
        get
        {
#if BIZZA_REAL_WITHDRAW
            return Teach01Module.Instance != null && Teach01Module.Instance.IsTeaching;
#else
            return false;
#endif
        }
    }

    public static string GetClosePageEndSuspendTag(string pageName)
    {
        return "TeachClosePageTag_" + pageName;
    }

    public static string GetClosePageEndSuspendTag(Type pageType)
    {
        return GetClosePageEndSuspendTag(pageType.Name);
    }

    public static string GetClosePageEndSuspendTag<TPage>() where TPage : UIPageBase
    {
        return GetClosePageEndSuspendTag(typeof(TPage));
    }
}


public class TeachSaveData : ISaveData
{
    public SortedList<string, int> states = new();
    public bool CheckState(string key)
    {
        if (states.ContainsKey(key))
            return true;
        return false;
    }
    public int GetState(string key)
    {
        states.TryGetValue(key, out int ret);
        return ret;
    }

    public bool IsCompleted(string key)
    {
        #if !BIZZA_HTTP_AD
        return true;
        #endif
        if (AccountModule.Instance != null && AccountModule.Instance.Os_Current_Uso != null && !AccountModule.Instance.Os_Current_Uso.Os_Ncm)
        {
            return true;
        }
#if BIZZA_REAL_WITHDRAW
#if UNITY_EDITOR
        if (AccountModule.Instance != null && Bizza.Sdk.ChannelConfig.Instance.real_CustomConfig.enterNewbieGuide)
        {
            return false;
        }
#endif
        if (AccountModule.Instance != null && AccountModule.Instance.Os_Current_Uso != null && !AccountModule.Instance.Os_Current_Uso.Os_Ncm)
        {
            return true;
        }
#endif
        return GetState(key) >= TeachUtil.FinishState;
    }

    public void SetState(string key, int value)
    {
        states[key] = value;
    }
    public void AfterLoadData()
    {

    }

    public void BeforeSave()
    {

    }

    public void InitData()
    {

    }
}
