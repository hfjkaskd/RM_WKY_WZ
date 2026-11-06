"""Offline prefab rebinding only. Source visual hierarchy and particle settings retained."""
from pathlib import Path
import re,uuid,json,subprocess,shutil,hashlib
PROJECT=Path(__file__).resolve().parents[1];RECOVERY=PROJECT.parent
ORIGINAL=RECOVERY/'assets/RecoveredProject/ExportedProject/Assets'
def meta(path,kind='DefaultImporter'):
    p=Path(str(path)+'.meta')
    if not p.exists():p.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n'+kind+':\n  externalObjects: {}\n',encoding='utf8')
    return re.search(r'guid: (\w+)',p.read_text(encoding='utf8'))[1]
def run():
    runtime=PROJECT/'Assets/Foundation/Runtime'
    for name in ['OriginalWinFailManager','OriginalBakedMeshAnimation','OriginalWinLogoVisual']:meta(runtime/(name+'.cs'),'MonoImporter')
    shader=PROJECT/'Assets/Foundation/Shaders/OriginalPmaMesh.shader';meta(shader.parent);shader_guid=meta(shader,'ShaderImporter')
    material=PROJECT/'Assets/Foundation/Settings/OriginalWinLogoMaterial.mat';material_guid=meta(material,'NativeFormatImporter')
    material.write_text('''%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: OriginalWinLogoMaterial
  m_Shader: {fileID: 4800000, guid: SHADER, type: 3}
  m_ValidKeywords: []
  m_InvalidKeywords: []
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 0
  m_DoubleSidedGI: 0
  m_CustomRenderQueue: -1
  stringTagMap: {}
  disabledShaderPasses: []
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs:
    - _MainTex:
        m_Texture: {fileID: 0}
        m_Scale: {x: 1, y: 1}
        m_Offset: {x: 0, y: 0}
    m_Ints: []
    m_Floats: []
    m_Colors: []
  m_BuildTextureStacks: []
'''.replace('SHADER',shader_guid),encoding='utf8')
    bake=RECOVERY/'tools/SpineOfflineBaker';data=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data');mono=data/'MonoBleedingEdge'
    # All third-party source/assembly compilation stays outside the Unity project.
    command=[str(data/'NetCoreRuntime/dotnet.exe'),str(data/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',*[f'-r:{mono/"lib/mono/4.5"/x}'for x in ['mscorlib.dll','System.dll','System.Core.dll']],'-out:'+str(bake/'BakeLogo.exe'),str(PROJECT/'SourceCatalog/BakeLogo.cs'),*[str(p)for p in (bake/'src').rglob('*.cs')]]
    subprocess.run(command,check=True)
    output=PROJECT/'Assets/Resources/Foundation/Animation';output.mkdir(exist_ok=True);meta(output)
    target=output/'OriginalWinLogo.bytes'
    subprocess.run([str(mono/'bin/mono.exe'),str(bake/'BakeLogo.exe'),str(ORIGINAL/'TextAsset/win_logo_animation.atlas.txt'),str(ORIGINAL/'TextAsset/win_logo_animation.skel.bytes'),str(target)],check=True);meta(target,'TextScriptImporter')
    source=ORIGINAL/'GameObject/WinLogoAnimationDialog.prefab';s=source.read_text(encoding='utf8')
    blocks={int(m[2]):(int(m[1]),m[0])for m in re.finditer(r'^--- !u!(\d+) &(\d+)\n[\s\S]*?(?=^--- !u!|\Z)',s,re.M)}
    # This asset is the visual-only dependency. Interactive dialog ownership is
    # bound separately; no original collider button or serialized event remains.
    excluded_go={1946936116752871,1711914853252429,1918264173551851,1587397909062814}
    kept={}
    for id,(cl,b)in blocks.items():
        go=re.search(r'm_GameObject: \{fileID: (\d+)\}',b)
        if (cl==1 and id in excluded_go)or(go and int(go[1])in excluded_go):continue
        if cl not in [1,4,33,23,198,199,210]:continue
        if id in [33089843903986396,23672306508523268]:continue
        kept[id]=(cl,b)
    visualid=114000001;bakedid=114000002
    def component(id,go,guid,fields):
        return (114,f'''--- !u!114 &{id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {guid}, type: 3}}
  m_Name:
  m_EditorClassIdentifier:
{fields}''')
    particles=[id for id,(cl,b)in kept.items()if cl==198]
    kept[visualid]=component(visualid,1709254077376921,meta(runtime/'OriginalWinLogoVisual.cs'),'  animationView: {fileID: '+str(bakedid)+'}\n  fireworks: {fileID: 4919942235722503}\n  fireworkSystems:\n'+''.join('  - {fileID: '+str(id)+'}\n'for id in particles)+'  particlesTime: 1\n  closeTime: 3\n  skipTime: 0.75\n')
    kept[bakedid]=component(bakedid,1618682377417806,meta(runtime/'OriginalBakedMeshAnimation.cs'),'  dataPath: Foundation/Animation/OriginalWinLogo\n  texturePath: Recovered/Resources/win_logo_animation\n  materialTemplate: {fileID: 2100000, guid: '+material_guid+', type: 2}\n  parts:\n  - {fileID: 33472185465350827}\n  - {fileID: 33967328832014496}\n')
    out=[]
    for id,(cl,b)in kept.items():
        if cl==1:
            b=re.sub(r'^  - component: \{fileID: (\d+)\}\n',lambda m:m[0]if int(m[1])in kept else'',b,flags=re.M)
            extra=visualid if id==1709254077376921 else bakedid if id==1618682377417806 else None
            if extra:b=b.replace('  m_Layer:',f'  - component: {{fileID: {extra}}}\n  m_Layer:',1)
            b=b.replace('m_TagString: Touchable','m_TagString: Untagged')
        if cl==4:b=re.sub(r'^  - \{fileID: (\d+)\}\n',lambda m:m[0]if int(m[1])in kept else'',b,flags=re.M)
        if cl==23:
            if id in [23518489005915545,23291844617409189]:
                b=re.sub(r'm_SortingOrder: -?\d+', 'm_SortingOrder: '+str(999 if id==23518489005915545 else 1000),b)
                b=re.sub(r'm_SortingLayerID: -?\d+','m_SortingLayerID: 2046106761',b)
        if cl==210:b=re.sub(r'm_SortingLayerID: -?\d+','m_SortingLayerID: 2046106761',b)
        # Empty component-only child arrays must be valid YAML sequences.
        b=re.sub(r'  m_Children:\n(?=  m_Father:)', '  m_Children: []\n',b)
        out.append(b)
    destination=PROJECT/'Assets/Resources/Foundation/Prefabs/OriginalWinLogoVisual.prefab'
    destination.write_text('%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'+''.join(out),encoding='utf8');meta(destination,'PrefabImporter')
    manifest={'original_prefab_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'original_skeleton_sha256':hashlib.sha256((ORIGINAL/'TextAsset/win_logo_animation.skel.bytes').read_bytes()).hexdigest(),'sample_rate_hz':120,'baked_bytes':target.stat().st_size,'parts':2,'clipping':True,'source_animation_version':'3.8.84','scope':'Visual dependency only. Original skeleton sample interpolation, not an exact runtime skeleton implementation; full orientation/skip/dialog/flow parity pending.','external_runtime_assembly_shipped':False}
    (PROJECT/'SourceCatalog/win_logo_visual_evidence.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf8');print(json.dumps(manifest))
if __name__=='__main__':run()
