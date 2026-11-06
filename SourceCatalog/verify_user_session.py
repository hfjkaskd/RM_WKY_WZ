"""Native user-session ownership/reset, animation reset and prelevel state."""
import sys
sys.dont_write_bytecode=True
import hashlib,json,os,random,sqlite3,subprocess,tempfile
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_move_budget.py').read_text().split('cases=[];expected=[]')[0],str(PROJECT/'SourceCatalog/verify_move_budget.py'),'exec'))
emu.hook_del(native_hook);base_hook=hook;metadata_next=0x101a0000
root,animation,active,pre,tags,foreign,delegate=[0x10020000+i*0x1000 for i in range(7)]
user_class,user_static,current_user,current_session,scene_manager=[0x10030000+i*0x1000 for i in range(5)]
put(read(0x53bb4e0,8),user_class,8);put(user_class+0xe0,1);put(user_class+0x132,1,1);put(user_class+0xb8,user_static,8)
offsets=[0x10,0x18,0x20,0x28,0x30,0x38,0x58,0x60,0x68,0x70,0x78,0x80,0x88,0x90,0xa0,0x40,0x98,0xa8,0xb0,0xb8,0xc0,0xc8,0xd0,0xd8,0xe0,0xe8,0xf0,0xf8,0x100,0x108,0x48,0x50]
children=[0x10040000+i*0x100 for i in range(32)];dictionary_pointers=[0x10044000+i*0x100 for i in range(4)]
trace=[];calls=0;failure=0;flags=15;constructor_allocation=0;dict_count=[0]*4;commands=0
def record(name):
 global calls
 trace.append(name);calls+=1
 if calls==failure:fail(emu);return False
 return True
def refresh():
 put(user_static,current_user if flags&1 else 0,8);put(current_user+0x38,current_session if flags&2 else 0,8)
 put(current_session+0x18,foreign if flags&4 else 0,8)
def init_animation():
 for o,p in zip(offsets,children):put(animation+o,p,8);emu.mem_write(p,bytes(0x80))
 for i,p in enumerate(dictionary_pointers):put(animation+0x110+8*i,p,8)
 put(animation+0x130,0,2)
def hook(uc,address,size,unused):
 global constructor_allocation
 x0,x1,x2=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(3)]
 if address==0xffda78:
  p=[animation,active,pre,tags,delegate][constructor_allocation];constructor_allocation+=1;returned(uc,p)
 elif address==0x338a648:
  if record('animation'):init_animation();returned(uc)
 elif address==0x32b6324:
  assert x1==animation
  if record('active,1'):returned(uc)
 elif address in(0x231007c,0x200297c):returned(uc)
 elif address==0x201331c:
  if record('subscribe'):returned(uc)
 elif address==0x32bb2b8:record('current')
 elif address==0x30f6400:
  assert x0==2
  if record('scene'):returned(uc,scene_manager if flags&8 else 0)
 elif address==0x3c96364:
  i=dictionary_pointers.index(x0)
  if record('clear,'+str(i)):dict_count[i]=0;returned(uc)
 else:base_hook(uc,address,size,unused)
native_hook=emu.hook_add(UC_HOOK_CODE,hook)
def fill():
 for i,(o,p)in enumerate(zip(offsets,children)):
  put(p+(0x10 if o==0x100 else 0x20),1,1);put(p+(0x14 if o==0x100 else 0x24),-10-i)
  if o!=0x100:put(p+0x28,3*i)
 dict_count[:]=[1]*4;put(animation+0x130,0x101,2)
def snapshot():
 consume=0;valuehash=0
 for i,(o,p)in enumerate(zip(offsets,children)):
  consume|=read(p+(0x10 if o==0x100 else 0x20),1)<<i
  left=signed(read(p+(0x14 if o==0x100 else 0x24)));right=0 if o==0x100 else signed(read(p+0x28))
  valuehash+=(i+1)*(left+31*right)
 bits=sum(read(pre+0x10+i,1)<<i for i in range(9))
 used=[int(any(read(pre+0x13+i+3*j,1)for j in range(3)))for i in range(3)]
 a=','.join(map(str,[signed(read(active+0x3c)),read(active+0x40,1),signed(read(root+0x30)),read(root+0x34,1),read(root+0x35,1),signed(read(root+0x38)),signed(read(active+0x14)),signed(read(tags+0x10))]))
 b=','.join(map(str,[bits,*used,*[signed(read(pre+o))for o in(0x1c,0x20,0x24)]]))
 c=','.join(map(str,[consume,signed(valuehash&0xffffffff),*dict_count,read(animation+0x130,1),read(animation+0x131,1)]))
 return a+':'+b+':'+c
cases=[];expected=[]
def run(steps):
 global flags,constructor_allocation,failure,calls,commands,fault
 for p,n in((root,0x100),(animation,0x140),(active,0x100),(pre,0x30),(tags,0xa0),(foreign,0x100),(scene_manager,0x80)):emu.mem_write(p,bytes(n))
 flags=15;constructor_allocation=0;failure=0;calls=0;trace.clear();dict_count[:]=[0]*4;refresh();put(scene_manager+0x28,2)
 call(0x32bb070,root);assert not fault;assert constructor_allocation==5
 assert [read(root+o,8)for o in(0x20,0x18,0x10,0x28)]==[animation,active,pre,tags]
 construction='/'.join(trace);out=[]
 for step in steps:
  f=step.split(',');mode=f[0];i=int(f[1])if len(f)>1 else 0;trace.clear();calls=0;commands+=1;result='-';fault=False
  if mode in('s','p','r','h'):call({'s':0x32bb280,'p':0x32bb320,'r':0x32bb22c,'h':0x338ae28}[mode],animation if mode=='h'else root,i)
  elif mode=='a':fill()
  elif mode in('m','u'):put(animation+offsets[i],0 if mode=='m'else children[i],8)
  elif mode=='n':put(active+0x14,i)
  elif mode=='j':put(foreign+0x14,i)
  elif mode=='c':put(scene_manager+0x28,i)
  elif mode=='f':flags=i;refresh()
  elif mode=='x':failure=i
  elif mode=='d':put(active+0x3c,i);put(active+0x40,1,1);put(root+0x30,0x418a0000);put(tags+0x10,99)
  elif mode in('t','b','v','z','e','l','o','q'):
   rva={'t':0x32b2670,'b':0x32bac3c,'v':0x32b165c,'z':0x32bacd4,'e':0x32bac70,'l':0x32bad28,'o':0x32babbc,'q':0x32babfc}[mode]
   v=call(rva,pre,i,int(f[2])if len(f)>2 else 0)
   if mode in('o','q'):result=str(v)
  elif mode=='k':
   for j in range(3):put(pre+0x10+j,(i>>j)&1,1)
  else:raise AssertionError(step)
  if fault:result='error'
  out.append(result+':'+snapshot()+':'+ '/'.join(trace))
 cases.append(';'.join(steps));expected.append(construction+'#'+'|'.join(out))
run(['a','d,2','n,1','j,1','s,2','j,2',*['s,2']*7,'c,2','p','c,3','p','r','s,3','s,-1','j,0','s,2','r'])
for f in(0,1,3,7,8,9,11,15):
 for scene in(-1,0,1,2,3,4):run(['a','d,7','j,2','s,2',f'f,{f}',f's,{scene}',f'c,{scene}','p','r'])
for slot in range(32):
 run(['a','d,5',f'm,{slot}','r',f'u,{slot}','r','a','h'])
for failure_case in range(1,5):run(['a','d,2',f'x,{failure_case}','r','x,0','r','a',f'x,{failure_case}','h','x,0','h'])
for failed in range(1,3):run(['d,2','j,2',f'x,{failed}','s,2','c,3','p','x,0','s,2','p'])
for mask in range(512):
 steps=[f'k,{mask&7}']
 for j in range(3):
  if mask&(1<<j):steps.append(f't,{j+1}')
  if mask&(1<<(j+3)):steps.append(f'b,{j+1},1')
  if mask&(1<<(j+6)):steps.append(f'v,{j+1}')
 steps+=['e','o,1','o,2','o,3','q,1','q,2','q,3',f'z,{1+mask%3}','e','l','e'];run(steps)
rng=random.Random(0x32bb22c)
for _ in range(45):
 steps=['a','d,2','j,2','s,2']
 for j in range(45):
  mode=rng.choice(['s','p','r','h','a','n','j','c','f','t','b','v','z','e','l','o','q','k'])
  if mode in('s','n','j','c'):steps.append(f'{mode},{rng.choice([-2147483648,-1,0,1,2,3,4,2147483647])}')
  elif mode in('t','b','v','z','o','q'):steps.append(f'{mode},{rng.randrange(-1,6)}'+(','+str(rng.randrange(2))if mode=='b'else''))
  elif mode in('f','k'):steps.append(f'{mode},{rng.randrange(16)}')
  else:steps.append(mode)
 run(steps)
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-user-session-')as directory:
 exe=Path(directory)/'verify.exe';files=['OriginalUserSessionData','OriginalPrelevelBoosterSelectionData','OriginalAnimationDataReset']
 p=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-langversion:latest','-out:'+str(exe),str(PROJECT/'SourceCatalog/UserSessionHarness.cs'),*[str(PROJECT/'Assets/Foundation/Runtime'/(n+'.cs'))for n in files]],capture_output=True,text=True);assert p.returncode==0,p.stdout+p.stderr
 p=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8-sig');assert p.returncode==0,p.stderr[-1500:]
 actual=p.stdout.splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
 for i,(a,e)in enumerate(zip(actual,expected)):
  if a!=e:
   a0,aa=a.split('#');e0,ee=e.split('#');assert a0==e0,(i,a0,e0)
   j=next(j for j,(x,y)in enumerate(zip(aa.split('|'),ee.split('|')))if x!=y)
   raise AssertionError((i,j,cases[i].split(';')[j],aa.split('|')[j],ee.split('|')[j]))
report={'passed':True,'native_sequences':len(cases),'native_commands':commands,'prelevel_boolean_combinations':512,'animation_reset_snapshot_count':32,
 'scope':'Actual ARM64 UserSessionData constructor/reset/scene-change/pause control flow, AnimationData.Reset and prelevel selection/timing/retry/usage-source/reset operations. Verifies constructor store/subscribe order, shared animation identity, global-current versus owned active level, Home2/level1 gates, history saturation at5, pause-only flag clearing, reset retention, all512selection/time/retry combinations, invalid booster ids,32snapshot null positions and four dictionary-clear failures with preceding writes retained. Session tags and numeric animation progress survive reset; only consume flags and four dictionaries clear. Supplied boundaries: CLR metadata/allocation/errors/dictionaries/delegate/event transport, child constructor bodies, global user/session/scene manager. Actual animation/active constructors separately native-verified and actual ownership checked in Unity. Full active/prelevel save restore, database/user/application lifecycle composition and formal playable input remain pending; SDK unchanged.'}
(PROJECT/'user_session_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
