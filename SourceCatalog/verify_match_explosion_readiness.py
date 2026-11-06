"""Native match/item explode readiness, cached group checks and falling waits."""
import sys
sys.dont_write_bytecode=True
import verify_board_layout as b
import itertools,json,random,struct,subprocess,tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE,UC_HOOK_MEM_READ
from unicorn.arm64_const import UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_X8,UC_ARM64_REG_LR,UC_ARM64_REG_PC,UC_ARM64_REG_SP,UC_ARM64_REG_S0
p=b.p
group=0x10000000;group_statics=0x10001000;dictionary=0x10002000
match_class=0x10003000;base_class=0x10004000;hierarchy=0x10005000
fall_class=0x10006000;interface_offsets=0x10007000
item_base=0x10010000;cell_base=0x10020000;fall_base=0x10030000;view_base=0x10040000
trace=[];native_error=False;time_bits=0;nodes=[];entries={};version=0
bits=lambda f:struct.unpack('<I',struct.pack('<f',f))[0]
def item(i):return item_base+i*0x400
def cell(i):return cell_base+i*0x800
def fall(i):return fall_base+i*0x100
def view(i):return view_base+i*0x100
def index(pointer,base,stride):return (pointer-base)//stride
def bind(slot,statics=None):
    if slot not in b.resolved_slots:
        p.emu.reg_write(UC_ARM64_REG_X0,slot);b.boundary(p.emu,0xffd960,4,None)
    klass=p.read(slot,8)
    if statics is not None:p.put(klass+0xb8,statics,8)
    return klass

def boundary(uc,address,size,data):
    global native_error,entries,version
    x0,x1,x2=[uc.reg_read(r) for r in (UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2)]
    result=None
    if address==0xffda88:
        native_error=True;uc.reg_write(UC_ARM64_REG_PC,0x20000000);return
    if address==0x2eea560:trace.append('steady'+str(index(x0,fall_base,0x100))) # Actual native fall state getter still executes.
    elif address==0x2ee8840:trace.append('changed'+str(index(x0,fall_base,0x100)))
    elif address==0x3205e3c:
        trace.append('time');uc.reg_write(UC_ARM64_REG_S0,time_bits);result=0
    elif address==0x2b45968:
        i=index(x0,item_base+0x300,0x400);result=cell(i) if nodes[i][10] else 0
    elif address==0x3cceac4:entries={};version+=1;result=0
    elif address==0x3cd04f8:
        value=entries.get(p.signed(x1&0xffffffff),0);p.put(x2,value,8);result=int(value!=0)
    elif address==0x3cce918:entries[p.signed(x1&0xffffffff)]=x2;version+=1;result=0
    elif address==0x3cced84:
        target=uc.reg_read(UC_ARM64_REG_X8);p.put(target+8,version);p.put(target+12,0);result=0
    elif address==0x3173e70:
        assert p.read(x0+8)==version,'Native dictionary enumeration invalidated'
        i=p.read(x0+12);values=list(entries.items());result=int(i<len(values))
        if result:p.put(x0+16,values[i][0]);p.put(x0+24,values[i][1],8);p.put(x0+12,i+1)
    elif address==0x3173f8c:result=0
    if result is not None:
        uc.reg_write(UC_ARM64_REG_X0,result);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
def memory(uc,access,address,size,value,data):
    if address>=view_base and address<view_base+0x2000 and (address-view_base)%0x100==0x19:
        trace.append('view'+str(index(address,view_base,0x100)))
p.emu.hook_add(UC_HOOK_CODE,boundary);p.emu.hook_add(UC_HOOK_MEM_READ,memory)
bind(0x5590270,group_statics);native_match_class=bind(0x55902a0);fall_interface=bind(0x558df78);bind(0x558aba0)

# Node: kind (0 absent/1 match/2 other), color, state, view presence/swap,
# reserve counter, group exists/cached/result, state time bits, current present,
# fall present, X, Y. Source and target slots are configured independently.
def node(kind=1,color=1,state=0,has_view=1,swapping=0,reserve=0,exists=0,calculated=0,can=0,changed=0,current=1,has_fall=1,x=0,y=0):
    return [kind,color,state,has_view,swapping,reserve,exists,calculated,can,changed,current,has_fall,x,y]
def setup(values):
    global nodes,trace,native_error,entries,version
    nodes=values;trace=[];native_error=False;entries={};version=0
    p.emu.mem_write(group,bytes(0x60000));p.put(group_statics,dictionary,8)
    p.put(native_match_class+0x12c,1,1);p.put(match_class+0x12c,1,1);p.put(match_class+0xc8,hierarchy,8);p.put(hierarchy,native_match_class,8)
    p.put(fall_class+0x12a,1,2);p.put(fall_class+0xb0,interface_offsets,8);p.put(interface_offsets,fall_interface,8);p.put(interface_offsets+8,0)
    p.put(fall_class+0x138,0x2eea560,8);p.put(fall_class+0x168,0x2ee8840,8)
    for i,n in enumerate(values):
        kind,color,state,hv,swap,reserve,exists,calculated,can,changed,current,hf,x,y=n
        p.put(item(i),match_class if kind==1 else base_class,8);p.put(item(i)+0x18,color);p.put(item(i)+0x38,item(i)+0x300,8)
        p.put(item(i)+0x40,fall(i) if hf else 0,8);p.put(item(i)+0xb0,view(i) if hv else 0,8);p.put(item(i)+0x260,reserve)
        p.put(item(i)+0x148,calculated,1);p.put(item(i)+0x149,can,1);p.put(item(i)+0x14a,exists,1)
        p.put(fall(i),fall_class,8);p.put(fall(i)+0x28,state);p.put(fall(i)+0x60,changed);p.put(view(i)+0x19,swap,1)
        med=cell(i)+0x100;slot=cell(i)+0x200;neighbors=cell(i)+0x300;array=cell(i)+0x400
        p.put(cell(i)+0x18,x);p.put(cell(i)+0x1c,y);p.put(cell(i)+0x10,med,8);p.put(cell(i)+0x20,neighbors,8)
        p.put(med+0x10,slot,8);p.put(med+0x20,slot+0x40,8)
        p.put(slot+0x18,item(i) if kind and current else 0,8);p.put(slot+0x40+0x18,item(i) if kind else 0,8)
        p.put(neighbors+0x18,array,8);p.put(array+0x18,8)
def above(i,j):p.put(cell(i)+0x400+0x20+8,cell(j),8)
def outcome(rva,owner):
    global native_error
    native_error=False
    result=b.call(rva,owner)
    return 'E' if native_error else str(result&1)
def serialized(values):return ';'.join(','.join(map(str,n)) for n in values)

cases=[];expected=[]
for hv,swap,hf,state,reserve in itertools.product(range(2),range(2),range(2),range(2),(-1,0,1)):
    ns=[node(has_view=hv,swapping=swap,has_fall=hf,state=state,reserve=reserve)]
    setup(ns);result=outcome(0x21a5aa4,item(0))
    cases.append('R#0#'+serialized(ns));expected.append(result+'|'+','.join(trace))

rng=random.Random(0x21a5b70)
for n in range(144):
    changed=bits(rng.choice((0,.1,1,1000000)))
    base=struct.unpack('<f',struct.pack('<I',changed))[0]
    deadline=bits(base+.25)
    time_bits=deadline+(-1,0,1)[n%3]
    if n%17==0:time_bits=0x7fc00000 # Native LE condition accepts unordered operands.
    ns=[node(changed=changed,y=0)]
    for i in range(1,1+n%4):
        ns.append(node(kind=(0,1,2,1,1)[(n+i)%5],color=1 if n%5 else 2,state=0 if i<len(range(1,1+n%4)) else n%2,
            swapping=n%2,reserve=n%3-1,exists=int(n%7==0),calculated=int(n%14==0),can=n%2,
            changed=bits(.125*i),current=int(n%19!=0),has_fall=int(n%23!=0),y=i))
    setup(ns)
    for i in range(len(ns)-1):above(i,i+1)
    result=outcome(0x21a5b70,item(0))
    cases.append('W#'+str(time_bits)+'#'+serialized(ns));expected.append(result+'|'+','.join(trace))

for n in range(128):
    count=n%7;ns=[]
    for i in range(count):
        ns.append(node(kind=1 if n%4==0 else rng.choice((0,1,1,1,2)),state=0 if n%4==0 else rng.randrange(2),
            has_view=int(n%11!=0),swapping=int(n%13==0),reserve=-1 if n%4==0 else rng.randrange(2),x=i%3,y=(i*3+n)%7))
    for i in range(count):
        ns.append(node(color=1 if n%3 else 2,state=1,changed=0,x=i%3,y=10+i))
    setup(ns);time_bits=bits((0,.25,.25000003,1)[n%4])
    for i in range(count):above(i,count+i)
    p.put(group+8,count);p.put(group+0x90,int(n%5==0),1);p.put(group+0x91,n%2,1)
    for i in range(count):p.put(group+0x10+i*8,cell(i),8)
    result=outcome(0x12df568,group)
    first=result+':'+str(p.read(group+0x90,1))+':'+str(p.read(group+0x91,1))+'|'+','.join(trace)
    if not native_error:
        trace=[];time_bits=bits(999)
        result=outcome(0x12df568,group)
        first+='~'+result+':'+str(p.read(group+0x90,1))+':'+str(p.read(group+0x91,1))+'|'+','.join(trace)
    cases.append('G#'+','.join(map(str,(count,int(n%5==0),n%2,bits((0,.25,.25000003,1)[n%4]))))+'#'+serialized(ns))
    expected.append(first)

harness=r'''using System;using System.Collections.Generic;using Whitebox.Foundation;
namespace UnityEngine{public static class Debug{public static void LogError(object x){throw new Exception(x.ToString());}}}
class View:IOriginalItemSwapView{public int Id;public bool Value;public bool IsSwapping{get{Verify.Trace.Add("view"+Id);return Value;}}}
class Fall:IOriginalItemFallTiming,IOriginalFillingFallComponent{public int Id,State;float changed;public void SetFillingCell(OriginalFillingCell cell){}public bool IsItemSteady{get{Verify.Trace.Add("steady"+Id);return State==0;}}public float StateChangedTime{get{Verify.Trace.Add("changed"+Id);return changed;}set{changed=value;}}}
class Verify{
public static List<string> Trace=new List<string>();static float Float(uint b){return BitConverter.ToSingle(BitConverter.GetBytes(b),0);}static string Call(Func<bool> action){try{return action()?"1":"0";}catch(NullReferenceException){return "E";}}
static void Main(){string line;while((line=Console.ReadLine())!=null){var a=line.Split('#');var descriptions=a[2].Length==0?new string[0]:a[2].Split(';');var cells=new OriginalGridCell[descriptions.Length];var items=new OriginalBoardItem[descriptions.Length];var values=new long[descriptions.Length][];for(int i=0;i<descriptions.Length;i++){values[i]=Array.ConvertAll(descriptions[i].Split(','),long.Parse);var n=values[i];cells[i]=new OriginalGridCell((int)n[12],(int)n[13],1);if(n[0]==0)continue;items[i]=n[0]==1?(OriginalBoardItem)new OriginalMatchBoardItem(i,1,(int)n[1]):new OriginalBoardItem(i,1);if(n[3]!=0)items[i].BindSwapView(new View{Id=i,Value=n[4]!=0});if(n[11]!=0)items[i].FallComponent=new Fall{Id=i,State=(int)n[2],StateChangedTime=Float((uint)n[9])};if(items[i] is OriginalMatchBoardItem m){for(int j=0;j<n[5];j++)m.Reserve();for(int j=0;j>n[5];j--)m.UnReserve();m.MatchGroup.Exists=n[6]!=0;m.MatchGroup.ExplosionCalculated=n[7]!=0;m.MatchGroup.CanExplode=n[8]!=0;}cells[i].Mediation.SetAllItems(items[i]);if(n[10]==0)cells[i].Mediation.ClearCurrentItem();}
float time=0;var readiness=new OriginalMatchExplosionReadiness(()=>{Trace.Add("time");return time;});Trace.Clear();if(a[0]=="R"){var result=Call(()=>OriginalMatchExplosionReadiness.IsReadyForAutoExplode((OriginalMatchBoardItem)items[0]));Console.WriteLine(result+"|"+string.Join(",",Trace));continue;}if(a[0]=="W"){time=Float(uint.Parse(a[1]));for(int i=0;i<cells.Length-1;i++)cells[i].Neighbors.Set(1,cells[i+1]);var result=Call(()=>readiness.ShouldWaitForFallingMatchItems((OriginalMatchBoardItem)items[0]));Console.WriteLine(result+"|"+string.Join(",",Trace));continue;}
var h=Array.ConvertAll(a[1].Split(','),long.Parse);int count=(int)h[0];for(int i=0;i<count;i++)cells[i].Neighbors.Set(1,cells[count+i]);var group=new OriginalMatchGroupSnapshot(1,1,1);for(int i=0;i<count;i++)group.Cells.Add(cells[i]);group.ExplosionCalculated=h[1]!=0;group.CanExplode=h[2]!=0;time=Float((uint)h[3]);string first=Call(()=>readiness.CanAutoExplode(ref group));string output=first+":"+(group.ExplosionCalculated?1:0)+":"+(group.CanExplode?1:0)+"|"+string.Join(",",Trace);if(first!="E"){Trace.Clear();time=999;string second=Call(()=>readiness.CanAutoExplode(ref group));output+="~"+second+":"+(group.ExplosionCalculated?1:0)+":"+(group.CanExplode?1:0)+"|"+string.Join(",",Trace);}Console.WriteLine(output);
}}}'''
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder);source=folder/'Verify.cs';source.write_text(harness);exe=folder/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
    names=('OriginalMatchExplosionReadiness','OriginalFallComponentState','OriginalItemSwapAvailability','OriginalFillingItemPlacement','OriginalFillingCell',
      'OriginalFallRefillColumns','OriginalCellTouchRules','OriginalCellMediation','OriginalGridIterator','OriginalMatchSearch','OriginalMatchCandidates',
      'OriginalMatchSnapshot','OriginalCellFallState','OriginalGameOperations')
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),*[str(runtime/(n+'.cs')) for n in names]],capture_output=True,text=True,encoding='utf-8')
    assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8')
    assert result.returncode==0,result.stdout+result.stderr
    actual=result.stdout.replace('\ufeff','').splitlines()
    assert actual==expected,next(((i,cases[i],a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e),'count mismatch')
report={'passed':True,'native_item_readiness_cases':48,'native_falling_wait_cases':144,'native_group_cache_cases':128,
 'original_rvas':['0x21a5aa4','0x21a5b70','0x12df568','0x12df5e8','0x2eea560','0x2ee8840'],
 'scope':'Actual native item readiness, reserve/view/fall short circuits, upward target walk, 0.25-second boundary, native unordered NaN comparison, top-item-per-column selection and group readiness cache vs production C#. Native fall state and timestamp getters execute. Dictionary/enum storage and metadata, level time and item current-cell delegate supplied. View flag reads, steady/time query order, missing-state errors and repeat-cache behavior compared. Full fall motion, recursive live group scheduling, actual explosion/visual recycle and playable loop remain pending.'}
(p.PROJECT/'match_explosion_readiness_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
