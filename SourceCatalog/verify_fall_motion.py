"""Differential concrete FallComponent ARM64 trajectories and state transitions."""
import sys
sys.dont_write_bytecode=True
import json,os,random,subprocess,tempfile,struct,math
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_move_budget.py').read_text().split('cases=[];expected=[]')[0],str(PROJECT/'SourceCatalog/verify_move_budget.py'),'exec'))
from unicorn.arm64_const import UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2,UC_ARM64_REG_D0
emu.hook_del(native_hook);base_hook=hook
comp,grid,board,level,sfx,entry=0x10020000,0x10021000,0x10021400,0x10021600,0x10021800,0x10022000
cells=[0x10030000+i*0x400 for i in range(12)];items=[0x10040000+i*0x1000 for i in range(2)];views=[p+0x400 for p in items];falls=[comp,comp+0x400]
vector2_slot=read(0x53dca28,8);filling_slot=read(0x53e7838,8) # ELF GOT entries refer to TypeInfo slots, not classes.
cell_ids={p:i for i,p in enumerate(cells)};item_ids={p:i+1 for i,p in enumerate(items)};view_ids={p:i+1 for i,p in enumerate(views)}
h=[];v=[];slots=[];positions=[];view_positions=[];calls=0;last_previous=0;last_max=0

def bits(f):return int.from_bytes(struct.pack('<f',f),'little')
def value(b):return struct.unpack('<f',int(b&0xffffffff).to_bytes(4,'little'))[0]
def sf(reg):return value(emu.reg_read(reg))
def vector(uc,values):
 for reg,f in zip((UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2),values):uc.reg_write(reg,bits(f))
def record(uc,label):
 global calls
 trace.append(label);calls+=1
 if calls==h[16]:fail(uc);return False
 return True
def ci(p):return -1 if not p else cell_ids[p]
def cp(i):return 0 if i<0 else cells[i]
def ip(i):return 0 if not i else items[i-1]
def clearslot(i,s):
 item=slots[i][s]
 if item:positions[item-1][s]=-1
 slots[i][s]=0
def setslot(i,s,item):
 if not item or(slots[i][s]and slots[i][s]!=item):return False
 if slots[i][s]==item:return True
 old=positions[item-1][s]
 if old>=0:slots[old][s]=0
 slots[i][s]=item;positions[item-1][s]=i;return True
def place(item,c,n,t):
 for s in(0,2,1):
  old=positions[item][s]
  if old>=0:clearslot(old,s)
 for s,i in enumerate((c,n,t)):
  if i>=0:setslot(i,s,item+1)
def neighbor(i,d):
 x,y=i%3,i//3;x+=[-1,0,1,1,1,0,-1,-1][d];y+=[1,1,1,0,-1,-1,-1,0][d]
 return y*3+x if 0<=x<3 and 0<=y<4 else -1

def motion_hook(uc,address,size,unused):
 global last_previous,last_max
 x0,x1,x2=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(3)]
 if address==0x20000100:
  i=item_ids[x0]-1
  if record(uc,'view,'+str(i+1)):returned(uc,0 if h[14]&(1<<i)else views[i])
 elif address==0x20000110:
  i=item_ids[x0]-1
  if record(uc,'item_pos,'+str(i+1)):vector(uc,view_positions[i]);returned(uc)
 elif address==0x2b4aa30:
  i=view_ids[x0]-1
  if record(uc,'position,'+str(i+1)):vector(uc,view_positions[i]);returned(uc)
 elif address==0x3e4737c:returned(uc,x0)
 elif address==0x3e4e400:
  i=view_ids[x0]-1
  if record(uc,'local,'+str(i+1)):
   q=view_positions[i][:];q[1]=value(bits(q[1]+v[14]));vector(uc,q);returned(uc)
 elif address==0x2b485ec:
  i=view_ids[x0]-1
  if record(uc,'set_position,'+str(i+1)):view_positions[i]=[sf(r)for r in(UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2)];returned(uc)
 elif address==0x3191dd4:
  i=ci(x0)
  if record(uc,'cell_pos,'+str(i)):vector(uc,(i%3,i//3,0));returned(uc)
 elif address==0x3205e3c:
  if record(uc,'time'):uc.reg_write(UC_ARM64_REG_S0,bits(v[5]));returned(uc)
 elif address==0x32037e4:
  if record(uc,'delta'):uc.reg_write(UC_ARM64_REG_S0,bits(v[6]));returned(uc)
 elif address==0x3196c8c:
  i=ci(x0)
  if record(uc,'board,'+str(i)):returned(uc,i//9==h[17])
 elif address==0x12d81cc:
  if record(uc,'cross_sort'):uc.reg_write(UC_ARM64_REG_X1,0);returned(uc,3|(9010<<32))
 elif address in(0x20000120,0x20000130,0x20000140,0x20000150,0x20000160,0x20000170,0x20000180,0x2b470f4):
  i=view_ids[x0]-1
  label={0x20000120:'mask',0x20000130:'sorting',0x20000140:'state',0x20000150:'started',0x20000160:'ended',0x20000170:'animation',0x20000180:'set_under',0x2b470f4:'arrange'}[address]
  label+=','+str(i+1)
  if address in(0x20000140,0x20000180):label+=','+str(signed(x1&0xffffffff))
  if record(uc,label):
   if address==0x20000120:put(x0+0x18,0,1)
   if address==0x20000180:put(x0+0x68,x1,1)
   if address==0x20000140:last_previous=uc.reg_read(UC_ARM64_REG_S0);last_max=uc.reg_read(UC_ARM64_REG_S1)
   returned(uc)
 elif address==0x1e197a8:
  if record(uc,'has_static'):returned(uc,h[10])
 elif address==0x2b470cc:
  if record(uc,'under_item'):returned(uc,h[11])
 elif address==0x3440fa4:
  i=ci(x0-0x350)
  if record(uc,f'static,{i},{x1}'):returned(uc,bool(h[12 if x1==4 else 13]&(1<<i)))
 elif address==0x218022c:
  if record(uc,'sfx'):returned(uc)
 elif address in(0x2b45968,0x2b45a7c,0x2b45b90):
  i=item_ids[x0-0x100]-1;returned(uc,cp(positions[i][{0x2b45968:0,0x2b45a7c:1,0x2b45b90:2}[address]]))
 elif address==0x2b45a6c:returned(uc,positions[item_ids[x0-0x100]-1][0]>=0)
 elif address in(0x343bd38,0x343bd5c,0x34487a4):
  i=ci(x0-0x100)
  returned(uc,slots[i][0]!=0 if address==0x343bd38 else ip(slots[i][0 if address==0x343bd5c else 1]))
 elif address in(0x344863c,0x3448654):returned(uc,setslot(ci(x0-0x100),0 if address==0x344863c else 1,item_ids[x1]))
 elif address==0x343d1ec:put(x0+0xe0,0,8);returned(uc)
 elif address==0x343d3f4:returned(uc) # Incoming set empty, CanRefill true in this motion fixture.
 elif address==0xfcd934:assert x0 in falls and x2==1;put(entry,0x20000190,8);returned(uc,entry)
 elif address==0x20000190:uc.reg_write(UC_ARM64_REG_S0,read(x0+0x5c));returned(uc)
 elif address==0xd01b50:
  arg=struct.unpack('<d',int(uc.reg_read(UC_ARM64_REG_D0)).to_bytes(8,'little'))[0]
  uc.reg_write(UC_ARM64_REG_D0,int.from_bytes(struct.pack('<d',math.acos(arg)),'little'));returned(uc)
 else:base_hook(uc,address,size,unused)
emu.hook_add(UC_HOOK_CODE,motion_hook)

def apply_headers():
 put(comp+0x40,0 if h[19]else grid,8);put(comp+0x38,0 if h[20]else level,8);put(comp+0x30,0 if h[21]else sfx,8);put(board+0x44,h[18],1)
 for i,p in enumerate(cells):put(p+0x28,int(bool(h[9]&(1<<i))))

def initialize(head,metrics):
 global h,v,slots,positions,view_positions,calls,last_previous,last_max,next_object
 h=head[:];v=[value(bits(f))for f in metrics];slots=[[0,0,0]for _ in cells];positions=[[-1,-1,-1]for _ in items];view_positions=[v[7:10],v[10:13]];calls=0;last_previous=0;last_max=0;next_object=0x10070000
 emu.mem_write(comp,bytes(0x3000));emu.mem_write(cells[0],bytes(0x3000));emu.mem_write(items[0],bytes(0x2000));put(grid+0xf0,board,8)
 call(0xffd960,vector2_slot);klass=read(vector2_slot,8);static=read(klass+0xb8,8);put(static+0x28,bits(1));put(static+0x2c,0)
 call(0xffd960,filling_slot);fill_type=read(filling_slot,8);put(fill_type+0x12c,1,1)
 for i,p in enumerate(cells):
  cls=0x10060000+(i==10)*0x1000;put(p,cls,8);put(cls+0x12c,1,1);put(cls+0xc8,cls+0x200,8);put(cls+0x200,fill_type if i==10 else 0,8)
  put(p+0x10,p+0x100,8);put(p+0x18,i%3);put(p+0x1c,i//3);put(p+0x20,p+0x200,8);put(p+0x200+0x18,p+0x280,8);put(p+0x280+0x18,8);put(p+0xc0,p+0x350,8)
  for d in range(8):put(p+0x280+0x20+d*8,cp(neighbor(i,d)),8)
 for i,p in enumerate(items):
  cls=p+0x800;vc=p+0xc00;put(p,cls,8);put(p+0x38,p+0x100,8);put(p+0x40,0 if h[15]&(1<<i)else falls[i],8)
  put(cls+0x418,0x20000100,8);put(cls+0x568,0x20000110,8);put(views[i],vc,8);put(views[i]+0x18,1,1);put(views[i]+0x68,h[11],1)
  for off,fn in((0x278,0x20000120),(0x208,0x20000130),(0x2a8,0x20000140),(0x2b8,0x20000150),(0x2c8,0x20000160),(0x2d8,0x20000170),(0x308,0x20000180)):put(vc+off,fn,8)
  put(falls[i],cls+0x400,8);put(falls[i]+0x10,p,8);put(falls[i]+0x18,p+0x100,8)
 put(comp+0x20,cp(h[5]),8);put(comp+0x28,h[0]);put(comp+0x2c,h[1],1);put(comp+0x48,h[2],1);put(comp+0x49,h[3],1);put(comp+0x58,h[4],1)
 for off,f in zip((0x5c,0x60,0x4c,0x50,0x54),v[:5]):put(comp+off,bits(f))
 put(falls[1]+0x5c,bits(v[13]));place(0,*h[6:9]);apply_headers()
 if h[5]>=0:put(cp(h[5])+0xe0,items[0],8)

def state_text():
 ints=[read(comp+0x28),read(comp+0x2c,1),read(comp+0x48,1),read(comp+0x49,1),read(comp+0x58,1),ci(read(comp+0x20,8)),item_ids.get(read(cells[10]+0xe0,8),0),read(views[0]+0x18,1),read(views[0]+0x68,1)]
 for row in slots:ints+=row
 floats=[read(comp+o)for o in(0x5c,0x60,0x4c,0x50,0x54)]+[bits(f)for f in view_positions[0]]+[last_previous,last_max]
 return ','.join(map(str,ints))+'/'+ '/'.join(f'{n:08x}'for n in floats)
cases=[];expected=[];commands=0

def run(head,metrics,steps):
 global calls,commands,fault
 initialize(head,metrics);out=[]
 for cmd in steps:
  calls=0;trace.clear();fault=False;parts=cmd.split(',');mode=parts[0];result='-';commands+=1
  if mode in('m','s','c','f','n','t','r','g','u','p'):
   call({'m':0x2ee8b1c,'s':0x2ee8cc8,'c':0x2ee8dec,'f':0x2ee8fbc,'n':0x2ee9458,'t':0x2ee89fc,'r':0x2ee8e3c,'g':0x2ee9404,'u':0x2ee923c,'p':0x2ee896c}[mode],comp,falls[1]if mode=='p'else int(parts[1])if len(parts)>1 else 0)
   if mode=='g':result=f'{emu.reg_read(UC_ARM64_REG_S0):08x}'
  elif mode in('b','z'):place(1 if mode=='b'else 0,*[int(a)for a in(parts[1:]+['-1'])[:3]])
  elif mode=='v':view_positions[0]=[value(bits(float(a)))for a in parts[1:]]
  elif mode=='h':h[int(parts[1])]=int(parts[2]);apply_headers()
  if fault:result='error'
  out.append(result+':'+state_text()+':'+ ';'.join(trace))
 cases.append(','.join(map(str,head))+'#'+','.join(str(f)for f in metrics)+'#'+';'.join(steps));expected.append('|'.join(out))
def world():return [0,0,0,0,0,-1,7,7,1,0,0,0,0,0,0,0,0,0,0,0,0,0],[0,0,0,2,0,12.25,1/60,1,2,0,1,0,0,8,0]
# Full ordinary straight/left/right trajectories at multiple time steps and caps.
for target in(0,1,2):
 for dt in(0,1/120,1/60,1/30,.1):
  for scroll in(0,1):
   head,metrics=world();head[8]=target;head[18]=scroll;metrics[6]=dt;run(head,metrics,['m']*85+['s','g,0','p'])
# Filling entry, delayed admission behind another item and blocked next slots.
for gate in range(32):
 head,metrics=world();head[5]=10;head[6]=-1;head[7]=-1;metrics[8]=4;metrics[3]=4;head[9]=(1<<10)if gate&1 else 0;metrics[13]=0 if gate&2 else 8
 if gate&4:metrics[11]=2.6
 head[15]=2 if gate&8 else 0;head[18]=bool(gate&16)
 run(head,metrics,['b,10,10'if gate&2 else 'b,-1,10','f','m','h,9,0']+['m']*40)
# State, mask/static/callback boundaries, preserved state after exceptions.
for failure in range(1,35):
 head,metrics=world();head[16]=failure;head[10]=1;head[11]=1;head[1]=1;head[3]=1;head[8]=0
 run(head,metrics,['t,1','n','u','m','r','s','t,0','g,1','p'])
for gate in range(64):
 head,metrics=world();head[0]=gate&1;head[1]=(gate>>1)&1;head[3]=(gate>>2)&1;head[10]=1;head[11]=(gate>>3)&1;head[19]=(gate>>4)&1;head[14]=(gate>>5)&1
 run(head,metrics,['t,0','t,1','m','z,1,1,1','v,1,0,0','s','r','m','p'])
# Exact float32 threshold boundaries must choose the same branch on x64 and ARM.
for cell in(1,4,7):
 for offset in(-.001,0,.001,.0010002,.0499999,.05,.0500001):
  head,metrics=world();head[0]=1;head[3]=1;head[10]=1;head[6]=cell;head[7]=cell;head[8]=cell;metrics[7]=cell%3;metrics[8]=cell//3+offset
  run(head,metrics,['s','r','m','t,1','m'])
rng=random.Random(0x2ee8b1c)
for seed in range(90):
 head,metrics=world();head[0]=rng.randrange(3);head[5]=rng.choice([-1,10]);head[6]=rng.randrange(-1,12);head[7]=rng.randrange(-1,12);head[8]=rng.randrange(-1,12);head[9]=rng.randrange(4096);head[10]=rng.randrange(2);head[11]=rng.randrange(2);head[12]=rng.randrange(4096);head[13]=rng.randrange(4096);head[18]=rng.randrange(2)
 for i in(1,2,3,4):head[i]=rng.randrange(2)
 metrics[0]=rng.choice([0,3.5,4,19.5,20,27.5,28,-1]);metrics[6]=rng.choice([0,1/60,.1]);metrics[7:10]=[rng.randrange(3)+rng.choice([0,.0005,.001,.5]),rng.randrange(5)+rng.choice([0,.0005,.001,.5]),rng.choice([0,.25])]
 run(head,metrics,['b,'+str(rng.randrange(-1,12))+','+str(rng.randrange(-1,12)),'n','u','f','r']+['m']*8+['h,9,0']+['m']*8+['p'])
# Encode booleans numerically for the C# input parser.
cases=[line.replace('True','1').replace('False','0')for line in cases]
cache=Path(tempfile.gettempdir())/'rm_fall_motion_cases.json';cache.write_text(json.dumps([cases,expected,commands]))
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-fall-motion-')as directory:
 exe=Path(directory)/'verify.exe';files=('OriginalCellMediation','OriginalGridIterator','OriginalCellFallState','OriginalFillingCell','OriginalFillingItemPlacement','OriginalFallRefillColumns','OriginalFallComponentState','OriginalFallMotion')
 p=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-langversion:latest','-out:'+str(exe),str(PROJECT/'SourceCatalog/FallMotionHarness.cs'),*[str(PROJECT/'Assets/Foundation/Runtime'/(n+'.cs'))for n in files]],capture_output=True,text=True);assert p.returncode==0,p.stdout+p.stderr
 p=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert p.returncode==0,p.stderr[-2000:]
 actual=p.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
 max_position_error=0.0
 for i,(a,e)in enumerate(zip(actual,expected)):
  aa=a.split('|');ee=e.split('|')
  for j,(x,y)in enumerate(zip(aa,ee)):
   if x==y:continue
   ax,ay=x.split(':'),y.split(':');sx,sy=ax[1].split('/'),ay[1].split('/')
   assert ax[0]==ay[0] and ax[2]==ay[2] and sx[0]==sy[0],(i,j,cases[i],x,y)
   for k,(bx,by)in enumerate(zip(sx[1:],sy[1:])):
    if bx==by:continue
    fx,fy=value(int(bx,16)),value(int(by,16));difference=abs(fx-fy)
    # ARM32 float operations and the x64 Mono JIT can round vector arithmetic
    # differently. Require exact discrete state/callbacks and speed/time, and
    # bound position/start-position differences to one millionth of a cell.
    assert 2<=k<=7 and difference<=1e-6,(i,j,k,cases[i],x,y)
    max_position_error=max(max_position_error,difference)
report={'passed':True,'native_sequences':len(cases),'native_commands':commands,'max_position_error_cells':max_position_error,'scope':'Actual ARM64 FallComponent SetFallState, ManualUpdate, StopIfReached, constant-speed dispatch, filling entry, next-cell selection, complete straight/cross movement, accelerated speed, angle calculation, under-static arrangement and CopyFrom. Exact speed/time bits and discrete state; position/start-position tolerance 1e-6 cell across ARM and x64 Mono, cell mediation, flags, mask and ordered callbacks compared through multi-frame trajectories. Item/view/transform/static/grid/clock/SFX and CLR/type/interface/mediation storage are supplied native boundaries; concrete managed motion and mediation execute. Full native constructor context lookup, master/fake fall implementations, actual view/mask/squash/static/SFX application bindings and production gameplay composition remain incomplete.'}
(PROJECT/'fall_motion_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
