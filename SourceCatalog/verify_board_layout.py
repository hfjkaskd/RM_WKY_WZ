"""Compare board partitioning with original ARM64 BoardManager.CreateBoard."""
import sys
sys.dont_write_bytecode=True
import verify_set_parser as p
import json, random, subprocess, tempfile
from pathlib import Path
from elftools.elf.relocation import RelocationSection
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_LR,UC_ARM64_REG_SP,UC_ARM64_REG_PC

# Apply ELF relative relocations used by metadata-pointer GOT loads.
with p.lib.open('rb') as stream:
    elf=p.ELFFile(stream)
    for section in elf.iter_sections():
        if not isinstance(section,RelocationSection):continue
        for relocation in section.iter_relocations():
            if relocation['r_info_type']==1027:
                p.put(relocation['r_offset'],relocation['r_addend'],8)

resolved_slots=set()
metadata_heap=0x100c0000
def boundary(uc,address,size,data):
    global metadata_heap
    x0,x1,x2=(uc.reg_read(r) for r in (UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2))
    value=None
    if address==0xffd960:
        if x0 not in resolved_slots:
            klass=metadata_heap;metadata_heap+=0x200
            p.emu.mem_write(klass,bytes(0x200));p.put(klass+0xe0,1);p.put(x0,klass,8)
            resolved_slots.add(x0)
        value=0
    elif address in (0x1c1ec14,0x1c1eca0):value=width if address==0x1c1ec14 else height
    elif address in (0x1c1f598,0x1c1f5dc):value=int(scroll if address==0x1c1f598 else water)
    elif address==0x1c1f7dc:value=items[p.signed(x1&0xffffffff)+width*p.signed(x2&0xffffffff)]
    elif address==0x1f16360:
        array=p.allocate(0x1000);p.put(array+0x18,128);p.put(x0+0x10,array,8);value=0
    elif address in (0x1f168d0,0x314c594):
        array=p.read(x0+0x10,8);value=p.read(array+0x20+x1*(8 if address==0x1f168d0 else 4),8 if address==0x1f168d0 else 4)
    elif address in (0x2608558,):value=0x10000800
    elif address in (0x3c95768,0x39ae3f0,0x3ccdeb0,0x32089c0):value=0
    if value is not None:
        uc.reg_write(UC_ARM64_REG_X0,value&0xffffffffffffffff);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,boundary)

def call(rva,owner,arg=0):
    for reg,val in ((UC_ARM64_REG_X0,owner),(UC_ARM64_REG_X1,arg),(UC_ARM64_REG_X2,0),(UC_ARM64_REG_SP,0x101ff000),(UC_ARM64_REG_LR,0x20000000)):
        p.emu.reg_write(reg,val&0xffffffffffffffff)
    try: p.emu.emu_start(rva,0x20000000,count=1000000)
    except Exception:
        print('Native failure',hex(p.emu.reg_read(UC_ARM64_REG_PC)),hex(p.emu.reg_read(UC_ARM64_REG_X0)))
        raise
    assert p.emu.reg_read(UC_ARM64_REG_PC)==0x20000000
    return p.emu.reg_read(UC_ARM64_REG_X0)

cases=[];expected=[]
cell_type_ids=list(range(-32,513))+[-2147483648,2147483647]
for id in cell_type_ids:
    cases.append('C;'+str(id));expected.append(str(call(0x1910eb4,id)))
for seed in range(48):
    rng=random.Random(seed);width=1+seed%5;height=12;scroll=seed%3==1;water=seed%3==2
    items=[rng.choice((0,1,6,7,8,9,11,12,13,17,320,321)) for _ in range(width*height)]
    # Paired separator rows give legal multi-board layouts; alternate terminal
    # stop marker with reaching the top row. Other columns retain holes/markers.
    for y in range(height):items[y*width]=1
    if scroll or water:
        for y in (3,4,8,9):items[y*width]=rng.choice((17,320))
        if seed%2:items[11*width]=12
    p.heap=0x10010000
    p.emu.mem_write(0x10000000,bytes(0x1000))
    call(0x320830c,0x10000000,0x10000100)
    array=p.read(0x10000018,8);count=p.read(array+0x18);current=p.signed(p.read(0x1000003c))
    regions=[]
    for i in range(count):
        board=p.read(array+0x20+i*8,8)
        regions.append(','.join(str(p.signed(p.read(board+offset))) for offset in (0x10,0x14,0x20,0x24,0x30,0x34)))
    ids=[]
    for y in range(-1,height+2):
        for x in (-100,0,100):ids.append(p.signed(call(0x3209820,0x10000000,(x&0xffffffff)|((y&0xffffffff)<<32))&0xffffffff))
    cases.append(';'.join((str(width),str(height),str(int(scroll)),str(int(water)),','.join(map(str,items)))))
    expected.append(str(current)+';'+ '|'.join(regions)+';'+','.join(map(str,ids)))

harness='''using System;using Whitebox.Foundation;
namespace UnityEngine{public static class Debug{public static void LogError(object value){}}}
class Verify{static void Main(){string l;while((l=Console.ReadLine())!=null){var a=l.Split(';');if(a[0]=="C"){Console.WriteLine(OriginalLevelRules.AsCellType(int.Parse(a[1])));continue;}int w=int.Parse(a[0]),h=int.Parse(a[1]);var items=Array.ConvertAll(a[4].Split(','),int.Parse);var layout=OriginalBoardLayout.Create(w,h,items,a[2]=="1",a[3]=="1");var regions=new string[layout.Count];for(int i=0;i<layout.Count;i++){var b=layout[i];regions[i]=string.Join(",",new[]{b.Left,b.Bottom,b.Right,b.Top,b.Width,b.Height});}var ids=new int[(h+3)*3];int k=0;for(int y=-1;y<h+2;y++)foreach(int x in new[]{-100,0,100})ids[k++]=layout.GetBoardId(x,y);Console.WriteLine(layout.CurrentBoardIndex+";"+string.Join("|",regions)+";"+string.Join(",",ids));
var grid=new OriginalCellGrid(w,h);for(int x=0;x<w;x++)for(int y=0;y<h;y++)grid.SetCell(x,y,new OriginalGridCell(x,y,OriginalLevelRules.AsCellType(items[x+w*y]),layout.GetBoardId(x,y)));grid.CacheNeighbors();var access=new OriginalBoardGridAccess(grid,layout);var iterator=new OriginalGridIterator(new OriginalBottomLeftHorizontalStrategy(access));if(iterator.MoveNext()!=access.CurrentBoardBottomLeftCell)throw new Exception("First cell differs");while(iterator.MoveNext()!=null){}while(!layout.IsFinalBoard)layout.ChangeBoard();layout.ChangeBoard();if(layout.CurrentBoardIndex!=0)throw new Exception("Final board differs");}}}'''
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder);source=folder/'Verify.cs';source.write_text(harness,encoding='utf-8');exe=folder/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
    subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),*[str(runtime/n) for n in ('OriginalCellMediation.cs','OriginalGridIterator.cs','OriginalCellGrid.cs','OriginalLevelRules.cs','OriginalBoardLayout.cs','OriginalFallGridStrategies.cs')]],capture_output=True,check=True)
    actual=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',text=True,encoding='utf-8',capture_output=True,check=True).stdout.lstrip('\ufeff').splitlines()
    assert actual==expected,next(((cases[i],a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e),'Output count differs')
report={'passed':True,'native_layout_cases':48,'native_board_id_probes':2160,'original_rvas':['0x320830c','0x32074d8','0x3208dc4','0x3209820','0x1910eb4'],'scope':'Original native board partitioning, board geometry, initial current-board index and board lookup versus compiled C#. Parser accessors, context services, allocation/managed storage and goal calculation are supplied boundaries. Managed grid/traversal integration is exercised, not a complete level lifecycle. Water-mode derivation, filling-cell construction, goal/state transitions, shifters and visuals remain unverified.'}
report['native_cell_type_cases']=len(cell_type_ids)
(p.PROJECT/'board_layout_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(json.dumps(report))
