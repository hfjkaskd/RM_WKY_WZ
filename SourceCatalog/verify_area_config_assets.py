"""Compare original serialized area data with files and actual Unity-loaded fields."""
from pathlib import Path
import re,json,hashlib,struct
PROJECT=Path(__file__).resolve().parents[1];RAW=PROJECT.parent/'assets/RecoveredProject/ExportedProject/Assets/Resources'
recovered=json.loads((PROJECT/'SourceCatalog/area_config_recovery_verification.json').read_text(encoding='utf-8'))
live=json.loads((PROJECT/'live_area_data_verification.json').read_text(encoding='utf-8'))
assert live['passed']and live['configs_loaded']==167 and live['new_textures_from_config_load']==0
assert len(live['config_data'])==167
def field(source,name,indent=2):
 m=re.search('^'+' '*indent+re.escape(name)+r':(?: (.*))?$',source,re.M);assert m,name
 return m.group(1)or''
def number(source,name,indent=2):return int(field(source,name,indent))
def float_equal(a,b):return struct.pack('<f',float(a))==struct.pack('<f',float(b))
count=0;task_count=0;paths=[]
for asset in recovered['assets']:
 path=PROJECT/asset['path'];source=RAW/path.name;before=source.read_text(encoding='utf-8-sig');current=path.read_text(encoding='utf-8-sig')
 assert hashlib.sha256(source.read_bytes()).hexdigest()==asset['original_sha256']
 assert hashlib.sha256(path.read_bytes()).hexdigest()==asset['sha256']
 old_script=re.search(r'm_Script: .*',before).group();restored=re.sub(r'm_Script: .*',old_script,current)
 for name,reference in asset['textures'].items():
  assert field(current,name+'Path')==reference['resource_path']
  restored=re.sub('^  '+name+'Path:.*$','  '+name+': '+reference['original'],restored,flags=re.M)
 assert restored==before,path
 identity=number(before,'areaId');actual=json.loads(live['config_data'][identity]);assert actual['areaId']==identity
 assert actual['areaName']==field(before,'areaName')
 for name in('tallDeviceReplayDelay','positionOffset'):assert float_equal(actual[name],field(before,name)),(path,name)
 for name in('useGameBackground','useGameBackgroundAlways'):assert actual[name]==bool(number(before,name))
 assert actual['crcNumbers']=={name:number(before,name,4)for name in('ios','android')}
 for name,reference in asset['textures'].items():assert actual[name+'Path']==reference['resource_path']
 assert all(name not in actual for name in('mainTexture','shadowTexture','distortTexture'))
 pieces=re.findall(r'^  - id:.*?(?=^  - id:|^  [a-zA-Z]|\Z)',before,re.M|re.S)
 assert len(pieces)==len(actual['tasks'])==asset['task_count']==live['task_counts'][identity]
 for piece,task in zip(pieces,actual['tasks']):
  assert task['id']==int(re.search(r'^  - id: (.*)',piece,re.M).group(1))
  for name in('price','dependency'):assert task[name]==number(piece,name,4),(path,name)
  data=bytes.fromhex(field(piece,'dependencies',4));expected=list(struct.unpack('<'+'i'*(len(data)//4),data));assert(task['dependencies']or[])==expected
  assert float_equal(task['positionOffset'],field(piece,'positionOffset',4))
  replay=task['replayConfig'];assert replay['order']==number(piece,'order',6)
  assert float_equal(replay['blending'],field(piece,'blending',6))
  for name in('hasAudio','disableHaptic'):assert replay[name]==bool(number(piece,name,6))
  task_count+=1
 count+=1;paths.append({'path':asset['path'],'sha256':asset['sha256']})
assert count==167
report={'passed':True,'configs':count,'tasks':task_count,'runtime_texture_load_delta':0,'files':paths,'scope':'Every original task id/price/dependency/dependency-array/replay-order/blending/audio/haptic/position and every area name/id/delay/background flag/CRC is compared against the actual Unity-loaded ScriptableObject JSON. Float fields compare as IEEE754 float32 bits. Original YAML is recovered exactly after reversing only script and three heavy-texture path substitutions. This proves data fidelity, not original area rendering/download/published-limit parity.'}
(PROJECT/'SourceCatalog/area_config_runtime_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps({k:v for k,v in report.items()if k!='files'},indent=2))
