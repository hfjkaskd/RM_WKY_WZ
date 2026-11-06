"""Execute original ARM64 CellGridManager.CreateItems and GridIterator traversal.

Data-provider, item-model creation, cell mediation, saved-cell/nullable/value
decode, item-save loading and initialized-event callbacks are explicit ports.
The original loops, skip gates, late null failures, signed short restore,
event order and repeated iterator reset execute from ELF. Actual production
mediation/models/prefab views are separately checked in the unchanged scene;
this does not prove all item/static/restore implementations or mobile parity.
"""
from pathlib import Path
import sys,json,hashlib,itertools,random,subprocess,tempfile
PROJECT=Path(__file__).resolve().parents[1];BASE=PROJECT.parent
sys.path.insert(0,str(BASE/'tools/python'))
from elftools.elf.elffile import ELFFile
from elftools.elf.relocation import RelocationSection
from unicorn import Uc,UC_ARCH_ARM64,UC_MODE_ARM,UC_HOOK_CODE
import unicorn.arm64_const as r
u=Uc(UC_ARCH_ARM64,UC_MODE_ARM);u.mem_map(0,0x6000000);u.mem_map(0x10000000,0x400000);u.mem_map(0x20000000,0x3000)
def put(p,v,n=8):u.mem_write(p,(int(v)&((1<<(8*n))-1)).to_bytes(n,'little'))
def read(p,n=8):return int.from_bytes(u.mem_read(p,n),'little')
def si(v):return (int(v)&0xffffffff)-0x100000000 if int(v)&0x80000000 else int(v)&0xffffffff
def short(v):return (int(v)&65535)-65536 if int(v)&32768 else int(v)&65535
def x(i):return u.reg_read(getattr(r,'UC_ARM64_REG_X'+str(i)))
def returned(v=0):u.reg_write(r.UC_ARM64_REG_X0,v&0xffffffffffffffff);u.reg_write(r.UC_ARM64_REG_PC,u.reg_read(r.UC_ARM64_REG_LR))
with(BASE.parent/'RoyalMatch_20260929/apk_unpacked/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so').open('rb')as stream:
 elf=ELFFile(stream)
 for s in elf.iter_segments():
  if s['p_type']=='PT_LOAD':u.mem_write(s['p_vaddr'],s.data())
 for s in elf.iter_sections():
  if isinstance(s,RelocationSection):
   for e in s.iter_relocations():
    if e['r_info_type']==1027:put(e['r_offset'],e['r_addend'])
manager,provider,provider_class,strategy,strategy_class,factory,creator,restore,loader,entry,event=[0x10001000+i*0x1000 for i in range(11)]
cells=[0x10010000+i*0x1000 for i in range(4)];mediators=[0x10020000+i*0x1000 for i in range(4)]
metadata=0x10200000;trace=[];calls=0;fault=False;v=[];states=[];created=[];next_id=100;saved_index=0
def fail():
 global fault
 fault=True;u.reg_write(r.UC_ARM64_REG_PC,0x20000000)
def step(label):
 global calls
 trace.append(label);calls+=1
 if calls==v[3]:fail();return False
 return True
def result(label,value=0):
 if step(label):returned(value)
def item_index(p):return -1 if not p else si(read(p+0x10,4))
def hook(uc,address,size,unused):
 global metadata,next_id,saved_index
 a,b,c,d=x(0),x(1),x(2),x(3)
 if address==0xffd960:put(a,metadata);put(metadata+0xe0,1,4);metadata+=0x200;returned();return
 if address in(0xffd90c,0xffda64):returned();return
 if address in(0xffda88,0xffda90):fail();return
 if address==0x20001000:assert a==strategy;result('first',cells[0]if v[1]else 0);return
 if address==0x20001010:
  assert a==strategy;i=cells.index(b);result('next,'+str(i),cells[i+1]if i+1<v[1]else 0);return
 if address==0xfcd934:
  assert a==provider and c in(1,2);put(entry,0x20001100+c*0x10);put(entry+8,0);returned(entry);return
 if address in(0x20001110,0x20001120):
  assert a==provider and c==b+10
  result(('tile,'if address==0x20001110 else'original,')+str(b),v[(8 if address==0x20001110 else 12)+b]);return
 if address==0x23ebf0c:
  assert a==creator;i=cells.index(d)
  if not step('create,'+str(i)+','+str(si(b))+','+str(si(c))):return
  ident=next_id;next_id+=1
  if i==v[4]:returned();return
  p=0x10100000+(ident-100)*0x100;put(p+0x10,ident,4);created.append([ident,999]);returned(p);return
 if address==0x3448578:
  i=mediators.index(a)
  if step('clear,'+str(i)):states[i]=[-1,-1,-1];returned()
  return
 if address==0x34485f4:
  i=mediators.index(a);ident=item_index(b)
  if step('set,'+str(i)+','+str(ident)):states[i]=[ident,ident,ident];returned()
  return
 if address==0x23df9e4:returned(0 if v[2]==5 else restore);return
 if address==0x23e2df8:assert a==loader;saved_index=si(b);returned(1);u.reg_write(r.UC_ARM64_REG_X1,0);return
 if address==0x1915578:u.mem_write(x(8),bytes(24));returned();return
 if address==0x2e04a74:
  if v[2]==7:fail();return
  returned(1);u.reg_write(r.UC_ARM64_REG_X1,0);return
 if address==0x19191dc:result('saved,'+str(saved_index),v[16+saved_index]);return
 if address==0x2b48674:
  ident=item_index(a)
  if step('load,'+str(ident)+','+str(short(b))):next(item for item in created if item[0]==ident)[1]=short(b);returned()
  return
 if address==0x20001200:
  if not step('event'):return
  if v[5]==2:fail();return
  if v[5]==3:put(manager+0x20,0)
  returned();return
 raise AssertionError(hex(address))
for a in(0xffd960,0xffd90c,0xffda64,0xffda88,0xffda90,0x20001000,0x20001010,0xfcd934,0x20001110,0x20001120,0x23ebf0c,0x3448578,0x34485f4,0x23df9e4,0x23e2df8,0x1915578,0x2e04a74,0x19191dc,0x2b48674,0x20001200):u.hook_add(UC_HOOK_CODE,hook,begin=a,end=a)
def call():
 global fault
 fault=False
 for i in range(9):u.reg_write(getattr(r,'UC_ARM64_REG_X'+str(i)),manager if i==0 else 0 if i==1 and v[2]==4 else provider if i==1 else v[0]if i==2 else 0)
 u.reg_write(r.UC_ARM64_REG_SP,0x103ff000);u.reg_write(r.UC_ARM64_REG_LR,0x20000000);u.emu_start(0x320f5a0,0x20000000,count=10000)
 assert u.reg_read(r.UC_ARM64_REG_PC)==0x20000000,hex(u.reg_read(r.UC_ARM64_REG_PC))
def native(values):
 global v,trace,calls,states,created,next_id
 v=values;trace=[];calls=0;states=[[i,i,i]for i in range(4)];created=[];next_id=100
 for p in(manager,provider,provider_class,strategy,strategy_class,factory,restore):u.mem_write(p,bytes(0x300))
 put(provider,provider_class);put(strategy,strategy_class);put(strategy_class+0x178,0x20001000);put(strategy_class+0x188,0x20001010)
 put(manager+0xb8,strategy);put(manager+0xc8,cells[0]if v[1]else 0);put(manager+0x48,0 if v[2]in(1,2)else factory);put(factory+0x38,0 if v[2]==3 else creator)
 put(restore+0x20,0 if v[2]==6 else loader)
 if v[5]:put(manager+0x20,event);put(event+0x18,0x20001200)
 for i,p in enumerate(cells):put(p+0x10,mediators[i]);put(p+0x18,i,4);put(p+0x1c,i+10,4);put(p+0xa0,v[20+i],4)
 for _ in range(v[6]):
  call()
  if fault:break
 return str(int(fault))+'#'+'/'.join(','.join(map(str,s))for s in states)+'#'+'/'.join(','.join(map(str,s))for s in created)+'#'+';'.join(trace)
cases=[]
for origin,length,missing,null_item,event_mode in itertools.product((0,1,2,3),range(5),range(8),(-1,0,2),range(4)):
 cases.append([origin,length,missing,0,null_item,event_mode,2,0,1,0,13,21,14,1,0,21,-32768,32767,65535,65536,0,1,2,1])
for origin,stage in itertools.product((1,3),range(1,55)):
 cases.append([origin,4,0,stage,-1,1,2,0,1,2,3,21,14,1,0,21,32768,-1,1,32767,1,1,2,1])
rng=random.Random(0x320f5a0)
for _ in range(400):
 cases.append([rng.choice((1,3)),rng.randrange(5),rng.randrange(8),rng.randrange(55),rng.randrange(-1,4),rng.randrange(4),rng.randrange(1,4),0,*[rng.choice((0,1,2,13,14,21,-1))for _ in range(8)],*[si(rng.getrandbits(32))for _ in range(4)],*[rng.randrange(3)for _ in range(4)]])
expected=[native(case)for case in cases]
mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
sources=['SourceCatalog/GridItemPopulationHarness.cs','Assets/Foundation/Runtime/OriginalGridItemPopulation.cs','Assets/Foundation/Runtime/OriginalGridIterator.cs','Assets/Foundation/Runtime/OriginalGridItemPopulationData.cs']
with tempfile.TemporaryDirectory()as directory:
 exe=Path(directory)/'Population.exe';command=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',*['-r:'+str(mono/'lib/mono/4.5'/p)for p in('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),*[str(PROJECT/p)for p in sources]]
 result=subprocess.run(command,capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(','.join(map(str,c))for c in cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 actual=result.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected)
 for i,(a,b)in enumerate(zip(actual,expected)):assert a==b,(i,cases[i],a,b)
report={'passed':True,'cases':len(cases),'scope':__doc__.strip(),'original_rvas':['0x320f5a0','0x318ec2c','0x318ecc8'],'files':[{'path':p,'sha256':hashlib.sha256((PROJECT/p).read_bytes()).hexdigest()}for p in sources+['SourceCatalog/verify_grid_item_population.py']]}
(PROJECT/'grid_item_population_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
