from hashlib import sha256
import json
from pathlib import Path
import random
import struct
import subprocess
import KingUiNativeReference as n

WORK = n.WORK
SOURCE = (Path(n.sys.argv[3]).resolve() if len(n.sys.argv) > 3 else n.ROOT / 'Assets/Foundation/Runtime') / 'OriginalKingVisual.cs'
source = SOURCE.read_text()
start = source.index('        private IEnumerator IdleCycle(')
brace = source.index('{', start)
depth = 1
end = brace + 1
while depth:
    depth += (source[end] == '{') - (source[end] == '}')
    end += 1
method = source[start:end].replace('private IEnumerator IdleCycle', 'public IEnumerator IdleCycle', 1)
header = '''using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
namespace UnityEngine {
 public class Coroutine {}
 public sealed class WaitForSeconds { public float Seconds; public WaitForSeconds(float value) { Seconds=value; } }
}
namespace Whitebox.Foundation {
 using UnityEngine;
 internal sealed class StatePort { public bool StopAnimationsCycle; }
 internal sealed class IdleHarness {
  StatePort state = new StatePort();
  int[] idleClips,connectClips;
  float idleStepDelay=5f,idleRepeatDelay=50f;
  Coroutine idleCycle;
  IEnumerator routine;
  readonly List<string> calls=new List<string>();
  void Shuffle(int[] array) { Array.Reverse(array); }
  void SetClip(int next) { calls.Add("S"+next+":0"); }
  void AddClip(int next,bool loop) { calls.Add("A"+next+":"+(loop?1:0)); }
'''
footer = '''
  public static void Main(string[] args) {
   var owner=new IdleHarness();
   using(var output=new StreamWriter(args[1])) foreach(string line in File.ReadAllLines(args[0])) {
    string[] c=line.Split(' '); owner.calls.Clear(); bool result=false; float wait=0;
    if(c[0]=="R") {
     owner.idleClips=new[]{11,12,13};owner.connectClips=new[]{31,32};owner.idleCycle=new Coroutine();
     owner.state.StopAnimationsCycle=c[2]=="1";owner.routine=owner.IdleCycle(c[1]=="1");
    } else if(c[0]=="S") owner.state.StopAnimationsCycle=c[1]=="1";
    else { result=owner.routine.MoveNext();if(result)wait=((WaitForSeconds)owner.routine.Current).Seconds; }
    output.WriteLine((result?1:0)+","+(owner.state.StopAnimationsCycle?1:0)+","+(owner.idleCycle==null?1:0)+","+wait.ToString(System.Globalization.CultureInfo.InvariantCulture)+","+string.Join(";",owner.calls));
   }
  }
 }
}
'''
(WORK / 'IdleHarness.cs').write_text(header + method + footer)

idle_array = 0x10009000
iterator = 0x10300000
n.put(n.king + 0x28, idle_array)
n.put(n.king + 0x90, 0x1000a000)

def hook(uc, address, size, unused):
    if address == 0x1e18f34: n.ret(0) # No first-100 table-game board.
    elif address == 0x260e574:
        array = n.x(1)
        count = n.get(array + 0x18, 4)
        values = [n.get(array + 0x20 + i * 8) for i in range(count)]
        for i, value in enumerate(reversed(values)): n.put(array + 0x20 + i * 8, value)
        n.ret()
n.u.hook_add(n.UC_HOOK_CODE, hook)
rng = random.Random(19052)
commands, expected = [], []
for _ in range(100):
    case = [('R', rng.randrange(2), rng.randrange(2)), ('U',)]
    case += [('S', rng.randrange(2)) if rng.random() < .25 else ('U',) for _ in range(14)]
    for command in case:
        n.track_calls.clear()
        result, wait = 0, 0
        if command[0] == 'R':
            n.put(n.king + 0xb8, 42)
            n.put(n.king + 0x8c, command[2], 1)
            n.put(iterator + 0x10, 0, 4)
            n.put(iterator + 0x20, n.king)
            n.put(iterator + 0x28, command[1], 1)
            for array, values in ((idle_array, [11, 12, 13]), (n.connects, [31, 32])):
                n.put(array + 0x18, len(values), 4)
                for i, value in enumerate(values): n.put(array + 0x20 + i * 8, value)
        elif command[0] == 'S': n.put(n.king + 0x8c, command[1], 1)
        else:
            result = n.call(0x18ca31c, iterator, 0)
            if result:
                wait = struct.unpack('<f', struct.pack('<I', n.get(n.get(iterator + 0x18) + 0x10, 4)))[0]
        calls = ';'.join(('S' if a == 0x2b79988 else 'A') + str(clip) + ':' + str(loop)
                         for a, clip, loop in n.track_calls)
        expected.append(','.join(map(str, [result, n.get(n.king + 0x8c, 1), int(n.get(n.king + 0xb8) == 0), int(wait)])) + ',' + calls)
        commands.append(' '.join(map(str, command)))
(WORK / 'idle_commands.txt').write_text('\n'.join(commands) + '\n')
(WORK / 'idle_native_expected.txt').write_text('\n'.join(expected) + '\n')
EDITOR = Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data')
subprocess.run([str(EDITOR / 'NetCoreRuntime/dotnet.exe'), str(EDITOR / 'DotNetSdkRoslyn/csc.dll'),
    '-nologo', '-nostdlib+', *['-r:' + str(EDITOR / 'MonoBleedingEdge/lib/mono/4.5' / name)
       for name in ('mscorlib.dll', 'System.dll', 'System.Core.dll')],
    '-out:' + str(WORK / 'IdleHarness.exe'), str(WORK / 'IdleHarness.cs')], check=True, creationflags=subprocess.CREATE_NO_WINDOW)
subprocess.run([str(EDITOR / 'MonoBleedingEdge/bin/mono.exe'), str(WORK / 'IdleHarness.exe'),
    str(WORK / 'idle_commands.txt'), str(WORK / 'idle_managed_actual.txt')], check=True, creationflags=subprocess.CREATE_NO_WINDOW)
actual = (WORK / 'idle_managed_actual.txt').read_text().splitlines()
assert len(actual) == len(expected)
bad = [{'line':i,'command':c,'expected':a,'actual':b}
       for i,(c,a,b) in enumerate(zip(commands,expected,actual)) if a != b]
report = {'passed':not bad,'commands':len(commands),'mismatches':bad[:12],
          'production_source_sha256':sha256(SOURCE.read_bytes()).hexdigest(),
          'native_move_next_calls':sum(c == 'U' for c in commands),
          'method':'Exact production IdleCycle body compiled with state/track/shuffle/wait ports; actual ARM64 IdleCycle.MoveNext and GetNextIdleLoop execute against equivalent ports. Shuffle permutations and engine scheduling are supplied.'}
(WORK / 'idle_comparison.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report,indent=2))
assert not bad
