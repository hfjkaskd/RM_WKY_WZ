"""Compare actual ItemCreator ARM64 generation orchestration with restored C#."""
import sys
sys.dont_write_bytecode=True
import json,os,random,subprocess,tempfile
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_move_budget.py').read_text().split('cases=[];expected=[]')[0],str(PROJECT/'SourceCatalog/verify_move_budget.py'),'exec'))
emu.hook_del(native_hook);base_hook=hook
owner,cell,rng_ptr,goal,all_list,priority_list,column_list,ratio_dict,count_dict,columns,enum_array,boxed,box_class=[0x10040000+i*0x400 for i in range(13)]
set_ptrs=[0x10050000+i*0x800 for i in range(4)]
sets=[];head=[];calls=0;dicts={}
def s(v):return signed(v&0xffffffff)
def record(t):
 global calls
 trace.append(t);calls+=1
 if calls==head[4]:fail(emu);return False
 return True
def lst(p):return [read(read(p+0x10,8)+0x20+i*8,8)for i in range(read(p+0x18))]
def make_list(p,values):
 storage=p+0x100;put(p+0x10,storage,8);put(storage+0x18,24);put(p+0x18,len(values))
 for i,v in enumerate(values):put(storage+0x20+8*i,v,8)
def ident(p):return set_ptrs.index(p)
def available(i):return read(set_ptrs[i]+0x50,1)!=0
def mask(i):return sum((read(set_ptrs[i]+0x300+j*0x30+0x18,1)!=0)<<j for j in range(len(sets[i])-11))
def hook(uc,address,size,unused):
 x0,x1,x2,x3=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(4)]
 if address in(0x190f314,0x190f9cc,0x19107d4,0x190fd14,0x1910d98,0x190fe8c,0x1910528,0x190fd64,0x190fe1c,0x190fa8c):
  i=ident(x0);v=sets[i];result=0
  name={0x190f314:'init',0x190f9cc:'calculate',0x19107d4:'priority',0x190fd14:'anycolumn',0x1910d98:'ratio',0x190fe8c:'canpriority',0x1910528:'item',0x190fd64:'hascolumn',0x190fe1c:'match',0x190fa8c:'update'}[address]
  event=name+','+str(i)+((','+str(s(x1)))if name=='ratio'else(','+str(s(x2)))if name=='hascolumn'else'')
  if not record(event):return
  if name=='init':
   put(x0+0x50,v[1],1);put(x0+0x54,0);put(x0+0x88,0)
   for j in range(len(v)-11):put(x0+0x300+j*0x30+0x18,v[1],1)
  elif name=='calculate':put(x0+0x54,mask(i).bit_count())
  elif name=='priority':result=v[2]
  elif name=='anycolumn':result=v[6]>=0
  elif name=='ratio':result=v[6+s(x1)]if 1<=s(x1)<=4 else 0
  elif name=='canpriority':result=v[3]!=0 and available(i)
  elif name=='item':result=v[5]if mask(i)else 0
  elif name=='hascolumn':result=v[6]==s(x2)
  elif name=='match':result=v[4]
  elif name=='update'and not mask(i):put(x0+0x50,0,1)
  returned(uc,result)
 elif address==0x1ed98c4:
  a,b=s(x1),s(x2)
  if not record(f'random,{a},{b}'):return
  if b<a:fail(uc)
  else:returned(uc,max(a,b-1)if head[3]else a)
 elif address in(0x1c93628,0x1c934b4):
  f,c=read(x1+0xa4),s(x2);kind='columntotal'if address==0x1c93628 else'columnsets'
  if not record(f'{kind},{f},{c}'):return
  picked=[i for i,v in enumerate(sets)if v[6]==c]
  if address==0x1c93628:returned(uc,sum(sets[i][6+f]if 1<=f<=4 else 0 for i in picked if available(i)and not sets[i][2]))
  else:p=allocate(0x200);make_list(p,[set_ptrs[i]for i in picked]);returned(uc,p)
 elif address==0x23f0540:
  if record('tune,'+str(ident(x1))):returned(uc,777)
 elif address in(0x32145d8,0x32142a0):
  name='limit'if address==0x32145d8 else'goal'
  if record(name+','+str(s(x1))):returned(uc,head[5]if name=='limit'else head[6])
 elif address==0x2b452a0:returned(uc,s(x0)+1000)
 elif address==0x190f514:returned(uc,x0)
 elif address==0x2a2e598:
  value=dicts[ratio_dict].get(read(cell+0xa4),0)
  if record('fallback,'+str(value)):returned(uc)
 elif address==0x261458c:returned(uc,box_class)
 elif address==0x23011e4:returned(uc,enum_array)
 elif address==0x261e750:returned(uc,5)
 elif address==0x261e7b0:put(boxed,box_class,8);put(boxed+0x10,x1);returned(uc,boxed)
 elif address==0xffda70:returned(uc,x0+0x10)
 elif address==0xffda78:returned(uc,allocate(0x200))
 elif address==0x2c1b430:make_list(x0,[]);returned(uc)
 elif address==0x2c1b9a0:
  values=lst(x0);i=s(x1)
  if not 0<=i<len(values):fail(uc)
  else:returned(uc,values[i])
 elif address==0x2627eb8:emu.mem_write(x0+0x20+x1*8,bytes(x2*8));returned(uc)
 elif address==0x3c96364:dicts[x0].clear();returned(uc)
 elif address==0x3c963d0:returned(uc,s(x1)in dicts[x0])
 elif address==0x3c96128:
  if s(x1)not in dicts[x0]:fail(uc)
  else:returned(uc,dicts[x0][s(x1)])
 elif address in(0x3c961c0,0x3c961d8):dicts[x0][s(x1)]=s(x2);returned(uc)
 elif address==0x3c97c34:put(x2,dicts[x0].get(s(x1),0));returned(uc,s(x1)in dicts[x0])
 else:base_hook(uc,address,size,unused)
emu.hook_add(UC_HOOK_CODE,hook)
cases=[];expected=[]
def run(h,sv,commands):
 global head,sets,calls,next_object
 head=h;sets=sv;calls=0;next_object=0x10060000;dicts.clear();dicts[ratio_dict]={};dicts[count_dict]={};emu.mem_write(owner,bytes(0x200));put(owner+0x10,priority_list,8);put(owner+0x18,column_list,8);put(owner+0x20,all_list,8);put(owner+0xf0,rng_ptr,8);put(owner+0x108,columns,8);put(owner+0x110,goal,8);put(owner+0x118,count_dict,8);put(owner+0x120,ratio_dict,8);put(owner+0x130,h[0]&1,1);put(cell+0xa4,h[1]);put(enum_array+0x18,5)
 put(all_list+0x18,len(sv))
 for i,p in enumerate(set_ptrs[:len(sv)]):put(all_list+0x20+8*i,p,8)
 make_list(priority_list,[]);make_list(column_list,[])
 for i,v in enumerate(sv):
  p=set_ptrs[i];emu.mem_write(p,bytes(0x800));put(p+0x3c,v[0]);make_list(p+0x100,[p+0x300+j*0x30 for j in range(len(v)-11)]);put(p+0x18,p+0x100,8)
  for j,t in enumerate(v[11:]):put(p+0x300+j*0x30+0x10,t)
 output=[]
 for step in commands:
  trace.clear();mode,*a=step;result='ok'
  if mode=='add':call(0x23eb73c,owner,all_list)
  elif mode in('reset','calc'):call(0x23eb758 if mode=='reset'else 0x23eb28c,owner)
  elif mode in('fill','target','color'):result='value,'+str(s(call({'fill':0x23efcec,'target':0x23efa00,'color':0x23efec0}[mode],owner,cell,h[2])))
  elif mode=='new':call(0x23ef5b0,owner,a[0])
  elif mode=='rule':result='value,'+str(s(call(0x2b452f8,a[0])))
  state='/'.join(f'{int(available(i))},{s(read(set_ptrs[i]+0x54))},{s(read(set_ptrs[i]+0x88))},{mask(i)}'for i in range(len(sv)))
  ds=lambda d:'/'.join(str(k)+','+str(v)for k,v in dicts[d].items())
  output.append(('error'if fault else result)+':'+state+':'+ds(ratio_dict)+':'+ds(count_dict)+':'+'/'.join(trace))
 cases.append(','.join(map(str,h))+'#'+'/'.join(','.join(map(str,s))for s in sv)+'#'+';'.join(','.join(map(str,s))for s in commands));expected.append('|'.join(output))
rng=random.Random(312)
for i in range(180):
 sv=[]
 for j in range(i%5):sv.append([rng.randint(1,5),rng.randrange(2),rng.randrange(2),rng.randrange(2),rng.randrange(2),rng.choice((0,3,7,13)),rng.choice((-1,0,1)),*[rng.randint(0,4)for _ in range(4)],7,7,13])
 run([i%2,1+i%4,i%2,(i//2)%2,0,i%3,1+i%4],sv,[('add',),('fill',),('target',),('color',),('new',7),('fill',),('new',7),('calc',),('target',),('reset',)])
for failure in range(1,45):run([1,1,0,0,failure,2,3],[[2,1,0,1,1,3,-1,2,1,1,1,7,7,13],[3,1,1,1,0,7,0,3,2,2,2,7,13]],[('add',),('fill',),('target',),('color',),('new',7),('new',7),('fill',)])
for t in range(-2,119):run([0,1,0,0,0,0,1],[],[('add',),('rule',t),('new',t)])
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-generation-')as folder:
 exe=Path(folder)/'verify.exe';p=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-langversion:latest','-out:'+str(exe),str(PROJECT/'SourceCatalog/FillSetGenerationHarness.cs'),str(PROJECT/'Assets/Foundation/Runtime/OriginalFillSetGeneration.cs')],capture_output=True,text=True,encoding='utf-8-sig');assert p.returncode==0,p.stdout+p.stderr
 p=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8-sig');assert p.returncode==0,p.stderr
 actual=[x.replace('\ufeff','')for x in p.stdout.splitlines()];assert len(actual)==len(expected)
 for i,(a,e)in enumerate(zip(actual,expected)):assert a==e,(i,cases[i],a,e)
report={'passed':True,'native_cases':len(cases),'scope':'Actual ARM64 ItemCreator AddAvailableSets/SetupAvailableFillItems, CalculateRandomFactors, availability filter, prioritized and target-column/fill/color selection, OnNewItemCreated limit/goal disabling, plus full IsFillingObstacle classification. Exact output/state, callback and random-bound order, reset and injected failures. LevelSet per-set selection/availability, goal/tuning/column services and CLR storage/random are supplied boundaries. Production random/session/model creation and automatic refill composition remain pending.'}
(PROJECT/'fill_set_generation_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
