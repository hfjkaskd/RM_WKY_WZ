"""Run the production icon and language logic with isolated local fixtures."""

import argparse
from pathlib import Path
import subprocess


ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / "Assets/BizzaWZ/Common/Framework/Runtime"


def load_production():
    ui = (ASSETS / "Utility/UIUtils.cs").read_text(encoding="utf-8-sig")
    fields = ui[ui.index("    private static readonly Dictionary<Image, int> WzSpriteRequests"):
                ui.index("    private const int MaxNetworkErrorTips")]
    methods = ui[ui.index("    public static void SetWzSprite("):
                 ui.index("    public static Rect GetAABBWorldRect(")]
    tips = ui[ui.index("    private const int MaxNetworkErrorTips"):
              ui.index("    public static void ShowTips(")]
    language = (ASSETS / "Module/Localization/LanguageUtils.cs").read_text(encoding="utf-8-sig")
    language = language[language.index("public class LanguageUtils"):]
    reward_dir = ROOT / "Assets/BizzaWZ/Final/Real/UI/GetRewardPanel"
    progress = (reward_dir / "WathAdProgress.cs").read_text(encoding="utf-8-sig")
    # Keep the actual lifecycle and request branches. Skip the unrelated JSON rendering code.
    progress = progress[progress.index("    private AccountModule configRequestAccount;"):
                        progress.index("        var dict = JsonConvert")]
    progress += "        RenderCount++;\n    }"
    reward = (reward_dir / "GetRewardPanel.cs").read_text(encoding="utf-8-sig")
    ratio = reward[reward.index("    private void RefreshBonusRate()"):
                   reward.index("    private void CustomWinClose()")]
    slots = reward_dir.parent / "SlotsPanel"
    entry = (slots / "SlotEnter/SlotEnter.cs").read_text(encoding="utf-8-sig")
    entry = entry[entry.index("    public void OnClick()"):entry.rindex("\n}")]
    spin = (slots / "SlotPanel/SlotPanel.cs").read_text(encoding="utf-8-sig")
    spin = spin[spin.index("    public void PlaySlotBtn()"):spin.index('    [Button("Test Slot Btn")]')]
    currency = (slots / "SlotPanel/CurrencyInfo.cs").read_text(encoding="utf-8-sig")
    currency = currency[currency.index("public class CurrencyInfo"):currency.rindex("#endif")]
    return dict(ICONS="\n".join((fields, methods, "#if BIZZA_REAL_WITHDRAW", tips)), LANGUAGE=language,
                PROGRESS=progress, RATIO=ratio, SLOT_ENTRY=entry, SLOT_SPIN=spin, SLOT_CURRENCY=currency)


FIXTURE = r"""
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using UniTask = System.Threading.Tasks.Task;

enum RuntimeInitializeLoadType { BeforeSceneLoad, SubsystemRegistration }
class HeaderAttribute : Attribute { public HeaderAttribute(string text) { } }
class SerializeField : Attribute { }
public class MonoBehaviour { }
public class TMP_Text { public string text; }
public enum E_ItemType { Gold, Dollar }
enum Ease { OutCubic }
class Tween
{
    public void Kill() { }
    public Tween SetEase(Ease ease) => this;
}
static class DOVirtual
{
    public static Tween Float(float from, float to, float duration, Action<float> update)
    {
        update(to);
        return new Tween();
    }
}
class RuntimeInitializeOnLoadMethodAttribute : Attribute
{
    public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType loadType) { }
}
enum SystemLanguage
{
    English, Portuguese, Indonesian, Chinese, ChineseSimplified, ChineseTraditional,
    Russian, Japanese, Korean, Spanish, Turkish, Vietnamese, Arabic, German, French,
}
static class Application { public static SystemLanguage systemLanguage; }
static class PlayerPrefs
{
    private static readonly Dictionary<string, string> Values = new();
    public static bool HasKey(string key) => Values.ContainsKey(key);
    public static string GetString(string key, string fallback) => Values.GetValueOrDefault(key, fallback);
    public static void SetString(string key, string value) => Values[key] = value;
    public static void Save() { }
    public static void Clear() => Values.Clear();
}
static class BizzaEventSystem
{
    private static Action countryChanged;
    public static void On(int value, Action listener) => countryChanged += listener;
    public static void Off(int value, Action listener) => countryChanged -= listener;
    public static void Emit(int value)
    {
        if (value == EventDefine.Login.InitContentByCountry) countryChanged?.Invoke();
    }
    public static int CountryListeners => countryChanged?.GetInvocationList().Length ?? 0;
}
static class EventDefine
{
    public static class Frame { public const int LanguageChange = 0; }
    public static class Login { public const int InitContentByCountry = 1; }
    public static class CustomGameEvent { public const int SlotProgressChanged = 2; }
}
class Row { public Dictionary<string, string> Dict = new(); }
class LanguageTable
{
    public Dictionary<string, Row> DataMap = new();
    public Row GetOrDefault(string key) => DataMap.GetValueOrDefault(key);
}
class Tables { public LanguageTable TblLanguage = new(); }
static class TableUtils { public static Tables Tables = new(); }
class Sprite { }
class Image
{
    public Sprite sprite;
    public bool enabled = true;
    public int NativeSizeUpdates;
    public void SetNativeSize() => NativeSizeUpdates++;
}
class AccountModule
{
    public enum E_CountryType { None, US, BR, ID, JP, KR, SA, DE }
    public static E_CountryType CountryType;
    public static AccountModule Instance;
    public bool AccountReady, IsSuspended;
    public string OceanShineAppOtherConfigResponse;
    public class UserInfo
    {
        public double Balance;
        public double GetBalance() => Balance;
    }
    public UserInfo Os_Current_Uso;
    public Action<bool, string> CompleteRequest;
    public int Requests, RatioReads;
    public void Request_AppOtherConfigRequest(Action<bool, string> callback, bool black)
    {
        if (!AccountReady) throw new Exception("config requested offline");
        Requests++;
        CompleteRequest = (success, response) =>
        {
            OceanShineAppOtherConfigResponse = response;
            callback(success, response);
        };
    }
    public int GetMaxDrawithRatio(bool isCash)
    {
        if (!AccountReady) throw new Exception("ratio read offline");
        RatioReads++;
        return 5;
    }
}
static class AssetUtils
{
    public static readonly List<string> Paths = new();
    public static readonly List<TaskCompletionSource<Sprite>> Replies = new();
    public static Task<Sprite> LoadAssetAsync<T>(string path)
    {
        Paths.Add(path);
        var reply = new TaskCompletionSource<Sprite>();
        Replies.Add(reply);
        return reply.Task;
    }
}
class UIModule
{
    public static UIModule Instance;
    public int Opened, m_curadvertistics;
    public void OpenPage(int page) => Opened++;
}
static class UIPageIds { public const int SlotPanel = 0; }
static class BroadcastBarController { public static object Instance; }
static class CommonConfirmTipsPanel
{
    public struct Args { public bool isLanguage; public string des; }
}
static class SDKAssetHandler
{
    public static readonly List<CommonConfirmTipsPanel.Args> Prompts = new();
    public static Task OpenCommonConfirmTipsPanel(CommonConfirmTipsPanel.Args args)
    {
        Prompts.Add(args);
        return Task.CompletedTask;
    }
}
static class TaskExtensions
{
    public static void Forget(this Task task)
    {
        if (task.IsFaulted) throw task.Exception;
    }
}
static class UIUtils
{
    public static readonly List<string> Toasts = new();
    public static void ShowTips(string text) => Toasts.Add(text);
// ICONS
}
// LANGUAGE
class GameObject
{
    public bool activeSelf = true;
    public bool activeInHierarchy => activeSelf;
    public Action OnEnable;
    public void SetActive(bool value)
    {
        if (activeSelf == value) return;
        activeSelf = value;
        if (value) OnEnable?.Invoke();
    }
}
class Transform { public Transform parent; public GameObject gameObject = new(); }
class WathAdProgress
{
    public GameObject gameObject = new();
    public Transform transform = new() { parent = new() };
    public bool enabled = true;
    public int RenderCount;
    public void Start()
    {
        gameObject.OnEnable = OnEnable;
        Awake();
        OnEnable();
    }
    public void Dispose() => OnDestroy();
// PROGRESS
}
enum E_AdPos { USSlot }
static class WithdrawalUtil
{
    public static float GetDollarCountByReward() => 0f;
    public static float GetCustomizedFloatByCountryType(float value) => value;
    public static string GetCustomizedIntByCountryType2(float value) => value.ToString();
    public static string GetCustomizedValueByCountryType(float value) => value.ToString();
}
static class ItemUtils
{
    public static (float Count, int Marker) Get(E_ItemType itemType) => (3f, 0);
}
static class BizzaSdk
{
    public static class Ad
    {
        public static int AdRequests;
        public static void ShowRewardAd(string pos, float reward, Action<bool> result) => AdRequests++;
    }
}
static class SlotProgressUtil
{
    public static int Progress = 1;
    public static bool CanFreeSpin => Progress > 0;
    public static void SetProgress(int value) => Progress = value;
}
class SlotEnter
{
// SLOT_ENTRY
}
class SlotPanel
{
    private bool isSloting;
    public int Spins;
    private void OnPlaySlot(bool isAd) => Spins++;
    private void OnAdResult(bool success) { }
    private void Refresh() { }
// SLOT_SPIN
}
// SLOT_CURRENCY
class BonusRate
{
    public GameObject gameObject = new();
    public string Text;
    public void Init(string value, bool white, bool bubble) => Text = value;
}
class GetRewardPanel
{
    public BonusRate bonusRate = new();
    public void Refresh() => RefreshBonusRate();
// RATIO
}
static class Program
{
    private static int assertions;
    private static void Check(bool pass, string name)
    {
        if (!pass) throw new Exception(name);
        assertions++;
        Console.WriteLine("PASS " + name);
    }

    static void Main()
    {
        var previous = new Sprite();
        var image = new Image { sprite = previous };
        AccountModule.CountryType = AccountModule.E_CountryType.None;
        UIUtils.SetWzSprite(image, "MoneyIcon");
        Check(image.enabled && image.sprite == previous, "offline icon keeps its authored sprite");
        var empty = new Image();
        foreach (string key in new[] {"MoneyIcon","PieceMoney","StackMoney","PileMoney","PileWealth",
            "PileGold","HundredMoney","AbundanceWealth","MoneyEnhancement","GoldCoin","BubbleCoin","BubbleMoney"})
            UIUtils.SetWzSprite(empty, key);
        Check(!empty.enabled && AssetUtils.Paths.Count == 0, "unknown country skips every icon request");
        AccountModule.CountryType = AccountModule.E_CountryType.BR;
        UIUtils.SetWzSprite(empty, "StackMoney", true);
        Check(!empty.enabled && AssetUtils.Paths[0] == "StackMoney_BR", "recovery uses actual country");
        AssetUtils.Replies[0].SetResult(previous);
        Check(empty.enabled && empty.sprite == previous && empty.NativeSizeUpdates == 1, "valid icon restores graphic and native size");
        UIUtils.SetWzSprite(empty, "StackMoney");
        AssetUtils.Replies[1].SetResult(null);
        Check(empty.enabled && empty.sprite == previous, "failed request preserves the previous graphic");
        UIUtils.SetWzSprite(empty, "StackMoney");
        AccountModule.CountryType = AccountModule.E_CountryType.ID;
        UIUtils.SetWzSprite(empty, "StackMoney");
        var indonesia = new Sprite();
        AssetUtils.Replies[3].SetResult(indonesia);
        AssetUtils.Replies[2].SetResult(previous);
        Check(empty.sprite == indonesia, "late former-country request is ignored");
        UIUtils.SetWzSprite(empty, "MoneyIcon");
        UIUtils.SetWzSprite(empty, "GoldCoin");
        var latest = new Sprite();
        AssetUtils.Replies[5].SetResult(latest);
        AssetUtils.Replies[4].SetResult(previous);
        Check(empty.sprite == latest, "late same-country request is ignored");
        UIUtils.SetWzSprite(empty, "GoldCoin");
        AccountModule.CountryType = AccountModule.E_CountryType.None;
        UIUtils.SetWzSprite(empty, "GoldCoin");
        AssetUtils.Replies[6].SetResult(previous);
        Check(empty.sprite == latest && AssetUtils.Paths.Count == 7, "offline state invalidates a pending icon request");

        Application.systemLanguage = SystemLanguage.Portuguese;
        LanguageUtils.InitLanguageFromIP().MoveNext();
        Check(LanguageUtils.HasReadLanguage && LanguageUtils.SelectedLanguage == "pt-BR", "Portuguese initialized locally");
        Application.systemLanguage = SystemLanguage.Indonesian;
        LanguageUtils.InitLanguageFromIP().MoveNext();
        Check(LanguageUtils.SelectedLanguage == "pt-BR", "system changes preserve the stored choice");
        PlayerPrefs.Clear();
        LanguageUtils.InitLanguageFromIP().MoveNext();
        Check(LanguageUtils.SelectedLanguage == "id-ID", "Indonesian recognized at first launch");
        PlayerPrefs.Clear();
        Application.systemLanguage = SystemLanguage.French;
        LanguageUtils.InitLanguageFromIP().MoveNext();
        Check(LanguageUtils.SelectedLanguage == "en-US", "unsupported device language uses English");
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("es-MX");
        Application.systemLanguage = SystemLanguage.Spanish;
        Check(LanguageUtils.GetStandardLanguageCode() == "es-MX", "preserves Mexican Spanish on first launch");
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-PH");
        Application.systemLanguage = SystemLanguage.English;
        Check(LanguageUtils.GetStandardLanguageCode() == "en-PH", "preserves Philippine English on first launch");
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        Check(LanguageUtils.GetStandardLanguageCode() == "en-US", "empty system region uses regular English");
        PlayerPrefs.SetString("SelectedLanguage", "unknown-old-code");
        LanguageUtils.InitLanguageFromIP().MoveNext();
        Check(LanguageUtils.SelectedLanguage == "en-US", "unsupported legacy preference migrated");
        foreach (string locale in new[] {"en-US","pt-BR","id-ID","zh-CN","ru-RU","ja-JP","ko-KR",
            "es-ES","es-MX","tr-TR","vi-VN","en-PH","ar-SA","de-DE"})
        {
            LanguageUtils.SelectedLanguage = locale;
            Check(LanguageUtils.SelectedLanguage == locale, "keeps " + locale);
        }

        LanguageUtils.SelectedLanguage = "pt-BR";
        var row = new Row();
        row.Dict["en-US"] = "English fallback";
        TableUtils.Tables.TblLanguage.DataMap.Add("Key", row);
        Check(LanguageUtils.GetText("Key") == "English fallback", "missing translation falls back to English");
        row.Dict["pt-BR"] = "";
        Check(LanguageUtils.GetText("Key") == "English fallback", "empty translation falls back to English");
        row.Dict["pt-BR"] = "Translated";
        Check(LanguageUtils.GetText("Key") == "Translated", "present translation stays selected");
        row.Dict["en-US"] = row.Dict["pt-BR"] = "";
        Check(LanguageUtils.GetText("Key") == "Key", "empty translations fall back to the key");
        AccountModule.Instance = new AccountModule();
        Check(!UIUtils.CheckWKYAccountReady() && SDKAssetHandler.Prompts.Count == 1 &&
            SDKAssetHandler.Prompts[0].isLanguage && SDKAssetHandler.Prompts[0].des == "WKY_Reconnecting",
            "offline business action opens the shared reconnect prompt");
        AccountModule.Instance.AccountReady = true;
        Check(UIUtils.CheckWKYAccountReady() && SDKAssetHandler.Prompts.Count == 1,
            "recovered business action proceeds without another prompt");
        AccountModule.Instance.IsSuspended = true;
        Check(!UIUtils.CheckWKYAccountReady() && SDKAssetHandler.Prompts.Count == 1,
            "suspension takes precedence without another reconnect prompt");

        UIUtils.ShowNetworkErrorTip();
        Check(UIUtils.Toasts.Count == 0, "network tip waits for its UI presenter");
        UIModule.Instance = new UIModule();
        BroadcastBarController.Instance = new object();
        for (int request = 0; request < 8; request++)
            UIUtils.ShowNetworkErrorTip();
        Check(UIUtils.Toasts.Count == 5, "network tip budget is shared and stops at five");
        var recoveredAccount = new AccountModule();
        AccountModule.Instance = recoveredAccount;
        var progress = new WathAdProgress();
        progress.Start();
        Check(recoveredAccount.Requests == 0 && !progress.gameObject.activeSelf,
            "offline reward panel hides the ad strip without sending config");
        var panel = new GetRewardPanel();
        recoveredAccount.OceanShineAppOtherConfigResponse = "cached config";
        panel.Refresh();
        Check(recoveredAccount.RatioReads == 0 && !panel.bonusRate.gameObject.activeSelf,
            "cached config does not trigger an offline ratio read");

        recoveredAccount.OceanShineAppOtherConfigResponse = null;
        recoveredAccount.AccountReady = true;
        BizzaEventSystem.Emit(EventDefine.Login.InitContentByCountry);
        BizzaEventSystem.Emit(EventDefine.Login.InitContentByCountry);
        Check(recoveredAccount.Requests == 1 && !progress.gameObject.activeSelf,
            "account recovery starts one config request for an open panel");
        recoveredAccount.CompleteRequest(true, "valid config");
        Check(progress.gameObject.activeSelf && progress.RenderCount == 1,
            "successful config refreshes the recovered ad strip");
        panel.Refresh();
        Check(recoveredAccount.RatioReads == 1 && panel.bonusRate.Text == "5%",
            "real account can display the configured bonus ratio");
        progress.transform.parent.gameObject.SetActive(false);
        recoveredAccount.OceanShineAppOtherConfigResponse = null;
        BizzaEventSystem.Emit(EventDefine.Login.InitContentByCountry);
        Check(recoveredAccount.Requests == 1, "closed reward panel never requests config");
        progress.Dispose();
        Check(BizzaEventSystem.CountryListeners == 0, "destroyed progress detaches its recovery listener");
        AccountModule.Instance = new AccountModule();
        var slots = new SlotPanel();
        var slotEntry = new SlotEnter();
        slotEntry.OnClick();
        slots.PlaySlotBtn();
        Check(UIModule.Instance.Opened == 0 && SlotProgressUtil.Progress == 1 &&
            slots.Spins == 0 && BizzaSdk.Ad.AdRequests == 0,
            "offline slots cannot open or consume a free spin");

        var currency = new CurrencyInfo { e_ItemType = E_ItemType.Gold, valueText = new TMP_Text() };
        currency.EnableForTest();
        Check(currency.valueText.text == "\u2014", "missing slots profile displays a placeholder");
        AccountModule.Instance.AccountReady = true;
        AccountModule.CountryType = AccountModule.E_CountryType.BR;
        AccountModule.Instance.Os_Current_Uso = new AccountModule.UserInfo { Balance = 12 };
        BizzaEventSystem.Emit(EventDefine.Login.InitContentByCountry);
        Check(currency.valueText.text == "12", "slots currency refreshes from recovered user");
        AccountModule.Instance.Os_Current_Uso = null;
        currency.Refresh();
        Check(currency.valueText.text == "12", "missing replacement profile keeps last real slots balance");
        currency.DisableForTest();
        Check(BizzaEventSystem.CountryListeners == 0, "disabled slots currency unregisters recovery listener");

        AccountModule.Instance.Os_Current_Uso = new AccountModule.UserInfo { Balance = 21 };
        slotEntry.OnClick();
        slots.PlaySlotBtn();
        Check(UIModule.Instance.Opened == 1 && SlotProgressUtil.Progress == 0 && slots.Spins == 1,
            "ready slots can open and consume one free spin");
        Console.WriteLine("UI_RECOVERY_PASS cases=" + assertions);
    }
}
"""


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--unity-editor", required=True, type=Path)
    data = parser.parse_args().unity_editor / "Data"
    production = load_production()
    output = ROOT / "Temp/OfflineSyncChecks/ui-recovery"
    output.mkdir(parents=True, exist_ok=True)
    source = output / "UiRecovery.cs"
    production["SLOT_CURRENCY"] = production["SLOT_CURRENCY"].replace(
        "    private void OnEnable()", "    public void EnableForTest()").replace(
        "    private void OnDisable()", "    public void DisableForTest()")
    fixture = FIXTURE
    for key, value in production.items():
        fixture = fixture.replace("// " + key, value)
    source.write_text(fixture, encoding="utf-8")
    executable = output / "UiRecovery.exe"
    shims = data / "NetStandard/compat/2.1.0/shims/netfx"
    references = [data / "NetStandard/ref/2.1.0/netstandard.dll"] + [
        shims / name for name in ("mscorlib.dll", "System.dll", "System.Core.dll")]
    response = output / "compile.rsp"
    response.write_text("\n".join([
        "-nologo", "-nostdlib+", "-langversion:latest", "-target:exe",
        "-define:BIZZA_REAL_WITHDRAW;BIZZA_HTTP_AD", f'-out:"{executable}"',
        *(f'-r:"{reference}"' for reference in references), f'"{source}"',
    ]), encoding="utf-8")
    subprocess.run([str(data / "NetCoreRuntime/dotnet.exe"), str(data / "DotNetSdkRoslyn/csc.dll"),
                    f"@{response}"], check=True)
    subprocess.run([str(data / "MonoBleedingEdge/bin/mono.exe"), str(executable)], check=True)


if __name__ == "__main__":
    main()
