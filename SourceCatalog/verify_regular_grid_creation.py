"""Compare regular CreateCells and filling initialization against native ARM64."""
import sys
sys.dont_write_bytecode=True
import verify_board_layout as b
import json,random,subprocess,tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_X3,UC_ARM64_REG_X4,UC_ARM64_REG_X5,UC_ARM64_REG_LR,UC_ARM64_REG_PC
p=b.p
cell_index=0
filling_objects=set()
refill_bindings=[]
def setup_neighbors(cell,point):
    neighbors=p.allocate(0x40);array=p.allocate(0x80)
    p.put(cell+0x20,neighbors,8);p.put(neighbors+0x10,point,8);p.put(neighbors+0x18,array,8)
    p.put(array,0x100d0000,8);p.put(array+0x18,8)

def boundary(uc,address,size,data):
    global cell_index
    regs=[uc.reg_read(r) for r in (UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_X3,UC_ARM64_REG_X4,UC_ARM64_REG_X5)]
    x0,x1,x2,x3,x4,x5=regs;value=None
    if address==0x1e197d8:value=0
    elif address==0x1e191a0:value=int(wall)
    elif address==0x1c1f6dc:cell_index=x1+b.width*x2;value=0
    elif address==0x1620404:value=fills[cell_index]
    elif address==0x1620448:value=predefined[cell_index]
    elif address==0x3195020:
        array=p.allocate(0x1000);bounds=p.allocate(0x20)
        p.put(x0+0x10,array,8);p.put(x0+0x18,x1);p.put(x0+0x1c,x2)
        p.put(array+0x10,bounds,8);p.put(bounds,x1);p.put(bounds+0x10,x2);value=0
    elif address==0x319c4e4:
        # Base-cell mediator/context constructors are a supplied boundary. Keep
        # the exact constructor arguments and neighbor storage, not visual objects.
        p.put(x0,0x100d0000,8);p.put(x0+0x18,x4,8);p.put(x0+0x68,x5)
        p.put(x0+0xa0,x2);p.put(x0+0xa4,x3);p.put(x0+0x58,0x10000000,8)
        setup_neighbors(x0,x4);value=0
    elif address in (0x2214c98,0x145b3a0,0x32099b8,0x3207f00):value=0
    elif address==0x343cefc:value=0
    elif address==0x343ce30:
        # Delegate-storage boundary: filling constructor's native branch runs.
        refill_bindings.append(1);value=0
    elif address==0x343d004:filling_objects.add(x0)
    if value is not None:
        uc.reg_write(UC_ARM64_REG_X0,value&0xffffffffffffffff);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,boundary)
# Initialized static eight-direction list, supplied as in the neighbor oracle.
klass=0x100d1000;p.put(klass+0xe0,1);p.put(klass+0xb8,0x100d1200,8)
p.put(0x100d1200,0x100d1300,8);p.put(0x100d1318,8)
for i in range(8):p.put(0x100d1320+i*4,i)
neighbor_metadata=p.read(0x53b2db8,8)
p.put(neighbor_metadata,klass,8);b.resolved_slots.add(neighbor_metadata)
p.put(0x100d0000+0x238,0x319cfdc,8)

cases=[];expected=[]
for seed in range(32):
    rng=random.Random(seed);b.width=3;b.height=8;b.scroll=bool(seed%2);b.water=False;wall=bool(seed%4>=2)
    b.items=[rng.choice((0,1,2,7,8,9,13,14)) for _ in range(b.width*b.height)]
    if b.scroll:
        # Separators are non-normal across the entire row. Normal cells must
        # belong to a board; native normal-cell counters index by board ID.
        for y in (3,4):
            for x in range(b.width):b.items[x+b.width*y]=17
        b.items[0]=1;b.items[b.width*2]=1
    fills=[rng.randrange(7) for _ in b.items];predefined=[rng.randrange(2) for _ in b.items]
    p.heap=0x10010000;p.emu.mem_write(0x10000000,bytes(0x1000))
    p.put(0x10000090,0x10000200,8);p.put(0x100002a3,0,1)
    filling_objects.clear();refill_bindings.clear()
    b.call(0x320aee0,0x10000000,0x10000100)
    grid=p.read(0x10000040,8);array=p.read(grid+0x10,8);manager=p.read(0x100000f0,8);boards=p.read(manager+0x18,8)
    models=[p.read(array+0x20+(x*b.height+y)*8,8) for y in range(b.height) for x in range(b.width)]
    snapshot=[]
    for i,cell in enumerate(models):
        filling=cell in filling_objects
        if filling:
            # Exercise native refill enabling with mismatched then matching IDs.
            board=p.signed(p.read(cell+0x68));b.call(0x343d1c0,cell,board+100)
            initial=p.read(cell+0xf8,1);b.call(0x343d1c0,cell,board)
            enabled=p.read(cell+0xf8,1)
        else:initial=enabled=-1
        snapshot.append(','.join(map(str,[p.signed(p.read(cell+0xa0)),p.signed(p.read(cell+0xa4)),p.signed(p.read(cell+0x68)),int(filling),p.read(cell+0xdc,1) if filling else -1,initial,enabled,p.read(cell+0x80,1)])))
    counts=[p.read(p.read(boards+0x20+i*8,8)+0x40) for i in range(p.read(boards+0x18))]
    assert len(refill_bindings)==(len(filling_objects) if wall else 0)
    cases.append(';'.join((str(int(b.scroll)),str(int(wall)),','.join(map(str,b.items)),','.join(map(str,fills)),','.join(map(str,predefined)))))
    expected.append('|'.join(snapshot)+';'+','.join(map(str,counts)))

harness='''using System;using Whitebox.Foundation;using Whitebox.Foundation.ConfigData;
namespace UnityEngine{public static class Debug{public static void LogError(object value){}}}
class Verify{static void Main(){string l;while((l=Console.ReadLine())!=null){var a=l.Split(';');var items=Array.ConvertAll(a[2].Split(','),int.Parse);var fills=Array.ConvertAll(a[3].Split(','),int.Parse);var pre=Array.ConvertAll(a[4].Split(','),int.Parse);var config=new FTiledGrid{Width=3,Height=8,Cells=new FTiledCell[24]};for(int i=0;i<24;i++)config.Cells[i]=new FTiledCell{FillType=fills[i],IsPredefined=pre[i]!=0};OriginalBoardLayout layout;var grid=OriginalRegularGridCreation.Create(config,items,a[0]=="1",false,a[1]=="1",out layout);var snapshots=new string[24];for(int i=0;i<24;i++){var c=grid.GetCell(i%3,i/3);var f=c as OriginalFillingCell;int initial=-1,enabled=-1;if(f!=null){f.AllowRefillForBoard(c.BoardId+100);initial=f.CanRefill?1:0;f.AllowRefillForBoard(c.BoardId);enabled=f.CanRefill?1:0;var item=new OriginalBoardItem(1,1);f.AddCreatedItem(item);if(!f.HasCreatedItem||f.LastCreatedItem!=item)throw new Exception("Created item differs");f.RemoveCreatedItem();if(f.HasCreatedItem)throw new Exception("Removal differs");}snapshots[i]=string.Join(",",new[]{c.CellType,c.FillType,c.BoardId,f==null?0:1,f==null?-1:f.IsPredefined?1:0,initial,enabled,c.IsBottomCell?1:0});}var counts=new int[layout.Count];for(int i=0;i<counts.Length;i++)counts[i]=layout[i].NormalCellCount;Console.WriteLine(string.Join("|",snapshots)+";"+string.Join(",",counts));}}}'''
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder);source=folder/'Verify.cs';source.write_text(harness,encoding='utf-8');exe=folder/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
    names=('OriginalCellMediation.cs','OriginalGridIterator.cs','OriginalCellGrid.cs','OriginalLevelRules.cs','OriginalBoardLayout.cs','OriginalFillingCell.cs','OriginalRegularGridCreation.cs','OriginalConfigData.cs')
    subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),*[str(runtime/n) for n in names]],capture_output=True,check=True)
    actual=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8',check=True).stdout.lstrip('\ufeff').splitlines()
    assert actual==expected,next(((cases[i],a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e),'Output count differs')
report={'passed':True,'native_regular_grid_cases':32,'native_created_cells':768,'original_rvas':['0x320aee0','0x320b28c','0x343d004','0x343d1c0'],'scope':'Original regular-cell creation loop, branch selection, filling constructor refill/predefined state, board normal-cell counters and neighbor cache compared with compiled C#. Parser/context services, base-cell mediator constructors, managed storage, delegate storage, goals and CacheFillingCells are supplied boundaries. Native filling-event subscription branch counts are checked; managed shared-event dispatch and incoming/falling queues are not verified. Shifter grids, item execution and visuals remain incomplete.'}
(p.PROJECT/'regular_grid_creation_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(json.dumps(report))
