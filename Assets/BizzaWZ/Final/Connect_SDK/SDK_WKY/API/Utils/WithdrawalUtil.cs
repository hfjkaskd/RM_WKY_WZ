#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections.Generic;
using UnityEngine;
using Bizza.Sdk;
using System.Globalization;

[Obfuz.ObfuzIgnore]
public static class WithdrawalUtil
{
    #if !COMMONGAME
    public static float GetDollarCountByReward()
    {
        float reward = 0;
        decimal current = 0;
#if BIZZA_REAL_WITHDRAW
        if (ChannelConfig.Instance.real_CustomConfig.singleCurrencyMode)
        {
            return 0;
        }
        AccountModule.E_CountryType countryType = AccountModule.CountryType;

        if (countryType == AccountModule.E_CountryType.None)
        {
            LogLogger.LogVerbose(BaseConst.LOG_Game, "玩家的国家数值为 null");
            return 0;
        }

        current = (decimal)ItemUtils.GetItemCount(E_ItemType.Dollar);

        
        reward = countryType switch
        {
            AccountModule.E_CountryType.US => (float)RewardRandomizerForUs.GetRandomAmount(current, E_RewardType.Reward),
            AccountModule.E_CountryType.ID => (float)RewardRandomizerForIndonesia.GetRandomAmount(current, E_RewardType.Reward),
            AccountModule.E_CountryType.BR => (float)RewardRandomizerForUs.GetRandomAmount(current, E_RewardType.Reward),
            _ => 0
        };
#endif
        Debug.Log($"current={current}, reward={reward}");
        return (float)reward;
    }

    public static float GetDollarCountBtFree()
    {
        float reward = 0;
        decimal current = 0;
#if BIZZA_REAL_WITHDRAW
        if (ChannelConfig.Instance.real_CustomConfig.singleCurrencyMode)
        {
            return 0;
        }
        AccountModule.E_CountryType countryType = AccountModule.CountryType;
        current = (decimal)ItemUtils.GetItemCount(E_ItemType.Dollar);
        reward = 0;
        reward = countryType switch
        {
            AccountModule.E_CountryType.US => (float)RewardRandomizerForUs.GetRandomAmount(current, E_RewardType.FreeReward),
            AccountModule.E_CountryType.ID => (float)RewardRandomizerForIndonesia.GetRandomAmount(current, E_RewardType.FreeReward),
            AccountModule.E_CountryType.BR => (float)RewardRandomizerForUs.GetRandomAmount(current, E_RewardType.FreeReward),
            _ => 0
        };
#endif
        Debug.Log($"current={current}, free={reward}");
        return (float)reward;
    }


    public static float GetNewbieGift()
    {
        float count = 0;
#if BIZZA_REAL_WITHDRAW
        if (ChannelConfig.Instance.real_CustomConfig.singleCurrencyMode)
        {
            count = AccountModule.CountryType switch
            {
                AccountModule.E_CountryType.US => 40f,
                AccountModule.E_CountryType.BR => 0.4f,
                AccountModule.E_CountryType.ID => 800f,
                _ => 0f
            };
        }
        else
        {
            count = AccountModule.CountryType switch
            {
                AccountModule.E_CountryType.US => 77f,
                AccountModule.E_CountryType.BR => 51f,
                AccountModule.E_CountryType.ID => 25000f,
                _ => 0f
            };
        }
#endif
        return count;
    }
#endif

    private static CultureInfo culture = new CultureInfo("de-DE");
    public static string GetCustomizedValueByCountryType(float _value) // 根据国家先处理数值，再按对应国家格式转成字符串。
    {
        //LogLogger.LOGNumericalShow($"数值小数点优化_字符串： 原始值_{value}");
        string current = "";
        #if BIZZA_REAL_WITHDRAW
        float value = GetCustomizedFloatByCountryType(_value);
        switch (AccountModule.CountryType)
        {
            case AccountModule.E_CountryType.BR:
                current = value.ToString("F2", culture);
                break;
            case AccountModule.E_CountryType.US:
                current = value.ToString("F2");
                break;
            case AccountModule.E_CountryType.ID:
                current = Math.Truncate(value).ToString();
                break;
        }
        #endif
        return current;
    }

    public static string GetCustomizedIntByCountryType2(float value) // 把传入值截断到小数点后两位，再用 culture 格式转成字符串。
    {
        decimal decValue = (decimal)value;
        decimal truncated = Math.Truncate(decValue * 100) / 100;
        return truncated.ToString(culture);
        return GetCustomizedValueByCountryType(value);
    }

    public static float GetCustomizedFloatByCountryType(float value) // 根据国家返回处理后的数值：BR/US 保留两位向下取整，ID 只保留整数。
    {
        float result = value;
        #if BIZZA_REAL_WITHDRAW
        switch (AccountModule.CountryType)
        {
            case AccountModule.E_CountryType.BR:
            case AccountModule.E_CountryType.US:
                result = Mathf.Floor(value * 100f) / 100f; // 向下取整
                break;
            case AccountModule.E_CountryType.ID:
                result = (float)Math.Truncate(value);
                break;
        }
        #endif
        return result;
    }

    public static float GetFreeDollarRewardAmount(float value)
    {
        float result = value;
        value = value * ChannelConfig.Instance.GetFreeDollarRewardRate;
        #if BIZZA_REAL_WITHDRAW
        switch (AccountModule.CountryType)
        {
            case AccountModule.E_CountryType.BR:
            case AccountModule.E_CountryType.US:
                result = Math.Max(0.01f, value);
                break;
            case AccountModule.E_CountryType.ID:
                value = (float)Math.Truncate(value);
                result = Math.Max(1, value);
                break;
        }
        #endif
        return result;
    }
}
[Obfuz.ObfuzIgnore]
public enum E_RewardType
{
    Reward,      // 奖励金额（奖励金额上限/下限）
    FreeReward   // 免费奖励（免费奖励上限/下限）
}

[Obfuz.ObfuzIgnore]
public static class RewardRandomizerForUs
{
    [Serializable]
    private struct RewardRow
    {
        public decimal currentMin;   // 当前金额下限
        public decimal currentMax;   // 当前金额上限（不含）
        public decimal rewardMax;    // 奖励金额上限
        public decimal rewardMin;    // 奖励金额下限
        public decimal freeMax;      // 免费奖励上限
        public decimal freeMin;      // 免费奖励下限

        public RewardRow(decimal cMin, decimal cMax,
            decimal rMax, decimal rMin,
            decimal fMax, decimal fMin)
        {
            currentMin = cMin; currentMax = cMax;
            rewardMax = rMax; rewardMin = rMin;
            freeMax = fMax; freeMin = fMin;
        }

        public bool Match(decimal currentAmount)
        {
            // 规则：下限 <= 金额 < 上限
            return currentAmount >= currentMin && currentAmount < currentMax;
        }

        public (decimal min, decimal max) GetRange(E_RewardType type)
        {
            decimal a, b;
            if (type == E_RewardType.Reward)
            {
                a = rewardMin; b = rewardMax;
            }
            else
            {
                a = freeMin; b = freeMax;
            }

            // 防止写反：确保 min <= max
            var min = Math.Min(a, b);
            var max = Math.Max(a, b);
            return (min, max);
        }
    }

    // 这里先把表写死
    private static readonly List<RewardRow> Rows = new List<RewardRow>
    {
        new RewardRow(0.00m,   365.00m, 13.00m, 9.00m,  1.30m, 0.90m),
        new RewardRow(365.00m, 450.00m, 12.00m, 8.00m,  1.20m, 0.80m),
        new RewardRow(450.00m, 545.00m, 10.00m, 7.00m,  1.00m, 0.70m),
        new RewardRow(545.00m, 590.00m, 7.00m,  5.00m,  0.70m, 0.50m),
        new RewardRow(590.00m, 636.00m, 6.00m,  4.00m,  0.60m, 0.40m),
        new RewardRow(636.00m, 681.00m, 4.00m,  3.00m,  0.40m, 0.30m),
        new RewardRow(681.00m, 727.00m, 3.00m,  2.00m,  0.30m, 0.20m),
        new RewardRow(727.00m, 736.00m, 2.00m,  1.00m,  0.20m, 0.10m),
        new RewardRow(736.00m, 745.00m, 1.00m,  0.70m,  0.10m, 0.07m),
        new RewardRow(745.00m, 754.00m, 0.70m,  0.55m,  0.07m, 0.05m),
        new RewardRow(754.00m, 763.00m, 0.55m,  0.36m,  0.05m, 0.03m),
        new RewardRow(763.00m, 772.00m, 0.36m,  0.18m,  0.03m, 0.02m),
        new RewardRow(772.00m, 781.00m, 0.18m,  0.09m,  0.01m, 0.01m),
        new RewardRow(781.00m, 790.00m, 0.09m,  0.05m,  0.01m, 0.01m),
        new RewardRow(790.00m, 795.00m, 0.05m,  0.04m,  0.01m, 0.01m),
        new RewardRow(795.00m, 796.00m, 0.03m,  0.03m,  0.01m, 0.01m),
        new RewardRow(796.00m, 797.00m, 0.03m,  0.02m,  0.01m, 0.01m),
        new RewardRow(797.00m, 798.00m, 0.02m,  0.02m,  0.01m, 0.01m),
        new RewardRow(798.00m, 799.00m, 0.02m,  0.01m,  0.01m, 0.01m),
        new RewardRow(799.00m, 800.00m, 0.01m,  0.01m,  0.01m, 0.01m),
        new RewardRow(800.00m, 90909090909.08m, 13.00m, 9.00m, 1.30m, 0.90m),
    };

    /// <summary>
    /// 获取随机金额（默认四舍五入到2位小数）
    /// </summary>
    public static decimal GetRandomAmount(decimal currentAmount, E_RewardType type, int decimals = 2)
    {
        var row = FindRow(currentAmount);
        if (row == null)
            throw new ArgumentOutOfRangeException(nameof(currentAmount), $"当前金额 {currentAmount} 不在任何区间内。");

        var (min, max) = row.Value.GetRange(type);

        // min==max 直接返回
        if (min == max) return DecimalRound(min, decimals);

        // UnityEngine.Random.value: [0,1)
        decimal t = (decimal)UnityEngine.Random.value;
        decimal value = min + (max - min) * t;

        return DecimalRound(value, decimals);
    }

    private static RewardRow? FindRow(decimal currentAmount)
    {
        for (int i = 0; i < Rows.Count; i++)
        {
            if (Rows[i].Match(currentAmount))
                return Rows[i];
        }
        return null;
    }

    private static decimal DecimalRound(decimal v, int decimals)
        => Math.Round(v, decimals, MidpointRounding.AwayFromZero);
}

[Obfuz.ObfuzIgnore]
public static class RewardRandomizerForIndonesia
{
    private struct RewardRow
    {
        public decimal currentMin;
        public decimal currentMax;
        public decimal rewardMax;
        public decimal rewardMin;
        public decimal freeMax;
        public decimal freeMin;

        public RewardRow(
            decimal cMin, decimal cMax,
            decimal rMax, decimal rMin,
            decimal fMax, decimal fMin)
        {
            currentMin = cMin;
            currentMax = cMax;
            rewardMax = rMax;
            rewardMin = rMin;
            freeMax = fMax;
            freeMin = fMin;
        }

        public bool Match(decimal current)
        {
            // 统一规则：下限 <= current < 上限
            return current >= currentMin && current < currentMax;
        }

        public (decimal min, decimal max) GetRange(E_RewardType type)
        {
            decimal a, b;
            if (type == E_RewardType.Reward)
            {
                a = rewardMin;
                b = rewardMax;
            }
            else
            {
                a = freeMin;
                b = freeMax;
            }

            return (Math.Min(a, b), Math.Max(a, b));
        }
    }

    // ================= 写死的印度尼西亚数据 =================
    private static readonly List<RewardRow> Rows = new()
    {
        new RewardRow(0m,     40000m, 1500m, 1000m, 150m, 100m),
        new RewardRow(40000m, 50000m, 1400m, 900m,  140m, 90m),
        new RewardRow(50000m, 60000m, 1200m, 800m,  120m, 80m),
        new RewardRow(60000m, 65000m, 800m,  600m,  80m,  60m),
        new RewardRow(65000m, 70000m, 700m,  500m,  70m,  50m),
        new RewardRow(70000m, 75000m, 500m,  400m,  50m,  40m),
        new RewardRow(75000m, 80000m, 350m,  250m,  35m,  25m),
        new RewardRow(80000m, 81000m, 200m,  100m,  20m,  10m),
        new RewardRow(81000m, 82000m, 120m,  80m,   12m,  8m),
        new RewardRow(82000m, 83000m, 80m,   60m,   8m,   6m),
        new RewardRow(83000m, 84000m, 60m,   40m,   6m,   4m),
        new RewardRow(84000m, 85000m, 40m,   20m,   4m,   2m),
        new RewardRow(85000m, 86000m, 20m,   10m,   2m,   2m),
        new RewardRow(86000m, 87000m, 10m,   5m,    2m,   1m),
        new RewardRow(87000m, 87500m, 5m,    4m,    1m,   1m),
        new RewardRow(87500m, 87600m, 3m,    3m,    1m,   1m),
        new RewardRow(87600m, 87700m, 3m,    2m,    1m,   1m),
        new RewardRow(87700m, 87800m, 2m,    2m,    1m,   1m),
        new RewardRow(87800m, 87900m, 2m,    1m,    1m,   1m),
        new RewardRow(87900m, 88000m, 1m,    1m,    1m,   1m),
        new RewardRow(88000m, 9999999999999m, 1500m, 1000m, 150m, 100m),
    };

    /// <summary>
    /// 获取随机奖励金额
    /// </summary>
    public static decimal GetRandomAmount(decimal currentAmount, E_RewardType rewardType, int decimals = 2)
    {
        var row = FindRow(currentAmount);
        if (row == null)
        {
            Debug.LogError($"[Indonesia] 当前金额 {currentAmount} 未匹配任何区间");
            return 0;
        }

        var (min, max) = row.Value.GetRange(rewardType);

        if (min == max)
            return Round(min, decimals);

        decimal t = (decimal)UnityEngine.Random.value;
        decimal value = min + (max - min) * t;

        return Round(value, decimals);
    }

    private static RewardRow? FindRow(decimal current)
    {
        foreach (var row in Rows)
        {
            if (row.Match(current))
                return row;
        }
        return null;
    }

    private static decimal Round(decimal v, int decimals)
    {
        return Math.Round(v, decimals, MidpointRounding.AwayFromZero);
    }
}

#endif
