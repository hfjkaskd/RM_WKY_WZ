"""Compare actual native normal-swap completion callbacks with managed choreography."""
import sys
sys.dont_write_bytecode=True
import verify_board_layout as b
import itertools,json,struct,subprocess,tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_X3,UC_ARM64_REG_PC,UC_ARM64_REG_LR,UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2,UC_ARM64_REG_S3
p=b.p
action=0x10000000;valid=0x10001000;secondary=0x10002000;wrong=0x10003000
items=[0x10004000,0x10005000];views=[0x10006000,0x10007000]
item_klass=0x10008000;view_klass=0x10009000;mediators=[0x1000a000,0x1000b000]
cells=[0x1000c000,0x1000d000];state=0x1000e000;container=0x1000e100;counts=0x1000e200
event=0x1000f000;particles=[0x10010000,0x10010100];tween=0x10010200
trace=[];present=[True,True];native_error=False
bits=lambda value:struct.unpack('<I',struct.pack('<f',value))[0]
def index(values,value):return values.index(value) if value else -1
def sorting(pack,sort_everything):return f'{pack&0xffffffff},{(pack>>32)&0xffffffff},{sort_everything&255}'
def boundary(uc,address,size,data):
    global native_error
    x0,x1,x2,x3=[uc.reg_read(r) for r in (UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_X3)]
    value=None
    if address==0xffda88:
        native_error=True;uc.reg_write(UC_ARM64_REG_PC,0x20000000);return
    if address==0x20000100:
        i=views.index(x0);trace.append(f'end{i}');p.put(x0+0x19,0,1);value=0
    elif address==0x20000120:value=views[items.index(x0)]
    elif address==0x20000140:
        i=views.index(x0);trace.append(f'sort{i}:'+sorting(x1,x2));value=x0
    elif address==0x20000180:
        trace.append(f'event{x1}:{p.read(action+0x10,1)}:{p.read(cells[0]+0x28)}:{p.read(cells[1]+0x28)}');value=0
    elif address==0x2b45a6c:value=int(present[mediators.index(x0)])
    elif address==0x2b470f4:
        i=views.index(x0);trace.append(f'arrange{i}');value=(17+i)|((100+i)<<32);uc.reg_write(UC_ARM64_REG_X1,1)
    elif address==0x1ee0a98:
        trace.append(f'groups:{index(items,x1)}:{index(items,x2)}:{p.read(cells[0]+0x28)}:{p.read(cells[1]+0x28)}');value=0
    elif address==0x1ee42c0:trace.append(f'special:{index(items,x1)}:{index(items,x2)}');value=0
    elif address==0x3205d7c:value=123
    elif address==0x21805c0:trace.append(f'sfx{x1}:{uc.reg_read(UC_ARM64_REG_S0)}');value=0
    elif address==0x359e6bc:trace.append(f'haptic{x1}:{x2}:{uc.reg_read(UC_ARM64_REG_S0)}:{uc.reg_read(UC_ARM64_REG_S1)}:{x3}');value=0
    elif address==0x21a95d8:trace.append(f'particle{particles.index(x0)}:'+sorting(x1,x2));value=0
    elif address==0x3e4737c:value=x0
    elif address==0x2c97a6c:
        values=[uc.reg_read(r) for r in (UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2,UC_ARM64_REG_S3)]
        trace.append(f'move{views.index(x0)}:'+','.join(map(str,values)));value=tween
    elif address==0x2044a78:value=0
    elif address==0x2687f9c:value=x0
    elif address==0x2861a3c:value=0
    if value is not None:
        uc.reg_write(UC_ARM64_REG_X0,value);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,boundary)
def setup():
    p.emu.mem_write(action,bytes(0x20000));p.heap=0x10020000
    p.put(action+0x10,1,1);p.put(action+0x68,cells[0],8);p.put(action+0x70,cells[1],8)
    p.put(action+0x20,event,8);p.put(action+0x40,0x10011000,8);p.put(action+0x48,0x10011100,8)
    p.put(event+0x18,0x20000180,8);p.put(event+0x40,action,8)
    p.put(state+0x10,container,8);p.put(state+0x28,-1);p.put(container+0x10,counts,8);p.put(container+0x18,-1);p.put(counts+0x18,28);p.put(counts+0x24,2)
    for i in range(2):
        p.put(cells[i]+0x28,1);p.put(cells[i]+0x70,state,8)
        p.put(items[i],item_klass,8);p.put(items[i]+0x38,mediators[i],8)
        p.put(views[i],view_klass,8);p.put(views[i]+0x19,1,1)
    p.put(item_klass+0x418,0x20000120,8);p.put(view_klass+0x238,0x20000100,8);p.put(view_klass+0x208,0x20000140,8)
def snapshot():
    return ','.join(map(str,[p.read(action+0x10,1),p.read(cells[0]+0x28),p.read(cells[1]+0x28),p.read(views[0]+0x19,1),p.read(views[1]+0x19,1),p.read(counts+0x24),p.signed(p.read(state+0x28))]))
cases=[];expected=[]
for combo,other_exists,has0,has1,order in itertools.product(range(2),repeat=5):
    setup();present=[bool(has0),bool(has1)];trace=[]
    p.put(valid+0x10,items[0] if other_exists else 0,8);p.put(valid+0x18,views[1],8);p.put(valid+0x20,combo,1)
    p.put(valid+0x28,items[1],8);p.put(valid+0x30,action,8);p.put(valid+0x38,1)
    p.put(secondary+0x10,views[0],8);p.put(secondary+0x18,valid,8)
    if order and other_exists:b.call(0x1ee45cc,secondary)
    b.call(0x1ee44b4,valid)
    if not order and other_exists:b.call(0x1ee45cc,secondary)
    cases.append(f'V,{combo},{other_exists},{has0},{has1},{order}');expected.append('|'.join(trace)+'#'+snapshot())
for has0,has1,order in itertools.product(range(2),repeat=3):
    setup();present=[bool(has0),bool(has1)];trace=[]
    p.put(wrong+0x10,items[0],8);p.put(wrong+0x18,action,8);p.put(wrong+0x20,views[0],8);p.put(wrong+0x28,particles[0],8)
    p.put(wrong+0x40,views[1],8);p.put(wrong+0x48,23);p.put(wrong+0x4c,9005);p.put(wrong+0x50,1)
    p.put(wrong+0x58,particles[1],8);p.put(wrong+0x70,items[1],8)
    for offset,values in ((0x30,(1.25,-2.5,0.125)),(0x60,(-3.5,4.25,-0.5))):
        for i,value in enumerate(values):p.put(wrong+offset+4*i,bits(value))
    b.call(0x1ee4734,wrong);b.call(0x1ee4968,wrong)
    if order:b.call(0x1ee4aa8,wrong)
    b.call(0x1ee48dc,wrong)
    if not order:b.call(0x1ee4aa8,wrong)
    cases.append(f'W,0,1,{has0},{has1},{order}');expected.append('|'.join(trace)+'#'+snapshot())
for first_present,second_present,first_busy,second_busy in itertools.product(range(2),repeat=4):
    setup();manager=0x10011000;first=0x10012000;second=0x10013000
    p.put(manager+0x58,first if first_present else 0,8);p.put(manager+0x60,second if second_present else 0,8)
    p.put(first+0x10,first_busy,1);p.put(second+0x10,second_busy,1);native_error=False
    result=b.call(0x1ee251c,manager)
    cases.append(f'S,{first_present},{second_present},{first_busy},{second_busy},0')
    expected.append('E' if native_error else str(1 if result==first else 2 if result==second else 0))
harness=r'''using System;using System.Collections.Generic;using Whitebox.Foundation;
namespace UnityEngine{public class MonoBehaviour{}public static class Debug{public static void LogError(object x){throw new Exception(x.ToString());}}}
class View:OriginalItemSwapView{public int Id;public List<string> Trace;public override void SwapAnimationEnded(){Trace.Add("end"+Id);base.SwapAnimationEnded();}}
class Context:IOriginalSwapMatchContext{public IReadOnlyList<IOriginalSwapMatchGroup> FindExtensiveMatchesInArea(OriginalGridCell c,int r){return new IOriginalSwapMatchGroup[0];}public bool HasSpecialItem(OriginalGridCell c){return false;}public bool CanComboWith(OriginalBoardItem a,OriginalBoardItem b){throw new Exception();}}
class Backend:IOriginalSwapAnimationBackend{public void PlayValidSwap(OriginalSwapActionState a){a.MarkAnimating();}public void PlayWrongSwap(OriginalSwapActionState a){a.MarkAnimating();}}
class Services:IOriginalSwapCompletionServices{public List<string> Trace;public View[] Views;public OriginalGridCell[] Cells;public Action[] Returns=new Action[2];static uint Bits(float value){return BitConverter.ToUInt32(BitConverter.GetBytes(value),0);}static string Sorting(OriginalSwapSorting s){return s.Layer+","+s.Order+","+(s.SortEverything?1:0);}public IOriginalSwapAnimationView GetView(OriginalBoardItem item){return Views[item.Id];}public OriginalSwapSorting ArrangeSelfSorting(IOriginalSwapAnimationView v){int i=((View)v).Id;Trace.Add("arrange"+i);return new OriginalSwapSorting(17+i,100+i,true);}public void SetSorting(IOriginalSwapAnimationView v,OriginalSwapSorting s){Trace.Add("sort"+((View)v).Id+":"+Sorting(s));}public void UpdateParticleSorting(object particle,OriginalSwapSorting s){Trace.Add("particle"+particle+":"+Sorting(s));}public void PlaySfx(int id,float delay){Trace.Add("sfx"+id+":"+Bits(delay));}public void PlayHaptic(int id,int type,float strength,float duration,bool replay){Trace.Add("haptic"+id+":"+type+":"+Bits(strength)+":"+Bits(duration)+":"+(replay?1:0));}public void MoveToLocal(IOriginalSwapAnimationView view,OriginalSwapPoint p,float duration,Action complete){int i=((View)view).Id;Trace.Add("move"+i+":"+Bits(p.X)+","+Bits(p.Y)+","+Bits(p.Z)+","+Bits(duration));Returns[i]=complete;}public void ExplodeMatchGroups(OriginalSwapDecision decision,OriginalBoardItem other,OriginalBoardItem moving){Trace.Add("groups:"+(other==null?-1:other.Id)+":"+moving.Id+":"+Cells[0].FallFreezeCount+":"+Cells[1].FallFreezeCount);}public void ExplodeSpecialItems(OriginalBoardItem other,OriginalBoardItem moving){Trace.Add("special:"+(other==null?-1:other.Id)+":"+moving.Id);}}
class Verify{static void Main(){string line;while((line=Console.ReadLine())!=null){var a=line.Split(',');bool combo=a[1]=="1",other=a[2]=="1",order=a[5]=="1";var trace=new List<string>();var ops=new OriginalGameOperations(()=>123);var cells=new[]{new OriginalGridCell(0,0,1),new OriginalGridCell(1,0,1)};var items=new[]{new OriginalMatchBoardItem(0,0,1),new OriginalMatchBoardItem(1,0,2)};var views=new[]{new View{Id=0,Trace=trace},new View{Id=1,Trace=trace}};for(int i=0;i<2;i++){cells[i].BindGameOperations(ops);cells[i].FreezeFall();cells[i].Mediation.SetAllItems(items[i]);items[i].BindSwapView(views[i]);views[i].SwapAnimationStarted(true,0);}var action=new OriginalSwapActionState(new OriginalSwapDecision(new Context()));action.StartSwapAnimation(cells[0],cells[1],new Backend());action.OnSwapAnimationEnd+=(move)=>trace.Add("event"+move+":"+(action.IsSwapping?1:0)+":"+cells[0].FallFreezeCount+":"+cells[1].FallFreezeCount);for(int i=0;i<2;i++)if(a[3+i]=="0")items[i].Mediator.ClearFromAllRegisteredCells();var services=new Services{Trace=trace,Views=views,Cells=cells};var completion=new OriginalSwapCompletion(action,services,0.135f,0.04f);
if(a[0]=="V"){completion.ConfigureValid(other?items[0]:null,items[1],views[1],views[0],combo,1);if(order&&other)completion.ValidSecondaryComplete();completion.ValidPrimaryComplete();if(!order&&other)completion.ValidSecondaryComplete();}
else{completion.ConfigureWrong(items[0],items[1],views[0],views[1],0,1,new OriginalSwapPoint(1.25f,-2.5f,0.125f),new OriginalSwapPoint(-3.5f,4.25f,-0.5f),new OriginalSwapSorting(23,9005,true));completion.WrongPrimaryOutComplete();completion.WrongSecondaryOutComplete();if(order)services.Returns[1]();services.Returns[0]();if(!order)services.Returns[1]();}
Console.WriteLine(string.Join("|",trace)+"#"+(action.IsSwapping?1:0)+","+cells[0].FallFreezeCount+","+cells[1].FallFreezeCount+","+(views[0].IsSwapping?1:0)+","+(views[1].IsSwapping?1:0)+","+ops.Container.GetCount(1)+","+ops.LastOperationFrame);}}}'''
# Exercise selector null/idle/busy cases before entering callback fixtures.
harness=harness.replace("var a=line.Split(',');bool combo=", "var a=line.Split(',');if(a[0]==\"S\"){OriginalSwapActionState first=a[1]==\"1\"?new OriginalSwapActionState(new OriginalSwapDecision(new Context())):null;OriginalSwapActionState second=a[2]==\"1\"?new OriginalSwapActionState(new OriginalSwapDecision(new Context())):null;if(first!=null&&a[3]==\"1\")first.MarkAnimating();if(second!=null&&a[4]==\"1\")second.MarkAnimating();try{var selected=OriginalSwapActionState.Select(first,second);Console.WriteLine(selected==null?\"0\":selected==first?\"1\":\"2\");}catch(NullReferenceException){Console.WriteLine(\"E\");}continue;}bool combo=")
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder);source=folder/'Verify.cs';source.write_text(harness);exe=folder/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
    names=('OriginalSwapCompletion','OriginalSwapActionState','OriginalSwapDecision','OriginalItemSwapAvailability','OriginalItemSwapView',
        'OriginalFillingItemPlacement','OriginalFillingCell','OriginalFallRefillColumns','OriginalCellTouchRules','OriginalCellMediation',
        'OriginalGridIterator','OriginalMatchSearch','OriginalMatchCandidates','OriginalMatchSnapshot','OriginalGameOperations','OriginalCellFallState')
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),*[str(runtime/(n+'.cs')) for n in names]],capture_output=True,text=True,encoding='utf-8')
    assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8')
    assert result.returncode==0,result.stdout+result.stderr
    actual=result.stdout.replace('\ufeff','').splitlines()
    assert actual==expected,next(((cases[i],a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e),'count mismatch')
report={'passed':True,'native_valid_callback_cases':32,'native_wrong_callback_cases':8,'native_action_selector_cases':16,
 'original_rvas':['0x1ee44b4','0x1ee45cc','0x1ee4734','0x1ee48dc','0x1ee4968','0x1ee4aa8','0x1ee251c'],
 'scope':'Actual native callback ordering, view flags, native freeze/operation release, sorting/particle offsets, return tween targets/timing, SFX/haptic arguments and busy state during OnEnd. Rendering, motion evaluation, concrete explosions, particle/audio services and view lookup supplied at boundaries. Unity input and playable resolution remain pending.'}
(p.PROJECT/'swap_completion_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
