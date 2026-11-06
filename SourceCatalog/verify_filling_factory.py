"""Compare filling-factory orchestration and call order with original ARM64."""
import sys
sys.dont_write_bytecode=True
import verify_board_layout as b
import itertools,json,struct,subprocess,tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_LR,UC_ARM64_REG_SP,UC_ARM64_REG_PC,UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2
p=b.p
ITEMS=(0x10004000,0x10005000,0x10006000)
def float_read(uc,reg):return struct.unpack('<f',(uc.reg_read(reg)&0xffffffff).to_bytes(4,'little'))[0]
def float_put(address,value):p.emu.mem_write(address,struct.pack('<f',value))
def vector(uc):return ','.join(str(int(float_read(uc,reg))) for reg in (UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2))
def identity(address):return 0 if not address else ITEMS.index(address)+1
def boundary(uc,address,size,data):
    x0,x1=(uc.reg_read(r) for r in (UC_ARM64_REG_X0,UC_ARM64_REG_X1));value=None
    if address==0x343d1f8:log.append('R');value=ready
    elif address==0x343d5bc:log.append('Q');value=ITEMS[0] if queued else 0
    elif address==0x2b45cd4:log.append('U1');value=0
    elif address==0x343e6bc:
        log.append('P')
        for reg,v in ((UC_ARM64_REG_S0,10),(UC_ARM64_REG_S1,20),(UC_ARM64_REG_S2,0)):uc.reg_write(reg,int.from_bytes(struct.pack('<f',v),'little'))
        value=0
    elif address==0x2b485ec:log.append('S'+str(p.read(x0+0x10))+':'+vector(uc));value=0
    elif address==0x1c9159c:log.append('D');value=ITEMS[1] if predefined==2 else 0
    elif address==0x1c935f0:log.append('H');value=int(target_mode!=0)
    elif address==0x23efa00:log.append('T');value=4 if target_mode==2 else 0
    elif address==0x23efcec:log.append('F');value=fill_id
    elif address==0x23efec0:log.append('C');value=9
    elif address==0x23f0038:log.append('N'+str(x1)+':'+vector(uc));value=ITEMS[2]
    elif address==0x23ef5b0:log.append('O'+str(x1));value=0
    elif address==0x20000100:log.append('G'+str(identity(x0)));value=p.read(x0+0xb0,8)
    elif address==0x20000110:value=100+identity(x0)
    elif address==0x20000120:log.append('V'+str(identity(x0))+':'+vector(uc));p.put(x0+0x14,1,1);value=0
    elif address==0x20000130:log.append('M'+str(p.read(x0+0x10)));value=0
    if value is not None:
        uc.reg_write(UC_ARM64_REG_X0,value);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,boundary)
slot=p.read(0x53dca28,8);p.put(slot,0x100d4000,8);b.resolved_slots.add(slot)
p.put(0x100d4000+0xb8,0x100d4200,8);float_put(0x100d4210,1);float_put(0x100d4214,2);p.put(0x573994c,1,1)
for offset,function in ((0x358,0x2b45d70),(0x378,0x20000110),(0x408,0x20000120),(0x418,0x20000100)):p.put(0x100d0000+offset,function,8)
p.put(0x100d2000+0x268,0x20000130,8)
cases=[];expected=[]
for ready,queued,predefined,pre_view,target_mode,fill_id in itertools.product(range(2),range(2),range(3),range(2),range(3),(0,7)):
    p.emu.mem_write(0x10000000,bytes(0x10000));p.heap=0x10010000;log=[]
    p.put(0x10000100,0x10002000,8);p.put(0x10000108,0x10002100,8)
    p.put(0x10001018,3);p.put(0x1000101c,6);p.put(0x100010dc,int(predefined!=0),1);p.put(0x100010b8,0x10007000,8)
    for i,item in enumerate(ITEMS):
        view=0x10008000+i*0x100;p.put(item,0x100d0000,8);p.put(item+0x14,pre_view if i==1 else 1,1)
        p.put(item+0x38,0x10009000+i*0x100,8);p.put(item+0xb0,view,8);p.put(view,0x100d2000,8);p.put(view+0x10,i+1)
    for reg,value in ((UC_ARM64_REG_X0,0x10000000),(UC_ARM64_REG_X1,0x10001000),(UC_ARM64_REG_X2,0),(UC_ARM64_REG_SP,0x101ff000),(UC_ARM64_REG_LR,0x20000000)):p.emu.reg_write(reg,value)
    for reg,value in ((UC_ARM64_REG_S0,3),(UC_ARM64_REG_S1,7),(UC_ARM64_REG_S2,0)):p.emu.reg_write(reg,int.from_bytes(struct.pack('<f',value),'little'))
    p.emu.emu_start(0x23ef7cc,0x20000000,count=100000)
    assert p.emu.reg_read(UC_ARM64_REG_PC)==0x20000000
    returned=p.emu.reg_read(UC_ARM64_REG_X0);last=p.read(0x100010e0,8)
    cases.append(','.join(map(str,(ready,queued,predefined,pre_view,target_mode,fill_id))))
    expected.append(str(identity(returned))+','+str(identity(last))+';'+ '|'.join(log))
harness='''using System;using System.Collections.Generic;using Whitebox.Foundation;
namespace UnityEngine{public static class Debug{public static void LogError(object value){}}}
class Item:OriginalBoardItem{public Item(int id,bool view):base(id,100+id){HasView=view;}public void View(){HasView=true;}}
class View:IOriginalFillingItemView{public int Id;public List<string> Log;public void SetPosition(float x,float y,float z){Log.Add("S"+Id+":"+x+","+y+","+z);}public void EnableFillMask(){Log.Add("M"+Id);}}
class Backend:IOriginalFillingCreationBackend,IOriginalFillingPlacementSpace,IOriginalColumnRefill{
public int[] A;public List<string> Log=new List<string>();public Item[] Items;public View[] Views;public Backend(int[] a){A=a;Items=new[]{new Item(1,true),new Item(2,a[3]!=0),new Item(3,true)};Views=new View[3];for(int i=0;i<3;i++)Views[i]=new View{Id=i+1,Log=Log};}
public bool CanRefillFromColumn(int x){Log.Add("R");return A[0]!=0;}public float OffsetX{get{return 1;}}public float OffsetY{get{return 2;}}public void GetCellViewPosition(OriginalFillingCell c,out float x,out float y){Log.Add("P");x=10;y=20;}
public OriginalBoardItem GetPredefinedItem(OriginalFillingCell c){Log.Add("D");return A[2]==2?Items[1]:null;}public bool HasTarget(int col){Log.Add("H");return A[4]!=0;}public int GetRandomTargetColumnItem(OriginalFillingCell c,int col){Log.Add("T");return A[4]==2?4:0;}public int GetRandomFillItem(OriginalFillingCell c){Log.Add("F");return A[5];}public int GetRandomColor(OriginalFillingCell c){Log.Add("C");return 9;}public OriginalBoardItem CreateItemAt(int id,float x,float y,float z){Log.Add("N"+id+":"+x+","+y+","+z);return Items[2];}public void OnNewItemCreated(int type){Log.Add("O"+type);}public IOriginalFillingItemView GetItemView(OriginalBoardItem item){Log.Add("G"+item.Id);return Views[item.Id-1];}public void CreateView(OriginalBoardItem item,float x,float y,float z){Log.Add("V"+item.Id+":"+x+","+y+","+z);((Item)item).View();}}
class Verify{static void Main(){string l;while((l=Console.ReadLine())!=null){var a=Array.ConvertAll(l.Split(','),int.Parse);var backend=new Backend(a);var cell=new OriginalFillingCell(3,6,1,0,1,a[2]!=0,false,0);if(a[1]!=0)cell.AddToTopBoardItemQueue(backend.Items[0]);var factory=new OriginalFillingItemFactory(backend,backend,backend);var item=factory.CreateItemForFillingCellAt(3,7,cell);backend.Log.Insert(1,"Q");if(a[0]==0)backend.Log.Remove("Q");else if(a[1]!=0)backend.Log.Insert(2,"U1");Console.WriteLine((item==null?0:item.Id)+","+(cell.LastCreatedItem==null?0:cell.LastCreatedItem.Id)+";"+string.Join("|",backend.Log));}}}'''
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder);source=folder/'Verify.cs';source.write_text(harness,encoding='utf-8');exe=folder/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
    names=('OriginalCellMediation.cs','OriginalGridIterator.cs','OriginalFillingCell.cs','OriginalFallRefillColumns.cs','OriginalFillingItemPlacement.cs','OriginalFillingItemFactory.cs')
    subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),*[str(runtime/n) for n in names]],capture_output=True,check=True)
    actual=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8',check=True).stdout.lstrip('\ufeff').splitlines()
    assert actual==expected,next(((cases[i],a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e),'Output count differs')
report={'passed':True,'native_factory_cases':144,'original_rvas':['0x23ef7cc','0x343d1e4','0x2b45d70'],'scope':'Actual native filling factory decision/call order, queued reposition coordinates, predefined override, target/fill/color fallback, creation notification and fill-mask-before-registration versus compiled C#. Containers, random selection, concrete creation, view operations, readiness and queue return are supplied boundaries. Managed Q/U trace markers represent internal calls verified by separate queue/mediation oracles; visual rendering, concrete item creation and full gameplay remain incomplete.'}
(p.PROJECT/'filling_factory_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report))
