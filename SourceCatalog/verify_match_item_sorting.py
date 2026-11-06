"""Compare match renderer ordering and animation sorting locks with native code."""
import sys
sys.dont_write_bytecode = True
import verify_board_layout as b
import json, random, subprocess, tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import UC_ARM64_REG_X0, UC_ARM64_REG_X1, UC_ARM64_REG_X2, UC_ARM64_REG_X3, UC_ARM64_REG_SP, UC_ARM64_REG_PC, UC_ARM64_REG_LR
p = b.p
view = 0x10000000
klass = 0x10001000
renderers = [0x10002000, 0x10002100, 0x10002200]  # base, overlay, shadow
manager = 0x10003000
statics = 0x10004000
particles = 0x10005000
array = 0x10006000
trace = []
native_error = False

def boundary(uc, address, size, data):
    global native_error
    x0, x1 = uc.reg_read(UC_ARM64_REG_X0), uc.reg_read(UC_ARM64_REG_X1)
    value = None
    if address == 0xffda88:
        native_error = True
        uc.reg_write(UC_ARM64_REG_PC, 0x20000000)
        return
    if address in (0x3e3ebc0, 0x3e3ec40):
        index = renderers.index(x0)
        is_layer = address == 0x3e3ebc0
        p.put(x0 + (0x10 if is_layer else 0x14), x1)
        trace.append(('L' if is_layer else 'O') + str(index) + ':' + str(p.signed(x1 & 0xffffffff)))
        value = 0
    elif address in (0x3e3eb84, 0x3e3ec04):
        value = p.read(x0 + (0x10 if address == 0x3e3eb84 else 0x14))
    if value is not None:
        uc.reg_write(UC_ARM64_REG_X0, value)
        uc.reg_write(UC_ARM64_REG_PC, uc.reg_read(UC_ARM64_REG_LR))

p.emu.hook_add(UC_HOOK_CODE, boundary)
# Resolve the real relocated Sorting metadata slot, then supply Item layer ID.
b.call(0x12dd2f4, 0)
sorting_klass = p.read(p.read(0x5356db8, 8), 8)
p.put(sorting_klass + 0xb8, statics, 8)

def call(rva, *arguments):
    registers = (UC_ARM64_REG_X0, UC_ARM64_REG_X1, UC_ARM64_REG_X2, UC_ARM64_REG_X3)
    for register, value in zip(registers, (*arguments, 0, 0, 0, 0)):
        p.emu.reg_write(register, value & 0xffffffffffffffff)
    p.emu.reg_write(UC_ARM64_REG_SP, 0x101ff000)
    p.emu.reg_write(UC_ARM64_REG_LR, 0x20000000)
    p.emu.emu_start(rva, 0x20000000, count=1000000)
    assert p.emu.reg_read(UC_ARM64_REG_PC) == 0x20000000
    return p.emu.reg_read(UC_ARM64_REG_X0), p.emu.reg_read(UC_ARM64_REG_X1)

def packed(layer, order):
    return (layer & 0xffffffff) | ((order & 0xffffffff) << 32)

def data_string(pair):
    pack, everything = pair
    return f'{p.signed(pack & 0xffffffff)},{p.signed(pack >> 32)},{everything & 1}'

def snapshot():
    return ';'.join(f'{p.signed(p.read(r+0x10))},{p.signed(p.read(r+0x14))}' for r in renderers)

cases, expected = [], []
rng = random.Random(0x21a965c)
orders = [0, 1, -1, 1000, 9005, 8905, 2147483647, -2147483648]
for n in range(128):
    p.emu.mem_write(view, bytes(0x7000))
    p.put(view, klass, 8)
    p.put(view + 0x50, manager, 8)
    p.put(klass + 0x208, 0x21a965c, 8)
    p.put(klass + 0x1b8, 0x2b4abe4, 8)
    p.put(klass + 0x2e8, 0x2b4b258, 8)
    p.put(klass + 0x2f8, 0x2b4b260, 8)
    for offset, renderer in zip((0x78, 0x80, 0x88), renderers):
        p.put(view + offset, renderer, 8)
        p.put(renderer + 0x10, 5)
        p.put(renderer + 0x14, -7)
    layer = (231352099, 17, -1, 2147483647)[n % 4]
    chain = n % 2
    p.put(statics + 8, layer)
    p.put(view + 0x69, chain, 1)
    operations = ['I,0,0,0,0']
    # Include initial state, blocked updates, animation overrides, enabling,
    # ArrangeSelfSorting while locked, and clearing the under-chain flag.
    for operation in range(24):
        kind = ('A', 'U', 'R', 'E', 'C', 'A', 'U', 'R')[operation % 8]
        target_layer = rng.choice((layer, 3, -17, 151353909))
        order = rng.choice(orders)
        operations.append(f'{kind},{target_layer},{order},{rng.randrange(2)},{rng.randrange(2)}')
    cases.append(f'{layer},{chain}#' + ';'.join(operations))
    for operation in operations:
        kind, target_layer, order, everything, allow = operation.split(',')
        target_layer, order, everything, allow = map(int, (target_layer, order, everything, allow))
        trace = []
        if kind == 'I':
            call(0x2b4abc8, view, packed(layer, 1000), 0)
        elif kind == 'A':
            call(0x2b4ac00, view, packed(target_layer, order), everything, allow)
        elif kind == 'U':
            call(0x21a965c, view, packed(target_layer, order), everything)
        elif kind == 'R':
            result = data_string(call(0x2b470f4, view))
            trace.append('R:' + result)
        elif kind == 'E':
            call(0x2b4abf4, view)
        else:
            call(0x2b4b444, view)
        result = data_string(call(0x21a9748, view))
        expected.append('|'.join(trace) + '#' + snapshot() + '#' + result)

# Offset must retain sortEverything, including overflow, and swap start is 9005/true.
for n in range(64):
    layer = rng.choice((231352099, -1, 17))
    order = rng.choice(orders)
    everything = rng.randrange(2)
    offset = rng.choice(orders)
    cases.append(f'F,{layer},{order},{everything},{offset}')
    p.put(statics + 8, layer)
    result = data_string(call(0x12d5db4, packed(layer, order), everything, offset))
    swap = data_string(call(0x12d83fc))
    expected.append(result + '#' + swap)

# Merge sorting uses the Item layer, a negative index offset and false sorting flag.
for n in range(64):
    layer=rng.choice((231352099,-1,17));index=rng.choice(orders)
    p.put(statics+8,layer)
    cases.append(f'M,{layer},{index}')
    expected.append(data_string(call(0x12d97d0,index)))

# Particle renderer iteration: real native loop and extension, including empty
# arrays and null array / null entry failures. Unity property storage is supplied.
for n in range(32):
    length = n % 4
    is_null = n in (8, 16)
    null_entry = n % 7 == 0 and length > 0
    p.put(particles + 0x50, 0 if is_null else array, 8)
    p.put(array + 0x18, length)
    for i in range(length): p.put(array + 0x20 + i * 8, 0 if null_entry and i == length-1 else renderers[i], 8)
    trace, native_error = [], False
    call(0x21a95d8, particles, packed(17, 9006), 0)
    cases.append(f'P,{length},{int(is_null)},{int(null_entry)}')
    expected.append('|'.join(trace) + ('#E' if native_error else '#OK'))

harness = r'''using System;using System.Collections.Generic;using Whitebox.Foundation;
namespace UnityEngine{
public static class Debug{public static void LogError(object x){}}
public class Renderer{public static List<string> Trace=new List<string>();public int Id;int layer=5,order=-7;public int sortingLayerID{get{return layer;}set{layer=value;Trace.Add("L"+Id+":"+value);}}public int sortingOrder{get{return order;}set{order=value;Trace.Add("O"+Id+":"+value);}}}
public class SpriteRenderer:Renderer{}
}
class Verify{
static string Data(OriginalSwapSorting s){return s.Layer+","+s.Order+","+(s.SortEverything?1:0);}
static void Main(){string line;while((line=Console.ReadLine())!=null){
var f=line.Split(',');
if(f[0]=="M"){Console.WriteLine(Data(OriginalItemSortingRules.SpecialCreation(int.Parse(f[1]),int.Parse(f[2]))));continue;}if(f[0]=="F"){Console.WriteLine(Data(OriginalItemSortingRules.WithOffset(new OriginalSwapSorting(int.Parse(f[1]),int.Parse(f[2]),f[3]=="1"),int.Parse(f[4])))+"#"+Data(OriginalItemSortingRules.SwapStart(int.Parse(f[1]))));continue;}
if(f[0]=="P"){int count=int.Parse(f[1]);UnityEngine.Renderer[] renderers=f[2]=="1"?null:new UnityEngine.Renderer[count];if(renderers!=null)for(int i=0;i<count;i++)renderers[i]=f[3]=="1"&&i==count-1?null:new UnityEngine.Renderer{Id=i};UnityEngine.Renderer.Trace.Clear();bool error=false;try{OriginalItemSortingRules.UpdateParticleRenderers(renderers,new OriginalSwapSorting(17,9006,false));}catch(NullReferenceException){error=true;}Console.WriteLine(string.Join("|",UnityEngine.Renderer.Trace)+(error?"#E":"#OK"));continue;}
var halves=line.Split('#');var header=halves[0].Split(',');var views=new UnityEngine.SpriteRenderer[3];for(int i=0;i<3;i++)views[i]=new UnityEngine.SpriteRenderer{Id=i};var sorting=new OriginalMatchItemSorting(views[0],views[1],views[2],int.Parse(header[0]),header[1]=="1");
foreach(var operation in halves[1].Split(';')){var a=operation.Split(',');var value=new OriginalSwapSorting(int.Parse(a[1]),int.Parse(a[2]),a[3]=="1");UnityEngine.Renderer.Trace.Clear();switch(a[0]){case "I":sorting.Initialize();break;case "A":sorting.UpdateSortingForAnimation(value,a[4]=="1");break;case "U":sorting.UpdateSorting(value);break;case "R":var result=sorting.ArrangeSelfSorting();UnityEngine.Renderer.Trace.Add("R:"+Data(result));break;case "E":sorting.EnableSortingUpdates();break;case "C":sorting.RevealFromChain();break;}
var states=new string[3];for(int i=0;i<3;i++)states[i]=views[i].sortingLayerID+","+views[i].sortingOrder;Console.WriteLine(string.Join("|",UnityEngine.Renderer.Trace)+"#"+string.Join(";",states)+"#"+Data(sorting.GetSorting()));}
}}}'''
with tempfile.TemporaryDirectory() as folder:
    folder = Path(folder)
    source, exe = folder/'Verify.cs', folder/'Verify.exe'
    source.write_text(harness)
    runtime = p.PROJECT/'Assets/Foundation/Runtime'
    names = ('OriginalMatchItemSorting', 'OriginalSwapCompletion', 'OriginalSwapActionState', 'OriginalSwapDecision',
        'OriginalItemSwapAvailability', 'OriginalFillingItemPlacement', 'OriginalFillingCell', 'OriginalFallRefillColumns',
        'OriginalCellTouchRules', 'OriginalCellMediation', 'OriginalGridIterator', 'OriginalMatchSearch',
        'OriginalMatchCandidates', 'OriginalMatchSnapshot', 'OriginalGameOperations', 'OriginalCellFallState')
    result = subprocess.run([str(p.mono/'bin/mono.exe'), str(p.mono/'lib/mono/4.5/mcs.exe'), '-out:'+str(exe), str(source),
        *[str(runtime/(name+'.cs')) for name in names]], capture_output=True, text=True, encoding='utf-8')
    assert result.returncode == 0, result.stdout+result.stderr
    result = subprocess.run([str(p.mono/'bin/mono.exe'), str(exe)], input='\n'.join(cases)+'\n', capture_output=True, text=True, encoding='utf-8')
    assert result.returncode == 0, result.stdout+result.stderr
    actual = result.stdout.replace('\ufeff', '').splitlines()
    assert actual == expected, next(((i,a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a != e), 'count mismatch')

# Sorting layers retain the recovered IDs and ordering; only that section is changed.
tag = p.PROJECT/'ProjectSettings/TagManager.asset'
original = p.RECOVERY/'assets/RecoveredProject/ExportedProject/ProjectSettings/TagManager.asset'
assert tag.read_text().split('  m_SortingLayers:')[1] == original.read_text().split('  m_SortingLayers:')[1]
report = {'passed': True, 'native_match_view_sequences': 128, 'native_sorting_operations': 3200,
    'native_sorting_data_cases': 64, 'native_merge_sorting_cases': 64, 'native_particle_sorting_cases': 32,
    'original_rvas': ['0x21a965c','0x21a9748','0x2b4abc8','0x2b4ac00','0x2b4abf4','0x2b470f4','0x2b4b444','0x12d5db4','0x12d83fc','0x12d97d0','0x21a95d8','0x2d6e314'],
    'scope': 'Actual native match sorting, normal-view ArrangeSelfSorting, indexed special creation sorting with overflow, animation lock/unlock, layer/order write sequence, GetSorting and particle renderer loop vs compiled production sorting state. Unity renderer property storage and Sorting Item layer supplied. Recovered sorting layer definitions checked exactly. Particle playback, visible swaps, other item sorting and full visual parity remain pending.'}
(p.PROJECT/'match_item_sorting_verification.json').write_text(json.dumps(report, indent=2)+'\n')
print(json.dumps(report))
