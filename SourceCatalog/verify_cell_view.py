"""CellMask and CellBorder geometry: execute original ARM64 vs runtime C#."""
import sys
sys.dont_write_bytecode=True
import json,os,random,struct,subprocess,tempfile
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_move_budget.py').read_text().split('cases=[];expected=[]')[0],str(PROJECT/'SourceCatalog/verify_move_budget.py'),'exec'))
emu.hook_del(native_hook);base_hook=hook
mask,border,go,normal,water=[0x10040000+i*0x100 for i in range(5)]
renderers=[0x10041000+i*0x100 for i in range(4)]
transforms=[0x10042000+i*0x100 for i in range(4)]
sorting_class,sorting_static,border_class,border_static,view_class,view_static=[0x10043000+i*0x400 for i in range(6)]
for got,cls,static in((0x5356db8,sorting_class,sorting_static),(0x53b9288,border_class,border_static),(0x5395958,view_class,view_static)):
 put(read(got,8),cls,8);put(cls+0xe0,1);put(cls+0xb8,static,8)
put(sorting_static+0x14,487648401);put(sorting_static+4,1795358237)
state=[];present=0;failure=0;calls=0;active=True;lookup_assets=False
def record(name,ident,*values):
 global calls
 trace.append(','.join(map(str,(name,ident,*values))));calls+=1
 if calls==failure:fail(emu);return False
 return True
def fbits(value):return struct.unpack('<I',struct.pack('<f',value))[0]
def fvalue(value):return struct.unpack('<f',struct.pack('<I',value))[0]
def floats(values):
 for i,v in enumerate(values):emu.reg_write(UC_ARM64_REG_S0+i,v)
def hook(uc,address,size,unused):
 global active
 x0,x1,x2=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(3)]
 if address==0x3e473b8:
  if record('game',9):returned(uc,go if present&16 else 0)
 elif address==0x3e49b30:
  if record('active',9,x1):active=bool(x1);returned(uc)
 elif address==0x3e4737c:
  i=renderers.index(x0)
  if record('transform',i):returned(uc,transforms[i]if present&(4 if i==0 else 8 if i==1 else 128)else 0)
 elif address==0x3e4e4a0:
  i=transforms.index(x0);v=[uc.reg_read(UC_ARM64_REG_S0+j)for j in range(3)]
  if record('position',i,*v):state[i][4:7]=v;returned(uc)
 elif address in(0x3442578,0x34425a4,0x344169c,0x3441674):
  if lookup_assets:return
  kind=0 if x0==normal else 1;inner=address in(0x3442578,0x344169c)
  if record('inner'if inner else'outer',kind):returned(uc,(30 if inner else 40)+kind)
 elif address in(0x3e3eb84,0x3e3ebc0,0x3e3ec04,0x3e3ec40,0x3e50d30,0x3e50dc0,0x3e51090,0x3e510cc,0x3e51110,0x3e5114c,0x3e3e804,0x3e50f78,0x3e50c6c):
  if not x0:fail(uc);return
  i=renderers.index(x0);s=state[i]
  simple={0x3e3eb84:('layerget',0,False),0x3e3ebc0:('layer',0,True),0x3e3ec04:('orderget',1,False),0x3e3ec40:('order',1,True),0x3e51090:('flipxget',7,False),0x3e510cc:('flipx',7,True),0x3e51110:('flipyget',8,False),0x3e5114c:('flipy',8,True),0x3e3e804:('enabled',9,True),0x3e50c6c:('sprite',14,True)}
  if address in simple:
   name,k,setter=simple[address];v=signed(x1&0xffffffff)
   if record(name,i,*([v]if setter else[])):
    if setter:s[k]=v;returned(uc)
    else:returned(uc,s[k])
  elif address==0x3e50d30:
   if record('sizeget',i):floats(s[2:4]);returned(uc)
  else:
   n=4 if address==0x3e50f78 else 2;k=10 if n==4 else 2;v=[uc.reg_read(UC_ARM64_REG_S0+j)for j in range(n)]
   if record('color'if n==4 else'size',i,*v):s[k:k+n]=v;returned(uc)
 else:base_hook(uc,address,size,unused)
emu.hook_add(UC_HOOK_CODE,hook)
call(0x3446500);call(0x3447e14)
defaults={'side_size':[read(border_static+i)for i in(0,4)],'outer_reduction':[read(border_static+i)for i in(0x68,0x6c)],'inner_reduction':[read(border_static+i)for i in(0x70,0x74)],'main_color':[read(view_static+4*i)for i in range(4)],'alt_color':[read(view_static+0x10+4*i)for i in range(4)]}
cases=[];expected=[];commands=0
def run(head,steps):
 global state,present,failure,calls,active,commands,lookup_assets
 present,failure,flips,*v=head[:8];calls=0;active=True
 state=[[10+i,20+i,*v,(flips>>(2*i))&1,(flips>>(2*i+1))&1,0,*map(fbits,(.1,.2,.3,.4)),7+i]for i in range(4)]
 put(mask+0x18,renderers[0]if present&1 else 0,8);put(mask+0x20,renderers[1]if present&2 else 0,8)
 put(border+0x38,normal if present&256 else 0,8);put(border+0x40,water if present&512 else 0,8)
 length=head[8]if len(head)>8 else 13;arr=0x10046000
 put(normal+0x20,arr if length>=0 else 0,8);put(arr+0x18,max(0,length))
 for i in range(max(0,length)):put(arr+0x20+8*i,100+i,8)
 output=[]
 for mode,*args in steps:
  trace.clear();commands+=1;result='ok';lookup_assets=mode=='g'
  if mode=='e':call(0x3446a08,mask,args[0])
  elif mode in('s','w'):
   layer,order,flag=args;call(0x3446904 if mode=='s'else 0x34469a0,mask,(layer&0xffffffff)|((order&0xffffffff)<<32),flag)
  elif mode=='a':floats(args);call(0x3446b58,mask,renderers[0]if present&1 else 0)
  elif mode in('t','l'):
   f,*pos=args;floats(pos);call(0x344511c if mode=='t'else 0x3445448,renderers[2]if present&32 else 0,*[(f>>i)&1 for i in range(4)])
  elif mode=='c':
   f=args[0];put(border+0x48,(f>>4)&1,1);call(0x3445774,border,renderers[2]if present&32 else 0,renderers[3]if present&64 else 0,*[(f>>i)&1 for i in range(3)])
  else:
   method,type=args;result='value,'+str(call((0x34424f8,0x3442520,0x344254c,0x3442578,0x34425a4,0x34425d0,0x3442610,0x3442650,0x3442690,0x34426d0)[method],normal,type))
  output.append(('error'if fault else result)+':'+str(int(active))+':'+ '/'.join(','.join(map(str,s))for s in state)+':'+ '/'.join(trace))
 cases.append(','.join(map(str,head))+'#'+';'.join(','.join(map(str,(m,*a)))for m,*a in steps));expected.append('|'.join(output))
def head(p=1023,f=0,flips=0,size=(1,.3),pos=(0,0,0)):return[p,f,flips,*map(fbits,(*size,*pos))]
cache=Path(tempfile.gettempdir())/'rm_cell_view_cases.json'
if '--managed-only'in sys.argv:cases,expected,commands,defaults=json.loads(cache.read_text())
else:
 for p in[1023,*[1023^(1<<i)for i in range(10)],0]:
  for fail_at in range(20):
   run(head(p,fail_at),[('s',-17,-2147483648,1),('w',89,2147483647,1),('e',0),('e',1),('a',*map(fbits,(.5,3,-2,4,7)))])
 for mode in('t','l'):
  for f in range(16):
   for p in(1023,1023^32,1023^128):
    for fail_at in range(9):
     for size,pos in[((1,.3),(0,.5,0)),((1.05,-.4),(-.5,0,7)),((-.125,3.25),(1.234567,2.34567,-9))]:
      run(head(p,fail_at,size=size,pos=pos),[(mode,f,*map(fbits,pos))])
 for f in[*range(8),*[16+i for i in range(8)]]:
  for p in(1023,1023^32,1023^64,1023^256,1023^512):
   for flips in(0,16,32,48,64,128,192,255):
    for fail_at in range(17):run(head(p,fail_at,flips),[('c',f),('c',f)])
 for length in range(-1,15):
  run(head()+[length],[('g',i,t)for i in range(10)for t in(-2147483648,-1,0,1,2147483647)]*2)
 cache.write_text(json.dumps([cases,expected,commands,defaults]))
# Verify serialized art constants against the executed native constructors.
settings=(PROJECT/'Assets/Foundation/Settings/CellViewSettings.asset').read_text()
import re
for key,field in(('side_size','sideBorderSize'),('outer_reduction','outerCornerSizeReduction'),('inner_reduction','innerCornerSizeReduction'),('main_color','mainColor'),('alt_color','altColor')):
 line=re.search(r'^  '+field+r': \{([^}]+)\}',settings,re.M)[1]
 bits=[fbits(float(x.split(':')[1]))for x in line.split(',')]
 assert bits==defaults[key],(field,bits,defaults[key])
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-cell-view-')as folder:
 exe=Path(folder)/'verify.exe';names=('OriginalCellMask','OriginalCellViewSettings','OriginalCellBorderGeometry','OriginalCellAssets')
 p=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-langversion:latest','-out:'+str(exe),str(PROJECT/'SourceCatalog/CellViewHarness.cs'),*[str(PROJECT/'Assets/Foundation/Runtime'/(n+'.cs'))for n in names]],capture_output=True,text=True);assert p.returncode==0,p.stdout+p.stderr
 p=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert p.returncode==0,p.stderr
 actual=p.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
 for i,(a,e)in enumerate(zip(actual,expected)):
  aa,ee=a.split('|'),e.split('|');assert len(aa)==len(ee),(i,len(aa),len(ee))
  for j,(x,y)in enumerate(zip(aa,ee)):assert x==y,(i,j,cases[i],x,y)
report={'passed':True,'native_sequences':len(cases),'native_commands':commands,'native_default_float_bits':defaults,'serialized_native_defaults_match':True,'scope':'Actual ARM64 CellMask SetSorting, SetWriteMaskSortingOrder, Enable and ArrangeMaskSize; CellBorder PrepareTopBottom, PrepareLeftRight and PrepareCorner; all ten CellAssets sprite selectors with null/short/full arrays and zero/nonzero/signed-extreme mask type. Exact renderer state, float bits, callback order and injected-failure partial state. Native static constructors execute for border geometry and main/alternate colors; serialized settings match their exact float bits. Unity engine storage/renderer properties and normal/water sprite getters inside corner geometry are supplied boundaries; normal sprite selectors also execute separately. Resource transport/caching is a replacement boundary, exercised with real resources in Unity. Full CellBorder topology, water initialization, cell lifecycle, formal cell placement and fall composition remain pending.'}
(PROJECT/'cell_view_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
