"""Compare filling queues/refill transitions with original ARM64 functions."""
import sys
sys.dont_write_bytecode=True
import verify_board_layout as b
import json,random,subprocess,tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_LR,UC_ARM64_REG_PC
p=b.p
OWNER=0x10001000
incoming={};queues={};callbacks={};column_allowed=True;column_calls=[]
def boundary(uc,address,size,data):
    x0,x1,x2=(uc.reg_read(r) for r in (UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2));value=None
    if address==0x2215f5c:
        values=incoming.setdefault(x0,set());before=len(values);values.add(x1);p.put(x0+0x20,len(values));value=int(len(values)!=before)
    elif address==0x22155ec:
        values=incoming.setdefault(x0,set());value=int(x1 in values);values.discard(x1);p.put(x0+0x20,len(values))
    elif address==0x2215408:value=int(x1 in incoming.setdefault(x0,set()))
    elif address==0x1161ebc:queues[x0]=[];p.put(x0+0x20,0);value=0
    elif address==0x116278c:value=int(x1 in queues[x0])
    elif address==0x11624cc:queues[x0].append(x1);p.put(x0+0x20,len(queues[x0]));value=0
    elif address==0x1162734:value=queues[x0][0]
    elif address==0x1162684:value=queues[x0].pop(0);p.put(x0+0x20,len(queues[x0]))
    elif address==0x2044a78:p.put(x0+0x40,x1,8);value=0
    elif address==0x2b45e24:callbacks.setdefault(x0,[]).append(p.read(x1+0x40,8));value=0
    elif address==0x32120bc:column_calls.append(p.signed(x1&0xffffffff));value=int(column_allowed)
    if value is not None:
        uc.reg_write(UC_ARM64_REG_X0,value&0xffffffffffffffff);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,boundary)
slot=p.read(0x53e7838,8);klass=0x100d1000
p.put(slot,klass,8);b.resolved_slots.add(slot);p.put(klass+0xe0,1);p.put(klass+0xb8,0x100d1200,8)
p.put(0x100d1200,0x100d1300,8);p.put(0x100d1318,0x343d1c0,8);p.put(0x100d1340,OWNER,8)
p.put(0x100d0000+0x358,0x2b45d70,8)

cases=[];expected=[];operation_count=0
for seed in range(24):
    rng=random.Random(seed);p.heap=0x10010000
    p.emu.mem_write(OWNER,bytes(0x1000));p.put(OWNER+0x18,3);p.put(OWNER+0x68,1)
    p.put(OWNER+0x60,0x10000900,8);p.put(OWNER+0xe8,0x10002000,8);p.put(0x10002020,0)
    incoming.clear();queues.clear();callbacks.clear();column_calls.clear();column_allowed=True
    objects=[0x10003000+i*0x100 for i in range(4)]+[0]
    for i,address in enumerate(objects[:4]):
        p.emu.mem_write(address,bytes(0x100));p.put(address,0x100d0000,8);p.put(address+0x10,i+1)
    ops=['V,1,1','Q,0,0','Q,4,0','Q,1,0','D,0,0','D,0,0','A,2,0','A,2,0','Q,2,0','E,2,0','R,3,0']
    for _ in range(60):
        op=rng.choice('AQR EVCPD'.replace(' ',''));i=rng.randrange(4 if op in 'AEV' else 5);ops.append(f'{op},{i},{rng.randrange(2)}')
    trace=[]
    for operation in ops:
        op,i,v=operation.split(',');i=int(i);v=int(v);obj=objects[i];returned=-1
        if op=='A':b.call(0x343d2dc,OWNER,obj)
        elif op=='Q':b.call(0x343d498,OWNER,obj)
        elif op=='R':b.call(0x343d3f4,OWNER,obj)
        elif op=='E':
            # Event delegate storage/invocation boundary; each original closure
            # callback executes its actual native body in registration order.
            for closure in callbacks.get(obj,[]):b.call(0x343d8d0,closure)
        elif op=='V':p.put(obj+0x14,v,1)
        elif op=='C':p.put(OWNER+0xf8,v,1)
        elif op=='P':column_allowed=bool(v)
        elif op=='D':
            result=b.call(0x343d5bc,OWNER);returned=0 if result==0 else objects.index(result)+1
        ready=b.call(0x343d1f8,OWNER)
        contains=[b.call(0x343d69c,OWNER,obj) for obj in objects]
        trace.append(','.join(map(str,[p.read(OWNER+0xf8,1),ready,returned,*contains,len(column_calls),column_calls[-1]])))
    cases.append(';'.join(ops));expected.append('|'.join(trace));operation_count+=len(ops)

harness='''using System;using Whitebox.Foundation;
namespace UnityEngine{public static class Debug{public static void LogError(object value){}}}
class Item:OriginalBoardItem{public Item(int id):base(id,1){}public void View(bool value){HasView=value;}}
class Fall:IOriginalColumnRefill{public bool Allowed=true;public int Calls,X;public bool CanRefillFromColumn(int x){Calls++;X=x;return Allowed;}}
class Verify{static void Main(){string l;while((l=Console.ReadLine())!=null){var f=new OriginalFillingCell(3,0,1,1,1,false,true,2);var fall=new Fall();var items=new Item[]{new Item(1),new Item(2),new Item(3),new Item(4),null};var ops=l.Split(';');var trace=new string[ops.Length];for(int n=0;n<ops.Length;n++){var a=ops[n].Split(',');int i=int.Parse(a[1]);bool v=a[2]=="1";int returned=-1;switch(a[0]){case "A":f.AddItemToComingFromTopBoard(items[i]);break;case "Q":f.AddToTopBoardItemQueue(items[i]);break;case "R":f.RemoveItemComingFromTopBoard(items[i]);break;case "E":items[i].InvokeExplodeEvent();break;case "V":items[i].View(v);break;case "C":f.CanRefill=v;break;case "P":fall.Allowed=v;break;case "D":var item=f.GetQueuedItemFromTopBoard();returned=item==null?0:item.Id;break;}bool ready=f.IsReadyToRefill(fall);var values=new int[10];values[0]=f.CanRefill?1:0;values[1]=ready?1:0;values[2]=returned;for(int k=0;k<5;k++)values[k+3]=f.ContainsInTopQueue(items[k])?1:0;values[8]=fall.Calls;values[9]=fall.X;trace[n]=string.Join(",",values);}Console.WriteLine(string.Join("|",trace));}}}'''
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder);source=folder/'Verify.cs';source.write_text(harness,encoding='utf-8');exe=folder/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
    subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),*[str(runtime/n) for n in ('OriginalCellMediation.cs','OriginalGridIterator.cs','OriginalFillingCell.cs')]],capture_output=True,check=True)
    actual=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8',check=True).stdout.lstrip('\ufeff').splitlines()
    assert actual==expected,next(((cases[i],a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e),'Output count differs')
report={'passed':True,'native_traces':24,'native_operations':operation_count,'original_rvas':['0x343d1f8','0x343d2dc','0x343d3f4','0x343d498','0x343d5bc','0x343d69c','0x343d8d0','0x2b45d70'],'scope':'Original filling incoming/queue/refill functions and explode callback bodies versus compiled C# traces. HashSet/Queue storage, delegate allocation/storage/invocation, metadata and FallManager column decisions supplied at boundaries. HasView getter executes natively. Single-target board permission callback executes natively; managed shared dispatch exercises multiple retained targets. Falling, visual creation and item recycle/reset lifecycle remain incomplete.'}
(p.PROJECT/'filling_queue_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(json.dumps(report))
