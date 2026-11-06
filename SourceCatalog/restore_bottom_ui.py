"""Author source portrait Bottom/Boosters and visible standard Button adapters.

Current normal entry: original AB201 default group0, level1 locked branches.
Settings and unlocked booster ownership are separate pending source owners.
"""
from pathlib import Path
import hashlib,json,re,uuid
ROOT=Path(__file__).resolve().parents[1];RAW=ROOT.parent/'assets/RecoveredProject/ExportedProject/Assets'
SOURCE=RAW/'Royal/Scenes/Game/GameScene.unity'
pattern=re.compile(r'^--- !u!(\d+) &(-?\d+)\n[\s\S]*?(?=^--- !u!|\Z)',re.M)
blocks={int(m[2]):(int(m[1]),m[0])for m in pattern.finditer(SOURCE.read_text(encoding='utf8'))}
inspection=json.loads((ROOT/'SourceCatalog/bottom_scene_inspection.json').read_text())
def meta(p,kind='MonoImporter'):
    m=Path(str(p)+'.meta')
    if not m.exists():m.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n'+kind+':\n  externalObjects: {}\n')
    return re.search(r'guid: (\w+)',m.read_text())[1]
def field(b,name):return int(re.search(r'^  '+name+r': \{fileID: (\d+)\}',b,re.M)[1])
def owner(id):return field(blocks[id][1],'m_GameObject')
def transform(go):return int(re.search(r'component: \{fileID: (\d+)\}',blocks[go][1])[1])
def children(t):return [int(x)for x in re.findall(r'^  - \{fileID: (\d+)\}',blocks[t][1],re.M)]
def subtree(t):
    result={t}
    for child in children(t):result|=subtree(child)
    return result
def add_component(go,id):
    k,b=kept[go];kept[go]=(k,b.replace('  m_Layer:',f'  - component: {{fileID: {id}}}\n  m_Layer:',1))
def native(kind,id,go,body):return f'''--- !u!{kind} &{id}
{ {114:'MonoBehaviour',223:'Canvas',222:'CanvasRenderer'}[kind]}:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
'''+body
def mono(id,go,guid,body):return native(114,id,go,f'''  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {guid}, type: 3}}
  m_Name:
  m_EditorClassIdentifier:
'''+body)
def rect(t,size,pivot=(.5,.5),flip=False):
    k,b=kept[t];assert k==4
    position=re.search(r'm_LocalPosition: \{x: ([^,]+), y: ([^,]+)',b).groups()
    b=b.replace(f'!u!4 &{t}',f'!u!224 &{t}',1).replace('Transform:\n','RectTransform:\n',1)
    if flip:b=re.sub(r'(m_LocalScale: \{x: )([^,]+)',lambda m:m[1]+str(-float(m[2])),b)
    b+=f'''  m_AnchorMin: {{x: 0.5, y: 0.5}}
  m_AnchorMax: {{x: 0.5, y: 0.5}}
  m_AnchoredPosition: {{x: {position[0]}, y: {position[1]}}}
  m_SizeDelta: {{x: {size[0]}, y: {size[1]}}}
  m_Pivot: {{x: {pivot[0]}, y: {pivot[1]}}}
''';kept[t]=(224,b)
root=315;selected={root}|subtree(323)|subtree(312)
# Keep only the four booster slots under their original manager transform.
slot_ids=[927,926,911,925];slot_roots=[transform(owner(i))for i in slot_ids]
selected={root,312}|subtree(323)
for t in slot_roots:selected|=subtree(t)
gos={owner(t)for t in selected};kept={}
for id,(kind,b)in blocks.items():
    go=owner(id)if kind!=1 and re.search(r'm_GameObject:',b)else id
    if go not in gos:continue
    if kind in (1,4,224,23,33,210,212):kept[id]=(kind,b)
    elif kind==114 and ('guid: 334b3af4b97d3ad6a80278f82bcc6489,'in b or 'guid: 9541d86e2fd84c1d9990edf0852d74ab,'in b):
        kept[id]=(kind,b.replace('334b3af4b97d3ad6a80278f82bcc6489','9541d86e2fd84c1d9990edf0852d74ab'))
for go in gos:
    k,b=kept[go];b=re.sub(r'^  - component: \{fileID: (\d+)\}\n',lambda m:m[0]if int(m[1])in kept else'',b,flags=re.M)
    kept[go]=(k,b.replace('m_TagString: Touchable','m_TagString: Untagged'))
for t in selected:
    k,b=kept[t];b=re.sub(r'^  - \{fileID: (\d+)\}\n',lambda m:m[0]if int(m[1])in kept else'',b,flags=re.M)
    b=re.sub(r'  m_Children:\n(?=  m_Father:)','  m_Children: []\n',b)
    if t==root:b=re.sub(r'm_Father: \{fileID: \d+\}','m_Father: {fileID: 0}',b)
    kept[t]=(k,b)
for name in ('OriginalBoosterUnlock','OriginalInGameBoosterSlot','OriginalBottomUi'):meta(ROOT/'Assets/Foundation/Runtime'/f'{name}.cs')
slot_guid=meta(ROOT/'Assets/Foundation/Runtime/OriginalInGameBoosterSlot.cs');bottom_guid=meta(ROOT/'Assets/Foundation/Runtime/OriginalBottomUi.cs')
extra={};slots=[];button_adapters=[]
for index,(slot_id,slot_root)in enumerate(zip(slot_ids,slot_roots)):
    source=blocks[slot_id][1];go=owner(slot_id);native_button=field(source,'button');button_go=owner(native_button);button_t=transform(button_go)
    collider=next(blocks[id][1]for id,(kind,b)in blocks.items()if kind==61 and owner(id)==button_go)
    size=tuple(float(x)for x in re.search(r'm_Size: \{x: ([^,]+), y: ([^}]+)',collider).groups())
    rect(button_t,size)
    group=field(source,'sortingGroup');gk,gb=kept[group]
    order=int(re.search(r'm_SortingOrder: (-?\d+)',gb)[1]);layer=int(re.search(r'm_SortingLayerID: (-?\d+)',gb)[1])
    kept[group]=(gk,gb.replace('  m_Enabled: 1','  m_Enabled: 0'))
    group_nodes=subtree(transform(owner(group)))
    # Visible background graphics replace the original collider hit target.
    background_t=next(t for t in children(button_t)if '  m_Name: Background\n'in blocks[owner(t)][1])
    backplates=[id for id,(k,b)in kept.items()if k==212 and transform(owner(id))in subtree(background_t)]
    assert len(backplates)==2
    for id,(k,b)in list(kept.items()):
        if k==212 and transform(owner(id))in group_nodes:
            kept[id]=(k,re.sub(r'm_SortingOrder: (-?\d+)',lambda m:'m_SortingOrder: '+str(int(m[1])+order),b))
    base=900010000+index*100;canvas=base;raycaster=base+1;button=base+2;slot=base+3;image_ids=[]
    for j,id in enumerate(backplates):
        rb=blocks[id][1];sprite=re.search(r'm_Sprite: (.*)',rb)[1];sprite_guid=re.search(r'guid: (\w+)',sprite)[1]
        assert sprite_guid=='7dbf1c26749851c4795dbd961546f1e1'
        sprite_asset=ROOT/'Assets/Resources/Recovered/Sprite/Game-booster_button.asset';sb=sprite_asset.read_text()
        ppu=float(re.search(r'm_PixelsToUnits: ([^\n]+)',sb)[1]);pivot=tuple(float(x)for x in re.search(r'm_Pivot: \{x: ([^,]+), y: ([^}]+)',sb).groups())
        width=float(re.search(r'^    width: ([^\n]+)',sb,re.M)[1]);height=float(re.search(r'^    height: ([^\n]+)',sb,re.M)[1])
        image_go=owner(id);image_t=transform(image_go);rect(image_t,(width/ppu,height/ppu),pivot,'m_FlipX: 1'in rb)
        k,b=kept[image_go];kept[image_go]=(k,b.replace(f'  - component: {{fileID: {id}}}\n',''));del kept[id]
        renderer=base+10+j*2;image=renderer+1;add_component(image_go,renderer);add_component(image_go,image);image_ids.append(image)
        extra[renderer]=(222,native(222,renderer,image_go,'  m_CullTransparentMesh: 0\n'))
        extra[image]=(114,mono(image,image_go,'fe87c0e1cc204ed48ad3b37840f39efc',f'''  m_Material: {{fileID: 0}}
  m_Color: {{r: 1, g: 1, b: 1, a: 1}}
  m_RaycastTarget: 1
  m_RaycastPadding: {{x: 0, y: 0, z: 0, w: 0}}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {sprite}
  m_Type: 0
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 1
  m_PixelsPerUnitMultiplier: 1
'''))
    for id in (canvas,raycaster,button):add_component(button_go,id)
    extra[canvas]=(223,native(223,canvas,button_go,f'''  m_Enabled: 1
  serializedVersion: 3
  m_RenderMode: 2
  m_Camera: {{fileID: 0}}
  m_PlaneDistance: 100
  m_PixelPerfect: 0
  m_ReceivesEvents: 1
  m_OverrideSorting: 1
  m_OverridePixelPerfect: 0
  m_SortingBucketNormalizedSize: 0
  m_AdditionalShaderChannelsFlag: 0
  m_SortingLayerID: {layer}
  m_SortingOrder: {order+20}
  m_TargetDisplay: 0
'''))
    extra[raycaster]=(114,mono(raycaster,button_go,'dc42784cf147c0c48a680349fa168899','''  m_IgnoreReversedGraphics: 1
  m_BlockingObjects: 0
  m_BlockingMask:
    serializedVersion: 2
    m_Bits: 4294967295
'''))
    extra[button]=(114,mono(button,button_go,'4e29b1a8efbd4b44bb3f3716e73f07ff',f'''  m_Navigation:
    m_Mode: 0
    m_WrapAround: 0
    m_SelectOnUp: {{fileID: 0}}
    m_SelectOnDown: {{fileID: 0}}
    m_SelectOnLeft: {{fileID: 0}}
    m_SelectOnRight: {{fileID: 0}}
  m_Transition: 0
  m_Interactable: 1
  m_TargetGraphic: {{fileID: {image_ids[0]}}}
  m_OnClick:
    m_PersistentCalls:
      m_Calls: []
'''))
    icon_id=field(source,'icon');icon_t=transform(owner(icon_id));icon_block=blocks[icon_id][1]
    icon_sprite=re.search(r'm_Sprite: (.*)',icon_block)[1]
    icon_position=re.search(r'm_LocalPosition: (.*)',blocks[icon_t][1])[1];icon_scale=re.search(r'm_LocalScale: (.*)',blocks[icon_t][1])[1]
    add_component(go,slot);slots.append(slot)
    config=f'''  boosterType: {index+4}
  button: {{fileID: {button}}}
  icon: {{fileID: {icon_id}}}
  lockedIcon: {{fileID: 21300000, guid: 5fe0d0db72449cd42aba2678dd7ae8df, type: 2}}
  unlockedIcon: {icon_sprite}
'''
    for f in ('count','addBooster','freeRibbon','landscapeShadowsGo','countText','freeText','levelText'):
        config+='  '+('landscapeShadows'if f=='landscapeShadowsGo'else f)+': {fileID: '+str(field(source,f))+'}\n'
    const=inspection['native_constants']
    config+=f'''  unlockedIconPosition: {icon_position}
  unlockedIconScale: {icon_scale}
  oldLockedIconPosition: {{x: 0, y: 0, z: 0}}
  newLockedIconPosition: {{x: 0, y: {const['new_lock_y']['float']}, z: 0}}
  oldLockedScale: {const['old_lock_scale']['float']}
  newLockedScale: {const['new_lock_scale']['float']}
  levelLabel: 'Level '
'''
    extra[slot]=(114,mono(slot,go,slot_guid,config))
    button_adapters.append({'source_button':native_button,'source_collider_size':list(size),'visible_backplate_images':image_ids,'standard_button':button,'canvas_sorting':order+20})
bottom=900010900;add_component(5,bottom)
extra[bottom]=(114,mono(bottom,5,bottom_guid,'  boosters:\n'+''.join(f'  - {{fileID: {s}}}\n'for s in slots)+f'''  safeBottomOffset: {inspection['native_constants']['bottom_y_offset']['float']}
  portraitScale: 1
  newUserEventFlowAbGroup: 0
'''))
kept.update(extra);mapping={id:(kind*10**12+id if id<900000000 else id)for id,(kind,b)in kept.items()}
out=[]
for id,(kind,b)in kept.items():
    b=re.sub(r'^--- !u!(\d+) &(-?\d+)',lambda m:f'--- !u!{m[1]} &{mapping[int(m[2])]}',b)
    b=re.sub(r'\{fileID: (\d+)\}',lambda m:'{fileID: '+str(mapping.get(int(m[1]),int(m[1])))+'}',b)
    out.append(b)
target=ROOT/'Assets/Resources/Foundation/Prefabs/OriginalBottomUi.prefab';proof_path=ROOT/'SourceCatalog/bottom_ui_authoring.json'
if target.exists():assert hashlib.sha256(target.read_bytes()).hexdigest()==json.loads(proof_path.read_text())['prefab_sha256'],'Existing prefab changed outside this authoring pass.'
target.write_text('%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'+''.join(out),encoding='utf8',newline='\n');meta(target,'PrefabImporter')
serialized=target.read_text();known=set(mapping.values())
missing={int(x)for x in re.findall(r'\{fileID: (\d+)\}',serialized)if int(x)!=0 and int(x)not in known}
assert not missing,sorted(missing)
assert serialized.count('guid: 9541d86e2fd84c1d9990edf0852d74ab,')==12
proof={'original_scene_sha256':hashlib.sha256(SOURCE.read_bytes()).hexdigest(),'prefab_sha256':hashlib.sha256(target.read_bytes()).hexdigest(),
    'objects':len(gos),'button_adapters':button_adapters,'original_portrait_background_geometry_preserved':True,
    'local_component_references_verified':True,'original_tmp_fields_rebound_to_official_component_count':12,
    'static_runtime_hierarchy_creation':False,'serialized_unity_event_calls':False,'transparent_hit_images':False,
    'default_new_user_flow_ab201_group':0,'unlocked_booster_inventory_selection_use_and_tooltip_owners_bound':False,
    'settings_owner_restored':False,'full_original_bottom_ui_parity':False,
    'scope':'Restored portrait source background and four booster visual subtrees. Standard Buttons use visible original backplate Images on the same visual containers. Four sprite-group orders are flattened into renderer orders to permit Canvas-backed backplates; source transforms/sprites/fonts are retained. Current level1 AB201 group0 executes original disabled lock branch. Full production inventory/use/purchases/tutorial/tooltip/settings and AB provider remain pending.'}
proof_path.write_text(json.dumps(proof,indent=2)+'\n');print(json.dumps(proof,indent=2))
