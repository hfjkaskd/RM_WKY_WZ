#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static partial class EventDefine
{
    public static class AdEvent
    {
        public static GameEvent RewardAdStart = new();
        public static GameEvent<bool> RewardAdFinish = new();

        public static GameEvent InterAdStart = new();
        public static GameEvent InterAdFinish = new();

        public static GameEvent SplashAdStart = new();
        public static GameEvent SplashAdFinish = new();

        public static GameEvent AdShowHint = new();

        public static GameEvent<bool> BannerShowOrHide = new();
    }
}
#endif
