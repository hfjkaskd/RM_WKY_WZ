"""Execute the original ARM64 result actions against the runtime C# control path.

Ad transport, event eligibility/flow scheduling, orientation, dialog Close and
scene exit are explicit hooked ports. Actual Continue/Close/Rewarded guards,
callback filtering, event order and captured manager references execute from
libil2cpp. This does not prove actual SDK, event UI, scene exit or mobile parity.
"""
from pathlib import Path
import sys, itertools, random, subprocess, tempfile, json, hashlib
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
original_callbacks={read(slot):rva for slot,rva in ((0x5386008,0x1121b74),(0x53daa18,0x1121f50),(0x536f9c8,0x133ef20),(0x5348128,0x133ef3c))}
dialog,bottom,ads,lava,ancient,manager_a,manager_b=[0x10001000+i*0x1000 for i in range(7)]
metadata=0x10100000;allocation=0x10200000;trace=[];fault=False
normal=rewarded=gate=None;pending=[];use_b=False;missing=accept_lava=accept_ancient=immediate=0
def fail():
 global fault
 fault=True;u.reg_write(r.UC_ARM64_REG_PC,0x20000000)
def hook(uc,address,size,unused):
 global metadata,allocation,normal,rewarded,gate
 a,b=x(0),x(1)
 if address==0xffd960:put(a,metadata);put(metadata,original_callbacks.get(a,0));put(metadata+0xe0,1,4);metadata+=0x200
 elif address in(0xffd90c,0xffda64,0x133ef18):pass
 elif address==0xffda78:
  result=allocation;allocation+=0x100;u.mem_write(result,bytes(0x100));returned(result);return
 elif address in(0xffda88,0xffda90):fail();return
 elif address in(0x2044a78,0x14f5bb4):put(a+0x10,b);put(a+0x18,x(2))
 elif address==0x29e27cc:
  if a==75:trace.append('ads');returned(0 if missing==1 else ads)
  elif a==34:trace.append('getlava');returned(0 if missing==4 else lava)
  elif a==68:trace.append('getancient');returned(0 if missing==5 else ancient)
  else:raise AssertionError(a)
  return
 elif address==0x2605110:
  assert a==1;trace.append('manager');returned(0 if missing==3 else manager_b if use_b else manager_a);return
 elif address==0x1121868:trace.append('star')
 elif address==0x111fa78:assert a==bottom;trace.append('bottom')
 elif address==0x3e9fb98:
  assert a==ads;trace.append('normal');normal=b
  if immediate:pending.append(('N',normal))
 elif address==0x3e9f854:
  assert a==ads;trace.append('rewarded');rewarded=b
  if immediate:pending.append(('Y'if immediate==1 else'Z',rewarded))
 elif address==0x1cb5e00:
  assert a==lava;trace.append('lava,'+str(b));gate=x(2)
  if missing==8:fail();return
  returned(accept_lava);return
 elif address==0x20e9f74:
  assert a==ancient;trace.append('ancient,'+str(x(2))+','+str(x(3)));gate=b
  if missing==9:fail();return
  returned(accept_ancient);return
 elif address==0x25cb9f0:assert a==dialog;trace.append('close')
 elif address==0x1e3b604:
  assert a in(manager_a,manager_b);trace.append('exit'+('A'if a==manager_a else'B'))
  if missing==7:fail();return
 else:raise AssertionError(hex(address))
 returned()
for a in(0xffd960,0xffd90c,0xffda64,0xffda78,0xffda88,0xffda90,0x133ef18,0x2044a78,0x14f5bb4,0x29e27cc,0x2605110,0x1121868,0x111fa78,0x3e9fb98,0x3e9f854,0x1cb5e00,0x20e9f74,0x25cb9f0,0x1e3b604):u.hook_add(UC_HOOK_CODE,hook,begin=a,end=a)
def call(address,*args):
 global fault
 fault=False
 for i in range(8):u.reg_write(getattr(r,'UC_ARM64_REG_X'+str(i)),args[i]if i<len(args)else 0)
 u.reg_write(r.UC_ARM64_REG_SP,0x103ff000);u.reg_write(r.UC_ARM64_REG_LR,0x20000000)
 u.emu_start(address,0x20000000,count=10000);assert u.reg_read(r.UC_ARM64_REG_PC)==0x20000000,hex(u.reg_read(r.UC_ARM64_REG_PC))
def callback(command,delegate):
 if not delegate:return
 target=read(delegate+0x10);method=read(read(delegate+0x18))
 if command=='N':assert method==0x1121b74;call(method,target)
 elif command in('Y','Z'):assert method==0x1121f50;call(method,target,command=='Y')
 else:assert method in(0x133ef20,0x133ef3c);call(method,target)
cases=[]
for fields in itertools.product((0,1,2,3,4,5,7,8,9),range(2),range(2),range(3)):
 for sequence in('C;C;N;M;V','R;Z;Y;M;V','X;N;X;N','T;M;V','C;R;Z;X;Y;V'):
  cases.append(','.join(map(str,fields))+';'+sequence)
rng=random.Random(0x1121b74)
for _ in range(400):cases.append(f'0,{rng.randrange(2)},{rng.randrange(2)},{rng.randrange(3)};'+ ';'.join(rng.choice('CRXTNYZVM')for _ in range(12)))
expected=[]
for case in cases:
 fields,*commands=case.split(';');missing,accept_lava,accept_ancient,immediate=map(int,fields.split(','));trace=[];normal=rewarded=gate=None;pending=[];use_b=False
 u.mem_write(dialog,bytes(0x200));put(dialog+0xd8,0 if missing==2 else bottom)
 for command in commands:
  if command=='M':use_b=True;continue
  if command in'CRXT':call({'C':0x11205f0,'R':0x1120754,'X':0x1121aa0,'T':0x1121b74}[command],dialog)
  else:callback(command,normal if command=='N'else rewarded if command in'YZ'else gate)
  while pending and not fault:
   cmd,delegate=pending.pop(0);callback(cmd,delegate)
  if fault:break
 expected.append(str(int(fault))+'#'+str(read(dialog+0x1a0,1))+'#'+';'.join(trace))
mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
sources=['SourceCatalog/LevelWinActionsHarness.cs','Assets/Foundation/Runtime/OriginalLevelWinActions.cs']
with tempfile.TemporaryDirectory()as directory:
 exe=Path(directory)/'Win.exe';command=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',*['-r:'+str(mono/'lib/mono/4.5'/p)for p in('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),*[str(PROJECT/p)for p in sources]]
 result=subprocess.run(command,capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 actual=result.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected)
 for index,(a,b)in enumerate(zip(actual,expected)):assert a==b,(index,cases[index],a,b)
report={'passed':True,'cases':len(cases),'scope':__doc__.strip(),'original_rvas':['0x11205f0','0x112060c','0x1120754','0x1121aa0','0x1121b74','0x1121f50','0x133ef20','0x133ef3c'],'files':[{'path':p,'sha256':hashlib.sha256((PROJECT/p).read_bytes()).hexdigest()}for p in sources+['SourceCatalog/verify_level_win_actions.py']]}
(PROJECT/'level_win_actions_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
