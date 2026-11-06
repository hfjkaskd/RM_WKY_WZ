"""Compare actual ARM64 CellModel.Reset and FillingCellModel.Reset with runtime code.

Factory/view recycle, mediation Reset, static-model Reset and queue Dequeue are
typed ports. Callback mutation/throws, target lookup order, orphan handling,
cell-view publication and captured queue count execute from the original ELF.
Only the orphan diagnostic block is skipped. This does not prove all concrete
item/static models, grid-manager subscriptions or the full level factory.
"""
from pathlib import Path
import sys,json,hashlib,itertools,subprocess,tempfile
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
cell,view,mediator,statics,queue,factory,klass=[0x10001000+i*0x1000 for i in range(7)]
items=[0]+[0x10010000+i*0x1000 for i in range(4)]
metadata=0x10200000;values=[];trace=[];calls=0;fault=False;pending=[];appended=False
def itemid(p):return items.index(p)if p else 0
def step(label):
 global calls,fault
 trace.append(label+','+str(int(read(cell+0xb8)!=0))+','+str(itemid(read(mediator+0x20)))+','+str(itemid(read(cell+0xe0))))
 calls+=1
 if calls==values[6]:fault=True;u.reg_write(r.UC_ARM64_REG_PC,0x20000000);return False
 return True
def hook(uc,address,size,unused):
 global metadata,appended
 a,b=x(0),x(1)
 if address==0xffd960:put(a,metadata);put(metadata+0xe0,1,4);metadata+=0x200
 elif address in(0xffd90c,0xffda64):pass
 elif address in(0xffda88,0xffda90):raise AssertionError('Unexpected required-null native failure')
 elif address==0x260dea4:assert a==1;returned(factory);return
 elif address==0x3e4d92c:returned(int(a!=0));return
 elif address==0x260a1fc:
  assert a==factory and b==view
  if not step('cell'):return
  if values[5]==1:put(mediator+0x20,items[2])
 elif address==0x3448800:assert a==mediator;returned(int(read(mediator+0x20)!=0));return
 elif address==0x34487e4:assert a==mediator;returned(read(mediator+0x20));return
 elif address==0x3448824:
  assert a==mediator
  if not step('mediation'):return
  put(mediator+0x20,0)
 elif address==0x3441394:
  assert a==statics
  if not step('static'):return
 elif address==0x2b45c94:returned(int(read(a+0x20)!=0));return
 elif address==0x343d788:u.reg_write(r.UC_ARM64_REG_PC,0x343d83c);return
 elif address==0x20000610:
  identity=itemid(a);assert identity!=0
  if not step('item'+str(identity)):return
  if values[5]==2:put(mediator+0x20,0)
  if values[5]==3 and identity==2 and not appended:pending.append(items[4]);put(queue+0x20,len(pending),4);appended=True
 elif address==0x1162684:
  assert a==queue and pending
  item=pending.pop(0);put(queue+0x20,len(pending),4);returned(item);return
 else:raise AssertionError(hex(address))
 returned()
for a in(0xffd960,0xffd90c,0xffda64,0xffda88,0xffda90,0x260dea4,0x3e4d92c,0x260a1fc,0x3448800,0x34487e4,0x3448824,0x3441394,0x2b45c94,0x343d788,0x20000610,0x1162684):u.hook_add(UC_HOOK_CODE,hook,begin=a,end=a)
cases=list(itertools.product(range(2),range(2),range(2),(-1,0,1),range(3),range(4),range(7)))
expected=[]
for values in cases:
 trace=[];calls=0;fault=False;appended=False;pending=[]
 for p in(cell,mediator,klass,queue):u.mem_write(p,bytes(0x800))
 put(cell+0xb8,view if values[1]else 0);put(cell+0x10,mediator);put(cell+0xc0,statics);put(mediator+0x20,items[3]if values[2]else 0)
 if values[0]:
  put(cell+0xe0,items[1]if values[3]!=-1 else 0);put(cell+0xf0,queue)
  pending=[items[i+2]for i in range(values[4])];put(queue+0x20,len(pending),4)
 put(klass+0x5a8,0x20000610)
 for p in items[1:]:put(p,klass);put(p+0x38,p+0x200);put(p+0x220,1 if values[3]==0 else 0)
 for i in range(8):u.reg_write(getattr(r,'UC_ARM64_REG_X'+str(i)),cell if i==0 else 0)
 u.reg_write(r.UC_ARM64_REG_SP,0x103ff000);u.reg_write(r.UC_ARM64_REG_LR,0x20000000)
 u.emu_start(0x343d6fc if values[0]else 0x319d5a0,0x20000000,count=10000)
 assert u.reg_read(r.UC_ARM64_REG_PC)==0x20000000
 expected.append(str(int(fault))+'#'+','.join(map(str,(int(read(cell+0xb8)!=0),itemid(read(mediator+0x20)),itemid(read(cell+0xe0)))))+'#'+','.join(str(itemid(p))for p in pending)+'#'+';'.join(trace))
mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
sources=['SourceCatalog/CellResetHarness.cs','Assets/Foundation/Runtime/OriginalCellReset.cs','Assets/Foundation/Runtime/OriginalFillingCell.cs']
with tempfile.TemporaryDirectory()as directory:
 exe=Path(directory)/'Reset.exe';command=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',*['-r:'+str(mono/'lib/mono/4.5'/p)for p in('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),*[str(PROJECT/p)for p in sources]]
 result=subprocess.run(command,capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(','.join(map(str,v))for v in cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 actual=result.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected)
 for i,(a,b)in enumerate(zip(actual,expected)):assert a==b,(i,cases[i],a,b)
report={'passed':True,'cases':len(cases),'scope':__doc__.strip(),'original_rvas':['0x319d5a0','0x343d6fc'],'files':[{'path':p,'sha256':hashlib.sha256((PROJECT/p).read_bytes()).hexdigest()}for p in sources+['SourceCatalog/verify_cell_reset.py']]}
(PROJECT/'cell_reset_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
