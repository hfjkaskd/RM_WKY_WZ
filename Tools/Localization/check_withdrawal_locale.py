"""Exercise production withdrawal name and money formatting with the Unity toolchain.

Also validate the authored scrolling detail hierarchy and localized status widths.
Does not access accounts, submit withdrawals, or modify player saves.
"""
from pathlib import Path
import argparse, json, re, subprocess, sys

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "Tools/OfflineSync"))
from source_blocks import block


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--unity-editor", type=Path, required=True)
    args = parser.parse_args()
    data = args.unity_editor / "Data"
    output = ROOT / "Temp/LocalizationFixChecks/withdrawal"
    output.mkdir(parents=True, exist_ok=True)
    source = (ROOT / "Assets/BizzaWZ/Final/Real/UI/WithdrawFillPanel/UIWithdrawalPanel.cs").read_text(encoding="utf-8-sig")
    confirmation = (ROOT / "Assets/BizzaWZ/Final/Real/UI/WithdrawFillPanel/UIWithdrawalConfirmPanel.cs").read_text(encoding="utf-8-sig")
    money = (ROOT / "Assets/BizzaWZ/Final/Connect_SDK/SDK_WKY/API/Utils/WithdrawalUtil.cs").read_text(encoding="utf-8-sig")
    regex = re.search(r'private static readonly Regex NameRegex\s*=\s*new Regex\([\s\S]*?;', source)[0]
    cultures = "\n".join(re.findall(r"private static readonly CultureInfo \w+ = .*?;", money))
    generated = """
using System;
using System.Globalization;
using System.Text.RegularExpressions;
static class Mathf { public static float Floor(float v) => (float)Math.Floor(v); }
static class AccountModule {
    public enum E_CountryType { None, BR, US, ID } public static E_CountryType CountryType;
    public class RoutineTaskLookAdMoneyResponse { public int Os_Ss, Os_Tid; public double Os_My; }
}
enum E_WithdrawType { DailyMission, Real, Fake }
class SnapshotCheck {
    class Button { public bool interactable; }
    Button withdrawalBtn = new Button();
    E_WithdrawType withdrawType;
    double? dailyMissionAmount;
    int dailyMissionTaskId;
    // SNAPSHOT_METHODS
    public void Open(E_WithdrawType type, AccountModule.RoutineTaskLookAdMoneyResponse mission) {
        withdrawType=type;
        CaptureDailyMissionSnapshot(mission);
    }
    public double? Amount => dailyMissionAmount;
    public string Reference => GetDailyMissionReference();
    public bool Enabled => withdrawalBtn.interactable;
}
class NameCheck {
    class Platform { public string Os_Cn; }
    Platform data;
    const string ovoInfo="ovo.jpg", danaInfo="dana.jpg";
    // REGEX
    // NAME_METHOD
    public bool Accept(string name, bool indonesian) {
        data = indonesian ? new Platform{Os_Cn=danaInfo} : null;
        return IsNameValueValid(name);
    }
}
static class WithdrawalUtil {
    // CULTURES
    // MONEY_METHODS
}
static class Program {
    static int count;
    static void Check(bool value, string message) { if(!value)throw new Exception(message); count++; Console.WriteLine("PASS "+message); }
    static void Main() {
        var check=new NameCheck();
        foreach(var s in new[]{"José","João","Márcia","Jo\\u0303ao","John Smith","  José  ","Muhammad Rizky Pratama Putra Ramadhan Wijaya"})
            Check(check.Accept(s,true),"accept real Unicode name: "+s);
        foreach(var s in new[]{""," ","123","José3","<b>José</b>","\\u0301Jose","John\\nSmith","John\\tSmith","Name!"})
            Check(!check.Accept(s,false),"reject non-name input");
        Check(check.Accept(new string('a',44),true),"Indonesian 44-character name remains allowed");
        Check(!check.Accept(new string('a',45),true),"Indonesian 45-character limit remains enforced");
        Check(check.Accept(new string('a',99),false),"Brazilian 99-character name remains allowed");
        Check(!check.Accept(new string('a',100),false),"Brazilian 100-character limit remains enforced");
        foreach(var culture in new[]{"en-US","pt-BR","id-ID"}) {
            CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo(culture);
            AccountModule.CountryType=AccountModule.E_CountryType.US;
            Check(WithdrawalUtil.GetCustomizedValueByCountryType(10.5f)=="10.50","US money independent of "+culture+" device culture");
            AccountModule.CountryType=AccountModule.E_CountryType.BR;
            Check(WithdrawalUtil.GetCustomizedValueByCountryType(10.5f)=="10,50","Brazil money independent of "+culture+" device culture");
            AccountModule.CountryType=AccountModule.E_CountryType.ID;
            Check(WithdrawalUtil.GetCustomizedValueByCountryType(1500.75f)=="1.500","Indonesia groups and truncates rupiah under "+culture);
        }
        var snapshot=new SnapshotCheck();
        var mission=new AccountModule.RoutineTaskLookAdMoneyResponse{Os_Tid=17,Os_My=800,Os_Ss=2};
        snapshot.Open(E_WithdrawType.DailyMission,mission);
        Check(snapshot.Enabled&&snapshot.Amount==800&&snapshot.Reference=="task_17","claimable task snapshots amount and request ID together");
        mission.Os_Tid=18; mission.Os_My=2000;
        Check(snapshot.Amount==800&&snapshot.Reference=="task_17","later response changes cannot mismatch confirmed amount and submitted task");
        snapshot.Open(E_WithdrawType.DailyMission,null);
        Check(!snapshot.Enabled&&snapshot.Amount==null&&snapshot.Reference=="","missing task cannot be submitted");
        snapshot.Open(E_WithdrawType.Fake,null);
        Check(snapshot.Enabled&&snapshot.Amount==null&&snapshot.Reference=="","reopening introductory withdrawal resets a disabled button and stale task");
        snapshot.Open(E_WithdrawType.DailyMission,new AccountModule.RoutineTaskLookAdMoneyResponse());
        Check(!snapshot.Enabled,"SDK default empty task is not a valid confirmation");
        snapshot.Open(E_WithdrawType.Real,null);
        Check(snapshot.Enabled,"reopening real withdrawal resets a disabled button");
        foreach(var value in new[]{0d,-1d,double.NaN,double.PositiveInfinity,double.NegativeInfinity}) {
            snapshot.Open(E_WithdrawType.DailyMission,new AccountModule.RoutineTaskLookAdMoneyResponse{Os_Tid=1,Os_My=value,Os_Ss=2});
            Check(!snapshot.Enabled&&snapshot.Reference=="","invalid task amount cannot be submitted");
        }
        snapshot.Open(E_WithdrawType.DailyMission,new AccountModule.RoutineTaskLookAdMoneyResponse{Os_Tid=19,Os_My=.2,Os_Ss=3});
        Check(!snapshot.Enabled,"already claimed task cannot be resubmitted by confirmation");
        Console.WriteLine("WITHDRAWAL_LOCALE_PASS cases="+count);
    }
}
"""
    generated = generated.replace("// REGEX", regex).replace("// NAME_METHOD", block(source, "private bool IsNameValueValid("))
    generated = generated.replace("// CULTURES", cultures).replace("// MONEY_METHODS", "\n".join(block(money, method) for method in (
        "public static string GetCustomizedValueByCountryType(", "public static float GetCustomizedFloatByCountryType(")))
    generated = generated.replace("// SNAPSHOT_METHODS", "\n".join(block(confirmation, method) for method in (
        "private void CaptureDailyMissionSnapshot(", "private string GetDailyMissionReference(")))
    assert "CaptureDailyMissionSnapshot(" in block(confirmation, "protected override void OnOpen(")
    assert "GetDailyMissionReference();" in block(confirmation, "public void OnClickWithdrawBtn(")
    assert "routineTaskLookAdMoneyResponse" not in block(confirmation, "public void OnClickWithdrawBtn(")
    cs = output / "WithdrawalLocaleChecks.cs"
    cs.write_text(generated, encoding="utf-8")
    shims = data / "NetStandard/compat/2.1.0/shims/netfx"
    refs = [data / "NetStandard/ref/2.1.0/netstandard.dll"] + [shims / name for name in ("mscorlib.dll", "System.dll", "System.Core.dll")]
    exe = output / "WithdrawalLocaleChecks.exe"
    rsp = output / "checks.rsp"
    rsp.write_text("\n".join(["-nologo","-utf8output","-nostdlib+","-langversion:latest","-target:exe","-define:BIZZA_REAL_WITHDRAW",
        f'-out:"{exe}"', *[f'-r:"{p}"' for p in refs], f'"{cs}"']), encoding="utf-8")
    for phase, command in [
        ("compile",[str(data/"NetCoreRuntime/dotnet.exe"),str(data/"DotNetSdkRoslyn/csc.dll"),"@"+str(rsp)]),
        ("run",[str(data/"MonoBleedingEdge/bin/mono.exe"),str(exe)]),
    ]:
        r = subprocess.run(command,cwd=ROOT,capture_output=True,text=True,encoding="utf-8",errors="replace")
        (output/(phase+".log")).write_text(r.stdout+r.stderr,encoding="utf-8")
        print(r.stdout+r.stderr,end="")
        if r.returncode: raise SystemExit(r.returncode)
    check_prefabs(output)


def documents(path):
    text = path.read_text(encoding="utf-8-sig")
    return {int(m[2]): m[4] for m in re.finditer(r"^--- !u!(\d+) &(\d+)( stripped)?\n(.*?)(?=^--- !u!|\Z)",text,re.M|re.S)}


def check_prefabs(output):
    for rel in (
        "Assets/BizzaWZ/Final/Real/UI/UIWithdrawalConfirmPanel/UIWithdrawalConfirmPanel.prefab",
        "Assets/BizzaWZ/Final/Real/UI/UIWithdrawalPendingPanel/UIWithdrawalPendingPanel.prefab",
        "Assets/BizzaWZ/Final/Real/UI/WithdrawFillPanel/WithdrawFillPanel.prefab",
        "Assets/BizzaWZ/Final/Real/UI/WithdrawHistory/WithdrawHistoryItem.prefab",
    ):
        docs = documents(ROOT/rel)
        original = ROOT/"Logs/LocalizationFix/before"/rel
        if original.exists():
            assert set(documents(original)).issubset(docs), "An existing (including stripped) prefab object was lost: "+rel
        for identifier,d in docs.items():
            if not d.startswith("RectTransform:"): continue
            children=re.search(r"  m_Children:\n(.*?)  m_Father:",d,re.S)
            if children:
                for child in re.findall(r"fileID: (\d+)",children[1]):
                    assert int(child) in docs,(rel,identifier,"missing child",child)
    confirm = documents(ROOT/"Assets/BizzaWZ/Final/Real/UI/UIWithdrawalConfirmPanel/UIWithdrawalConfirmPanel.prefab")
    pending = documents(ROOT/"Assets/BizzaWZ/Final/Real/UI/UIWithdrawalPendingPanel/UIWithdrawalPendingPanel.prefab")
    for docs in (confirm,pending):
        for d in docs.values():
            if d.startswith("GameObject:"):
                assert all(int(i) in docs for i in re.findall(r"component: \{fileID: (\d+)\}",d))
        scrolls = [d for d in docs.values() if "guid: 1aa08ab6e0800fa44ae55d278d1423e3" in d]
        assert len(scrolls) == 1
        for d in scrolls:
            assert "m_Horizontal: 0" in d and "m_Vertical: 1" in d
            content=int(re.search(r"m_Content: \{fileID: (\d+)\}",d)[1])
            viewport=int(re.search(r"m_Viewport: \{fileID: (\d+)\}",d)[1])
            assert f"m_Father: {{fileID: {viewport}}}" in docs[content]
    for text in (398121796105642911,8553279188846189835,4103617465848996635):
        assert "m_enableWordWrapping: 1" in confirm[text]
        assert "m_overflowMode: 0" in confirm[text]
        assert "m_isRichText: 0" in confirm[text]
    # TMP overflow checks cannot detect two individually valid text rectangles overlapping.
    detail_column_gaps = []
    for title, value in ((2905743246625564868,5606062196254376354),
                         (5528718886607180131,8550677573109569583),
                         (7865899812459753909,8984784099156520587)):
        def rect_x_width(identifier):
            d=confirm[identifier]
            x=float(re.search(r"m_AnchoredPosition: \{x: ([\d.-]+)",d)[1])
            width=float(re.search(r"m_SizeDelta: \{x: ([\d.-]+)",d)[1])
            return x,width
        tx,tw=rect_x_width(title)
        vx,vw=rect_x_width(value)
        gap=vx-vw/2-(tx+tw/2)
        assert gap>=15,(title,value,"detail columns overlap or lack spacing",gap)
        detail_column_gaps.append(gap)
    assert "m_overflowMode: 0" in pending[110003]
    assert "m_enableWordWrapping: 1" in pending[110003]
    assert "m_isRichText: 0" in pending[110003]

    font=(ROOT/"Assets/BizzaWZ/Final/RoyalMatchReskin/SettingsMikado.asset").read_text(encoding="utf-8-sig")
    glyphs={int(m[1]):float(m[2]) for m in re.finditer(r"  - m_Index: (\d+)\n.*?m_HorizontalAdvance: ([\d.]+)",font,re.S)}
    chars={int(m[1]):int(m[2]) for m in re.finditer(r"m_Unicode: (\d+)\n    m_GlyphIndex: (\d+)",font)}
    point=float(re.search(r"m_PointSize: ([\d.]+)",font)[1])
    translations=json.loads((ROOT/"Tools/Localization/withdrawal-translations.json").read_text(encoding="utf-8"))
    widths={}
    for key in ("Withdrawal_StatusSuccess","Withdrawal_StatusProcessing","RM_WithdrawalHistory_Failed"):
        for locale,text in translations[key].items():
            width=sum(glyphs[chars[ord(c)]] for c in text)*28/point + max(0,len(text)-1)*7*28*.01
            assert width<260,(key,locale,width)
            widths[key+"/"+locale]=round(width,2)
    (output/"prefab-checks.json").write_text(json.dumps({"scrolling_details":True,"full_wrapped_values":True,
        "scrolling_result_reason":True,"detail_column_gaps":detail_column_gaps,
        "status_badge_minimum_widths":widths},indent=2),encoding="utf-8")
    print("WITHDRAWAL_PREFAB_PASS scrolling details/reasons, references, nine localized badge widths")


if __name__ == "__main__":
    main()
