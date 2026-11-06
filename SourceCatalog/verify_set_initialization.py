"""Compare LevelSet.InitFillAvailability with original ARM64 instructions."""
import sys
sys.dont_write_bytecode = True
import verify_set_parser as p
import json
from pathlib import Path
import subprocess
import tempfile
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_LR,UC_ARM64_REG_SP,UC_ARM64_REG_PC

def hook(uc,address,size,data):
    x0=uc.reg_read(UC_ARM64_REG_X0); x1=uc.reg_read(UC_ARM64_REG_X1); value=None
    if address==0x2c1b9a0: value=p.read(p.read(x0+0x10,8)+0x20+x1*8,8)
    elif address==0x3c96364: p.dictionaries[x0].clear();value=0
    elif address==0x3c96128: value=p.dictionaries[x0][x1]
    if value is not None:
        uc.reg_write(UC_ARM64_REG_X0,value);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,hook)
p.put(0x573c8d4,1,1);p.put(0x573c8e7,1,1)
cases=[];expected=[]
for ids in ([],[1,2],[10,35],[35,10],[10,10,35],[35,125],[125,35],[1,35,10]):
    for limits in ({},{3:7,16:5,31:9},{1:0,3:-2,16:5,31:9}):
        for can_fall in (False,True):
            p.heap=0x10010000;p.dictionaries={}
            obj=p.allocate();lst=p.allocate();store=p.allocate(0x1000);dictionary=p.allocate()
            p.put(obj+0x18,lst,8);p.put(lst+0x10,store,8);p.put(lst+0x18,len(ids));p.put(obj+0x40,dictionary,8);p.put(obj+0x38,int(can_fall),1)
            p.put(obj+0x54,99);p.put(obj+0x88,99)
            p.dictionaries[dictionary]=dict(limits)
            for i,id in enumerate(ids):
                e=p.allocate();p.put(e+0x10,id);p.put(store+0x20+i*8,e,8)
            p.emu.reg_write(UC_ARM64_REG_X0,obj);p.emu.reg_write(UC_ARM64_REG_SP,0x101ff000);p.emu.reg_write(UC_ARM64_REG_LR,0x20000000)
            # Repeated initialization must not restore discarded dictionary entries.
            for repeat in range(2):
                p.emu.reg_write(UC_ARM64_REG_X0,obj);p.emu.reg_write(UC_ARM64_REG_LR,0x20000000)
                p.emu.emu_start(0x190f314,0x20000000,count=100000)
                assert p.emu.reg_read(UC_ARM64_REG_PC)==0x20000000
            result=p.dictionaries[dictionary]
            expected.append('|'.join([str(p.read(obj+0x50,1)),str(p.read(obj+0x54)),str(p.read(obj+0x88)),str(int(bool(result))),p.pairs(sorted((k,p.signed(v)) for k,v in result.items()))]))
            cases.append('|'.join([p.csv(ids),p.pairs(sorted(limits.items())),str(int(can_fall))]))
harness='''using System;using System.Collections.Generic;using Whitebox.Foundation;using Whitebox.Foundation.ConfigData;
namespace Whitebox.Foundation {public sealed class OriginalLevelRandomSource {public int DifficultyNext(int a,int b){return 0;}}}
class Verify {
 static void Main(){string l;while((l=Console.ReadLine())!=null){var p=l.Split('|');var elements=new List<FTiledSetElement>();if(p[0].Length>0)foreach(var i in p[0].Split(','))elements.Add(new FTiledSetElement{Id=int.Parse(i),CreateRatio=1});var limits=new List<FTiledSetMaxItemCount>();if(p[1].Length>0)foreach(var i in p[1].Split(',')){var v=i.Split(':');limits.Add(new FTiledSetMaxItemCount{Id=int.Parse(v[0]),MaxCount=int.Parse(v[1])});}var set=new OriginalLevelSetSelection(new OriginalParsedLevelSet(new FTiledSet{Elements=elements.ToArray(),MaxItemCounts=limits.ToArray(),CanFall=p[2]=="1"},0,0),new OriginalLevelRandomSource());set.InitFillAvailability();set.InitFillAvailability();var keys=new List<int>(set.MaxItemCounts.Keys);keys.Sort();var entries=new List<string>();foreach(int k in keys)entries.Add(k+":"+set.MaxItemCounts[k]);Console.WriteLine((set.IsAvailableForFill?1:0)+"|"+set.TotalCreateRatio+"|"+set.RemainingItemCount+"|"+(set.ShouldPrioritize?1:0)+"|"+string.Join(",",entries));}}}
'''
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder);source=folder/'Verify.cs';source.write_text(harness,encoding='utf-8');exe=folder/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
    subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),*[str(runtime/n) for n in ('OriginalConfigData.cs','OriginalLevelRules.cs','OriginalParsedLevelSet.cs','OriginalLevelSetSelection.cs','OriginalTiledItemTypes.cs')]],check=True,capture_output=True)
    actual=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',text=True,encoding='utf-8',capture_output=True,check=True).stdout.lstrip('\ufeff').splitlines()
    assert actual==expected,next(((cases[i],a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e),'Output count differs')
report={'passed':True,'native_initialization_cases':len(cases),'original_rvas':['0x190f314','0x190f514'],'scope':'Actual ARM64 initialization and item conversion, including ordered limit pruning and repeated initialization. Managed collection storage supplied at boundaries. Board scan and per-frame quota checks remain unverified.'}
(p.PROJECT/'set_initialization_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(json.dumps(report))
