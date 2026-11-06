"""Execute native swap availability with real native fall/readiness and view flags."""
import sys
sys.dont_write_bytecode=True
import verify_board_layout as b
import itertools,json,subprocess,tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import UC_ARM64_REG_X0,UC_ARM64_REG_PC,UC_ARM64_REG_LR
p=b.p
item=0x10000000;item_klass=0x10001000;fall=0x10002000;fall_klass=0x10003000
view=0x10004000;interface_map=0x10005000;interface_type=0x100d4000
slot=p.read(0x5384ca8,8);p.put(slot,interface_type,8);b.resolved_slots.add(slot)
error=False;has_view=True
def boundary(uc,address,size,data):
    global error
    if address==0x20000100:
        uc.reg_write(UC_ARM64_REG_X0,view if has_view else 0);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
    elif address==0xffda88:
        error=True;uc.reg_write(UC_ARM64_REG_PC,0x20000000)
p.emu.hook_add(UC_HOOK_CODE,boundary)
def available():
    global error
    error=False;value=b.call(0x2b46380,item)
    return 'E' if error else str(value)
cases=[];expected=[]
states=(-2147483648,-2,-1,0,1,2,3,2147483647)
for match,state,swapping,has_fall,has_view in itertools.product(range(2),states,range(2),range(2),range(2)):
    p.emu.mem_write(item,bytes(0x10000));p.put(item,item_klass,8);p.put(item+0x40,fall if has_fall else 0,8)
    p.put(item_klass+0x448,0x21a5a5c if match else 0x2bc5b08,8);p.put(item_klass+0x418,0x20000100,8)
    p.put(fall,fall_klass,8);p.put(fall+0x28,state);p.put(fall_klass+0x12a,1,2);p.put(fall_klass+0xb0,interface_map,8)
    p.put(interface_map,interface_type,8);p.put(interface_map+8,0);p.put(fall_klass+0x138,0x2eea560,8)
    p.put(view+0x19,swapping,1)
    results=[available()]
    b.call(0x2b4acc8,view,0);results.extend([str(b.call(0x2b4aa1c,view)),available()])
    b.call(0x2b4acd4,view);results.extend([str(b.call(0x2b4aa1c,view)),available()])
    cases.append(','.join(map(str,[match,state,swapping,has_fall,has_view])));expected.append(','.join(results))
harness=r'''using System;using Whitebox.Foundation;
namespace UnityEngine{public class MonoBehaviour{}public static class Debug{public static void LogError(object x){}}}
class Verify{static string Available(OriginalItemSwapAvailability a,OriginalBoardItem item){try{return a.IsAvailableForSwap(item)?"1":"0";}catch(NullReferenceException){return "E";}}static void Main(){string line;while((line=Console.ReadLine())!=null){var a=Array.ConvertAll(line.Split(','),int.Parse);OriginalBoardItem item=a[0]!=0?(OriginalBoardItem)new OriginalMatchBoardItem(1,0,1):new OriginalBoxBoardItem(1,1);var view=new OriginalItemSwapView();if(a[2]!=0)view.SwapAnimationStarted(false,0);if(a[3]!=0)item.FallComponent=new OriginalFallComponentState(item){CurrentFallState=a[1]};if(a[4]!=0)item.BindSwapView(view);var availability=new OriginalItemSwapAvailability();string output=Available(availability,item);view.SwapAnimationStarted(false,0);output+=","+(view.IsSwapping?1:0)+","+Available(availability,item);view.SwapAnimationEnded();output+=","+(view.IsSwapping?1:0)+","+Available(availability,item);Console.WriteLine(output);}}}'''
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder);source=folder/'Verify.cs';source.write_text(harness);exe=folder/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
    names=('OriginalItemSwapAvailability','OriginalItemSwapView','OriginalFallComponentState','OriginalFillingItemPlacement',
        'OriginalFillingCell','OriginalFallRefillColumns','OriginalCellTouchRules','OriginalCellMediation','OriginalGridIterator',
        'OriginalMatchSearch','OriginalMatchCandidates','OriginalMatchSnapshot')
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),*[str(runtime/(n+'.cs')) for n in names]],capture_output=True,text=True,encoding='utf-8')
    assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8')
    assert result.returncode==0,result.stdout+result.stderr
    actual=result.stdout.replace('\ufeff','').splitlines()
    assert actual==expected,next(((cases[i],a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e),'count mismatch')
report={'passed':True,'native_item_cases':len(cases),'native_availability_queries':len(cases)*3,
 'original_rvas':['0x2b46380','0x21a5a5c','0x2bc5b08','0x2eea560','0x2b4acc8','0x2b4acd4','0x2b4aa1c'],
 'scope':'Native match/box capability, real native fall state predicate, view swap flag start/end and null-fall/view exception branches. Metadata/interface dispatch supplied; item view lookup uses fixture storage. Fall motion, swap motion, input and special-item overrides are not covered.'}
(p.PROJECT/'item_swap_availability_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
