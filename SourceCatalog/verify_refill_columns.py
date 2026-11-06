"""Compare refill columns and ManualUpdate pending-column tail with native ARM64."""
import sys
sys.dont_write_bytecode=True
import verify_board_layout as b
import json,random,subprocess,tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X8,UC_ARM64_REG_LR,UC_ARM64_REG_PC
p=b.p
storage={};columns=(-1,0,1,2,2147483647);success_mask=0
def boundary(uc,address,size,data):
    x0,x1=(uc.reg_read(r) for r in (UC_ARM64_REG_X0,UC_ARM64_REG_X1));key=p.signed(x1&0xffffffff);value=None
    if address==0x220c2b0:storage[x0]={};p.put(x0+0x20,0);value=0
    elif address==0x220d588:
        values=storage[x0];value=int(key not in values);values[key]=True;p.put(x0+0x20,len(values))
    elif address==0x220cc0c:
        values=storage[x0];value=int(key in values);values.pop(key,None);p.put(x0+0x20,len(values))
    elif address==0x220ca20:value=int(key in storage[x0])
    elif address==0x220c9c0:storage[x0].clear();p.put(x0+0x20,0);value=0
    elif address in (0x16d0dcc,0x16d0fa4,0x32109f0,0x3210a88,0x3210b24,0x3210bb4):value=0
    elif address==0x314c094:
        array=p.allocate(0x1000);p.put(array+0x18,x1);p.put(x0+0x10,array,8);value=0
    elif address==0x220cee0:
        result=uc.reg_read(UC_ARM64_REG_X8);p.put(result,x0,8);p.put(result+8,0);p.put(result+0x10,0);value=0
    elif address==0x27f55d0:
        values=list(storage[p.read(x0,8)]);index=p.read(x0+8);value=int(index<len(values))
        if value:p.put(x0+0x10,values[index]);p.put(x0+8,index+1)
    elif address==0x27f55cc:value=0
    elif address==0x3210d84:value=int(bool(success_mask&(1<<columns.index(key))))
    if value is not None:
        uc.reg_write(UC_ARM64_REG_X0,value);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,boundary)
cases=[];expected=[];operations=0
for seed in range(24):
    rng=random.Random(seed);p.heap=0x10010000;p.emu.mem_write(0x10000000,bytes(0x1000));storage.clear()
    b.call(0x3206aa8,0x10000000)
    ops=['E,0,0','M,0,0','M,0,31','D,1,0','E,1,0','D,1,0','M,0,31','C,0,0']
    ops += [f'{rng.choice("DEMC")},{rng.randrange(5)},{rng.randrange(32)}' for _ in range(40)]
    trace=[]
    for operation in ops:
        op,index,mask=operation.split(',');index=int(index);success_mask=int(mask)
        if op=='D':b.call(0x3210618,0x10000000,columns[index])
        elif op=='E':b.call(0x3210670,0x10000000,columns[index])
        elif op=='C':b.call(0x32102d4,0x10000000,success_mask&1)
        else:b.call(0x32106f0,0x10000000)
        trace.append(','.join(str(b.call(0x32120bc,0x10000000,col)) for col in columns))
    cases.append(';'.join(ops));expected.append('|'.join(trace));operations+=len(ops)
harness='''using System;using Whitebox.Foundation;
namespace UnityEngine{public static class Debug{public static void LogError(object value){}}}
class Queue:IOriginalRemainingColumnQueue{public int Mask;public int[] Columns={-1,0,1,2,int.MaxValue};public bool AddRemainingItemsToFillingCellQueue(int col){return (Mask&(1<<Array.IndexOf(Columns,col)))!=0;}}
class Verify{static void Main(){string l;while((l=Console.ReadLine())!=null){var state=new OriginalFallRefillColumns();var queue=new Queue();var ops=l.Split(';');var trace=new string[ops.Length];for(int n=0;n<ops.Length;n++){var a=ops[n].Split(',');int col=queue.Columns[int.Parse(a[1])];queue.Mask=int.Parse(a[2]);switch(a[0]){case "D":state.DisableReFillForTopColumn(col);break;case "E":state.EnableReFillForTopColumn(col);break;case "C":state.Clear();break;case "M":state.ProcessPendingColumns(queue);break;}var v=new int[5];for(int i=0;i<5;i++)v[i]=state.CanRefillFromColumn(queue.Columns[i])?1:0;trace[n]=string.Join(",",v);}Console.WriteLine(string.Join("|",trace));}}}'''
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder);source=folder/'Verify.cs';source.write_text(harness,encoding='utf-8');exe=folder/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
    names=('OriginalCellMediation.cs','OriginalGridIterator.cs','OriginalFillingCell.cs','OriginalFallRefillColumns.cs')
    subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),*[str(runtime/n) for n in names]],capture_output=True,check=True)
    actual=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8',check=True).stdout.lstrip('\ufeff').splitlines()
    assert actual==expected,next(((cases[i],a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e),'Output count differs')
report={'passed':True,'native_traces':24,'native_operations':operations,'original_rvas':['0x3206aa8','0x3210618','0x3210670','0x32120bc','0x32102d4','0x32106f0'],'scope':'Original column disable/enable/query/clear and ManualUpdate pending-column completion tail versus compiled C#. HashSet/Stack/List storage and enumeration, traversal/fall phases and remaining-item transfer decisions are supplied boundaries. Callback enumeration order is not verified. Temporary completion list is reused in managed runtime for GC control, unlike native per-update allocation. Full fall calculation, ItemCreator, visuals and frame scheduling remain incomplete.'}
(p.PROJECT/'refill_columns_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report))
