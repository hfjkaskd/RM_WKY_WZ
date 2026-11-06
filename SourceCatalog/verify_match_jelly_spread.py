"""Compare native MatchGroup.InitJellySpreadData with its runtime helper."""
import sys
sys.dont_write_bytecode=True
import json,os,subprocess,tempfile
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_move_budget.py').read_text().split('cases=[];expected=[]')[0],str(PROJECT/'SourceCatalog/verify_move_budget.py'),'exec'))
emu.hook_del(native_hook);base_hook=hook
group,data,level=0x10020000,0x10021000,0x10022000
cells=[0x10030000+i*0x100 for i in range(14)];head=[];calls=0
def record(s):
 global calls
 trace.append(s);calls+=1
 if calls==head[4]:fail(emu);return False
 return True
def hook(uc,address,size,unused):
 x0,x1=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(2)]
 if address==0x29e27cc:assert x0==2;returned(uc,level)
 elif address==0x1e19780:assert x1==3;returned(uc,head[0])
 elif address==0x12de610:returned(uc,0 if x1==head[5]else cells[x1])
 elif address==0x319df48:
  i=cells.index(x0)
  if record('view,'+str(i)):returned(uc,bool(head[3]&(1<<i)))
 elif address==0x12dfcf8:
  if record('spread,'+str(head[2])):returned(uc)
 else:base_hook(uc,address,size,unused)
for a in [0xffd960,0xffd90c,0xffda64,0xffda88,0xffda90,0xffda50,0x29e27cc,0x1e19780,0x12de610,0x319df48,0x12dfcf8]:emu.hook_add(UC_HOOK_CODE,hook,begin=a,end=a)
cases=[];expected=[]
def run(h):
 global head,calls
 head=h;calls=0;trace.clear();put(group+8,h[2]);put(data+0x14,h[1],1)
 answer=call(0x12dfba8,group,data)
 expected.append(('error'if fault else str(answer))+':'+str(read(data+0x14,1))+':'+'/'.join(trace));cases.append(','.join(map(str,h)))
for present in (0,1):
 for initial in (0,1):
  for count in range(15):
   for mask in (0,1,0x1555,0x3fff):run([present,initial,count,mask,0,-1])
for failure in range(1,17):run([1,0,14,0x1555,failure,-1]);run([1,1,14,0,failure,-1])
for null in range(14):run([1,0,14,0x3fff,0,null])
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-match-jelly-')as folder:
 exe=Path(folder)/'verify.exe';runtime=PROJECT/'Assets/Foundation/Runtime'
 p=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-langversion:latest','-out:'+str(exe),str(PROJECT/'SourceCatalog/MatchJellySpreadHarness.cs'),str(runtime/'OriginalMatchGroupJellySpread.cs'),str(runtime/'OriginalExplodeData.cs')],capture_output=True,text=True,encoding='utf-8-sig');assert p.returncode==0,p.stdout+p.stderr
 p=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8-sig');assert p.returncode==0,p.stderr
 actual=p.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected)
 for i,(a,e)in enumerate(zip(actual,expected)):assert a==e,(i,cases[i],a,e)
report={'passed':True,'native_cases':len(cases),'scope':'Actual ARM64 MatchGroup.InitJellySpreadData: absent-level gate, full ordered0..14-cell scan even after a Jelly hit, incoming spread flag, final SetSpreadData dispatch, null cells and partial ExplodeData mutation on injected callback failures. Level lookup/presence, Cell14 storage, HasJellyView and SetSpreadData are supplied boundaries; concrete Jelly view/model spreading remains required and is not proven by this test.'}
(PROJECT/'match_jelly_spread_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
