"""Run production level-loop, save/restore, progression and framework-flow code.

Requires the installed Unity Editor toolchain, but does not launch Unity or touch
player saves. The save suite uses the real FlatBuffer encoder/decoder and writer.
Only Unity/board/database/network boundaries are substituted; no reflection.
"""
from pathlib import Path
import argparse
import re
import subprocess
import sys

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'Tools/OfflineSync'))
from source_blocks import block


def read(relative):
    return (ROOT / relative).read_text(encoding='utf-8-sig')


def run_suite(name, fixture, sources, data, output):
    generated = output / (name + '.cs')
    generated.write_text(fixture, encoding='utf-8')
    exe = output / (name + '.exe')
    shims = data / 'NetStandard/compat/2.1.0/shims/netfx'
    refs = [data / 'NetStandard/ref/2.1.0/netstandard.dll']
    refs += [shims / dll for dll in ('mscorlib.dll', 'System.dll', 'System.Core.dll')]
    rsp = output / (name + '.rsp')
    rsp.write_text('\n'.join([
        '-nologo', '-utf8output', '-nostdlib+', '-langversion:latest', '-target:exe',
        '-define:BIZZA_REAL_WITHDRAW', f'-out:"{exe}"',
        *[f'-r:"{p}"' for p in refs], f'"{generated}"',
        *[f'"{ROOT / p}"' for p in sources],
    ]), encoding='utf-8')
    for phase, command in (
        ('compile', [str(data / 'NetCoreRuntime/dotnet.exe'), str(data / 'DotNetSdkRoslyn/csc.dll'), '@' + str(rsp)]),
        ('results', [str(data / 'MonoBleedingEdge/bin/mono.exe'), str(exe), str(output / 'save-fixtures')]),
    ):
        result = subprocess.run(command, cwd=ROOT, capture_output=True, text=True, encoding='utf-8', errors='replace')
        log = result.stdout + result.stderr
        (output / (name + '-' + phase + '.log')).write_text(log, encoding='utf-8')
        print(log, end='')
        if result.returncode:
            raise SystemExit(result.returncode)


def main():
    sys.stdout.reconfigure(encoding='utf-8')
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--unity-editor', type=Path, required=True)
    args = parser.parse_args()
    output = ROOT / 'Temp/LevelLoopChecks'
    output.mkdir(parents=True, exist_ok=True)
    catalog = read('Assets/Foundation/Runtime/CoreLevelCatalog.cs')
    fixture = Path(__file__).with_name('level_loop_fixture.cs.txt').read_text(encoding='utf-8')
    fixture = fixture.replace('// CATALOG_METHODS', '\n'.join(block(catalog, signature) for signature in (
        'public static int GetResourceLevelNumber(', 'public Entry GetEntry(', 'public FTiledLevel Load(')))
    run_suite('LevelLoopCoreChecks', fixture, [
        'Assets/Foundation/Runtime/CoreLevelProgress.cs',
        'Assets/Foundation/Runtime/CoreLevelAttemptStore.cs',
        'Assets/Foundation/Runtime/OriginalConfigData.cs',
        'Assets/Foundation/Runtime/OriginalLevelSaveFlatBuffer.cs',
        'Assets/Foundation/Runtime/OriginalGenericSaveData.cs',
        'Assets/Foundation/Runtime/OriginalGameStateSaveData.cs',
    ], args.unity_editor / 'Data', output)

    # Reuse the established async/UI boundary fixture, replacing only its tests.
    recovery = read('Tools/OfflineSync/gameplay_recovery_fixture.cs.txt')
    setup = block(recovery, 'static WkyGameplayBridge Reset(')
    setup += '\n' + '\n'.join(block(recovery, method) for method in (
        'static void Ready(', 'static void Win(', 'static void Receive('))
    fixture = recovery[:recovery.index('static class Runner {')]
    fixture += Path(__file__).with_name('level_loop_flow_fixture.cs.txt').read_text(encoding='utf-8')
    fixture = fixture.replace('// RUNNER_SETUP', setup)
    flow = read('Assets/BizzaWZ/Common/Framework/Runtime/Module/FlowModule.cs')
    bridge = read('Assets/WkyGameplayIntegration/WkyGameplayBridge.cs')
    fixture = fixture.replace('// FLOW_CURRENT_LEVEL', re.search(r'public static int CurrentLevel\s*=>[^;]+;', flow)[0])
    fixture = fixture.replace('// FLOW_METHODS', '\n'.join(block(flow, method) for method in (
        'public static int BeginLevelLoading(', 'public static void ApplyServerProgress(',
        'private static void ResetLevelState(', 'public static void ReportFirstEffectiveAction(',
        'public static void GameStart(', 'public static void OpenGameWinPanel(',
        'private static void AdvanceCompletedLevel(', 'public static void CanShowGuide(')))
    fixture = fixture.replace('// BRIDGE_FIELDS', bridge[bridge.index('    internal const string SceneName'):bridge.index('    public bool BlocksGameplayInput')])
    fixture = fixture.replace('// BRIDGE_METHODS', '\n'.join(block(bridge, method) for method in (
        'private void ObserveAccountLevel(', 'public bool TryGetCurrentLevel(',
        'internal void PrepareInitialAttempt(', 'public void OnAttemptReady(',
        'internal async UniTask CompleteSceneHandoff(', 'private async UniTask CompleteSceneHandoffOnce(',
        'private void EnsureCurrentAttempt(', 'private void ReportTutorialCompletion(',
        'public void OnAttemptEnded(', 'private static LevelInfo ToLevelInfo(',
        'internal bool TryCaptureWinReport(', 'internal async UniTask ReloadAttempt(',
        'internal static UniTask ShowLoadFailure(', 'internal static void ReportProgress(',
        'private void PrepareNextAttempt(', 'private sealed class WinSettlement')))
    run_suite('LevelLoopFlowChecks', fixture, [], args.unity_editor / 'Data', output)


if __name__ == '__main__':
    main()
