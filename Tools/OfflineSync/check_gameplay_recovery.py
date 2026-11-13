"""Execute target gameplay recovery methods with isolated account/UI/scheduler boundaries.

Uses the production FlowModule and WkyGameplayBridge method bodies unchanged.
The fixture models async frame/UI completion; it does not invoke Unity scenes,
network endpoints, player saves, ad rewards, or withdrawal services.
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
    flow = (ROOT / 'Assets/BizzaWZ/Common/Framework/Runtime/Module/FlowModule.cs').read_text(encoding='utf-8-sig')
    bridge = (ROOT / 'Assets/WkyGameplayIntegration/WkyGameplayBridge.cs').read_text(encoding='utf-8-sig')
    fixture = Path(__file__).with_name('gameplay_recovery_fixture.cs.txt').read_text(encoding='utf-8')
    flow_methods = ('public static int BeginLevelLoading(', 'public static void ApplyServerProgress(',
                    'private static void ResetLevelState(', 'public static void ReportFirstEffectiveAction(',
                    'public static void GameStart(', 'public static void OpenGameWinPanel(',
                    'private static void AdvanceCompletedLevel(', 'public static void CanShowGuide(')
    bridge_methods = ('private void ObserveAccountLevel(', 'public bool TryGetCurrentLevel(',
                      'internal void PrepareInitialAttempt(', 'public void OnAttemptReady(',
                      'internal async UniTask CompleteSceneHandoff(', 'private async UniTask CompleteSceneHandoffOnce(',
                      'private void EnsureCurrentAttempt(', 'private void ReportTutorialCompletion(',
                      'public void OnAttemptEnded(', 'private static LevelInfo ToLevelInfo(',
                      'internal bool TryCaptureWinReport(', 'internal async UniTask ReloadAttempt(',
                      'internal static UniTask ShowLoadFailure(', 'internal static void ReportProgress(',
                      'private void PrepareNextAttempt(', 'private sealed class WinSettlement')
    fixture = fixture.replace('// FLOW_CURRENT_LEVEL', re.search(r'public static int CurrentLevel\s*=>[^;]+;', flow)[0])
    fixture = fixture.replace('// FLOW_METHODS', '\n'.join(block(flow, method) for method in flow_methods))
    fixture = fixture.replace('// BRIDGE_FIELDS', bridge[bridge.index('    internal const string SceneName'):bridge.index('    public bool BlocksGameplayInput')])
    fixture = fixture.replace('// BRIDGE_METHODS', '\n'.join(block(bridge, method) for method in bridge_methods))
    output = ROOT / 'Temp/OfflineSyncChecks/gameplay-recovery'
    output.mkdir(parents=True, exist_ok=True)
    generated = output / 'GameplayRecoveryChecks.cs'
    generated.write_text(fixture, encoding='utf-8')
    data = args.unity_editor / 'Data'
    shims = data / 'NetStandard/compat/2.1.0/shims/netfx'
    refs = [data / 'NetStandard/ref/2.1.0/netstandard.dll']
    refs += [shims / name for name in ('mscorlib.dll', 'System.dll', 'System.Core.dll')]
    exe = output / 'GameplayRecoveryChecks.exe'
    rsp = output / 'gameplay.rsp'
    rsp.write_text('\n'.join(['-nologo', '-utf8output', '-nostdlib+', '-langversion:latest', '-target:exe',
                             '-define:BIZZA_REAL_WITHDRAW', f'-out:"{exe}"',
                             *[f'-r:"{p}"' for p in refs], f'"{generated}"']), encoding='utf-8')
    for name, command in (
        ('compile', [str(data / 'NetCoreRuntime/dotnet.exe'), str(data / 'DotNetSdkRoslyn/csc.dll'), '@' + str(rsp)]),
        ('results', [str(data / 'MonoBleedingEdge/bin/mono.exe'), str(exe)]),
    ):
        result = subprocess.run(command, cwd=ROOT, capture_output=True, text=True, encoding='utf-8', errors='replace')
        text = result.stdout + result.stderr
        (output / (name + '.log')).write_text(text, encoding='utf-8')
        print(text, end='')
        if result.returncode:
            raise SystemExit(result.returncode)


if __name__ == '__main__':
    main()
