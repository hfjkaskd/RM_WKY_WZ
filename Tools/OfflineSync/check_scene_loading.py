"""Check production scene startup/error paths after async EH simplification.

Only scene/UI/scheduler boundaries are substituted; exception classification,
release checks and once-only handoff methods are extracted unchanged.
"""
from pathlib import Path
import argparse
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
    folder = ROOT / 'Assets/BizzaWZ/Common/Framework/Runtime/GameMode'
    game = (folder / 'GameMode_GamePlay.cs').read_text(encoding='utf-8-sig')
    load = (folder / 'GameMode_Loading.cs').read_text(encoding='utf-8-sig')
    fixture = Path(__file__).with_name('scene_loading_fixture.cs.txt').read_text(encoding='utf-8')
    async_boundary = Path(__file__).with_name('gameplay_recovery_fixture.cs.txt').read_text(encoding='utf-8').split('enum NetworkReachability')[0]
    async_boundary = async_boundary.replace('public Task Value;', 'public static UniTask CompletedTask => new(Task.CompletedTask);\n    public Task Value;', 1)
    fixture = fixture.replace('// ASYNC_BOUNDARY', async_boundary)
    fixture = fixture.replace('// GAME_METHODS', '\n'.join(block(game, key) for key in (
        'private async UniTask LoadGameplayScene(', 'private bool CanContinueAsyncInit(', 'protected override void OnPreRelease(')))
    methods = '\n'.join(block(load, key) for key in (
        'private async UniTaskVoid RunLoadingSafe(', 'private void OnLoadingFinish(', 'public void StartGame(', 'protected override void OnRelease('))
    # Preserve its body; expose the fire-and-forget task as awaitable to the boundary harness.
    methods = methods.replace('async UniTaskVoid RunLoadingSafe(', 'async UniTask RunLoadingSafe(')
    fixture = fixture.replace('// LOAD_METHODS', methods)
    for source, signature in ((game, 'private async UniTask LoadGameplayScene('), (load, 'private async UniTaskVoid RunLoadingSafe(')):
        method = block(source, signature)
        assert 'catch (Exception error)' in method and ' when (' not in method
        assert 'await ' not in block(method, 'catch (Exception error)')
    output = ROOT / 'Temp/OfflineSyncChecks/scene-loading'
    output.mkdir(parents=True, exist_ok=True)
    generated = output / 'SceneLoadingChecks.cs'
    generated.write_text(fixture, encoding='utf-8')
    data = args.unity_editor / 'Data'
    shims = data / 'NetStandard/compat/2.1.0/shims/netfx'
    refs = [data / 'NetStandard/ref/2.1.0/netstandard.dll']
    refs += [shims / name for name in ('mscorlib.dll', 'System.dll', 'System.Core.dll')]
    exe = output / 'SceneLoadingChecks.exe'
    rsp = output / 'scene.rsp'
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
