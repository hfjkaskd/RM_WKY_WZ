"""Run original ARM64 GameContext update/deactivation/reset/animation cleanup.

Unit Clear, orientation/coroutine/tween/pool/audio/camera/animator/touch owners,
level/inventory getters, scene loader and system-message transport are typed
ports. Original borrowed-array traversal, event/publication order, branch
lookups, required-null failures and partial state execute from ELF. Only native
diagnostic logging is skipped. This is not full startup, HomeScene, renderer,
all-level unit factory or Android/iOS execution proof.
"""
from pathlib import Path
import sys,json,hashlib,itertools,random,subprocess,tempfile
PROJECT=Path(__file__).resolve().parents[1];BASE=PROJECT.parent
sys.path.insert(0,str(BASE/'tools/python'))
from elftools.elf.elffile import ELFFile
from elftools.elf.relocation import RelocationSection
from unicorn import Uc,UC_ARCH_ARM64,UC_MODE_ARM,UC_HOOK_CODE
import unicorn.arm64_const as r
u=Uc(UC_ARCH_ARM64,UC_MODE_ARM);u.mem_map(0,0x6000000);u.mem_map(0x10000000,0x400000);u.mem_map(0x20000000,0x1000)
def put(p,v,n=8):u.mem_write(p,(int(v)&((1<<(8*n))-1)).to_bytes(n,'little'))
def read(p,n=8):return int.from_bytes(u.mem_read(p,n),'little')
def x(i):return u.reg_read(getattr(r,'UC_ARM64_REG_X'+str(i)))
def returned(v=0):u.reg_write(r.UC_ARM64_REG_X0,v);u.reg_write(r.UC_ARM64_REG_PC,u.reg_read(r.UC_ARM64_REG_LR))
with(BASE.parent/'RoyalMatch_20260929/apk_unpacked/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so').open('rb')as stream:
 elf=ELFFile(stream)
 for s in elf.iter_segments():
  if s['p_type']=='PT_LOAD':u.mem_write(s['p_vaddr'],s.data())
 for s in elf.iter_sections():
  if isinstance(s,RelocationSection):
   for e in s.iter_relocations():
    if e['r_info_type']==1027:put(e['r_offset'],e['r_addend'])
context,manager,statics,container,array,alternate,klass,offsets,controller,coroutines,factory,audio,camera,touch,level,scene,messages,user,user_statics,inventory,event,late_event=[0x10001000+i*0x1000 for i in range(22)]
units=[0x10020000+i*0x1000 for i in range(4)]
game_slot=read(0x5351eb0);unit_slot=read(0x539dde0);user_slot=read(0x53bb4e0);mtween_slot=read(0x5375e78)
metadata=0x10200000;allocation=0x10300000;trace=[];fault=False;v=[];counts=[];late_calls=0
def fail():
 global fault
 fault=True;u.reg_write(r.UC_ARM64_REG_PC,0x20000000)
def step(value,missing=0):
 trace.append(value)
 if missing and v[0]==missing:fail();return False
 return True
def hook(uc,address,size,unused):
 global metadata,allocation,late_calls
 a,b=x(0),x(1)
 if address==0xffd960:
  put(a,metadata);put(metadata+0xe0,1,4)
  if a==game_slot:put(metadata+0xb8,statics)
  if a==user_slot:put(metadata+0xb8,user_statics)
  if a==unit_slot:put(offsets,metadata)
  if a==mtween_slot:put(metadata+0xb8,0x10040000)
  metadata+=0x200
 elif address in(0xffd90c,0xffda64):pass
 elif address in(0xffda88,0xffda90):fail();return
 elif address==0xffda78:allocation+=0x100;returned(allocation);return
 elif address==0x20063c8:put(a+0x10,b)
 elif address==0x2002348:
  assert read(a+0x10)==context
  if not step('orientation-',24):return
 elif address==0x20000610:
  index=units.index(a);counts[index]+=1;trace.append('clear,'+str(index)+','+str(b))
  if index==1:put(manager+0x68,0,1)
  if index==0:
   if v[0]==4 or v[5]==3:fail();return
   if v[5]==1:put(array+0x28,0)
   if v[5]==2:put(container+0x18,alternate)
 elif address==0x20000620:
  trace.append('event,'+str(read(statics+0x20,1)))
  if v[4]==2:fail();return
  if v[4]==3:put(statics+0x20,1,1)
  if v[4]==4:put(statics+8,0)
  if v[4]==5:put(container+0x18,alternate)
 elif address==0x20000630:late_calls+=1
 elif address==0x1e39694:u.reg_write(r.UC_ARM64_REG_PC,0x1e396f0);return # Diagnostic type/logger reads only.
 elif address==0x3f417b0:assert a==container;step('update')
 elif address==0x1d7b030:
  if not step('tweens,'+str(a),23):return
 elif address==0x3e4c71c:assert a==controller;step('stop-mtween')
 elif address==0x2d7cd40:assert a==coroutines;step('kill-coroutines')
 elif address==0x260dea4:assert a==1;returned(0 if v[0]==7 else factory);return
 elif address==0x23f1e6c:assert a==factory;step('recycle')
 elif address==0x23f1e50:
  assert a==factory
  if not step('clear-pools',15):return
 elif address==0x30f6400:
  returned({17:0 if v[0]==10 else touch,18:0 if v[0]==8 else audio,2:0 if v[0]==19 else scene}[a]);return
 elif address==0x3e18818:assert a==audio;step('sound')
 elif address==0x2002208:returned(0 if v[0]==9 else camera);return
 elif address==0x2002f20:assert a==camera;step('camera')
 elif address==0x38ff740:
  if not step('animators',22):return
 elif address in(0x2c986cc,0x2c9869c,0x2c9866c):
  assert a==touch+0x40 and b==context
  value,missing={0x2c986cc:('pinch',11),0x2c9869c:('vertical',12),0x2c9866c:('horizontal',13)}[address]
  if not step(value,missing):return
 elif address==0x2d78788:
  if not step('low',14):return
  returned(v[1]);return
 elif address==0x1e18fec:
  if not step('early',16):return
  returned(v[2]);return
 elif address==0x28e68d4:assert a==inventory;step('played');returned(v[3]);return
 elif address==0x29e27cc:returned((0 if v[0]==18 else level)if a==2 else(0 if v[0]==20 else messages));return
 elif address==0x1e1a8fc:assert a==level;step('reload')
 elif address in(0x39089a0,0x3908124):assert a==scene;step('game'if address==0x39089a0 else'home')
 elif address==0x2230810:
  assert a==messages and b==0
  if not step('messages,null',21):return
 else:raise AssertionError(hex(address))
 returned()
for a in(0xffd960,0xffd90c,0xffda64,0xffda88,0xffda90,0xffda78,0x20063c8,0x2002348,0x20000610,0x20000620,0x20000630,0x1e39694,0x3f417b0,0x1d7b030,0x3e4c71c,0x2d7cd40,0x260dea4,0x23f1e6c,0x23f1e50,0x30f6400,0x3e18818,0x2002208,0x2002f20,0x38ff740,0x2c986cc,0x2c9869c,0x2c9866c,0x2d78788,0x1e18fec,0x28e68d4,0x29e27cc,0x1e1a8fc,0x39089a0,0x3908124,0x2230810):u.hook_add(UC_HOOK_CODE,hook,begin=a,end=a)
def call(address,arg=0):
 global fault
 fault=False
 for i in range(8):u.reg_write(getattr(r,'UC_ARM64_REG_X'+str(i)),context if i==0 else arg if i==1 else 0)
 u.reg_write(r.UC_ARM64_REG_SP,0x103ff000);u.reg_write(r.UC_ARM64_REG_LR,0x20000000);u.emu_start(address,0x20000000,count=20000)
 assert u.reg_read(r.UC_ARM64_REG_PC)==0x20000000,hex(u.reg_read(r.UC_ARM64_REG_PC))
cases=[]
for missing,low,early,played,event_mode,unit_mode in itertools.product(range(25),range(2),range(2),range(2),range(3),range(3)):
 cases.append(','.join(map(str,(missing,low,early,played,event_mode,unit_mode,1,1)))+';U;U;R0;K')
for event_mode,unit_mode,active,flag in itertools.product(range(6),range(4),range(2),range(2)):
 for sequence in('U;U','R1;R0;K','A0;U;A1;F0;U;F1;U','K;U'):
  cases.append(','.join(map(str,(0,1,1,1,event_mode,unit_mode,active,flag)))+';'+sequence)
expected=[]
for case in cases:
 fields,*commands=case.split(';');v=list(map(int,fields.split(',')));trace=[];fault=False;counts=[0]*4;late_calls=0;allocation=0x10300000
 for p in(context,statics,container,array,alternate,klass,offsets,user,user_statics,manager):u.mem_write(p,bytes(0x400))
 put(statics,0 if v[0]==2 else container);put(statics+8,controller);put(statics+0x10,0 if v[0]==6 else coroutines);put(statics+0x18,late_event);put(statics+0x20,v[6],1)
 put(0x10040000,0 if v[0]==5 else controller);put(context+0x20,0 if v[0]==1 else manager);put(manager+0x68,v[7],1);put(container+0x18,0 if v[0]==3 else array)
 for p in(array,alternate):put(p+0x18,3,4)
 for i,p in enumerate(units[:3]):put(array+0x20+i*8,p)
 put(alternate+0x20,units[3]);put(user_statics,user);put(user+0x20,0 if v[0]==17 else inventory)
 put(klass+0x12a,1,2);put(klass+0xb0,offsets);put(offsets+8,0,4);put(klass+0x138,0x20000610)
 put(offsets,read(read(0x539dde0)))
 for p in units:put(p,klass)
 for p,fn in((event,0x20000620),(late_event,0x20000630)):put(p+0x18,fn);put(p+0x40,context)
 put(context+0x18,event if v[4]else 0)
 for command in commands:
  if command[0]=='A':put(statics+0x20,int(command[1]),1);continue
  if command[0]=='F':put(manager+0x68,int(command[1]),1);continue
  call({'U':0x1e39ba0,'R0':0x1e398b8,'R1':0x1e398b8,'K':0x1e39a14}[command],int(command[1])if command.startswith('R')else 0)
  if fault:break
 failed=fault;call(0x1e39d68)
 expected.append(str(int(failed))+'#'+','.join(map(str,(read(statics+0x20,1),int(read(statics+8)!=0),late_calls,read(manager+0x68,1),*counts)))+'#'+';'.join(trace))
mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
sources=['SourceCatalog/GameContextExitHarness.cs','Assets/Foundation/Runtime/OriginalGameContextDeactivation.cs','Assets/Foundation/Runtime/OriginalGameLateUpdateDispatcher.cs']
with tempfile.TemporaryDirectory()as directory:
 exe=Path(directory)/'Context.exe';command=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',*['-r:'+str(mono/'lib/mono/4.5'/p)for p in('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),*[str(PROJECT/p)for p in sources]]
 result=subprocess.run(command,capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 actual=result.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected)
 for i,(a,b)in enumerate(zip(actual,expected)):assert a==b,(i,cases[i],a,b)
report={'passed':True,'cases':len(cases),'scope':__doc__.strip(),'original_rvas':['0x1e39ba0','0x1e39488','0x1e398b8','0x1e39a14','0x1e39d68'],'files':[{'path':p,'sha256':hashlib.sha256((PROJECT/p).read_bytes()).hexdigest()}for p in sources+['SourceCatalog/verify_game_context_exit.py']]}
(PROJECT/'game_context_exit_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
