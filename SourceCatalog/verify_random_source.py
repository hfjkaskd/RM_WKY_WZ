"""Compare Unity Mono difficulty random sequences with original ARM64 Random bodies."""
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile

PROJECT = Path(__file__).resolve().parents[1]
RECOVERY = PROJECT.parent
sys.path.insert(0, str(RECOVERY / 'tools/python'))
from elftools.elf.elffile import ELFFile
from unicorn import Uc, UC_ARCH_ARM64, UC_MODE_ARM, UC_HOOK_CODE
from unicorn.arm64_const import UC_ARM64_REG_X0, UC_ARM64_REG_X1, UC_ARM64_REG_X2, UC_ARM64_REG_X3, UC_ARM64_REG_LR, UC_ARM64_REG_SP, UC_ARM64_REG_PC

emu = Uc(UC_ARCH_ARM64, UC_MODE_ARM)
emu.mem_map(0, 0x6000000)
lib = RECOVERY.parent / 'RoyalMatch_20260929/apk_unpacked/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so'
with lib.open('rb') as stream:
    for segment in ELFFile(stream).iter_segments():
        if segment['p_type'] == 'PT_LOAD': emu.mem_write(segment['p_vaddr'], segment.data())
emu.mem_map(0x10000000, 0x10000)
emu.mem_map(0x20000000, 0x1000)

def pointer(address, value): emu.mem_write(address, value.to_bytes(8, 'little'))
def integer(address, value): emu.mem_write(address, (value & 0xffffffff).to_bytes(4, 'little'))
def read_int(address): return int.from_bytes(emu.mem_read(address, 4), 'little')
def list_length(): return read_int(0x10005018) if lifecycle_mode else len(shuffle_values)
def list_item(index):
    return int.from_bytes(emu.mem_read(0x10007020 + index * 2, 2), 'little') if lifecycle_mode else shuffle_values[index]
lifecycle_mode = False
tile_mode = False

# Resolve only allocation, GC bookkeeping and empty Object ctor. Random arithmetic runs native.
def hook(uc, address, size, data):
    global tile_list, tile_iterator
    if tile_mode and address == 0x260ebc4:
        uc.reg_write(UC_ARM64_REG_X2, 0x10003000)
        return # Execute original shuffle with generic list entry points resolved.
    if tile_mode and address == 0x39b08ec:
        key = uc.reg_read(UC_ARM64_REG_X1)
        if key in tile_cache: integer(uc.reg_read(UC_ARM64_REG_X2), tile_cache[key])
        uc.reg_write(UC_ARM64_REG_X0, int(key in tile_cache))
    elif tile_mode and address == 0x39aee60:
        key = uc.reg_read(UC_ARM64_REG_X1)
        assert key not in tile_cache
        tile_cache[key] = uc.reg_read(UC_ARM64_REG_X2)
    elif tile_mode and address == 0x39af254:
        tile_iterator = iter(tile_cache.values())
    elif tile_mode and address == 0x3171068:
        selected = next(tile_iterator, None)
        if selected is not None: integer(uc.reg_read(UC_ARM64_REG_X0) + 20, selected)
        uc.reg_write(UC_ARM64_REG_X0, int(selected is not None))
    elif tile_mode and address == 0x3171160:
        pass
    elif tile_mode and address == 0xffda78:
        uc.reg_write(UC_ARM64_REG_X0, 0x1000a300)
    elif tile_mode and address == 0x2c1b558:
        tile_list = list(range(len(tile_ids)))
        integer(0x1000a318, len(tile_ids))
    elif tile_mode and address in (0x2c1b9a0, 0x20000104):
        index = tile_list[uc.reg_read(UC_ARM64_REG_X1)]
        uc.reg_write(UC_ARM64_REG_X0, 0x1000b000 + index * 32)
    elif tile_mode and address == 0x20000100:
        uc.reg_write(UC_ARM64_REG_X0, len(tile_list))
    elif tile_mode and address == 0x20000108:
        tile_list[uc.reg_read(UC_ARM64_REG_X1)] = (uc.reg_read(UC_ARM64_REG_X2) - 0x1000b000) // 32
    elif address == 0xffd9d0:
        length = uc.reg_read(UC_ARM64_REG_X1)
        storage = 0x10002000 if uc.reg_read(UC_ARM64_REG_LR) == 0x2616594 else 0x10006000
        uc.mem_write(storage, bytes(0x1000))
        integer(storage + 0x18, length)
        pointer(storage, 0x10000380)
        uc.reg_write(UC_ARM64_REG_X0, storage)
    elif address == 0xffda78:
        pointer(0x10001000, 0x10001000)
        uc.reg_write(UC_ARM64_REG_X0, 0x10001000)
    elif address == 0xffda6c:
        uc.reg_write(UC_ARM64_REG_X0, 0)
    elif address == 0x1ed8f28:
        uc.reg_write(UC_ARM64_REG_X0, supplied_device_key)
    elif address == 0x25e50d8:
        uc.reg_write(UC_ARM64_REG_X0, attempt_index)
    elif address == 0x1e68b8c:
        uc.reg_write(UC_ARM64_REG_X0, list_item(uc.reg_read(UC_ARM64_REG_X1)))
    elif address == 0x20000100:
        uc.reg_write(UC_ARM64_REG_X0, list_length())
    elif address == 0x20000104:
        uc.reg_write(UC_ARM64_REG_X0, list_item(uc.reg_read(UC_ARM64_REG_X1)))
    elif address == 0x20000108:
        index, value = uc.reg_read(UC_ARM64_REG_X1), uc.reg_read(UC_ARM64_REG_X2) & 0xffff
        if lifecycle_mode: uc.mem_write(0x10007020 + index * 2, value.to_bytes(2, 'little'))
        else: shuffle_values[index] = value
    elif address not in (0xffd90c, 0x231007c, 0x2a2da14):
        return
    uc.reg_write(UC_ARM64_REG_PC, uc.reg_read(UC_ARM64_REG_LR))

emu.hook_add(UC_HOOK_CODE, hook)
emu.mem_write(0x57420a0, b'\x01')
# Constructor's Math class-initialization check needs a resolved, already initialized class.
pointer(0x53b3c40, 0x10000100)
pointer(0x10000100, 0x10000200)
integer(0x100002e0, 1)
# Virtual Sample slot used by Next(min,max).
pointer(0x10001178, 0x2616700)

def call(address, *args):
    for reg, value in zip((UC_ARM64_REG_X0, UC_ARM64_REG_X1, UC_ARM64_REG_X2, UC_ARM64_REG_X3), args):
        emu.reg_write(reg, value & 0xffffffffffffffff)
    emu.reg_write(UC_ARM64_REG_SP, 0x1000f000)
    emu.reg_write(UC_ARM64_REG_LR, 0x20000000)
    emu.emu_start(address, 0x20000000, count=1000000)
    if emu.reg_read(UC_ARM64_REG_PC) != 0x20000000: raise RuntimeError('Native call did not return')
    value = emu.reg_read(UC_ARM64_REG_X0) & 0xffffffff
    return value - 0x100000000 if value >= 0x80000000 else value

seeds = [0, 1, -1, 999, 1000, 38911, 161803398, 2147483647, -2147483648]
ranges = [(0, 6), (0, 1), (5, 5), (-100, 100), (0, 2147483647), (-2147483648, 2147483647)]
cases, expected = [], []
for seed in seeds:
    pointer(0x10001000, 0x10001000)
    call(0x2616540, 0x10001000, seed)
    for i in range(180):
        low, high = ranges[i % len(ranges)]
        cases.append(f'{seed}|{low}|{high}')
        expected.append(str(call(0x2616824, 0x10001000, low, high)))

sequence_count = len(cases)
shuffle_values = []
pointer(0x10001198, 0x2616824)
pointer(0x10003038, 0x10003100)
for offset, descriptor, address in [(8, 0x10003200, 0x20000100), (16, 0x10003220, 0x20000104), (24, 0x10003240, 0x20000108)]:
    pointer(0x10003100 + offset, descriptor)
    pointer(descriptor + 8, address)
pointer(0x10004038, 0x10001000)
for seed in seeds:
    for length in (0, 1, 2, 6, 55, 1000):
        pointer(0x10001000, 0x10001000)
        call(0x2616540, 0x10001000, seed)
        integer(0x10004028, 0)
        shuffle_values = list(range(length))
        call(0x260ecc4, 0x10004000, 0x10005000, 0x10003000)
        count = int.from_bytes(emu.mem_read(0x10004028, 4), 'little')
        cases.append(f'shuffle|{seed}|{length}')
        expected.append(','.join(map(str, shuffle_values)) + '|' + str(count))

shuffle_count = len(cases) - sequence_count
lifecycle_mode = True
for flag in (0x573ec63, 0x573ec64, 0x573ec66, 0x573963a): emu.mem_write(flag, b'\x01')
pointer(0x53bb4e0, 0x10000300)
pointer(0x10000300, 0x10008000)
pointer(0x100080b8, 0x10008200)
pointer(0x10008200, 0x10008300)
pointer(0x10008338, 0x10008400)
pointer(0x10008418, 0x10008500)
pointer(0x10008310, 0x10008600)
pointer(0x53b9db0, 0x10000320)
pointer(0x10000320, 0x10003000)
pointer(0x10004010, 0x10005000)
pointer(0x10005010, 0x10007000)
integer(0x10007018, 1000)
for level, user, supplied_device_key in [(1, 1, 12345), (15253, 4294967297, 99999), (2147483647, 2147483647, 9)]:
    integer(0x1000402c, 0)
    emu.mem_write(0x10004020, b'\x00')
    integer(0x10008514, level)
    pointer(0x10008610, user)
    for clear, attempt_index in [(False, 0), (False, 1), (False, 999), (False, 1000), (False, 2147483647), (True, 3)]:
        if clear: call(0x1ed9a00, 0x10004000)
        call(0x1ed92b4, 0x10004000)
        selected = read_int(0x10004024)
        count = read_int(0x10004028)
        following = call(0x1ed98c8, 0x10004000, 0, 6)
        cases.append(f'attempt|{level}|{user}|{supplied_device_key}|{attempt_index}|{int(clear)}')
        expected.append(f'{selected}|{count}|{following}')

attempt_count = len(cases) - sequence_count - shuffle_count
lifecycle_mode = False
emu.mem_write(0x573daa7, b'\x01')
pointer(0x10009058, 0x10009200)
pointer(0x1000a018, 0x1000a200)
for seed in (0, 38911, -2147483648):
    for tile_ids in ([1,2,3,4,5,6], [1], [1,1,2], [0,14,21], [1,2,14]):
        for must, replaced in ((0,0), (6,1), (7,1)):
            tile_mode = False
            pointer(0x10001000, 0x10001000)
            call(0x2616540, 0x10001000, seed)
            integer(0x10004028, 0)
            integer(0x10009050, must)
            integer(0x10009054, replaced)
            for i, value in enumerate(tile_ids): integer(0x1000b010 + i * 32, value)
            tile_cache = {}
            tile_mode = True
            results = []
            for item in (211,212,213,211,212,213):
                selected = call(0x1c24f70, 0x10009000, item, 0x1000a000, 0x10004000)
                results.append(str(selected) + ':' + str(read_int(0x10004028)))
            cases.append(f'tilecolors|{seed}|{must}|{replaced}|' + ','.join(map(str,tile_ids)))
            expected.append(','.join(results))

harness = '''using System;
using System.Collections.Generic;
using Whitebox.Foundation;
class Verify {
 static void Main() {
  OriginalLevelRandomSource r = null; int previous = 0; int count = 0;
  OriginalAttemptSeeds attempts = null; OriginalLevelRandomSource attemptRandom = null; int previousLevel = 0;
  string line; while ((line = Console.ReadLine()) != null) {
   string[] p = line.Split('|');
   if(p[0] == "tilecolors") {
    var tileRandom=new OriginalLevelRandomSource(int.Parse(p[1]));
    var tiles=new OriginalMatchTileColors(tileRandom,int.Parse(p[2]),int.Parse(p[3]));
    var ids=Array.ConvertAll(p[4].Split(','),int.Parse); var results=new List<string>();
    foreach(int item in new int[]{211,212,213,211,212,213}) results.Add(tiles.SelectMatchTileColor(item,ids)+":"+tileRandom.DifficultySeedGenerationCount);
    Console.WriteLine(string.Join(",",results));continue;
   }
   if (p[0] == "attempt") {
    int level = int.Parse(p[1]);
    if(attempts == null || level != previousLevel) { attemptRandom = new OriginalLevelRandomSource(0); attempts = new OriginalAttemptSeeds(attemptRandom); previousLevel = level; }
    if(p[5] == "1") attempts.ClearUserCaches();
    attempts.ResetDifficultySeed(level,long.Parse(p[2]),int.Parse(p[3]),int.Parse(p[4]));
    Console.WriteLine(attemptRandom.DifficultySeed + "|" + attemptRandom.DifficultySeedGenerationCount + "|" + attemptRandom.DifficultyNext(0,6)); continue;
   }
   if (p[0] == "shuffle") {
    var shuffle = new OriginalLevelRandomSource(int.Parse(p[1]));
    var values = new List<ushort>(); for(int i=0;i<int.Parse(p[2]);i++) values.Add((ushort)i);
    shuffle.ShuffleListBasedOnDifficulty(values);
    Console.WriteLine(string.Join(",",values) + "|" + shuffle.DifficultySeedGenerationCount); continue;
   }
   int seed = int.Parse(p[0]);
   if (r == null || seed != previous) { r = new OriginalLevelRandomSource(seed); previous = seed; count = 0; }
   Console.WriteLine(r.DifficultyNext(int.Parse(p[1]), int.Parse(p[2])));
   if (r.DifficultySeedGenerationCount != ++count || r.DifficultySeed != seed) throw new Exception("state");
  }
  r.SetDifficultySeed(38911);
  var fresh = new OriginalLevelRandomSource(38911);
  var candidates = new List<byte> { 1, 3, 6 };
  for (int i=0;i<100;i++) if(r.GetRandomItemFromListBasedOnDifficulty(candidates) != candidates[fresh.DifficultyNext(0,3)]) throw new Exception("list");
 }
}'''
mono = Path(os.environ.get('ROYAL_MONO_ROOT', 'C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory() as folder:
    folder = Path(folder)
    source = folder / 'Verify.cs'; source.write_text(harness, encoding='utf-8')
    output = folder / 'Verify.exe'
    runtime = PROJECT / 'Assets/Foundation/Runtime'
    subprocess.run([str(mono / 'bin/mono.exe'), str(mono / 'lib/mono/4.5/mcs.exe'), '-out:' + str(output), str(source), str(runtime / 'OriginalMatchItemRandomizer.cs'), str(runtime / 'OriginalLevelRandomSource.cs'), str(runtime / 'OriginalAttemptSeeds.cs'), str(runtime / 'OriginalMatchTileColors.cs'), str(runtime / 'OriginalLevelRules.cs')], check=True, capture_output=True)
    result = subprocess.run([str(mono / 'bin/mono.exe'), str(output)], input='\n'.join(cases)+'\n', text=True, encoding='utf-8', capture_output=True, check=True)
    actual = result.stdout.lstrip('\ufeff').splitlines()
    if actual != expected:
        for i, (a, e) in enumerate(zip(actual, expected)):
            if a != e: raise AssertionError((cases[i], a, e))
        raise AssertionError((len(actual), len(expected)))
report = {'passed': True, 'native_sequence_values': sequence_count, 'native_shuffle_cases': shuffle_count, 'native_attempt_cases': len(cases) - sequence_count - shuffle_count, 'seeds': seeds, 'original_rvas': ['0x2616540', '0x2616720', '0x2616824', '0x260ecc4', '0x1ed98c8', '0x1ed95bc', '0x1ed92b4', '0x1ed9a00'], 'scope': 'Seeded sequences, list shuffle, generation/selection/reuse of attempt seeds and cache invalidation. Device ID parsing and storage reads are supplied at their boundaries; their implementations and full game initialization are not verified.'}
report['native_attempt_cases'] = attempt_count
report['native_tile_color_sequences'] = len(cases) - sequence_count - shuffle_count - attempt_count
report['original_rvas'].extend(['0x1c24f70', '0x260ebc4'])
report['scope'] += ' Special tile selection, replacement and repeated cache calls are compared with original native branches; dictionary/list storage operations are supplied at managed boundaries.'
(PROJECT / 'random_source_verification.json').write_text(json.dumps(report, indent=2)+'\n', encoding='utf-8')
print(json.dumps(report))
