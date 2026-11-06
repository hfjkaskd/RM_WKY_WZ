"""Original ARM64 rocket strategy versus production C#, configured poses included."""
import sys
sys.dont_write_bytecode=True
import verify_board_layout as b
import json, re, struct, random, subprocess, tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import *
p=b.p
strategy,view,assets,sound=[0x10010000+i*0x1000 for i in range(4)]
parts={0x10020000:'b',0x10021000:'s',0x10022000:'p1',0x10023000:'p2',0x10024000:'h',0x10025000:'v'}
sprite=lambda i:0x10030000+i*0x100
bits=lambda f:struct.unpack('<I',struct.pack('<f',f))[0]
trace=[];state_hash=state_time=0
def boundary(uc,address,size,data):
    x0,x1,x2=(uc.reg_read(r) for r in (UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2));value=None
    if address in (0x3e473b8,0x3e4737c):value=x0
    elif address==0x3e49b30:trace.append('active:'+parts[x0]+':'+str(x1));value=0
    elif address in (0x27bb424,0x27bb44c,0x27bb478,0x27bb4a4,0x27bb4d0,0x27bb4fc,0x27bb528):
        value=sprite((0x27bb424,0x27bb44c,0x27bb478,0x27bb4a4,0x27bb4d0,0x27bb4fc,0x27bb528).index(address))
    elif address==0x3e50c6c:trace.append('sprite:'+parts[x0]+':'+str((x1-sprite(0))//0x100));value=0
    elif address==0x3e4e4a0:
        trace.append('pos:'+parts[x0]+':'+':'.join(str(uc.reg_read(r)) for r in (UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2)));value=0
    elif address==0x3626968:
        z=uc.reg_read(UC_ARM64_REG_S2);trace.append('euler:'+str(z))
        for reg,f in zip((UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2,UC_ARM64_REG_S3),(0.,0.,.70710677 if z else 0.,.70710677 if z else 1.)):uc.reg_write(reg,bits(f))
        value=0
    elif address==0x3e4e6fc:trace.append('rot:'+parts[x0]+':'+str(uc.reg_read(UC_ARM64_REG_S2))+':'+str(uc.reg_read(UC_ARM64_REG_S3)));value=0
    elif address==0x21805c0:trace.append('sound:'+str(x1)+':'+str(uc.reg_read(UC_ARM64_REG_S0)));value=0
    elif address==0x1e3fe50:trace.append('play:'+str(p.signed(x1&0xffffffff))+':'+str(x2)+':'+str(uc.reg_read(UC_ARM64_REG_S0)));value=0
    elif address==0x1e3f44c:
        trace.append('state:'+str(x1));uc.mem_write(uc.reg_read(UC_ARM64_REG_X8),bytes(36));value=0
    elif address==0x1e3ed48:value=state_hash
    elif address==0x1e3ed58:uc.reg_write(UC_ARM64_REG_S0,state_time);value=0
    elif address==0xffda88:raise AssertionError('Unexpected null native rocket boundary')
    if value is not None:
        uc.reg_write(UC_ARM64_REG_X0,value&0xffffffffffffffff);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,boundary)
def call(rva,*args):
    for i,v in enumerate(args):p.emu.reg_write(UC_ARM64_REG_X0+i,v&0xffffffffffffffff)
    p.emu.reg_write(UC_ARM64_REG_SP,0x101ff000);p.emu.reg_write(UC_ARM64_REG_LR,0x20000000)
    p.emu.emu_start(rva,0x20000000,count=200000)
    assert p.emu.reg_read(UC_ARM64_REG_PC)==0x20000000
    return p.emu.reg_read(UC_ARM64_REG_X0)
def resolve(slot):
    if slot not in b.resolved_slots:p.emu.reg_write(UC_ARM64_REG_X0,slot);b.boundary(p.emu,0xffd960,4,None)
    return p.read(slot,8)
for i,(slot,hash_value) in enumerate(((0x558da88,12345),(0x5595f00,-98765))):
    statics=0x10040000+i*0x100;p.put(resolve(slot)+0xb8,statics,8);p.put(statics,hash_value)
p.put(strategy+0x10,view,8);p.put(strategy+0x18,assets,8);p.put(strategy+0x30,sound,8)
for offset,obj in ((0x70,0x10020000),(0x78,0x10026000),(0x80,0x10021000),(0xa0,0x10025000),(0xa8,0x10024000),(0xb0,0x10022000),(0xb8,0x10023000)):p.put(view+offset,obj,8)
config=(p.PROJECT/'Assets/Foundation/Settings/RocketViewAssets.asset').read_text()
scalar=lambda key:re.search(r'^  '+key+r': (.+)$',config,re.M)[1]
poses=[]
for key in ('horizontalPose','verticalPose'):
    values=[]
    block=re.search(r'^  '+key+r':\n((?:    .*\n?)+)',config,re.M)[1]
    for field in ('BodyOffset','ShadowOffset','ShadowEuler','FirstPartOffset','SecondPartOffset'):
        vector=re.search(field+r': \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}',block)
        values.extend(str(bits(float(v))) for v in vector.groups())
    values.extend(re.search(field+r': (\d+)',block)[1] for field in ('BodySprite','FirstPartSprite','SecondPartSprite'))
    poses.append(','.join(values))
configline='|'.join([','.join(re.findall(r'^  - (.+)$',config,re.M)),scalar('horizontalCreationState'),scalar('verticalCreationState'),scalar('shadowSprite'),scalar('creationSound'),str(bits(float(scalar('creationSoundOffset')))),*poses])
cases=[];expected=[];rng=random.Random(0x27bc5c4)
for orientation in range(3):
    vertical=orientation!=0
    trace=[];call(0x27bdbb8 if vertical else 0x27bc244,strategy);cases.append('S,'+str(orientation));expected.append('-;'+'|'.join(trace))
    for sound_flag in range(2):
        for start in (0.,.5,1.,-1.,float('nan')):
            trace=[];p.emu.reg_write(UC_ARM64_REG_S0,bits(start));call(0x27bde70 if vertical else 0x27bc4ec,strategy,sound_flag)
            cases.append(','.join(map(str,['P',orientation,bits(start),sound_flag])));expected.append('-;'+'|'.join(trace))
    for n in range(128):
        state_hash=rng.choice((12345,-98765,0));state_time=bits(rng.choice((0.,-.1,.5,.99999994,1.,1.0000001,float('nan'),float('inf'),-float('inf'))))
        trace=[];value=call(0x27bdf48 if vertical else 0x27bc5c4,strategy)
        cases.append(','.join(map(str,['I',orientation,state_hash,state_time])));expected.append(str(value)+';'+'|'.join(trace))
with tempfile.TemporaryDirectory() as folder:
    exe=Path(folder)/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(p.PROJECT/'SourceCatalog/RocketCreationHarness.cs'),str(runtime/'OriginalRocketCreationStrategy.cs'),str(runtime/'OriginalRocketViewAssets.cs')],capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join([configline,*cases])+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    actual=result.stdout.replace('\ufeff','').splitlines()
    assert actual==expected,next(((i,a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e),(len(actual),len(expected)))
report={'passed':True,'native_sprite_strategies':3,'native_animation_dispatch_cases':30,'native_animation_state_cases':384,'scope':'Actual ARM64 horizontal/vertical strategy SetSprites, PlayCreationAnimation and IsCreationAnimationPlaying versus production C# strategies and serialized production pose/path/state config. Water creation uses the inherited vertical native functions. Ordered active/sprite/local pose/rotation calls, 314/.04 sound dispatch, full-path hash/layer/start time and ordered normalized-time boundaries including NaN/infinities are compared. Unity component/property storage, quaternion conversion, sprite resource resolution, Animator state and SFX service are supplied boundaries. Concrete model/prefab/pool/player-loop checks are in the separate Unity verifier. Rocket explosion/combo travel, audio playback and original rendered parity are not asserted.'}
(p.PROJECT/'rocket_creation_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
