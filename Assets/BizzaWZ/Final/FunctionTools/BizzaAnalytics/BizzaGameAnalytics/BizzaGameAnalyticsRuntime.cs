using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Bizza.GameAnalytics
{
  [DefaultExecutionOrder(-1000)]
  internal sealed class BizzaGameAnalyticsRuntime : MonoBehaviour
  {
    private const string HostObjectName = "[BizzaGameAnalyticsHost]";
    private const string PlayerPrefsPrefix = "bizza_game_analytics_v1_";
    private const string InstallTimeKey = PlayerPrefsPrefix + "install_time";
    private const string InstallEventSentKey = PlayerPrefsPrefix + "install_event_sent";
    private const string ActivationKey = PlayerPrefsPrefix + "activated";
    private const string TotalImpressionsKey = PlayerPrefsPrefix + "total_impressions";
    private const string TotalGameTimeKey = PlayerPrefsPrefix + "total_game_time";
    private const string ActiveDaysKey = PlayerPrefsPrefix + "active_days";
    private const string LastActiveDateKey = PlayerPrefsPrefix + "last_active_date";
    private const float UserPropertyCoalesceSeconds = 1f;
    private const int TotalImpressionPropertyInterval = 10;
    private const int MaxCrashTextLength = 512;
    private const int MaxCrashStackLength = 4096;
    private const int LifecycleDrainLimit = 32;

    private static BizzaGameAnalyticsRuntime _instance;

    private readonly BizzaPendingEventQueue _criticalEvents = new BizzaPendingEventQueue(256);
    private readonly BizzaPendingEventQueue _regularEvents = new BizzaPendingEventQueue(512);
    private readonly Dictionary<string, object> _pendingUserProperties = new Dictionary<string, object>(12);
    private readonly Dictionary<string, double> _pageOpenTimes = new Dictionary<string, double>(8);
    private readonly BizzaRecentIdSet _adImpressionIds = new BizzaRecentIdSet(512);
    private readonly BizzaRecentIdSet _adRevenueIds = new BizzaRecentIdSet(512);
    private readonly BizzaRecentIdSet _withdrawalRequestIds = new BizzaRecentIdSet(256);
    private readonly BizzaRecentIdSet _withdrawalResultIds = new BizzaRecentIdSet(256);

    private readonly BizzaGameAnalyticsOptions _options = new BizzaGameAnalyticsOptions();
    private bool _trackingStarted;
    private bool _exceptionCallbackRegistered;
    private bool _sessionActive;
    private bool _lifecycleClosed;
    private bool _skipNextFrameDelta;
    private string _sessionId = string.Empty;
    private double _sessionStartedAt;
    private double _sessionAccumulatedSeconds;
    private double _foregroundElapsedSeconds;
    private double _totalGameTimeSeconds;
    private float _userPropertyElapsed;
    private float _performanceElapsed;
    private int _performanceFrames;
    private int _slowFrames;
    private int _automaticExceptionCount;
    private int _totalImpressions;
    private int _activeDays;
    private string _installTime = string.Empty;
    private string _activeLevelId = string.Empty;
    private string _activeLevelAttemptId = string.Empty;
    private int _activeLevelAttempt;
    private double _activeLevelStartedAt;
    private bool _hasEconomyContext;
    private double _currentBalance;
    private double _firstWithdrawalThreshold;
    private int _withdrawalStage;
    private string _balanceUnit = string.Empty;
    private bool _criticalQueueOverflowErrorLogged;
    private bool _regularQueueOverflowWarningLogged;
    private bool _activationQueued;
    private bool _hasLoggedBizzaState;
    private BizzaAnalyticsInitializationState _lastLoggedBizzaState;
    private bool _lastLoggedReportingEnabled;

    public static BizzaGameAnalyticsRuntime EnsureInstance()
    {
      if (_instance != null)
      {
        return _instance;
      }

      GameObject host = new GameObject(HostObjectName);
      DontDestroyOnLoad(host);
      return host.AddComponent<BizzaGameAnalyticsRuntime>();
    }

    private void Awake()
    {
      if (_instance != null && _instance != this)
      {
        Destroy(gameObject);
        return;
      }

      _instance = this;
      DontDestroyOnLoad(gameObject);
    }

    internal void BeginTracking()
    {
      if (_trackingStarted)
      {
        return;
      }

      _trackingStarted = true;
      LogCall("BeginTracking", "started");
      LoadLocalState();
      RefreshExceptionCallback();
      QueueInitialUserProperties();

      if (PlayerPrefs.GetInt(InstallEventSentKey, 0) != 1)
      {
        Dictionary<string, object> install = NewPayload(2);
        install["install_time"] = _installTime;
        Enqueue("user_install", install, true);
      }

      BeginSession("app_start");
      Enqueue("app_start", NewPayload(), false);
    }

    internal bool SetEconomyContext(
      double currentBalance,
      double firstWithdrawalThreshold,
      int withdrawalStage,
      string balanceUnit)
    {
      if (!TryNormalizeUnit(balanceUnit, out string normalizedUnit)
        || double.IsNaN(currentBalance)
        || double.IsInfinity(currentBalance)
        || double.IsNaN(firstWithdrawalThreshold)
        || double.IsInfinity(firstWithdrawalThreshold)
        || firstWithdrawalThreshold < 0d
        || withdrawalStage < 0)
      {
        LogRejected("经济参数无效；余额单位不能为空，阈值和阶段不能为负数。");
        return false;
      }

      _hasEconomyContext = true;
      _currentBalance = currentBalance;
      _firstWithdrawalThreshold = firstWithdrawalThreshold;
      _withdrawalStage = withdrawalStage;
      _balanceUnit = normalizedUnit;
      LogCall(
        "SetEconomyContext",
        "accepted balance_unit=" + normalizedUnit + " withdrawal_stage=" + withdrawalStage);
      return true;
    }

    internal void SetAttribution(string network, string campaign, string country)
    {
      SetUserPropertyIfNotEmpty("network", network, 128);
      SetUserPropertyIfNotEmpty("campaign", campaign, 256);
      SetUserPropertyIfNotEmpty("country", country, 8);
      LogCall("SetAttribution", "queued");
    }

    internal void SetUserGroup(string userGroup)
    {
      SetUserPropertyIfNotEmpty("user_group", userGroup, 128);
      LogCall("SetUserGroup", "queued");
    }

    internal bool TrackActivation()
    {
      if (!_trackingStarted
        || _activationQueued
        || PlayerPrefs.GetInt(ActivationKey, 0) == 1)
      {
        LogRejected("user_activate 未接受：尚未初始化、已入队或已激活。");
        return false;
      }

      _activationQueued = Enqueue("user_activate", NewPayload(), true);
      return _activationQueued;
    }

    internal void TrackTutorialComplete(string tutorialId)
    {
      Dictionary<string, object> data = NewPayload(1);
      data["tutorial_id"] = NormalizeText(tutorialId, "main", 128);
      Enqueue("tutorial_complete", data, true);
    }

    internal void TrackPageOpen(string pageName)
    {
      string normalizedPage = NormalizeText(pageName, "unknown", 128);
      _pageOpenTimes[normalizedPage] = ActiveTimeNow();

      Dictionary<string, object> data = NewPayload(1);
      data["page_name"] = normalizedPage;
      Enqueue("page_open", data, false);
    }

    internal void TrackPageClose(string pageName)
    {
      string normalizedPage = NormalizeText(pageName, "unknown", 128);
      double duration = 0d;
      if (_pageOpenTimes.TryGetValue(normalizedPage, out double openedAt))
      {
        duration = Math.Max(0d, ActiveTimeNow() - openedAt);
        _pageOpenTimes.Remove(normalizedPage);
      }

      Dictionary<string, object> data = NewPayload(2);
      data["page_name"] = normalizedPage;
      data["duration_seconds"] = RoundDuration(duration);
      Enqueue("page_close", data, false);
    }

    internal void TrackItemUse(string itemId, string itemType, int quantity, string source)
    {
      Dictionary<string, object> data = NewPayload(4);
      data["item_id"] = NormalizeText(itemId, "unknown", 128);
      data["item_type"] = NormalizeText(itemType, "unknown", 64);
      data["quantity"] = Math.Max(1, quantity);
      AddText(data, "source", source, 128);
      Enqueue("item_use", data, false);
    }

    internal string TrackLevelStart(string levelId, int attempt)
    {
      if (!string.IsNullOrEmpty(_activeLevelId))
      {
        LogRejected("已有未结束的关卡尝试，新的 level_start 已忽略。");
        return string.Empty;
      }

      string normalizedLevel = NormalizeOptionalText(levelId, 128);
      if (string.IsNullOrEmpty(normalizedLevel))
      {
        LogRejected("levelId 不能为空。");
        return string.Empty;
      }

      _activeLevelId = normalizedLevel;
      _activeLevelAttempt = Math.Max(1, attempt);
      _activeLevelAttemptId = Guid.NewGuid().ToString("N");
      _activeLevelStartedAt = ActiveTimeNow();

      Dictionary<string, object> data = NewPayload(3);
      data["level_id"] = _activeLevelId;
      data["attempt"] = _activeLevelAttempt;
      data["attempt_id"] = _activeLevelAttemptId;
      if (!Enqueue("level_start", data, false))
      {
        ClearActiveLevel();
        return string.Empty;
      }
      return _activeLevelAttemptId;
    }

    internal bool TrackLevelEnd(
      string levelId,
      string attemptId,
      string result,
      string reason)
    {
      string normalizedLevel = NormalizeOptionalText(levelId, 128);
      string normalizedAttemptId = NormalizeOptionalText(attemptId, 64);
      if (string.IsNullOrEmpty(_activeLevelId)
        || !string.Equals(_activeLevelId, normalizedLevel, StringComparison.Ordinal)
        || (!string.IsNullOrEmpty(normalizedAttemptId)
          && !string.Equals(_activeLevelAttemptId, normalizedAttemptId, StringComparison.Ordinal)))
      {
        LogRejected("关卡结束与当前活动 attempt 不匹配，已忽略。");
        return false;
      }

      int attempt = Math.Max(1, _activeLevelAttempt);
      double duration = Math.Max(0d, ActiveTimeNow() - _activeLevelStartedAt);
      string activeAttemptId = _activeLevelAttemptId;

      string eventName = result == "win"
        ? "level_win"
        : (result == "fail" ? "level_fail" : "level_quit");

      Dictionary<string, object> outcome = NewPayload(4);
      outcome["level_id"] = normalizedLevel;
      outcome["attempt"] = attempt;
      outcome["attempt_id"] = activeAttemptId;
      outcome["duration_seconds"] = RoundDuration(duration);
      AddText(outcome, "reason", reason, 256);
      if (!Enqueue(eventName, outcome, true))
      {
        return false;
      }

      Dictionary<string, object> timing = NewPayload(5);
      timing["level_id"] = normalizedLevel;
      timing["attempt"] = attempt;
      timing["attempt_id"] = activeAttemptId;
      timing["result"] = result;
      timing["duration_seconds"] = RoundDuration(duration);
      Enqueue("level_duration", timing, false);
      ClearActiveLevel();
      return true;
    }

    internal void TrackAdLoad(
      string placement,
      BizzaAdType adType,
      string adNetwork,
      BizzaAdLoadResult result,
      long latencyMilliseconds,
      string errorCode)
    {
      Dictionary<string, object> data = NewAdPayload(placement, adType, adNetwork, 3);
      data["result"] = result == BizzaAdLoadResult.Success ? "success" : "failed";
      data["latency_ms"] = Math.Max(0L, latencyMilliseconds);
      AddText(data, "error_code", errorCode, 128);
      Enqueue("ad_load", data, false);
    }

    internal void TrackAdShowRequest(
      string placement,
      BizzaAdType adType,
      string adNetwork,
      bool isReady)
    {
      Dictionary<string, object> data = NewAdPayload(placement, adType, adNetwork, 1);
      data["is_ready"] = isReady;
      Enqueue("ad_show_request", data, false);
    }

    internal void TrackAdShow(string placement, BizzaAdType adType, string adNetwork)
    {
      Enqueue("ad_show", NewAdPayload(placement, adType, adNetwork), false);
    }

    internal bool TrackAdImpression(
      string placement,
      BizzaAdType adType,
      string adNetwork,
      string impressionId)
    {
      string normalizedImpressionId = NormalizeOptionalText(impressionId, 256);
      if (string.IsNullOrEmpty(normalizedImpressionId)
        || _adImpressionIds.Contains(normalizedImpressionId))
      {
        LogRejected("impressionId 为空或已处理，ad_impression 已忽略。");
        return false;
      }

      Dictionary<string, object> data = NewAdPayload(placement, adType, adNetwork, 1);
      data["impression_id"] = normalizedImpressionId;
      if (!Enqueue("ad_impression", data, true))
      {
        return false;
      }

      _adImpressionIds.Add(normalizedImpressionId);
      return true;
    }

    internal bool TrackAdRevenue(
      string placement,
      BizzaAdType adType,
      string adNetwork,
      double revenue,
      string currency,
      string impressionId)
    {
      string normalizedImpressionId = NormalizeOptionalText(impressionId, 256);
      if ((!string.IsNullOrEmpty(normalizedImpressionId)
          && _adRevenueIds.Contains(normalizedImpressionId))
        || !TryNormalizeCurrency(currency, out string normalizedCurrency)
        || double.IsNaN(revenue)
        || double.IsInfinity(revenue)
        || revenue < 0d)
      {
        LogRejected("广告收益参数无效或 impressionId 已处理，ad_revenue 已忽略。");
        return false;
      }

      Dictionary<string, object> data = NewAdPayload(placement, adType, adNetwork, 3);
      data["revenue"] = revenue;
      data["currency"] = normalizedCurrency;
      AddText(data, "impression_id", normalizedImpressionId, 256);
      if (!Enqueue("ad_revenue", data, true))
      {
        return false;
      }

      if (!string.IsNullOrEmpty(normalizedImpressionId))
      {
        _adRevenueIds.Add(normalizedImpressionId);
      }
      return true;
    }

    internal bool TrackWithdrawalStageEnter(int stage, double stageThresholdAmount)
    {
      if (!_hasEconomyContext
        || stage < 0
        || double.IsNaN(stageThresholdAmount)
        || double.IsInfinity(stageThresholdAmount)
        || stageThresholdAmount < 0d)
      {
        LogRejected("进入提现阶段前必须设置有效的经济上下文。");
        return false;
      }

      int previousStage = _withdrawalStage;
      _withdrawalStage = stage;
      Dictionary<string, object> data = NewPayload(1);
      data["stage_threshold_amount"] = stageThresholdAmount;
      if (!Enqueue("withdrawal_stage_enter", data, true))
      {
        _withdrawalStage = previousStage;
        return false;
      }
      return true;
    }

    internal bool TrackWithdrawalRequest(string requestId, double amount, string currency)
    {
      string candidateId = NormalizeOptionalText(requestId, 128);
      if (string.IsNullOrEmpty(candidateId)
        || _withdrawalRequestIds.Contains(candidateId)
        || !TryCreateWithdrawalPayload(
          requestId,
          amount,
          currency,
          0,
          out string normalizedRequestId,
          out Dictionary<string, object> data))
      {
        LogRejected("提现请求参数无效或 requestId 已处理。");
        return false;
      }

      if (!Enqueue("withdrawal_request", data, true))
      {
        return false;
      }

      _withdrawalRequestIds.Add(normalizedRequestId);
      return true;
    }

    internal bool TrackWithdrawalResult(
      string requestId,
      double amount,
      string currency,
      bool success,
      string failureReason)
    {
      string candidateId = NormalizeOptionalText(requestId, 128);
      if (string.IsNullOrEmpty(candidateId)
        || _withdrawalResultIds.Contains(candidateId)
        || !TryCreateWithdrawalPayload(
          requestId,
          amount,
          currency,
          2,
          out string normalizedRequestId,
          out Dictionary<string, object> data))
      {
        LogRejected("提现结果参数无效或 requestId 已处理。");
        return false;
      }

      data["result"] = success ? "success" : "failed";
      if (!success)
      {
        AddText(data, "failure_reason", failureReason, 256);
      }
      if (!Enqueue("withdrawal_result", data, true))
      {
        return false;
      }

      _withdrawalResultIds.Add(normalizedRequestId);
      return true;
    }

    internal void TrackException(
      string message,
      string stackTrace,
      string exceptionType,
      bool isFatal)
    {
      Dictionary<string, object> data = NewPayload(4);
      data["message"] = Truncate(message, MaxCrashTextLength);
      data["stack_trace"] = Truncate(stackTrace, MaxCrashStackLength);
      data["exception_type"] = NormalizeText(exceptionType, "unknown", 256);
      data["is_fatal"] = isFatal;
      Enqueue("crash", data, true);
    }

    private void Update()
    {
      if (!_trackingStarted || !_sessionActive)
      {
        return;
      }

      float deltaTime = Mathf.Clamp(Time.unscaledDeltaTime, 0f, 1f);
      if (_skipNextFrameDelta)
      {
        _skipNextFrameDelta = false;
        deltaTime = 0f;
      }
      _sessionAccumulatedSeconds += deltaTime;
      _foregroundElapsedSeconds += deltaTime;
      _userPropertyElapsed += deltaTime;

      if (_options.EnablePerformanceTracking)
      {
        _performanceElapsed += deltaTime;
        _performanceFrames += 1;
        if (deltaTime >= _options.SlowFrameThresholdSeconds)
        {
          _slowFrames += 1;
        }

        if (_performanceElapsed >= _options.PerformanceSampleIntervalSeconds)
        {
          EmitPerformanceSample();
        }
      }

      if (_userPropertyElapsed >= UserPropertyCoalesceSeconds)
      {
        FlushUserProperties();
      }
    }

    private void LateUpdate()
    {
      if (!_trackingStarted)
      {
        return;
      }

      DrainOneEvent();
    }

    private void OnApplicationPause(bool paused)
    {
      if (!_trackingStarted)
      {
        return;
      }

      if (paused)
      {
        CloseLifecycle("application_pause");
      }
      else if (!_sessionActive)
      {
        _lifecycleClosed = false;
        BeginSession("application_resume");
      }
    }

    private void OnApplicationQuit()
    {
      if (_trackingStarted)
      {
        CloseLifecycle("application_quit");
      }
    }

    private void OnDestroy()
    {
      if (_instance != this)
      {
        return;
      }

      if (_trackingStarted)
      {
        CloseLifecycle("host_destroy");
      }
      UnregisterExceptionCallback();
      ClearQueues();
      _instance = null;
    }

    private void BeginSession(string source)
    {
      if (_sessionActive)
      {
        return;
      }

      _sessionActive = true;
      _sessionId = Guid.NewGuid().ToString("N");
      _sessionStartedAt = RealtimeNow();
      _sessionAccumulatedSeconds = 0d;
      _skipNextFrameDelta = true;
      _automaticExceptionCount = 0;
      ResetPerformanceSample();

      Dictionary<string, object> data = NewPayload(2);
      data["session_id"] = _sessionId;
      data["source"] = source;
      Enqueue("session_start", data, false);
    }

    private void EndSession(string reason)
    {
      if (!_sessionActive)
      {
        return;
      }

      double measuredDuration = Math.Max(0d, RealtimeNow() - _sessionStartedAt);
      double duration = Math.Max(measuredDuration, _sessionAccumulatedSeconds);
      _totalGameTimeSeconds += duration;

      Dictionary<string, object> data = NewPayload(3);
      data["session_id"] = _sessionId;
      data["duration_seconds"] = RoundDuration(duration);
      data["end_reason"] = reason;
      Enqueue("session_end", data, true, true);

      SetUserProperty("play_time", (long)Math.Floor(duration));
      SetUserProperty("total_gametime", (long)Math.Floor(_totalGameTimeSeconds));
      SetUserProperty("total_ipu", _totalImpressions);

      _sessionActive = false;
      _sessionId = string.Empty;
      _sessionStartedAt = 0d;
      _sessionAccumulatedSeconds = 0d;
    }

    private void CloseLifecycle(string reason)
    {
      if (_lifecycleClosed)
      {
        return;
      }

      _lifecycleClosed = true;
      if (_performanceFrames > 0 && _performanceElapsed >= 5f)
      {
        EmitPerformanceSample();
      }
      EndSession(reason);
      SaveLocalState();
      FlushUserProperties();
      DrainAllEvents(LifecycleDrainLimit);
    }

    private void EmitPerformanceSample()
    {
      if (_performanceFrames <= 0 || _performanceElapsed <= 0f)
      {
        ResetPerformanceSample();
        return;
      }

      double fps = _performanceFrames / (double)_performanceElapsed;
      double slowFrameRatio = _slowFrames / (double)_performanceFrames;
      int lagScore = Mathf.RoundToInt((float)((1d - slowFrameRatio) * 100d));

      Dictionary<string, object> fpsData = NewPayload(2);
      fpsData["fps"] = Math.Round(fps, 2, MidpointRounding.AwayFromZero);
      fpsData["sample_seconds"] = RoundDuration(_performanceElapsed);
      Enqueue("fps", fpsData, false);

      Dictionary<string, object> lagData = NewPayload(3);
      lagData["lag_score"] = Mathf.Clamp(lagScore, 0, 100);
      lagData["slow_frame_ratio"] = Math.Round(slowFrameRatio, 4, MidpointRounding.AwayFromZero);
      lagData["slow_frame_threshold_ms"] = Mathf.RoundToInt(_options.SlowFrameThresholdSeconds * 1000f);
      Enqueue("lag_state", lagData, false);

      ResetPerformanceSample();
    }

    private void ResetPerformanceSample()
    {
      _performanceElapsed = 0f;
      _performanceFrames = 0;
      _slowFrames = 0;
    }

    private void RefreshExceptionCallback()
    {
      bool shouldRegister = _trackingStarted && _options.EnableAutomaticExceptionTracking;
      if (shouldRegister && !_exceptionCallbackRegistered)
      {
        Application.logMessageReceived += OnLogMessageReceived;
        _exceptionCallbackRegistered = true;
      }
      else if (!shouldRegister)
      {
        UnregisterExceptionCallback();
      }
    }

    private void UnregisterExceptionCallback()
    {
      if (!_exceptionCallbackRegistered)
      {
        return;
      }

      Application.logMessageReceived -= OnLogMessageReceived;
      _exceptionCallbackRegistered = false;
    }

    private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
    {
      if (type != LogType.Exception
        || _automaticExceptionCount >= _options.MaxAutomaticExceptionsPerSession)
      {
        return;
      }

      _automaticExceptionCount += 1;
      TrackException(condition, stackTrace, ExtractExceptionType(condition), false);
    }

    private void LoadLocalState()
    {
      _installTime = PlayerPrefs.GetString(InstallTimeKey, string.Empty);
      bool firstInstall = string.IsNullOrEmpty(_installTime);
      if (firstInstall)
      {
        _installTime = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        PlayerPrefs.SetString(InstallTimeKey, _installTime);
      }

      _totalImpressions = Math.Max(0, PlayerPrefs.GetInt(TotalImpressionsKey, 0));
      _totalGameTimeSeconds = ParseStoredDouble(PlayerPrefs.GetString(TotalGameTimeKey, "0"));
      _activeDays = Math.Max(0, PlayerPrefs.GetInt(ActiveDaysKey, 0));

      string today = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
      string lastActiveDate = PlayerPrefs.GetString(LastActiveDateKey, string.Empty);
      if (!string.Equals(lastActiveDate, today, StringComparison.Ordinal))
      {
        _activeDays += 1;
        PlayerPrefs.SetInt(ActiveDaysKey, _activeDays);
        PlayerPrefs.SetString(LastActiveDateKey, today);
      }

      if (firstInstall)
      {
        PlayerPrefs.Save();
      }
    }

    private void QueueInitialUserProperties()
    {
      SetUserProperty("game_version", NormalizeText(Application.version, "unknown", 64));
      SetUserProperty("device_info", NormalizeText(SystemInfo.deviceModel, "unknown", 256));
      SetUserProperty("install_time", _installTime);
      SetUserProperty("total_ipu", _totalImpressions);
      SetUserProperty("total_gametime", (long)Math.Floor(_totalGameTimeSeconds));
      SetUserProperty("play_time", 0L);
      SetUserProperty("active_days", _activeDays);

      string country = ResolveCountryCode();
      if (!string.IsNullOrEmpty(country))
      {
        SetUserProperty("country", country);
      }
    }

    private void SaveLocalState()
    {
      PlayerPrefs.SetInt(TotalImpressionsKey, _totalImpressions);
      PlayerPrefs.SetString(
        TotalGameTimeKey,
        _totalGameTimeSeconds.ToString("R", CultureInfo.InvariantCulture));
      PlayerPrefs.SetInt(ActiveDaysKey, _activeDays);
      PlayerPrefs.Save();
    }

    private void SetUserProperty(string key, object value)
    {
      _pendingUserProperties[key] = value;
    }

    private void SetUserPropertyIfNotEmpty(string key, string value, int maxLength)
    {
      string normalized = NormalizeOptionalText(value, maxLength);
      if (!string.IsNullOrEmpty(normalized))
      {
        SetUserProperty(key, normalized);
      }
    }

    private void FlushUserProperties()
    {
      _userPropertyElapsed = 0f;
      if (_pendingUserProperties.Count == 0 || !CanSendToBizza())
      {
        return;
      }

      try
      {
        int propertyCount = _pendingUserProperties.Count;
        global::BizzaAnalyticsAgent agent = global::BizzaAnalyticsAgent.Instance;
        bool reportingEnabled = agent != null && agent.IsReportingEnabled;
        global::BizzaAnalyticsAgent.UserProp(_pendingUserProperties);
        _pendingUserProperties.Clear();
        LogUserPropertiesProcessed(propertyCount, reportingEnabled);
      }
      catch (Exception exception)
      {
        LogSubmissionFailure("user_properties", exception);
      }
    }

    private Dictionary<string, object> NewPayload(int extraCapacity = 0)
    {
      Dictionary<string, object> data = BizzaDictionaryPool.Rent();
      if (!string.IsNullOrEmpty(_sessionId))
      {
        data["session_id"] = _sessionId;
      }
      if (_hasEconomyContext)
      {
        data["current_balance"] = _currentBalance;
        data["first_withdrawal_threshold"] = _firstWithdrawalThreshold;
        data["withdrawal_stage"] = _withdrawalStage;
        data["balance_unit"] = _balanceUnit;
      }
      return data;
    }

    private Dictionary<string, object> NewAdPayload(
      string placement,
      BizzaAdType adType,
      string adNetwork,
      int extraCapacity = 0)
    {
      Dictionary<string, object> data = NewPayload(3 + extraCapacity);
      data["placement"] = NormalizeText(placement, "unknown", 128);
      data["ad_type"] = AdTypeToString(adType);
      data["ad_network"] = NormalizeText(adNetwork, "unknown", 128);
      return data;
    }

    private bool TryCreateWithdrawalPayload(
      string requestId,
      double amount,
      string currency,
      int extraCapacity,
      out string normalizedRequestId,
      out Dictionary<string, object> data)
    {
      normalizedRequestId = NormalizeOptionalText(requestId, 128);
      data = null;
      if (string.IsNullOrEmpty(normalizedRequestId)
        || !TryNormalizeCurrency(currency, out string normalizedCurrency)
        || double.IsNaN(amount)
        || double.IsInfinity(amount)
        || amount < 0d)
      {
        return false;
      }

      data = NewPayload(3 + extraCapacity);
      data["request_id"] = normalizedRequestId;
      data["amount"] = amount;
      data["currency"] = normalizedCurrency;
      return true;
    }

    private bool Enqueue(
      string eventName,
      Dictionary<string, object> data,
      bool critical,
      bool insertAtFront = false)
    {
      BizzaPendingEventQueue queue = critical ? _criticalEvents : _regularEvents;
      bool accepted = insertAtFront
        ? queue.EnqueueFront(eventName, data)
        : queue.Enqueue(eventName, data);
      if (accepted)
      {
        LogEventAccepted(eventName, data, critical);
        return true;
      }

      LogEventRejected(eventName, data, critical, "queue_full");
      BizzaDictionaryPool.Return(data);
      if (critical && !_criticalQueueOverflowErrorLogged)
      {
        _criticalQueueOverflowErrorLogged = true;
        Debug.LogError(
          "[BizzaGameAnalytics] 关键事件队列已满，本次事件未被接受：" + eventName);
      }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
      else if (!critical && !_regularQueueOverflowWarningLogged)
      {
        _regularQueueOverflowWarningLogged = true;
        Debug.LogWarning(
          "[BizzaGameAnalytics] 普通事件队列已满，本次事件未被接受：" + eventName);
      }
#endif
      return false;
    }

    private void DrainOneEvent()
    {
      if (!CanSendToBizza())
      {
        return;
      }

      if (!_criticalEvents.TryDequeue(out BizzaPendingEvent pending)
        && !_regularEvents.TryDequeue(out pending))
      {
        return;
      }

      SendPendingEvent(pending);
    }

    private void DrainAllEvents(int maxCount)
    {
      if (!CanSendToBizza())
      {
        return;
      }

      int sent = 0;
      while (sent < maxCount)
      {
        if (!_criticalEvents.TryDequeue(out BizzaPendingEvent pending)
          && !_regularEvents.TryDequeue(out pending))
        {
          break;
        }

        SendPendingEvent(pending);
        sent += 1;
      }
    }

    private void SendPendingEvent(BizzaPendingEvent pending)
    {
      bool submitted = false;
      try
      {
        global::BizzaAnalyticsAgent agent = global::BizzaAnalyticsAgent.Instance;
        bool reportingEnabled = agent != null && agent.IsReportingEnabled;
        global::BizzaAnalyticsAgent.Track(pending.EventName, pending.Data);
        LogEventProcessed(pending.EventName, pending.Data, reportingEnabled);
        submitted = true;
      }
      catch (Exception exception)
      {
        LogSubmissionFailure(pending.EventName, exception);
      }
      finally
      {
        BizzaDictionaryPool.Return(pending.Data);
      }

      if (submitted)
      {
        OnEventSubmitted(pending.EventName);
      }
    }

    private bool CanSendToBizza()
    {
      global::BizzaAnalyticsAgent agent = global::BizzaAnalyticsAgent.Instance;
      if (agent == null)
      {
        return false;
      }

      BizzaAnalyticsInitializationState state = agent.InitializationState;
      LogBizzaStateIfChanged(agent, state);
      if (state == BizzaAnalyticsInitializationState.Disabled
        || state == BizzaAnalyticsInitializationState.Failed)
      {
        LogQueuesCleared(_criticalEvents.Count, _regularEvents.Count, _pendingUserProperties.Count);
        ClearQueues();
        _pendingUserProperties.Clear();
        return false;
      }

      return state == BizzaAnalyticsInitializationState.Ready;
    }

    private void ClearQueues()
    {
      while (_criticalEvents.TryDequeue(out BizzaPendingEvent critical))
      {
        BizzaDictionaryPool.Return(critical.Data);
      }
      while (_regularEvents.TryDequeue(out BizzaPendingEvent regular))
      {
        BizzaDictionaryPool.Return(regular.Data);
      }
    }

    private void OnEventSubmitted(string eventName)
    {
      if (string.Equals(eventName, "user_install", StringComparison.Ordinal))
      {
        PlayerPrefs.SetInt(InstallEventSentKey, 1);
        PlayerPrefs.Save();
      }
      else if (string.Equals(eventName, "user_activate", StringComparison.Ordinal))
      {
        _activationQueued = false;
        PlayerPrefs.SetInt(ActivationKey, 1);
        PlayerPrefs.Save();
      }
      else if (string.Equals(eventName, "ad_impression", StringComparison.Ordinal))
      {
        _totalImpressions += 1;
        PlayerPrefs.SetInt(TotalImpressionsKey, _totalImpressions);
        if ((_totalImpressions % TotalImpressionPropertyInterval) == 0)
        {
          SetUserProperty("total_ipu", _totalImpressions);
        }
      }
    }

    private void ClearActiveLevel()
    {
      _activeLevelId = string.Empty;
      _activeLevelAttemptId = string.Empty;
      _activeLevelAttempt = 0;
      _activeLevelStartedAt = 0d;
    }

    private static string AdTypeToString(BizzaAdType adType)
    {
      switch (adType)
      {
        case BizzaAdType.Rewarded:
          return "rewarded";
        case BizzaAdType.Interstitial:
          return "interstitial";
        case BizzaAdType.Banner:
          return "banner";
        case BizzaAdType.AppOpen:
          return "app_open";
        default:
          return "other";
      }
    }

    private static void AddText(
      Dictionary<string, object> data,
      string key,
      string value,
      int maxLength)
    {
      string normalized = NormalizeOptionalText(value, maxLength);
      if (!string.IsNullOrEmpty(normalized))
      {
        data[key] = normalized;
      }
    }

    private static string NormalizeText(string value, string fallback, int maxLength)
    {
      string normalized = NormalizeOptionalText(value, maxLength);
      return string.IsNullOrEmpty(normalized) ? fallback : normalized;
    }

    private static string NormalizeOptionalText(string value, int maxLength)
    {
      if (string.IsNullOrWhiteSpace(value))
      {
        return string.Empty;
      }

      string normalized = value.Trim();
      return normalized.Length <= maxLength ? normalized : normalized.Substring(0, maxLength);
    }

    private static bool TryNormalizeCurrency(string value, out string currency)
    {
      string raw = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
      if (raw.Length != 3)
      {
        currency = string.Empty;
        return false;
      }
      currency = raw.ToUpperInvariant();

      for (int i = 0; i < currency.Length; i++)
      {
        if (currency[i] < 'A' || currency[i] > 'Z')
        {
          currency = string.Empty;
          return false;
        }
      }
      return true;
    }

    private static bool TryNormalizeUnit(string value, out string unit)
    {
      string raw = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
      if (raw.Length == 0 || raw.Length > 16)
      {
        unit = string.Empty;
        return false;
      }
      unit = raw.ToUpperInvariant();

      for (int i = 0; i < unit.Length; i++)
      {
        char character = unit[i];
        bool accepted = (character >= 'A' && character <= 'Z')
          || (character >= '0' && character <= '9')
          || character == '_';
        if (!accepted)
        {
          unit = string.Empty;
          return false;
        }
      }
      return true;
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private static void LogRejected(string message)
    {
      Debug.LogWarning("[BizzaGameAnalytics] " + message);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private static void LogCall(string callName, string result)
    {
      Debug.Log("[BizzaGameAnalytics] call=" + callName + " result=" + result);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private void LogBizzaStateIfChanged(
      global::BizzaAnalyticsAgent agent,
      BizzaAnalyticsInitializationState state)
    {
      bool reportingEnabled = agent != null && agent.IsReportingEnabled;
      if (_hasLoggedBizzaState
        && _lastLoggedBizzaState == state
        && _lastLoggedReportingEnabled == reportingEnabled)
      {
        return;
      }

      _hasLoggedBizzaState = true;
      _lastLoggedBizzaState = state;
      _lastLoggedReportingEnabled = reportingEnabled;
      Debug.Log(
        "[BizzaGameAnalytics] bizza_state=" + GetInitializationStateName(state)
        + " reporting_enabled=" + reportingEnabled);
    }

    private static string GetInitializationStateName(BizzaAnalyticsInitializationState state)
    {
      switch ((int)state)
      {
        case 0:
          return "Uninitialized";
        case 1:
          return "Loading";
        case 2:
          return "Ready";
        case 3:
          return "Disabled";
        case 4:
          return "Failed";
        default:
          return "Unknown";
      }
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private void LogEventAccepted(
      string eventName,
      Dictionary<string, object> data,
      bool critical)
    {
      Debug.Log(
        "[BizzaGameAnalytics] event_accepted name=" + eventName
        + " priority=" + (critical ? "critical" : "regular")
        + BuildCorrelationLog(data)
        + " pending_critical=" + _criticalEvents.Count
        + " pending_regular=" + _regularEvents.Count);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private static void LogEventProcessed(
      string eventName,
      Dictionary<string, object> data,
      bool reportingEnabled)
    {
      string action = reportingEnabled ? "event_handed_off" : "event_sampled_out";
      Debug.Log(
        "[BizzaGameAnalytics] " + action + " name=" + eventName
        + BuildCorrelationLog(data));
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private static void LogEventRejected(
      string eventName,
      Dictionary<string, object> data,
      bool critical,
      string reason)
    {
      Debug.LogWarning(
        "[BizzaGameAnalytics] event_rejected name=" + eventName
        + " priority=" + (critical ? "critical" : "regular")
        + " reason=" + reason
        + BuildCorrelationLog(data));
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private static void LogUserPropertiesProcessed(int propertyCount, bool reportingEnabled)
    {
      string action = reportingEnabled
        ? "user_properties_handed_off"
        : "user_properties_sampled_out";
      Debug.Log(
        "[BizzaGameAnalytics] " + action + " property_count=" + propertyCount);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private static void LogSubmissionFailure(string name, Exception exception)
    {
      string message = exception == null ? "unknown" : Truncate(exception.Message, 256);
      Debug.LogWarning(
        "[BizzaGameAnalytics] submission_failed name=" + name + " error=" + message);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private static void LogQueuesCleared(int criticalCount, int regularCount, int propertyCount)
    {
      if (criticalCount == 0 && regularCount == 0 && propertyCount == 0)
      {
        return;
      }

      Debug.LogWarning(
        "[BizzaGameAnalytics] pending_data_cleared reason=bizza_unavailable"
        + " critical=" + criticalCount
        + " regular=" + regularCount
        + " user_properties=" + propertyCount);
    }

    private static string BuildCorrelationLog(Dictionary<string, object> data)
    {
      if (data == null)
      {
        return string.Empty;
      }

      string[] keys = { "request_id", "impression_id", "attempt_id", "session_id" };
      for (int i = 0; i < keys.Length; i++)
      {
        if (data.TryGetValue(keys[i], out object value) && value != null)
        {
          return " " + keys[i] + "=" + Truncate(value.ToString(), 128);
        }
      }
      return string.Empty;
    }

    private static string Truncate(string value, int maxLength)
    {
      if (string.IsNullOrEmpty(value))
      {
        return string.Empty;
      }
      return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }

    private static string ExtractExceptionType(string condition)
    {
      if (string.IsNullOrWhiteSpace(condition))
      {
        return "unknown";
      }

      int colonIndex = condition.IndexOf(':');
      string candidate = colonIndex > 0 ? condition.Substring(0, colonIndex) : condition;
      return NormalizeText(candidate, "unknown", 256);
    }

    private static string ResolveCountryCode()
    {
      try
      {
        string code = RegionInfo.CurrentRegion.TwoLetterISORegionName;
        if (string.IsNullOrWhiteSpace(code) || code.Length != 2 || code == "IV")
        {
          return string.Empty;
        }
        return code.ToUpperInvariant();
      }
      catch
      {
        return string.Empty;
      }
    }

    private static double ParseStoredDouble(string value)
    {
      if (double.TryParse(
        value,
        NumberStyles.Float,
        CultureInfo.InvariantCulture,
        out double parsed)
        && !double.IsNaN(parsed)
        && !double.IsInfinity(parsed))
      {
        return Math.Max(0d, parsed);
      }
      return 0d;
    }

    private static double FiniteOrZero(double value)
    {
      return double.IsNaN(value) || double.IsInfinity(value) ? 0d : value;
    }

    private static double RoundDuration(double seconds)
    {
      return Math.Round(Math.Max(0d, FiniteOrZero(seconds)), 3, MidpointRounding.AwayFromZero);
    }

    private static double RealtimeNow()
    {
      return Time.realtimeSinceStartupAsDouble;
    }

    private double ActiveTimeNow()
    {
      return _foregroundElapsedSeconds;
    }
  }

  internal struct BizzaPendingEvent
  {
    public string EventName;
    public Dictionary<string, object> Data;
  }

  internal sealed class BizzaPendingEventQueue
  {
    private readonly BizzaPendingEvent[] _items;
    private int _head;
    private int _tail;
    private int _count;

    public BizzaPendingEventQueue(int capacity)
    {
      _items = new BizzaPendingEvent[Math.Max(1, capacity)];
    }

    public int Count
    {
      get { return _count; }
    }

    public bool Enqueue(string eventName, Dictionary<string, object> data)
    {
      if (_count >= _items.Length)
      {
        return false;
      }

      _items[_tail] = new BizzaPendingEvent
      {
        EventName = eventName,
        Data = data,
      };
      _tail = (_tail + 1) % _items.Length;
      _count += 1;
      return true;
    }

    public bool EnqueueFront(string eventName, Dictionary<string, object> data)
    {
      if (_count >= _items.Length)
      {
        return false;
      }

      _head = (_head - 1 + _items.Length) % _items.Length;
      _items[_head] = new BizzaPendingEvent
      {
        EventName = eventName,
        Data = data,
      };
      _count += 1;
      return true;
    }

    public bool TryDequeue(out BizzaPendingEvent pending)
    {
      if (_count == 0)
      {
        pending = default(BizzaPendingEvent);
        return false;
      }

      pending = _items[_head];
      _items[_head] = default(BizzaPendingEvent);
      _head = (_head + 1) % _items.Length;
      _count -= 1;
      return true;
    }
  }

  internal sealed class BizzaRecentIdSet
  {
    private readonly int _capacity;
    private readonly HashSet<string> _ids;
    private readonly Queue<string> _order;

    public BizzaRecentIdSet(int capacity)
    {
      _capacity = Math.Max(1, capacity);
      _ids = new HashSet<string>(StringComparer.Ordinal);
      _order = new Queue<string>(_capacity);
    }

    public bool Contains(string id)
    {
      return !string.IsNullOrEmpty(id) && _ids.Contains(id);
    }

    public void Add(string id)
    {
      if (string.IsNullOrEmpty(id) || !_ids.Add(id))
      {
        return;
      }

      _order.Enqueue(id);
      if (_order.Count > _capacity)
      {
        _ids.Remove(_order.Dequeue());
      }
    }
  }

  internal static class BizzaDictionaryPool
  {
    private const int MaxPoolSize = 64;
    private static readonly Stack<Dictionary<string, object>> Pool
      = new Stack<Dictionary<string, object>>(MaxPoolSize);

    public static Dictionary<string, object> Rent()
    {
      return Pool.Count > 0
        ? Pool.Pop()
        : new Dictionary<string, object>(12);
    }

    public static void Return(Dictionary<string, object> data)
    {
      if (data == null)
      {
        return;
      }

      data.Clear();
      if (Pool.Count < MaxPoolSize)
      {
        Pool.Push(data);
      }
    }
  }
}
