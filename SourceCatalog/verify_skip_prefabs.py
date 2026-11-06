"""Check preserved tap text/style, standard Button ownership and native dialog configuration."""
from pathlib import Path
import hashlib,json,re
ROOT=Path(__file__).resolve().parents[1]
def blocks(path):
 text=path.read_text(encoding='utf-8-sig')
 return {re.search(r'&(-?\d+)\n',part).group(1):part for part in re.split(r'(?=^--- !u!)',text,flags=re.M)[1:]}
def digest(path):return hashlib.sha256((ROOT/path).read_bytes()).hexdigest()
path='Assets/Resources/Foundation/Prefabs/OriginalTapToSkip.prefab';tap=blocks(ROOT/path)
original=blocks(ROOT/'Assets/Resources/Recovered/GameObject/WinLogoAnimationDialog.prefab')
text_id='114611579644537198';label=tap[text_id];old=original[text_id]
keys=('m_fontAsset','m_sharedMaterial','m_text','m_fontSize','m_fontSizeBase','m_characterSpacing','m_fontStyle','m_HorizontalAlignment','m_VerticalAlignment')
for key in keys:
 match=lambda data:re.search(r'^  '+key+r':.*$',data,re.M).group(0)
 assert match(label)==match(old),(key,match(label),match(old))
gradient=lambda data:re.search(r'^  m_fontColorGradient:\n(?:    .*\n)+',data,re.M).group(0)
assert gradient(label)==gradient(old)
root=tap['1946936116752871'];button=tap['114900004'];image=tap['114900002'];raycaster=tap['114900003']
assert 'component: {fileID: 114900004}'in root and 'm_GameObject: {fileID: 1946936116752871}'in button
assert '4e29b1a8efbd4b44bb3f3716e73f07ff'in button and 'fe87c0e1cc204ed48ad3b37840f39efc'in image and 'dc42784cf147c0c48a680349fa168899'in raycaster
assert 'm_Calls: []'in button and 'm_Target:'not in button
assert not any('BoxCollider2D:'in p or 'm_MethodName:'in p for p in tap.values())
assert 'm_Father: {fileID: 4969505138979683}'in tap['4617365563186364']
assert 'm_Father: {fileID: 4617365563186364}'in tap['224998874197659892']
assert 'm_RenderMode: 2'in tap['223900001'] and 'm_CullTransparentMesh: 0'in tap['222900001']
remaining=blocks(ROOT/'Assets/Resources/Foundation/Prefabs/OriginalRemainingMovesDialog.prefab')
assert not any('BoxCollider2D:'in p for p in remaining.values())
assert 'BackgroundFadeInDuration: 0.3'in remaining['114900030'] and 'BackgroundFadeAlpha: 0\n'in remaining['114900030']
assert 'blackFadeOutDuration: 0.3'in remaining['114900030']
foundation=blocks(ROOT/'Assets/Foundation/Prefabs/Foundation.prefab')
assert '76c392e42b5098c458856cdf6ecaaaa1'in foundation['114900040']
assert '4f231c4fb786f3946a6b90b886c48677'in foundation['114900041']
paths=[path,'Assets/Resources/Foundation/Prefabs/OriginalRemainingMovesDialog.prefab',
 'Assets/Resources/Foundation/Prefabs/OriginalWinLogoVisual.prefab','Assets/Foundation/Prefabs/Foundation.prefab',
 'Assets/Foundation/Runtime/OriginalTapToSkipView.cs','Assets/Foundation/Runtime/OriginalRemainingMovesDialog.cs']
report={'passed':True,'scope':__doc__,'standard_button_root_contains_original_visual_subtree':True,'persistent_button_events':0,
 'collider_input_components':0,'original_font_style_preserved':True,'native_full_panel_size':[100,100],
 'transport_difference':'Native100x100 BoxCollider2D/proprietary UiButton is replaced by a100x100 world Canvas/Image/standard Button root containing the original visual subtree, per the user requirement. Text font/material/gradient/layout subtree are preserved. This is not the identical native interaction implementation.',
 'physical_pointer_or_rendered_original_parity_verified':False,
 'files':[{'path':p,'sha256':digest(p)}for p in paths]}
(ROOT/'skip_prefab_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
