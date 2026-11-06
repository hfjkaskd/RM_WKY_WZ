"""Extract the original mask GLES programs/render state, without dummy exporters."""
from pathlib import Path
import hashlib,json,sys
PROJECT=Path(__file__).resolve().parents[1];RECOVERY=PROJECT.parent
sys.path.insert(0,str(RECOVERY/'tools/python'))
import UnityPy
from UnityPy.export.ShaderConverter import ShaderProgram
from UnityPy.helpers import CompressionHelper
from UnityPy.streams import EndianBinaryReader
source=RECOVERY.parent/'RoyalMatch_20260929/apk_unpacked/base/assets/bin/Data/data.unity3d'
objects=[o for o in UnityPy.load(str(source)).objects if o.type.name=='Shader'and o.path_id==26]
assert len(objects)==1
obj=objects[0];data=obj.read();assert data.m_ParsedForm.m_Name=='Alpha Masked/Sprites Alpha Masked - World Coords'
files=[]
for platform,version in((5,'100'),(9,'300')):
    i=data.platforms.index(platform);offset=data.offsets[i][0];length=data.compressedLengths[i][0];size=data.decompressedLengths[i][0]
    code=CompressionHelper.decompress_lz4(bytes(data.compressedBlob)[offset:offset+length],size)
    parsed=ShaderProgram(EndianBinaryReader(code,endian='<'),obj.version)
    text=bytes(parsed.m_SubPrograms[1].m_ProgramCode).decode('utf-8')
    assert text.startswith('#ifdef VERTEX\n#version '+version)and'#ifdef FRAGMENT'in text and'_WorldToMask'in text
    path='SourceCatalog/original_ball_ball_mask_gles'+version+'.glsl';(PROJECT/path).write_text(text,encoding='utf-8',newline='\n');files.append(path)
state=obj.read_typetree()['m_ParsedForm']['m_SubShaders'][0]['m_Passes'][0]['m_State']
assert(state['rtBlend0']['srcBlend']['val'],state['rtBlend0']['destBlend']['val'],state['zWrite']['val'],state['culling']['val'])==(1,10,0,0)
path='SourceCatalog/original_ball_ball_mask_render_state.json';(PROJECT/path).write_text(json.dumps(state,indent=2)+'\n',encoding='utf-8');files.append(path)
files+=['Assets/Foundation/Shaders/OriginalWorldSpaceAlphaMask.shader','Assets/Resources/Recovered/Material/TutorialBlackMasked.mat']
report={'original_shader_name':data.m_ParsedForm.m_Name,'original_path_id':26,'original_bundle_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),
 'extraction':'UnityPy decompression of the original serialized GPU subprogram tables, platform5/program1 and platform9/program1. The generic shader text exporter omits these player subprograms; dummy ShaderLab exports are not used as code evidence.',
 'restoration':'Translate the original GLES vertex/fragment equations to Unity HLSL: world/object-to-mask coordinates, optional pixel snapping, UV scale/offset, channel/clamp controls, text/tint handling and premultiplied output. Restore serialized blend One/OneMinusSrcAlpha, color/stencil property bindings, ZTest LEqual, ZWrite Off and Cull Off. Bind the original black material used by BallBall prefab.',
 'limitations':'Source equations and render-state recovery plus formal-scene GPU alpha observations do not prove complete original-render or cross-platform GPU equivalence; physical Android/iOS comparison remains pending.',
 'files':[{'path':p,'sha256':hashlib.sha256((PROJECT/p).read_bytes()).hexdigest()}for p in files]}
(PROJECT/'SourceCatalog/ball_ball_mask_shader_source_evidence.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
