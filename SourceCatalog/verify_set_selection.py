"""Execute original ARM64 selection routines against actual managed runtime code."""
import importlib.util
import json
from pathlib import Path
import subprocess
import tempfile

import sys
sys.dont_write_bytecode = True
spec = importlib.util.spec_from_file_location('parser', Path(__file__).with_name('verify_set_parser.py'))
p = importlib.util.module_from_spec(spec)
spec.loader.exec_module(p)
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import UC_ARM64_REG_X0, UC_ARM64_REG_X1, UC_ARM64_REG_X2, UC_ARM64_REG_PC, UC_ARM64_REG_LR, UC_ARM64_REG_SP

def selection_hook(uc, address, size, data):
    global draws
    x0, x1, x2 = (uc.reg_read(r) for r in (UC_ARM64_REG_X0, UC_ARM64_REG_X1, UC_ARM64_REG_X2))
    result = None
    if address == 0x2c1b9a0:
        result = p.read(p.read(x0 + 0x10, 8) + 0x20 + x1 * 8, 8)
    elif address == 0x314f4b4:
        result = int(p.signed(x1 & 0xffffffff) in forbidden)
    elif address == 0x1ed98c4:
        bound = p.signed(x2 & 0xffffffff)
        assert bound >= 0
        draws.append(bound)
        # Deterministic boundary picker, independently verified random source elsewhere.
        result = max(0, bound - 1) if last else 0
    if result is not None:
        uc.reg_write(UC_ARM64_REG_X0, result)
        uc.reg_write(UC_ARM64_REG_PC, uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE, selection_hook)
for flag in (0x573c8d5,0x573c8d6,0x573c8e0,0x573c8e1): p.put(flag,1,1)

def call(rva):
    p.emu.reg_write(UC_ARM64_REG_X0, owner)
    p.emu.reg_write(UC_ARM64_REG_X1, 0x10000800)
    p.emu.reg_write(UC_ARM64_REG_SP, 0x101ff000)
    p.emu.reg_write(UC_ARM64_REG_LR, 0x20000000)
    p.emu.emu_start(rva,0x20000000,count=100000)
    assert p.emu.reg_read(UC_ARM64_REG_PC)==0x20000000
    return p.signed(p.emu.reg_read(UC_ARM64_REG_X0)&0xffffffff)

cases, expected = [], []
for ids, weights in (([],[]),([1],[1]),([1,2,1,14],[1,3,2,9])):
    for mask in range(1 << len(ids)):
        for initial in (False,True):
            for forbidden in ([],[1],[1,2,14]):
                for last in (False,True):
                    p.heap=0x10010000
                    owner=p.allocate(); lst=p.allocate(); storage=p.allocate(0x1000)
                    p.put(owner+0x18,lst,8);p.put(lst+0x10,storage,8);p.put(lst+0x18,len(ids))
                    p.put(owner+0x50,int(initial),1);p.put(owner+0x60,0x10000900,8)
                    for i,(id,weight) in enumerate(zip(ids,weights)):
                        element=p.allocate();p.put(storage+0x20+i*8,element,8)
                        p.put(element+0x10,id);p.put(element+0x14,weight);p.put(element+0x18,(mask>>i)&1,1)
                    call(0x190f9cc); call(0x190fa8c)
                    total=p.signed(p.read(owner+0x54)); flag=p.read(owner+0x50,1)
                    draws=[]; normal=call(0x1910528); excluded=call(0x19105fc)
                    expected.append(f'{total}|{flag}|{normal}|{excluded}|{len(draws)}')
                    cases.append('|'.join([p.csv(ids),p.csv(weights),str(mask),str(int(initial)),p.csv(forbidden),str(int(last))]))
harness='''using System;using System.Collections.Generic;using Whitebox.Foundation;using Whitebox.Foundation.ConfigData;
namespace Whitebox.Foundation {
 public interface IOriginalMatchRandomSource {byte GetRandomItemFromListBasedOnDifficulty(List<byte> a);}
 public sealed class OriginalLevelRandomSource {public int Count; public bool Last; public int DifficultyNext(int a,int b){Count++;return Last?Math.Max(0,b-1):0;}}
}
class Verify {
 static int[] Ints(string s){return s.Length==0?new int[0]:Array.ConvertAll(s.Split(','),int.Parse);}
 static void Main(){string l;while((l=Console.ReadLine())!=null){var p=l.Split('|');var ids=Ints(p[0]);var w=Ints(p[1]);var e=new FTiledSetElement[ids.Length];for(int i=0;i<e.Length;i++)e[i]=new FTiledSetElement{Id=ids[i],CreateRatio=w[i]};var r=new OriginalLevelRandomSource{Last=p[5]=="1"};var s=new OriginalLevelSetSelection(new OriginalParsedLevelSet(new FTiledSet{Elements=e},0,0),r);s.InitializeAvailability(p[3]=="1");int mask=int.Parse(p[2]);for(int i=0;i<e.Length;i++)s.SetElementAvailability(i,(mask&(1<<i))!=0);s.CalculateFillRatioForItems();s.UpdateFillAvailability();int normal=s.GetRandomItem(),excluded=s.GetRandomItemExcept(new List<int>(Ints(p[4])));Console.WriteLine(s.TotalCreateRatio+"|"+(s.IsAvailableForFill?1:0)+"|"+normal+"|"+excluded+"|"+r.Count);}}}
'''
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder);source=folder/'Verify.cs';source.write_text(harness,encoding='utf-8');exe=folder/'Verify.exe'
    runtime=p.PROJECT/'Assets/Foundation/Runtime'
    compiled=subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),*[str(runtime/name) for name in ('OriginalConfigData.cs','OriginalLevelRules.cs','OriginalParsedLevelSet.cs','OriginalLevelSetSelection.cs','OriginalTiledItemTypes.cs')]],capture_output=True,text=True)
    assert compiled.returncode==0,compiled.stdout+compiled.stderr
    actual=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',text=True,encoding='utf-8',capture_output=True,check=True).stdout.lstrip('\ufeff').splitlines()
    assert actual==expected, next(((cases[i],a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e),'Output count differs')
report={'passed':True,'native_selection_cases':len(cases),'original_rvas':['0x190f9cc','0x190fa8c','0x1910528','0x19105fc'],'scope':'Weight sum, availability flag, normal/excluded selection and sample counts. Original instructions execute with managed list storage and first/last random picks at boundaries. Board scanning, max-item availability and actual seeded random generation are outside this verifier.'}
(p.PROJECT/'set_selection_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(json.dumps(report))
