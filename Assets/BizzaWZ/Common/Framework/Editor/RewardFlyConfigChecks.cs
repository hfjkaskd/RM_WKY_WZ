#if BIZZA_REAL_WITHDRAW
using System;
using System.IO;
using System.Reflection;
using Bizza.FlyMoney;
using UnityEditor;
using UnityEngine;

public static class RewardFlyConfigChecks
{
    [MenuItem("Tools/Fly Money Lab/Check Country Trail Settings")]
    public static void Run()
    {
        int assertions = 0;
        var config = ScriptableObject.CreateInstance<RewardItemCollectFxConfig>();
        var restored = ScriptableObject.CreateInstance<RewardItemCollectFxConfig>();
        void Require(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new InvalidOperationException(message);
        }
        bool Near(Color a, Color b) => Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) +
            Mathf.Abs(a.b - b.b) + Mathf.Abs(a.a - b.a) < 0.0001f;
        try
        {
            config.autoLowNativeQuality = false;
            Require(config.MaxConcurrentFx == 4 && config.MaxStartsPerSecond == 4, "Standard profile accepts four starts");
            Require(config.MaxConcurrentFreeFx == 16, "Free channel accepts sixteen concurrent animations");
            config.maxConcurrentFreeFx = 1;
            Require(config.MaxConcurrentFx == 4 && config.MaxStartsPerSecond == 4, "Free limits do not change ad limits");
            config.maxConcurrentFreeFx = 500;
            Require(config.MaxConcurrentFreeFx == RewardItemCollectFlow.FreeConcurrencyLimit, "Free channel remains bounded");
            config.maxConcurrentFreeFx = RewardItemCollectFlow.FreeConcurrencyLimit;
            config.forceLowNativeQuality = true;
            Require(config.MaxConcurrentFx == 4, "Low profile retains four concurrent slots");
            config.maxConcurrentFx = 500;
            Require(config.MaxConcurrentFx == FlyMoneyPlayer.HardConcurrencyLimit, "Concurrency stays bounded");
            config.maxConcurrentFx = 4;
            config.forceLowNativeQuality = false;
            var countries = new[] { AccountModule.E_CountryType.US, AccountModule.E_CountryType.ID, AccountModule.E_CountryType.BR };
            var colors = new[] { new Color(0.35f, 0.85f, 0.38f, 0.42f), new Color(0.2f, 0.7f, 1f, 0.42f), new Color(0.94f, 0.22f, 0.69f, 0.42f) };
            for (int i = 0; i < countries.Length; i++)
            {
                Require(Near(config.GetNativeSettings(countries[i], true).trailColor, colors[i]), "Default cash palette " + countries[i]);
                Require(Near(config.GetNativeSettings(countries[i], false).trailColor, new Color(1f, 0.73f, 0.16f, 0.42f)), "Default coin palette " + countries[i]);
            }
            config.trailsUS.cashStyle = RewardTrailStyle.IndonesiaBlue;
            Require(Near(config.GetNativeSettings(countries[0], true).trailColor, colors[1]), "Country can select another palette");
            Require(Near(config.GetNativeSettings(countries[2], true).trailColor, colors[2]), "Other country is unchanged");
            config.trailsUS.coinStyle = RewardTrailStyle.Custom;
            config.trailsUS.customCoinColor = new Color(0.8f, 0.4f, 0.1f, 0.05f);
            Require(Near(config.GetNativeSettings(countries[0], false).trailColor, new Color(0.8f, 0.4f, 0.1f, 0.42f)), "Custom coin RGB preserves profile alpha");
            config.trailsUS.cashStyle = RewardTrailStyle.Off;
            Require(!config.GetNativeSettings(countries[0], true).trails && config.GetNativeSettings(countries[0], false).trails, "Per-item disable");
            config.trailsUS.cashStyle = RewardTrailStyle.Common;
            config.nativeSettings.trailColor = new Color(0.3f, 0.4f, 0.5f, 0.2f);
            Require(Near(config.GetNativeSettings(countries[0], true).trailColor, config.nativeSettings.trailColor), "Common palette is not overwritten");
            config.useCountryTrails = false;
            foreach (var country in countries)
                Require(Near(config.GetNativeSettings(country, true).trailColor, config.nativeSettings.trailColor), "Global common color " + country);
            config.useCountryTrails = true;
            config.forceLowNativeQuality = true;
            var low = config.GetNativeSettings(countries[1], true);
            Require(low.flyCount == config.nativeLowSettings.flyCount && low.trailSegments == config.nativeLowSettings.trailSegments &&
                low.trailWidth == config.nativeLowSettings.trailWidth && low.trailTime == config.nativeLowSettings.trailTime, "Low profile geometry retained");
            Require(Mathf.Approximately(low.trailColor.a, config.nativeLowSettings.trailColor.a), "Low profile transparency retained");
            config.nativeLowSettings.trails = false;
            Require(!config.GetNativeSettings(countries[1], true).trails, "Country palette cannot re-enable globally disabled trails");
            Require(Near(config.GetNativeSettings((AccountModule.E_CountryType)999, true).trailColor,
                config.GetNativeSettings(countries[2], true).trailColor), "Unknown country follows sprite fallback BR");
            config.forceLowNativeQuality = false;
            config.trailsUS.cashStyle = RewardTrailStyle.Custom;
            config.trailsUS.customCashColor = new Color(float.NaN, float.PositiveInfinity, -2f, 1f);
            var sanitized = config.GetNativeSettings(countries[0], true).Sanitized();
            Require(!float.IsNaN(sanitized.trailColor.r) && !float.IsInfinity(sanitized.trailColor.g) && sanitized.trailColor.b == 0f, "Invalid custom colors are sanitized by player settings");
            config.trailsUS.customCashColor = Color.cyan;
            config.nativeSettings.flyCount = 11;
            config.nativeLowSettings.flyCount = 7;
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(config), restored);
            Require(restored.nativeSettings.flyCount == 11 && restored.nativeLowSettings.flyCount == 7, "Existing quantity fields survive serialization");
            Require(restored.trailsUS.cashStyle == RewardTrailStyle.Custom && restored.trailsUS.customCashColor == Color.cyan,
                "Country selection survives serialization");
            var before = config.nativeSettings;
            config.GetNativeSettings(countries[2], true);
            Require(Near(before.trailColor, config.nativeSettings.trailColor), "Per-play selection does not mutate shared settings");

            Type drawerType = Type.GetType("Bizza.FlyMoney.Editor.FlyMoneySettingsDrawer, Bizza.FlyMoney.Editor", true);
            var fields = (string[])drawerType.GetField("Fields", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            var labels = (GUIContent[])drawerType.GetField("Labels", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            var serialized = new SerializedObject(config);
            var property = serialized.FindProperty("nativeSettings");
            int fieldCount = 0;
            foreach (FieldInfo field in typeof(FlyMoneySettings).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                fieldCount++;
                Require(Array.IndexOf(fields, field.Name) >= 0, "Localized drawer covers " + field.Name);
            }
            Require(fields.Length == labels.Length && fields.Length == fieldCount, "Drawer has no missing or extra settings");
            for (int i = 0; i < fields.Length; i++)
                Require(property.FindPropertyRelative(fields[i]) != null && labels[i].text[0] > 127, "Chinese label bound to serialized field " + fields[i]);
            var drawer = (PropertyDrawer)Activator.CreateInstance(drawerType);
            property.isExpanded = false;
            float collapsedHeight = drawer.GetPropertyHeight(property, GUIContent.none);
            property.isExpanded = true;
            Require(drawer.GetPropertyHeight(property, GUIContent.none) > collapsedHeight * fields.Length, "Expanded drawer reserves all rows");

            const string path = "Library/RewardFlyValidation/Reports/country-trails.json";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, "{\"passed\":true,\"assertions\":" + assertions + "}");
            Debug.Log("Country trail settings checks passed: " + assertions);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(config);
            UnityEngine.Object.DestroyImmediate(restored);
        }
    }
}
#endif
