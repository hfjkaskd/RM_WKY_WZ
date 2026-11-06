using System;
using System.Collections.Generic;

namespace Bizza.Channel.Analytics
{
    public class Manager
    {
        public static event Action<string, Dictionary<string, object>> OnCustomEvent;

        public static void SendCustomEvent(string eventName, Dictionary<string, object> @params = null)
        {
            @params ??= new Dictionary<string, object>();
            OnCustomEvent?.Invoke(eventName, @params);
        }

        public static void ShowViewEvent(string viewName)
        {
            string eventName = "Show_" + viewName;
            SendCustomEvent(eventName);
        }

        public static void ShowTeachEvent(int step)
        {
            string eventName = EventName.Teach;
            var @params = new Dictionary<string, object>()
            {
                { ParamName.Step, step },
            };
            SendCustomEvent(eventName, @params);
        }

        public static void SendSwitchEvent(string eventName, bool on)
        {
            var @params = new Dictionary<string, object>()
            {
                { ParamName.On, on.ToString() },
            };
            SendCustomEvent(eventName, @params);
        }
    }

    public static class EventName
    {
        public const string Teach = "Teach";
        public const string GameStart = "Game_Start";
        public const string GameWin = "Game_Win";
        public const string GameLose = "Game_Lose";
        public const string SelectPlane = "Select_Plane";
        public const string SelectMainPage = "Select_Main_Page";
        public const string UpgradeTalentSuccess = "Upgrade_Talent_Success";
        public const string UpgradeTalentFail = "Upgrade_Talent_Fail";
        public const string ClickCard = "Click_Card";
        public const string MusicSwitch = "Music_Switch";
        public const string SoundSwitch = "Sound_Switch";
        public const string VibrateSwitch = "Vibrate_Switch";
        public const string SelectLanguage = "Select_Language";
        public const string CollectCard = "Collect_Card";
        public const string GameFPS = "Game_FPS";
    }

    public static class ParamName
    {
        public const string Step = "step";
        public const string On = "on";
    }
}

