"""Author the original GameScene King subtree into the real HUD prefab."""
from pathlib import Path
import hashlib,json,re,uuid
ROOT=Path(__file__).resolve().parents[1];RAW=ROOT.parent/'assets/RecoveredProject/ExportedProject/Assets'
def meta(p,kind='MonoImporter'):
    m=Path(str(p)+'.meta')
    if not m.exists():m.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n'+kind+':\n  externalObjects: {}\n')
    return re.search(r'guid: (\w+)',m.read_text())[1]
for name in ('OriginalKingRigData','OriginalKingPose','OriginalKingMesh','OriginalKingVisual'):meta(ROOT/'Assets/Foundation/Runtime'/f'{name}.cs')
# The recovered PNG contains premultiplied RGB. Alpha color dilation is a
# straight-alpha import operation and makes PMA atlas borders visible.
texture_meta=ROOT/'Assets/Resources/Recovered/Texture2D/King.png.meta'
texture_text=texture_meta.read_text();assert '  alphaIsTransparency:'in texture_text
texture_meta.write_text(texture_text.replace('  alphaIsTransparency: 1','  alphaIsTransparency: 0'),newline='\n')
source=RAW/'Royal/Scenes/Game/GameScene.unity';s=source.read_text(encoding='utf8')
blocks={int(m[2]):(int(m[1]),m[0])for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)\n[\s\S]*?(?=^--- !u!|\Z)',s,re.M)}
transforms=set()
def walk(t):
    transforms.add(t)
    for child in re.findall(r'^  - \{fileID: (\d+)\}',blocks[t][1],re.M):walk(int(child))
walk(371)
gos={int(re.search(r'm_GameObject: \{fileID: (\d+)\}',blocks[t][1])[1])for t in transforms}
kept={}
for i,(kind,b)in blocks.items():
    go=re.search(r'm_GameObject: \{fileID: (\d+)\}',b)
    if(kind==1 and i in gos)or(kind in (4,23,33,210,212)and go and int(go[1])in gos):kept[i]=(kind,b)
assert not any(k==114 for k,b in kept.values())
mapping={i:kind*10**12+i for i,(kind,b)in kept.items()}
def remap(b):
    b=re.sub(r'(?m)^--- !u!(\d+) &(\d+)',lambda m:f'--- !u!{m[1]} &{mapping[int(m[2])]}',b)
    return re.sub(r'\{fileID: (\d+)\}',lambda m:'{fileID: '+str(mapping.get(int(m[1]),int(m[1])))+'}',b)
visual=114000000100062;out=[]
for i,(kind,b)in kept.items():
    if kind==1:
        b=re.sub(r'^  - component: \{fileID: (\d+)\}\n',lambda m:m[0]if int(m[1])in kept else'',b,flags=re.M)
        if i==62:b=b.replace('  m_Layer:',f'  - component: {{fileID: {visual}}}\n  m_Layer:',1)
    if kind==4:
        b=re.sub(r'^  - \{fileID: (\d+)\}\n',lambda m:m[0]if int(m[1])in kept else'',b,flags=re.M)
        b=re.sub(r'  m_Children:\n(?=  m_Father:)','  m_Children: []\n',b)
    b=remap(b)
    if kind==4 and i==371:b=b.replace('m_Father: {fileID: 386}','m_Father: {fileID: 4000000000386}')
    # Mesh/material dependencies are runtime generated/weak loaded. Original
    # source disabled generator renderer remains disabled; two actual parts draw.
    if kind==33:b=re.sub(r'  m_Mesh: .*','  m_Mesh: {fileID: 0}',b)
    if kind==23:b=re.sub(r'  m_Materials:\n(?:  - .*\n)+','  m_Materials: []\n',b)
    out.append(b)
filters=[];renderers=[]
for original_go in (66,46):
    filters.append(next(mapping[i]for i,(k,b)in kept.items()if k==33 and f'm_GameObject: {{fileID: {original_go}}}'in b))
    renderers.append(next(mapping[i]for i,(k,b)in kept.items()if k==23 and f'm_GameObject: {{fileID: {original_go}}}'in b))
guid=meta(ROOT/'Assets/Foundation/Runtime/OriginalKingVisual.cs')
material_guid=meta(ROOT/'Assets/Foundation/Settings/OriginalWinLogoMaterial.mat','NativeFormatImporter')
out.append(f'''--- !u!114 &{visual}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {mapping[62]}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {guid}, type: 3}}
  m_Name:
  m_EditorClassIdentifier:
  dataPath: Foundation/Animation/OriginalKing
  texturePath: Recovered/Texture2D/King
  separatorSlot: sol frame
  idleAnimations:
  - idle_loop_2
  - idle_loop_2
  - idle_loop_3
  idleConnects:
  - idle_connect_1
  - idle_connect_2
  happyAnimation: k-1sevinme_6
  sadAnimation: k-1_uzulme
  idleStepDelay: 5
  idleRepeatDelay: 50
  happyCooldown: 10
  frameSlots:
  - sag frame
  - sol frame
  frameAttachment: blue frame
  materialTemplate: {{fileID: 2100000, guid: {material_guid}, type: 2}}
  parts:
'''+''.join(f'  - {{fileID: {i}}}\n'for i in filters)+'  renderers:\n'+''.join(f'  - {{fileID: {i}}}\n'for i in renderers))
hud_path=ROOT/'Assets/Resources/Foundation/Prefabs/OriginalGameplayHud.prefab';hud=hud_path.read_text(encoding='utf8')
if 'kingVisual:'in hud:
    # Replace only the previously authored, explicitly mapped subtree. Keep
    # every other HUD block and field byte-for-byte.
    authored=set(mapping.values())|{visual}
    hud=re.sub(r'^--- !u!(\d+) &(-?\d+)\n[\s\S]*?(?=^--- !u!|\Z)',lambda m:''if int(m[2])in authored else m[0],hud,flags=re.M)
    hud=re.sub(r'^  kingVisual: .*\n','',hud,flags=re.M)
    hud=hud.replace(f'  - {{fileID: {mapping[371]}}}\n','')
hud=hud.replace('  portraitBackground:',f'  kingVisual: {{fileID: {visual}}}\n  portraitBackground:',1)
root=re.search(r'^--- !u!4 &4000000000386\n[\s\S]*?(?=^---|\Z)',hud,re.M)
assert root;changed=root[0].replace('  m_Children:\n',f'  m_Children:\n  - {{fileID: {mapping[371]}}}\n',1)
hud=hud[:root.start()]+changed+hud[root.end():]+''.join(out)
hud_path.write_text(hud,encoding='utf8',newline='\n')
report={'source_scene_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'original_objects':sorted(gos),'original_transforms':sorted(transforms),
    'parts':filters,'renderers':renderers,'prefab_hierarchy_runtime_created':False,'third_party_runtime_shipped':False,
    'current_animation_scope':'Analytical original idle_loop_1. Full KingUi queue, shuffle, 0.2s mixing, reactions and frame/theme policy pending.',
    'original_pma_texture_sha256':hashlib.sha256((ROOT/'Assets/Resources/Recovered/Texture2D/King.png').read_bytes()).hexdigest(),
    'pma_transparent_rgb_dilation_disabled':True,
    'full_original_scene_restored':False}
(ROOT/'SourceCatalog/king_visual_authoring.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report,indent=2))
