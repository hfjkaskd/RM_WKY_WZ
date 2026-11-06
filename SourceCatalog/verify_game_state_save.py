"""Execute actual ARM64 packed game-state codec and snapshot/getter paths."""
import sys
sys.dont_write_bytecode=True
import hashlib,json,os,random,struct,subprocess,tempfile
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_move_budget.py').read_text().split('cases=[];expected=[]')[0],str(PROJECT/'SourceCatalog/verify_move_budget.py'),'exec'))
emu.hook_del(native_hook);base_hook=hook;metadata_next=0x101a0000
owner,conf,width_array,stored=0x10020000,0x10021000,0x10022000,0x10024000
moves,ego,random_owner,user_unit,new_helper,tutorial,dynamic,active,user,session=[0x10030000+i*0x200 for i in range(10)]
user_class,user_static,extra_class,extra_static,state_class,state_static=[0x10040000+i*0x400 for i in range(6)]
for got,klass,static in ((0x53bb4e0,user_class,user_static),(0x5367c00,extra_class,extra_static),(0x53e8e88,state_class,state_static)):
 put(read(got,8),klass,8);put(klass+0xe0,1);put(klass+0x132,1,1);put(klass+0xb8,static,8)
empty_method,empty_rgctx,empty_class,empty_static,empty_array=0x10048000,0x10048400,0x10048800,0x10048c00,0x10049000
put(read(0x535f790,8),empty_method,8);put(empty_method+0x38,empty_rgctx,8);put(empty_rgctx,empty_class,8)
put(empty_class+0xe0,1);put(empty_class+0x132,1,1);put(empty_class+0xb8,empty_static,8);put(empty_static,empty_array,8);put(empty_array+0x18,0)
WIDTHS=[7,7,13,7,1,10,30,1,1,1,5,1,1,1];blob=struct.pack('<14i',*WIDTHS)
symbols={x['Address']:x['Name']for x in json.loads((PROJECT.parent/'code/metadata/script.json').read_text())['ScriptMetadata']}
assert symbols[read(0x53a6d78,8)].endswith(hashlib.sha256(blob).hexdigest().upper())
raw_metadata=(PROJECT.parent.parent/'RoyalMatch_20260929/apk_unpacked/base/assets/bin/Data/Managed/Metadata/global-metadata.dat').read_bytes()
assert raw_metadata[0xa40af8:0xa40b30]==blob
flags=1023;failure=0;calls=0;heap=0;values=[];trace=[];allocations=[]
def wide(v):return v-(1<<64)if v>>63 else v
def alloc(size):
 global heap
 p=heap;heap=(heap+size+15)&~15;assert heap<0x10180000;emu.mem_write(p,bytes(size));return p
def arr(a,n=8):
 if a is None:return 0
 p=alloc(0x20+n*len(a));put(p+0x18,len(a))
 for i,v in enumerate(a):put(p+0x20+n*i,v,n)
 return p
def show(p):return '~'if not p else '/'.join(str(wide(read(p+0x20+8*i,8)))for i in range(read(p+0x18)))
def record(value):
 global calls
 trace.append(value);calls+=1
 if calls==failure:fail(emu);return False
 return True
def hook(uc,address,size,unused):
 x0,x1,x2=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(3)]
 if address==0xffda78:returned(uc,alloc(0x100))
 elif address==0xffd9d0:
  length=signed(x1);allocations.append(length)
  if length<0 or length>128:fail(uc)
  else:returned(uc,arr([0]*length))
 elif address==0x231007c:returned(uc)
 elif address==0x22bc630:
  assert read(x0+0x18)==14;uc.mem_write(x0+0x20,blob);returned(uc)
 elif address==0x260dea4:
  p,name,mask={11:(moves,'move',1),32:(ego,'ego',2),0:(random_owner,'random',4),21:(tutorial,'tutorial',32),51:(dynamic,'dynamic',64)}[x0]
  if record(name):returned(uc,p if flags&mask else 0)
 elif address==0x29e27cc:
  assert x0==2
  if record('newuser'):returned(uc,user_unit if flags&8 else 0)
 elif address==0x23e099c:record('active')
 elif address==0x1f71a94:
  if record('shown'):returned(uc,int(values[7]!=0))
 else:base_hook(uc,address,size,unused)
native_hook=emu.hook_add(UC_HOOK_CODE,hook)
heap=0x10060000;call(0x23e63ec);assert not fault
sizes_pointer=read(state_static,8);assert[read(sizes_pointer+0x20+4*i)for i in range(14)]==WIDTHS
getters=[0x23e3490,0x23e345c,0x23e3a58,0x23e34c4,0x23e34f8,0x23e6304,0x23e6318,0x23e632c,0x23e6348,0x23e6364,0x23e6380,0x23e6398,0x23e63b4,0x23e63d0]
def ints(t):return None if t=='~'else[]if t==''else list(map(int,t.split('/')))
def setconfig(t):
 if t=='~':put(owner+0x10,0,8);return None
 w=None if t=='!'else ints(t);p=arr(w,4);put(conf+0x10,p,8);put(owner+0x10,conf,8);return p
def refresh():
 for p,o,k in((moves,0x20,0),(active,0x20,1),(moves,0x24,2),(active,0x4c,3),(random_owner,0x24,5),(random_owner,0x28,6)):put(p+o,values[k])
 for p,o,k in((ego,0x44,4),(tutorial,0x58,8),(dynamic,0xa5,9),(dynamic,0x10,11),(dynamic,0x11,12),(dynamic,0x14,13)):put(p+o,int(values[k]!=0),1)
 put(extra_static,values[10]);put(user_unit+0x18,new_helper if flags&16 else 0,8)
 put(user_static,user if flags&128 else 0,8);put(user+0x38,session if flags&256 else 0,8);put(session+0x18,active if flags&512 else 0,8)
cases=[];expected=[];commands=0
def run(kind,c,data,steps):
 global heap,flags,values,failure,calls,commands,fault
 heap=0x10070000;flags=1023;failure=0
 if kind=='s':
  values=ints(c);refresh();call(0x23e2a7c,owner);assert not fault;wp=0
 else:wp=setconfig(c)
 dp=arr(ints(data));call(0x23e5d28,owner,dp)
 out=[]
 for step in steps:
  calls=0;trace.clear();allocations.clear();commands+=1;f=step.split(',');mode=f[0];result='-'
  if mode=='g':
   rva=getters[int(f[1])]if kind=='s'else 0x23dee18;call(rva,owner,int(f[1]));v=emu.reg_read(UC_ARM64_REG_X0)
   result=str(signed(v)if kind=='s'else wide(v))
  elif mode=='r':result=str(call(0x23e3dd0,owner))
  elif mode in('p','q'):
   if kind=='s':call(0x23e0834,owner)
   else:call(0x23deb9c,owner,arr(ints(f[1])))
   if not fault:
    p=emu.reg_read(UC_ARM64_REG_X0);result=show(p)
    if mode=='q':dp=p;call(0x23e5d28,owner,p)
  elif mode=='c':wp=setconfig(f[1]);call(0x23e5d20,owner,read(owner+0x10,8))
  elif mode=='d':dp=arr(ints(f[1]));call(0x23e5d28,owner,dp)
  elif mode in('m','v'):
   p=wp if mode=='m'else dp;n=4 if mode=='m'else 8;i=int(f[1]);fault=False
   if not p or not 0<=i<read(p+0x18):fault=True
   else:put(p+0x20+n*i,int(f[2]),n)
  elif mode in('o','z','n','t'):
   p=read(owner+0x10,8);fault=False
   if not p:fault=True
   else:result=str(call({'o':0x23e5e3c,'z':0x23e5e98,'n':0x23e5ec8,'t':0x23e5ee4}[mode],p,int(f[1])if len(f)>1 else 0))
  elif mode=='f':flags=int(f[1]);refresh();fault=False
  elif mode=='x':failure=int(f[1]);fault=False
  elif mode=='e':values[int(f[1])]=int(f[2]);refresh();fault=False
  elif mode in('b','w'):
   call(0x23e5e24 if mode=='b'else 0x23e5f44,*map(int,f[1:]));result=str(wide(emu.reg_read(UC_ARM64_REG_X0)))
  else:raise AssertionError(step)
  if fault:result='error'
  out.append(result+':'+show(dp)+':'+ '/'.join(trace))
 cases.append(kind+'#'+c+'#'+data+'#'+';'.join(steps));expected.append('|'.join(out))
def text(a):return '~'if a is None else '/'.join(map(str,a))
edges=[-2**63,-1,0,1,2**31-1,2**32+1,2**63-1]
for a in edges:
 for b in(-65,-1,0,1,31,63,64,65,129):
  run('g','1','0',[f'b,{a},{b},{e}'for e in(-1,0,1,63,64,65,129)]+[f'w,{a},{a},{b},{e}'for e in(0,1,63,64,65,129)])
for widths in(None,[],[0],[64],[65],[63,2],[45,30,11],[7,7,13,7,1,10,30,1,1,1,5,1,1,1],[1,0,63,0,64],[1,-1,2],[-1,65],[65,-65],[2,128],[130],[2147483647,2147483647]):
 c='!'if widths is None else text(widths)
 for loaded in(None,[],[0],[-1],[0,0],[-2**63,2**63-1,42]):
  steps=['n','t','r','o,-1','o,0']
  for i in range(-1,3 if widths is None else len(widths)+2):steps +=[f'o,{i}',f'z,{i}',f'g,{i}']
  if widths is None or sum(widths)<1000:steps+=['p,~','p,','q,'+text([123]*max(1,len(widths or []))),'r','g,0']
  run('g',c,text(loaded),steps)
run('g','~','~',['o,-1','o,0','n','t','r','g,0','p,','c,1','q,1','c,!','q,1','c,','p,~','r'])
run('g','7/7','3',['m,0,63','o,1','p,100/2','g,0','v,0,-1','g,0','m,1,65','r','g,1','c,1/2147483647','t','o,2'])
rng=random.Random(0x23deb9c)
for _ in range(180):
 widths=[rng.randrange(0,66)for _ in range(rng.randrange(1,12))];v=[rng.choice(edges)if rng.randrange(4)==0 else rng.randrange(1<<min(n,62))for n in widths]
 steps=['p,'+text(v),'q,'+text(v),'r']+[f'g,{i}'for i in range(len(widths))]+['d,0','r',f'g,{len(widths)-1}']
 run('g',text(widths),'7/8',steps)
default=[21,30,9,2,1,371,123456789,1,0,1,-3,1,0,1]
for flags_case in[1023,0]+[1023^(1<<i)for i in range(10)]:
 for failure_case in range(10):
  run('s',text(default),'42/43',[f'f,{flags_case}',f'x,{failure_case}','p','q','r']+[f'g,{i}'for i in range(14)])
for _ in range(160):
 v=[rng.randrange(1<<w)for w in WIDTHS];v[10]-=15
 if _<14:v[_]=rng.choice([-2147483648,-1,2147483647])if _ not in(4,7,8,9,11,12,13)else _%2
 run('s',text(v),'~',['p','r','q','r']+[f'g,{i}'for i in range(14)]+['d,0','r','g,6','d,','r','g,0'])
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-game-state-')as directory:
 exe=Path(directory)/'verify.exe';files=['OriginalGenericSaveData','OriginalGameStateSaveData']
 p=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-langversion:latest','-out:'+str(exe),str(PROJECT/'SourceCatalog/GameStateSaveHarness.cs'),*[str(PROJECT/'Assets/Foundation/Runtime'/(n+'.cs'))for n in files]],capture_output=True,text=True);assert p.returncode==0,p.stdout+p.stderr
 p=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8-sig');assert p.returncode==0,p.stderr[-1600:]
 actual=p.stdout.splitlines();assert len(actual)==len(expected),(len(actual),len(expected),p.stderr[-500:])
 for i,(a,e)in enumerate(zip(actual,expected)):
  if a!=e:
   j=next(j for j,(x,y)in enumerate(zip(a.split('|'),e.split('|')))if x!=y)
   raise AssertionError((i,j,cases[i],a.split('|')[j],e.split('|')[j]))
report={'passed':True,'native_sequences':len(cases),'native_commands':commands,'state_widths':WIDTHS,'state_total_bits':sum(WIDTHS),'state_array_length':2,
 'field_initializer_sha256':hashlib.sha256(blob).hexdigest().upper(),'metadata_blob_offset':'0xa40af8',
 'scope':'Actual ARM64 GenericSaveDataConfig offset/size/count/total/bit read-write; AGenericSaveDataHelper SetConfig, SetData, GetData, IsDataReadable and PrepareData; GameStateSaveDataHelper constructor/static initializer, all14 getters and ordered PrepareStateSaveData. Tests cross-long MSB-first fields, 64-bit shift masking, unmasked write spill, signed/overflow/zero layouts, null and truncated data, borrowed width/data mutation, preparation without loaded-data replacement, extra-move+15 encoding, context lookup/null order and callback failure. Metadata field SHA256 and exact56-byte blob verified. Supplied boundaries: CLR metadata/allocation/errors, context lookup, tutorial method and explicit owner fields. Large allocations are bounded by fixtures; no transport/database/user-session integration or rendered parity claim. SDK unchanged.'}
(PROJECT/'game_state_save_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
