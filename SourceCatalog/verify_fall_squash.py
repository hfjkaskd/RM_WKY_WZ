"""Compare actual native FallSquash state and complete frame trajectories."""
import sys
sys.dont_write_bytecode=True
import json,os,random,subprocess,tempfile,struct,math
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_useful_move.py').read_text().split('active, king,')[0],str(PROJECT/'SourceCatalog/verify_useful_move.py'),'exec'))
component,grid,board,transform=0x10020000,0x10021000,0x10021400,0x10022000
h=[];v=[];world=[];local=[];scale=[];trace=[];calls=0;fault=False
def bits(f):return int.from_bytes(struct.pack('<f',f),'little')
def val(b):return struct.unpack('<f',int(b&0xffffffff).to_bytes(4,'little'))[0]
def f32(f):return val(bits(f))
def returned(uc,x=0):uc.reg_write(UC_ARM64_REG_X0,x);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
def vector(uc,values):
 for r,f in zip((UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2),values):uc.reg_write(r,bits(f))
def record(uc,label):
 global calls,fault
 trace.append(label);calls+=1
 if calls==h[3]:fault=True;uc.reg_write(UC_ARM64_REG_PC,0x20000000);return False
 return True
def hook(uc,address,size,data):
 global fault,local,scale
 x0=uc.reg_read(UC_ARM64_REG_X0)
 if address==0x3e4737c:returned(uc,transform if h[2] else 0)
 elif address in(0x3e4e2c8,0x3e4e400,0x3e4e958):
  label,values={0x3e4e2c8:('world',world),0x3e4e400:('local',local),0x3e4e958:('scale',scale)}[address]
  if record(uc,label):vector(uc,values);returned(uc)
 elif address in(0x3e4e4a0,0x3e4e9f8):
  if record(uc,'setlocal'if address==0x3e4e4a0 else 'setscale'):
   a=[val(uc.reg_read(r))for r in(UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2)]
   if address==0x3e4e4a0:local=a
   else:scale=a
   returned(uc)
 elif address==0x32037e4:
  if record(uc,'dt'):uc.reg_write(UC_ARM64_REG_S0,bits(v[9]));returned(uc)
 elif address in(0xd012e0,0xd026f0):
  x=val(uc.reg_read(UC_ARM64_REG_S0));fn=math.sin if address==0xd012e0 else math.cos
  uc.reg_write(UC_ARM64_REG_S0,bits(fn(x)));returned(uc)
 elif address==0x3e4c7ac:returned(uc)
 elif address in(0xffda88,0xffda90):fault=True;uc.reg_write(UC_ARM64_REG_PC,0x20000000)
emu.hook_add(UC_HOOK_CODE,hook)
def call(rva,arg=0,fs=()):
 for reg,value in((UC_ARM64_REG_X0,component),(UC_ARM64_REG_X1,arg),(UC_ARM64_REG_X2,0),(UC_ARM64_REG_SP,0x101ff000),(UC_ARM64_REG_LR,0x20000000)):emu.reg_write(reg,value)
 for i,f in enumerate(fs[:8]):emu.reg_write(UC_ARM64_REG_S0+i,bits(f))
 if len(fs)>8:put(0x101ff000,bits(fs[8]))
 try:emu.emu_start(rva,0x20000000,count=1000000)
 except:print('native failure',hex(emu.reg_read(UC_ARM64_REG_PC)));raise
 assert emu.reg_read(UC_ARM64_REG_PC)==0x20000000
def dump():
 ints=[read(component+o,s)for o,s in((0x58,4),(0x54,4),(0x51,1),(0x50,1))]
 floats=[read(component+o)for o in(0x44,0x48,0x4c,0x5c,0x60,0x64,0x68,0x6c,0x70,0x74)]+[bits(f)for f in local+scale]
 return ','.join(map(str,ints))+'/'+','.join(f'{f:08x}'for f in floats)
cases=[];expected=[];commands=0
def run(head,metrics,steps):
 global h,v,world,local,scale,calls,fault,commands
 h=head[:];v=list(map(f32,metrics));world=v[:3];local=v[3:6];scale=v[6:9]
 emu.mem_write(component,bytes(0x100));put(component+0x18,grid if h[1] else 0,8);put(grid+0xf0,board,8);put(board+0x44,h[0],1)
 for o,s,x in((0x58,4,h[4]),(0x54,4,h[5]),(0x51,1,h[6]),(0x50,1,h[7])):put(component+o,x,s)
 call(0x2eea5d0,fs=v[10:]);out=[]
 for cmd in steps:
  trace.clear();calls=0;fault=False;commands+=1;p=cmd.split(',');m=p[0]
  if m=='t':call(0x2eea5ec,int(p[1]),[float(p[2]),float(p[3])])
  elif m in('u','s','b'):call({'u':0x2eea6d8,'s':0x2eea6f4,'b':0x2eea7e8}[m])
  elif m=='p':call(0x2eea5d0,fs=list(map(float,p[1:])))
  elif m=='w':world[1]=f32(float(p[1]))
  elif m=='d':v[9]=f32(float(p[1]))
  elif m=='c':h[0]=int(p[1]);put(board+0x44,h[0],1)
  out.append(('error'if fault else '-')+':'+dump()+':'+ ';'.join(trace))
 cases.append(','.join(map(str,head))+'#'+','.join(map(str,metrics))+'#'+';'.join(steps));expected.append('|'.join(out))
def default():return [0,1,1,0,0,0,0,0],[0,5,0,.25,0,.5,1,1,1,1/60,.97,.05,.1,.08,.05,.05,-.025,.08,.05]
for scroll in(0,1):
 for speed in(0,4,4.1,7.999,8,8.001,12,20,28):
  for dt in(0,1/120,1/60,1/30,.1):
   a,b=default();a[0]=scroll;b[9]=dt
   steps=['t,1,0,20']
   for y in(4.9,4,3.5,3,2.999,2):steps+=['w,'+str(y),'u']
   steps+=['t,0,'+str(speed)+',20']+['u']*40+['t,1,8,28','w,1.5','u','t,1,9,28','t,0,28,28']+['u']*40
   run(a,b,steps)
for failure in range(1,9):
 a,b=default();a[3]=failure
 run(a,b,['t,1,0,20','u','w,2','u','t,0,20,20']+['u']*30)
for state in range(4):
 for target in range(4):
  a,b=default();a[4]=state
  run(a,b,[f't,{target},12,20','u','t,0,20,20','u','t,1,0,20','u'])
rng=random.Random(0x2eea578)
for seed in range(60):
 a,b=default();a[0]=rng.randrange(2);a[4:]=[rng.randrange(3),rng.randrange(7),rng.randrange(2),rng.randrange(2)]
 b[:9]=[rng.choice([0,1,.97,1.001,1.049,3,-.5])for _ in range(9)];b[9]=rng.choice([0,.01,.1]);b[10:]=[rng.choice([0,.05,.08,.1,.97,1,-.025])for _ in range(9)]
 run(a,b,['u','t,1,8,20','w,-3','s','t,0,20,20']+['u']*35+['p,.96,.06,.1,.08,.06,.04,-.03,.08,.05','t,1,0,28','u','t,0,28,28']+['u']*35)
# Missing grid: UpdateState fails before writing state; idle Update still works.
a,b=default();a[1]=0;run(a,b,['t,1,0,20','u','t,0,20,20'])
cache=Path(tempfile.gettempdir())/'rm_fall_squash_cases.json';cache.write_text(json.dumps([cases,expected,commands]))
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-fall-squash-')as folder:
 exe=Path(folder)/'verify.exe'
 p=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-langversion:latest','-out:'+str(exe),str(PROJECT/'SourceCatalog/FallSquashHarness.cs'),str(PROJECT/'Assets/Foundation/Runtime/OriginalFallSquashState.cs')],capture_output=True,text=True);assert p.returncode==0,p.stdout+p.stderr
 p=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert p.returncode==0,p.stderr
 actual=p.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected),(len(actual),len(expected));max_error=0
 for i,(a,e)in enumerate(zip(actual,expected)):
  aa,ee=a.split('|'),e.split('|');assert len(aa)==len(ee)
  for j,(x,y)in enumerate(zip(aa,ee)):
   if x==y:continue
   ax,ay=x.split(':'),y.split(':');ix,fx=ax[1].split('/');iy,fy=ay[1].split('/')
   assert ax[0]==ay[0] and ax[2]==ay[2] and ix==iy,(i,j,cases[i],x,y)
   for k,(bx,by)in enumerate(zip(fx.split(','),fy.split(','))):
    if bx==by:continue
    vx,vy=val(int(bx,16)),val(int(by,16));delta=abs(vx-vy)
    assert (math.isnan(vx)and math.isnan(vy))or(k>=10 and delta<=1e-6),(i,j,k,cases[i],x,y)
    if not math.isnan(delta):max_error=max(max_error,delta)
report={'passed':True,'native_sequences':len(cases),'native_commands':commands,'max_transform_error':max_error,'scope':'Actual ARM64 FallSquash SetParameters, UpdateState, Update, ScaleItemOnFall and complete BounceItemAfterFall phases; exact state/parameters/internal values and ordered transform callbacks, transform tolerance 1e-6 across x64 Mono/ARM. Grid/board/Transform/time/libm are supplied boundaries; actual native easing and Approximately execute. Native Awake context lookup, complete ItemView static/roof lifecycle and player-driven resolution remain incomplete.'}
(PROJECT/'fall_squash_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
