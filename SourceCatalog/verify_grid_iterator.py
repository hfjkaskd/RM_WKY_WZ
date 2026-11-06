"""Compare horizontal traversal plus iterator reset/advance with original ARM64."""
import sys
sys.dont_write_bytecode=True
import verify_set_parser as p
import json,subprocess,tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_PC,UC_ARM64_REG_LR,UC_ARM64_REG_SP

def hook(uc,a,size,data):
    v=None
    if a==0x320819c: v=0x10000500
    elif a==0x31932c8: v=right
    elif a==0x3207954:
        point=uc.reg_read(UC_ARM64_REG_X1)
        xy=(point&0xffffffff,point>>32);requests.append(xy)
        v=0x10000600 if xy==(3,4) else next_row if xy==(0,5) else 0
    elif a==0x3196c8c: v=int(in_board)
    if v is not None:
        uc.reg_write(UC_ARM64_REG_X0,v);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,hook)

def call(rva,arg=0):
    p.emu.reg_write(UC_ARM64_REG_X0,0x10000000);p.emu.reg_write(UC_ARM64_REG_X1,arg)
    p.emu.reg_write(UC_ARM64_REG_SP,0x101ff000);p.emu.reg_write(UC_ARM64_REG_LR,0x20000000)
    p.emu.emu_start(rva,0x20000000,count=100000)
    assert p.emu.reg_read(UC_ARM64_REG_PC)==0x20000000
    return {0:0,0x10000600:1,0x10000800:2,0x10000900:3}[p.read(0x10000010,8)]

cases=[];expected=[]
for right in (0,0x10000800):
    for next_row in (0,0x10000900):
        for in_board in (False,True):
            p.emu.mem_write(0x10000000,bytes(0x10000));requests=[]
            p.put(0x10000100,0x10000300,8);p.put(0x10000110,0x10000200,8)
            p.put(0x10000300+0x178,0x319b9b8,8);p.put(0x10000300+0x188,0x319b9f0,8)
            p.put(0x100002f0,0x10000400,8);p.put(0x10000510,3|(4<<32),8)
            p.put(0x1000061c,4);p.put(0x10000620,0x10000700,8)
            values=[call(0x319c000,0x10000100),call(0x318ecc8),call(0x318ecc8),call(0x318ec2c),call(0x318ecc8)]
            expected.append(','.join(map(str,values)))
            assert requests==[(3,4)]+([] if right else [(0,5)])+[(3,4)],requests
            cases.append(f'{int(bool(right))},{int(bool(next_row))},{int(in_board)}')
harness='''using System;using Whitebox.Foundation;
namespace UnityEngine{public static class Debug{public static void LogError(object a){}}}
class Grid:IOriginalGridAccess{
 public OriginalGridCell First=new OriginalGridCell(3,4,1),Right=new OriginalGridCell(4,4,1),Row=new OriginalGridCell(0,5,1);public bool HaveRow,Inside;
 public OriginalGridCell CurrentBoardBottomLeftCell=>First;
 public OriginalGridCell GetCell(int x,int y){if(x!=0||y!=5)throw new Exception("Unexpected row point");return HaveRow?Row:null;}
 public bool IsInCurrentBoard(OriginalGridCell c)=>Inside;
 public int Id(OriginalGridCell c)=>c==null?0:c==First?1:c==Right?2:3;
}
class Verify{static void Main(){string l;while((l=Console.ReadLine())!=null){var p=l.Split(',');var grid=new Grid{HaveRow=p[1]=="1",Inside=p[2]=="1"};grid.First.Right=p[0]=="1"?grid.Right:null;var iterator=new OriginalGridIterator(new OriginalBottomLeftHorizontalStrategy(grid));string s=grid.Id(iterator.Cell)+","+grid.Id(iterator.MoveNext())+","+grid.Id(iterator.MoveNext());iterator.Reset();s+=","+grid.Id(iterator.Cell)+","+grid.Id(iterator.MoveNext());Console.WriteLine(s);}}}
'''
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder);source=folder/'Verify.cs';source.write_text(harness,encoding='utf-8');exe=folder/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
    subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),str(runtime/'OriginalCellMediation.cs'),str(runtime/'OriginalGridIterator.cs')],check=True,capture_output=True)
    actual=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',text=True,encoding='utf-8',capture_output=True,check=True).stdout.lstrip('\ufeff').splitlines()
    assert actual==expected,(actual,expected)
report={'passed':True,'native_iterator_cases':len(cases),'original_rvas':['0x319b9b8','0x319b9f0','0x319c000','0x318ec2c','0x318ecc8'],'scope':'Actual native horizontal traversal and iterator constructor/reset/advance. Board lookup, right-neighbor lookup and membership supplied at boundaries. Grid construction, neighbor linking and other traversal strategies remain unverified.'}
(p.PROJECT/'grid_iterator_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(json.dumps(report))
