"""Execute native predefined container control flow against compiled C#."""
import sys
sys.dont_write_bytecode=True
import verify_board_layout as b
import json,random,subprocess,tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_X3,UC_ARM64_REG_PC,UC_ARM64_REG_LR,UC_ARM64_REG_SP
p=b.p
owner=0x10000000
queues={};counts={}
def boundary(uc,address,size,data):
    x0,x1,x2=[uc.reg_read(r) for r in (UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2)]
    value=None
    if address==0x23f1fcc:queues[x0].append(x1);value=0
    elif address==0x23f2024:value=queues[x0].pop(0) if queues[x0] else 0
    elif address==0x23f2278:queues[x0].clear();value=0
    elif address==0x23f20e0:value=sum(p.read(i+0x24) for i in queues[x0])
    elif address==0x3c97c34:p.put(x2,counts.get(x1,0));value=int(x1 in counts)
    elif address==0x3c961c0:counts[x1]=x2&0xffffffff;value=0
    elif address==0x3c96364:counts.clear();value=0
    elif address==0x20000110:value=p.read(x0+0x20)
    if value is not None:uc.reg_write(UC_ARM64_REG_X0,value);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,boundary)
p.put(0x100d0000+0x378,0x20000110,8)
def call(address,*args):
    for reg,value in zip((UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_X3),args+(0,)*(4-len(args))):p.emu.reg_write(reg,value)
    p.emu.reg_write(UC_ARM64_REG_SP,0x101ff000);p.emu.reg_write(UC_ARM64_REG_LR,0x20000000)
    p.emu.emu_start(address,0x20000000,count=100000)
    assert p.emu.reg_read(UC_ARM64_REG_PC)==0x20000000
    return p.emu.reg_read(UC_ARM64_REG_X0)&0xffffffff
traces=[];expected=[]
for seed in range(24):
    p.emu.mem_write(owner,bytes(0x10000));p.heap=0x10010000
    array=owner+0x100;queues={owner+0x200+i*0x100:[] for i in range(9)};counts={}
    p.put(owner+0x10,array,8);p.put(owner+0x18,owner+0x1200,8);p.put(array+0x18,9)
    for i,q in enumerate(queues):p.put(array+0x20+8*i,q,8)
    for i in range(1,7):
        item=owner+0x2000+i*0x100;p.put(item,0x100d0000,8);p.put(item+0x20,i%3);p.put(item+0x24,i)
    rng=random.Random(seed);ops=[];out=[]
    for n in range(80):
        op=0 if n<12 else rng.randrange(4);column=rng.randrange(9);item=rng.randrange(1,7)
        ops.append(f'{op},{column},{item}')
        if op==0:call(0x1c91498,owner,column,owner+0x2000+item*0x100);value=0
        elif op==1:
            result=call(0x1c9159c,owner,column);value=0 if result==0 else (result-owner-0x2000)//0x100
        elif op==2:call(0x1c91404,owner);value=0
        else:value=call(0x1c915d8,owner)
        out.append(str(value)+':'+','.join(str(counts.get(i,0)) for i in range(3)))
    traces.append(';'.join(ops));expected.append('|'.join(out))
harness='''using System;using System.Collections.Generic;using Whitebox.Foundation;
namespace UnityEngine{public static class Debug{public static void LogError(object x){}}}
class Verify{static void Main(){string line;while((line=Console.ReadLine())!=null){var c=new OriginalPredefinedItemContainer();var items=new OriginalBoardItem[7];for(int i=1;i<7;i++)items[i]=new OriginalBoardItem(i,i%3);var output=new List<string>();foreach(var op in line.Split(';')){var a=Array.ConvertAll(op.Split(','),int.Parse);int value=0;if(a[0]==0)c.AddPredefinedItem(a[1],items[a[2]]);else if(a[0]==1){var item=c.GetPredefinedItem(a[1]);value=item==null?0:item.Id;}else if(a[0]==2)c.Clear();else value=c.HitRequiredForLevelProgression(item=>item.Id);int[] counts=new int[3];for(int i=0;i<3;i++)c.InitialItemCounts.TryGetValue(i,out counts[i]);output.Add(value+":"+string.Join(",",counts));}Console.WriteLine(string.Join("|",output));}}}'''
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder);source=folder/'Verify.cs';source.write_text(harness);exe=folder/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
    subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),str(runtime/'OriginalCellMediation.cs'),str(runtime/'OriginalGridIterator.cs'),str(runtime/'OriginalPredefinedItemContainer.cs')],capture_output=True,check=True)
    actual=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(traces)+'\n',capture_output=True,text=True,encoding='utf-8',check=True).stdout.replace('\ufeff','').splitlines()
    actual=[line.lstrip(chr(65279)) for line in actual]
    assert actual==expected,next(((i,a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e),'count')
report={'passed':True,'traces':24,'operations':1920,'scope':'Native container add, dequeue, clear and progression aggregation control flow. Queue storage, dictionary storage and item hit values supplied boundaries. Save-data conversion and concrete item hit logic remain pending.'}
(p.PROJECT/'predefined_container_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
