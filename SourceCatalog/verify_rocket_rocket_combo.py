"""Actual RocketRocketCombo ARM64 versus compiled production combo control/data flow."""
from pathlib import Path
import hashlib,json,random,sqlite3,struct,subprocess,sys,tempfile
sys.path.insert(0,str(Path(__file__).resolve().parents[2]/'tools/python'))
from elftools.elf.elffile import ELFFile
from elftools.elf.relocation import RelocationSection
from unicorn import Uc,UC_ARCH_ARM64,UC_MODE_ARM,UC_HOOK_CODE
import unicorn.arm64_const as r
PROJECT=Path(__file__).resolve().parents[1];RECOVERY=PROJECT.parent
lib=RECOVERY.parent/'RoyalMatch_20260929/apk_unpacked/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so'
u=Uc(UC_ARCH_ARM64,UC_MODE_ARM);u.mem_map(0,0x6000000);u.mem_map(0x10000000,0x200000);u.mem_map(0x20000000,0x1000)
def put(p,v,n=4):u.mem_write(p,(v&((1<<(n*8))-1)).to_bytes(n,'little'))
def read(p,n=4):return int.from_bytes(u.mem_read(p,n),'little')
def signed(v):v&=0xffffffff;return v-(1<<32) if v&(1<<31)else v
with lib.open('rb')as f:
    elf=ELFFile(f)
    for s in elf.iter_segments():
        if s['p_type']=='PT_LOAD':u.mem_write(s['p_vaddr'],s.data())
    for s in elf.iter_sections():
        if isinstance(s,RelocationSection):
            for v in s.iter_relocations():
                if v['r_info_type']==1027:put(v['r_offset'],v['r_addend'],8)
owner,to,frm,tc,fc,tm,fm,klass,table,data,charger=[0x10000000+i*0x1000 for i in range(11)]
slots={};heap=0x10080000;trace=[];counter=0
session_slot=read(0x53bb4e0,8);names={to:'to',frm:'from',tm:'to',fm:'from',tc:'to',fc:'from'};cells={}
def resolve(slot):
    global heap
    if slot in slots:return slots[slot]
    p=heap;heap+=0x800;slots[slot]=p;put(p+0xe0,1);put(p+0x12c,1,1);put(p+0xb8,p+0x200,8)
    if slot==session_slot:put(p+0x200,p+0x300,8);put(p+0x300+0x38,p+0x400,8);put(p+0x400+0x18,p+0x500,8)
    return p
def vector():return [struct.unpack('<f',struct.pack('<I',u.reg_read(getattr(r,'UC_ARM64_REG_S'+str(i)))))[0]for i in range(3)]
def payload(point,trig,eid):return struct.pack('<iiiii??2xiiiiifff',*point,trig,eid,0,False,False,-1,0,-1,-1,-1,0,0,0)
def boundary(uc,address,size,unused):
    global counter
    x0,x1,x2=[uc.reg_read(getattr(r,'UC_ARM64_REG_X'+str(i)))for i in range(3)];value=None
    if address==0xffd960:put(x0,resolve(x0),8);value=0
    elif address in(0xffd90c,0xffda64):value=x0
    elif address in(0xffda88,0xffde20):raise AssertionError(('Native exception',hex(address),hex(uc.reg_read(r.UC_ARM64_REG_LR))))
    elif address==0x2b45968:value=cells[x0]
    elif address==0x2b45cd4:trace.append('clear:'+names[x0]);cells[x0]=0;value=0
    elif address==0x3190664:trace.append('freeze:'+names[x0]+':'+str(uc.reg_read(r.UC_ARM64_REG_S0)));value=0
    elif address==0x27bb2b0:trace.append('orientation:'+names[x0]+':'+str(signed(x1)));put(x0+0x100,x1);value=0
    elif address==0x27b4740:trace.append('position:'+names[x0]+':'+':'.join(str(int(v))for v in vector()[:2]));value=0
    elif address in(0x2eebc08,0x2eebcec):
        counter+=1;point=(-1,-1)if address==0x2eebc08 else(signed(x1),signed(x1>>32));u.mem_write(uc.reg_read(r.UC_ARM64_REG_X8),payload(point,x0,counter));value=0
    elif address==0x27ba914:trace.append('explode:'+names[x0]+':'+bytes(uc.mem_read(x1,56)).hex()+':'+bytes(uc.mem_read(x2,56)).hex());value=0
    elif address==0x260dea4:value=charger
    elif address==0x3191dd4:
        point=(signed(read(x0+0x18)),signed(read(x0+0x1c)),0)
        for i,v in enumerate(point):uc.reg_write(getattr(r,'UC_ARM64_REG_S'+str(i)),struct.unpack('<I',struct.pack('<f',v))[0])
        value=0
    elif address in(0x344f544,0x32b966c,0x32b9690):value=0 # Locked first-board charger/events are outside the oracle.
    if value is not None:uc.reg_write(r.UC_ARM64_REG_X0,value&0xffffffffffffffff);uc.reg_write(r.UC_ARM64_REG_PC,uc.reg_read(r.UC_ARM64_REG_LR))
u.hook_add(UC_HOOK_CODE,boundary)
def call(addr,*args):
    for i,v in enumerate(args):u.reg_write(getattr(r,'UC_ARM64_REG_X'+str(i)),v)
    u.reg_write(r.UC_ARM64_REG_SP,0x101ff000);u.reg_write(r.UC_ARM64_REG_LR,0x20000000);u.emu_start(addr,0x20000000,count=100000)
    assert u.reg_read(r.UC_ARM64_REG_PC)==0x20000000,hex(u.reg_read(r.UC_ARM64_REG_PC))
put(klass+0x12c,1,1);put(klass+0xc8,table,8);put(table,resolve(read(0x53790d0,8)),8)
put(to,klass,8);put(frm,klass,8);put(to+0x38,tm,8);put(frm+0x38,fm,8)
rng=random.Random(20261020);cases=[];expected=[]
for index in range(96):
    x,y=rng.randrange(9),rng.randrange(8);dx,dy=rng.choice([(1,0),(-1,0),(0,1),(0,-1)])
    orientations=[0,1,-2,-1,2,3,2147483647,-2147483648];a=[x,y,x+dx,y+dy,orientations[index%8],index%2,index+200]
    cells.update({tm:tc,fm:fc});put(tc+0x18,a[0]);put(tc+0x1c,a[1]);put(fc+0x18,a[2]);put(fc+0x1c,a[3]);put(to+0x100,a[4]);put(frm+0x100,a[5]);u.mem_write(data,payload((-1,-1),2,a[6]));trace.clear();counter=0
    call(0x1e2f16c,owner,to,frm);assert cells[tm]==cells[fm]==0
    call(0x1e2f314,owner,data)
    assert read(owner+0x10,8)==read(owner+0x18,8)==read(owner+0x30,8)==0
    assert read(owner+0x20,8)==fc and read(owner+0x28,8)==read(fc+0x18,8),'Clear retains center fields'
    expected.append('|'.join(trace));cases.append(','.join(map(str,a)))
mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
with tempfile.TemporaryDirectory()as folder:
    exe=Path(folder)/'Verify.exe';runtime=PROJECT/'Assets/Foundation/Runtime'
    command=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',*["-r:"+str(mono/'lib/mono/4.5'/name)for name in('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),str(PROJECT/'SourceCatalog/RocketRocketComboHarness.cs'),str(runtime/'OriginalRocketRocketCombo.cs'),str(runtime/'OriginalExplodeData.cs')]
    result=subprocess.run(command,capture_output=True,text=True,encoding='utf8');assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf8');assert result.returncode==0,result.stdout+result.stderr
    actual=result.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected)
    assert actual==expected,next((i,a,e)for i,(a,e)in enumerate(zip(actual,expected))if a!=e)
db=sqlite3.connect(RECOVERY/'code/native/functions.sqlite3');methods=[]
for a in[0x1e2f16c,0x1e2f314,0x1e30a9c,0x2b489ac,0x343b790]:
    n,b=db.execute('select name,body from functions where address=?',(a,)).fetchone();methods.append({'rva':hex(a),'name':n,'body_sha256':hashlib.sha256(b.encode()).hexdigest()})
report={'passed':True,'native_combo_cases':len(cases),'methods':methods,'scope':'Actual ARM64 RocketRocketCombo Prepare/Explode/Clear, Opposite and CellPoint.ToVector vs compiled production OriginalRocketRocketCombo with production OriginalExplodeData/Exploder. Exact clear/freeze/orientation/position/explosion order and both56byte trigger/preset payloads, repeatedowner reuse, horizontal/vertical points and signedorientation boundaries. Unity/model view reinitialization, managed allocation, cell freeze implementation, actual rocket damage/flight and locked event/charger ownership are supplied boundaries; no rendered-original/session/SDK parity claim.'}
(PROJECT/'rocket_rocket_combo_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf8');print(json.dumps({k:v for k,v in report.items()if k!='methods'}))
