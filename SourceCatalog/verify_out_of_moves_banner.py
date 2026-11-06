"""Compare actual banner coroutines with native-constructed motion segments.

The native sequence dispatcher and Unity coroutine/transform transport are
supplied. Original ARM64 sequence construction, quantization and easing run;
the production Play/SkipExit bodies compile unchanged against explicit ports.
"""
import contextlib
from hashlib import sha256
import io
import json
from pathlib import Path
import runpy
import struct
import subprocess
import sys

ROOT = Path(sys.argv[1]).resolve()
WORK = Path(sys.argv[2]).resolve()
WORK.mkdir(parents=True, exist_ok=True)
CURRENT = Path(sys.argv[3]).resolve() if len(sys.argv) > 3 else ROOT
HERE = Path(__file__).resolve().parent
BASELINE = '68120bbe9aa8b202a3ebf2d3b1386770151f5353'
baseline_root = WORK / 'baseline_inputs'
sys.dont_write_bytecode = True
sys.path.insert(0, str(ROOT.parent / 'tools/git_api'))
from dulwich.repo import Repo
with Repo(str(ROOT)) as repo:
    for name in ('Assets/Foundation/Runtime/CoreOutOfMovesBanner.cs', 'Assets/Resources/Foundation/Prefabs/CoreOutOfMovesBanner.prefab'):
        node = repo[BASELINE.encode()].tree
        for part in name.split('/'):
            node = repo[node][part.encode()][1]
        path = baseline_root / name
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(repo[node].data)
original_args = list(sys.argv)
sys.argv = [str(HERE / 'NativeOutOfMovesBannerReference.py'), str(ROOT), str(WORK / 'native')]
with contextlib.redirect_stdout(io.StringIO()):
    environment = runpy.run_path(str(HERE / 'NativeOutOfMovesBannerReference.py'))
sys.argv = original_args
environment = environment['call'].__globals__
u, r, call = environment['u'], environment['r'], environment['call']
bits, val, f32 = environment['bits'], environment['val'], environment['f32']
construction = environment['report']
moves = [s for s in construction['automatic'] if 'duration' in s]
intervals = [s['interval'] for s in construction['automatic'] if 'interval' in s]
start = construction['initial_position']
skip_move = construction['skipped'][0]
first_end = moves[0]['duration']
second_end = f32(first_end + moves[1]['duration'])
settled_end = f32(second_end + moves[2]['duration'])
exit_start = f32(settled_end + intervals[0])
end = f32(f32(exit_start + moves[3]['duration']) + intervals[1])
skip_end = f32(skip_move['duration'] + construction['skipped'][1]['interval'])
ease_cache, quant_cache = {}, {}


def quantize(raw):
    key = bits(raw)
    if key not in quant_cache:
        environment['raw_delta_bits'] = key
        call(0x318d1c4, 0)
        quant_cache[key] = val(u.reg_read(r.UC_ARM64_REG_S0))
    return quant_cache[key]


def ease(kind, time, duration, overshoot=1.):
    time = min(max(time, 0.), duration)
    key = (kind, bits(time), bits(duration), bits(overshoot))
    if key not in ease_cache:
        for i, value in enumerate((time, duration, overshoot, 0.)):
            u.reg_write(getattr(r, 'UC_ARM64_REG_S' + str(i)), bits(value))
        call(0x33d5f78, kind, 0)
        ease_cache[key] = val(u.reg_read(r.UC_ARM64_REG_S0))
    return ease_cache[key]


def lerp(a, b, ratio):
    return [f32(x + f32(f32(y - x) * ratio)) for x, y in zip(a, b)]


def automatic(time):
    if time < first_end:
        i, source, t = 0, start, time
    elif time < second_end:
        i, source, t = 1, moves[0]['target'], f32(time - first_end)
    elif time < settled_end:
        i, source, t = 2, moves[1]['target'], f32(time - second_end)
    elif time < exit_start:
        return moves[2]['target']
    else:
        i, source, t = 3, moves[2]['target'], f32(time - exit_start)
    move = moves[i]
    return lerp(source, move['target'], ease(move['ease'], t, move['duration'], move.get('overshoot', 1.)))


names = ('enterDuration', 'overshootDuration', 'settleDuration', 'holdDuration', 'exitDuration',
         'exitPause', 'enterOffset', 'overshootOffset', 'exitOvershoot', 'skipDuration')
parameter_sets = {}
for mode, root in (('baseline', baseline_root), ('fixed', CURRENT)):
    prefab = root / 'Assets/Resources/Foundation/Prefabs/CoreOutOfMovesBanner.prefab'
    lines = prefab.read_text(encoding='utf-8-sig').splitlines()
    parameter_sets[mode] = {name: next(s.strip().split(':', 1)[1].strip() for s in lines if s.startswith('  ' + name + ':')) for name in names}

scenarios = []
for label, cadence in (
    ('60fps', [1 / 60]), ('30fps', [1 / 30]), ('120fps', [1 / 120]),
    ('alternating', [0., .00005, .010051, .01679, .03337, .071013])):
    for skip_at in (None, .05, .25, .42, 1.1, 1.6):
        scenarios.append((label, [f32(v) for v in cadence], skip_at, 700))
scenarios.append(('subquantum', [f32(.00005)], None, 60))
commands = {mode: [] for mode in parameter_sets}
expected = []
labels = []


def record(label, command, position, completed, enabled, sounds):
    for mode in commands:
        if command == 'I': commands[mode].append('I ' + ' '.join(parameter_sets[mode][name] for name in names))
        else: commands[mode].append(command)
    labels.append(label)
    expected.append((list(position), completed, int(enabled), list(sounds)))


for name, cadence, skip_at, maximum in scenarios:
    label = name + '_skip_' + str(skip_at)
    elapsed = raw_elapsed = 0.
    skipped = done = False
    enabled = True
    sounds = [258]
    position = list(start)
    skip_source = None
    record(label, 'I', position, 0, True, sounds)
    for frame in range(maximum):
        raw = cadence[frame % len(cadence)]
        delta = quantize(raw)
        raw_elapsed = f32(raw_elapsed + raw)
        elapsed = f32(elapsed + delta)
        if skipped:
            position = lerp(skip_source, skip_move['target'], ease(skip_move['ease'], elapsed, skip_move['duration']))
            done = elapsed >= skip_end
            if done: position = list(skip_move['target'])
        else:
            position = automatic(elapsed)
            if elapsed >= exit_start:
                enabled = False
                if len(sounds) == 1: sounds.append(259)
            done = elapsed >= end
            if done: position = list(moves[-1]['target'])
        record(label, 'T ' + str(bits(raw)) + ' ' + str(bits(delta)), position, int(done), enabled, sounds)
        if skip_at is not None and not skipped and raw_elapsed >= skip_at:
            if enabled:
                skipped = True
                enabled = False
                skip_source = list(position)
                elapsed = 0.
            record(label, 'K', position, int(done), enabled, sounds)
            record(label, 'K', position, int(done), enabled, sounds)
            skip_at = None
        if done: break

mono = Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
files = {}
reports = {}
for mode, root in (('baseline', baseline_root), ('fixed', CURRENT)):
    source_path = root / 'Assets/Foundation/Runtime/CoreOutOfMovesBanner.cs'
    text = source_path.read_text(encoding='utf-8-sig')
    # Visibility-only adaptation for serialized fixture parameters. Coroutine
    # bodies and their scalar/vector math remain byte-for-byte as source text.
    text = text.replace('[SerializeField] private', '[SerializeField] internal')
    path = WORK / (mode + '_CoreOutOfMovesBanner.cs')
    path.write_text(text, encoding='utf-8', newline='\n')
    exe = WORK / (mode + '_Banner.exe')
    command = [str(mono.parent / 'NetCoreRuntime/dotnet.exe'), str(mono.parent / 'DotNetSdkRoslyn/csc.dll'),
        '-nologo', '-nostdlib+', *['-r:' + str(mono / 'lib/mono/4.5' / p) for p in ('mscorlib.dll', 'System.dll', 'System.Core.dll')],
        '-out:' + str(exe), str(path), str(HERE / 'BannerMotionHarness.cs')]
    result = subprocess.run(command, capture_output=True, text=True, encoding='utf-8', creationflags=subprocess.CREATE_NO_WINDOW)
    assert result.returncode == 0, result.stdout + result.stderr
    result = subprocess.run([str(mono / 'bin/mono.exe'), str(exe)], input='\n'.join(commands[mode]) + '\n',
        capture_output=True, text=True, encoding='utf-8', creationflags=subprocess.CREATE_NO_WINDOW)
    assert result.returncode == 0, result.stdout + result.stderr
    lines = result.stdout.lstrip('\ufeff').splitlines()
    assert len(lines) == len(expected), (mode, len(lines), len(expected), result.stdout[:500])
    mismatches = []
    max_error = 0.
    for i, (line, state) in enumerate(zip(lines, expected)):
        p, completed, enabled, sound = line.split('|')
        position = list(map(float, p.split(',')))
        clips = [int(s) for s in sound.split(',')] if sound else []
        error = max(abs(a - b) for a, b in zip(position, state[0]))
        max_error = max(max_error, error)
        if error > 3e-6 or int(completed) != state[1] or int(enabled) != state[2] or clips != state[3]:
            mismatches.append({'command': i, 'scenario': labels[i], 'input': commands[mode][i],
                'actual': line, 'expected': state, 'position_error': error})
    reports[mode] = {'passed': not mismatches, 'mismatch_count': len(mismatches),
        'max_position_error': max_error, 'first_mismatches': mismatches[:12],
        'source_sha256': sha256(source_path.read_bytes()).hexdigest(), 'parameters': parameter_sets[mode]}
    (WORK / (mode + '_actual.txt')).write_text(result.stdout, encoding='utf-8')
    (WORK / (mode + '_commands.txt')).write_text('\n'.join(commands[mode]) + '\n', encoding='utf-8')
assert not reports['baseline']['passed']
report = {'passed': reports['fixed']['passed'], 'baseline_commit': BASELINE, 'scenarios': len(scenarios), 'commands': len(expected),
    'native_ease_evaluations': len(ease_cache), 'native_quantization_inputs': len(quant_cache),
    'native_construction': construction, 'results': reports, 'scope': __doc__.strip()}
(WORK / 'banner_motion_comparison.json').write_text(json.dumps(report, indent=2) + '\n')
print(json.dumps({'passed': report['passed'], 'scenarios': len(scenarios), 'commands': len(expected),
    'native_ease_evaluations': len(ease_cache), 'native_quantization_inputs': len(quant_cache),
    'baseline_mismatches': reports['baseline']['mismatch_count'], 'fixed_mismatches': reports['fixed']['mismatch_count'],
    'fixed_max_position_error': reports['fixed']['max_position_error']}, indent=2))
assert report['passed'], reports['fixed']
