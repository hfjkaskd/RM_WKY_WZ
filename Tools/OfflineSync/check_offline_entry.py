"""Exercise production login/ad callbacks against isolated transport, clock and save doubles.
No Unity instance, device, network endpoint or real PlayerPrefs is used.
"""
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
    out = ROOT / 'Temp/OfflineSyncChecks/entry'
    out.mkdir(parents=True, exist_ok=True)
    account = (ROOT / 'Assets/BizzaWZ/Final/Connect_SDK/SDK_WKY/API/AccountModule.cs').read_text(encoding='utf-8-sig')
    fields = account[account.index('    public bool LoadingFinish'):account.index('    private void CheckSuspension')]
    methods = '\n'.join(block(account, signature) for signature in (
        'private void CheckSuspension(',
        'private void MarkLoadingUserDataFailed(string',
        'private void MarkLoadingUserDataFailed<T>(',
        'private async UniTask RetryLoginAsync(',
        'private UniTask OnLoginData(',
        'private async UniTask TryLoginGame(',
        'private void InitUserInfo(',
        'public void Response_LoginResponse(',
        'public void Response_UserInfoResponse(',
        'public void Request_UserInfoRequest(',
        'public void Request_UserReachReportRequest(',
    ))
    assert 'useEditorUserId' not in methods and 'editorUserId' not in methods and 'UNITY_EDITOR' not in methods, 'Login must use the same account path on Editor and device'
    # Replace only the async task carrier. Branches, callbacks, state and await order are unchanged.
    methods = methods.replace('async UniTask ', 'async Task ').replace('private UniTask OnLoginData(', 'private Task OnLoginData(')
    fixture = Path(__file__).with_name('offline_entry_fixture.cs.txt').read_text(encoding='utf-8')
    fixture = fixture.replace('// ACCOUNT_FIELDS', fields).replace('// ACCOUNT_METHODS', methods)
    wky = (ROOT / 'Assets/BizzaWZ/Final/Connect_SDK/SDK_WKY/WKY_Flow.cs').read_text(encoding='utf-8-sig')
    native = (ROOT / 'Assets/BizzaWZ/Final/FunctionTools/CommonPublic/DeviceInfo/DeviceNativeBridge.cs').read_text(encoding='utf-8-sig')
    boots = (ROOT / 'Assets/BizzaWZ/Final/FunctionTools/CommonPublic/Boots_Flow.cs').read_text(encoding='utf-8-sig')
    assert '#if USER_VERIFICATION_SDK && !WKY_SDK' in boots
    init = block(wky, 'private async UniTask Init(')
    order = [init.index(signature) for signature in ('await VerifyCountryAccessAsync(', 'await VerifyVpnAccessAsync()', 'account.Login()')]
    assert order == sorted(order) and 'USER_VERIFICATION_SDK' not in init, 'WKY country/VPN decision order is independent of Auth SDK'
    gate_state = wky[wky.index('    private const string CountryWorkerUrl'):wky.index('    public async UniTask Start_Init_Flow')]
    gate_methods = wky[wky.index('    internal static async UniTask<bool> CanStartAccountRequestsAsync('):
                       wky.index('\n}\n\npublic sealed class WKYInitializationException')]
    gate_methods = gate_methods.replace('async UniTask', 'async Task')
    fixture = fixture.replace('// COUNTRY_FIELDS', gate_state).replace('// COUNTRY_METHODS', gate_methods)
    fixture = fixture.replace('// VPN_POLICY_METHOD', block(native, 'public static void ApplyVpnDetectionPolicy('))
    http = (ROOT / 'Assets/BizzaWZ/Final/Connect_SDK/SDK_WKY/API/Utils/HttpUtil.cs').read_text(encoding='utf-8-sig')
    for marker, signature in (
        ('// HTTP_GENERIC_ERROR', 'private static async UniTask RequestToServerCoroutine<T>('),
        ('// HTTP_RAW_ERROR', 'private static async UniTask RequestToServerCoroutine('),
    ):
        fixture = fixture.replace(marker, block(block(http, signature), 'if (outer.code != "200")'))
    generated = out / 'OfflineEntryFixture.cs'
    generated.write_text(fixture, encoding='utf-8')
    ad = ROOT / 'Assets/BizzaWZ/Final/Connect_SDK/SDK_WKY/API/Ad'
    data = args.unity_editor / 'Data'
    refs = [data / 'NetStandard/ref/2.1.0/netstandard.dll']
    refs += [data / 'NetStandard/compat/2.1.0/shims/netfx' / name for name in ('mscorlib.dll', 'System.dll', 'System.Core.dll')]
    exe = out / 'OfflineEntryFixture.exe'
    rsp = out / 'fixture.rsp'
    rsp.write_text('\n'.join([
        '-nologo', '-utf8output', '-nostdlib+', '-langversion:latest', '-target:exe', '-define:BIZZA_REAL_WITHDRAW;BIZZA_HTTP_AD;COMMONGAME',
        f'-out:"{exe}"', *[f'-r:"{p}"' for p in refs],
        f'"{generated}"', f'"{ad / "BizzaSdk.Ad.cs"}"', f'"{ad / "BizzaSdk.Types.cs"}"',
    ]), encoding='utf-8')
    for name, command in (
        ('fixture-compile', [str(data / 'NetCoreRuntime/dotnet.exe'), str(data / 'DotNetSdkRoslyn/csc.dll'), '@' + str(rsp)]),
        ('fixture-results', [str(data / 'MonoBleedingEdge/bin/mono.exe'), str(exe)]),
    ):
        result = subprocess.run(command, cwd=ROOT, capture_output=True, text=True, encoding='utf-8', errors='replace')
        output = result.stdout + result.stderr
        (out / (name + '.log')).write_text(output, encoding='utf-8')
        print(output, end='')
        if result.returncode:
            raise SystemExit(result.returncode)


if __name__ == '__main__':
    main()
