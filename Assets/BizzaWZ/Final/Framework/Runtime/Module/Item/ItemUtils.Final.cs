#if BIZZA_REAL_WITHDRAW
using System;
using UnityEngine;

public partial struct AddItemParam
{
    public bool isAd;
    public bool showCurrencyBar;
}

public partial class ItemUtils
{
    private static readonly string[] CountSuffixes = { "", "K", "M", "B", "T" };

    public static string GetFormatDollar(float dollar)
    {
        int suffixIndex = 0;
        double value = Math.Abs(dollar);

        while (value >= 10000 && suffixIndex < CountSuffixes.Length - 1)
        {
            value /= 1000;
            suffixIndex++;
        }

        if (suffixIndex == 0)
        {
            if (Math.Abs(value) < 1 && Math.Abs(value) > 0)
            {
                return value.ToString("0.##");
            }

            if (Math.Abs(value) < 10000)
            {
                if (Math.Abs(value - Math.Round(value)) < 0.001)
                {
                    return ((int)Math.Round(value)).ToString();
                }

                double roundedValue = Math.Round(value, 3, MidpointRounding.AwayFromZero);
                double abs = Math.Abs(roundedValue);

                if (abs >= 1000)
                {
                    return roundedValue.ToString("0");
                }

                if (abs >= 100)
                {
                    return roundedValue.ToString("0.#");
                }

                return abs >= 10 ? roundedValue.ToString("0.##") : roundedValue.ToString("0.###");
            }
        }

        string format;
        string result;
        if (value >= 100 || value <= -100)
        {
            format = "0";
            result = value.ToString(format) + CountSuffixes[suffixIndex];
        }
        else if (value >= 10 || value <= -10)
        {
            format = "0.#";
            result = value.ToString(format) + CountSuffixes[suffixIndex];
        }
        else
        {
            format = "0.##";
            result = value.ToString(format) + CountSuffixes[suffixIndex];
        }

        if (result.Length <= 5)
        {
            return result;
        }

        if (format == "0.##")
        {
            result = value.ToString("0.#") + CountSuffixes[suffixIndex];
        }
        else if (format == "0.#")
        {
            result = value.ToString("0") + CountSuffixes[suffixIndex];
        }

        return result.Length > 5 ? result.Substring(0, 5) : result;
    }

    public static void AddItem(ItemEntry item, bool playAnim, bool isAd, Vector3 startPos,
        bool showCurrencyBar = false, float rate = 1.0f)
    {
        AddItem(item.Type, item.Count * rate, playAnim, isAd, startPos, true, showCurrencyBar);
    }

    public static void AddItem(E_ItemType itemType, float amount, bool playAnim, bool isAd,
        Vector3 startPos, bool bUiPos = true, bool showCurrencyBar = false)
    {
        AddItem(new ItemEntry()
        {
            Type = itemType,
            Count = amount,
        }, new AddItemParam()
        {
            playAnim = playAnim,
            isAd = isAd,
            bUiPos = bUiPos,
            startPos = startPos,
            showCurrencyBar = showCurrencyBar,
        });
    }
}
#endif
