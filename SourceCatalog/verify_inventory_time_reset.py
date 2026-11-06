"""Execute original ResetTimes, scalar time writes and unlimited-booster serialization.

Real ARM64 ResetTimes, packing, model stores, BitArrayOperations clear/copy/trim
and SaveAndSyncData(false) execute. BCL arrays/string/builder/formatting and typed
table writes are supplied ports; diagnostic logging is skipped. Tests compare
retained bits, identity/cached string, writes/order, failures and callback changes.
True backend-sync/migration branches and production startup/mobile are not proved.
"""
from pathlib import Path
import base64, hashlib, json, random, struct, subprocess, sys, tempfile
PROJECT = Path(__file__).resolve().parents[1]; BASE = PROJECT.parent
sys.path.insert(0, str(BASE / 'tools/python'))
from elftools.elf.elffile import ELFFile
from elftools.elf.relocation import RelocationSection
from unicorn import Uc, UC_ARCH_ARM64, UC_MODE_ARM, UC_HOOK_CODE
import unicorn.arm64_const as r
from native_inventory_time_reset import InventoryTimeResetNative
u = Uc(UC_ARCH_ARM64, UC_MODE_ARM)
u.mem_map(0, 0x6000000); u.mem_map(0x10000000, 0x400000); u.mem_map(0x20000000, 0x3000)
def put(p, value, size=8): u.mem_write(p, (int(value) & ((1 << (size * 8)) - 1)).to_bytes(size, 'little'))
def get(p, size=8, signed=False): return int.from_bytes(u.mem_read(p, size), 'little', signed=signed)
def sx(i, value): u.reg_write(getattr(r, 'UC_ARM64_REG_X' + str(i)), int(value) & ((1 << 64) - 1))
def x(i): return u.reg_read(getattr(r, 'UC_ARM64_REG_X' + str(i)))
def returned(value=0): sx(0, value); u.reg_write(r.UC_ARM64_REG_PC, u.reg_read(r.UC_ARM64_REG_LR))
def enc(text): return '~' if text is None else base64.b64encode(text.encode()).decode()
with (BASE.parent / 'RoyalMatch_20260929/apk_unpacked/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so').open('rb') as stream:
    elf = ELFFile(stream)
    for segment in elf.iter_segments():
        if segment['p_type'] == 'PT_LOAD': u.mem_write(segment['p_vaddr'], segment.data())
    for section in elf.iter_sections():
        if isinstance(section, RelocationSection):
            for entry in section.iter_relocations():
                if entry['r_info_type'] == 1027: put(entry['r_offset'], entry['r_addend'])
owner = 0x10001000; metadata = 0x10200000
v = []; trace = []; error = ''; writes = 0; original = other = 0
def failure(kind):
    global error
    error = kind; u.emu_stop()
def mutate(kind):
    if kind == 1: put(owner + 0x1c, 0x1234ffff, 4)
    elif kind == 2: put(owner + 0x48, (1 << 63) - 1)
    elif kind == 3: put(owner + 0x60, -1)
    elif kind == 4: put(owner + 0x58, -1)
    elif kind == 5: put(owner + 0xd8, 0)
    elif kind == 6: put(owner + 0xd8, other)
    elif kind == 7: put(owner + 0xe0, helper.new_string(''))
    elif kind == 8: put(owner + 0x58, 0)
    elif kind == 9: put(owner + 0x1c, -1, 4)
def write(kind, key, value):
    global writes
    if v[12]: failure('null'); return False
    text = value if kind == 'string' else helper.number(value)
    trace.append(key + ',' + enc(text)); writes += 1
    if writes == v[9]: failure('write'); return False
    if writes == v[10]: mutate(v[11])
    return True
helper = InventoryTimeResetNative(u, r, write, failure)
def hook(uc, address, size, unused):
    global metadata
    if address == 0xffd960:
        slot = x(0); put(slot, metadata); put(metadata + 0xe0, 1, 4)
        helper.initialize_metadata(slot, metadata); metadata += 0x200; returned(); return
    if address in (0xffd90c, 0xffda64): returned(); return
    if address == 0xffda88: failure('null'); return
    if helper.handle(address): return
    raise AssertionError(hex(address))
for address in set(helper.addresses) | {0xffd960, 0xffd90c, 0xffda64, 0xffda88}:
    u.hook_add(UC_HOOK_CODE, hook, begin=address, end=address)
methods = [0x28e7184, 0x28e7220, 0x28e7394, 0x28e7508, 0x28e70a8, 0x28e85d0, 0x2d7b500, 0x28e7184]
def call(method, target):
    for i in range(8): sx(i, 0)
    sx(0, target); sx(1, v[1]); u.reg_write(r.UC_ARM64_REG_SP, 0x103ff000); u.reg_write(r.UC_ARM64_REG_LR, 0x20000000)
    try: u.emu_start(method, 0x20000000, count=100000)
    except Exception as problem: raise AssertionError((v, hex(u.reg_read(r.UC_ARM64_REG_PC)), trace)) from problem
    assert error or u.reg_read(r.UC_ARM64_REG_PC) == 0x20000000, (v, hex(u.reg_read(r.UC_ARM64_REG_PC)), trace)
    return x(0)
def bits(pointer): return '~' if not pointer else ''.join('1' if bit else '0' for bit in helper.bitarrays[pointer])
def native(case):
    global v, trace, writes, error, original, other
    v, data, cached = case; trace = []; writes = 0; error = ''
    helper.prepare('~' if v[13] else '-'); u.mem_write(owner, bytes(0x100))
    for offset, value in ((0x38, v[2]), (0x3c, v[3]), (0x40, v[4]), (0x1c, v[5])): put(owner + offset, value, 4)
    for offset, value in ((0x48, v[6]), (0x60, v[7]), (0x58, v[8])): put(owner + offset, value)
    original = helper.new_bits(None if data == '~' else [char == '1' for char in data]); other = helper.new_bits([True] * 200)
    put(owner + 0xd8, original); put(owner + 0xe0, helper.new_string(cached))
    value = call(methods[v[0]], original if v[0] == 6 else owner)
    result = helper.string(value) if not error and v[0] == 6 else None
    if v[0] == 7 and not error: call(methods[7], owner)
    pointer = get(owner + 0xd8); identity = -1 if not pointer else 0 if pointer == original else 1
    state = [get(owner + a, 4, True) for a in (0x38, 0x3c, 0x40, 0x1c)] + [get(owner + a, signed=True) for a in (0x48, 0x60, 0x58)] + [identity]
    return ','.join(map(str, state)) + ',' + enc(helper.string(get(owner + 0xe0))) + ',' + enc(result) + ',' + error + '|' + bits(original) + '|' + bits(other) + '#' + ';'.join(trace)
base = [0, 0, 123, 456, 789, -1, -1, -1, -1, 0, 0, 0, 0, 0]
cases = []
for op in range(len(methods)):
    for data in ('~', '', '1' * 191, '1' * 192, '1' * 193, '0' * 192, '0' * 200, '1' * 200, '10' * 128):
        for cached in (None, '', '0', '-1', 'noncanonical'):
            c = base.copy(); c[0] = op; cases.append((c, data, cached))
for fail in range(1, 17):
    c = base.copy(); c[0] = 7; c[9] = fail; cases.append((c, '1' * 200, None))
for kind in range(1, 10):
    for at in range(1, 9):
        c = base.copy(); c[10:12] = [at, kind]; cases.append((c, '1' * 200, None))
rng = random.Random(0x28e7184)
limits = [-(1 << 63), (1 << 63) - 1, -(1 << 31), (1 << 31) - 1, -65536, -1, 0, 1, 65535, 65536]
for _ in range(1800):
    c = base.copy(); c[0] = rng.randrange(8); c[1:9] = [rng.choice(limits + [rng.randrange(-(1 << 63), (1 << 63))]) for _ in range(8)]
    c[9] = rng.choice((0, 0, 0, rng.randrange(1, 17))); c[10:12] = [rng.randrange(9), rng.randrange(10)]
    c[12] = rng.choice((0, 0, 0, 1)); c[13] = rng.randrange(2)
    length = rng.choice((0, 1, 7, 8, 9, 128, 191, 192, 193, 200, 256, 512))
    data = ''.join(str(rng.randrange(2)) for _ in range(length)); cached = rng.choice((None, '', '0', '-1', '~1', 'noncanonical'))
    cases.append((c, data, cached))
expected = [native(c) for c in cases]
mono = Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
sources = ['SourceCatalog/InventoryTimeResetHarness.cs'] + ['Assets/Foundation/Runtime/' + name + '.cs' for name in (
    'OriginalInventoryTimeReset', 'OriginalLifeData', 'OriginalBasicLevelProgress', 'OriginalUserInventoryBalances', 'OriginalLocalAccountLoad', 'OriginalKeyValueStorage')]
with tempfile.TemporaryDirectory() as directory:
    exe = Path(directory) / 'Inventory.exe'
    args = [str(mono.parent / 'NetCoreRuntime/dotnet.exe'), str(mono.parent / 'DotNetSdkRoslyn/csc.dll'), '-nologo', '-nostdlib+',
        *['-r:' + str(mono / 'lib/mono/4.5' / n) for n in ('mscorlib.dll', 'System.dll', 'System.Core.dll')], '-out:' + str(exe), *[str(PROJECT / p) for p in sources]]
    result = subprocess.run(args, capture_output=True, text=True, encoding='utf-8'); assert result.returncode == 0, result.stdout + result.stderr
    lines = [','.join(map(str, c)) + '|' + data + '|' + enc(cached) for c, data, cached in cases]
    result = subprocess.run([str(mono / 'bin/mono.exe'), str(exe)], input='\n'.join(lines) + '\n', capture_output=True, text=True, encoding='utf-8')
    assert result.returncode == 0, result.stdout + result.stderr
    actual = result.stdout.lstrip('\ufeff').splitlines(); assert len(actual) == len(expected)
    for i, (a, b) in enumerate(zip(actual, expected)): assert a == b, (i, cases[i], a, b)
report = {'passed': True, 'cases': len(cases), 'scope': __doc__.strip(), 'original_rvas': [hex(n) for n in dict.fromkeys(methods)] +
    ['0x28e767c', '0x28e8f20', '0x28e78d0', '0x28ea568', '0x2d7b558', '0x2d7b5e8', '0x2d7b1dc', '0x2d7bbc0'],
    'files': [{'path': p, 'sha256': hashlib.sha256((PROJECT / p).read_bytes()).hexdigest()} for p in sources + ['SourceCatalog/verify_inventory_time_reset.py', 'SourceCatalog/native_inventory_time_reset.py']]}
(PROJECT / 'inventory_time_reset_native_verification.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
print(json.dumps(report, indent=2))
