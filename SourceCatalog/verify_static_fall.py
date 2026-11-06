"""Execute original ARM64 static/fall queries and ItemView callbacks against runtime C#."""
import sys
sys.dont_write_bytecode=True
import json,os,random,subprocess,tempfile
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_move_budget.py').read_text().split('cases=[];expected=[]')[0],str(PROJECT/'SourceCatalog/verify_move_budget.py'),'exec'))
emu.hook_del(native_hook);base_hook=hook
cells=[0x10020000+i*0x1000 for i in range(3)]
meds=[c+0x100 for c in cells];slots=[c+0x200 for c in cells]
lists=[[c+0x400,c+0x500]for c in cells]
models=[0x10024000+i*0x100 for i in range(20)]
item,mediator,view,delegate,manager=0x10028000,0x10028100,0x10028200,0x10028300,0x10028400
static_class,roof_class,item_class,view_class,delegate_class=0x10030000,0x10031000,0x10032000,0x10033000,0x10034000
pair=0x10035000
stub={n:0x20000100+i*4 for i,n in enumerate(('touch','block','remaining','fallable','getview','current','atlas','bycell','reverse','update'))}
for cls,entries in ((static_class,{0x408:'touch',0x428:'block',0x4e8:'remaining'}),(roof_class,{0x408:'touch',0x428:'block',0x4e8:'remaining'}),(item_class,{0x438:'fallable',0x418:'getview'}),(view_class,{0x1b8:'atlas',0x2f8:'bycell',0x2e8:'reverse',0x208:'update'})):
 for offset,name in entries.items():put(cls+offset,stub[name],8)
put(pair,stub['current'],8)
sorting_class,sorting_static=0x10036000,0x10036800
put(read(0x5356db8,8),sorting_class,8);put(sorting_class+0xe0,1);put(sorting_class+0xb8,sorting_static,8)
# Supply IL2CPP metadata identity for the checked RoofModel cast.
roof_slot=read(0x5381560,8);put(roof_slot,roof_class,8)
put(roof_class+0x12c,1,1);put(roof_class+0xc8,roof_class+0x800,8);put(roof_class+0x800,roof_class,8)
put(static_class+0x12c,1,1);put(static_class+0xc8,static_class+0x800,8);put(static_class+0x800,static_class,8)
h=[];calls=0;current_calls=0;notifies=0;last=(0,0);current_cell=-1;next_cell=-1

def record(value):
 global calls
 trace.append(value);calls+=1
 if calls==h[11]:fail(emu);return False
 return True

def pointer(index):return cells[index]if index>=0 else 0
def model_id(p):return models.index(p)+1 if p else 0
def list_values(p):return [read(read(p+0x10,8)+0x20+i*8,8)for i in range(read(p+0x18))]

def hook(uc,address,size,unused):
 global current_calls,notifies,last
 x0,x1,x2,x3=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(4)]
 if address in stub.values():
  name=next(k for k,v in stub.items()if v==address)
  if name in('touch','block','remaining'):
   ident=model_id(x0)
   if not record(name+','+str(ident)):return
   result=(bool(h[14]&(1<<(ident-1)))if name=='touch'else bool(h[15]&(1<<(ident-1)))if name=='block'else h[12])
  elif name=='fallable':
   if not record('fallable'):return
   result=h[0]
  elif name=='getview':result=view if h[2]else 0
  elif name=='current':
   if not record('current'):return
   result=pointer(h[5]if current_calls==0 else h[6]);current_calls+=1
   if h[7]:put(view+0x60,0,8)
  elif name=='update':
   layer,order=signed(x1&0xffffffff),signed(x1>>32)
   if not record('update,'+str(layer)+','+str(order)):return
   last=(layer,order);result=0
  else:
   if not record(name):return
   result=-1 if name=='atlas'else 0
  returned(uc,result)
 elif address==0x2b45a6c:returned(uc,current_cell>=0)
 elif address==0x2b45b80:returned(uc,next_cell>=0)
 elif address==0x2b45df4:returned(uc,pointer(current_cell))
 elif address==0x2b45968:returned(uc,pointer(current_cell))
 elif address==0x2b45a7c:returned(uc,pointer(next_cell))
 elif address==0x2c1b9a0:
  values=list_values(x0);index=signed(x1&0xffffffff)
  if not 0<=index<len(values):fail(uc)
  else:returned(uc,values[index])
 elif address==0x2c1d0e4:
  values=list_values(x0)
  found=x1 in values
  if found:
   values.remove(x1);a=read(x0+0x10,8);put(x0+0x18,len(values));put(x0+0x1c,read(x0+0x1c)+1)
   for i,p in enumerate(values+[0]):put(a+0x20+i*8,p,8)
  returned(uc,found)
 elif address==0x319d0d4:
  if record('extra'):returned(uc,h[13])
 elif address==0xfcd934:assert x2==3;returned(uc,pair)
 elif address==0x31932c8:assert x1==5;returned(uc,cells[1]if x0==cells[0]+0xe0 else 0)
 elif address==0x2c01a5c:put(x0,1,1);put(x0+1,x1,1);returned(uc)
 elif address==0x2c0363c:put(x0,1,1);put(x0+4,x1,8);returned(uc)
 elif address==0x343b6e8:put(x0,x1);put(x0+4,x2);returned(uc)
 elif address==0x1611b24:
  if record('notify,'+str(model_id(x0))):notifies+=1;returned(uc)
 elif address==0x12dd208:
  x,y=signed(x3&0xffffffff),signed(x3>>32)
  if record('sorting,'+str(signed(x1&0xffffffff))+','+str(x)+','+str(y)):
   returned(uc,77+((x*100+y)&0xffffffff)*0x100000000);uc.reg_write(UC_ARM64_REG_X1,0)
 elif address==0xffde20:fail(uc)
 else:base_hook(uc,address,size,unused)
emu.hook_add(UC_HOOK_CODE,hook)
cases=[];expected=[];commands=0

def dump():
 texts=[]
 for c,m,a,ls in zip(cells,meds,slots,lists):
  texts.append(','.join(str(model_id(read(a+0x20+i*8,8)))for i in range(20))+'/'+','.join(str(model_id(p))for p in list_values(ls[0]))+'/'+','.join(str(model_id(p))for p in list_values(ls[1])))
 owners=[cells.index(read(p+0x28,8))if read(p+0x28,8)else -1 for p in models]
 return '~'.join(texts)+'/'+','.join(map(str,owners))+'/'+','.join(map(str,(read(view+0x68,1),notifies,*last)))

def run(head,steps):
 global h,calls,current_calls,notifies,last,current_cell,next_cell,commands,fault
 h=head.copy();notifies=0;last=(0,0);current_cell=h[3];next_cell=h[4]
 for c,m,a,ls,xy in zip(cells,meds,slots,lists,((2,3),(2,2),(4,5))):
  emu.mem_write(c,bytes(0x1000));put(c+0x18,xy[0]);put(c+0x1c,xy[1]);put(c+0x20,c+0xe0,8);put(c+0xc0,m,8)
  put(m+0x10,a,8);put(m+0x18,c,8);put(a,static_class,8);put(a+0x18,20)
  for j,l in enumerate(ls):
   arr=c+0x600+j*0x300;put(l+0x10,arr,8);put(arr+0x18,64);put(m+0x30+j*8,l,8)
 for i,p in enumerate(models):
  emu.mem_write(p,bytes(0x100));put(p,roof_class if i==7 and not h[10]else static_class,8);put(p+0x18,i)
 emu.mem_write(item,bytes(0x500));put(item,item_class,8);put(item+0x38,mediator,8);put(view,view_class,8);put(delegate,delegate_class,8)
 put(view+0x50,manager if h[8]else 0,8);put(view+0x60,delegate if h[9]else 0,8);put(view+0x19,h[1],1)
 out=[]
 for step in steps:
  commands+=1;trace.clear();calls=0;current_calls=0;mode,*args=step;k=args[0]if args else 0;result='-';fault=False
  if mode in('a','c','g','h'):
   arg=models[args[1]]if mode=='a'else args[1]
   value=call({'a':0x34410b4,'c':0x34411f4,'g':0x3440a80,'h':0x3440fa4}[mode],meds[k],arg)
   if mode=='g'and not fault:result=str(model_id(value))
   elif mode=='h':result=str(value)
  elif mode in('b','f','n'):result=str(call({'b':0x3440f00,'f':0x3440ff4,'n':0x3440fbc}[mode],meds[k]))
  elif mode in('i','u','r','s'):result=str(call({'i':0x2b462f8,'u':0x2b4744c,'r':0x2b474d8,'s':0x2b470cc}[mode],item))
  elif mode in('k','z'):
   put(sorting_static+8,k);call(0x12d81cc,item if mode=='k'else 0)
   packed=emu.reg_read(UC_ARM64_REG_X0);result=','.join(map(str,(signed(packed&0xffffffff),signed(packed>>32),emu.reg_read(UC_ARM64_REG_X1)&0xffffffff)))
  elif mode=='q':result=str(call(0x319d024,cells[k],*args[1:]))
  elif mode=='j':put(cells[k]+0x38,args[1]);put(cells[k]+0x3c,args[2])
  elif mode=='e':result=str(call(0x319e620,cells[k]))
  elif mode=='w':result=str(call(0x2b47564,item))
  elif mode=='v':call(0x2b4b034,view)
  elif mode=='t':call(0x2b4b288,view,k)
  elif mode=='p':current_cell,next_cell=args
  elif mode=='d':put(view+0x60,delegate if k else 0,8)
  elif mode=='x':h[k]=args[1];put(view+0x19,h[1],1)
  if fault:result='error'
  out.append(result+':'+dump()+':'+ ';'.join(trace))
 cases.append(','.join(map(str,head))+'#'+';'.join(','.join(map(str,s))for s in steps));expected.append('|'.join(out))

def default():return [1,0,1,0,-1,0,0,0,1,1,0,0,2,1,0xfffff,0]
for above in(0,0xfffff):
 for block in(0,0xfffff):
  for t in range(20):
   a=default();a[14:]=[above,block]
   run(a,[('a',0,t),('g',0,t),('h',0,t),('b',0),('f',0),('n',0),('q',0,0,0),('q',0,1,1),('i',),('u',),('r',),('s',),('a',1,t),('a',0,t),('c',0,t),('b',0),('c',1,t)])
for fallable in(0,1):
 for swapping in(0,1):
  for hasview in(0,1):
   for current in(-1,0,1):
    a=default();a[:4]=[fallable,swapping,hasview,current]
    run(a,[('i',),('a',0,4),('u',),('r',),('s',),('p',-1,0),('u',),('s',),('a',0,7),('r',),('s',),('p',1,0),('u',),('r',),('i',)])
for count in(-2147483648,-1,0,1,2,2147483647):
 for extra in(-2147483648,-1,0,1,2,2147483647):
  a=default();a[12:14]=[count,extra];run(a,[('a',0,6),('q',0,1,0),('q',0,1,1),('c',0,6),('q',0,1,1)])
for first in(-1,0,1,2):
 for second in(-1,0,1,2):
  for clear in(0,1):
   for wrong in(0,1):
    a=default();a[5:8]=[first,second,clear];a[10]=wrong
    run(a,[('v',),('d',1),('a',0,7),('v',),('d',1),('a',1,7),('v',),('d',1),('c',1,7),('t',1),('d',1),('t',0)])
for failure in range(9):
 for hasmodel in(0,1):
  for managerpresent in(0,1):
   a=default();a[8:10]=[managerpresent,hasmodel];a[11]=failure
   run(a,[('a',0,7),('v',),('t',1),('t',0),('c',0,7),('b',0)])
a=default();run(a,[(m,0,t)for m in('g','h','c')for t in(-1,20,2147483647)])
for honey in(-2147483648,-1,0,1,2147483647):
 for roof in(-2147483648,-1,0,1,2147483647):
  a=default();run(a,[('j',0,honey,roof),('e',0),('w',),('p',-1,0),('w',),('p',1,0),('w',)])
for current in(-1,0,1):
 for next_index in(-1,0,1):
  for failure in range(4):
   for mask in(0,1<<4,1<<7,(1<<4)|(1<<7)):
    a=default();a[3:5]=[current,next_index];a[11]=failure;a[14]=mask
    run(a,[('k',-17),('a',0,4),('k',0),('a',1,7),('k',2147483647),('c',0,4),('k',-2147483648),('z',1)])
rng=random.Random(0x34410b4)
for i in range(60):
 a=default();a[14:]=[rng.randrange(1<<20),rng.randrange(1<<20)];a[11]=rng.randrange(5);steps=[]
 for j in range(40):
  m=rng.choice(('a','c','b','f','n','q','i','u','r','s','v','t','d','p'))
  steps.append((m,rng.randrange(3),rng.randrange(20))if m in('a','c')else(m,rng.randrange(3))if m in('b','f','n')else(m,rng.randrange(3),rng.randrange(2),rng.randrange(2))if m=='q'else(m,rng.randrange(2))if m in('t','d')else(m,rng.randrange(-1,3),rng.randrange(-1,3))if m=='p'else(m,))
 run(a,steps)
cache=Path(tempfile.gettempdir())/'rm_static_fall_cases.json';cache.write_text(json.dumps([cases,expected,commands]))
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-static-fall-')as folder:
 exe=Path(folder)/'verify.exe'
 names=('OriginalCellMediation','OriginalGridIterator','OriginalItemSwapAvailability','OriginalStaticFallQueries','OriginalItemFallAvailability','OriginalItemFallCallbacks','OriginalCellTouchRules','OriginalCrossFallSorting')
 p=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-langversion:latest','-out:'+str(exe),str(PROJECT/'SourceCatalog/StaticFallHarness.cs'),*[str(PROJECT/'Assets/Foundation/Runtime'/(n+'.cs'))for n in names]],capture_output=True,text=True);assert p.returncode==0,p.stdout+p.stderr
 p=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert p.returncode==0,p.stderr
 actual=p.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
 for i,(a,e)in enumerate(zip(actual,expected)):
  aa,ee=a.split('|'),e.split('|');assert len(aa)==len(ee),(i,len(aa),len(ee))
  for j,(x,y)in enumerate(zip(aa,ee)):assert x==y,(i,j,cases[i],x,y)
report={'passed':True,'native_sequences':len(cases),'native_commands':commands,'scope':'Actual ARM64 static registry Add/Clear/Get/Has, fall-blocking/above queries, ItemModel fall availability and current/next honey/roof and spreading queries, Cell.IsFallBlocked/IsAboveStaticSpreading, Sorting.GetCrossSorting through real touch query, common ItemView FallAnimationStarted and under-static sorting setter. Exact state, callback order and injected-failure state. IL2CPP metadata/CLR lists and mediator getters, grid neighbors, roof notification owner, sorting manager and extra incoming exploder service are supplied boundaries. Static constructors/factory/lifecycle, full roof model, sorting-manager branches and player-driven fall composition are not established by this test.'}
(PROJECT/'static_fall_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
