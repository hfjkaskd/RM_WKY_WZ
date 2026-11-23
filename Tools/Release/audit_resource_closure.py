from pathlib import Path
from collections import defaultdict, deque
import argparse, json, re, shutil, subprocess

parser = argparse.ArgumentParser(description='Compute a bounded Resources dependency closure without changing Unity assets.')
parser.add_argument('--source', type=Path, required=True, help='Authoritative current Unity project; never modified.')
parser.add_argument('--baseline', type=Path, default=Path(__file__).resolve().with_name('resource-baseline-100-levels.json'))
parser.add_argument('--output', type=Path, required=True, help='Directory for the audit and retention manifests.')
parser.add_argument('--max-assets', type=int, default=2500, help='Fail if the closure unexpectedly expands beyond this count.')
args = parser.parse_args()
SOURCE = args.source.resolve()
OUT = args.output.resolve()
PREFIX = 'Assets/Resources/'
GUID = re.compile(rb'guid:\s*([0-9a-fA-F]{32})')
LITERAL = re.compile(r'"([^"\r\n]+)"')
VALUE = re.compile(r'^\s*(?:[\w.]+:\s*|-\s+)([^\r\n]+)$', re.M)
TEXT_EXT = {'.prefab', '.unity', '.asset', '.mat', '.controller', '.overridecontroller', '.anim', '.playable', '.rendertexture', '.physicmaterial', '.physicsmaterial2d', '.terrainlayer', '.spriteatlas', '.spriteatlasv2', '.json', '.uxml'}

all_paths = {}
for path in (SOURCE / 'Assets').rglob('*'):
    if path.is_file():
        all_paths[path.relative_to(SOURCE).as_posix()] = path
print(f'Indexed {len(all_paths)} asset/source files.', flush=True)

def is_editor(path):
    return any(part in ('Editor', 'EditorOnly') for part in Path(path).parts)

assets = {name: path for name, path in all_paths.items() if not name.endswith('.meta')}
guid_to_asset = {}
resource_paths = defaultdict(list)
rg = shutil.which('rg')
if rg:
    result = subprocess.run([rg, '--hidden', '--no-ignore', '--no-heading', '--with-filename', '-g', '*.meta', '^guid: [0-9a-fA-F]{32}', 'Assets'], cwd=SOURCE, capture_output=True, check=True)
    for row in result.stdout.decode('utf-8').splitlines():
        meta_path, value = row.rsplit(':guid:', 1)
        guid_to_asset[value.strip().lower()] = meta_path.replace('\\', '/')[:-5]
for name, path in all_paths.items():
    if name.endswith('.meta'):
        if not rg:
            with path.open('rb') as stream:
                match = GUID.search(stream.read(2048))
            if match:
                guid_to_asset[match.group(1).decode().lower()] = name[:-5]
    elif '/Resources/' in name and not is_editor(name):
        resource_key = name.split('/Resources/', 1)[1]
        resource_key = str(Path(resource_key).with_suffix('')).replace('\\', '/')
        resource_paths[resource_key.lower()].append(name)
print(f'Indexed {len(guid_to_asset)} asset GUIDs and {len(resource_paths)} resource keys.', flush=True)

baseline_document = json.loads(args.baseline.read_text(encoding='utf-8-sig'))
baseline = set(baseline_document['paths'])
if any(not name.startswith(PREFIX) or '..' in Path(name).parts or name.endswith('.meta') for name in baseline):
    raise ValueError('Baseline contains an invalid Resources asset path.')
missing_baseline = sorted(baseline - assets.keys())
retained, reasons, visited, queue = set(), defaultdict(list), set(), deque()

def include(name, reason):
    if name not in assets:
        return
    if reason not in reasons[name]:
        reasons[name].append(reason)
    if name.startswith(PREFIX):
        retained.add(name)
    if name not in visited:
        visited.add(name)
        queue.append(name)

for name in sorted(baseline):
    include(name, 'verified_134MiB_build_resource')
for name in sorted(assets):
    if name.startswith(PREFIX) and (len(Path(name).parts) == 3 or name.startswith((PREFIX + 'Levels/', PREFIX + 'Obfuz/'))):
        include(name, 'current_level_and_bootstrap_resource')
    elif not name.startswith(PREFIX) and not is_editor(name) and Path(name).suffix.lower() in TEXT_EXT:
        include(name, 'preserved_nonfiltered_serialized_asset')

literal_hits = []
for name, path in assets.items():
    if path.suffix != '.cs' or is_editor(name):
        continue
    text = path.read_text(encoding='utf-8-sig', errors='replace')
    for value in LITERAL.findall(text):
        for match in resource_paths.get(value.lower(), []):
            include(match, 'runtime_literal:' + name + ':' + value)
            literal_hits.append({'source': name, 'value': value, 'asset': match})
print(f'Seeded {len(retained)} root Resources assets; traversing {len(queue)} preserved serialized assets.', flush=True)

unresolved_serialized_resource_values = []
edges = []
while queue:
    name = queue.popleft()
    path = assets[name]
    contents = []
    if path.suffix.lower() in TEXT_EXT:
        contents.append((name, path.read_bytes()))
    meta_name = name + '.meta'
    if meta_name in all_paths:
        contents.append((meta_name, all_paths[meta_name].read_bytes()))
    for origin, data in contents:
        for value in GUID.findall(data):
            target = guid_to_asset.get(value.decode().lower())
            if target and target != name and target in assets:
                include(target, 'serialized_guid:' + origin)
                if target.startswith(PREFIX):
                    edges.append({'from': origin, 'to': target, 'kind': 'guid'})
        if origin.endswith('.meta'):
            continue
        text = data.decode('utf-8', errors='replace')
        for raw in VALUE.findall(text) + LITERAL.findall(text):
            value = raw.strip().strip('"\'')
            for target in resource_paths.get(value.lower(), []):
                include(target, 'serialized_resource_path:' + origin + ':' + value)
                if target.startswith(PREFIX):
                    edges.append({'from': origin, 'to': target, 'kind': 'resource_path'})
            if value.startswith(('Foundation/', 'Recovered/', 'RecoveredLatestFrame/', 'Levels/')) and value.lower() not in resource_paths:
                # Reference-like exact values only, not JSON or expressions.
                if not any(character in value for character in ('{', '}', '"', "'", '+', ';')):
                    unresolved_serialized_resource_values.append({'source': origin, 'value': value})
    if len(visited) % 1000 == 0:
        print(f'Dependency graph: {len(visited)} visited, {len(queue)} pending, {len(retained)} Resources.', flush=True)

all_resource_assets = {name for name in assets if name.startswith(PREFIX)}
added = sorted(retained - baseline)
removed = sorted(all_resource_assets - retained)
full_retain = set(retained)
for name in list(retained):
    if name + '.meta' in all_paths:
        full_retain.add(name + '.meta')
    parent = Path(name).parent
    while str(parent).replace('\\', '/') != 'Assets':
        folder_meta = str(parent).replace('\\', '/') + '.meta'
        if folder_meta in all_paths:
            full_retain.add(folder_meta)
        parent = parent.parent

report = {
    'source': str(SOURCE), 'baseline_file': str(args.baseline),
    'strategy': 'Current bytes of verified resource subset; current 100 level JSONs and root-level bootstrap/Obfuz files; recursive GUID + exact serialized Resources paths from retained assets and preserved non-Editor serialized assets; exact resource literals from runtime C# only. No prefix expansion of recovered archive or unused area configurations.',
    'baseline_asset_count': len(baseline), 'all_resource_asset_count': len(all_resource_assets),
    'retained_asset_count': len(retained), 'excluded_asset_count': len(removed),
    'retained_source_bytes': sum(assets[x].stat().st_size for x in retained),
    'excluded_source_bytes': sum(assets[x].stat().st_size for x in removed),
    'added_asset_count': len(added), 'missing_baseline': missing_baseline,
    'added': [{'path': name, 'bytes': assets[name].stat().st_size, 'reasons': reasons[name]} for name in added],
    'unresolved_serialized_resource_values': sorted({(x['source'], x['value']) for x in unresolved_serialized_resource_values}),
    'retained_files': sorted(full_retain),
    'excluded_assets': removed,
    'resource_dependency_edges': edges,
    'runtime_resource_literal_hits': literal_hits,
    'level_files_1_to_100_present': all(f'Assets/Resources/Levels/level_{n}.json' in retained for n in range(1, 101)),
}
OUT.mkdir(parents=True, exist_ok=True)
(OUT / 'resource-closure-audit.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
(OUT / 'retain-relative-files.json').write_text(json.dumps(sorted(full_retain), ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({k: v for k, v in report.items() if k not in ('retained_files', 'excluded_assets', 'resource_dependency_edges', 'runtime_resource_literal_hits', 'added')}, ensure_ascii=True, indent=2))
print('Added assets:')
for item in report['added']:
    print(json.dumps(item, ensure_ascii=True))
failures = []
if missing_baseline:
    failures.append('Verified baseline assets are missing from current source.')
if report['unresolved_serialized_resource_values']:
    failures.append('Serialized gameplay Resources paths could not be resolved.')
if not report['level_files_1_to_100_present']:
    failures.append('The verified 100-level resource scope is incomplete.')
if len(retained) > args.max_assets:
    failures.append(f'Resources closure exceeded safety threshold {args.max_assets}.')
if failures:
    raise SystemExit('RESOURCE AUDIT FAILED: ' + ' '.join(failures))
