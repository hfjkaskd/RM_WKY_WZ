"""Compare all native candidate classification branches and stable sorting."""
import sys
sys.dont_write_bytecode=True
import verify_board_layout as b
import json,random,subprocess,tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_X3,UC_ARM64_REG_X4,UC_ARM64_REG_X5,UC_ARM64_REG_X6,UC_ARM64_REG_LR,UC_ARM64_REG_PC,UC_ARM64_REG_SP
p=b.p
cell_base=0x10010000;temp_base=0x10020000
def ci(address):return -1 if address==0 else (address-cell_base)//0x200
def ti(address):return -1 if address==0 else (address-temp_base)//0x100
def boundary(uc,address,size,data):
    if address==0x12e25d4:
        args=[uc.reg_read(r) for r in (UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_X3,UC_ARM64_REG_X4,UC_ARM64_REG_X5,UC_ARM64_REG_X6)]
        output.append((args[0],args[1],ti(args[2]),ti(args[3]),ci(args[4]),ci(args[5])))
        uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
    elif address==0xd01fa0:
        p.emu.mem_write(uc.reg_read(UC_ARM64_REG_X0),bytes(uc.reg_read(UC_ARM64_REG_X2)))
        uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,boundary)
def call(rva):
    for reg,value in ((UC_ARM64_REG_X0,0x10000000),(UC_ARM64_REG_X1,0),(UC_ARM64_REG_SP,0x101ff000),(UC_ARM64_REG_LR,0x20000000)):p.emu.reg_write(reg,value)
    p.emu.emu_start(rva,0x20000000,count=2000000)
    assert p.emu.reg_read(UC_ARM64_REG_PC)==0x20000000
patterns=[]
for y in range(5):
    for x in range(3):patterns.append((2,x+5*y,x+1+5*y,x+2+5*y,-1))
for x in range(5):
    for y in range(3):patterns.append((1,x+5*y,x+5*(y+1),x+5*(y+2),-1))
for x in range(4):
    for y in range(4):patterns.append((0,x+5*y,x+5*(y+1),x+1+5*y,x+1+5*(y+1)))
rng=random.Random(1962731);fixtures=[]
# Exhaustive ordered pairs include reverse scan order, disjoint patterns,
# overlaps, equal directions and intersections at every position.
for a in range(len(patterns)):
    for c in range(len(patterns)):fixtures.append([patterns[a],patterns[c]])
for n in range(64):fixtures.append(rng.sample(patterns,rng.randrange(3,12)))
cases=[];expected=[]
for fixture in fixtures:
    p.emu.mem_write(0x10000000,bytes(0x40000));p.heap=0x10050000;output=[]
    p.put(0x10000068,0x10001000,8);p.put(0x10001018,len(fixture));p.put(0x10000078,len(fixture));p.put(0x10000010,0x10002000,8)
    for i in range(25):
        cell=cell_base+i*0x200;med=cell+0x40;slot=cell+0x80;item=cell+0xc0
        p.put(cell+0x10,med,8);p.put(med+0x10,slot,8);p.put(slot+0x18,item,8);p.put(item+0x18,1)
    for i,(direction,*indices) in enumerate(fixture):
        temp=temp_base+i*0x100;array=temp+0x40;p.put(temp+0x10,direction);p.put(temp+0x18,array,8);p.put(array+0x18,4)
        p.put(0x10001020+8*i,temp,8)
        for j,c in enumerate(indices):p.put(array+0x20+8*j,0 if c<0 else cell_base+c*0x200,8)
    call(0x12e1bc8)
    unsorted=list(output)
    p.put(0x1000007c,len(output));p.put(0x10000070,0x10003000,8);p.put(0x10003000,0x10004000,8);p.put(0x10003018,len(output))
    for i,candidate in enumerate(output):
        obj=0x10006000+0x40*i;p.put(0x10003020+8*i,obj,8);p.put(obj+0x10,candidate[0])
    call(0x12e2764)
    sorted_output=[unsorted[(p.read(0x10003020+8*i,8)-0x10006000)//0x40] for i in range(len(output))]
    cases.append(';'.join(','.join(map(str,pattern)) for pattern in fixture))
    expected.append('|'.join(','.join(map(str,c)) for c in unsorted)+'/'+ '|'.join(','.join(map(str,c)) for c in sorted_output))
harness='''using System;using System.Collections.Generic;using Whitebox.Foundation;
namespace UnityEngine{public static class Debug{public static void LogError(object x){}}}
class Verify{static int Index(OriginalGridCell c){return c==null?-1:c.X+5*c.Y;}static string Format(List<OriginalPossibleMatch> list){var text=new List<string>();foreach(var c in list)text.Add(c.PatternType+","+c.MatchType+","+c.FirstTempIndex+","+c.SecondTempIndex+","+Index(c.FirstIntersect)+","+Index(c.SecondIntersect));return string.Join("|",text);}static void Main(){string line;while((line=Console.ReadLine())!=null){var cells=new OriginalGridCell[25];for(int i=0;i<25;i++){cells[i]=new OriginalGridCell(i%5,i/5,1);cells[i].Mediation.SetAllItems(new OriginalMatchBoardItem(i,0,1));}var threes=new List<OriginalTempMatch>();foreach(var part in line.Split(';')){var a=Array.ConvertAll(part.Split(','),int.Parse);threes.Add(new OriginalTempMatch(a[0],cells[a[1]],cells[a[2]],cells[a[3]],a[4]<0?null:cells[a[4]]));}var output=new List<OriginalPossibleMatch>();OriginalMatchCandidates.Build(threes,output);string first=Format(output);OriginalMatchCandidates.Sort(output);Console.WriteLine(first+"/"+Format(output));}}}'''
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder);source=folder/'Verify.cs';source.write_text(harness);exe=folder/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
    names=('OriginalMatchSnapshot.cs','OriginalCellMediation.cs','OriginalGridIterator.cs','OriginalMatchSearch.cs','OriginalMatchCandidates.cs')
    compiled=subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),*[str(runtime/n) for n in names]],capture_output=True,text=True)
    assert compiled.returncode==0,compiled.stdout+compiled.stderr
    actual=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8',check=True).stdout.replace('\ufeff','').splitlines()
    assert actual==expected,next(((cases[i],a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e),'count')
report={'passed':True,'native_candidate_and_sort_cases':len(cases),'original_rvas':['0x12e1bc8','0x12e2764'],'scope':'Actual native candidate emission and stable priority sorting versus compiled C#. Exhaustive ordered pairs of 5x5 straight triples and 2x2 patterns plus multi-pattern fixtures. Input patterns, current item colors, storage and AddPossibleMatchGroup logging are supplied boundaries. Final visited arbitration, match-group construction, extensions and playable integration remain incomplete.'}
(p.PROJECT/'match_candidates_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
