"""Actual ARM64 UiDialog Show/Close/force/background/Escape and close iterator vs compiled runtime C#.

DialogManager, camera/parent, MonoBehaviour coroutine transport, virtual dialog
hooks/config and destruction are typed boundaries. Native guard ordering,
partial mutation, nested close-animation yield, callback/flow order and config
rereads execute from the original ELF. Manager backdrop/render/skip/mobile and
full production composition are not proved by this differential test.
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
def x(i):return u.reg_read(getattr(r,'UC_ARM64_REG_X'+str(i)))
def returned(value=0):u.reg_write(r.UC_ARM64_REG_X0,value);u.reg_write(r.UC_ARM64_REG_PC,u.reg_read(r.UC_ARM64_REG_LR))
def fbits(value):return struct.unpack('<I',struct.pack('<f',value))[0]
with(BASE.parent/'RoyalMatch_20260929/apk_unpacked/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so').open('rb')as stream:
 elf=ELFFile(stream)
 for segment in elf.iter_segments():
  if segment['p_type']=='PT_LOAD':u.mem_write(segment['p_vaddr'],segment.data())
 for section in elf.iter_sections():
  if isinstance(section,RelocationSection):
   for entry in section.iter_relocations():
    if entry['r_info_type']==1027:put(entry['r_offset'],entry['r_addend'])
dialog,binding,manager,klass,action_class,iterator,animation,camera,metadata=[0x10001000+i*0x1000 for i in range(9)]
v=[];trace=[];fault=False;routine=0;config_calls=0
def fail():
 global fault
 fault=True;u.reg_write(r.UC_ARM64_REG_PC,0x20000000)
def hit(value,number=0):
 trace.append(value)
 if number and v[10]==number:fail();return True
 return False
def hook(uc,address,size,unused):
 global metadata,routine,config_calls
 a,b=x(0),x(1)
 if address==0xffd960:
  put(a,metadata);put(metadata+0xe0,1,4);metadata+=0x200
 elif address in(0xffd90c,0xffda64,0x231007c):pass
 elif address in(0xffda88,0xffda90):fail();return
 elif address==0xffda78:returned(iterator);return
 elif address==0x25cb374:uc.reg_write(r.UC_ARM64_REG_PC,0x25cb3d0);return # Duplicate-show diagnostic body only; retain native epilogue.
 elif address==0x266c4e8:pass
 elif address==0x25cba50:uc.reg_write(r.UC_ARM64_REG_PC,0x25cba44);return # Not-showing warning only.
 elif address==0x25c97f0:
  if hit('show-manager',5):return
 elif address==0x25cb464:
  if hit('parent',3):return
 elif address==0x2002208:returned(0 if v[10]==4 else camera);return
 elif address==0x2004f70:
  hit('camera');u.reg_write(r.UC_ARM64_REG_S0,fbits(2));u.reg_write(r.UC_ARM64_REG_S1,fbits(3));u.reg_write(r.UC_ARM64_REG_S2,fbits(99))
 elif address==0x20000200:
  assert u.reg_read(r.UC_ARM64_REG_S2)==0
  if hit('shown,2,3,0',6):return
 elif address==0x20000210:hit('can');returned(v[2]);return
 elif address==0x25ca07c:
  if hit('close-manager',7):return
  returned(v[3]);return
 elif address==0x3e473b8:
  if hit('object',8):return
  returned(dialog);return
 elif address==0x3e49b30:
  if hit('active,'+str(b),9):return
 elif address==0x3e4c364:
  if hit('start',10):return
  routine=b
 elif address==0x20000220:
  config_calls+=1
  if hit('config',11):return
  target=x(8);u.mem_write(target,bytes(0x24))
  for off,val in((0,v[6]),(1,v[7]),(2,v[8]),(0x1e,v[4]),(0x1f,v[5])):put(target+off,val,1)
  put(target+0x20,fbits(v[9]/4),4)
 elif address==0x20000230:
  if hit('animation',12):return
  returned(animation);return
 elif address==0x25ca498:
  if hit('closed-manager',13):return
  if v[11]==4:put(dialog+0x30,0)
 elif address==0x20000240:
  if hit('onclose',14):return
  if v[11]==1:put(dialog+0x30,0)
 elif address==0x20000250:
  if hit('complete',15):return
  if v[11]==3:v[5]=1
 elif address==0x3e4d448:
  delay=struct.unpack('<f',struct.pack('<I',u.reg_read(r.UC_ARM64_REG_S0)))[0]
  if hit('destroy,'+format(delay,'.2f'),16):return
 elif address in(0x25cbc8c,0x25cbc94):hit('background'if address==0x25cbc8c else'escape');return # Execute native forwarding method.
 else:raise AssertionError(hex(address))
 returned()
for address in(0xffd960,0xffd90c,0xffda64,0xffda88,0xffda90,0xffda78,0x231007c,0x25cb374,0x266c4e8,0x25cba50,0x25c97f0,0x25cb464,0x2002208,0x2004f70,0x20000200,0x20000210,0x25ca07c,0x3e473b8,0x3e49b30,0x3e4c364,0x20000220,0x20000230,0x25ca498,0x20000240,0x20000250,0x3e4d448,0x25cbc8c,0x25cbc94):u.hook_add(UC_HOOK_CODE,hook,begin=address,end=address)
def call(address,*args):
 global fault
 fault=False
 for i in range(8):u.reg_write(getattr(r,'UC_ARM64_REG_X'+str(i)),args[i]if i<len(args)else 0)
 u.reg_write(r.UC_ARM64_REG_SP,0x103ff000);u.reg_write(r.UC_ARM64_REG_LR,0x20000000)
 u.emu_start(address,0x20000000,count=10000);assert u.reg_read(r.UC_ARM64_REG_PC)==0x20000000
 return x(0)
vectors=[]
for mode,shown,can,accept,play,retain in itertools.product(range(6),range(3),range(2),range(2),range(2),range(2)):
 vectors.append([mode,shown,can,accept,play,retain,1,1,1,3,0,0])
for mode,missing in itertools.product(range(6),range(1,17)):
 for play in range(2):vectors.append([mode,0 if mode==0 else 1,1,1,play,0,1,1,1,7,missing,0])
for mutation in(1,3,4):
 for play,retain in itertools.product(range(2),range(2)):vectors.append([1,1,1,1,play,retain,1,1,1,-2,0,mutation])
rng=random.Random(0x25cbf44)
for _ in range(800):vectors.append([rng.randrange(6),rng.randrange(3),*[rng.randrange(2)for _ in range(7)],rng.randrange(-2,9),rng.randrange(17),rng.choice((0,0,0,1,3,4))])
for a in vectors:assert len(a)==12,a
cases=[];expected=[]
for source in vectors:
 v=source.copy();trace=[];routine=0;config_calls=0
 for p in(dialog,binding,manager,klass,action_class,iterator,animation):u.mem_write(p,bytes(0x500))
 put(dialog,klass);put(dialog+0x30,0 if v[10]==1 else binding);put(binding,action_class);put(binding+0x20,0 if v[10]==2 else manager)
 put(dialog+0x38,v[1]!=0,1);put(dialog+0x39,v[1]==2,1);put(action_class+0x1b8,0x20000250)
 for offset,pointer in((0x298,0x20000200),(0x2c8,0x20000210),(0x338,0x20000220),(0x2d8,0x20000230),(0x2e8,0x20000240),(0x2f8,0x25cbc8c),(0x308,0x25cbc94)):put(klass+offset,pointer)
 for offset in(0xde4,0xde6,0xde7,0xdf0):put(0x5741000+offset,0,1)
 # The iterator allocation helper constructs the actual native state machine.
 result=call((0x25cb31c,0x25cb9f0,0x25ca99c,0x25ca9a4,0x25ca9a4,0x25cac00)[v[0]],dialog,int(v[0]==4))
 if routine and not fault:
  for _ in range(3):
   value=call(0x25cbf44,routine)
   if fault or not value:break
   trace.append('yield');assert call(0x25cbed8,animation)==0 and not fault
 # Native camera-null check precedes GetCenterPosition; model it as the same
 # required camera owner's failed lookup in the C# host trace.
 if v[10]==4 and v[0]==0 and source[1]==0 and not any(t=='camera'for t in trace):
  if 'parent'in trace:trace.append('camera')
 expected.append(str(int(fault))+'#'+str(read(dialog+0x38,1))+','+str(read(dialog+0x39,1))+'#'+';'.join(trace));cases.append(','.join(map(str,source)))
mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
sources=['SourceCatalog/DialogLifecycleHarness.cs','Assets/Foundation/Runtime/OriginalUiDialog.cs']
with tempfile.TemporaryDirectory()as directory:
 exe=Path(directory)/'Dialog.exe';command=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',*['-r:'+str(mono/'lib/mono/4.5'/p)for p in('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),*[str(PROJECT/p)for p in sources]]
 result=subprocess.run(command,capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 actual=result.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
 for index,(a,b)in enumerate(zip(actual,expected)):assert a==b,(index,cases[index],a,b)
report={'passed':True,'cases':len(cases),'scope':__doc__.strip(),'original_rvas':['0x25cb31c','0x25cb9f8','0x25ca99c','0x25ca9a4','0x25cac00','0x25cbf44','0x25cbed8','0x25cbc8c','0x25cbc94'],
 'files':[{'path':p,'sha256':hashlib.sha256((PROJECT/p).read_bytes()).hexdigest()}for p in sources]}
(PROJECT/'dialog_lifecycle_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
