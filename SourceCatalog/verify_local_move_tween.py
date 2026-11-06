"""Compare native coroutine/easing/local-position arithmetic frame by frame."""
import sys
sys.dont_write_bytecode=True
import verify_board_layout as b
import json,random,struct,subprocess,tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import UC_ARM64_REG_X0,UC_ARM64_REG_PC,UC_ARM64_REG_LR,UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2
p=b.p
coroutine=0x10000000;tween=0x10001000;procedure=0x10002000;procedure_klass=0x10003000;target=0x10004000
move_method=0x10005000;move_context=0x10005100;move_table=0x10005200;delta_method=0x10005300
apply_method=0x10005400;apply_context=0x10005500;apply_table=0x10005600;ease_method=0x10005700;complete=0x10006000
bits=lambda value:struct.unpack('<I',struct.pack('<f',value))[0]
delta_bits=0;completed=0;applied=0
def boundary(uc,address,size,data):
    global completed,applied
    value=None
    if address==0x20000100:uc.reg_write(UC_ARM64_REG_S0,delta_bits);value=0
    elif address==0x20000120:completed+=1;value=0
    elif address==0x3e4e4a0:
        for i,r in enumerate((UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2)):p.put(target+0x10+4*i,uc.reg_read(r))
        applied+=1;value=0
    if value is not None:
        uc.reg_write(UC_ARM64_REG_X0,value);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,boundary)
cases=[];expected=[];rng=random.Random(1499970)
for n in range(72):
    p.emu.mem_write(coroutine,bytes(0x10000));p.put(coroutine+0x20,tween,8)
    p.put(move_method+0x20,move_context,8);p.put(move_context+0xc0,move_table,8);p.put(move_table,delta_method,8);p.put(delta_method+8,0x20000100,8)
    p.put(move_table+0x10,apply_method,8);p.put(apply_method+8,0x28619c4,8);p.put(apply_method+0x20,apply_context,8)
    p.put(apply_context+0xc0,apply_table,8);p.put(apply_table,ease_method,8);p.put(ease_method+8,0x28619a4,8)
    p.put(tween+0x10,target,8);p.put(tween+0x20,procedure,8);p.put(procedure,procedure_klass,8);p.put(procedure+0x10,tween,8);p.put(procedure_klass+0x178,0x2c97e74,8)
    p.put(tween+0x50,complete,8);p.put(complete+0x18,0x20000120,8)
    ease=(0,1,6)[n%3];duration=(0.135,0.27,0,-0.1,0.05,0.333)[n%6]
    start=[bits(rng.uniform(-12,12)) for _ in range(3)];end=[bits(rng.uniform(-12,12)) for _ in range(3)]
    p.put(tween+0x18,ease);p.put(tween+0x28,bits(duration));p.put(tween+0x2c,bits(1.70158))
    for i in range(3):p.put(tween+0x34+4*i,start[i]);p.put(tween+0x40+4*i,end[i]);p.put(target+0x10+4*i,start[i])
    deltas=[bits(rng.choice((0,1/60,1/30,0.0067,0.1))) for _ in range(48)]
    completed=0;applied=0
    for delta_bits in deltas:
        moved=b.call(0x14e9970,coroutine,move_method)
        expected.append(','.join(map(str,[moved,p.read(tween+0x60),*[p.read(target+0x10+4*i) for i in range(3)],completed,applied])))
    cases.append(','.join(map(str,[ease,bits(duration),*start,*end]))+';'+','.join(map(str,deltas)))
harness=r'''using System;using Whitebox.Foundation;class Position:IOriginalLocalPositionTarget{public OriginalSwapPoint LocalPosition{get;set;}}
class Verify{static uint Bits(float v){return BitConverter.ToUInt32(BitConverter.GetBytes(v),0);}static float Float(string v){return BitConverter.ToSingle(BitConverter.GetBytes(uint.Parse(v)),0);}static void Main(){string line;while((line=Console.ReadLine())!=null){var parts=line.Split(';');var a=parts[0].Split(',');var position=new Position{LocalPosition=new OriginalSwapPoint(Float(a[2]),Float(a[3]),Float(a[4]))};float delta=0;int completed=0,applied=0;var tween=new OriginalLocalMoveTween(()=>delta);tween.Configure(position,new OriginalSwapPoint(Float(a[5]),Float(a[6]),Float(a[7])),Float(a[1]),()=>{if(!tween.IsInUse)throw new Exception("Iterator reused during completion");completed++;},int.Parse(a[0]));foreach(var d in parts[1].Split(',')){delta=Float(d);bool moved=tween.MoveNext();if(moved)applied++;var p=position.LocalPosition;Console.WriteLine((moved?1:0)+","+Bits(tween.Elapsed)+","+Bits(p.X)+","+Bits(p.Y)+","+Bits(p.Z)+","+completed+","+applied);if(!moved&&tween.IsInUse)throw new Exception("Pool slot leaked");}
// A completed iterator can be configured again; completion cannot fire twice.
tween.Configure(position,new OriginalSwapPoint(1,2,3),0,()=>completed++);if(tween.MoveNext()||tween.MoveNext())throw new Exception();}}}'''
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder);source=folder/'Verify.cs';source.write_text(harness);exe=folder/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
    # SwapCompletion defines the shared point struct; its references are compiled
    # from the same production sources rather than mocked model definitions.
    names=('OriginalLocalMoveTween','OriginalSwapCompletion','OriginalSwapActionState','OriginalSwapDecision','OriginalItemSwapAvailability',
        'OriginalFillingItemPlacement','OriginalFillingCell','OriginalFallRefillColumns','OriginalCellTouchRules','OriginalCellMediation',
        'OriginalGridIterator','OriginalMatchSearch','OriginalMatchCandidates','OriginalMatchSnapshot','OriginalGameOperations','OriginalCellFallState')
    source.write_text(harness+'\nnamespace UnityEngine{public static class Debug{public static void LogError(object x){}}}')
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),*[str(runtime/(n+'.cs')) for n in names]],capture_output=True,text=True,encoding='utf-8')
    assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8')
    assert result.returncode==0,result.stdout+result.stderr
    actual=result.stdout.replace('\ufeff','').splitlines()
    assert actual==expected,next(((i,a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e),'count mismatch')
report={'passed':True,'native_tween_cases':72,'native_frame_comparisons':len(expected),
 'original_rvas':['0x14e9970','0x28619a4','0x28619c4','0x33d5f78','0x2c97e74'],
 'scope':'Real native coroutine state, default/linear/OutQuad easing, float-bit local position and final-yield callback timing. Delta provider, generic metadata, transform setter and completion delegate supplied. Managed iterator reuse checked. Unity scheduling integration, replay provider and other eases remain pending.'}
(p.PROJECT/'local_move_tween_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
