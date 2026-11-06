"""Compare complete production hint-manager animation methods with native traces."""
from hashlib import sha256
from pathlib import Path
import json
import struct
import subprocess
import sys

ROOT = Path(sys.argv[1]).resolve()
WORK = Path(sys.argv[2]).resolve()
SOURCE = Path(sys.argv[3]).resolve() if len(sys.argv) > 3 else ROOT / 'Assets/Foundation/Runtime/OriginalMatchHintManager.cs'
LABEL = sys.argv[4] if len(sys.argv) > 4 else 'canonical'
assert LABEL.replace('_', '').isalnum()
SCRIPTS = Path(__file__).resolve().parent
WORK.mkdir(parents=True, exist_ok=True)
OUT = WORK / LABEL
OUT.mkdir(exist_ok=True)
native_path = WORK / 'native_hint_motion.json'
if len(sys.argv) <= 3:
    native_path = WORK / 'native/native_hint_motion.json'
    reference = subprocess.run([sys.executable, str(SCRIPTS / 'NativeHintMotionReference.py'),
        str(ROOT), str(native_path.parent)], capture_output=True, timeout=120,
        creationflags=subprocess.CREATE_NO_WINDOW | subprocess.BELOW_NORMAL_PRIORITY_CLASS)
    assert reference.returncode == 0, (reference.stdout + reference.stderr).decode(errors='replace')
native = json.loads(native_path.read_text(encoding='utf-8'))
raw = SOURCE.read_text(encoding='utf-8-sig')
needle = 'public sealed class OriginalMatchHintManager : IDisposable'
assert raw.count(needle) == 1
(OUT / 'ProductionManager.cs').write_text(raw.replace(needle,
    'public sealed partial class OriginalMatchHintManager : IDisposable'), encoding='utf-8')
modern = 'PrepareForward' in raw
phase = '(int)phase' if modern else '''phase switch {
    Phase.None => 0, Phase.Forward => 2, Phase.Return => 2, Phase.Pause => 5,
    Phase.Fade => 6, Phase.StopRequested => 7, Phase.Stopping => 2, Phase.StopComplete => 8,
    _ => throw new InvalidOperationException() }'''
duration = 'phaseDuration' if modern else '''phase switch {
    Phase.Forward => active.Type == Kind.Tap ? timing.tapPulseSeconds : active.Type == Kind.Combo ? timing.comboForwardSeconds : timing.forwardSeconds,
    Phase.Return => active.Type == Kind.Tap ? timing.tapPulseSeconds : active.Type == Kind.Combo ? timing.comboReturnSeconds : timing.returnSeconds,
    Phase.Stopping => timing.stopSeconds, _ => 0f }'''
bridge = '''using System;
using UnityEngine;
namespace Whitebox.Foundation {
public sealed partial class OriginalMatchHintManager {
    public static OriginalMatchHintManager FixtureCreate(string kind, int count, int dx, int dy, float[] maxima, float[] brightness) {
        FixtureTrace.Views.Clear(); FixtureTrace.Events.Clear();
        var board = new OriginalInitialBoardView();
        var game = new OriginalPlayableBoard();
        var timing = new OriginalMatchHintTiming {
            stableFrames=60, initialCycleFrames=120, moveDistance=.1f,
            forwardSeconds=.18f, returnSeconds=.23f, pauseSeconds=.2f, repeatSeconds=3f,
            comboDistance=.05f, comboForwardSeconds=.13f, comboReturnSeconds=.17f,
            tapScale=1.05f, tapPulseSeconds=.19f, tapPauseSeconds=.4f, stopSeconds=.1f,
            tapHintLevelCutoff=19, tapHintTapLimit=5 };
        var cells = new OriginalGridCell[count];
        for (int i=0; i<count; i++) {
            var view = new OriginalMatchItemSpriteView { Maximum=maxima[i], Brightness=brightness[i] };
            FixtureTrace.Views.Add(view);
            var cell = new OriginalGridCell { X=2+i, Y=3 };
            var item = kind == "match" ? (OriginalBoardItem)new OriginalMatchBoardItem() : new OriginalTntBoardItem();
            item.SwapView=view; item.Mediator.CurrentCell=new HintCurrentCell { Owner=cell };
            cell.Mediation.CurrentItem=item; cells[i]=cell;
        }
        var from=cells[0]; OriginalGridCell to=null;
        var group=new OriginalResolvedMatchGroup();
        if (kind == "match") {
            to=new OriginalGridCell { X=from.X+dx, Y=from.Y+dy };
            group.Cells.Add(to);
            for (int i=1; i<count; i++) group.Cells.Add(cells[i]);
        } else if (kind == "combo") { to=cells[1]; to.X=from.X+dx; to.Y=from.Y+dy; }
        var manager=new OriginalMatchHintManager(board,game,new CoreLevelProgress(),"fixture",timing);
        manager.stableAt=0;
        var hint=kind == "match" ? new Hint(from,to,group,0) : new Hint(from,to,2,1,0);
        manager.candidates.Add(hint); manager.index=0; manager.StartAnimation(hint);
        FixtureTrace.Events.Clear();
        return manager;
    }
    public void FixtureStep(float delta, bool stop) { if (stop) StopProcess(); AdvanceAnimation(delta); }
    public string FixtureState(string kind) => FixtureTrace.State(PHASE,pulses,bursts,phaseElapsed,DURATION,nextAt,kind);
}}
'''.replace('PHASE', phase).replace('DURATION', duration)
(OUT / 'FixtureBridge.cs').write_text(bridge, encoding='utf-8')
UNITY = Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data')
MONO = UNITY / 'MonoBleedingEdge'
references = MONO / 'lib/mono/4.8-api'
command = [str(UNITY / 'NetCoreRuntime/dotnet.exe'), str(UNITY / 'DotNetSdkRoslyn/csc.dll'),
    '-nologo', '-langversion:9', '-target:exe', '-out:' + str(OUT / 'motion.exe'),
    *['-r:' + str(references / name) for name in ('mscorlib.dll','System.dll','System.Core.dll')],
    str(SCRIPTS / 'HintMotionHarness.cs'), str(OUT / 'ProductionManager.cs'), str(OUT / 'FixtureBridge.cs')]
process = subprocess.run(command, capture_output=True, timeout=60, creationflags=subprocess.CREATE_NO_WINDOW)
(OUT / 'compile.log').write_bytes(process.stdout + process.stderr)
assert process.returncode == 0, (process.stdout + process.stderr).decode(errors='replace')

def f32(v): return struct.unpack('<f', struct.pack('<f', v))[0]
def numbers(values): return ','.join(format(v, '.9g') for v in values)
lines = []
for case in native['cases']:
    maxima = [f32(1.2 + .1 * i) for i in range(case['count'])]
    brightness = [f32(m * (.15 if case['nonzero_highlight'] else 0)) for m in maxima]
    lines.append('|'.join(['BEGIN', case['name'], case['kind'], str(case['count']),
        str(int(case['direction'][0])), str(int(case['direction'][1])), numbers(maxima), numbers(brightness)]))
    for state in case['states']:
        lines.append('|'.join(['STEP', format(state['delta'], '.9g'), format(state['time'], '.9g'), str(int(state['stop']))]))
data = ('\n'.join(lines) + '\n').encode('utf-8')
(OUT / 'inputs.txt').write_bytes(data)
process = subprocess.run([str(MONO / 'bin/mono.exe'), str(OUT / 'motion.exe')],
    input=data, capture_output=True, timeout=60, creationflags=subprocess.CREATE_NO_WINDOW)
(OUT / 'actual.jsonl').write_bytes(process.stdout)
(OUT / 'runtime_stderr.log').write_bytes(process.stderr)
assert process.returncode == 0, process.stderr.decode(errors='replace')
actual = [json.loads(line) for line in process.stdout.decode('utf-8-sig').splitlines()]
assert len(actual) == sum(len(c['states']) for c in native['cases'])
TOLERANCE = 3e-6
differences, observed_differences, samples, max_error, offset, excluded_schedule_states = [], [], 0, 0., 0, 0

def compare(left, right, at, errors):
    global samples, max_error
    if isinstance(left, str) or left is None:
        if left != right: errors.append({'at': at, 'expected': left, 'actual': right})
    elif isinstance(left, (float, int)):
        samples += 1
        # Mono's float.ToString("R") is a binary32 round-trip spelling, not
        # the exact decimal expansion of the stored value. Compare its float.
        error = abs(left - (f32(right) if isinstance(left, float) else right))
        max_error = max(max_error, error)
        if error > TOLERANCE: errors.append({'at': at, 'expected': left, 'actual': right, 'error': error})
    elif isinstance(left, list):
        if len(left) != len(right): errors.append({'at': at + '.length', 'expected': len(left), 'actual': len(right)})
        else:
            for i,(a,b) in enumerate(zip(left,right)): compare(a,b,at + '.' + str(i),errors)
    else: raise AssertionError(type(left))

for case in native['cases']:
    for expected in case['states']:
        observed = actual[offset]; offset += 1
        errors, visible = [], []
        for field in ('positions', 'scales', 'brightness', 'events'):
            compare(expected[field], observed[field], field, visible)
        if case['stop_at'] < 0 or expected['step'] < case['stop_at']:
            compare(expected['next_at'], observed['next_at'], 'next_at', visible)
        else:
            # StopProcess resets the manager's search/repeat gate. The native
            # oracle executes animation owners only, whose old completionTime
            # field survives GentleStop. These are different ownership scopes.
            excluded_schedule_states += 1
        for field in ('phase', 'pulses', 'bursts', 'elapsed', 'duration'):
            compare(expected[field], observed[field], field, errors)
        if visible: observed_differences.append({'case': case['name'], 'step': expected['step'], 'differences': visible})
        if errors or visible: differences.append({'case': case['name'], 'step': expected['step'], 'differences': visible + errors})
report = {'passed': not differences, 'source': SOURCE.as_posix(), 'source_sha256': sha256(SOURCE.read_bytes()).hexdigest(),
    'scenarios': len(native['cases']), 'states': offset, 'numeric_samples': samples,
    'tolerance': TOLERANCE, 'maximum_absolute_error': max_error,
    'mismatched_states': len(differences), 'observable_mismatched_states': len(observed_differences),
    'native_calls': native['native_calls'], 'differences': differences,
    'binary32_roundtrip_normalized': True,
    'post_interrupt_manager_schedule_states_outside_animation_oracle': excluded_schedule_states,
    'scope': 'The complete production manager is compiled; only its declaration gains partial for a strongly typed fixture bridge. No method bodies are edited or called by reflection. Identical explicit root/highlight/clock/particle ports and prepared input match the native oracle. Repeat scheduling is compared before interruption and in every natural cycle; after StopProcess, its cleared search gate is not equated with the native animation-only completionTime field. Search, native Start/PrepareHintables, actual Unity roots/rendering/audio, scheduling and external root takeover are outside this comparison.'}
(OUT / 'comparison.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
print(json.dumps({k:v for k,v in report.items() if k != 'differences'}, indent=2))
if differences:
    print(json.dumps({'first_difference': differences[0]}, indent=2))
    raise SystemExit(1)
