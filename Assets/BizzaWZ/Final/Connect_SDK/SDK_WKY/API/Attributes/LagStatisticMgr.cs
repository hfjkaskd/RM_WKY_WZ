#if BIZZA_REAL_WITHDRAW
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

public struct LagSnapshot
{
    public string type;
    public int count;
    public float totalTime;
    public float maxTime;
}

internal static class LagSnapshotTypes
{
    public const string Single = "Single";
    public const string Continuous = "Continuous";
    public const string Average = "Average";
}

#if !COMMONGAME
public class LagStatisticMgr : InstanceMono<LagStatisticMgr>
#else
public class LagStatisticMgr : MonoBehaviour
#endif
{
    public static LagStatisticMgr Instance { get; private set; }

    private readonly List<LagRecorder> _levelRecorders = new();

    [Header("Lag Thresholds (seconds)")]
    [SerializeField] private float singleLagThreshold = 0.1f;
    [SerializeField] private float continuousLagThreshold = 0.05f;
    [SerializeField] private float continuousLagMinDuration = 0.2f;
    [SerializeField] private float averageLagThreshold = 0.04f;
    [SerializeField] private float averageLagMinDuration = 0.3f;

    private bool _isLevelRecording;
    private int _recordingLevelId;
    private float _currentLevelElapsed;
    private int _currentLevelFrames;
    private bool _ignoreNextFrameSample;

    /// <summary>
    /// 关卡结束后，临时缓存这一关的卡顿结果。
    /// 外部读取后会自动清空。
    /// </summary>
    private bool _hasPendingLevelLagReport;
    private string _pendingLevelLagReport = string.Empty;

    public bool IsLevelRecording => _isLevelRecording;
    public bool HasPendingLevelLagReport => _hasPendingLevelLagReport;
#if !COMMONGAME
    protected override void Awake()
    {
        
#if !BIZZA_REAL_WITHDRAW
        return;
#endif
        base.Awake();
#else
    private void Awake()
    {
        Instance = this;
#endif
        DontDestroyOnLoad(gameObject);
        InitializeLevelRecorders();
    }

    private void Update()
    {
#if !BIZZA_REAL_WITHDRAW
        return;
#endif
        if (!_isLevelRecording)
        {
            return;
        }

        if (!ShouldCollect())
        {
            return;
        }

        EnsureLevelRecordersCreated();

        if (_ignoreNextFrameSample)
        {
            _ignoreNextFrameSample = false;
            return;
        }

        float dt = Time.unscaledDeltaTime;
        if (dt <= 0f)
        {
            return;
        }

        _currentLevelElapsed += dt;
        _currentLevelFrames++;

        foreach (var recorder in _levelRecorders)
        {
            recorder.Update(dt);
        }
    }

    /// <summary>
    /// 每关开始时调用。
    /// 会清空上一轮正在采集的关卡窗口，但不会清空已经缓存、等待外部读取的结果。
    /// </summary>
    public void BeginLevelLagRecord(int levelId)
    {
#if !BIZZA_REAL_WITHDRAW
        return;
#endif
        EnsureLevelRecordersCreated();

        _recordingLevelId = levelId;
        _currentLevelElapsed = 0f;
        _currentLevelFrames = 0;
        _ignoreNextFrameSample = false;
        _isLevelRecording = ShouldCollect();

        ResetRecorders(_levelRecorders);
    }

    /// <summary>
    /// 每关结束时调用。
    /// 这里只把本关统计结果存到 _pendingLevelLagReport，不主动上报。
    /// </summary>
    public void EndLevelLagRecord(int levelId, bool levelResult, int levelAttempt)
    {
#if !BIZZA_REAL_WITHDRAW
        return;
#endif
        if (!_isLevelRecording)
        {
            return;
        }

        int finalLevelId = levelId > 0 ? levelId : _recordingLevelId;
        float levelElapsed = _currentLevelElapsed;
        int levelFrames = _currentLevelFrames;

        List<LagSnapshot> snapshots = CollectAllSnapshots(_levelRecorders);

        _pendingLevelLagReport = BuildLevelLagReport(
            finalLevelId,
            levelResult,
            levelAttempt,
            levelElapsed,
            levelFrames,
            snapshots
        );

        _hasPendingLevelLagReport = true;

        ClearCurrentLevelState();
    }

    /// <summary>
    /// 外部读取本关卡顿结果。
    /// 读取成功后，会自动清空缓存。
    /// </summary>
    public bool TryConsumeLevelLagReport(out string report)
    {
#if !BIZZA_REAL_WITHDRAW
        report = string.Empty;
        return false;
#else
        if (!_hasPendingLevelLagReport)
        {
            report = string.Empty;
            return false;
        }

        report = _pendingLevelLagReport;

        _pendingLevelLagReport = string.Empty;
        _hasPendingLevelLagReport = false;

        return true;
#endif
    }

    /// <summary>
    /// 外部只查看，不清空。
    /// 一般不建议结算日志用这个，结算日志建议用 TryConsumeLevelLagReport。
    /// </summary>
    public bool TryPeekLevelLagReport(out string report)
    {
#if !BIZZA_REAL_WITHDRAW
        report = string.Empty;
        return false;
#else
        if (!_hasPendingLevelLagReport)
        {
            report = string.Empty;
            return false;
        }

        report = _pendingLevelLagReport;
        return true;
#endif
    }

    /// <summary>
    /// 外部手动清空缓存。
    /// 正常情况下不需要调用，因为 TryConsumeLevelLagReport 会自动清空。
    /// </summary>
    public void ClearPendingLevelLagReport()
    {
#if !BIZZA_REAL_WITHDRAW
        return;
#endif
        _pendingLevelLagReport = string.Empty;
        _hasPendingLevelLagReport = false;
    }

    /// <summary>
    /// 关卡被强制中断、退出、重开时可以调用。
    /// 只清空当前正在采集的窗口，不生成本关结果。
    /// </summary>
    public void CancelCurrentLevelLagRecord()
    {
#if !BIZZA_REAL_WITHDRAW
        return;
#endif
        ResetRecorders(_levelRecorders);
        ClearCurrentLevelState();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
#if !BIZZA_REAL_WITHDRAW
        return;
#endif
        if (pauseStatus)
        {
            if (_isLevelRecording)
            {
                FlushRecorders(_levelRecorders);
            }

            return;
        }

        // 从后台回来时跳过第一帧，避免把切回游戏的超大 dt 算成卡顿。
        _ignoreNextFrameSample = true;
    }

    private void OnApplicationQuit()
    {
#if !BIZZA_REAL_WITHDRAW
        return;
#endif
        if (_isLevelRecording)
        {
            FlushRecorders(_levelRecorders);
        }
    }

    private void InitializeLevelRecorders()
    {
        _levelRecorders.Clear();
        _levelRecorders.Add(new ContinuousLagRecorder(continuousLagThreshold, continuousLagMinDuration));
        _levelRecorders.Add(new AverageLagRecorder(averageLagThreshold, averageLagMinDuration));
        _levelRecorders.Add(new SingleLagRecorder(singleLagThreshold));
    }

    private void EnsureLevelRecordersCreated()
    {
        if (_levelRecorders.Count > 0)
        {
            return;
        }

        InitializeLevelRecorders();
    }

    private bool ShouldCollect()
    {
        if (SaveDataUtils.GameData == null)
        {
            return false;
        }
        return true;
    }

    private List<LagSnapshot> CollectAllSnapshots(List<LagRecorder> recorders)
    {
        FlushRecorders(recorders);

        var snapshots = new List<LagSnapshot>(recorders.Count);
        foreach (var recorder in recorders)
        {
            snapshots.Add(recorder.CollectAndReset());
        }

        return snapshots;
    }

    private void FlushRecorders(List<LagRecorder> recorders)
    {
        foreach (var recorder in recorders)
        {
            recorder.Flush();
        }
    }

    private void ResetRecorders(List<LagRecorder> recorders)
    {
        foreach (var recorder in recorders)
        {
            recorder.Flush();
            recorder.CollectAndReset();
        }
    }

    private void ClearCurrentLevelState()
    {
        _isLevelRecording = false;
        _recordingLevelId = 0;
        _currentLevelElapsed = 0f;
        _currentLevelFrames = 0;
        _ignoreNextFrameSample = false;
    }

    private string BuildLevelLagReport(
        int levelId,
        bool levelResult,
        int levelAttempt,
        float levelElapsed,
        int levelFrames,
        IReadOnlyList<LagSnapshot> snapshots)
    {
        var builder = new StringBuilder(256);

        AppendField(builder, "Device", GetDeviceInfo());
        AppendField(builder, "LevelId", levelId.ToString(CultureInfo.InvariantCulture));
        AppendField(builder, "LevelResult", levelResult ? "1" : "0");
        AppendField(builder, "LevelAttempt", levelAttempt.ToString(CultureInfo.InvariantCulture));
        AppendField(builder, "LevelSec", FormatFloat(levelElapsed));

        float avgFps = levelElapsed > 0f ? levelFrames / levelElapsed : 0f;
        AppendField(builder, "LevelAvgFPS", Mathf.RoundToInt(avgFps).ToString(CultureInfo.InvariantCulture));

        foreach (var snapshot in snapshots)
        {
            AppendField(builder, snapshot.type + "Count", snapshot.count.ToString(CultureInfo.InvariantCulture));
            AppendField(builder, snapshot.type + "Time", FormatFloat(snapshot.totalTime));
            AppendField(builder, snapshot.type + "MaxTime", FormatFloat(snapshot.maxTime));
        }

        return builder.ToString();
    }

    private string GetDeviceInfo()
    {
        var deviceInfo = DeviceInfoUtil.GetDeviceInfoDataForCloud();
        string brand = string.IsNullOrEmpty(deviceInfo.Os_Bbd) ? SystemInfo.deviceModel : deviceInfo.Os_Bbd;
        string model = string.IsNullOrEmpty(deviceInfo.Os_Mbl) ? SystemInfo.deviceModel : deviceInfo.Os_Mbl;
        return brand + "--" + model;
    }

    private static void AppendField(StringBuilder builder, string key, string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.Append("; ");
        }

        builder.Append(key).Append(':').Append(value);
    }

    private static string FormatFloat(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}

public abstract class LagRecorder
{
    private readonly string _snapshotType;

    protected LagRecorder(string snapshotType)
    {
        _snapshotType = snapshotType;
    }

    public abstract void Update(float dt);
    public abstract void Flush();
    public abstract LagSnapshot CollectAndReset();

    protected LagSnapshot CreateSnapshot(int count, float totalTime, float maxTime)
    {
        return new LagSnapshot
        {
            type = _snapshotType,
            count = count,
            totalTime = totalTime,
            maxTime = maxTime,
        };
    }
}

public class ContinuousLagRecorder : LagRecorder
{
    private readonly float _continuousThreshold;
    private readonly float _segmentMinDuration;

    private float _currentSegmentTime;
    private int _lagCount;
    private float _lagTime;
    private float _maxLagTime;

    public ContinuousLagRecorder(float continuousThreshold, float segmentMinDuration)
        : base(LagSnapshotTypes.Continuous)
    {
        _continuousThreshold = continuousThreshold;
        _segmentMinDuration = segmentMinDuration;
    }

    public override void Update(float dt)
    {
        if (dt >= _continuousThreshold)
        {
            _currentSegmentTime += dt;
            return;
        }

        CompleteCurrentSegment();
    }

    public override void Flush()
    {
        CompleteCurrentSegment();
    }

    public override LagSnapshot CollectAndReset()
    {
        LagSnapshot snapshot = CreateSnapshot(_lagCount, _lagTime, _maxLagTime);
        _lagCount = 0;
        _lagTime = 0f;
        _maxLagTime = 0f;
        return snapshot;
    }

    private void CompleteCurrentSegment()
    {
        if (_currentSegmentTime < _segmentMinDuration)
        {
            _currentSegmentTime = 0f;
            return;
        }

        _lagCount++;
        _lagTime += _currentSegmentTime;

        if (_currentSegmentTime > _maxLagTime)
        {
            _maxLagTime = _currentSegmentTime;
        }

        _currentSegmentTime = 0f;
    }
}

public class SingleLagRecorder : LagRecorder
{
    private readonly float _singleThreshold;

    private int _lagCount;
    private float _lagTime;
    private float _maxLagTime;

    public SingleLagRecorder(float singleThreshold)
        : base(LagSnapshotTypes.Single)
    {
        _singleThreshold = singleThreshold;
    }

    public override void Update(float dt)
    {
        if (dt < _singleThreshold)
        {
            return;
        }

        _lagCount++;
        _lagTime += dt;

        if (dt > _maxLagTime)
        {
            _maxLagTime = dt;
        }
    }

    public override void Flush()
    {
    }

    public override LagSnapshot CollectAndReset()
    {
        LagSnapshot snapshot = CreateSnapshot(_lagCount, _lagTime, _maxLagTime);
        _lagCount = 0;
        _lagTime = 0f;
        _maxLagTime = 0f;
        return snapshot;
    }
}

public class AverageLagRecorder : LagRecorder
{
    private readonly float _averageThreshold;
    private readonly float _segmentMinDuration;

    private float _currentSegmentTime;
    private int _currentSegmentFrames;
    private int _lagCount;
    private float _lagTime;
    private float _maxLagTime;

    public AverageLagRecorder(float averageThreshold, float segmentMinDuration)
        : base(LagSnapshotTypes.Average)
    {
        _averageThreshold = averageThreshold;
        _segmentMinDuration = segmentMinDuration;
    }

    public override void Update(float dt)
    {
        // 这里不是固定窗口平均值，而是一段连续区间的整体平均 dt。
        float nextSegmentTime = _currentSegmentTime + dt;
        int nextSegmentFrames = _currentSegmentFrames + 1;
        float nextAverage = nextSegmentTime / nextSegmentFrames;

        if (nextAverage >= _averageThreshold)
        {
            _currentSegmentTime = nextSegmentTime;
            _currentSegmentFrames = nextSegmentFrames;
            return;
        }

        CompleteCurrentSegment();

        if (dt >= _averageThreshold)
        {
            _currentSegmentTime = dt;
            _currentSegmentFrames = 1;
        }
    }

    public override void Flush()
    {
        CompleteCurrentSegment();
    }

    public override LagSnapshot CollectAndReset()
    {
        LagSnapshot snapshot = CreateSnapshot(_lagCount, _lagTime, _maxLagTime);
        _lagCount = 0;
        _lagTime = 0f;
        _maxLagTime = 0f;
        return snapshot;
    }

    private void CompleteCurrentSegment()
    {
        if (_currentSegmentTime < _segmentMinDuration)
        {
            _currentSegmentTime = 0f;
            _currentSegmentFrames = 0;
            return;
        }

        _lagCount++;
        _lagTime += _currentSegmentTime;

        if (_currentSegmentTime > _maxLagTime)
        {
            _maxLagTime = _currentSegmentTime;
        }

        _currentSegmentTime = 0f;
        _currentSegmentFrames = 0;
    }
}
#endif
