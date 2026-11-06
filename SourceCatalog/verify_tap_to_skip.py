"""Actual ARM64 TapToSkip guards, captured level owner, show-once and dispatch state vs runtime C#.

Unity Button/Collider and TMP rendering, localization/tweening, account/context
lookup and RemainingMovesManager dispatch are typed boundaries. Original
state writes, first-level gate, signed text-level limit, retained reference,
callback ordering and failure mutations execute from the ELF. This is not
physical pointer input, full skip visual or production account composition.
"""
from pathlib import Path
import hashlib,itertools,json,random,subprocess,sys,tempfile
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
with(BASE.parent/'RoyalMatch_20260929/apk_unpacked/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so').open('rb')as stream:
 elf=ELFFile(stream)
 for s in elf.iter_segments():
  if s['p_type']=='PT_LOAD':u.mem_write(s['p_vaddr'],s.data())
 for s in elf.iter_sections():
  if isinstance(s,RelocationSection):
   for e in s.iter_relocations():
    if e['r_info_type']==1027:put(e['r_offset'],e['r_addend'])
tap,user,session,level,alternate,button,text,userclass,statics,manager=[0x10001000+i*0x1000 for i in range(10)]
metadata=0x10200000;trace=[];fault=False;missing=0;has=0
def fail():
 global fault
 fault=True;u.reg_write(r.UC_ARM64_REG_PC,0x20000000)
def hook(uc,address,size,unused):
 global metadata
 a,b=x(0),x(1)
 if address==0xffd960:put(a,metadata);put(metadata+0xe0,1,4);metadata+=0x200
 elif address in(0xffd90c,0xffda64):pass
 elif address in(0xffda88,0xffda90):fail();return
 elif address==0x18d08b8:trace.append('current');return # Execute original owner reads and field assignment.
 elif address==0x3e4944c:trace.append(('interaction,'if a==button else'text,')+str(b))
 elif address==0x32072dc:assert a==33;trace.append('has');returned(has);return
 elif address==0x260dea4:assert a==33;returned(0 if missing==8 else manager);return
 elif address==0x2183378:
  trace.append('dispatch')
  if missing==6:fail();return
 elif address==0x18d0b64:trace.append('arrange')
 elif address==0x18d09e4:
  trace.append('visual')
  if missing==7:fail();return
  u.reg_write(r.UC_ARM64_REG_PC,0x18d0b50);return # Typed TMP/localization/tween presentation boundary; keep native epilogue.
 else:raise AssertionError(hex(address))
 returned()
for address in(0xffd960,0xffd90c,0xffda64,0xffda88,0xffda90,0x18d08b8,0x3e4944c,0x32072dc,0x260dea4,0x2183378,0x18d0b64,0x18d09e4):u.hook_add(UC_HOOK_CODE,hook,begin=address,end=address)
def call(address,*args):
 global fault
 fault=False
 for i in range(8):u.reg_write(getattr(r,'UC_ARM64_REG_X'+str(i)),args[i]if i<len(args)else 0)
 u.reg_write(r.UC_ARM64_REG_SP,0x103ff000);u.reg_write(r.UC_ARM64_REG_LR,0x20000000)
 u.emu_start(address,0x20000000,count=10000);assert u.reg_read(r.UC_ARM64_REG_PC)==0x20000000
 return x(0)
levels=(-2147483648,-1,0,1,2,30,31,2147483647)
cases=[]
for number,missing,has in itertools.product(levels,range(9),range(2)):
 for sequence in('H;E;T;T;C;C','E;Q;H;T','C;E;C','H;T','E;S1;A1;T;E;T','H;E;T;H;E;T'):
  cases.append(f'{number},{missing},{has};'+sequence)
rng=random.Random(0x18d0ecc)
for _ in range(500):
 commands=[rng.choice(('H','E','T','C','Q','S0','S1','N1','N2','N31','A1','A30','M0','M1'))for _ in range(15)]
 cases.append(f'{rng.choice(levels)},0,{rng.randrange(2)};'+';'.join(commands))
expected=[]
for case in cases:
 fields,*commands=case.split(';');number,missing,has=map(int,fields.split(','));trace=[]
 for p in(tap,user,session,level,alternate,userclass,statics):u.mem_write(p,bytes(0x200))
 put(read(0x53bb4e0,8),userclass);put(userclass+0xe0,1,4);put(userclass+0xb8,statics);put(statics,0 if missing==1 else user)
 put(user+0x38,0 if missing==2 else session);put(session+0x18,0 if missing==3 else level)
 put(level+0x14,number,4);put(alternate+0x14,2,4);put(tap+0x28,0 if missing==4 else button);put(tap+0x20,0 if missing==5 else text)
 put(0x573963a,1,1)
 for command in commands:
  c=command[0]
  if c in'HETCQ':
   result=call({'H':0x18d084c,'E':0x18d088c,'T':0x18d0970,'C':0x18d0ecc,'Q':0x18d0cec}[c],tap)
   if fault:break
   if c=='Q':trace.append('result,'+str(result))
  elif c in'NA':put((level if c=='N'else alternate)+0x14,int(command[1:]),4)
  elif c=='S':put(session+0x18,0 if missing==3 else level if command[1]=='0'else alternate)
  elif c=='M':has=int(command[1])
 expected.append(str(int(fault))+'#'+str(read(tap+0x30))+','+str(read(tap+0x40,1))+'#'+';'.join(trace))
mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
sources=['SourceCatalog/TapToSkipHarness.cs','Assets/Foundation/Runtime/OriginalTapToSkip.cs']
with tempfile.TemporaryDirectory()as directory:
 exe=Path(directory)/'Tap.exe';command=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',*['-r:'+str(mono/'lib/mono/4.5'/p)for p in('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),*[str(PROJECT/p)for p in sources]]
 result=subprocess.run(command,capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 actual=result.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected)
 for index,(a,b)in enumerate(zip(actual,expected)):assert a==b,(index,cases[index],a,b)
report={'passed':True,'cases':len(cases),'scope':__doc__.strip(),'original_rvas':['0x18d084c','0x18d088c','0x18d0970','0x18d0ecc','0x18d0cec'],
 'files':[{'path':p,'sha256':hashlib.sha256((PROJECT/p).read_bytes()).hexdigest()}for p in sources]}
(PROJECT/'tap_to_skip_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
