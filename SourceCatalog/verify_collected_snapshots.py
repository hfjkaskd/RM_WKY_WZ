"""Compare concrete collection flags, packed snapshots and mission counters to ARM64."""
import sys
sys.dont_write_bytecode=True
import json,os,random,subprocess,tempfile
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_move_budget.py').read_text().split('cases=[];expected=[]')[0],str(PROJECT/'SourceCatalog/verify_move_budget.py'),'exec'))
emu.hook_del(native_hook);base_hook=hook
obj,manager,entry,klass=[0x10020000+i*0x400 for i in range(4)]
# ctor, setter, save, load, length, signed, user manager id, manager flag offset.
variants=[
 (0x32bb7d8,0x32bb438,0x32bb658,0x32bb6fc,0x32bb790,0x32bb7bc,65,0x48),
 (0x32b3e58,0x32b3c88,0x32b3c94,0x32b3d38,0x32b3e2c,0x338c82c,38,0x1c),
 (0x338ae20,0x338e338,0x338e344,0x338e3e8,0x338e4dc,0x338c82c,46,0x19),
 (0x32b4558,0x32b4388,0x32b4394,0x32b4438,0x32b452c,0x338c82c,59,0x19),
 (0x32b5184,0x32b4fb4,0x32b4fc0,0x32b5064,0x32b5158,0x338c82c,64,0x18),
 (0x32b4030,0x32b3e60,0x32b3e6c,0x32b3f10,0x32b4004,0x338c82c,73,0x18),
 (0x32b62ec,0x32b611c,0x32b6128,0x32b61cc,0x32b62c0,0x338c82c,80,0x18)]
h=[];kind=0
def record(uc,label):
 trace.append(label)
 if len(trace)==h[2]:fail(uc);return False
 return True
def collected_hook(uc,address,size,unused):
 x0,x1,x2=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(3)]
 if address==0x29e27cc:
  assert x0==(variants[kind][6]if kind<7 else 29),x0
  if record(uc,'get,'+str(x0)):returned(uc,0 if h[1]else manager)
 elif address==0x222dba0:
  if record(uc,'playing'):returned(uc,h[3])
 elif address==0xfcd934:
  assert x0==obj and x2 in(2,3)
  put(entry,variants[kind][4 if x2==2 else 5]if kind<7 else 0x32b4370 if x2==2 else 0x32b4380,8);returned(uc,entry)
 elif address==0x32b120c:uc.reg_write(UC_ARM64_REG_PC,0x32b1294) # Only diagnostic Log.Debug construction/tail omitted.
 elif address==0x231007c:returned(uc)
 else:base_hook(uc,address,size,unused)
emu.hook_add(UC_HOOK_CODE,collected_hook)
cases=[];expected=[];commands=0
def run(head,steps):
 global h,kind,next_object,commands,fault
 h=head[:];kind=h[0];next_object=0x10030000
 emu.mem_write(obj,bytes(0x100));emu.mem_write(manager,bytes(0x100));put(obj,klass,8)
 call(variants[kind][0]if kind<7 else 0x32b4038 if kind==7 else 0x32b12a8,obj)
 saved=0;out=[]
 for command in steps:
  commands+=1;trace.clear();fault=False;v=command.split(',');mode=v[0];n=int(v[1])if len(v)>1 else 0;result='-'
  if kind<7:
   ctor,setter,save,load,*_=variants[kind]
   if mode=='d':put(obj+0x20,n,1);put(obj+0x24,int(v[2]));put(obj+0x28,int(v[3]))
   elif mode=='l':call(load,obj,n)
   elif mode=='g':
    call(save,obj);x=emu.reg_read(UC_ARM64_REG_X0);saved=x-(1<<64)if x>>63 else x;result=str(saved)
   else:call({'s':setter,'i':0x338c6a8,'f':0x338c6b4,'a':0x338b130,'c':0x338c6dc}[mode],obj,n)
   fields=[read(obj+0x20,1),signed(read(obj+0x24)),signed(read(obj+0x28)),read(obj+0x2c,1),read(manager+variants[kind][7],1)]
  elif kind==7:
   if mode=='s':put(obj+0x20,n,1)
   elif mode=='g':
    call(0x32b4244,obj);x=emu.reg_read(UC_ARM64_REG_X0);saved=x-(1<<64)if x>>63 else x;result=str(saved)
   else:call({'i':0x32b4084,'a':0x32b4094,'t':0x32b41a4,'o':0x32b41fc,'m':0x32b421c,'c':0x32b423c,'l':0x32b42f8}[mode],obj,n,int(v[2])if len(v)>2 else 0)
   fields=[read(obj+0x20,1)]+[signed(read(obj+o))for o in range(0x24,0x38,4)]
  else:
   call({'i':0x32b1180,'s':0x32b118c,'c':0x32b119c}[mode],obj,n);fields=[read(obj+0x10,1),signed(read(obj+0x14))]
  if fault:result='error'
  out.append(result+':'+','.join(map(str,fields))+':'+str(saved)+':'+ ';'.join(trace))
 cases.append(','.join(map(str,head))+'#'+';'.join(steps));expected.append('|'.join(out))
edges=[-2147483648,-8388609,-8388608,-1048577,-1,0,1,1023,1024,1048575,1048576,8388607,8388608,2147483647]
for kind in range(7):
 for n in edges:
  for missing,throw_at in((0,0),(1,0),(0,1)):
   run([kind,missing,throw_at,0],[f'd,1,{n},{n}','s,1','g','i,7','g','f','g','l,-1','g','s,0',f'i,{n}','a,-1','g','l,0','g'])
for missing,throw_at,playing in((0,0,0),(0,0,1),(1,0,1),(0,1,1),(0,2,1)):
 for n in edges:
  steps=[f'i,{n}','s,1','g','l,-1','g','i,0']
  steps += [f'a,{item},{amount}'for item in(-1,0,1,2,3,4,5,6,2147483647)for amount in(0,1,-1)]
  steps += [f't,{t},{n}'for t in(-1,0,10,20,30,40,50,60)]
  steps += [f'o,{o},{n}'for o in(-1,0,1,2,3,4)]
  steps += [f'm,{n}','g','c','g','l,0','g']
  run([7,missing,throw_at,playing],steps)
for n in edges:run([8,0,0,0],[f's,{n}','c',f's,{n}','i'])
rng=random.Random(0x338a648)
for i in range(90):
 k=i%8;steps=[f'l,{rng.randrange(-2**63,2**63)}','g',f'i,{rng.randrange(-2**31,2**31)}','g','s,1']
 steps += [f'a,{rng.randrange(-2**31,2**31)}'if k<7 else f'a,5,{rng.randrange(-2**31,2**31)}','g','c','g']
 run([k,0,0,i%2],steps)
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-collected-snapshots-')as directory:
 exe=Path(directory)/'verify.exe';files=('OriginalSessionValues','OriginalLevelSessionCounters','OriginalActiveLevelInitialization','OriginalActiveLevelData','OriginalMoveBudgetAdjustment','OriginalSlbMoveConfig','OriginalActiveLevelStart','OriginalCollectedLevelSnapshots')
 p=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-langversion:latest','-out:'+str(exe),str(PROJECT/'SourceCatalog/CollectedSnapshotsHarness.cs'),*[str(PROJECT/'Assets/Foundation/Runtime'/(n+'.cs'))for n in files]],capture_output=True,text=True);assert p.returncode==0,p.stdout+p.stderr
 p=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert p.returncode==0,p.stderr[-2000:]
 actual=p.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
 for i,(a,e)in enumerate(zip(actual,expected)):
  if a!=e:
   aa=a.split('|');ee=e.split('|');j=next(j for j,(x,y)in enumerate(zip(aa,ee))if x!=y)
   raise AssertionError((i,j,cases[i],aa[j],ee[j]))
report={'passed':True,'native_sequences':len(cases),'native_commands':commands,'scope':'Actual ARM64 constructors, inherited Init/FullReset and collect setters/save/load of WeeklyPass, MagicCauldron, DukesFortune, OceanOdyssey, PuzzleBreak, MergeSmith and ShortExpedition; native manager collect-flag writes after snapshot restore. Weekly signed 1/24/24/1 and six unsigned 1/20/20/1 layouts; extra flags retained across Init and FullReset. Also concrete Mission constructor/item/tiled/slot counters, super-booster query partial writes, overflow/multiplication, unsigned five 10-bit save/load retaining shouldConsume; InviteFriends Init/SetEgoPrice/Consume. CLR allocation/interface resolution and actual feature-manager resolution/booster state are supplied boundaries. Invite diagnostic Log.Debug construction/tail only omitted. AnimationData and application composition are outside this native snapshot test; formal playable loop remains pending.'}
(PROJECT/'collected_snapshots_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
