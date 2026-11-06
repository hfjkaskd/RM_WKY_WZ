"""Recover cell-mask render states and GPU programs from actual serialized shaders."""
import sys,json,re,hashlib
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(PROJECT.parent/'tools/python'))
import UnityPy
from UnityPy.helpers import CompressionHelper
source=PROJECT.parent.parent/'RoyalMatch_20260929/apk_unpacked/base/assets/bin/Data/data.unity3d'
evidence=[]
for o in UnityPy.load(str(source)).objects:
 if o.type.name!='Shader' or o.assets_file.name!='globalgamemanagers.assets' or o.path_id not in(21,25):continue
 t=o.read_typetree();form=t['m_ParsedForm'];sub=form['m_SubShaders'][0];state=sub['m_Passes'][0]['m_State'];write=o.path_id==21
 assert len(form['m_SubShaders'])==1 and len(sub['m_Passes'])==1
 expected={'zTest':4,'zWrite':0,'culling':2,'zClip':1,'stencilReadMask':255,'stencilWriteMask':255}
 for k,v in expected.items():assert state[k]['val']==v,(k,state[k])
 assert {k:state['rtBlend0'][k]['val']for k in('srcBlend','destBlend','srcBlendAlpha','destBlendAlpha','blendOp','blendOpAlpha','colMask')}==dict(srcBlend=0,destBlend=1,srcBlendAlpha=0,destBlendAlpha=1,blendOp=0,blendOpAlpha=0,colMask=15)
 assert {k:v['val']for k,v in state['stencilOp'].items()}==dict(comp=8,**{'pass':3 if write else 4,'fail':0,'zFail':0})
 assert state['stencilRef']['name']=='_StencilRef'
 shader=PROJECT/'Assets/Foundation/Shaders'/('StencilMaskWrite.shader'if write else'StencilMaskRead.shader');code=shader.read_text()
 for token in('Blend Zero One','ColorMask RGBA','ZWrite Off','ZTest LEqual','ZClip On','Cull Back','Ref [_StencilRef]','ReadMask 255','WriteMask 255','Comp Always','Pass '+('IncrSat'if write else'DecrSat'),'Fail Keep','ZFail Keep','UnityObjectToClipPos(position.xyz)','fixed4(0,0,0,0)'):assert token in code,token
 assert 'DummyShader'not in code
 programs=[];blob=bytes(t['compressedBlob'])
 for i,platform in enumerate(t['platforms']):
  for j,offset in enumerate(t['offsets'][i]):
   raw=CompressionHelper.decompress_lz4(blob[offset:offset+t['compressedLengths'][i][j]],t['decompressedLengths'][i][j]);text=raw.decode('ascii',errors='ignore')
   assert 'SV_TARGET0 = vec4(0.0, 0.0, 0.0, 0.0);'in text and 'gl_Position = hlslcc_mtx4x4unity_MatrixVP[3] * u_xlat0.wwww + u_xlat1;'in text
   programs.append({'platform':platform,'decompressed_sha256':hashlib.sha256(raw).hexdigest(),'vertex':'unity_MatrixVP * unity_ObjectToWorld * float4(position.xyz,1)','fragment':'constant zero RGBA; no texture sampling or alpha clipping'})
 mat=PROJECT/'Assets/Resources/Recovered/Material'/('StencilWriteMat.mat'if write else'StencilReadMat.mat')
 guid=re.search(r'guid: ([0-9a-f]+)',Path(str(shader)+'.meta').read_text())[1];assert 'guid: '+guid in mat.read_text()
 evidence.append({'path_id':o.path_id,'asset_file':o.assets_file.name,'shader':form['m_Name'],'shader_meta_guid':guid,'original_subshader_tags':sub['m_Tags'],'original_state':{k:state[k]for k in(*expected,'rtBlend0','stencilOp','stencilRef')},'programs':programs})
assert len(evidence)==2
report={'passed':True,'source':'RoyalMatch_20260929/apk_unpacked/base/assets/bin/Data/data.unity3d','source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'shaders':evidence,'scope':'Actual serialized shader pass state and decompressed GLES2/GLES3 programs, with matching restored ShaderLab/HLSL and original material GUID links. Runtime GPU counter/color checks are in cell_view_unity_verification.json. General material migration, all other stencil shaders and Android/iOS device rendering remain unverified.'}
(PROJECT/'cell_stencil_source_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'passed':True,'shader_count':len(evidence),'names':[e['shader']for e in evidence]}))
