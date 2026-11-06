using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bizza
{
    public static class DataTimeTool
    {
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
            return new DateTime(DateTimeZero.Ticks + stamp * (long)ELTime.MS);
        }
        public static string FormatTime(long stamp)
        {
            DateTimeZero.ToString();
            long minute = stamp / (long)ETime.Minute;
            long second = (stamp - (minute * (long)ETime.Minute)) / (long)ETime.Second;
            string s = second < 10 ? "0" + second : second.ToString();
            return string.Concat(minute.ToString(), ":", s);
        }
    }
}
