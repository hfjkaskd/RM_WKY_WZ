"""Compare actual native post-swap match resolution and explosion data creation."""
import sys
sys.dont_write_bytecode=True
import verify_board_layout as b
import json,random,struct,subprocess,tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_X3,UC_ARM64_REG_X8,UC_ARM64_REG_LR,UC_ARM64_REG_PC,UC_ARM64_REG_SP
p=b.p
action=0x10000000;state=0x10001000;container=0x10001200;counts=0x10001400
cells=[0x10002000+i*0x200 for i in range(8)]
items=[0x10005000+i*0x200 for i in range(4)]
match_klass=0x10006000;base_klass=0x10007000;hierarchy=0x10008000
groups_list=0x10009000;groups_array=0x1000a000
group_statics=0x1000b000;point_statics=0x1000c000;exploder_statics=0x1000d000;vector_statics=0x1000e000
data_out=0x1000f000;failure_pointer=0x10010000;failure_klass=0x10011000;failure_statics=0x10012000;empty_array=0x10013000
trace=[];group_calls=0;success_mask=3

def bind(slot,statics=None):
    if slot not in b.resolved_slots:
        p.emu.reg_write(UC_ARM64_REG_X0,slot)
        b.boundary(p.emu,0xffd960,4,None)
    klass=p.read(slot,8)
    if statics is not None:p.put(klass+0xb8,statics,8)
    return klass

def cell_id(pointer):return -1 if not pointer else cells.index(pointer)
def group_string(owner):
    result=[p.read(owner+0x80,1),p.signed(p.read(owner)),p.signed(p.read(owner+4)),p.read(owner+0x92,1),cell_id(p.read(owner+0x88,8)),p.read(owner+0x90,1),p.read(owner+0x91,1),p.read(owner+8)]
    result += [cell_id(p.read(owner+0x10+i*8,8)) for i in range(result[-1])]
    return ','.join(map(str,result))
def data_string(owner):
    offsets=(0,4,8,12,16)
    result=[p.signed(p.read(owner+o)) for o in offsets]
    result += [p.read(owner+0x14,1),p.read(owner+0x15,1)]
    result += [p.signed(p.read(owner+o)) for o in (0x18,0x1c,0x20,0x24,0x28)]
    result += [p.read(owner+o) for o in (0x2c,0x30,0x34)]
    return ','.join(map(str,result))
def frozen():return ','.join(str(p.signed(p.read(c+0x28))) for c in cells)+','+str(p.read(counts+0x24))

def boundary(uc,address,size,data):
    global group_calls
    x0,x1,x2,x3=[uc.reg_read(r) for r in (UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_X3)]
    value=None
    if address==0xffda88:raise AssertionError('Native null at '+hex(uc.reg_read(UC_ARM64_REG_LR)))
    if address in (0xd018a0,0xd00b50):uc.mem_write(x0,bytes(uc.mem_read(x1,x2)));value=x0
    elif address==0xd01fa0:uc.mem_write(x0,bytes([x1&255])*x2);value=x0
    elif address==0x20000100:value=p.read(x0+0x20)
    elif address==0x12e1448:
        assert x1==(7|(9<<32)) and x2==3
        trace.append('search');value=groups_list
    elif address==0x284dfd0:
        destination=uc.reg_read(UC_ARM64_REG_X8)
        uc.mem_write(destination,bytes(uc.mem_read(groups_array+x1*0x100,0x98)));value=0
    elif address==0xffda6c:
        value=p.allocate();p.put(value+0x10,p.read(x1))
    elif address==0x2a2e598:
        assert x1==11
        if p.read(x3+0x18)==2:
            old=p.signed(p.read(p.read(x3+0x20,8)+0x10));new=p.signed(p.read(p.read(x3+0x28,8)+0x10))
            trace.append(f'pattern:{old}:{new}:'+frozen())
        else:
            assert p.read(x3+0x18)==0
            trace.append('failed:'+frozen())
        value=0
    elif address==0x12e33dc:
        assert x3==0
        trace.append('explode:'+group_string(x1)+'/'+data_string(x2)+'/'+frozen()+'/'+group_string(action+0x78)+'/'+group_string(action+0x110))
        value=int(bool(success_mask&(1<<group_calls)));group_calls+=1
        # Generic Array.Empty<object> method metadata used by the native failure
        # diagnostic, supplied with initialized class/static storage.
        info=p.read(0x55c1248,8);p.put(info+0x38,failure_pointer,8);p.put(failure_pointer,failure_klass,8)
        p.put(failure_klass+0x132,1,1);p.put(failure_klass+0xe0,1);p.put(failure_klass+0xb8,failure_statics,8);p.put(failure_statics,empty_array,8)
    elif address==0x3205d7c:value=77
    if value is not None:
        uc.reg_write(UC_ARM64_REG_X0,value);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))

p.emu.hook_add(UC_HOOK_CODE,boundary)
bind(0x5590270,group_statics);native_match_klass=bind(0x55902a0)
bind(0x558abf0,point_statics);bind(0x558cb38,exploder_statics);bind(0x5595e08,vector_statics)
bind(0x558aba0)

def call_data(rva,args):
    for r,v in zip((UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_X3),[*args,0,0,0,0]):p.emu.reg_write(r,v&0xffffffffffffffff)
    p.emu.reg_write(UC_ARM64_REG_X8,data_out);p.emu.reg_write(UC_ARM64_REG_SP,0x101ff000);p.emu.reg_write(UC_ARM64_REG_LR,0x20000000)
    p.emu.emu_start(rva,0x20000000,count=1000000)
    assert p.emu.reg_read(UC_ARM64_REG_PC)==0x20000000

def write_group(owner,g):
    id,pattern,color,exists,creation,calculated,can,count,*indices=g
    assert len(indices)==count
    p.emu.mem_write(owner,bytes(0x98));p.put(owner,pattern);p.put(owner+4,color);p.put(owner+8,count)
    p.put(owner+0x80,id,1);p.put(owner+0x88,0 if creation<0 else cells[creation],8)
    for offset,v in ((0x90,calculated),(0x91,can),(0x92,exists)):p.put(owner+offset,v,1)
    for i,index in enumerate(indices):p.put(owner+0x10+i*8,cells[index],8)

rng=random.Random(0x1ee0a98);cases=[];expected=[]
for n in range(160):
    p.emu.mem_write(action,bytes(0x20000));p.heap=0x10030000
    p.put(point_statics,-1,8)
    p.put(action+0x28,0x10018000,8);p.put(action+0x68,cells[0],8);p.put(action+0x70,cells[1],8)
    p.put(state+0x10,container,8);p.put(state+0x28,-1);p.put(container+0x10,counts,8);p.put(container+0x18,-1);p.put(counts+0x18,28)
    p.put(native_match_klass+0x12c,1,1);p.put(match_klass+0x12c,1,1);p.put(match_klass+0xc8,hierarchy,8);p.put(hierarchy,native_match_klass,8)
    p.put(base_klass+0x378,0x20000100,8);p.put(match_klass+0x378,0x20000100,8)
    current_from,current_to=n%3,(n//3)%3 # absent, normal match, non-match.
    from_id,to_id=3,(3 if n%4==0 else 9)
    other_type,moving_type=(-1,1,5)[n%3],(-1,1,5)[(n//3)%3]
    for i,c in enumerate(cells):
        p.put(c+0x18,2+i if i else 5);p.put(c+0x1c,4 if i!=1 else 9)
        if i==1:p.put(c+0x18,7)
        p.put(c+0x70,state,8);med=c+0x100;slot=c+0x180;p.put(c+0x10,med,8);p.put(med+0x10,slot,8)
        if i<2:
            kind=(current_from,current_to)[i]
            p.put(slot+0x18,items[i] if kind else 0,8);p.put(items[i],match_klass if kind==1 else base_klass,8)
            p.put(items[i]+0x138,(from_id,to_id)[i],1)
        p.put(c+0x28,n%2)
    for i,t in enumerate((other_type,moving_type),2):p.put(items[i],base_klass,8);p.put(items[i]+0x20,t)
    def group(id,exists,creation):
        indices=[rng.randrange(8) for _ in range(rng.randrange(1,7))]
        return [id,rng.randrange(1,8),rng.randrange(1,7),exists,creation,n%2,(n//2)%2,len(indices),*indices]
    old_from=group(15,int(n%4!=0),0);old_to=group(16,int(n%5!=0),1)
    # Explicit single-three cases exercise the lightball counterpart exception.
    if n%3==2:old_from[1]=1;old_to[1]=1
    write_group(action+0x78,old_from);write_group(action+0x110,old_to)
    for g in (old_from,old_to):
        if g[3]:
            for i in g[8:]:p.put(cells[i]+0x28,p.read(cells[i]+0x28)+1)
    p.put(counts+0x24,sum(p.read(c+0x28) for c in cells))
    fresh=[]
    if n%7:fresh.append(group(from_id,1,4))
    if n%6:fresh.append(group(to_id,1,5))
    if n%5==0:fresh.append(group(to_id,1,6)) # Last matching group wins.
    if n%2==0 and fresh:fresh[0][1]=old_from[1] # Same pattern, different ID/cells.
    p.put(groups_list+0x18,len(fresh))
    for i,g in enumerate(fresh):write_group(groups_array+i*0x100,g)
    trace=[];group_calls=0;success_mask=n%4
    b.call(0x2eebbc0,0)
    # Full native method receives independent old swapped-item arguments.
    for r,v in ((UC_ARM64_REG_X0,action),(UC_ARM64_REG_X1,0 if other_type<0 else items[2]),(UC_ARM64_REG_X2,0 if moving_type<0 else items[3]),(UC_ARM64_REG_LR,0x20000000),(UC_ARM64_REG_SP,0x101ff000)):p.emu.reg_write(r,v)
    try:p.emu.emu_start(0x1ee0a98,0x20000000,count=1000000)
    except Exception:print('Native resolution PC',hex(p.emu.reg_read(UC_ARM64_REG_PC)));raise
    assert p.emu.reg_read(UC_ARM64_REG_PC)==0x20000000
    header=[current_from,current_to,from_id,to_id,other_type,moving_type,success_mask,n%2]
    cases.append('G#'+','.join(map(str,header))+'#'+'#'.join(','.join(map(str,g)) for g in (old_from,old_to,*fresh)))
    expected.append('|'.join(trace)+'#'+frozen()+'/'+group_string(action+0x78)+'/'+group_string(action+0x110)+'/'+str(p.read(exploder_statics)))

# All five Next overloads share the same global ID counter. Check every field
# of the actual native value result, plus booster and explode-all predicates.
data_cases=0
for n in range(180):
    if n%13==0:b.call(0x2eebbc0,0);cases.append('R');expected.append('R')
    kind=n%5;trigger=([-2147483648,2147483647,-1,0,2,17,18,19,20,25,40]+list(range(41)))[n%52]
    x,y=rng.randrange(-20,20),rng.randrange(-20,20);match=n%7;spread=n%2
    point=(x&0xffffffff)|((y&0xffffffff)<<32)
    rvas=(0x2eebc08,0x2eebcec,0x2eebda4,0x2eebe94,0x2eebf70)
    args=((trigger,),(trigger,point),(trigger,match),(trigger,spread),(trigger,spread,point))[kind]
    call_data(rvas[kind],args)
    expected.append(data_string(data_out)+','+str(b.call(0x2eeb93c,data_out)))
    cases.append('D#'+','.join(map(str,(kind,trigger,x,y,match,spread))));data_cases+=1

harness=r'''using System;using System.Collections.Generic;using Whitebox.Foundation;
namespace UnityEngine{public static class Debug{public static void LogError(object x){throw new Exception(x.ToString());}}}
class Context:IOriginalSwapMatchContext{public List<IOriginalSwapMatchGroup> Groups=new List<IOriginalSwapMatchGroup>();public IReadOnlyList<IOriginalSwapMatchGroup> FindExtensiveMatchesInArea(OriginalGridCell c,int r){if(Trace!=null){if(c.X!=7||c.Y!=9||r!=3)throw new Exception("Area");Trace.Add("search");}return Groups;}public List<string> Trace;public bool HasSpecialItem(OriginalGridCell c){return false;}public bool CanComboWith(OriginalBoardItem a,OriginalBoardItem b){throw new Exception();}}
class Prepare:IOriginalSwapAnimationBackend{public void PlayValidSwap(OriginalSwapActionState a){throw new Exception();}public void PlayWrongSwap(OriginalSwapActionState a){}}
class Services:IOriginalMatchExplosionServices{
public OriginalGridCell[] Cells;public OriginalGameOperations Operations;public OriginalSwapActionState Action;public List<string> Trace=new List<string>();public int Mask,Calls;
public string Frozen(){var a=new string[9];for(int i=0;i<8;i++)a[i]=Cells[i].FallFreezeCount.ToString();a[8]=Operations.Container.GetCount(1).ToString();return string.Join(",",a);}
public string Group(OriginalMatchGroupSnapshot s){var a=new List<int>{s.Id,s.PatternType,s.MatchType,s.Exists?1:0,s.SpecialItemCreationCell==null?-1:Array.IndexOf(Cells,s.SpecialItemCreationCell),s.ExplosionCalculated?1:0,s.CanExplode?1:0,s.Cells.Count};for(int i=0;i<s.Cells.Count;i++)a.Add(Array.IndexOf(Cells,s.Cells.Get(i)));return string.Join(",",a);}
public bool ExplodeMatchGroup(OriginalMatchGroupSnapshot group,OriginalExplodeData data,bool propeller){if(propeller)throw new Exception();Trace.Add("explode:"+Group(group)+"/"+Verify.Data(data)+"/"+Frozen()+"/"+Group(Action.Decision.SavedFromMatch)+"/"+Group(Action.Decision.SavedToMatch));return(Mask&(1<<Calls++))!=0;}
public void ReportPatternChanged(int a,int b){Trace.Add("pattern:"+a+":"+b+":"+Frozen());}public void ReportMatchExplosionFailed(){Trace.Add("failed:"+Frozen());}}
class Verify{
static uint Bits(float v){return BitConverter.ToUInt32(BitConverter.GetBytes(v),0);}public static string Data(OriginalExplodeData d){return string.Join(",",new object[]{d.Point.X,d.Point.Y,d.Trigger,d.Id,d.MatchType,d.CanSpreadJelly?1:0,d.IsHorizontalRocket?1:0,d.ComboId,d.LastTargetItemIdentifier.ItemType,d.LastTargetItemIdentifier.StaticItemType,d.LastTargetItemIdentifier.ItemGroupId,d.ExploderIndex,Bits(d.IncomingX),Bits(d.IncomingY),Bits(d.IncomingZ)});}
static OriginalMatchGroupSnapshot Parse(string text,OriginalGridCell[] cells){var a=Array.ConvertAll(text.Split(','),int.Parse);var s=new OriginalMatchGroupSnapshot((byte)a[0],a[1],a[2]);s.Exists=a[3]!=0;s.SpecialItemCreationCell=a[4]<0?null:cells[a[4]];s.ExplosionCalculated=a[5]!=0;s.CanExplode=a[6]!=0;for(int i=0;i<a[7];i++)s.Cells.Add(cells[a[8+i]]);return s;}
static void Main(){string line;while((line=Console.ReadLine())!=null){var parts=line.Split('#');if(parts[0]=="R"){OriginalExploder.Reset();Console.WriteLine("R");continue;}if(parts[0]=="D"){var a=Array.ConvertAll(parts[1].Split(','),int.Parse);OriginalExplodeData d;switch(a[0]){case 0:d=OriginalExploder.Next(a[1]);break;case 1:d=OriginalExploder.Next(a[1],new OriginalCellPoint(a[2],a[3]));break;case 2:d=OriginalExploder.NextWithMatchType(a[1],a[4]);break;case 3:d=OriginalExploder.Next(a[1],a[5]!=0);break;default:d=OriginalExploder.Next(a[1],a[5]!=0,new OriginalCellPoint(a[2],a[3]));break;}Console.WriteLine(Data(d)+","+(d.ShouldExplodeAll?1:0));continue;}
var h=Array.ConvertAll(parts[1].Split(','),int.Parse);var ops=new OriginalGameOperations(()=>77);var cells=new OriginalGridCell[8];for(int i=0;i<8;i++){cells[i]=new OriginalGridCell(i==0?5:i==1?7:2+i,i==1?9:4,1);cells[i].BindGameOperations(ops);for(int j=0;j<h[7];j++)cells[i].FreezeFall();}var context=new Context();var decision=new OriginalSwapDecision(context);var action=new OriginalSwapActionState(decision);cells[0].Mediation.SetAllItems(new OriginalMatchBoardItem(0,1,1));cells[1].Mediation.SetAllItems(new OriginalMatchBoardItem(1,1,2));action.StartSwapAnimation(cells[0],cells[1],new Prepare());for(int i=0;i<2;i++){cells[i].Mediation.ClearAllItems();int kind=h[i];if(kind==1){var item=new OriginalMatchBoardItem(i,1,1);item.NativeMatchGroupId=(byte)h[i+2];cells[i].Mediation.SetAllItems(item);}else if(kind==2)cells[i].Mediation.SetAllItems(new OriginalBoardItem(i,1));}
var from=Parse(parts[2],cells);var to=Parse(parts[3],cells);decision.ReplaceFromMatch(from);decision.ReplaceToMatch(to);foreach(var g in new[]{from,to})if(g.Exists)for(int i=0;i<g.Cells.Count;i++)g.Cells.Get(i).FreezeFall();for(int i=4;i<parts.Length;i++){var saved=new OriginalSavedSwapGroup();saved.CopyFrom(Parse(parts[i],cells),null);context.Groups.Add(saved);}var services=new Services{Cells=cells,Operations=ops,Action=action,Mask=h[6]};context.Trace=services.Trace;OriginalExploder.Reset();var resolver=new OriginalSwapMatchResolution(action,services);resolver.Resolve(h[4]<0?null:new OriginalBoardItem(2,h[4]),h[5]<0?null:new OriginalBoardItem(3,h[5]));int nextId=OriginalExploder.Next(0).Id;Console.WriteLine(string.Join("|",services.Trace)+"#"+services.Frozen()+"/"+services.Group(decision.SavedFromMatch)+"/"+services.Group(decision.SavedToMatch)+"/"+(nextId-1));
}}}'''
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder);source=folder/'Verify.cs';source.write_text(harness);exe=folder/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
    names=('OriginalExplodeData','OriginalSwapMatchResolution','OriginalSwapCompletion','OriginalSwapActionState','OriginalSwapDecision','OriginalItemSwapAvailability',
      'OriginalFillingItemPlacement','OriginalFillingCell','OriginalFallRefillColumns','OriginalCellTouchRules','OriginalCellMediation','OriginalGridIterator',
      'OriginalMatchSearch','OriginalMatchCandidates','OriginalMatchSnapshot','OriginalGameOperations','OriginalCellFallState')
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),*[str(runtime/(n+'.cs')) for n in names]],capture_output=True,text=True,encoding='utf-8')
    assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8')
    assert result.returncode==0,result.stdout+result.stderr
    actual=result.stdout.replace('\ufeff','').splitlines()
    assert actual==expected,next(((i,cases[i],a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e),'count mismatch')
report={'passed':True,'native_swap_resolution_cases':160,'native_explode_data_cases':data_cases,
 'original_rvas':['0x1ee0a98','0x1ee4238','0x12dfa10','0x2eebbc0','0x2eebc08','0x2eebcec','0x2eebda4','0x2eebe94','0x2eebf70','0x2eeb8ac','0x2eeb93c','0x318d000'],
 'scope':'Native re-query/selection, old group unfreeze order across from/to groups, pattern-only replacement, saved/creation-cell values during dispatch, diagnostics, lightball skip and sequential snapshot clearing vs compiled production resolver. Actual native Exploder counter, all five Next overloads, constructor/default identifier and value fields compared. Search result storage, item type getter, native object/boxing/generic metadata, diagnostics and terminal MatchManager explosion supplied at boundaries. Concrete item explosion, visuals, refill and playable loop remain pending.'}
(p.PROJECT/'swap_match_resolution_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
