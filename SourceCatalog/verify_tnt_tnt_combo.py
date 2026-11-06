"""Compare actual TntTntCombo ARM64 control/callback order with production C#."""
from pathlib import Path
import hashlib,json,random,sqlite3,struct,subprocess,sys,tempfile
sys.path.insert(0,str(Path(__file__).resolve().parents[2]/'tools/python'))
from elftools.elf.elffile import ELFFile
from elftools.elf.relocation import RelocationSection
from unicorn import Uc,UC_ARCH_ARM64,UC_MODE_ARM,UC_HOOK_CODE
import unicorn.arm64_const as r
PROJECT=Path(__file__).resolve().parents[1]; RECOVERY=PROJECT.parent
lib=RECOVERY.parent/'RoyalMatch_20260929/apk_unpacked/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so'
u=Uc(UC_ARCH_ARM64,UC_MODE_ARM);u.mem_map(0,0x6000000);u.mem_map(0x10000000,0x200000);u.mem_map(0x20000000,0x1000)
def put(p,v,n=4):u.mem_write(p,(v&((1<<(n*8))-1)).to_bytes(n,'little'))
def read(p,n=4):return int.from_bytes(u.mem_read(p,n),'little')
def signed(v):return v-(1<<32) if v&(1<<31) else v
with lib.open('rb') as f:
    elf=ELFFile(f)
    for segment in elf.iter_segments():
        if segment['p_type']=='PT_LOAD':u.mem_write(segment['p_vaddr'],segment.data())
    for section in elf.iter_sections():
        if isinstance(section,RelocationSection):
            for relocation in section.iter_relocations():
                if relocation['r_info_type']==1027:put(relocation['r_offset'],relocation['r_addend'],8)
owner,to,frm,tc,fc,klass,table,factory,state,view,data,closure,action=[0x10000000+i*0x1000 for i in range(13)]
slots={};heap=0x10080000;trace=[];positions={};names={to:'to',frm:'from',tc:'to',fc:'from'}
session_slot=read(0x53bb4e0,8);action_slot=read(0x536d610,8)
def resolve(slot):
    global heap
    if slot in slots:return slots[slot]
    p=heap;heap+=0x800;slots[slot]=p
    put(p+0xe0,1);put(p+0x12c,1,1);put(p+0xb8,p+0x200,8)
    if slot==session_slot:
        put(p+0x200,p+0x300,8);put(p+0x300+0x38,p+0x400,8);put(p+0x400+0x18,p+0x500,8)
    return p
def boundary(uc,address,size,unused):
    x0,x1,x2=[uc.reg_read(getattr(r,'UC_ARM64_REG_X'+str(i))) for i in range(3)]; value=None
    if address==0xffd960:put(x0,resolve(x0),8);value=0
    elif address in (0xffd90c,0xffda64,0x231007c):value=x0
    elif address in (0xffda88,0xffde20):raise AssertionError(('Native exception',hex(address),hex(uc.reg_read(r.UC_ARM64_REG_LR))))
    elif address==0xffda78:value=action if x0==resolve(action_slot) else closure
    elif address==0x2b45df4:value=tc if x0==to else fc
    elif address==0x27b4320:trace.append('active:'+names[x0]+':'+str(x1));value=0
    elif address in (0x31933d4,0x31932f8):trace.append(('freeze:' if address==0x31933d4 else 'unfreeze:')+names[x0]);value=0
    elif address==0x260a220:trace.append('spawn:'+str(x1)+':'+str(x2));value=view
    elif address==0x260dea4:value=state
    elif address in (0x1ee5ac0,0x1ee5ad0):trace.append('start' if address==0x1ee5ac0 else 'finish');value=0
    elif address==0x3191dd4:
        for i,v in enumerate((*positions[x0],0)):uc.reg_write(getattr(r,'UC_ARM64_REG_S'+str(i)),struct.unpack('<I',struct.pack('<f',v))[0])
        value=0
    elif address==0x2044a78:put(x0+0x10,x1,8);value=0
    elif address==0x27b9d4c:
        vector=[struct.unpack('<f',struct.pack('<I',uc.reg_read(getattr(r,'UC_ARM64_REG_S'+str(i)))))[0] for i in range(2)]
        trace.append('init:'+':'.join(str(int(v)) for v in vector));value=0
    elif address in (0x32b966c,0x32b9690):value=0 # First-board inactive event ownership is outside this oracle.
    elif address==0x20000300:
        px,py,trig,eid=struct.unpack('<iiii',bytes(uc.mem_read(x1,16)));combo=signed(read(x1+24))
        trace.append('explode:'+names[x0]+':'+':'.join(map(str,(eid,px,py,trig,combo))));value=0
    if value is not None:uc.reg_write(r.UC_ARM64_REG_X0,value&0xffffffffffffffff);uc.reg_write(r.UC_ARM64_REG_PC,uc.reg_read(r.UC_ARM64_REG_LR))
u.hook_add(UC_HOOK_CODE,boundary)
def call(addr,*args):
    for i,v in enumerate(args):u.reg_write(getattr(r,'UC_ARM64_REG_X'+str(i)),v)
    u.reg_write(r.UC_ARM64_REG_SP,0x101ff000);u.reg_write(r.UC_ARM64_REG_LR,0x20000000)
    u.emu_start(addr,0x20000000,count=100000)
    assert u.reg_read(r.UC_ARM64_REG_PC)==0x20000000,hex(u.reg_read(r.UC_ARM64_REG_PC))
put(klass+0x12c,1,1);put(klass+0xc8,table,8);put(table,resolve(read(0x53565b0,8)),8);put(klass+0x508,0x20000300,8)
put(to,klass,8);put(frm,klass,8);put(owner+0x18,factory,8)
cases=[];expected=[];rng=random.Random(20261020)
for index in range(80):
    x,y=rng.randrange(9),rng.randrange(8);dx,dy=rng.choice([(1,0),(-1,0),(0,1),(0,-1)])
    a=[x,y,x+dx,y+dy,index+1,2,rng.randrange(-1,9),rng.randrange(-1,8),rng.randrange(-1,100)]
    positions.update({tc:(a[0],a[1]),fc:(a[2],a[3])});trace.clear()
    put(to+0xc0,2);put(frm+0xc0,2)
    payload=struct.pack('<iiiii??2xiiiiifff',a[6],a[7],a[5],a[4],0,False,False,a[8],0,-1,-1,-1,0,0,0)
    u.mem_write(data,payload)
    call(0x1e2ecf8,owner,to,frm);assert read(owner+0x20,8)==to and read(owner+0x28,8)==frm
    call(0x1e2ee20,owner,data)
    assert read(owner+0x20,8)==0 and read(owner+0x28,8)==0,'Native prepared refs release before completion'
    trace.append('state:'+str(read(owner+0x10,1))+':'+str(read(to+0xc0))+':'+str(read(frm+0xc0)))
    call(0x1e32e88,closure)
    trace.append('state:'+str(read(owner+0x10,1))+':'+str(read(to+0xc0))+':'+str(read(frm+0xc0)))
    expected.append('|'.join(trace));cases.append(','.join(map(str,a)))
mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
with tempfile.TemporaryDirectory() as folder:
    exe=Path(folder)/'Verify.exe';runtime=PROJECT/'Assets/Foundation/Runtime'
    command=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',*["-r:"+str(mono/'lib/mono/4.5'/name) for name in ('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),str(PROJECT/'SourceCatalog/TntTntComboHarness.cs'),str(runtime/'OriginalTntTntCombo.cs'),str(runtime/'OriginalExplodeData.cs')]
    result=subprocess.run(command,capture_output=True,text=True,encoding='utf8');assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf8');assert result.returncode==0,result.stdout+result.stderr
    actual=result.stdout.lstrip('\ufeff').splitlines()
    assert len(actual)==len(expected)
    assert actual==expected,next((i,a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e)
db=sqlite3.connect(RECOVERY/'code/native/functions.sqlite3');methods=[]
for a in [0x1e2ecf8,0x1e2ee20,0x1e32e88,0x27b65c0,0x27b65cc]:
    n,b=db.execute('select name,body from functions where address=?',(a,)).fetchone();methods.append({'rva':hex(a),'name':n,'body_sha256':hashlib.sha256(b.encode()).hexdigest()})
report={'passed':True,'native_combo_cases':len(cases),'methods':methods,'scope':'Actual ARM64 TntTntCombo.Prepare/Explode/completion closure and native TNT config radius writes versus compiled production OriginalTntTntCombo control/callback order. Adjacent horizontal/vertical positions, repeated owner reuse, payload id/point/trigger/comboId, active flag, prepared-ref release, pool/start/finish/freeze/unfreeze/explosion ordering exact. Unity/managed allocation, view/model damage/game-state/cell/event boundaries supplied; does not differentially verify radius damage, model config C# writers, Animator/render/audio/charger/session or natural combo creation.'}
(PROJECT/'tnt_tnt_combo_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf8');print(json.dumps({k:v for k,v in report.items() if k!='methods'}))
