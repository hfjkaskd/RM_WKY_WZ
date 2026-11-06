"""CellView normal initialization/lifecycle and model world/local positions vs ARM64."""
import sys
sys.dont_write_bytecode=True
import json,os,struct,subprocess,tempfile,random
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_move_budget.py').read_text().split('cases=[];expected=[]')[0],str(PROJECT/'SourceCatalog/verify_move_budget.py'),'exec'))
emu.hook_del(native_hook);base_hook=hook
view,factory,model,mask,border,library,assets,parent,transform,user,cell=[0x10040000+i*0x100 for i in range(11)]
renderers=[0x10042000+i*0x100 for i in range(14)];arrays=[0x10044000,0x10044100]
pairs=0x10044200;stub_point,stub_fill=0x20000100,0x20000104
put(pairs,stub_point,8);put(pairs+9*16,stub_fill,8);put(model,0x10045000,8)
for i,got in enumerate((0x5395958,0x53df178,0x536b340)):
 cls=0x10050000+i*0x800;static=cls+0x400;put(read(got,8),cls,8);put(cls+0xe0,1);put(cls+0xb8,static,8)
 if got==0x53df178:
  for j in(12,16,20):put(static+j,0x3f800000)
head=[];calls=0;point_calls=0;state=[];pos=[];scale=[];parented=False
def bits(f):return struct.unpack('<I',struct.pack('<f',f))[0]
def val(b):return struct.unpack('<f',struct.pack('<I',b&0xffffffff))[0]
def f32(f):return val(bits(f))
def record(n,*a):
 global calls
 trace.append(','.join(map(str,(n,*a))));calls+=1
 if calls==head[1]:fail(emu);return False
 return True
def putfloats(values):
 for i,v in enumerate(values):emu.reg_write(UC_ARM64_REG_S0+i,v)
def hook(uc,address,size,unused):
 global point_calls,pos,scale,parented
 x0,x1,x2=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(3)]
 if address==0xfcd934:
  assert x2 in(0,9,10);put(pairs+10*16,0x20000108,8);returned(uc,pairs+x2*16)
 elif address==0x20000108:
  if record('unfreeze'):returned(uc)
 elif address==stub_point:
  if record('point'):
   y=head[4]if point_calls==0 else head[5];point_calls+=1;returned(uc,(head[3]&0xffffffff)|((y&0xffffffff)<<32))
 elif address==stub_fill:
  if record('filling'):returned(uc,head[6])
 elif address==0x3e4737c:
  assert x0==view
  if record('transform'):returned(uc,transform if head[0]&1 else 0)
 elif address==0x3e4ec54:
  if record('parent',int(x1!=0)):
   v=[val(b)for b in pos]
   if parented:v=[f32(a+b)for a,b in zip(v,(3,4,5))]
   if x1:v=[f32(a-b)for a,b in zip(v,(3,4,5))]
   pos=list(map(bits,v));parented=x1!=0;returned(uc)
 elif address==0x3e4e400:
  if record('localget'):putfloats(pos);returned(uc)
 elif address==0x3e4e2c8:
  if record('worldget'):putfloats([bits(f32(val(v)+(d if parented else 0)))for v,d in zip(pos,(3,4,5))]);returned(uc)
 elif address in(0x3e4e4a0,0x3e4e9f8):
  v=[uc.reg_read(UC_ARM64_REG_S0+i)for i in range(3)]
  if record('local'if address==0x3e4e4a0 else'scale',*v):
   if address==0x3e4e4a0:pos=v
   else:scale=v
   returned(uc)
 elif address==0x260a0a4:
  if record('assets'):returned(uc,assets)
 elif address==0x3446a08:
  if record('mask',x1):returned(uc)
 elif address==0x3442894:
  if record('init'):returned(uc)
 elif address in(0x34430cc,0x3442c14,0x3445a08,0x3442ab4):
  n={0x34430cc:'show',0x3442c14:'hide',0x3445a08:'activate',0x3442ab4:'recycle'}[address]
  if record(n,*([signed(x1&0xffffffff),x2]if n=='show'else[])):returned(uc)
 elif address==0x29e27cc:returned(uc,user if head[0]&2 else 0)
 elif address==0x3e4d92c:returned(uc,x0!=0)
 elif address==0x32037e4:
  if not head[0]&2:fail(uc)
  elif record('delta'):putfloats([head[8]&0xffffffff]);returned(uc)
 elif address==0x3e473b8:
  i=renderers.index(x0)
  if record('game',i):returned(uc,0 if i==head[9]else x0+0x40)
 elif address==0x3e49b30:
  i=renderers.index(x0-0x40)
  if record('active',i,x1):state[i][1]=x1;returned(uc)
 elif address in(0x3e3e804,0x3e50f78,0x3e5104c):
  if not x0:fail(uc);return
  i=renderers.index(x0)
  if address==0x3e50f78:
   v=[uc.reg_read(UC_ARM64_REG_S0+j)for j in range(4)]
   if record('color',i,*v):state[i][3:7]=v;returned(uc)
  elif record('enabled'if address==0x3e3e804 else'interaction',i,x1):state[i][0 if address==0x3e3e804 else 2]=x1;returned(uc)
 else:base_hook(uc,address,size,unused)
emu.hook_add(UC_HOOK_CODE,hook)
call(0x3447e14)
def snapshot():return str(int(read(view+0x58,8)!=0))+':'+','.join(map(str,(*pos,*scale,int(parented))))+':'+str(read(view+0x38))+','+str(read(view+0x48,1))+':'+ '/'.join(','.join(map(str,s))for s in state)
cases=[];expected=[];commands=0
def run(h,steps):
 global head,calls,point_calls,state,pos,scale,parented,commands
 head=h;calls=0;point_calls=0;state=[[0,0,0,*map(bits,(.1,.2,.3,.4))]for i in range(14)];pos=[bits(.125),bits(-2),h[7]&0xffffffff];scale=list(map(bits,(2,3,4)));parented=False
 emu.mem_write(view,bytes(0x100));put(view+0x18,mask if h[0]&16 else 0,8);put(view+0x20,border if h[0]&32 else 0,8)
 put(view+0x30,renderers[0]if h[2]!=0 else 0,8);put(view+0x28,renderers[1]if h[2]!=1 else 0,8)
 put(factory+0x18,parent if h[0]&64 else 0,8);put(factory+0x28,library if h[0]&8 else 0,8);put(view+0x38,bits(1.5));put(view+0x48,1,1)
 put(cell+0xb8,view,8)
 for j in range(2):
  length=h[10+j];put(border+0x20+8*j,arrays[j]if length>=0 else 0,8);put(arrays[j]+0x18,max(0,length))
  for i in range(max(0,length)):
   ident=(2 if j==0 else 10)+i;put(arrays[j]+0x20+8*i,renderers[ident]if ident!=h[2]else 0,8)
 output=[]
 for step in steps:
  trace.clear();commands+=1;mode,*a=step;result='ok'
  if mode=='i':call(0x3446d60,view,model if h[0]&4 else 0,factory if h[0]&2 else 0)
  elif mode in('v','m'):call(0x3447110 if mode=='v'else 0x343dadc,view,a[0])
  elif mode=='id':result='value,'+str(call(0x344738c,view))
  elif mode=='point':
   call(0x3446cc0,view);v=emu.reg_read(UC_ARM64_REG_X0);result='point,'+str(signed(v&0xffffffff))+','+str(signed(v>>32))
  elif mode in('p','world','local'):
   call({'p':0x343e6bc,'world':0x3191dd4,'local':0x319db58}[mode],view if mode=='p'else cell)
   result=('position,'if mode=='p'else'vector,')+','.join(str(emu.reg_read(UC_ARM64_REG_S0+i))for i in range(2 if mode=='p'else 3))
  elif mode=='u':call(0x3446be4,view)
  else:call({'e':0x3447cd0,'d':0x34476e8,'a':0x344004c,'r':0x3447398,'s':0x3447394}[mode],view)
  output.append(('error'if fault else result)+':'+snapshot()+':'+ '/'.join(trace))
 cases.append(','.join(map(str,h))+'#'+';'.join(','.join(map(str,s))for s in steps));expected.append('|'.join(output))
cache=Path(tempfile.gettempdir())/'rm_cell_lifecycle_cases.json'
def default(flags=127,failure=0,missing=-1,x=3,y1=4,y2=9,filling=1,z=7,delta=.5,game=-1,borders=8,fills=4):return[flags,failure,missing,x,y1,y2,filling,bits(z),bits(delta),game,borders,fills]
if '--managed-only'in sys.argv:cases,expected,commands=json.loads(cache.read_text())
else:
 for flags in(0,1,3,7,15,31,63,127,125,123,119,111,95,126):
  for failure in range(32):run(default(flags,failure),[('i',),('v',1),('v',0),('e',),('r',),('p',),('world',),('local',),('point',),('m',0),('d',),('a',),('id',),('s',)])
 for missing in range(-1,14):
  for game in(-1,missing):run(default(missing=missing,game=game),[('i',),('e',),('r',),('v',1),('d',)])
 for x in(-2147483648,-1,0,2147483647):
  for y in(-2147483648,-1,0,2147483647):
   for z in(-7,0,7):run(default(x=x,y1=y,y2=-19,z=z),[('i',),('p',),('world',),('local',),('point',)])
 for borders in(-1,0,3,8):
  for fills in(-1,0,2,4):run(default(borders=borders,fills=fills),[('i',),('e',),('r',),('d',),('m',1)])
 for dt in(0,.5,2):
  for failure in range(8):run(default(delta=dt,failure=failure),[('i',),('u',),('u',),('u',),('r',)])
 cache.write_text(json.dumps([cases,expected,commands]))
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-cell-lifecycle-')as folder:
 exe=Path(folder)/'verify.exe';names=('OriginalCellView','OriginalCellViewDelegate','OriginalCellViewSettings','OriginalCellFreezeCountdown')
 p=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-langversion:latest','-out:'+str(exe),str(PROJECT/'SourceCatalog/CellLifecycleHarness.cs'),*[str(PROJECT/'Assets/Foundation/Runtime'/(n+'.cs'))for n in names]],capture_output=True,text=True);assert p.returncode==0,p.stdout+p.stderr
 p=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert p.returncode==0,p.stderr
 actual=p.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
 for i,(a,e)in enumerate(zip(actual,expected)):
  aa,ee=a.split('|'),e.split('|');assert len(aa)==len(ee),(i,len(aa),len(ee))
  for j,(x,y)in enumerate(zip(aa,ee)):assert x==y,(i,j,cases[i],x,y)
report={'passed':True,'native_sequences':len(cases),'native_commands':commands,'scope':'Actual ARM64 CellView normal InitializeView/SetCellColor/ShowView, Update with shared level delta and real freeze-countdown state, mask-state/interaction lifecycle, normal OnRecycle, pool identity/OnSpawn, CellPoint/Position and CellModel world/local position. Exact renderer/transform/countdown state and ordered/injected-failure callbacks. Typed factory/context, original separately verified CellMask/CellBorder, unfreeze callback and Unity transform/renderer storage are supplied boundaries. Full shifter/water behavior, native staged initialization, session and fall composition remain pending.'}
(PROJECT/'cell_lifecycle_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
