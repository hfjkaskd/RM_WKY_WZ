"""Run original ParsePredefined orchestration against restored typed C#."""
import sys
sys.dont_write_bytecode=True
import json,os,random,subprocess,tempfile
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_move_budget.py').read_text().split('cases=[];expected=[]')[0],str(PROJECT/'SourceCatalog/verify_move_budget.py'),'exec'))
emu.hook_del(native_hook);base_hook=hook
owner,provider,provider_class,manager,creator,set_array,first_set,rng_ptr,method_slots=[0x10040000+i*0x400 for i in range(9)]
methods={6:0x2b45d70,7:0x2b45d78,8:0x2b45c94,9:0x2b45b90}
put(provider,provider_class,8);put(provider_class+0x12a,0,2)
for n,m in methods.items():put(method_slots+n*0x10,m,8)
head=[];entries=[];calls=created=0
def s(v):return signed(v&0xffffffff)
def record(t):
 global calls
 trace.append(t);calls+=1
 if calls==head[3]:fail(emu);return False
 return True
def hook(uc,address,size,unused):
 global created
 x0,x1,x2,x3,x4=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(5)]
 if address==0x260dea4:
  assert x0==0
  if record('random'):returned(uc,0 if head[2]&8 else rng_ptr)
 elif address==0xfcd934:assert x2 in methods;returned(uc,method_slots+x2*0x10)
 elif address==methods[6]:
  if record('count'):returned(uc,0 if head[2]&128 and created else len(entries))
 elif address==methods[9]:
  if record('column,'+str(s(x1))):returned(uc,entries[x1][0])
 elif address==methods[7]:
  if record('length,'+str(s(x1))):returned(uc,0 if head[2]&128 and created else len(entries[x1])-1)
 elif address==methods[8]:
  if record('id,'+str(s(x1))+','+str(s(x2))):returned(uc,entries[x1][x2+1])
 elif address==0x1c24f70:
  assert x2==first_set
  if record('select,'+str(s(x1))+','+str(int(x3!=0))):returned(uc,head[4])
 elif address==0x23ebf0c:
  assert x4==0
  if record('create,'+str(s(x1))+','+str(s(x2))+','+str(int(x3!=0))):
   created+=1
   if head[2]&32:put(manager+0x38,0,8)
   returned(uc,0 if head[2]&64 else 0x10050000+created*0x100)
 elif address==0x23eb8c4:
  if record('add,'+str(s(x1))+','+str((x2-0x10050000)//0x100 if x2 else 0)):returned(uc)
 else:base_hook(uc,address,size,unused)
emu.hook_add(UC_HOOK_CODE,hook)
cases=[];expected=[]
def run(h,values):
 global head,entries,calls,created,next_object
 head=h;entries=values;calls=created=0;next_object=0x10060000;trace.clear();emu.mem_write(owner,bytes(0x200));put(owner+0x18,manager,8);put(manager+0x38,0 if h[2]&16 else creator,8);put(owner+0x40,0 if h[2]&2 else set_array,8);put(set_array+0x18,0 if h[2]&4 else 1);put(set_array+0x20,first_set,8);put(owner+0x50,h[0]);put(owner+0x54,h[1]);call(0x1c24788,owner,0 if h[2]&1 else provider)
 cases.append(','.join(map(str,h))+'#'+'/'.join(','.join(map(str,v))for v in values));expected.append(('error'if fault else'ok')+':'+str(created)+':'+'/'.join(trace))
rng=random.Random(902)
for i in range(220):
 values=[[rng.randrange(-1,10),*[rng.choice((-1,0,1,2,4,6,7,211,212,213,214,320))for _ in range(rng.randrange(5))]]for j in range(i%5)]
 run([rng.choice((0,1,4,7)),rng.choice((0,1,4,7)),i%256,0,rng.choice((0,2,6,211))],values)
for f in range(1,45):run([4,1,0,f,3],[[2,1,211,212,213],[0,3,4]])
level=json.loads((PROJECT/'Assets/Resources/Levels/level_1.json').read_text());run([0,0,0,0,0],[[v.get('X',0),*v.get('Ids',[])]for v in level['Predefined']])
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-predefined-')as folder:
 exe=Path(folder)/'verify.exe';runtime=PROJECT/'Assets/Foundation/Runtime';p=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-langversion:latest','-out:'+str(exe),str(PROJECT/'SourceCatalog/PredefinedPreparationHarness.cs'),*[str(runtime/n)for n in('OriginalPredefinedPreparation.cs','OriginalLevelRules.cs','OriginalConfigData.cs')]],capture_output=True,text=True,encoding='utf-8-sig');assert p.returncode==0,p.stdout+p.stderr
 p=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8-sig');assert p.returncode==0,p.stderr
 actual=[x.replace('\ufeff','')for x in p.stdout.splitlines()];assert len(actual)==len(expected)
 for i,(a,e)in enumerate(zip(actual,expected)):assert a==e,(i,cases[i],a,e)
report={'passed':True,'native_cases':len(cases),'first_level_created':created,'scope':'Actual ARM64 ParsePredefined including original color conversion, repeated provider counts/column/Ids order, first-set211..213 placeholder selection, early random lookup, null/empty sets/providers/creators, null item results, creator changes, mutable provider counts and injected service failures. Exact trace and creation state. Data accessor dispatch, placeholder selection and concrete model/container calls supplied boundaries; first level all90 configured entries covered. Application/session/model/fill composition remains pending.'}
(PROJECT/'predefined_preparation_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
