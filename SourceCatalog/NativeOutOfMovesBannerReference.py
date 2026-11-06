"""Execute original banner sequence construction with explicit engine ports."""
from hashlib import sha256
import json
import math
from pathlib import Path
import sqlite3
import struct
import sys

ROOT = Path(sys.argv[1]).resolve()
WORK = Path(sys.argv[2]).resolve()
WORK.mkdir(parents=True, exist_ok=True)
sys.dont_write_bytecode = True
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


def put(p, value, n=8): u.mem_write(p, int(value).to_bytes(n, 'little'))
def get(p, n=8): return int.from_bytes(u.mem_read(p, n), 'little')
def bits(v): return struct.unpack('<I', struct.pack('<f', v))[0]
def val(v): return struct.unpack('<f', struct.pack('<I', v))[0]
def f32(v): return val(bits(v))
def x(i): return u.reg_read(getattr(r, 'UC_ARM64_REG_X' + str(i)))
def floats(n): return [val(u.reg_read(getattr(r, 'UC_ARM64_REG_S' + str(i)))) for i in range(n)]
def return_(value=0):
    u.reg_write(r.UC_ARM64_REG_X0, value)
    u.reg_write(r.UC_ARM64_REG_PC, u.reg_read(r.UC_ARM64_REG_LR))
def vector(values):
    for i, value in enumerate(values): u.reg_write(getattr(r, 'UC_ARM64_REG_S' + str(i)), bits(value))
def call(address, *args):
    for i, value in enumerate(args): u.reg_write(getattr(r, 'UC_ARM64_REG_X' + str(i)), value)
    u.reg_write(r.UC_ARM64_REG_SP, 0x103ff000)
    u.reg_write(r.UC_ARM64_REG_LR, 0x20000000)
    u.emu_start(address, 0x20000000, count=50000)
    assert u.reg_read(r.UC_ARM64_REG_PC) == 0x20000000
    return x(0)


banner, camera, transform, sprite, game_object, coroutine, audio, banner_class, close_info = [0x10000000 + i * 0x1000 for i in range(9)]
v2class, v3class, v2static, v3static = [0x10010000 + i * 0x1000 for i in range(4)]
put(v2class + 0xb8, v2static)
put(v3class + 0xb8, v3static)
put(v2class + 0xe0, 1, 4)
put(v3class + 0xe0, 1, 4)
v2vectors = [(0, 0), (1, 1), (0, 1), (0, -1), (-1, 0), (1, 0)]
v3vectors = [(0, 0, 0), (1, 1, 1), (0, 1, 0), (0, -1, 0)]
for i, value in enumerate(v2vectors): u.mem_write(v2static + i * 8, struct.pack('<2f', *value))
for i, value in enumerate(v3vectors): u.mem_write(v3static + i * 12, struct.pack('<3f', *value))
known_metadata = {get(0x53dca28): v2class, get(0x53df178): v3class}
metadata, allocation = 0x10100000, 0x10200000
safe, top, bottom = (1, -.4, 0), (1, 8, -10), (1, -8, -10)
sprite_size = (10, 1.5)
seqs, tweens, callbacks, positions, trace = {}, {}, {}, [], []
audio_calls = []
raw_delta_bits = 0


def hook(uc, address, size, unused):
    global metadata, allocation
    a, b = x(0), x(1)
    if address == 0xffd960:
        if a in known_metadata: put(a, known_metadata[a])
        else:
            put(a, metadata); put(metadata + 0xe0, 1, 4)
            put(metadata + 0xb8, metadata + 0x200); metadata += 0x400
        return_()
    elif address in (0xffd90c, 0xffda64): return_()
    elif address in (0xffda88, 0xffda90): raise AssertionError(hex(address))
    elif address == 0xffda78:
        value = allocation; allocation += 0x200
        return_(value)
    elif address == 0x158a3b4: trace.append('portrait-preparation-port'); return_()
    elif address == 0x2005734: vector(safe); return_()
    elif address == 0x2004f98: vector(top); return_()
    elif address == 0x2005000: vector(bottom); return_()
    elif address == 0x3e4737c: return_(transform)
    elif address == 0x3e473b8: return_(game_object)
    elif address == 0x3e50d30: vector(sprite_size); return_()
    elif address == 0x3e4e368: positions.append(floats(3)); return_()
    elif address == 0x1d7ad40:
        value = allocation; allocation += 0x200
        seqs[value] = []; put(value + 0xe0, 1, 1)
        return_(value)
    elif address == 0x1d788fc:
        callbacks[a] = {'target': b, 'method_info_port': x(2)}
        return_()
    elif address == 0x1d83a58:
        seqs[a].append({'callback': callbacks[b]}); return_(a)
    elif address == 0x1d7f6bc:
        assert a == transform
        values = floats(4)
        value = allocation; allocation += 0x200
        tweens[value] = {'target': values[:3], 'duration': values[3]}
        return_(value)
    elif address in (0x2922414, 0x2922480):
        tweens[a]['ease'] = b
        if address == 0x2922480: tweens[a]['overshoot'] = floats(1)[0]
        return_(a)
    elif address == 0x1d81ee8: seqs[a].append(tweens[b]); return_(a)
    elif address == 0x1d7bc54: seqs[a].append({'interval': floats(1)[0]}); return_(a)
    elif address == 0x2922140:
        seqs[a].append({'on_complete': callbacks[b]}); return_(a)
    elif address == 0x1d7c000: trace.append('kill-original-sequence'); return_()
    elif address == 0x29225c4: assert b == game_object; trace.append('link-close-sequence-to-object'); return_(a)
    elif address == 0x1d7ca30: trace.append('wait-for-close-sequence-kill'); return_(0x10060000)
    elif address == 0x3e17838:
        assert a == audio
        audio_calls.append({'clip': b, 'cooldown': floats(1)[0]})
        return_()
    elif address == 0x3e424ec:
        u.reg_write(r.UC_ARM64_REG_S0, raw_delta_bits)
        return_()
    elif address in (0xd009c0, 0xd00ce0):
        arg = struct.unpack('<d', struct.pack('<Q', u.reg_read(r.UC_ARM64_REG_D0)))[0]
        value = math.sin(arg) if address == 0xd009c0 else math.cos(arg)
        u.reg_write(r.UC_ARM64_REG_D0, struct.unpack('<Q', struct.pack('<d', value))[0])
        return_()


u.hook_add(UC_HOOK_CODE, hook)
put(banner + 0x40, sprite)
put(banner + 0x60, camera)
put(banner + 0x68, audio)
put(banner, banner_class)
put(banner_class + 0x2c0, close_info)
call(0x1589e80, banner)
assert len(seqs) == 1
automatic = list(seqs.values())[0]
automatic_position = positions[0]
put(coroutine + 0x20, banner)
assert call(0x158a9a4, coroutine) == 1
assert get(coroutine + 0x10, 4) == 1
assert get(coroutine + 0x18) == 0x10060000
assert call(0x158a9a4, coroutine) == 0
assert len(seqs) == 2
skipped = list(seqs.values())[1]
moves = [step for step in automatic if 'duration' in step]
intervals = [step['interval'] for step in automatic if 'interval' in step]
assert [step['ease'] for step in moves] == [3, 3, 3, 26]
expected_durations = [.25, 1 / 6, .1, 1 / 3]
assert all(abs(step['duration'] - expected) < 1e-7 for step, expected in zip(moves, expected_durations))
assert abs(moves[0]['target'][1] - (safe[1] - .6)) < 1e-6
assert abs(moves[1]['target'][1] - (safe[1] + .1)) < 1e-6
assert abs(moves[2]['target'][1] - safe[1]) < 1e-6
assert intervals == [1, f32(.1)]
assert skipped[0]['ease'] == 8 and abs(skipped[0]['duration'] - .3) < 1e-7
assert skipped[0]['target'] == [bottom[0], bottom[1] - 1, bottom[2]]
call(0x158a944, banner)
call(0x158a96c, banner)
assert get(banner + 0x80, 1) == 1
call(0x158a978, banner)
assert [entry['clip'] for entry in audio_calls] == [258, 259]

with sqlite3.connect(ROOT.parent / 'code/native/functions.sqlite3') as db:
    native_methods = {}
    for address in (0x1589e80, 0x158a9a4, 0x158a944, 0x158a96c, 0x158a978, 0x38ffa38, 0x38ffc34, 0x38ffc88):
        name, body = db.execute('select name,body from functions where address=?', (address,)).fetchone()
        native_methods[hex(address)] = {'name': name, 'body_sha256': sha256(body.encode()).hexdigest()}
        (WORK / ('native_' + hex(address)[2:] + '.txt')).write_text(body, encoding='utf-8')
report = {'passed': True, 'automatic': automatic, 'skipped': skipped,
    'initial_position': automatic_position, 'camera_ports': {'safe': safe, 'top': top, 'bottom': bottom, 'sprite_size': sprite_size},
    'nominal_exit_start': sum(step['duration'] for step in moves[:3]) + intervals[0],
    'nominal_automatic_duration': sum(step['duration'] for step in moves) + sum(intervals),
    'trace': trace, 'audio_callbacks': audio_calls, 'native_methods': native_methods,
    'scope': 'Actual ARM64 OutOfMovesBanner.OnShow and CloseAnimation.MoveNext construct the captured target/duration/ease/callback/interval sequence. Unity transform/camera/sprite transport, portrait preparation, metadata/allocation and DOTween sequence objects are supplied ports. Native TweenManager clock selection is audited separately. This does not execute the full native DOTween scheduler or original renderer.'}
(WORK / 'native_banner_construction.json').write_text(json.dumps(report, indent=2) + '\n')
print(json.dumps(report, indent=2))
