"""Compare native ItemModel.RecycleView against the actual common runtime kernel.

Native virtual model/view callbacks, Unity-object comparison and factory recycle
are typed ports. HasView publication, destroyed/null view handling, hidden-root,
fill-mask/reset ordering and failures execute from original ARM64. Concrete
renderers, object-pool internals and all unimplemented item classes are outside
this differential; formal Unity exit tests bind the current six view types.
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
model,view,root,factory,model_class,view_class=[0x10001000+i*0x1000 for i in range(6)]
metadata=0x10200000;values=[];trace=[];calls=0;fault=False;recycled=0
def fail():
 global fault
 fault=True;u.reg_write(r.UC_ARM64_REG_PC,0x20000000)
def step(label):
 global calls
 trace.append(label);calls+=1
 if calls==values[6]:fail();return False
 return True
def hook(uc,address,size,unused):
 global metadata,recycled
 a,b=x(0),x(1)
 if address==0xffd960:put(a,metadata);put(metadata+0xe0,1,4);metadata+=0x200
 elif address in(0xffd90c,0xffda64):pass
 elif address==0xffda88:fail();return
 elif address==0x3e4d92c:
  returned(int(a!=0 and values[1]==1));return
 elif address==0x260a1fc:
  assert a==factory and b==view
  if not step('recycle'):return
  recycled+=1
 elif address==0x20000610:
  assert a==model
  if not step('has'):return
  returned(read(model+0x14,1));return
 elif address==0x20000620:
  assert a==model
  if not step('set'):return
  put(model+0x14,b,1)
 elif address==0x20000630:
  assert a==model
  if not step('view'):return
  returned(view if values[1]else 0);return
 elif address==0x20000640:
  if not step('show'):return
  put(root+0x31,0,1)
 elif address==0x20000650:
  if not step('mask'):return
  put(view+0x18,0,1)
 elif address==0x20000660:
  if not step('reset'):return
 else:raise AssertionError(hex(address))
 returned()
for a in(0xffd960,0xffd90c,0xffda64,0xffda88,0x3e4d92c,0x260a1fc,*range(0x20000610,0x20000670,0x10)):u.hook_add(UC_HOOK_CODE,hook,begin=a,end=a)
cases=list(itertools.product(range(2),range(3),range(2),range(2),range(2),range(2),range(9),(1,2)));expected=[]
for values in cases:
 trace=[];calls=0;fault=False;recycled=0
 for p in(model,view,root,model_class,view_class):u.mem_write(p,bytes(0x800))
 put(model,model_class);put(view,view_class);put(model+0x14,values[0],1);put(view+0x18,values[3],1);put(root+0x31,values[2],1)
 put(view+0x20,0 if values[4]else root);put(model+0x58,0 if values[5]else factory)
 for offset,fn in((0x358,0x20000610),(0x368,0x20000620),(0x418,0x20000630)):put(model_class+offset,fn)
 for offset,fn in((0x258,0x20000640),(0x278,0x20000650),(0x378,0x20000660)):put(view_class+offset,fn)
 for repeat in range(values[7]):
  for i in range(8):u.reg_write(getattr(r,'UC_ARM64_REG_X'+str(i)),model if i==0 else 0)
  u.reg_write(r.UC_ARM64_REG_SP,0x103ff000);u.reg_write(r.UC_ARM64_REG_LR,0x20000000);u.emu_start(0x2b46db0,0x20000000,count=10000)
  assert u.reg_read(r.UC_ARM64_REG_PC)==0x20000000
  if fault:break
 expected.append(str(int(fault))+'#'+','.join(map(str,(read(model+0x14,1),read(root+0x31,1),read(view+0x18,1),recycled)))+'#'+';'.join(trace))
mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
sources=['SourceCatalog/ItemViewRecycleHarness.cs','Assets/Foundation/Runtime/OriginalItemViewRecycle.cs']
with tempfile.TemporaryDirectory()as directory:
 exe=Path(directory)/'Recycle.exe';command=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',*['-r:'+str(mono/'lib/mono/4.5'/p)for p in('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),*[str(PROJECT/p)for p in sources]]
 result=subprocess.run(command,capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(','.join(map(str,v))for v in cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 actual=result.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected)
 for i,(a,b)in enumerate(zip(actual,expected)):assert a==b,(i,cases[i],a,b)
report={'passed':True,'cases':len(cases),'scope':__doc__.strip(),'original_rvas':['0x2b46db0'],'files':[{'path':p,'sha256':hashlib.sha256((PROJECT/p).read_bytes()).hexdigest()}for p in sources+['SourceCatalog/verify_item_view_recycle.py']]}
(PROJECT/'item_view_recycle_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
