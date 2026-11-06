"""Compare original native container registration, binding and frame dispatch."""
import sys
sys.dont_write_bytecode=True
import json,os,random,subprocess,tempfile
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_move_budget.py').read_text().split('cases=[];expected=[]')[0],str(PROJECT/'SourceCatalog/verify_move_budget.py'),'exec'))
emu.hook_del(native_hook);base_hook=hook
owner,lst,slots,backing=[0x10020000+i*0x1000 for i in range(4)]
unit_interface,behaviour_interface,late_interface=0x10026000,0x10026400,0x10026800
method_slot=0x10028000;id_call,bind_call,update_call,factory_call,remove_call=[0x10029000+i*0x100 for i in range(5)]
generic,return_type=0x1002e000,0x1002e100
for got,klass in [(0x5380af8,unit_interface),(0x53df6b0,behaviour_interface),(0x5392628,late_interface)]:
 put(read(got,8),klass,8);put(klass+0xe0,1);put(klass+0x132,1,1)
put(generic+0x38,return_type,8);put(return_type,unit_interface,8)
put(unit_interface+8,factory_call,8)
rgctx,arguments,predicate_class,predicate_static,predicate=0x1002f000,0x1002f200,0x10030000,0x10030400,0x10030800
put(generic+0x20,rgctx,8);put(rgctx+0xc0,arguments,8);put(arguments+8,unit_interface,8)
put(arguments+0x28,predicate_class,8);put(predicate_class+0x132,1,1);put(predicate_class+0xe0,1)
put(predicate_class+0xb8,predicate_static,8);put(predicate_static+8,predicate,8)
for i,fn in enumerate((id_call,bind_call,update_call,remove_call)):put(method_slot+i*16,fn,8)
config=[];row=[];mutated=set();calls=0
def ref(tag):return 0 if tag<0 else 0x10040000+tag*0x400
def tag(pointer):return -1 if pointer==0 else (pointer-0x10040000)//0x400
def values():return [read(backing+0x20+i*8,8)for i in range(read(lst+0x18))]
def record(value):
 global calls
 trace.append(value);calls+=1
 if calls==row[5]:fail(emu);return False
 return True
def hook(uc,address,size,unused):
 x0,x1,x2=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(3)]
 if address==0xfcd934:
  kind=3 if x1==late_interface else 2 if x1==behaviour_interface else 0 if x2==0 else 1
  returned(uc,method_slot+kind*16)
 elif address==0xffda68:
  returned(uc,x0 if x0 and (x1==unit_interface or x1==behaviour_interface and config[tag(x0)][1]in(1,3,5)or x1==late_interface and config[tag(x0)][1]in(2,3)) else 0)
 elif address==id_call:
  i=tag(x0)
  if record('id,'+str(i)):returned(uc,config[i][0])
 elif address==factory_call:
  if record('create,'+str(row[4])):returned(uc,ref(row[4]))
 elif address in (bind_call,update_call,remove_call):
  i=tag(x0)
  if not record(('bind,'if address==bind_call else'update,'if address==update_call else'remove,')+str(i)):return
  if i==row[2] and i not in mutated and row[3]!=0:
   mutated.add(i);action=row[3]
   if action==8:
    if 0<=row[1]<read(slots+0x18):put(slots+0x20+row[1]*8,ref(row[4]),8);returned(uc)
    else:fail(uc)
   else:
    uc.reg_write(UC_ARM64_REG_X0,owner);uc.reg_write(UC_ARM64_REG_X1,ref(row[4])if action in(1,2,5)else row[1]if action==6 else generic if action==7 else row[4]&0xffffffffffffffff)
    uc.reg_write(UC_ARM64_REG_X2,generic)
    uc.reg_write(UC_ARM64_REG_PC,{1:0x3f40bf8,2:0x3f40d7c,3:0x3f41308,4:0x3f414b0,5:0x3f40eac,6:0x3a139e8,7:0x3f41560}[action])
  else:returned(uc)
 elif address==0x2310854:returned(uc,read(x0,8))
 elif address==0x261dca0:returned(uc,x0==x1)
 elif address==0x2c1b9f4:
  i=signed(x1&0xffffffff)
  if not 0<=i<read(lst+0x18):fail(uc)
  else:put(backing+0x20+i*8,x2,8);put(lst+0x1c,read(lst+0x1c)+1);returned(uc)
 elif address==0x2c1d22c:
  original=values();v=[p for p in original if config[tag(p)][1]not in(2,3)]
  put(lst+0x18,len(v));put(lst+0x1c,read(lst+0x1c)+1)
  for j,p in enumerate(v+[0]*(len(original)-len(v))):put(backing+0x20+j*8,p,8)
  returned(uc,len(original)-len(v))
 elif address==0x2c1b9a0:
  i=signed(x1&0xffffffff)
  if not 0<=i<read(lst+0x18):fail(uc)
  else:returned(uc,read(backing+0x20+i*8,8))
 elif address==0x2c1c940:
  v=values();i=signed(x1&0xffffffff)
  if not 0<=i<=len(v):fail(uc)
  else:
   v.insert(i,x2);put(lst+0x18,len(v));put(lst+0x1c,read(lst+0x1c)+1)
   for j,p in enumerate(v):put(backing+0x20+j*8,p,8)
   returned(uc)
 elif address==0x2c1d3c0:
  v=values();i=signed(x1&0xffffffff)
  if not 0<=i<len(v):fail(uc)
  else:
   v.pop(i);put(lst+0x18,len(v));put(lst+0x1c,read(lst+0x1c)+1)
   for j,p in enumerate([*v,0]):put(backing+0x20+j*8,p,8)
   returned(uc)
 elif address==0x2627eb8:emu.mem_write(x0+0x20+x1*8,bytes(x2*8));returned(uc)
 elif address==0x3a13938:uc.reg_write(UC_ARM64_REG_PC,0x3a139cc) # Omit missing-unit diagnostic formatting only.
 elif address==0x3a13c0c:uc.reg_write(UC_ARM64_REG_PC,0x3a13d58) # Omit post-bind diagnostic logging only.
 else:base_hook(uc,address,size,unused)
for a in [0xffd960,0xffd90c,0xffda64,0xffda88,0xffda90,0xffde20,0xfcd934,0xffda68,id_call,bind_call,update_call,factory_call,remove_call,0x2310854,0x261dca0,0x2c1b9f4,0x2c1d22c,0x2c1b9a0,0x2c1c940,0x2c1d3c0,0x2627eb8,0x3a13938,0x3a13c0c]:emu.hook_add(UC_HOOK_CODE,hook,begin=a,end=a)
cases=[];expected=[];commands=0
def run(count,cfg,rows):
 global config,row,mutated,calls,commands
 config=cfg;emu.mem_write(owner,bytes(0x4000));put(owner+0x10,lst,8);put(owner+0x18,slots,8)
 put(lst+0x10,backing,8);put(backing+0x18,256);put(slots+0x18,count)
 for i in range(len(cfg)):
  klass=0x10060000+cfg[i][1]*0x400;put(ref(i),klass,8);put(klass+0x12a,0,2)
 output=[]
 for row in rows:
  trace.clear();calls=0;mutated=set();commands+=1;mode,a=row[:2]
  pc={0:0x3f40bf8,1:0x3f40d7c,2:0x3f41308,3:0x3f418b4,4:0x3f417b0,5:0x3f414b0,6:0x3f41478,7:0x3a13898,8:0x3f418ac,9:0x3f418ac,10:0x3f40eac,11:0x3a139e8,12:0x3f41560}[mode]
  answer=call(pc,owner,ref(a)if mode in(0,1,10)else generic if mode==12 else a,generic);result='-'
  if not fault:
   if mode==6:result=str(answer)
   elif mode in(7,11):result=str(tag(emu.reg_read(UC_ARM64_REG_X0)))
   elif mode==8:
    if 0<=a<count:put(slots+0x20+a*8,ref(row[4]),8)
    else:fail(emu)
   elif mode==9:result=str(int(emu.reg_read(UC_ARM64_REG_X0)==slots))
  if fault:result='error'
  slot_values=','.join(str(tag(read(slots+0x20+i*8,8)))for i in range(count))
  output.append(result+':'+slot_values+':'+','.join(str(tag(p))for p in values())+':'+'/'.join(trace))
 cases.append(str(count)+'#'+';'.join(','.join(map(str,v))for v in cfg)+'#'+';'.join(','.join(map(str,v))for v in rows));expected.append('|'.join(output))
cfg=[[7,1],[3,0],[1,1],[3,1],[0,1],[4,0],[-1,1],[8,1]]
prefix=[[0,i,-1,0,0,0]for i in (0,1,2,3,4,5)]
for failure in range(25):
 for action in range(5):
  for mode in (3,4):run(8,cfg,prefix+[[mode,0,2,action,3,failure],[3,0,-1,0,0,0],[4,0,-1,0,0,0],[2,3,-1,0,0,0],[9,0,-1,0,0,0]])
rng=random.Random(0x3f417b0)
for i in range(220):
 rows=[]
 for j in range(28):
  mode=rng.randrange(10);a=rng.randrange(-1,8)if mode in(0,1)else rng.randrange(-1,9)
  rows.append([mode,a,rng.randrange(-1,6),rng.randrange(5),rng.randrange(-1,8),rng.randrange(16)])
 run(8,cfg,rows)
for count in(0,1,3):run(count,cfg,[[0,4,-1,0,0,0],[3,0,-1,0,0,0],[4,0,-1,0,0,0],[7,0,-1,0,0,0],[6,-1,-1,0,0,0],[5,0,-1,0,0,0]])
# All concrete type pairs: same type retains old instance; plain replacements
# can intentionally keep an older behaviour. Slot id may differ from factory Id.
cfg=[[i%4,k]for k in range(6)for i in range(4)]+[[-1,3],[8,3]]
for old in range(24):
 for new in range(24):
  if cfg[old][0]!=cfg[new][0]:continue
  for failure in range(12):run(8,cfg,[[0,old,-1,0,0,0],[10,new,-1,0,0,failure],[3,0,-1,0,0,0],[4,0,-1,0,0,0],[12,0,-1,0,0,0]])
for factory in range(-1,len(cfg)):
 for requested in(-1,0,3,7,8):
  for failure in range(4):run(8,cfg,[[11,requested,-1,0,factory,failure],[11,requested,-1,0,-1,0],[12,0,-1,0,0,0]])
for failure in range(18):
 for action in range(9):
  for mode in(3,4,11,12):
   run(8,cfg,[[0,i,-1,0,0,0]for i in(12,9,6,3,10)]+[[mode,7,12,action,15,failure],[12,0,-1,0,0,0],[4,0,-1,0,0,0]])
for i in range(160):
 rows=[]
 for j in range(36):
  mode=rng.randrange(13);a=rng.randrange(-1,len(cfg))if mode in(0,1,10)else rng.randrange(-1,9)
  rows.append([mode,a,rng.randrange(-1,24),rng.randrange(9),rng.randrange(-1,len(cfg)),rng.randrange(16)])
 run(8,cfg,rows)
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-context-container-')as folder:
 exe=Path(folder)/'verify.exe'
 result=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-langversion:latest','-out:'+str(exe),str(PROJECT/'SourceCatalog/ContextContainerHarness.cs'),str(PROJECT/'Assets/Foundation/Runtime/OriginalContextContainer.cs')],capture_output=True,text=True,encoding='utf-8-sig');assert result.returncode==0,result.stdout+result.stderr
 result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8-sig');assert result.returncode==0,result.stdout+result.stderr
 actual=result.stdout.splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
 for i,(a,e)in enumerate(zip(actual,expected)):assert a==e,(i,cases[i],a,e)
report={'passed':True,'native_sequences':len(cases),'native_commands':commands,'scope':'Actual ARM64 ContextContainer Add/AddToFirst/Replace/Remove/Has/Get/GetLate/GetAllUnits/BindAll/ManualUpdate/Clear/RemoveLateUnits. Compares slot and behaviour order, concrete type identity, duplicate ids, plain/behaviour/late units, ascending-id binding versus registration-order updates, first matching replacement/removal, live callback mutations, deferred construction/publication/binding/reentrant lookup, requested slot independent of unit Id, late removal and partial state after injected constructor/bind/remove/id failures. CLR metadata/interface dispatch/type identity/list operations/storage and error transport supplied boundaries. Deferred constructor transport is an explicit factory in C# instead of native Activator/reflection. Missing-unit/post-bind diagnostics and SDK exception logging omitted; callback exceptions propagate. Complete actual unit factories and application activation remain separate.'}
(PROJECT/'context_container_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
