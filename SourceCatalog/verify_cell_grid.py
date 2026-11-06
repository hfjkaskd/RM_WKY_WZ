"""Compare CellGrid lookup and downward search with original ARM64."""
import sys
sys.dont_write_bytecode=True
import verify_set_parser as p
import itertools,json,subprocess,tempfile
from pathlib import Path
from unicorn.arm64_const import UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_LR,UC_ARM64_REG_SP,UC_ARM64_REG_PC

def call(rva,owner,x=0,y=0):
    p.emu.reg_write(UC_ARM64_REG_X0,owner);p.emu.reg_write(UC_ARM64_REG_X1,x&0xffffffffffffffff);p.emu.reg_write(UC_ARM64_REG_X2,y&0xffffffffffffffff)
    p.emu.reg_write(UC_ARM64_REG_SP,0x101ff000);p.emu.reg_write(UC_ARM64_REG_LR,0x20000000)
    p.emu.emu_start(rva,0x20000000,count=100000)
    assert p.emu.reg_read(UC_ARM64_REG_PC)==0x20000000
    return p.emu.reg_read(UC_ARM64_REG_X0)

def setup():
    p.emu.mem_write(0x10000000,bytes(0x10000))
    p.put(0x10000040,0x10000200,8);p.put(0x10000210,0x10000400,8)
    p.put(0x10000218,1);p.put(0x1000021c,4)
    p.put(0x10000410,0x10000600,8);p.put(0x10000600,1);p.put(0x10000610,4)
    p.put(0x10000800+0x238,0x319cfdc,8)

def put_cell(y,state):
    address=0x10001000+y*0x200
    if state is None:return 0
    type,board,fill=state
    p.put(address,0x10000800,8);p.put(address+0x18,0);p.put(address+0x1c,y)
    p.put(address+0x68,board);p.put(address+0xa0,type);p.put(address+0xa4,fill)
    p.put(0x10000420+y*8,address,8)
    return address

cases=[];expected=[]
states=(None,(0,1,0),(1,1,0),(0,1,1),(1,2,0),(1,1,1))
for column in itertools.product(states,repeat=3):
    setup()
    for y,state in enumerate(column):put_cell(y,state)
    top=put_cell(3,(1,1,0))
    result=call(0x320b824,0x10000000,top)
    expected.append(str((result-0x10001000)//0x200+1 if result else 0))
    cases.append(';'.join('null' if s is None else ','.join(map(str,s)) for s in column))
lookup_expected=[]
for x in (-1,0,1,2147483647):
    for y in (-1,0,3,4):
        result=call(0x31950bc,0x10000200,x,y)
        lookup_expected.append((x,y,int(bool(result))))
harness='''using System;using Whitebox.Foundation;
namespace UnityEngine{public static class Debug{public static void LogError(object a){}}}
class Verify{static void Main(){string l;while((l=Console.ReadLine())!=null){var grid=new OriginalCellGrid(1,4);var a=l.Split(';');for(int y=0;y<3;y++)if(a[y]!="null"){var v=Array.ConvertAll(a[y].Split(','),int.Parse);grid.SetCell(0,y,new OriginalGridCell(0,y,v[0],v[1],v[2]));}var top=new OriginalGridCell(0,3,1,1);grid.SetCell(0,3,top);var result=grid.GetFirstNormalBelow(top);Console.WriteLine(result==null?0:result.Y+1);grid.SetCell(-1,0,top);grid.SetCell(1,0,top);if(grid.GetCell(-1,0)!=null||grid.GetCell(1,0)!=null||grid.GetCell(0,4)!=null)throw new Exception("Bounds differ");}}}
'''
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder);source=folder/'Verify.cs';source.write_text(harness,encoding='utf-8');exe=folder/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
    subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),*[str(runtime/n) for n in ('OriginalCellMediation.cs','OriginalGridIterator.cs','OriginalCellGrid.cs')]],check=True,capture_output=True)
    actual=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',text=True,encoding='utf-8',capture_output=True,check=True).stdout.lstrip('\ufeff').splitlines()
    assert actual==expected,next(((cases[i],a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e),'Output count differs')
assert all(not value for x,y,value in lookup_expected if x!=0 or y<0 or y>=4)
report={'passed':True,'native_downward_search_cases':len(cases),'native_lookup_cases':len(lookup_expected),'original_rvas':['0x320b824','0x31950bc','0x319cfdc','0x3195f9c'],'scope':'Actual native downward search, regular-cell filling status, multidimensional lookup and bounds. Managed arrays supplied at the storage boundary. Shifter overrides, cell creation, neighbor caching and presentation remain unverified.'}
(p.PROJECT/'cell_grid_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(json.dumps(report))
