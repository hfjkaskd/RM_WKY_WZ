"""Differential verification of original ARM64 camera projection arithmetic.

Execute UpdateSizeParameters from its real prologue through orthographic-size
publication. Unity Camera/Rect and table-kind queries are explicit supplied
ports; width/height/aspect selection, captured/live reads, branch thresholds
and viewport equations execute from ELF. Safe-area correction, square side
blockers/flag cameras, orientation/device lifecycle and logging are outside
this projection test. The build's private GetIsLandscape body also executes.
"""
from pathlib import Path
import hashlib
import itertools
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
u.mem_map(0, 0x6000000); u.mem_map(0x10000000, 0x400000); u.mem_map(0x20000000, 0x3000)


def put(p, v, size=8):
    u.mem_write(p, (int(v) & ((1 << (8 * size)) - 1)).to_bytes(size, 'little'))


def f32(v):
    return struct.unpack('<f', struct.pack('<f', v))[0]


def hx(v):
    return struct.pack('>f', v).hex()


def s(i):
    return struct.unpack('<f', u.reg_read(getattr(r, 'UC_ARM64_REG_S' + str(i))).to_bytes(4, 'little'))[0]


def set_s(i, v):
    u.reg_write(getattr(r, 'UC_ARM64_REG_S' + str(i)), int.from_bytes(struct.pack('<f', v), 'little'))


def returned(value=0):
    u.reg_write(r.UC_ARM64_REG_X0, value)
    u.reg_write(r.UC_ARM64_REG_PC, u.reg_read(r.UC_ARM64_REG_LR))


with (BASE.parent / 'RoyalMatch_20260929/apk_unpacked/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so').open('rb') as stream:
    elf = ELFFile(stream)
    for segment in elf.iter_segments():
        if segment['p_type'] == 'PT_LOAD': u.mem_write(segment['p_vaddr'], segment.data())
    for section in elf.iter_sections():
        if isinstance(section, RelocationSection):
            for entry in section.iter_relocations():
                if entry['r_info_type'] == 1027: put(entry['r_offset'], entry['r_addend'])

owner, camera = 0x10001000, 0x10002000
metadata = 0x10200000
trace = []
head = []
aspects = []
current = 0
reads = 0
cropped = False


def hook(uc, address, size, unused):
    global metadata, current, reads, cropped
    if address == 0xffd960:
        put(u.reg_read(r.UC_ARM64_REG_X0), metadata); put(metadata + 0xe0, 1, 4); metadata += 0x200; returned(); return
    if address in (0xffd90c, 0xffda64): returned(); return
    if address == 0x2003f3c:
        u.reg_write(r.UC_ARM64_REG_PC, 0x20000000); return
    if address == 0x3e42980:
        value = aspects[min(reads, len(aspects) - 1)] if head[4] == 2 else current
        reads += 1; trace.append('a' + hx(value)); set_s(0, value); returned(); return
    if address in (0x3e42f1c, 0x3e42ee0):
        height = address == 0x3e42f1c; value = head[3] if height else head[2]
        trace.append(('h' if height else 'w') + str(value)); returned(value); return
    if address == 0x3630794:
        dest = u.reg_read(r.UC_ARM64_REG_X0)
        for i in range(4): u.mem_write(dest + i * 4, struct.pack('<f', s(i)))
        returned(); return
    if address == 0x3e42d14:
        trace.append('r' + ','.join(hx(s(i)) for i in range(4))); cropped = True
        if head[4] == 1: current = f32(current * s(2))
        returned(); return
    if address == 0x1e18f34: returned(head[1]); return
    if address == 0x3e4282c: trace.append('s' + hx(s(0))); returned(); return
    raise AssertionError(hex(address))


for address in (0xffd960, 0xffd90c, 0xffda64, 0x2003f3c, 0x3e42980, 0x3e42f1c,
                0x3e42ee0, 0x3630794, 0x3e42d14, 0x1e18f34, 0x3e4282c):
    u.hook_add(UC_HOOK_CODE, hook, begin=address, end=address)


def native(case):
    global head, aspects, current, reads, trace, cropped
    head, aspects = case; current = aspects[0]; reads = 0; trace = []; cropped = False
    u.mem_write(owner, bytes(0x100)); put(owner + 0x18, camera)
    for i in range(8): u.reg_write(getattr(r, 'UC_ARM64_REG_X' + str(i)), 0)
    u.reg_write(r.UC_ARM64_REG_X0, owner); u.reg_write(r.UC_ARM64_REG_X1, head[0])
    u.reg_write(r.UC_ARM64_REG_SP, 0x103ff000); u.reg_write(r.UC_ARM64_REG_LR, 0x20000000)
    try: u.emu_start(0x2003ce0, 0x20000000, count=3000)
    except Exception as error: raise AssertionError((case, hex(u.reg_read(r.UC_ARM64_REG_PC)), trace)) from error
    assert u.reg_read(r.UC_ARM64_REG_PC) == 0x20000000
    values = [u.mem_read(owner + offset, 4)[::-1].hex() for offset in (0x24, 0x20, 0x28, 0x5c)]
    u.reg_write(r.UC_ARM64_REG_LR, 0x20000000); u.emu_start(0x20022cc, 0x20000000, count=20)
    assert u.reg_read(r.UC_ARM64_REG_X0) == 0
    return ','.join(values + [str(int(cropped)), '0']) + '#' + ';'.join(trace)


values = [f32(value) for value in (.25, .43, .5, .57, .6, .65, .7, .75, .8, 1, 1.25, 1.333333333, 1.777777778, 2.4)]
for value in (.57, .6, .65, .8):
    bits = int.from_bytes(struct.pack('<f', value), 'little')
    values += [struct.unpack('<f', (bits + step).to_bytes(4, 'little'))[0] for step in (-1, 1)]
cases = [(list(head), [value]) for head in itertools.product((0, 1, 2, 3), (0, 1), (720,), (1280,), (0, 1)) for value in values]
rng = random.Random(0x2003ce0)
for _ in range(500):
    cases.append(([rng.randrange(4), rng.randrange(2), rng.choice((480, 640, 720, 1280, 2048)),
                   rng.choice((480, 720, 1280, 1440, 2048)), rng.randrange(3)],
                  [f32(rng.choice(values)) for _ in range(9)]))
expected = [native(case) for case in cases]
mono = Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
sources = ['SourceCatalog/CameraProjectionHarness.cs', 'Assets/Foundation/Runtime/OriginalCameraProjection.cs']
with tempfile.TemporaryDirectory() as directory:
    exe = Path(directory) / 'Projection.exe'
    args = [str(mono.parent / 'NetCoreRuntime/dotnet.exe'), str(mono.parent / 'DotNetSdkRoslyn/csc.dll'), '-nologo', '-nostdlib+',
            *['-r:' + str(mono / 'lib/mono/4.5' / name) for name in ('mscorlib.dll', 'System.dll', 'System.Core.dll')],
            '-out:' + str(exe), *[str(PROJECT / path) for path in sources]]
    result = subprocess.run(args, capture_output=True, text=True, encoding='utf-8'); assert result.returncode == 0, result.stdout + result.stderr
    lines = [','.join(map(str, head)) + '|' + ','.join(map(str, vals)) for head, vals in cases]
    result = subprocess.run([str(mono / 'bin/mono.exe'), str(exe)], input='\n'.join(lines) + '\n', capture_output=True, text=True, encoding='utf-8')
    assert result.returncode == 0, result.stdout + result.stderr
    actual = result.stdout.lstrip('\ufeff').splitlines(); assert len(actual) == len(expected)
    for i, (a, b) in enumerate(zip(actual, expected)): assert a == b, (i, cases[i], a, b)
report = {'passed': True, 'cases': len(cases), 'scope': __doc__.strip(),
          'original_rvas': ['0x2003ce0', '0x2003628', '0x20022cc'],
          'files': [{'path': path, 'sha256': hashlib.sha256((PROJECT / path).read_bytes()).hexdigest()}
                    for path in sources + ['SourceCatalog/verify_camera_projection.py']]}
(PROJECT / 'camera_projection_native_verification.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
print(json.dumps(report, indent=2))
