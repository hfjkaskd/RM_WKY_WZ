using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

internal sealed class BizzaRecord
{
  internal int StorageBytes;
  public string type = "";
  public string ts = "";
  public string eventName = "";
  public Dictionary<string, object> payload = new Dictionary<string, object>();
  public string key = "";
  public object oldValue;
  public object newValue;
  public Dictionary<string, object> meta = new Dictionary<string, object>();

  public static BizzaRecord NewEvent(string eventName, Dictionary<string, object> payload)
  {
    return new BizzaRecord
    {
      type = "event",
      ts = DateTime.UtcNow.ToString("o"),
      eventName = eventName,
      payload = payload ?? new Dictionary<string, object>(),
    };
  }

  public static BizzaRecord NewUserProfileChange(string key, object oldValue, object newValue, string ts)
  {
    return new BizzaRecord
    {
      type = "user_profile_change",
      ts = ts,
      key = key,
      oldValue = oldValue,
      newValue = newValue,
      meta = new Dictionary<string, object>(),
    };
  }

  public Dictionary<string, object> ToWireRecord(string originDt)
  {
    if (type == "event")
    {
      var eventRecord = new Dictionary<string, object>(string.IsNullOrEmpty(originDt) ? 4 : 5);
      eventRecord["type"] = "event";
      eventRecord["ts"] = ts;
      eventRecord["event_name"] = eventName;
      eventRecord["payload"] = payload ?? new Dictionary<string, object>();
      if (!string.IsNullOrEmpty(originDt))
      {
        eventRecord["origin_dt"] = originDt;
      }
      return eventRecord;
    }

    var hasOriginDt = !string.IsNullOrEmpty(originDt);
    var metaCount = meta == null ? 0 : meta.Count;
    var addOriginDtToMeta = hasOriginDt && (meta == null || !meta.ContainsKey("origin_dt"));
    var metaObj = new Dictionary<string, object>(metaCount + (addOriginDtToMeta ? 1 : 0));
    if (meta != null)
    {
      foreach (var kv in meta)
      {
        metaObj[kv.Key] = kv.Value;
      }
    }
    if (addOriginDtToMeta)
    {
      metaObj["origin_dt"] = originDt;
    }

    var profileRecordCapacity = 5;
    if (metaObj.Count > 0)
    {
      profileRecordCapacity += 1;
    }
    if (hasOriginDt)
    {
      profileRecordCapacity += 1;
    }

    var profileRecord = new Dictionary<string, object>(profileRecordCapacity);
    profileRecord["type"] = "user_profile_change";
    profileRecord["ts"] = ts;
    profileRecord["key"] = key;
    profileRecord["old"] = oldValue;
    profileRecord["new"] = newValue;
    if (metaObj.Count > 0)
    {
      profileRecord["meta"] = metaObj;
    }
    if (hasOriginDt)
    {
      profileRecord["origin_dt"] = originDt;
    }
    return profileRecord;
  }

  public Dictionary<string, object> ToStateObject()
  {
    var data = new Dictionary<string, object>
    {
      { "type", type },
      { "ts", ts },
    };

    if (type == "event")
    {
      data["event_name"] = eventName;
      data["payload"] = payload ?? new Dictionary<string, object>();
      return data;
    }

    data["key"] = key;
    data["old"] = oldValue;
    data["new"] = newValue;
    if (meta != null && meta.Count > 0)
    {
      data["meta"] = meta;
    }
    return data;
  }

  public static BizzaRecord FromStateObject(object raw)
  {
    var map = BizzaValueUtil.AsStringObjectDictionary(raw);
    var type = BizzaValueUtil.AsString(map.ContainsKey("type") ? map["type"] : null, "");
    var record = new BizzaRecord
    {
      type = type,
      ts = BizzaValueUtil.AsString(map.ContainsKey("ts") ? map["ts"] : null, DateTime.UtcNow.ToString("o")),
    };

    if (type == "event")
    {
      record.eventName = BizzaValueUtil.AsString(map.ContainsKey("event_name") ? map["event_name"] : null, "");
      record.payload = BizzaValueUtil.AsStringObjectDictionary(map.ContainsKey("payload") ? map["payload"] : null);
      return record;
    }

    record.key = BizzaValueUtil.AsString(map.ContainsKey("key") ? map["key"] : null, "");
    record.oldValue = map.ContainsKey("old") ? map["old"] : null;
    record.newValue = map.ContainsKey("new") ? map["new"] : null;
    record.meta = BizzaValueUtil.AsStringObjectDictionary(map.ContainsKey("meta") ? map["meta"] : null);
    return record;
  }
}

internal sealed class BizzaBatch
{
  public string batchId = "";
  public string channel = "realtime";
  public string dt = "";
  public string originDt = "";
  public string earliestSendDt = "";
  public int part = -1;
  public int retryCount = 0;
  public string nextRetryAt = "";
  public List<BizzaRecord> records = new List<BizzaRecord>();

  public Dictionary<string, object> ToStateObject()
  {
    var recordList = new List<object>();
    for (var i = 0; i < records.Count; i++)
    {
      recordList.Add(records[i].ToStateObject());
    }

    return new Dictionary<string, object>
    {
      { "batch_id", batchId },
      { "channel", channel },
      { "dt", dt },
      { "origin_dt", originDt },
      { "earliest_send_dt", earliestSendDt },
      { "part", part },
      { "retry_count", retryCount },
      { "next_retry_at", nextRetryAt },
      { "records", recordList },
    };
  }

  public static BizzaBatch FromStateObject(object raw)
  {
    var map = BizzaValueUtil.AsStringObjectDictionary(raw);
    var batch = new BizzaBatch
    {
      batchId = BizzaValueUtil.AsString(map.ContainsKey("batch_id") ? map["batch_id"] : null, ""),
      channel = BizzaValueUtil.AsString(map.ContainsKey("channel") ? map["channel"] : null, "realtime"),
      dt = BizzaValueUtil.AsString(map.ContainsKey("dt") ? map["dt"] : null, ""),
      originDt = BizzaValueUtil.AsString(map.ContainsKey("origin_dt") ? map["origin_dt"] : null, ""),
      earliestSendDt = BizzaValueUtil.AsString(map.ContainsKey("earliest_send_dt") ? map["earliest_send_dt"] : null, ""),
      part = BizzaValueUtil.AsInt(map.ContainsKey("part") ? map["part"] : null, -1),
      retryCount = BizzaValueUtil.AsInt(map.ContainsKey("retry_count") ? map["retry_count"] : null, 0),
      nextRetryAt = BizzaValueUtil.AsString(map.ContainsKey("next_retry_at") ? map["next_retry_at"] : null, ""),
    };

    var listRaw = BizzaValueUtil.AsList(map.ContainsKey("records") ? map["records"] : null);
    for (var i = 0; i < listRaw.Count; i++)
    {
      batch.records.Add(BizzaRecord.FromStateObject(listRaw[i]));
    }
    return batch;
  }
}

internal sealed class BizzaState
{
  public string uid = "";
  public string playerId = "";
  public bool registerSent;
  public double gameDurationSeconds;
  public double userProfileFlushElapsedSeconds;
  public Dictionary<string, int> dailyRecordCountByDt = new Dictionary<string, int>();
  public Dictionary<string, double> dailyActiveSecondsByDt = new Dictionary<string, double>();
  public Dictionary<string, double> dailyFirstPhaseFlushElapsedByDt = new Dictionary<string, double>();
  public Dictionary<string, double> dailyFallbackFlushElapsedByDt = new Dictionary<string, double>();
  public Dictionary<string, object> userProps = new Dictionary<string, object>();
  public List<BizzaRecord> realtimeBuffer = new List<BizzaRecord>();
  public List<BizzaRecord> userProfileBuffer = new List<BizzaRecord>();
  public List<BizzaBatch> pendingBatches = new List<BizzaBatch>();
  public List<BizzaBatch> delayedQueue = new List<BizzaBatch>();
  public Dictionary<string, int> realtimeSeqByDt = new Dictionary<string, int>();
  public Dictionary<string, int> delayedSeqByDt = new Dictionary<string, int>();
  public string delayedQueueNonEmptySince = "";

  public static BizzaState CreateDefault()
  {
    return new BizzaState();
  }

  public Dictionary<string, object> ToStateObject()
  {
    var realtimeList = new List<object>();
    for (var i = 0; i < realtimeBuffer.Count; i++)
    {
      realtimeList.Add(realtimeBuffer[i].ToStateObject());
    }

    var userProfileList = new List<object>();
    for (var i = 0; i < userProfileBuffer.Count; i++)
    {
      userProfileList.Add(userProfileBuffer[i].ToStateObject());
    }

    var pendingList = new List<object>();
    for (var i = 0; i < pendingBatches.Count; i++)
    {
      pendingList.Add(pendingBatches[i].ToStateObject());
    }

    var delayedList = new List<object>();
    for (var i = 0; i < delayedQueue.Count; i++)
    {
      delayedList.Add(delayedQueue[i].ToStateObject());
    }

    return new Dictionary<string, object>
    {
      { "uid", uid },
      { "player_id", playerId },
      { "register_sent", registerSent },
      { "game_duration_seconds", gameDurationSeconds },
      { "user_profile_flush_elapsed_seconds", userProfileFlushElapsedSeconds },
      { "daily_record_count_by_dt", new Dictionary<string, int>(dailyRecordCountByDt) },
      { "daily_active_seconds_by_dt", new Dictionary<string, double>(dailyActiveSecondsByDt) },
      { "daily_first_phase_flush_elapsed_by_dt", new Dictionary<string, double>(dailyFirstPhaseFlushElapsedByDt) },
      { "daily_fallback_flush_elapsed_by_dt", new Dictionary<string, double>(dailyFallbackFlushElapsedByDt) },
      { "user_props", new Dictionary<string, object>(userProps) },
      { "realtime_buffer", realtimeList },
      { "user_profile_buffer", userProfileList },
      { "pending_batches", pendingList },
      { "delayed_queue", delayedList },
      { "realtime_seq_by_dt", new Dictionary<string, int>(realtimeSeqByDt) },
      { "delayed_seq_by_dt", new Dictionary<string, int>(delayedSeqByDt) },
      { "delayed_queue_non_empty_since", delayedQueueNonEmptySince },
    };
  }

  public static BizzaState FromStateObject(object raw)
  {
    var map = BizzaValueUtil.AsStringObjectDictionary(raw);
    var state = CreateDefault();

    state.uid = BizzaValueUtil.AsString(map.ContainsKey("uid") ? map["uid"] : null, "");
    state.playerId = BizzaValueUtil.AsString(map.ContainsKey("player_id") ? map["player_id"] : null, "");
    state.registerSent = BizzaValueUtil.AsBool(map.ContainsKey("register_sent") ? map["register_sent"] : null, false);
    state.gameDurationSeconds = BizzaValueUtil.AsDouble(map.ContainsKey("game_duration_seconds") ? map["game_duration_seconds"] : null, 0);
    state.userProfileFlushElapsedSeconds = BizzaValueUtil.AsDouble(
      map.ContainsKey("user_profile_flush_elapsed_seconds") ? map["user_profile_flush_elapsed_seconds"] : null,
      0d
    );
    state.dailyRecordCountByDt = BizzaValueUtil.AsStringIntDictionary(map.ContainsKey("daily_record_count_by_dt") ? map["daily_record_count_by_dt"] : null);
    state.dailyActiveSecondsByDt = BizzaValueUtil.AsStringDoubleDictionary(
      map.ContainsKey("daily_active_seconds_by_dt") ? map["daily_active_seconds_by_dt"] : null
    );
    state.dailyFirstPhaseFlushElapsedByDt = BizzaValueUtil.AsStringDoubleDictionary(
      map.ContainsKey("daily_first_phase_flush_elapsed_by_dt") ? map["daily_first_phase_flush_elapsed_by_dt"] : null
    );
    state.dailyFallbackFlushElapsedByDt = BizzaValueUtil.AsStringDoubleDictionary(
      map.ContainsKey("daily_fallback_flush_elapsed_by_dt") ? map["daily_fallback_flush_elapsed_by_dt"] : null
    );
    state.userProps = BizzaValueUtil.AsStringObjectDictionary(map.ContainsKey("user_props") ? map["user_props"] : null);
    state.realtimeSeqByDt = BizzaValueUtil.AsStringIntDictionary(map.ContainsKey("realtime_seq_by_dt") ? map["realtime_seq_by_dt"] : null);
    state.delayedSeqByDt = BizzaValueUtil.AsStringIntDictionary(map.ContainsKey("delayed_seq_by_dt") ? map["delayed_seq_by_dt"] : null);
    state.delayedQueueNonEmptySince = BizzaValueUtil.AsString(
      map.ContainsKey("delayed_queue_non_empty_since") ? map["delayed_queue_non_empty_since"] : null,
      ""
    );

    var realtimeRaw = BizzaValueUtil.AsList(map.ContainsKey("realtime_buffer") ? map["realtime_buffer"] : null);
    for (var i = 0; i < realtimeRaw.Count; i++)
    {
      state.realtimeBuffer.Add(BizzaRecord.FromStateObject(realtimeRaw[i]));
    }

    var userProfileRaw = BizzaValueUtil.AsList(map.ContainsKey("user_profile_buffer") ? map["user_profile_buffer"] : null);
    for (var i = 0; i < userProfileRaw.Count; i++)
    {
      state.userProfileBuffer.Add(BizzaRecord.FromStateObject(userProfileRaw[i]));
    }

    var pendingRaw = BizzaValueUtil.AsList(map.ContainsKey("pending_batches") ? map["pending_batches"] : null);
    for (var i = 0; i < pendingRaw.Count; i++)
    {
      state.pendingBatches.Add(BizzaBatch.FromStateObject(pendingRaw[i]));
    }

    var delayedRaw = BizzaValueUtil.AsList(map.ContainsKey("delayed_queue") ? map["delayed_queue"] : null);
    for (var i = 0; i < delayedRaw.Count; i++)
    {
      state.delayedQueue.Add(BizzaBatch.FromStateObject(delayedRaw[i]));
    }

    return state;
  }
}

internal sealed class BizzaStateStore
{
  private readonly string _path;
  private readonly object _saveLock = new object();
  private Dictionary<string, object> _pendingSnapshot;
  private bool _writerRunning;
  private int _saveCount;
  internal Task SaveTask { get; private set; }
  internal int SaveCount { get { return Volatile.Read(ref _saveCount); } }
  internal int LastSaveThreadId { get; private set; }
  internal string LastError { get; private set; }

  public BizzaStateStore(string persistentDataPath, bool verboseLog)
  {
    _path = Path.Combine(persistentDataPath, "bizza_analytics_state.json");
  }

  public BizzaState Load()
  {
    try
    {
      if (!File.Exists(_path))
      {
        return BizzaState.CreateDefault();
      }

      byte[] bytes;
      using (var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
      {
        if (stream.Length > BizzaAnalyticsLimits.MaxLegacyFileBytes)
        {
          LastError = "legacy_analytics_file_exceeds_load_limit";
          return BizzaState.CreateDefault();
        }
        bytes = new byte[(int)stream.Length];
        var read = 0;
        while (read < bytes.Length)
        {
          var count = stream.Read(bytes, read, bytes.Length - read);
          if (count == 0) break;
          read += count;
        }
        if (read != bytes.Length) return BizzaState.CreateDefault();
      }
      var json = Encoding.UTF8.GetString(bytes).TrimStart('\uFEFF');
      if (string.IsNullOrWhiteSpace(json))
      {
        return BizzaState.CreateDefault();
      }

      var parsed = BizzaJson.Deserialize(json);
      return BizzaState.FromStateObject(parsed);
    }
    catch (Exception ex)
    {
      LastError = "load_failed:" + ex.Message;
      return BizzaState.CreateDefault();
    }
  }

  public void Save(BizzaState state)
  {
    SaveSnapshot(state.ToStateObject());
  }

  internal void RequestSave(Dictionary<string, object> snapshot)
  {
    lock (_saveLock)
    {
      // One writer and at most one waiting snapshot. Newer state replaces a
      // queued snapshot instead of creating an unbounded task/write backlog.
      _pendingSnapshot = snapshot;
      if (_writerRunning) return;
      _writerRunning = true;
      SaveTask = Task.Run((Action)DrainSaves);
    }
  }

  private void DrainSaves()
  {
    while (true)
    {
      Dictionary<string, object> snapshot;
      lock (_saveLock)
      {
        snapshot = _pendingSnapshot;
        _pendingSnapshot = null;
        if (snapshot == null) { _writerRunning = false; return; }
      }
      SaveSnapshot(snapshot);
    }
  }

  private void SaveSnapshot(Dictionary<string, object> snapshot)
  {
    try
    {
      var json = BizzaJson.Serialize(snapshot);
      var bytes = Encoding.UTF8.GetBytes(json);
      if (bytes.Length > BizzaAnalyticsLimits.MaxStateFileBytes)
      {
        LastError = "analytics_snapshot_exceeds_file_limit";
        return;
      }
      var dir = Path.GetDirectoryName(_path);
      if (!string.IsNullOrEmpty(dir))
      {
        Directory.CreateDirectory(dir);
      }
      var temp = _path + ".tmp";
      File.WriteAllBytes(temp, bytes);
      if (File.Exists(_path)) File.Replace(temp, _path, null);
      else File.Move(temp, _path);
      LastSaveThreadId = Thread.CurrentThread.ManagedThreadId;
      Interlocked.Increment(ref _saveCount);
      LastError = null;
    }
    catch (Exception ex)
    {
      LastError = "save_failed:" + ex.Message;
    }
  }
}

internal static class BizzaValueUtil
{
  public static object Normalize(object value)
  {
    if (value == null) return null;

    if (value is string || value is bool || value is int || value is long || value is double || value is float || value is decimal)
    {
      return value;
    }
    if (value is short) return (int)(short)value;
    if (value is byte) return (int)(byte)value;
    if (value is DateTime) return ((DateTime)value).ToUniversalTime().ToString("o");
    if (value is DateTimeOffset) return ((DateTimeOffset)value).ToUniversalTime().ToString("o");

    var dict = value as IDictionary;
    if (dict != null)
    {
      var normalizedDict = new Dictionary<string, object>();
      foreach (DictionaryEntry entry in dict)
      {
        normalizedDict[Convert.ToString(entry.Key)] = Normalize(entry.Value);
      }
      return normalizedDict;
    }

    var list = value as IList;
    if (list != null)
    {
      var normalizedList = new List<object>();
      for (var i = 0; i < list.Count; i++)
      {
        normalizedList.Add(Normalize(list[i]));
      }
      return normalizedList;
    }

    return Convert.ToString(value, CultureInfo.InvariantCulture);
  }

  public static Dictionary<string, object> NormalizeDictionary(Dictionary<string, object> source)
  {
    var result = new Dictionary<string, object>();
    if (source == null) return result;
    foreach (var kv in source)
    {
      result[kv.Key] = Normalize(kv.Value);
    }
    return result;
  }

  public static bool AreEqual(object a, object b)
  {
    var left = Normalize(a);
    var right = Normalize(b);
    return DeepEquals(left, right);
  }

  private static bool DeepEquals(object left, object right)
  {
    if (ReferenceEquals(left, right)) return true;
    if (left == null || right == null) return false;

    var leftMap = left as IDictionary;
    var rightMap = right as IDictionary;
    if (leftMap != null && rightMap != null)
    {
      if (leftMap.Count != rightMap.Count) return false;
      foreach (DictionaryEntry entry in leftMap)
      {
        var key = Convert.ToString(entry.Key);
        if (!rightMap.Contains(key)) return false;
        if (!DeepEquals(entry.Value, rightMap[key])) return false;
      }
      return true;
    }

    var leftList = left as IList;
    var rightList = right as IList;
    if (leftList != null && rightList != null)
    {
      if (leftList.Count != rightList.Count) return false;
      for (var i = 0; i < leftList.Count; i++)
      {
        if (!DeepEquals(leftList[i], rightList[i])) return false;
      }
      return true;
    }

    return string.Equals(
      Convert.ToString(left, CultureInfo.InvariantCulture),
      Convert.ToString(right, CultureInfo.InvariantCulture),
      StringComparison.Ordinal
    );
  }

  public static string AsString(object raw, string fallback)
  {
    if (raw == null) return fallback;
    var text = Convert.ToString(raw, CultureInfo.InvariantCulture);
    return text ?? fallback;
  }

  public static bool AsBool(object raw, bool fallback)
  {
    if (raw is bool) return (bool)raw;
    bool parsed;
    if (bool.TryParse(AsString(raw, ""), out parsed)) return parsed;
    return fallback;
  }

  public static int AsInt(object raw, int fallback)
  {
    if (raw is int) return (int)raw;
    if (raw is long) return (int)(long)raw;
    if (raw is double) return (int)(double)raw;
    int parsed;
    if (int.TryParse(AsString(raw, ""), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed)) return parsed;
    return fallback;
  }

  public static double AsDouble(object raw, double fallback)
  {
    if (raw is double) return (double)raw;
    if (raw is float) return (float)raw;
    if (raw is int) return (int)raw;
    if (raw is long) return (long)raw;
    double parsed;
    if (double.TryParse(AsString(raw, ""), NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)) return parsed;
    return fallback;
  }

  public static List<object> AsList(object raw)
  {
    var list = new List<object>();
    var rawList = raw as IList;
    if (rawList == null) return list;
    for (var i = 0; i < rawList.Count; i++)
    {
      list.Add(rawList[i]);
    }
    return list;
  }

  public static Dictionary<string, object> AsStringObjectDictionary(object raw)
  {
    var dict = new Dictionary<string, object>();
    var rawDict = raw as IDictionary;
    if (rawDict == null) return dict;
    foreach (DictionaryEntry entry in rawDict)
    {
      dict[Convert.ToString(entry.Key)] = entry.Value;
    }
    return dict;
  }

  public static Dictionary<string, int> AsStringIntDictionary(object raw)
  {
    var result = new Dictionary<string, int>();
    var rawDict = raw as IDictionary;
    if (rawDict == null) return result;
    foreach (DictionaryEntry entry in rawDict)
    {
      var key = Convert.ToString(entry.Key);
      var value = AsInt(entry.Value, 0);
      if (!string.IsNullOrEmpty(key))
      {
        result[key] = value;
      }
    }
    return result;
  }

  public static Dictionary<string, double> AsStringDoubleDictionary(object raw)
  {
    var result = new Dictionary<string, double>();
    var rawDict = raw as IDictionary;
    if (rawDict == null) return result;
    foreach (DictionaryEntry entry in rawDict)
    {
      var key = Convert.ToString(entry.Key);
      var value = AsDouble(entry.Value, 0d);
      if (!string.IsNullOrEmpty(key))
      {
        result[key] = value;
      }
    }
    return result;
  }
}
