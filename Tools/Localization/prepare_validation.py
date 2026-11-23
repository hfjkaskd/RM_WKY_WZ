"""Synchronize changed project files into the existing isolated Unity test copy."""
from pathlib import Path
import argparse
import json
import re
import shutil
import subprocess
from update_language_table import read_table

ROOT = Path(__file__).resolve().parents[2]


def text_path(path, text_id):
    chunks = re.split(r'^--- !u!\d+ &(\d+)[^\n]*\n', path.read_text(encoding='utf-8-sig'), flags=re.M)
    blocks = dict(zip(chunks[1::2], chunks[2::2]))
    text = blocks[str(text_id)]
    go = re.search(r'm_GameObject: \{fileID: (\d+)', text)[1]
    names = {}
    parents = {}
    transforms = {}
    for identity, block in blocks.items():
        if block.startswith('GameObject:'):
            names[identity] = re.search(r'^  m_Name: (.*)', block, re.M)[1].strip('"\'')
        elif block.startswith(('Transform:', 'RectTransform:')):
            obj = re.search(r'm_GameObject: \{fileID: (\d+)', block)[1]
            transforms[obj] = identity
            parents[identity] = (obj, re.search(r'm_Father: \{fileID: (\d+)', block)[1])
    parts = [names[go]]
    current = parents[transforms[go]][1]
    while current in parents:
        obj, current = parents[current]
        parts.insert(0, names[obj])
    return '/'.join(parts[1:])


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project', type=Path, required=True)
    args = parser.parse_args()
    target = args.project.resolve()
    assert target != ROOT and (target / 'Library').is_dir() and (target / 'ProjectSettings/ProjectVersion.txt').is_file()
    table = read_table((ROOT / 'Assets/Game/Resources/ConfigAssets/TableBin/Table01/tbllanguage.bytes').read_bytes())
    game = json.loads((ROOT / 'Logs/LocalizationFix/gameplay-manifest.json').read_text(encoding='utf-8'))
    frame = json.loads((ROOT / 'Tools/Localization/framework-text-cases.json').read_text(encoding='utf-8'))
    cases = frame['cases']
    modal = next(item for item in game['localizedText'] if item['file'].endswith('/CoreModalTutorial.prefab'))
    suffix = text_path(ROOT / modal['file'], modal['textFileId'])
    settings = (ROOT / 'Assets/Foundation/Settings/CoreFlowSettings.asset').read_text(encoding='utf-8')
    for key in re.findall(r'^    instructionKey: (\S+)', settings, re.M):
        cases.append(dict(prefab=modal['file'], pathSuffix=suffix, scenario=key, texts=table[key]))
    for item in game['localizedText']:
        if item['key'] not in ('RM_Play_Target', 'RM_Play_TutorialHammerSelect'): continue
        extra = 'RM_Play_Reward' if item['key'] == 'RM_Play_Target' else 'RM_Play_TutorialHammerStrike'
        if extra in table:
            cases.append(dict(prefab=item['file'], pathSuffix=text_path(ROOT/item['file'], item['textFileId']), scenario=extra, texts=table[extra]))
    for case in cases:
        value = case['texts']
        case['texts'] = dict(english=value['en-US'], portuguese=value['pt-BR'], indonesian=value['id-ID'])
    (ROOT/'Tools/Localization/validation-cases.json').write_text(json.dumps({'cases': cases}, ensure_ascii=False, indent=2), encoding='utf-8')
    paths = {p for p in game['changedAssets'] if p.endswith('.prefab')}
    paths.update(case['prefab'] for case in cases)
    paths.update(p for p in json.loads((ROOT/'Logs/LocalizationFix/framework-files.json').read_text(encoding='utf-8')) if p.endswith('.prefab'))
    real = ROOT/'Assets/BizzaWZ/Final/Real/UI'
    for directory in ('WithdrawFillPanel','UIWithdrawalConfirmPanel','UIWithdrawalPendingPanel','WithdrawHistory','RealWithdrawalPanl','ServicePanel','ServiceSelectPanel','FAQPanel','DailyMissionPanel','GetRewardPanel','NewbieGiftPage','StarRatingPopup','AddPropPanel'):
        paths.update(p.relative_to(ROOT).as_posix() for p in (real/directory).rglob('*.prefab'))
    (ROOT/'Tools/Localization/validation-prefabs.txt').write_text('\n'.join(sorted(paths))+'\n',encoding='utf-8')
    files = set()
    for command in (['git','diff','--name-only','-z'],['git','ls-files','--others','--exclude-standard','-z']):
        files.update(p for p in subprocess.check_output(command,cwd=ROOT).decode('utf-8').split('\0') if p.startswith('Assets/'))
    files.update(p.relative_to(ROOT).as_posix() for p in (ROOT/'Tools/Localization').rglob('*') if p.is_file() and '__pycache__' not in p.parts)
    files.update(p+'.meta' for p in list(files) if (ROOT/(p+'.meta')).is_file())
    count = 0
    for rel in sorted(files):
        source = ROOT/rel
        if not source.is_file(): continue
        destination = target/rel
        destination.parent.mkdir(parents=True,exist_ok=True)
        shutil.copy2(source,destination); count+=1
    editor = target/'Assets/Editor/LocalizationValidation.cs'
    editor.parent.mkdir(parents=True,exist_ok=True)
    shutil.copy2(ROOT/'Tools/Localization/LocalizationValidation.cs',editor)
    shutil.copy2(ROOT/'Tools/Localization/WithdrawalValidationSetup.cs',editor.with_name('WithdrawalValidationSetup.cs'))
    print(f'Synchronized {count} files; {len(paths)} prefabs and {len(cases)} dynamic text scenarios. Source project unchanged by validation.')


if __name__ == '__main__': main()
