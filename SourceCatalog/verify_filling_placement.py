"""Compare FallManager filling placement with original ARM64."""
import sys
sys.dont_write_bytecode=True
import verify_board_layout as b
import itertools,json,struct,subprocess,tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_LR,UC_ARM64_REG_PC,UC_ARM64_REG_S0,UC_ARM64_REG_S1
p=b.p
def boundary(uc,address,size,data):
    global creator_calls,component_calls,target_item,coordinates
    x0,x1=(uc.reg_read(r) for r in (UC_ARM64_REG_X0,UC_ARM64_REG_X1));value=None
    if address==0x220ca20:value=int(disabled if x0==0x10009000 else pending)
    elif address==0x2b45c94:value=int(p.read(x0+0x20,8)!=0)
    elif address==0x3448800:value=int(target_item!=0)
    elif address==0x344866c:
        value=int(target_item in (0,x1))
        if value:target_item=x1
    elif address==0x23ef7cc:
        creator_calls+=1;coordinates=tuple(struct.unpack('<f',(uc.reg_read(reg)&0xffffffff).to_bytes(4,'little'))[0] for reg in (UC_ARM64_REG_S0,UC_ARM64_REG_S1))
        assert x1==0x10001000
        value=0 if no_creation else 0x10006000
    elif address==0xfcd934:value=0x100d2800
    elif address==0x20000100:
        assert x0==0x10008000 and x1==0x10001000
        component_calls+=1;value=0
    if value is not None:
        uc.reg_write(UC_ARM64_REG_X0,value);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,boundary)
p.put(0x100d2800,0x20000100,8)
cases=[];expected=[]
for disabled,pending,held,occupied,no_creation in itertools.product(range(2),range(2),range(3),range(2),range(2)):
    p.emu.mem_write(0x10000000,bytes(0xc000));p.heap=0x10010000
    p.put(0x10000020,0x10009000,8);p.put(0x10000018,0x10009100,8)
    p.put(0x10000038,0x1000a000,8);p.put(0x1000a038,0x1000b000,8)
    p.put(0x10001018,3);p.put(0x1000101c,6)
    p.put(0x10002010,0x10003000,8)
    p.put(0x10004038,0x10005000,8);p.put(0x10005020,0x10002000 if held==2 else 0,8)
    p.put(0x100010e0,0x10004000 if held else 0,8)
    p.put(0x10006040,0x10008000,8);p.put(0x10008000,0x100d2000,8)
    target_item=0x10004100 if occupied else 0;creator_calls=component_calls=0;coordinates=(0,0)
    # Third integer argument is the target cell, not method metadata.
    from unicorn.arm64_const import UC_ARM64_REG_X2,UC_ARM64_REG_SP
    for reg,value in ((UC_ARM64_REG_X0,0x10000000),(UC_ARM64_REG_X1,0x10001000),(UC_ARM64_REG_X2,0x10002000),(UC_ARM64_REG_SP,0x101ff000),(UC_ARM64_REG_LR,0x20000000)):p.emu.reg_write(reg,value)
    p.emu.emu_start(0x32112d0,0x20000000,count=100000)
    assert p.emu.reg_read(UC_ARM64_REG_PC)==0x20000000
    identity={0:0,0x10006000:1,0x10004100:2,0x10004000:3}[target_item]
    can_refill=b.call(0x32120bc,0x10000000,3)
    cases.append(','.join(map(str,(disabled,pending,held,occupied,no_creation))))
    expected.append(','.join(map(str,(identity,creator_calls,component_calls,int(coordinates[0]),int(coordinates[1]),can_refill))))
harness='''using System;using Whitebox.Foundation;
namespace UnityEngine{public static class Debug{public static void LogError(object value){}}}
class Fall:IOriginalFillingFallComponent{public int Calls;public void SetFillingCell(OriginalFillingCell cell){if(cell.X!=3||cell.Y!=6)throw new Exception("Filling cell differs");Calls++;}}
class Creator:IOriginalFillingItemCreation{public bool Empty;public int Calls;public float X,Y;public OriginalBoardItem Item;public OriginalBoardItem CreateItemForFillingCellAt(float x,float y,OriginalFillingCell cell){Calls++;X=x;Y=y;return Empty?null:Item;}}
class Verify{static void Main(){string l;while((l=Console.ReadLine())!=null){var a=Array.ConvertAll(l.Split(','),int.Parse);var columns=new OriginalFallRefillColumns();if(a[1]!=0)columns.EnableReFillForTopColumn(3);if(a[0]!=0)columns.DisableReFillForTopColumn(3);var filling=new OriginalFillingCell(3,6,1,0,1,false,false,0);var target=new OriginalGridCell(0,0,1);if(a[3]!=0)target.Mediation.SetTargetItem(new OriginalBoardItem(2,1));if(a[2]!=0){var held=new OriginalBoardItem(3,1);filling.AddCreatedItem(held);if(a[2]==2)new OriginalCellMediation().SetTargetItem(held);}var fall=new Fall();var creator=new Creator{Empty=a[4]!=0,Item=new OriginalBoardItem(1,1){FallComponent=fall}};OriginalFillingItemPlacement.CreateItemFromFillingCell(columns,filling,target,creator);Console.WriteLine(string.Join(",",new[]{target.Mediation.TargetItem==null?0:target.Mediation.TargetItem.Id,creator.Calls,fall.Calls,(int)creator.X,(int)creator.Y,columns.CanRefillFromColumn(3)?1:0}));}}}'''
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder);source=folder/'Verify.cs';source.write_text(harness,encoding='utf-8');exe=folder/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
    names=('OriginalCellMediation.cs','OriginalGridIterator.cs','OriginalFillingCell.cs','OriginalFallRefillColumns.cs','OriginalFillingItemPlacement.cs')
    subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),*[str(runtime/n) for n in names]],capture_output=True,check=True)
    actual=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8',check=True).stdout.lstrip('\ufeff').splitlines()
    assert actual==expected,next(((cases[i],a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e),'Output count differs')
report={'passed':True,'native_placement_cases':96,'original_rvas':['0x32112d0','0x32120bc','0x343cfd0'],'scope':'Original placement control flow, disabled/pending column checks, held-item reuse, occupied targets, null creation results, creation coordinates and fall-component initialization versus compiled C#. HashSet storage, item/target mediation getters/setters, ItemCreator and component interface dispatch are supplied boundaries. Actual item factory, fall execution, views and full ManualUpdate remain incomplete.'}
(p.PROJECT/'filling_placement_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report))
