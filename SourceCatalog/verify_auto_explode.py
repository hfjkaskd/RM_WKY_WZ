"""Actual ARM64 AutoExplodeManager gates, retries and group-loop ordering."""
import sys
sys.dont_write_bytecode=True
import json,os,random,struct,subprocess,tempfile
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_move_budget.py').read_text().split('cases=[];expected=[]')[0],str(PROJECT/'SourceCatalog/verify_move_budget.py'),'exec'))
emu.hook_del(native_hook);base_hook=hook
owner,state,matches,groups=[0x10020000+i*0x1000 for i in range(4)]
row=[];calls=0;exploder_id=0
bits=lambda f:struct.unpack('<I',struct.pack('<f',f))[0]
def record(value):
 global calls
 trace.append(value);calls+=1
 if calls==row[7]:fail(emu);return False
 return True
def hook(uc,address,size,unused):
 global exploder_id
 x0,x1,x2,x3=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(4)]
 if address==0x1eda1e8:
  if record('op,'+str(x1)):returned(uc,row[3])
 elif address==0x1edb430:
  if record('any'):returned(uc,row[4])
 elif address==0x3205d7c:
  if record('frame'):returned(uc,row[1])
 elif address==0x12e1038:
  if record('find'):put(groups+0x18,row[5]);returned(uc,0 if row[2]&4 else groups)
 elif address==0x2eebc08:
  if record('next,'+str(x0)):
   exploder_id=signed((exploder_id+1)&0xffffffff);p=uc.reg_read(UC_ARM64_REG_X8);emu.mem_write(p,bytes(56));put(p+8,x0);put(p+12,exploder_id);returned(uc)
 elif address==0x284dfd0:
  if record('get,'+str(x1)):
   p=uc.reg_read(UC_ARM64_REG_X8);emu.mem_write(p,bytes(152));put(p,x1+10);returned(uc)
 elif address==0x12e33dc:
  index=read(x1)-10
  if record(','.join(map(str,('explode',index,read(x2+8),signed(read(x2+12)),x3)))):
   if index==0:
    if row[8]==1:put(owner+0x2c,0,1)
    elif row[8]==2:put(owner+0x28,0);put(owner+0x10,0,1);put(owner+0x2c,1,1)
    elif row[8]==3:put(owner+0x20,0,8)
    elif row[8]==4:put(owner+0x18,0,8)
   returned(uc,bool(row[6]&(1<<index)))
 elif address==0xd00b50:emu.mem_write(x0,bytes(emu.mem_read(x1,x2)));returned(uc,x0)
 elif address==0xd01fa0:emu.mem_write(x0,bytes([x1&255])*x2);returned(uc,x0)
 else:base_hook(uc,address,size,unused)
for a in [0xffd960,0xffd90c,0xffda64,0xffda88,0xffda90,0xffda50,0x1eda1e8,0x1edb430,0x3205d7c,0x12e1038,0x2eebc08,0x284dfd0,0x12e33dc,0xd00b50,0xd01fa0]:emu.hook_add(UC_HOOK_CODE,hook,begin=a,end=a)
cases=[];expected=[];commands=0
def run(initial,ident,rows):
 global row,calls,exploder_id,commands
 emu.mem_write(owner,bytes(64));put(owner+0x28,initial);put(owner+0x2c,1,1);exploder_id=ident;output=[]
 for row in rows:
  commands+=1;trace.clear();calls=0;put(owner+0x20,0 if row[2]&1 else state,8);put(owner+0x18,0 if row[2]&2 else matches,8)
  call([0x1c938a0,0x1c93898,0x1c93af0][row[0]],owner,row[1])
  output.append(('error'if fault else'-')+','+','.join((str(read(owner+0x10,1)),str(read(owner+0x2c,1)),format(read(owner+0x28),'08x'),str(exploder_id)))+':'+ ';'.join(trace))
 cases.append(format(initial,'08x')+','+str(ident)+'#'+';'.join(','.join(map(str,r))for r in rows));expected.append('|'.join(output))
for initial in [bits(v)for v in(-1,0,1,10,float('nan'),float('inf'),float('-inf'))]:
 for flags in range(8):
  for operations in range(4):
   run(initial,0,[[0,frame,flags,operations&1,operations>>1,3,5,0,0]for frame in (0,1,9,10,11,12,16777217,2147483647,-2147483648)])
for failure in range(1,20):
 for reentry in range(5):run(bits(-1),2147483646,[[0,4,0,0,0,3,5,failure,reentry],[0,5,0,1,0,0,0,0,0],[1,0,0,0,0,0,0,0,0],[0,6,0,0,0,2,3,0,0],[2,1,0,0,0,0,0,0,0],[0,7,0,0,0,2,3,0,0]])
rng=random.Random(0x1c938a0)
for i in range(180):
 rows=[]
 for j in range(30):rows.append([rng.choices([0,1,2],[15,1,2])[0],rng.choice([-2147483648,-1,0,1,9,10,11,50,16777217,2147483647]),rng.randrange(8),rng.randrange(2),rng.randrange(2),rng.randrange(5),rng.randrange(16),rng.randrange(14),rng.randrange(5)])
 run(rng.choice([bits(-1),bits(0),bits(1),bits(float('nan'))]),rng.choice([0,2147483647]),rows)
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-auto-explode-')as folder:
 exe=Path(folder)/'verify.exe'
 result=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-langversion:latest','-out:'+str(exe),str(PROJECT/'SourceCatalog/AutoExplodeHarness.cs'),str(PROJECT/'Assets/Foundation/Runtime/OriginalAutoExplodeManager.cs')],capture_output=True,text=True,encoding='utf-8-sig');assert result.returncode==0,result.stdout+result.stderr
 result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8-sig');assert result.returncode==0,result.stdout+result.stderr
 actual=result.stdout.splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
 for i,(a,e)in enumerate(zip(actual,expected)):assert a==e,(i,cases[i],a,e)
report={'passed':True,'native_sequences':len(cases),'native_commands':commands,'scope':'Actual ARM64 AutoExplodeManager ManualUpdate/DestroyAllMatches/Disable/Clear. Compares ordered operation6/any/frame/find/Next4/value-snapshot/explode callbacks, ten-frame float gate including large signed frames and NaN/infinity, retained waiting state on disable/op6, retry on false result, per-group ids and value copies, mixed results, reentrant disable/clear/rebind and partial mutations on injected failures. CLR metadata/memcpy/list storage, GameState operation queries, frame getter, Exploder.Next and match search/explosion are supplied boundaries; concrete board integration is verified separately. Full original context-container lifecycle/update registration remains pending.'}
(PROJECT/'auto_explode_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
