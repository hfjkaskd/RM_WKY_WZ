"""Execute actual ARM64 low-match configuration, tuning and neighbor rules.

Random draws, grid/target-mediation getters and LevelSet selection are typed
boundaries. Native branching, sums, array ownership, target search, forbidden
color order and short circuits run from the original ELF. Diagnostic formatting
in UpdateLowMatchRatios is skipped. Scene checks independently exercise actual
board mediation, random source and weighted set selection after real WinFail.
"""
from pathlib import Path
import hashlib,json,random,subprocess,sys,tempfile
PROJECT=Path(__file__).resolve().parents[1];BASE=PROJECT.parent
sys.path.insert(0,str(BASE/'tools/python'))
from elftools.elf.elffile import ELFFile
from elftools.elf.relocation import RelocationSection
from unicorn import Uc,UC_ARCH_ARM64,UC_MODE_ARM,UC_HOOK_CODE
import unicorn.arm64_const as r
u=Uc(UC_ARCH_ARM64,UC_MODE_ARM);u.mem_map(0,0x6000000);u.mem_map(0x10000000,0x400000);u.mem_map(0x20000000,0x1000)
def put(p,v,n=8):u.mem_write(p,(int(v)&((1<<(8*n))-1)).to_bytes(n,'little'))
def read(p,n=4):return int.from_bytes(u.mem_read(p,n),'little')
def si(v):return (v&0x7fffffff)-(v&0x80000000)
def x(i):return u.reg_read(getattr(r,'UC_ARM64_REG_X'+str(i)))
def returned(value=0):u.reg_write(r.UC_ARM64_REG_X0,value&0xffffffffffffffff);u.reg_write(r.UC_ARM64_REG_PC,u.reg_read(r.UC_ARM64_REG_LR))
with(BASE.parent/'RoyalMatch_20260929/apk_unpacked/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so').open('rb')as stream:
    elf=ELFFile(stream)
    for s in elf.iter_segments():
        if s['p_type']=='PT_LOAD':u.mem_write(s['p_vaddr'],s.data())
    for s in elf.iter_sections():
        if isinstance(s,RelocationSection):
            for v in s.iter_relocations():
                if v['r_info_type']==1027:put(v['r_offset'],v['r_addend'])
owner,ratio,random_owner,level_set,items,storage,klass,metadata=[0x10001000+i*0x1000 for i in range(8)]
cells=[0x10010000+i*0x1000 for i in range(25)];types=[];trace=[];head=[];fault=False
put(klass+0x548,0x20000200)
def failure():
    global fault
    fault=True;u.reg_write(r.UC_ARM64_REG_PC,0x20000000)
def ident(p):return (p-0x10010000)//0x1000
def boundary(uc,address,size,unused):
    global metadata
    a,b,c=x(0),x(1),x(2)
    if address==0xffd960:
        if not 0x10008000<=read(a,8)<0x10010000:
            put(a,metadata);put(metadata+0xe0,1,4);put(metadata+0x132,1,1);metadata+=0x200
    elif address in(0xffd90c,0xffda64):pass
    elif address in(0xffda88,0xffda90):failure();return
    elif address==0x23f0aac:u.reg_write(r.UC_ARM64_REG_PC,0x23f0bf0);return # diagnostic formatting only
    elif address==0x23f0c80:u.reg_write(r.UC_ARM64_REG_PC,0x23f0c4c);return # disabled diagnostic only
    elif address==0x1ed98c4:
        trace.append('random,'+str(si(b))+','+str(si(c)))
        if si(c)<si(b):failure();return
        returned(head[1]);return
    elif address==0x31932c8:
        index=ident(a-0x200);direction=si(b);trace.append(f'neighbor,{index},{direction}')
        px,py=index%5,index//5
        n=index-5 if direction==5 and py>0 else index-1 if direction==7 and px>0 else index+1 if direction==3 and px<4 else -1
        returned(cells[n]if n>=0 else 0);return
    elif address==0x3448800:
        index=ident(a-0x400);trace.append('has,'+str(index));returned(types[index]>=0);return
    elif address==0x34487e4:
        index=ident(a-0x400);trace.append('target,'+str(index));returned(cells[index]+0x600 if types[index]>=0 else 0);return
    elif address==0x20000200:returned(types[ident(a-0x600)]>0);return
    elif address==0x1910528:trace.append('item');returned(17);return
    elif address==0x19105fc:
        data=read(b+0x10,8);v=[si(read(data+0x20+4*i))for i in range(read(b+0x18))]
        trace.append('except,'+','.join(map(str,v)));returned(18);return
    else:raise AssertionError(hex(address))
    returned()
for addr in(0xffd960,0xffd90c,0xffda64,0xffda88,0xffda90,0x23f0aac,0x23f0c80,0x1ed98c4,0x31932c8,0x3448800,0x34487e4,0x20000200,0x1910528,0x19105fc):
    u.hook_add(UC_HOOK_CODE,boundary,begin=addr,end=addr)
def call(addr,*args):
    global fault
    fault=False
    for i in range(8):u.reg_write(getattr(r,'UC_ARM64_REG_X'+str(i)),(args[i]if i<len(args)else 0)&0xffffffffffffffff)
    u.reg_write(r.UC_ARM64_REG_SP,0x103ff000);u.reg_write(r.UC_ARM64_REG_LR,0x20000000)
    u.emu_start(addr,0x20000000,count=10000);assert u.reg_read(r.UC_ARM64_REG_PC)==0x20000000
    return si(x(0))if not fault else 0
cases=[];expected=[];rng=random.Random(0x23f0540)
for n in range(1400):
    mode=n%5;values=rng.choice((None,[],[1],[1,2],[10,45,45],[0,0,0],[1,1,1],[2147483647,1,1],[-1,3,4],[0,0,10,1]))
    total=si(sum(values))if values is not None and len(values)>=3 else 0
    draw=rng.randrange(total)if total>0 else 0
    head=[mode,rng.choice((3,5,7))if mode in(3,4)else draw,rng.randrange(25),rng.choice((0,0,0,0,1,2,3)),rng.choice((0,0,0,1,99))]
    types=[rng.choice((-1,-1,0,1,2,3,4,5,6,7))for _ in range(25)]
    u.mem_write(owner,bytes(0x200));put(owner+0xf0,0 if head[3]==1 else random_owner);put(owner+0x128,items);put(items+0x10,storage);put(items+0x18,0,4);put(storage+0x18,16,4)
    if values is not None:
        put(ratio+0x18,len(values),4)
        for i,v in enumerate(values):put(ratio+0x20+4*i,v,4)
    trace=[];call(0x23f0a34,owner,ratio if values is not None else 0);assert not fault
    if head[4]and values:put(ratio+0x20,head[4],4)
    for i,p in enumerate(cells):
        put(p+0x10,p+0x400);put(p+0x20,p+0x200);put(p+0x600,klass);put(p+0x618,types[i],4)
    value=0
    if mode==1:value=call(0x23f07dc,owner)
    elif mode==2:value=call(0x23f0540,owner,0 if head[3]==2 else level_set,0 if head[3]==3 else cells[22])
    elif mode in(3,4):value=call(0x23f084c if mode==3 else 0x23f08e0,owner,0 if head[3]==3 else cells[head[2]],head[1])
    cases.append(','.join(map(str,head))+'|'+('null'if values is None else ','.join(map(str,values)))+'|'+','.join(map(str,types)))
    expected.append(str(int(fault))+'#'+str(read(owner+0x130,1))+','+str(si(read(owner+0x140)))+','+str(value)+'#'+';'.join(trace))
mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
sources=['SourceCatalog/LowMatchHarness.cs','Assets/Foundation/Runtime/OriginalLowMatchTuning.cs','Assets/Foundation/Runtime/OriginalLevelRules.cs']
with tempfile.TemporaryDirectory()as directory:
    exe=Path(directory)/'LowMatch.exe';args=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',
        *['-r:'+str(mono/'lib/mono/4.5'/p)for p in('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),*[str(PROJECT/p)for p in sources]]
    result=subprocess.run(args,capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    actual=result.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
    for i,(a,b)in enumerate(zip(actual,expected)):assert a==b,(i,cases[i],a,b)
report={'passed':True,'cases':len(cases),'scope':__doc__.strip(),'original_rvas':['0x23f0a34','0x23f07dc','0x23f0540','0x23f084c','0x23f08e0'],
    'files':[{'path':p,'sha256':hashlib.sha256((PROJECT/p).read_bytes()).hexdigest()}for p in sources]}
(PROJECT/'low_match_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
