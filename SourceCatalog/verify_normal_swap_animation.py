"""Compare native normal-swap startup and both completion legs end to end."""
import sys
sys.dont_write_bytecode = True
import verify_board_layout as b
import itertools, json, random, struct, subprocess, tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import UC_ARM64_REG_X0, UC_ARM64_REG_X1, UC_ARM64_REG_X2, UC_ARM64_REG_X3, UC_ARM64_REG_PC, UC_ARM64_REG_LR, UC_ARM64_REG_S0, UC_ARM64_REG_S1, UC_ARM64_REG_S2, UC_ARM64_REG_S3
p = b.p
action=0x10000000; state=0x10001000; container=0x10002000; counts=0x10003000
cells=[0x10004000,0x10005000]; cell_mediators=[0x10006000,0x10006200]; slots=[0x10006400,0x10006600]
cell_views=[0x10008000,0x10009000]; neighbors=[0x1000a000,0x1000a100]
items=[0x1000b000,0x1000b100]; mediators=[0x1000c000,0x1000c100]; item_klass=0x1000d000
views=[0x1000e000,0x1000f000]; view_klass=0x10010000; particles=[0x10011000,0x10012000]
events=[0x10013000,0x10013100]; statics=0x10014000; flash=0x10015000; flash_klass=0x10016000
bits=lambda f:struct.unpack('<I',struct.pack('<f',f))[0]
trace=[]; moves=[]; positions={}; present=[True,True]; callback_count=0
callback_slots={0x55f7eb0:0x1ee44b4,0x55f7eb8:0x1ee45cc,0x55f7ec8:0x1ee4734,0x55f7ed0:0x1ee4968,0x55f7ed8:0x1ee48dc,0x55f7ee0:0x1ee4aa8}

def sort_string(pack, everything):
    return f'{p.signed(pack&0xffffffff)},{p.signed((pack>>32)&0xffffffff)},{everything&1}'

def status():
    return f'{p.read(action+0x10,1)},{p.read(cells[0]+0x28)},{p.read(cells[1]+0x28)},{p.read(counts+0x24)},{p.read(counts+0x28)}'

def boundary(uc,address,size,data):
    x0,x1,x2,x3=[uc.reg_read(r) for r in (UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_X3)]
    value=None
    if address==0xffda88: raise AssertionError('Unexpected native null at '+hex(uc.reg_read(UC_ARM64_REG_LR)))
    if address==0xd018a0:
        # Native Clear copies the static empty MatchGroup value. Supply its
        # initialized storage at the external memcpy boundary.
        assert x2==0x98
        uc.mem_write(x0,bytes(x2));value=x0
    elif address==0x23e648c: value=0x10000800
    elif address==0x23e6e44: trace.append('clearKing');value=0
    elif address in (0x1e19780,0x1e2d558,0x1e2fdfc): value=0  # No jelly or special items in this domain.
    elif address==0x20000100:
        i=views.index(x0);p.put(x0+0x19,0,1);trace.append(f'end{i}');value=0
    elif address==0x20000120:
        i=items.index(x0);trace.append(f'view{i}');value=views[i]
    elif address==0x20000140:
        i=views.index(x0);p.put(x0+0x100,x1,8);p.put(x0+0x108,x2)
        trace.append(f'sort{i}:'+sort_string(x1,x2));value=0
    elif address==0x20000160:
        i=views.index(x0);p.put(x0+0x19,1,1);trace.append(f'begin{i}:{x1&1}:{x2}');value=0
    elif address==0x20000180:
        trace.append('start:'+status())
        uc.reg_write(UC_ARM64_REG_X0,state);uc.reg_write(UC_ARM64_REG_X1,2);uc.reg_write(UC_ARM64_REG_PC,0x1ee1c9c);return
    elif address==0x200001a0:
        trace.append(f'finish{x1}:'+status())
        uc.reg_write(UC_ARM64_REG_X0,state);uc.reg_write(UC_ARM64_REG_X1,2);uc.reg_write(UC_ARM64_REG_PC,0x1ee1d14);return
    elif address==0x200001c0:
        point=tuple(uc.reg_read(r) for r in (UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2))
        i=[positions[c] for c in cell_views].index(point)
        trace.append(f'flash{i}:'+status());value=0
    elif address==0x200001e0: value=1 # Match ItemType.
    elif address==0x26912f0: value=flash
    elif address==0x3191dd4:
        for r,v in zip((UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2),positions[cell_views[cells.index(x0)]]):uc.reg_write(r,v)
        value=0
    elif address==0x2b45a6c: value=int(present[mediators.index(x0)])
    elif address==0x21805c0: trace.append(f'sfx{x1}:{uc.reg_read(UC_ARM64_REG_S0)}');value=0
    elif address==0x359e6bc:
        trace.append(f'haptic{x1}:{x2}:{uc.reg_read(UC_ARM64_REG_S0)}:{uc.reg_read(UC_ARM64_REG_S1)}:{x3}');value=0
    elif address==0x1ee3ef0:
        i=views.index(x1);sorting=p.read(x1+0x100,8)
        particle_pack=(sorting&0xffffffff)|((((sorting>>32)+1)&0xffffffff)<<32)
        trace.append(f'particle{i}:{x2}:'+sort_string(particle_pack,0));value=particles[i]
    elif address==0x21a95d8: trace.append(f'psort{particles.index(x0)}:'+sort_string(x1,x2));value=0
    elif address==0x2b470f4:
        i=views.index(x0);value=231352099|(1000<<32);p.put(x0+0x100,value,8)
        trace.append(f'arrange{i}');uc.reg_write(UC_ARM64_REG_X1,0)
    elif address==0x1ee0a98: trace.append(f'groups:{-1 if not x1 else items.index(x1)}:{items.index(x2)}');value=0
    elif address==0x1ee42c0: trace.append(f'special:{-1 if not x1 else items.index(x1)}:{items.index(x2)}');value=0
    elif address==0x3e4737c: value=x0
    elif address==0x3e4e400:
        for r,v in zip((UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2),positions[x0]):uc.reg_write(r,v)
        value=0
    elif address==0x2c97a6c:
        value=p.allocate();point=tuple(uc.reg_read(r) for r in (UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2))
        p.put(value+0x10,x0,8)
        for i,v in enumerate(point):p.put(value+0x20+4*i,v)
        p.put(value+0x2c,uc.reg_read(UC_ARM64_REG_S3))
    elif address==0x2044a78:
        matches=[rva for slot,rva in callback_slots.items() if p.read(slot,8)==x2]
        assert len(matches)==1,(hex(x2),matches)
        p.put(x0+0x18,matches[0],8);p.put(x0+0x40,x1,8);value=0
    elif address==0x2687f9c: p.put(x0+0x18,x1,8);value=x0
    elif address==0x2861a3c:
        target=p.read(x0+0x10,8);point=tuple(p.read(x0+0x20+4*i) for i in range(3));delegate=p.read(x0+0x18,8)
        trace.append(f'move{views.index(target)}:'+','.join(map(str,(*point,p.read(x0+0x2c)))))
        moves.append((target,point,p.read(delegate+0x18,8),p.read(delegate+0x40,8)));value=0
    elif address==0x3e3b754: value=int(x0==x1)
    elif address==0x3205d7c: value=123
    if value is not None:
        uc.reg_write(UC_ARM64_REG_X0,value);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))

p.emu.hook_add(UC_HOOK_CODE,boundary)
b.call(0x12dd2f4,0)
sorting_klass=p.read(p.read(0x5356db8,8),8)
p.put(sorting_klass+0xb8,statics,8)

def setup(valid,other,dx,dy,base0,base1):
    global positions,moves,trace,present
    p.emu.mem_write(action,bytes(0x20000));p.heap=0x10030000
    p.put(statics+8,231352099);p.put(action+0x68,cells[0],8);p.put(action+0x70,cells[1],8)
    for offset in (0x40,0x48,0x60):p.put(action+offset,0x10000800,8)
    p.put(action+0x18,events[0],8);p.put(action+0x20,events[1],8)
    p.put(events[0]+0x18,0x20000180,8);p.put(events[1]+0x18,0x200001a0,8)
    p.put(state+0x10,container,8);p.put(state+0x28,-1 if base0+base1 else 0)
    p.put(container+0x10,counts,8);p.put(container+0x18,-1);p.put(counts+0x18,28);p.put(counts+0x24,base0+base1)
    p.put(item_klass+0x418,0x20000120,8);p.put(item_klass+0x378,0x200001e0,8)
    p.put(view_klass+0x208,0x20000140,8);p.put(view_klass+0x228,0x20000160,8);p.put(view_klass+0x238,0x20000100,8)
    p.put(flash,flash_klass,8);p.put(flash_klass+0x218,0x200001c0,8)
    points=[(2,3),(2+dx,3+dy)]
    positions={cell_views[0]:(bits(2.25),bits(-3.5),bits(.125)),cell_views[1]:(bits(2.25+dx),bits(-3.5+dy),bits(-.25))}
    for i in range(2):
        p.put(cells[i]+0x10,cell_mediators[i],8);p.put(cell_mediators[i]+0x10,slots[i],8)
        occupant=(1-i if valid else i)
        p.put(slots[i]+0x18,items[occupant] if occupant==0 or other else 0,8)
        p.put(cells[i]+0xb8,cell_views[i],8);p.put(cells[i]+0x20,neighbors[i],8);p.put(cells[i]+0x70,state,8)
        p.put(cells[i]+0x28,(base0,base1)[i]);p.put(cells[i]+0x18,points[i][0]);p.put(cells[i]+0x1c,points[i][1])
        p.put(neighbors[i]+0x10,points[i][0]);p.put(neighbors[i]+0x14,points[i][1])
        p.put(items[i],item_klass,8);p.put(items[i]+0x38,mediators[i],8);p.put(views[i],view_klass,8)
        p.put(views[i]+0x100,231352099|(1000<<32),8);positions[views[i]]=positions[cell_views[i]]
    moves=[];trace=[];present=[True,True]

cases=[];expected=[]
for n,(valid,other,dxdy,has0,has1,reverse_order) in enumerate(itertools.product(range(2),range(2),((0,1),(1,0),(0,-1),(-1,0)),range(2),range(2),range(2))):
    dx,dy=dxdy;base0=n%3;base1=(n//3)%3
    setup(valid,other,dx,dy,base0,base1)
    b.call(0x1ee2cd0 if valid else 0x1ee35f4,action)
    present=[bool(has0),bool(has1)]
    outward=list(moves);moves=[]
    if reverse_order:outward.reverse()
    for target,point,rva,owner in outward:positions[target]=point;b.call(rva,owner)
    returns=list(moves);moves=[]
    if reverse_order:returns.reverse()
    for target,point,rva,owner in returns:positions[target]=point;b.call(rva,owner)
    assert not moves
    final=status()+f',{p.read(views[0]+0x19,1)},{p.read(views[1]+0x19,1)}'
    cases.append(','.join(map(str,(valid,other,dx,dy,has0,has1,reverse_order,base0,base1))))
    expected.append('|'.join(trace)+'#'+final)

# Force reuse after the primary leg has made the action idle, while the old
# secondary delegate is still queued. This checks native closure lifetime,
# independently of the separately tested touch/readiness gates.
setup(1,1,1,0,0,0)
b.call(0x1ee2cd0,action)
first=list(moves);moves=[]
primary=next(m for m in first if m[0]==views[0])
late_secondary=next(m for m in first if m[0]==views[1])
positions[primary[0]]=primary[1];b.call(primary[2],primary[3])
b.call(0x1edf774,action)
p.put(action+0x68,cells[0],8);p.put(action+0x70,cells[1],8)
b.call(0x1ee35f4,action)
second=list(moves);moves=[]
trace.append('oldSecondary')
positions[late_secondary[0]]=late_secondary[1];b.call(late_secondary[2],late_secondary[3])
for target,point,rva,owner in second:positions[target]=point;b.call(rva,owner)
returns=list(moves);moves=[]
for target,point,rva,owner in returns:positions[target]=point;b.call(rva,owner)
cases.append('O')
expected.append('|'.join(trace)+'#'+status()+f',{p.read(views[0]+0x19,1)},{p.read(views[1]+0x19,1)}')

# Direction lookup intentionally reports the reverse vector, including native
# unchecked subtraction at Int32 boundaries; exercise all neighbor positions.
rng=random.Random(0x3195550)
direction_cases=[]
for x,y in ((0,0),(3,-4),(2147483647,-2147483648)):
    for dx,dy in itertools.product(range(-2,3),repeat=2):
        wrap=lambda a:p.signed(a&0xffffffff)
        direction_cases.append((x,y,wrap(x+dx),wrap(y+dy)))
for _ in range(64):direction_cases.append(tuple(rng.randint(-2147483648,2147483647) for _ in range(4)))
for x,y,tx,ty in direction_cases:
    p.put(neighbors[0]+0x10,x);p.put(neighbors[0]+0x14,y)
    result=b.call(0x3195550,neighbors[0],(tx&0xffffffff)|((ty&0xffffffff)<<32))
    cases.append('D,'+','.join(map(str,(x,y,tx,ty))));expected.append(str(p.signed(result&0xffffffff)))

harness=r'''using System;using System.Collections.Generic;using Whitebox.Foundation;
namespace UnityEngine{public static class Debug{public static void LogError(object x){throw new Exception(x.ToString());}}}
class View:IOriginalSwapAnimationView{public int Id;public Services S;public bool IsSwapping{get;private set;}public OriginalSwapSorting Sorting=new OriginalSwapSorting(231352099,1000,false);public void SwapAnimationStarted(bool valid,int other){IsSwapping=true;S.Trace.Add("begin"+Id+":"+(valid?1:0)+":"+other);}public void SwapAnimationEnded(){IsSwapping=false;S.Trace.Add("end"+Id);}}
class Context:IOriginalSwapMatchContext{public IReadOnlyList<IOriginalSwapMatchGroup> FindExtensiveMatchesInArea(OriginalGridCell c,int r){return new IOriginalSwapMatchGroup[0];}public bool HasSpecialItem(OriginalGridCell c){return false;}public bool CanComboWith(OriginalBoardItem a,OriginalBoardItem b){throw new Exception();}}
class Prepare:IOriginalSwapAnimationBackend{public void PlayValidSwap(OriginalSwapActionState a){throw new Exception();}public void PlayWrongSwap(OriginalSwapActionState a){}}
class Move{public View Target;public OriginalSwapPoint End;public Action Complete;}
class Services:IOriginalNormalSwapServices{
public List<string> Trace=new List<string>();public List<Move> Moves=new List<Move>();public OriginalGridCell[] Cells;public View[] Views;public OriginalGameOperations Operations;public OriginalSwapActionState Action;public OriginalSwapPoint[] Points;
public static uint Bits(float f){return BitConverter.ToUInt32(BitConverter.GetBytes(f),0);}public string Status(){return(Action.IsSwapping?1:0)+","+Cells[0].FallFreezeCount+","+Cells[1].FallFreezeCount+","+Operations.Container.GetCount(1)+","+Operations.Container.GetCount(2);}static string Sorting(OriginalSwapSorting s){return s.Layer+","+s.Order+","+(s.SortEverything?1:0);}
public void ClearSinglePropellerWinCalculationData(){Trace.Add("clearKing");}public OriginalSwapPoint GetCellLocalPosition(OriginalGridCell c){return Points[Array.IndexOf(Cells,c)];}public void PlayCellSwapFlash(OriginalGridCell c){Trace.Add("flash"+Array.IndexOf(Cells,c)+":"+Status());}public OriginalSwapSorting GetSwapStartSorting(){return new OriginalSwapSorting(231352099,9005,true);}public bool IsViewMissing(IOriginalSwapAnimationView view){return view==null;}
public IOriginalSwapAnimationView GetView(OriginalBoardItem item){Trace.Add("view"+item.Id);return Views[item.Id];}public OriginalSwapSorting ArrangeSelfSorting(IOriginalSwapAnimationView view){var v=(View)view;v.Sorting=new OriginalSwapSorting(231352099,1000,false);Trace.Add("arrange"+v.Id);return v.Sorting;}public void SetSorting(IOriginalSwapAnimationView view,OriginalSwapSorting s){var v=(View)view;v.Sorting=s;Trace.Add("sort"+v.Id+":"+Sorting(s));}
public object PlaySwapParticle(IOriginalSwapAnimationView view,int direction){var v=(View)view;var s=v.Sorting;s.Order=unchecked(s.Order+1);s.SortEverything=false;Trace.Add("particle"+v.Id+":"+direction+":"+Sorting(s));return v.Id;}public void UpdateParticleSorting(object particle,OriginalSwapSorting sorting){Trace.Add("psort"+particle+":"+Sorting(sorting));}
public void PlaySfx(int id,float delay){Trace.Add("sfx"+id+":"+Bits(delay));}public void PlayHaptic(int id,int type,float strength,float duration,bool replay){Trace.Add("haptic"+id+":"+type+":"+Bits(strength)+":"+Bits(duration)+":"+(replay?1:0));}
public void MoveToLocal(IOriginalSwapAnimationView view,OriginalSwapPoint point,float duration,Action complete){var v=(View)view;Trace.Add("move"+v.Id+":"+Bits(point.X)+","+Bits(point.Y)+","+Bits(point.Z)+","+Bits(duration));Moves.Add(new Move{Target=v,End=point,Complete=complete});}
public void ExplodeMatchGroups(OriginalSwapDecision decision,OriginalBoardItem other,OriginalBoardItem moving){Trace.Add("groups:"+(other==null?-1:other.Id)+":"+moving.Id);}public void ExplodeSpecialItems(OriginalBoardItem other,OriginalBoardItem moving){Trace.Add("special:"+(other==null?-1:other.Id)+":"+moving.Id);}}
class Verify{static void Overlap(){var op=new OriginalGameOperations(()=>123);var cells=new[]{new OriginalGridCell(2,3,1),new OriginalGridCell(3,3,1)};foreach(var c in cells)c.BindGameOperations(op);var items=new[]{new OriginalMatchBoardItem(0,1,1),new OriginalMatchBoardItem(1,1,2)};for(int i=0;i<2;i++)cells[i].Mediation.SetAllItems(items[i]);var action=new OriginalSwapActionState(new OriginalSwapDecision(new Context()));action.StartSwapAnimation(cells[0],cells[1],new Prepare());OriginalSwapDecision.SetAllItems(cells[0],items[1]);OriginalSwapDecision.SetAllItems(cells[1],items[0]);
var s=new Services{Operations=op,Cells=cells,Action=action,Points=new[]{new OriginalSwapPoint(2.25f,-3.5f,.125f),new OriginalSwapPoint(3.25f,-3.5f,-.25f)}};s.Views=new[]{new View{Id=0,S=s},new View{Id=1,S=s}};action.OnSwapAnimationStart+=()=>{s.Trace.Add("start:"+s.Status());op.OperationStart(2);};action.OnSwapAnimationEnd+=type=>{s.Trace.Add("finish"+type+":"+s.Status());op.OperationFinish(2);};var backend=new OriginalNormalSwapAnimationBackend(action,s,.135f,.04f);backend.PlayValidSwap(action);var first=s.Moves.ToArray();s.Moves.Clear();first[1].Complete();if(action.IsSwapping)throw new Exception("Action did not become idle");action.Clear();action.StartSwapAnimation(cells[0],cells[1],new Prepare());backend.PlayWrongSwap(action);var second=s.Moves.ToArray();s.Moves.Clear();s.Trace.Add("oldSecondary");first[0].Complete();foreach(var m in second)m.Complete();var returns=s.Moves.ToArray();s.Moves.Clear();foreach(var m in returns)m.Complete();Console.WriteLine(string.Join("|",s.Trace)+"#"+s.Status()+","+(s.Views[0].IsSwapping?1:0)+","+(s.Views[1].IsSwapping?1:0));}
static void Main(){string line;while((line=Console.ReadLine())!=null){var a=line.Split(',');if(a[0]=="O"){Overlap();continue;}if(a[0]=="D"){var neighbor=new OriginalCellNeighbors(int.Parse(a[1]),int.Parse(a[2]));Console.WriteLine(neighbor.GetNeighborType(int.Parse(a[3]),int.Parse(a[4])));continue;}var v=Array.ConvertAll(a,int.Parse);bool valid=v[0]!=0,other=v[1]!=0,reverse=v[6]!=0;var op=new OriginalGameOperations(()=>123);var cells=new[]{new OriginalGridCell(2,3,1),new OriginalGridCell(2+v[2],3+v[3],1)};foreach(var c in cells)c.BindGameOperations(op);for(int i=0;i<v[7];i++)cells[0].FreezeFall();for(int i=0;i<v[8];i++)cells[1].FreezeFall();var items=new[]{new OriginalMatchBoardItem(0,1,1),new OriginalMatchBoardItem(1,1,2)};cells[0].Mediation.SetAllItems(items[0]);if(other)cells[1].Mediation.SetAllItems(items[1]);var action=new OriginalSwapActionState(new OriginalSwapDecision(new Context()));action.StartSwapAnimation(cells[0],cells[1],new Prepare());if(valid){if(other)OriginalSwapDecision.SetAllItems(cells[0],items[1]);OriginalSwapDecision.SetAllItems(cells[1],items[0]);}
var s=new Services{Operations=op,Cells=cells,Action=action,Points=new[]{new OriginalSwapPoint(2.25f,-3.5f,.125f),new OriginalSwapPoint(2.25f+v[2],-3.5f+v[3],-.25f)}};s.Views=new[]{new View{Id=0,S=s},new View{Id=1,S=s}};action.OnSwapAnimationStart+=()=>{s.Trace.Add("start:"+s.Status());op.OperationStart(2);};action.OnSwapAnimationEnd+=type=>{s.Trace.Add("finish"+type+":"+s.Status());op.OperationFinish(2);};var backend=new OriginalNormalSwapAnimationBackend(action,s,.135f,.04f);if(valid)backend.PlayValidSwap(action);else backend.PlayWrongSwap(action);
for(int i=0;i<2;i++)if(v[4+i]==0)items[i].Mediator.ClearFromAllRegisteredCells();var outward=s.Moves.ToArray();s.Moves.Clear();if(reverse)Array.Reverse(outward);foreach(var m in outward)m.Complete();var returns=s.Moves.ToArray();s.Moves.Clear();if(reverse)Array.Reverse(returns);foreach(var m in returns)m.Complete();if(s.Moves.Count!=0)throw new Exception();Console.WriteLine(string.Join("|",s.Trace)+"#"+s.Status()+","+(s.Views[0].IsSwapping?1:0)+","+(s.Views[1].IsSwapping?1:0));}}}'''
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder);source=folder/'Verify.cs';source.write_text(harness);exe=folder/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
    names=('OriginalNormalSwapAnimationBackend','OriginalSwapCompletion','OriginalSwapActionState','OriginalSwapDecision','OriginalItemSwapAvailability',
        'OriginalFillingItemPlacement','OriginalFillingCell','OriginalFallRefillColumns','OriginalCellTouchRules','OriginalCellMediation','OriginalGridIterator',
        'OriginalMatchSearch','OriginalMatchCandidates','OriginalMatchSnapshot','OriginalGameOperations','OriginalCellFallState')
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),*[str(runtime/(n+'.cs')) for n in names]],capture_output=True,text=True,encoding='utf-8')
    assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8')
    assert result.returncode==0,result.stdout+result.stderr
    actual=result.stdout.replace('\ufeff','').splitlines()
    assert actual==expected,next(((i,cases[i],a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e),'count mismatch')
report={'passed':True,'native_normal_swap_lifecycles':128,'native_forced_action_reuse_cases':1,'native_direction_cases':len(direction_cases),
 'original_rvas':['0x1ee2cd0','0x1ee35f4','0x319db80','0x3195550','0x1ee44b4','0x1ee45cc','0x1ee4734','0x1ee48dc','0x1ee4968','0x1ee4aa8'],
 'scope':'Actual native normal-match swap startup, native cell freeze/operations, tween scheduling and real generated completion callbacks vs production backend and completion implementation. Four cardinal directions, empty second cell, both callback orders, surviving/removed items, nested freezes and a forced action reuse with the previous secondary callback pending. Normal domain supplies no jelly/combos. View/particle/SFX/haptic/King services, cell positions, delegate construction and tween scheduling/storage are supplied boundaries; explosion services are traced. Unity input, concrete particle/factory/resolution integration, specials and full playable loop remain pending.'}
(p.PROJECT/'normal_swap_animation_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
