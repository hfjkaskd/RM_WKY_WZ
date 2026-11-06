"""Native normal-item swap decision and full mediation, supplied match groups."""
import sys
sys.dont_write_bytecode=True
import verify_board_layout as b
import verify_cell_mediation as m
import itertools,json,subprocess,tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_X8,UC_ARM64_REG_LR,UC_ARM64_REG_PC
p=b.p
slot=p.read(0x5391658,8);klass=0x100d0000;p.put(slot,klass,8);b.resolved_slots.add(slot)
p.put(klass+0x12c,1,1);p.put(klass+0xc8,klass+0x200,8);p.put(klass+0x200,klass,8);p.put(klass+0xe0,1)
def boundary(uc,address,size,data):
    x0,x1,x2=[uc.reg_read(r) for r in (UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2)];value=None
    if address==0xd01fa0:p.emu.mem_write(x0,bytes(152));value=0
    elif address==0xd00b50:p.emu.mem_write(x0,bytes(p.emu.mem_read(x1,152)));value=0
    elif address==0x12e1448:log.append('search');value=0x10008000
    elif address==0x284dfd0:p.emu.mem_write(uc.reg_read(UC_ARM64_REG_X8),bytes(p.emu.mem_read(0x10009000+152*x1,152)));value=0
    elif address==0x12df4ec:
        group=p.read(x0);item=m.items.index(x1);log.append('contains'+str(group)+':'+str(item));value=(groups[group]>>item)&1
    elif address==0x12df978:log.append('freeze'+str(p.read(x0)));value=0
    elif address==0x319cd50:log.append('special'+str(m.cells.index(x0)));value=0
    if value is not None:uc.reg_write(UC_ARM64_REG_X0,value);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,boundary)
cases=[];expected=[]
for groups,ignore in itertools.product(([],[0],[1],[2],[3],[1,2],[3,1],[2,2]),range(2)):
    p.emu.mem_write(0x10000000,bytes(0x10000));m.setup();log=[]
    for item in m.items:p.put(item,klass,8)
    m.call(0x34485f4,m.mediators[0],m.items[0]);m.call(0x34485f4,m.mediators[1],m.items[1])
    p.put(0x10000068,m.cells[0],8);p.put(0x10000070,m.cells[1],8);p.put(0x10000028,0x10007000,8);p.put(0x10008018,len(groups))
    for i,g in enumerate(groups):p.put(0x10009000+152*i,i);p.put(0x10009000+152*i+146,1,1)
    result=m.call(0x1edfb40,0x10000000,ignore)
    from_group=p.read(0x10000078) if p.read(0x1000010a,1) else -1
    to_group=p.read(0x10000110) if p.read(0x100001a2,1) else -1
    creation=[-1 if not p.read(address,8) else m.cells.index(p.read(address,8)) for address in (0x10000100,0x10000198)]
    actual_ids=[m.items.index(p.read(row[0]+0x18,8)) for row in m.slots[:2]]
    expected.append(','.join(map(str,[result,from_group,to_group]+creation+actual_ids))+';'+'|'.join(log))
    cases.append(','.join(map(str,[ignore]+groups)))
harness='''using System;using System.Collections.Generic;using Whitebox.Foundation;
namespace UnityEngine{public static class Debug{public static void LogError(object x){}}}
class Group:IOriginalSwapMatchGroup{public int Id,Mask;public List<string> Log;public bool IsValid{get{return true;}}public bool ContainsItem(OriginalBoardItem item){Log.Add("contains"+Id+":"+item.Id);return (Mask&(1<<item.Id))!=0;}public void FreezeCells(){Log.Add("freeze"+Id);}public void CopySnapshotTo(OriginalSavedSwapGroup destination){destination.CopyFrom(new OriginalMatchGroupSnapshot((byte)Id,Id,1),null);}}
class Context:IOriginalSwapMatchContext{public List<string> Log=new List<string>();public List<IOriginalSwapMatchGroup> Groups=new List<IOriginalSwapMatchGroup>();public IReadOnlyList<IOriginalSwapMatchGroup> FindExtensiveMatchesInArea(OriginalGridCell cell,int radius){if(cell.X!=1||radius!=3)throw new Exception();Log.Add("search");return Groups;}public bool HasSpecialItem(OriginalGridCell cell){Log.Add("special"+cell.X);return false;}public bool CanComboWith(OriginalBoardItem a,OriginalBoardItem b){throw new Exception();}}
class Verify{static void Main(){string line;while((line=Console.ReadLine())!=null){var a=Array.ConvertAll(line.Split(','),int.Parse);var context=new Context();for(int i=1;i<a.Length;i++)context.Groups.Add(new Group{Id=i-1,Mask=a[i],Log=context.Log});var from=new OriginalGridCell(0,0,1);var to=new OriginalGridCell(1,0,1);from.Mediation.SetAllItems(new OriginalMatchBoardItem(0,0,1));to.Mediation.SetAllItems(new OriginalMatchBoardItem(1,0,2));var decision=new OriginalSwapDecision(context);bool ok=decision.ShouldPlaySwapAnimation(from,to,a[0]!=0);Console.WriteLine((ok?1:0)+","+(decision.FromMatch==null?-1:((OriginalSavedSwapGroup)decision.FromMatch).Snapshot.PatternType)+","+(decision.ToMatch==null?-1:((OriginalSavedSwapGroup)decision.ToMatch).Snapshot.PatternType)+","+(decision.SavedFromMatch.SpecialItemCreationCell==null?-1:decision.SavedFromMatch.SpecialItemCreationCell.X)+","+(decision.SavedToMatch.SpecialItemCreationCell==null?-1:decision.SavedToMatch.SpecialItemCreationCell.X)+","+from.Mediation.CurrentItem.Id+","+to.Mediation.CurrentItem.Id+";"+string.Join("|",context.Log));decision.Clear();if(decision.HasMatchGroup||decision.SavedFromMatch.Exists||decision.SavedToMatch.Exists||decision.SavedFromMatch.Cells.Count!=0||decision.SavedToMatch.Cells.Count!=0||decision.FromSpecialCreationCell!=null||decision.ToSpecialCreationCell!=null)throw new Exception("Clear retained saved group data");}}}'''
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder);source=folder/'Verify.cs';source.write_text(harness);exe=folder/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
    names=('OriginalMatchSnapshot.cs','OriginalCellMediation.cs','OriginalGridIterator.cs','OriginalMatchSearch.cs','OriginalMatchCandidates.cs','OriginalSwapDecision.cs')
    compiled=subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),*[str(runtime/n) for n in names]],capture_output=True,text=True)
    assert compiled.returncode==0,compiled.stdout+compiled.stderr
    actual=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8',check=True).stdout.replace('\ufeff','').splitlines()
    assert actual==expected,next(((cases[i],a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e),'count')
report={'passed':True,'native_cases':len(cases),'scope':'Native normal-item ShouldPlaySwapAnimation, incoming/destination slot clearing, group assignment precedence, saved snapshot creation-cell assignment, freeze order, ignore-match option and invalid-swap restoration versus compiled C#. Match groups and freeze callbacks supplied; special combo implementations, matching/group discovery, animations and playable input integration remain incomplete.'}
(p.PROJECT/'swap_decision_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
