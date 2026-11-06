"""Compare actual rocket projectile iterator bodies with production C# frame traces."""
from pathlib import Path
import hashlib, json, random, sqlite3, struct, subprocess, sys, tempfile
sys.path.insert(0, str(Path(__file__).resolve().parents[2] / 'tools/python'))
from elftools.elf.elffile import ELFFile
from elftools.elf.relocation import RelocationSection
from unicorn import Uc, UC_ARCH_ARM64, UC_MODE_ARM, UC_HOOK_CODE
from unicorn.arm64_const import UC_ARM64_REG_X0, UC_ARM64_REG_X1, UC_ARM64_REG_X2, UC_ARM64_REG_X8, UC_ARM64_REG_LR, UC_ARM64_REG_PC, UC_ARM64_REG_SP, UC_ARM64_REG_S0, UC_ARM64_REG_S1, UC_ARM64_REG_S2

PROJECT=Path(__file__).resolve().parents[1]
RECOVERY=PROJECT.parent
lib=RECOVERY.parent/'RoyalMatch_20260929/apk_unpacked/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so'
emu=Uc(UC_ARCH_ARM64,UC_MODE_ARM)
emu.mem_map(0,0x6000000);emu.mem_map(0x10000000,0x200000);emu.mem_map(0x20000000,0x1000)
def put(a,v,size=4):emu.mem_write(a,(v&((1<<(size*8))-1)).to_bytes(size,'little'))
def read(a,size=4):return int.from_bytes(emu.mem_read(a,size),'little')
def signed(v):return v-0x100000000 if v>=0x80000000 else v
def bits(v):return struct.unpack('<I',struct.pack('<f',v))[0]
def value(v):return struct.unpack('<f',struct.pack('<I',v))[0]
with lib.open('rb') as stream:
    elf=ELFFile(stream)
    for segment in elf.iter_segments():
        if segment['p_type']=='PT_LOAD':emu.mem_write(segment['p_vaddr'],segment.data())
    for section in elf.iter_sections():
        if isinstance(section,RelocationSection):
            for relocation in section.iter_relocations():
                if relocation['r_info_type']==1027:put(relocation['r_offset'],relocation['r_addend'],8)

iterator,owner,view,trans,grid,center=0x10000000,0x10001000,0x10002000,0x10003000,0x10004000,0x10005000
lst,array,exploded,exploded_array=0x10006000,0x10007000,0x10008000,0x10009000
cell_base,point_statics=0x10010000,0x10090000
positions={}; points={}; trace=[]; heap=0x100a0000; metadata_heap=0x100c0000; resolved={}; delta=0
put(point_statics,-1,8)
def cell_id(pointer):return 99 if pointer==center else (pointer-cell_base)//0x100
def boundary(uc,address,size,unused):
    global heap,metadata_heap
    x0,x1,x2=[uc.reg_read(r) for r in (UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2)]
    result=None
    if address==0xffd960:
        if x0 not in resolved:
            resolved[x0]=metadata_heap;metadata_heap+=0x200;put(resolved[x0]+0xe0,1);put(resolved[x0]+0xb8,point_statics,8)
        put(x0,resolved[x0],8);result=0
    elif address==0xffd9d0:
        result=heap;heap+=0x100;emu.mem_write(result,bytes(0x100));put(result+0x18,x1)
    elif address in (0xffd90c,0xffda64):result=0
    elif address in (0xffda88,0xffda90):raise AssertionError('Native error at '+hex(uc.reg_read(UC_ARM64_REG_LR)))
    elif address==0x1f168d0:
        storage=read(x0+0x10,8);result=read(storage+0x20+x1*8,8)
    elif address==0x3207954:result=points.get((signed(x1&0xffffffff),signed(x1>>32)),0)
    elif address in (0x3e4e2c8,0x3191dd4):
        for reg,v in zip((UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2),positions[x0]):uc.reg_write(reg,bits(v))
        result=0
    elif address==0x3e4e368:
        positions[x0]=[value(uc.reg_read(r)) for r in (UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2)];result=0
    elif address==0x32037e4:uc.reg_write(UC_ARM64_REG_S0,delta);result=0
    elif address in (0x31933d4,0x31932f8):trace.append(('F' if address==0x31933d4 else 'U')+str(cell_id(x0)));result=0
    elif address in (0x27bbeb4,0x27bbee8):trace.append('Start' if address==0x27bbeb4 else 'End');result=0
    elif address==0x319e1dc:result=0 # Formal first board has no jelly.
    elif address==0x18e61e8:emu.mem_write(uc.reg_read(UC_ARM64_REG_X8),bytes(24));result=0
    elif address==0x31906cc:result=0 # No-jelly TrySetJellySpreadData boundary.
    elif address==0x3190884:
        trace.append('E'+str(cell_id(x0))+':'+','.join(str(signed(read(x1+offset))) for offset in (0,4,8,12))+':'+str(read(x1+0x15,1)));result=0
    elif address==0x20000100:trace.append('Complete');result=0
    if result is not None:uc.reg_write(UC_ARM64_REG_X0,result&0xffffffffffffffff);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
emu.hook_add(UC_HOOK_CODE,boundary)
def call(rva):
    for reg,v in ((UC_ARM64_REG_X0,iterator),(UC_ARM64_REG_X1,0),(UC_ARM64_REG_LR,0x20000000),(UC_ARM64_REG_SP,0x101ff000)):emu.reg_write(reg,v)
    emu.emu_start(rva,0x20000000,count=300000)
    assert emu.reg_read(UC_ARM64_REG_PC)==0x20000000,hex(emu.reg_read(UC_ARM64_REG_PC))
    return emu.reg_read(UC_ARM64_REG_X0)&1

rng=random.Random(0x27bd1bc);cases=[];expected=[]
for n in range(96):
    vertical=n%2;wide=(n//2)%2;sign=-1 if n%8<4 else 1
    length=8 if vertical else 9;origin=(n//8)%length
    indices=list(range(origin+sign,length if sign>0 else -1,sign))
    # Alternate edge/empty, short and full cardinal paths.
    if n%9==0:indices=[]
    target=sign*(7.5+(n%5)*.7);unfreeze=sign*5.65 if wide else target
    axis_origin=origin-(3.5 if vertical else 4)
    start=axis_origin+sign*.3
    steps=[bits(rng.choice((1/60,1/30,.01,.025,0 if i%7==0 else .02))) for i in range(240)]
    payload=struct.pack('<5i2B2x5i3f',origin,3,6,91,0,0,int(not vertical),-1,0,-1,-1,-1,0,0,0)
    cases.append('|'.join((str(vertical),str(wide),str(sign),str(origin),str(bits(start)),str(bits(target)),str(bits(unfreeze)),','.join(map(str,indices)),','.join(map(str,steps)))))
    emu.mem_write(iterator,bytes(0x9000));heap=0x100a0000;positions={};points={};trace=[]
    put(iterator+0x20,owner,8);put(iterator+0x28,lst,8);put(iterator+0x40,trans,8)
    put(owner+0x10,view,8);put(owner+0x48,grid,8);put(owner+0x50,1);put(owner+0x58,exploded,8);put(owner+0x60,wide,1)
    put(lst+0x10,array,8);put(lst+0x18,len(indices));put(array+0x18,32)
    put(exploded+0x10,exploded_array,8);put(exploded_array+0x18,512)
    put(center+0x18,origin);put(center+0x1c,3)
    for i in range(length):
        ptr=cell_base+i*0x100;point=(3,i) if vertical else (i,3);points[point]=ptr
        put(ptr+0x18,point[0]);put(ptr+0x1c,point[1]);positions[ptr]=[.25,i-3.5,0] if vertical else [i-4,.25,0]
    for i,index in enumerate(indices):
        point=(3,index) if vertical else (index,3);put(array+0x20+i*8,(point[0]&0xffffffff)|((point[1]&0xffffffff)<<32),8)
    positions[trans]=[.25,start,0] if vertical else [start,.25,0]
    put(iterator+(0x34 if vertical else 0x30),bits(target))
    if vertical:
        put(iterator+0x80,center,8);emu.mem_write(iterator+0x48,payload)
        put(iterator+0x88,0x10080000,8);put(0x10080018,0x20000100,8)
    else:
        put(iterator+0x48,center,8);emu.mem_write(iterator+0x50,payload);put(iterator+0x88,bits(unfreeze))
    frames=[]
    for frame in range(240):
        delta=steps[frame];trace=[];result=call(0x27be674 if vertical else 0x27bd1bc)
        frames.append(str(result)+','+','.join(str(bits(v)) for v in positions[trans])+'#'+','.join(trace))
        if not result:break
    else:raise AssertionError('Native flight did not terminate')
    expected.append(';'.join(frames))

with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder);exe=folder/'Verify.exe';mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
    command=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',*["-r:"+str(mono/'lib/mono/4.5'/name) for name in ('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),str(PROJECT/'SourceCatalog/RocketFlightHarness.cs'),
             str(PROJECT/'Assets/Foundation/Runtime/OriginalExplodeData.cs'),str(PROJECT/'Assets/Foundation/Runtime/OriginalRocketFlightStrategy.cs')]
    result=subprocess.run(command,capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    actual=result.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
    for case,(a,e) in enumerate(zip(actual,expected)):
        aa,ee=a.split(';'),e.split(';');assert len(aa)==len(ee),(case,'frame count',len(aa),len(ee))
        for frame,(av,ev) in enumerate(zip(aa,ee)):
            ap,at=av.split('#');ep,et=ev.split('#');ap=ap.split(',');ep=ep.split(',')
            assert at==et and ap[0]==ep[0],(case,frame,av,ev)
            assert ap[1:]==ep[1:],(case,frame,av,ev)
db=sqlite3.connect(RECOVERY/'code/native/functions.sqlite3');methods=[]
for rva in (0x27bd1bc,0x27be674,0x27bd040,0x27bd0e0,0x27be37c):
    name,body=db.execute('select name,body from functions where address=?',(rva,)).fetchone();methods.append({'rva':hex(rva),'name':name,'sha256':hashlib.sha256(body.encode()).hexdigest()})
report={'passed':True,'native_flights':len(cases),'position_bits_exact':True,'compiler':'Unity bundled Roslyn; bundled Mono runtime with default options','methods':methods,'scope':'Actual ARM64 horizontal/vertical projectile iterators vs compiled production C# frame traces: repeated cell hits, operation start/end, all freeze/unfreeze order, empty/edge paths, wide-device threshold and vertical completion callback. Variable/zero frame deltas and both directions are covered. Unity transform/time, native metadata/storage, no-jelly cell damage and delegate calls are supplied boundaries. Horizontal harness starts with one other part already completed to verify shared release; simultaneous dual launch, pools, model/tap/swap and source visuals are verified separately in the formal scene. No jelly/water/combo or rendered-original claim.'}
(PROJECT/'rocket_flight_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
