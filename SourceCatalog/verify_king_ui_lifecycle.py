from hashlib import sha256
import json
from pathlib import Path
import subprocess
import sys

CATALOG = Path(__file__).resolve().parent
ROOT = Path(sys.argv[2]).resolve() if len(sys.argv) > 2 else CATALOG.parent
WORK = Path(sys.argv[1]).resolve()
RUNTIME = Path(sys.argv[3]).resolve() if len(sys.argv) > 3 else ROOT / 'Assets/Foundation/Runtime'
WORK.mkdir(parents=True, exist_ok=True)
EDITOR = Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data')
flags = subprocess.CREATE_NO_WINDOW if sys.platform == 'win32' else 0
subprocess.run([sys.executable, str(CATALOG / 'KingUiIdleReference.py'), str(WORK), str(ROOT), str(RUNTIME)],
               check=True, creationflags=flags)
sources = [CATALOG / 'KingUiStateHarness.cs', RUNTIME / 'OriginalKingUiState.cs',
           ROOT / 'Assets/Foundation/Runtime/OriginalMoveManager.cs']
subprocess.run([str(EDITOR / 'NetCoreRuntime/dotnet.exe'), str(EDITOR / 'DotNetSdkRoslyn/csc.dll'),
    '-nologo', '-nostdlib+', *['-r:' + str(EDITOR / 'MonoBleedingEdge/lib/mono/4.5' / name)
       for name in ('mscorlib.dll', 'System.dll', 'System.Core.dll')],
    '-out:' + str(WORK / 'KingUiStateHarness.exe'), *map(str, sources)], check=True, creationflags=flags)
subprocess.run([str(EDITOR / 'MonoBleedingEdge/bin/mono.exe'), str(WORK / 'KingUiStateHarness.exe'),
    str(WORK / 'commands.txt'), str(WORK / 'managed_actual.txt')], check=True, creationflags=flags)
expected, actual = [(WORK / name).read_text().splitlines() for name in ('native_expected.txt', 'managed_actual.txt')]
assert len(expected) == len(actual)
bad = [{'line':i, 'command':c, 'expected':a, 'actual':b}
       for i,(c,a,b) in enumerate(zip((WORK / 'commands.txt').read_text().splitlines(),expected,actual)) if a != b]
idle = json.loads((WORK / 'idle_comparison.json').read_text())
report = {'passed':not bad and idle['passed'], 'state_commands':len(actual), 'idle_commands':idle['commands'],
          'mismatches':bad[:15], 'native_reference':json.loads((WORK / 'native_reference.json').read_text()),
          'idle_reference':idle, 'source_files':{p.as_posix():sha256(p.read_bytes()).hexdigest() for p in sources},
          'scope':'Typed state, cached move resets, accepted-happy accounting, separate delayed resumes, and idle queue command/wait/stop behavior. Engine coroutine scheduling, animation ports and shuffle permutations are supplied in offline comparison; real Unity owner execution is verified separately.'}
(WORK / 'king_ui_lifecycle_comparison.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({k:report[k] for k in ('passed','state_commands','idle_commands','mismatches')},indent=2))
assert report['passed']
