from hashlib import sha256
from pathlib import Path
import json
import random
import sqlite3
import struct
import subprocess
import sys

ROOT=Path(__file__).resolve().parents[1]
if len(sys.argv)!=2:raise SystemExit('Usage: verify_goal_icon_animation.py OUTPUT_DIRECTORY')
WORK=Path(sys.argv[1]).resolve()
WORK.mkdir(parents=True,exist_ok=True)
sys.path.insert(0,str(ROOT.parent/'tools/python'))
from elftools.elf.elffile import ELFFile
from elftools.elf.relocation import RelocationSection
from unicorn import Uc,UC_ARCH_ARM64,UC_MODE_ARM,UC_HOOK_CODE
import unicorn.arm64_const as r
u=Uc(UC_ARCH_ARM64,UC_MODE_ARM);u.mem_map(0,0x6000000);u.mem_map(0x10000000,0x400000);u.mem_map(0x20000000,0x1000)
with (ROOT.parent.parent/'RoyalMatch_20260929/apk_unpacked/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so').open('rb') as stream:
    elf=ELFFile(stream)
    for segment in elf.iter_segments():
        if segment['p_type']=='PT_LOAD':u.mem_write(segment['p_vaddr'],segment.data())
    for section in elf.iter_sections():
        if isinstance(section,RelocationSection):
            for item in section.iter_relocations():
                if item['r_info_type']==1027:u.mem_write(item['r_offset'],struct.pack('<Q',item['r_addend']))
def bits(v):return struct.unpack('<I',struct.pack('<f',v))[0]
def val(v):return struct.unpack('<f',struct.pack('<I',v))[0]
def f32(v):return val(bits(v))
def put(p,v,n=8):u.mem_write(p,int(v).to_bytes(n,'little'))
def get(p,n=8):return int.from_bytes(u.mem_read(p,n),'little')
def fp(p,v):put(p,bits(v),4)
def ret(v=0):u.reg_write(r.UC_ARM64_REG_X0,v);u.reg_write(r.UC_ARM64_REG_PC,u.reg_read(r.UC_ARM64_REG_LR))
def x(i):return u.reg_read(getattr(r,'UC_ARM64_REG_X'+str(i)))
def floats(n):return tuple(val(u.reg_read(getattr(r,'UC_ARM64_REG_S'+str(i)))) for i in range(n))
def call(address,*args):
    for i,arg in enumerate(args):u.reg_write(getattr(r,'UC_ARM64_REG_X'+str(i)),arg)
    u.reg_write(r.UC_ARM64_REG_SP,0x103ff000);u.reg_write(r.UC_ARM64_REG_LR,0x20000000)
    u.emu_start(address,0x20000000,count=20000)
    assert u.reg_read(r.UC_ARM64_REG_PC)==0x20000000,hex(u.reg_read(r.UC_ARM64_REG_PC))
    return u.reg_read(r.UC_ARM64_REG_X0)

goal,text,text_class,renderer,transform,manager,circle,particles=[0x10000000+i*0x1000 for i in range(8)]
for offset,pointer in ((0x18,renderer),(0x30,text),(0x50,manager),(0x40,circle),(0x48,particles)):put(goal+offset,pointer)
put(text,text_class);put(text_class+0x518,0x20000010);put(goal+0x80,21,4);put(goal+0x84,1,1)
metadata=0x10100000;next_object=0x10200000
active=[];objects={};flying=False;current=(1.,1.,1.);original=current
particle_calls=[];native_creations=[];startup_reads=0
def create(kind):
    global next_object
    p=next_object;next_object+=0x200
    objects[p]={'kind':kind,'elapsed':0.,'started':False,'children':[],'active':True}
    active.append(p);return p
def hook(uc,address,size,unused):
    global metadata,next_object
    a,b=x(0),x(1)
    if address==0xffd960:
        put(a,metadata);put(metadata+0xe0,1,4);put(metadata+0xb8,metadata+0x200);metadata+=0x400;ret()
    elif address in (0xffd90c,0xffda64):ret()
    elif address in (0xffda88,0xffda90):raise RuntimeError(('native null',hex(address)))
    elif address==0xffda78:result=next_object;next_object+=0x200;ret(result)
    elif address in (0x2653db0,):ret(0x10009000)
    elif address in (0x20000010,0x3e4944c):ret()
    elif address==0x3213d54:ret(int(flying))
    elif address==0x38db1c4:ret(0)
    elif address==0x38db708:particle_calls.append(a);ret()
    elif address==0x3e4737c:ret(transform)
    elif address==0x1d7ad40:ret(create('sequence'))
    elif address==0x1d80490:
        target=floats(3);duration=floats(4)[3];p=create('tween')
        objects[p].update(target=target,duration=duration,callback=False)
        native_creations.append({'target':target,'duration':duration});ret(p)
    elif address==0x1d81ee8:
        objects[a]['children'].append(b);active.remove(b);ret(a)
    elif address in (0x291e7d4,):ret(a)
    elif address==0x1d7cd5c:ret(int(objects.get(a,{}).get('active',False)))
    elif address==0x1d788fc:ret()
    elif address==0x2922140:objects[a]['callback']=True;ret(a)
    elif address==0x20000020:
        # Synthetic getter/delegate transport for the real generic startup.
        for i,value in enumerate(current):uc.reg_write(getattr(r,'UC_ARM64_REG_S'+str(i)),bits(value))
        ret()
    elif address in (0x20000024,0x20000028,0x2000002c):ret(1)
u.hook_add(UC_HOOK_CODE,hook)

# The real Tweener.DoStartup getter path with explicit delegate/plugin ports.
startup,method,rgctx,invoke,plugin,plugin_class=range(0x10010000,0x10016000,0x1000)
put(method+0x38,rgctx);put(rgctx+0x10,invoke);put(invoke+8,0x20000020)
put(rgctx+0x38,invoke+0x100);put(invoke+0x108,0x20000024)
put(startup+0x148,0x10017000);put(startup+0x158,plugin);put(plugin,plugin_class)
put(plugin_class+0x1a8,0x20000028);put(plugin_class+0x1c8,0x2000002c);fp(startup+0xa0,.1)
def startup_value():
    global startup_reads
    put(startup+0xf4,0,4);put(startup+0x118,0,1);put(startup+0xb0,0,1)
    assert call(0x292a9ac,startup,method)==1
    startup_reads+=1
    return tuple(val(get(startup+offset,4)) for offset in (0x11c,0x120,0x124))
def ease(kind,elapsed,duration,overshoot=1.70158):
    for i,value in enumerate((elapsed,duration,overshoot,0.)):u.reg_write(getattr(r,'UC_ARM64_REG_S'+str(i)),bits(value))
    call(0x33d5f78,kind,0)
    return val(u.reg_read(r.UC_ARM64_REG_S0))
def apply(tween,time):
    global current
    if not tween['started']:
        tween['start']=startup_value();tween['started']=True
    ratio=ease(1,min(time,tween['duration']),tween['duration'])
    current=tuple(f32(a+f32(f32(b-a)*ratio)) for a,b in zip(tween['start'],tween['target']))
def update(delta):
    # This supplied dispatcher retains the snapshot and stable ordering visible
    # in 0x33dcf78; native pulse construction/startup/ease execute above.
    if delta==0:return
    for p in list(active):
        obj=objects[p];obj['elapsed']=f32(obj['elapsed']+delta)
        if obj['kind']=='sequence':
            first,second=[objects[c] for c in obj['children']]
            if obj['elapsed']<first['duration']:apply(first,obj['elapsed'])
            else:
                if not first.get('done'):
                    apply(first,first['duration']);first['done']=True
                apply(second,f32(obj['elapsed']-first['duration']))
            completed=obj['elapsed']>=f32(first['duration']+second['duration'])
        else:
            apply(obj,obj['elapsed']);completed=obj['elapsed']>=obj['duration']
        if completed:
            obj['active']=False;active.remove(p)
            if obj.get('callback'):call(0x18ccc6c,goal,0)
def state():return len(active),int(objects.get(get(goal+0x90),{}).get('active',False)),*map(bits,current)

commands=[];expected=[];native_operations=0
def command(text):
    global current,original,flying,active,objects,next_object,native_operations
    a=text.split(',')
    if a[0]=='I':
        current=original=tuple(val(int(v)) for v in a[1:]);active=[];objects={};put(goal+0x90,0)
        for offset,value in zip((0x64,0x68,0x6c),original):fp(goal+offset,value)
    elif a[0]=='P':
        flying=a[1]=='1';before=len(native_creations)
        call(0x18cc5cc,goal,21,7,0,0,0)
        native_operations+=1;expected.append(str(int(len(native_creations)>before)));commands.append(text);return
    elif a[0]=='T':update(val(int(a[1])))
    elif a[0]=='S':current=tuple(val(int(v)) for v in a[1:])
    elif a[0]=='C':active=[];objects={};put(goal+0x90,0)
    expected.append(','.join(map(str,state())));commands.append(text)
rng=random.Random(18052026)
for scale in ((1.,1.,1.),(.8,1.2,0.),(1.15,.65,1.)):
    for cadence in (30,60,90,0):
        command('I,'+','.join(map(str,map(bits,scale))))
        for frame in range(180):
            if frame in (0,1,2,5,6,15,18,23,55,56,57,58,90,93,120):
                for _ in range(2 if frame in (0,55) else 1):command('P,'+str(int(frame%3==0)))
            if frame==70:command('S,'+','.join(map(str,map(bits,(.7,1.3,.4)))))
            if frame==110:command('C')
            raw=f32(1/(cadence or (30,60,90)[frame%3]))
            delta=f32(int(f32(raw*10000))/10000)
            if frame in (0,10,11):delta=raw=0.
            command('T,'+str(bits(delta))+','+str(bits(raw)))

data=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data');mono=data/'MonoBleedingEdge'
exe=WORK/'GoalIconHarness.exe'
process=subprocess.run([str(data/'NetCoreRuntime/dotnet.exe'),str(data/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',
                       *['-r:'+str(mono/'lib/mono/4.5'/name) for name in ('mscorlib.dll','System.dll','System.Core.dll')],
                       '-out:'+str(exe),str(ROOT/'SourceCatalog/GoalIconAnimationHarness.cs'),str(ROOT/'Assets/Foundation/Runtime/OriginalGoalIconAnimation.cs')],
                      capture_output=True,text=True,creationflags=subprocess.CREATE_NO_WINDOW)
assert process.returncode==0,process.stdout+process.stderr
process=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(commands+['G'])+'\n',capture_output=True,text=True,creationflags=subprocess.CREATE_NO_WINDOW)
assert process.returncode==0,process.stdout+process.stderr
actual=process.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected)+1
max_error=0.
for index,(got,want) in enumerate(zip(actual,expected)):
    if commands[index].startswith('P'):
        assert got==want,(index,commands[index],got,want);continue
    a=list(map(int,got.split(',')));b=list(map(int,want.split(',')))
    assert a[:2]==b[:2],(index,commands[index],a,b)
    error=max(abs(val(x)-val(y)) for x,y in zip(a[2:],b[2:]));max_error=max(max_error,error)
    assert error<.000002,(index,commands[index],a,b,error)
assert actual[-1]=='GC,0',actual[-1]
report={'passed':True,'commands':len(commands),'native_goal_updates':native_operations,'native_startup_reads':startup_reads,
        'max_scale_error':max_error,'gc_bytes_10000_active_ticks':0,
        'scope':'Actual original ARM64 GoalUi.OnGoalUpdated/return callback emit tween targets, durations and suppression; generic Tweener.DoStartup reads the live transform through supplied getter/plugin ports; native EaseManager evaluates Linear. The independent active-range/sequence dispatcher and vector transform transport are supplied, guided by the original update and sequence bodies. This is not execution of the entire native DOTween manager or proof of original rendered parity.'}
(WORK/'goal_icon_comparison.json').write_text(json.dumps(report,indent=2)+'\n')
(WORK/'commands.txt').write_text('\n'.join(commands)+'\n')
(WORK/'expected.txt').write_text('\n'.join(expected)+'\n')
(WORK/'actual.txt').write_text(process.stdout)
print(json.dumps(report,indent=2))
