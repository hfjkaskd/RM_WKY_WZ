#if BIZZA_REAL_WITHDRAW
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bizza.Sdk
{
    public static class AnalyticsEventName
    {
    public const string register = "ta_register";
    public const string login = "ta_login";
    public const string stageStart = "ta_stage_start";
    public const string stageFinish = "ta_stage_finish";
    public const string stageFail = "ta_stage_fail";
    public const string newDevice = "ta_new_device";
    }

    public static class AnalyticsParamKey
    {
    public const string index = "index";
    public const string unlock = "unlock";
    public const string channel = "channel";
    public const string registerTime = "register_time";
    public const string lastLoginTime = "last_login_time";
    public const string stageId = "stage_id";
    }
}
#endif
