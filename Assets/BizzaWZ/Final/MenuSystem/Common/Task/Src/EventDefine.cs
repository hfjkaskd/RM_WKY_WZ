#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static partial class EventDefine
{
    public static class Task
    {
        public static int OnTaskFinish;
        public static GameEvent TaskRefresh = new();
        public static GameEvent<string> DailyTaskUpdateProgress = new();
        public static int DailyTaskRewarded;
        public static int DailyActivityTaskReward;

    }
}
#endif
