"""Execute original ARM64 normal cell border topology against runtime C#."""
import sys
sys.dont_write_bytecode=True
import json,os,random,struct,subprocess,tempfile,hashlib
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_move_budget.py').read_text().split('cases=[];expected=[]')[0],str(PROJECT/'SourceCatalog/verify_move_budget.py'),'exec'))
emu.hook_del(native_hook);base_hook=hook
border,view,delegate,neighbors,assets=0x10040000,0x10040100,0x10040200,0x10040300,0x10040400
renderers=[0x10041000+i*0x100 for i in range(16)];cells=[0x10043000+i*0x200 for i in range(8)]
border_arrays=[0x10045000+i*0x100 for i in range(3)];pair=0x10045400
statics={}
for i,got in enumerate((0x53b9288,0x53b2db8,0x536b340,0x53cfac8,0x53df178)):
 cls=0x10050000+i*0x800;static=cls+0x400;put(read(got,8),cls,8);put(cls+0xe0,1);put(cls+0xb8,static,8);statics[got]=static
put(statics[0x53cfac8]+12,0x3f800000)
stub_neighbors=0x20000100;put(pair,stub_neighbors,8);put(delegate,0x10054000,8)
case_head=[];state=[];calls=0;array_values={};commands=0;cases=[];expected=[]
next_object=0x10070000
raw_metadata=(PROJECT.parent.parent/'RoyalMatch_20260929/apk_unpacked/base/assets/bin/Data/Managed/Metadata/global-metadata.dat').read_bytes()
arrays=((0,1,2,3,4,5,6,7),(1,3,5,7),(2,4,6,0))
field_slots=(read(0x53bf800,8),read(0x5397568,8),read(0x53e17d8,8))
array_blobs=[struct.pack('<'+'i'*len(a),*a)for a in arrays]
symbols={x['Address']:x['Name']for x in json.loads((PROJECT.parent/'code/metadata/script.json').read_text())['ScriptMetadata']}
for slot,blob in zip(field_slots,array_blobs):assert symbols[slot].endswith(hashlib.sha256(blob).hexdigest().upper())
for b in array_blobs:assert raw_metadata.find(b)>=0
def record(n,*args):
 global calls
 trace.append(','.join(map(str,(n,*args))));calls+=1
 if calls==case_head[5]:fail(emu);return False
 return True
def hook(uc,address,size,unused):
 x0,x1,x2,x3,x4,x5=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(6)]
 if address==0x22bc630:
  i=next(i for i,p in enumerate(field_slots)if read(p,8)==x1);uc.mem_write(x0+0x20,array_blobs[i]);returned(uc)
 elif address==0xfcd934:assert x2==1;returned(uc,pair)
 elif address==stub_neighbors:
  if record('neighbors'):returned(uc,neighbors if case_head[6]&4 else 0)
 elif address==0x31932c8:
  if record('get',x1):returned(uc,cells[x1]if case_head[0]&(1<<x1)else 0)
 elif address in(0x344511c,0x3445448):
  if record('side',0 if address==0x344511c else 1,renderers.index(x0)if x0 else -1,*[uc.reg_read(UC_ARM64_REG_S0+i)for i in range(3)],x1,x2,x3,x4):returned(uc)
 elif address==0x3445774:
  if record('corner',renderers.index(x1)if x1 else -1,renderers.index(x2)if x2 else -1,x3,x4,x5):returned(uc)
 elif address==0x3e473b8:
  i=renderers.index(x0)
  if record('game',i):returned(uc,0 if i==case_head[8]else x0+0x40)
 elif address==0x3e49b30:
  i=renderers.index(x0-0x40)
  if record('active',i,x1):state[i][5]=x1;returned(uc)
 elif address==0x3e4737c:
  i=renderers.index(x0)
  if record('transform',i):returned(uc,0 if i==case_head[9]else x0+0x80)
 elif address in(0x3e4e4a0,0x3e4e6fc):
  i=renderers.index(x0-0x80);n=3 if address==0x3e4e4a0 else 4;k=6 if n==3 else 9;v=[uc.reg_read(UC_ARM64_REG_S0+j)for j in range(n)]
  if record('position'if n==3 else'rotation',i,*v):state[i][k:k+n]=v;returned(uc)
 elif address==0x3e4d92c:
  if record('nonnull',x0):returned(uc,x0!=0)
 elif address in(0x3e3e7c8,0x3e3e804,0x3e510cc,0x3e5114c,0x3e50dc0,0x3e50c30):
  if not x0:fail(uc);return
  i=renderers.index(x0)
  if address==0x3e50dc0:
   v=[uc.reg_read(UC_ARM64_REG_S0+j)for j in range(2)]
   if record('size',i,*v):state[i][3:5]=v;returned(uc)
  elif address==0x3e50c30:
   if record('sprite',i):returned(uc,state[i][13])
  else:
   name,k={0x3e3e7c8:('enabledget',0),0x3e3e804:('enabled',0),0x3e510cc:('flipx',1),0x3e5114c:('flipy',2)}[address]
   setter=address!=0x3e3e7c8
   if record(name,i,*([x1]if setter else[])):
    if setter:state[i][k]=x1;returned(uc)
    else:returned(uc,state[i][k])
 else:base_hook(uc,address,size,unused)
emu.hook_add(UC_HOOK_CODE,hook)
call(0x31955ec);call(0x3446500)
side_array=read(statics[0x53b2db8],8);native_sides=[read(side_array+0x20+4*i)for i in range(read(side_array+0x18))];assert native_sides==list(arrays[0])
import re
serialized=(PROJECT/'Assets/Foundation/Settings/CellViewSettings.asset').read_text()
for field,offset in(('topPosition',8),('rightPosition',0x14),('bottomPosition',0x20),('leftPosition',0x2c)):
 values=re.search(r'^  '+field+r': \{([^}]+)\}',serialized,re.M)[1]
 configured=[struct.unpack('<I',struct.pack('<f',float(v.split(':')[1])))[0]for v in values.split(',')]
 assert configured==[read(statics[0x53b9288]+offset+4*i)for i in range(3)],field
def bits(f):return struct.unpack('<I',struct.pack('<f',f))[0]
def run(h,steps):
 global case_head,state,calls,commands
 case_head=h;calls=0;state=[[1,1,0,bits(2),bits(3),1,*map(bits,(2,3,4,1,2,3,4)),0 if i==h[13]else 100+i]for i in range(16)]
 put(border+0x18,view if h[6]&1 else 0,8);put(view+0x58,delegate if h[6]&2 else 0,8)
 for j in range(3):
  length=h[10+j];put(border+0x20+8*j,border_arrays[j]if length>=0 else 0,8);put(border_arrays[j]+0x18,max(0,length))
  for i in range(max(0,length)):
   ident=(0 if j==0 else 8 if j==1 else 12)+i;put(border_arrays[j]+0x20+8*i,0 if ident==h[7]else renderers[ident],8)
 for i,cell in enumerate(cells):put(cell+0xa0,int(bool(h[1]&(1<<i))));put(cell+0xd8,h[3]if h[2]&(1<<i)else h[3]+1)
 call(0x3442894,border,assets)
 output=[]
 for step in steps:
  trace.clear();commands+=1;mode,*a=step
  if mode=='s':call(0x34430cc,border,h[3],h[4])
  elif mode=='p':call(0x3443004,border,a[0],renderers[a[1]]if a[1]>=0 else 0,h[3],h[4])
  else:call({'h':0x3442c14,'a':0x3445a08,'r':0x3442ab4}[mode],border)
  output.append(('error'if fault else'ok')+':'+ '/'.join(','.join(map(str,s))for s in state)+':'+ '/'.join(trace))
 cases.append(','.join(map(str,h))+'#'+';'.join(','.join(map(str,s))for s in steps));expected.append('|'.join(output))
def head(p=255,n=255,same=255,grid=0,separate=0,failure=0,flags=7,missing=-1,game=-1,transform=-1,borders=8,fills=4,masks=4,sprite=-1):return[p,n,same,grid,separate,failure,flags,missing,game,transform,borders,fills,masks,sprite]
cache=Path(tempfile.gettempdir())/'rm_cell_topology_cases.json'
if '--managed-only'in sys.argv:cases,expected,commands=json.loads(cache.read_text())
else:
 for shape in range(256):
  for separate in(0,1):run(head(p=shape,same=shape^85,separate=separate),[('s',),('h',),('s',),('r',)])
 for flags in(0,1,3,5,7):
  for fail_at in range(17):run(head(n=0x55,same=0xaa,separate=1,failure=fail_at,flags=flags),[('p',i,i)for i in range(-1,9)]+[('p',1,-1),('h',),('a',),('r',)])
 for missing in range(-1,16):
  for fail_at in(0,1,5,13,24):run(head(p=0,n=0,failure=fail_at,missing=missing),[('s',),('h',),('a',),('r',)])
 for kind in('game','transform','sprite'):
  for missing in range(16):run(head(p=0,n=0,**{kind:missing}),[('s',),('h',),('a',),('r',)])
 for b in(-1,0,3,7,8):
  for f in(-1,0,2,3,4):
   for m in(-1,0,2,4):run(head(p=0,borders=b,fills=f,masks=m),[('s',),('h',),('a',),('r',)])
 rng=random.Random(0x34430cc)
 for i in range(90):run(head(p=rng.randrange(256),n=rng.randrange(256),same=rng.randrange(256),grid=rng.choice((-2147483648,-1,0,7,2147483647)),separate=rng.randrange(2),failure=rng.randrange(250)),[('s',),('h',),('a',),('s',)])
 cache.write_text(json.dumps([cases,expected,commands]))
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-cell-topology-')as folder:
 exe=Path(folder)/'verify.exe';names=('OriginalCellBorder','OriginalCellViewSettings')
 p=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-langversion:latest','-out:'+str(exe),str(PROJECT/'SourceCatalog/CellTopologyHarness.cs'),*[str(PROJECT/'Assets/Foundation/Runtime'/(n+'.cs'))for n in names]],capture_output=True,text=True);assert p.returncode==0,p.stdout+p.stderr
 p=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert p.returncode==0,p.stderr
 actual=p.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
 for i,(a,e)in enumerate(zip(actual,expected)):
  aa,ee=a.split('|'),e.split('|');assert len(aa)==len(ee),(i,len(aa),len(ee))
  for j,(x,y)in enumerate(zip(aa,ee)):assert x==y,(i,j,cases[i],x,y)
report={'passed':True,'native_sequences':len(cases),'native_commands':commands,'native_sides':native_sides,'field_sha256':[hashlib.sha256(b).hexdigest()for b in array_blobs],'scope':'Actual ARM64 CellBorder normal Init, ShowParts, all eight PrepareBorder direction branches, normal/shifter-grid membership predicate, HideParts, ActivateBorders and normal OnRecycle. Native CellNeighbors constructor executes with original metadata array bytes, yielding original direction order. Exact neighbor getter/query order, geometry arguments, renderer state and injected-failure partial state. Unity renderer storage, delegate/neighbor access and already separately verified geometry helpers are supplied boundaries. Water preparation/reset, shifter orchestration, static fade masks and full cell lifecycle are outside this verifier.'}
(PROJECT/'cell_topology_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
