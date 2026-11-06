"""Native LevelRandomManager startup, cache invalidation and saved cursor."""
import sys
sys.dont_write_bytecode=True
import base64,json,os,random,struct,subprocess,tempfile,unicodedata
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_move_budget.py').read_text().split('cases=[];expected=[]')[0],str(PROJECT/'SourceCatalog/verify_move_budget.py'),'exec'))
emu.hook_del(native_hook);base_hook=hook;metadata_next=0x101a0000
owner=0x10020000
user_class,user_static,user,session,active,user_data=0x10030000,0x10030400,0x10031000,0x10032000,0x10033000,0x10034000
app,user_events,save,save_data,save_helper=0x10035000,0x10035400,0x10036000,0x10036400,0x10036800
random_class,generic,rgctx=0x10038000,0x10039000,0x10039400
count_call,item_call,set_call=0x1003c000,0x1003c100,0x1003c200
for got,klass in [(0x53bb4e0,user_class),(0x53d1518,random_class)]:
 put(read(got,8),klass,8);put(klass+0xe0,1);put(klass+0x132,1,1)
put(user_class+0xb8,user_static,8)
for offset,rva in [(0x178,0x2616700),(0x198,0x2616824),(0x1a8,0x2616940)]:put(random_class+offset,rva,8)
put(read(0x53b9db0,8),generic,8);put(generic+0x38,rgctx,8)
for i,rva in enumerate((count_call,item_call,set_call)):
 descriptor=0x10039800+i*0x40;put(rgctx+8+i*8,descriptor,8);put(descriptor+8,rva,8)
trace=[];handlers=[];actions={};calls=0;failure=0;heap=0;env=[];parser_mode=False;device_string=None
def alloc(size=0x100):
 global heap
 p=heap;heap=(heap+size+15)&~15;assert heap<0x10180000;emu.mem_write(p,bytes(size));return p
def text_object(value):
 if value is None:return 0
 raw=value.encode('utf-16-le','surrogatepass');p=alloc(0x20+len(raw));put(p+0x10,len(raw)//2);emu.mem_write(p+0x14,raw);return p
def text_value(p):return bytes(emu.mem_read(p+0x14,read(p+0x10)*2)).decode('utf-16-le','surrogatepass')if p else None
def record(value):
 global calls
 trace.append(value);calls+=1
 if calls==failure:fail(emu);return False
 return True
def refresh():
 flags,level,uid,device,attempt,appseed,savedseed,savedcount,state=env
 put(user_static,user if flags&2 else 0,8);put(user+0x38,session if flags&4 else 0,8)
 put(session+0x18,active if flags&8 else 0,8);put(active+0x14,level);put(active+0x3c,state)
 put(user+0x10,user_data if flags&16 else 0,8);put(user_data+0x10,uid,8)
 put(save+0x20,save_data if flags&128 else 0,8);put(save_data+0x30,save_helper if flags&256 else 0,8)
def hook(uc,address,size,unused):
 x0,x1,x2=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(3)]
 if address==0xffda78:
  p=alloc();put(p,x0,8);returned(uc,p)
 elif address==0xffd9d0:
  p=alloc(0x20+x1*8);put(p+0x18,x1);returned(uc,p)
 elif address==0x231007c:returned(uc)
 elif address==0x1e6868c:
  p=alloc(0x20+x1*2);put(p+0x18,x1);put(x0+0x10,p,8);returned(uc)
 elif address==0x30f6400:
  assert x0==5
  if record('app'):returned(uc,app if env[0]&1 else 0)
 elif address==0x2d70ac4:
  if record('appnext,'+str(signed(x1))+','+str(signed(x2))):returned(uc,env[5])
 elif address==0x1ed91e8:record('session')
 elif address==0x1ed95bc:record('level')
 elif address==0x1ed9730:record('userid')
 elif address==0x1ed8f28 and not parser_mode:
  if record('device'):returned(uc,env[3])
 elif address==0x25e50d8:
  if record('attempt'):returned(uc,env[4])
 elif address==0x29e27cc:
  assert x0==1
  if record('user'):returned(uc,user_events if env[0]&32 else 0)
 elif address==0x2044a78:actions[x0]=x1;returned(uc)
 elif address in(0x2ae74bc,0x2ae2d24):
  remove=address==0x2ae74bc
  if record('remove'if remove else'add'):
   target=actions[x1]
   if remove:
    for i in range(len(handlers)-1,-1,-1):
     if handlers[i]==target:handlers.pop(i);break
   else:handlers.append(target)
   returned(uc)
 elif address==0x1ed9a10:record('win')
 elif address==0x23df9e4:
  if record('saved'):returned(uc,save if env[0]&64 else 0)
 elif address in(0x23e6304,0x23e6318):
  seed=address==0x23e6304
  if record('savedseed'if seed else'savedcount'):returned(uc,env[6 if seed else 7])
 elif address==0x1e68b8c or address==item_call:
  i=signed(x1);count=read(x0+0x18)
  if not 0<=i<count:fail(uc)
  else:returned(uc,read(read(x0+0x10,8)+0x20+i*2,2))
 elif address==count_call:returned(uc,read(x0+0x18))
 elif address==set_call:
  i=signed(x1);count=read(x0+0x18)
  if not 0<=i<count:fail(uc)
  else:put(read(x0+0x10,8)+0x20+i*2,x2,2);returned(uc)
 elif address==0x1ed9398:uc.reg_write(UC_ARM64_REG_PC,0x1ed9414) # Post-selection diagnostic allocation/logging only.
 elif address==0x1ed9778:
  # The omitted diagnostic block also loads the later shuffle's generic
  # descriptor. Preserve that load while omitting only boxing/log transport.
  uc.reg_write(UC_ARM64_REG_X0+23,read(0x53b9db0,8));uc.reg_write(UC_ARM64_REG_PC,0x1ed97fc)
 elif address==0x26168ac:fail(uc) # CLR invalid-range exception transport.
 elif address==0x2d7922c:returned(uc,text_object(device_string))
 elif address==0x1d6cc9c:
  if not x0 or x1>=read(x0+0x10):fail(uc)
  else:returned(uc,read(x0+0x14+x1*2,2))
 elif address==0x2044854:returned(uc,unicodedata.category(chr(x0))=='Nd')
 elif address==0x203abec:returned(uc,text_object(chr(read(x0,2))))
 elif address==0x1d6812c:returned(uc,text_object((text_value(x0)or'')+(text_value(x1)or'')))
 elif address==0x26540e0:
  value=text_value(x0)
  if not value or not value.isascii()or not value.isdecimal():fail(uc)
  else:returned(uc,int(value))
 else:base_hook(uc,address,size,unused)
addresses=[0xffda78,0xffd9d0,0x231007c,0x1e6868c,0x30f6400,0x2d70ac4,0x1ed91e8,0x1ed95bc,0x1ed9730,0x1ed8f28,0x25e50d8,0x29e27cc,0x2044a78,0x2ae74bc,0x2ae2d24,0x1ed9a10,0x23df9e4,0x23e6304,0x23e6318,0x1e68b8c,count_call,item_call,set_call,0x1ed9398,0x1ed9778,0x26168ac,0x2d7922c,0x1d6cc9c,0x2044854,0x203abec,0x1d6812c,0x26540e0,0xffd960,0xffd90c,0xffda64,0xffda88,0xffda90,0xffda50]
for a in addresses:emu.hook_add(UC_HOOK_CODE,hook,begin=a,end=a)
cases=[];expected=[];commands=0;parser_cases=0
def snapshot():
 lst=read(owner+0x10,8);count=read(lst+0x18);raw=bytes(emu.mem_read(read(lst+0x10,8)+0x20,count*2));values=struct.unpack('<'+'H'*count,raw)
 checksum=sum((i+1)*v for i,v in enumerate(values))&0xffffffff
 return ','.join(map(str,[signed(read(owner+0x24)),signed(read(owner+0x28)),read(owner+0x20,1),signed(read(owner+0x2c)),count,signed(checksum),read(owner+0x18,1),read(owner+0x48,1),read(owner+0x40,1),signed(read(owner+0x44)),len(handlers)]))
def run(head,rows):
 global heap,env,calls,failure,commands,parser_mode,device_string,parser_cases
 heap=0x10060000;env=list(head);handlers.clear();actions.clear();emu.mem_write(owner,bytes(0x100));refresh()
 put(read(0x5358570,8),text_object(''),8);failure=0;calls=0;trace.clear();parser_mode=False
 call(0x1ed9b6c,owner);assert not fault
 output=[]
 for mode,a,b,failure in rows:
  commands+=1;calls=0;trace.clear();result='-';failed=False;parser_mode=mode==13
  if mode in(0,1,3,4,6,7,8,9,11,12,13,14):
   pc={0:0x1ed9160,1:0x1ed9054,3:0x1ed9a10,4:0x1ed9a8c,6:0x1ed98c8,7:0x1ed9438,8:0x1ed9458,9:0x1ed8f04,11:0x1ed9840,12:0x1ed92b4,13:0x1ed8f28,14:0x1ed904c}[mode]
   if mode==13:device_string=b;parser_cases+=1
   answer=call(pc,owner,a,0 if mode==13 else b);failed=fault
   if mode in(6,7,8,9,13,14)and not failed:result=str(answer)
  elif mode==2:
   for p in list(handlers):call(0x1ed9a00,p);assert not fault
  elif mode==5:put(owner+0x18,bool(a&1),1);put(owner+0x48,bool(a&2),1);put(owner+0x40,bool(a&4),1);put(owner+0x44,b)
  elif mode==10:env[a]=b;refresh()
  if failed:result='error'
  output.append(result+':'+snapshot()+':'+'/'.join(trace))
 def encode(row):
  mode,a,b,f=row
  if mode==13:b='~'if b is None else base64.b64encode(b.encode()).decode()
  return ','.join(map(str,[mode,a,b,f]))
 cases.append(','.join(map(str,head))+'#'+';'.join(encode(r)for r in rows));expected.append('|'.join(output))
head=[511,1,4294967297,12345,0,371,999,17,2]
run([511,1,1,12345,362,371,999,0,2],[[0,0,0,0],[14,0,0,0],[1,0,0,0],[6,0,6,0],[7,0,100,0]])
for flags_value in [0,1,3,7,15,31,63,127,255,511,509,507,503,495,479,447,383,255]:
 for failure_value in range(10):
  h=head.copy();h[0]=flags_value
  run(h,[[5,7,47,0],[1,0,0,failure_value],[1,0,0,0],[0,0,0,failure_value],[7,0,100,0],[6,0,6,0],[8,0,0,0],[9,0,0,0],[4,0,0,failure_value],[6,0,100,0],[10,8,3,0],[3,1,0,failure_value],[2,0,0,0],[0,0,0,0],[6,0,6,0]])
for level,uid,device in [(1,1,0),(15253,4294967297,99999),(2147483647,2147483647,9),(-2147483648,-2147483648,-17)]:
 for attempt in [-2147483648,-1001,-1,0,1,999,1000,2147483647]:
  h=head.copy();h[1:5]=[level,uid,device,attempt]
  run(h,[[9,0,0,0],[0,0,0,0],[6,0,6,0],[10,1,153,0],[10,3,99871,0],[0,0,0,0],[7,-10,10,0],[6,5,5,0],[1,0,0,0],[2,0,0,0],[0,0,0,0],[6,0,100,0]])
for seed in [0,1,-1,371,2147483647,-2147483648]:
 for count in [-2147483648,-3,0,1,2,17,56,123]:
  h=head.copy();h[6:8]=[seed,count]
  run(h,[[0,0,0,0],[7,0,100,0],[4,0,0,0],*[[6,-100,100,0]for _ in range(8)],[7,0,100,0],[8,0,0,0],[3,0,0,0]])
texts=[None,'','abc','0','abc123456xyz','ab1-c2_d3e4f5g6','0000000','a9b8c7d6e5f4','１２３４５','١٢٣٤٥','1٢3','😀a12b34c56','𝟙𝟚𝟛','1²2³3']
for value in texts:run(head,[[13,0,value,0]])
rng=random.Random(0x1ed9160)
for i in range(75):
 h=head.copy();h[1]=rng.randrange(20000);h[2]=rng.randrange(2**40);h[3]=rng.choice([0,1,12345]);h[4]=rng.randrange(1004);h[6]=rng.randrange(-999,999);h[7]=rng.randrange(-2,30);rows=[]
 for j in range(20):
  mode=rng.randrange(15)
  if mode==13:rows.append([mode,0,rng.choice(texts),0]);continue
  if mode in(6,7):a,b=rng.choice([(0,6),(-100,100),(5,5),(-2147483648,2147483647),(6,5)])
  elif mode==10:a=rng.randrange(9);b=rng.randrange(-2,32)if a==7 else rng.choice([0,1,2,3,511,999])
  elif mode==11:a,b=rng.randrange(-999,999),0
  else:a,b=rng.randrange(8),rng.randrange(-99,99)
  rows.append([mode,a,b,rng.randrange(10)])
 run(h,rows)
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-level-random-')as folder:
 exe=Path(folder)/'verify.exe';sources=['SourceCatalog/LevelRandomLifecycleHarness.cs',*[f'Assets/Foundation/Runtime/{n}.cs'for n in('OriginalLevelRandomManager','OriginalLevelSeedRandom','OriginalLevelRandomSource','OriginalAttemptSeeds','OriginalMatchItemRandomizer','OriginalLevelRules','OriginalLevelContext','OriginalContextContainer','OriginalExplodeData')]]
 result=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-langversion:latest','-out:'+str(exe),*[str(PROJECT/s)for s in sources]],capture_output=True,text=True,encoding='utf-8-sig');assert result.returncode==0,result.stdout+result.stderr
 result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8-sig');assert result.returncode==0,result.stdout+result.stderr
 actual=result.stdout.splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
 for i,(a,e)in enumerate(zip(actual,expected)):assert a==e,(i,cases[i],a,e)
report={'passed':True,'native_sequences':len(cases),'native_commands':commands,'device_parser_cases':parser_cases,'scope':'Actual ARM64 LevelRandomManager ctor/Bind/ResetSeed/SetSeed/ResetDifficultySeed/GenerateTrySeeds/DeviceIdKey/device digit parser/ClearUserCaches/Clear/RestoreFromSaveData/Next/NextBool/DifficultyNext plus actual System.Random ctor/sample/range arithmetic and ushort difficulty shuffle. Compares independent general/difficulty streams, 1000-entry attempt permutation checksum, lazy device caching, generated flag before persistence read, cache retention across level changes/loss and reset on user change/win, id/admin/remote/switch state, remove-before-add subscription, saved-count Next(1) replay including negative counts, and partial state after injected provider failures. CLR metadata/allocation/list/delegate/string/char/integer parsing, Unity application random, current user/session/save/storage and SDK device string are supplied boundaries. Diagnostic boxing/logging omitted. Actual user/storage/save providers, remaining ordinary shuffle specializations and complete application/player startup remain pending.'}
(PROJECT/'level_random_lifecycle_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
