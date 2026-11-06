"""Compare life runtime behavior with original ARM64 LifeHelper and life data writes.

Actual original query/mutation bodies, nested life calls and model stores execute
from ELF. User gold/weekly-pass status, server clock, application/context queries,
analytics and table writes are explicit ports. Diagnostic log construction is
skipped; BasicUserData/Chest model assignments and their table call execute.
This does not restore TimeHelper/backend/app lifecycle or bind production startup.
"""
from pathlib import Path
import hashlib
import json
import random
import struct
import subprocess
import sys
import tempfile

PROJECT = Path(__file__).resolve().parents[1]
BASE = PROJECT.parent
sys.path.insert(0, str(BASE / 'tools/python'))
from elftools.elf.elffile import ELFFile
from elftools.elf.relocation import RelocationSection
from unicorn import Uc, UC_ARCH_ARM64, UC_MODE_ARM, UC_HOOK_CODE
import unicorn.arm64_const as r

u = Uc(UC_ARCH_ARM64, UC_MODE_ARM)
u.mem_map(0, 0x6000000)
u.mem_map(0x10000000, 0x400000)
u.mem_map(0x20000000, 0x3000)


def put(p, value, size=8):
    u.mem_write(p, (int(value) & ((1 << (size * 8)) - 1)).to_bytes(size, 'little'))


def get(p, size=8, signed=False):
    return int.from_bytes(u.mem_read(p, size), 'little', signed=signed)


def x(i):
    return u.reg_read(getattr(r, 'UC_ARM64_REG_X' + str(i)))


def set_x(i, value):
    u.reg_write(getattr(r, 'UC_ARM64_REG_X' + str(i)), int(value) & ((1 << 64) - 1))


def signed(value, bits=64):
    value &= (1 << bits) - 1
    return value - (1 << bits) if value >> (bits - 1) else value


def returned(value=0):
    set_x(0, value)
    u.reg_write(r.UC_ARM64_REG_PC, u.reg_read(r.UC_ARM64_REG_LR))


with (BASE.parent / 'RoyalMatch_20260929/apk_unpacked/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so').open('rb') as stream:
    elf = ELFFile(stream)
    for segment in elf.iter_segments():
        if segment['p_type'] == 'PT_LOAD': u.mem_write(segment['p_vaddr'], segment.data())
    for section in elf.iter_sections():
        if isinstance(section, RelocationSection):
            for entry in section.iter_relocations():
                if entry['r_info_type'] == 1027: put(entry['r_offset'], entry['r_addend'])

life, clock, user, basic, inventory, other, second, second_inventory, app = [0x10001000 + i * 0x1000 for i in range(9)]
static_user, static_active, full_key, unlimited_key, chest_key = [0x10010000 + i * 0x1000 for i in range(5)]
metadata = 0x10200000
user_metadata_slot, active_metadata_slot = get(0x53bb4e0), get(0x53d0ae8)
user_type = active_type = 0
trace = []
v = []
clock_reads = writes = 0
gold = back = False
error = ''


def hook(uc, address, size, unused):
    global metadata, user_type, active_type, clock_reads, writes, gold, back, error
    if address == 0xffd960:
        slot = x(0)
        put(slot, metadata); put(metadata + 0xe0, 1, 4)
        if slot == user_metadata_slot:
            user_type = metadata; put(metadata + 0xb8, static_user)
        if slot == active_metadata_slot:
            active_type = metadata; put(metadata + 0xb8, static_active)
        metadata += 0x200; returned(); return
    if address in (0xffda64, 0xffd90c): returned(); return
    if address == 0xffda88:
        error = 'null'; u.emu_stop(); return
    if address == 0x30f6400:
        if x(0) == 16:
            trace.append('time-owner'); returned(0 if v[6] & 8 else clock)
        elif x(0) == 3: returned(app)
        else: raise AssertionError(('context', x(0)))
        return
    if address == 0xffda78: returned(0x10020000); return
    if address == 0x200297c: returned(); return
    if address == 0x2012fcc: trace.append('subscribe'); returned(); return
    if address == 0x3385a2c:
        trace.append('p,' + str(int(gold))); returned(gold); return
    if address == 0x1e1e164:
        trace.append('back,' + str(int(back))); return
    if address == 0x239649c:
        now = signed(v[12] + clock_reads * v[13]); clock_reads += 1
        trace.append('t,' + str(now))
        if clock_reads == v[8]:
            change = v[9]
            if change == 1: put(static_user, other)
            elif change == 2: put(user + 0x18, second)
            elif change == 3: put(user + 0x18, 0)
            elif change == 4: put(basic + 0x28, now + 3600000)
            elif change == 5: put(basic + 0x30, now + 10000)
            elif change == 6: gold = not gold
            elif change == 7: back = not back; put(clock + 0x11, back, 1)
            elif change == 8: put(static_user, 0)
            elif change == 9: put(user + 0x20, second_inventory)
        returned(now); return
    if address == 0x1e1e328:
        trace.append('active,' + str(v[4])); return
    if address == 0x200f3dc: trace.append('paused,' + str(v[5])); returned(v[5]); return
    if address == 0x239773c: trace.append('recalculate'); returned(); return
    if address == 0x1ff9470: trace.append('track,' + str(signed(x(0), 32))); returned(); return
    if address == 0x1e1df9c:
        u.reg_write(r.UC_ARM64_REG_PC, 0x1e1e038); return
    # Preserve each actual prologue/epilogue, store and table call. Skip only
    # formatting/allocating the diagnostic old/new value log.
    skips = {0x2f824bc: (0x2f82588, full_key), 0x2f82340: (0x2f8240c, unlimited_key), 0x28e5218: (0x28e52e4, chest_key)}
    if address in skips:
        target, key = skips[address]; set_x(24, key); u.reg_write(r.UC_ARM64_REG_PC, target); return
    if address in (0x25e514c, 0x25e50ac):
        label = {full_key: 'F,', unlimited_key: 'U,', chest_key: 'C,'}[x(0)]
        value = signed(x(1), 32 if address == 0x25e50ac else 64)
        trace.append(label + str(value)); writes += 1
        if writes == v[7]: error = 'write'; u.emu_stop(); return
        returned(1); return
    if address == 0x2657780: returned(max(signed(x(0), 32), signed(x(1), 32))); return
    if address == 0x265778c: returned(max(signed(x(0)), signed(x(1)))); return
    if address == 0x2657978: returned(min(signed(x(0), 32), signed(x(1), 32))); return
    if address in (0x261d17c, 0x261d064):
        value = struct.unpack('<d', u.reg_read(r.UC_ARM64_REG_D0).to_bytes(8, 'little'))[0]
        returned(int(value * (60000 if address == 0x261d064 else 1000)) * 10000); return
    if address == 0x261cb90:
        value = get(x(0), signed=True) / 10000
        u.reg_write(r.UC_ARM64_REG_D0, int.from_bytes(struct.pack('<d', value), 'little')); returned(); return
    if address == 0x231007c: returned(); return
    raise AssertionError(hex(address))


hooks = (0xffd960, 0xffda64, 0xffd90c, 0xffda88, 0x30f6400, 0xffda78, 0x200297c, 0x2012fcc,
         0x3385a2c, 0x1e1e164, 0x239649c, 0x1e1e328, 0x200f3dc, 0x239773c, 0x1ff9470,
         0x1e1df9c, 0x2f824bc, 0x2f82340, 0x28e5218, 0x25e514c, 0x25e50ac,
         0x2657780, 0x265778c, 0x2657978, 0x261d17c, 0x261d064, 0x261cb90, 0x231007c)
for address in hooks: u.hook_add(UC_HOOK_CODE, hook, begin=address, end=address)

methods = [0x1e1dd84, 0x1e1dee4, 0x1e1e07c, 0x1e1e11c, 0x1e1bd0c, 0x1e1e154,
           0x1e1e1e4, 0x1e1e25c, 0x1e1e394, 0x1e1e470, 0x1e1e558, 0x1e1e58c,
           0x1e1e668, 0x1e1e724, 0x1e1e7b8, 0x2f8244c, 0x2f822d0, 0x2f825c8,
           0x28e516c, 0x28e5180, 0x1e1e908]


def invoke(method, owner, amount=0):
    for i in range(8): set_x(i, 0)
    set_x(0, owner); set_x(1, amount)
    u.reg_write(r.UC_ARM64_REG_SP, 0x103ff000); u.reg_write(r.UC_ARM64_REG_LR, 0x20000000)
    try: u.emu_start(method, 0x20000000, count=20000)
    except Exception as failure: raise AssertionError((v, hex(method), hex(u.reg_read(r.UC_ARM64_REG_PC)), hex(x(8)), hex(get(0x53bb4e0)), hex(get(get(0x53bb4e0))), hex(get(static_user)), trace)) from failure
    assert error or u.reg_read(r.UC_ARM64_REG_PC) == 0x20000000, hex(u.reg_read(r.UC_ARM64_REG_PC))
    return signed(x(0))


def native(case):
    global v, trace, clock_reads, writes, gold, back, error
    v = case; trace = []; clock_reads = writes = 0; gold = bool(v[2]); back = bool(v[3]); error = ''
    for ptr in (life, clock, user, basic, inventory, other, second, second_inventory, app): u.mem_write(ptr, bytes(0x100))
    put(static_user, 0 if v[6] & 1 else user); put(static_active + 0xc, v[4], 1)
    put(user + 0x18, 0 if v[6] & 2 else basic); put(user + 0x20, 0 if v[6] & 4 else inventory)
    put(other + 0x18, second); put(other + 0x20, second_inventory)
    put(basic + 0x28, v[10]); put(basic + 0x30, v[11]); put(inventory + 0x1c, v[14], 4)
    put(second + 0x28, v[10] + 12345); put(second + 0x30, v[11] + 54321); put(second_inventory + 0x1c, v[14] + 70000, 4)
    put(clock + 0x11, back, 1)
    for key in (full_key, unlimited_key, chest_key): put(key, key)
    invoke(0x1e1e908, life)
    assert get(life + 0x10) == 1800000
    invoke(0x1e1ddec, life)
    result = 0
    owner = basic if 15 <= v[0] <= 17 else inventory if v[0] in (18, 19) else life
    value = invoke(methods[v[0]], owner, v[1])
    if not error and v[0] in (0, 2, 3, 4, 5, 9, 10, 11, 12, 18, 20):
        result = get(life + 0x10) if v[0] == 20 else signed(value, 32) if v[0] in (0, 2, 3, 4, 5, 9, 11, 18) else value
    state = [result, get(basic + 0x28, signed=True), get(basic + 0x30, signed=True), get(inventory + 0x1c, 4, True),
             get(second + 0x28, signed=True), get(second + 0x30, signed=True), get(second_inventory + 0x1c, 4, True)]
    return ','.join(map(str, state)) + ',' + error + '#' + ';'.join(trace)


cases = []
base = [0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 1700000000000, 0, 1700000000000, 0, 0]
for op in range(len(methods)):
    for gold_flag in (0, 1):
        for delta in (-1, 0, 1, 999, 1000, 1799999, 1800000, 1800001, 7199999, 7200000, 7200001, 9000000, 10800000, 16200000):
            case = base.copy(); case[0] = op; case[2] = gold_flag; case[10] += delta; case[11] = case[12] + delta; cases.append(case)
    for missing in (1, 2, 4, 8, 15):
        case = base.copy(); case[0] = op; case[6] = missing; cases.append(case)
    for fail in (1, 2):
        case = base.copy(); case[0] = op; case[7] = fail; case[11] = case[12] - 1; case[14] = 12; cases.append(case)
    for mutation in range(1, 10):
        for clock_read in (1, 2, 3):
            case = base.copy(); case[0] = op; case[8:10] = [clock_read, mutation]; case[10] += 10800000; case[14] = 12; cases.append(case)
rng = random.Random(0x1e1e908)
limits = [-(1 << 63), (1 << 63) - 1, -(1 << 31), (1 << 31) - 1, -65536, -60, -1, 0, 1, 60, 65535, 65536]
for _ in range(1800):
    case = base.copy(); case[0] = rng.randrange(len(methods)); case[1] = rng.choice(limits + [rng.randrange(-10000, 10000)])
    case[2:6] = [rng.randrange(2) for _ in range(4)]; case[6] = rng.choice((0, 0, 0, 1, 2, 4, 8))
    case[7:10] = [rng.randrange(3), rng.randrange(5), rng.randrange(10)]
    case[10:13] = [rng.choice(limits + [1700000000000 + rng.randrange(-20000000, 20000000)]) for _ in range(3)]
    case[13] = rng.choice((0, 0, 1, 1000, -1000)); case[14] = rng.choice(limits)
    cases.append(case)
expected = [native(case) for case in cases]
mono = Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
sources = ['SourceCatalog/LifeHelperHarness.cs'] + ['Assets/Foundation/Runtime/' + name + '.cs' for name in (
    'OriginalLifeHelper', 'OriginalLifeData', 'OriginalBasicLevelProgress', 'OriginalUserInventoryBalances',
    'OriginalLocalAccountLoad', 'OriginalKeyValueStorage')]
with tempfile.TemporaryDirectory() as directory:
    exe = Path(directory) / 'Life.exe'
    args = [str(mono.parent / 'NetCoreRuntime/dotnet.exe'), str(mono.parent / 'DotNetSdkRoslyn/csc.dll'), '-nologo', '-nostdlib+',
            *['-r:' + str(mono / 'lib/mono/4.5' / name) for name in ('mscorlib.dll', 'System.dll', 'System.Core.dll')],
            '-out:' + str(exe), *[str(PROJECT / path) for path in sources]]
    result = subprocess.run(args, capture_output=True, text=True, encoding='utf-8'); assert result.returncode == 0, result.stdout + result.stderr
    result = subprocess.run([str(mono / 'bin/mono.exe'), str(exe)], input='\n'.join(','.join(map(str, c)) for c in cases) + '\n', capture_output=True, text=True, encoding='utf-8')
    assert result.returncode == 0, result.stdout + result.stderr
    actual = result.stdout.lstrip('\ufeff').splitlines(); assert len(actual) == len(expected)
    for i, (a, b) in enumerate(zip(actual, expected)): assert a == b, (i, cases[i], a, b)
report = {'passed': True, 'cases': len(cases), 'scope': __doc__.strip(), 'original_rvas': [hex(n) for n in methods] + ['0x1e1ddec', '0x28e51ac', '0x2d7bb28'],
          'files': [{'path': path, 'sha256': hashlib.sha256((PROJECT / path).read_bytes()).hexdigest()} for path in sources + ['SourceCatalog/verify_life_helper.py']]}
(PROJECT / 'life_helper_native_verification.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
print(json.dumps(report, indent=2))
