"""Run production match-removal, first-action persistence and post-preload gift gates.

The rendering/HTTP/persistence backends are isolated; no player data or service is used.
"""
from pathlib import Path
import argparse
import re
import subprocess
import sys

sys.dont_write_bytecode = True
from source_blocks import block

ROOT = Path(__file__).resolve().parents[2]


def read(path):
    return (ROOT / path).read_text(encoding='utf-8-sig')


def expression(source, name):
    return re.search(r'(?:public|private) (?:static )?bool ' + name + r'(?:\(\))?\s*=>[^;]+;', source)[0]


def main():
    sys.stdout.reconfigure(encoding='utf-8')
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--unity-editor', type=Path, required=True)
    args = parser.parse_args()
    core = read('Assets/Foundation/Runtime/CoreGameFlow.cs')
    services = read('Assets/BizzaWZ/Final/Framework/Runtime/Module/Teach/Teach01UnityServices.cs')
    fixture = Path(__file__).with_name('effective_action_fixture.cs.txt').read_text(encoding='utf-8')
    # Share only the deterministic async scheduler, never gameplay decisions.
    async_boundary = Path(__file__).with_name('gameplay_recovery_fixture.cs.txt').read_text(encoding='utf-8').split('enum NetworkReachability')[0]
    fixture = fixture.replace('// ASYNC_BOUNDARY', async_boundary)
    fixture = fixture.replace('// EXPLOSION', read('Assets/Foundation/Runtime/OriginalMatchGroupExplosion.cs').replace('using System;', '').replace('using System.Collections.Generic;', '').replace('using UnityEngine;', ''))
    fixture = fixture.replace('// CORE_DECISIONS', '\n'.join([
        expression(core, 'CanRecordEffectiveAction'), expression(core, 'CanRecordSynthesis'),
        expression(core, 'CanShowAutomaticPopup'), block(core, 'private void RecordEffectiveAction(')]))
    fixture = fixture.replace('// BRIDGE_METHOD', block(read('Assets/WkyGameplayIntegration/WkyGameplayBridge.Synthesis.cs'), 'public void OnEffectiveMatch('))
    fixture = fixture.replace('// FLOW_METHOD', block(read('Assets/BizzaWZ/Common/Framework/Runtime/Module/FlowModule.cs'), 'public static void ReportFirstEffectiveAction('))
    fixture = fixture.replace('// TEACH_METHOD', block(read('Assets/BizzaWZ/Final/Framework/Runtime/Module/Teach/Teach01Module.cs'), 'public void OnFirstMatch('))
    fixture = fixture.replace('// SERVICE_METHODS', '\n'.join([expression(services, 'CanShowGift'), block(services, 'public async UniTask<object> OpenGift(')]))
    fixture = fixture.replace('// PUBLIC_GATE', expression(read('Assets/Foundation/Runtime/RoyalMatchSessionBridge.cs'), 'CanShowAutomaticPopup'))
    # Verify real subscription ownership in addition to executing the unchanged decision methods.
    assert 'game.BindEffectiveActionObserver(CanRecordEffectiveAction, RecordEffectiveAction);' in block(core, 'internal void Initialize(')
    assert 'game?.BindEffectiveActionObserver(null, null);' in block(core, 'public void Release(')
    board = read('Assets/Foundation/Runtime/OriginalPlayableBoard.cs')
    assert 'explosion.BindEffectiveActionObserver(canRecord, record);' in board
    output = ROOT / 'Temp/OfflineSyncChecks/effective-action'
    output.mkdir(parents=True, exist_ok=True)
    generated = output / 'EffectiveActionChecks.cs'
    generated.write_text(fixture, encoding='utf-8')
    data = args.unity_editor / 'Data'
    shims = data / 'NetStandard/compat/2.1.0/shims/netfx'
    refs = [data / 'NetStandard/ref/2.1.0/netstandard.dll']
    refs += [shims / name for name in ('mscorlib.dll', 'System.dll', 'System.Core.dll')]
    exe = output / 'EffectiveActionChecks.exe'
    rsp = output / 'effective.rsp'
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
