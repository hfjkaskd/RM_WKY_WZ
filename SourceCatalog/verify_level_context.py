"""Compare actual native LevelContext activation, selection and clear control flow."""
import sys
sys.dont_write_bytecode=True
import json,os,random,subprocess,tempfile
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_move_budget.py').read_text().split('cases=[];expected=[]')[0],str(PROJECT/'SourceCatalog/verify_move_budget.py'),'exec'))
emu.hook_del(native_hook);base_hook=hook
owner,container,slots=0x10020000,0x10021000,0x10022000
context_interface,game_interface=0x10026000,0x10026400
level_class,level_static,player_class,player_static=0x10028000,0x10028400,0x10029000,0x10029400
clear_call,bind_call,method_slot=0x1002a000,0x1002a100,0x1002b000
for got,klass in [(0x5380af8,context_interface),(0x539dde0,game_interface),(0x53f0418,level_class),(0x536b340,player_class)]:
 put(read(got,8),klass,8);put(klass+0xe0,1);put(klass+0x132,1,1)
put(level_class+0xb8,level_static,8);put(player_class+0xb8,player_static,8)
put(method_slot,clear_call,8);put(method_slot+16,bind_call,8)
kind_classes={-1:0x10034000,-3:0x10035000,4:0x10036000,**{i:0x10030000+i*0x1000 for i in range(4)}}
for kind,klass in kind_classes.items():
 put(klass+0x12a,0,2);put(klass+0x12c,1 if kind<=0 else 2,1);put(klass+0xc8,klass+0x400,8)
 put(klass+0x400,kind_classes[0]if kind in range(4)else klass,8);put(klass+0x408,klass,8)
for got,kind in [(0x53b4f40,0),(0x53a4cd8,1),(0x53b7248,2),(0x53594d8,3)]:put(read(got,8),kind_classes[kind],8)
objects={};behaviours=[];clocks={};trace=[];clock_count=0;heap=0;flags=0;failure=0;calls=0;mutate=False
def record(value):
 global calls
 trace.append(value);calls+=1
 if calls==failure:fail(emu);return False
 return True
def alloc():
 global heap
 p=heap;heap+=0x100;emu.mem_write(p,bytes(0x100));return p
def make(key,kind=-1,pointer=None):
 p=alloc()if pointer is None else pointer;objects[p]=(key,kind);put(p,kind_classes[kind],8);return p
def slot(i):return read(slots+0x20+i*8,8)
def setslot(i,p):put(slots+0x20+i*8,p,8)
def remove_late(uc):
 behaviours[:]=[p for p in behaviours if objects[p][1]!=4]
 for i in range(112):
  p=slot(i)
  if p and objects[p][1]==4:
   if not record('remove,'+str(objects[p][0])):return
   setslot(i,0)
 returned(uc)
def hook(uc,address,size,unused):
 global clock_count
 x0,x1,x2=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(3)]
 if address==0xfcd934:returned(uc,method_slot if x1==game_interface else method_slot+16)
 elif address==clear_call:
  key=objects[x0][0]
  if record('clear,'+str(key)+','+str(x1&1)):
   if key==2 and mutate:setslot(16,make(99))
   returned(uc)
 elif address==0x260dea4:returned(uc,slot(x0))
 elif address==0x3a13898:returned(uc,slot(x1))
 elif address==0x1ed9160:
  if record('seed'):returned(uc)
 elif address==0xffda78:returned(uc,alloc())
 elif address==0x318d268:
  if record('clock'):clock_count+=1;clocks[x0]=clock_count;returned(uc)
 elif address in(0x1e18f34,0x1e18cf4):
  table=address==0x1e18f34
  if record('table'if table else'story'):returned(uc,bool(flags&(1 if table else 2)))
 elif address==0x32071a0:record('water') # Actual inline static water-flag read.
 elif address in(0x1edd1b8,0x1edac5c,0x1edc024,0x1edc014):
  kind={0x1edd1b8:0,0x1edac5c:1,0x1edc024:2,0x1edc014:3}[address]
  if record('win,'+str(kind)):make(16,kind,x0);returned(uc)
 elif address==0x3f40eac:
  key=objects[x1][0];old=slot(key)
  if old and objects[old][1]==objects[x1][1]:returned(uc);return
  setslot(key,x1)
  for i,p in enumerate(behaviours):
   if objects[p][0]==key:behaviours[i]=x1;break
  else:behaviours.append(x1)
  returned(uc)
 elif address==0x3f418b4:
  for i in range(112):
   p=slot(i)
   if p and not record('bind,'+str(objects[p][0])):return
  returned(uc)
 elif address==0x3f41560:remove_late(uc)
 elif address==0x3f417b0:
  for p in behaviours:
   if not record('update,'+str(objects[p][0])):return
  returned(uc)
 elif address==0x2a2da14:returned(uc) # Debug diagnostic transport only.
 else:base_hook(uc,address,size,unused)
for a in [0xfcd934,clear_call,0x260dea4,0x3a13898,0x1ed9160,0xffda78,0x318d268,0x1e18f34,0x1e18cf4,0x32071a0,0x1edd1b8,0x1edac5c,0x1edc024,0x1edc014,0x3f40eac,0x3f418b4,0x3f41560,0x3f417b0,0x2a2da14,0xffd960,0xffd90c,0xffda64,0xffda88,0xffda90]:emu.hook_add(UC_HOOK_CODE,hook,begin=a,end=a)
cases=[];expected=[];commands=0
def slot_text(i):
 p=slot(i)
 return '-'if not p else ','.join(map(str,objects[p]))
def run(rows):
 global clock_count,heap,flags,failure,calls,mutate,commands
 heap=0x10050000;clock_count=0;objects.clear();behaviours.clear();clocks.clear()
 emu.mem_write(owner,bytes(0x4000));put(owner+0x10,container,8);put(container+0x18,slots,8);put(slots+0x18,112)
 put(level_static,owner,8);put(level_static+8,0,1)
 for key,kind in[(0,-3),(2,-1),(75,4)]:
  p=make(key,kind);setslot(key,p);behaviours.append(p)
 output=[]
 for mode,a,flags,failure,m in rows:
  commands+=1;calls=0;trace.clear();mutate=bool(m);put(player_static+0x14,bool(flags&4),1);result='-'
  if mode==2:setslot(16,0 if a==-2 else make(16,a))
  else:
   call({0:0x3206e7c,1:0x3206be4,3:0x3206e2c,4:0x3206ddc}[mode],owner,a)
   if fault:result='error'
  output.append(result+':'+str(read(level_static+8,1))+':'+str(clocks.get(read(owner+0x18,8),0))+':'+slot_text(16)+':'+slot_text(75)+':'+'/'.join(trace))
 cases.append(';'.join(','.join(map(str,r))for r in rows));expected.append('|'.join(output))
for flags_value in range(8):
 for kind in range(-2,4):
  for failure_value in range(14):
   run([[2,kind,0,0,0],[0,1,flags_value,failure_value,0],[3,0,flags_value,0,0],[0,0,flags_value,0,0],[1,0,flags_value,0,0],[0,1,flags_value,0,0],[1,1,flags_value,0,0],[3,0,0,0,0]])
for failure_value in range(12):
 for m in(0,1):
  run([[2,1,0,0,0],[0,1,2,0,0],[1,1,0,failure_value,m],[3,0,0,0,0],[4,0,0,0,0],[0,1,0,0,0]])
rng=random.Random(0x3206e7c)
for i in range(150):
 rows=[]
 for j in range(24):
  mode=rng.randrange(5);a=rng.randrange(-2,4)if mode==2 else rng.randrange(2)
  rows.append([mode,a,rng.randrange(8),rng.randrange(14),rng.randrange(2)])
 run(rows)
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-level-context-')as folder:
 exe=Path(folder)/'verify.exe'
 sources=['SourceCatalog/LevelContextHarness.cs',*[f'Assets/Foundation/Runtime/{n}.cs'for n in('OriginalLevelContext','OriginalContextContainer','OriginalExplodeData')]]
 result=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-langversion:latest','-out:'+str(exe),*[str(PROJECT/s)for s in sources]],capture_output=True,text=True,encoding='utf-8-sig');assert result.returncode==0,result.stdout+result.stderr
 result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8-sig');assert result.returncode==0,result.stdout+result.stderr
 actual=result.stdout.splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
 for i,(a,e)in enumerate(zip(actual,expected)):assert a==e,(i,cases[i],a,e)
report={'passed':True,'native_sequences':len(cases),'native_commands':commands,'scope':'Actual ARM64 LevelContext Activate/ArrangeWinFailManager/Clear/RemoveLateUnits/ManualUpdate control flow and Exploder.Reset. Covers new/resumed activation, table/story/water/normal manager inheritance gates, reset-seed then new-clock then replacement then binding then activation, live borrowed unit clear order, game-exit-only late removal, repeated activation/clear and partial state after injected service/query/clock/bind/clear/remove failures. Container calls, unit constructors/bodies, CLR type metadata/allocation and error transport are supplied boundaries. Container operations separately native-verified; constructor order/table count separately verified by actual native ctor inventory and asserted in C# factory harness. Diagnostic logs omitted. Clock registration/time getters, cross-instance ownership, actual permanent/win-fail manager implementations and complete application/player startup are not proven by this test.'}
(PROJECT/'level_context_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
