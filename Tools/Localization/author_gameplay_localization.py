"""Author localization components on the existing gameplay prefab text nodes.

Only edits serialized content/layout; runtime does not manufacture UI. Existing
files are preserved once under Logs/LocalizationFix/before before any mutation.
"""
from pathlib import Path
import json
import re

ROOT = Path(__file__).resolve().parents[2]
GUID = '7cdf8f1617e24435a8e9371d3d648b55'
PREFABS = ROOT / 'Assets/Resources/Foundation/Prefabs'
BACKUP = ROOT / 'Logs/LocalizationFix/before'
translations = {}


def term(key, en, pt, ind):
    translations[key] = {'en-US': en, 'pt-BR': pt, 'id-ID': ind}
    return key


TEXT_KEYS = {
    'Target': term('RM_Play_Target', 'Target', 'Meta', 'Target'),
    'Moves': term('RM_Play_Moves', 'Moves', 'Jogadas', 'Langkah'),
    'Reward': term('RM_Play_Reward', 'Reward', 'Recompensa', 'Hadiah'),
    'Free': term('RM_Play_Free', 'Free', 'Grátis', 'Gratis'),
    'Quit Level?': term('RM_Play_QuitTitle', 'Quit Level?', 'Sair da fase?', 'Keluar level?'),
    'Current attempt progress will be lost.': term('RM_Play_QuitHint', 'Current attempt progress will be lost.', 'O progresso desta tentativa será perdido.', 'Progres percobaan ini akan hilang.'),
    'Quit': term('RM_Play_Quit', 'Quit', 'Sair', 'Keluar'),
    'Out of Moves!': term('RM_Play_OutOfMoves', 'Out of Moves!', 'Sem jogadas!', 'Langkah habis!'),
    'Tap to Skip': term('RM_Play_Skip', 'Tap to Skip', 'Toque para pular', 'Ketuk untuk melewati'),
    'Make a match next to a BOX to break it!': term('RM_Play_TutorialBox', 'Make a match next to a BOX to break it!', 'Faça uma combinação ao lado da CAIXA para quebrá-la!', 'Buat kombinasi di samping KOTAK untuk memecahkannya!'),
    'Make a match on the GRASS to remove it!': term('RM_Play_TutorialGrass', 'Make a match on the GRASS to remove it!', 'Faça uma combinação na GRAMA para removê-la!', 'Buat kombinasi di atas RUMPUT untuk menghapusnya!'),
    'You can tap on power-ups to explode them!': term('RM_Play_TutorialTapPower', 'You can tap on power-ups to explode them!', 'Toque nos poderes para ativá-los!', 'Ketuk power-up untuk mengaktifkannya!'),
    'You can swap two power-ups to make a more powerful explosion!': term('RM_Play_TutorialCombinePower', 'You can swap two power-ups to make a more powerful explosion!', 'Combine dois poderes para uma explosão mais forte!', 'Gabungkan dua power-up untuk ledakan lebih kuat!'),
    'ARROW clears everything\nin a row!': term('RM_Play_TutorialArrow', 'ARROW clears everything\nin a row!', 'A FLECHA limpa uma linha inteira!', 'PANAH membersihkan satu baris penuh!'),
    'CANNON clears everything in a column!': term('RM_Play_TutorialCannon', 'CANNON clears everything in a column!', 'O CANHÃO limpa uma coluna inteira!', 'MERIAM membersihkan satu kolom penuh!'),
    'JESTER HAT shuffles\nall the items on the board!': term('RM_Play_TutorialJester', 'JESTER HAT shuffles\nall the items on the board!', 'O CHAPÉU DO BOBO embaralha todas as peças!', 'TOPI BADUT mengacak semua keping di papan!'),
    'Hit OWL STATUE with power-ups to break it!': term('RM_Play_TutorialOwl', 'Hit OWL STATUE with power-ups to break it!', 'Use poderes para quebrar a ESTÁTUA DE CORUJA!', 'Gunakan power-up untuk memecahkan PATUNG BURUNG HANTU!'),
    'This is a bonus level! Collect as many COINS as possible in given moves!': term('RM_Play_TutorialBonus', 'This is a bonus level! Collect as many COINS as possible in given moves!', 'Fase bônus! Use as jogadas para coletar o máximo de MOEDAS!', 'Level bonus! Kumpulkan sebanyak mungkin KOIN dengan langkah yang ada!'),
    'Continue': term('RM_Play_Continue', 'Continue', 'Continuar', 'Lanjutkan'),
    'Try Again': term('RM_Play_Retry', 'Try Again', 'Tentar de novo', 'Coba lagi'),
    'Level Failed!': term('RM_Play_Failed', 'Level Failed!', 'Fase perdida!', 'Level gagal!'),
    'Well Done!': term('RM_Play_WellDone', 'Well Done!', 'Muito bem!', 'Hebat!'),
    'Hard Level': term('RM_Play_Hard', 'Hard Level', 'Fase difícil', 'Level sulit'),
    'Super Hard': term('RM_Play_SuperHard', 'Super Hard', 'Muito difícil', 'Sangat sulit'),
    'Extremely Hard': term('RM_Play_ExtremeHard', 'Extremely Hard', 'Dificuldade extrema', 'Sangat menantang'),
    'No Possible Moves!': term('RM_Play_NoMoves', 'No Possible Moves!', 'Sem combinações!', 'Tidak ada langkah!'),
    'You can restart the level!': term('RM_Play_RestartHint', 'You can restart the level!', 'Você pode reiniciar a fase!', 'Kamu bisa mengulang level ini!'),
    'Restart': term('RM_Play_Restart', 'Restart', 'Reiniciar', 'Ulangi'),
}
term('RM_Play_LevelPrefix', 'Level ', 'Nível ', 'Level ')
term('RM_Play_LevelFormat', 'Level {0}', 'Nível {0}', 'Level {0}')
term('RM_Play_Complete', '100 Levels Complete', '100 fases concluídas', '100 level selesai')
term('RM_Play_PlayAgain', 'Play Again', 'Jogar de novo', 'Main lagi')
term('RM_Play_HammerSelect', 'Tap on ROYAL HAMMER to select it!', 'Toque no MARTELO REAL para selecioná-lo!', 'Ketuk PALU KERAJAAN untuk memilihnya!')
term('RM_Play_HammerStrike', 'Tap on the item to clear it from the board!', 'Toque na peça que deseja remover!', 'Ketuk keping yang ingin dihapus!')
MODAL = {
    21: term('RM_Play_TutorialMail', 'Make matches next to a MAILBOX to collect MAILS!', 'Faça combinações ao lado da CAIXA DE CORREIO para coletar CARTAS!', 'Buat kombinasi di samping KOTAK POS untuk mengumpulkan SURAT!'),
    31: term('RM_Play_TutorialEgg', 'Break the ROYAL EGG by making a match next to it!', 'Faça uma combinação ao lado do OVO REAL para quebrá-lo!', 'Buat kombinasi di samping TELUR KERAJAAN untuk memecahkannya!'),
    41: term('RM_Play_TutorialPotion', 'Break a POTION BOTTLE by making a colored match next to it!', 'Faça uma combinação da mesma cor ao lado da POÇÃO para quebrá-la!', 'Buat kombinasi warna yang sama di samping BOTOL RAMUAN untuk memecahkannya!'),
    51: term('RM_Play_TutorialVase', 'Make matches 2 times\nnext to a VASE to break it!', 'Faça 2 combinações ao lado do VASO para quebrá-lo!', 'Buat 2 kombinasi di samping VAS untuk memecahkannya!'),
    71: term('RM_Play_TutorialBush', 'Make matches 5 times next to a BUSH to spread GRASS!', 'Faça 5 combinações ao lado do ARBUSTO para espalhar GRAMA!', 'Buat 5 kombinasi di samping SEMAK untuk menyebarkan RUMPUT!'),
    81: term('RM_Play_TutorialHoney', 'Clear HONEY by making a match next to it!', 'Faça uma combinação ao lado do MEL para removê-lo!', 'Buat kombinasi di samping MADU untuk menghapusnya!'),
    91: term('RM_Play_TutorialSafe', 'Hit SAFE 5 times with power-ups to break it!', 'Acerte o COFRE 5 vezes com poderes para abri-lo!', 'Hantam BRANKAS 5 kali dengan power-up untuk membukanya!'),
}


def backup(path):
    dest = BACKUP / path.relative_to(ROOT)
    dest.parent.mkdir(parents=True, exist_ok=True)
    if not dest.exists(): dest.write_bytes(path.read_bytes())


def write_preserving_format(path, text):
    original = path.read_bytes()
    newline = b'\r\n' if b'\r\n' in original else b'\n'
    content = text.replace('\r\n', '\n').replace('\r', '\n').replace('\n', newline.decode('ascii'))
    bom = b'\xef\xbb\xbf' if original.startswith(b'\xef\xbb\xbf') else b''
    path.write_bytes(bom + content.encode('utf-8'))


def scalar(doc, field):
    match = re.search(r'^  ' + re.escape(field) + r':(.*?)(?=^  \w|\Z)', doc, re.M | re.S)
    if not match: return ''
    raw = match[1].strip()
    if raw.startswith('"'):
        try: return json.loads(raw)
        except json.JSONDecodeError: return raw.strip('"').replace('\\n', '\n')
    if raw.startswith("'"):
        raw = raw[1:-1].replace("''", "'")
    return re.sub(r'\s+', ' ', raw).strip()


def setfield(doc, field, value):
    pat = r'(?m)^  ' + re.escape(field) + r':[^\n]*'
    if re.search(pat, doc): return re.sub(pat, lambda _: '  ' + field + ': ' + str(value), doc)
    return doc.rstrip() + '\n  ' + field + ': ' + str(value) + '\n'


manifest = []
changed = []
for path in sorted(PREFABS.glob('*.prefab')):
    source = path.read_text(encoding='utf-8-sig')
    chunks = re.split(r'(?m)^--- ', source)
    docs = {re.search(r'&(-?\d+)', doc)[1]: doc for doc in chunks[1:]}
    additions = []
    localizers = {}
    next_id = 991000000000001
    for text_id, doc in list(docs.items()):
        if '  m_text:' not in doc: continue
        english = scalar(doc, 'm_text')
        if english.startswith('Current attempt progress will be lost.'):
            english = 'Current attempt progress will be lost.'
        key = TEXT_KEYS.get(english)
        if not key: continue
        go = re.search(r'm_GameObject: \{fileID: (-?\d+)\}', doc)[1]
        existing = next((i for i, d in docs.items() if f'guid: {GUID}' in d and f'm_GameObject: {{fileID: {go}}}' in d), None)
        while str(next_id) in docs: next_id += 1
        local_id = existing or str(next_id)
        next_id += 1
        localizers[text_id] = local_id
        if existing is None:
            docs[go] = docs[go].replace('  m_Layer:', f'  - component: {{fileID: {local_id}}}\n  m_Layer:', 1)
            additions.append(local_id)
        docs[local_id] = f'''!u!114 &{local_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {GUID}, type: 3}}
  m_Name:
  m_EditorClassIdentifier:
  target: {{fileID: {text_id}}}
  key: {key}
  fallback: {json.dumps(english, ensure_ascii=True)}
'''
        # Remove stale placeholder tails and keep the unbound scene readable.
        doc = re.sub(r'^  m_text:.*?(?=^  m_isRightToLeft:)', lambda _: '  m_text: ' + json.dumps(english, ensure_ascii=True) + '\n', doc, flags=re.M | re.S)
        if 'Guide' in path.stem or path.stem == 'CoreModalTutorial':
            for f, v in [('m_enableAutoSizing', 1), ('m_fontSizeMin', 2.1), ('m_fontSizeMax', 3.75), ('m_enableWordWrapping', 1), ('m_overflowMode', 0)]: doc = setfield(doc, f, v)
        elif path.stem in ('CoreTapToSkip', 'OriginalTapToSkip'):
            gui = 'guid: f4688fdb7df04437aeb418b961361dc5' in doc
            minimum, maximum = (0.42, 0.63) if gui else (2.4, 4.2)
            for f, v in [('m_enableAutoSizing', 1), ('m_fontSizeMin', minimum), ('m_fontSizeMax', maximum), ('m_enableWordWrapping', 0), ('m_overflowMode', 0)]: doc = setfield(doc, f, v)
        elif path.stem in ('OriginalBottomUi', 'CoreTutorialFreeBoosterBadge'):
            for f, v in [('m_enableAutoSizing', 1), ('m_fontSizeMin', 2.0), ('m_fontSizeMax', 3.5), ('m_enableWordWrapping', 0), ('m_overflowMode', 0)]: doc = setfield(doc, f, v)
        elif path.stem == 'CoreLevelQuitDialog':
            for f, v in [('m_enableAutoSizing', 1), ('m_enableWordWrapping', int(key == 'RM_Play_QuitHint')), ('m_overflowMode', 0)]: doc = setfield(doc, f, v)
        elif path.stem == 'OriginalGameplayHud':
            doc = setfield(doc, 'm_enableAutoSizing', 1)
            doc = setfield(doc, 'm_fontSizeMin', 3.0)
            doc = setfield(doc, 'm_enableWordWrapping', 0)
            doc = setfield(doc, 'm_overflowMode', 0)
        docs[text_id] = doc
        rect = next((d for d in docs.values() if d.startswith('!u!224 ') and f'm_GameObject: {{fileID: {go}}}' in d), '')
        manifest.append({'file': str(path.relative_to(ROOT)).replace('\\', '/'), 'textFileId': text_id, 'componentFileId': local_id, 'key': key, 'fallback': english, 'sizeDelta': scalar(rect, 'm_SizeDelta')})

    # Runtime-owned labels keep their localization key as state changes.
    for doc_id, doc in list(docs.items()):
        if path.stem == 'OriginalGameplayHud' and re.search(r'^  targetTitle: ', doc, re.M):
            tid = re.search(r'targetTitle: \{fileID: (-?\d+)\}', doc)[1]
            doc = setfield(doc, 'localizedTargetTitle', '{fileID: ' + localizers[tid] + '}')
            doc = setfield(doc, 'targetTitleKey', 'RM_Play_Target')
            doc = setfield(doc, 'rewardTitleKey', 'RM_Play_Reward')
        if path.stem in ('CoreModalTutorial', 'CoreLevelEightHammerGuide') and re.search(r'^  instruction: \{fileID:', doc, re.M):
            tid = re.search(r'instruction: \{fileID: (-?\d+)\}', doc)[1]
            doc = setfield(doc, 'localizedInstruction', '{fileID: ' + localizers[tid] + '}')
            if path.stem == 'CoreLevelEightHammerGuide':
                doc = setfield(doc, 'selectTextKey', 'RM_Play_HammerSelect')
                doc = setfield(doc, 'strikeTextKey', 'RM_Play_HammerStrike')
        if path.stem == 'OriginalBottomUi' and re.search(r'^  levelLabel:', doc, re.M):
            doc = setfield(doc, 'levelLabelKey', 'RM_Play_LevelPrefix')
        docs[doc_id] = doc
    output = chunks[0] + ''.join('--- ' + doc for doc in docs.values())
    if output != source:
        backup(path); write_preserving_format(path, output); changed.append(str(path.relative_to(ROOT)).replace('\\', '/'))

path = ROOT / 'Assets/Foundation/Settings/CoreFlowSettings.asset'
source = path.read_text(encoding='utf-8-sig')
parts = re.split(r'(?m)(?=  - level: )', source)
for i in range(1, len(parts)):
    level = int(re.match(r'  - level: (\d+)', parts[i])[1])
    if level in MODAL and re.search(r'^    instruction:', parts[i], re.M):
        parts[i] = re.sub(r'^    instructionKey:.*\n', '', parts[i], flags=re.M)
        parts[i] = re.sub(r'(^    instruction:.*\n)', lambda m: m[1] + '    instructionKey: ' + MODAL[level] + '\n', parts[i], flags=re.M)
output = ''.join(parts)
for field, key in [('levelTitleKey', 'RM_Play_LevelFormat'), ('completedTitleKey', 'RM_Play_Complete'), ('continueTextKey', 'RM_Play_Continue'), ('completedContinueTextKey', 'RM_Play_PlayAgain'), ('retryTextKey', 'RM_Play_Retry'), ('failTextKey', 'RM_Play_Failed'), ('skipTextKey', 'RM_Play_Skip')]:
    output = setfield(output, field, key)
if output != source:
    backup(path); write_preserving_format(path, output); changed.append(str(path.relative_to(ROOT)).replace('\\', '/'))

# Level 20 is a prefab variant of the owl tutorial. Override the inherited
# localizer as well as its TMP fallback, otherwise it would show the owl text.
path = PREFABS / 'CoreLevelTwentyBonusGuide.prefab'
source = path.read_text(encoding='utf-8-sig')
base_guid = 'ae9255fdaed0ff44ab7a9f21a22cfaa3'
base_localizer = next(row['componentFileId'] for row in manifest if row['file'].endswith('/CoreLevelSixtyOneOwlGuide.prefab'))
for field, value in [('key', 'RM_Play_TutorialBonus'), ('fallback', translations['RM_Play_TutorialBonus']['en-US'])]:
    pattern = (r'    - target: \{fileID: ' + base_localizer + ', guid: ' + base_guid + r', type: 3\}\n'
               r'      propertyPath: ' + field + r'\n      value: [^\n]*\n      objectReference: \{fileID: 0\}\n')
    source = re.sub(pattern, '', source)
    addition = (f'    - target: {{fileID: {base_localizer}, guid: {base_guid}, type: 3}}\n'
                f'      propertyPath: {field}\n      value: {json.dumps(value)}\n      objectReference: {{fileID: 0}}\n')
    source = source.replace('    m_RemovedComponents:', addition + '    m_RemovedComponents:', 1)
if source != path.read_text(encoding='utf-8-sig'):
    backup(path); write_preserving_format(path, source); changed.append(str(path.relative_to(ROOT)).replace('\\', '/'))
manifest.append({'file': str(path.relative_to(ROOT)).replace('\\', '/'), 'textFileId': '3084874079005156344', 'componentFileId': base_localizer, 'key': 'RM_Play_TutorialBonus', 'fallback': translations['RM_Play_TutorialBonus']['en-US'], 'variantOf': 'CoreLevelSixtyOneOwlGuide.prefab'})

Path(__file__).with_name('gameplay-translations.json').write_text(json.dumps(translations, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
dest = ROOT / 'Logs/LocalizationFix/gameplay-manifest.json'
dest.parent.mkdir(parents=True, exist_ok=True)
configured_assets = sorted(set(changed + [row['file'] for row in manifest] + ['Assets/Foundation/Settings/CoreFlowSettings.asset']))
dest.write_text(json.dumps({'changedAssets': configured_assets, 'localizedText': manifest, 'translationKeys': list(translations)}, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print('Gameplay localized labels:', len(manifest), 'translation keys:', len(translations), 'configured assets:', len(configured_assets), 'changed this run:', len(changed))
