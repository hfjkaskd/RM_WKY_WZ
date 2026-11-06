"""Directional swap particles and cell flash setup against original ARM64."""
import sys
sys.dont_write_bytecode=True
import verify_board_layout as b
import json,re,struct,subprocess,tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import *
p=b.p
view,parent,array,flash,statics=[0x10010000+i*0x1000 for i in range(5)]
systems=[0x10020000+i*0x1000 for i in range(4)]
renderers=[0x10030000+i*0x1000 for i in range(3)]
trace=[];fault=False;scheduled=[]
def bits(v):return struct.unpack('<I',struct.pack('<f',v))[0]
def resolve(slot):
 if slot not in b.resolved_slots:p.emu.reg_write(UC_ARM64_REG_X0,slot);b.boundary(p.emu,0xffd960,4,None)
 return p.read(slot,8)
def boundary(uc,address,size,data):
 global fault
 x0,x1=[uc.reg_read(r) for r in (UC_ARM64_REG_X0,UC_ARM64_REG_X1)];value=None
 if address in (0x2b489c8,0x2b4e478):trace.append('base' if address==0x2b489c8 else 'optimized');value=0
 elif address==0x1fdc5c8:trace.append('get-renderers');value=array
 elif address==0x1fdc224:trace.append('get-renderer');value=renderers[0]
 elif address==0x3e4737c:value=x0
 elif address==0x3e4ec54:assert x1==parent;trace.append('parent');value=0
 elif address in (0x3e4e9f8,0x3e4e4a0,0x3e4e6fc):
  kind={0x3e4e9f8:'scale',0x3e4e4a0:'pos',0x3e4e6fc:'rot'}[address];regs=(UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2,UC_ARM64_REG_S3)
  trace.append(kind+':'+':'.join(str(uc.reg_read(r)) for r in regs[:4 if kind=='rot' else 3]));value=0
 elif address==0x38db708:trace.append('play:'+str(systems.index(x0)));value=0
 elif address==0x3e4bf38:
  assert x1==resolve(0x5617418) and uc.reg_read(UC_ARM64_REG_S0)==bits(1.)
  scheduled.append(21.);value=0
 elif address in (0x3e3ebc0,0x3e3ec40):trace.append(('L' if address==0x3e3ebc0 else 'O')+str(renderers.index(x0))+':'+str(p.signed(x1&0xffffffff)));value=0
 elif address==0xffda88:fault=True;uc.reg_write(UC_ARM64_REG_PC,0x20000000);return
 if value is not None:uc.reg_write(UC_ARM64_REG_X0,value);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,boundary)
def call(rva,*args):
 for i,value in enumerate((*args,0,0,0)):p.emu.reg_write(UC_ARM64_REG_X0+i,value&0xffffffffffffffff)
 p.emu.reg_write(UC_ARM64_REG_SP,0x101ff000);p.emu.reg_write(UC_ARM64_REG_LR,0x20000000)
 p.emu.emu_start(rva,0x20000000,count=200000)
 assert p.emu.reg_read(UC_ARM64_REG_PC)==0x20000000
 return p.emu.reg_read(UC_ARM64_REG_X0)
for slot,values in ((0x5595e08,(0.,0.,0.,1.,1.,1.)),(0x5591e28,(0.,0.,0.,1.))):
 storage=0x10050000+(slot&0xfff);p.put(resolve(slot)+0xb8,storage,8)
 for i,v in enumerate(values):p.put(storage+i*4,bits(v))
p.put(resolve(0x55940a8)+0xb8,statics,8);p.put(statics+8,0)
p.put(array+0x18,3)
for i,r in enumerate(renderers):p.put(array+0x20+i*8,r,8)
cases=[];expected=[]
for missing in range(-1,4):
 for i,ps in enumerate(systems):p.put(view+0x30+i*8,0 if missing==i else ps,8)
 for op in ('A','I','G','P','S'):
  directions=(-2147483648,*range(-3,12),2147483647) if op in ('G','P') else (1,)
  for direction in directions:
   if op=='S':direction=17
   trace=[];fault=False;scheduled=[]
   call(0x21a9340,view)
   assert trace==['base','get-renderers'] and p.read(view+0x50,8)==array
   if op!='A':trace=[]
   if op=='I':call(0x21a93a4,view,0,parent)
   elif op=='G':
    result=call(0x21a9588,view,direction);trace.append('null' if result==0 else str(systems.index(result)))
   elif op=='P':
    call(0x21a94e0,view,direction)
    if not fault:trace+=['before']+['recycle']*len(scheduled)
   elif op=='S':call(0x21a95d8,view,(17&0xffffffff)|((9006&0xffffffff)<<32),1)
   cases.append(','.join(map(str,(op,missing,direction,9006,1))))
   expected.append('|'.join(trace)+('' if op=='A' else '#E' if fault else '#OK'))
for missing in (0,1):
 trace=[];fault=False;p.put(flash+0x88,0 if missing else systems[0],8);call(0x3448374,flash)
 cases.append('F,'+str(missing));expected.append('|'.join(trace)+('#E' if fault else '#OK'))
# Engine Invoke is supplied at the one-second boundary; runtime cancellation,
# shared-pool lifecycle and player-loop deadlines are checked in Unity.
assert call(0x21a9338,view)==3 and call(0x3448418,flash)==1
with tempfile.TemporaryDirectory() as folder:
 exe=Path(folder)/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
 result=subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(p.PROJECT/'SourceCatalog/SwapParticlesHarness.cs'),*[str(runtime/f) for f in ('OriginalMatchItemSwapParticles.cs','OriginalSwapParticles.cs','OriginalParticleRecycleScheduler.cs','OriginalMatchItemSorting.cs')]],capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 result=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 actual=result.stdout.lstrip('\ufeff').splitlines();assert actual==expected,next(((i,cases[i],a,e)for i,(a,e)in enumerate(zip(actual,expected))if a!=e),(len(actual),len(expected)))
# Preserve every recovered ParticleSystem module rather than reconstruct effects.
asset_root=p.RECOVERY/'assets/RecoveredProject/ExportedProject/Assets/GameObject'
for name in ('MatchItemSwapParticles','SwapParticles'):
 original=(asset_root/(name+'.prefab')).read_text();current=(p.PROJECT/'Assets/Resources/Recovered/GameObject'/(name+'.prefab')).read_text()
 blocks=lambda s:re.findall(r'--- !u!198 &[^\n]+\n.*?(?=--- !u!|\Z)',s,re.S)
 assert blocks(original)==blocks(current),name+' particle modules differ'
assert 'matchSwapPrewarmCount: 10' in (p.PROJECT/'Assets/Foundation/Prefabs/Foundation.prefab').read_text()
report={'passed':True,'native_cases':len(cases),'scope':'Actual ARM64 direction selection including invalid/missing references, cached renderer Awake, parent/local pose reset, per-renderer layer-before-order, Play followed by Invoke RecycleSelf at one scaled second, cell flash optimized-base Awake and Item/-1 sorting, and pool IDs versus production C#. Engine storage and Invoke are supplied boundaries. Actual recovered ParticleSystem YAML modules are unchanged. Unity separately checks real particle playback, tweens, timed recycle, manager cancellation and pool reuse. Full input/resolution/audio playback and original rendered parity remain pending.'}
(p.PROJECT/'swap_particles_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
