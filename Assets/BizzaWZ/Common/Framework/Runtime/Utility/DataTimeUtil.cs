using System;
using System.Collections;
using System.Collections.Generic;

[Obfuz.ObfuzIgnore]
public enum E_Time
{
    Ms = 1,
    Second = 1000,
    Minute = 60 * Second,
    Hour = 60 * Minute,
    Day = 24 * Hour,
}

[Obfuz.ObfuzIgnore]
public enum E_TimeL : long
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

    public static class DataTimeUtil
    {
        private const string m_d2 = "D2";
        public static DateTime DateTimeZero => new(1970, 1, 1, 0, 0, 0);
        public static DateTime Utc8Now => DateTime.UtcNow.AddHours(8);

        public static long GetTimeStamp(this DateTime dateTime)
        {
            TimeSpan st = dateTime - DateTimeZero;
            return Convert.ToInt64(st.TotalMilliseconds);
        }

        public static long GetTimeStamp()
        {
            TimeSpan st = DateTime.Now - DateTimeZero;
            return Convert.ToInt64(st.TotalMilliseconds);
        }

        public static DateTime ToDateTime(this long stamp)
        {
            return new DateTime(DateTimeZero.Ticks + stamp * (long)E_TimeL.MS);
        }

        public static string FormatTime(long stamp)
        {
            long minute = stamp / (long)E_Time.Minute;
            long second = (stamp - minute * (long)E_Time.Minute) / (long)E_Time.Second;
            return $"{minute:D2}:{second:D2}";
        }

        public static string FormatSecondTime(long secondTime)
        {
            long stamp = secondTime * (long)E_Time.Second;
            return FormatTime(stamp);
        }

        public static bool GetDifference(DateTime date, out string msg)
        {
            var diff = date - DataTimeUtil.Utc8Now;

            msg =
                $"{((int)diff.TotalHours).ToString(m_d2)}:{diff.Minutes.ToString(m_d2)}:{diff.Seconds.ToString(m_d2)}";

            return diff.TotalSeconds > 0;
        }
    }
