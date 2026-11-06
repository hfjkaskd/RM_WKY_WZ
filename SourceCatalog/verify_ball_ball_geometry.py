"""Compare production BallBall geometry against original ARM64 instruction blocks.

Original CellPoint.Distance, banker's rounding in normal ExplodeCells, ring-delay
arithmetic and GetSwipeTypeFromPoints execute from the ELF. Framework modf is an
explicit host boundary. The C# harness executes the production combo controller
and column iterator with typed view/damage/wait/service boundaries. This is not
native differential proof of the complete coroutine, damage, animations or SDKs.
"""
from pathlib import Path
import hashlib,json,math,random,sqlite3,struct,subprocess,sys,tempfile
PROJECT=Path(__file__).resolve().parents[1];RECOVERY=PROJECT.parent
sys.path.insert(0,str(RECOVERY/'tools/python'))
from elftools.elf.elffile import ELFFile
from unicorn import Uc,UC_ARCH_ARM64,UC_MODE_ARM,UC_HOOK_CODE
import unicorn.arm64_const as r
u=Uc(UC_ARCH_ARM64,UC_MODE_ARM);u.mem_map(0,0x6000000);u.mem_map(0x10000000,0x400000);u.mem_map(0x20000000,0x1000)
lib=RECOVERY.parent/'RoyalMatch_20260929/apk_unpacked/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so'
with lib.open('rb')as stream:
    elf=ELFFile(stream)
    for segment in elf.iter_segments():
        if segment['p_type']=='PT_LOAD':u.mem_write(segment['p_vaddr'],segment.data())
def put(p,v,n=8):u.mem_write(p,(v&((1<<(n*8))-1)).to_bytes(n,'little'))
def packed(x,y):return(x&0xffffffff)|((y&0xffffffff)<<32)
def fp(v):return struct.unpack('<Q',struct.pack('<d',v))[0]
def f32(v):return struct.unpack('<f',struct.pack('<f',v))[0]
def modf(uc,address,size,unused):
    value=struct.unpack('<d',uc.reg_read(r.UC_ARM64_REG_D0).to_bytes(8,'little'))[0]
    fraction,integer=math.modf(value);uc.mem_write(uc.reg_read(r.UC_ARM64_REG_X0),struct.pack('<d',integer))
    uc.reg_write(r.UC_ARM64_REG_D0,fp(fraction));uc.reg_write(r.UC_ARM64_REG_PC,uc.reg_read(r.UC_ARM64_REG_LR))
u.hook_add(UC_HOOK_CODE,modf,begin=0xd019c0,end=0xd019c0)
def run(start,end):
    u.emu_start(start,end,count=1000)
    assert u.reg_read(r.UC_ARM64_REG_PC)==end,(hex(start),hex(u.reg_read(r.UC_ARM64_REG_PC)))
def call(address,*args):
    for i,v in enumerate(args):u.reg_write(getattr(r,'UC_ARM64_REG_X'+str(i)),v)
    u.reg_write(r.UC_ARM64_REG_LR,0x20000000);u.reg_write(r.UC_ARM64_REG_SP,0x103ff000);run(address,0x20000000)
    return u.reg_read(r.UC_ARM64_REG_X0)
radius_cache={};delay_cache={};queries=0
def radius(dx,dy):
    global queries
    queries+=1
    if(dx,dy)in radius_cache:return radius_cache[dx,dy]
    put(0x10000000,packed(dx,dy));call(0x343ba6c,0x10000000,packed(0,0))
    u.reg_write(r.UC_ARM64_REG_S8,u.reg_read(r.UC_ARM64_REG_S0));run(0x1e2afc0,0x1e2afc4) # Native doubling.
    for reg,value in ((9,-.5),(10,-1.),(11,.5),(12,1.)):u.reg_write(getattr(r,'UC_ARM64_REG_D'+str(reg)),fp(value))
    u.reg_write(r.UC_ARM64_REG_SP,0x103ff000);u.reg_write(r.UC_ARM64_REG_X19,0x10001000);put(0x10001068,0x10002000)
    u.reg_write(r.UC_ARM64_REG_X20,0x7ff0000000000000);run(0x1e2afd0,0x1e2b04c)
    result=u.reg_read(r.UC_ARM64_REG_W24);radius_cache[dx,dy]=result;return result
def delay(key):
    if key not in delay_cache:
        u.reg_write(r.UC_ARM64_REG_W20,key);run(0x1e2c164,0x1e2c194);delay_cache[key]=u.reg_read(r.UC_ARM64_REG_S0)
    return delay_cache[key]
rng=random.Random(0x1e2a400);cases=[]
for y in range(8):
    for x in range(9):cases.append((9,8,x,y,x-1 if x else x+1,y))
for n in range(128):
    w,h=rng.randrange(2,13),rng.randrange(2,11);x,y=rng.randrange(w),rng.randrange(h)
    other=(x,y+1 if y<h-1 else y-1)if n%2 else(x+1 if x<w-1 else x-1,y)
    cases.append((w,h,x,y,*other))
expected=[]
for w,h,x,y,tx,ty in cases:
    hits=sorted((radius(x-xx,y-yy),xx,yy)for xx in range(w)for yy in range(h))
    rings=sorted({a[0]for a in hits})
    swipe=call(0x1e2a8d0,packed(x,y),packed(tx,ty))
    expected.append(str(swipe)+'|'+';'.join(':'.join(map(str,a))for a in hits)+'|'+';'.join(str(k)+':'+str(delay(k))for k in rings[:-1]))
mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
sources=['SourceCatalog/BallBallGeometryHarness.cs','Assets/Foundation/Runtime/OriginalBallBallCombo.cs','Assets/Foundation/Runtime/OriginalGridIterator.cs','Assets/Foundation/Runtime/OriginalFallGridStrategies.cs']
with tempfile.TemporaryDirectory()as folder:
    exe=Path(folder)/'Verify.exe'
    args=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',
          *['-r:'+str(mono/'lib/mono/4.5'/name)for name in ('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),*[str(PROJECT/p)for p in sources]]
    result=subprocess.run(args,capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(','.join(map(str,c))for c in cases)+'\n',capture_output=True,text=True,encoding='utf-8')
    assert result.returncode==0,result.stdout+result.stderr
    actual=result.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
    for i,(a,b)in enumerate(zip(actual,expected)):assert a==b,(i,cases[i],a,b)
methods=[]
with sqlite3.connect(RECOVERY/'code/native/functions.sqlite3')as db:
    for address in(0x343ba6c,0x1e2ae24,0x1e2bcec,0x1e2a8d0,0x319bc54,0x319bc8c):
        name,body=db.execute('select name,body from functions where address=?',(address,)).fetchone()
        methods.append({'rva':hex(address),'name':name,'body_sha256':hashlib.sha256(body.encode()).hexdigest()})
report={'passed':True,'geometry_cases':len(cases),'cell_radius_queries':queries,'distinct_native_distance_rounding_inputs':len(radius_cache),
        'distinct_native_ring_delays':len(delay_cache),'scope':__doc__.strip(),'native_methods':methods,
        'files':[{'path':p,'sha256':hashlib.sha256((PROJECT/p).read_bytes()).hexdigest()}for p in sources]}
(PROJECT/'ball_ball_geometry_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
