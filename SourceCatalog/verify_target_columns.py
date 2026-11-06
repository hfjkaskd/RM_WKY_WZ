"""Native target-column ratio and predicate comparisons; storage boundaries supplied."""
import sys
sys.dont_write_bytecode=True
import verify_board_layout as b
import itertools,json,subprocess,tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_PC,UC_ARM64_REG_LR,UC_ARM64_REG_SP
p=b.p
def boundary(uc,address,size,data):
    x0,x1=uc.reg_read(UC_ARM64_REG_X0),uc.reg_read(UC_ARM64_REG_X1);value=None
    if address==0x1c934b4:value=0x10001000
    elif address==0x2c1b9a0:value=p.read(0x10001020+8*x1,8)
    elif address==0x19107d4:value=p.read(x0+0x88)
    elif address==0x314cc6c:value=int(x1 in fills)
    elif address==0x314d484:value=fills.index(x1) if x1 in fills else -1
    elif address==0x314c594:value=ratios[x1]
    if value is not None:uc.reg_write(UC_ARM64_REG_X0,value);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,boundary)
def call(address,x0,x1=0,x2=0):
    for reg,value in ((UC_ARM64_REG_X0,x0),(UC_ARM64_REG_X1,x1),(UC_ARM64_REG_X2,x2),(UC_ARM64_REG_SP,0x101ff000),(UC_ARM64_REG_LR,0x20000000)):p.emu.reg_write(reg,value)
    p.emu.emu_start(address,0x20000000,count=100000);assert p.emu.reg_read(UC_ARM64_REG_PC)==0x20000000
    return p.signed(p.emu.reg_read(UC_ARM64_REG_X0)&0xffffffff)
cases=[];expected=[]
for fill,available,priority,mode in itertools.product(range(0,7),range(2),range(2),range(3)):
    fills=[[],[1,3],[2,2]][mode];ratios=[[],[7,-2],[9,4]][mode]
    p.emu.mem_write(0x10000000,bytes(0x10000));p.heap=0x10010000
    model=0x10002000;p.put(model+0x28,0x10003000,8);p.put(0x10003018,len(fills));p.put(model+0x30,0x10004000,8)
    p.put(model+0x3c,5);p.put(model+0x50,available,1);p.put(model+0x88,priority)
    p.put(0x100050a4,fill);p.put(0x10001018,1);p.put(0x10001020,model,8)
    direct=call(0x1910d98,model,fill)
    # Native total gets the filtered list through a supplied boundary; execute
    # actual predicate independently and supply its resulting list membership.
    p.put(0x10006010,0x10005000,8)
    predicate=call(0x1c93784,0x10006000,model)
    selected=fill==1 or predicate!=0;p.put(0x10001018,int(selected))
    total=call(0x1c93628,0x10000000,0x10005000,2)
    cases.append(f'{fill},{available},{priority},{mode}');expected.append(f'{direct},{total}')
harness='''using System;using System.Collections.Generic;using Whitebox.Foundation;using Whitebox.Foundation.ConfigData;
class Verify{static void Main(){string line;while((line=Console.ReadLine())!=null){var a=Array.ConvertAll(line.Split(','),int.Parse);var fills=a[3]==0?new int[0]:a[3]==1?new[]{1,3}:new[]{2,2};var ratios=a[3]==0?new int[0]:a[3]==1?new[]{7,-2}:new[]{9,4};var config=new FTiledSet{CanFall=a[1]!=0,CreateRatio=5,TargetFills=fills,TargetFillRatios=ratios};var parsed=new OriginalParsedLevelSet(config,0,0);if(a[2]!=0)parsed.MaxItemCounts.Add(1,3);var set=new OriginalLevelSetSelection(parsed,new OriginalLevelRandomSource(1));set.InitializeAvailability(a[1]!=0);var c=new OriginalTargetColumnSetContainer();c.AddSetToColumn(set,2);var buffer=new List<OriginalLevelSetSelection>();Console.WriteLine(set.GetFillRatio(a[0])+","+c.GetTotalRatioForColumn(a[0],2,buffer));}
var container=new OriginalTargetColumnSetContainer();for(int i=0;i<9;i++)if(container.HasTarget(i))throw new Exception();container.Clear();for(int i=0;i<9;i++)if(!container.HasTarget(i))throw new Exception();}}
'''
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder);source=folder/'Verify.cs';source.write_text(harness);exe=folder/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
    names=('OriginalTargetColumnSetContainer.cs','OriginalLevelSetSelection.cs','OriginalParsedLevelSet.cs','OriginalLevelRandomSource.cs','OriginalLevelRules.cs','OriginalTiledItemTypes.cs')
    names=names+('OriginalMatchItemRandomizer.cs',)
    # DTO definitions live in the config catalog file.
    dto=list(runtime.glob('**/*ConfigData*.cs'))
    compiled=subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),*[str(runtime/n) for n in names],*[str(n) for n in dto]],capture_output=True,text=True)
    assert compiled.returncode==0,compiled.stdout+compiled.stderr
    actual=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8',check=True).stdout.replace('\ufeff','').splitlines()
    assert actual==expected
report={'passed':True,'native_cases':len(cases),'scope':'Native fill ratio, target-fill predicate and available non-prioritized total aggregation versus compiled C#. List storage, priority accessor and filtered-list boundary supplied. Constructor/Clear HasTarget behavior checked in managed code against inspected native implementation; full item creator remains incomplete.'}
(p.PROJECT/'target_columns_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
