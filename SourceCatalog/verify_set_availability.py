"""Execute original LevelSet quota ARM64 with typed grid/CLR boundaries."""
import sys
sys.dont_write_bytecode=True
import json,os,random,subprocess,tempfile
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_move_budget.py').read_text().split('cases=[];expected=[]')[0],str(PROJECT/'SourceCatalog/verify_move_budget.py'),'exec'))
emu.hook_del(native_hook);base_hook=hook
owner,limits,board_counts,grid,rng_ptr,item_class,fill_class,normal_class,ancestry=[0x10040000+i*0x400 for i in range(9)]
cell_ptrs=[0x10050000+i*0x200 for i in range(10)];item_ptrs=[0x10054000+i*0x100 for i in range(10)]
put(item_class+0x378,0x2b45d70,8);put(fill_class+0x12c,1,1);put(fill_class+0xc8,ancestry,8);put(ancestry,fill_class,8);put(normal_class+0x12c,0,1)
frame=flying=edge=failure=calls=iterator_index=0;cell_data=[];item_data=[];d={};hashes={};enumerators={}
def s(v):return signed(v&0xffffffff)
def record(t):
 global calls
 trace.append(t);calls+=1
 if failure==calls:fail(emu);return False
 return True
def hook(uc,address,size,unused):
 global iterator_index
 x0,x1,x2,x3=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(4)]
 if address==0xffd960:
  if x0==read(0x53e7838,8):put(x0,fill_class,8);returned(uc)
  else:base_hook(uc,address,size,unused)
 elif address==0x3205d7c:
  if record('frame'):returned(uc,frame)
 elif address==0x260dea4:assert x0==2;returned(uc,grid)
 elif address==0x320aeb8:
  assert x1==1
  if record('iterator'):
   p=uc.reg_read(UC_ARM64_REG_X8);emu.mem_write(p,bytes(24));put(p+16,cell_ptrs[0]if cell_data else 0,8);returned(uc)
 elif address==0x318ec2c:
  if record('reset'):iterator_index=-1;put(x0+16,cell_ptrs[0]if cell_data else 0,8);returned(uc)
 elif address==0x318ecc8:
  if record('next'):
   iterator_index+=1;p=cell_ptrs[iterator_index]if iterator_index<len(cell_data)else 0;put(x0+16,p,8);returned(uc,p)
 elif address==0x3195f9c:returned(uc,cell_data[cell_ptrs.index(x0)][1])
 elif address in(0x3448800,0x343bd38,0x34487e4,0x343bd5c,0x319c4a8,0x319017c):
  mediator=address in(0x3448800,0x343bd38,0x34487e4,0x343bd5c);i=cell_ptrs.index(x0-0x100 if mediator else x0);v=cell_data[i][3 if address in(0x3448800,0x34487e4,0x319c4a8)else 2]
  returned(uc,int(v>=0)if address in(0x3448800,0x343bd38)else item_ptrs[v]if v>=0 else 0)
 elif address==0x343cfd0:returned(uc,cell_data[cell_ptrs.index(x0)][4]>=0)
 elif address==0x2b45b90:returned(uc,item_data[item_ptrs.index(x0-0x80)][2])
 elif address==0x2b45d70:returned(uc,item_data[item_ptrs.index(x0)][1])
 elif address==0x2b452a0:returned(uc,s(x0)+1000)
 elif address==0x320feac:
  if record('flying,'+str(s(x1))):returned(uc,flying)
 elif address==0x1ed98c4:
  a,b=s(x1),s(x2)
  if record(f'random,{a},{b}'):
   if b<a:fail(uc)
   else:returned(uc,max(a,b-1)if edge else a)
 elif address==0x3c96128:
  if s(x1)in d[limits]:returned(uc,d[limits][s(x1)])
  else:fail(uc)
 elif address==0x3c9c9ec:d[x0].clear();returned(uc)
 elif address==0x3c9ca58:returned(uc,s(x1)in d[x0])
 elif address==0x3c9c7a4:returned(uc,d[x0][s(x1)])
 elif address==0x3c9c858:d[x0][s(x1)]=x2;returned(uc)
 elif address==0x220c2b0:hashes[x0]=set();put(x0+0x20,0);returned(uc)
 elif address==0x220d588:
  old=len(hashes[x0]);hashes[x0].add(s(x1));put(x0+0x20,len(hashes[x0]));returned(uc,len(hashes[x0])!=old)
 elif address==0x3c965cc:enumerators[uc.reg_read(UC_ARM64_REG_X8)]=iter(d[x0]);returned(uc)
 elif address==0x347b0c0:
  key=next(enumerators[x0],None)
  if key is not None:put(x0+16,key)
  returned(uc,key is not None)
 elif address==0x347b1b8:returned(uc)
 else:base_hook(uc,address,size,unused)
emu.hook_add(UC_HOOK_CODE,hook)
cases=[];expected=[]
def run(h,items,cells,commands):
 global frame,flying,edge,failure,calls,cell_data,item_data,next_object
 frame,flying,edge,failure,t,n,a=h;calls=0;cell_data=cells;item_data=items;next_object=0x10060000;d.clear();hashes.clear();d[limits]={t:n};d[board_counts]={}
 emu.mem_write(owner,bytes(0x200));put(owner+0x40,limits,8);put(owner+0x48,board_counts,8);put(owner+0x50,a,1);put(owner+0x60,rng_ptr,8);put(owner+0x68,grid,8)
 for i,v in enumerate(items):
  p=item_ptrs[i];emu.mem_write(p,bytes(0x100));put(p,item_class,8);put(p+0x10,v[0]);put(p+0x38,p+0x80,8)
 for i,v in enumerate(cells):
  p=cell_ptrs[i];emu.mem_write(p,bytes(0x200));put(p,fill_class if v[0]else normal_class,8);put(p+0x10,p+0x100,8);put(p+0xe0,item_ptrs[v[4]]if v[4]>=0 else 0,8)
 output=[]
 for cmd in commands:
  trace.clear();mode,*args=cmd;result='ok'
  if mode in('m','p'):
   value=call(0x1910000 if mode=='m'else 0x190fe8c,owner,*(args if args else []));result='error'if fault else 'value,'+str(value&1)
  elif mode=='f':frame=args[0]
  elif mode=='y':flying=args[0]
  elif mode=='consume':put(owner+0x88,read(owner+0x88)-1)
  state=','.join(str(s(read(owner+o)))for o in(0x5c,0x58,0x88));output.append(result+':'+state+':'+'/'.join(trace))
 cases.append(','.join(map(str,h))+'#'+'/'.join(','.join(map(str,v))for v in items)+'#'+'/'.join(','.join(map(str,v))for v in cells)+'#'+';'.join(','.join(map(str,v))for v in commands));expected.append('|'.join(output))
rng=random.Random(891)
for i in range(360):
 t=rng.choice((6,7,13));items=[[rng.randrange(5),rng.choice((6,7,13)),rng.randrange(2)]for _ in range(7)]
 cells=[[rng.randrange(2),rng.randrange(2),rng.randrange(-1,7),rng.randrange(-1,7),rng.randrange(-1,7)]for _ in range(i%9)]
 run([i%3,rng.randrange(-2,5),i%2,0,t,rng.choice((-2147483648,-1,0,1,3,6,2147483647)),i%2],items,cells,[('p',),('m',t),('consume',),('m',t),('y',99),('m',t),('f',3),('m',t),('m',t),('f',4),('m',t+1),('m',t)])
for f in range(1,65):run([1,0,0,f,7,5,1],[[1,7,0],[1,7,0],[2,13,1]],[[1,1,0,1,0],[1,1,-1,-1,1],[0,0,2,-1,-1]],[('p',),('m',7),('f',2),('m',7),('m',7)])
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-availability-')as folder:
 exe=Path(folder)/'verify.exe';p=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-langversion:latest','-out:'+str(exe),str(PROJECT/'SourceCatalog/LevelSetAvailabilityHarness.cs'),str(PROJECT/'Assets/Foundation/Runtime/OriginalLevelSetAvailability.cs')],capture_output=True,text=True,encoding='utf-8-sig');assert p.returncode==0,p.stdout+p.stderr
 p=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8-sig');assert p.returncode==0,p.stderr
 actual=[x.replace('\ufeff','')for x in p.stdout.splitlines()];assert len(actual)==len(expected)
 for i,(a,e)in enumerate(zip(actual,expected)):assert a==e,(i,cases[i],a,e)
report={'passed':True,'native_cases':len(cases),'scope':'Actual ARM64 CanGetPrioritizedItem/CanFillNewItemWithMaxCount/CalculateRatio/AddToItemInBoards, exact results, frame/empty/remaining state, callback/random bound order, duplicate item IDs, target/current and untargeted filling-created items, frame-zero cache, cross-type same-frame cache, exhausted limits, overflow, absent keys and injected service failures. Grid traversal/cell mediation/item-type virtual getters, goal conversion and CLR collection/random storage are supplied boundaries. Application/session ownership and automatic refill remain pending.'}
(PROJECT/'set_availability_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
