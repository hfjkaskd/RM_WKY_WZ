using System;
using UnityEngine;

[Serializable]
 
public class SettingSaveData : ISaveData
{
    public bool enableSound = true;
    public bool enableMusic = true;
    public bool enableVibrate = true;
    public float SoundVolume = 1f;
    public float MusicVolume = 1f;
    public SystemLanguage selectedLanguage;

    void ISaveData.InitData()
    {
        // selectedLanguage = Application.systemLanguage;
    }

    void ISaveData.AfterLoadData()
    {
        // SoundModule.Instance.SetBGMEnable(enableMusic);
        // SoundModule.Instance.EnableSound = enableSound;
        // LanguageUtil.SetLanguage(selectedLanguage);
    }

    public void BeforeSave()
    {
        // SoundModule.Instance.SetBGMEnable(enableMusic);
        // SoundModule.Instance.EnableSound = enableSound;
    }

    public void SaveData()
    {

    }
}