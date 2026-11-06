"""Actual ARM64 FallManager frame phases, iterators and steady aggregation."""
import sys
sys.dont_write_bytecode=True
import json,os,random,subprocess,tempfile,hashlib,sqlite3
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_move_budget.py').read_text().split('cases=[];expected=[]')[0],str(PROJECT/'SourceCatalog/verify_move_budget.py'),'exec'))
emu.hook_del(native_hook);base_hook=hook
root,grid,board_manager,board,state,pending_ptr,disabled_ptr,entry=0x10020000,0x10021000,0x10021200,0x10021400,0x10021600,0x10022000,0x10022200,0x10022400
cells=[0x10030000+i*0x400 for i in range(9)];items=[0x10040000+i*0x400 for i in range(9)]
cell_ids={p:i for i,p in enumerate(cells)};item_ids={p:i+1 for i,p in enumerate(items)}
strategies={t:0x10050000+t*0x1000 for t in(0,1,4,5)}
h=[];cd=[];iv=[];slots=[];target_positions=[];steady=[];updates=[];pending={};disabled={};held=0;opcount=0;calls=0

def ci(p):return -1 if not p else cell_ids[p]
def ii(p):return 0 if not p else item_ids[p]
def cp(i):return 0 if i<0 else cells[i]
def ip(i):return 0 if i==0 else items[i-1]
def record(uc,label):
 global calls
 trace.append(label);calls+=1
 if calls==h[4]:fail(uc);return False
 return True
def set_target(i,item):
 if not item or slots[i][1]:return
 previous=target_positions[item-1]
 if previous>=0:slots[previous][1]=0
 target_positions[item-1]=i;slots[i][1]=item
def clear_target(i):
 item=slots[i][1]
 if item:target_positions[item-1]=-1
 slots[i][1]=0
def neighbor(i,d):
 x,y=i%3,i//3;x+=[-1,0,1,1,1,0,-1,-1][d];y+=[1,1,1,0,-1,-1,-1,0][d]
 return y*3+x if 0<=x<3 and 0<=y<3 else -1
def hook_manager(uc,address,size,unused):
 global held,opcount
 x0,x1,x2=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(3)]
 if address==0x1e18dac:
  if record(uc,'portal'):returned(uc,h[0])
 elif address==0x320da64:
  if record(uc,'strategy,'+str(x1)):returned(uc,strategies[x1])
 elif address==0x320819c:returned(uc,board)
 elif address==0x320a4e4:returned(uc,h[1])
 elif address==0x3207954:
  x,y=signed(x1&0xffffffff),signed(x1>>32)
  if record(uc,f'cell,{x},{y}'):returned(uc,cp(y*3+x)if 0<=x<3 and 0<=y<3 else 0)
 elif address==0x3196c8c:returned(uc,cd[ci(x0)][1]==h[2])
 elif address in(0x3448800,0x34487e4,0x319c4a8):returned(uc,slots[ci(x0-0x100)if address!=0x319c4a8 else ci(x0)][1]!=0 if address==0x3448800 else ip(slots[ci(x0-0x100)if address!=0x319c4a8 else ci(x0)][1]))
 elif address==0x319c48c:returned(uc,ip(slots[ci(x0)][0]))
 elif address==0x34485dc:clear_target(ci(x0-0x100));returned(uc)
 elif address==0x3210e48:
  i=ci(x1);put(x2,0,8)
  if record(uc,'plan,'+str(i)):
   put(x1+0x30,cd[i][8],1);set_target(i,cd[i][5]);put(x2,cells[7]if cd[i][6]else 0,8);returned(uc)
 elif address==0x3211494:
  i=ci(x1)
  if record(uc,'cross,'+str(i)):set_target(i,cd[i][7]);returned(uc)
 elif address==0x32112d0:
  target=ci(x2)
  if 1 in disabled:returned(uc);return
  if held:
   if target_positions[held-1]<0:set_target(target,held)
   returned(uc);return
  if slots[target][1]:returned(uc);return
  if record(uc,'create,1,3,7'):
   held=9;set_target(target,9)
   if iv[8][1]:fail(uc);return
   if record(uc,'filling,9,7'):returned(uc)
 elif address==0x1edc738:assert x1==0;opcount=0;returned(uc)
 elif address==0x1ee1c9c:assert x1==0;opcount+=1;returned(uc)
 elif address==0xfcd934:
  assert x0 in [p+0x200 for p in items] and x2 in(0,5)
  put(entry,0x20000100 if x2==5 else 0x20000110,8);returned(uc,entry)
 elif address==0x20000100:
  i=(x0-items[0]-0x200)//0x400
  if record(uc,'fall,'+str(i+1)):
   updates[i]+=1
   if h[7]==1 and target_positions[i]>=0:clear_target(target_positions[i])
   if h[7]==2:steady[i]^=1
   if h[7]==3:opcount+=1
   returned(uc)
 elif address==0x20000110:
  i=(x0-items[0]-0x200)//0x400
  if record(uc,'steady,'+str(i+1)):returned(uc,steady[i])
 elif address==0x3210d84:
  col=signed(x1&0xffffffff)
  if record(uc,'queue,'+str(col)):returned(uc,bool(h[5]&(1<<(col+1))))
 elif address in(0x220d588,0x220cc0c,0x220c9c0):
  d=pending if x0==pending_ptr else disabled;col=signed(x1&0xffffffff)
  if address==0x220d588:d[col]=1
  elif address==0x220cc0c:d.pop(col,None)
  else:d.clear()
  put(x0+0x20,len(d));returned(uc)
 elif address==0x16d0fa4:returned(uc)
 elif address==0x314c094:
  array=allocate(0x100);put(array+0x18,x1);put(x0+0x10,array,8);returned(uc)
 elif address==0x220cee0:
  out=uc.reg_read(UC_ARM64_REG_X8);put(out,x0,8);put(out+8,0);put(out+0x10,0);returned(uc)
 elif address==0x27f55d0:
  values=list(pending);i=read(x0+8)
  if i<len(values):put(x0+0x10,values[i]);put(x0+8,i+1)
  returned(uc,i<len(values))
 elif address==0x27f55cc:returned(uc)
 elif address==0x314c594:returned(uc,read(read(x0+0x10,8)+0x20+x1*4))
 else:base_hook(uc,address,size,unused)
emu.hook_add(UC_HOOK_CODE,hook_manager)

def initialize(head,cell_data,item_data):
 global h,cd,iv,slots,target_positions,steady,updates,pending,disabled,held,opcount,next_object
 h=head[:];cd=[a[:]for a in cell_data];iv=[a[:]for a in item_data];slots=[[0,0]for _ in cells];target_positions=[-1]*9;steady=[a[0]for a in iv];updates=[0]*9;pending={};disabled={};held=0;opcount=0;next_object=0x10070000
 emu.mem_write(root,bytes(0x4000));emu.mem_write(cells[0],bytes(0x2400));emu.mem_write(items[0],bytes(0x2400))
 put(root+0x18,pending_ptr,8);put(root+0x20,disabled_ptr,8);put(root+0x28,0 if h[6]else state,8);put(root+0x30,grid,8);put(root+0x40,entry+0x100,8);put(root+0x48,entry+0x200,8);put(root+0x98,h[3],1);put(grid+0xf0,board_manager,8);put(board+0x10,h[1]<<32,8)
 for t,obj in strategies.items():
  klass=obj+0x400;put(obj,klass,8);put(obj+0x10,grid,8)
  first,nxt={0:(0x319bc54,0x319bc8c),1:(0x319b9b8,0x319b9f0),4:(0x319c360,0x319c3a8),5:(0x319c27c,0x319c2c4)}[t]
  put(klass+0x178,first,8);put(klass+0x188,nxt,8)
 for i,p in enumerate(cells):
  put(p+0x10,p+0x100,8);put(p+0x18,i%3);put(p+0x1c,i//3);put(p+0x20,p+0x200,8);put(p+0x200+0x18,p+0x280,8);put(p+0x280+0x18,8);put(p+0x58,grid,8);put(p+0x68,cd[i][1]);put(p+0xa0,cd[i][0]);put(p+0x30,cd[i][4],1)
  for d in range(8):put(p+0x280+0x20+d*8,cp(neighbor(i,d)),8)
  item=cd[i][2]
  if item:
   for slot in slots:
    if slot[0]==item:slot[0]=0
   slots[i][0]=item
  set_target(i,cd[i][3])
 for i,p in enumerate(items):
  put(p+0x40,0 if iv[i][1]else p+0x200,8);put(p+0x200,p+0x300,8)

def state_text():
 result=[','.join(map(str,[read(root+0x10,1),opcount,sum(1<<(k+1)for k in pending),sum(1<<(k+1)for k in disabled),held]))]
 result += [','.join(map(str,[*slots[i],read(p+0x30,1)]))for i,p in enumerate(cells)]
 result += [','.join(map(str,[target_positions[i],updates[i],steady[i]]))for i in range(9)]
 return '/'.join(result)
cases=[];expected=[];commands=0

def run(head,cell_data,item_data,steps):
 global calls,commands,fault
 initialize(head,cell_data,item_data);output=[]
 for cmd in steps:
  calls=0;trace.clear();fault=False;v=cmd.split(',');mode=v[0];n=int(v[1])if len(v)>1 else 0;result='-';commands+=1
  if mode in('i','m','t','s','x','u','c','e','d','a'):call({'i':0x3210494,'m':0x32106f0,'t':0x32109f0,'s':0x3210a88,'x':0x3210b24,'u':0x3210bb4,'c':0x32102d4,'e':0x3210e3c,'d':0x3210618,'a':0x3210670}[mode],root,n)
  elif mode=='h':h[n]=int(v[2]);put(board+0x10,h[1]<<32,8)
  if fault:result='error'
  output.append(result+':'+state_text()+':'+ ';'.join(trace))
 cases.append(','.join(map(str,head))+'#'+'/'.join(','.join(map(str,a))for a in cell_data)+'#'+'/'.join(','.join(map(str,a))for a in item_data)+'#'+';'.join(steps));expected.append('|'.join(output))
def world():return [0,0,0,1,0,15,0,0],[[1,0,0,0,0,0,0,0,1]for _ in range(9)],[[1,0]for _ in range(9)]
for flags in range(128):
 head,cd,iv=world();head[0]=flags&1;head[1]=(flags>>1)&1;head[2]=(flags>>1)&1;head[6]=(flags>>2)&1;head[7]=(flags>>3)&3
 for i,a in enumerate(cd):a[1]=i//3;a[2]=i+1 if flags&32 else 0;a[3]=i+1;a[5]=i+1 if flags&64 else 0;a[7]=9-i
 iv[0][0]=0;iv[4][1]=int(bool(flags&64))
 run(head,cd,iv,['i','u','m','a,1','a,-1','m','e,0','m','e,1','c,0','m'])
# Native creation is a supplied boundary; real runtime placement is exercised.
for portal in range(2):
 for disabled_gate in range(2):
  head,cd,iv=world();head[0]=portal
  for i in(0,1,3):cd[i][6]=1
  steps=['i']+(['d,1']if disabled_gate else[])+['s','u','m','c,1','a,1','m'];run(head,cd,iv,steps)
for failure in range(1,38):
 head,cd,iv=world();head[4]=failure;head[7]=failure%4
 for i in range(9):cd[i][2]=i+1;cd[i][3]=i+1;cd[i][5]=i+1;iv[i][0]=i%2
 run(head,cd,iv,['i','u','m','t','s','x','a,0','a,1','m','c,0','m'])
rng=random.Random(0x32106f0)
for seed in range(100):
 head,cd,iv=world();head[0]=rng.randrange(2);head[1]=rng.randrange(3);head[2]=rng.randrange(3);head[7]=rng.randrange(4)
 for a in cd:a[:]=[rng.randrange(3),rng.randrange(3),rng.randrange(10),rng.randrange(10),rng.randrange(2),rng.randrange(10),int(rng.random()<.12),rng.randrange(10),rng.randrange(2)]
 for a in iv:a[:]=[rng.randrange(2),int(rng.random()<.1)]
 run(head,cd,iv,['i','t','s','x','u','a,0','a,1','a,2','m','h,5,0','m','d,1','e,0','m','e,1','c,1','h,5,15','m'])
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-fall-manager-')as directory:
 exe=Path(directory)/'verify.exe';files=('OriginalCellMediation','OriginalGridIterator','OriginalCellFallState','OriginalFillingCell','OriginalFallRefillColumns','OriginalFallTargetPlanner','OriginalFillingItemPlacement','OriginalFallManager','OriginalFallGridStrategies','OriginalGameOperations')
 p=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-langversion:latest','-out:'+str(exe),str(PROJECT/'SourceCatalog/FallManagerHarness.cs'),*[str(PROJECT/'Assets/Foundation/Runtime'/(n+'.cs'))for n in files]],capture_output=True,text=True);assert p.returncode==0,p.stdout+p.stderr
 p=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert p.returncode==0,p.stderr[-2000:]
 actual=p.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
 for i,(a,e)in enumerate(zip(actual,expected)):
  if a!=e:
   aa=a.split('|');ee=e.split('|');j=next(j for j,(x,y)in enumerate(zip(aa,ee))if x!=y);raise AssertionError((i,j,cases[i],aa[j],ee[j]))
report={'passed':True,'native_sequences':len(cases),'native_commands':commands,'scope':'Actual ARM64 FallManager InitializeGrid, four full update phases, ManualUpdate and pending tail, Enable and Clear; actual GridIterator ctor/reset/move and native portal/current-board and full-grid vertical/horizontal strategies. Compares ordered grid lookup, target/cross/creation/motion/steady and queue callbacks, target mediation/reverse ownership, steady aggregate, operation-zero count, partial mutation/failure state and enabled/clear behavior. Target decisions, item creation/placement and motion callbacks, operation accounting and CLR/collection/type infrastructure are supplied native boundaries. Compiled runtime uses actual mediation, grid strategies, placement, operation accounting, cached column helper and manager; actual target planner has separate differential coverage. Full per-item physics and formal application composition remain incomplete. Reuses pending completion storage as required for GC control; native allocates a list per pending update.'}
(PROJECT/'fall_manager_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
