"""Native level/cell press and cancellation against production typed dispatch."""
import sys
sys.dont_write_bytecode=True
import verify_board_layout as b
import json,subprocess,tempfile,itertools
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import *
p=b.p
manager,cell,statics,mediation,above_list,below_list,above_array,below_array,current,previous,matchklass,baseklass,matchhier,basehier,view,oldview,booster,static0,static1,static10,static11,staticklass,first,second,actionklass=[0x10010000+i*0x1000 for i in range(25)]
STATIC,INVALID,CLEAR=[0x20000100+i*4 for i in range(3)]
trace=[];kind=0;enabled=block=selected=False;above=below=0
def resolve(slot):
 if slot not in b.resolved_slots:p.emu.reg_write(UC_ARM64_REG_X0,slot);b.boundary(p.emu,0xffd960,4,None)
 return p.read(slot,8)
def boundary(uc,address,size,data):
 x0,x1=[uc.reg_read(r)for r in (UC_ARM64_REG_X0,UC_ARM64_REG_X1)];value=None
 if address==0x319c9c4:value=int(enabled)
 elif address==0x3440db8:value=int(block and above>0)
 elif address==0x343bd5c:trace.append('item');value=current if kind else 0
 elif address==0x2c1b9a0:
  array=p.read(x0+0x10,8);value=p.read(array+0x20+x1*8,8)
 elif address==0x260dea4:value=booster
 elif address==0x1e35b24:trace.append('booster');value=int(selected)
 elif address==0x21a6ae8:trace.append('highlight:'+str(int(x0==oldview))+':'+str(x1&1));value=0
 elif address in (STATIC,INVALID):trace.append(('static:' if address==STATIC else 'invalid:')+str({static0:0,static1:1,static10:10,static11:11}[x0]));value=1
 elif address==CLEAR:value=0
 elif address==0xffda88:raise AssertionError('Unexpected native null boundary')
 if value is not None:uc.reg_write(UC_ARM64_REG_X0,value);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,boundary)
def call(rva,*args):
 for i,v in enumerate((*args,0,0,0)):p.emu.reg_write(UC_ARM64_REG_X0+i,v&0xffffffffffffffff)
 p.emu.reg_write(UC_ARM64_REG_SP,0x101ff000);p.emu.reg_write(UC_ARM64_REG_LR,0x20000000)
 p.emu.emu_start(rva,0x20000000,count=200000);assert p.emu.reg_read(UC_ARM64_REG_PC)==0x20000000
 return p.emu.reg_read(UC_ARM64_REG_X0)&0xffffffff
p.put(cell+0x10,mediation,8);p.put(cell+0xc0,statics,8);p.put(statics+0x30,above_list,8);p.put(statics+0x38,below_list,8)
p.put(above_list+0x10,above_array,8);p.put(below_list+0x10,below_array,8)
for array,items in ((above_array,(static0,static1)),(below_array,(static10,static11))):
 for i,item in enumerate(items):p.put(array+0x20+i*8,item,8)
for item in (static0,static1,static10,static11):p.put(item,staticklass,8)
p.put(staticklass+0x3f8,STATIC,8);p.put(staticklass+0x3e8,INVALID,8)
native_match_class=resolve(0x55902a0);p.put(native_match_class+0x12c,1,1)
for klass,hierarchy,parent in ((matchklass,matchhier,native_match_class),(baseklass,basehier,baseklass)):
 p.put(klass+0x12c,1,1);p.put(klass+0xc8,hierarchy,8);p.put(hierarchy,parent,8)
 p.put(klass+0x798,0x21a6abc if klass==matchklass else 0x2b47acc,8);p.put(klass+0x7a8,0x21a6b58 if klass==matchklass else 0x2b47ad4,8)
p.put(previous,matchklass,8);p.put(previous+0xb0,oldview,8);p.put(current+0xb0,view,8)
p.put(manager+0x58,first,8);p.put(manager+0x60,second,8);p.put(first,actionklass,8);p.put(second,actionklass,8);p.put(actionklass+0x178,CLEAR,8)
cases=[];expected=[]
for op in ('L','D','I','M','K'):
 for above,below,kind,enabled,block,selected,prior in itertools.product(range(3),range(3),range(3),(False,True),(False,True),(False,True),(False,True)):
  p.put(above_list+0x18,above);p.put(below_list+0x18,below);p.put(current,matchklass if kind==1 else baseklass,8);p.put(manager+0x50,previous if prior else 0,8);trace=[];result=''
  if op=='L':call(0x1ee1df0,manager,cell)
  elif op=='D':call(0x319d290,cell)
  elif op=='I':call(0x344108c,statics)
  elif op=='M':result=str(call(0x319d340,cell)&1)
  elif op=='K':call(0x1ee1d58,manager,0)
  touched=p.read(manager+0x50,8);before=0 if touched==0 else 2 if touched==previous else 1
  call(0x1ee1ebc,manager);call(0x1ee1ebc,manager)
  cases.append(','.join(map(str,(op,above,below,kind,int(enabled),int(block),int(selected),int(prior)))))
  expected.append('|'.join(trace)+'#'+','.join(map(str,(before,int(p.read(manager+0x50,8)!=0),result))))
with tempfile.TemporaryDirectory() as folder:
 exe=Path(folder)/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
 result=subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(p.PROJECT/'SourceCatalog/LevelPressHarness.cs'),*[str(runtime/f)for f in ('OriginalLevelPressRouter.cs','OriginalCellTouchRules.cs','OriginalItemPress.cs','OriginalMatchHighlight.cs')]],capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 result=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 actual=result.stdout.lstrip('\ufeff').splitlines();assert actual==expected,next(((i,cases[i],a,e)for i,(a,e)in enumerate(zip(actual,expected))if a!=e),(len(actual),len(expected)))
report={'passed':True,'native_cases':len(cases),'scope':'Actual ARM64 LevelTouchManager.TouchCell/CancelTouchedItemAnimation, CellModel.TouchDown/IsTouchingMatchItem, static last-above/last-below lookup and invalid-swap feedback routing versus production typed dispatch. Includes enabled/blocked/booster states, empty/match/base current item, zero/one/two above and below items, existing touched item, cancel twice and touched-field Clear subset without stop. Match press and stop use actual native MatchItemModel methods and base false/empty implementations; highlight itself is a separately verified boundary. Unity input, domain storage, static virtual presentation, booster selection and generic list/interface storage are supplied. Concrete box/static press visuals, full Clear/events, tap and complete playable context remain pending.'}
(p.PROJECT/'level_press_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
