"""Author a runtime result prefab from the recovered static hierarchy.

Only the input adapters/custom owner are replaced. Sprite/font/particle/
Animator data stay in the source prefab. Collider input becomes standard Unity
Buttons on the same visual containers, as required by project policy.
"""
from pathlib import Path
import re, uuid, json, hashlib

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'Assets/Resources/Recovered/GameObject/LevelWinDialog.prefab'
TARGET = ROOT / 'Assets/Resources/Foundation/Prefabs/OriginalLevelWinDialog.prefab'
assert not TARGET.exists(), 'One-time authoring helper must not overwrite edits.'
parts = re.split(r'(?=^--- !u!)', SOURCE.read_text(encoding='utf-8'), flags=re.M)
blocks = {re.search(r'&(-?\d+)\n', p).group(1): p for p in parts[1:]}

def native(kind, fileid, go, fields):
    return f'''--- !u!{kind[0]} &{fileid}
{kind[1]}:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
'''+fields

def mono(fileid, go, guid, fields):
    return native((114, 'MonoBehaviour'), fileid, go, f'''  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {guid}, type: 3}}
  m_Name:
  m_EditorClassIdentifier:
'''+fields)

def add_components(go, ids):
    blocks[go] = re.sub(r'(  m_Component:\n(?:  - component:.*\n)+)',
                       lambda m:m[1]+''.join(f'  - component: {{fileID: {i}}}\n' for i in ids), blocks[go])

colliders = [k for k,b in blocks.items() if b.startswith('--- !u!61 ')]
collider_data = {k: blocks[k] for k in colliders}
for k in colliders:
    blocks.pop(k)
    for go in list(blocks):
        if blocks[go].startswith('--- !u!1 '):
            blocks[go] = blocks[go].replace(f'  - component: {{fileID: {k}}}\n', '').replace('m_TagString: Touchable', 'm_TagString: Untagged')

buttons = []
canvases = []
for index, (go, transform, collider) in enumerate((
    ('1044625135789424','4240571728440943','61617365563186364'),
    ('1002669891404536','4771092198208730','61570723408815475'),
    ('1946936116752871','4708180620337799','61589652054545887'),
    ('1658330495870062','4077469347768860','61232720755092366'))):
    ids = [str(900100000+index*10+i) for i in range(5)]
    canvas, renderer, graphic, raycaster, button = ids
    add_components(go, ids)
    data=collider_data[collider]
    size = tuple(map(float,re.search(r'm_Size: \{x: ([^,]+), y: ([^}]+)',data).groups()))
    offset = tuple(map(float,re.search(r'm_Offset: \{x: ([^,]+), y: ([^}]+)',data).groups()))
    position = tuple(map(float,re.search(r'm_LocalPosition: \{x: ([^,]+), y: ([^,]+),',blocks[transform]).groups()))
    blocks[transform]=blocks[transform].replace('!u!4 ', '!u!224 ',1).replace('Transform:\n','RectTransform:\n',1)+f'''  m_AnchorMin: {{x: 0.5, y: 0.5}}
  m_AnchorMax: {{x: 0.5, y: 0.5}}
  m_AnchoredPosition: {{x: {position[0]}, y: {position[1]}}}
  m_SizeDelta: {{x: {size[0]}, y: {size[1]}}}
  m_Pivot: {{x: {0.5-offset[0]/size[0]}, y: {0.5-offset[1]/size[1]}}}
'''
    blocks[canvas]=native((223,'Canvas'),canvas,go,'''  m_Enabled: 1
  serializedVersion: 3
  m_RenderMode: 2
  m_Camera: {fileID: 0}
  m_PlaneDistance: 100
  m_PixelPerfect: 0
  m_ReceivesEvents: 1
  m_OverrideSorting: 1
  m_OverridePixelPerfect: 0
  m_SortingBucketNormalizedSize: 0
  m_AdditionalShaderChannelsFlag: 0
  m_SortingLayerID: 2046106761
  m_SortingOrder: 1
  m_TargetDisplay: 0
''')
    blocks[renderer]=native((222,'CanvasRenderer'),renderer,go,'  m_CullTransparentMesh: 0\n')
    blocks[graphic]=mono(graphic,go,'fe87c0e1cc204ed48ad3b37840f39efc','''  m_Material: {fileID: 0}
  m_Color: {r: 1, g: 1, b: 1, a: 0}
  m_RaycastTarget: 1
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {fileID: 0}
  m_Type: 0
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
''')
    blocks[raycaster]=mono(raycaster,go,'dc42784cf147c0c48a680349fa168899','''  m_IgnoreReversedGraphics: 1
  m_BlockingObjects: 0
  m_BlockingMask:
    serializedVersion: 2
    m_Bits: 4294967295
''')
    blocks[button]=mono(button,go,'4e29b1a8efbd4b44bb3f3716e73f07ff',f'''  m_Navigation:
    m_Mode: 0
    m_WrapAround: 0
    m_SelectOnUp: {{fileID: 0}}
    m_SelectOnDown: {{fileID: 0}}
    m_SelectOnLeft: {{fileID: 0}}
    m_SelectOnRight: {{fileID: 0}}
  m_Transition: 0
  m_Interactable: 1
  m_TargetGraphic: {{fileID: {graphic}}}
  m_OnClick:
    m_PersistentCalls:
      m_Calls: []
''')
    buttons.append(button);canvases.append(canvas)

for name in ('OriginalLevelWinDialog','OriginalLevelWinActions'):
    meta=ROOT/f'Assets/Foundation/Runtime/{name}.cs.meta'
    assert not meta.exists()
    guid=uuid.uuid4().hex
    meta.write_text('fileFormatVersion: 2\nguid: '+guid+'\nMonoImporter:\n  externalObjects: {}\n',encoding='utf-8')
    if name=='OriginalLevelWinDialog': owner_guid=guid

add_components('1709254077376921',['114900100'])
blocks['114900100']=mono('114900100','1709254077376921',owner_guid,f'''  sortingGroup: {{fileID: 210354824845356902}}
  config:
    ShouldCloseOnEscape: 1
    ShouldCloseOnTouch: 0
    ShouldCloseOnSwipe: 0
    BackgroundFadeInDelay: 0
    BackgroundFadeInDuration: 0
    BackgroundFadeOutDuration: 0
    BackgroundFadeOutDelay: 0
    BackgroundFadeAlpha: 0.8
    BackgroundStartAlphaForAnimation: 0
    ShouldInheritUiScale: 1
    ShouldShowBackgroundOnClose: 0
    ShouldPlayCloseAnimation: 0
    ShouldNotDestroyOnClose: 0
    DestroyOnCloseDelay: 0
  continueButtons:
  - {{fileID: {buttons[0]}}}
  - {{fileID: {buttons[1]}}}
  rewardedButton: {{fileID: {buttons[2]}}}
  closeButton: {{fileID: {buttons[3]}}}
  buttonCanvases:
'''+''.join(f'  - {{fileID: {c}}}\n' for c in canvases)+'''  title: {fileID: 114894352506636257}
  defaultCoinText: {fileID: 114036828218548272}
  rewardedCoinText: {fileID: 114941304181978548}
  bottomWithAd: {fileID: 1336590748365959}
  bottomWithoutAd: {fileID: 1587765826080250}
  centerWithAd: {fileID: 1743393880019028}
  centerWithoutAd: {fileID: 1808113985169905}
  bottomPanel: {fileID: 1704330047573733}
  star: {fileID: 4231556087528228}
  pillowAnimator: {fileID: 95041320612064254}
  pillowState: LevelWinPillowAnimation
  starStartPosition: {x: -0.05000000074505806, y: 0, z: 0}
  starStartScale: {x: 5, y: 5, z: 0}
  starStartAngle: 120.00000333930423
  starMoveDuration: 0.25
  pillowCue: 0.10000000149011612
  textCue: 0.20000000298023224
  starSoundOffset: 0.03999999910593033
  starBounce:
  - {scale: 0.699999988079071, duration: 0.01549999974668026}
  - {scale: 1.149999976158142, duration: 0.10000000149011612}
  - {scale: 1, duration: 0.17000000178813934}
  wellDoneText: {fileID: 1722951815217645}
''')
# Derive star Transform from the original SpriteRenderer's GameObject.
star_go=re.search(r'm_GameObject: \{fileID: (\d+)',blocks['212012617856089587'])[1]
star_transform=re.search(r'component: \{fileID: (\d+)',blocks[star_go])[1]
blocks['114900100']=blocks['114900100'].replace('star: {fileID: 4231556087528228}',f'star: {{fileID: {star_transform}}}')
TARGET.write_text(parts[0]+''.join(blocks.values()),encoding='utf-8')
TARGET.with_suffix('.prefab.meta').write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nPrefabImporter:\n  externalObjects: {}\n',encoding='utf-8')
print(json.dumps({'prefab':str(TARGET),'source_sha256':hashlib.sha256(SOURCE.read_bytes()).hexdigest(),'blocks':len(blocks),'colliders_replaced':len(colliders),'standard_buttons':len(buttons),'star_transform':star_transform},indent=2))
