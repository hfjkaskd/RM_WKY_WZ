"""Actual ARM64 DialogManager registration, close handoff, close removal and touch sorting vs runtime C#.

Managed dictionary/enumerator operations, Unity sorting/view calls, background,
scene touch and events are typed boundaries; original manager control flow,
ID/sorting counters, dictionary mutation order and event timing execute in ELF.
Diagnostic formatting and empty-sequence exception transport are omitted.
Destroyed Unity-object equality, backdrop tween rendering, app/mobile binding
and all other DialogManager entry points remain outside this check.
"""
from pathlib import Path
import hashlib,itertools,json,random,struct,subprocess,sys,tempfile
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
def returned(value=0):u.reg_write(r.UC_ARM64_REG_X0,value);u.reg_write(r.UC_ARM64_REG_PC,u.reg_read(r.UC_ARM64_REG_LR))
with(BASE.parent/'RoyalMatch_20260929/apk_unpacked/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so').open('rb')as stream:
 elf=ELFFile(stream)
 for s in elf.iter_segments():
  if s['p_type']=='PT_LOAD':u.mem_write(s['p_vaddr'],s.data())
 for s in elf.iter_sections():
  if isinstance(s,RelocationSection):
   for e in s.iter_relocations():
    if e['r_info_type']==1027:put(e['r_offset'],e['r_addend'])
owner,dictionary,klass,background,tooltip,event_shown,event_closed,binding,metadata=[0x10001000+i*0x1000 for i in range(9)]
metadata=0x10200000
objects=[0x10020000+i*0x1000 for i in range(3)]
table={};trace=[];enumerators={};fault=False;pending=0;offset=0
def fail():
 global fault
 fault=True;u.reg_write(r.UC_ARM64_REG_PC,0x20000000)
def ident(pointer):return objects.index(pointer)
def hook(uc,address,size,unused):
 global metadata,pending,offset
 a,b,c=x(0),x(1),x(2)
 if address==0xffd960:put(a,metadata);put(metadata+0xe0,1,4);metadata+=0x200
 elif address in(0xffd90c,0xffda64,0x2044a78):pass
 elif address in(0xffda88,0xffda90):fail();return
 elif address==0xffda78:returned(binding);return
 elif address==0x1274f28:returned(int(b in table.values()));return
 elif address==0x1274ed0:returned(int(b in table));return
 elif address==0x1274e4c:
  assert b not in table;table[b]=c
 elif address==0x12751a0:returned(int(table.pop(b,None)is not None));return
 elif address==0x1274ccc:returned(len(table));return
 elif address==0x25c9d14:trace.append('disable')
 elif address==0x18d02f8:trace.append('tooltip,'+str(b))
 elif address in(0x20000620,0x20000630):returned();u.reg_write(r.UC_ARM64_REG_X1,0);return
 elif address==0x12d5db4:offset=si(c);returned();u.reg_write(r.UC_ARM64_REG_X1,0);return
 elif address==0x25c9e84:trace.append('sort,'+str(ident(a))+','+str(offset))
 elif address==0x20000640:trace.append('config,'+str(ident(a)));u.mem_write(x(8),bytes(0x24))
 elif address==0x25c7790:trace.append('background,'+str(b)+','+str(offset)+','+str(x(5)))
 elif address==0x25c7e40:trace.append('hide,'+str(b));pending+=1
 elif address==0x25cae50:trace.append('enable')
 elif address==0x20000600:trace.append('shown,'+str(ident(b))+','+str(len(table))+','+str(si(read(owner+0x74))))
 elif address==0x20000610:trace.append('closed,'+str(ident(b))+','+str(len(table))+','+str(si(read(owner+0x74))))
 elif address==0x1275108:enumerators[x(8)]=[list(sorted(table)), -1]
 elif address==0x3174330:
  state=enumerators[a];state[1]+=1;returned(int(state[1]<len(state[0])));return
 elif address==0x31743a4:
  keys,index=enumerators[a];key=keys[index];returned(key);u.reg_write(r.UC_ARM64_REG_X1,table[key]);return
 elif address==0x31743a0:pass
 elif address==0x25ca82c:trace.append('touch,'+str(ident(a))+','+str(si(b)))
 elif address==0x25ca7d0:
  if not table:fail();return
  returned(table[max(table)]);return
 elif address==0x3e3b754:returned(int(a==b));return
 # Preserve each native epilogue/return value while omitting diagnostics only.
 elif address==0x25c98d0:u.reg_write(r.UC_ARM64_REG_PC,0x25c9ce4);return
 elif address==0x25c9b10:u.reg_write(r.UC_ARM64_REG_PC,0x25c9ce4);return
 elif address==0x25ca1f0:u.reg_write(r.UC_ARM64_REG_PC,0x25ca3c4);return
 elif address==0x25ca3cc:u.reg_write(r.UC_ARM64_REG_PC,0x25ca468);return
 elif address==0x25ca5cc:u.reg_write(r.UC_ARM64_REG_PC,0x25ca7a8);return
 else:raise AssertionError(hex(address))
 returned()
for address in(0xffd960,0xffd90c,0xffda64,0xffda88,0xffda90,0xffda78,0x2044a78,0x1274f28,0x1274ed0,0x1274e4c,0x12751a0,0x1274ccc,0x25c9d14,0x18d02f8,0x20000620,0x20000630,0x12d5db4,0x25c9e84,0x20000640,0x25c7790,0x25c7e40,0x25cae50,0x20000600,0x20000610,0x1275108,0x3174330,0x31743a4,0x31743a0,0x25ca82c,0x25ca7d0,0x3e3b754,0x25c98d0,0x25c9b10,0x25ca1f0,0x25ca3cc,0x25ca5cc):u.hook_add(UC_HOOK_CODE,hook,begin=address,end=address)
def call(address,*args):
 global fault
 fault=False
 for i in range(8):u.reg_write(getattr(r,'UC_ARM64_REG_X'+str(i)),args[i]if i<len(args)else 0)
 u.reg_write(r.UC_ARM64_REG_SP,0x103ff000);u.reg_write(r.UC_ARM64_REG_LR,0x20000000)
 u.emu_start(address,0x20000000,count=10000);assert u.reg_read(r.UC_ARM64_REG_PC)==0x20000000
 return x(0)
cases=['S0;C0;D0;H0','S0;S0;C0;C0;D0;H0;H0','S0;S1;S2;C1;D1;C2;D2;C0;D0',
       'S0;S1;D0;S0;D1;D0','C0;D0','S0;D1;D2;D0','S0;C0;H0;S1;D0;C1;D1;H0']
for commands in itertools.product(('S0','S1','C0','C1','D0','D1'),repeat=3):cases.append(';'.join(commands))
rng=random.Random(0x25ca498)
for _ in range(600):cases.append(';'.join(rng.choice('SSSCCD')+str(rng.randrange(3))for _ in range(rng.randrange(3,20))))
expected=[]
for case in cases:
 trace=[];table={};enumerators={};pending=0;offset=0
 for p in(owner,klass,*objects):u.mem_write(p,bytes(0x500))
 for p in objects:put(p,klass)
 put(klass+0x318,0x20000620);put(klass+0x328,0x20000630);put(klass+0x338,0x20000640)
 for off,val in((0x68,dictionary),(0x48,tooltip),(0x50,background),(0x10,event_shown),(0x18,event_closed)):put(owner+off,val)
 for event,handler in((event_shown,0x20000600),(event_closed,0x20000610)):put(event+0x18,handler)
 for command in case.split(';'):
  kind,index=command[0],int(command[1])
  if kind=='H':
   if index>=pending:fault=True;break
   pending-=1;trace.append('enable');continue
  result=call({'S':0x25c97f0,'C':0x25ca07c,'D':0x25ca498}[kind],owner,objects[index])
  if fault:break
  if kind=='C':trace.append('return,'+str(result))
 expected.append(str(int(fault))+'#'+','.join(map(str,[len(table),si(read(owner+0x74)),*[si(read(p+0x2c))for p in objects]]))+'#'+';'.join(trace))
mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
sources=['SourceCatalog/DialogManagerHarness.cs','Assets/Foundation/Runtime/OriginalUiDialog.cs','Assets/Foundation/Runtime/OriginalDialogManager.cs']
with tempfile.TemporaryDirectory()as directory:
 exe=Path(directory)/'DialogManager.exe';command=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',*['-r:'+str(mono/'lib/mono/4.5'/p)for p in('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),*[str(PROJECT/p)for p in sources]]
 result=subprocess.run(command,capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 actual=result.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected)
 for index,(a,b)in enumerate(zip(actual,expected)):assert a==b,(index,cases[index],a,b)
report={'passed':True,'cases':len(cases),'scope':__doc__.strip(),'original_rvas':['0x25c97f0','0x25ca07c','0x25ca498','0x25c9eec','0x25c8be8','0x25c8c44'],
 'files':[{'path':p,'sha256':hashlib.sha256((PROJECT/p).read_bytes()).hexdigest()}for p in sources]}
(PROJECT/'dialog_manager_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
