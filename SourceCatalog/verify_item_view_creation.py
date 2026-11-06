"""Actual ARM64 match spawn/init and create-with-view orchestration."""
import sys
sys.dont_write_bytecode=True
import json,os,subprocess,tempfile,struct
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_move_budget.py').read_text().split('cases=[];expected=[]')[0],str(PROJECT/'SourceCatalog/verify_move_budget.py'),'exec'))
emu.hook_del(native_hook);base_hook=hook
model,klass,view,vklass,factory,cell,mediator,settings,owner=[0x10040000+i*0x800 for i in range(9)]
put(model,klass,8);put(klass+0x368,0x2b45d78,8);put(klass+0x408,0x21a4fd8,8);put(klass+0x418,0x21a5668,8);put(view,vklass,8);put(vklass+0x278,0x2b45d70,8);put(cell+0x10,mediator,8)
head=[];calls=0;mask=True;state=7
def s(v):return signed(v&0xffffffff)
def record(t):
 global calls
 trace.append(t);calls+=1
 if calls==head[2]:fail(emu);return False
 return True
def point():
 return ','.join(str(int(struct.unpack('<f',struct.pack('<I',emu.reg_read(UC_ARM64_REG_S0+i)))[0]*4))for i in range(3))
def hook(uc,address,size,unused):
 global mask,state
 x0,x1,x2=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(3)]
 if address==0x260a220:
  assert x1==2 and x2==1
  if record('spawn,2,1'):returned(uc,0 if head[0]&2 else view)
 elif address==0x21a5094:
  if record('init,'+point()+','+str(int(read(model+0xb0,8)==x0))+','+str(read(model+0x14,1))):returned(uc)
 elif address==0x2b45d78:put(x0+0x14,x1,1);returned(uc)
 elif address==0x23ec140:
  p=uc.reg_read(UC_ARM64_REG_X8);emu.mem_write(p,bytes(72));put(p,x0);returned(uc)
 elif address==0x23ec1e8:
  if record('model,'+str(s(read(x0)))):returned(uc,0 if head[0]&1 else model)
 elif address==0x21a5668:
  if record('view'):returned(uc,0 if head[0]&4 else read(model+0xb0,8))
 elif address==0x2b45d70:
  if record('disable'):mask=False;returned(uc)
 elif address==0x3448578:
  if record('clear'):state=0;returned(uc)
 elif address==0x34485f4:
  if record('set'):state=91;returned(uc)
 elif address==0xd00b50:emu.mem_write(x0,bytes(emu.mem_read(x1,x2)));returned(uc,x0)
 else:base_hook(uc,address,size,unused)
for a in(0xffd960,0xffd90c,0xffda64,0xffda88,0xffda90,0xffda50,0x260a220,0x21a5094,0x2b45d78,0x23ec140,0x23ec1e8,0x21a5668,0x2b45d70,0x3448578,0x34485f4,0xd00b50):emu.hook_add(UC_HOOK_CODE,hook,begin=a,end=a)
cases=[];expected=[]
for kind in('m','s','a','c'):
 for flags in range(32):
  for initial in(0,1):
   for failure in range(7):
    head=[flags,initial,failure,-7,13,2,-3,6,25];calls=0;trace.clear();mask=True;state=7;put(model+0x14,initial,1);put(model+0xb0,0,8);put(model+0x58,0 if flags&16 else factory,8);put(cell+0x18,head[6]);put(cell+0x1c,head[7]);put(settings,head[8])
    for i in range(3):emu.reg_write(UC_ARM64_REG_S0+i,struct.unpack('<I',struct.pack('<f',head[3+i]/4))[0])
    if kind=='m':call(0x21a4fd8,model)
    elif kind=='s':call(0x23f00bc,owner,settings)
    elif kind=='a':call(0x23f0038,owner,head[8])
    else:call(0x23f0154,owner,head[8],0 if flags&8 else cell)
    expected.append(('error'if fault else'ok')+':'+','.join(map(str,(read(model+0x14,1),int(read(model+0xb0,8)!=0),int(mask),state)))+':'+'/'.join(trace));cases.append(kind+'#'+','.join(map(str,head)))
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-view-creation-')as folder:
 exe=Path(folder)/'verify.exe';runtime=PROJECT/'Assets/Foundation/Runtime'
 p=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-langversion:latest','-out:'+str(exe),str(PROJECT/'SourceCatalog/ItemViewCreationHarness.cs'),str(runtime/'OriginalMatchViewCreation.cs'),str(runtime/'OriginalItemViewCreation.cs')],capture_output=True,text=True,encoding='utf-8-sig');assert p.returncode==0,p.stdout+p.stderr
 p=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8-sig');assert p.returncode==0,p.stderr
 actual=[x.replace('\ufeff','')for x in p.stdout.splitlines()];assert len(actual)==len(expected)
 for i,(a,e)in enumerate(zip(actual,expected)):assert a==e,(i,cases[i],a,e)
report={'passed':True,'native_cases':len(cases),'scope':'Actual ARM64 MatchItemModel.CreateView, ItemCreator.CreateItemAt/CreateItemWithSettings/CreateItemAtCell and CellPoint.ToVector. Exact view assignment before Init, HasView timing/state on null/failure, position forwarding, DisableFillMask and cell clear/set ordering, including injected failures. Concrete model/settings creation, prefab spawn, MatchItemView.Init and CLR mediation are supplied boundaries; actual prefab/pool/fill integration is verified in Unity. Full application/session and automatic FallManager/refill/player resolution remain pending.'}
(PROJECT/'item_view_creation_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
