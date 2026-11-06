"""Differentially execute original ARM64 coin fan/flight arithmetic and Update.

Real native first/final impulse and Update instructions execute from the ELF;
metadata, Unity transforms, level delta, callback/shadow side effects and the
Unity quaternion engine calls are explicit boundaries. This proves scoped math
and callback thresholds, not prefab pixels, goal/accounting or engine parity.
"""
from pathlib import Path
import hashlib,json,math,random,sqlite3,struct,subprocess,sys,tempfile
PROJECT=Path(__file__).resolve().parents[1];BASE=PROJECT.parent
sys.path.insert(0,str(BASE/'tools/python'))
from elftools.elf.elffile import ELFFile
from elftools.elf.relocation import RelocationSection
from unicorn import Uc,UC_ARCH_ARM64,UC_MODE_ARM,UC_HOOK_CODE
import unicorn.arm64_const as r
u=Uc(UC_ARCH_ARM64,UC_MODE_ARM);u.mem_map(0,0x6000000);u.mem_map(0x10000000,0x400000);u.mem_map(0x20000000,0x1000)
def put(p,v,n=8):u.mem_write(p,(v&((1<<(8*n))-1)).to_bytes(n,'little'))
def f32(v):return struct.unpack('<f',struct.pack('<f',v))[0]
def fv(p,v):u.mem_write(p,struct.pack('<'+'f'*len(v),*v))
def sv(i,v):u.reg_write(getattr(r,'UC_ARM64_REG_S'+str(i)),struct.unpack('<I',struct.pack('<f',v))[0])
def sf(i):return struct.unpack('<f',u.reg_read(getattr(r,'UC_ARM64_REG_S'+str(i))).to_bytes(4,'little'))[0]
with (BASE.parent/'RoyalMatch_20260929/apk_unpacked/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so').open('rb')as stream:
 elf=ELFFile(stream)
 for s in elf.iter_segments():
  if s['p_type']=='PT_LOAD':u.mem_write(s['p_vaddr'],s.data())
 for s in elf.iter_sections():
  if isinstance(s,RelocationSection):
   for v in s.iter_relocations():
    if v['r_info_type']==1027:put(v['r_offset'],v['r_addend'])
meta,statics,owner,coin,shadow,tracer,callback=[0x10000000+i*0x1000 for i in range(7)]
put(meta+0xe0,1,4);put(meta+0x132,1,1);put(meta+0xb8,statics)
for i,vec in enumerate(((0,0,0),(1,1,1),(0,1,0),(0,-1,0),(-1,0,0),(1,0,0),(0,0,1),(0,0,-1))):fv(statics+i*12,vec)
put(owner+0x98,tracer);put(coin+0x80,shadow);put(callback+0x18,0x20000100);put(owner+0x68,callback)
current=(0.,0.,0.);start=(0.,0.,0.);delta=.0166;reached=0
def boundary(uc,address,size,unused):
 global current,reached
 x0=uc.reg_read(r.UC_ARM64_REG_X0);value=0;floats=None
 if address==0xffd960:put(x0,meta)
 elif address in(0xffda64,0xffd90c,0x2182ac8,0x2182bc4,0x2184598):pass
 elif address in(0xffda88,0xffda90):raise AssertionError(('Native error',hex(uc.reg_read(r.UC_ARM64_REG_LR))))
 elif address==0x32037e4:floats=(delta,)
 elif address==0x3e4737c:value=x0
 elif address==0x3e4e400:floats=(.01,-.075,0.)
 elif address==0x3e4e2c8:floats=current
 elif address==0x3e4e368:current=tuple(sf(i)for i in range(3))
 elif address==0x20000100:reached+=1
 elif address==0x36270ac:
  angle=f32(f32(sf(0)*f32(math.pi/180))*.5);floats=(0.,0.,f32(math.sin(angle)),f32(math.cos(angle)))
 elif address==0x36266f4:
  z,w=sf(2),sf(3);vx,vy,vz=sf(4),sf(5),sf(6);zz=f32(f32(z*2)*z);wz=f32(f32(z*2)*w)
  floats=(f32(f32(f32(1-zz)*vx)-f32(wz*vy)),f32(f32(wz*vx)+f32(f32(1-zz)*vy)),vz)
 else:raise AssertionError(hex(address))
 if floats is not None:
  for i,v in enumerate(floats):sv(i,v)
 uc.reg_write(r.UC_ARM64_REG_X0,value);uc.reg_write(r.UC_ARM64_REG_PC,uc.reg_read(r.UC_ARM64_REG_LR))
for address in(0xffd960,0xffda64,0xffd90c,0xffda88,0xffda90,0x2182ac8,0x2182bc4,0x2184598,0x32037e4,0x3e4737c,0x3e4e400,0x3e4e2c8,0x3e4e368,0x20000100,0x36270ac,0x36266f4):
 u.hook_add(UC_HOOK_CODE,boundary,begin=address,end=address)
def call(address,args=(),floats=()):
 for i in range(8):u.reg_write(getattr(r,'UC_ARM64_REG_X'+str(i)),args[i]if i<len(args)else 0)
 for i in range(8):sv(i,floats[i]if i<len(floats)else 0.)
 u.reg_write(r.UC_ARM64_REG_SP,0x103ff000);u.reg_write(r.UC_ARM64_REG_LR,0x20000000)
 u.emu_start(address,0x20000000,count=50000);assert u.reg_read(r.UC_ARM64_REG_PC)==0x20000000,hex(u.reg_read(r.UC_ARM64_REG_PC))
 return tuple(sf(i)for i in range(3))
cases=[];expected=[];rng=random.Random(0x21819a8)
for n in range(450):
 x=n%9;y=n//9%8;count=1+n//72%6;index=n//7%count;last=count-1
 start=tuple(f32(rng.uniform(-8,8))for _ in range(3));final=tuple(f32(rng.uniform(-8,8))for _ in range(3));current=tuple(f32(rng.uniform(-8,8))for _ in range(3))
 if n%5==0:current=tuple(f32(v+.02)for v in final)
 elapsed=f32(rng.uniform(0,2));delta=f32(rng.choice((.0083,.0166,.0333,.1,.25)))
 point=(x&0xffffffff)|((y&0xffffffff)<<32)
 first=call(0x21820b4,(owner,point,index,last),start)
 saved=current;current=start
 call(0x2182354,(owner,coin,index,callback),first+final)
 initial=struct.unpack('<3f',u.mem_read(owner+0x30,12));initial=(initial[1],initial[2],initial[0])
 current=saved;fv(owner+0x70,(elapsed,));before=(call(0x2184254,(owner,)),call(0x2184400,(owner,)))
 put(owner+0x3c,1,1);reached=0;call(0x2183fa4,(owner,));after=current
 expected.append((first,initial,*before,after,reached))
 cases.append(','.join(map(str,(x,y,index,last,*start,*final,*saved,elapsed,delta))))
mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
sources=['SourceCatalog/RemainingCoinFlightHarness.cs','Assets/Foundation/Runtime/OriginalRemainingMovesCoinFlight.cs']
with tempfile.TemporaryDirectory()as folder:
 exe=Path(folder)/'Flight.exe';command=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',
  *['-r:'+str(mono/'lib/mono/4.5'/name)for name in('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),*[str(PROJECT/p)for p in sources]]
 result=subprocess.run(command,capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 actual=result.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected)
 maximum=0.
 for n,(line,wanted)in enumerate(zip(actual,expected)):
  values=line.split('|');assert int(values[-1])==wanted[-1],(n,'arrival',cases[n],line,wanted)
  for a,b in zip(values[:-1],wanted[:-1]):
   for got,want in zip(map(float,a.split(',')),b):
    error=abs(got-want);maximum=max(maximum,error);assert error<2e-5,(n,cases[n],line,wanted,error)
report={'passed':True,'cases':len(cases),'maximum_absolute_error':maximum,'scope':__doc__.strip(),
 'files':[{'path':p,'sha256':hashlib.sha256((PROJECT/p).read_bytes()).hexdigest()}for p in sources]}
(PROJECT/'remaining_coin_flight_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
