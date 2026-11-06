from hashlib import sha256
import json
from pathlib import Path
import subprocess
import sys

ROOT = Path(sys.argv[1]).resolve()
WORK = Path(sys.argv[2]).resolve()
SOURCE = ROOT / 'Assets/Foundation/Runtime/OriginalRuntimeShuffle.cs'
LABEL = 'canonical'
SCRIPTS = Path(__file__).resolve().parent
WORK.mkdir(parents=True, exist_ok=True)
OUT = WORK / LABEL
OUT.mkdir(exist_ok=True)
reference = subprocess.run([sys.executable, str(SCRIPTS / 'NativeShuffleMotionReference.py'), str(ROOT), str(WORK / 'native')], capture_output=True, timeout=180, creationflags=subprocess.CREATE_NO_WINDOW)
assert reference.returncode == 0, reference.stderr.decode(errors='replace')
native = json.loads((WORK / 'native/native_shuffle_motion.json').read_text(encoding='utf-8'))
raw = SOURCE.read_text(encoding='utf-8-sig')
adapted = raw.replace('private readonly ', 'internal readonly ')
assert adapted.count('private int count, recolorOriginalIndex') == 1
adapted = adapted.replace('private int count, recolorOriginalIndex', 'internal int count, recolorOriginalIndex')
assert adapted.count('private IEnumerator AnimateShuffle()') == 1
adapted = adapted.replace('private IEnumerator AnimateShuffle()', 'internal IEnumerator AnimateShuffle()')
(OUT / 'OriginalRuntimeShuffle.cs').write_text(adapted, encoding='utf-8')
MONO = Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
command = [str(MONO / 'bin/mono.exe'), str(MONO / 'lib/mono/4.5/mcs.exe'),
           '-langversion:latest', '-out:' + str(OUT / 'motion.exe'),
           str(SCRIPTS / 'ShuffleMotionHarness.cs'), str(OUT / 'OriginalRuntimeShuffle.cs')]
process = subprocess.run(command, capture_output=True, timeout=45, creationflags=subprocess.CREATE_NO_WINDOW)
(OUT / 'compile.log').write_bytes(process.stdout + process.stderr)
assert process.returncode == 0, (process.stdout + process.stderr).decode(errors='replace')
lines = []
for case in native['cases']:
    positions = ';'.join(','.join(format(v, '.9g') for v in p['start'] + p['end']) for p in case['positions'])
    lines.append('#'.join([case['name'], str(case['count']), str(int(case['recolor'])),
                          ','.join(format(v, '.9g') for v in case['deltas']), positions]))
data = ('\n'.join(lines) + '\n').encode('utf-8')
(OUT / 'inputs.txt').write_bytes(data)
process = subprocess.run([str(MONO / 'bin/mono.exe'), str(OUT / 'motion.exe')],
                         input=data, capture_output=True, timeout=60, creationflags=subprocess.CREATE_NO_WINDOW)
(OUT / 'actual.jsonl').write_bytes(process.stdout)
assert process.returncode == 0, process.stderr.decode(errors='replace')
actual = [json.loads(line) for line in process.stdout.decode('utf-8-sig').splitlines()]
assert len(actual) == len(native['cases'])
differences, max_error, samples, changed_states = [], 0.0, 0, 0
TOLERANCE = 3e-6

def compare(left, right, location, errors):
    global max_error, samples
    if isinstance(left, bool) or isinstance(left, str) or left is None:
        if left != right: errors.append({'at': location, 'expected': left, 'actual': right})
    elif isinstance(left, (int, float)):
        error = abs(left - right)
        samples += 1
        max_error = max(max_error, error)
        if error > TOLERANCE: errors.append({'at': location, 'expected': left, 'actual': right, 'error': error})
    elif isinstance(left, list):
        if len(left) != len(right): errors.append({'at': location + '.length', 'expected': len(left), 'actual': len(right)})
        else:
            for i, (a, b) in enumerate(zip(left, right)): compare(a, b, location + '.' + str(i), errors)
    else: raise AssertionError(type(left))

for expected, observed in zip(native['cases'], actual):
    assert expected['name'] == observed['name']
    if len(expected['states']) != len(observed['states']):
        differences.append({'case': expected['name'], 'state_count': [len(expected['states']), len(observed['states'])]})
    for left, right in zip(expected['states'], observed['states']):
        errors = []
        for field in ('step', 'alive', 'events', 'positions'):
            compare(left[field], right[field], field, errors)
        if errors:
            changed_states += 1
            differences.append({'case': expected['name'], 'step': left['step'],
                                'native_elapsed': left['elapsed'], 'differences': errors})

report = {'passed': not differences, 'source': SOURCE.as_posix(), 'source_sha256': sha256(SOURCE.read_bytes()).hexdigest(),
          'label': LABEL, 'scenarios': len(actual), 'states': sum(len(c['states']) for c in actual),
          'numeric_samples': samples, 'tolerance': TOLERANCE, 'maximum_absolute_error': max_error,
          'mismatched_states': changed_states, 'differences': differences,
          'native_steps': native['native_steps'], 'native_ease_calls': native['native_ease_calls'],
          'native_vector_calls': native['native_vector_calls'],
          'scope': 'Compile the full production OriginalRuntimeShuffle class with field/method visibility-only adaptation. Typed scene/view/clock/notification ports supply the same data as the native harness. Compare the actual coroutine yielded/completed states, events and all positions. Local compiler-generated elapsed is not read or reflected. Wait scheduling, real engine transforms and permutation/search remain outside this portable comparison.'}
(OUT / 'comparison.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
print(json.dumps({k: v for k, v in report.items() if k != 'differences'}, indent=2))
if differences:
    print(json.dumps({'first_difference': differences[0]}, indent=2))

if not report['passed']: raise SystemExit(1)
