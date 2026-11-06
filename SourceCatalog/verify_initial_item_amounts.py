"""Execute actual ARM64 initial pool counts and level-presence queries against C#."""
import sys
sys.dont_write_bytecode=True
import json,os,random,subprocess,tempfile
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_move_budget.py').read_text().split('cases=[];expected=[]')[0],str(PROJECT/'SourceCatalog/verify_move_budget.py'),'exec'))
emu.hook_del(native_hook);base_hook=hook
owner,items,statics,level=0x10040000,0x10041000,0x10042000,0x10043000
goals_pool,counts_pool=0x10044000,0x10045000
objects={};head=[];calls=0
def i32(v):return signed(v&0xffffffff)
def record(s):
 global calls
 trace.append(s);calls+=1
 if calls==head[5]:fail(emu);return False
 return True
def hook(uc,address,size,unused):
 x0,x1,x2,x3,x8=[uc.reg_read(UC_ARM64_REG_X0+i)for i in(0,1,2,3,8)]
 if address==0x2b44b10 and head[0]!=4:
  if record('particle,'+str(i32(x0))):returned(uc,head[3])
 elif address==0x2d78788:returned(uc,head[2])
 elif address==0x29e27cc:assert x0==2;returned(uc,level)
 elif address==0x1e19890:
  if record('piggy'):returned(uc,head[4])
 elif address==0x2657978:returned(uc,min(i32(x0),i32(x1)))
 elif address==0x2627eb8:
  emu.mem_write(x0+0x20+4*x1,bytes(4*x2));returned(uc)
 elif address in(0x3c9c9ec,0x39aefec):objects[x0].clear();returned(uc)
 elif address in(0x3c9ca58,0x39af058):returned(uc,i32(x1)in objects[x0])
 elif address in(0x3c9c858,0x39aee60,0x3c9c840,0x39aee48):objects[x0][i32(x1)]=x2 if address in(0x3c9c858,0x3c9c840)else i32(x2);returned(uc)
 elif address in(0x3c9c7a4,0x39aedb0):
  k=i32(x1)
  if k not in objects[x0]:fail(uc)
  else:returned(uc,objects[x0][k])
 elif address==0x39b08ec:
  k=i32(x1);put(x2,objects[x0].get(k,0));returned(uc,k in objects[x0])
 elif address in(0x3c9ccac,0x39af254,0x2c1c668):
  emu.mem_write(x8,bytes(40 if address==0x3c9ccac else 32));put(x8,x0,8);returned(uc)
 elif address in(0x347c108,0x3171068,0x2b3560c):
  values=objects[read(x0,8)];index=read(x0+8);values=list(values.items())if isinstance(values,dict)else values
  if index>=len(values):returned(uc,0)
  else:
   put(x0+8,index+1)
   if address==0x2b3560c:put(x0+0x10,values[index],8)
   else:
    k,v=values[index];put(x0+0x10,k);put(x0+(0x18 if address==0x347c108 else 0x14),v,8 if address==0x347c108 else 4)
   returned(uc,1)
 elif address in(0x347c224,0x3171160,0x2b35608):returned(uc)
 else:base_hook(uc,address,size,unused)
native_amounts_hook=emu.hook_add(UC_HOOK_CODE,hook)
cases=[];expected=[];rng=random.Random(4172)
def dict_text(v):return 'null'if v is None else '/'.join(str(k)+','+str(a)for k,a in v.items())
def boards_text(v):return 'null'if v is None else 'empty'if not v else '~'.join(dict_text(d)for d in v)
def store_object(value):
 p=allocate(0x30);objects[p]=value;return p
def run(h,d=None,goals=None,counts=None,args=None,initial=None):
 global head,calls,next_object
 head=h;calls=0;next_object=0x10060000;objects.clear();trace.clear();emu.mem_write(owner,bytes(0x100));put(owner+0x10,items,8);put(owner+0x18,statics,8);put(owner+0x28,goals_pool,8);put(owner+0x30,counts_pool,8);objects[goals_pool]={};objects[counts_pool]={}
 iv,sv=initial or ([rng.randrange(-4,9)for _ in range(116)],[rng.randrange(-4,9)for _ in range(20)])
 for ptr,values in((items,iv),(statics,sv)):
  put(ptr+0x18,len(values))
  for i,v in enumerate(values):put(ptr+0x20+i*4,v)
 dp=store_object(d)if d is not None else 0
 result='ok';args=args or [0]*12;args[4]&=1
 if h[0]==0:call(0x33f11bc,owner,h[1],h[2],dp)
 elif h[0]==1:call(0x33f2420,owner,h[1],h[2],dp)
 elif h[0]==2:
  gp=[]
  for board in goals or []:
   if board is None:gp.append(0);continue
   temp={}
   for k,v in board.items():
    t=allocate(0x20);put(t+0x10,k);put(t+0x14,v);temp[k]=t
   gp.append(store_object(temp))
  g=store_object(gp)if goals is not None else 0
  cp=[store_object(b)if b is not None else 0 for b in counts or []];c=store_object(cp)if counts is not None else 0
  # Seven trailing int arguments occupy ARM64 eight-byte stack slots.
  for i,v in enumerate(args[5:]):put(0x101ff000+i*8,v,8)
  call(0x33f0500,owner,g,c,*args[:5])
 elif h[0]==3:
  values=[]
  for a,params in((0x33f2518,(owner,h[1])),(0x33f2548,(owner,h[2])),(0x1e197a8,(level,)),(0x33f24dc,(owner,h[1],h[3])),(0x33f2578,(owner,h[2],h[3]))):
   put(level+0xc0,owner,8);values.append(call(a,*params))
   if fault:break
  result='value,'+','.join(str(i32(x))for x in values)
 elif h[0]==4:result='value,'+str(i32(call(0x2b44b10,h[1])))
 result='error'if fault else result
 state=lambda ptr,n:','.join(str(i32(read(ptr+0x20+4*i)))for i in range(n))
 expected.append(result+':'+state(items,116)+':'+state(statics,20)+':'+str(i32(read(owner+0x20)))+':'+'/'.join(trace))
 cases.append(','.join(map(str,h))+'#'+','.join(map(str,iv))+'#'+','.join(map(str,sv))+'#'+dict_text(d)+'#'+boards_text(goals)+'#'+boards_text(counts)+'#'+','.join(map(str,args)))
# Goal types include native fall-through gaps and all original switch branches.
for goal in range(-1,108):
 for target,limit in((0,74),(36,74),(100,37),(-3,74),(2147483647,74)):
  run([0,goal,target,limit,goal%2,0],{k:rng.randrange(-2,100)for k in range(-19,116)})
for goal in(14,18,22,23,27,39,48,66,77,105):
 for d in(None,{}, {13:4,50:2,67:3}):
  for failure in(0,1,2):run([0,goal,75,74,1,failure],d)
for type in(-21,-19,-14,-1,0,1,6,115,116):run([1,type,int(type<0),74,0,0],{type:17})
for i in range(120):
 gs=[dict((rng.randrange(1,106),rng.randrange(-4,100))for _ in range(6))for _ in range(3)]
 cs=[dict((rng.randrange(-19,116),rng.randrange(-4,100))for _ in range(8))for _ in range(3)]
 for c in cs:c[13]=4
 run([2,0,0,37 if i%2 else 74,i%2,0],{},gs,cs,[rng.randrange(-4,100)for _ in range(12)])
for gs,cs in(([],[]),(None,[]),([None],[]),([{}],None),([{}],[None])):run([2,0,0,74,0,0],{},gs,cs)
for k in(0,6,115,-1,116):
 for s in(0,4,7,19,-1,20):run([3,k,s,19,0,0],{})
for n in(-2147483648,-7,-1,0,1,2,3,10,74,2147483647):
 for low in(0,1):run([4,n,low,0,0,0],{})
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-initial-amounts-')as folder:
 exe=Path(folder)/'verify.exe';names=('OriginalInitialItemAmounts','OriginalGoalTuple','OriginalGoalTypes','OriginalLevelRules','OriginalTiledItemTypes')
 p=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-langversion:latest','-out:'+str(exe),str(PROJECT/'SourceCatalog/InitialItemAmountsHarness.cs'),*[str(PROJECT/'Assets/Foundation/Runtime'/(n+'.cs'))for n in names]],capture_output=True,text=True);assert p.returncode==0,p.stdout+p.stderr
 p=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8-sig');assert p.returncode==0,p.stderr
 actual=[a.replace('\ufeff','') for a in p.stdout.splitlines()];assert len(actual)==len(expected),(len(actual),len(expected))
 for i,(a,e)in enumerate(zip(actual,expected)):assert a==e,(i,[hex(ord(c))for c in a[:4]],cases[i],a,e)
report={'passed':True,'native_cases':len(cases),'scope':'Actual ARM64 initial item/static amount goal switch, non-goal counts, full CreateItemLists orchestration with per-board max aggregation, pool-count overrides and story goal totals, item/static queries, differences and level honey/roof presence;20 direct native particle-budget cases with supplied low-end classification. CLR dictionaries/enumeration, original goal tuple storage, device particle amount and filling helmet-piggy context are supplied boundaries. Device classification, unique filling-item ownership and full application startup remain pending.'}
(PROJECT/'initial_item_amounts_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))

