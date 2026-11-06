"""Actual ARM64 CloseDialogs/ClearDialogs/HardReset vs the runtime manager.

Managed collection/list copies and ForceClose are typed boundaries. Callback
ports may remove, re-register or add dialogs, throw, or reset the manager.
The actual native snapshot loop, ordering, reset timing and partial counters
execute from ELF. The UiDialog Close coroutine is separately verified.
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
def si(v):return(v&0x7fffffff)-(v&0x80000000)
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
owner,dictionary,values,snapshot,array=[0x10001000+i*0x1000 for i in range(5)]
dialogs=[0x10010000+i*0x1000 for i in range(3)];metadata=0x10200000
trace=[];table={};fault=False;mode=0
def fail():
 global fault
 fault=True;u.reg_write(r.UC_ARM64_REG_PC,0x20000000)
def show(p):
 if p in table.values():return
 counter=si(read(owner+0x70))+1;put(owner+0x70,counter,4);put(p+0x30,counter,4);table[counter]=p;put(owner+0x74,read(owner+0x74)+1,4)
def reset():table.clear();put(owner+0x70,0,8)
def closed(p):
 if not table:fail();return
 if p==table[max(table)]:put(owner+0x74,read(owner+0x74)-1,4)
 table.pop(si(read(p+0x30)),None)
 if not table:put(owner+0x74,0,4)
def hook(uc,address,size,unused):
 global metadata
 a,b=x(0),x(1)
 if address==0xffd960:put(a,metadata);put(metadata+0xe0,1,4);metadata+=0x200
 elif address in(0xffd90c,0xffda64):pass
 elif address in(0xffda88,0xffda90):fail();return
 elif address==0x1274da0:assert a==dictionary;returned(values);return
 elif address==0x20bfa44:
  assert a==values;items=[table[k]for k in sorted(table)];put(snapshot+0x18,len(items),4);put(snapshot+0x10,array)
  for i,p in enumerate(items):put(array+0x20+i*8,p)
  returned(snapshot);return
 elif address==0x2c1b9a0:assert a==snapshot;returned(read(array+0x20+b*8,8));return
 elif address==0x1274eac:assert a==dictionary;table.clear()
 elif address==0x25cb9f8:
  assert b==1;index=dialogs.index(a);trace.append('force,'+str(index))
  if mode==1:closed(a)
  elif mode==2:show(dialogs[2])
  elif mode==3:fail();return
  elif mode==4:reset()
  elif mode==5:closed(a);show(a)
  if fault:return
 else:raise AssertionError(hex(address))
 returned()
for a in(0xffd960,0xffd90c,0xffda64,0xffda88,0xffda90,0x1274da0,0x20bfa44,0x2c1b9a0,0x1274eac,0x25cb9f8):u.hook_add(UC_HOOK_CODE,hook,begin=a,end=a)
def call(address):
 global fault
 fault=False
 for i in range(8):u.reg_write(getattr(r,'UC_ARM64_REG_X'+str(i)),owner if i==0 else 0)
 u.reg_write(r.UC_ARM64_REG_SP,0x103ff000);u.reg_write(r.UC_ARM64_REG_LR,0x20000000);u.emu_start(address,0x20000000,count=10000)
 assert u.reg_read(r.UC_ARM64_REG_PC)==0x20000000
sequences=('A;S0;A;S1','S0;S1;A;S2','S0;S1;C;S2;A','S0;S2;S1;C;C','S0;S1;H;S2;A','S0;S1;A;A;S0','S0;A;S0;C')
cases=[str(m)+';'+s for m,s in itertools.product(range(6),sequences)]
rng=random.Random(0x25ca8c8)
for _ in range(600):cases.append(str(rng.randrange(6))+';'+';'.join(rng.choice(('S0','S1','S2','A','C','H'))for _ in range(14)))
expected=[]
for case in cases:
 parts=case.split(';');mode=int(parts[0]);trace=[];table={};fault=False;u.mem_write(owner,bytes(0x100));put(owner+0x68,dictionary)
 for p in dialogs:u.mem_write(p,bytes(0x100))
 for command in parts[1:]:
  if command[0]=='S':show(dialogs[int(command[1])])
  else:call({'C':0x25ca8c8,'A':0x25ca8b0,'H':0x25c948c}[command])
  if fault:break
 expected.append(str(int(fault))+'#'+','.join(map(str,(len(table),si(read(owner+0x74)),*[si(read(p+0x30))for p in dialogs])))+'#'+';'.join(trace))
mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
sources=['SourceCatalog/DialogClearHarness.cs','Assets/Foundation/Runtime/OriginalDialogManager.cs']
with tempfile.TemporaryDirectory()as directory:
 exe=Path(directory)/'Clear.exe';command=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',*['-r:'+str(mono/'lib/mono/4.5'/p)for p in('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),*[str(PROJECT/p)for p in sources]]
 result=subprocess.run(command,capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 actual=result.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected)
 for i,(a,b)in enumerate(zip(actual,expected)):assert a==b,(i,cases[i],a,b)
report={'passed':True,'cases':len(cases),'scope':__doc__.strip(),'original_rvas':['0x25ca8c8','0x25ca8b0','0x25c948c'],'files':[{'path':p,'sha256':hashlib.sha256((PROJECT/p).read_bytes()).hexdigest()}for p in sources+['SourceCatalog/verify_dialog_clear.py']]}
(PROJECT/'dialog_clear_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
