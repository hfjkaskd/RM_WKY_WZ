"""ARM64 match-collection gates, session forwarding and partial mutation."""
import sys
sys.dont_write_bytecode=True
import json,os,random,subprocess,tempfile,struct
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_move_budget.py').read_text().split('cases=[];expected=[]')[0],str(PROJECT/'SourceCatalog/verify_move_budget.py'),'exec'))
emu.hook_del(native_hook);base_hook=hook
model,animation,daily,collect_manager,factory,view,dictionary,created=0x10020000,0x10021000,0x10022000,0x10023000,0x10024000,0x10025000,0x10026000,0x10027000
head=[];calls=0;counts={}
def record(s):
 global calls
 trace.append(s);calls+=1
 if calls==head[1]:fail(emu);return False
 return True
def hook(uc,address,size,unused):
 x0,x1,x2=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(3)]
 if address==0x32b2f8c:
  if record('valid'):returned(uc,head[5])
 elif address==0x32b2eb4:
  if record('manager'):returned(uc,0 if head[0]&128 else collect_manager)
 elif address==0x338da00:
  if record('daily'):returned(uc,head[12])
 elif address==0x32b2f1c:
  if record('factory'):returned(uc,0 if head[0]&256 else factory)
 elif address==0x260a220:
  assert x1==107 and x2==1
  if record('spawn,107,1'):returned(uc,0 if head[0]&512 else view)
 elif address==0x3e4d92c:
  if record('missing'):returned(uc,bool(x0)and not(head[0]&1024))
 elif address==0x18cd858:
  pos=[int(struct.unpack('<f',struct.pack('<I',uc.reg_read(UC_ARM64_REG_S0+i)))[0]*4)for i in range(3)]
  if record('anim,'+','.join(map(str,[*pos,signed(x1&0xffffffff),signed(x2&0xffffffff),int(uc.reg_read(UC_ARM64_REG_X3)!=0)]))):returned(uc)
 elif address==0x39b08ec:
  key=signed(x1&0xffffffff);put(x2,counts.get(key,0));returned(uc,key in counts)
 elif address==0x39aee48:counts[signed(x1&0xffffffff)]=signed(x2&0xffffffff);returned(uc)
 else:base_hook(uc,address,size,unused)
for a in [0xffd960,0xffd90c,0xffda64,0xffda88,0xffda90,0xffda50,0x32b2f8c,0x32b2eb4,0x338da00,0x32b2f1c,0x260a220,0x3e4d92c,0x18cd858,0x39b08ec,0x39aee48]:emu.hook_add(UC_HOOK_CODE,hook,begin=a,end=a)
cases=[];expected=[]
def run(h):
 global head,calls,counts
 head=h;counts={};put(model+0x20,h[4],1);put(model+0x24,h[2]);put(model+0x28,h[3]);put(model+0x30,dictionary if h[14]else 0,8)
 put(collect_manager+0x1c,h[6]);put(collect_manager+0x22,h[13],1)
 put(user_static,0 if h[0]&4 else user,8);put(user+0x38,0 if h[0]&8 else session,8);put(session+0x18,0 if h[0]&16 else active,8)
 put(active+0x3c,h[10]);put(active+0x30,h[9],1);put(active+0x88,0 if h[0]&1 else animation,8);put(animation+0x40,0 if h[0]&2 else model,8)
 # Daily lookup follows the current user's active animation independently.
 if h[0]&32:put(active+0x88,0,8)
 put(animation+0x88,0 if h[0]&64 else daily,8)
 out=[]
 for mode in ['t','a','g','i','d']:
  trace.clear();calls=0
  for i,p in enumerate([-1.25,2.5,.75]):emu.reg_write(UC_ARM64_REG_S0+i,struct.unpack('<I',struct.pack('<f',p))[0])
  if mode=='t':answer=call(0x32b9ab0,active,h[7],h[8],17,created,h[11])
  elif mode=='a':answer=call(0x32b370c,model,h[7],h[8],h[9],17,created)
  elif mode=='g':answer=call(0x32b2f60,model)
  elif mode=='i':answer=call(0x32b9bbc,active,h[7],31)
  else:answer=call(0x32b393c,model,h[7],31)
  result='error'if fault else str(answer)if mode=='g'else'ok'
  out.append(result+':'+str(signed(read(model+0x28)))+','+str(read(model+0x20,1))+','+str(counts.get(31,0))+':'+'/'.join(trace))
 cases.append(','.join(map(str,h)));expected.append('|'.join(out))
# Bit32 represents a missing current animation, including caller lookup.
for flags in [0,1,2,4,8,16,32,64,128,256,512,1024]:
 for state in [0,2,4]:
  for mode in range(8):run([flags,0,9,2147483646,0,mode&1,2,0 if mode&2 else 2,-1 if mode&4 else 3,3,state,mode&1,(mode>>1)&1,(mode>>2)&1,1])
for failure in range(1,10):
 for difficulty in [-1,0,1,2,3,4]:run([0,failure,9,-10,1,1,2,2,3,difficulty,2,0,0,0,1])
rng=random.Random(0x32b370c)
for _ in range(160):run([0,0,9,rng.randrange(-100,100),rng.randrange(2),rng.randrange(2),rng.randrange(1,4),rng.randrange(4),rng.choice([0,-1,1,2147483647]),rng.randrange(-1,5),rng.choice([0,2,4]),rng.randrange(2),rng.randrange(2),rng.randrange(2),rng.randrange(2)])
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-madness-match-')as folder:
 exe=Path(folder)/'verify.exe';runtime=PROJECT/'Assets/Foundation/Runtime'
 p=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-langversion:latest','-out:'+str(exe),str(PROJECT/'SourceCatalog/MadnessMatchCollectionHarness.cs'),str(runtime/'OriginalMadnessMatchCollection.cs'),str(runtime/'OriginalSessionValues.cs')],capture_output=True,text=True,encoding='utf-8-sig');assert p.returncode==0,p.stdout+p.stderr
 p=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8-sig');assert p.returncode==0,p.stderr
 actual=p.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected)
 for i,(a,e)in enumerate(zip(actual,expected)):assert a==e,(i,cases[i],a,e)
report={'passed':True,'native_sequences':len(cases),'native_commands':len(cases)*5,'scope':'Actual ARM64 ActiveLevel.TryCollectMadness/IncrementMadnessMatchItemCount, MadnessSessionData.TryAddMadnessMatch/MatchTypeToCollect/TryIncrementMadnessForExploderId, real difficulty-factor and BeforeAfter.AddExtra bodies. Compares gates, repeated event/manager reads, count overflow/partial mutation, animation suppression/null/missing and position/count/delay forwarding, dictionary increments and injected boundary failures. IsEventValid (separately verified), manager/current-session/CLR/pool/Unity-object and animation APIs are supplied boundaries. Full event eligibility, sticker visual and application-owned session composition remain separate.'}
(PROJECT/'madness_match_collection_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
