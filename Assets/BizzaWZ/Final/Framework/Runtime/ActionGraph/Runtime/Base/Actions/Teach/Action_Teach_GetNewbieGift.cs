#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;

 
[Preserve]
[GraphElementInfo(Category = "教学", Text = "等待-请求新手奖励", SupportTypes = new Type[] { typeof(ActionGraphBase) },
    TipsText = "与服务器通信直到返回新手奖励")]
[Obfuz.ObfuzIgnore]
public class Action_Teach_GetNewbieGift : ActionNodeBase
{
    public override string DebugName => $"等待-请求新手奖励";

#if BIZZA_REAL_WITHDRAW
    private FailHttpResponse<AccountModule.OceanShineUserReachReportResponse> _rewsponse;
#endif

    public override object Clone()
    {
        var clone = new Action_Teach_GetNewbieGift();
        return clone;
    }

    protected override void OnEnter(in ExecuteArgs executeArgs)
    {
        base.OnEnter(executeArgs);
#if BIZZA_REAL_WITHDRAW
        AccountModule.Instance.Request_UserReachReportRequest(OnRefresh);
#endif
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
#if BIZZA_REAL_WITHDRAW
        if (_rewsponse.success)
        {
            return E_ExecuteState.Success;
        }
        else
        {
            return E_ExecuteState.Running;
        }
#else
        return E_ExecuteState.Success;
#endif
    }

#if BIZZA_REAL_WITHDRAW
    private void OnRefresh(FailHttpResponse<AccountModule.OceanShineUserReachReportResponse> response)
    {
        if (response.success)
        {
            _rewsponse = response;
        }
        else
        {
            //SaveDataUtils.GameData.isInFrameTutorial = false;
            LogLogger.LogError("新手引导 - 请求获得新手奖励失败");
        }
    }
#endif
}
#endif
