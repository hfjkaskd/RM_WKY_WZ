using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bizza;
using Sirenix.OdinInspector;
using UnityEngine;



public class TimeModule : BaseGameModule<TimeModule>
{
    private static readonly WaitForEndOfFrame WaitForEndOfFrameYield = new WaitForEndOfFrame();

    [Title("运行中的计时器")][HideLabel]
    public List<TimerData> DebugTimerData;

    public ListDictionary<string, ETimer> AllTimers => _timers;
    private ListDictionary<string, ETimer> _timers = new ListDictionary<string, ETimer>();
    private Queue<(ETimer, bool)> _toDoList = new Queue<(ETimer, bool)>();

    public Action OnNewDay;
    // 记录上一次触发的日期
    // 指定触发时间，这里设置为上午 10 点
    private TimeSpan triggerTime = new TimeSpan(8, 0, 0);
    private bool _loadingFinish;

    public override void InitGameModule()
    {
        base.InitGameModule();
    }

    protected override void SetListener(bool addOrRemove)
    {
        BizzaEventSystem.Set(EventDefine.Frame.LoadingStageComplete, OnLoadingFinish, addOrRemove);
    }

    public ETimer Create(float duration, Action<ETimer> onUpdate = null, Action<ETimer> onComplete = null)
    {
        return Create().SetDuration(duration).OnUpdate(onUpdate).OnComplete(onComplete);
    }

    public ETimer Create(string timerId = "", bool immedinateAdd = false)
    {
        if (AllTimers.ContainsKey(timerId))
        {
            Debug.LogError($"Add timer failed! Because repeat timer id:{timerId}");
            return null;
        }

        if (string.IsNullOrEmpty(timerId))
        {
            timerId = Guid.NewGuid().ToString();
        }

        var timer = new ETimer(timerId);
        if (immedinateAdd)
        {
            TimeModule.Instance.AllTimers.Add(timerId, timer);
        }
        else
        {
            OnTimerCreate(timer);
        }
        return timer;
    }

    public ETimer Create(TimerData timerData, string timerId = "", bool immedinateAdd = false)
    {
        var timer = Create(timerId, immedinateAdd);
        timer.SetData(timerData);
        return timer;
    }

    public void Delete(ETimer timer)
    {
        if (timer == null) return;

        Delete(timer.TimerId);
    }

    public void Delete(string timerId)
    {
        OnTimerDelete(Get(timerId));
    }

    public ETimer Get(string timerId)
    {
        if (!_timers.TryGetValue(timerId, out var timer))
        {
            foreach (var v in _toDoList)
            {
                if (v.Item1.TimerId == timerId)
                {
                    timer = v.Item1;
                    break;
                }
            }
        }

        return timer;
    }

    public ETimer GetOrCreate(string timerId)
    {
        if (!_timers.TryGetValue(timerId, out var timer))
        {
            timer = Create(timerId);
        }

        return timer;
    }

    public ETimer GetOrCreate(string timerId, bool createCondition)
    {
        if (!_timers.TryGetValue(timerId, out var timer))
        {
            if (!createCondition) return null;
            timer = Create(timerId);
        }

        return timer;
    }

    public void Restart(string timerId, bool checkReady = false)
    {
        var timer = Get(timerId);
        Restart(timer, checkReady);
    }

    public void Restart(ETimer timer, bool checkReady = false)
    {
        if (timer == null) return;
        if (checkReady && !timer.IsReady) return;

        timer.Start();
    }

    void Update()
    {
        if (!_loadingFinish)
        {
            return;
        }

        if (_toDoList.Count > 0)
        {
            while (_toDoList.Count > 0)
            {
                var v = _toDoList.Dequeue();
                if (v.Item2)
                {
                    if (!_timers.ContainsKey(v.Item1.TimerId))
                    {
                        _timers.Add(v.Item1.TimerId, v.Item1);
                        v.Item1.OnCreate();
                        v.Item1.Start();
                    }
                    else
                    {
                        Debug.LogError($"Add timer failed! Because repeat timer id:{v.Item1.TimerId}");
                    }
                }
                else
                {
                    if (v.Item1 != null)
                    {
                        if (_timers.ContainsKey(v.Item1.TimerId))
                        {
                            _timers.RemoveByKey(v.Item1.TimerId);
                        }
                        else
                        {
                            Debug.LogError($"Delete timer failed! There is no timer with id:{v.Item1.TimerId}");
                        }
                    }
                }
            }
        }

        for (var i = 0; i < _timers.Count; i++)
        {
            _timers.GetAt(i).Update();
        }

        CheckNewDay();

        if (Time.frameCount % 300 == 0)
        {
            SaveDataUtils.TimeSaveStrategy.SaveData();
        }

#if UNITY_EDITOR
        //DebugTimerData = AllTimers.Values.Select(x => x._timerData).ToList();
#endif
    }

    private void OnTimerCreate(ETimer timer)
    {
        _toDoList.Enqueue((timer, true));
    }

    private void OnTimerDelete(ETimer timer)
    {
        _toDoList.Enqueue((timer, false));
    }

    public void DelayFrame(int frameCount, Action cb)
    {
        StartCoroutine(_DelayFrame(frameCount, cb));
    }

    private IEnumerator _DelayFrame(int frameCount, Action cb)
    {
        for (int i = 0; i < frameCount; i++)
        {
            yield return WaitForEndOfFrameYield;
        }

        cb?.Invoke();
    }

    private void OnLoadingFinish()
    {
        _loadingFinish = true;
        BizzaEventSystem.Emit(EventDefine.Time.InitTimer);
        foreach (var v in AllTimers)
        {
            if (v.Value.NeedToSave())
            {
                v.Value.OnLoad();
            }
        }
    }

    private void CheckNewDay()
    {
        if (!_loadingFinish)
        {
            return;
        }

        // 获取当前时间
        DateTime now = DateTime.Now;

        if (SaveDataUtils.TimeSaveData == null)
        {
            return;
        }

        //检查是否是新的一天，并且当前时间是否到达指定时间点
        if (now.Date != SaveDataUtils.TimeSaveData.lastTriggerNewDayTime.ToDateTime() && now.TimeOfDay >= triggerTime)
        {
            LogLogger.LogInfo("New Day");
            OnNewDay?.Invoke();
            BizzaEventSystem.Emit(EventDefine.Time.SystemNewDay);
            // 更新上一次触发的日期
            SaveDataUtils.TimeSaveData.lastTriggerNewDayTime = now.Date.GetTimeStamp();
            SaveDataUtils.TimeSaveStrategy.SaveData();
        }
    }
}
