using System.Text;
using System;
using System.Globalization;

[Obfuz.ObfuzIgnore]
public enum ETimeUnit
{
    MILLISECONDS = 0,
    SECONDS,
    MINUTES,
    HOURS,
    DAYS,
}

public static class TimeUtil
{
    private static StringBuilder sb = new StringBuilder();

    private const string m_d1 = "D1";
    private const string m_d2 = "D2";
    private const string m_colon = ":";


    public static string SecondToString(long second)
    {
        sb.Clear();
        long m = second / 60;
        second = second - m * 60;
        long h = m / 60;
        m = m - h * 60;
        if (h > 0)
        {
            sb.Append(h.ToString(m_d2));
            sb.Append(m_colon);
        }

        sb.Append(m.ToString(m_d2));
        sb.Append(m_colon);
        sb.Append(second.ToString(m_d2));
        return sb.ToString();
    }

    //计算当前时间距离某日的某一时刻还有多少小时
    public static int GetHourToTargetTime(int targetHour)
    {
        int hour = DateTime.Now.Hour;
        int hourToTarget = targetHour - hour;
        if (hourToTarget < 0)
        {
            hourToTarget += 24;
        }

        return hourToTarget;
    }

    //计算当前时间距离某日的某一时刻还有多少秒
    public static long GetSecondsToTargetTime(int targetHour)
    {
        DateTime now = DateTime.Now;
        DateTime targetTime = new DateTime(now.Year, now.Month, now.Day, targetHour, 0, 0);
        if (now.Hour >= targetHour)
        {
            targetTime = targetTime.AddDays(1);
        }
        TimeSpan timeSpan = targetTime - now;
        return (long)timeSpan.TotalSeconds;
    }


    //计算当前时刻距离某周的某一时刻还有多少秒
    public static long GetSecondsToTargetTime(int targetHour, int dayInWeek)
    {
        DateTime now = DateTime.Now;
        int dayOfWeek = (int)now.DayOfWeek;

        DateTime targetTime = new DateTime(now.Year, now.Month, now.Day, targetHour, 0, 0);
        if (dayOfWeek >= dayInWeek)
        {
            targetTime = targetTime.AddDays(7);
        }
        TimeSpan timeSpan = targetTime - now;
        return (long)timeSpan.TotalSeconds;
    }

    /// <summary>
    /// 当前时间是否是新的一天的X时分后。    
    /// 用于判定系统刷新时间
    /// </summary>
    /// <param name="hour"></param>
    /// <returns></returns>
    public static bool IsNewDayAndHour(int resetDayHour, long timeStamp_Day)
    {
        long curDay = TimeUtil.GetCurrentTime(ETimeUnit.DAYS);
        long curHour = TimeUtil.GetCurrentTime(ETimeUnit.HOURS);

        if (curDay > timeStamp_Day && curHour >= resetDayHour)
        {
            return true;
        }
        return false;
    }

    // utc start time
    public static readonly DateTime UTC_START_TIME = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /**
     * 根据UTC时间戳的含义= UTC时间 - UTC起始时间
     */
    public static long GetCurrentTime(ETimeUnit timeUnit)
    {
        TimeSpan timeSpan = DateTime.UtcNow - UTC_START_TIME;
        long result = -1L;
        switch (timeUnit)
        {
            case ETimeUnit.DAYS:
                result = (long)timeSpan.TotalDays;
                break;
            case ETimeUnit.HOURS:
                result = (long)timeSpan.TotalHours;
                break;
            case ETimeUnit.MINUTES:
                result = (long)timeSpan.TotalMinutes;
                break;
            case ETimeUnit.SECONDS:
                result = (long)timeSpan.TotalSeconds;
                break;
            case ETimeUnit.MILLISECONDS:
                result = (long)timeSpan.TotalMilliseconds;
                break;
        }
        return result;
    }

    /**
     * 先获取 对应的 UTC -> 转为 当前时区的时间
     */
    public static string GetFormatTime(string format, long time, ETimeUnit timeUnit)
    {
        DateTime end = DateTime.MinValue;
        DateTime start = UTC_START_TIME;
        switch (timeUnit)
        {
            case ETimeUnit.DAYS:
                end = start.AddDays(time);
                break;
            case ETimeUnit.HOURS:
                end = start.AddHours(time);
                break;
            case ETimeUnit.MINUTES:
                end = start.AddMinutes(time);
                break;
            case ETimeUnit.SECONDS:
                end = start.AddSeconds(time);
                break;
            case ETimeUnit.MILLISECONDS:
                end = start.AddMilliseconds(time);
                break;
        }
        end = TimeZone.CurrentTimeZone.ToLocalTime(end);
        return end.ToString(format);
    }

    //获取当前是今年的第几周
    public static int GetTheWeekNum()
    {
        // 获取当年1月1日的 时间
        DateTime dateTime = new DateTime(DateTime.Now.Year, 1, 1);

        //获取当前时间 与第一天的 天数
        int dayCount = (int)(DateTime.Now - dateTime).TotalDays;

        //目标日期距离该年第一周第一天的天数（sunday为0，monday为1）
        dayCount += Convert.ToInt32(dateTime.DayOfWeek);

        //获取大于或等于最小整数
        return (int)MathF.Ceiling(dayCount / 7.0f);
    }

    /// <summary>
    /// 本地时间是否晚于服务器时间
    /// </summary>
    /// <param name="localTime">本地时间字符串，例如：2026-04-09 11:07:53</param>
    /// <param name="serverTime">服务器时间字符串，例如：2026-04-09 11:07:53</param>
    /// <returns>如果本地时间大于服务器时间，则返回 true；否则返回 false</returns>
    public static bool IsLocalTimeLaterThanServerTime(string localTime, string serverTime)
    {
        if (string.IsNullOrWhiteSpace(localTime) || string.IsNullOrWhiteSpace(serverTime))
        {
            return false;
        }

        const string format = "yyyy-MM-dd HH:mm:ss";

        bool localOk = DateTime.TryParseExact(
            localTime,
            format,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out DateTime localDateTime);

        bool serverOk = DateTime.TryParseExact(
            serverTime,
            format,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out DateTime serverDateTime);

        if (!localOk || !serverOk)
        {
            return false;
        }

        return localDateTime > serverDateTime;
    }

    /// <summary>
    /// 本地时间是否晚于服务器时间
    /// </summary>
    /// <param name="localTime">本地时间字符串，例如：2026-04-09 11:07:53</param>
    /// <param name="serverTimestamp">服务器毫秒时间戳，例如：1775732873984</param>
    /// <returns>如果本地时间大于服务器时间，则返回 true；否则返回 false</returns>
    public static bool IsLocalTimeLaterThanServerTime(string localTime, long serverTimestamp)
    {
        if (string.IsNullOrWhiteSpace(localTime))
        {
            return false;
        }

        const string format = "yyyy-MM-dd HH:mm:ss";

        bool localOk = DateTime.TryParseExact(
            localTime,
            format,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out DateTime localDateTime);

        if (!localOk)
        {
            return false;
        }

        DateTime serverDateTime = DateTimeOffset
            .FromUnixTimeMilliseconds(serverTimestamp)
            .LocalDateTime;

        return localDateTime > serverDateTime;
    }

    /// <summary>
    /// 将服务器毫秒时间戳转换为时间字符串
    /// </summary>
    /// <param name="serverTimestamp">服务器毫秒时间戳，例如：1775732873984</param>
    /// <returns>格式化后的时间字符串，例如：2026-04-09 11:07:53</returns>
    public static string ConvertServerTimestampToTimeString(long serverTimestamp)
    {
        DateTime localDateTime = DateTimeOffset
            .FromUnixTimeMilliseconds(serverTimestamp)
            .LocalDateTime;

        return localDateTime.ToString("yyyy-MM-dd HH:mm:ss");
    }

    /// <summary>
    /// 将字符串安全转换为长整型数字
    /// </summary>
    /// <param name="inputString">需要转换的字符串，例如："1234567890"</param>
    /// <returns>转换后的长整型数字，如果转换失败则返回 0L</returns>
    public static long ConvertStringToLong(string inputString)
{
    // 1. 定义时间格式，严格匹配 "yyyy-MM-dd HH:mm:ss"
    string format = "yyyy-MM-dd HH:mm:ss";

    // 2. 尝试将字符串解析为 DateTime 对象
    if (DateTime.TryParseExact(inputString, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedTime))
    {
        // 3. 解析成功，将 DateTime 转换为 Unix 时间戳（毫秒级 long）
        // 这里假设传入的时间是本地时间。如果是 UTC 时间，请使用 ToUniversalTime()
        DateTimeOffset dto = new DateTimeOffset(parsedTime);
        return dto.ToUnixTimeMilliseconds();
    }

    // 4. 解析失败（格式不对或为空），返回 0
    // 注意：如果返回 0，所有解析失败的时间在排序时都会被排在一起（视为相同）
    return 0L;
}
}
