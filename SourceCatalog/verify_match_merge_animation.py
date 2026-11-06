"""Native merge movement, early completion and coroutine-handle ordering."""
import sys
sys.dont_write_bytecode=True
import verify_board_layout as b
import json,math,random,struct,subprocess,tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import *
p=b.p
iterator,view,easer,callback,statics=[0x10000000+i*0x1000 for i in range(5)]
bits=lambda f:struct.unpack('<I',struct.pack('<f',f))[0]
number=lambda n:struct.unpack('<f',struct.pack('<I',n))[0]
normal=lambda n:'nan' if math.isnan(number(n)) else str(n)
delta=0;trace=[];position=[];mutation=-1;callback_count=0

def boundary(uc,address,size,unused):
    global callback_count
    x0,x1=uc.reg_read(UC_ARM64_REG_X0),uc.reg_read(UC_ARM64_REG_X1);value=None
    if address==0x3e4737c:assert x0==view;value=view
    elif address==0x3e4e2c8:
        trace.append('get')
        for reg,v in zip((UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2),position):uc.reg_write(reg,v)
        value=0
    elif address==0x3e4e368:
        position[:]=[uc.reg_read(reg) for reg in (UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2)]
        trace.append('set');value=0
    elif address==0x2c9543c:assert x0==2;value=easer
    elif address==0x32037e4:trace.append('delta');uc.reg_write(UC_ARM64_REG_S0,delta);value=0
    elif address==0xd026f0:
        angle=number(uc.reg_read(UC_ARM64_REG_S0))
        uc.reg_write(UC_ARM64_REG_S0,bits(math.cos(angle) if math.isfinite(angle) else float('nan')));value=0
    elif address==0x20000100:
        trace.append('complete');callback_count+=1
        if mutation!=-1:p.put(view+0x70,mutation,8)
        value=0
    elif address==0x1e39e34:trace.append('stop:'+str(x0));value=0
    if value is not None:uc.reg_write(UC_ARM64_REG_X0,value);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,boundary)

def call():
    for r,v in ((UC_ARM64_REG_X0,iterator),(UC_ARM64_REG_X1,0),(UC_ARM64_REG_SP,0x101ff000),(UC_ARM64_REG_LR,0x20000000)):p.emu.reg_write(r,v)
    p.emu.emu_start(0x21aa5ac,0x20000000,count=100000)
    assert p.emu.reg_read(UC_ARM64_REG_PC)==0x20000000
    return p.emu.reg_read(UC_ARM64_REG_X0)

slot=0x55902b0
if slot not in b.resolved_slots:p.emu.reg_write(UC_ARM64_REG_X0,slot);b.boundary(p.emu,0xffd960,4,None)
p.put(p.read(slot,8)+0xb8,statics,8)
rng=random.Random(0x21aa5ac);cases=[];expected=[]
for case in range(192):
    p.emu.mem_write(iterator,bytes(0x5000));trace=[];callback_count=0
    duration=bits([.2,.3,.0,-.2,float('nan'),float('inf')][case%6])
    position=[bits(rng.choice((0.,1.,-1.,rng.uniform(-20,20)))) for _ in range(3)];start=list(position)
    end=[bits(rng.uniform(-20,20)) for _ in range(3)]
    extrapolate=(case//6)%2;handle=[0,1234,4321][(case//12)%3];mutation=[-1,0,9876][(case//36)%3];has_callback=(case//18)%2
    p.put(iterator+0x20,view,8);p.put(iterator+0x28,2);p.put(iterator+0x2c,duration)
    for i,v in enumerate(end):p.put(iterator+0x30+i*4,v)
    p.put(iterator+0x3c,extrapolate,1);p.put(iterator+0x40,callback if has_callback else 0,8)
    p.put(view+0x70,handle,8);p.put(callback+0x18,0x20000100,8);p.put(easer+0x18,0x2c95c94,8)
    schedule=case//48
    deltas=[bits(rng.choice((0.,1/60,1/30,.0067,.1))) for _ in range(40)]
    if schedule==1:deltas[:8]=[bits(v) for v in (.085,.085,0.,.00001,.1,0.,.2,.1)]
    if schedule==2:deltas[:8]=[bits(v) for v in (-.1,.2,0.,.5,.1,.1,.1,.1)]
    if schedule==3:deltas[:2]=[bits(float('inf') if case%2 else float('nan')),bits(.5)]
    ratios=[bits(.85) for _ in deltas]
    if case%8==1:ratios=[bits(rng.choice((0.,.5,.85,1.,float('nan')))) for _ in deltas]
    cases.append(';'.join([','.join(map(str,[duration,extrapolate,handle,mutation,has_callback,*start,*end])),','.join(map(str,deltas)),','.join(map(str,ratios))]))
    for delta,ratio in zip(deltas,ratios):
        p.put(statics+4,ratio)
        moved=call()
        expected.append(','.join([str(moved),normal(p.read(iterator+0x48)),*[normal(v) for v in position],str(p.read(view+0x70,8)),str(callback_count)])+';'+','.join(trace))

harness=r'''using System;using System.Collections.Generic;using Whitebox.Foundation;
namespace Whitebox.Foundation{public struct OriginalSwapPoint{public float X,Y,Z;public OriginalSwapPoint(float x,float y,float z){X=x;Y=y;Z=z;}}}
class View:IOriginalMatchMergeAnimationView{public OriginalSwapPoint Point;public int Handle,Mutation,CallbackCount;public List<string> Trace=new List<string>();public OriginalSwapPoint Position{get{Trace.Add("get");return Point;}set{Point=value;Trace.Add("set");}}public void Complete(){Trace.Add("complete");CallbackCount++;if(Mutation!=-1)Handle=Mutation;}public void StopMergeCoroutine(){if(Handle==0)return;Trace.Add("stop:"+Handle);Handle=0;}}
class Verify{static float Float(string s){return BitConverter.ToSingle(BitConverter.GetBytes(uint.Parse(s)),0);}static string Bits(float f){return float.IsNaN(f)?"nan":BitConverter.ToUInt32(BitConverter.GetBytes(f),0).ToString();}static void Main(){string line;while((line=Console.ReadLine())!=null){var a=line.Split(';');var h=a[0].Split(',');var d=a[1].Split(',');var ratios=a[2].Split(',');var view=new View{Handle=int.Parse(h[2]),Mutation=int.Parse(h[3]),Point=new OriginalSwapPoint(Float(h[5]),Float(h[6]),Float(h[7]))};var settings=new OriginalMatchMergeSettings{Duration=Float(h[0])};float delta=0;var it=new OriginalMatchMergeAnimation(view,()=>{view.Trace.Add("delta");return delta;},settings,new OriginalSwapPoint(Float(h[8]),Float(h[9]),Float(h[10])),h[1]=="1",h[4]=="1"?(Action)view.Complete:null);settings.Duration=999f;for(int i=0;i<d.Length;i++){delta=Float(d[i]);settings.ExplodeCompleteRatio=Float(ratios[i]);bool moved=it.MoveNext();var q=view.Point;Console.WriteLine((moved?1:0)+","+Bits(it.Elapsed)+","+Bits(q.X)+","+Bits(q.Y)+","+Bits(q.Z)+","+view.Handle+","+view.CallbackCount+";"+string.Join(",",view.Trace));}try{it.Reset();throw new Exception("Reset supported");}catch(NotSupportedException){}}}}'''
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder);source=folder/'Verify.cs';source.write_text(harness);exe=folder/'Verify.exe'
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),str(p.PROJECT/'Assets/Foundation/Runtime/OriginalMatchMergeAnimation.cs')],capture_output=True,text=True,encoding='utf-8')
    assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8')
    assert result.returncode==0,result.stdout+result.stderr
    actual=result.stdout.lstrip('\ufeff').splitlines()
    assert len(actual)==len(expected),(len(actual),len(expected))
    assert actual==expected,next(((i//40,i%40,cases[i//40],a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e),'count mismatch')
report={'passed':True,'native_animation_cases':len(cases),'native_frame_comparisons':len(expected),
 'original_rvas':['0x21aa5ac','0x2c95c94','0x2d7bdf4'],
 'scope':'Native merge iterator, actual SineIn evaluator and weighted vector interpolation compared with production C#. Includes clamped/extrapolated paths, zero/negative/NaN/infinite durations and deltas, live completion-ratio changes, null callback, callback-mutated coroutine handle and callback-before-stop ordering plus the extra post-callback yield. Native cosine is supplied with float-rounded host cosine; platform cosine accuracy is not asserted. Unity transform storage and GameContext stop service are boundaries; actual host/recycle interaction is tested separately in Unity. Special-item creation and full model resolution remain incomplete.'}
(p.PROJECT/'match_merge_animation_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
