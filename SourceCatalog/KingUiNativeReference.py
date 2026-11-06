from pathlib import Path
import json
import random
import struct
import subprocess
import sys

ROOT = Path(sys.argv[2]).resolve() if len(sys.argv) > 2 else Path(__file__).resolve().parents[1]
WORK = Path(sys.argv[1]).resolve()
WORK.mkdir(parents=True, exist_ok=True)
sys.path.insert(0, str(ROOT.parent / 'tools/python'))
from elftools.elf.elffile import ELFFile
from elftools.elf.relocation import RelocationSection
from unicorn import Uc, UC_ARCH_ARM64, UC_MODE_ARM, UC_HOOK_CODE
import unicorn.arm64_const as r

u = Uc(UC_ARCH_ARM64, UC_MODE_ARM)
u.mem_map(0, 0x6000000)
u.mem_map(0x10000000, 0x400000)
u.mem_map(0x20000000, 0x1000)
with (ROOT.parent.parent / 'RoyalMatch_20260929/apk_unpacked/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so').open('rb') as stream:
    elf = ELFFile(stream)
    for segment in elf.iter_segments():
        if segment['p_type'] == 'PT_LOAD': u.mem_write(segment['p_vaddr'], segment.data())
    for section in elf.iter_sections():
        if isinstance(section, RelocationSection):
            for item in section.iter_relocations():
                if item['r_info_type'] == 1027:
                    u.mem_write(item['r_offset'], struct.pack('<Q', item['r_addend']))

def put(p, value, size=8): u.mem_write(p, int(value).to_bytes(size, 'little', signed=value < 0))
def get(p, size=8, signed=False): return int.from_bytes(u.mem_read(p, size), 'little', signed=signed)
def x(i): return u.reg_read(getattr(r, 'UC_ARM64_REG_X' + str(i)))
def ret(value=0):
    u.reg_write(r.UC_ARM64_REG_X0, value)
    u.reg_write(r.UC_ARM64_REG_PC, u.reg_read(r.UC_ARM64_REG_LR))
def call(address, *args):
    for i, value in enumerate(args): u.reg_write(getattr(r, 'UC_ARM64_REG_X' + str(i)), value & 0xffffffffffffffff)
    u.reg_write(r.UC_ARM64_REG_SP, 0x103ff000)
    u.reg_write(r.UC_ARM64_REG_LR, 0x20000000)
    u.emu_start(address, 0x20000000, count=25000)
    assert u.reg_read(r.UC_ARM64_REG_PC) == 0x20000000
    return x(0)

king, skeleton, track, connects, manager = [0x10000000 + i * 0x1000 for i in range(5)]
for offset, value in ((0x18, skeleton), (0x20, connects), (0x40, 88), (0x48, 77)): put(king + offset, value)
put(skeleton + 0xd0, track)
put(connects + 0x18, 2, 4)
put(connects + 0x20, 31)
put(connects + 0x28, 32)
put(king + 0x98, 0x10008000) # Non-active SuperDuke owner, read before the legacy bonus exit.
metadata = 0x10100000
allocation = 0x10200000
improved = True
waits = []
idle_starts = []
track_calls = []
wait_seconds = []
function_calls = {}

def hook(uc, address, size, unused):
    global metadata, allocation
    a, b = x(0), x(1)
    if address in (0x18c96b8, 0x18c9f68, 0x18ca830, 0x18ca2b0):
        function_calls[address] = function_calls.get(address, 0) + 1
    if address == 0xffd960:
        put(a, metadata)
        put(metadata + 0xe0, 1, 4)
        metadata += 0x100
        ret()
    elif address in (0xffd90c, 0xffda64, 0x231007c): ret()
    elif address in (0xffda88, 0xffda90): raise RuntimeError(('native null/bounds', hex(address)))
    elif address == 0xffda78:
        pointer = allocation
        allocation += 0x100
        ret(pointer)
    elif address == 0x23e743c: ret(int(improved))
    elif address == 0x23e648c: ret(manager)
    elif address == 0x18c8f50:
        idle_starts.append(b & 1)
        put(king + 0x8c, 0, 1) # Supplied synchronous IdleCycle first resume.
        ret()
    elif address == 0x2059f7c: ret(a)
    elif address in (0x2b79988, 0x2b79cf8):
        track_calls.append((address, x(2), x(3)))
        ret(0x10006000)
    elif address == 0x1e2aaac:
        waits.append(a)
        ret(0x10007000)
    elif address == 0x3e49528:
        seconds = struct.unpack('<f', struct.pack('<I', u.reg_read(r.UC_ARM64_REG_S0)))[0]
        put(a + 0x10, u.reg_read(r.UC_ARM64_REG_S0), 4)
        wait_seconds.append(seconds)
        ret()

u.hook_add(UC_HOOK_CODE, hook)

def snapshot():
    return [int(improved), get(king + 0xb1, 1), get(king + 0x88, 4, True),
            get(king + 0x8c, 1), get(manager + 0x8c, 4), len(waits),
            len(idle_starts), idle_starts[-1] if idle_starts else -1]

commands, expected = [], []
rng = random.Random(11934)
for profile in range(12):
    improved = profile % 3 != 2
    initial = 27 if improved else 20
    put(king + 0xb0, int(not improved), 1)
    put(king + 0xb1, 1, 1)
    put(king + 0x88, initial if improved else 0, 4)
    put(king + 0x8c, 0, 1)
    put(manager + 0x8c, 0, 4)
    waits.clear(); idle_starts.clear(); track_calls.clear()
    cases = [('R', int(improved), initial), ('H',), ('H',), ('M', initial, 0),
             ('M', 0, 1), ('M', initial, 4), ('M', initial, 1), ('H',), ('Q', 0), ('H',),
             ('D',), ('B', int(improved), initial)]
    for _ in range(140):
        kind = rng.choice(['H', 'H', 'M', 'M', 'M', 'Q', 'S', 'B', 'D'])
        if kind == 'M': cases.append((kind, rng.choice([-2, -1, 0, 1, 2, 3, 10, 20, 27, 30]), rng.choice([0, 1, 2, 3, 4, 5, 9])))
        elif kind == 'B': cases.append((kind, int(rng.choice([True, False])), rng.choice([0, 3, 20, 27, 30])))
        elif kind == 'Q': cases.append((kind, rng.randrange(10)))
        else: cases.append((kind,))
    for command in cases:
        kind = command[0]
        before = len(waits)
        if kind == 'M': call(0x18c96b8, king, command[1], command[2], 0)
        elif kind == 'H':
            track_before = len(track_calls)
            call(0x18c9f68, king, 0)
            assert len(track_calls) - track_before in (0, 3)
        elif kind == 'Q' and command[1] < len(waits): call(0x18ca830, waits[command[1]], 0)
        elif kind == 'S': put(king + 0x8c, 1, 1)
        elif kind == 'D': call(0x18ca2b0, king, 0)
        elif kind == 'B':
            waits.clear()
            improved = bool(command[1])
            put(king + 0xb0, int(not improved), 1)
            call(0x18c96b8, king, command[2], 1, 0)
        for pointer in waits[before:]:
            assert call(0x18ca830, pointer, 0) == 1
        commands.append(' '.join(map(str, command)))
        expected.append(','.join(map(str, snapshot())))

assert all(seconds == 10.0 for seconds in wait_seconds)
(WORK / 'commands.txt').write_text('\n'.join(commands) + '\n')
(WORK / 'native_expected.txt').write_text('\n'.join(expected) + '\n')
(WORK / 'native_reference.json').write_text(json.dumps({
    'commands': len(commands), 'native_function_calls': {hex(a): n for a, n in function_calls.items()},
    'native_wait_durations': sorted(set(wait_seconds)), 'native_wait_constructions': len(wait_seconds),
    'ports': 'Original MoveChanged, BeHappy, BeSad and EnableHappyAnimDelayed ARM64 bodies execute. Metadata/allocation, animation state and coroutine dispatch are supplied. Idle-start first-resume effects are supplied, not a native Unity scheduler.'
}, indent=2) + '\n')
print(json.dumps({'commands': len(commands), 'waits': len(wait_seconds), 'native_calls': sum(function_calls.values())}))
