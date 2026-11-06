"""Actual ARM64 item settings/start selection/core construction comparison."""
import sys
sys.dont_write_bytecode=True
import json,os,random,subprocess,tempfile
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_move_budget.py').read_text().split('cases=[];expected=[]')[0],str(PROJECT/'SourceCatalog/verify_move_budget.py'),'exec'))
emu.hook_del(native_hook);base_hook=hook
owner,cell,sets_array,rng_ptr,potions,drill_list,result_item,settings_buffer,item_class,base_class,id_static,service=[0x10040000+i*0x400 for i in range(12)]
put(item_class+0x378,0x2b45d70,8);put(item_class+0x3f8,0x2b46230,8)
put(base_class+0xb8,id_static,8);put(base_class+0xe0,1);put(base_class+0x132,1,1)
set_ptrs=[0x10050000+i*0x800 for i in range(3)]
head=[];calls=0;mode='s';core_type=0;settings_seen=''
def s(v):return signed(v&0xffffffff)
def record(t):
 global calls
 trace.append(t);calls+=1
 if calls==head[6]:fail(emu);return False
 return True
def snapshot(p):
 values=[s(read(p+o))for o in(0,4,8,12,16)];values+=[int.from_bytes(emu.mem_read(p+20,2),'little',signed=True),s(read(p+24))]
 values += [read(p+28,1),read(p+29,1),int(read(p+32,8)!=0),int(read(p+40,8)!=0),s(read(p+48)),read(p+52,1),read(p+53,1),s(read(p+56)),read(p+60,1),s(read(p+64))]
 return ','.join(map(str,values))
def hook(uc,address,size,unused):
 global settings_seen
 x0,x1,x2,x3=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(4)]
 if address==0xffd960 and x0==read(0x53ca2e8,8):put(x0,base_class,8);returned(uc)
 elif address==0xffda78:
  p=allocate(0x400);put(p,item_class,8);put(p+0x300,core_type);returned(uc,p)
 elif address in(0xd00b50,0xd018a0):emu.mem_write(x0,bytes(emu.mem_read(x1,x2)));returned(uc,x0)
 elif address==0xd01fa0:emu.mem_write(x0,bytes([x1&255])*x2);returned(uc,x0)
 elif address in(0x231007c,0x2eec0d0,0x318cf78,0x318cdf8,0x2ee8850):returned(uc)
 elif address==0x23e5778:emu.mem_write(x0,bytes(16));returned(uc)
 elif address in(0x260dea4,0x29e27cc):returned(uc,service)
 elif address==0x222dcc8:returned(uc,0)
 elif address==0x2b45d70:returned(uc,read(x0+0x300))
 elif address==0x1ed98c4:
  a,b=s(x1),s(x2)
  if record(f'random,{a},{b}'):
   if b<a:fail(uc)
   else:returned(uc,max(a,b-1)if head[7]else a)
 elif address==0x2c1b9a0:
  i=s(x1)
  if i<0 or i>=read(x0+0x18):fail(uc)
  else:returned(uc,read(read(x0+0x10,8)+0x20+8*i,8))
 elif address==0x1f16fa8:
  if record('drill'):returned(uc,head[4])
 elif address==0x23ec1e8 and mode=='i':
  settings_seen=snapshot(x0)
  if record('create,'+settings_seen):returned(uc,0 if head[5]&4 else result_item)
 elif address==0x23ef5b0:
  if record('new,'+str(s(x1))):returned(uc)
 else:base_hook(uc,address,size,unused)
# Intercept only supplied runtime/service boundaries; execute game instructions directly.
for boundary in (0xffd960,0xffda78,0xffd9d0,0xffda64,0xffd90c,0xffda6c,0xffda68,0x261458c,0xffda88,0xffda90,0xffda50,0x2a2b5fc,0xd00b50,0xd018a0,0xd01fa0,0x231007c,0x2eec0d0,0x318cf78,0x318cdf8,0x2ee8850,0x23e5778,0x260dea4,0x29e27cc,0x222dcc8,0x2b45d70,0x1ed98c4,0x2c1b9a0,0x1f16fa8,0x23ec1e8,0x23ef5b0):
 emu.hook_add(UC_HOOK_CODE,hook,begin=boundary,end=boundary)
cases=[];expected=[]
def run_settings(t,save):
 global mode,next_object
 mode='s';next_object=0x10060000;trace.clear();emu.reg_write(UC_ARM64_REG_X8,settings_buffer);call(0x23ec140,t,save)
 assert not fault,(t,hex(emu.reg_read(UC_ARM64_REG_LR)))
 cases.append('s#'+str(t)+','+str(save)+'#');expected.append(snapshot(settings_buffer)+':')
def run_initial(h,rows,kind='i'):
 global head,calls,mode,next_object
 head=h;calls=0;mode=kind;next_object=0x10060000;trace.clear();emu.mem_write(owner,bytes(0x200));put(owner+0x20,0 if h[5]&1 else sets_array,8);put(sets_array+0x18,len(rows));put(owner+0xf0,0 if h[5]&2 else rng_ptr,8);put(owner+0x30,potions if h[3]else 0,8);put(owner+0x38,0 if h[5]&8 else drill_list,8);put(result_item,item_class,8);put(result_item+0x300,h[8])
 for i,values in enumerate(rows):
  p=set_ptrs[i];put(sets_array+0x20+8*i,0 if values is None else p,8)
  if values is None:continue
  lst=p+0x100;store=p+0x200;put(p+0x18,lst,8);put(lst+0x10,store,8);put(lst+0x18,len(values))
  for j,v in enumerate(values):e=p+0x300+j*0x30;put(store+0x20+j*8,e,8);put(e+0x10,v)
 value=call(0x23ec07c if kind=='r'else 0x23ebf0c,owner,h[0],h[1],cell if h[2]else 0)
 result='error'if fault else'value,'+str(value if kind=='r'else 91)
 cases.append(kind+'#'+','.join(map(str,h))+'#'+'/'.join('~'if r is None else ','.join(map(str,r))for r in rows));expected.append(result+':'+'/'.join(trace))
def run_core(h):
 global mode,next_object,core_type
 mode='c';core_type=h[2];next_object=0x10060000;trace.clear();put(id_static,h[0]);emu.mem_write(settings_buffer,bytes(72))
 for o,v in zip((0,4,8,12,16),(h[1],h[2],h[3],h[4],h[5])):put(settings_buffer+o,v)
 put(settings_buffer+60,h[6],1);value=call(0x23ec1e8,settings_buffer);assert not fault
 v=[s(read(value+16)),s(read(id_static)),s(read(value+28)),h[2],s(read(value+104)),s(read(value+32)),read(value+0x27a,1)if h[2]==1 else 0,int(read(value+64,8)!=0)]
 cases.append('c#'+','.join(map(str,h))+'#');expected.append(','.join(map(str,v))+':')
for t in range(65536):run_settings(t,t%65536-32768)
for t in(-2147483648,-65537,-1,65536,65537,2147483647):run_settings(t,32767)
print('Native settings complete',flush=True)
rng=random.Random(664)
for i in range(240):
 rows=[rng.choice((None,[],[1,2,3],[25,21,24],[6,4]))for _ in range(3)];h=[rng.choice((-1,1,6,14,15,16,17,21,25,211)),rng.choice((0,1,14,15,16,17)),i%2,(i//2)%2,(i//4)%2,i%16,0,i%2,rng.choice((1,6,7))];run_initial(h,rows)
for f in range(1,7):run_initial([14,14,1,1,1,0,f,1,7],[[1,25,24],[3],[2]])
for t in(-1,13,14,15,16,17,2147483647):run_initial([t,0,0,0,0,0,0,0,1],[[2,5],[4],[]],'r')
for counter in(-2147483648,-1,0,1,2147483647):
 for typ in(1,6):
  for tiled in(1,6,21,24):
   for hand in(0,1):run_core([counter,tiled,typ,4,3,99,hand])
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-initial-creation-')as folder:
 exe=Path(folder)/'verify.exe';runtime=PROJECT/'Assets/Foundation/Runtime';names=('OriginalItemSettings.cs','OriginalTiledItemLayers.cs','OriginalTiledItemTypes.cs','OriginalLevelRules.cs','OriginalGoalTypes.cs','OriginalInitialItemCreation.cs','OriginalItemModelConstruction.cs')
 p=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-langversion:latest','-out:'+str(exe),str(PROJECT/'SourceCatalog/InitialItemCreationHarness.cs'),*[str(runtime/n)for n in names]],capture_output=True,text=True,encoding='utf-8-sig');assert p.returncode==0,p.stdout+p.stderr
 p=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8-sig');assert p.returncode==0,p.stderr
 actual=[x.replace('\ufeff','')for x in p.stdout.splitlines()];assert len(actual)==len(expected),(len(actual),len(expected))
 for i,(a,e)in enumerate(zip(actual,expected)):assert a==e,(i,cases[i],a,e)
report={'passed':True,'native_settings_cases':65542,'native_start_cases':253,'native_core_construction_cases':80,'scope':'Actual ARM64 GetSettingsForTileId/ItemSettings ctor and all ushort tiled IDs plus signed edges, layer/type/match/goal/save/default fields. Actual start/set14..16 selection, raw unweighted element access, cell/potion/drill/hand-given flags, model then count order and failures. Actual factory match/box branches with native constructors/base identity and standard fall creation, including counter overflow. CLR allocation, model services/containers, native runtime memory operations and type virtual getter supplied boundaries; start-mode concrete factory/count callbacks supplied. Other model factory branches, application reset/session ownership and automatic refill remain pending.'}
(PROJECT/'initial_item_creation_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
