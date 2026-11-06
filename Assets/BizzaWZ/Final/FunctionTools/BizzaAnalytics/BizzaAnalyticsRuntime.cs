using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

public enum BizzaMessageFrequency
{
  Default = 0,
  High = 1,
}

public sealed class BizzaAnalyticsConfig
{
  public string appId = "";
  public string ingestUrl = "";
  public string ingestToken = "";
  public BizzaMessageFrequency messageFrequency = BizzaMessageFrequency.Default;
  public float reportingRatio = 1f;
  public bool formalPackageReportingEnabled = true;
  public bool encryptionEnabled = true;
  public string encryptionKeyBase64 = "";
  public string encryptionKid = "";
  public bool verboseLog;
}

internal sealed class BizzaAnalyticsRuntime
{
  private const string LogPrefix = "[BizzaAnalytics]";
  private const string LogColorInfo = "#66CCFF";
  private const string LogColorSuccess = "#58D68D";
  private const string LogColorWarn = "#F5B041";
  private const string LogColorError = "#EC7063";
  private const int MaxLogTextLength = 600;
  private const int Partitions = 200;
  private const int NormalRealtimeCap = 120;
  private const int OverloadRealtimeCap = 100;
  private const float HeartbeatIntervalSeconds = 30f;
  private const float UserProfileFlushSeconds = 60f;
  private const float FirstPhaseSeconds = 300f;
  private const float FirstPhaseFlushSeconds = 60f;
  private const float FallbackFlushSeconds = 240f;
  private const int MaxRetryCount = 8;
  private const string ReportingBucketPlayerPrefsKeyPrefix = "bizza_analytics_reporting_bucket_v1_";
  private static readonly Regex IdPattern = new Regex("^[A-Za-z0-9._-]{1,128}$");
  private static readonly Regex CountryCodePattern = new Regex("^[A-Za-z]{2}$");

  private readonly BizzaAnalyticsAgent _host;
  private readonly BizzaAnalyticsConfig _config;
  private readonly BizzaStateStore _stateStore;
  private BizzaState _state;
  private BizzaAnalyticsBudget _budget;
  private Task<BizzaAnalyticsBudget> _loadTask;
  private readonly string _ingestEndpoint;
  private readonly string _authorizationHeaderValue;
  private BizzaCrypto _crypto;

  private float _timeSinceHeartbeat;
  private bool _isSending;
  private Coroutine _sendCoroutine;
  private float _reportingBucket = -1f;
  private bool _stateDirty;
  private double _saveElapsed;
  private int _workFrame = -1;
  private int _frameWork;
  private string _lastPromotedDay = "";
  private string _oldestDelayedOrigin = "";
  private bool _senderScheduleDirty = true;
  private DateTime _nextSendCheck = DateTime.MinValue;
  private DateTime _utcDate = DateTime.UtcNow.Date;
  private string _today = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
  internal int DelayedMaintenancePasses { get; private set; }
  internal int QueuedRecords { get { return _budget == null ? 0 : _budget.RecordCount; } }
  internal int QueuedBytes { get { return _budget == null ? 0 : _budget.QueueBytes; } }
  internal long DroppedRecords { get { return _budget == null ? 0 : _budget.DroppedRecords; } }
  internal BizzaStateStore StateStore { get { return _stateStore; } }
  internal BizzaState DiagnosticState { get { return _state; } }

  public bool IsReady { get; private set; }
  public bool IsReportingEnabled { get; private set; }
  public string LastError { get; private set; }

  public BizzaAnalyticsRuntime(BizzaAnalyticsAgent host, BizzaAnalyticsConfig config)
  {
    _host = host;
    _config = config;
    LastError = string.Empty;

    if (!ValidateConfig())
    {
      IsReady = false;
      return;
    }

    IsReportingEnabled = ResolveReportingEligibility();
    if (!IsReportingEnabled)
    {
      IsReady = true;
      LogReportingSamplingSnapshot();
      return;
    }

    _stateStore = new BizzaStateStore(Application.persistentDataPath, _config.verboseLog);
    _ingestEndpoint = _config.ingestUrl + "/v1/ingest";
    _authorizationHeaderValue = "Bearer " + _config.ingestToken;
    _loadTask = Task.Run(() => BizzaAnalyticsBudget.Restore(_stateStore.Load()));
  }

  internal bool CompleteInitialization()
  {
    if (IsReady) return true;
    if (_loadTask == null || !_loadTask.IsCompleted) return false;
    if (_loadTask.IsFaulted || _loadTask.IsCanceled)
    {
      var observedError = _loadTask.Exception;
      _budget = BizzaAnalyticsBudget.Restore(BizzaState.CreateDefault());
    }
    else _budget = _loadTask.Result;
    _loadTask = null;
    _state = _budget.State;
    EnsureStableIds();
    BootstrapAutoSignals();
    SaveState();

    IsReady = true;
    LogReportingSamplingSnapshot();
    LogStartupSnapshot();
    FlushNow("bootstrap");
    return true;
  }

  public void Tick(float deltaTime)
  {
    if (!IsReady || !IsReportingEnabled)
    {
      return;
    }

    if (deltaTime < 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
    {
      deltaTime = 0f;
    }

    var date = DateTime.UtcNow.Date;
    if (date != _utcDate)
    {
      _utcDate = date;
      _today = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
      BizzaAnalyticsBudget.PruneHistory(_state, _today);
      SaveState();
    }
    var today = UtcDateToday();
    _saveElapsed += deltaTime;
    AddDailyElapsed(today, deltaTime);
    AddDailyFirstPhaseFlushElapsed(today, deltaTime);
    AddDailyFallbackFlushElapsed(today, deltaTime);
    _timeSinceHeartbeat += deltaTime;

    if (_timeSinceHeartbeat >= HeartbeatIntervalSeconds)
    {
      var ticks = Math.Floor(_timeSinceHeartbeat / HeartbeatIntervalSeconds);
      _state.gameDurationSeconds += (ticks - 1d) * HeartbeatIntervalSeconds;
      _timeSinceHeartbeat %= HeartbeatIntervalSeconds;
      OnHeartbeatTick();
    }

    if (_state.userProfileBuffer.Count > 0)
    {
      _state.userProfileFlushElapsedSeconds += deltaTime;
      if (_state.userProfileFlushElapsedSeconds >= UserProfileFlushSeconds)
      {
        FlushUserProfileNow("user_profile_minute_interval");
      }
    }
    else if (_state.userProfileFlushElapsedSeconds > 0d)
    {
      _state.userProfileFlushElapsedSeconds = 0d;
      SaveState();
    }

    if (GetDailyElapsed(today) <= FirstPhaseSeconds)
    {
      if (GetDailyRecordCount(today) >= 4 && GetDailyFirstPhaseFlushElapsed(today) >= FirstPhaseFlushSeconds)
      {
        _state.dailyFirstPhaseFlushElapsedByDt[today] = 0d;
        SaveState();
        FlushNow("first_five_minutes_of_day");
      }
    }
    else
    {
      if (GetDailyFallbackFlushElapsed(today) >= FallbackFlushSeconds)
      {
        _state.dailyFallbackFlushElapsedByDt[today] = 0d;
        SaveState();
        FlushNow("fallback_four_minutes_of_day");
      }
    }

    PromoteDelayedQueue(today);
    EnsureSenderRunning();
    PersistIfDue(false);
  }

  public void Track(string eventName, Dictionary<string, object> data)
  {
    if (!IsReady || !IsReportingEnabled)
    {
      return;
    }

    if (string.IsNullOrEmpty(eventName) || eventName.Length > 128 || string.IsNullOrWhiteSpace(eventName))
    {
      LogWarn("BTrack eventName is empty, ignored.");
      return;
    }

    if (!TryTakeWork()) return;
    object copy;
    int bytes;
    if (!BizzaAnalyticsLimits.TryCopy(data, BizzaAnalyticsLimits.MaxPayloadBytes, out copy, out bytes))
    { _budget.NoteDrop(); return; }
    var payload = copy as Dictionary<string, object>;
    if (_config.verboseLog) LogVerbose("track(user) event=" + eventName.Trim() + " payload=" + SerializeForLog(payload));
    var record = BizzaRecord.NewEvent(eventName.Trim(), payload);
    if (!EnqueueEventRealtimeRecord(record)) return;
    IncrementDailyRecordCount(UtcDateToday());
    OnEventRecordEnqueued();
  }

  public void UpdateUserProperties(Dictionary<string, object> data)
  {
    if (!IsReady || !IsReportingEnabled || data == null || data.Count == 0)
    {
      return;
    }

    if (data.Count > BizzaAnalyticsLimits.MaxUserProperties) { _budget.NoteDrop(); return; }
    var changed = 0;
    foreach (var kv in data)
    {
      if (_workFrame == Time.frameCount && _frameWork >= BizzaAnalyticsLimits.MaxRecordsPerFrame) break;
      if (string.IsNullOrWhiteSpace(kv.Key))
      {
        continue;
      }
      if (ApplyUserProperty(kv.Key.Trim(), kv.Value, false, "user"))
      {
        changed += 1;
      }
    }

    if (changed > 0)
    {
      OnUserProfileRecordEnqueued();
    }
  }

  public void FlushNow(string reason)
  {
    if (!IsReady || !IsReportingEnabled)
    {
      return;
    }

    if (_config.verboseLog)
    {
      var today = UtcDateToday();
      LogInfo(
        "FlushNow reason=" + reason +
        " uid=" + _state.uid +
        " player_id=" + _state.playerId +
        " today_records=" + GetDailyRecordCount(today) +
        " today_active_seconds=" + Math.Floor(GetDailyElapsed(today)) +
        " event_realtime_records=" + _state.realtimeBuffer.Count +
        " user_profile_records=" + _state.userProfileBuffer.Count +
        " pending_batches=" + _state.pendingBatches.Count +
        " pending_records=" + CountQueuedRecords(_state.pendingBatches) +
        " delayed_batches=" + _state.delayedQueue.Count +
        " delayed_records=" + CountQueuedRecords(_state.delayedQueue));
    }

    MoveEventRealtimeToPendingBatches(true);
    PromoteDelayedQueue(UtcDateToday());
    EnsureSenderRunning();
  }

  public void FlushLifecycleNow(string reason)
  {
    if (!IsReady || !IsReportingEnabled)
    {
      return;
    }

    FinalizePendingGameDurationBeforeLifecycleFlush();
    FlushNow(reason);
    FlushUserProfileNow(reason + "_user_profile");
    PersistIfDue(true);
  }

  private bool ValidateConfig()
  {
    if (_config == null)
    {
      LastError = "config_is_null";
      LogError(LastError);
      return false;
    }

    _config.appId = (_config.appId ?? string.Empty).Trim();
    if (!IdPattern.IsMatch(_config.appId))
    {
      LastError = "app_id_must_match_^[A-Za-z0-9._-]{1,128}$";
      LogError(LastError);
      return false;
    }

    _config.ingestUrl = (_config.ingestUrl ?? string.Empty).Trim().TrimEnd('/');
    if (string.IsNullOrEmpty(_config.ingestUrl) || !_config.ingestUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
    {
      LastError = "ingest_url_is_invalid";
      LogError(LastError);
      return false;
    }

    _config.ingestToken = (_config.ingestToken ?? string.Empty).Trim();
    if (string.IsNullOrEmpty(_config.ingestToken))
    {
      LastError = "ingest_token_is_empty";
      LogError(LastError);
      return false;
    }

    _config.encryptionKeyBase64 = (_config.encryptionKeyBase64 ?? string.Empty).Trim();
    _config.encryptionKid = (_config.encryptionKid ?? string.Empty).Trim();

    if (float.IsNaN(_config.reportingRatio) || float.IsInfinity(_config.reportingRatio))
    {
      LogWarn("reporting_ratio_is_invalid, defaulted_to_1");
      _config.reportingRatio = 1f;
    }
    else if (_config.reportingRatio < 0f)
    {
      _config.reportingRatio = 0f;
    }
    else if (_config.reportingRatio > 1f)
    {
      _config.reportingRatio = 1f;
    }

    if (_config.encryptionEnabled)
    {
      string cryptoError;
      if (!BizzaCrypto.TryCreate(_config.encryptionKeyBase64, _config.encryptionKid, out _crypto, out cryptoError))
      {
        LastError = "encryption_config_invalid:" + cryptoError;
        LogError(LastError);
        return false;
      }
    }
    else
    {
      _crypto = null;
    }

    return true;
  }

  private bool ResolveReportingEligibility()
  {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
    if (!_config.formalPackageReportingEnabled)
    {
      _reportingBucket = -1f;
      return false;
    }
#endif

    var playerPrefsKey = ReportingBucketPlayerPrefsKeyPrefix + _config.appId;
    try
    {
      if (PlayerPrefs.HasKey(playerPrefsKey))
      {
        _reportingBucket = PlayerPrefs.GetFloat(playerPrefsKey, -1f);
      }

      if (!IsValidReportingBucket(_reportingBucket))
      {
        _reportingBucket = GenerateReportingBucket();
        PlayerPrefs.SetFloat(playerPrefsKey, _reportingBucket);
        PlayerPrefs.Save();
      }
    }
    catch (Exception ex)
    {
      _reportingBucket = -1f;
      LogWarn("reporting_sampling(player_prefs_failed) error=" + ex.Message);
      return _config.reportingRatio >= 1f;
    }

    return _reportingBucket < _config.reportingRatio;
  }

  private static bool IsValidReportingBucket(float value)
  {
    return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f && value < 1f;
  }

  private static float GenerateReportingBucket()
  {
    var bytes = Guid.NewGuid().ToByteArray();
    var raw24 = BitConverter.ToUInt32(bytes, 0) & 0x00FFFFFFu;
    return raw24 / 16777216f;
  }

  private void EnsureStableIds()
  {
    if (!IdPattern.IsMatch(_state.uid))
    {
      _state.uid = GenerateId("u");
    }

    if (string.IsNullOrEmpty(_state.playerId))
    {
      _state.playerId = GenerateId("player");
    }
  }

  private void BootstrapAutoSignals()
  {
    if (!_state.registerSent)
    {
      EnqueueEventRealtimeRecord(BizzaRecord.NewEvent("register", new Dictionary<string, object>()));
      LogVerbose("track(auto_event) event=register payload={}");
      _state.registerSent = true;
      IncrementDailyRecordCount(UtcDateToday());
    }

    EnqueueEventRealtimeRecord(BizzaRecord.NewEvent("login", new Dictionary<string, object>()));
    LogVerbose("track(auto_event) event=login payload={}");
    IncrementDailyRecordCount(UtcDateToday());

    ApplyUserProperty("playerID", _state.playerId, false, "auto_bootstrap");
    ApplyUserProperty("device_model", string.IsNullOrEmpty(SystemInfo.deviceModel) ? "unknown" : SystemInfo.deviceModel, false, "auto_bootstrap");

    var countryCode = ResolveDeviceCountryCode();
    if (!string.IsNullOrEmpty(countryCode))
    {
      ApplyUserProperty("country_code", countryCode, false, "auto_bootstrap");
    }

    var gameVersion = string.IsNullOrWhiteSpace(Application.version) ? "unknown" : Application.version.Trim();
    ApplyUserProperty("game_version", gameVersion, false, "auto_bootstrap");

    var duration = (long)Math.Floor(_state.gameDurationSeconds);
    ApplyUserProperty("game_duration", duration, false, "auto_bootstrap");
  }

  private static string ResolveDeviceCountryCode()
  {
#if UNITY_ANDROID && !UNITY_EDITOR
    try
    {
      using (var localeClass = new AndroidJavaClass("java.util.Locale"))
      using (var locale = localeClass.CallStatic<AndroidJavaObject>("getDefault"))
      {
        var androidCountry = NormalizeCountryCode(locale.Call<string>("getCountry"));
        if (!string.IsNullOrEmpty(androidCountry))
        {
          return androidCountry;
        }
      }
    }
    catch
    {
      // Fall through to the managed locale path.
    }
#endif

    try
    {
      return NormalizeCountryCode(RegionInfo.CurrentRegion.TwoLetterISORegionName);
    }
    catch
    {
      return string.Empty;
    }
  }

  private static string NormalizeCountryCode(string value)
  {
    var code = (value ?? string.Empty).Trim().ToUpperInvariant();
    if (!CountryCodePattern.IsMatch(code) || code == "IV")
    {
      return string.Empty;
    }
    return code;
  }

  private bool ApplyUserProperty(string key, object value, bool countAsDailyRecord, string source)
  {
    if (key.Length > 128 || !TryTakeWork()) return false;
    object normalizedValue;
    int bytes;
    if (!BizzaAnalyticsLimits.TryCopy(value, BizzaAnalyticsLimits.MaxPropertyBytes, out normalizedValue, out bytes))
    { _budget.NoteDrop(); return false; }
    object oldValue = null;
    var exists = _state.userProps.TryGetValue(key, out oldValue);
    if (exists && BizzaValueUtil.AreEqual(oldValue, normalizedValue))
    {
      return false;
    }

    if (!exists && _state.userProps.Count >= BizzaAnalyticsLimits.MaxUserProperties) return false;
    var record = BizzaRecord.NewUserProfileChange(
      key,
      exists ? oldValue : null,
      normalizedValue,
      DateTime.UtcNow.ToString("o")
    );
    if (!EnqueueUserProfileRecord(record)) return false;
    _state.userProps[key] = normalizedValue;
    if (_config.verboseLog) LogVerbose(
      "user_prop(" + source + ") key=" + key +
      " old=" + SerializeForLog(SanitizeUserPropertyValueForLog(key, exists ? oldValue : null)) +
      " new=" + SerializeForLog(SanitizeUserPropertyValueForLog(key, normalizedValue)));

    if (countAsDailyRecord)
    {
      IncrementDailyRecordCount(UtcDateToday());
    }

    return true;
  }

  private void FinalizePendingGameDurationBeforeLifecycleFlush()
  {
    var pendingWholeSeconds = (long)Math.Floor(_timeSinceHeartbeat);
    if (pendingWholeSeconds <= 0L)
    {
      return;
    }

    var oldDuration = (long)Math.Floor(_state.gameDurationSeconds);
    _state.gameDurationSeconds += pendingWholeSeconds;
    _timeSinceHeartbeat = Mathf.Max(0f, _timeSinceHeartbeat - (float)pendingWholeSeconds);

    var newDuration = (long)Math.Floor(_state.gameDurationSeconds);
    if (newDuration != oldDuration)
    {
      ApplyUserProperty("game_duration", newDuration, false, "lifecycle_flush");
    }

    SaveState();
  }

  private void OnHeartbeatTick()
  {
    var oldDuration = (long)Math.Floor(_state.gameDurationSeconds);
    _state.gameDurationSeconds += HeartbeatIntervalSeconds;
    var newDuration = (long)Math.Floor(_state.gameDurationSeconds);

    if (newDuration != oldDuration)
    {
      ApplyUserProperty("game_duration", newDuration, false, "auto_heartbeat");
      OnUserProfileRecordEnqueued();
    }
  }

  private void OnEventRecordEnqueued()
  {
    SaveState();

    var today = UtcDateToday();
    var dailyRecordCount = GetDailyRecordCount(today);
    if (dailyRecordCount <= 4)
    {
      if ((dailyRecordCount % 2) == 0)
      {
        FlushNow("first_four_records_of_day");
      }
      return;
    }

    if (GetDailyElapsed(today) > FirstPhaseSeconds)
    {
      var threshold = ResolveEffectiveBatchSize();
      if (_state.realtimeBuffer.Count >= threshold)
      {
        FlushNow("count_threshold");
      }
    }
  }

  private void OnUserProfileRecordEnqueued()
  {
    SaveState();
  }

  private bool EnqueueEventRealtimeRecord(BizzaRecord record)
  {
    if (!_budget.TryAdd(record)) return false;
    _state.realtimeBuffer.Add(record);
    return true;
  }

  private bool EnqueueUserProfileRecord(BizzaRecord record)
  {
    if (!_budget.TryAdd(record)) return false;
    _state.userProfileBuffer.Add(record);
    return true;
  }

  private void MoveEventRealtimeToPendingBatches(bool flushAll)
  {
    if (_state.realtimeBuffer.Count == 0)
    {
      SaveState();
      return;
    }

    var batchSize = ResolveEffectiveBatchSize();
    if (batchSize < 2)
    {
      batchSize = 2;
    }

    while (_state.realtimeBuffer.Count >= batchSize)
    {
      CreatePendingBatch(_state.realtimeBuffer, "realtime", batchSize);
    }

    if (flushAll && _state.realtimeBuffer.Count > 0)
    {
      CreatePendingBatch(_state.realtimeBuffer, "realtime", _state.realtimeBuffer.Count);
    }

    SaveState();
  }

  private void FlushUserProfileNow(string reason)
  {
    if (!IsReady)
    {
      return;
    }

    if (_config.verboseLog)
    {
      LogInfo(
        "FlushUserProfileNow reason=" + reason +
        " uid=" + _state.uid +
        " player_id=" + _state.playerId +
        " user_profile_records=" + _state.userProfileBuffer.Count +
        " pending_batches=" + _state.pendingBatches.Count +
        " pending_records=" + CountQueuedRecords(_state.pendingBatches) +
        " delayed_batches=" + _state.delayedQueue.Count +
        " delayed_records=" + CountQueuedRecords(_state.delayedQueue));
    }

    MoveUserProfileToPendingBatches();
    PromoteDelayedQueue(UtcDateToday());
    EnsureSenderRunning();
  }

  private void MoveUserProfileToPendingBatches()
  {
    if (_state.userProfileBuffer.Count == 0)
    {
      if (_state.userProfileFlushElapsedSeconds != 0d)
      {
        _state.userProfileFlushElapsedSeconds = 0d;
      }
      SaveState();
      return;
    }

    CreatePendingBatch(_state.userProfileBuffer, "user_profile_realtime", _state.userProfileBuffer.Count);
    _state.userProfileFlushElapsedSeconds = 0d;
    SaveState();
  }

  private void CreatePendingBatch(List<BizzaRecord> sourceBuffer, string channel, int takeCount)
  {
    if (sourceBuffer == null || takeCount <= 0)
    {
      return;
    }

    var records = new List<BizzaRecord>(takeCount);
    for (var i = 0; i < takeCount; i++)
    {
      records.Add(sourceBuffer[i]);
    }
    sourceBuffer.RemoveRange(0, takeCount);

    _state.pendingBatches.Add(new BizzaBatch
    {
      batchId = GenerateId("b"),
      channel = channel,
      dt = UtcDateToday(),
      originDt = "",
      earliestSendDt = UtcDateToday(),
      part = -1,
      retryCount = 0,
      nextRetryAt = "",
      records = records,
    });
    _senderScheduleDirty = true;
  }

  private int ResolveEffectiveBatchSize()
  {
    var today = UtcDateToday();
    var baseSize = 20;
    if (GetDailyRecordCount(today) < 4)
    {
      baseSize = 2;
    }
    else if (GetDailyElapsed(today) > FirstPhaseSeconds)
    {
      baseSize = _config.messageFrequency == BizzaMessageFrequency.High ? 100 : 20;
    }

    if (IsOverloadMode(today))
    {
      baseSize = Math.Max(baseSize, 100);
    }

    var usedPartitions = GetUsedPartitions(today);
    if (usedPartitions >= 175)
    {
      baseSize *= 4;
    }
    else if (usedPartitions >= 160)
    {
      baseSize *= 2;
    }

    if (baseSize > 500) baseSize = 500;
    if (baseSize < 2) baseSize = 2;
    return baseSize;
  }

  private int GetUsedPartitions(string dt)
  {
    return GetCounter(_state.realtimeSeqByDt, dt) + GetCounter(_state.delayedSeqByDt, dt);
  }

  private void PromoteDelayedQueue(string today)
  {
    // Newly deferred batches become eligible tomorrow. No sorting is needed.
    if (_lastPromotedDay == today) return;
    _lastPromotedDay = today;
    DelayedMaintenancePasses++;
    _oldestDelayedOrigin = "";
    var moved = 0;
    var retained = 0;
    for (var i = 0; i < _state.delayedQueue.Count; i++)
    {
      var next = _state.delayedQueue[i];
      var earliest = string.IsNullOrEmpty(next.earliestSendDt) ? today : next.earliestSendDt;
      if (string.CompareOrdinal(earliest, today) > 0)
      {
        _state.delayedQueue[retained++] = next;
        RememberDelayedOrigin(next.originDt);
      }
      else
      {
        _state.pendingBatches.Add(next);
        moved++;
      }
    }
    _state.delayedQueue.RemoveRange(retained, _state.delayedQueue.Count - retained);

    if (_state.delayedQueue.Count == 0)
    {
      _state.delayedQueueNonEmptySince = "";
    }
    else if (string.IsNullOrEmpty(_state.delayedQueueNonEmptySince))
    {
      _state.delayedQueueNonEmptySince = today;
    }

    if (moved > 0)
    {
      _senderScheduleDirty = true;
      SaveState();
    }
  }

  private void EnsureSenderRunning()
  {
    if (_isSending || !IsReady)
    {
      return;
    }

    if (!HasReadyBatch())
    {
      return;
    }

    _sendCoroutine = _host.RunCoroutine(SendLoop());
  }

  private bool HasReadyBatch()
  {
    var now = DateTime.UtcNow;
    if (!_senderScheduleDirty && now < _nextSendCheck) return false;
    _senderScheduleDirty = false;
    _nextSendCheck = DateTime.MaxValue;
    for (var i = 0; i < _state.pendingBatches.Count; i++)
    {
      var batch = _state.pendingBatches[i];
      if (batch.part < 0)
      {
        return true;
      }
      if (string.IsNullOrEmpty(batch.nextRetryAt))
      {
        return true;
      }

      DateTime retryAt;
      if (!DateTime.TryParse(batch.nextRetryAt, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out retryAt))
      {
        return true;
      }
      if (now >= retryAt)
      {
        return true;
      }
      if (retryAt < _nextSendCheck) _nextSendCheck = retryAt;
    }
    return false;
  }

  private IEnumerator SendLoop()
  {
    _isSending = true;

    while (true)
    {
      var today = UtcDateToday();
      PromoteDelayedQueue(today);

      var index = FindNextReadyBatchIndex(DateTime.UtcNow);
      if (index < 0)
      {
        break;
      }

      var batch = _state.pendingBatches[index];
      if (batch.part < 0)
      {
        int part;
        if (!TryAllocatePart(batch, today, out part))
        {
          // A restored backlog can contain thousands of batches. Moving and
          // saving them one at a time, without yielding, serializes the entire
          // state thousands of times on Unity's main thread.
          DeferBatchesWithoutCapacity(today);
          SaveState();
          yield return null;
          continue;
        }

        batch.part = part;
        batch.dt = today;
        _state.pendingBatches[index] = batch;
        SaveState();
      }

      var sent = false;
      var statusCode = 0L;
      var errorText = "";

      yield return SendBatchRequest(
        batch,
        (ok, code, err) =>
        {
          sent = ok;
          statusCode = code;
          errorText = err;
        }
      );

      if (sent)
      {
        _state.pendingBatches.RemoveAt(index);
        _budget.Release(batch);
        _senderScheduleDirty = true;
        SaveState();
        yield return null;
        continue;
      }

      HandleSendFailure(index, batch, today, statusCode, errorText);
      SaveState();
      _senderScheduleDirty = true;
      yield return null;
    }

    _isSending = false;
    _sendCoroutine = null;
  }

  private IEnumerator SendBatchRequest(BizzaBatch batch, Action<bool, long, string> onDone)
  {
    // One send coroutine owns this batch until completion. Record payloads are
    // immutable copies, so encryption/serialization can run off the Unity thread.
    var prepare = Task.Run(() => Encoding.UTF8.GetBytes(BizzaJson.Serialize(BuildRequestPayload(batch))));
    while (!prepare.IsCompleted) yield return null;
    if (prepare.IsFaulted || prepare.IsCanceled)
    {
      var error = prepare.Exception == null ? "request_preparation_canceled" : prepare.Exception.GetBaseException().Message;
      LogError(
        "send(build_failed) batch=" + batch.batchId +
        " uid=" + _state.uid +
        " part=" + batch.part +
        " channel=" + batch.channel +
        " error=" + error);
      onDone(false, 0L, "build_payload_failed:" + error);
      yield break;
    }
    var bodyRaw = prepare.Result;

    using (var request = new UnityWebRequest(_ingestEndpoint, "POST"))
    {
      if (_config.verboseLog)
      {
        LogInfo(
          "send(start) url=" + _ingestEndpoint +
          " batch=" + batch.batchId +
          " uid=" + _state.uid +
          " part=" + batch.part +
          " channel=" + batch.channel +
          " records=" + (batch.records == null ? 0 : batch.records.Count) +
          " bytes=" + (bodyRaw == null ? 0 : bodyRaw.Length));
      }

      request.uploadHandler = new UploadHandlerRaw(bodyRaw);
      request.timeout = 30;
      request.downloadHandler = new DownloadHandlerBuffer();
      request.SetRequestHeader("Authorization", _authorizationHeaderValue);
      request.SetRequestHeader("Content-Type", "application/json; charset=utf-8");

      yield return request.SendWebRequest();

      var ok = request.result == UnityWebRequest.Result.Success && request.responseCode >= 200 && request.responseCode < 300;
      var responseText = request.downloadHandler != null ? request.downloadHandler.text : "";
      var error = ok ? "" : (!string.IsNullOrEmpty(request.error) ? request.error : responseText);

      if (ok)
      {
        ApplyServerContextFromResponse(responseText);
      }

      if (_config.verboseLog)
      {
        var responseTextForLog = ok ? SanitizeSuccessResponseForLog(responseText) : responseText;
        var resultText =
          "send(result) batch=" + batch.batchId +
          " uid=" + _state.uid +
          " part=" + batch.part +
          " channel=" + batch.channel +
          " ok=" + ok +
          " status=" + request.responseCode +
          " err=" + LimitForLog(error) +
          " resp=" + LimitForLog(responseTextForLog);
        if (ok)
        {
          LogSuccess(resultText);
        }
        else
        {
          LogWarn(resultText);
        }
      }

      onDone(ok, request.responseCode, error);
    }
  }

  private void ApplyServerContextFromResponse(string responseText)
  {
    if (string.IsNullOrWhiteSpace(responseText))
    {
      return;
    }

    try
    {
      var response = BizzaValueUtil.AsStringObjectDictionary(BizzaJson.Deserialize(responseText));
      object rawClientContext;
      if (!response.TryGetValue("client_context", out rawClientContext))
      {
        return;
      }

      var clientContext = BizzaValueUtil.AsStringObjectDictionary(rawClientContext);
      object rawIpAddress;
      if (!clientContext.TryGetValue("ip_address", out rawIpAddress))
      {
        return;
      }

      var ipAddress = NormalizeIpAddress(BizzaValueUtil.AsString(rawIpAddress, ""));
      if (string.IsNullOrEmpty(ipAddress))
      {
        return;
      }

      if (ApplyUserProperty("ip_address", ipAddress, false, "server_context"))
      {
        OnUserProfileRecordEnqueued();
      }
    }
    catch (Exception ex)
    {
      LogVerbose("server_context(parse_failed) error=" + ex.Message);
    }
  }

  private static string NormalizeIpAddress(string value)
  {
    var text = (value ?? string.Empty).Trim();
    IPAddress parsed;
    return IPAddress.TryParse(text, out parsed) ? parsed.ToString() : string.Empty;
  }

  private string SanitizeSuccessResponseForLog(string responseText)
  {
    if (string.IsNullOrWhiteSpace(responseText))
    {
      return "";
    }

    try
    {
      var response = BizzaValueUtil.AsStringObjectDictionary(BizzaJson.Deserialize(responseText));
      object rawClientContext;
      if (response.TryGetValue("client_context", out rawClientContext))
      {
        var clientContext = BizzaValueUtil.AsStringObjectDictionary(rawClientContext);
        if (clientContext.ContainsKey("ip_address"))
        {
          clientContext["ip_address"] = "[redacted]";
        }
        response["client_context"] = clientContext;
      }
      return SerializeForLog(response);
    }
    catch
    {
      return "[unparseable_success_response]";
    }
  }

  private Dictionary<string, object> BuildRequestPayload(BizzaBatch batch)
  {
    if (_config.encryptionEnabled)
    {
      if (_crypto == null)
      {
        throw new InvalidOperationException("encryption_enabled_but_crypto_is_unavailable");
      }
      return BuildRequestPayload(batch, true);
    }

    return BuildRequestPayload(batch, false);
  }

  private Dictionary<string, object> BuildRequestPayload(BizzaBatch batch, bool encryptRecords)
  {
    var recordCount = batch.records == null ? 0 : batch.records.Count;
    var records = new List<object>(recordCount);
    var originDt = batch.channel == "delayed" ? batch.originDt : "";

    for (var i = 0; i < recordCount; i++)
    {
      var wireRecord = batch.records[i].ToWireRecord(originDt);
      if (encryptRecords && _crypto != null)
      {
        EncryptWireRecordInPlace(wireRecord);
      }
      records.Add(wireRecord);
    }

    var payload = new Dictionary<string, object>(6);
    payload["app_id"] = _config.appId;
    payload["uid"] = _state.uid;
    payload["batch_id"] = batch.batchId;
    payload["dt"] = string.IsNullOrEmpty(batch.dt) ? UtcDateToday() : batch.dt;
    payload["part"] = batch.part;
    payload["records"] = records;
    return payload;
  }

  private void EncryptWireRecordInPlace(Dictionary<string, object> wireRecord)
  {
    var type = BizzaValueUtil.AsString(wireRecord.ContainsKey("type") ? wireRecord["type"] : null, "");
      if (type == "event")
    {
      if (wireRecord.ContainsKey("payload"))
      {
        wireRecord["payload"] = _crypto.EncryptValue(wireRecord["payload"]);
      }
      return;
    }

    if (type == "user_profile_change")
    {
      if (wireRecord.ContainsKey("old") && wireRecord["old"] != null)
      {
        wireRecord["old"] = _crypto.EncryptValue(wireRecord["old"]);
      }
      if (wireRecord.ContainsKey("new") && wireRecord["new"] != null)
      {
        wireRecord["new"] = _crypto.EncryptValue(wireRecord["new"]);
      }
    }
  }

  private void HandleSendFailure(int pendingIndex, BizzaBatch batch, string today, long statusCode, string errorText)
  {
    if (statusCode == 409)
    {
      _state.pendingBatches.RemoveAt(pendingIndex);
      MoveBatchToDelayedQueue(batch, today);
      return;
    }

    batch.retryCount += 1;

    if (batch.retryCount > MaxRetryCount)
    {
      _state.pendingBatches.RemoveAt(pendingIndex);
      MoveBatchToDelayedQueue(batch, today);
      return;
    }

    var backoff = Math.Min(300d, Math.Pow(2d, Math.Min(8, batch.retryCount)) * 2d);
    batch.nextRetryAt = DateTime.UtcNow.AddSeconds(backoff).ToString("o");
    _state.pendingBatches[pendingIndex] = batch;

    if (_config.verboseLog)
    {
      LogWarn(
        "send(retry) batch=" + batch.batchId +
        " status=" + statusCode + " retry=" + batch.retryCount +
        " next_retry_at=" + batch.nextRetryAt + " error=" + LimitForLog(errorText)
      );
    }
  }

  private void MoveBatchToDelayedQueue(BizzaBatch batch, string today)
  {
    batch.channel = "delayed";
    if (string.IsNullOrEmpty(batch.originDt))
    {
      batch.originDt = string.IsNullOrEmpty(batch.dt) ? today : batch.dt;
    }
    batch.earliestSendDt = NextUtcDate(today);
    batch.dt = batch.earliestSendDt;
    batch.part = -1;
    batch.retryCount = 0;
    batch.nextRetryAt = "";

    _state.delayedQueue.Add(batch);
    RememberDelayedOrigin(batch.originDt);
    _senderScheduleDirty = true;
    if (string.IsNullOrEmpty(_state.delayedQueueNonEmptySince))
    {
      _state.delayedQueueNonEmptySince = today;
    }
  }

  private bool TryAllocatePart(BizzaBatch batch, string dt, out int part)
  {
    Dictionary<string, int> sequenceByDate;
    int start, capacity;
    GetPartitionRange(batch, dt, out sequenceByDate, out start, out capacity);
    var sequence = GetCounter(sequenceByDate, dt);
    if (sequence >= capacity)
    {
      part = -1;
      return false;
    }

    part = start + sequence;
    sequenceByDate[dt] = sequence + 1;
    return true;
  }

  private void GetPartitionRange(BizzaBatch batch, string dt,
    out Dictionary<string, int> sequenceByDate, out int start, out int capacity)
  {
    var realtimeCap = IsOverloadMode(dt) ? OverloadRealtimeCap : NormalRealtimeCap;
    var realtime = batch.channel == "realtime" || batch.channel == "user_profile_realtime";
    sequenceByDate = realtime ? _state.realtimeSeqByDt : _state.delayedSeqByDt;
    start = realtime ? 0 : realtimeCap;
    capacity = realtime ? realtimeCap : Partitions - realtimeCap;
  }

  private void DeferBatchesWithoutCapacity(string today)
  {
    var pending = _state.pendingBatches;
    var retained = 0;
    for (var i = 0; i < pending.Count; i++)
    {
      var candidate = pending[i];
      Dictionary<string, int> sequenceByDate;
      int start, capacity;
      GetPartitionRange(candidate, today, out sequenceByDate, out start, out capacity);
      // Already allocated retries still own their partition. Keep those and
      // channels with remaining capacity in their original pending order.
      if (candidate.part >= 0 || GetCounter(sequenceByDate, today) < capacity)
      {
        pending[retained++] = candidate;
      }
      else
      {
        MoveBatchToDelayedQueue(candidate, today);
      }
    }
    pending.RemoveRange(retained, pending.Count - retained);
  }

  private bool IsOverloadMode(string today)
  {
    if (_state.delayedQueue.Count == 0)
    {
      return false;
    }

    var oldestOrigin = _oldestDelayedOrigin;
    DateTime oldestOriginDate;
    DateTime todayDate;
    if (TryParseDate(oldestOrigin, out oldestOriginDate) && TryParseDate(today, out todayDate))
    {
      if ((todayDate - oldestOriginDate).TotalDays > 2d)
      {
        return true;
      }
    }

    if (!string.IsNullOrEmpty(_state.delayedQueueNonEmptySince))
    {
      DateTime sinceDate;
      if (TryParseDate(_state.delayedQueueNonEmptySince, out sinceDate) && TryParseDate(today, out todayDate))
      {
        if ((todayDate - sinceDate).TotalDays >= 2d)
        {
          return true;
        }
      }
    }

    return false;
  }

  private int FindNextReadyBatchIndex(DateTime nowUtc)
  {
    for (var i = 0; i < _state.pendingBatches.Count; i++)
    {
      var batch = _state.pendingBatches[i];
      if (batch.part < 0)
      {
        return i;
      }

      if (string.IsNullOrEmpty(batch.nextRetryAt))
      {
        return i;
      }

      DateTime retryAt;
      if (!DateTime.TryParse(batch.nextRetryAt, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out retryAt))
      {
        return i;
      }

      if (nowUtc >= retryAt)
      {
        return i;
      }
    }
    return -1;
  }

  private void IncrementDailyRecordCount(string dt)
  {
    _state.dailyRecordCountByDt[dt] = GetCounter(_state.dailyRecordCountByDt, dt) + 1;
  }

  private void AddDailyElapsed(string dt, float deltaTime)
  {
    if (deltaTime <= 0f)
    {
      return;
    }

    _state.dailyActiveSecondsByDt[dt] = GetDoubleCounter(_state.dailyActiveSecondsByDt, dt) + deltaTime;
  }

  private void AddDailyFirstPhaseFlushElapsed(string dt, float deltaTime)
  {
    if (deltaTime <= 0f)
    {
      return;
    }

    _state.dailyFirstPhaseFlushElapsedByDt[dt] = GetDoubleCounter(_state.dailyFirstPhaseFlushElapsedByDt, dt) + deltaTime;
  }

  private void AddDailyFallbackFlushElapsed(string dt, float deltaTime)
  {
    if (deltaTime <= 0f)
    {
      return;
    }

    _state.dailyFallbackFlushElapsedByDt[dt] = GetDoubleCounter(_state.dailyFallbackFlushElapsedByDt, dt) + deltaTime;
  }

  private int GetDailyRecordCount(string dt)
  {
    return GetCounter(_state.dailyRecordCountByDt, dt);
  }

  private double GetDailyElapsed(string dt)
  {
    return GetDoubleCounter(_state.dailyActiveSecondsByDt, dt);
  }

  private double GetDailyFirstPhaseFlushElapsed(string dt)
  {
    return GetDoubleCounter(_state.dailyFirstPhaseFlushElapsedByDt, dt);
  }

  private double GetDailyFallbackFlushElapsed(string dt)
  {
    return GetDoubleCounter(_state.dailyFallbackFlushElapsedByDt, dt);
  }

  private int GetCounter(Dictionary<string, int> map, string key)
  {
    int value;
    if (!map.TryGetValue(key, out value))
    {
      return 0;
    }
    return value;
  }

  private double GetDoubleCounter(Dictionary<string, double> map, string key)
  {
    double value;
    if (!map.TryGetValue(key, out value))
    {
      return 0d;
    }
    return value;
  }

  private void LogVerbose(string message)
  {
    if (!_config.verboseLog)
    {
      return;
    }

    LogInfo(message);
  }

  private string SerializeForLog(object value)
  {
    try
    {
      return BizzaJson.Serialize(BizzaValueUtil.Normalize(value));
    }
    catch
    {
      return value == null ? "null" : value.ToString();
    }
  }

  private static object SanitizeUserPropertyValueForLog(string key, object value)
  {
    if (string.Equals(key, "ip_address", StringComparison.OrdinalIgnoreCase))
    {
      return value == null ? null : "[redacted]";
    }
    return value;
  }

  private static Dictionary<string, object> SanitizeUserPropertiesForLog(Dictionary<string, object> userProps)
  {
    var sanitized = new Dictionary<string, object>();
    if (userProps == null)
    {
      return sanitized;
    }

    foreach (var kv in userProps)
    {
      sanitized[kv.Key] = SanitizeUserPropertyValueForLog(kv.Key, kv.Value);
    }
    return sanitized;
  }

  private static string Colorize(string color, string message)
  {
    return "<color=" + color + ">" + LogPrefix + " " + message + "</color>";
  }

  private void LogInfo(string message)
  {
    if (!_config.verboseLog)
    {
      return;
    }

    Debug.Log(Colorize(LogColorInfo, message));
  }

  private void LogSuccess(string message)
  {
    if (!_config.verboseLog)
    {
      return;
    }

    Debug.Log(Colorize(LogColorSuccess, message));
  }

  private void LogWarn(string message)
  {
    if (!_config.verboseLog)
    {
      return;
    }

    Debug.LogWarning(Colorize(LogColorWarn, message));
  }

  private void LogError(string message)
  {
    Debug.LogError(Colorize(LogColorError, message));
  }

  private static string LimitForLog(string text)
  {
    if (string.IsNullOrEmpty(text))
    {
      return "";
    }

    if (text.Length <= MaxLogTextLength)
    {
      return text;
    }

    return text.Substring(0, MaxLogTextLength) + "...(truncated)";
  }

  private void SaveState()
  {
    _stateDirty = true;
  }

  private bool TryTakeWork()
  {
    if (_workFrame != Time.frameCount) { _workFrame = Time.frameCount; _frameWork = 0; }
    if (!_budget.HasCapacity || _frameWork >= BizzaAnalyticsLimits.MaxRecordsPerFrame)
    { _budget.NoteDrop(); return false; }
    _frameWork++;
    return true;
  }

  private void RememberDelayedOrigin(string origin)
  {
    if (string.IsNullOrEmpty(_oldestDelayedOrigin) || string.CompareOrdinal(origin, _oldestDelayedOrigin) < 0)
      _oldestDelayedOrigin = origin;
  }

  private void PersistIfDue(bool lifecycle)
  {
    if (!_stateDirty || (!lifecycle && _saveElapsed < BizzaAnalyticsLimits.SaveIntervalSeconds)) return;
    // Copy bounded lists/maps only. Owned payload values stay immutable; the
    // writer never enumerates a live mutable collection.
    var snapshot = _state.ToStateObject();
    _stateDirty = false;
    _saveElapsed = 0d;
    _stateStore.RequestSave(snapshot);
  }

  private void LogReportingSamplingSnapshot()
  {
    if (!_config.verboseLog)
    {
      return;
    }

    var bucketText = IsValidReportingBucket(_reportingBucket)
      ? _reportingBucket.ToString("0.000000", CultureInfo.InvariantCulture)
      : "unavailable";
    LogSuccess(
      "startup(reporting_sampling) formal_package_enabled=" + _config.formalPackageReportingEnabled +
      " ratio=" + _config.reportingRatio.ToString("0.######", CultureInfo.InvariantCulture) +
      " bucket=" + bucketText +
      " enabled=" + IsReportingEnabled);
  }

  private void LogStartupSnapshot()
  {
    if (!_config.verboseLog)
    {
      return;
    }

    LogSuccess("startup(app_id) " + _config.appId);
    LogSuccess("startup(current_uid) " + _state.uid);
    LogSuccess("startup(current_player_id) " + _state.playerId);
    var today = UtcDateToday();
    LogSuccess(
      "startup(buffer_state) register_sent=" + _state.registerSent +
      " today_records=" + GetDailyRecordCount(today) +
      " today_active_seconds=" + Math.Floor(GetDailyElapsed(today)) +
      " event_realtime_records=" + _state.realtimeBuffer.Count +
      " user_profile_records=" + _state.userProfileBuffer.Count +
      " pending_batches=" + _state.pendingBatches.Count +
      " pending_records=" + CountQueuedRecords(_state.pendingBatches) +
      " delayed_batches=" + _state.delayedQueue.Count +
      " delayed_records=" + CountQueuedRecords(_state.delayedQueue));
    LogSuccess("startup(user_props) " + SerializeForLog(SanitizeUserPropertiesForLog(_state.userProps)));
  }

  private static int CountQueuedRecords(List<BizzaBatch> batches)
  {
    if (batches == null || batches.Count == 0)
    {
      return 0;
    }

    var total = 0;
    for (var i = 0; i < batches.Count; i++)
    {
      total += batches[i].records == null ? 0 : batches[i].records.Count;
    }
    return total;
  }

  private static string GenerateId(string prefix)
  {
    var core = Guid.NewGuid().ToString("N");
    var id = prefix + "_" + core.Substring(0, 20);
    return id;
  }

  private string UtcDateToday()
  {
    return _today;
  }

  private static string NextUtcDate(string utcDate)
  {
    DateTime date;
    if (!TryParseDate(utcDate, out date))
    {
      date = DateTime.UtcNow.Date;
    }
    return date.AddDays(1).ToString("yyyy-MM-dd");
  }

  private static bool TryParseDate(string dateText, out DateTime result)
  {
    return DateTime.TryParseExact(
      dateText,
      "yyyy-MM-dd",
      CultureInfo.InvariantCulture,
      DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
      out result
    );
  }
}
