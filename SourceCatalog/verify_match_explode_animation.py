"""Original ARM64 MatchItemView.ExplodeAnim versus compiled production iterator."""
import sys
sys.dont_write_bytecode=True
import verify_board_layout as b
import json,math,random,struct,subprocess,tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_LR,UC_ARM64_REG_PC,UC_ARM64_REG_SP,UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2
p=b.p
iterator=0x10000000;view=0x10001000;root=0x10002000;transform=0x10003000;visual=0x10004000
easer=0x10005000;wait=0x10006000;zero=0x10007000;factory=0x10008000
bits=lambda f:struct.unpack('<I',struct.pack('<f',f))[0]
number=lambda n:struct.unpack('<f',struct.pack('<I',n))[0]
normal=lambda n:'nan' if math.isnan(number(n)) else str(n)
delta_bits=0;trace=[];scales=[];mutation=-1

def boundary(uc,address,size,unused):
    x0,x1,x2=[uc.reg_read(r) for r in (UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2)]
    value=None
    if address==0x2c9543c:
        assert x0==2
        value=easer
    elif address==0xd026f0:
        angle=number(uc.reg_read(UC_ARM64_REG_S0))
        result=math.cos(angle) if math.isfinite(angle) else float('nan')
        uc.reg_write(UC_ARM64_REG_S0,bits(result));value=0
    elif address==0x3e4e958:
        assert x0==transform
        trace.append('get')
        for reg,n in zip((UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2),scales):uc.reg_write(reg,n)
        value=0
    elif address==0x3e4e9f8:
        assert x0==transform
        scales[:]=[uc.reg_read(r) for r in (UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2)]
        trace.append('scale');value=0
    elif address==0x32037e4:trace.append('delta');uc.reg_write(UC_ARM64_REG_S0,delta_bits);value=0
    elif address==0x21a9c94:
        trace.append('particle:'+str(x1))
        if mutation!=-1:p.put(view+0x19,mutation,1)
        value=0
    elif address==0x3e473b8:assert x0==visual;value=visual+0x100
    elif address==0x3e49b30:assert x0==visual+0x100 and x1==0;trace.append('hide');value=0
    elif address==0x3e49528:assert x0==wait and uc.reg_read(UC_ARM64_REG_S0)==bits(1.);trace.append('wait');value=0
    elif address==0x260a1fc:assert x0==factory and x1==view;trace.append('recycle');value=0
    elif address==0xffda78:value=wait
    if value is not None:uc.reg_write(UC_ARM64_REG_X0,value);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,boundary)

def call():
    for r,v in ((UC_ARM64_REG_X0,iterator),(UC_ARM64_REG_X1,0),(UC_ARM64_REG_SP,0x101ff000),(UC_ARM64_REG_LR,0x20000000)):p.emu.reg_write(r,v)
    p.emu.emu_start(0x21aa268,0x20000000,count=100000)
    assert p.emu.reg_read(UC_ARM64_REG_PC)==0x20000000
    return p.emu.reg_read(UC_ARM64_REG_X0)

# Resolve Vector3 static zero data once. The original easing implementation and
# root/hide wrappers execute; Unity transform calls and cosine are boundaries.
slot=0x5595e08
if slot not in b.resolved_slots:p.emu.reg_write(UC_ARM64_REG_X0,slot);b.boundary(p.emu,0xffd960,4,None)
p.put(p.read(slot,8)+0xb8,zero,8)
rng=random.Random(0x21aa268);cases=[];expected=[]
for case in range(144):
    p.emu.mem_write(iterator,bytes(0x10000));trace=[]
    trigger=[2,4,0,5,18,-1][case%6];swapping=(case//6)%2;mutation=[-1,0,1][(case//12)%3]
    scales=[bits(rng.choice((0.,1.,.8,-1.,rng.uniform(-3,3)))) for _ in range(3)]
    start=list(scales)
    p.put(iterator+0x20,trigger);p.put(iterator+0x28,view,8);p.put(view+0x19,swapping,1)
    p.put(view+0x20,root,8);p.put(view+0x30,factory,8);p.put(root+0x18,transform,8);p.put(root+0x20,visual,8)
    p.put(easer+0x18,0x2c95c94,8)
    schedule=(case//36)%4
    deltas=[bits(rng.choice((0.,1/60,1/30,.0067,.1))) for _ in range(40)]
    if schedule==1:deltas=[bits(.5)]+deltas[1:]
    elif schedule==2:deltas=[bits(0.)]*3+[bits(.15)]+deltas[4:]
    elif schedule==3:deltas=[bits(-.02),bits(float('inf'))]+deltas[2:]
    cases.append(','.join(map(str,[trigger,swapping,mutation,*start]))+';'+','.join(map(str,deltas)))
    for delta_bits in deltas:
        moved=call()
        expected.append(','.join([str(moved),normal(p.read(iterator+0x30)),*[normal(n) for n in scales],str(int(p.read(iterator+0x18,8)!=0)),str(p.read(view+0x19,1)),str(p.read(root+0x31,1))])+';'+','.join(trace))

harness=r'''using System;using System.Collections.Generic;using Whitebox.Foundation;
class View:IOriginalMatchExplodeAnimationView{public List<string> Trace=new List<string>();public OriginalSwapPoint Scale;public bool Swapping,Hidden;public int Mutation;
public bool IsSwapping{get{return Swapping;}}public OriginalSwapPoint RootScale{get{Trace.Add("get");return Scale;}set{Scale=value;Trace.Add("scale");}}
public void PlayExplodeParticle(bool extra){Trace.Add("particle:"+(extra?1:0));if(Mutation!=-1)Swapping=Mutation==1;}
public void HideView(){Trace.Add("hide");Hidden=true;}public void RecycleView(){Trace.Add("recycle");}}
class Verify{static string Bits(float v){return float.IsNaN(v)?"nan":BitConverter.ToUInt32(BitConverter.GetBytes(v),0).ToString();}
static float Float(string s){return BitConverter.ToSingle(BitConverter.GetBytes(uint.Parse(s)),0);}
static void Main(){string line;while((line=Console.ReadLine())!=null){var parts=line.Split(';');var a=parts[0].Split(',');
var view=new View{Swapping=a[1]=="1",Mutation=int.Parse(a[2]),Scale=new OriginalSwapPoint(Float(a[3]),Float(a[4]),Float(a[5]))};float delta=0;
var iterator=new OriginalMatchExplodeAnimation(view,()=>{view.Trace.Add("delta");return delta;},seconds=>{if(seconds!=1f)throw new Exception();view.Trace.Add("wait");return new object();},int.Parse(a[0]));
foreach(var d in parts[1].Split(',')){delta=Float(d);bool moved=iterator.MoveNext();var s=view.Scale;
Console.WriteLine((moved?1:0)+","+Bits(iterator.Elapsed)+","+Bits(s.X)+","+Bits(s.Y)+","+Bits(s.Z)+","+(iterator.Current==null?0:1)+","+(view.Swapping?1:0)+","+(view.Hidden?1:0)+";"+string.Join(",",view.Trace));}
try{iterator.Reset();throw new Exception("Reset supported");}catch(NotSupportedException){}}}}
namespace UnityEngine{public static class Debug{public static void LogError(object x){}}}'''
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder);source=folder/'Verify.cs';source.write_text(harness);exe=folder/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
    names=('OriginalMatchExplodeAnimation','OriginalSwapCompletion','OriginalSwapActionState','OriginalSwapDecision','OriginalItemSwapAvailability',
        'OriginalFillingItemPlacement','OriginalFillingCell','OriginalFallRefillColumns','OriginalCellTouchRules','OriginalCellMediation',
        'OriginalGridIterator','OriginalMatchSearch','OriginalMatchCandidates','OriginalMatchSnapshot','OriginalGameOperations','OriginalCellFallState')
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),*[str(runtime/(n+'.cs')) for n in names]],capture_output=True,text=True,encoding='utf-8')
    assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8')
    assert result.returncode==0,result.stdout+result.stderr
    actual=result.stdout.replace('\ufeff','').splitlines()
    assert len(actual)==len(expected),(len(actual),len(expected))
    assert actual==expected,next(((i,cases[i//40],a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e),'count mismatch')
report={'passed':True,'native_animation_cases':len(cases),'native_frame_comparisons':len(expected),
 'original_rvas':['0x21aa268','0x2c95c94','0x2b51e84','0x2b51ea0','0x2b4acf4'],
 'scope':'Native iterator state, SineIn float arithmetic, clamped scale, particle delay/extraEmit, immediate versus one-second delayed recycle, swap-state mutation during particle callback and final yield. Includes zero/negative/large/infinite delta, all six trigger cases and 40 resumes per case. Native cosine is supplied using correctly rounded float of host cosine; platform cosine implementation accuracy is not asserted. Unity transform/storage, wait-object allocation, particle and item-factory recycle are supplied boundaries; concrete particle playback/pool reset and scene input/refill remain pending.'}
(p.PROJECT/'match_explode_animation_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
