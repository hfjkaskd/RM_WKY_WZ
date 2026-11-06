"""Compare actual ARM64 ItemModel/MatchItemModel.ReplaceItem control flow.

Goal/collection/hat/Madness/factory calls and view getters are explicit typed
boundaries. Native instructions decide order, short-circuit manager selection,
view ownership and match-count dispatch. This test does not replace the full
Unity scene checks or prove active collection-manager presentation.
"""
from pathlib import Path
import hashlib,json,random,struct,subprocess,sys,tempfile
PROJECT=Path(__file__).resolve().parents[1];BASE=PROJECT.parent
sys.path.insert(0,str(BASE/'tools/python'))
from elftools.elf.elffile import ELFFile
from elftools.elf.relocation import RelocationSection
from unicorn import Uc,UC_ARCH_ARM64,UC_MODE_ARM,UC_HOOK_CODE
import unicorn.arm64_const as r
u=Uc(UC_ARCH_ARM64,UC_MODE_ARM);u.mem_map(0,0x6000000);u.mem_map(0x10000000,0x400000);u.mem_map(0x20000000,0x1000)
def put(p,v,n=8):u.mem_write(p,(v&((1<<(8*n))-1)).to_bytes(n,'little'))
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
item,klass,owner,view,metadata=[0x10001000+i*0x1000 for i in range(5)]
put(item,klass)
for offset,addr in((0x368,0x20000100),(0x588,0x20000110),(0x568,0x20000120),(0x418,0x20000130)):
    put(klass+offset,addr)
values=[];trace=[];fault=False
def flag(n):return (values[4]>>n)&1
def boundary(uc,address,size,unused):
    global metadata,fault
    a,b,c=x(0),x(1),x(2)
    if address==0xffd960:
        if not 0x10005000<=read(a,8)<0x10300000:
            put(a,metadata);put(metadata+0xe0,1,4);put(metadata+0x132,1,1);metadata+=0x200
    elif address in(0xffd90c,0xffda64):pass
    elif address==0xffda88:fault=True;u.reg_write(r.UC_ARM64_REG_PC,0x20000000);return
    elif address==0x20000100:trace.append('visible,'+str(b&1));put(item+0x80,b&1,1)
    elif address==0x20000110:trace.append('goal,'+str(b&1))
    elif address==0x20000120:
        trace.append('position')
        for i,f in enumerate((2.,3.,4.)):u.reg_write(getattr(r,'UC_ARM64_REG_S'+str(i)),struct.unpack('<I',struct.pack('<f',f))[0])
    elif address==0x20000130:returned(view);return
    elif address==0x260a1fc:trace.append('recycle')
    elif address==0x1e19848:trace.append('item,'+str(b));returned(flag(0 if b==22 else 2));return
    elif address==0x1e19780:trace.append('static,'+str(b));returned(flag(1 if b==5 else 3));return
    elif address==0x3214794:trace.append('goals,'+str(b));returned(values[2]);return
    elif address==0x260df1c:returned(owner);return
    elif address==0x317e8f8:trace.append('purple')
    elif address in(0x21e6864,0x2e57a64,0x21ee360):
        name,index={0x21e6864:('curtain',4),0x2e57a64:('drill',5),0x21ee360:('cryptex',6)}[address]
        trace.append(name+','+str(si(b)));returned(owner if flag(index)else 0);return
    elif address==0x21a6400:trace.append('collect-goal')
    elif address==0x21a6510:trace.append('collect-manager')
    elif address==0x21a3fb0:trace.append('count,'+str(si(a))+','+str(si(b)))
    elif address==0x32b9ab0:trace.append('madness,'+str(si(b))+','+str(si(c))+','+str(si(x(3)))+','+str(x(5)&1))
    else:raise AssertionError(hex(address))
    returned()
addresses=(0xffd960,0xffd90c,0xffda64,0xffda88,0x20000100,0x20000110,0x20000120,0x20000130,
    0x260a1fc,0x1e19848,0x1e19780,0x3214794,0x260df1c,0x317e8f8,0x21e6864,0x2e57a64,0x21ee360,0x21a6400,0x21a6510,0x21a3fb0,0x32b9ab0)
for addr in addresses:u.hook_add(UC_HOOK_CODE,boundary,begin=addr,end=addr)
cases=[];expected=[];rng=random.Random(0x21a7168)
for kind in(0,1):
    for n in range(400):
        values=[kind,rng.choice((0,1,2,3,4,5,6,7)),rng.choice((-2147483648,-1,0,1,2,2147483647)),n&1,rng.randrange(128),rng.choice((0,0,0,0,1,2))if kind else 0]
        trace=[];fault=False;put(item+0x18,values[1],4);put(item+0x20,7,4);put(item+0x80,1,1)
        for offset in(0x48,0x58,0x60):put(item+offset,owner)
        if values[5]==1:put(item+0x48,0);put(item+0x60,0)
        if values[5]==2:put(item+0x60,0)
        for i in range(8):u.reg_write(getattr(r,'UC_ARM64_REG_X'+str(i)),item if i==0 else values[3]if i==1 else 0)
        u.reg_write(r.UC_ARM64_REG_SP,0x103ff000);u.reg_write(r.UC_ARM64_REG_LR,0x20000000)
        # Native Madness owner chain is a boundary, but pointer dereferences
        # before the actual call must exist even when the event is locked.
        slot=read(0x53bb4e0,8)
        if not 0x10005000<=read(slot,8)<0x10300000:
            put(slot,metadata);put(metadata+0xe0,1,4);put(metadata+0x132,1,1);metadata+=0x200
        userclass=read(slot,8);statics=0x100d0000;user=0x100d1000;data=0x100d2000;session=0x100d3000
        put(userclass+0xb8,statics);put(statics,user);put(user+0x38,data);put(data+0x18,session)
        u.emu_start(0x2b46f90 if kind==0 else 0x21a7168,0x20000000,count=10000)
        assert u.reg_read(r.UC_ARM64_REG_PC)==0x20000000,hex(u.reg_read(r.UC_ARM64_REG_PC))
        cases.append(','.join(map(str,values)));expected.append(str(int(fault))+'#'+str(read(item+0x80,1))+'#'+';'.join(trace))
mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
sources=['SourceCatalog/ItemReplacementHarness.cs','Assets/Foundation/Runtime/OriginalItemReplacement.cs']
with tempfile.TemporaryDirectory()as directory:
    exe=Path(directory)/'Replacement.exe'
    args=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',
        *['-r:'+str(mono/'lib/mono/4.5'/p)for p in('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),*[str(PROJECT/p)for p in sources]]
    result=subprocess.run(args,capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    actual=result.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
    for i,(a,b)in enumerate(zip(actual,expected)):assert a==b,(i,cases[i],a,b)
report={'passed':True,'cases':len(cases),'base_cases':400,'match_cases':400,'scope':__doc__.strip(),
    'files':[{'path':p,'sha256':hashlib.sha256((PROJECT/p).read_bytes()).hexdigest()}for p in sources]}
(PROJECT/'item_replacement_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
