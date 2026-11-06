"""Compare decoded set configuration parsing with original ARM64 ParseSets."""
import json
from pathlib import Path
import subprocess
import sys
import tempfile

PROJECT = Path(__file__).resolve().parents[1]
RECOVERY = PROJECT.parent
sys.path.insert(0, str(RECOVERY / 'tools/python'))
from elftools.elf.elffile import ELFFile
from unicorn import Uc, UC_ARCH_ARM64, UC_MODE_ARM, UC_HOOK_CODE
from unicorn.arm64_const import UC_ARM64_REG_X0, UC_ARM64_REG_X1, UC_ARM64_REG_X2, UC_ARM64_REG_LR, UC_ARM64_REG_SP, UC_ARM64_REG_PC
emu = Uc(UC_ARCH_ARM64, UC_MODE_ARM)
emu.mem_map(0, 0x6000000)
emu.mem_map(0x10000000, 0x200000)
emu.mem_map(0x20000000, 0x1000)
lib = RECOVERY.parent / 'RoyalMatch_20260929/apk_unpacked/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so'
with lib.open('rb') as stream:
    for segment in ELFFile(stream).iter_segments():
        if segment['p_type'] == 'PT_LOAD': emu.mem_write(segment['p_vaddr'], segment.data())
def put(address, value, size=4): emu.mem_write(address, (value & ((1 << (size * 8)) - 1)).to_bytes(size, 'little'))
def read(address, size=4): return int.from_bytes(emu.mem_read(address, size), 'little')
def signed(value): return value - 0x100000000 if value >= 0x80000000 else value
def allocate(size=0x100):
    global heap
    value = heap; heap += size
    emu.mem_write(value, bytes(size)); return value
def hook(uc, address, size, data):
    global element_index, limit_index
    x0, x1, x2 = (uc.reg_read(r) for r in (UC_ARM64_REG_X0, UC_ARM64_REG_X1, UC_ARM64_REG_X2))
    value = None
    getters = {0x1621a8c: 1, 0x1629970: len(limits), 0x1629210: 0, 0x162940c: can_fall,
        0x1629454: ratio, 0x16295b0: bird, 0x16295f4: frog, 0x16293d8: len(elements),
        0x16294f8: len(columns), 0x1629698: len(fills), 0x16297b0: len(fill_ratios)}
    if address in getters: value = getters[address]
    elif address == 0x16292d0: element_index = x1; value = 0
    elif address == 0x1629868:
        limit_index = x1
        # Nullable result's HasValue byte is read before extracting the value.
        from unicorn.arm64_const import UC_ARM64_REG_X8
        put(uc.reg_read(UC_ARM64_REG_X8), 1, 1)
        value = 0
    elif address == 0x162a388: value = elements[element_index][0]
    elif address == 0x162a3cc: value = elements[element_index][1]
    elif address == 0x162a5c8: value = limits[limit_index][0]
    elif address == 0x162a60c: value = limits[limit_index][1]
    elif address == 0x1629498: value = columns[x1]
    elif address == 0x1629638: value = fills[x1]
    elif address == 0x1629750: value = fill_ratios[x1]
    elif address == 0xffda78: value = allocate()
    elif address == 0xffd9d0:
        value = allocate(0x1000); put(value + 0x18, x1)
    elif address in (0x2c1b430, 0x314c024):
        storage = allocate(0x1000); put(storage + 0x18, 64); put(x0 + 0x10, storage, 8); value = 0
    elif address in (0x3c95784, 0x3c9bdd8): dictionaries[x0] = {}; value = 0
    elif address == 0x3c963d0: value = int(x1 in dictionaries[x0])
    elif address == 0x3c961d8:
        assert x1 not in dictionaries[x0]
        dictionaries[x0][x1] = x2 & 0xffffffff; value = 0
    elif address == 0xffda68: value = x0
    elif address == 0x260dea4: value = 0x10000800
    elif address == 0x23f0a18:
        # ItemCreator/container boundary: native AddTargetColumn still executes.
        uc.reg_write(UC_ARM64_REG_X0, x1)
        uc.reg_write(UC_ARM64_REG_X1, x2)
        uc.reg_write(UC_ARM64_REG_PC, 0x190fb1c)
        return
    elif address in (0x1621984, 0x2e0a1ec, 0x2e0a6c8, 0x2e0aba4, 0xffd90c, 0x231007c, 0xffda64): value = 0
    if value is not None:
        uc.reg_write(UC_ARM64_REG_X0, value & 0xffffffffffffffff)
        uc.reg_write(UC_ARM64_REG_PC, uc.reg_read(UC_ARM64_REG_LR))
emu.hook_add(UC_HOOK_CODE, hook)
for flag in (0x573da98, 0x573c8d3, 0x573c8d7, 0x573c8d8, 0x573c8d9): put(flag, 1, 1)
def native():
    emu.reg_write(UC_ARM64_REG_X0, 0x10000000)
    emu.reg_write(UC_ARM64_REG_SP, 0x101ff000)
    emu.reg_write(UC_ARM64_REG_LR, 0x20000000)
    try: emu.emu_start(0x1c22304, 0x20000000, count=1000000)
    except Exception:
        raise RuntimeError('Native PC ' + hex(emu.reg_read(UC_ARM64_REG_PC)))
    assert emu.reg_read(UC_ARM64_REG_PC) == 0x20000000
    array = emu.reg_read(UC_ARM64_REG_X0)
    return read(array + 0x20, 8)
def values(owner, offset):
    lst = read(owner + offset, 8); storage = read(lst + 0x10, 8)
    return [signed(read(storage + 0x20 + i * 4)) for i in range(read(lst + 0x18))]
def csv(items): return ','.join(map(str, items))
def pairs(items): return ','.join(f'{a}:{b}' for a,b in items)
cases, expected = [], []
for must, replaced in ((0,0),(6,1),(7,1)):
    for ratio in (-9,0,1,23):
        for bird, frog, limits in ((0,0,[]),(3,5,[]),(3,5,[(16,0),(31,-2),(99,7)])):
            can_fall = ratio != 0
            elements = [(1,-3),(14,0),(6,9)]
            columns, fills, fill_ratios = [1,3,3], [0,4,2], [-1,0,30]
            heap = 0x10010000; dictionaries = {}; element_index = limit_index = 0
            emu.mem_write(0x10000000, bytes(0x1000))
            put(0x53b3c40, 0x10000300, 8); put(0x10000300, 0x10000400, 8); put(0x100004e0, 1)
            put(0x10000018, 0x10000700, 8); put(0x10000738, 0x10000800, 8)
            put(0x10000050, must); put(0x10000054, replaced)
            obj = native()
            lst = read(obj + 0x18, 8); storage = read(lst + 0x10, 8)
            element_objects = [read(storage + 0x20 + i * 8, 8) for i in range(read(lst + 0x18))]
            ids = [signed(read(e+0x10)) for e in element_objects]
            weights = [signed(read(e+0x14)) for e in element_objects]
            maximums = sorted(dictionaries[read(obj+0x40,8)].items())
            expected.append('|'.join([str(read(obj+0x38,1)), str(signed(read(obj+0x3c))), csv(ids), csv(weights), csv(values(obj,0x20)), csv(values(obj,0x28)), csv(values(obj,0x30)), pairs([(a,signed(b)) for a,b in maximums])]))
            cases.append('|'.join([str(must),str(replaced),str(int(can_fall)),str(ratio),str(bird),str(frog),pairs(limits),pairs(elements),csv(columns),csv(fills),csv(fill_ratios)]))
harness = '''using System; using System.Collections.Generic; using Whitebox.Foundation; using Whitebox.Foundation.ConfigData;
class Verify {
 static int[] Ints(string s) {return s.Length==0?new int[0]:Array.ConvertAll(s.Split(','),int.Parse);}
 static FTiledSetMaxItemCount[] Limits(string s){var a=new List<FTiledSetMaxItemCount>();if(s.Length>0)foreach(var v in s.Split(',')){var p=v.Split(':');a.Add(new FTiledSetMaxItemCount{Id=int.Parse(p[0]),MaxCount=int.Parse(p[1])});}return a.ToArray();}
 static FTiledSetElement[] Elements(string s){var a=new List<FTiledSetElement>();foreach(var v in s.Split(',')){var p=v.Split(':');a.Add(new FTiledSetElement{Id=int.Parse(p[0]),CreateRatio=int.Parse(p[1])});}return a.ToArray();}
 static void Main(){string line;while((line=Console.ReadLine())!=null){var p=line.Split('|');var raw=new FTiledSet{CanFall=p[2]=="1",CreateRatio=int.Parse(p[3]),BirdMax=int.Parse(p[4]),FrogMax=int.Parse(p[5]),MaxItemCounts=Limits(p[6]),Elements=Elements(p[7]),TargetColumns=Ints(p[8]),TargetFills=Ints(p[9]),TargetFillRatios=Ints(p[10])};
 var set=OriginalParsedLevelSet.ParseSets(new FTiledLevel{Sets=new[]{raw}},int.Parse(p[0]),int.Parse(p[1]))[0];var keys=new List<int>(set.MaxItemCounts.Keys);keys.Sort();var max=new List<string>();foreach(int k in keys)max.Add(k+":"+set.MaxItemCounts[k]);
 Console.WriteLine((set.CanFall?"1":"0")+"|"+set.CreateRatio+"|"+string.Join(",",set.ElementIds)+"|"+string.Join(",",set.ElementCreateRatios)+"|"+string.Join(",",set.TargetColumns)+"|"+string.Join(",",set.TargetFills)+"|"+string.Join(",",set.TargetFillRatios)+"|"+string.Join(",",max));}}}'''
mono = Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder); source=folder/'Verify.cs'; source.write_text(harness,encoding='utf-8'); exe=folder/'Verify.exe'
    runtime=PROJECT/'Assets/Foundation/Runtime'
    subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),str(runtime/'OriginalConfigData.cs'),str(runtime/'OriginalLevelRules.cs'),str(runtime/'OriginalParsedLevelSet.cs')],check=True,capture_output=True)
    actual=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',text=True,encoding='utf-8',capture_output=True,check=True).stdout.lstrip('\ufeff').splitlines()
    if actual!=expected:
        for i,(a,e) in enumerate(zip(actual,expected)):
            if a!=e: raise AssertionError((cases[i],a,e))
        raise AssertionError('Output count differs')
report={'passed':True,'native_set_cases':len(cases),'original_rvas':['0x1c22304','0x190f048','0x1910c3c','0x190fb1c','0x190fbc4','0x190fc6c'],'scope':'Decoded set configuration, color replacement, weight clamping, limits and ordered target lists. FlatBuffers accessors and managed allocation/storage are supplied at boundaries. ItemCreator column distribution and runtime fill availability are not verified.'}
(PROJECT/'set_parser_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(json.dumps(report))
