"""Execute native ARM64 Session.Win and all23 win-reward arithmetic entries.

Actual state/move capture, Coin.Init/AddExtra, milestone/star/feature snapshot
writes and native branching instructions run. Account/event/feature getters,
reward-dispatch calls, metadata/vtable resolution and diagnostic formatting are
explicit typed boundaries. Independent executions cover actual23 AnimationData
reward bodies, including wheel/streak side effects and partial failures. This
does not prove production account providers, win trigger/Flow/dialog binding,
active event presentation, persistence or original rendered parity.
"""
from pathlib import Path
import hashlib,json,random,sqlite3,struct,subprocess,sys,tempfile
PROJECT=Path(__file__).resolve().parents[1];BASE=PROJECT.parent
sys.path.insert(0,str(BASE/'tools/python'))
from elftools.elf.elffile import ELFFile
from elftools.elf.relocation import RelocationSection
from unicorn import Uc,UC_ARCH_ARM64,UC_MODE_ARM,UC_HOOK_CODE
import unicorn.arm64_const as r
spec=json.loads((PROJECT/'SourceCatalog/session_win_boundaries.json').read_text())
flags=spec['flags'];kernels=spec['kernels']
u=Uc(UC_ARCH_ARM64,UC_MODE_ARM);u.mem_map(0,0x6000000);u.mem_map(0x10000000,0x400000);u.mem_map(0x20000000,0x1000)
def put(p,v,n=8):u.mem_write(p,(int(v)&((1<<(8*n))-1)).to_bytes(n,'little'))
def read(p,n=4):return int.from_bytes(u.mem_read(p,n),'little')
def si(v):v&=0xffffffff;return v-0x100000000 if v&0x80000000 else v
def s8(v):v&=255;return v-256 if v&128 else v
def x(i):return u.reg_read(getattr(r,'UC_ARM64_REG_X'+str(i)))
with (BASE.parent/'RoyalMatch_20260929/apk_unpacked/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so').open('rb')as stream:
 elf=ELFFile(stream)
 for seg in elf.iter_segments():
  if seg['p_type']=='PT_LOAD':u.mem_write(seg['p_vaddr'],seg.data())
 for seg in elf.iter_sections():
  if isinstance(seg,RelocationSection):
   for v in seg.iter_relocations():
    if v['r_info_type']==1027:put(v['r_offset'],v['r_addend'])
session,moves,animation=[0x10001000+i*0x1000 for i in range(3)]
snapshots=[0x10004000+i*0x1000 for i in range(6)]
cloche=0x1000a000;user_statics,user,inventory,persistent,package,level_statics,event_data=[0x10015000+i*0x1000 for i in range(7)]
coin_class=0x100e0000;metadata_next=0x10200000
put(snapshots[0],coin_class);put(coin_class+0x1b8,0x20000200)
mode='';trace=[];fault=False;v=[];missing_actor=0;mask=0;kernel_index=0
def actor(n):return 0x10030000+n*0x1000
def returned(value=0):u.reg_write(r.UC_ARM64_REG_X0,int(value)&0xffffffffffffffff);u.reg_write(r.UC_ARM64_REG_PC,u.reg_read(r.UC_ARM64_REG_LR))
io_flags={address:i for i,(_,_,address)in enumerate(flags)if address>0x3200000 and i in(3,15,17,18,21,22)}
method_flags={address:i for i,(_,_,address)in enumerate(flags)if i not in(3,15,17,18,21,22)}
method_flags.update({0x222da98:23,0x222d9a8:24,0x222d5ec:25,0x222db10:26,0x2105fdc:28})
reward_map={row[2]:(i,row[4])for i,row in enumerate(kernels)}
def boundary(uc,address,size,unused):
 global metadata_next,fault
 a,b,c=x(0),x(1),x(2)
 if address==0xffd960:
  if not 0x10200000<=read(a,8)<0x10300000:
   value=metadata_next;metadata_next+=0x200;put(a,value);put(value+0xe0,1,4);put(value+0x132,1,1)
   # Runtime metadata initialization receives the relocated ScriptMetadata
   # address, rather than the GOT slot which points to that address.
   put(value+0xb8,user_statics if a in(0x53bb4e0,0x5595c00)else level_statics)
 elif address in(0xffda64,0xffd90c):pass
 elif address in(0xffda88,0xffda90,0xffdaac):fault=True;u.reg_write(r.UC_ARM64_REG_PC,0x20000000);return
 elif address==0x32b8ad4 and mode=='w':u.reg_write(r.UC_ARM64_REG_PC,0x32b8bdc);return # Debug formatting only.
 elif address in io_flags:
  if mode=='w':trace.append('q'+str(io_flags[address]))
  return
 elif address in(0x32b8c2c,0x32b8c3c,0x32b8d30,0x32b9488,0x32b95c8):
  if mode=='w':trace.append({0x32b8c2c:'inventory-owner',0x32b8c3c:'coin',0x32b8d30:'water',0x32b9488:'persistent',0x32b95c8:'magic-max'}[address])
  return
 elif address==0x20000200:
  trace.append('init,'+str(si(b)));u.reg_write(r.UC_ARM64_REG_PC,0x338c6a8);return
 elif address==0x29e27cc:
  assert a in(7,8,65,16,26,57,13,35,41,49,38,45,46,58,59,64,72,77,73,80,31,29,50,63),a
  if mode=='w'and a in(29,50,63):trace.append({29:'get-super',50:'get-duke',63:'get-magic'}[a])
  returned(0 if a==missing_actor else actor(a));return
 elif address in method_flags:
  index=method_flags[address];trace.append('q'+str(index));returned((mask>>index)&1);return
 elif address in reward_map and mode=='w':
  index,kind=reward_map[address];entry='r'+str(index)
  if kind!='one':entry+=','+str(s8(b))
  if kind=='mult':entry+=','+str(si(c))
  trace.append(entry)
 elif address==0x337d258:
  trace.append('milestone,'+str(si(a)));returned(0 if missing_actor==-2 else package);return
 elif address==0x20ef780:trace.append('archery-mult');returned(v[15]);return
 elif address==0x2757a44:trace.append('world-mult');returned(v[16]);return
 elif address==0x1f88d24:trace.append('cloche,'+str(si(read(b+0x28))if b else -999))
 elif address==0x222d88c:trace.append('unlock,'+str(a&1));returned(v[18]if a&1 else v[17]);return
 elif address==0x222db68:trace.append('super-win')
 elif address==0x222fd04:trace.append('duke-can,'+str(b&1));returned((mask>>27)&1);return
 elif address==0x222fdc8:trace.append('duke-win')
 elif address==0x2105ffc:trace.append('magic-progress,'+str(si(b)))
 elif address==0x2405dbc:trace.append('wheel')
 elif address==0x1f99e34:trace.append('streak');returned(v[2]);return
 else:raise AssertionError((mode,hex(address)))
 returned()
addresses={0xffd960,0xffda64,0xffd90c,0xffda88,0xffda90,0xffdaac,0x32b8ad4,0x32b8c2c,0x32b8c3c,0x32b8d30,0x32b9488,0x32b95c8,
 0x20000200,0x29e27cc,0x337d258,0x20ef780,0x2757a44,0x1f88d24,0x222d88c,0x222db68,0x222fd04,0x222fdc8,0x2105ffc,0x2405dbc,0x1f99e34,*io_flags,*method_flags,*reward_map}
for address in addresses:
 def dispatch(uc,addr,size,unused):
  if addr in reward_map and mode=='k':return
  boundary(uc,addr,size,unused)
 u.hook_add(UC_HOOK_CODE,dispatch,begin=address,end=address)
def call(address,*args):
 global fault
 fault=False
 for i in range(8):u.reg_write(getattr(r,'UC_ARM64_REG_X'+str(i)),(args[i]if i<len(args)else 0)&0xffffffffffffffff)
 u.reg_write(r.UC_ARM64_REG_SP,0x103ff000);u.reg_write(r.UC_ARM64_REG_LR,0x20000000)
 u.emu_start(address,0x20000000,count=100000);assert u.reg_read(r.UC_ARM64_REG_PC)==0x20000000,hex(u.reg_read(r.UC_ARM64_REG_PC))
def snapshot(p):return [si(read(p+0x24)),si(read(p+0x28)),read(p+0x20,1)]
def set_snapshot(p,before,after,consume=0):put(p+0x24,before,4);put(p+0x28,after,4);put(p+0x20,consume,1)
cases=[];expected=[];counts={'win':0,'kernel':0};rng=random.Random(0x32b8920)
def run(kind,values):
 global mode,trace,v,missing_actor,mask,kernel_index
 mode=kind;v=list(map(int,values));trace=[]
 if mode=='w':
  counts['win']+=1;missing_actor=v[22];mask=v[14];missing=v[23]
  for offset,value,n in((0x10,v[4],1),(0x14,v[0],4),(0x2c,v[1],4),(0x30,v[5],1),(0x34,v[2],4),(0x39,v[3],1),(0x3c,2,4),(0x40,0,1),(0x44,-9,4),(0x48,-8,4),(0x50,v[24],4)):put(session+offset,value,n)
  for offset,value in((0x20,v[6]),(0x24,v[7]),(0x2c,v[8])):put(moves+offset,value,4)
  put(session+0x88,0 if missing==1 else animation)
  for offset,p,bad in((0x28,snapshots[0],2),(0x30,snapshots[1],3),(0x48,event_data,8),(0xa0,event_data,9)):put(animation+offset,0 if missing==bad else p)
  for offset,p,bad in((0x60,snapshots[2],4),(0x70,snapshots[3],5),(0x78,snapshots[4],6),(0x80,snapshots[5],7)):put(session+offset,0 if missing==bad else p)
  put(session+0x68,cloche);set_snapshot(cloche,0,17)
  for p,(before,after,consume)in zip(snapshots,((31,32,1),(7,9,0),(v[25],v[26],0),(0,4,0),(0,5,0),(v[20],v[21],0))):set_snapshot(p,before,after,consume)
  put(user_statics,user);put(user+0x20,0 if missing_actor==-1 else inventory);put(inventory+0x10,v[10],4)
  put(user+0x18,0 if missing_actor==-3 else persistent);put(persistent+0x14,v[12],4);put(package+0x14,v[11],4);put(level_statics+0x14,v[13],1)
  for index,(_,owner,address)in enumerate(flags):
   if index in(3,15,17,18,21,22):put(actor(owner)+(0x48 if index==3 else 0x19 if index in(15,17)else 0x18),(mask>>index)&1,1)
  put(actor(63)+0x28,v[19],4)
  call(0x32b8920,session,0 if missing==10 else moves,v[9])
  result=[si(read(session+0x3c)),si(read(session+0x44)),si(read(session+0x48)),read(session+0x40,1),si(read(session+0x50))]
  for p in snapshots:result+=snapshot(p)
 elif mode=='k':
  counts['kernel']+=1;kernel_index=v[0];name,field,address,offset,typ=kernels[kernel_index]
  missing_actor=(16 if typ=='wheel'else 46)if v[6]else 0
  put(animation+offset,0 if v[5]else event_data);set_snapshot(event_data,13,v[3],v[4])
  call(address,animation,v[1],v[2]);result=snapshot(event_data)
 else:raise AssertionError(mode)
 cases.append(mode+','+','.join(map(str,v)));expected.append(f'{int(fault)}#'+','.join(map(str,result))+'#'+';'.join(trace))
edge=(-2147483648,-3,-1,0,1,7,2147483647)
actors=(0,0,0,7,8,65,16,26,57,13,35,41,49,38,45,46,58,59,64,72,77,73,80,31,29,50,63,-1,-2,-3)
for n in range(720):
 run('w',[rng.choice((1,91,92,291,292,401,402,403,409,410,999,1000,2000,-1,2147483647)),rng.choice((-2,0,1)),rng.choice((-3,0,1)),rng.randrange(2),n%7==0,
  rng.choice((-128,-1,0,1,2,3,127)),rng.choice(edge),rng.choice(edge),rng.choice(edge),rng.choice(edge),rng.choice(edge),rng.choice(edge),rng.choice((1,92,292,410)),rng.randrange(2),
  rng.randrange(1<<29),rng.choice(edge),rng.choice(edge),rng.choice((92,292,410)),92,rng.choice(edge),rng.choice(edge),rng.choice(edge),rng.choice(actors),rng.choice((0,0,0,1,2,3,4,5,6,7,8,9,10)),19,3,rng.choice(edge)])
# Fully admitted paths and exact boundary levels with all reward gates open.
for number in(1,91,92,291,292,401,402,403,409,410,999,1000,2000):
 for league,story,table,water in((0,0,0,0),(1,0,0,0),(0,-1,0,0),(0,-1,1,0),(0,-1,1,1),(0,0,1,0)):
  run('w',[number,league,story,table,0,0,24,6,31,7,120,500,400,water,(1<<29)-1,2,3,292,92,10,4,6,0,0,19,0,0])
for index in range(len(kernels)):
 for n in range(50):run('k',[index,rng.choice((-128,-1,0,1,2,3,127)),rng.choice(edge),rng.choice(edge),n%3==0,n%11==0,n%7==0])
mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
sources=['SourceCatalog/SessionWinHarness.cs','Assets/Foundation/Runtime/OriginalSessionWin.cs','Assets/Foundation/Runtime/OriginalWinAnimationRewards.cs','Assets/Foundation/Runtime/OriginalSessionValues.cs']
with tempfile.TemporaryDirectory()as folder:
 exe=Path(folder)/'Win.exe';command=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',
  *['-r:'+str(mono/'lib/mono/4.5'/name)for name in('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),*[str(PROJECT/p)for p in sources]]
 result=subprocess.run(command,capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 actual=result.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected)
 for i,(a,b)in enumerate(zip(actual,expected)):assert a==b,(i,cases[i],a,b)
report={'passed':True,'cases':len(cases),'win_cases':counts['win'],'reward_cases':counts['kernel'],'reward_entries':len(kernels),'scope':__doc__.strip(),
 'files':[{'path':p,'sha256':hashlib.sha256((PROJECT/p).read_bytes()).hexdigest()}for p in sources+['SourceCatalog/session_win_boundaries.json']]}
(PROJECT/'session_win_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
