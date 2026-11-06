"""Actual ARM64 game-exit, no-loading UI preparation and home-loading decision.

GameState.SetStateToNoGame, level-kind getters, scene UI/background/flags,
DialogManager.ClearDialogs and sliding text are typed ports. Native decision
short-circuits, exit-event order, flag writes and Clear mutations execute from
ELF. Dialog clear and actual state/view composition have separate checks;
this does not prove full context deactivation, HomeScene or mobile execution.
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
def read(p,n=4):return int.from_bytes(u.mem_read(p,n),'little')
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
game_context_slot=read(0x5351eb0,8)
manager,state,scene,controller,game_ui,ui_go,clearer,area_owner,area,event,statics=[0x10001000+i*0x1000 for i in range(11)]
metadata=0x10200000;trace=[];fault=False;v=[];missing=callback=0
def fail():
 global fault
 fault=True;u.reg_write(r.UC_ARM64_REG_PC,0x20000000)
def hook(uc,address,size,unused):
 global metadata
 a,b=x(0),x(1)
 if address==0xffd960:
  put(a,metadata);put(metadata+0xe0,1,4)
  if a==game_context_slot:put(metadata+0xb8,statics)
  metadata+=0x200
 elif address in(0xffd90c,0xffda64):pass
 elif address in(0xffda88,0xffda90):fail();return
 elif address==0x1ee5690:assert a==state;trace.append('nogame');put(state+0x10,0,4)
 elif address in(0x1e18cf4,0x1e18f34,0x1e192cc):
  index={0x1e18cf4:1,0x1e18f34:2,0x1e192cc:3}[address];trace.append(('story','table','lava')[index-1])
  if missing==13 and index==1:fail();return
  returned(v[index]);return
 elif address==0x2608558:
  assert a==0;trace.append('area');returned(0 if missing==12 else area_owner);return
 elif address==0x3e3b754:assert b==0;returned(int(a==0));return
 elif address==0x3e473b8:assert a==game_ui;returned(ui_go);return
 elif address==0x3e49b30:
  assert a==ui_go;trace.append('ui,'+str(b))
  if missing==6:fail();return
 elif address==0x2c8374c:
  trace.append('flags,'+str(a)+','+str(b))
  if missing==7:fail();return
 elif address==0x1e3a378:
  assert a==controller;trace.append('background')
  if missing==8:fail();return
 elif address==0x30f6400:
  assert a==15;trace.append('dialogs');returned(0 if missing==5 else clearer);return
 elif address==0x25ca8b0:
  assert a==clearer;trace.append('clear')
  if missing==9:fail();return
 elif address==0x3d8750c:
  trace.append('hide')
  if missing==10:fail();return
 elif address==0x20000600:
  trace.append('event,'+str(read(manager+0x68,1)))
  if missing==11:fail();return
  if callback>=2:
   put(manager+0x68,0,1)
   if callback==3:put(manager+0x98,0)
 else:raise AssertionError(hex(address))
 returned()
for a in(0xffd960,0xffd90c,0xffda64,0xffda88,0xffda90,0x1ee5690,0x1e18cf4,0x1e18f34,0x1e192cc,0x2608558,0x3e3b754,0x3e473b8,0x3e49b30,0x2c8374c,0x1e3a378,0x30f6400,0x25ca8b0,0x3d8750c,0x20000600):u.hook_add(UC_HOOK_CODE,hook,begin=a,end=a)
def call(address,obj,arg=0):
 global fault
 fault=False
 for i in range(8):u.reg_write(getattr(r,'UC_ARM64_REG_X'+str(i)),obj if i==0 else arg if i==1 else 0)
 u.reg_write(r.UC_ARM64_REG_SP,0x103ff000);u.reg_write(r.UC_ARM64_REG_LR,0x20000000);u.emu_start(address,0x20000000,count=10000)
 assert u.reg_read(r.UC_ARM64_REG_PC)==0x20000000,hex(u.reg_read(r.UC_ARM64_REG_PC))
 return x(0)
cases=[]
for bits in itertools.product(range(2),repeat=6):
 for missing,callback in itertools.product(range(14),range(4)):
  cases.append(','.join(map(str,(missing,*bits,callback)))+';L;X;X;C0;X;C1;X')
for missing,callback in itertools.product(range(14),range(4)):
 for sequence in('U;X','F;X;C1;X','X;C1;F;X','C0;X;C0;X'):
  cases.append(','.join(map(str,(missing,0,0,0,1,0,0,callback)))+';'+sequence)
expected=[]
for case in cases:
 fields,*commands=case.split(';');v=list(map(int,fields.split(',')));missing,callback=v[0],v[7];trace=[];fault=False
 for p in(manager,state,scene,controller,statics):u.mem_write(p,bytes(0x200))
 put(manager+0x28,0 if missing==1 else state);put(manager+0x40,0 if missing==2 else scene);put(state+0x10,2,4)
 put(controller+0x20,0 if missing==4 else game_ui);put(statics+8,0 if missing==3 else controller)
 put(scene+0x20,v[5],1);put(scene+0x31,v[6],1);put(area_owner+0x18,area if v[4]else 0)
 if callback:put(manager+0x98,event);put(event+0x18,0x20000600);put(event+0x40,manager)
 for command in commands:
  if command=='F':put(manager+0x68,1,1);continue
  if command=='U' and missing==3:fault=True;break
  if command=='X':call(0x1e3b604,manager)
  elif command=='U':call(0x1e3a2bc,controller)
  elif command=='L':
   result=call(0x3908264,scene)
   if not fault:trace.append('loading,'+str(result))
  else:call(0x1e3ac98,manager,int(command[1]))
  if fault:break
 expected.append(str(int(fault))+'#'+str(read(state+0x10))+','+str(read(manager+0x68,1))+'#'+';'.join(trace))
mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
sources=['SourceCatalog/GameExitHarness.cs','Assets/Foundation/Runtime/OriginalGameExitManager.cs','Assets/Foundation/Runtime/OriginalGameExitUi.cs','Assets/Foundation/Runtime/OriginalLevelWinActions.cs']
with tempfile.TemporaryDirectory()as directory:
 exe=Path(directory)/'Exit.exe';command=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',*['-r:'+str(mono/'lib/mono/4.5'/p)for p in('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),*[str(PROJECT/p)for p in sources]]
 result=subprocess.run(command,capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 actual=result.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected)
 for i,(a,b)in enumerate(zip(actual,expected)):assert a==b,(i,cases[i],a,b)
report={'passed':True,'cases':len(cases),'scope':__doc__.strip(),'original_rvas':['0x1e3b604','0x1e3b62c','0x1e3ac98','0x1e3a2bc','0x3908264'],'files':[{'path':p,'sha256':hashlib.sha256((PROJECT/p).read_bytes()).hexdigest()}for p in sources+['SourceCatalog/verify_game_exit.py']]}
(PROJECT/'game_exit_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
