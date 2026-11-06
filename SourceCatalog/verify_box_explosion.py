"""Actual native box hit/view flow and cell near-match routing vs production C#."""
import sys
sys.dont_write_bytecode=True
import verify_item_explosion as v
import json,subprocess,tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_LR,UC_ARM64_REG_PC,UC_ARM64_REG_S0
p=v.p
sfx=0x10050000;assets=0x10051000;renderer=0x10052000;sprites=0x10053000;random_context=0x10054000;sprite_base=0x10055000
replacement=0x10060000;replacement_class=0x10061000;replacement_mediator=0x10062000
random_values=(-1,0,1,2,99);random_index=0;static_action=0;cap_mode=0
def boundary(uc,address,size,unused):
    global random_index
    x0,x1,x2=[uc.reg_read(r) for r in (UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2)]
    result=None
    if address==0xffda90:v.error=True;uc.reg_write(UC_ARM64_REG_PC,0x20000000);return
    if address==0x30f6400:
        assert x0==5;result=random_context
    elif address==0x2d70ac4:
        assert (x0,x1,x2)==(random_context,0,3)
        result=random_values[random_index%len(random_values)];random_index+=1;v.trace.append('random:'+str(result))
    elif address==0x21805c0:
        assert x0==sfx;v.trace.append('sfx:'+str(x1)+':'+str(uc.reg_read(UC_ARM64_REG_S0)));result=0
    elif address==0x2bc5fa8:v.trace.append('get-sprite:'+str(p.signed(x1&0xffffffff))) # Actual native bounds/index executes.
    elif address==0x3e50c6c:
        assert x0==renderer;v.trace.append('sprite:'+str((x1-sprite_base)//0x100+1));result=0
    elif address==0x2bc6220:v.trace.append('particles:'+str(p.signed(x1&0xffffffff)));result=0
    elif address==0x3440544:
        v.trace.append('above:'+v.data_string(x1)+':'+v.state_string())
        if static_action==2:
            p.put(v.mediator+0x10,0,8);p.put(v.cell(0)+0x218,0,8)
        result=int(static_action==1)
    elif address==0x20000500:
        v.trace.append('cap')
        if cap_mode==1:p.put(v.mediator+0x10,0,8);p.put(v.cell(0)+0x218,0,8)
        elif cap_mode==2:
            p.put(v.mediator+0x10,0,8);p.put(v.cell(0)+0x218,replacement,8);p.put(replacement_mediator+0x10,v.cell(0)+0x200,8)
        result=int(cap_mode!=0)
    elif address==0x20000600:v.trace.append('replacement:'+v.data_string(x1));result=0
    if result is not None:uc.reg_write(UC_ARM64_REG_X0,result&0xffffffffffffffff);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,boundary)
def setup(layer=1,current=1,with_goal=1,with_view=1,with_event=1):
    global random_index
    v.kind='B';v.refreeze=False;v.mutate=0;v.setup(layer,current,with_goal,True,with_event);random_index=0
    p.put(v.klass+0x458,0x2bc5b10,8);p.put(v.klass+0x528,0x2b46aa0,8);p.put(v.klass+0x428,0x2bc5af8,8);p.put(v.klass+0x508,0x2b468dc,8)
    p.put(v.model+0xb0,v.item_view if with_view else 0,8)
    p.put(v.item_view+0x30,v.factory,8);p.put(v.item_view+0x38,sfx,8);p.put(v.item_view+0x70,renderer,8);p.put(v.item_view+0x78,assets,8)
    p.put(assets+0x20,sprites,8);p.put(sprites+0x18,4)
    for i in range(4):p.put(sprites+0x20+i*8,sprite_base+i*0x100,8)
def finish_timer():v.delta_bits=v.bits(.2);v.call(0x3446be4,(v.view,))
cases=[];expected=[]
for n in range(72):
    layer=(-2,0,1,2,3,4,5,9)[n%8];current=int(n%11!=0);with_goal=int(n%7!=0);with_view=int(n%13!=0);with_event=n%2
    setup(layer,current,with_goal,with_view,with_event);operations=[];outputs=[]
    for i in range(10):
        ident=(n+i*3)%6-1;all_=(n+i)%3==0;trigger=25 if all_ else 5;v.write_data(ident,trigger)
        v.call(0x2b46978 if all_ else 0x2b468dc,(v.model,v.data));outputs.append(('E' if v.error else '1')+':'+str(p.signed(p.read(v.model+0x68)))+':'+v.state_string());operations.append(str(int(all_))+','+str(ident))
    finish_timer();header=(layer,current,with_goal,with_view,with_event)
    cases.append('B#'+','.join(map(str,header))+'#'+';'.join(operations));expected.append('|'.join(v.trace)+'#'+'|'.join(outputs)+'#'+v.state_string())
for n in range(40):
    setup();random_index=n%5;explode=n%2;layer=(-1,0,1,2,4,5)[n%6]
    v.call(0x2bc5b6c if explode else 0x2bc5c58,(v.item_view,layer));result='E' if v.error else '1'
    cases.append('V#'+','.join(map(str,(explode,layer,n%5))));expected.append(result+'#'+'|'.join(v.trace))
for n in range(96):
    target=n%4;current=int(n%7!=0 and target!=0);static_action=(n//4)%3;cap_mode=(n//12)%4;with_static=int(n%11!=0)
    setup(1,current);v.write_data(11,5)
    if not with_static:p.put(v.cell(0)+0xc0,0,8)
    if target==1:p.put(v.klass+0x428,0x21a5a6c,8)
    elif target==3:
        p.put(v.klass+0x428,0x20000500,8);p.put(v.klass+0x458,0x20000100,8)
        p.put(replacement,replacement_class,8);p.put(replacement+0x38,replacement_mediator,8);p.put(replacement+0x70,v.container,8)
        p.put(replacement_class+0x458,0x20000600,8);p.put(replacement_class+0x508,0x2b468dc,8)
    v.call(0x319d7a4,(v.cell(0),v.data));result='E' if v.error else '1';finish_timer()
    cases.append('N#'+','.join(map(str,(target,current,static_action,cap_mode,with_static))));expected.append(result+'#'+'|'.join(v.trace)+'#'+v.state_string())
harness=r'''using System;using System.Collections.Generic;using Whitebox.Foundation;
namespace UnityEngine{public static class Debug{public static void LogError(object x){}}}
class Services:IOriginalBoxViewExplosionServices,IOriginalBoxExplosionServices,IOriginalItemGoalServices,IOriginalCellStaticExplosionServices{
static readonly int[] values={-1,0,1,2,99};public int RandomIndex,StaticAction;
public int NextApplicationRandom(int min,int max){if(min!=0||max!=3)throw new Exception();int value=values[RandomIndex++%values.Length];Verify.Trace.Add("random:"+value);return value;}
public void PlaySfx(int type,float delay){Verify.Trace.Add("sfx:"+type+":"+Verify.Bits(delay));}public void SetSpriteForLayer(int layer){Verify.Trace.Add("get-sprite:"+layer);if(layer<1||layer>4)throw new IndexOutOfRangeException();Verify.Trace.Add("sprite:"+layer);}public void PlayExplodeParticle(int layer){Verify.Trace.Add("particles:"+layer);}public void RecycleView(){Verify.Trace.Add("recycle:"+Verify.State());}public void UsefulMoveMade(){Verify.Trace.Add("useful");}
public void DecreaseGoal(int goal){Verify.Trace.Add("decrease:"+goal+":"+Verify.State());}public void CollectAsGoalUi(OriginalBoardItem item){Verify.Trace.Add("ui:"+Verify.State());}
public bool TryExplodeTopMostAboveItem(OriginalGridCell cell,OriginalExplodeData data){Verify.Trace.Add("above:"+Verify.Data(data)+":"+Verify.State());if(StaticAction==2)cell.Mediation.ClearCurrentItem();return StaticAction==1;}
}
class Probe:OriginalBoardItem{public int Mode;public bool Replacement;public Probe():base(1,1){}public override bool CanExplodeWithNearMatch{get{if(Replacement)return true;Verify.Trace.Add("cap");if(Mode==1)Verify.Cells[0].Mediation.ClearCurrentItem();else if(Mode==2){Verify.Cells[0].Mediation.ClearCurrentItem();Verify.Cells[0].Mediation.SetCurrentItem(new Probe{Replacement=true});}return Mode!=0;}}protected override void TryExplode(OriginalExplodeData d){Verify.Trace.Add(Replacement?"replacement:"+Verify.Data(d):"try:"+LayerCount+":"+Verify.Data(d));}}
class Verify{
public static List<string> Trace=new List<string>();public static OriginalGridCell[] Cells;static OriginalBoardItem item;static OriginalCellFreezeCountdown timer;public static uint Bits(float f){return BitConverter.ToUInt32(BitConverter.GetBytes(f),0);}public static string State(){var m=item.Mediator;return (item.HasView?1:0)+","+(m.CurrentCell!=null?1:0)+","+(m.NextCell!=null?1:0)+","+(m.TargetCell!=null?1:0)+","+Cells[0].FallFreezeCount+","+Bits(timer.Remaining)+","+(timer.IsCounting?1:0);}
public static string Data(OriginalExplodeData d){var t=d.LastTargetItemIdentifier;return string.Join(",",new object[]{d.Point.X,d.Point.Y,d.Trigger,d.Id,d.MatchType,d.CanSpreadJelly?1:0,d.IsHorizontalRocket?1:0,d.ComboId,t.ItemType,t.StaticItemType,t.ItemGroupId,d.ExploderIndex,Bits(d.IncomingX),Bits(d.IncomingY),Bits(d.IncomingZ)});}static OriginalExplodeData Data(int id,int trigger){var d=new OriginalExplodeData(trigger,id,new OriginalCellPoint(3,4),2,true);d.IncomingX=.125f;d.IncomingY=-.25f;d.IncomingZ=.5f;return d;}
static void Setup(OriginalBoardItem model,bool current,bool withGoal,bool withEvent,Services services){item=model;Cells=new[]{new OriginalGridCell(0,0,1),new OriginalGridCell(1,0,1),new OriginalGridCell(2,0,1)};var ops=new OriginalGameOperations(()=>0);foreach(var c in Cells)c.BindGameOperations(ops);timer=new OriginalCellFreezeCountdown(Cells[0].UnFreezeFall);Cells[0].BindFreezeCountdown(timer);if(current)Cells[0].Mediation.SetCurrentItem(item);Cells[1].Mediation.SetNextItem(item);Cells[2].Mediation.SetTargetItem(item);item.MarkViewCreated();item.GoalType=7;if(withGoal)item.BindGoalServices(services);if(withEvent)item.OnExplodeEvent+=()=>Trace.Add("event:"+State());Trace.Clear();}
static string Call(Action action){try{action();return "1";}catch(NullReferenceException){return "E";}catch(IndexOutOfRangeException){return "E";}}
static void Main(){string line;while((line=Console.ReadLine())!=null){var a=line.Split('#');var h=Array.ConvertAll(a[1].Split(','),int.Parse);var services=new Services();if(a[0]=="B"){var box=new OriginalBoxBoardItem(1,36,h[0]);Setup(box,h[1]!=0,h[2]!=0,h[4]!=0,services);if(h[3]!=0)box.BindExplosionServices(new OriginalBoxViewExplosion(services),services);var outputs=new List<string>();foreach(var op in a[2].Split(';')){var p=Array.ConvertAll(op.Split(','),int.Parse);var data=Data(p[1],p[0]!=0?25:5);string result=Call(()=>{if(p[0]!=0)box.ExplodeAll(data);else box.Explode(data);});outputs.Add(result+":"+box.LayerCount+":"+State());}timer.Update(.2f);Console.WriteLine(string.Join("|",Trace)+"#"+string.Join("|",outputs)+"#"+State());continue;}
if(a[0]=="V"){Setup(new Probe(),true,true,true,services);services.RandomIndex=h[2];var view=new OriginalBoxViewExplosion(services);string result=Call(()=>{if(h[0]!=0)view.Explode();else view.Damage(h[1]);});Console.WriteLine(result+"#"+string.Join("|",Trace));continue;}
OriginalBoardItem target=h[0]==1?(OriginalBoardItem)new OriginalMatchBoardItem(1,1,1):h[0]==3?(OriginalBoardItem)new Probe{Mode=h[3]}:new OriginalBoxBoardItem(1,36);Setup(target,h[1]!=0,true,true,services);if(target is OriginalBoxBoardItem b)b.BindExplosionServices(new OriginalBoxViewExplosion(services),services);services.StaticAction=h[2];string outcome=Call(()=>OriginalCellNearMatchExplosion.ExplodeCurrentItemByNearMatch(Cells[0],Data(11,5),h[4]!=0?services:null));timer.Update(.2f);Console.WriteLine(outcome+"#"+string.Join("|",Trace)+"#"+State());
}}}'''
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder);source=folder/'Verify.cs';source.write_text(harness);exe=folder/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
    names=('OriginalBoxItemExplosion','OriginalBoxViewExplosion','OriginalCellNearMatchExplosion',
      'OriginalItemExplosion','OriginalMatchItemExplosion','OriginalCellFreezeCountdown','OriginalMatchExplosionData','OriginalMatchExplosionReadiness','OriginalExplodeData',
      'OriginalFallComponentState','OriginalItemSwapAvailability','OriginalFillingItemPlacement','OriginalFillingCell','OriginalFallRefillColumns','OriginalCellTouchRules',
      'OriginalCellMediation','OriginalGridIterator','OriginalMatchSearch','OriginalMatchCandidates','OriginalMatchSnapshot','OriginalCellFallState','OriginalGameOperations')
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),*[str(runtime/(n+'.cs')) for n in names]],capture_output=True,text=True,encoding='utf-8')
    assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8')
    assert result.returncode==0,result.stdout+result.stderr
    actual=result.stdout.replace('\ufeff','').splitlines()
    assert actual==expected,next(((i,cases[i],value,wanted) for i,(value,wanted) in enumerate(zip(actual,expected)) if value!=wanted),'count mismatch')
report={'passed':True,'native_box_hit_traces':72,'native_box_hit_operations':720,'native_box_view_cases':40,'native_near_match_cases':96,
 'original_rvas':['0x2bc5af8','0x21a5a6c','0x2bc5b10','0x2bc5b6c','0x2bc5c58','0x2bc5fa8','0x319d7a4','0x2b468dc','0x2b46978','0x2b46aa0'],
 'scope':'Actual native box TryExplode, view Explode/Damage and cell near-match routing vs production C#. Native base ID dedup/layer-all handling, final removal/goal dispatch and timed freeze execute too. Covers layer decrement before view checks, damage versus final explode, application-random SFX selection and order, sprite bounds, particle/recycle dispatch, missing views/goals/current cells, duplicate IDs and capability-query removal/replacement. HashSet storage, item cell delegates and freeze counters supplied; static top-item handling, random provider, SFX/renderer/particle/item-factory/goal/session boundaries traced. Concrete Unity sprite/particle/audio/pool adapter, special spawn and fall/refill/input remain pending.'}
(p.PROJECT/'box_explosion_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
