"""Compare original native touch/empty/swap predicates and short-circuit order."""
import sys
sys.dont_write_bytecode=True
import verify_board_layout as b
import json,random,subprocess,tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import UC_ARM64_REG_X0,UC_ARM64_REG_PC,UC_ARM64_REG_LR
p=b.p
cell=0x10000000;med=0x10001000;static=0x10002000;reserve=0x10003000
above=0x10004000;array=0x10005000;item=0x10006000;klass=0x10007000
static_base=0x10008000;static_klass=0x10009000
trace=[];level_honey=0;half_honey=0
def boundary(uc,address,size,data):
    x0=uc.reg_read(UC_ARM64_REG_X0);value=None
    if address==0x1e19848:value=level_honey
    elif address==0x319de28:value=half_honey;trace.append('H')
    elif address in (0x20000100,0x20000120):
        i=(x0-static_base)//0x80
        field=0x30 if address==0x20000100 else 0x31
        trace.append(('T' if field==0x30 else 'E')+str(i));value=p.read(x0+field,1)
    elif address==0x20000140:value=p.read(x0+0x32,1);trace.append('A')
    if value is not None:
        uc.reg_write(UC_ARM64_REG_X0,value);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,boundary)
cases=[];expected=[];rng=random.Random(319938)
for n in range(512):
    p.emu.mem_write(cell,bytes(0x10000))
    count=n%5;touches=rng.randrange(3);current=rng.randrange(2);nxt=rng.randrange(2);normal=rng.randrange(2)
    available=rng.randrange(2);reserved=rng.randrange(3);level_honey=rng.randrange(2);half_honey=rng.randrange(2);roof=rng.randrange(2)
    flags=[rng.randrange(4) for _ in range(count)]
    p.put(cell+0x10,med,8);p.put(cell+0xc0,static,8);p.put(cell+0x48,reserve,8);p.put(cell+0x78,0x1000a000,8)
    p.put(cell+0x2c,touches);p.put(cell+0xa0,normal);p.put(cell+0x44,roof,1);p.put(reserve+0x10,reserved)
    p.put(med+0x10,med+0x100,8);p.put(med+0x18,med+0x180,8)
    p.put(med+0x100+0x18,item if current else 0,8);p.put(med+0x180+0x18,item if nxt else 0,8)
    p.put(item,klass,8);p.put(item+0x32,available,1);p.put(klass+0x488,0x20000140,8)
    p.put(static+0x30,above,8);p.put(above+0x10,array,8);p.put(above+0x18,count);p.put(array+0x18,count)
    p.put(static_klass+0x408,0x20000100,8);p.put(static_klass+0x438,0x20000120,8)
    for i,flag in enumerate(flags):
        address=static_base+0x80*i;p.put(array+0x20+8*i,address,8);p.put(address,static_klass,8)
        p.put(address+0x30,flag&1,1);p.put(address+0x31,(flag>>1)&1,1)
    trace=[]
    values=[b.call(0x3440db8,static),b.call(0x3440c70,static),b.call(0x319dc64,cell),
            b.call(0x319dc24,cell),b.call(0x319ccec,cell),b.call(0x319ccf0,cell),b.call(0x319c9c4,cell),b.call(0x319cc54,cell)]
    # DisableTouch/EnableTouch use saturating-at-zero enable semantics.
    b.call(0x319d26c,cell);b.call(0x319d27c,cell)
    for _ in range(4):b.call(0x319d27c,cell)
    values.append(p.read(cell+0x2c))
    cases.append('|'.join([','.join(map(str,[touches,current,nxt,normal,available,reserved,level_honey,half_honey,roof])),','.join(map(str,flags))]))
    expected.append(','.join(map(str,values))+'|'+','.join(trace))
harness=r'''using System;using System.Collections.Generic;using Whitebox.Foundation;
namespace UnityEngine{public static class Debug{public static void LogError(object x){}}}
class Item:IOriginalStaticTouchItem{public int Flag,Index;public List<string> Trace;public bool IsTouchBlocking{get{Trace.Add("T"+Index);return (Flag&1)!=0;}}public bool IsTouchBlockingExceptChain{get{Trace.Add("E"+Index);return (Flag&2)!=0;}}}
class Honey:IOriginalHalfGridHoneyViews{public bool Half;public List<string> Trace;public bool HasHalfGridHoneyView(OriginalGridCell c){Trace.Add("H");return Half;}}
class Available:IOriginalItemSwapAvailability{public bool Value;public List<string> Trace;public bool IsAvailableForSwap(OriginalBoardItem item){Trace.Add("A");return Value;}}
class Verify{static int B(bool v){return v?1:0;}static void Main(){string line;while((line=Console.ReadLine())!=null){var p=line.Split('|');var a=Array.ConvertAll(p[0].Split(','),int.Parse);var trace=new List<string>();var c=new OriginalGridCell(0,0,a[3]);for(int i=0;i<a[0];i++)c.DisableTouch();for(int i=0;i<a[5];i++)c.Reservation.Enable();if(a[1]!=0)c.Mediation.SetCurrentItem(new OriginalBoardItem(1,0));if(a[2]!=0)c.Mediation.SetNextItem(new OriginalBoardItem(2,0));c.HasMostlyPlacedRoofTile=a[8]!=0;
if(p[1]!=""){var flags=Array.ConvertAll(p[1].Split(','),int.Parse);for(int i=0;i<flags.Length;i++)c.StaticTouch.AboveItems.Add(new Item{Flag=flags[i],Index=i,Trace=trace});}
var honey=new Honey{Half=a[7]!=0,Trace=trace};var blocking=new OriginalMatchBlockingState(a[6]!=0,honey);var items=new Available{Value=a[4]!=0,Trace=trace};var values=new List<int>{B(c.StaticTouch.HasTouchBlockingItem()),B(c.StaticTouch.HasTouchBlockingItemExceptChain()),B(blocking.HasIncomingTouchBlockingItem(c)),B(blocking.CanEnterToMatch(c)),B(OriginalCellTouchRules.CanSwapFrom(c,items)),B(OriginalCellTouchRules.CanSwapTo(c,items)),B(c.IsTouchEnabled),B(OriginalCellTouchRules.IsEmptyCell(c))};c.DisableTouch();c.EnableTouch();for(int i=0;i<4;i++)c.EnableTouch();values.Add(c.TouchDisableCount);Console.WriteLine(string.Join(",",values)+"|"+string.Join(",",trace));}}}'''
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder);source=folder/'Verify.cs';source.write_text(harness);exe=folder/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
    names=('OriginalCellTouchRules.cs','OriginalCellMediation.cs','OriginalGridIterator.cs','OriginalMatchSearch.cs','OriginalMatchCandidates.cs','OriginalMatchSnapshot.cs')
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),*[str(runtime/n) for n in names]],capture_output=True,text=True,encoding='utf-8')
    assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8')
    assert result.returncode==0,result.stdout+result.stderr
    actual=result.stdout.replace('\ufeff','').splitlines()
    assert actual==expected,next(((cases[i],a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e),'count mismatch')
report={'passed':True,'native_cell_cases':512,'original_rvas':['0x3440db8','0x3440c70','0x319dc64','0x319dc24','0x319ccec','0x319ccf0','0x319c9c4','0x319cc54','0x319d26c','0x319d27c'],
 'scope':'Native predicate values and static-item/availability/honey short-circuit order. Real native list queries, empty/current/next mediator queries and reservation counter read. Concrete static virtual results, item swap lifecycle, level honey presence and honey view are supplied boundaries. Touch input, animations and resolution remain incomplete.'}
(p.PROJECT/'cell_touch_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
