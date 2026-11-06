using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;


public class TestTimeModule : MonoBehaviour
{
    [Button("单次临时计时器10s")]
    void Btn_TmpTimer1()
    {
        TimeModule.Instance.Create(10,
            (e) => {Debug.Log($"剩余时间:{e.RestTimeInFormat_HMS}");},
            (e) => { Debug.Log("计时完成");});
    }

    private ETimer _tmpTimer2;
    [Button("多次临时计时器")]
    void Btn_TmpTimer2()
    {
        _tmpTimer2 = TimeModule.Instance.Create(10)
            .SetLoopTimes(-1)
            .OnUpdate((e)=> {Debug.Log($"剩余时间:{e.RestTimeInFormat_MS}");})
            .OnOnceLoop((e) => { Debug.Log("完成一次"); });
    }

    [Button("重新开始多次临时计时器")]
    void Btn_RestartTmpTimer2()
    {
        TimeModule.Instance.Restart(_tmpTimer2);
    }

    private ETimer _persistantTimer1;
    private ETimer _persistantTimer2;
    private const string PersistantTimerId1 = "PT1";
    private const string PersistantTimerId2 = "PT2";
    [Button("单次后台计时器")]
    void Btn_PersistTimer1()
    {
        _persistantTimer1 = TimeModule.Instance.Create(PersistantTimerId1)
            .SetDuration(60)
            .SetPersistent(true);
        InitPersistantTimer1(_persistantTimer1);
    }

    [Button("多次后台计时器")]
    void Btn_PersistTimer2()
    {
        _persistantTimer2 = TimeModule.Instance.Create(PersistantTimerId2)
            .SetDuration(60)
            .SetPersistent(true)
            .SetLoopTimes(3)
            .SetAutoNext(true);
        InitPersistantTimer2(_persistantTimer2);
    }

    void Start()
    {
        BizzaEventSystem.Set(EventDefine.Time.InitTimer, OnTimerLoaded, true);
    }

    private void OnTimerLoaded()
    {
        _persistantTimer1 = TimeModule.Instance.Get(PersistantTimerId1);
        _persistantTimer2 = TimeModule.Instance.Get(PersistantTimerId2);
        InitPersistantTimer1(_persistantTimer1);
        InitPersistantTimer2(_persistantTimer2);
    }

    private void InitPersistantTimer1(ETimer timer)
    {
        if (timer != null)
        {
            timer.OnComplete((e) => { Debug.Log("完成"); });
            timer.OnUpdate((e) => Debug.Log(e.RestTimeInFormat_HMS));
        }
    }

    private void InitPersistantTimer2(ETimer timer)
    {
        if (timer != null)
        {
            timer.OnOnceLoop((e) => { Debug.Log("完成一次"); });
            timer.OnComplete((e) => { Debug.Log("全部完成"); });
            timer.OnUpdateSecond((e) => Debug.Log(e.RestTimeInFormat_MS));
        }
    }
}
