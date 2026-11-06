"""Full native base-group arbitration/construction against compiled managed code."""
import sys
sys.dont_write_bytecode=True
import verify_board_layout as b
import json,random,subprocess,tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_X3,UC_ARM64_REG_X4,UC_ARM64_REG_X5,UC_ARM64_REG_X6,UC_ARM64_REG_X8,UC_ARM64_REG_SP,UC_ARM64_REG_LR,UC_ARM64_REG_PC
p=b.p
cell_base=0x10010000;temp_base=0x10030000
slot=p.read(0x5391658,8);klass=0x100d0000;p.put(slot,klass,8);b.resolved_slots.add(slot)
p.put(klass+0x12c,1,1);p.put(klass+0xc8,klass+0x200,8);p.put(klass+0x200,klass,8)
def index(cell):return (cell-cell_base)//0x600
def boundary(uc,address,size,data):
    trace.append(address)
    if len(trace)>40:del trace[0]
    x0,x1,x2,x3=[uc.reg_read(r) for r in (UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_X3)];value=None
    if address==0xd01fa0:p.emu.mem_write(x0,bytes(x2));value=0
    elif address in (0xd00b50,0xd018a0):p.emu.mem_write(x0,bytes(p.emu.mem_read(x1,x2)));value=x0
    elif address==0x2b313c0:value=0
    elif address==0x209f5d0:
        refs=[p.read(x0+0x20+i*8,8) for i in range(p.read(x0+0x18))]
        value=int(any(index(c) in visited for c in refs if c))
    elif address==0x1c1b858:
        point=(x1&0xffffffff)+5*(x2&0xffffffff);visited.add(point);visit_owners[point]=x3&255;value=0
    elif address==0x12e25d4:
        n=p.read(0x1000007c);obj=0x10006000+n*0x40
        p.put(obj+0x10,x1);p.put(obj+0x14,x2)
        for offset,reg in zip((0x18,0x20,0x28,0x30),(UC_ARM64_REG_X3,UC_ARM64_REG_X4,UC_ARM64_REG_X5,UC_ARM64_REG_X6)):p.put(obj+offset,uc.reg_read(reg),8)
        p.put(0x10002020+n*8,obj,8);p.put(0x1000007c,n+1);value=0
    elif address==0x284dfd0:
        array=p.read(x0+0x10,8);p.emu.mem_write(uc.reg_read(UC_ARM64_REG_X8),bytes(p.emu.mem_read(array+0x20+152*x1,152)));value=0
    if value is not None:uc.reg_write(UC_ARM64_REG_X0,value);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,boundary)
patterns=[]
for y in range(5):
    for x in range(3):patterns.append((2,x+5*y,x+1+5*y,x+2+5*y,-1))
for x in range(5):
    for y in range(3):patterns.append((1,x+5*y,x+5*(y+1),x+5*(y+2),-1))
for x in range(4):
    for y in range(4):patterns.append((0,x+5*y,x+5*(y+1),x+1+5*y,x+1+5*(y+1)))
visited=set();trace=[];visit_owners={}
def verify_base_groups():
    global visited,trace,visit_owners
    rng=random.Random(2033996);fixtures=[[a] for a in patterns]
    fixtures += [rng.sample(patterns,rng.randrange(2,13)) for n in range(192)]
    cases=[];expected=[]
    for fixture in fixtures:
        p.emu.mem_write(0x10000000,bytes(0x90000));p.heap=0x10090000;visited=set();trace=[];visit_owners={}
        p.put(0x10000068,0x10001000,8);p.put(0x10001018,len(fixture));p.put(0x10000078,len(fixture))
        p.put(0x10000070,0x10002000,8);p.put(0x10002018,256);p.put(0x10002000,0x10005000,8)
        p.put(0x10000010,0x10003000,8);p.put(0x10003010,0x10050000,8);p.put(0x10050018,64);p.put(0x10000060,0x10004000,8)
        for i in range(25):
            cell=cell_base+i*0x600;med=cell+0x40;slot=cell+0x80;item=cell+0x100
            p.put(cell+0x18,i%5);p.put(cell+0x1c,i//5);p.put(cell+0x10,med,8);p.put(med+0x10,slot,8);p.put(slot+0x18,item,8);p.put(item+0x18,1);p.put(item,klass,8)
        for i,(direction,*indices) in enumerate(fixture):
            temp=temp_base+i*0x100;array=temp+0x40;p.put(temp+0x10,direction);p.put(temp+0x18,array,8);p.put(array+0x18,4);p.put(0x10001020+8*i,temp,8)
            for j,c in enumerate(indices):p.put(array+0x20+8*j,0 if c<0 else cell_base+c*0x600,8)
        for reg,value in ((UC_ARM64_REG_X0,0x10000000),(UC_ARM64_REG_X1,0),(UC_ARM64_REG_SP,0x101ff000),(UC_ARM64_REG_LR,0x20000000)):p.emu.reg_write(reg,value)
        try:p.emu.emu_start(0x12e1bc8,0x20000000,count=2000000)
        except Exception as e:raise RuntimeError((hex(p.emu.reg_read(UC_ARM64_REG_PC)),[hex(a) for a in trace])) from e
        assert p.emu.reg_read(UC_ARM64_REG_PC)==0x20000000
        groups=[]
        for i in range(p.read(0x10003018)):
            group=0x10050020+152*i;cells=[index(p.read(group+0x10+8*j,8)) for j in range(p.read(group+8))]
            groups.append(','.join(map(str,[p.read(group),p.read(group+4),p.read(group+0x80,1)]+cells)))
        ids=[p.read(cell_base+i*0x600+0x100+0x138,1) for i in range(25)]
        cases.append(';'.join(','.join(map(str,a)) for a in fixture));expected.append('|'.join(groups)+'/'+','.join(map(str,ids)))
    harness='''using System;using System.Collections.Generic;using Whitebox.Foundation;
    namespace UnityEngine{public static class Debug{public static void LogError(object x){}}}
    class Freeze:IOriginalMatchFreezeOperations{public void FreezeFall(OriginalGridCell cell){throw new Exception();}}
    class Verify{static void Main(){string line;while((line=Console.ReadLine())!=null){var cells=new OriginalGridCell[25];for(int i=0;i<25;i++){cells[i]=new OriginalGridCell(i%5,i/5,1);cells[i].Mediation.SetAllItems(new OriginalMatchBoardItem(i,0,1));}var threes=new List<OriginalTempMatch>();foreach(var part in line.Split(';')){var a=Array.ConvertAll(part.Split(','),int.Parse);threes.Add(new OriginalTempMatch(a[0],cells[a[1]],cells[a[2]],cells[a[3]],a[4]<0?null:cells[a[4]]));}var candidates=new List<OriginalPossibleMatch>();OriginalMatchCandidates.Build(threes,candidates);OriginalMatchCandidates.Sort(candidates);var groups=new OriginalMatchGroups(5,5,new Freeze()).Build(threes,candidates);var text=new List<string>();foreach(var g in groups){var row=new List<int>{g.PatternType,g.MatchType,g.Id};for(int i=0;i<g.CellCount;i++)row.Add(g.GetCell(i).X+5*g.GetCell(i).Y);text.Add(string.Join(",",row));}var ids=new List<int>();foreach(var c in cells)ids.Add(((OriginalMatchBoardItem)c.Mediation.CurrentItem).NativeMatchGroupId);Console.WriteLine(string.Join("|",text)+"/"+string.Join(",",ids));}}}'''
    with tempfile.TemporaryDirectory() as folder:
        folder=Path(folder);source=folder/'Verify.cs';source.write_text(harness);exe=folder/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
        names=('OriginalMatchSnapshot.cs','OriginalCellMediation.cs','OriginalGridIterator.cs','OriginalMatchSearch.cs','OriginalMatchCandidates.cs','OriginalMatchGroups.cs','OriginalSwapDecision.cs')
        compiled=subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),*[str(runtime/n) for n in names]],capture_output=True,text=True)
        assert compiled.returncode==0,compiled.stdout+compiled.stderr
        actual=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8',check=True).stdout.replace('\ufeff','').splitlines()
        assert actual==expected,next(((cases[i],a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e),'count')
    report={'passed':True,'native_base_group_cases':len(cases),'original_rvas':['0x12e1bc8','0x12e2890','0x12def34','0x12defe4'],'scope':'Native candidate traversal, stable sorting, visited rejection, base group cell ordering and item group-id writeback versus compiled C#. Candidate storage, LINQ Any/storage/delegate boundaries and visit writes supplied; actual native CreateAndAddMatchGroup and MatchGroup.Init execute. Extensions, full item group snapshot, freeze game-state operations, input and resolution remain incomplete.'}
    (p.PROJECT/'match_groups_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))

if __name__=="__main__":verify_base_groups()
