"""Execute native optimized-particle timing/size/rate/seed paths, compare production C#."""
import sys
sys.dont_write_bytecode = True
import verify_board_layout as b
import json, math, random, struct, subprocess, tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import *
p = b.p
owner, system, master, entry, listing, array, dictionary = (0x10010000+i*0x1000 for i in range(7))
bits = lambda f: struct.unpack('<I', struct.pack('<f', f))[0]
number = lambda n: struct.unpack('<f', struct.pack('<I', n))[0]
normal = lambda n: 'nan' if math.isnan(number(n)) else str(n)
vector_regs = (UC_ARM64_REG_S0, UC_ARM64_REG_S1, UC_ARM64_REG_S2)
configuration = {}; emits = []; evaluations = []; random_values = []; occupied = set()

def curve(uc, tag):
    target = uc.reg_read(UC_ARM64_REG_X8)
    uc.mem_write(target, bytes(32)); p.put(target, tag)

def boundary(uc, address, size, unused):
    x0,x1,x2 = [uc.reg_read(r) for r in (UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2)]
    value = None
    if address in (0xd00b50,0xd018a0): uc.mem_write(x0,bytes(uc.mem_read(x1,x2))); value=x0
    elif address==0xd01fa0: uc.mem_write(x0,bytes([x1&255])*x2); value=x0
    elif address==0x38dd170: value=configuration['scaling']
    elif address==0x3e4f9b4:
        for r,v in zip(vector_regs,configuration['scale']): uc.reg_write(r,v)
        value=0
    elif address==0x35084c8:
        for r,v in zip(vector_regs,configuration['previous']): uc.reg_write(r,v)
        value=0
    elif address in (0x38dc3ec,0x38dc518,0x38dc6dc,0x38dd3e8,0x38dd514,0x38df0c4):
        curve(uc,{0x38dc3ec:1,0x38dc518:2,0x38dc6dc:3,0x38dd3e8:4,0x38dd514:5,0x38df0c4:6}[address]);value=0
    elif address==0x38df1ac: value=configuration['mode'] if p.read(x0)==1 else 0
    elif address in (0x38df1b4,0x38df10c,0x38df148):
        uc.reg_write(UC_ARM64_REG_S0,configuration['curves'][p.read(x0)]);value=0
    elif address==0x38df1c4: value=configuration['curve_flags'].get(p.read(x0),0)
    elif address==0x38df1cc:
        evaluations.append(normal(uc.reg_read(UC_ARM64_REG_S0)))
        uc.reg_write(UC_ARM64_REG_S0,configuration['curves'][p.read(x0)]);value=0
    elif address==0x362ead8: uc.reg_write(UC_ARM64_REG_S0,random_values.pop(0));value=0
    elif address in (0x38dbaa4,0x38dbacc): value=x0
    elif address==0x38dbdd0: uc.reg_write(UC_ARM64_REG_S0,configuration['duration']);value=0
    elif address==0x32037e4: uc.reg_write(UC_ARM64_REG_S0,configuration['delta']);value=0
    elif address==0x38dbee0: value=configuration['loop']
    elif address==0x38dd91c: value=configuration['bursts']
    elif address==0x38dd958:
        target=uc.reg_read(UC_ARM64_REG_X8);uc.mem_write(target,bytes(56));value=0
    elif address==0x2b50648: emits.append(p.signed(uc.reg_read(UC_ARM64_REG_X5)&0xffffffff));value=0
    elif address==0x2c1b9a0: assert x0==listing and x1==0;value=entry
    elif address==0x2c1d0e4: assert x0==listing and x1==entry;p.put(listing+0x18,0);value=1
    elif address in (0x3e4d92c,0x3e3b754):
        same=x0==x1;value=int(not same if address==0x3e4d92c else same)
    elif address==0x39bf470: value=int(x1&0xffffffff in occupied)
    elif address in (0xffda88,0xffda90): raise AssertionError('Unexpected native null/index fault')
    if value is not None:
        uc.reg_write(UC_ARM64_REG_X0,value&0xffffffffffffffff)
        uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))

p.emu.hook_add(UC_HOOK_CODE,boundary)
def call(rva,args):
    for i,value in enumerate(args):p.emu.reg_write(UC_ARM64_REG_X0+i,value)
    p.emu.reg_write(UC_ARM64_REG_SP,0x101ff000);p.emu.reg_write(UC_ARM64_REG_LR,0x20000000)
    p.emu.emu_start(rva,0x20000000,count=100000)
    assert p.emu.reg_read(UC_ARM64_REG_PC)==0x20000000

# Native Vector3.one static field (the no-hierarchy branch returns it).
slot=0x5595e08
if slot not in b.resolved_slots:
    p.emu.reg_write(UC_ARM64_REG_X0,slot);b.boundary(p.emu,0xffd960,4,None)
p.put(p.read(slot,8)+0xb8,0x10048000,8)
for i in range(3):p.put(0x1004800c+i*4,bits(1.))
rng=random.Random(0x2b4e470);cases=[];expected=[]
for n in range(192):
    p.emu.mem_write(owner,bytes(0x8000));p.put(owner+0x38,dictionary,8)
    scale=[bits(rng.choice((0.,.25,1.,2.,-2.,3.5))) for _ in range(3)]
    previous=[bits(rng.choice((0.,.5,1.,-1.,4.))) for _ in range(3)]
    multiplier=[bits(rng.choice((0.,1.,.25,-3.,2.))) for _ in range(3)]
    sizes=[bits(rng.uniform(-2,5)) for _ in range(4)]
    configuration={'scaling':n%3,'mode':n//3%4,'scale':scale,'previous':previous,'curves':{1:0,2:sizes[0],3:sizes[1]}}
    random_values=sizes[2:]
    for i,v in enumerate(multiplier):p.put(entry+0xbc+i*4,v)
    cases.append(','.join(map(str,['S',n%3,n//3%4,*scale,*previous,*multiplier,*sizes])))
    call(0x2b50764,[owner,system,master,entry])
    expected.append(','.join(normal(v) for v in [*[p.emu.reg_read(r) for r in vector_regs],*[p.read(entry+0xbc+i*4) for i in range(3)]]))

for kind,total in [('R',192),('D',96)]:
    for n in range(total):
        p.emu.mem_write(owner,bytes(0x8000));p.put(owner+0x50,listing,8);p.put(listing+0x18,1);p.put(entry+0x10,system,8)
        duration=bits(rng.choice((.1,.2,.5,1.,2.)))
        time_rate=bits(rng.choice((0.,.1,1.,4.,12.5,100.)))
        distance_rate=bits(rng.choice((0.,1.,2.,20.)))
        delta=bits(rng.choice((0.,.0166,.0333,.1,.2,.5)))
        remaining=bits(rng.choice((0.,.0166,.1,.5,2.)))
        delay=bits(rng.choice((-.1,0.,.0166,.05,.2,.5)))
        accumulator=bits(rng.choice((0.,.1,.5,.99)))
        velocity=[bits(rng.uniform(-5,5)) for _ in range(3)]
        loop=n%2;bursts=n//2%2;burst=bits(float(n%6))
        time_curve=int(n%7==0);distance_curve=int(n%5==0)
        configuration={'scaling':1,'duration':duration,'delta':delta,'loop':loop,'bursts':bursts,'curve_flags':{4:time_curve,5:distance_curve},'curves':{4:time_rate,5:distance_rate,6:burst}}
        for offset,value in [(0xd0,remaining),(0xd4,delay),(0xd8,accumulator),*[(0xdc+4*i,v) for i,v in enumerate(velocity)]]:p.put(entry+offset,value)
        cases.append(','.join(map(str,[kind,duration,time_rate,time_curve,distance_rate,distance_curve,delta,remaining,delay,accumulator,*velocity,loop,bursts,burst])))
        for frame in range(12 if kind=='D' else 1):
            emits=[];evaluations=[]
            call(0x2b4f4ac if kind=='D' else 0x2b50a10,[owner,entry])
            state=','.join(map(str,[p.read(listing+0x18),*[normal(p.read(entry+o)) for o in (0xd0,0xd4,0xd8)]])) if kind=='D' else normal(p.read(entry+0xd8))
            expected.append(state+';'+','.join(map(str,emits))+';'+','.join(evaluations))

for n in range(96):
    initial=n%4;occupied=set(range(n%5));random_values=[bits(float(i)) for i in range(10)]
    if n%4==0:random_values=[bits(float('inf'))]
    if n%4==1:random_values=[bits(-1.),bits(float('nan')),*random_values]
    values=list(random_values);p.put(owner+0x40,dictionary,8)
    cases.append(','.join(map(str,['K',initial,len(occupied),*sorted(occupied),*values])))
    call(0x2b51128,[owner,initial])
    expected.append(str(p.emu.reg_read(UC_ARM64_REG_X0)&0xffffffff))

with tempfile.TemporaryDirectory() as folder:
    exe=Path(folder)/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
    command=[str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(p.PROJECT/'SourceCatalog/OptimizedParticlesHarness.cs'),str(runtime/'OriginalOptimizedItemParticles.cs'),str(runtime/'OriginalParticleShadowMath.cs')]
    result=subprocess.run(command,capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    actual=result.stdout.replace('\ufeff','').splitlines()
    assert actual==expected,next(((i,cases[i] if i<384 else '',a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e),(len(actual),len(expected)))
report={'passed':True,'native_size_cases':192,'native_rate_cases':192,'native_delayed_sequences':96,'native_delayed_frames':1152,'native_seed_collision_cases':96,
 'scope':'Actual ARM64 SetStartSize3D, EmitRateOverTimeAndDistanceParticles, CheckAndEmitDelayedEmitParticles, EmitBurstParticles and GetParticleRandomSeed versus unchanged production C#. Supplied Unity module/curve/random/storage boundaries; bitwise output comparison including accumulator, curve-evaluation times, loop/delay crossings and emitted counts. Delayed sequences use no master and Local scaling. Master follow/ownership, real curve interpolation, Unity particle simulation and typed recycle scheduler/Invoke phase parity are outside this native comparison.'}
(p.PROJECT/'optimized_particles_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
