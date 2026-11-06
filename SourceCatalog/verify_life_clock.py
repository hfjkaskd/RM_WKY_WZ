"""Differential execution of TimeHelper life-clock state, lifecycle and UpdateTime.

Execute actual ARM64 methods and nested clock methods. Life queries, client time,
feature/time-hack checks, ping and typed table writes are explicit ports. Actual
ResetTimes, packing, BitArray reset/serialization and cache stores now also run
from ELF; their BCL operations are ports. Diagnostic logging is skipped. Bind is limited to its two clock
reads/stores. This does not restore the HTTP/ping implementation, full context
binding, life-hack detector or full production application lifecycle.
"""
from pathlib import Path
import hashlib, json, random, struct, subprocess, sys, tempfile
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
def x(i): return u.reg_read(getattr(r, 'UC_ARM64_REG_X' + str(i)))
def sx(i, value): u.reg_write(getattr(r, 'UC_ARM64_REG_X' + str(i)), int(value) & ((1 << 64) - 1))
def signed(value, bits=64):
    value &= (1 << bits) - 1
    return value - (1 << bits) if value >> (bits - 1) else value
def returned(value=0): sx(0, value); u.reg_write(r.UC_ARM64_REG_PC, u.reg_read(r.UC_ARM64_REG_LR))
with (BASE.parent / 'RoyalMatch_20260929/apk_unpacked/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so').open('rb') as stream:
    elf = ELFFile(stream)
    for segment in elf.iter_segments():
        if segment['p_type'] == 'PT_LOAD': u.mem_write(segment['p_vaddr'], segment.data())
    for section in elf.iter_sections():
        if isinstance(section, RelocationSection):
            for entry in section.iter_relocations():
                if entry['r_info_type'] == 1027: put(entry['r_offset'], entry['r_addend'])
owner, life, user, basic, second, inventory, daily, weekly, tracker, delegate = [0x10001000 + i * 0x1000 for i in range(10)]
pub, user_static, app_static, full_key, unlimited_key = [0x10020000 + i * 0x1000 for i in range(5)]
user_slot, pub_slot, app_slot = [get(a) for a in (0x53bb4e0, 0x533ebe8, 0x53d0ae8)]
key_slots = {get(a): key for a, key in ((0x53dfed8, 'STD'), (0x536f8a8, 'LCT'), (0x53a02b8, 'LST'),
             (0x5351ab8, 'OULT'), (0x53dad18, 'LCP'), (0x5341108, 'ELC'), (0x53a2218, 'ST'))}
key_values = {}; metadata = 0x10200000
keys = ['LCP', 'LST', 'OULT', 'ELC', 'ST', 'STD', 'LCT']
v = []; rows = {}; trace = []; reads = ports = 0; error = ''
def record(value):
    global ports, error
    trace.append(value); ports += 1
    if ports == v[23]: error = 'port'; u.emu_stop(); return False
    return True
def mutate(change):
    if change == 1: put(owner + 0x60, 7777)
    elif change == 2: put(owner + 0x10, not get(owner + 0x10, 1), 1)
    elif change == 3: put(owner + 0x48, 0)
    elif change == 4: put(owner + 0x11, not get(owner + 0x11, 1), 1)
    elif change == 5: put(owner + 0x68, 7, 4)
    elif change == 6: put(user + 0x18, second)
    elif change == 7: put(user + 0x18, 0)
    elif change == 8: put(pub, 1, 1)
def reset_failure(kind):
    global error
    error = kind; u.emu_stop()
def reset_write(kind, key, value):
    return record('user,' + key + ',' + (value if kind == 'string' else str(value)))
reset = InventoryTimeResetNative(u, r, reset_write, reset_failure)
def hook(uc, address, size, unused):
    global metadata, reads, error
    if address == 0xffd960:
        slot = x(0); put(slot, metadata); put(metadata + 0xe0, 1, 4)
        if slot == user_slot: put(metadata + 0xb8, user_static)
        if slot == pub_slot: put(metadata + 0xb8, pub)
        if slot == app_slot: put(metadata + 0xb8, app_static)
        if slot in key_slots: key_values[metadata] = key_slots[slot]
        reset.initialize_metadata(slot, metadata)
        metadata += 0x200; returned(); return
    if address in (0xffda64, 0xffd90c): returned(); return
    if address == 0xffda88: error = 'null'; u.emu_stop(); return
    if reset.handle(address): return
    jumps = {0x2395cd4: 0x2395e10, 0x2395d94: 0x2395e10, 0x2395e7c: 0x2395ee0,
             0x2396060: 0x2396310, 0x2396208: 0x2396310, 0x2396364: 0x2396424,
             0x2393c6c: 0x2393cd0, 0x2393dd8: 0x2393e54, 0x2393ef0: 0x2393f68,
             0x2395a10: 0x2395bc4}
    if address in jumps: u.reg_write(r.UC_ARM64_REG_PC, jumps[address]); return
    if address == 0x2395948:
        sx(28, get(0x536f8a8)); u.reg_write(r.UC_ARM64_REG_PC, 0x23959fc); return
    if address == 0x29d6ef8:
        now = signed(v[2] + reads * v[3]); reads += 1
        if not record('now,' + str(now)): return
        if reads == v[24]: mutate(v[25])
        returned(now); return
    if address == 0x1e1dd84:
        if record('max,' + str(v[11])): returned(v[11])
        return
    life_ports = {0x1e1e07c: ('l,', 10), 0x1e1e558: ('passed,', 12), 0x1e1e668: ('unlimited,', 13)}
    if address in life_ports:
        label, index = life_ports[address]
        if record(label + str(v[index])): returned(v[index])
        return
    if address == 0x238dd60:
        if record('ping,' + str(x(1))): returned()
        return
    if address == 0x29e27cc:
        is_daily = x(0) == 82; assert is_daily or x(0) == 85
        if record('daily' if is_daily else 'weekly'): returned(0 if v[22] & (2 if is_daily else 4) else daily if is_daily else weekly)
        return
    checks = {0x1f94570: ('daily-check,', 28), 0x28b52b4: ('weekly-check,', 29), 0x2396580: ('hack,', 30)}
    if address in checks:
        label, index = checks[address]
        if record(label + str(signed(x(1)))): returned(v[index])
        return
    if address == 0x20001000:
        if record('validated'): mutate(v[32]); returned()
        return
    if address in (0x25d4ccc, 0x25d4edc):
        key = key_values[x(0)]; integer = address == 0x25d4ccc
        default = signed(x(1), 32 if integer else 64)
        if record(('gi,' if integer else 'gl,') + key + ',' + str(default)):
            value = rows.get(key, default); returned(signed(value, 32) if integer else value)
        return
    if address in (0x25d4bc4, 0x25d4dd4):
        key = key_values[x(0)]; integer = address == 0x25d4bc4; value = signed(x(1), 32 if integer else 64)
        if record(('si,' if integer else 'sl,') + key + ',' + str(value)): rows[key] = value; returned(1)
        return
    if address == 0x25d499c:
        key = key_values[x(0)]
        if record('del,' + key): present = key in rows; rows.pop(key, None); returned(present)
        return
    if address in (0x2f824bc, 0x2f82340):
        sx(24, full_key if address == 0x2f824bc else unlimited_key)
        u.reg_write(r.UC_ARM64_REG_PC, 0x2f82588 if address == 0x2f824bc else 0x2f8240c); return
    if address == 0x25e514c:
        key = 'FullLivesTimeInMs' if x(0) == full_key else 'UnlimitedLivesEndTimeInMs'
        if record('user,' + key + ',' + str(signed(x(1)))): returned(1)
        return
    raise AssertionError(hex(address))
hooks = (0xffd960, 0xffda64, 0xffd90c, 0xffda88, 0x2395cd4, 0x2395d94, 0x2395e7c, 0x2396060,
         0x2396208, 0x2396364, 0x2393c6c, 0x2393dd8, 0x2393ef0, 0x2395a10, 0x2395948, 0x29d6ef8,
         0x1e1dd84, 0x1e1e07c, 0x1e1e558, 0x1e1e668, 0x238dd60, 0x29e27cc, 0x1f94570, 0x28b52b4,
         0x2396580, 0x20001000, 0x25d4ccc, 0x25d4edc, 0x25d4bc4, 0x25d4dd4, 0x25d499c,
         0x2f824bc, 0x2f82340, 0x25e514c)
for address in set(hooks) | set(reset.addresses): u.hook_add(UC_HOOK_CODE, hook, begin=address, end=address)
methods = [0x239649c, 0x2396a88, 0x2395c14, 0x2395f04, 0x239773c, 0x2395bf8, 0x2395e34, 0x2393a9c, 0x239766c, 0x2395804]
def native(case):
    global v, trace, reads, ports, error, rows
    v = case; trace = []; reads = ports = 0; error = ''
    reset.prepare()
    rows = {key: v[15 + i] for i, key in enumerate(keys) if v[15 + i] != -(1 << 63)}
    for ptr in (owner, life, user, basic, second, inventory, daily, weekly, tracker, delegate): u.mem_write(ptr, bytes(0x100))
    for offset, value in ((0x10, v[6]), (0x11, v[7]), (0x12, v[9]), (0x14, v[8])): put(owner + offset, value, 1)
    put(owner + 0x58, v[4]); put(owner + 0x60, v[5]); put(owner + 0x68, v[33], 4)
    put(owner + 0x48, 0 if v[22] & 1 else life); put(life + 0x10, 1800000)
    put(owner + 0x28, delegate if v[31] else 0); put(delegate + 0x18, 0x20001000); put(delegate + 0x40, owner)
    put(user_static, 0 if v[22] & 16 else user); put(user + 0x18, 0 if v[22] & 32 else basic); put(user + 0x20, 0 if v[22] & 64 else inventory)
    put(inventory + 0xd8, reset.new_bits([True] * 192))
    put(basic + 0x28, v[36]); put(basic + 0x30, v[37]); put(second + 0x28, v[36] + 23); put(second + 0x30, v[37] + 29)
    put(daily + 0x18, v[26], 1); put(weekly + 0x48, 0 if v[22] & 8 else tracker); put(tracker + 0x10, v[27], 1)
    put(pub, v[34], 1); put(pub + 1, v[35], 1); put(app_static, v[14], 4)
    put(full_key, full_key); put(unlimited_key, unlimited_key)
    for i in range(8): sx(i, 0)
    sx(0, owner); sx(1, v[1]); u.reg_write(r.UC_ARM64_REG_SP, 0x103ff000); u.reg_write(r.UC_ARM64_REG_LR, 0x20000000)
    try: u.emu_start(methods[v[0]], 0x20000000, count=20000)
    except Exception as failure: raise AssertionError((v, hex(u.reg_read(r.UC_ARM64_REG_PC)), trace)) from failure
    assert error or u.reg_read(r.UC_ARM64_REG_PC) == 0x20000000, (v, hex(u.reg_read(r.UC_ARM64_REG_PC)), trace)
    result = signed(x(0)) if not error and v[0] in (0, 1, 7, 8) else 0
    state = [result, get(owner + 0x10, 1), get(owner + 0x11, 1), get(owner + 0x12, 1), get(owner + 0x58, signed=True),
        get(owner + 0x60, signed=True), get(owner + 0x68, 4, True), get(basic + 0x28, signed=True), get(basic + 0x30, signed=True),
        get(second + 0x28, signed=True), get(second + 0x30, signed=True), get(pub, 1), get(pub + 1, 1)]
    return ','.join(map(str, state)) + ',' + ','.join(str(rows[key]) if key in rows else '-' for key in keys) + ',' + error + '#' + ';'.join(trace)
def bits(value): return int.from_bytes(struct.pack('<f', value), 'little')
base = [0, 1700000000000, 1700000000000, 0, 1700000000000, 0, 0, 0, 0, 0, 3, 5, 123000, 600000, bits(1800.9),
        2, 1700000000000, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 2, 1, 0, 1700003600000, 0]
cases = []
for op in range(len(methods)):
    for index, values in ((4, [base[2] + d for d in (-1, 0, 59999, 60000, 60001)]),
        (5, [-60001, -60000, -59999, 0, 59999, 60000, 60001]), (6, [0, 1]), (7, [0, 1]),
        (14, [bits(n) for n in (-1, 0, .99, 1, 1799.9, 1800, float('nan'), float('inf'), -float('inf'), 1e20)]),
        (22, [1, 2, 4, 8, 16, 32, 64]), (23, list(range(1, 20)))):
        for value in values:
            c = base.copy(); c[0] = op; c[index] = value; cases.append(c)
    for change in range(1, 9):
        for at in (1, 2, 3):
            c = base.copy(); c[0] = op; c[24:26] = [at, change]; c[5] = 70000; cases.append(c)
rng = random.Random(0x2393a9c)
limits = [-(1 << 63), (1 << 63) - 1, -(1 << 31), (1 << 31) - 1, -60000, -1, 0, 1, 60000, 1700000000000]
for _ in range(2000):
    c = base.copy(); c[0] = rng.randrange(len(methods)); c[1:6] = [rng.choice(limits) for _ in range(5)]
    c[6:10] = [rng.randrange(2) for _ in range(4)]; c[10] = rng.choice((-1, 0, 3, 5, 8)); c[11] = rng.choice((5, 8))
    c[12:14] = [rng.choice(limits) for _ in range(2)]; c[14] = rng.getrandbits(32); c[15:22] = [rng.choice(limits) for _ in range(7)]
    c[22] = rng.choice((0, 0, 0, 1, 2, 4, 8, 16, 32, 64)); c[23] = rng.choice((0, 0, 0, rng.randrange(1, 20)))
    c[24:26] = [rng.randrange(4), rng.randrange(9)]; c[26:32] = [rng.randrange(2) for _ in range(6)]
    c[32] = rng.randrange(9); c[33] = rng.choice((0, 1, 7, (1 << 31) - 1)); c[34:36] = [rng.randrange(2) for _ in range(2)]
    cases.append(c)
expected = [native(c) for c in cases]
mono = Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
sources = ['SourceCatalog/LifeClockHarness.cs'] + ['Assets/Foundation/Runtime/' + name + '.cs' for name in (
    'OriginalLifeClock', 'OriginalLifeHelper', 'OriginalLifeData', 'OriginalInventoryTimeReset', 'OriginalBasicLevelProgress', 'OriginalUserInventoryBalances', 'OriginalLocalAccountLoad', 'OriginalKeyValueStorage')]
with tempfile.TemporaryDirectory() as directory:
    exe = Path(directory) / 'Clock.exe'
    args = [str(mono.parent / 'NetCoreRuntime/dotnet.exe'), str(mono.parent / 'DotNetSdkRoslyn/csc.dll'), '-nologo', '-nostdlib+',
        *['-r:' + str(mono / 'lib/mono/4.5' / n) for n in ('mscorlib.dll', 'System.dll', 'System.Core.dll')], '-out:' + str(exe), *[str(PROJECT / p) for p in sources]]
    result = subprocess.run(args, capture_output=True, text=True, encoding='utf-8'); assert result.returncode == 0, result.stdout + result.stderr
    result = subprocess.run([str(mono / 'bin/mono.exe'), str(exe)], input='\n'.join(','.join(map(str, c)) for c in cases) + '\n', capture_output=True, text=True, encoding='utf-8')
    assert result.returncode == 0, result.stdout + result.stderr
    actual = result.stdout.lstrip('\ufeff').splitlines(); assert len(actual) == len(expected)
    for i, (a, b) in enumerate(zip(actual, expected)): assert a == b, (i, cases[i], a, b)
report = {'passed': True, 'cases': len(cases), 'scope': __doc__.strip(), 'original_rvas': [hex(n) for n in methods] + ['0x28e7184', '0x28e78d0', '0x28ea568', '0x2d7b5e8'],
    'files': [{'path': p, 'sha256': hashlib.sha256((PROJECT / p).read_bytes()).hexdigest()} for p in sources + ['SourceCatalog/verify_life_clock.py', 'SourceCatalog/native_inventory_time_reset.py']]}
(PROJECT / 'life_clock_native_verification.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
print(json.dumps(report, indent=2))
