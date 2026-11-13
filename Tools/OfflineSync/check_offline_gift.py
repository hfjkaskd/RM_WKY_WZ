"""Exercise production gift UI and module decisions with isolated save/account/UI boundaries."""
from pathlib import Path
import argparse
import re
import subprocess
import sys

sys.dont_write_bytecode = True
from source_blocks import block

ROOT = Path(__file__).resolve().parents[2]


def main():
    sys.stdout.reconfigure(encoding='utf-8')
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--unity-editor', type=Path, required=True)
    args = parser.parse_args()
    assets = ROOT / 'Assets/BizzaWZ'
    module = (assets / 'Final/Framework/Runtime/Module/Teach/Teach01Module.cs').read_text(encoding='utf-8-sig')
    page = (assets / 'Final/Real/UI/NewbieGiftPage/NewbieGiftPage.cs').read_text(encoding='utf-8-sig')
    withdrawal = (assets / 'Final/Connect_SDK/SDK_WKY/API/Utils/WithdrawalUtil.cs').read_text(encoding='utf-8-sig')
    language = (assets / 'Common/Framework/Runtime/Module/Localization/LanguageUtils.cs').read_text(encoding='utf-8-sig')
    ui = (assets / 'Common/Framework/Runtime/Utility/UIUtils.cs').read_text(encoding='utf-8-sig')
    save = (assets / 'Final/Framework/Runtime/ActionGraph/Runtime/Base/Actions/Teach/TeachSaveData.cs').read_text(encoding='utf-8-sig')
    fixture = Path(__file__).with_name('offline_gift_fixture.cs.txt').read_text(encoding='utf-8')
    fixture = fixture.replace('// STATE', re.search(r'public enum NewbieGiftRequestState\s*\{[^}]+\}', save)[0])
    fixture = fixture.replace('// MODULE', '\n'.join(block(module, name) for name in ('public void OnFirstMatch(', 'void TryStart(', 'static bool IsNewUser(')))
    fixture = fixture.replace('// PAGE', '\n'.join(block(page, name) for name in ('protected override void OnOpen(', 'public void OnButtonClick(')))
    fixture = fixture.replace('// AMOUNT', block(withdrawal, 'public static float GetNewbieGift('))
    fixture = fixture.replace('// LANGUAGE', block(language, 'public static string GetText('))
    fixture = fixture.replace('// READY', block(ui, 'public static bool CheckWKYAccountReady('))
    # The real button listener must keep offline closure from opening the starter withdrawal.
    assert '!closeOnly && !ChannelConfig.Instance.real_CustomConfig.singleCurrencyMode' in block(page, 'void Awake(')
    out = ROOT / 'Temp/OfflineSyncChecks/gift'
    out.mkdir(parents=True, exist_ok=True)
    generated = out / 'OfflineGiftChecks.cs'
    generated.write_text(fixture, encoding='utf-8')
    data = args.unity_editor / 'Data'
    shims = data / 'NetStandard/compat/2.1.0/shims/netfx'
    refs = [data / 'NetStandard/ref/2.1.0/netstandard.dll']
    refs += [shims / name for name in ('mscorlib.dll', 'System.dll', 'System.Core.dll')]
    exe = out / 'OfflineGiftChecks.exe'
    rsp = out / 'gift.rsp'
    rsp.write_text('\n'.join(['-nologo', '-utf8output', '-nostdlib+', '-langversion:latest', '-target:exe', '-define:BIZZA_REAL_WITHDRAW;BIZZA_HTTP_AD',
                             f'-out:"{exe}"', *[f'-r:"{p}"' for p in refs], f'"{generated}"']), encoding='utf-8')
    for name, command in (
        ('gift-compile', [str(data / 'NetCoreRuntime/dotnet.exe'), str(data / 'DotNetSdkRoslyn/csc.dll'), '@' + str(rsp)]),
        ('gift-results', [str(data / 'MonoBleedingEdge/bin/mono.exe'), str(exe)]),
    ):
        result = subprocess.run(command, cwd=ROOT, capture_output=True, text=True, encoding='utf-8', errors='replace')
        output = result.stdout + result.stderr
        (out / (name + '.log')).write_text(output, encoding='utf-8')
        print(output, end='')
        if result.returncode:
            raise SystemExit(result.returncode)


if __name__ == '__main__':
    main()
