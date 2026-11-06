"""Author the normal first-board scene and recover its original top background.

This is asset authoring only. It does not recreate runtime hierarchies in code,
install a fake ApplicationContext, or stand in for the pending Home/start flow.
"""
from pathlib import Path
import hashlib
import json
import re

ROOT = Path(__file__).resolve().parents[1]
RAW = ROOT.parent / 'assets/RecoveredProject/ExportedProject/Assets'
SOURCE = RAW / 'Royal/Scenes/Game/GameScene.unity'
original = SOURCE.read_text(encoding='utf-8')
pattern = re.compile(r'^--- !u!(\d+) &(-?\d+)\n(.*?)(?=^---|\Z)', re.M | re.S)
blocks = {int(m[2]): (int(m[1]), m[0]) for m in pattern.finditer(original)}
preview = (ROOT / 'Assets/Foundation/Scenes/RecoveryPreview.unity').read_text(encoding='utf-8')
scene = preview
for cls in (104, 157, 196):
    old = next(m for m in pattern.finditer(scene) if int(m[1]) == cls)
    source = next(value[1] for value in blocks.values() if value[0] == cls)
    # Preserve the current scene's global settings object identity.
    source = re.sub(r'^--- !u!\d+ &-?\d+', f'--- !u!{cls} &{old[2]}', source)
    scene = scene[:old.start()] + source + scene[old.end():]
old = next(m for m in pattern.finditer(scene) if int(m[1]) == 20)
camera = next(value[1] for value in blocks.values() if value[0] == 20)
camera = re.sub(r'^--- !u!20 &\d+', '--- !u!20 &' + old[2], camera)
camera = camera.replace('m_GameObject: {fileID: 82}', 'm_GameObject: {fileID: 289721618}')
scene = scene[:old.start()] + camera + scene[old.end():]
scene = scene.replace('  m_Name: Main Camera\n', '  m_Name: GameCamera\n')
scene = scene.replace('      propertyPath: gameTouchInput\n', '      propertyPath: gameTouchInput\n', 1)
scene_path = ROOT / 'Assets/Foundation/Scenes/GameScene.unity'
assert not scene_path.exists(), 'Authoring is single-use; inspect existing scene instead of overwriting.'
scene_path.write_text(scene, encoding='utf-8', newline='\n')
(Path(str(scene_path) + '.meta')).write_text('fileFormatVersion: 2\nguid: f27cae0e77484b23891b182e5bd533b6\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n', encoding='utf-8')

hud_path = ROOT / 'Assets/Resources/Foundation/Prefabs/OriginalGameplayHud.prefab'
hud = hud_path.read_text(encoding='utf-8')
assert 'portraitBackground:' not in hud
selected = (125, 432, 733)  # Original Top/Background object, Transform and SpriteRenderer.
mapping = {key: blocks[key][0] * 10**12 + key for key in selected}
background = ''.join(blocks[key][1] for key in selected)
background = re.sub(r'(?m)^--- !u!(\d+) &(\d+)', lambda m: f'--- !u!{m[1]} &{mapping[int(m[2])]}', background)
background = re.sub(r'\{fileID: (\d+)\}', lambda m: '{fileID: ' + str(mapping.get(int(m[1]), int(m[1]))) + '}', background)
background = background.replace('m_Father: {fileID: 386}', 'm_Father: {fileID: 4000000000386}')
hud = hud.replace('  - {fileID: 4000000000361}\n', '  - {fileID: 4000000000361}\n  - {fileID: 4000000000432}\n', 1)
hud = hud.replace('  safeTopOffset: -0.1\n', '  safeTopOffset: -0.1\n  portraitBackground: {fileID: 212000000000733}\n')
hud_path.write_text(hud + background, encoding='utf-8', newline='\n')

build_path = ROOT / 'ProjectSettings/EditorBuildSettings.asset'
build = build_path.read_text(encoding='utf-8')
build = build.replace('  m_Scenes:\n', '  m_Scenes:\n  - enabled: 1\n    path: Assets/Foundation/Scenes/GameScene.unity\n    guid: f27cae0e77484b23891b182e5bd533b6\n')
build = build.replace('  - enabled: 1\n    path: Assets/Foundation/Scenes/RecoveryPreview.unity', '  - enabled: 0\n    path: Assets/Foundation/Scenes/RecoveryPreview.unity')
build_path.write_text(build, encoding='utf-8', newline='\n')
report = {
    'original_game_scene_sha256': hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
    'restored_camera_render_lighting_navmesh_settings': True,
    'original_top_background_ids': list(selected),
    'top_background_resource_guid': '0a933794f44b15945b7b6b1d4e4de892',
    'full_original_game_scene_restored': False,
    'original_scene_build_indices_restored': False,
    'home_start_interval_flow_restored': False,
    'scope': 'Normal GameScene uses existing first-board gameplay composition, original GameScene camera and global settings and recovered top background under the actual HUD prefab. GameScene is the temporary build entry while full Application/Start/Home/Interval owners remain pending. No inert booster/settings/King objects or unsupported controller stubs are copied. Camera orientation/safe-area and full scene presentation remain incomplete.',
}
(ROOT / 'SourceCatalog/game_scene_authoring.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
print(json.dumps(report, indent=2))
