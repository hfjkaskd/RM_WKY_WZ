"""Compare bidirectional cell/item mediation traces with original ARM64."""
import sys
sys.dont_write_bytecode=True
import verify_set_parser as p
import json,random,subprocess,tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_LR,UC_ARM64_REG_SP,UC_ARM64_REG_PC

def hook(uc,a,size,data):
    x0=uc.reg_read(UC_ARM64_REG_X0)
    # Cell delegate interface dispatch is supplied at the managed boundary.
    if a in (0x2b45968,0x2b45a7c,0x2b45b90):
        offset={0x2b45968:0x10,0x2b45a7c:0x18,0x2b45b90:0x20}[a]
        slot=p.read(x0+offset,8)
        uc.reg_write(UC_ARM64_REG_X0,p.read(slot+0x10,8) if slot else 0)
        uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
    elif a==0x3448730:
        # Logging/metadata boundary only; native rejection return still executes.
        uc.reg_write(UC_ARM64_REG_PC,0x3448794)
p.emu.hook_add(UC_HOOK_CODE,hook)
p.put(0x574944f,1,1)

def setup():
    global cells,mediators,slots,items,item_mediators
    p.heap=0x10010000;cells=[];mediators=[];slots=[];items=[];item_mediators=[]
    for c in range(3):
        cell=p.allocate();mediator=p.allocate();row=[];p.put(cell+0x10,mediator,8)
        for style in range(3):
            slot=p.allocate();p.put(slot+0x10,cell,8);p.put(slot+0x20,style);p.put(mediator+0x10+style*8,slot,8);row.append(slot)
        cells.append(cell);mediators.append(mediator);slots.append(row)
    for i in range(3):
        item=p.allocate();mediator=p.allocate();p.put(item+0x38,mediator,8);items.append(item);item_mediators.append(mediator)

def call(rva,owner,arg=0):
    p.emu.reg_write(UC_ARM64_REG_X0,owner);p.emu.reg_write(UC_ARM64_REG_X1,arg)
    p.emu.reg_write(UC_ARM64_REG_SP,0x101ff000);p.emu.reg_write(UC_ARM64_REG_LR,0x20000000)
    try: p.emu.emu_start(rva,0x20000000,count=100000)
    except Exception as error:
        raise RuntimeError((hex(p.emu.reg_read(UC_ARM64_REG_PC)),hex(rva),hex(owner),hex(arg))) from error
    assert p.emu.reg_read(UC_ARM64_REG_PC)==0x20000000
    return p.emu.reg_read(UC_ARM64_REG_X0)&1

def snapshot(result):
    values=[result]
    for row in slots:
        for slot in row:
            item=p.read(slot+0x18,8);values.append(items.index(item)+1 if item else 0)
    for mediator in item_mediators:
        for style in range(3):
            slot=p.read(mediator+0x10+style*8,8)
            cell=p.read(slot+0x10,8) if slot else 0
            values.append(cells.index(cell)+1 if cell else 0)
    return ','.join(map(str,values))

operations=[];expected=[]
for seed in range(30):
    setup();rng=random.Random(seed);operations.append('R')
    # Transfers, rejection, duplicate assignment, mixed slots, set-all and reset.
    trace=[(0,0,0,1),(0,1,0,1),(0,1,0,2),(0,1,0,1),(1,1,0,0),(2,2,0,2),(3,2,0,0),
           (0,0,0,2),(0,1,2,2),(0,2,1,2),(4,0,0,2)]
    trace += [(rng.randrange(4),rng.randrange(3),rng.randrange(3),rng.randrange(4)) for _ in range(40)]
    for op,cell,style,item in trace:
        if op==0:result=call((0x344863c,0x3448654,0x344866c)[style],mediators[cell],items[item-1] if item else 0)
        elif op==1:call((0x34485ac,0x34485c4,0x34485dc)[style],mediators[cell]);result=2
        elif op==2:call(0x34485f4,mediators[cell],items[item-1] if item else 0);result=2
        elif op==4:call(0x2b45cd4,item_mediators[item-1]);result=2
        else:call(0x3448824,mediators[cell]);result=2
        operations.append(f'{op},{cell},{style},{item}');expected.append(snapshot(result))
harness='''using System;using Whitebox.Foundation;
namespace UnityEngine{public static class Debug{public static void LogError(object a){}}}
class Verify{
 static OriginalCellMediation[] cells;static OriginalBoardItem[] items;
 static int ItemId(OriginalBoardItem a){return a==null?0:Array.IndexOf(items,a)+1;}
 static int CellId(OriginalCellMediation a){return a==null?0:Array.IndexOf(cells,a)+1;}
 static void Main(){string line;while((line=Console.ReadLine())!=null){if(line=="R"){cells=new[]{new OriginalCellMediation(),new OriginalCellMediation(),new OriginalCellMediation()};items=new[]{new OriginalBoardItem(1,1),new OriginalBoardItem(2,1),new OriginalBoardItem(3,1)};continue;}var p=Array.ConvertAll(line.Split(','),int.Parse);var c=cells[p[1]];var a=p[3]==0?null:items[p[3]-1];int result=2;if(p[0]==0){bool ok=p[2]==0?c.SetCurrentItem(a):p[2]==1?c.SetNextItem(a):c.SetTargetItem(a);result=ok?1:0;}else if(p[0]==1){if(p[2]==0)c.ClearCurrentItem();else if(p[2]==1)c.ClearNextItem();else c.ClearTargetItem();}else if(p[0]==2)c.SetAllItems(a);else if(p[0]==4)a.Mediator.ClearFromAllRegisteredCells();else c.Reset();string value=result.ToString();foreach(var cell in cells)value+=","+ItemId(cell.CurrentItem)+","+ItemId(cell.NextItem)+","+ItemId(cell.TargetItem);foreach(var item in items)value+=","+CellId(item.Mediator.CurrentCell)+","+CellId(item.Mediator.NextCell)+","+CellId(item.Mediator.TargetCell);Console.WriteLine(value);}}}
'''
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder);source=folder/'Verify.cs';source.write_text(harness,encoding='utf-8');exe=folder/'Verify.exe'
    subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),str(p.PROJECT/'Assets/Foundation/Runtime/OriginalCellMediation.cs'),str(p.PROJECT/'Assets/Foundation/Runtime/OriginalGridIterator.cs')],check=True,capture_output=True)
    actual=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(operations)+'\n',text=True,encoding='utf-8',capture_output=True,check=True).stdout.lstrip('\ufeff').splitlines()
    assert actual==expected,next(((i,a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e),'Output count differs')
report={'passed':True,'native_trace_count':30,'native_operation_count':len(expected),'original_rvas':['0x34486ac','0x3448684','0x2b45a20','0x2b45b34','0x2b45c48','0x34485f4','0x3448824','0x2b45cd4'],'scope':'Actual native slot assignment, transfer, rejection, clear-all, independent styles and item delegate mutation. GC write barriers, null-input diagnostic and interface cell getters supplied at boundaries. No gameplay, grid iteration or presentation verification.'}
(p.PROJECT/'cell_mediation_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(json.dumps(report))
