"""Execute original ARM64 GameManager.StartGame/RestoreGame/StartLevel/CreateItems.

Session.Start, feature services, life/storage/network/analytics, LevelContext
activation, goals/moves/parser/grid/item/tutorial/save/sorting and Stopwatch
are explicit typed boundaries. Native branching, captured/re-read owners,
provider dispatch, entry origin/exit flag writes and failure order execute
from the ELF, including initial population/retry/cap/restore ordering. Only
side-level diagnostic formatting is skipped. This does
not prove a production application factory, real scene startup, restore-file
format, Home navigation, rendered parity or mobile execution.
"""
from pathlib import Path
import sys,json,hashlib,itertools,random,subprocess,tempfile
PROJECT=Path(__file__).resolve().parents[1];BASE=PROJECT.parent
sys.path.insert(0,str(BASE/'tools/python'))
from elftools.elf.elffile import ELFFile
from elftools.elf.relocation import RelocationSection
from unicorn import Uc,UC_ARCH_ARM64,UC_MODE_ARM,UC_HOOK_CODE
import unicorn.arm64_const as r
u=Uc(UC_ARCH_ARM64,UC_MODE_ARM);u.mem_map(0,0x6000000);u.mem_map(0x10000000,0x400000);u.mem_map(0x20000000,0x3000)
def put(p,v,n=8):u.mem_write(p,(int(v)&((1<<(8*n))-1)).to_bytes(n,'little'))
def read(p,n=8):return int.from_bytes(u.mem_read(p,n),'little')
def si(v):return (int(v)&0xffffffff)-0x100000000 if int(v)&0x80000000 else int(v)&0xffffffff
def x(i):return u.reg_read(getattr(r,'UC_ARM64_REG_X'+str(i)))
def returned(v=0):u.reg_write(r.UC_ARM64_REG_X0,v&0xffffffffffffffff);u.reg_write(r.UC_ARM64_REG_PC,u.reg_read(r.UC_ARM64_REG_LR))
with(BASE.parent/'RoyalMatch_20260929/apk_unpacked/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so').open('rb')as stream:
 elf=ELFFile(stream)
 for s in elf.iter_segments():
  if s['p_type']=='PT_LOAD':u.mem_write(s['p_vaddr'],s.data())
 for s in elf.iter_sections():
  if isinstance(s,RelocationSection):
   for e in s.iter_relocations():
    if e['r_info_type']==1027:put(e['r_offset'],e['r_addend'])
manager,user,session,active,other_active,level,parser,parser2,saved,restore,restore2,grid,grid2,moves,goals,clock,level_context,factory,life,tutorial,sorting,analytics,feature,ux,clash,clash_helper,goal0,goal1,goal2,limit0,limit1,statics,user_statics,provider_class,entry=[0x10001000+i*0x1000 for i in range(35)]
game_slot=read(0x5362170);user_slot=read(0x53bb4e0);key_slot=read(0x53f1998)
parser_type_slot=read(0x53564f8);saved_class=0x10024000;creator=0x10025000;hierarchy=0x10026000
providers={parser:0,parser2:1,saved:2};grids={grid:0,grid2:1};restores={restore:0,restore2:1}
metadata=0x10200000;trace=[];calls=0;fault=False;v=[];running=False;tries=100;lives=0;cloche=17;left=-111;made=-222;reset=-1;restore_id=0
retries=0
def fail():
 global fault
 fault=True;u.reg_write(r.UC_ARM64_REG_PC,0x20000000)
def step(label):
 global calls,restore_id
 trace.append(label);calls+=1
 if calls==v[2]:fail();return False
 if calls==v[3]:
  kind=v[4]
  if kind in(1,2):put(level+0x30,parser2 if kind==1 else 0)
  elif kind in(3,4):put(manager+0x38,grid2 if kind==3 else 0)
  elif kind==5:restore_id=1
  elif kind==6:put(session+0x18,other_active)
  elif kind==7:put(level+0x38,limit1)
  elif kind==8:put(manager+0x60,0)
  elif kind==9:put(manager+0x88,0)
  elif kind==10:put(manager+0x70,0)
  elif kind==11:
   for p in(parser,parser2):put(p+0x15,0,1)
  elif kind==12:put(manager+0x30,0)
  elif kind==13:put(manager+0x20,0)
  elif kind==14:put(manager+0x80,0)
 return True
def result(label,value=0):
 if step(label):returned(value)
def hook(uc,address,size,unused):
 global metadata,running,tries,lives,cloche,left,made,reset,retries
 a,b,c,d=x(0),x(1),x(2),x(3)
 if address==0xffd960:
  put(a,metadata);put(metadata+0xe0,1,4)
  if a==game_slot:put(metadata+0xb8,statics)
  elif a==user_slot:put(metadata+0xb8,user_statics)
  elif a==parser_type_slot:
   put(metadata+0x12c,1,1);put(hierarchy,metadata)
  metadata+=0x200;returned();return
 if address in(0xffd90c,0xffda64):returned();return
 if address in(0xffda88,0xffda90,0xffde20):fail();return
 if address==0x1e38dc4:uc.reg_write(r.UC_ARM64_REG_PC,0x1e38e54);return # Side-progression diagnostic-only argument building.
 if address==0x29e27cc:
  if not step('unit,'+str(a)):return
  missing={29:4,50:5,63:6,2:7,69:9,74:10,23:30,31:13,20:14,1:15,7:16,8:17}[a]
  returned(0 if v[1]==missing else level if a==2 else clash if a==74 else feature);return
 if address==0x260dea4:
  assert a in(11,12)
  if not step('level,'+str(a)):return
  returned(0 if v[1]==(21 if a==11 else 22)else moves if a==11 else goals);return
 if address==0x32b6ed0:assert a in(active,other_active);result('session,'+str(int(a==other_active)));return
 if address in(0x222dbdc,0x222fe98,0x21061a4,0x1f71a08,0x2116e6c,0x3d0cb30,0x3272c78,0x3272da0,0x23f557c):
  result({0x222dbdc:'booster',0x222fe98:'duke',0x21061a4:'magic',0x1f71a08:'ux',0x2116e6c:'multiplier',0x3d0cb30:'clash',0x3272c78:'event',0x3272da0:'side',0x23f557c:'team'}[address]);return
 if address==0x33fe6f4:result('nightmare,'+str(si(b)));return
 if address==0x1e1e394:
  assert a==life
  if step('life'):lives+=1;returned()
  return
 if address==0x1f881dc:result('cloche',17);return
 if address==0x1f88230:result('reset?,'+str(si(b)),v[8]);return
 if address==0x1f88268:result('show?',v[9]);return
 if address==0x1f881f8:
  if step('set,'+str(si(b))):cloche=si(b);returned()
  return
 if address==0x2ae19c8:result('backend,'+str(b));return
 if address==0x1ca91f0:result('claim?',v[10]);return
 if address==0x1ca9228:result('claim,'+str(b));return
 if address==0x25e5100:
  assert a==read(key_slot)and(b,c)==(1,0)
  if step('increment,LevelTryCount,1,0'):tries=si(tries+1);returned(tries)
  return
 if address in(0x11de11c,0x11de0a4):
  assert a==clock
  if step('restart'if address==0x11de11c else'stop'):running=address==0x11de11c;returned()
  return
 if address==0x3206e7c:assert a==level_context;result('activate,'+str(b));return
 if address==0x23f1730:assert a==factory;result('parents');return
 if address==0x23df9e4:
  if step('restore'):returned(0 if v[1]==23 else restore if restore_id==0 else restore2)
  return
 if address in(0x23e51c4,0x23e5500):result(('before,'if address==0x23e51c4 else'after,')+str(restores[a]));return
 if address==0xfcd934:
  assert a in providers and c in(0,4,5,10),(hex(a),c)
  put(entry,0x20001000+c*0x10);put(entry+8,0);returned(entry);return
 if address in(0x20001000,0x20001040,0x20001050,0x200010a0):
  ident=providers[a];slot=(address-0x20001000)//16
  if slot==0:result('goals,'+str(ident),(goal0,goal1,goal2)[ident])
  elif slot==4:result('max,'+str(ident),(v[14]+ident)&0xffffffff)
  elif slot==5:result('left,'+str(ident),(v[15]+ident)&0xffffffff)
  else:result('made,'+str(ident),(v[16]+ident)&0xffffffff)
  return
 if address==0x3212444:
  assert a==goals
  label='init,'+str((goal0,goal1,goal2).index(b))+','+str((limit0,limit1).index(c))+','+str(d)
  if step(label):reset=d;returned()
  return
 if address==0x1c22064:result('prepare,'+str(providers[a])+','+str(providers[b]));return
 if address==0x1eda130:
  assert a==moves
  if step('moves,'+str(si(b))+','+str(si(c))+','+str(si(d))):left=si(c);made=si(d);returned()
  return
 if address==0x320aee0:result('cells,'+str(grids[a])+','+str(providers[b]if b else-1));return
 if address in(0x320f87c,0x320f5a0):result(('static,'if address==0x320f87c else'items,')+str(grids[a])+','+str(providers[b])+','+str(c));return
 if address==0x23eaf58:assert a==creator;result('counts-update');return
 if address==0x23ead94:assert a==creator;result('counts-clear');return
 if address==0x3206ddc:assert a==level_context;result('late-clear');return
 if address==0x1c2543c:result('randomize,'+str(providers[a]));return
 if address==0x1ee6c20:
  if step('retry?'):returned(int(retries<v[12]));retries+=1
  return
 if address==0x218c74c:assert a==tutorial;result('tutorial,'+str(b));return
 if address==0x23df7bc:result('save?',feature if v[11]else 0);return
 if address==0x23e00a4:result('save');return
 if address==0x12dcda8:assert a==sorting;result('sorting');return
 if address==0x320b8d4:result('views,'+str(grids[a]));return
 if address==0x1d0a540:assert a==analytics;result('analytics,'+str(b));return
 if address==0x1e3b0dc:assert a==manager;result('log,'+str(si(b)));return
 raise AssertionError(hex(address))
hooks=(0xffd960,0xffd90c,0xffda64,0xffda88,0xffda90,0xffde20,0x1e38dc4,0x29e27cc,0x260dea4,0x32b6ed0,0x222dbdc,0x222fe98,0x21061a4,0x1f71a08,0x2116e6c,0x3d0cb30,0x3272c78,0x3272da0,0x23f557c,0x33fe6f4,0x1e1e394,0x1f881dc,0x1f88230,0x1f88268,0x1f881f8,0x2ae19c8,0x1ca91f0,0x1ca9228,0x25e5100,0x11de11c,0x11de0a4,0x3206e7c,0x23f1730,0x23df9e4,0x23e51c4,0x23e5500,0xfcd934,0x20001000,0x20001040,0x20001050,0x200010a0,0x3212444,0x1c22064,0x1eda130,0x320aee0,0x320f87c,0x320f5a0,0x23eaf58,0x23ead94,0x3206ddc,0x1c2543c,0x1ee6c20,0x218c74c,0x23df7bc,0x23e00a4,0x12dcda8,0x320b8d4,0x1d0a540,0x1e3b0dc)
for a in hooks:u.hook_add(UC_HOOK_CODE,hook,begin=a,end=a)
def call(address):
 global fault
 fault=False
 for i in range(8):u.reg_write(getattr(r,'UC_ARM64_REG_X'+str(i)),manager if i==0 else 0)
 u.reg_write(r.UC_ARM64_REG_SP,0x103ff000);u.reg_write(r.UC_ARM64_REG_LR,0x20000000);u.emu_start(address,0x20000000,count=100000)
 assert u.reg_read(r.UC_ARM64_REG_PC)==0x20000000,hex(u.reg_read(r.UC_ARM64_REG_PC))
 return si(x(0))
def native(values):
 global v,trace,calls,running,tries,lives,cloche,left,made,reset,restore_id,retries
 v=values;trace=[];calls=0;running=False;tries=100;lives=0;cloche=17;left=-111;made=-222;reset=-1;restore_id=0;retries=0
 for p in(manager,user,session,active,other_active,level,restore,restore2,statics,user_statics,provider_class):u.mem_write(p,bytes(0x300))
 put(statics,3 if v[0]in(1,3)else 1,4);put(manager+0x68,1,1)
 put(user_statics,0 if v[1]==1 else user);put(user+0x38,0 if v[1]==2 else session);put(session+0x18,0 if v[1]==3 else active)
 for p,story,table,admin,number in((active,v[5],v[6],v[7],v[13]),(other_active,-9,0,0,88)):
  put(p+0x10,admin,1);put(p+0x14,number,4);put(p+0x34,story,4);put(p+0x39,table,1)
 put(level+0x18,0 if v[1]==8 else ux);put(level+0x30,0 if v[1]==25 else parser);put(level+0x38,limit0);put(clash+0x48,0 if v[1]==11 else clash_helper)
 put(restore+0x20,0 if v[1]==24 else saved);put(restore2+0x20,0 if v[1]==24 else parser2)
 for p in providers:put(p,saved_class if p==saved else provider_class);put(p+0x15,int(v[12]!=0),1)
 put(provider_class+0x12c,1,1);put(provider_class+0xc8,hierarchy);put(hierarchy,read(parser_type_slot));put(factory+0x38,0 if v[1]==32 else creator)
 for offset,p,missing in((0x10,clock,18),(0x20,level_context,19),(0x30,factory,20),(0x38,grid,26),(0x50,life,12),(0x60,tutorial,27),(0x70,analytics,29),(0x80,feature,31),(0x88,sorting,28)):
  put(manager+offset,0 if v[1]==missing else p)
 value=call(0x1e38b40 if v[0]==0 else 0x1e38a30 if v[0]==1 else 0x1e3acd0)
 if v[0]>=2 and not fault:trace.append('result,'+str(value))
 return str(int(fault))+'#'+','.join(map(str,(read(statics,4),read(manager+0x68,1),int(running),tries,lives,cloche,left,made,reset)))+'#'+';'.join(trace)
cases=[]
for mode,story,table,admin,reset_flag,show,claim,save_flag in itertools.product(range(4),(0,-3),(0,1),(0,1),(0,1),(0,1),(0,1),(0,1)):
 cases.append([mode,0,0,0,0,story,table,admin,reset_flag,show,claim,save_flag,2,42,30,21,9])
for mode,missing in itertools.product(range(4),range(1,33)):
 for story in(0,-3):cases.append([mode,missing,0,0,0,story,0,0,1,1,1,1,-7,42,-90,12,8])
for mode,stage in itertools.product(range(4),range(1,58)):
 cases.append([mode,0,stage,0,0,-3 if stage%2 else 0,0,0,1,1,1,1,2,42,30,21,9])
for mode,stage,kind in itertools.product(range(4),range(1,58),range(1,15)):
 cases.append([mode,0,0,stage,kind,-3 if stage%2 else 0,0,0,1,1,1,1,2,42,30,21,9])
for mode,attempts in itertools.product(range(4),(0,1,2,498,499,500,501)):
 cases.append([mode,0,0,0,0,0,0,0,1,1,1,1,attempts,42,30,21,9])
rng=random.Random(0x1e3acd0)
for _ in range(400):
 cases.append([rng.randrange(4),rng.randrange(31),rng.randrange(58),rng.randrange(58),rng.randrange(11),rng.choice((0,-3)),rng.randrange(2),rng.randrange(2),rng.randrange(2),rng.randrange(2),rng.randrange(2),rng.randrange(2),si(rng.getrandbits(32)),si(rng.getrandbits(32)),si(rng.getrandbits(32)),si(rng.getrandbits(32)),si(rng.getrandbits(32))])
expected=[native(case)for case in cases]
mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
sources=['SourceCatalog/GameStartHarness.cs','Assets/Foundation/Runtime/OriginalGameStart.cs','Assets/Foundation/Runtime/OriginalGameInitialPopulation.cs','Assets/Foundation/Runtime/OriginalGameExitManager.cs','Assets/Foundation/Runtime/OriginalGridItemPopulationData.cs']
with tempfile.TemporaryDirectory()as directory:
 exe=Path(directory)/'Start.exe';command=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',*['-r:'+str(mono/'lib/mono/4.5'/p)for p in('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),*[str(PROJECT/p)for p in sources]]
 result=subprocess.run(command,capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(','.join(map(str,c))for c in cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 actual=result.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
 for i,(a,b)in enumerate(zip(actual,expected)):assert a==b,(i,cases[i],a,b)
report={'passed':True,'cases':len(cases),'scope':__doc__.strip(),'original_rvas':['0x1e38b40','0x1e38a30','0x1e3acd0','0x1e3bb98'],'files':[{'path':p,'sha256':hashlib.sha256((PROJECT/p).read_bytes()).hexdigest()}for p in sources+['SourceCatalog/verify_game_start.py']]}
(PROJECT/'game_start_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
