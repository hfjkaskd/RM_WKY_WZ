"""Compare native extension discovery/processing on actual base groups."""
import sys
sys.dont_write_bytecode=True
import verify_match_groups as v
import json,random,subprocess,tempfile,hashlib,struct
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_X3,UC_ARM64_REG_LR,UC_ARM64_REG_PC,UC_ARM64_REG_SP
p=v.p;b=v.b
directions=(1,3,5,7)
assert hashlib.sha256(struct.pack('<4i',*directions)).hexdigest().upper()=='49C16B6F0EF802F3AAF7FC52DC1E6925199EDDC6E255028182BBA4E236F8F855'
slot=p.read(0x53b2db8,8);p.put(slot,0x100d4000,8);b.resolved_slots.add(slot)
p.put(0x100d40e0,1);p.put(0x100d40b8,0x100d5000,8);p.put(0x100d5008,0x100d6000,8);p.put(0x100d6018,4)
for i,d in enumerate(directions):p.put(0x100d6020+4*i,d)
def boundary(uc,address,size,data):
    x0,x1,x2,x3=[uc.reg_read(r) for r in (UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_X3)];value=None
    if n==3 and address in (0x12e32a0,0x12df15c):debug.append((hex(address),-1 if not x1 else v.index(x1)))
    if n==3 and address==0x12e31a4:debug.append(('candidate',-1 if not x1 else v.index(x1),dict(v.visit_owners)))
    if address in (0xffda88,0xffda90):raise RuntimeError(('native exception boundary',[hex(a) for a in v.trace]))
    if address==0x1c1b918:value=int((x1&0xffffffff)+5*(x2&0xffffffff) in v.visit_owners)
    elif address==0x1c1b8c4:value=int(v.visit_owners.get((x1&0xffffffff)+5*(x2&0xffffffff),0)==(x3&255))
    elif address==0x319dc24:value=int(p.read(x0+0x2c0,1)==0)
    elif address==0x12e242c:value=p.read(x0+0x30,1)
    elif address==0x284e034:
        array=p.read(x0+0x10,8);p.emu.mem_write(array+0x20+152*x1,bytes(p.emu.mem_read(x2,152)));value=0
    if n==3 and address==0x1c1b918:debug.append(('visited',x1,x2,value))
    if value is not None:uc.reg_write(UC_ARM64_REG_X0,value);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,boundary)
def run(rva,arg=0):
    for reg,value in ((UC_ARM64_REG_X0,0x10000000),(UC_ARM64_REG_X1,arg),(UC_ARM64_REG_SP,0x101ff000),(UC_ARM64_REG_LR,0x20000000)):p.emu.reg_write(reg,value)
    try:p.emu.emu_start(rva,0x20000000,count=3000000)
    except Exception as e:raise RuntimeError((hex(p.emu.reg_read(UC_ARM64_REG_PC)),[hex(a) for a in v.trace])) from e
    assert p.emu.reg_read(UC_ARM64_REG_PC)==0x20000000
rng=random.Random(2079973);cases=[];expected=[];full=0;extended=0
for n in range(160):
    fixture=[v.patterns[n%len(v.patterns)]] if n<128 else [(1,0,5,10,-1),(1,4,9,14,-1)]
    base={i for pattern in fixture for i in pattern[1:] if i>=0}
    colors=[1 if n<16 else rng.randrange(1,3) for i in range(25)]
    block=[0 if n<16 else int(rng.random()<0.15) for i in range(25)]
    falling=[0 if n<16 else rng.randrange(2) for i in range(25)];exclude=n%2
    for i in base:colors[i]=1;block[i]=0;falling[i]=0
    p.emu.mem_write(0x10000000,bytes(0x90000));p.heap=0x10090000;v.visited=set();v.trace=[];v.visit_owners={};debug=[]
    p.put(0x10000068,0x10001000,8);p.put(0x10001018,len(fixture));p.put(0x10000078,len(fixture));p.put(0x10000070,0x10002000,8);p.put(0x10002018,256);p.put(0x10002000,0x10005000,8)
    p.put(0x10000010,0x10003000,8);p.put(0x10003010,0x10050000,8);p.put(0x10050018,64);p.put(0x10000060,0x10004000,8);p.put(0x10000018,0x10060000,8);p.put(0x10060018,56)
    for i in range(25):
        cell=v.cell_base+i*0x600;med=cell+0x40;current=cell+0x80;item=cell+0x100;neighbors=cell+0x300;array=cell+0x380
        p.put(cell+0x18,i%5);p.put(cell+0x1c,i//5);p.put(cell+0x10,med,8);p.put(med+0x10,current,8);p.put(current+0x18,item,8);p.put(item+0x18,colors[i]);p.put(item,v.klass,8);p.put(item+0x30,1-falling[i],1);p.put(cell+0x2c0,block[i],1)
        p.put(cell+0x20,neighbors,8);p.put(neighbors+0x18,array,8);p.put(array+0x18,8)
        for d,(dx,dy) in enumerate(zip((-1,0,1,1,1,0,-1,-1),(1,1,1,0,-1,-1,-1,0))):
            x,y=i%5+dx,i//5+dy;p.put(array+0x20+8*d,v.cell_base+(x+5*y)*0x600 if 0<=x<5 and 0<=y<5 else 0,8)
    for i,(direction,*indices) in enumerate(fixture):
        temp=v.temp_base+i*0x100;array=temp+0x40;p.put(temp+0x10,direction);p.put(temp+0x18,array,8);p.put(array+0x18,4);p.put(0x10001020+8*i,temp,8)
        for j,c in enumerate(indices):p.put(array+0x20+8*j,0 if c<0 else v.cell_base+c*0x600,8)
    run(0x12e1bc8);before=p.read(0x10050028);run(0x12e1110,exclude)
    output=[]
    for i in range(p.read(0x10003018)):
        group=0x10050020+152*i;count=p.read(group+8);full+=int(count==14);extended+=int(count>before)
        output.append(','.join(map(str,[p.read(group),p.read(group+4),p.read(group+0x80,1)]+[v.index(p.read(group+0x10+8*j,8)) for j in range(count)])))
    ids=[]
    for i in range(25):
        snap=v.cell_base+i*0x600+0x100+0xb8;count=p.read(snap+8)
        values=[p.read(snap+0x80,1),p.read(snap),p.read(snap+4),p.read(snap+0x92,1),count]+[v.index(p.read(snap+0x10+8*j,8)) for j in range(count)]
        ids.append(','.join(map(str,values)))
    if n==3:debug_fixture=list(debug)
    cases.append('|'.join([':'.join(','.join(map(str,f)) for f in fixture),','.join(map(str,colors)),','.join(map(str,block)),','.join(map(str,falling)),str(exclude)]));expected.append('|'.join(output)+'/'+';'.join(ids))
harness='''using System;using System.Collections.Generic;using Whitebox.Foundation;
namespace UnityEngine{public static class Debug{public static void LogError(object x){}}}
class State:IOriginalMatchBlockingState,IOriginalMatchFreezeOperations{public int[] Block;public bool HasTouchBlockingItemExceptChain(OriginalGridCell c){return Block[c.X+5*c.Y]!=0;}public bool HasIncomingTouchBlockingItem(OriginalGridCell c){return false;}public void FreezeFall(OriginalGridCell c){throw new Exception();}}
class Verify{static void Main(){string line;while((line=Console.ReadLine())!=null){var parts=line.Split('|');var a=new int[5][];for(int z=1;z<parts.Length;z++)a[z]=Array.ConvertAll(parts[z].Split(','),int.Parse);var cells=new OriginalGridCell[25];for(int i=0;i<25;i++){cells[i]=new OriginalGridCell(i%5,i/5,1);var item=new OriginalMatchBoardItem(i,0,a[1][i]);cells[i].Mediation.SetAllItems(item);if(a[3][i]!=0)cells[i].Mediation.ClearNextItem();}for(int i=0;i<25;i++)for(int d=0;d<8;d++){int x,y;cells[i].Neighbors.GetPoint(d,out x,out y);if(x>=0&&x<5&&y>=0&&y<5)cells[i].Neighbors.Set(d,cells[x+5*y]);}var threes=new List<OriginalTempMatch>();foreach(var spec in parts[0].Split(':')){var f=Array.ConvertAll(spec.Split(','),int.Parse);threes.Add(new OriginalTempMatch(f[0],cells[f[1]],cells[f[2]],cells[f[3]],f[4]<0?null:cells[f[4]]));}var candidates=new List<OriginalPossibleMatch>();OriginalMatchCandidates.Build(threes,candidates);OriginalMatchCandidates.Sort(candidates);var state=new State{Block=a[2]};var builder=new OriginalMatchGroups(5,5,state);var groups=builder.Build(threes,candidates);builder.ExtendGroups(state,a[4][0]!=0);var text=new List<string>();foreach(var g in groups){var row=new List<int>{g.PatternType,g.MatchType,g.Id};for(int i=0;i<g.CellCount;i++)row.Add(g.GetCell(i).X+5*g.GetCell(i).Y);text.Add(string.Join(",",row));}var ids=new List<string>();foreach(var c in cells){var snap=((OriginalMatchBoardItem)c.Mediation.CurrentItem).MatchGroup;var row=new List<int>{snap.Id,snap.PatternType,snap.MatchType,snap.Exists?1:0,snap.Cells.Count};for(int j=0;j<snap.Cells.Count;j++)row.Add(snap.Cells.Get(j).X+5*snap.Cells.Get(j).Y);ids.Add(string.Join(",",row));}Console.WriteLine(string.Join("|",text)+"/"+string.Join(";",ids));}}}'''
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder);source=folder/'Verify.cs';source.write_text(harness);exe=folder/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
    names=('OriginalMatchSnapshot.cs','OriginalCellMediation.cs','OriginalGridIterator.cs','OriginalMatchSearch.cs','OriginalMatchCandidates.cs','OriginalMatchGroups.cs','OriginalMatchExtensions.cs','OriginalSwapDecision.cs')
    compiled=subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),*[str(runtime/n) for n in names]],capture_output=True,text=True);assert compiled.returncode==0,compiled.stdout+compiled.stderr
    actual=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8',check=True).stdout.replace('\ufeff','').splitlines()
    assert actual==expected,(next(((cases[i],a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e),'count'),debug_fixture)
report={'passed':True,'native_extension_cases':len(cases),'extended_groups':extended,'full_groups':full,'original_rvas':['0x12e1110','0x12e2ba0','0x12e31a4','0x12e32a0'],'scope':'Actual native extension gather/process, support-cell checks, 14-cell cap, order, ownership and full item snapshot writeback versus compiled C#. Touching-direction bytes match original private-field SHA256. Static/incoming blocking, falling-state query, visit storage and list struct storage supplied boundaries. Explode readiness, input, animation and resolution remain incomplete.'}
(p.PROJECT/'match_extensions_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
