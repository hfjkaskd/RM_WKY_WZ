"""Native clock construction, scaling and GameContext late-update integration."""
import sys
sys.dont_write_bytecode=True
import json,os,random,struct,subprocess,tempfile
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_move_budget.py').read_text().split('cases=[];expected=[]')[0],str(PROJECT/'SourceCatalog/verify_move_budget.py'),'exec'))
from unicorn.arm64_const import UC_ARM64_REG_S0
emu.hook_del(native_hook);base_hook=hook
clock_class,game_class,game_static,delegate,invoke,trampoline=0x10020000,0x10021000,0x10022000,0x10023000,0x10024000,0x10024100
put(clock_class+0x1c8,0x318d1c4,8)
put(read(0x5351eb0,8),game_class,8);put(game_class+0xe0,1);put(game_class+0x132,1,1);put(game_class+0xb8,game_static,8)
put(delegate+0x18,invoke,8)
trace=[];callbacks=[];actions={};pending=[];resume=0;failure=0;calls=0;delta_bits=0;scale_bits=0;heap=0;constructing=0
bits=lambda f:struct.unpack('<I',struct.pack('<f',f))[0]
def record(value):
 global calls
 trace.append(value);calls+=1
 if calls==failure:fail(emu);return False
 return True
def alloc():
 global heap
 p=heap;heap+=0x100;emu.mem_write(p,bytes(0x100));return p
def dispatch_next(uc):
 if not pending:uc.reg_write(UC_ARM64_REG_PC,resume);return
 p=pending.pop(0);uc.reg_write(UC_ARM64_REG_X0,p);uc.reg_write(UC_ARM64_REG_LR,trampoline);uc.reg_write(UC_ARM64_REG_PC,0x318d334)
def hook(uc,address,size,unused):
 global scale_bits,pending,resume
 x0,x1,x2=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(3)]
 if address==0xffda78:returned(uc,alloc())
 elif address==0x231007c:returned(uc)
 elif address==0x2044a78:actions[x0]=x1;returned(uc)
 elif address==0x1e37e8c:
  if record('register'):
   if x0:callbacks.append(actions[x0])
   put(game_static+0x18,delegate if callbacks else 0,8);returned(uc)
 elif address==0x3e425b4:
  if record('getscale'):uc.reg_write(UC_ARM64_REG_S0,scale_bits);returned(uc)
 elif address==0x3e424ec:
  if record('delta'):uc.reg_write(UC_ARM64_REG_S0,delta_bits);returned(uc)
 elif address==0x3e425dc:
  if record('setscale'):scale_bits=uc.reg_read(UC_ARM64_REG_S0);returned(uc)
 elif address==invoke:
  pending=list(callbacks);resume=uc.reg_read(UC_ARM64_REG_LR);dispatch_next(uc)
 elif address==trampoline:dispatch_next(uc)
 else:base_hook(uc,address,size,unused)
for a in [0xffda78,0x231007c,0x2044a78,0x1e37e8c,0x3e425b4,0x3e424ec,0x3e425dc,invoke,trampoline,0xffd960,0xffd90c,0xffda64,0xffda88,0xffda90]:emu.hook_add(UC_HOOK_CODE,hook,begin=a,end=a)
cases=[];expected=[];commands=0
def run(rows):
 global heap,scale_bits,delta_bits,calls,failure,commands,constructing
 heap=0x10030000;scale_bits=bits(1);callbacks.clear();actions.clear();put(game_static+0x18,0,8);current=0;output=[]
 for mode,value,failure,delta_bits in rows:
  commands+=1;trace.clear();calls=0;result='-';failed=False
  if mode==0:
   constructing=alloc();put(constructing,clock_class,8);call(0x318d268,constructing);failed=fault
   if not failed:current=constructing
  elif mode in(1,3):
   if not current:failed=True
   else:
    emu.reg_write(UC_ARM64_REG_S0,value);call(0x318d1c4 if mode==1 else 0x318d328,current);failed=fault
    if mode==1 and not failed:result=str(emu.reg_read(UC_ARM64_REG_S0))
  elif mode==2:call(0x1e39d68);failed=fault
  elif mode==4:scale_bits=value
  elif mode==5:callbacks.clear();put(game_static+0x18,0,8)
  elif mode==6:pass # Null Delegate.Combine preserves the current delegate.
  if failed:result='error'
  state='null'if not current else ','.join(str(read(current+o))for o in(0x10,0x14,0x18))
  output.append(result+':'+state+':'+str(scale_bits)+':'+'/'.join(trace))
 cases.append(';'.join(','.join(map(str,r))for r in rows));expected.append('|'.join(output))
values=[0,1,0x80000000,0x7f800000,0xff800000,0x7fc00000,0x7f7fffff,0xff7fffff,*[bits(f)for f in(1/60,1/30,.00009,-.00009,.123456,-.123456,214748.36,-214748.36)]]
for v in values:
 for failure_value in range(5):
  run([[0,0,failure_value,v],[1,0,0,v],[2,0,0,v],[0,0,0,v],[2,0,failure_value,v],[3,v,failure_value,v],[4,bits(.5),0,v],[1,0,0,v],[2,0,0,v],[5,0,0,v],[2,0,0,v]])
rng=random.Random(0x318d268)
for i in range(220):
 rows=[]
 for j in range(32):
  mode=rng.choice([0,1,1,2,2,2,3,4,5,6]);v=rng.choice(values)if i<40 else bits(rng.uniform(0,.05));scale=rng.choice(values)
  rows.append([mode,scale,rng.randrange(6),v])
 run(rows)
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-delta-manager-')as folder:
 exe=Path(folder)/'verify.exe';sources=['SourceCatalog/DeltaTimeManagerHarness.cs',*[f'Assets/Foundation/Runtime/{n}.cs'for n in('OriginalDeltaTimeManager','OriginalGameLateUpdateDispatcher','OriginalLevelClock','OriginalLevelContext','OriginalContextContainer','OriginalExplodeData')]]
 result=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-langversion:latest','-out:'+str(exe),*[str(PROJECT/s)for s in sources]],capture_output=True,text=True,encoding='utf-8-sig');assert result.returncode==0,result.stdout+result.stderr
 result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8-sig');assert result.returncode==0,result.stdout+result.stderr
 actual=result.stdout.splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
 for i,(a,e)in enumerate(zip(actual,expected)):assert a==e,(i,cases[i],a,e)
report={'passed':True,'native_sequences':len(cases),'native_commands':commands,'scope':'Actual ARM64 DeltaTimeManager ctor/current delta getter/late accumulation/SetTimeScale plus GameContext.RegisterToLateUpdate and ManualLateUpdate. Bit-exact current delta, accumulated time, cached versus externally changed Unity scale, registration-before-scale-read, repeated new clocks, callbacks retained after constructor scale-read failure, failed scale writes retaining cached value, ungated multicast clock dispatch and callback clearing. Unity Time, CLR delegate construction/combine/invocation/metadata/allocation and injected exception transport supplied boundaries. Replay clock and full GameContext activation/deactivation/scene lifecycle remain separate.'}
(PROJECT/'delta_time_manager_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
