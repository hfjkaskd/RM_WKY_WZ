"""Compare observed level-80 move decisions with the actual ARM64 finder.

The original finder executes with the existing verification ports extended
from 3x3 to the observed 8x7 board. Eligibility, neighbor lookup, current
item capabilities and metadata are supplied; no Unity engine is emulated.
"""
import hashlib
import json
from pathlib import Path
import sys

sys.dont_write_bytecode = True
comparison_scope = __doc__.strip()
assert len(sys.argv) == 4, 'project input-json output-json'
project = Path(sys.argv[1]).resolve()
source_path = project / 'SourceCatalog/verify_possible_moves.py'
source = source_path.read_text()
separator = 'rng=random.Random(0x1ee5c64);cases=[]'
assert source.count(separator) == 1
prefix = source.split(separator)[0]


def one(text, old, new):
    assert text.count(old) == 1, old
    return text.replace(old, new, 1)


prefix = one(prefix, 'PROJECT=Path(__file__).resolve().parents[1];BASE=PROJECT.parent',
             'PROJECT=Path(sys.argv[1]).resolve();BASE=PROJECT.parent')
for name, old_base, new_base in (
    ('cells', '0x10010000', '0x10010000'), ('meds', '0x10020000', '0x10050000'),
    ('items', '0x10030000', '0x10090000'), ('neighbors', '0x10040000', '0x100d0000'),
    ('static_items', '0x10050000', '0x10110000')):
    prefix = one(prefix, name + '=[' + old_base + '+i*0x1000 for i in range(9)]',
                 name + '=[' + new_base + '+i*0x1000 for i in range(56)]')
prefix = one(prefix,
    'xx=i%3+(-1,0,1,1,1,0,-1,-1)[d];yy=i//3+(1,1,1,0,-1,-1,-1,0)[d]',
    'xx=i%8+(-1,0,1,1,1,0,-1,-1)[d];yy=i//8+(1,1,1,0,-1,-1,-1,0)[d]')
prefix = one(prefix, 'return cells[xx+yy*3]if 0<=xx<3 and 0<=yy<3 else 0',
             'return cells[xx+yy*8]if 0<=xx<8 and 0<=yy<7 else 0')
prefix = one(prefix, 'rows=[data[i*13:i*13+13]for i in range(9)]',
             'rows=[data[i*13:i*13+13]for i in range(56)]')
prefix = one(prefix, 'data[117+i]', 'data[728+i]')
exec(compile(prefix, str(source_path), 'exec'))

payload = json.loads(Path(sys.argv[2]).read_text(encoding='utf-8-sig'))
cases = payload['move_finder_cases']
results = []
for index, state in enumerate(cases):
    assert len(state['rows']) == 7
    ids = [int(value) for row in state['rows'] for value in row.split(',')]
    assert len(ids) == 56 and set(ids) <= {1, 3, 30, 50, 60}
    data = []
    for tile in ids:
        color = tile if tile in (1, 3) else 0
        special = int(tile in (30, 50))
        item_type = 5 if tile == 50 else 2 if tile == 30 else 1 if color else 15
        data.extend((1, 1, 1, 1, special, 0, 0, color, item_type, 0, int(color != 0), special, 1))
    data.extend([1] * 56)
    answer = native([0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 56, 0], data)
    fields = answer.split('#')
    assert fields[0] == '0', answer
    expected = int(fields[1])
    assert expected == state['possible'], (index, expected, state['possible'], ids)
    results.append({'case': index, 'label': state['label'], 'native': expected,
                    'observed': state['possible'], 'native_port_calls': calls})

output = {'passed': True, 'cases': len(cases), 'native_rva': '0x1ee5c64',
          'reference_port_sha256': hashlib.sha256(source.encode()).hexdigest(),
          'input_sha256': hashlib.sha256(Path(sys.argv[2]).read_bytes()).hexdigest(),
          'scope': comparison_scope, 'results': results}
Path(sys.argv[3]).write_text(json.dumps(output, indent=2) + '\n')
print(json.dumps(output, indent=2))
