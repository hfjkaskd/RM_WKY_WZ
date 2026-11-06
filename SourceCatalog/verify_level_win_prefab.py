"""Verify result prefab preserves source layout/art and owns standard Buttons.

This is structural fidelity evidence, not a claim of rendered original parity.
"""
from pathlib import Path
import re,json,hashlib
ROOT=Path(__file__).resolve().parents[1]
def blocks(path):
 return {re.search(r'&(-?\d+)\n',p)[1]:p for p in re.split(r'(?=^--- !u!)',path.read_text(encoding='utf-8'),flags=re.M)[1:]}
source=ROOT/'Assets/Resources/Recovered/GameObject/LevelWinDialog.prefab'
target=ROOT/'Assets/Resources/Foundation/Prefabs/OriginalLevelWinDialog.prefab'
a,b=blocks(source),blocks(target);removed=[];changed=[];same=[]
for k,v in a.items():
 if k not in b:assert v.startswith('--- !u!61 ');removed.append(k)
 elif v!=b[k]:
  assert v.startswith(('--- !u!1 ','--- !u!4 ')),k
  changed.append(k)
 else:same.append(k)
assert len(removed)==5 and len(changed)==9
buttons=[(k,v)for k,v in b.items()if'guid: 4e29b1a8efbd4b44bb3f3716e73f07ff' in v]
assert len(buttons)==4
for k,v in buttons:
 assert 'm_Calls: []' in v
 go=re.search(r'm_GameObject: \{fileID: (\d+)',v)[1]
 transform=re.search(r'component: \{fileID: (\d+)',b[go])[1]
 assert b[transform].startswith('--- !u!224 ')
 children=re.search(r'm_Children:\n((?:  - .*\n)+)',b[transform]);assert children
 assert re.search(r'm_Children:\n((?:  - .*\n)+)',a[transform])[1]==children[1]
assert not any(v.startswith(('--- !u!61 ','--- !u!65 '))for v in b.values())
assert 'm_MethodName:' not in target.read_text(encoding='utf-8')
for k,v in a.items():
 if 'm_text:'in v:assert v==b[k],k
shader='guid: fe393ace9b354375a9cb14cdbbc28be4'
materials=('MikadoBlack_CurvedAnimatedBack','MikadoBlack_CurvedAnimatedText','MikadoBlack_GreenOutlineUnderlay2','MikadoBlack_GreenOutlineUnderlay5')
for name in materials:assert shader in (ROOT/f'Assets/Resources/Recovered/Material/{name}.mat').read_text(encoding='utf-8')
paths=[str(p.relative_to(ROOT)).replace('\\','/')for p in(source,target)]+[f'Assets/Resources/Recovered/Material/{n}.mat'for n in materials]
report={'passed':True,'scope':__doc__.strip(),'unchanged_original_blocks':len(same),'modified_go_or_button_transform_blocks':len(changed),'colliders_replaced':len(removed),'standard_buttons':len(buttons),'all_original_tmp_style_blocks_preserved':True,'all_button_visual_children_preserved':True,'files':[{'path':p,'sha256':hashlib.sha256((ROOT/p).read_bytes()).hexdigest()}for p in paths]}
(ROOT/'level_win_prefab_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
