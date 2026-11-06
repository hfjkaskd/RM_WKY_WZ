"""Original ARM64 static above/below explosion dispatch vs runtime C#."""
import sys
sys.dont_write_bytecode=True
import json,os,subprocess,tempfile
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_move_budget.py').read_text().split('cases=[];expected=[]')[0],str(PROJECT/'SourceCatalog/verify_move_budget.py'),'exec'))
emu.hook_del(native_hook);base_hook=hook
root,slots,above,below,aset,bset,data_ptr,klass=0x10020000,0x10021000,0x10022000,0x10023000,0x10024000,0x10025000,0x10026000,0x10027000
models={6:0x10030000,2:0x10031000,3:0x10032000,4:0x10033000};types={p:t for t,p in models.items()};sets={};head=[];calls=0
def record(value):
 global calls
 trace.append(value);calls+=1
 if calls==head[8]:fail(emu);return False
 return True
def hook(uc,address,size,unused):
 x0,x1,x2=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(3)]
 if address==0x2c1b9a0:
  i=signed(x1&0xffffffff)
  if not 0<=i<read(x0+0x18):fail(uc)
  else:returned(uc,read(read(x0+0x10,8)+0x20+i*8,8))
 elif address==0x2edbfd0:sets[x0].add(signed(x1&0xffffffff));returned(uc)
 elif address==0x2eec020:returned(uc,signed(x1&0xffffffff)in sets[x0])
 elif address in (0x20000010,0x20000020):
  near=address==0x20000010
  if record(('near'if near else'match')+','+str(types[x0])):returned(uc,head[2]if near else head[3])
 elif address in (0x2b52340,0x20000030):
  label='all'if address==0x20000030 else'explode';v=[types[x0],signed(read(x1+0xc)),signed(read(x1+8)),signed(read(x1+0x10)),read(x1+0x14,1)]
  if record(label+','+','.join(map(str,v))):returned(uc)
 else:base_hook(uc,address,size,unused)
for a in [0xffd960,0xffd90c,0xffda64,0xffda88,0xffda90,0xffda50,0x2c1b9a0,0x2edbfd0,0x2eec020,0x20000010,0x20000020,0x20000030,0x2b52340]:emu.hook_add(UC_HOOK_CODE,hook,begin=a,end=a)
put(klass+0x528,0x20000010,8);put(klass+0x448,0x20000020,8);put(klass+0x478,0x20000030,8)
for t,p in models.items():put(p,klass,8);put(p+0x18,t)
put(root+0x10,slots,8);put(slots+0x18,20);put(root+0x20,aset,8);put(root+0x28,bset,8);put(root+0x30,above,8);put(root+0x38,below,8)
for p in (above,below):put(p+0x10,p+0x100,8)
cases=[];expected=[]
def run(h):
 global head,sets,calls
 head=h;sets={aset:set(),bset:set()};put(above+0x18,h[0]);put(below+0x18,h[1]);put(above+0x120,models[6],8);put(above+0x128,models[6],8);put(below+0x120,models[h[9]],8);put(below+0x128,models[4],8)
 for t in range(20):put(slots+0x20+t*8,models.get(t,0)if h[10]==0 and t in (6,h[9],4)else 0,8)
 emu.mem_write(data_ptr,bytes(56));put(data_ptr,2);put(data_ptr+4,3);put(data_ptr+8,h[4]);put(data_ptr+0xc,h[5]);put(data_ptr+0x10,h[6]);put(data_ptr+0x14,h[7],1)
 out=[]
 for command in ['b','b','a','b','a','e2','e3','e6']:
  trace.clear();calls=0
  answer=call(0x3440544,root,data_ptr)if command=='a'else call(0x34407f4,root,data_ptr)if command=='b'else call(0x3440658,root,int(command[1:]),data_ptr)
  result='error'if fault else str(answer)if command=='a'else'ok'
  out.append(result+':'+str(int(h[5]in sets[aset]))+','+str(int(h[5]in sets[bset]))+':'+'/'.join(trace))
 cases.append(','.join(map(str,h)));expected.append('|'.join(out))
for trigger in range(-1,33):
 for mode in range(32):run([mode%3,(mode//3)%3,mode&1,(mode>>1)&1,trigger,71,(mode>>2)&1,(mode>>3)&1,0,3 if mode&16 else 2,0])
for failure in range(1,4):
 for trigger in (2,4,5,6,15,25):
  for jelly in (0,1):
   for kind in (2,3):run([2,2,1,1,trigger,71,1,jelly,failure,kind,0])
for trigger in (2,5,6,15,25):run([1,2,0,0,trigger,-1,1,1,0,3,1])
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-static-explosion-')as folder:
 exe=Path(folder)/'verify.exe';runtime=PROJECT/'Assets/Foundation/Runtime'
 p=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-langversion:latest','-out:'+str(exe),str(PROJECT/'SourceCatalog/StaticExplosionDispatchHarness.cs'),str(runtime/'OriginalStaticExplosionDispatch.cs'),str(runtime/'OriginalExplodeData.cs')],capture_output=True,text=True,encoding='utf-8-sig');assert p.returncode==0,p.stdout+p.stderr
 p=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8-sig');assert p.returncode==0,p.stderr
 actual=p.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected)
 for i,(a,e)in enumerate(zip(actual,expected)):assert a==e,(i,cases[i],a,e)
report={'passed':True,'native_sequences':len(cases),'native_commands':len(cases)*8,'scope':'Actual ARM64 StaticItemMediator.ExplodeTopMostAboveItem/ExplodeTopMostBelowItem/ExplodeStaticItem, ShouldExplodeAll and IsMatchExplodeOrItemSwap. Above last/below first ordering, near/match gates, Jelly count/dedup distinction, top suppression of below, ordinary versus all dispatch, missing registry targets and partial mutation on injected virtual-call failures. CLR list/array storage, exploder sets, static-model explosion and capability virtuals are supplied boundaries; full static-model factories and damage are not established. Actual normal match destruction/view recycling/freeze/refill is exercised separately in Unity.'}
(PROJECT/'static_explosion_dispatch_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
