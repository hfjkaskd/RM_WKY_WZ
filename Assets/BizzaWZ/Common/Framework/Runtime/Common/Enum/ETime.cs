using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bizza
{
      [Obfuz.ObfuzIgnore]
    public enum ETime
    {
        Ms = 1,
        Second = 1000,
        Minute = 60 * Second,
        Hour = 60 * Minute,
        Day = 24 * Hour,
    }

      [Obfuz.ObfuzIgnore]
    public enum ELTime : long
    {
        NS = 1,
        MS = 10000 * NS,
        Second = 1000 * MS,
        Minute = 60 * Second,
        Hour = 60 * Minute,
        Day = 24 * Hour,
        Week = 7 * Day,
        Month = 30 * Day,
        Year = 365 * Day,
    }
    // public DateTime startTime = new DateTime();
}
