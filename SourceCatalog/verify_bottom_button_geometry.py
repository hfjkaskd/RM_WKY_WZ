"""Verify visible Button backplate geometry against original Sprite vertices.

Uses the installed official UGUI Image.GenerateSprite equation, source packed
mesh, pivots and flip flags. This is local geometry proof, not whole-frame parity.
"""
from pathlib import Path
import hashlib,json,re,struct
ROOT=Path(__file__).resolve().parents[1]
def blocks(text):return {int(m[2]):(int(m[1]),m[0])for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)\n[\s\S]*?(?=^--- !u!|\Z)',text,re.M)}
original=blocks((ROOT.parent/'assets/RecoveredProject/ExportedProject/Assets/Royal/Scenes/Game/GameScene.unity').read_text(encoding='utf8'))
authored=blocks((ROOT/'Assets/Resources/Foundation/Prefabs/OriginalBottomUi.prefab').read_text(encoding='utf8'))
proof=json.loads((ROOT/'SourceCatalog/bottom_ui_authoring.json').read_text())
asset=ROOT/'Assets/Resources/Recovered/Sprite/Game-booster_button.asset';text=asset.read_text()
vertices=int(re.search(r'm_VertexCount: (\d+)',text)[1]);data=bytes.fromhex(re.search(r'_typelessdata: ([0-9a-f]+)',text)[1])
source=[struct.unpack_from('<3f',data,i*12)for i in range(vertices)]
bounds=[max(v[c]for v in source)-min(v[c]for v in source)for c in range(2)]
pivot=tuple(float(v)for v in re.search(r'm_Pivot: \{x: ([^,]+), y: ([^}]+)',text).groups())
f=lambda x:struct.unpack('<f',struct.pack('<f',x))[0]
maximum=0;comparisons=0
for adapter in proof['button_adapters']:
    for image in adapter['visible_backplate_images']:
        b=authored[image][1];assert '  m_UseSpriteMesh: 1'in b and '  m_RaycastTarget: 1'in b and 'm_Color: {r: 1, g: 1, b: 1, a: 1}'in b
        go=int(re.search(r'm_GameObject: \{fileID: (\d+)\}',b)[1]);source_go=go%10**12
        sprite=next(t for k,t in original.values()if k==212 and f'm_GameObject: {{fileID: {source_go}}}'in t)
        t=next(t for k,t in authored.values()if k==224 and f'm_GameObject: {{fileID: {go}}}'in t)
        size=tuple(f(float(v))for v in re.search(r'm_SizeDelta: \{x: ([^,]+), y: ([^}]+)',t).groups())
        actual_pivot=tuple(float(v)for v in re.search(r'm_Pivot: \{x: ([^,]+), y: ([^}]+)',t).groups());assert actual_pivot==pivot
        scale=tuple(float(v)for v in re.search(r'm_LocalScale: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)',t).groups())
        flip=-1 if 'm_FlipX: 1'in sprite else 1;assert scale[0]==flip
        for vertex in source:
            for c in range(2):
                actual=f(f(vertex[c]/bounds[c])*size[c])*scale[c];expected=vertex[c]*(flip if c==0 else 1)
                error=abs(actual-expected);maximum=max(error,maximum);comparisons+=1;assert error<=0.000001,(image,c,error)
report={'passed':True,'backplates':8,'scalar_comparisons':comparisons,'max_local_vertex_error':maximum,
    'native_sprite_bounds':bounds,'sprite_mesh_sha256':hashlib.sha256(asset.read_bytes()).hexdigest(),
    'official_ugui_equation_source':'Library/PackageCache/com.unity.ugui@1.0.0/Runtime/UI/Core/Image.cs:GenerateSprite',
    'whole_original_rendered_parity_verified':False,'physical_input_verified':False}
(ROOT/'bottom_button_geometry_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2))
