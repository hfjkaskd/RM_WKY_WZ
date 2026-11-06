"""Actual ARM64 level tap and cell guards versus production typed routing."""
import sys
sys.dont_write_bytecode=True
import verify_board_layout as b
import json,itertools,subprocess,tempfile,struct
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import *
p=b.p
manager,cell,mediation,statics,reservation,item,klass,hierarchy,interfaces,booster,level,ux,king,tap_event,move_event=[0x10010000+i*0x1000 for i in range(15)]
TAP,MOVE,DISABLED,TYPE,EVENT,COMPLETED=[0x20000100+i*4 for i in range(6)]
trace=[];kind=mutate=0;selected=blocked=result=False;move=0;fault=False
def resolve(slot):
 if slot not in b.resolved_slots:p.emu.reg_write(UC_ARM64_REG_X0,slot);b.boundary(p.emu,0xffd960,4,None)
 return p.read(slot,8)
tap_interface=resolve(0x558e588)
special_class=resolve(0x5594190);light_class=resolve(0x558fb68);rocket_class=resolve(0x5592508);tnt_class=resolve(0x5595118)
for cls,depth in ((special_class,2),(light_class,3),(rocket_class,3),(tnt_class,3)):p.put(cls+0x12c,depth,1)
def data_text(address):
 offsets=(0,4,8,12,16)
 values=[p.signed(p.read(address+o)) for o in offsets]+[p.read(address+0x14,1),p.read(address+0x15,1)]
 values += [p.signed(p.read(address+o))for o in (0x18,0x1c,0x20,0x24,0x28)]+[p.read(address+o)for o in (0x2c,0x30,0x34)]
 return ','.join(map(str,values))
def boundary(uc,address,size,unused):
 global fault,move
 x0,x1,x2=[uc.reg_read(r)for r in (UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2)];value=None
 if address==0x343bd5c:trace.append('item');value=item if kind else 0
 elif address==0x343bd38:trace.append('has');value=int(kind!=0)
 elif address==0x3440db8:value=int(blocked)
 elif address==0x1e35b24:trace.append('booster');value=int(selected)
 elif address==0x1e358e8:trace.append('use');value=0
 elif address==0x1ee2338:trace.append('useful');value=0
 elif address==0x23e648c:value=king
 elif address==0x23e6e44:trace.append('king');value=0
 elif address==0x27b3078:trace.append('lightball');value=0
 elif address==0x1f719b4:trace.append('special');value=0
 elif address==0x27b28f0:trace.append('count:'+str(x0)+':'+str(x1));value=0
 elif address==0x1f719d0:trace.append('rocket-tnt');value=0
 elif address==0x2eebc08:
  assert x0==3
  ptr=uc.reg_read(UC_ARM64_REG_X8);uc.mem_write(ptr,bytes(56))
  for offset,value32 in ((0,-1),(4,-1),(8,3),(12,1),(0x18,-1),(0x20,-1),(0x24,-1),(0x28,-1)):p.put(ptr+offset,value32&0xffffffff)
  value=0
 elif address==TAP:trace.append('tap:'+data_text(x1));value=int(result)
 elif address==DISABLED:trace.append('disabled');value=p.read(item+0x28,1)
 elif address==TYPE:trace.append('type');value=kind+20
 elif address==EVENT:
  trace.append('event')
  if mutate==1:move+=100
  value=0
 elif address==MOVE:
  trace.append('move')
  if mutate==2:p.put(manager+0x20,0,8)
  value=move
 elif address==COMPLETED:trace.append('completed:'+str(x1));value=0
 elif address==0xffda88:fault=True;uc.reg_write(UC_ARM64_REG_PC,0x20000000);return
 if value is not None:uc.reg_write(UC_ARM64_REG_X0,value&0xffffffffffffffff);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,boundary)
def call(rva,*args):
 for i,v in enumerate((*args,0,0,0)):p.emu.reg_write(UC_ARM64_REG_X0+i,v&0xffffffffffffffff)
 p.emu.reg_write(UC_ARM64_REG_SP,0x101ff000);p.emu.reg_write(UC_ARM64_REG_LR,0x20000000)
 try:p.emu.emu_start(rva,0x20000000,count=200000)
 except Exception:
  print('Native failure',hex(p.emu.reg_read(UC_ARM64_REG_PC)),inputs_row,trace);raise
 assert p.emu.reg_read(UC_ARM64_REG_PC)==0x20000000
 return p.emu.reg_read(UC_ARM64_REG_X0)&1
p.put(cell+0x10,mediation,8);p.put(cell+0xc0,statics,8);p.put(cell+0x48,reservation,8)
p.put(manager+0x38,booster,8);p.put(manager+0x48,level,8);p.put(level+0x18,ux,8)
p.put(item,klass,8);p.put(klass+0xc8,hierarchy,8);p.put(klass+0xb0,interfaces,8);p.put(klass+0x12a,1,2)
p.put(interfaces,tap_interface,8);p.put(interfaces+8,0)
for offset,fn in ((0x138,MOVE),(0x148,TAP),(0x158,DISABLED),(0x378,TYPE)):p.put(klass+offset,fn,8)
for event,fn in ((tap_event,EVENT),(move_event,COMPLETED)):p.put(event+0x18,fn,8)
cases=[];expected=[]
inputs=[]
for op in ('C','L'):
 for kind,enabled,blocked,reserved,disabled,normal,selected,result,mutate in itertools.product(range(6),(0,1),(0,1),(0,1),(0,1),(0,1),(0,1),(0,1),range(3)):
  inputs.append((op,kind,enabled,blocked,reserved,disabled,normal,selected,result,mutate,3))
for listeners in range(3):inputs.append(('L',5,1,0,0,0,1,0,1,0,listeners))
for inputs_row in inputs:
 op,kind,enabled,blocked,reserved,disabled,normal,selected,result,mutate,listeners=inputs_row
 p.put(cell+0x2c,0 if enabled else 1);p.put(cell+0xa0,1 if normal else 2);p.put(reservation+0x10,reserved);p.put(item+0x28,disabled,1)
 p.put(klass+0x12c,1 if kind<2 else 2 if kind==2 else 3,1)
 p.put(hierarchy,klass,8);p.put(hierarchy+8,special_class if kind>=2 else klass,8);p.put(hierarchy+16,{3:light_class,4:rocket_class,5:tnt_class}.get(kind,klass),8)
 p.put(manager+0x10,tap_event if listeners&1 else 0,8);p.put(manager+0x20,move_event if listeners&2 else 0,8)
 trace=[];fault=False;move=kind*10;output=-1
 if op=='C':
  r=call(0x319cda4,cell,0x10030000);output=int(p.read(0x10030000,8)!=0)
 else:r=call(0x1ee1ef8,manager,cell)
 cases.append(','.join(map(str,inputs_row)));expected.append('|'.join(trace)+'#'+('null'if fault else str(r))+','+str(output))
runtime=p.PROJECT/'Assets/Foundation/Runtime'
def compile_run(folder,harness,sources,stdin=''):
 exe=Path(folder)/(harness+'.exe')
 result=subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(p.PROJECT/'SourceCatalog'/harness),*[str(runtime/f)for f in sources]],capture_output=True,text=True,encoding='utf8');assert result.returncode==0,result.stdout+result.stderr
 result=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input=stdin,capture_output=True,text=True,encoding='utf8');assert result.returncode==0,result.stdout+result.stderr
 return result.stdout.lstrip('\ufeff').splitlines()
with tempfile.TemporaryDirectory() as folder:
 actual=compile_run(folder,'LevelTapHarness.cs',('OriginalLevelTapRouter.cs','OriginalCellTouchRules.cs','OriginalExplodeData.cs'),'\n'.join(cases)+'\n')
 assert actual==expected,next(((i,cases[i],a,e)for i,(a,e)in enumerate(zip(actual,expected))if a!=e),(len(actual),len(expected)))
 actual_base=compile_run(folder,'ItemTapHarness.cs',('OriginalItemTap.cs',))
 # Base ItemModel getter, setter and empty Tap bodies are actual ARM64.
 p.put(item+0x24,0);p.put(item+0x28,0,1)
 base=[str(call(0x2b45db4,item))+','+str(call(0x2b45dbc,item))+','+str(call(0x2b47ac4,item))]
 for value in (1,0):call(0x2b45dc4,item,value);base.append(str(call(0x2b45dbc,item)))
 # Return values are 13/1, so do not use the bool-normalizing call helper.
 for rva in (0x27ba4fc,0x27b555c):
  call(rva,item);base += [str(p.emu.reg_read(UC_ARM64_REG_X0)), 'pending']
 assert actual_base==base,(actual_base,base)
report={'passed':True,'native_cases':len(cases),'base_tap_and_move_properties_checked':True,'scope':'Actual ARM64 LevelTouchManager.TapCell/CheckForBoosterOperation and CellModel.CanTapCurrentItem versus production typed routing, including empty/base/special/lightball/rocket/TNT class branches; touch, blocking, reservation, tap-disabled, cell-kind and booster guards; rejected/successful Tap results; exact trigger-3 value payload; success event order, post-event move getter, captured completion delegate and required missing-subscriber faults. Base ItemModel tap/disabled/move properties and rocket/TNT move constants are separately compared with actual ARM64. Domain storage, native class hierarchy representation (lightball marker), concrete special Tap/explosion, booster/session/king/count services, Exploder.Next producer and event allocation are supplied boundaries. Actual production rocket/TNT Tap deliberately throws until concrete explosion is restored; no full playable context or special activation is claimed.'}
(p.PROJECT/'level_tap_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
