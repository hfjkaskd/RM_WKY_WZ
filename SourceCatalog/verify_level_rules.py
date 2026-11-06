"""Compare compiled recovery rules to the original ARM64 bodies, without Unity stubs."""
import json
import os
from pathlib import Path
import random
import subprocess
import sys
import tempfile

PROJECT = Path(__file__).resolve().parents[1]
RECOVERY = PROJECT.parent
sys.path.insert(0, str(RECOVERY / 'tools/python'))
from elftools.elf.elffile import ELFFile
from unicorn import Uc, UC_ARCH_ARM64, UC_MODE_ARM, UC_HOOK_CODE
from unicorn.arm64_const import UC_ARM64_REG_X0, UC_ARM64_REG_X1, UC_ARM64_REG_X2, UC_ARM64_REG_X3, UC_ARM64_REG_LR, UC_ARM64_REG_SP

LIB = RECOVERY.parent / 'RoyalMatch_20260929/apk_unpacked/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so'
MONO_ROOT = Path(os.environ.get('ROYAL_MONO_ROOT', 'C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
emu = Uc(UC_ARCH_ARM64, UC_MODE_ARM)
emu.mem_map(0, 0x6000000)
with LIB.open('rb') as stream:
    for segment in ELFFile(stream).iter_segments():
        if segment['p_type'] == 'PT_LOAD':
            emu.mem_write(segment['p_vaddr'], segment.data())
emu.mem_map(0x10000000, 0x10000)
emu.mem_map(0x20000000, 0x1000)
emu.mem_write(0x573da8e, b'\x01')
palette = []
capture_mask = False
choose_last = False
transform_mode = False
retry_calls = 0
secure_input = b''

from unicorn.arm64_const import UC_ARM64_REG_PC
# Keep register identifiers supplied by Unicorn, rather than assuming numeric ids.
def safe_hook(uc, address, size, data):
    global retry_calls, secure_input
    value = None
    if transform_mode and address == 0x1c1e0b0: retry_calls += 1
    if transform_mode and address == 0x1c1e4f0:
        secure_input = bytes(uc.mem_read(uc.reg_read(UC_ARM64_REG_X1) + 0x20, len(transform_values) * 4))
    if transform_mode and address in (0x1621790, 0x2e09834, 0xffd90c, 0x2a2da14, 0xffda64):
        value = 0
    elif transform_mode and address in (0xffda6c, 0xfcd644):
        value = 0
    elif transform_mode and address == 0xffda68:
        value = uc.reg_read(UC_ARM64_REG_X0)
    elif transform_mode and address == 0x1620eb0:
        value = 0x1000a000
    elif transform_mode and address == 0x26286c0:
        value = 0x10001000
    elif transform_mode and address == 0xffda78:
        value = 0x10000000
    elif transform_mode and address == 0x1c1dddc:
        prepare_randomizer(transform_values, transform_width, transform_masks)
        value = 0
    elif transform_mode and address in (0x1c1ec14, 0x1c1eca0, 0x2629cb0):
        value = transform_width if address == 0x1c1ec14 or (address == 0x2629cb0 and uc.reg_read(UC_ARM64_REG_X1) == 0) else transform_height
    elif transform_mode and address == 0xffd9d8:
        value = 0x1000c000 if not allocated_grids else 0x1000d000
        allocated_grids.append(value)
        uc.mem_write(value, bytes(0x800))
        write_pointer(value + 0x10, 0x1000b000)
        write_pointer(0x1000b000, transform_width)
        write_pointer(0x1000b010, transform_height)
        write_int(value + 0x18, len(transform_values))
    elif address == 0xffd9d0:
        if transform_mode:
            value = 0x1000e000
            uc.mem_write(value, bytes(0x400))
            write_int(value + 0x18, uc.reg_read(UC_ARM64_REG_X1))
        else:
            value = 0x10002000
            uc.mem_write(value, bytes(0x1000))
            uc.mem_write(value + 0x18, len(palette).to_bytes(4, 'little'))
    elif address == 0x1621b20:
        value = len(palette)
    elif address == 0x1621ac0:
        value = palette[uc.reg_read(UC_ARM64_REG_X1)] & 0xffffffff
    elif address == 0x1c1e850 and capture_mask:
        value = uc.reg_read(UC_ARM64_REG_X1)
    elif address == 0x260e410:
        candidate_list = uc.reg_read(UC_ARM64_REG_X1)
        storage = int.from_bytes(uc.mem_read(candidate_list + 0x10, 8), 'little')
        length = int.from_bytes(uc.mem_read(candidate_list + 0x18, 4), 'little')
        value = uc.mem_read(storage + 0x20 + (length - 1 if choose_last else 0), 1)[0]
    if value is not None:
        uc.reg_write(UC_ARM64_REG_X0, value)
        uc.reg_write(UC_ARM64_REG_PC, uc.reg_read(UC_ARM64_REG_LR))

emu.hook_add(UC_HOOK_CODE, safe_hook)

def signed(value):
    value &= 0xffffffff
    return value - 0x100000000 if value >= 0x80000000 else value

def call(address, args):
    for reg, value in zip((UC_ARM64_REG_X0, UC_ARM64_REG_X1, UC_ARM64_REG_X2, UC_ARM64_REG_X3), args):
        emu.reg_write(reg, value & 0xffffffffffffffff)
    emu.reg_write(UC_ARM64_REG_SP, 0x1000f000)
    emu.reg_write(UC_ARM64_REG_LR, 0x20000000)
    emu.emu_start(address, 0x20000000, count=1000000)
    if emu.reg_read(UC_ARM64_REG_PC) != 0x20000000:
        raise RuntimeError('Original method did not return')
    return emu.reg_read(UC_ARM64_REG_X0)

cases = []
expected = []
for value in list(range(-32, 256)) + [-2147483648, 2147483647]:
    for mode, address in [('match', 0x2b45044), ('tile', 0x2b45054)]:
        cases.append(f'{mode}|{value}')
        expected.append(str(signed(call(address, [value]))))
rng = random.Random(38911)
for _ in range(200):
    x, y, width = [rng.randint(-2147483648, 2147483647) for i in range(3)]
    cases.append(f'index|{x}|{y}|{width}')
    expected.append(str(signed(call(0x1c1f7cc, [0, x, y, width]))))
for source in ([], [1, 2, 3, 4], [0, 1, 6, 7, -1, 211, 2147483647]):
    for must in (0, 1, 6, 7, -1):
        for replaced in (0, 1, 6, 7, -1):
            palette = source
            emu.mem_write(0x10001000, bytes(0x100))
            emu.mem_write(0x10001050, (must & 0xffffffff).to_bytes(4, 'little'))
            emu.mem_write(0x10001054, (replaced & 0xffffffff).to_bytes(4, 'little'))
            output = call(0x1c21f80, [0x10001000, 0])
            values = [signed(int.from_bytes(emu.mem_read(output + 0x20 + i * 4, 4), 'little')) for i in range(len(source))]
            cases.append(f'colors|{must}|{replaced}|' + ','.join(map(str, source)))
            expected.append(','.join(map(str, values)))

def write_int(address, value):
    emu.mem_write(address, (value & 0xffffffff).to_bytes(4, 'little'))

def write_pointer(address, value):
    emu.mem_write(address, value.to_bytes(8, 'little'))

def prepare_randomizer(values, width, masks):
    emu.mem_write(0x10000000, bytes(0x8000))
    for address in (0x573da71, 0x573da72):
        emu.mem_write(address, b'\x01')
    write_pointer(0x10000010, 0x10001000)
    write_int(0x10000018, width)
    write_pointer(0x10000020, 0x10002000)
    write_pointer(0x10000028, 0x10005000)
    write_pointer(0x10000030, 0x10003000)
    write_pointer(0x10000038, 0x10004000)
    for address, array in ((0x10001000, values), (0x10002000, masks), (0x10003000, [0] * 6)):
        write_int(address + 0x18, len(array))
        for i, value in enumerate(array): write_int(address + 0x20 + 4 * i, value)
    write_pointer(0x10004010, 0x10006000)
    write_int(0x10006018, 6)

capture_mask = True
for _ in range(800):
    width = rng.randint(1, 9)
    values = [rng.choice([0, 1, 2, 3, 4, 5, 6, 14, 21]) for i in range(width * rng.randint(1, 8))]
    index = rng.randrange(len(values))
    mask = rng.randint(-64, 127)
    prepare_randomizer(values, width, [63])
    expected.append(str(signed(call(0x1c1e2a0, [0x10000000, index, mask, 0]))))
    cases.append(f'mask|{width}|{index}|{mask}|' + ','.join(map(str, values)))
capture_mask = False
for _ in range(240):
    width = rng.randint(1, 9)
    values = [rng.choice([0, 1, 2, 3, 4, 5, 6, 14, 15, 16, 21]) for i in range(width * rng.randint(1, 8))]
    masks = [rng.randrange(64) for i in range(3)]
    choose_last = bool(rng.randrange(2))
    prepare_randomizer(values, width, masks)
    result = [signed(call(0x1c1e1cc, [0x10000000, index, 0])) for index in range(len(values))]
    native_grid = [signed(int.from_bytes(emu.mem_read(0x10001020 + 4 * i, 4), 'little')) for i in range(len(values))]
    expected.append(','.join(map(str, result)) + ';' + ','.join(map(str, native_grid)))
    cases.append(f'fill|{width}|{int(choose_last)}|' + ','.join(map(str, masks)) + '|' + ','.join(map(str, values)))

transform_mode = True
secure_rewrites = 0
for guard in (0x573daa6, 0x573da6f): emu.mem_write(guard, b'\x01')
for _ in range(160):
    transform_width, transform_height = rng.randint(1, 9), rng.randint(1, 8)
    transform_values = [rng.choice([0, 1, 2, 3, 4, 5, 6, 14, 15, 16, 21]) for i in range(transform_width * transform_height)]
    transform_masks = [rng.randrange(64) for i in range(3)]
    choose_last = bool(rng.randrange(2))
    allocated_grids = []
    emu.mem_write(0x10009000, bytes(0x5000))
    write_pointer(0x10009040, 0x1000b800) # Pre-selection already resolved; skip 211..213 selection.
    write_int(0x1000a018, len(transform_values))
    for i, value in enumerate(transform_values): write_int(0x1000a020 + i * 4, value)
    output = call(0x1c24a78, [0x10009000, 0])
    if bytes(emu.mem_read(output + 0x20, len(transform_values) * 4)) != secure_input: secure_rewrites += 1
    def grid_text(address):
        return ','.join(str(signed(int.from_bytes(emu.mem_read(address + 0x20 + 4 * (i % transform_width * transform_height + i // transform_width), 4), 'little'))) for i in range(len(transform_values)))
    expected.append(grid_text(output) + ';' + grid_text(0x1000d000) + ';' + str(emu.mem_read(0x10009015, 1)[0]))
    cases.append(f'transform|{transform_width}|{transform_height}|{int(choose_last)}|' + ','.join(map(str, transform_masks)) + '|' + ','.join(map(str, transform_values)))
assert retry_calls > 0 and secure_rewrites > 0, 'Critical transform branches not exercised'

HARNESS = '''using System; using Whitebox.Foundation;
class Picker : IOriginalMatchRandomSource { public bool Last; public byte GetRandomItemFromListBasedOnDifficulty(System.Collections.Generic.List<byte> a) { return a[Last ? a.Count-1 : 0]; } }
class Verify { static void Main() { string line; while ((line = Console.ReadLine()) != null) {
var p = line.Split('|'); switch(p[0]) {
case "index": Console.WriteLine(OriginalLevelRules.GetIndexFromPoint(int.Parse(p[1]),int.Parse(p[2]),int.Parse(p[3]))); break;
case "match": Console.WriteLine(OriginalLevelRules.ToMatchType(int.Parse(p[1]))); break;
case "tile": Console.WriteLine(OriginalLevelRules.ToTiledId(int.Parse(p[1]))); break;
case "transform": var ids=Array.ConvertAll(p[5].Split(','),int.Parse); var tm=Array.ConvertAll(p[4].Split(','),int.Parse); var ts=new int[3][];
for(int s=0;s<3;s++){var list=new System.Collections.Generic.List<int>();for(int k=0;k<6;k++)if((tm[s]&(1<<k))!=0)list.Add(k+1);ts[s]=list.ToArray();}
var transformed=new OriginalGridTransform(ids,int.Parse(p[1]),int.Parse(p[2]),ts,new Picker{Last=p[3]=="1"}); var ti=new int[ids.Length]; var oi=new int[ids.Length];
for(int k=0;k<ids.Length;k++){ti[k]=transformed.Items[k%int.Parse(p[1]),k/int.Parse(p[1])];oi[k]=transformed.OriginalIds[k%int.Parse(p[1]),k/int.Parse(p[1])];}
Console.WriteLine(string.Join(",",ti)+";"+string.Join(",",oi)+";"+(transformed.HasAtLeastOnePossibleMatch?"1":"0"));break;
case "mask": Console.WriteLine(OriginalMatchItemRandomizer.GetAllowedMask(Array.ConvertAll(p[4].Split(','),int.Parse),int.Parse(p[1]),int.Parse(p[2]),int.Parse(p[3]))); break;
case "fill": var grid=Array.ConvertAll(p[4].Split(','),int.Parse); var masks=Array.ConvertAll(p[3].Split(','),int.Parse); var sets=new int[3][];
for(int s=0;s<3;s++) {var list=new System.Collections.Generic.List<int>(); for(int k=0;k<6;k++) if((masks[s]&(1<<k))!=0)list.Add(k+1); sets[s]=list.ToArray();}
var r=new OriginalMatchItemRandomizer(grid,int.Parse(p[1]),sets,new Picker{Last=p[2]=="1"}); var v=new int[grid.Length]; for(int k=0;k<v.Length;k++)v[k]=r.GetValue(k);
Console.WriteLine(string.Join(",",Array.ConvertAll(v,x=>x.ToString()))+";"+string.Join(",",Array.ConvertAll(grid,x=>x.ToString()))); break;
case "colors": var a = p[3].Length == 0 ? new int[0] : Array.ConvertAll(p[3].Split(','), int.Parse);
var b=OriginalLevelRules.ParseColors(a,int.Parse(p[1]),int.Parse(p[2]));
Console.WriteLine(string.Join(",",Array.ConvertAll(b, x => x.ToString()))); break;
}}}}'''
with tempfile.TemporaryDirectory(prefix='royal-level-rules-') as temporary:
    directory = Path(temporary)
    harness = directory / 'Verify.cs'
    harness.write_text(HARNESS)
    executable = directory / 'Verify.exe'
    mono = str(MONO_ROOT / 'bin/mono.exe')
    subprocess.run([mono, str(MONO_ROOT / 'lib/mono/4.5/mcs.exe'), '-checked+', '-out:' + str(executable), str(harness), str(PROJECT / 'Assets/Foundation/Runtime/OriginalLevelRules.cs'), str(PROJECT / 'Assets/Foundation/Runtime/OriginalMatchItemRandomizer.cs'), str(PROJECT / 'Assets/Foundation/Runtime/OriginalGridTransform.cs')], check=True)
    actual = subprocess.run([mono, str(executable)], input='\n'.join(cases) + '\n', encoding='utf-8', capture_output=True, check=True).stdout.lstrip('\ufeff').splitlines()
    if actual != expected:
        for i, (a, b) in enumerate(zip(actual, expected)):
            if a != b:
                raise AssertionError((cases[i], a, b))
        raise AssertionError('Output length differs')
report = {'passed': True, 'native_comparison_cases': len(cases), 'mask_cases': 800, 'sequential_fill_cases': 240, 'transform_cases': 160, 'original_rvas': ['0x1c1f7cc', '0x2b45044', '0x2b45054', '0x1c21f80', '0x1c1e2a0', '0x1c1e1cc', '0x1c24a78', '0x1c1e4f0'], 'scope': 'Index, color-id conversion, palette replacement, randomizer exclusions, sequential fills and prepared-grid transformation including retry, original ID grid and secure-color pass. Candidate selection is supplied by first/last test pickers. FlatBuffers reads, set construction and 211..213 selection are supplied at boundaries; full initialization and gameplay are not verified.'}
report['native_retry_calls'] = retry_calls
report['native_secure_rewrite_cases'] = secure_rewrites
(PROJECT / 'level_rules_verification.json').write_text(json.dumps(report, indent=2) + '\n')
print(json.dumps(report))
