"""Native LevelTouchManager swap selection, guards and dispatch ordering."""
import sys
sys.dont_write_bytecode=True
import verify_board_layout as b
import json,subprocess,tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import *
p=b.p
manager,first,second,source,dest,grid,tutorial,event,actionklass,destklass,playerstatics,player,data,session=[0x10010000+i*0x1000 for i in range(14)]
CLEAR,CAN_TO,SWAP_EVENT=[0x20000100+i*4 for i in range(3)]
trace=[];fault=False;can_from=can_to=valid=disabled=exists=False
def resolve(slot):
 if slot not in b.resolved_slots:p.emu.reg_write(UC_ARM64_REG_X0,slot);b.boundary(p.emu,0xffd960,4,None)
 return p.read(slot,8)
def boundary(uc,address,size,data):
 global fault
 x0,x1,x2,x3=[uc.reg_read(r)for r in (UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_X3)];result=None
 if address==CLEAR:trace.append('clear:'+str(int(x0==second)));p.put(x0+0x10,0,1);result=0
 elif address==CAN_TO:trace.append('to');result=int(can_to)
 elif address==0x3207954:trace.append('get:'+str(p.signed(x1&0xffffffff))+':'+str(p.signed(x1>>32)));result=dest if exists else 0
 elif address==0x319ccec:trace.append('from');result=int(can_from)
 elif address==0x218ee9c:trace.append('tutorial:'+str(p.signed(x1&0xffffffff))+':'+str(p.signed(x1>>32)));result=int(disabled)
 elif address==0x1ee2558:trace.append('impossible:'+str(int(x0==second))+':'+str(p.signed(x2&0xffffffff))+':'+str(p.signed(x2>>32)));p.put(x0+0x10,1,1);result=0
 elif address==0x1ee2ad4:trace.append('start:'+str(int(x0==second)));p.put(x0+0x10,1,1);result=int(valid)
 elif address==0x32ba924:trace.append('false');result=0
 elif address==0x1ee2338:trace.append('useful');result=0
 elif address==SWAP_EVENT:trace.append('event:'+str(1 if x1==source else 0)+':'+str(2 if x2==dest else 0)+':'+str(x3&1));result=0
 elif address==0xffda88:fault=True;uc.reg_write(UC_ARM64_REG_PC,0x20000000);return
 if result is not None:uc.reg_write(UC_ARM64_REG_X0,result);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,boundary)
p.put(resolve(0x5595c00)+0xb8,playerstatics,8);p.put(playerstatics,player,8);p.put(player+0x38,data,8);p.put(data+0x18,session,8)
p.put(actionklass+0x178,CLEAR,8);p.put(destklass+0x228,CAN_TO,8);p.put(dest,destklass,8)
p.put(first,actionklass,8);p.put(second,actionklass,8);p.put(source+0x18,1|(5<<32),8)
p.put(manager+0x58,first,8);p.put(manager+0x60,second,8);p.put(manager+0x30,grid,8);p.put(manager+0x40,tutorial,8);p.put(event+0x18,SWAP_EVENT,8)
cases=[];expected=[]
for n in range(512):
 flags=[(n>>i)&1 for i in range(9)];busy0,busy1,can_from,exists,can_to,disabled,valid,bound,missing=map(bool,flags)
 p.put(first+0x10,int(busy0),1);p.put(second+0x10,int(busy1),1);p.put(manager+0x18,event if bound else 0,8);trace=[];fault=False
 for r,v in ((UC_ARM64_REG_X0,manager),(UC_ARM64_REG_X1,0 if missing else source),(UC_ARM64_REG_X2,2|(5<<32)),(UC_ARM64_REG_X3,0),(UC_ARM64_REG_SP,0x101ff000),(UC_ARM64_REG_LR,0x20000000)):p.emu.reg_write(r,v)
 p.emu.emu_start(0x1ee23c4,0x20000000,count=200000);assert p.emu.reg_read(UC_ARM64_REG_PC)==0x20000000
 ok=0 if fault else p.emu.reg_read(UC_ARM64_REG_X0)&1
 cases.append(','.join(map(str,flags)));expected.append(','.join(map(str,(ok,int(fault),p.read(first+0x10,1),p.read(second+0x10,1))))+';'+'|'.join(trace))
with tempfile.TemporaryDirectory() as folder:
 exe=Path(folder)/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
 result=subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(p.PROJECT/'SourceCatalog/LevelSwapRouterHarness.cs'),str(runtime/'OriginalLevelSwapRouter.cs'),str(runtime/'OriginalSwapActionState.cs')],capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 result=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 actual=result.stdout.lstrip('\ufeff').splitlines();assert actual==expected,next(((i,cases[i],a,e)for i,(a,e)in enumerate(zip(actual,expected))if a!=e),(len(actual),len(expected)))
report={'passed':True,'native_cases':len(cases),'scope':'Actual ARM64 LevelTouchManager.SwapCells and SelectSwap versus production router and action state. Exhausts both action slots, source eligibility, missing/blocked target, tutorial suppression, valid/wrong decision, required event and null-source branches. Compares clear-before-lookup/guard, impossible animation request, false-swipe/useful records, event arguments and final busy flags. Cell eligibility, decision/animation start and session/tutorial calls are supplied boundaries, already separately restored/tested where available. Impossible animation, full touch/tap presentation, concrete session services and the complete playable lifecycle remain pending.'}
(p.PROJECT/'level_swap_router_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
