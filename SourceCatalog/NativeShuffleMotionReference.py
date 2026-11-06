"""Execute original ShuffleManager.MoveAllItems with explicit scene/runtime ports."""
from hashlib import sha256
import json
from pathlib import Path
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
LIB = ROOT.parent.parent / 'RoyalMatch_20260929/apk_unpacked/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so'
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
def x(i): return u.reg_read(getattr(r, 'UC_ARM64_REG_X' + str(i)))
def floats(n=3): return [val(u.reg_read(getattr(r, 'UC_ARM64_REG_S' + str(i)))) for i in range(n)]
def vector(values):
    for i, value in enumerate(values): u.reg_write(getattr(r, 'UC_ARM64_REG_S' + str(i)), bits(value))
def returned(value=0):
    u.reg_write(r.UC_ARM64_REG_X0, value)
    u.reg_write(r.UC_ARM64_REG_PC, u.reg_read(r.UC_ARM64_REG_LR))
def call(address, *args):
    for i, value in enumerate(args): u.reg_write(getattr(r, 'UC_ARM64_REG_X' + str(i)), value)
    u.reg_write(r.UC_ARM64_REG_SP, 0x103ff000)
    u.reg_write(r.UC_ARM64_REG_LR, 0x20000000)
    u.emu_start(address, 0x20000000, count=1000000)
    assert u.reg_read(r.UC_ARM64_REG_PC) == 0x20000000
    return x(0)

manager, routine, state, grid = 0x10000000, 0x10001000, 0x10002000, 0x10003000
model_class, view_class = 0x10010000, 0x10012000
virtual_can_shuffle, virtual_get_view, virtual_sort = 0x1003c000, 0x1003c010, 0x1003c020
for index, (slot, klass) in enumerate(((0x5353668, 0x10014000), (0x5391658, model_class),
        (0x53573d8, view_class), (0x5356db8, 0x10016000), (0x53dc998, 0x10018000),
        (0x53a0e80, 0x1001a000), (0x53e1378, 0x1001c000), (0x5369128, 0x1001e000))):
    link = 0x10030000 + index * 0x100
    put(slot, link); put(link, klass); put(klass + 0xe0, 1, 4)
for klass in (model_class, view_class):
    put(klass + 0x12c, 1, 1)
    put(klass + 0xc8, klass + 0x1000)
    put(klass + 0x1000, klass)
put(model_class + 0x548, virtual_can_shuffle)
put(model_class + 0x418, virtual_get_view)
put(view_class + 0x208, virtual_sort)
put(0x573ece2, 1, 1)

cells, media_to_item, view_to_transform, transform_positions, model_to_cell = [], {}, {}, {}, {}
events, replacements = [], set()
current_index, allocation, delta = 0, 0x10200000, 0.0
native_ease_calls = native_vector_calls = native_steps = 0

def allocate(size):
    global allocation
    value = allocation
    allocation = (allocation + size + 15) & ~15
    assert allocation < 0x10300000
    u.mem_write(value, bytes(size))
    return value

def hook(uc, address, size, unused):
    global current_index, native_ease_calls, native_vector_calls
    a, b = x(0), x(1)
    if address in (0xffd90c, 0xffda64, 0x231007c): returned()
    elif address == 0xffda78: returned(allocate(0x80))
    elif address == 0xffd9d0:
        value = allocate(0x20 + 12 * b)
        put(value + 0x18, b, 4); returned(value)
    elif address == 0x12de578:
        events.append(['wait', floats(1)[0]]); returned()
    elif address in (0x320a4c0, 0x320a49c): returned(len(cells) if address == 0x320a4c0 else 1)
    elif address == 0x318ec2c:
        current_index = 0; returned()
    elif address == 0x318ecc8:
        cell = cells[current_index] if current_index < len(cells) else 0
        current_index += 1; put(manager + 0x98, cell); returned(cell)
    elif address in (0x1ee6d9c, 0x3195f9c, 0x343bd38, virtual_can_shuffle): returned(1)
    elif address == 0x343bd5c: returned(media_to_item[a])
    elif address == 0x3440db8: returned(0)
    elif address == 0x21a97b4:
        events.append(['raise', model_to_cell[next(k for k in model_to_cell if get(k + 0xb0) == a)], bool(b & 1)])
        returned()
    elif address == 0x3e4737c: returned(view_to_transform[a])
    elif address == 0x3e4e2c8: vector(transform_positions[a]); returned()
    elif address == 0x3e4e368: transform_positions[a] = floats(); returned()
    elif address == 0x20023ac: events.append(['orientation', False]); returned()
    elif address == 0x2002494: events.append(['orientation', True]); returned()
    elif address == 0x1edc738: events.append(['reset', b]); returned()
    elif address == 0x32037e4: vector([delta]); returned()
    elif address == virtual_get_view: returned(get(a + 0xb0))
    elif address == 0x21a77dc:
        if get(a + 0x25c, 1) and a not in replacements:
            replacements.add(a)
            old_view = get(a + 0xb0)
            new_view = allocate(0x100); new_transform = allocate(0x100)
            put(new_view, view_class); view_to_transform[new_view] = new_transform
            transform_positions[new_transform] = floats()
            put(a + 0xb0, new_view)
            events.append(['replace', model_to_cell[a], floats()])
            returned(1)
        else: returned(0)
    elif address == 0x12d7c10:
        u.reg_write(r.UC_ARM64_REG_X1, 0)
        returned(a)
    elif address == virtual_sort:
        events.append(['restore', next(i for model, i in model_to_cell.items() if get(model + 0xb0) == a)])
        returned()
    elif address == 0x2c962dc: native_ease_calls += 1
    elif address == 0x2d7bdf4: native_vector_calls += 1
    else: raise AssertionError(('unexpected native boundary', hex(address), hex(a), hex(b)))

boundaries = [0xffd90c, 0xffda64, 0x231007c, 0xffda78, 0xffd9d0, 0x12de578,
              0x320a4c0, 0x320a49c, 0x318ec2c, 0x318ecc8, 0x1ee6d9c, 0x3195f9c,
              0x343bd38, virtual_can_shuffle, 0x343bd5c, 0x3440db8, 0x21a97b4,
              0x3e4737c, 0x3e4e2c8, 0x3e4e368, 0x20023ac, 0x2002494, 0x1edc738,
              0x32037e4, virtual_get_view, 0x21a77dc, 0x12d7c10, virtual_sort,
              0x2c962dc, 0x2d7bdf4, 0xffd960, 0xffda88, 0xffda90, 0xffde20]
for address in boundaries: u.hook_add(UC_HOOK_CODE, hook, begin=address, end=address)

profiles = {
    '30fps': [.0333], '60fps': [.0166], '90fps': [.0111], 'exact_quarters': [.25],
    'alternating': [.0083, .0373, .0111, .0247], 'zero_delta': [.0166, 0, 0, .0499],
    'hitch': [.0166] * 45 + [.1733, .0917], 'third_second': [.3333]
}
definitions = []
if len(sys.argv) > 3:
    definitions = json.loads(Path(sys.argv[3]).read_text(encoding='utf-8'))
else:
    for count in (1, 2, 14, 73):
        for recolor in (False, True):
            for profile, deltas in profiles.items():
                positions = []
                for i in range(count):
                    start = [f32(i % 9 - 3.25), f32(i // 9 + .375), f32(-.125 * (i % 3))]
                    end = [f32((count - i - 1) % 9 - 3.25), f32((count - i - 1) // 9 + .375), f32(.125 * (i % 2))]
                    positions.append({'start': start, 'end': end})
                definitions.append({'name': f'{count}_{int(recolor)}_{profile}', 'count': count,
                                    'recolor': recolor, 'deltas': deltas, 'positions': positions})
cases = []
for definition in definitions:
    case = {**definition, 'states': []}
    count, recolor, deltas = case['count'], case['recolor'], case['deltas']
    u.mem_write(manager, bytes(0x100)); u.mem_write(routine, bytes(0x80)); u.mem_write(state, bytes(0x40))
    put(manager + 0x18, state); put(manager + 0x20, grid); put(state + 0x18, 1, 4)
    put(routine + 0x20, manager); put(routine + 0x28, bits(1.0), 4)
    cells, media_to_item, view_to_transform, transform_positions, model_to_cell = [], {}, {}, {}, {}
    replacements = set(); allocation = 0x10200000
    for i in range(count):
        cell = 0x10040000 + i * 0x1000
        media, model, view, transform, component, target_transform, blocker = [cell + v for v in (0x200, 0x300, 0x600, 0x700, 0x800, 0x900, 0xa00)]
        start, end = case['positions'][i]['start'], case['positions'][i]['end']
        cells.append(cell); media_to_item[media] = model; model_to_cell[model] = i
        put(cell + 0x10, media); put(cell + 0x1c, i // 9, 4)
        put(cell + 0xb8, component); put(cell + 0xc0, blocker)
        put(model, model_class); put(model + 0xb0, view); put(model + 0x25c, int(recolor and i == 0), 1)
        put(view, view_class); view_to_transform[view] = transform
        view_to_transform[component] = target_transform
        transform_positions[transform] = start; transform_positions[target_transform] = end
    for step in range(400):
        delta = f32(deltas[max(0, step - 1) % len(deltas)])
        events = []
        alive = bool(call(0x1ee8844, routine))
        native_steps += 1
        case['states'].append({'step': step, 'alive': alive, 'events': events[:],
            'elapsed': val(get(routine + 0x40, 4)),
            'positions': [transform_positions[view_to_transform[get(media_to_item[get(c + 0x10)] + 0xb0)]] for c in cells]})
        if not alive: break
    else: raise AssertionError(('did not finish', case['name']))
    cases.append(case)

report = {'passed': True, 'native_method': '0x1ee8844', 'cases': cases,
          'native_steps': native_steps, 'native_ease_calls': native_ease_calls,
          'native_vector_calls': native_vector_calls, 'native_library_sha256': sha256(LIB.read_bytes()).hexdigest(),
          'ports': 'Scene grid iteration, eligibility, model/view and transform storage, allocations/metadata, the supplied active-level delta, wait dispatch, sorting, orientation/operation notifications and recolor replacement are ports. The actual native coroutine control flow, elapsed accumulation, Back.InOut and vector interpolation execute. Active-state scenarios only; search/permutation and native Unity scheduling/rendering/audio are not executed here.'}
(WORK / 'native_shuffle_motion.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
print(json.dumps({k: v for k, v in report.items() if k != 'cases'}, indent=2))
