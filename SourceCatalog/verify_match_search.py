"""Execute native match predicate and existing-match search against compiled C#."""
import sys
sys.dont_write_bytecode=True
import verify_board_layout as b
import random,json,subprocess,tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_X3,UC_ARM64_REG_X4,UC_ARM64_REG_X5,UC_ARM64_REG_PC,UC_ARM64_REG_LR,UC_ARM64_REG_SP
p=b.p
cells=[0x10001000+i*0x400 for i in range(9)]
slot=p.read(0x5391658,8);klass=0x100d0000;p.put(slot,klass,8);b.resolved_slots.add(slot)
p.put(klass+0x12c,1,1);p.put(klass+0xc8,klass+0x200,8);p.put(klass+0x200,klass,8)
cursor=0
def boundary(uc,address,size,data):
    global cursor
    x0,x1=uc.reg_read(UC_ARM64_REG_X0),uc.reg_read(UC_ARM64_REG_X1);value=None
    if address==0x3440c70:value=p.read(x0,1)
    elif address==0x319dc64:value=p.read(x0+0xc8,1)
    elif address==0x12e242c:value=p.read(x0+0x80,1)
    elif address==0x318ec2c:cursor=0;value=0
    elif address==0x318ecc8:
        value=cells[cursor] if cursor<9 else 0;cursor+=1;p.put(0x10000058,value,8)
    elif address==0x31932c8:value=p.read(x0+x1*8,8)
    elif address==0x12df088:value=0
    elif address==0x12e246c:
        refs=[uc.reg_read(r) for r in (UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_X3,UC_ARM64_REG_X4)]
        emitted.append(','.join(map(str,[uc.reg_read(UC_ARM64_REG_X5)]+[cells.index(c) if c else -1 for c in refs])));value=0
    if value is not None:uc.reg_write(UC_ARM64_REG_X0,value);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,boundary)
def call(rva,args):
    for reg,value in zip((UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_X3,UC_ARM64_REG_X4),args+[0]*(5-len(args))):p.emu.reg_write(reg,value)
    p.emu.reg_write(UC_ARM64_REG_SP,0x101ff000);p.emu.reg_write(UC_ARM64_REG_LR,0x20000000)
    p.emu.emu_start(rva,0x20000000,count=100000);assert p.emu.reg_read(UC_ARM64_REG_PC)==0x20000000
    return p.emu.reg_read(UC_ARM64_REG_X0)&1
cases=[];expected=[];rng=random.Random(1470)
for n in range(256):
    p.emu.mem_write(0x10000000,bytes(0x10000));p.heap=0x10010000
    colors=[rng.randrange(4) for i in cells] if n>=8 else [n%3+1]*9
    blockers=[rng.randrange(2) if n>=8 else 0 for i in cells]
    incoming=[rng.randrange(2) if n>=8 else 0 for i in cells]
    falling=[rng.randrange(2) if n>=8 else n%2 for i in cells]
    for i,cell in enumerate(cells):
        x,y=i%3,i//3;med=cell+0x100;current=cell+0x140;item=cell+0x180;neighbors=cell+0x240;static=cell+0x300
        p.put(cell+0x10,med,8);p.put(med+0x10,current,8);p.put(current+0x18,item if colors[i] else 0,8)
        p.put(item,klass,8);p.put(item+0x18,colors[i]);p.put(item+0x80,1-falling[i],1)
        p.put(cell+0x18,x);p.put(cell+0x1c,y);p.put(cell+0x20,neighbors,8);p.put(cell+0xc0,static,8);p.put(static,blockers[i],1);p.put(cell+0xc8,incoming[i],1)
        for direction,(dx,dy) in enumerate(zip((-1,0,1,1,1,0,-1,-1),(1,1,1,0,-1,-1,-1,0))):
            nx,ny=x+dx,y+dy;p.put(neighbors+direction*8,cells[nx+3*ny] if 0<=nx<3 and 0<=ny<3 else 0,8)
    exclude=n%2;first=rng.randrange(9);second=rng.randrange(9);direction=rng.randrange(4)
    predicate=call(0x12e1630,[0,cells[first],cells[second],direction,exclude])
    found=call(0x12e1470,[0x10000000,exclude])
    emitted=[];p.put(0x10000068,0x1000c000,8);p.put(0x1000c018,64)
    for i in range(64):p.put(0x1000c020+i*8,0x1000d000+i*0x20,8)
    call(0x12e1848,[0x10000000,cells[first],exclude])
    cases.append('|'.join(','.join(map(str,v)) for v in (colors,blockers,incoming,falling,[exclude,first,second,direction])))
    expected.append(f'{predicate},{found};'+'|'.join(emitted))
harness='''using System;using System.Collections.Generic;using Whitebox.Foundation;
namespace UnityEngine{public static class Debug{public static void LogError(object x){}}}
class State:IOriginalMatchBlockingState{public int[] B,I;public bool HasTouchBlockingItemExceptChain(OriginalGridCell c){return B[c.X+3*c.Y]!=0;}public bool HasIncomingTouchBlockingItem(OriginalGridCell c){return I[c.X+3*c.Y]!=0;}}
class Strategy:IOriginalGridIteratorStrategy{public OriginalGridCell[] C;public OriginalGridCell FirstCell(){return C[0];}public OriginalGridCell GetNext(OriginalGridCell c){int i=c.X+3*c.Y+1;return i<9?C[i]:null;}}
class Verify{static void Main(){string line;while((line=Console.ReadLine())!=null){var parts=line.Split('|');var values=Array.ConvertAll(parts,s=>Array.ConvertAll(s.Split(','),int.Parse));var cells=new OriginalGridCell[9];for(int i=0;i<9;i++){cells[i]=new OriginalGridCell(i%3,i/3,1,0,0);if(values[0][i]!=0){var item=new OriginalMatchBoardItem(i,0,values[0][i]);cells[i].Mediation.SetCurrentItem(item);if(values[3][i]==0)cells[i].Mediation.SetNextItem(item);}}for(int i=0;i<9;i++)for(int d=0;d<8;d++){int x,y;cells[i].Neighbors.GetPoint(d,out x,out y);if(x>=0&&x<3&&y>=0&&y<3)cells[i].Neighbors.Set(d,cells[x+3*y]);}var search=new OriginalMatchSearch(new Strategy{C=cells},new State{B=values[1],I=values[2]});var a=values[4];var emitted=new List<OriginalTempMatch>();search.CollectForCell(cells[a[1]],a[0]!=0,emitted);var trace=new List<string>();foreach(var t in emitted)trace.Add(t.Direction+","+(t.First.X+3*t.First.Y)+","+(t.Middle.X+3*t.Middle.Y)+","+(t.Last.X+3*t.Last.Y)+","+(t.Fourth==null?-1:t.Fourth.X+3*t.Fourth.Y));Console.WriteLine((search.IsValidToPatternSearch(cells[a[1]],cells[a[2]],a[3],a[0]!=0)?1:0)+","+(search.IsThereAnyMatch(a[0]!=0)?1:0)+";"+string.Join("|",trace));}}}'''
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder);source=folder/'Verify.cs';source.write_text(harness);exe=folder/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
    names=('OriginalMatchSnapshot.cs','OriginalCellMediation.cs','OriginalGridIterator.cs','OriginalMatchSearch.cs','OriginalMatchCandidates.cs')
    compiled=subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),*[str(runtime/n) for n in names]],capture_output=True,text=True)
    assert compiled.returncode==0,compiled.stdout+compiled.stderr
    actual=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8',check=True).stdout.replace('\ufeff','').splitlines()
    assert actual==expected,next(((cases[i],a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e),'count')
report={'passed':True,'native_predicate_cases':256,'native_grid_search_cases':256,'native_pattern_collection_cases':256,'scope':'Actual native IsValidToPatternSearch, IsThereAnyMatch and FindThreeMatchesAndPropellersForCell pattern emission, including three-in-row, three-in-column, 2x2, coordinate direction, blockers and second-item falling exclusion versus compiled C#. Native match class metadata supplied; obstacle queries, falling-state result, per-item group reset, temporary pattern storage, iterator and neighbor storage supplied at boundaries. Match grouping, explosions, swapping, refill and rendering remain incomplete.'}
(p.PROJECT/'match_search_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
