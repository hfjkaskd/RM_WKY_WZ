using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

// Telemetry is best effort. Reject excess work before copying/serializing payloads.
internal static class BizzaAnalyticsLimits
{
  internal const int MaxRecords = 1000;
  internal const int MaxQueueBytes = 512 * 1024;
  internal const int MaxRecordBytes = 8 * 1024;
  internal const int MaxPayloadBytes = 4 * 1024;
  internal const int MaxStateFileBytes = 1024 * 1024;
  internal const int MaxLegacyFileBytes = 8 * 1024 * 1024;
  internal const int MaxPreInitRecords = 64;
  internal const int MaxPreInitBytes = 64 * 1024;
  internal const int MaxRecordsPerFrame = 16;
  internal const int MaxUserProperties = 64;
  internal const int MaxPropertyBytes = 512;
  internal const double SaveIntervalSeconds = 2d;
  // Charged even for unbatched records, so making one batch per record stays bounded.
  internal const int BatchOverheadBytes = 384;

  internal static bool TryCopy(object input, int limit, out object copy, out int bytes)
  {
    var remaining = limit;
    var nodes = 256;
    bool ok = Walk(input, true, 0, ref remaining, ref nodes, out copy);
    bytes = limit - remaining;
    return ok;
  }

  internal static bool TryMeasure(object input, int limit, out int bytes)
  {
    var remaining = limit;
    var nodes = 512;
    object ignored;
    bool ok = Walk(input, false, 0, ref remaining, ref nodes, out ignored);
    bytes = limit - remaining;
    return ok;
  }

  private static bool Charge(int amount, ref int remaining)
  {
    if (amount < 0 || amount > remaining) return false;
    remaining -= amount;
    return true;
  }

  private static bool ChargeString(string value, ref int remaining)
  {
    if (value == null) return Charge(4, ref remaining);
    if (value.Length > remaining - 2 || !Charge(2, ref remaining)) return false;
    foreach (var c in value)
    {
      int bytes = c == '"' || c == '\\' ? 2 : c >= 32 && c <= 126 ? 1 : 6;
      if (!Charge(bytes, ref remaining)) return false;
    }
    return true;
  }

  private static bool Walk(object value, bool clone, int depth, ref int remaining, ref int nodes, out object result)
  {
    result = null;
    if (depth > 8 || --nodes < 0) return false;
    if (value == null) return Charge(4, ref remaining);
    if (value is string)
    {
      result = value;
      return ChargeString((string)value, ref remaining);
    }
    if (value is bool || value is byte || value is short || value is int || value is long
      || value is float || value is double || value is decimal)
    {
      result = value;
      return Charge(40, ref remaining);
    }
    if (value is DateTime || value is DateTimeOffset)
    {
      result = value is DateTime ? ((DateTime)value).ToUniversalTime().ToString("o")
        : ((DateTimeOffset)value).ToUniversalTime().ToString("o");
      return ChargeString((string)result, ref remaining);
    }
    var map = value as IDictionary;
    if (map != null)
    {
      if (map.Count > 64 || !Charge(2, ref remaining)) return false;
      var target = clone ? new Dictionary<string, object>(map.Count) : null;
      foreach (DictionaryEntry entry in map)
      {
        var key = entry.Key as string;
        object item;
        if (key == null || !ChargeString(key, ref remaining) || !Charge(2, ref remaining)
          || !Walk(entry.Value, clone, depth + 1, ref remaining, ref nodes, out item)) return false;
        if (clone) target[key] = item;
      }
      result = target;
      return true;
    }
    var list = value as IList;
    if (list != null)
    {
      if (list.Count > 64 || !Charge(2, ref remaining)) return false;
      var target = clone ? new List<object>(list.Count) : null;
      for (var i = 0; i < list.Count; i++)
      {
        object item;
        if (!Charge(1, ref remaining) || !Walk(list[i], clone, depth + 1, ref remaining, ref nodes, out item)) return false;
        if (clone) target.Add(item);
      }
      result = target;
      return true;
    }
    // Do not invoke arbitrary user ToString implementations from a telemetry hook.
    return false;
  }
}

internal sealed class BizzaAnalyticsBudget
{
  internal readonly BizzaState State;
  internal int RecordCount { get; private set; }
  internal int QueueBytes { get; private set; }
  internal long DroppedRecords { get; private set; }
  internal bool HasCapacity { get { return RecordCount < BizzaAnalyticsLimits.MaxRecords
    && QueueBytes < BizzaAnalyticsLimits.MaxQueueBytes; } }

  internal BizzaAnalyticsBudget(BizzaState state) { State = state; }
  internal void NoteDrop() { DroppedRecords++; }

  internal bool TryAdd(BizzaRecord record, int maxCount = BizzaAnalyticsLimits.MaxRecords,
    int maxBytes = BizzaAnalyticsLimits.MaxQueueBytes)
  {
    int bytes;
    if (record == null || RecordCount >= maxCount || QueueBytes >= maxBytes
      || !BizzaAnalyticsLimits.TryMeasure(record.ToStateObject(), BizzaAnalyticsLimits.MaxRecordBytes, out bytes)
      || bytes + BizzaAnalyticsLimits.BatchOverheadBytes > maxBytes - QueueBytes)
    {
      DroppedRecords++;
      return false;
    }
    record.StorageBytes = bytes + BizzaAnalyticsLimits.BatchOverheadBytes;
    RecordCount++;
    QueueBytes += record.StorageBytes;
    return true;
  }

  internal void Release(BizzaBatch batch)
  {
    foreach (var record in batch.records)
    {
      RecordCount--;
      QueueBytes -= record.StorageBytes;
    }
  }

  // Invoked on a worker before exposing the restored state to Unity. Reserve room
  // for the bounded pre-initialization queue, which can exist while the file loads.
  internal static BizzaAnalyticsBudget Restore(BizzaState state)
  {
    var budget = new BizzaAnalyticsBudget(state);
    if (state.uid.Length > 128) state.uid = "";
    if (state.playerId.Length > 128) state.playerId = "";
    var properties = new Dictionary<string, object>();
    foreach (var pair in state.userProps)
    {
      object copy;
      int bytes;
      if (properties.Count == BizzaAnalyticsLimits.MaxUserProperties) break;
      if (pair.Key.Length <= 128 && BizzaAnalyticsLimits.TryCopy(pair.Value,
          BizzaAnalyticsLimits.MaxPropertyBytes, out copy, out bytes)) properties[pair.Key] = copy;
    }
    state.userProps = properties;
    budget.TrimBatches(state.pendingBatches, true);
    budget.TrimBatches(state.pendingBatches, false);
    budget.TrimRecords(state.realtimeBuffer);
    budget.TrimRecords(state.userProfileBuffer);
    budget.TrimBatches(state.delayedQueue, true);
    budget.TrimBatches(state.delayedQueue, false);
    PruneHistory(state, DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    return budget;
  }

  private bool AddRestored(BizzaRecord record)
  {
    return TryAdd(record, BizzaAnalyticsLimits.MaxRecords - BizzaAnalyticsLimits.MaxPreInitRecords - BizzaAnalyticsLimits.MaxRecordsPerFrame,
      BizzaAnalyticsLimits.MaxQueueBytes - BizzaAnalyticsLimits.MaxPreInitBytes
        - BizzaAnalyticsLimits.MaxRecordsPerFrame * (BizzaAnalyticsLimits.MaxRecordBytes + BizzaAnalyticsLimits.BatchOverheadBytes));
  }

  private void TrimRecords(List<BizzaRecord> records)
  {
    var retained = 0;
    for (var i = 0; i < records.Count; i++)
      if (AddRestored(records[i])) records[retained++] = records[i];
    records.RemoveRange(retained, records.Count - retained);
  }

  private void TrimBatches(List<BizzaBatch> batches, bool allocated)
  {
    var retained = 0;
    for (var i = 0; i < batches.Count; i++)
    {
      var batch = batches[i];
      if ((batch.part >= 0) != allocated) { batches[retained++] = batch; continue; }
      if (batch.batchId.Length > 128 || batch.channel.Length > 32 || batch.dt.Length > 10
        || batch.originDt.Length > 10 || batch.earliestSendDt.Length > 10 || batch.nextRetryAt.Length > 64)
      { DroppedRecords += batch.records.Count; continue; }
      if (allocated)
      {
        var countBefore = RecordCount;
        var bytesBefore = QueueBytes;
        var accepted = true;
        foreach (var record in batch.records)
          if (!AddRestored(record)) { accepted = false; break; }
        if (!accepted)
        {
          RecordCount = countBefore;
          QueueBytes = bytesBefore;
          continue; // Never change the body of an already allocated/retried batch.
        }
      }
      else TrimRecords(batch.records);
      if (batch.records.Count > 0) batches[retained++] = batch;
    }
    batches.RemoveRange(retained, batches.Count - retained);
  }

  internal static void PruneHistory(BizzaState state, string today)
  {
    var cutoff = DateTime.ParseExact(today, "yyyy-MM-dd", CultureInfo.InvariantCulture).AddDays(-6).ToString("yyyy-MM-dd");
    Prune(state.dailyRecordCountByDt, cutoff, today);
    Prune(state.dailyActiveSecondsByDt, cutoff, today);
    Prune(state.dailyFirstPhaseFlushElapsedByDt, cutoff, today);
    Prune(state.dailyFallbackFlushElapsedByDt, cutoff, today);
    Prune(state.realtimeSeqByDt, cutoff, today);
    Prune(state.delayedSeqByDt, cutoff, today);
  }

  private static void Prune<T>(Dictionary<string, T> values, string cutoff, string today)
  {
    var remove = new List<string>();
    foreach (var key in values.Keys)
    {
      DateTime parsed;
      if (!DateTime.TryParseExact(key, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed)
        || string.CompareOrdinal(key, cutoff) < 0 || string.CompareOrdinal(key, today) > 0) remove.Add(key);
    }
    foreach (var key in remove) values.Remove(key);
  }
}
