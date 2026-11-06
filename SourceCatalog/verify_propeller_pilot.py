"""Execute original DefaultPropellerPilot ARM64 and compare compiled runtime flight traces."""
from pathlib import Path
import hashlib,json,math,random,sqlite3,struct,subprocess,sys,tempfile
sys.path.insert(0,str(Path(__file__).resolve().parents[2]/'tools/python'))
from elftools.elf.elffile import ELFFile
from elftools.elf.relocation import RelocationSection
from unicorn import Uc,UC_ARCH_ARM64,UC_MODE_ARM,UC_HOOK_CODE
import unicorn.arm64_const as regs

PROJECT=Path(__file__).resolve().parents[1];RECOVERY=PROJECT.parent
lib=RECOVERY.parent/'RoyalMatch_20260929/apk_unpacked/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so'
emu=Uc(UC_ARCH_ARM64,UC_MODE_ARM);emu.mem_map(0,0x6000000);emu.mem_map(0x10000000,0x200000);emu.mem_map(0x20000000,0x1000)
def put(a,v,size=4):emu.mem_write(a,(int(v)&((1<<(size*8))-1)).to_bytes(size,'little'))
def read(a,size=4):return int.from_bytes(emu.mem_read(a,size),'little')
def f(v):return struct.unpack('<f',struct.pack('<f',v))[0]
def bits(v):return struct.unpack('<I',struct.pack('<f',v))[0]
def value(v):return struct.unpack('<f',struct.pack('<I',v))[0]
def vs():return [value(emu.reg_read(getattr(regs,'UC_ARM64_REG_S'+str(i)))) for i in range(8)]
def setv(values):
    for i,v in enumerate(values):emu.reg_write(getattr(regs,'UC_ARM64_REG_S'+str(i)),bits(v))
with lib.open('rb') as stream:
    elf=ELFFile(stream)
    for segment in elf.iter_segments():
        if segment['p_type']=='PT_LOAD':emu.mem_write(segment['p_vaddr'],segment.data())
    for section in elf.iter_sections():
        if isinstance(section,RelocationSection):
            for relocation in section.iter_relocations():
                if relocation['r_info_type']==1027:put(relocation['r_offset'],relocation['r_addend'],8)

owner,owner_class,view,view_class,delegate,delegate_class,methods=0x10000000,0x10001000,0x10002000,0x10003000,0x10004000,0x10005000,0x10006000
trans,parent,anim,fan,shadow,shadow_component,hierarchy=0x10010000,0x10011000,0x10012000,0x10013000,0x10014000,0x10015000,0x10016000
generic,meta_heap=0x10080000,0x10090000
slots={};class_statics={};positions={};local={};scales={};angles={};quaternions={};events=[];ended=False;valid=True
frame=1;level_time=0;level_delta=0;unity_delta=0;target=[0,0,0];random_values=[];random_index=0
known_slots={read(got,8):name for got,name in {0x53df178:'vector',0x53aa610:'pilot',0x5356db8:'sorting',0x535ca48:'finder'}.items()}
def resolve(slot):
    global meta_heap
    if slot in slots:return slots[slot]
    pointer=meta_heap;meta_heap+=0x400;statics=meta_heap;meta_heap+=0x400
    slots[slot]=pointer;put(pointer+0xe0,1);put(pointer+0x132,1,1);put(pointer+0x12c,1,1);put(pointer+0xb8,statics,8)
    if slot in known_slots:class_statics[known_slots[slot]]=statics
    if known_slots.get(slot)=='vector':
        vectors=[(0,0,0),(1,1,1),(0,1,0),(0,-1,0),(-1,0,0),(1,0,0),(0,0,1),(0,0,-1)]
        for i,vector in enumerate(vectors):
            for j,v in enumerate(vector):put(statics+i*12+j*4,bits(v))
    if known_slots.get(slot)=='pilot':put(statics,bits(.5));put(statics+4,bits(-.6))
    if known_slots.get(slot)=='finder':put(statics,10000)
    if known_slots.get(slot)=='sorting':put(statics+0x14,41)
    return pointer
def position(pointer):
    return [f(local[trans][i]+local[parent][i]) for i in range(3)] if pointer==trans else local[pointer]
def quaternion(angle,axis):
    rad=angle*math.pi/180.0/2.0;s=f(math.sin(rad));q=[f(v*s) for v in axis]+[f(math.cos(rad))]
    quaternions[tuple(map(bits,q))]=angle;return q
def positive(v):return f(v+360) if v<f(-.005729578) else f(v-360) if v>f(359.99427) else v

def boundary(uc,address,size,unused):
    global random_index,ended
    x0,x1,x2=[uc.reg_read(getattr(regs,'UC_ARM64_REG_X'+str(i))) for i in range(3)];result=None
    if address==0xffd960:put(x0,resolve(x0),8);result=0
    elif address in (0xffd90c,0xffda64):result=x0
    elif address in (0xffda88,0xffda90,0xffde20):raise AssertionError(('Native exception',hex(address),hex(uc.reg_read(regs.UC_ARM64_REG_LR))))
    elif address==0xfcd934:result=methods+x2*16
    elif address==0x3e4737c:result=trans if x0==view else shadow if x0==shadow_component else 0
    elif address==0x3e4e2c8:setv(position(x0));result=0
    elif address==0x3e4e368:
        p=vs()[:3];local[x0]=[f(p[i]-local[parent][i]) for i in range(3)] if x0==trans else p;result=0
    elif address==0x3e4e400:setv(local[x0]);result=0
    elif address==0x3e4e4a0:local[x0]=vs()[:3];result=0
    elif address==0x3e4e958:setv(scales[x0]);result=0
    elif address==0x3e4e9f8:scales[x0]=vs()[:3];result=0
    elif address==0x3e4ea90:result=parent
    elif address==0x3e4f758:setv([f(v-local[x0][i]) for i,v in enumerate(vs()[:3])]);result=0
    elif address==0x36270ac:
        v=vs();setv(quaternion(v[0],v[1:4]));result=0
    elif address in (0x3e4e5f0,0x3e4e6fc):angles[x0]=quaternions[tuple(uc.reg_read(getattr(regs,'UC_ARM64_REG_S'+str(i))) for i in range(4))];result=0
    elif address==0x3e4e538:setv([0,0,positive(angles[x0])]);result=0
    elif address==0x3e4e674:setv(quaternion(angles[x0],[0,0,1]));result=0
    elif address==0x36268e0:
        angle=quaternions[tuple(uc.reg_read(getattr(regs,'UC_ARM64_REG_S'+str(i))) for i in range(4))];setv([0,0,f(angle*f(.0174532924))]);result=0
    elif address==0xd01b50:
        d=struct.unpack('<d',struct.pack('<Q',uc.reg_read(regs.UC_ARM64_REG_D0)))[0]
        uc.reg_write(regs.UC_ARM64_REG_D0,struct.unpack('<Q',struct.pack('<d',math.acos(d)))[0]);result=0
    elif address==0x3205d7c:result=frame
    elif address in (0x3205e3c,0x32037e4,0x3e424ec):setv([level_time if address==0x3205e3c else level_delta if address==0x32037e4 else unity_delta]);result=0
    elif address==0x362eba0:setv([random_values[random_index]]);random_index+=1;result=0
    elif address==0x27b1e14:events.append('Start');result=0
    elif address==0x2b4ac00:events.append('SortDown' if (x1>>32)<1000 else 'SortFly');result=0
    elif address==0x27b2510:events.append('Hit');result=0
    elif address==0x27b25a4:events.append('Fake');result=0
    elif address==0x20000400:events.append('Explode');ended=True;result=0
    elif address==0x20000300:result=int(valid)
    elif address==0x20000310:setv(target);result=0
    elif address==0x20000330:
        events.append('Force');uc.reg_write(regs.UC_ARM64_REG_X0,owner);uc.reg_write(regs.UC_ARM64_REG_PC,0x27c142c);return
    elif address in (0x27c1918,0x27c1b78):
        # Error-message boxing/format/Firebase logging only, after the actual
        # force-retarget/fake-hit callbacks. SDK behavior is outside this oracle.
        uc.reg_write(regs.UC_ARM64_REG_PC,0x27c1dc0);return
    elif address==0x27c2758:
        # Diagnostic logging only; native clamp at0x27c27e0 still executes.
        uc.reg_write(regs.UC_ARM64_REG_PC,0x27c27e0);return
    if result is not None:
        uc.reg_write(regs.UC_ARM64_REG_X0,result&0xffffffffffffffff);uc.reg_write(regs.UC_ARM64_REG_PC,uc.reg_read(regs.UC_ARM64_REG_LR))
emu.hook_add(UC_HOOK_CODE,boundary)

native_vtable={10:0x27c1138,11:0x27c1140,12:0x27c114c,13:0x27c1154,14:0x27c115c,15:0x27c1168,16:0x27c1170,17:0x27c1180,18:0x27c1424,19:0x27c142c,20:0x27c1478,21:0x27c157c,22:0x27c2174,23:0x27c217c,24:0x27c2654,25:0x27c26bc,26:0x27c26c4,27:0x27c26cc,28:0x27c30f0,29:0x27c3130,30:0x27c3384}
for slot,method in native_vtable.items():put(owner_class+0x138+slot*16,method,8)
put(view,view_class,8);put(view_class+0x12c,1,1);put(view_class+0xc8,hierarchy,8);put(hierarchy,resolve(read(0x5386530,8)),8)
put(view+0x80,shadow_component,8);put(view+0x98,anim,8);put(view+0xc0,fan,8);put(view_class+0x468,0x20000400,8)
put(delegate,delegate_class,8)
for slot in range(7):put(methods+slot*16,0x20000300+slot*16,8)
def call(rva,*args):
    for i,v in enumerate(args):emu.reg_write(getattr(regs,'UC_ARM64_REG_X'+str(i)),v)
    emu.reg_write(regs.UC_ARM64_REG_SP,0x101ff000);emu.reg_write(regs.UC_ARM64_REG_LR,0x20000000)
    try:emu.emu_start(rva,0x20000000,count=500000)
    except Exception as error:raise AssertionError((hex(rva),hex(emu.reg_read(regs.UC_ARM64_REG_PC)),hex(emu.reg_read(regs.UC_ARM64_REG_LR)))) from error
    assert emu.reg_read(regs.UC_ARM64_REG_PC)==0x20000000,hex(emu.reg_read(regs.UC_ARM64_REG_PC))
def snapshot():
    floats=position(trans)+scales[trans]+local[shadow]+[angles[anim],angles[fan]]
    return str(read(owner+0x78))+','+','.join(str(bits(v)) for v in floats)+'#'+','.join(events)

rng=random.Random(0x27c1180);cases=[];expected=[]
for n in range(65):
    init=[f(-4.0),f(-3.5),float(n%9),float((n//9)%8),float(rng.randrange(9)),float(rng.randrange(8)),f(rng.random()),f(rng.random())]
    random_values=init[6:];random_index=0;events=[];ended=False;angles={anim:0.0,fan:0.0};quaternions={}
    local={trans:init[2:4]+[0],parent:init[:2]+[0],shadow:[f(.01),f(-.2),0]};scales={trans:[1,1,1]};target=[f(init[4]+init[0]),f(init[5]+init[1]),0]
    emu.mem_write(owner,bytes(0xb0));put(owner,owner_class,8)
    frame_values=[];answers=[];time=0
    for i in range(240):
        frame=i+1 if n%8!=0 else i//2+1
        unity_delta=0.0 if n==63 else f(.0005) if n==64 else f(rng.choice([1/60,1/30,.01,.025,.7 if n==62 else .02]))
        level_delta=f(math.floor(unity_delta*10000)/10000) if n!=63 else f(.0166)
        time=f(time+level_delta);level_time=time;valid=not(n%7==0 and 5<i<12);changed=n%5==0 and i==20
        frame_values.append(','.join(map(str,(frame,bits(unity_delta),bits(level_delta),bits(level_time),int(valid),int(changed)))))
        if changed:call(0x27c142c,owner)
        call(0x27c1180,owner,view,delegate) if i==0 else call(0x27c1478,owner)
        answers.append(snapshot());events=[]
        if ended:break
    else:raise AssertionError(('Native pilot did not finish',n))
    cases.append(','.join(str(bits(v)) for v in init)+'|'+';'.join(frame_values));expected.append(';'.join(answers))

mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
with tempfile.TemporaryDirectory() as folder:
    exe=Path(folder)/'Verify.exe';command=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',*["-r:"+str(mono/'lib/mono/4.5'/name) for name in ('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),str(PROJECT/'SourceCatalog/PropellerPilotHarness.cs'),str(PROJECT/'Assets/Foundation/Runtime/OriginalDefaultPropellerPilot.cs')]
    r=subprocess.run(command,capture_output=True,text=True,encoding='utf8');assert r.returncode==0,r.stdout+r.stderr
    r=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf8');assert r.returncode==0,r.stdout+r.stderr
    actual=r.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected),(len(actual),len(expected));maximum=0
    for case,(a,e) in enumerate(zip(actual,expected)):
        aa,ee=a.split(';'),e.split(';');assert len(aa)==len(ee),(case,'frames',len(aa),len(ee))
        for frame_index,(av,ev) in enumerate(zip(aa,ee)):
            ap,at=av.split('#');ep,et=ev.split('#');ap=ap.split(',');ep=ep.split(',');assert ap[0]==ep[0] and at==et,(case,frame_index,av,ev)
            for field,(va,ve) in enumerate(zip(ap[1:],ep[1:])):
                delta=abs(value(int(va))-value(int(ve)));maximum=max(maximum,delta)
                assert delta<.00002,(case,frame_index,field,value(int(va)),value(int(ve)),av,ev)
db=sqlite3.connect(RECOVERY/'code/native/functions.sqlite3');methods=[]
for rva in sorted(set(native_vtable.values())):
    name,body=db.execute('select name,body from functions where address=?',(rva,)).fetchone();methods.append({'rva':hex(rva),'name':name,'sha256':hashlib.sha256(body.encode()).hexdigest()})
report={'passed':True,'native_flights':len(cases),'native_frames':sum(v.count(';')+1 for v in expected),'max_numeric_difference':maximum,'methods':methods,'scope':'Actual DefaultPropellerPilot StartFlying/Move/InitializeCurveVariables/position/scale/rotation/retarget/hit ARM64 bodies vs compiled production C#. Unity Transform and AngleAxis/euler engine boundaries, frame/time/random/target/view callbacks are supplied. Original managed SmoothDamp, quaternion-vector multiply, normalization/angle/math bodies execute natively. Diagnostic message boxing/Firebase logging bypassed after gameplay callbacks; large-delta log bypass retains native clamp. Covers edges/center, zero-distance, varying/zero/large/0.0005 uncapped-batch deltas, duplicate frame calls, invalid targets, external retarget and45-frame forced redirect/fake-hit. Numeric tolerance2e-5; state/event/frame transitions exact. Does not verify source Animator/audio/pools, live guided model ownership, full combos or rendered-original parity.'}
(PROJECT/'propeller_pilot_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items() if k!='methods'}))
