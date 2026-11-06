"""Compare ordinary ItemParticles and box layer dispatch with actual ARM64."""
import sys
sys.dont_write_bytecode=True
import verify_board_layout as b
import json,struct,subprocess,tempfile,random
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import *
p=b.p
owner=0x10010000;array=0x10020000;systems=[0x10030000+i*0x100 for i in range(4)];shadows=[0x10040000+i*0x100 for i in range(4)];shadow_systems=[0x10050000+i*0x100 for i in range(4)]
system_ids={ps:i for i,ps in enumerate(systems)}|{ps:10+i for i,ps in enumerate(shadow_systems)}
bits=lambda v:struct.unpack('<I',struct.pack('<f',v))[0]
vec=lambda values:','.join(map(str,values))
vector_regs=(UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2)
trace=[];routines=[];cfg={};maxes={}
def curve(uc,ps,tag):
 target=uc.reg_read(UC_ARM64_REG_X8);uc.mem_write(target,bytes(32));p.put(target,tag);p.put(target+8,ps,8)
def boundary(uc,address,size,data):
 x0,x1,x2=[uc.reg_read(r)for r in (UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2)];value=None
 if address in (0xd00b50,0xd018a0):uc.mem_write(x0,bytes(uc.mem_read(x1,x2)));value=x0
 elif address==0xd01fa0:uc.mem_write(x0,bytes([x1&255])*x2);value=x0
 elif address==0x38df524:
  for i,reg in enumerate(vector_regs):p.put(x0+i*4,uc.reg_read(reg))
  value=0
 elif address==0x38df538:p.put(x0+12,x1);value=0
 elif address==0x38db1c4:value=cfg['playing']
 elif address==0x362ead8:uc.reg_write(UC_ARM64_REG_S0,cfg['seed']);value=0
 elif address==0x362eb18:trace.append('range:'+str(p.signed(x0&0xffffffff))+':'+str(p.signed(x1&0xffffffff)));value=x0
 elif address==0x38db300:trace.append('seed:'+str(system_ids[x0])+':'+str(x1));value=0
 elif address in (0x38dbaa4,0x38dbacc):value=x0
 elif address==0x38dd91c:value=cfg['bursts']
 elif address==0x38dd958:
  target=uc.reg_read(UC_ARM64_REG_X8);uc.mem_write(target,bytes(56));p.put(target,p.read(x0,8),8);value=0
 elif address==0x38df0c4:curve(uc,p.read(x0,8),1);value=0
 elif address==0x38dbfe0:curve(uc,p.read(x0,8),2);value=0
 elif address==0x38df1b4:uc.reg_write(UC_ARM64_REG_S0,cfg['amount']if p.read(x0)==1 else cfg['delay']);value=0
 elif address==0x38df1ac:value=cfg['mode']
 elif address in (0x38df12c,0x38df0f0):value=(cfg['high']if address==0x38df12c else cfg['low'])&0xffffffff
 elif address==0x38dd1e8:value=maxes[p.read(x0,8)]
 elif address==0x38db23c:value=cfg['count']+(1 if x0 in shadow_systems else 0)
 elif address==0x38dd260:
  ps=p.read(x0,8);maxes[ps]=p.signed(x1&0xffffffff);trace.append('max:'+str(system_ids[ps])+':'+str(maxes[ps]));value=0
 elif address==0x38db9b8:trace.append('emit:'+str(system_ids[x0])+':'+str(p.signed(x2&0xffffffff))+':'+vec([p.read(x1+i*4)for i in range(3)])+':'+str(p.read(x1+12)));value=0
 elif address==0x2d74718:trace.append('shadow:'+str(shadows.index(x0))+':'+str(p.signed(x1&0xffffffff))+':'+vec([uc.reg_read(r)for r in vector_regs]));value=0
 elif address in (0x2b48fd0,0x2b491d4):value=0 # World-space identity; separate arithmetic verification covers other spaces.
 elif address==0x3e4d92c:value=int(x0!=x1)
 elif address==0x3e4c364:trace.append('start');routines.append(x1);value=0
 elif address in (0xffda88,0xffda90):raise AssertionError('Unexpected native null/index fault')
 if value is not None:uc.reg_write(UC_ARM64_REG_X0,value&0xffffffffffffffff);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,boundary)
def call(rva,*args,position=(0,0,0)):
 for i,v in enumerate((*args,0,0,0)):p.emu.reg_write(UC_ARM64_REG_X0+i,v&0xffffffffffffffff)
 for reg,v in zip(vector_regs,position):p.emu.reg_write(reg,v)
 p.emu.reg_write(UC_ARM64_REG_SP,0x101ff000);p.emu.reg_write(UC_ARM64_REG_LR,0x20000000)
 try:p.emu.emu_start(rva,0x20000000,count=500000)
 except Exception:print('Native failed',hex(p.emu.reg_read(UC_ARM64_REG_PC)),case,trace);raise
 assert p.emu.reg_read(UC_ARM64_REG_PC)==0x20000000
 return p.emu.reg_read(UC_ARM64_REG_X0)&1
p.put(owner+0x28,0x10000999,8)
for i in range(4):
 p.put(shadows[i]+0x18,shadow_systems[i],8)
 p.put(owner+0x48-i*8,array+(i+1)*0x1000,8)
 p.put(array+(i+1)*0x1000+0x18,1)
 p.put(array+(i+1)*0x1000+0x20,systems[i],8)
rng=random.Random(0x2b48aec);cases=[];expected=[];wait_steps=0
for op in ('P','B'):
 for n in range(400):
  layer=n%6-1;bursts=n//2%3;playing=n//3%2;shadow=n//5%2;mode=n//7%4;capacity=rng.choice((0,1,4,10,100));count=rng.choice((0,1,3,10))
  amount=bits(rng.choice((-1.,0.,1.,2.5,4.,float('nan'),float('inf'))));delay=bits(rng.choice((-.1,0.,.0249,.025,.0499,.05,.1,.125,float('nan'),float('inf'))));seed=bits(rng.choice((0.,12345.,4294967296.,-1.,float('nan'))));low,high=rng.choice(((1,4),(7,3),(0,0)))
  position=tuple(bits(rng.uniform(-4,4))for _ in range(3));cfg={'bursts':bursts,'playing':playing,'mode':mode,'count':count,'amount':amount,'delay':delay,'seed':seed,'low':low,'high':high}
  case=','.join(map(str,(op,layer,bursts,playing,shadow,mode,capacity,count,amount,delay,seed,low,high,*position)));cases.append(case)
  p.put(array+0x18,3)
  for i in range(4):
   present=shadows[i]if shadow and i!=1 else 0
   p.put(array+0x20+i*16,systems[i],8);p.put(array+0x28+i*16,present,8)
   p.put(array+(i+1)*0x1000+0x28,present,8)
  maxes={ps:capacity for ps in system_ids};trace=[];routines=[];p.heap=0x10100000
  if op=='P':call(0x2b48aec,owner,array,position=position)
  else:call(0x2bc61a8,owner,layer,position=position)
  for routine in routines:
   waits=0
   while call(0x2b494f4,routine):
    assert p.read(routine+0x18,8)==0x10000999
    waits+=1;assert waits<=120
   wait_steps+=waits;trace.append('waits:'+str(waits))
  expected.append('|'.join(trace))
runtime=p.PROJECT/'Assets/Foundation/Runtime'
with tempfile.TemporaryDirectory()as folder:
 exe=Path(folder)/'Verify.exe'
 cmd=[str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(p.PROJECT/'SourceCatalog/ItemParticleEmissionHarness.cs'),*[str(runtime/f)for f in ('OriginalItemParticleEmission.cs','OriginalBoxExplodeParticles.cs','OriginalParticleShadowMath.cs')]]
 result=subprocess.run(cmd,capture_output=True,text=True,encoding='utf8');assert result.returncode==0,result.stdout+result.stderr
 result=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf8');assert result.returncode==0,result.stdout+result.stderr
 actual=result.stdout.lstrip('\ufeff').splitlines();assert actual==expected,next(((i,cases[i],a,e)for i,(a,e)in enumerate(zip(actual,expected))if a!=e),(len(actual),len(expected)))
report={'passed':True,'native_cases':len(cases),'native_delayed_wait_steps':wait_steps,'scope':'Actual ARM64 ItemParticles.PlayParticles, StartEmit factory/MoveNext and inlined BoxItemExplodeParticles.Play versus unchanged production emitter. Covers three-system batches, box layer 0..3 and invalid layers, playing/seed reuse, paired shadows, absent bursts, constant/nonconstant burst sizes, inclusive integer random bounds, capacity thresholds/growth, NaN/infinity conversions, original/arranged positions and cached wait identity/count. Unity module/random/Emit/storage, managed iterator allocation and host scheduling are supplied; all positions use World space so rotation/scale helpers are identity boundaries (their separate verification covers arithmetic). Delayed coroutines are driven in a supplied deterministic order; Unity real scheduling/rendered particles and original visual parity require runtime verification.'}
(p.PROJECT/'item_particle_emission_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
