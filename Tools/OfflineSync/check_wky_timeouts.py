"""Check the WKY timeout contract using the production resolver and endpoint constants.

Runs without Unity, a device, network requests or real user data. Accepts either
the Cubie or Sweet Blast project; checks both generic and raw transport wiring.
"""
from pathlib import Path
import argparse
import re
import subprocess
import sys

sys.dont_write_bytecode = True
from source_blocks import block

ROOT = Path(__file__).resolve().parents[2]
API = Path('Assets/BizzaWZ/Final/Connect_SDK/SDK_WKY/API')
LONG_REQUESTS = {'ad_info', 'get_ecpm_id', 'add_ecpm', 'add_order', 'add_order_task', 'task_list'}
SHORT_REQUESTS = {'base_list', 'from', 'msg', 'msg_list', 'new_user', 'number',
                  'order_list', 'plat_from', 'user_info', 'app_info'}


def check_wiring(http, java):
    for signature in ('private static async UniTask RequestToServerCoroutine<T>(',
                      'private static async UniTask RequestToServerCoroutine('):
        method = block(http, signature)
        assert 'float timeoutSeconds = RequestTimeout(path);' in method, signature
        assert re.search(r'SendHttpMethodName,\s+url,\s+ToSByteArray\(bodyRaw\),\s+id,\s+delay,\s+\(int\)timeoutSeconds', method), signature
        assert 'WaitAndroidBridgeReplyOrTimeout(id, timeoutSeconds + Mathf.Max(0f, delay))' in method, signature
        assert re.search(r'SendWithRetry\([^\n]+timeoutSeconds\)', method), signature
        assert 'if (replied) return;' in method and 'replied = true;' in method, signature
        assert 'HTTPReply.Remove(nativeRequestId)' in method, signature
    assert 'HTTPReply.TryGetValue(id, out string pending) && pending == null' in http
    assert re.search(r'SendHTTPInfo\(\s+final String url,\s+final byte\[\] bodyRaw,\s+final int id,\s+final float delay,\s+final int timeoutSeconds', java)
    assert java.count('doSendHTTP(url, bodyRaw, id, triggerTime, timeoutSeconds);') == 2
    assert 'request.timeout().timeout(timeoutSeconds, TimeUnit.SECONDS);' in java
    assert '.retryOnConnectionFailure(false)' in java
    for phase in ('connect', 'read', 'write'):
        assert f'.{phase}Timeout(0, TimeUnit.SECONDS)' in java
    print('PASS typed/raw Android and Unity deadline wiring, native total-call timeout, callback once/late guards')

def main():
    sys.stdout.reconfigure(encoding='utf-8')
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project', type=Path, default=ROOT)
    parser.add_argument('--unity-editor', type=Path, required=True)
    args = parser.parse_args()
    project = args.project.resolve()
    http = (project / API / 'Utils/HttpUtil.cs').read_text(encoding='utf-8-sig')
    resolver = block(http, 'private static float RequestTimeout(')
    cfg = (project / API / 'AccountModuleCfg.cs').read_text(encoding='utf-8-sig')
    cfg = re.sub(r'//[^\n]*|/\*[\s\S]*?\*/', '', cfg)
    routes = dict(re.findall(r'public const string (\w+) = "(/[^"\n]+)";', cfg))
    assert set(routes) == LONG_REQUESTS | SHORT_REQUESTS | {'login'}, 'Review new/removed endpoints explicitly'
    native_path = ('_Bizza_BuildTemplate/Android/unityLibrary/src/main/java/'
                   'com/bangsawan/oceanshine/HttpUtil.java')
    java = (project / native_path).read_text(encoding='utf-8-sig')
    check_wiring(http, java)
    constants = '\n'.join(f'public const string {name} = "{path}";' for name, path in routes.items())
    cases = '\n'.join(f'Check("{name}", AccountModuleCfg.{name}, {600 if name in LONG_REQUESTS else 6 if name == "login" else 5}f);'
                      for name in sorted(routes))
    fixture = '''using System;
public static class AccountModuleCfg { // CONSTANTS
}
class TimeoutContract {
// RESOLVER
static int count;
static void Check(string name, string path, float expected, float requested = 5f) {
    float actual = RequestTimeout(path, requested);
    if (actual != expected) throw new Exception(name + ": expected " + expected + ", got " + actual);
    Console.WriteLine("PASS " + name + " = " + actual + "s"); count++;
}
static void Main() {
// CASES
Check("unknown route", "/unknown", 5f);
Check("explicit custom timeout", "/unknown", 23f, 23f);
Check("longer business timeout", AccountModuleCfg.add_ecpm, 1200f, 1200f);
Check("startup stays short", AccountModuleCfg.login, 6f, 5f);
Console.WriteLine("PASS " + count + " production timeout contract cases");
}}
'''.replace('// CONSTANTS', constants).replace('// RESOLVER', resolver).replace('// CASES', cases)
    out = ROOT / 'Temp/OfflineSyncChecks/timeouts' / project.name
    out.mkdir(parents=True, exist_ok=True)
    generated = out / 'TimeoutContract.cs'
    generated.write_text(fixture, encoding='utf-8')
    data = args.unity_editor / 'Data'
    shims = data / 'NetStandard/compat/2.1.0/shims/netfx'
    refs = [data / 'NetStandard/ref/2.1.0/netstandard.dll']
    refs += [shims / name for name in ('mscorlib.dll', 'System.dll', 'System.Core.dll')]
    exe = out / 'TimeoutContract.exe'
    rsp = out / 'contract.rsp'
    rsp.write_text('\n'.join(['-nologo', '-utf8output', '-nostdlib+', '-langversion:latest', '-target:exe',
                             f'-out:"{exe}"', *[f'-r:"{p}"' for p in refs], f'"{generated}"']), encoding='utf-8')
    for name, command in (
        ('compile', [str(data / 'NetCoreRuntime/dotnet.exe'), str(data / 'DotNetSdkRoslyn/csc.dll'), '@' + str(rsp)]),
        ('results', [str(data / 'MonoBleedingEdge/bin/mono.exe'), str(exe)]),
    ):
        result = subprocess.run(command, cwd=ROOT, capture_output=True, text=True, encoding='utf-8', errors='replace')
        output = result.stdout + result.stderr
        (out / (name + '.log')).write_text(output, encoding='utf-8')
        print(output, end='')
        if result.returncode:
            raise SystemExit(result.returncode)


if __name__ == '__main__':
    main()
