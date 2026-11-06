"""Run native operation/fall-freeze methods and compare managed state per step."""
import sys
sys.dont_write_bytecode=True
import verify_board_layout as b
import json, random, subprocess, tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import UC_ARM64_REG_X0,UC_ARM64_REG_PC,UC_ARM64_REG_LR
p=b.p
frame=0
def boundary(uc,address,size,data):
    if address==0x3205d7c:
        uc.reg_write(UC_ARM64_REG_X0,frame);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,boundary)
owner=0x10000000;container=0x10001000;array=0x10002000;cell_base=0x10003000
def cell(i):return cell_base+0x100*i
def snap():
    result=[p.signed(p.read(owner+0x28))]
    result += [p.signed(p.read(array+0x20+4*i)) for i in range(28)]
    result += [p.signed(p.read(cell(i)+0x28)) for i in range(4)]
    result += [b.call(0x1ee51b4,container),b.call(0x1ee5210,container,6)]
    return ','.join(map(str,result))
cases=[];expected=[]
for seed in range(64):
    p.emu.mem_write(owner,bytes(0x10000));p.put(owner+0x10,container,8);p.put(container+0x10,array,8);p.put(array+0x18,28);p.put(container+0x18,-1)
    for i in range(4):p.put(cell(i)+0x70,owner,8)
    rng=random.Random(seed);steps=[];out=[]
    for step in range(64):
        frame=seed*100+step;target=rng.randrange(4);op=rng.randrange(2,28)
        action=rng.randrange(5)
        if action==0 or (action==1 and p.read(cell(target)+0x28)==0):
            action=0;b.call(0x31933d4,cell(target))
        elif action==1:b.call(0x31932f8,cell(target))
        elif action==2:b.call(0x1ee1c9c,owner,op)
        elif action==3:
            # Suppress only the external native error formatter on an already-zero
            # Finish. Actual counter/manager code still runs in the native ELF.
            if p.read(array+0x20+4*op)==0:p.put(container+0x18,op)
            b.call(0x1ee1d14,owner,op)
        else:b.call(0x1edc738,owner,op)
        steps.append(','.join(map(str,[action,target,op,frame])));out.append(snap())
    # Drain all freezes/operations to verify the last-finish frame transition.
    for i in range(4):
        while p.read(cell(i)+0x28):
            frame+=1;b.call(0x31932f8,cell(i));steps.append(f'1,{i},0,{frame}');out.append(snap())
    for op in range(2,28):
        frame+=1;b.call(0x1edc738,owner,op);steps.append(f'4,0,{op},{frame}');out.append(snap())
    cases.append(';'.join(steps));expected.extend(out)
harness=r'''using System;using Whitebox.Foundation;
namespace UnityEngine{public static class Debug{public static void LogError(object x){}}}
class Verify{static void Main(){string line;while((line=Console.ReadLine())!=null){int frame=0;var state=new OriginalGameOperations(()=>frame);var cells=new OriginalGridCell[4];for(int i=0;i<4;i++){cells[i]=new OriginalGridCell(i,0,1);cells[i].BindGameOperations(state);}foreach(var step in line.Split(';')){var a=Array.ConvertAll(step.Split(','),int.Parse);frame=a[3];switch(a[0]){case 0:cells[a[1]].FreezeFall();break;case 1:cells[a[1]].UnFreezeFall();break;case 2:state.OperationStart(a[2]);break;case 3:state.OperationFinish(a[2]);break;case 4:state.OperationReset(a[2]);break;}string output=state.LastOperationFrame.ToString();for(int i=0;i<28;i++)output+=","+state.Container.GetCount(i);for(int i=0;i<4;i++){output+=","+cells[i].FallFreezeCount;if(cells[i].IsFallFrozen!=(cells[i].FallFreezeCount>0))throw new Exception();}output+=","+(state.Container.HasAny()?1:0)+","+(state.Container.HasMask(6)?1:0);Console.WriteLine(output);}}}}'''
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder);source=folder/'Verify.cs';source.write_text(harness);exe=folder/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
    names=('OriginalGameOperations.cs','OriginalCellFallState.cs','OriginalGridIterator.cs','OriginalCellMediation.cs','OriginalMatchSnapshot.cs','OriginalMatchSearch.cs','OriginalMatchCandidates.cs')
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),*[str(runtime/n) for n in names]],capture_output=True,text=True,encoding='utf-8')
    assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8')
    assert result.returncode==0,result.stdout+result.stderr
    actual=result.stdout.replace('\ufeff','').splitlines()
    assert actual==expected,next(((i,a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e),'count mismatch')
report={'passed':True,'native_sequence_count':64,'native_state_comparisons':len(expected),
 'original_rvas':['0x31933d4','0x31932f8','0x1ee1c9c','0x1ee1d14','0x1edc738','0x1ee51b4','0x1ee5210'],
 'scope':'Actual native nested cell freezes, 28 operation counters, finish/reset and final-operation frame, exact active-mask comparison. Frame getter supplied. Native zero-finish external error formatting suppressed; diagnostics text and full state-machine transitions are not differential-tested.'}
(p.PROJECT/'game_operations_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
