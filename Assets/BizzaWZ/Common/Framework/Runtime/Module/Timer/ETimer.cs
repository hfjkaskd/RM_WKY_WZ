using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

public class EasyTimerDataForSave
{
    public List<TimerData> list;

    public EasyTimerDataForSave()
    {
    }
}

[Serializable]
 
public class TimerData
{
    [JsonIgnore]
    [ShowInInspector]
    public string DebugRestTime
    {
        get
        {
            var ts = TimeSpan.FromSeconds(restTime);
            return $"{(int) ts.TotalDays:d2}{ts.Hours:d2}:{ts.Minutes:d2}:{ts.Seconds:d2}";
        }
    }

    public string timerId;
    [JsonIgnore]
    public double restTime;
    public float duration;
    public int loopTimes = 1;
    public bool initAvailable = false;
    public bool persistent = false;
    public bool autoNext = false;
    public bool realtimeUpdate = true;
    [JsonIgnore] [ShowInInspector]
    public string debugDateTime => DateTime.FromBinary(startDateTime).ToString();
    public long startDateTime;
    public bool isReady;

    public TimerData()
    {
    }

    [Button("立即完成")]
    void Finish()
    {
        restTime = 0.1f;
    }

    [Button("减30s")]
    void SubTime30s()
    {
        restTime -= 30;
    }

    [Button("模拟时间流逝")]
    void SimulateTimeElapse(int days, int hours, int minutes, int seconds)
    {
        var data = DateTime.FromBinary(startDateTime);
        var newData = data.Subtract(new TimeSpan(days, hours, minutes, seconds));
        startDateTime = newData.ToBinary();
        var timer = TimeModule.Instance.Get(timerId);
        timer.OnLoad();
    }

    [Button("删除")]
    void Delete()
    {
        TimeModule.Instance.Delete(timerId);
    }
}

public class ETimer
{
    internal TimerData _timerData;

    private Action<ETimer> _onCreate;
    private Action<ETimer> _onUpdate;
    private Action<ETimer> _onUpdateSecond;
    private Action<ETimer> _onOnceLoop;
    private Action<ETimer> _onComplete;
    private Action<ETimer, int> _onBackgroundLoop;

    #region public

    public string TimerId => _timerData.timerId;
    public bool IsReady => RestTime <= 0; //_timerData.isReady;

    public double RestTime
    {
        get => _timerData.restTime;
        set => _timerData.restTime = value;
    }

    public string RestTimeInFormat_HMS
    {
        get
        {
            var ts = TimeSpan.FromSeconds(RestTime);
            return $"{(int) ts.TotalHours:d2}:{ts.Minutes:d2}:{ts.Seconds:d2}";
        }
    }

    public string RestTimeInFormat_MS
    {
        get
        {
            var ts = TimeSpan.FromSeconds(RestTime);
            return $"{(int)ts.TotalMinutes:d2}:{ts.Seconds:d2}";
        }
    }

    public string RestTimeInFormat_HM
    {
        get
        {
            var ts = TimeSpan.FromSeconds(RestTime);
            return $"{(int)ts.TotalHours:d2}:{ts.Minutes:d2}";
        }
    }

    #endregion

    internal ETimer()
    {
        if (_timerData == null)
        {
            _timerData = new TimerData();
        }
    }

    internal ETimer(TimerData timerData)
    {
        _timerData = timerData;
    }

    internal ETimer(string timerId)
    {
        _timerData = new TimerData {timerId = timerId};
    }

    internal void OnCreate()
    {
        _onCreate?.Invoke(this);
    }

    internal void Start()
    {
        if (_timerData.duration <= 0)
        {
            Debug.LogError("Duration should be positive.");
        }

        if (_timerData.initAvailable)
        {
            _timerData.isReady = true;
            RestTime = 0;
            _timerData.initAvailable = false;
        }
        else
        {
            _timerData.isReady = false;
            RestTime = _timerData.duration;
        }

        OnStart();
    }

    internal void Update()
    {
        if (IsReady)
        {
            if (_timerData.autoNext)
            {
                Start();
            }
            else
            {
                return;
            }
        }

        OnUpdate();
        var prevLong = (long)Math.Round(RestTime);
        RestTime -= _timerData.realtimeUpdate ? Time.unscaledDeltaTime : Time.deltaTime;
        if (prevLong != (long)Math.Round(RestTime))
        {
            OnUpdateSecond();
        }
        if (RestTime <= 0)
        {
            OnOnceLoop();
        }
    }

    internal void SetData(TimerData timerData)
    {
        _timerData = timerData;
    }

    internal bool NeedToSave()
    {
        return _timerData.persistent;
    }

    internal void OnLoad()
    {
        if (!_timerData.persistent)
        {
            return;
        }

        var now = DateTime.Now;
        var startDateTime = DateTime.FromBinary(_timerData.startDateTime);
        var ts = now - startDateTime;
        var eclipseSeconds = ts.TotalSeconds;
        if (eclipseSeconds < 0) eclipseSeconds = 0;

        if (_timerData.autoNext)
        {
            int finishLoopTimes = 0;
            while (_timerData.loopTimes > 0 && eclipseSeconds >= _timerData.duration)
            {
                eclipseSeconds -= _timerData.duration;
                OnOnceLoop(true, false, _onBackgroundLoop == null);
                finishLoopTimes++;
            }

            if (finishLoopTimes > 0)
            {
                OnBackgroundLoop(finishLoopTimes);
            }

            RestTime = _timerData.duration - eclipseSeconds;
        }
        else
        {
            if (!_timerData.isReady)
            {
                RestTime = _timerData.duration;
                RestTime -= eclipseSeconds;
                if (RestTime <= 0)
                {
                    RestTime = 0;

                    OnOnceLoop(true, _onBackgroundLoop == null);
                    OnBackgroundLoop(1);
                }
            }
        }
    }

    private void CheckNextLoop(bool delayInvokeEvent, bool setStart)
    {
        if (_timerData.loopTimes == 0)
        {
            OnAllLoopFinish(delayInvokeEvent);
            return;
        }

        if (_timerData.autoNext)
        {
            if (setStart)
            {
                Start();
            }
        }
    }

    #region SetData

    public ETimer SetDuration(float duration)
    {
        _timerData.duration = duration;
        return this;
    }

    public ETimer SetLoopTimes(int loopTimes)
    {
        _timerData.loopTimes = loopTimes;
        return this;
    }

    public ETimer SetUpdate(bool realTimeUpdate)
    {
        _timerData.realtimeUpdate = realTimeUpdate;
        return this;
    }

    public ETimer SetPersistent(bool persistent)
    {
        _timerData.persistent = persistent;
        return this;
    }

    public ETimer SetInitAvailable(bool initAvailable)
    {
        _timerData.initAvailable = initAvailable;
        return this;
    }

    public ETimer SetAutoNext(bool autoNext)
    {
        _timerData.autoNext = autoNext;
        return this;
    }

    // public ETimer SetCountTimeOverflowInBackground(bool countTimeOverflowInBackground)
    // {
        // _timerData.countTimeOverflowInBackground = countTimeOverflowInBackground;
        // return this;
    // }

    public ETimer OnCreate(Action<ETimer> cb)
    {
        _onCreate += cb;
        return this;
    }

    public ETimer OnUpdate(Action<ETimer> cb)
    {
        _onUpdate += cb;
        return this;
    }

    public ETimer OnUpdateSecond(Action<ETimer> cb)
    {
        _onUpdateSecond += cb;
        return this;
    }

    public ETimer OnOnceLoop(Action<ETimer> cb)
    {
        _onOnceLoop += cb;
        return this;
    }

    public ETimer OnBackgroundLoop(Action<ETimer, int> cb)
    {
        _onBackgroundLoop += cb;
        return this;
    }

    public ETimer OnComplete(Action<ETimer> cb)
    {
        _onComplete += cb;
        return this;
    }

    #endregion

    #region Event

    private void OnStart()
    {
        _timerData.startDateTime = DateTime.Now.ToBinary();
    }

    private void OnUpdate()
    {
        _onUpdate?.Invoke(this);
    }

    private void OnUpdateSecond()
    {
        _onUpdateSecond?.Invoke(this);
    }

    private void OnBackgroundLoop(int times)
    {
        _onBackgroundLoop?.Invoke(this, times);
    }

    private void OnOnceLoop(bool delayInvokeEvent = false, bool setStart = true, bool invokeEvt = true)
    {
        _timerData.isReady = true;
        if (_timerData.loopTimes != -1)
        {
            _timerData.loopTimes--;
        }

        if (invokeEvt)
        {
            if (delayInvokeEvent)
            {
                TimeModule.Instance.DelayFrame(1, () => { _onOnceLoop?.Invoke(this); });
            }
            else
            {
                _onOnceLoop?.Invoke(this);
            }
        }

        CheckNextLoop(delayInvokeEvent, setStart);
    }

    private void OnAllLoopFinish(bool delayInvokeEvent = false)
    {
        if (delayInvokeEvent)
        {
            TimeModule.Instance.DelayFrame(1, () => { _onComplete?.Invoke(this); });
        }
        else
        {
            _onComplete?.Invoke(this);
        }

        TimeModule.Instance.Delete(TimerId);
    }

    #endregion
}