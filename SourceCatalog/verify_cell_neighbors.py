"""Compare neighbor points and grid cache against original ARM64 instructions."""
import sys
sys.dont_write_bytecode = True
import verify_set_parser as p
import json, random, subprocess, tempfile
from pathlib import Path
from unicorn.arm64_const import UC_ARM64_REG_X0, UC_ARM64_REG_X1, UC_ARM64_REG_X2, UC_ARM64_REG_LR, UC_ARM64_REG_SP, UC_ARM64_REG_PC

def call(rva, owner, arg=0):
    for reg, value in ((UC_ARM64_REG_X0, owner), (UC_ARM64_REG_X1, arg), (UC_ARM64_REG_X2, 0), (UC_ARM64_REG_SP, 0x101ff000), (UC_ARM64_REG_LR, 0x20000000)):
        p.emu.reg_write(reg, value & 0xffffffffffffffff)
    p.emu.emu_start(rva, 0x20000000, count=1000000)
    assert p.emu.reg_read(UC_ARM64_REG_PC) == 0x20000000
    return p.emu.reg_read(UC_ARM64_REG_X0)

# Supply initialized metadata and array storage; the native cache/point/lookup/
# downward search/Set instructions themselves execute without interception.
for address in (0x5748130, 0x5747f62): p.put(address, 1, 1)
p.put(0x53839d0, 0x568abf0, 8); p.put(0x53b2db8, 0x568abe8, 8)
p.put(0x568abe8, 0x10008000, 8); p.put(0x568abf0, 0x10008200, 8)
for address in (0x10008000, 0x10008200): p.put(address+0xe0, 1)
p.put(0x100080b8, 0x10008400, 8); p.put(0x10008400, 0x10008600, 8)
p.put(0x10008618, 8)
for i in range(8): p.put(0x10008620+i*4, i)

lines=[]; expected=[]
for x,y in ((0,0),(3,4),(-3,-4),(2147483647,-2147483648)):
    p.put(0x10007010,x); p.put(0x10007014,y)
    for direction in (-1,0,1,2,3,4,5,6,7,8,2147483647):
        packed=call(0x31954e4,0x10007000,direction)
        lines.append(f'P;{x};{y};{direction}')
        expected.append(f'{p.signed(packed&0xffffffff)},{p.signed(packed>>32)}')

def identity(address):
    return 0 if address==0 else (address-0x10001000)//0x200+1

for seed in range(32):
    rng=random.Random(seed)
    p.emu.mem_write(0x10000000,bytes(0x7000))
    p.put(0x10000040,0x10000200,8)
    p.put(0x10000210,0x10000400,8); p.put(0x10000218,3); p.put(0x1000021c,4)
    p.put(0x10000410,0x10000600,8); p.put(0x10000600,3); p.put(0x10000610,4)
    p.put(0x10000800+0x238,0x319cfdc,8)
    states=[]
    for x in range(3):
        for y in range(4):
            i=x*4+y; cell=0x10001000+i*0x200; neighbors=cell+0x100; array=cell+0x140
            state=None if rng.randrange(5)==0 else (rng.randrange(2),rng.randrange(2),rng.randrange(6))
            states.append(state)
            if state is None: continue
            p.put(0x10000420+i*8,cell,8); p.put(cell,0x10000800,8)
            p.put(cell+0x18,x); p.put(cell+0x1c,y); p.put(cell+0x20,neighbors,8)
            p.put(cell+0x68,state[1]); p.put(cell+0xa0,state[0]); p.put(cell+0xa4,state[2])
            p.put(neighbors+0x10,x); p.put(neighbors+0x14,y); p.put(neighbors+0x18,array,8)
            p.put(array,0x10000800,8); p.put(array+0x18,8)
    call(0x320b3e8,0x10000000)
    snapshots=[]
    for i,state in enumerate(states):
        if state is None: snapshots.append('null'); continue
        cell=0x10001000+i*0x200; neighbors=cell+0x100; array=cell+0x140
        snapshots.append(','.join(map(str,[identity(p.read(array+0x20+d*8,8)) for d in range(8)]+[identity(p.read(neighbors+0x20,8)),p.read(cell+0x80,1)])))
    lines.append('G;'+';'.join('null' if s is None else ','.join(map(str,s)) for s in states))
    expected.append(';'.join(snapshots))

harness='''using System;using Whitebox.Foundation;
namespace UnityEngine{public static class Debug{public static void LogError(object value){}}}
class Verify{
static int Id(OriginalGridCell c){return c==null?0:c.X*4+c.Y+1;}
static void Main(){string line;while((line=Console.ReadLine())!=null){var a=line.Split(';');
if(a[0]=="P"){var n=new OriginalCellNeighbors(int.Parse(a[1]),int.Parse(a[2]));int x,y;n.GetPoint(int.Parse(a[3]),out x,out y);Console.WriteLine(x+","+y);continue;}
var grid=new OriginalCellGrid(3,4);for(int i=0;i<12;i++)if(a[i+1]!="null"){var v=Array.ConvertAll(a[i+1].Split(','),int.Parse);grid.SetCell(i/4,i%4,new OriginalGridCell(i/4,i%4,v[0],v[1],v[2]));}
grid.CacheNeighbors();var result=new string[12];for(int i=0;i<12;i++){var c=grid.GetCell(i/4,i%4);if(c==null){result[i]="null";continue;}var v=new int[10];for(int d=0;d<8;d++)v[d]=Id(c.Neighbors.Get(d));v[8]=Id(c.Neighbors.FirstNormalCellBelow);v[9]=c.IsBottomCell?1:0;result[i]=string.Join(",",v);
if(c.Right!=c.Neighbors.Get(3))throw new Exception("Right cache differs");var below=c.Neighbors.FirstNormalCellBelow;c.Neighbors.NextCell=c;c.Neighbors.Clear();for(int d=0;d<8;d++)if(c.Neighbors.Get(d)!=null)throw new Exception("Clear failed");if(c.Neighbors.FirstNormalCellBelow!=below||c.Neighbors.NextCell!=c)throw new Exception("Clear changed persistent references");}
Console.WriteLine(string.Join(";",result));}}}'''
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder);source=folder/'Verify.cs';source.write_text(harness,encoding='utf-8');exe=folder/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
    subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),*[str(runtime/n) for n in ('OriginalCellMediation.cs','OriginalGridIterator.cs','OriginalCellGrid.cs')]],capture_output=True,check=True)
    actual=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(lines)+'\n',capture_output=True,text=True,encoding='utf-8',check=True).stdout.lstrip('\ufeff').splitlines()
    assert actual==expected,next(((lines[i],a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e),'Output count differs')
report={'passed':True,'native_point_cases':44,'native_cache_grids':32,'original_rvas':['0x31954e4','0x343ba2c','0x320b3e8','0x319547c'],'scope':'Actual native cache, point offsets, lookup and downward search versus compiled C#. Initialized metadata, allocation/storage and all-eight-direction iteration list are supplied boundaries; static initializer list order, cell creation, shifter overrides and presentation are not verified. Clear persistence is source-inspected plus managed assertions.'}
(p.PROJECT/'cell_neighbors_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(json.dumps(report))
