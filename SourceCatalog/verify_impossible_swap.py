"""Compare impossible-swap choreography and sine evaluation with actual ARM64."""
import sys
sys.dont_write_bytecode=True
import verify_board_layout as b
import json,math,struct,subprocess,tempfile,itertools
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import *
p=b.p
action,cell,item,view,cellview,grid,dest,static,iklass,vklass,mediator=[0x10010000+i*0x1000 for i in range(11)]
VIEW,SORT,BEGIN,END=[0x20000100+i*4 for i in range(4)]
trace=[];queue=[];actions={};tweens={};origin=scale=();static_exists=has_current=False;swapping=False
def bits(x):return struct.unpack('<I',struct.pack('<f',x))[0]
def frombits(x):return struct.unpack('<f',struct.pack('<I',x))[0]
def doublebits(x):return struct.unpack('<Q',struct.pack('<d',x))[0]
def resolve(slot):
 if slot not in b.resolved_slots:p.emu.reg_write(UC_ARM64_REG_X0,slot);b.boundary(p.emu,0xffd960,4,None)
 return p.read(slot,8)
callback_slots={0x56f7ee8-0x100000:0x1ee4b00,0x56f7ef0-0x100000:0x1ee4df4,0x56f7ef8-0x100000:0x1ee4c64,0x56f7f00-0x100000:0x1ee4d8c,0x56f7f08-0x100000:0x1ee4f1c}
method_callbacks={resolve(slot):rva for slot,rva in callback_slots.items()}
def boundary(uc,address,size,data):
 global swapping
 x0,x1,x2,x3=[uc.reg_read(r)for r in (UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_X3)];value=None
 ss=[uc.reg_read(r) for r in (UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2,UC_ARM64_REG_S3)]
 if address==0x21805c0:trace.append('sfx:'+str(x1)+':'+str(ss[0]));value=0
 elif address==0x359e6bc:trace.append('haptic:'+str(x1)+':'+str(x2)+':'+str(ss[0])+':'+str(ss[1])+':'+str(x3));value=0
 elif address==0x343bd5c:trace.append('item');value=item
 elif address==VIEW:trace.append('view');value=view
 elif address==0x3e4737c:value=x0
 elif address in (0x3e4e400,0x3e4e958):
  trace.append('origin' if address==0x3e4e400 else 'scale-get')
  for r,v in zip((UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2),origin if address==0x3e4e400 else scale):uc.reg_write(r,bits(v))
  value=0
 elif address==0x12d83fc:trace.append('sort-get');uc.reg_write(UC_ARM64_REG_X1,1);value=17|(9005<<32)
 elif address==SORT:trace.append('sort:'+str(x1&0xffffffff)+':'+str(x1>>32)+':'+str(x2&1));value=0
 elif address==BEGIN:swapping=True;trace.append('begin:'+str(x1&1)+':'+str(x2)+':'+str(p.read(action+0x10,1)));value=0
 elif address==END:trace.append('end:'+str(p.read(action+0x10,1)));swapping=False;value=0
 elif address in (0x2c97a6c,0x2c97d1c):
  handle=p.allocate(0x100);tweens[handle]={'kind':'move' if address==0x2c97a6c else 'scale','end':ss[:3],'duration':ss[3],'ease':0,'callback':None};value=handle
 elif address==0x2687f30:tweens[x0]['ease']=x1;value=x0
 elif address==0x2044a78:actions[x0]=(x1,method_callbacks[x2]);value=0
 elif address==0x2687f9c:tweens[x0]['callback']=actions[x1];value=x0
 elif address==0x2861a3c:
  t=tweens[x0];trace.append(t['kind']+':'+':'.join(map(str,t['end']))+':'+str(t['duration'])+':'+str(t['ease']));queue.append(t);value=0
 elif address==0x3207954:trace.append('grid:'+str(p.signed(x1&0xffffffff))+':'+str(p.signed(x1>>32)));value=dest if static_exists else 0
 elif address==0x344108c:trace.append('static');value=0
 elif address==0x2b45a6c:trace.append('has:'+str(int(has_current)));value=int(has_current)
 elif address==0x2b470f4:trace.append('arrange');value=0
 elif address==0x3e4e9f8:value=0 # Actual scale procedure's engine setter boundary.
 elif address in (0xd009c0,0xd00ce0):
  d=struct.unpack('<d',struct.pack('<Q',uc.reg_read(UC_ARM64_REG_D0)))[0]
  uc.reg_write(UC_ARM64_REG_D0,doublebits((math.sin if address==0xd009c0 else math.cos)(d)));value=0
 elif address==0xffda88:raise AssertionError('Unexpected native null boundary')
 if value is not None:uc.reg_write(UC_ARM64_REG_X0,value);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,boundary)
def call(rva,*args):
 for i,v in enumerate((*args,0,0,0)):p.emu.reg_write(UC_ARM64_REG_X0+i,v&0xffffffffffffffff)
 p.emu.reg_write(UC_ARM64_REG_SP,0x101ff000);p.emu.reg_write(UC_ARM64_REG_LR,0x20000000)
 p.emu.emu_start(rva,0x20000000,count=200000);assert p.emu.reg_read(UC_ARM64_REG_PC)==0x20000000
p.put(action+0x40,0x10000700,8);p.put(action+0x48,0x10000708,8);p.put(action+0x58,grid,8)
p.put(cell+0x10,mediator,8);p.put(cell+0xb8,cellview,8);p.put(item,iklass,8);p.put(item+0x38,mediator,8);p.put(iklass+0x418,VIEW,8)
p.put(view,vklass,8);p.put(vklass+0x208,SORT,8);p.put(vklass+0x228,BEGIN,8);p.put(vklass+0x238,END,8);p.put(dest+0xc0,static,8)
storage=0x10060000;p.put(resolve(0x5595e08)+0xb8,storage,8)
for i in range(3):p.put(storage+i*4,0)
cases=[];expected=[]
positions=[(0.,7.,0.),(-3.25,9.5,-.2),(.01,-.01,-0.)]
scales=[(1.,1.,1.),(.5,1.5,2.)]
for (dx,dy),origin,scale,has_current,static_exists in itertools.product(itertools.product((-1,0,1),repeat=2),positions,scales,(False,True),(False,True)):
 p.heap=0x10070000;actions={};tweens={};queue=[];trace=[];swapping=False
 x,y=(0,7);tx,ty=x+dx,y+dy;p.put(cell+0x18,(x&0xffffffff)|((y&0xffffffff)<<32),8);p.put(action+0x10,0,1);p.put(action+0x68,0,8);p.put(action+0x70,0,8)
 call(0x1ee2558,action,cell,(tx&0xffffffff)|((ty&0xffffffff)<<32))
 i=0
 while i<len(queue):
  cb=queue[i]['callback'];i+=1
  if cb:call(cb[1],cb[0])
 assert i==6
 cases.append(','.join(map(str,('I',x,y,tx,ty,*map(bits,origin),*map(bits,scale),int(has_current),int(static_exists)))))
 expected.append('|'.join(trace)+'#'+','.join(map(str,(p.read(action+0x10,1),int(p.read(action+0x68,8)==cell),int(p.read(action+0x70,8)==0),int(swapping),0))))
impossible_count=len(cases)
# Exercise the native evaluator actually selected by each tween curve. libm
# sin/cos are supplied double boundaries; native float rounding/order is kept.
for ease,duration,n in itertools.product((0,1,3,4,6),(.075,.135,1.),range(33)):
 dt=frombits(bits(frombits(bits(duration))*n/32));duration=frombits(bits(duration))
 p.emu.reg_write(UC_ARM64_REG_S0,bits(dt));p.emu.reg_write(UC_ARM64_REG_S1,bits(duration));p.emu.reg_write(UC_ARM64_REG_S2,0);p.emu.reg_write(UC_ARM64_REG_S3,0)
 call(0x33d5f78,ease,0);ratio=p.emu.reg_read(UC_ARM64_REG_S0)
 # Compare the applied Vector3, including addition's signed-zero behavior.
 p.put(0x10061000+0x10,0x10061100,8);p.put(0x10061100+0x10,view,8)
 for r,v in zip((UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2,UC_ARM64_REG_S3,UC_ARM64_REG_S4,UC_ARM64_REG_S5,UC_ARM64_REG_S6),(0,0,0,bits(1.),0,0,ratio)):p.emu.reg_write(r,v)
 call(0x2c97ebc,0x10061000);cases.append(','.join(map(str,('E',ease,bits(dt),bits(duration)))));expected.append(str(p.emu.reg_read(UC_ARM64_REG_S0)))
with tempfile.TemporaryDirectory() as folder:
 exe=Path(folder)/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
 result=subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(p.PROJECT/'SourceCatalog/ImpossibleSwapHarness.cs'),*[str(runtime/f)for f in ('OriginalImpossibleSwapAnimation.cs','OriginalSwapActionState.cs','OriginalLocalMoveTween.cs')]],capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 result=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 actual=result.stdout.lstrip('\ufeff').splitlines();assert actual==expected,next(((i,cases[i],a,e)for i,(a,e)in enumerate(zip(actual,expected))if a!=e),(len(actual),len(expected)))
report={'passed':True,'native_impossible_swap_cases':impossible_count,'native_ease_cases':len(cases)-impossible_count,'scope':'Actual ARM64 StartImpossibleSwapAnimation and all five generated callbacks versus production C#: source/busy assignment without normal swap events, direction normalization, three .075-second move and scale legs, horizontal component exchange for nonuniform scales, sorting, missing current-cell tail, SFX/haptic and static-target dispatch. Actual native ease evaluator covers OutSine/InOutSine and existing Linear/OutQuad paths. Transform/tween scheduling, allocation, libm sin/cos, static presentation and audio/haptic are supplied boundaries. Unity player-loop motion and cached scale targets are separately checked; full input context, item touch/tap, special/static adapters, valid resolution/fall/refill/goals/settlement and rendered parity remain pending.'}
(p.PROJECT/'impossible_swap_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
