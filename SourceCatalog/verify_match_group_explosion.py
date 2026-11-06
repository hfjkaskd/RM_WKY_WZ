"""Compare native group explosion, data preparation and touching-cell dispatch."""
import sys
sys.dont_write_bytecode=True
import verify_board_layout as b
import json,random,struct,subprocess,tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_X3,UC_ARM64_REG_X4,UC_ARM64_REG_X8,UC_ARM64_REG_LR,UC_ARM64_REG_PC,UC_ARM64_REG_SP,UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2
p=b.p
manager=0x10000000;group=0x10001000;data=0x10002000;output=0x10003000
group_statics=0x10004000;point_statics=0x10005000;exploder_statics=0x10006000;vector_statics=0x10007000
neighbor_statics=0x10008000;directions=0x10009000;session_statics=0x1000a000
match_class=0x1000b000;base_class=0x1000c000;hierarchy=0x1000d000;fall_class=0x1000e000;interface_offsets=0x1000f000
item_base=0x10010000;cell_base=0x10020000;fall_base=0x10040000
collect_list=0x10060000;special_list=0x10062000;context=0x10064000;session_unit=0x10065000;active_level=0x10066000
grid_manager=0x10067000;creation_unit=0x10068000;special_creator=0x10069000;sfx=0x1006a000;haptic=0x1006b000;sorter_statics=0x1006c000
trace=[];nodes=[];water=0;jelly=0;last_origin=-1
bits=lambda f:struct.unpack('<I',struct.pack('<f',f))[0]
def item(i):return item_base+i*0x400
def cell(i):return cell_base+i*0x800
def fall(i):return fall_base+i*0x100
def cell_id(pointer):return -1 if not pointer else (pointer-cell_base)//0x800
def item_id(pointer):return -1 if not pointer else (pointer-item_base)//0x400
def bind(slot,statics=None):
    if slot not in b.resolved_slots:
        p.emu.reg_write(UC_ARM64_REG_X0,slot);b.boundary(p.emu,0xffd960,4,None)
    klass=p.read(slot,8)
    if statics is not None:p.put(klass+0xb8,statics,8)
    return klass
def cells_string(owner):return ','.join(str(cell_id(p.read(owner+8+i*8,8))) for i in range(p.read(owner)))
def collect_string(owner):return cells_string(owner)+'/'+','.join(str(p.signed(p.read(owner+o))) for o in (0x78,0x7c,0x80,0x84))+'/'+str(cell_id(p.read(owner+0x88,8)))
def special_string(owner):return cells_string(owner)+'/'+str(cell_id(p.read(owner+8,8)))+'/'+str(int(p.read(owner)>0))
def group_string(owner):return ','.join(map(str,(p.read(owner),p.read(owner+4),p.read(owner+8),cell_id(p.read(owner+0x88,8)),p.read(owner+0x90,1),p.read(owner+0x91,1))))
def data_string(owner):
    result=[p.signed(p.read(owner+o)) for o in (0,4,8,12,16)]
    result += [p.read(owner+0x14,1),p.read(owner+0x15,1)]
    result += [p.signed(p.read(owner+o)) for o in (0x18,0x1c,0x20,0x24,0x28)]
    result += [p.read(owner+o) for o in (0x2c,0x30,0x34)]
    return ','.join(map(str,result))
def boundary(uc,address,size,unused):
    global last_origin
    x0,x1,x2,x3,x4=[uc.reg_read(r) for r in (UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_X3,UC_ARM64_REG_X4)]
    result=None
    if address==0xffda88:raise AssertionError('Native null at '+hex(uc.reg_read(UC_ARM64_REG_LR)))
    if address in (0xd018a0,0xd00b50):uc.mem_write(x0,bytes(uc.mem_read(x1,x2)));result=x0
    elif address==0xd01fa0:uc.mem_write(x0,bytes([x1&255])*x2);result=x0
    elif address==0x2627eb8:uc.mem_write(x0+0x20+x1*8,bytes(x2*8));result=0
    elif address==0x2c1d5f8:
        array=p.read(x0+0x10,8);values=[p.read(array+0x20+i*8,8) for i in range(p.read(x0+0x18))]
        values.sort(key=lambda ptr:(nodes[item_id(ptr)][2],nodes[item_id(ptr)][1]))
        for i,value in enumerate(values):p.put(array+0x20+i*8,value,8)
        result=0
    elif address==0x2c1c940:
        array=p.read(x0+0x10,8);count=p.read(x0+0x18)
        for i in range(count,x1,-1):p.put(array+0x20+i*8,p.read(array+0x20+(i-1)*8,8),8)
        p.put(array+0x20+x1*8,x2,8);p.put(x0+0x18,count+1);p.put(x0+0x1c,p.read(x0+0x1c)+1);result=0
    elif address==0x2b45968:result=cell((x0-item_base-0x300)//0x400)
    elif address==0x2ee8840:
        i=(x0-fall_base)//0x100;trace.append('changed:'+str(i)) # Native getter executes.
    elif address==0x1ed98f8:trace.append('shuffle:'+cells_string(x1));result=0
    elif address==0x3440fa4:
        assert x1==6
        i=cell_id(x0-0x600);trace.append('static:'+str(i));result=nodes[i][3]
    elif address==0x12e03dc:trace.append('water');result=water
    elif address==0x12dfba8:trace.append('jelly:'+str(p.read(x1+0x14,1)));result=jelly
    elif address==0x3440544:trace.append('above:'+str(cell_id(x0-0x600))+':'+data_string(x1));result=1
    elif address==0x20000100:
        trace.append('item:'+str(item_id(x0))+':'+data_string(x1)+':'+collect_string(x0+0x150)+':'+special_string(x0+0x1e0));result=0
    elif address==0x3207954:
        point=(p.signed(x1&0xffffffff),p.signed(x1>>32));trace.append('grid:'+','.join(map(str,point)))
        last_origin=next((i for i,node in enumerate(nodes) if tuple(node[1:3])==point),-1);result=cell(last_origin) if last_origin>=0 else 0
    elif address==0x3191dd4:
        i=cell_id(x0)
        for reg,value in ((UC_ARM64_REG_S0,nodes[i][1]+.125),(UC_ARM64_REG_S1,nodes[i][2]+.25),(UC_ARM64_REG_S2,.5)):uc.reg_write(reg,bits(value))
        result=0
    elif address==0x32b9ab0:
        assert x0==active_level and x3==5
        assert uc.reg_read(UC_ARM64_REG_S0)==bits(nodes[last_origin][1]+.125)
        trace.append('madness:'+','.join(map(str,(last_origin,x1,x2,x3))));result=0
    elif address==0x1c91994:
        assert x0==special_creator
        trace.append('create:'+str(cell_id(x2))+':'+str(x3)+':'+group_string(x1));result=0
    elif address==0x319d7a4:trace.append('near:'+str(cell_id(x0))+':'+data_string(x1));result=0
    elif address==0x2180304:
        assert x0==sfx;trace.append('sfx:'+str(x1));result=0
    elif address==0x359e6bc:
        assert (x0,x1,x2,x3)==(haptic,25,1,0)
        assert uc.reg_read(UC_ARM64_REG_S0)==bits(1) and uc.reg_read(UC_ARM64_REG_S1)==bits(0)
        trace.append('haptic');result=0
    if result is not None:uc.reg_write(UC_ARM64_REG_X0,result&0xffffffffffffffff);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,boundary)
bind(0x5590270,group_statics);native_match_class=bind(0x55902a0);fall_interface=bind(0x558df78)
bind(0x558abf0,point_statics);bind(0x558cb38,exploder_statics);bind(0x5595e08,vector_statics)
bind(0x558abe8,neighbor_statics);bind(0x5595c00,session_statics);bind(0x558ed38,sorter_statics);bind(0x558aba0)
def call(rva,args=(),target=output):
    for reg,value in zip((UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_X3,UC_ARM64_REG_X4),[*args,0,0,0,0,0]):p.emu.reg_write(reg,value&0xffffffffffffffff)
    p.emu.reg_write(UC_ARM64_REG_X8,target);p.emu.reg_write(UC_ARM64_REG_SP,0x101ff000);p.emu.reg_write(UC_ARM64_REG_LR,0x20000000)
    try:p.emu.emu_start(rva,0x20000000,count=2000000)
    except Exception:print('Native PC',hex(p.emu.reg_read(UC_ARM64_REG_PC)));raise
    assert p.emu.reg_read(UC_ARM64_REG_PC)==0x20000000
    return p.emu.reg_read(UC_ARM64_REG_X0)
def setup(values,indices,pattern,creation,can=1):
    global nodes,trace
    nodes=values;trace=[]
    p.emu.mem_write(manager,bytes(0x70000));p.heap=0x10080000
    p.put(point_statics,-1,8);p.put(neighbor_statics+8,directions,8);p.put(directions+0x18,4)
    for i,d in enumerate((1,3,5,7)):p.put(directions+0x20+i*4,d)
    p.put(session_statics,context,8);p.put(context+0x38,session_unit,8);p.put(session_unit+0x18,active_level,8)
    p.put(manager+0x20,collect_list,8);p.put(manager+0x28,special_list,8);p.put(manager+0x30,haptic,8)
    p.put(manager+0x38,grid_manager,8);p.put(manager+0x40,creation_unit,8);p.put(creation_unit+0x40,special_creator,8);p.put(manager+0x80,sfx,8)
    for lst in (collect_list,special_list):p.put(lst+0x10,lst+0x1000,8);p.put(lst+0x1018,64)
    p.put(native_match_class+0x12c,1,1);p.put(match_class+0x12c,1,1);p.put(match_class+0xc8,hierarchy,8);p.put(hierarchy,native_match_class,8)
    p.put(match_class+0x508,0x20000100,8)
    p.put(fall_class+0x12a,1,2);p.put(fall_class+0xb0,interface_offsets,8);p.put(interface_offsets,fall_interface,8);p.put(interface_offsets+8,0);p.put(fall_class+0x168,0x2ee8840,8)
    # Node: match/base kind, X,Y, static6, StateChangedTime bits, neighbors1/3/5/7.
    for i,(kind,x,y,static,changed,*neighbors) in enumerate(values):
        p.put(item(i),match_class if kind else base_class,8);p.put(item(i)+0x38,item(i)+0x300,8);p.put(item(i)+0x40,fall(i),8)
        p.put(fall(i),fall_class,8);p.put(fall(i)+0x60,changed)
        p.put(cell(i)+0x18,x);p.put(cell(i)+0x1c,y);p.put(cell(i)+0xc0,cell(i)+0x600,8)
        p.put(cell(i)+0x10,cell(i)+0x100,8);p.put(cell(i)+0x110,cell(i)+0x200,8);p.put(cell(i)+0x218,item(i),8)
        p.put(cell(i)+0x20,cell(i)+0x300,8);p.put(cell(i)+0x318,cell(i)+0x400,8);p.put(cell(i)+0x418,8)
        for d,neighbor in zip((1,3,5,7),neighbors):p.put(cell(i)+0x420+8*d,0 if neighbor<0 else cell(neighbor),8)
    p.put(group,pattern);p.put(group+4,3);p.put(group+8,len(indices));p.put(group+0x88,cell(creation) if creation>=0 else 0,8)
    p.put(group+0x90,1,1);p.put(group+0x91,can,1);p.put(group+0x92,1,1)
    for i,index in enumerate(indices):p.put(group+0x10+i*8,cell(index),8)
def serialize_nodes(values):return ';'.join(','.join(map(str,node)) for node in values)
def input_line(kind,header,indices,values):return kind+'#'+','.join(map(str,header))+'#'+','.join(map(str,indices))+'#'+serialize_nodes(values)
rng=random.Random(0x12e33dc);cases=[];expected=[]
for n in range(96):
    count=n%15;indices=rng.sample(range(25),count);creation=indices[n%count] if count and n%3 else -1;pattern=1+n%7
    values=[[1,i%5-2,i//5-2,int(rng.random()<.2),bits(rng.choice((0,.1,1,2))),-1,-1,-1,-1] for i in range(25)]
    setup(values,indices,pattern,creation)
    # The native caller passes a distinct value-copy to each helper.
    p.emu.mem_write(group+0x200,bytes(p.emu.mem_read(group,0x98)))
    call(0x12e3a98,(manager,group+0x200));collect=collect_string(output)
    p.emu.mem_write(group+0x200,bytes(p.emu.mem_read(group,0x98)))
    call(0x12e3d88,(manager,group+0x200));special=special_string(output)
    cases.append(input_line('P',(pattern,creation),indices,values));expected.append(collect+'#'+special+'#'+'|'.join(trace)+'#'+str(cell_id(p.read(group+0x88,8))))
for n in range(80):
    count=n%15;indices=rng.sample(range(25),count);creation=indices[0] if count and n%9==0 else -1
    values=[[1,i%5-2,i//5-2,int(rng.random()<.25),bits(rng.choice((0,-.1,.1,1,2))) if n%11 else 0x7fc00000,-1,-1,-1,-1] for i in range(25)]
    setup(values,indices,2,creation);result=call(0x12df234,(group,))
    cases.append(input_line('S',(2,creation),indices,values));expected.append(str(cell_id(result))+'#'+'|'.join(trace)+'#'+str(cell_id(p.read(group+0x88,8))))
for n in range(160):
    count=n%15;indices=rng.sample(range(25),count);creation=indices[n%count] if count and n%3 else -1;pattern=1 if n%2 else 2+n%6
    values=[]
    for i in range(25):
        x=i%5-2;y=i//5-2
        neighbors=[i+5 if y<2 else -1,i+1 if x<2 else -1,i-5 if y>-2 else -1,i-1 if x>-2 else -1]
        values.append([int(n%5!=0 or i%2==0),x,y,int(n%4==0 and i%3==0),bits(rng.choice((0,.1,1))),*neighbors])
    water=int(n%13==0);can=int(n%11!=0);jelly=n%2;trigger=(2,4,18)[n%3];spread=(n//2)%2;auto=(n//3)%2;start=n%5
    setup(values,indices,pattern,creation,can)
    # Explicit native value includes non-default identifier/vector fields.
    p.emu.mem_write(data,bytes(56))
    for off,value in ((0,n%5-2),(4,n//5%5-2),(8,trigger),(12,17),(16,2),(0x18,7),(0x1c,8),(0x20,9),(0x24,10),(0x28,11),(0x2c,bits(.125)),(0x30,bits(-.25)),(0x34,bits(.5))):p.put(data+off,value)
    p.put(data+0x14,spread,1);p.put(data+0x15,n%2,1);p.put(exploder_statics,start)
    trace=[];result=call(0x12e33dc,(manager,group,data,auto))
    header=(pattern,creation,water,can,jelly,trigger,spread,auto,start,n%5-2,n//5%5-2,n%2)
    cases.append(input_line('G',header,indices,values));expected.append(str(result&1)+'#'+'|'.join(trace)+'#'+str(p.read(exploder_statics)))
for n in range(48):
    indices=[rng.randrange(25) for _ in range(n%8)] # Repeated origins remain repeated hits.
    values=[[1,i%5,i//5,0,0,*[rng.choice((-1,-1,0,3,9,24)) for _ in range(4)]] for i in range(25)]
    setup(values,[],1,-1);start=n%5;p.put(exploder_statics,start)
    lst=collect_list;array=p.read(lst+0x10,8);p.put(lst+0x18,len(indices))
    for i,index in enumerate(indices):p.put(array+0x20+8*i,cell(index),8)
    trace=[];call(0x12e416c,(manager,lst,n%7))
    cases.append(input_line('N',(n%7,start),indices,values));expected.append('|'.join(trace)+'#'+str(p.read(exploder_statics)))

harness=r'''using System;using System.Collections.Generic;using System.Globalization;using Whitebox.Foundation;
namespace UnityEngine{public static class Debug{public static void LogError(object x){throw new Exception(x.ToString());}}}
class Fall:IOriginalFillingFallComponent,IOriginalItemFallTiming{public int Id;public float Changed;public bool IsItemSteady{get{return true;}}public float StateChangedTime{get{Verify.Trace.Add("changed:"+Id);return Changed;}set{Changed=value;}}public void SetFillingCell(OriginalFillingCell c){}}
class Services:IOriginalMatchGroupExplosionServices{
public OriginalGridCell[] Cells;public bool[] Static;public bool Water,Jelly;
public bool IsAutoMatchDisabledForWaterLevel{get{Verify.Trace.Add("water");return Water;}}
public bool HasStaticItem(OriginalGridCell c,int type){if(type!=6)throw new Exception();int id=Verify.Id(c);Verify.Trace.Add("static:"+id);return Static[id];}
public void ShuffleCellsBasedOnDifficulty(OriginalMatchCells c){Verify.Trace.Add("shuffle:"+Verify.CellString(c));}
public bool InitJellySpreadData(ref OriginalMatchGroupSnapshot g,OriginalExplodeData d){Verify.Trace.Add("jelly:"+(d.CanSpreadJelly?1:0));return Jelly;}
public void ExplodeTopMostAboveItem(OriginalGridCell c,OriginalExplodeData d){Verify.Trace.Add("above:"+Verify.Id(c)+":"+Verify.Data(d));}
public void ExplodeMatchItem(OriginalMatchBoardItem i,OriginalExplodeData d){Verify.Trace.Add("item:"+i.Id+":"+Verify.Data(d)+":"+Verify.Collect(i.CollectData)+":"+Verify.Special(i.SpecialItemCreationData));}
public OriginalGridCell GetCell(OriginalCellPoint p){Verify.Trace.Add("grid:"+p.X+","+p.Y);for(int i=0;i<Cells.Length;i++)if(Cells[i].X==p.X&&Cells[i].Y==p.Y)return Cells[i];return null;}
public void TryCollectMadness(OriginalGridCell c,int color,int count,int trigger){Verify.Trace.Add("madness:"+Verify.Id(c)+","+color+","+count+","+trigger);}
public void CreateSpecialItemForMatchGroup(OriginalMatchGroupSnapshot g,OriginalGridCell c,bool auto){Verify.Trace.Add("create:"+Verify.Id(c)+":"+(auto?1:0)+":"+Verify.Group(g));}
public void ExplodeCurrentItemByNearMatch(OriginalGridCell c,OriginalExplodeData d){Verify.Trace.Add("near:"+Verify.Id(c)+":"+Verify.Data(d));}
public void PlayMatchExplode(bool swap){Verify.Trace.Add("sfx:"+(swap?1:0));}
public void PlayHaptic(int id,int type,float strength,float duration,bool replay){if(id!=25||type!=1||strength!=1||duration!=0||replay)throw new Exception();Verify.Trace.Add("haptic");}
}
class Verify{
public static List<string> Trace=new List<string>();static OriginalGridCell[] cells;static float Float(uint b){return BitConverter.ToSingle(BitConverter.GetBytes(b),0);}static uint Bits(float f){return BitConverter.ToUInt32(BitConverter.GetBytes(f),0);}public static int Id(OriginalGridCell c){return c==null?-1:Array.IndexOf(cells,c);}public static string CellString(OriginalMatchCells c){var s=new List<string>();for(int i=0;i<c.Count;i++)s.Add(Id(c.Get(i)).ToString());return string.Join(",",s);}
public static string Collect(OriginalMatchCollectData c){return CellString(c.OrderedCells)+"/"+c.CollectType+","+c.PatternOrientation+","+c.MinYIndex+","+c.MinXIndex+"/"+Id(c.SpecialItemCreationCell);}public static string Special(OriginalSpecialItemCreationData c){return CellString(c.OrderedCells)+"/"+Id(c.GetCreationCell())+"/"+(c.ShouldCreateSpecialItem?1:0);}public static string Group(OriginalMatchGroupSnapshot g){return g.PatternType+","+g.MatchType+","+g.Cells.Count+","+Id(g.SpecialItemCreationCell)+","+(g.ExplosionCalculated?1:0)+","+(g.CanExplode?1:0);}
public static string Data(OriginalExplodeData d){var t=d.LastTargetItemIdentifier;return string.Join(",",new object[]{d.Point.X,d.Point.Y,d.Trigger,d.Id,d.MatchType,d.CanSpreadJelly?1:0,d.IsHorizontalRocket?1:0,d.ComboId,t.ItemType,t.StaticItemType,t.ItemGroupId,d.ExploderIndex,Bits(d.IncomingX),Bits(d.IncomingY),Bits(d.IncomingZ)});}
static void Main(){string line;while((line=Console.ReadLine())!=null){var a=line.Split('#');var h=Array.ConvertAll(a[1].Split(','),int.Parse);var indices=a[2].Length==0?new int[0]:Array.ConvertAll(a[2].Split(','),int.Parse);var ns=a[3].Split(';');cells=new OriginalGridCell[ns.Length];var values=new long[ns.Length][];var services=new Services{Cells=cells,Static=new bool[ns.Length]};for(int i=0;i<ns.Length;i++){var v=Array.ConvertAll(ns[i].Split(','),long.Parse);values[i]=v;cells[i]=new OriginalGridCell((int)v[1],(int)v[2],1);OriginalBoardItem item=v[0]!=0?(OriginalBoardItem)new OriginalMatchBoardItem(i,1,3):new OriginalBoardItem(i,1);item.FallComponent=new Fall{Id=i,Changed=Float((uint)v[4])};cells[i].Mediation.SetAllItems(item);services.Static[i]=v[3]!=0;}for(int i=0;i<cells.Length;i++)for(int d=0;d<4;d++)if(values[i][5+d]>=0)cells[i].Neighbors.Set(1+d*2,cells[(int)values[i][5+d]]);
var g=new OriginalMatchGroupSnapshot(1,h[0],3);for(int i=0;i<indices.Length;i++)g.Cells.Add(cells[indices[i]]);g.SpecialItemCreationCell=h[1]<0?null:cells[h[1]];g.ExplosionCalculated=true;g.CanExplode=true;var preparation=new OriginalMatchDataPreparation(services);var explosion=new OriginalMatchGroupExplosion(new OriginalMatchExplosionReadiness(()=>1f),services);Trace.Clear();
if(a[0]=="P"){var collect=preparation.CreateCollectData(g);var special=preparation.CreateSpecialItemData(g);Console.WriteLine(Collect(collect)+"#"+Special(special)+"#"+string.Join("|",Trace)+"#"+Id(g.SpecialItemCreationCell));continue;}if(a[0]=="S"){var c=preparation.GetSpecialItemCell(ref g);Console.WriteLine(Id(c)+"#"+string.Join("|",Trace)+"#"+Id(g.SpecialItemCreationCell));continue;}
OriginalExploder.Reset();int start=a[0]=="N"?h[1]:h[8];for(int i=0;i<start;i++)OriginalExploder.Next(0);Trace.Clear();if(a[0]=="N"){var list=new List<OriginalGridCell>();foreach(int i in indices)list.Add(cells[i]);explosion.DestroyTouchingItems(list,h[0]);Console.WriteLine(string.Join("|",Trace)+"#"+(OriginalExploder.Next(0).Id-1));continue;}
services.Water=h[2]!=0;g.CanExplode=h[3]!=0;services.Jelly=h[4]!=0;var data=new OriginalExplodeData(h[5],17,new OriginalCellPoint(h[9],h[10]),2,h[6]!=0,h[11]!=0,7,11);data.LastTargetItemIdentifier=new OriginalItemIdentifier{ItemType=8,StaticItemType=9,ItemGroupId=10};data.IncomingX=.125f;data.IncomingY=-.25f;data.IncomingZ=.5f;bool result=explosion.ExplodeMatchGroup(g,data,h[7]!=0);Console.WriteLine((result?1:0)+"#"+string.Join("|",Trace)+"#"+(OriginalExploder.Next(0).Id-1));
}}}'''
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder);source=folder/'Verify.cs';source.write_text(harness);exe=folder/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
    names=('OriginalMatchExplosionData','OriginalMatchGroupExplosion','OriginalMatchExplosionReadiness','OriginalExplodeData','OriginalFallComponentState','OriginalItemSwapAvailability',
      'OriginalFillingItemPlacement','OriginalFillingCell','OriginalFallRefillColumns','OriginalCellTouchRules','OriginalCellMediation','OriginalGridIterator',
      'OriginalMatchSearch','OriginalMatchCandidates','OriginalMatchSnapshot','OriginalCellFallState','OriginalGameOperations')
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),*[str(runtime/(n+'.cs')) for n in names]],capture_output=True,text=True,encoding='utf-8')
    assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8')
    assert result.returncode==0,result.stdout+result.stderr
    actual=result.stdout.replace('\ufeff','').splitlines()
    assert actual==expected,next(((i,cases[i],a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e),'count mismatch')
report={'passed':True,'native_data_preparation_cases':96,'native_creation_cell_selection_cases':80,'native_group_explosion_cases':160,'native_touching_dispatch_cases':48,
 'original_rvas':['0x12e33dc','0x12e3a98','0x12e3d88','0x12df234','0x12df298','0x12df454','0x21ac470','0x21a6678','0x21a63a4','0x12e416c','0x2eebda4'],
 'scope':'Actual native collect/special data preparation, creation-cell selection, inline data copies to match items, group explosion orchestration and touching-cell dispatch vs production C#. Tests cover patterns 1..7, 0..14 cells, static-6 skips, fall-time/top-row selection, NaN, stable equal-distance insertion, early water/readiness rejection, normal/special branches, trigger-dependent origin, cloned jelly data, neighbor order/repeated hits and shared trigger IDs. Native cached readiness, fall-time getter, distance, inline Cell14 and Exploder execute. List/array storage, generic sort (Y then X), memory helpers and metadata supplied; random shuffle, jelly propagation, concrete item/static explosion, special creation, session/presentation are traced boundaries. Full collectible lifecycle, item removal, obstacle damage, fall/refill and playable input remain pending.'}
(p.PROJECT/'match_group_explosion_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
