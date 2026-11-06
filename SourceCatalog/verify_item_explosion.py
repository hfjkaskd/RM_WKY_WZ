"""Native item hit deduplication, match TryExplode, model release and freeze timer."""
import sys
sys.dont_write_bytecode=True
import verify_board_layout as b
import json,random,struct,subprocess,tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_X3,UC_ARM64_REG_X4,UC_ARM64_REG_X8,UC_ARM64_REG_LR,UC_ARM64_REG_PC,UC_ARM64_REG_SP,UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2
p=b.p
model=0x10000000;klass=0x10001000;mediator=0x10002000;container=0x10003000;hashset=0x10004000
cell_base=0x10010000;view=0x10005000;view_class=0x10006000;interfaces=0x10007000;level=0x10008000;goal=0x10009000
state=0x1000a000;factory=0x1000b000;item_view=0x1000c000;event=0x1000d000;data=0x1000e000
bits=lambda f:struct.unpack('<I',struct.pack('<f',f))[0]
trace=[];hits=set();kind='E';mutate=0;freeze_count=0;error=False;delta_bits=0;goal_left=0;static_mask=0;item_mask=0;manager_mask=0;pending=False;refreeze=False
def cell(i):return cell_base+i*0x1000
def cell_id(pointer):return -1 if not pointer else (pointer-cell_base)//0x1000
def bind(slot):
    if slot not in b.resolved_slots:p.emu.reg_write(UC_ARM64_REG_X0,slot);b.boundary(p.emu,0xffd960,4,None)
    return p.read(slot,8)
cell_view_interface=bind(0x558dca8)
def state_string():
    return ','.join(map(str,[p.read(model+0x14,1),*[int(p.read(mediator+0x10+i*8,8)!=0) for i in range(3)],freeze_count,p.read(view+0x38),p.read(view+0x48,1)]))
def data_string(owner):
    values=[p.signed(p.read(owner+o)) for o in (0,4,8,12,16)]
    values += [p.read(owner+0x14,1),p.read(owner+0x15,1)]
    values += [p.signed(p.read(owner+o)) for o in (0x18,0x1c,0x20,0x24,0x28)]
    values += [p.read(owner+o) for o in (0x2c,0x30,0x34)]
    return ','.join(map(str,values))
def boundary(uc,address,size,unused):
    global freeze_count,error,pending
    x0,x1,x2,x3=[uc.reg_read(r) for r in (UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_X3)]
    value=None
    if address==0xffda88:error=True;uc.reg_write(UC_ARM64_REG_PC,0x20000000);return
    if address in (0xd00b50,0xd018a0):uc.mem_write(x0,bytes(uc.mem_read(x1,x2)));value=x0
    elif address==0xd01fa0:uc.mem_write(x0,bytes([x1&255])*x2);value=x0
    elif address==0x220ca20:value=int(p.signed(x1&0xffffffff) in hits)
    elif address==0x220d588:hits.add(p.signed(x1&0xffffffff));value=1
    elif address==0x220cc0c:
        ident=p.signed(x1&0xffffffff);value=int(ident in hits);hits.discard(ident)
    elif address in (0x2b45968,0x2b45a7c,0x2b45b90):
        offset={0x2b45968:0x10,0x2b45a7c:0x18,0x2b45b90:0x20}[address];slot=p.read(x0+offset,8);value=p.read(slot+0x10,8) if slot else 0
    elif address==0x31933d4:freeze_count+=1;value=0
    elif address==0x20000400:
        freeze_count-=1
        if kind=='T' and refreeze:p.put(view+0x38,bits(.7));p.put(view+0x48,1,1)
        value=0
    elif address==0x32037e4:uc.reg_write(UC_ARM64_REG_S0,delta_bits);value=0
    elif address==0x20000100:
        before=p.signed(p.read(model+0x68));trace.append('try:'+str(before)+':'+data_string(x1))
        if mutate==1:p.put(model+0x68,before-1)
        elif mutate==2:p.put(model+0x68,-3)
        value=0
    elif address==0x32136cc:trace.append('decrease:'+str(x1)+':'+state_string());value=0
    elif address==0x20000200:trace.append('ui:'+state_string());value=0
    elif address==0x20000300:trace.append('event:'+state_string());value=0
    elif address==0x3214794:trace.append('goal-ui:'+str(x1));value=goal_left
    elif address==0x1e19780:trace.append('static-level:'+str(x1));value=int(bool(static_mask&(1<<x1)))
    elif address==0x1e19848:trace.append('item-level:'+str(x1));value=int(bool(item_mask&(1<<x1)))
    elif address==0x260df1c:value=0x10030000+x0*0x100
    elif address in (0x21e6864,0x2e57a64,0x21ee360):
        ident={0x21e6864:0,0x2e57a64:1,0x21ee360:2}[address];trace.append('manager:'+str(ident)+':'+str(x1));value=0x10050000+ident*0x100 if manager_mask&(1<<ident) else 0
    elif address==0x317e8f8:trace.append('purple');value=0
    elif address==0x21a61d8:trace.append('ice');value=0
    elif address==0x21a6400:trace.append('collect-goal');value=0
    elif address==0x21a6510:trace.append('collect-manager:'+str((x1-0x10050000)//0x100));value=0
    elif address==0x1ee5ae8:trace.append('shifter-start') # Actual native counter updates.
    elif address==0x1ee5af8:trace.append('shifter-finish')
    elif address==0x3191dd4:
        for r,f in ((UC_ARM64_REG_S0,float(cell_id(x0))),(UC_ARM64_REG_S1,2.),(UC_ARM64_REG_S2,0.)):uc.reg_write(r,bits(f))
        value=0
    elif address==0x2044a78:value=0 # Action allocation/target metadata boundary.
    elif address==0x21a66d4:
        trace.append('merge:'+str(uc.reg_read(UC_ARM64_REG_S0))+':'+str(x1));pending=True;value=0
    elif address==0x21a9b14:trace.append('animation:'+str(x1));value=0x10070000
    elif address==0x3e4c364:value=0x10071000
    elif address==0x2b46b54:trace.append('useful');value=0
    elif address==0x34407f4:trace.append('below:'+data_string(x1)+':'+state_string());value=0
    elif address==0x21a6834:trace.append('madness:'+data_string(x1));value=0
    elif address==0x21a3fb0:trace.append('count:'+str(x0)+':'+str(x1));value=0
    elif address==0x260a1fc:trace.append('recycle:'+state_string());value=0
    if value is not None:uc.reg_write(UC_ARM64_REG_X0,value&0xffffffffffffffff);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,boundary)
def call(rva,args=(),floating=None):
    global error
    error=False
    for r,v in zip((UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_X3,UC_ARM64_REG_X4),[*args,0,0,0,0,0]):p.emu.reg_write(r,v&0xffffffffffffffff)
    if floating is not None:p.emu.reg_write(UC_ARM64_REG_S0,floating)
    p.emu.reg_write(UC_ARM64_REG_SP,0x101ff000);p.emu.reg_write(UC_ARM64_REG_LR,0x20000000)
    try:p.emu.emu_start(rva,0x20000000,count=1000000)
    except Exception:print('Native PC',hex(p.emu.reg_read(UC_ARM64_REG_PC)));raise
    assert p.emu.reg_read(UC_ARM64_REG_PC)==0x20000000
def setup(layer=1,current=1,with_goal=True,with_timer=True,with_event=True):
    global trace,hits,freeze_count,pending
    p.emu.mem_write(model,bytes(0x80000));p.heap=0x10090000;trace=[];hits=set();freeze_count=0;pending=False
    p.put(model,klass,8);p.put(model+0x14,1,1);p.put(model+0x20,7);p.put(model+0x38,mediator,8);p.put(model+0x68,layer);p.put(model+0x70,container,8);p.put(container+0x10,hashset,8)
    p.put(model+0x48,level,8);p.put(model+0x60,goal if with_goal else 0,8);p.put(model+0x58,factory,8);p.put(model+0x268,state,8);p.put(model+0xb0,item_view,8)
    p.put(klass+0x368,0x2b45d78,8);p.put(klass+0x458,0x20000100,8);p.put(klass+0x588,0x2b46c28,8);p.put(klass+0x598,0x20000200,8)
    if with_event:p.put(model+0xa8,event,8);p.put(event+0x18,0x20000300,8);p.put(event+0x40,model,8)
    for i in range(3):
        med=cell(i)+0x100;slot=cell(i)+0x200+i*0x100
        p.put(cell(i)+0x10,med,8);p.put(cell(i)+0xc0,cell(i)+0x700,8)
        p.put(med+0x10+i*8,slot,8);p.put(slot+0x10,cell(i),8);p.put(slot+0x18,model,8);p.put(slot+0x20,i)
        p.put(mediator+0x10+i*8,slot if current or i else 0,8)
        if not current and i==0:p.put(slot+0x18,0,8)
    p.put(cell(0)+0xb8,view if with_timer else 0,8)
    p.put(view+0x58,cell(0),8);p.put(cell(0),view_class,8);p.put(view_class+0x12a,1,2);p.put(view_class+0xb0,interfaces,8)
    p.put(interfaces,cell_view_interface,8);p.put(interfaces+8,0);p.put(view_class+0x1d8,0x20000400,8)
def write_data(ident,trigger=2):
    p.emu.mem_write(data,bytes(56))
    for o,v in ((0,3),(4,4),(8,trigger),(12,ident),(16,2),(0x18,-1),(0x1c,0),(0x20,-1),(0x24,-1),(0x28,-1),(0x2c,bits(.125)),(0x30,bits(-.25)),(0x34,bits(.5))):p.put(data+o,v)
    p.put(data+0x14,1,1)
cases=[];expected=[];rng=random.Random(0x21a5d90)
for n in range(60):
    kind='E';layer=(-3,0,1,2,7)[n%5];mutate=n%3;setup(layer)
    identifiers=[rng.randrange(-2,5) for _ in range(12)];operations=[]
    for i,ident in enumerate(identifiers):
        all_=(n+i)%2;write_data(ident,25 if all_ else 2);call(0x2b46978 if all_ else 0x2b468dc,(model,data));operations.append(str(all_)+','+str(ident))
    cases.append('E#'+str(layer)+','+str(mutate)+'#'+';'.join(operations));expected.append('|'.join(trace)+'#'+str(p.signed(p.read(model+0x68)))+'#'+','.join(map(str,sorted(hits))))
for n in range(48):
    kind='F';current=int(n%7!=0);with_goal=int(n%5!=0);with_timer=int(n%11!=0);with_event=n%2;duration=(0,.15,.2,-.1)[n%4]
    setup(current=current,with_goal=with_goal,with_timer=with_timer,with_event=with_event)
    call(0x2b46aa0,(model,),bits(duration));result='E' if error else '1'
    cases.append('F#'+','.join(map(str,(current,with_goal,with_timer,with_event,bits(duration)))));expected.append(result+'#'+'|'.join(trace)+'#'+state_string())
for n in range(64):
    kind='T';setup();refreeze=n%7==0;steps=[];outputs=[]
    pool=[bits(v) for v in (0,-0.,.15,.2,.001,1,1/60,-.1)]+[0x7f800000,0xff800000,0x7fc00000]
    for i in range(24):
        if i%3==0:
            val=rng.choice(pool);call(0x3190664,(cell(0),),val);steps.append('F,'+str(val))
        else:
            delta_bits=rng.choice(pool);call(0x3446be4,(view,));steps.append('U,'+str(delta_bits))
        outputs.append(str(freeze_count)+','+str(p.read(view+0x38))+','+str(p.read(view+0x48,1)))
    cases.append('T#'+str(int(refreeze))+'#'+';'.join(steps));expected.append('|'.join(outputs))
for n in range(152):
    current=int(n<144)
    kind='M';setup(current=current);color=5 if n%3==0 else 1+n%6;goal_left=(-1,0,2)[n%3];static_mask=(1<<5 if n%2 else 0)|(1<<16 if n%4 else 0)|(1<<17 if n%5 else 0)
    item_mask=(1<<29 if n%3 else 0)|(1<<22 if n%7 else 0);manager_mask=n%8;special=n%2;selected=int(n%13==0);trigger=(2,4,18,25)[n%4]
    p.put(model+0x18,color);p.put(model+0x279,selected,1);p.put(klass+0x458,0x21a5d90,8);p.put(klass+0x528,0x2b46aa0,8);p.put(klass+0x588,0x21a6b74,8)
    if special:
        p.put(model+0x1e0,3)
        for i,index in enumerate((2,0,1)):p.put(model+0x1e8+i*8,cell(index),8)
    write_data(11,trigger);call(0x2b468dc,(model,data));first_result='E' if error else '1'
    # A duplicate ID is ignored even after color-mixer selection clears.
    p.put(model+0x279,0,1);call(0x2b468dc,(model,data));assert not error
    if pending:call(0x21a7d18,(model,));assert not error
    delta_bits=bits(.2);call(0x3446be4,(view,))
    header=(color,goal_left,static_mask,item_mask,manager_mask,special,selected,trigger,current)
    cases.append('M#'+','.join(map(str,header)));expected.append(first_result+'#'+'|'.join(trace)+'#'+state_string()+'#'+str(p.read(state+0x20)))

harness=r'''using System;using System.Collections.Generic;using Whitebox.Foundation;
namespace UnityEngine{public static class Debug{public static void LogError(object x){}}}
class TestItem:OriginalBoardItem{public int Mutate;public TestItem(int layer):base(1,1,layer){}protected override void TryExplode(OriginalExplodeData d){Verify.Trace.Add("try:"+layer+":"+Verify.Data(d));if(Mutate==1)layer--;else if(Mutate==2)layer=-3;}public void Finish(float duration){FinalExplodeCompleted(duration);}}
class Manager:IOriginalMatchCollectManager{public int Id;}
class Services:IOriginalItemGoalServices,IOriginalMatchItemExplosionServices{
public int Goals,StaticMask,ItemMask,ManagerMask,Shifter;public Action Pending;
public void DecreaseGoal(int goal){Verify.Trace.Add("decrease:"+goal+":"+Verify.State());}public void CollectAsGoalUi(OriginalBoardItem item){Verify.Trace.Add("ui:"+Verify.State());}
public int GetGoalUiLeftCount(int goal){Verify.Trace.Add("goal-ui:"+goal);return Goals;}public bool IsThereStaticItemInLevel(int type){Verify.Trace.Add("static-level:"+type);return (StaticMask&(1<<type))!=0;}public bool IsThereItemInLevel(int type){Verify.Trace.Add("item-level:"+type);return (ItemMask&(1<<type))!=0;}
IOriginalMatchCollectManager Get(int id,int color){Verify.Trace.Add("manager:"+id+":"+color);return (ManagerMask&(1<<id))!=0?new Manager{Id=id}:null;}public IOriginalMatchCollectManager GetCurtainForMatchType(int color){return Get(0,color);}public IOriginalMatchCollectManager GetDrillForMatchType(int color){return Get(1,color);}public IOriginalMatchCollectManager GetCryptexForMatchType(int color){return Get(2,color);}
public void DecrementPurpleOnBoard(){Verify.Trace.Add("purple");}public void TryDamageNeighborIceBorders(OriginalMatchBoardItem item){Verify.Trace.Add("ice");}public void CollectToGoal(OriginalMatchBoardItem item){Verify.Trace.Add("collect-goal");}public void CollectToManager(OriginalMatchBoardItem item,IOriginalMatchCollectManager manager){Verify.Trace.Add("collect-manager:"+((Manager)manager).Id);}
public void StartShifterSpecialOperation(){Verify.Trace.Add("shifter-start");Shifter++;}public void ExplodeAndMoveToPosition(OriginalMatchBoardItem item,OriginalGridCell cell,int index,Action done){Verify.Trace.Add("merge:"+Verify.Bits((float)Array.IndexOf(Verify.Cells,cell))+":"+index);Pending=done;}public void RecycleMatchView(OriginalMatchBoardItem item){Verify.Trace.Add("recycle:"+Verify.State());}public void FinishShifterSpecialOperation(){Verify.Trace.Add("shifter-finish");Shifter--;}
public void StartExplodeAnimation(OriginalMatchBoardItem item,int trigger){Verify.Trace.Add("animation:"+trigger);}public void UsefulMoveMade(){Verify.Trace.Add("useful");}public void ExplodeTopMostBelowItem(OriginalGridCell cell,OriginalExplodeData data){Verify.Trace.Add("below:"+Verify.Data(data)+":"+Verify.State());}public void HandleMadness(OriginalMatchBoardItem item,OriginalExplodeData data){Verify.Trace.Add("madness:"+Verify.Data(data));}public void MatchItemExploded(int color,int count){Verify.Trace.Add("count:"+color+":"+count);}
}
class Verify{
public static List<string> Trace=new List<string>();public static OriginalGridCell[] Cells;static OriginalBoardItem item;static OriginalCellFreezeCountdown timer;static bool refreeze;public static uint Bits(float f){return BitConverter.ToUInt32(BitConverter.GetBytes(f),0);}static float Float(uint b){return BitConverter.ToSingle(BitConverter.GetBytes(b),0);}public static string State(){var m=item.Mediator;return (item.HasView?1:0)+","+(m.CurrentCell!=null?1:0)+","+(m.NextCell!=null?1:0)+","+(m.TargetCell!=null?1:0)+","+Cells[0].FallFreezeCount+","+Bits(timer.Remaining)+","+(timer.IsCounting?1:0);}
public static string Data(OriginalExplodeData d){var t=d.LastTargetItemIdentifier;return string.Join(",",new object[]{d.Point.X,d.Point.Y,d.Trigger,d.Id,d.MatchType,d.CanSpreadJelly?1:0,d.IsHorizontalRocket?1:0,d.ComboId,t.ItemType,t.StaticItemType,t.ItemGroupId,d.ExploderIndex,Bits(d.IncomingX),Bits(d.IncomingY),Bits(d.IncomingZ)});}static OriginalExplodeData Data(int id,int trigger){var d=new OriginalExplodeData(trigger,id,new OriginalCellPoint(3,4),2,true);d.IncomingX=.125f;d.IncomingY=-.25f;d.IncomingZ=.5f;return d;}
static void Setup(bool current,bool withTimer,bool withGoal,bool withEvent,Services services){Cells=new[]{new OriginalGridCell(0,0,1),new OriginalGridCell(1,0,1),new OriginalGridCell(2,0,1)};var ops=new OriginalGameOperations(()=>0);foreach(var c in Cells)c.BindGameOperations(ops);timer=new OriginalCellFreezeCountdown(()=>{Cells[0].UnFreezeFall();if(refreeze)timer.StartCountdown(.7f);});if(withTimer)Cells[0].BindFreezeCountdown(timer);if(current)Cells[0].Mediation.SetCurrentItem(item);Cells[1].Mediation.SetNextItem(item);Cells[2].Mediation.SetTargetItem(item);item.MarkViewCreated();if(withGoal)item.BindGoalServices(services);item.GoalType=7;if(withEvent)item.OnExplodeEvent+=()=>Trace.Add("event:"+State());Trace.Clear();}
static void Main(){string line;while((line=Console.ReadLine())!=null){var a=line.Split('#');refreeze=false;var services=new Services();if(a[0]=="E"){var h=Array.ConvertAll(a[1].Split(','),int.Parse);var test=new TestItem(h[0]){Mutate=h[1]};item=test;var container=new OriginalExploderContainer();test.ManualSetExploderContainer(container);Trace.Clear();foreach(var op in a[2].Split(';')){var v=Array.ConvertAll(op.Split(','),int.Parse);if(v[0]!=0)test.ExplodeAll(Data(v[1],25));else test.Explode(Data(v[1],2));}var hits=new List<string>();for(int i=-2;i<5;i++)if(container.ContainsExploder(i))hits.Add(i.ToString());Console.WriteLine(string.Join("|",Trace)+"#"+test.LayerCount+"#"+string.Join(",",hits));continue;}
if(a[0]=="F"){var h=Array.ConvertAll(a[1].Split(','),long.Parse);var test=new TestItem(1);item=test;Setup(h[0]!=0,h[2]!=0,h[1]!=0,h[3]!=0,services);string result="1";try{test.Finish(Float((uint)h[4]));}catch(NullReferenceException){result="E";}Console.WriteLine(result+"#"+string.Join("|",Trace)+"#"+State());continue;}
if(a[0]=="T"){item=new TestItem(1);Setup(true,true,true,false,services);refreeze=a[1]=="1";var outputs=new List<string>();foreach(var op in a[2].Split(';')){var v=op.Split(',');float value=Float(uint.Parse(v[1]));if(v[0]=="F")Cells[0].FreezeFallForDuration(value);else timer.Update(value);outputs.Add(Cells[0].FallFreezeCount+","+Bits(timer.Remaining)+","+(timer.IsCounting?1:0));}Console.WriteLine(string.Join("|",outputs));continue;}
var m=Array.ConvertAll(a[1].Split(','),int.Parse);var match=new OriginalMatchBoardItem(1,1,m[0]);item=match;Setup(m[8]!=0,true,true,true,services);services.Goals=m[1];services.StaticMask=m[2];services.ItemMask=m[3];services.ManagerMask=m[4];match.BindExplosionServices(services);if(m[5]!=0){var special=default(OriginalSpecialItemCreationData);special.OrderedCells.Add(Cells[2]);special.OrderedCells.Add(Cells[0]);special.OrderedCells.Add(Cells[1]);match.SpecialItemCreationData=special;}if(m[6]!=0)match.SelectByColorMixer();var data=Data(11,m[7]);string first="1";try{match.Explode(data);}catch(NullReferenceException){first="E";}match.UnselectByColorMixer();match.Explode(data);if(services.Pending!=null)services.Pending();timer.Update(.2f);Console.WriteLine(first+"#"+string.Join("|",Trace)+"#"+State()+"#"+services.Shifter);
}}}'''
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder);source=folder/'Verify.cs';source.write_text(harness);exe=folder/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
    names=('OriginalItemExplosion','OriginalMatchItemExplosion','OriginalCellFreezeCountdown','OriginalMatchExplosionData','OriginalMatchExplosionReadiness','OriginalExplodeData',
      'OriginalFallComponentState','OriginalItemSwapAvailability','OriginalFillingItemPlacement','OriginalFillingCell','OriginalFallRefillColumns','OriginalCellTouchRules',
      'OriginalCellMediation','OriginalGridIterator','OriginalMatchSearch','OriginalMatchCandidates','OriginalMatchSnapshot','OriginalCellFallState','OriginalGameOperations')
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),*[str(runtime/(n+'.cs')) for n in names]],capture_output=True,text=True,encoding='utf-8')
    assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8')
    assert result.returncode==0,result.stdout+result.stderr
    actual=result.stdout.replace('\ufeff','').splitlines()
    assert len(actual)==len(expected),(len(actual),len(expected))
    def normalized_timer(value):
        # ARM64 and x86 generate different NaN sign bits for infinity-minus-
        # infinity. Compare the NaN state, preserving exact finite/infinity bits.
        rows=[]
        for row in value.split('|'):
            count,remainder,active=row.split(',');raw=int(remainder)
            if raw&0x7f800000==0x7f800000 and raw&0x007fffff:remainder='NaN'
            rows.append(','.join((count,remainder,active)))
        return '|'.join(rows)
    for i,(value,wanted) in enumerate(zip(actual,expected)):
        compared=normalized_timer(value) if cases[i].startswith('T#') else value
        reference=normalized_timer(wanted) if cases[i].startswith('T#') else wanted
        assert compared==reference,(i,cases[i],value,wanted)
report={'passed':True,'native_hit_dedup_traces':60,'native_final_release_cases':48,'native_freeze_timer_traces':64,'native_freeze_timer_steps':1536,'native_match_explosion_cases':152,
 'original_rvas':['0x2b468dc','0x2b46978','0x2b46aa0','0x2b46c28','0x21a5d90','0x21a6b74','0x21a7d18','0x3190664','0x344737c','0x3446be4','0x2eec020','0x2edbfd0'],
 'scope':'Actual native base item Explode/ExplodeAll and full MatchItem.TryExplode, final model release, goal-call/event order and native CellView countdown vs production C#. Covers repeated IDs, layer changes, color-mixer selection, goal/collect-manager priorities, special merge callback, hammer/purple branches, 0.15/0.2 freeze durations, three mediation slots clearing and nonfinite/reentrant countdowns. Finite/infinity countdown values compare exact float bits; arithmetic NaNs compare by class because ARM64/x86 NaN sign differs. Native inline special data/index, mediation clearing, goal dispatch, shifter counters and countdown arithmetic execute. HashSet storage, cell delegate dispatch and freeze operation counters supplied; goal/level/helper/session/item-factory/Unity view and coroutine services are traced boundaries. Concrete animations, manager collection, madness internals, obstacle damage and fall/refill/playable loop remain pending.'}
(p.PROJECT/'item_explosion_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
