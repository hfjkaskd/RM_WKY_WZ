"""Native MatchGroup ctor/Init/Reset plus value-copy lifetime tests."""
import sys
sys.dont_write_bytecode=True
import verify_board_layout as b
import json,random,subprocess,tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_X3,UC_ARM64_REG_X4,UC_ARM64_REG_PC,UC_ARM64_REG_LR,UC_ARM64_REG_SP
p=b.p
def boundary(uc,address,size,data):
    if address in (0xd018a0,0xd00b50):
        x0,x1,x2=[uc.reg_read(r) for r in (UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2)]
        p.emu.mem_write(x0,bytes(p.emu.mem_read(x1,x2)));uc.reg_write(UC_ARM64_REG_X0,x0);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,boundary)
def call(rva,args):
    for reg,value in zip((UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_X3,UC_ARM64_REG_X4),args+[0]*(5-len(args))):p.emu.reg_write(reg,value)
    p.emu.reg_write(UC_ARM64_REG_SP,0x101ff000);p.emu.reg_write(UC_ARM64_REG_LR,0x20000000);p.emu.emu_start(rva,0x20000000,count=100000)
    assert p.emu.reg_read(UC_ARM64_REG_PC)==0x20000000
def cell(i):return 0x10003000+i*0x100
def snapshot(owner):
    values=[p.read(owner),p.read(owner+4),p.read(owner+0x80,1),p.read(owner+0x90,1),p.read(owner+0x91,1),p.read(owner+0x92,1),-1 if not p.read(owner+0x88,8) else (p.read(owner+0x88,8)-0x10003000)//0x100]
    values += [(p.read(owner+0x10+8*i,8)-0x10003000)//0x100 for i in range(p.read(owner+8))]
    return ','.join(map(str,values))
rng=random.Random(2231589);cases=[];expected=[]
for n in range(128):
    count=n%15;new_count=rng.randrange(15);id=rng.randrange(256);pattern=rng.randrange(1,8);color=rng.randrange(1,7);exists=n%2
    p.emu.mem_write(0x10000000,bytes(0x10000));owner=0x10000000;copy=owner+0x100;source=owner+0x1000
    p.put(owner,pattern);p.put(owner+4,color);p.put(owner+8,count);p.put(owner+0x80,id,1);p.put(owner+0x88,cell(15),8);p.put(owner+0x90,1,1);p.put(owner+0x91,1,1);p.put(owner+0x92,exists,1)
    for i in range(count):p.put(owner+0x10+8*i,cell(i),8)
    p.emu.mem_write(copy,bytes(p.emu.mem_read(owner,152)))
    call(0x12df088,[owner]);out=[snapshot(owner),snapshot(copy)]
    p.put(source,new_count)
    for i in range(new_count):p.put(source+8+8*i,cell(14-i),8)
    call(0x12defe4,[owner,id,7,6,source]);out += [snapshot(owner),snapshot(copy)]
    call(0x12def34,[owner,id,pattern,color]);out.append(snapshot(owner))
    call(0x12defe4,[owner,id,7,6,source]);out.append(snapshot(owner))
    cases.append(','.join(map(str,[count,new_count,id,pattern,color,exists])));expected.append('|'.join(out))
harness='''using System;using System.Collections.Generic;using Whitebox.Foundation;
namespace UnityEngine{public static class Debug{public static void LogError(object x){}}}
class Freeze:IOriginalMatchFreezeOperations{public List<int> Cells=new List<int>();public void FreezeFall(OriginalGridCell cell){Cells.Add(cell.X);}}
class Context:IOriginalSwapMatchContext{public List<IOriginalSwapMatchGroup> Groups=new List<IOriginalSwapMatchGroup>();public IReadOnlyList<IOriginalSwapMatchGroup> FindExtensiveMatchesInArea(OriginalGridCell c,int r){return Groups;}public bool HasSpecialItem(OriginalGridCell c){return false;}public bool CanComboWith(OriginalBoardItem a,OriginalBoardItem b){throw new Exception();}}
class Verify{static string Format(OriginalMatchGroupSnapshot s){var a=new List<int>{s.PatternType,s.MatchType,s.Id,s.ExplosionCalculated?1:0,s.CanExplode?1:0,s.Exists?1:0,s.SpecialItemCreationCell==null?-1:s.SpecialItemCreationCell.X};for(int i=0;i<s.Cells.Count;i++)a.Add(s.Cells.Get(i).X);return string.Join(",",a);}static void Main(){string line;while((line=Console.ReadLine())!=null){var a=Array.ConvertAll(line.Split(','),int.Parse);var cells=new OriginalGridCell[16];for(int i=0;i<16;i++)cells[i]=new OriginalGridCell(i,0,1);var s=new OriginalMatchGroupSnapshot((byte)a[2],a[3],a[4]);s.Exists=a[5]!=0;s.ExplosionCalculated=true;s.CanExplode=true;s.SpecialItemCreationCell=cells[15];for(int i=0;i<a[0];i++)s.Cells.Add(cells[i]);var copy=s;var output=new List<string>();s.Reset();output.Add(Format(s));output.Add(Format(copy));var source=new OriginalMatchCells();for(int i=0;i<a[1];i++)source.Add(cells[14-i]);s.Init((byte)a[2],7,6,source);if(source.Count<14)source.Add(cells[15]);output.Add(Format(s));output.Add(Format(copy));s=new OriginalMatchGroupSnapshot((byte)a[2],a[3],a[4]);output.Add(Format(s));var fresh=new OriginalMatchCells();for(int i=0;i<a[1];i++)fresh.Add(cells[14-i]);s.Init((byte)a[2],7,6,fresh);output.Add(Format(s));Console.WriteLine(string.Join("|",output));}
// Actual restored pooled groups must detach swap snapshots before a new search.
var grid=new OriginalGridCell[6];for(int i=0;i<6;i++){grid[i]=new OriginalGridCell(i,0,1);grid[i].Mediation.SetAllItems(new OriginalMatchBoardItem(i,0,i<3?1:2));}var freeze=new Freeze();var builder=new OriginalMatchGroups(6,1,freeze);var threes=new List<OriginalTempMatch>{new OriginalTempMatch(2,grid[0],grid[1],grid[2])};var candidates=new List<OriginalPossibleMatch>();OriginalMatchCandidates.Build(threes,candidates);OriginalMatchCandidates.Sort(candidates);var groups=builder.Build(threes,candidates);var context=new Context();context.Groups.Add(groups[0]);var swap=new OriginalSwapDecision(context);if(!swap.ShouldPlaySwapAnimation(grid[0],grid[1],false))throw new Exception();var saved=swap.ToMatch as OriginalSavedSwapGroup;if(saved==null)throw new Exception();threes.Clear();threes.Add(new OriginalTempMatch(2,grid[3],grid[4],grid[5]));OriginalMatchCandidates.Build(threes,candidates);OriginalMatchCandidates.Sort(candidates);builder.Build(threes,candidates);if(saved.Snapshot.MatchType!=1||saved.Snapshot.Cells.Get(0)!=grid[0]||saved.Snapshot.Cells.Count!=3)throw new Exception("Snapshot alias");freeze.Cells.Clear();saved.FreezeCells();if(string.Join(",",freeze.Cells)!="0,1,2")throw new Exception("Wrong frozen snapshot");}}'''
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder);source=folder/'Verify.cs';source.write_text(harness);exe=folder/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
    names=('OriginalMatchSnapshot.cs','OriginalCellMediation.cs','OriginalGridIterator.cs','OriginalMatchSearch.cs','OriginalMatchCandidates.cs','OriginalMatchGroups.cs','OriginalSwapDecision.cs')
    compiled=subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),*[str(runtime/n) for n in names]],capture_output=True,text=True);assert compiled.returncode==0,compiled.stdout+compiled.stderr
    actual=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8',check=True).stdout.replace('\ufeff','').splitlines()
    assert actual==expected,next(((cases[i],a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e),'count')
report={'passed':True,'native_snapshot_cases':128,'managed_pool_reuse_swap_case':True,'original_rvas':['0x12def34','0x12defe4','0x12df088'],'scope':'Native ctor/Init/Reset field flags, preserved cell storage, inline value-copy independence and managed swap snapshot survival across actual group-pool reuse. Native memmove and write barriers supplied. Explode readiness, creation-cell selection, input, animation and resolution remain incomplete.'}
(p.PROJECT/'match_snapshot_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
