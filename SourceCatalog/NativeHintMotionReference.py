"""Execute the original three hint animation state machines and interpolation helpers.

Unity roots, highlight interfaces, clock, restore and particle transport are ports.
No search, actual scene rendering, audio or full HintManager dispatch is emulated.
"""
from hashlib import sha256
from pathlib import Path
import json
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
import unicorn.arm64_const as reg

LIB = ROOT.parent.parent / 'RoyalMatch_20260929/apk_unpacked/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so'
u = Uc(UC_ARCH_ARM64, UC_MODE_ARM)
u.mem_map(0, 0x6000000)
u.mem_map(0x10000000, 0x400000)
u.mem_map(0x20000000, 0x1000)
with LIB.open('rb') as stream:
    elf = ELFFile(stream)
    for segment in elf.iter_segments():
        if segment['p_type'] == 'PT_LOAD':
            u.mem_write(segment['p_vaddr'], segment.data())
    for section in elf.iter_sections():
        if isinstance(section, RelocationSection):
            for item in section.iter_relocations():
                if item['r_info_type'] == 1027:
                    u.mem_write(item['r_offset'], struct.pack('<Q', item['r_addend']))

def put(p, value, n=8): u.mem_write(p, int(value).to_bytes(n, 'little', signed=value < 0))
def get(p, n=8): return int.from_bytes(u.mem_read(p, n), 'little')
def bits(v): return struct.unpack('<I', struct.pack('<f', v))[0]
def val(v): return struct.unpack('<f', struct.pack('<I', v))[0]
def f32(v): return val(bits(v))
def readf(p): return val(get(p, 4))
def writef(p, v): put(p, bits(v), 4)
def x(i): return u.reg_read(getattr(reg, 'UC_ARM64_REG_X' + str(i)))
def floats(): return [val(u.reg_read(getattr(reg, 'UC_ARM64_REG_S' + str(i)))) for i in range(3)]
def vector(values):
    for i, value in enumerate(values): u.reg_write(getattr(reg, 'UC_ARM64_REG_S' + str(i)), bits(value))
def returned(value=0):
    u.reg_write(reg.UC_ARM64_REG_X0, value)
    u.reg_write(reg.UC_ARM64_REG_PC, u.reg_read(reg.UC_ARM64_REG_LR))
def call(address, *args):
    for i, value in enumerate(args): u.reg_write(getattr(reg, 'UC_ARM64_REG_X' + str(i)), value)
    u.reg_write(reg.UC_ARM64_REG_SP, 0x103ff000)
    u.reg_write(reg.UC_ARM64_REG_LR, 0x20000000)
    u.emu_start(address, 0x20000000, count=100000)
    assert u.reg_read(reg.UC_ARM64_REG_PC) == 0x20000000

manager = 0x10000000
roots = [0x10001000, 0x10002000]
hintables = [0x10010000 + i * 0x100 for i in range(14)]
klass, link, statics = 0x10020000, 0x10021000, 0x10022000
put(0x53df178, link); put(link, klass); put(klass + 0xb8, statics)
u.mem_write(statics, struct.pack('<6f', 0, 0, 0, 1, 1, 1))
put(0x53b0720, 0x10023000); put(0x10023000, 0x10024000)
for flag in (0x5739572, 0x5739573, 0x573ff6c, 0x573ff6d): put(flag, 1, 1)
for i, root in enumerate(roots): put(root + 0x18, root + 0x80)
for h in hintables:
    put(h, 0x10025000)
put(0x10025000 + 0x12a, 0, 2)
interface_funcs = [0x10026000 + i * 0x10 for i in range(7)]
for i, fn in enumerate(interface_funcs):
    put(0x10027000 + i * 0x10, fn)
positions, scales, brightness, maxima = [], [], [], []
events = []
delta, clock, kind, count = 0., 0., '', 0
native_calls = {'manual': 0, 'interpolation_helper': 0, 'gentle_stop': 0}
executed_entries = {hex(p): 0 for p in (0x2186574, 0x2188134, 0x2187db8,
    0x2186890, 0x218b508, 0x218b9e8, 0x2186450, 0x2187a00, 0x21879ec)}

def restore():
    global positions, scales, brightness
    positions = [[0., 0., 0.], [0., 0., 0.]]
    scales = [[1., 1., 1.], [1., 1., 1.]]
    brightness = [0.] * count
    events.append('restore')

def hook(uc, address, size, unused):
    a = x(0)
    if hex(address) in executed_entries: executed_entries[hex(address)] += 1
    if address in (0xffd960, 0xffd90c, 0xffda64): returned()
    elif address == 0x32037e4: vector([delta]); returned()
    elif address == 0x3e4249c: vector([clock]); returned()
    elif address == 0x2b51e4c: vector(positions[roots.index(a)]); returned()
    elif address == 0x2b51e68: positions[roots.index(a)] = floats(); returned()
    elif address == 0x2b51ea0: scales[roots.index(a)] = floats(); returned()
    elif address == 0x3e4e4a0: positions[[root + 0x80 for root in roots].index(a)] = floats(); returned()
    elif address == 0x3e4e9f8: scales[[root + 0x80 for root in roots].index(a)] = floats(); returned()
    elif address == 0xfcd934:
        slot = x(2)
        assert 0 <= slot < 7
        returned(0x10027000 + slot * 0x10)
    elif address in interface_funcs:
        i, slot = hintables.index(a), interface_funcs.index(address)
        if slot == 4: brightness[i] = floats()[0]; returned()
        elif slot == 5: vector([brightness[i]]); returned()
        elif slot == 6: vector([maxima[i]]); returned()
        else: raise AssertionError(('unexpected interface slot', slot))
    elif address == 0x21869a0: events.append('hide'); returned()
    elif address == 0x2186a08: events.append('recycle'); returned()
    elif address in (0x2186478, 0x218b404, 0x218b8f0): restore(); returned()
    elif address in (0x2186574, 0x2188134, 0x2187db8): native_calls['manual'] += 1
    elif address in (0x2186890, 0x218b508, 0x218b9e8): native_calls['interpolation_helper'] += 1
    elif address in (0x2186450, 0x2187a00, 0x21879ec): native_calls['gentle_stop'] += 1
    else: raise AssertionError(('unexpected native boundary', hex(address), hex(a), hex(x(1))))

boundaries = [0xffd960, 0xffd90c, 0xffda64, 0x32037e4, 0x3e4249c, 0x2b51e4c,
    0x2b51e68, 0x2b51ea0, 0x3e4e4a0, 0x3e4e9f8, 0xfcd934, *interface_funcs,
    0x21869a0, 0x2186a08, 0x2186478, 0x218b404, 0x218b8f0,
    0x2186574, 0x2188134, 0x2187db8, 0x2186890, 0x218b508, 0x218b9e8,
    0x2186450, 0x2187a00, 0x21879ec, 0xffda88, 0xffda90, 0xffde20]
for address in boundaries: u.hook_add(UC_HOOK_CODE, hook, begin=address, end=address)

def array(p, length, values, fmt):
    put(p + 0x18, length, 4)
    u.mem_write(p + 0x20, struct.pack('<' + fmt * len(values), *values))
    return p

def run(definition):
    global positions, scales, brightness, maxima, events, delta, clock, kind, count
    kind, count = definition['kind'], definition['count']
    positions = [[0., 0., 0.], [0., 0., 0.]]
    scales = [[1., 1., 1.], [1., 1., 1.]]
    maxima = definition.get('maxima', [f32(1.2 + .1 * i) for i in range(count)])
    brightness = definition.get('initial_brightness', [f32(maxima[i] * (.15 if definition['nonzero_highlight'] else 0)) for i in range(count)])
    u.mem_write(manager, bytes(0x2000))
    for i, root in enumerate(roots): put(root + 0x18, root + 0x80)
    put(manager + 0x10, 1, 4)
    root_array = array(0x10030000, 2, roots, 'Q')
    hint_array = array(0x10031000, count, hintables[:count], 'Q')
    start_array = array(0x10032000, max(2, count), [0.] * max(6, count), 'f')
    end_array = array(0x10033000, max(2, count), [0.] * max(6, count), 'f')
    direction = definition['direction']
    if kind == 'match':
        put(manager + 0x40, roots[0]); u.mem_write(manager + 0x48, struct.pack('<3f', *direction))
        writef(manager + 0x54, 3.)
        put(manager + 0x108, count, 4); put(manager + 0x110, hint_array)
        put(manager + 0x118, start_array); put(manager + 0x120, end_array)
        put(manager + 0x128, 0x10034000)
        method, stop, completion = 0x2188134, 0x2187a00, 0x130
    elif kind == 'combo':
        writef(manager + 0x28, 3.)
        put(manager + 0xe0, 2, 4); u.mem_write(manager + 0xe4, struct.pack('<3f', *direction))
        put(manager + 0xf0, hint_array); put(manager + 0x100, start_array); put(manager + 0x108, end_array)
        put(manager + 0x138, root_array); put(manager + 0x1b8, 0x10034000)
        method, stop, completion = 0x2186574, 0x2186450, 0x1c0
    else:
        writef(manager + 0x28, 3.)
        put(manager + 0xe0, hint_array); put(manager + 0x110, root_array)
        put(manager + 0x190, 0x10034000)
        method, stop, completion = 0x2187db8, 0x21879ec, 0x198
    states = []
    raw_time = f32(100.)
    inputs = definition.get('inputs')
    for step in range(len(inputs) if inputs is not None else 2500):
        delta = f32(inputs[step]['delta'] if inputs is not None else definition['deltas'][step % len(definition['deltas'])])
        raw_delta = 0 if inputs is not None else f32(definition['raw_deltas'][step % len(definition['raw_deltas'])])
        raw_time = f32(inputs[step]['time']) if inputs is not None else f32(raw_time + raw_delta)
        clock = raw_time
        events = []
        if step == definition['stop_at']: call(stop, manager)
        call(method, manager)
        state = get(manager + 0x10, 4)
        semantic = ({2: 3, 3: 2}.get(state, state) if kind != 'match' else state)
        states.append({'step': step, 'delta': delta, 'time': clock,
            'stop': step == definition['stop_at'], 'phase': semantic,
            'pulses': get(manager + 0x18, 4), 'bursts': get(manager + 0x1c, 4),
            'elapsed': readf(manager + 0x20), 'duration': readf(manager + 0x24),
            'next_at': readf(manager + completion), 'positions': [p[:] for p in positions],
            'scales': [s[:] for s in scales], 'brightness': brightness[:] if kind == 'match' else [],
            'events': events[:]})
        if state == 0: break
    else:
        assert inputs is not None, ('did not finish', definition['name'])
    return {**definition, 'states': states}

profiles = {
    '60fps': ([.0166], [1 / 60]), '30fps': ([.0333], [1 / 30]),
    '120fps': ([.0083], [1 / 120]),
    'alternate': ([.0083, .0373, .0111, .0247], [.008333333, .037333333, .011111111, .024777777]),
    'zero_delta': ([.0166, 0., 0., .0499], [1 / 60, 0., 0., .05]),
    'hitch': ([.0166] * 9 + [.1733, .0917], [1 / 60] * 9 + [.173333333, .091777777]),
    'large_delta': ([.3333], [1 / 3])
}
cases = []
definitions = json.loads(Path(sys.argv[3]).read_text(encoding='utf-8')) if len(sys.argv) > 3 else None
for kind in (() if definitions is not None else ('match', 'combo', 'tap')):
    for profile, (deltas, raw_deltas) in profiles.items():
        base = {'name': kind + '_' + profile, 'kind': kind,
            'count': 3 if kind == 'match' else (2 if kind == 'combo' else 1),
            'direction': [0., -1., 0.] if profile in ('alternate', 'large_delta') else [1., 0., 0.],
            'nonzero_highlight': False, 'deltas': deltas, 'raw_deltas': raw_deltas, 'stop_at': -1}
        natural = run(base)
        cases.append(natural)
        stops = {0, 1, 2, 7, max(0, len(natural['states']) - 3)}
        for wanted in (3, 4, 5, 6):
            found = next((s['step'] + 1 for s in natural['states'] if s['phase'] == wanted), None)
            if found is not None: stops.add(found)
        for stop_at in sorted(stops):
            cases.append(run({**base, 'name': base['name'] + '_stop' + str(stop_at), 'stop_at': stop_at}))
        if kind == 'match':
            cases.append(run({**base, 'name': base['name'] + '_existing_highlight', 'nonzero_highlight': True}))
for count in (() if definitions is not None else (6, 14)):
    cases.append(run({'name': 'match_count' + str(count), 'kind': 'match', 'count': count,
        'direction': [-1., 0., 0.], 'nonzero_highlight': True,
        'deltas': [.0166], 'raw_deltas': [1 / 60], 'stop_at': -1}))
if definitions is not None:
    cases = [run(definition) for definition in definitions]
report = {'passed': True, 'native_methods': {'match': '0x2188134', 'combo': '0x2186574', 'tap': '0x2187db8'},
    'native_helper_methods': ['0x218b508', '0x2186890', '0x218b9e8'],
    'native_library_sha256': sha256(LIB.read_bytes()).hexdigest(),
    'native_calls': native_calls, 'executed_entries': executed_entries, 'cases': cases,
    'ports': 'Explicit current-level delta and Unity Time.time; neutral item-root storage; IHintable get/set/max highlight dispatch; metadata; restoration and particle Hide/Recycle transport. Actual ManualUpdate and interpolation helpers execute. Fixtures enter prepared state1; search, native Start/PrepareHintables, rendered scene, Unity scheduler/audio and external root handoff are outside this oracle.'}
(WORK / 'native_hint_motion.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
print(json.dumps({'cases': len(cases), 'states': sum(len(c['states']) for c in cases),
                  'native_calls': native_calls, 'sha256': report['native_library_sha256']}), flush=True)
