"""Compare pool/particle-manager lifecycle traces with the original ARM64."""
import sys
sys.dont_write_bytecode = True
import json, random, subprocess, tempfile
from collections import Counter
from pathlib import Path
from types import SimpleNamespace
PROJECT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(PROJECT.parent/'tools/python'))
from elftools.elf.elffile import ELFFile
from elftools.elf.relocation import RelocationSection
from unicorn import Uc,UC_ARCH_ARM64,UC_MODE_ARM,UC_HOOK_CODE
from unicorn.arm64_const import *
p=SimpleNamespace(PROJECT=PROJECT,mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
p.emu=Uc(UC_ARCH_ARM64,UC_MODE_ARM);p.emu.mem_map(0,0x6000000);p.emu.mem_map(0x10000000,0x200000);p.emu.mem_map(0x20000000,0x1000)
def put(address,value,size=4):p.emu.mem_write(address,(int(value)&((1<<(size*8))-1)).to_bytes(size,'little'))
def read(address,size=4):return int.from_bytes(p.emu.mem_read(address,size),'little')
p.put=put;p.read=read;p.signed=lambda v:v-0x100000000 if v>=0x80000000 else v
with(PROJECT.parent.parent/'RoyalMatch_20260929/apk_unpacked/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so').open('rb')as stream:
    elf=ELFFile(stream)
    for segment in elf.iter_segments():
        if segment['p_type']=='PT_LOAD':p.emu.mem_write(segment['p_vaddr'],segment.data())
    for section in elf.iter_sections():
        if isinstance(section,RelocationSection):
            for relocation in section.iter_relocations():
                if relocation['r_info_type']==1027:put(relocation['r_offset'],relocation['r_addend'],8)
resolved_slots=set();metadata_heap=0x10080000
regs = [UC_ARM64_REG_X0, UC_ARM64_REG_X1, UC_ARM64_REG_X2, UC_ARM64_REG_X3, UC_ARM64_REG_X4]
owner, array, root, manager, active, active_array, dictionary = [0x10010000+i*0x1000 for i in range(7)]
klass, method, generic, invoke, enum_class = [0x100e0000+i*0x1000 for i in range(5)]
GET_ID, SPAWN, RECYCLE, HASH, ENUM_NEXT, ENUM_ITEM, DISPOSE, TYPE_NAME = [0x20000100+i*4 for i in range(8)]
RECYCLE_SELF = 0x20000120
trace, objects, stacks, sets, dictionaries = [], {}, {}, {}, {}
set_free = {}
heap = 0x10100000
error = ''

def allocate(size=0x100):
    global heap
    value=heap; heap+=size
    assert heap < 0x101f0000
    p.emu.mem_write(value, bytes(size)); return value

def setup_class():
    # Interface dispatch/type hierarchy metadata are storage, never behavior.
    for i,slot in enumerate((0x558e3a0,0x5590858,0x558ded8,0x5585dc0,0x558de30)):
        p.put(slot,0x100e6000+i*0x200,8); resolved_slots.add(slot)
        c=p.read(slot,8);p.put(c+0xe0,1);p.put(c+0x12c,1,1)
    p.put(klass+0x132,1,1);p.put(klass+0x12a,1,2);p.put(klass+0x12c,1,1)
    hierarchy=allocate();p.put(hierarchy,p.read(0x5590858,8),8);p.put(klass+0xc8,hierarchy,8)
    table=allocate();p.put(klass+0xb0,table,8);p.put(table,p.read(0x558e3a0,8),8);p.put(table+8,20)
    for offset,fn in ((0x278,GET_ID),(0x288,SPAWN),(0x298,RECYCLE),(0x158,HASH),(0x1b8,RECYCLE_SELF)):
        p.put(klass+offset,fn,8)
    p.put(method+0x38,generic,8);p.put(generic,klass,8);p.put(generic+8,invoke,8);p.put(invoke+8,0x2690a8c,8)
    table=allocate();p.put(enum_class+0xb0,table,8);p.put(enum_class+0x12a,3,2)
    for i,(slot,fn) in enumerate(((0x558ded8,ENUM_NEXT),(0x5585dc0,ENUM_ITEM),(0x558de30,DISPOSE))):
        p.put(table+i*16,p.read(slot,8),8);p.put(table+i*16+8,20+i);p.put(enum_class+0x278+i*16,fn,8)

def make_object(pool_id, ident, parent=root):
    obj=allocate();p.put(obj,klass,8)
    objects[obj]={'id':ident,'pool':pool_id,'active':True,'parent':parent,'alive':True}
    p.put(obj+0x20,manager,8)
    p.put(obj+0x18,owner,8)
    return obj

def set_count(ptr):
    p.put(ptr+0x20,sum(v is not None for v in sets[ptr]));p.put(ptr+0x1c,0)

def stack_count(ptr): p.put(ptr+0x18,len(stacks[ptr]))
def identity(ptr): return objects[ptr]['id'] if ptr else -1
def alive(ptr): return ptr != 0 and (ptr not in objects or objects[ptr]['alive'])
def list_values(): return [p.read(active_array+0x20+i*8,8) for i in range(p.read(active+0x18))]
def write_list(values):
    p.put(active+0x18,len(values))
    for i,v in enumerate(values):p.put(active_array+0x20+i*8,v,8)
def fault(uc,reason):
    global error
    error=reason;uc.reg_write(UC_ARM64_REG_PC,0x20000000)

def boundary(uc,address,size,unused):
    global metadata_heap
    x0,x1,x2,x3,x4=[uc.reg_read(r) for r in regs];value=None
    if address==0xffd960:
        if x0 not in resolved_slots:
            p.emu.mem_write(metadata_heap,bytes(0x200));p.put(metadata_heap+0xe0,1);p.put(x0,metadata_heap,8)
            metadata_heap+=0x200;resolved_slots.add(x0)
        value=0
    if address in (0xffda88,0xffda90,0xffde20):fault(uc,{0xffda88:'null',0xffda90:'index',0xffde20:'cast'}[address]);return
    if address==0xffda78:value=allocate(0x200)
    elif address==0xffd9d0:value=allocate(0x200);p.put(value+0x18,x1)
    elif address==0xffda68:value=x0
    elif address in (0xd00b50,0xd018a0):uc.mem_write(x0,bytes(uc.mem_read(x1,x2)));value=x0
    elif address==0xd01fa0:uc.mem_write(x0,bytes([x1&255])*x2);value=x0
    elif address==0x16d0e3c:
        if p.signed(x1&0xffffffff)<0:fault(uc,'argument');return
        stacks[x0]=[];stack_count(x0);value=0
    elif address in (0x220c2b0,0x2214d48):sets[x0]=[];set_free[x0]=[];set_count(x0);value=0
    elif address==0x16d14a4:stacks[x0].append(x1);stack_count(x0);value=0
    elif address==0x16d1420:
        if not stacks[x0]:fault(uc,'empty');return
        value=stacks[x0].pop();stack_count(x0)
    elif address==0x16d0fa4:stacks[x0].clear();stack_count(x0);value=0
    elif address in (0x220ca20,0x220d588,0x220cc0c,0x2215f5c,0x22155ec):
        collection=sets[x0];found=x1 in collection
        if address in (0x220d588,0x2215f5c) and not found:
            if set_free[x0]:collection[set_free[x0].pop()]=x1
            else:collection.append(x1)
        elif address in (0x220cc0c,0x22155ec) and found:
            i=collection.index(x1);collection[i]=None;set_free[x0].append(i)
            # HashSet.Remove resets lastIndex/freeList when the final entry is removed.
            if all(v is None for v in collection):collection.clear();set_free[x0].clear()
        set_count(x0);value=int(found if address in (0x220ca20,0x220cc0c,0x22155ec) else not found)
    elif address in (0x220c9c0,0x22153a8):sets[x0].clear();set_free[x0].clear();set_count(x0);value=0
    elif address in (0x22158b4,0x16d12d0):
        output=uc.reg_read(UC_ARM64_REG_X8);p.put(output,x0,8);p.put(output+8,0);p.put(output+16,0,8);value=0
    elif address==0x2b350a0:
        collection=sets[p.read(x0,8)];i=p.read(x0+8)
        while i<len(collection) and collection[i] is None:i+=1
        value=int(i<len(collection))
        if value:p.put(x0+16,collection[i],8);p.put(x0+8,i+1)
    elif address==0xffda6c:
        value=allocate();p.put(value,enum_class,8);uc.mem_write(value+0x10,bytes(uc.mem_read(x1,24)))
    elif address==ENUM_NEXT:
        collection=stacks[p.read(x0+0x10,8)];i=p.read(x0+0x18);value=int(i<len(collection))
        if value:p.put(x0+0x20,collection[-1-i],8);p.put(x0+0x18,i+1)
    elif address==ENUM_ITEM:value=p.read(x0+0x20,8)
    elif address in (DISPOSE,0x2b3509c,0xffd90c,0x231007c,0xffda64):value=0
    elif address==0x2690a8c:
        if not alive(x0):fault(uc,'null');return
        value=make_object(objects[x0]['pool'],len([o for o in objects.values() if o['id']>0])+1,x1)
        trace.append('clone:'+str(identity(value)))
    elif address in (0x3e473b8,0x3e4737c,0x3e49a74):value=x0
    elif address==0x3e49b30:objects[x0]['active']=bool(x1);trace.append('active:'+str(identity(x0))+':'+str(x1));value=0
    elif address==0x3e4ec54:objects[x0]['parent']=x1;trace.append('parent:'+str(identity(x0))+':'+str(identity(x1)));value=0
    elif address==0x3e49b74:value=int(objects[x0]['active'])
    elif address in (0x3e4d92c,0x3e3b754):value=int(alive(x0) if address==0x3e4d92c else not alive(x0))
    elif address==0x3e4d494:objects[x0]['alive']=False;trace.append('destroy:'+str(identity(x0)));value=0
    elif address==0x3e4bec0:trace.append('cancel:'+str(identity(x0)));value=0
    elif address==GET_ID:value=objects[x0]['pool']
    elif address==HASH:value=identity(x0)
    elif address in (SPAWN,RECYCLE):
        trace.append(('spawn:' if address==SPAWN else 'recycle:')+str(identity(x0)))
        # Execute ItemParticles callbacks as well as the manager implementation.
        uc.reg_write(UC_ARM64_REG_PC,0x2b48aac if address==SPAWN else 0x2b48acc);return
    elif address==RECYCLE_SELF:
        uc.reg_write(UC_ARM64_REG_PC,0x2b48a58);return
    elif address==0x260a1fc:
        uc.reg_write(UC_ARM64_REG_X0,owner);uc.reg_write(UC_ARM64_REG_PC,0x2605c54);return
    elif address==0x260a220:
        uc.reg_write(UC_ARM64_REG_X0,owner);uc.reg_write(UC_ARM64_REG_X3,method)
        uc.reg_write(UC_ARM64_REG_PC,0x2605dfc);return
    elif address==0x3cd04f8:
        values=dictionaries[x0];p.put(x2,values.get(x1,0),8);value=int(x1 in values)
    elif address==0x3cce930:
        if x1 in dictionaries[x0]:fault(uc,'duplicate');return
        dictionaries[x0][x1]=x2;value=0
    elif address==0x3cceac4:dictionaries[x0].clear();value=0
    elif address==0x2c1b9a0:
        if x1>=p.read(active+0x18):fault(uc,'index');return
        value=p.read(active_array+0x20+x1*8,8)
    elif address==0x2c1d0e4:
        values=list_values();value=int(x1 in values)
        if value:values.remove(x1);write_list(values)
    elif address==0x2310854:
        value=allocate();type_klass=allocate(0x200);p.put(value,type_klass,8);p.put(type_klass+0x1b8,TYPE_NAME,8)
    elif address==TYPE_NAME:value=0
    elif address in (0x2a2e598,0x2a2da14):trace.append('error' if address==0x2a2e598 else 'debug');value=0
    if value is not None:uc.reg_write(UC_ARM64_REG_X0,value&0xffffffffffffffff);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,boundary)

def call(rva,*args):
    global error
    error=''
    for reg,val in zip(regs,args+(0,)*(5-len(args))):p.emu.reg_write(reg,val&0xffffffffffffffff)
    p.emu.reg_write(UC_ARM64_REG_SP,0x101ff000);p.emu.reg_write(UC_ARM64_REG_LR,0x20000000)
    p.emu.emu_start(rva,0x20000000,count=1000000)
    assert p.emu.reg_read(UC_ARM64_REG_PC)==0x20000000,hex(p.emu.reg_read(UC_ARM64_REG_PC))
    return p.emu.reg_read(UC_ARM64_REG_X0) if not error else 0

def reset():
    global heap,trace,objects,stacks,sets,dictionaries,set_free
    heap=0x10100000;p.heap=0x10040000
    trace=[];objects={};stacks={};sets={};set_free={};dictionaries={dictionary:{}}
    p.emu.mem_write(owner,bytes(0x8000));p.emu.mem_write(klass,bytes(0x5000))
    setup_class();p.put(owner+0x10,array,8);p.put(array+0x18,8);p.put(owner+0x18,root,8)
    objects[root]={'id':0,'pool':0,'active':True,'parent':0,'alive':True}
    p.put(manager+0x10,owner,8);p.put(manager+0x18,active,8);p.put(manager+0x20,dictionary,8)
    p.put(active+0x10,active_array,8);p.put(active_array+0x18,1024)

def snapshot():
    lengths=[call(0x2d71ed4,owner,i) for i in range(8)]
    vals=','.join(str(identity(v)) for v in list_values())
    state=','.join(str(o['id'])+':'+str(int(o['active']))+':'+str(identity(o['parent']))+':'+str(int(o['alive']))
        for o in objects.values() if o['id']>0)
    return '/'.join(map(str,lengths))+';'+vals+';'+state

rng=random.Random(0x2d713ac);cases=[];expected=[];operation_count=0;outcomes=Counter()
for sequence in range(64):
    reset();references=[];commands=[];results=[]
    prefabs={i:make_object(i,-i) for i in (2,4)}
    operations=[('C',2,sequence%5),('C',4,1),('S',2,1),('S',2,0),('R',0),('R',0),('G',4),('G',4)]
    for n in range(32):
        command=rng.choice(('C','S','R','A','D','G','M','I'))
        if command=='C':op=(command,rng.choice((2,4)),rng.randrange(0,6))
        elif command=='S':op=(command,rng.choice((2,4,7)),rng.randrange(2))
        elif command in ('R','I'):op=(command,rng.randrange(64))
        elif command in ('A','D'):op=(command,rng.choice((2,4,7)),rng.randrange(5))
        elif command=='G':op=(command,4)
        else:op=(command,rng.randrange(2))
        operations.append(op)
    operations += [('M',0),('A',2,0),('A',4,0),('K',2),('K',4)]
    for op in operations:
        command=op[0];args=op[1:];value=0;trace=[]
        if command=='C':call(0x2605a00,owner,prefabs[args[0]],args[1],method)
        elif command=='S':
            result=call(0x2605dfc,owner,*args,method)
            if not error:references.append(result);value=identity(result)
        elif command=='R':
            ref=references[args[0]%len(references)] if references else 0
            call(0x2605c54,owner,ref)
        elif command=='A':call(0x2d71f60,owner,args[0])
        elif command=='D':call(0x2d71804,owner,*args)
        elif command=='G':
            result=call(0x26912f0,manager,args[0],method)
            if not error:references.append(result);value=identity(result)
        elif command=='M':call(0x1edab64,manager,*args)
        elif command=='I':
            ref=references[args[0]%len(references)] if references else 0
            if ref:objects[ref]['active']=False
        elif command=='K':call(0x2d719ec,owner,*args)
        outcome=error
        outcomes[outcome or 'success']+=1
        results.append(outcome+';'+str(value)+';'+'|'.join(trace)+';'+snapshot())
        commands.append(','.join(map(str,op)));operation_count+=1
    cases.append('|'.join(commands));expected.append('\t'.join(results))

harness=r'''using System;using System.Collections.Generic;using UnityEngine;using Whitebox.Foundation;
namespace UnityEngine{
public class Object{public bool Alive=true;static bool IsAlive(Object o){return !ReferenceEquals(o,null)&&o.Alive&&(!(o is MonoBehaviour)||((MonoBehaviour)o).gameObject.Alive);}public static bool operator==(Object a,Object b){return !IsAlive(a)==!IsAlive(b);}public static bool operator!=(Object a,Object b){return !(a==b);}public override bool Equals(object o){return ReferenceEquals(this,o);}public override int GetHashCode(){return base.GetHashCode();}public static void DontDestroyOnLoad(Object o){}public static void Destroy(Object o){o.Alive=false;Verify.Trace.Add("destroy:"+((GameObject)o).Owner.Id);}public static T Instantiate<T>(T o,Transform parent)where T:Object{if(o==null)throw new NullReferenceException();var f=(Fixture)(object)o;var clone=new Fixture(f.PoolId,Verify.Objects.Count+1);clone.transform.parent=parent;Verify.Objects.Add(clone);Verify.Trace.Add("clone:"+clone.Id);return (T)(object)clone;}}
public class GameObject:Object{public Fixture Owner;public bool activeSelf=true;public void SetActive(bool value){activeSelf=value;Verify.Trace.Add("active:"+Owner.Id+":"+(value?1:0));}}
public class Transform:Object{public GameObject gameObject;public Transform parent;public void SetParent(Transform value){parent=value;Verify.Trace.Add("parent:"+gameObject.Owner.Id+":"+(value==null?-1:value.gameObject.Owner.Id));}}
public class MonoBehaviour:Object{public GameObject gameObject;public Transform transform;public void CancelInvoke(){Verify.Trace.Add("cancel:"+gameObject.Owner.Id);}}
public class WaitForSeconds{public WaitForSeconds(float v){}}
public static class Debug{public static void LogError(object s){Verify.Trace.Add("error");}public static void Log(object s){Verify.Trace.Add("debug");}}}
namespace Whitebox.Foundation{public abstract class OriginalItemSwapView:UnityEngine.MonoBehaviour,IOriginalPoolable{public abstract int GetPoolId();public abstract void OnSpawn();public abstract void OnRecycle();}}
public class Fixture:OriginalItemParticles{public int Id,PoolId;public Fixture(int pool,int id){PoolId=pool;Id=id;gameObject=new GameObject{Owner=this};transform=new Transform{gameObject=gameObject,parent=Verify.Root};base.Awake();}public override int GetHashCode(){return Id;}public override int GetPoolId(){return PoolId;}public override void OnSpawn(){Verify.Trace.Add("spawn:"+Id);base.OnSpawn();}public override void OnRecycle(){Verify.Trace.Add("recycle:"+Id);base.OnRecycle();}public override void RecycleSelf(){base.RecycleSelf();}}
class Verify{public static List<string> Trace=new List<string>();public static List<Fixture> Objects=new List<Fixture>();public static Transform Root;public static OriginalGameObjectPool Pool;static string Snapshot(OriginalParticlesManager manager){var lengths=new string[8];for(int i=0;i<8;i++)lengths[i]=Pool.GetPoolLength(i).ToString();var ordered=new List<string>();foreach(var f in Active)ordered.Add(f.Id.ToString());var states=new List<string>();foreach(var f in Objects)states.Add(f.Id+":"+(f.gameObject.activeSelf?1:0)+":"+(f.transform.parent==null?-1:f.transform.parent.gameObject.Owner.Id)+":"+(f.gameObject.Alive?1:0));if(manager.ActiveCount!=Active.Count)throw new Exception("Active count mismatch");return string.Join("/",lengths)+";"+string.Join(",",ordered)+";"+string.Join(",",states);}
public static List<Fixture> Active=new List<Fixture>();static void Main(){string line;while((line=Console.ReadLine())!=null){Objects.Clear();Active.Clear();Trace.Clear();Root=null;var rootOwner=new Fixture(0,0);Root=rootOwner.transform;Pool=new OriginalGameObjectPool(8,Root);var manager=new OriginalParticlesManager(Pool);OriginalParticleContext.Manager=manager;var prefabs=new Dictionary<int,Fixture>{{2,new Fixture(2,-2)},{4,new Fixture(4,-4)}};var refs=new List<Fixture>();var results=new List<string>();foreach(var op in line.Split('|')){Trace.Clear();var a=op.Split(',');int x=int.Parse(a[1]);int y=a.Length>2?int.Parse(a[2]):0;string error="";int value=0;try{switch(a[0]){case "C":Pool.CreatePool(prefabs[x],y);break;case "S":var f=Pool.Spawn<Fixture>(x,y==1);refs.Add(f);value=ReferenceEquals(f,null)?-1:f.Id;break;case "R":Pool.Recycle(refs.Count==0?null:refs[x%refs.Count]);break;case "A":Pool.RecycleAllSpawnedOfPoolId(x);break;case "D":Pool.DecreasePool(x,y);break;case "G":var g=manager.GetGlobalParticle<Fixture>(x);refs.Add(g);value=ReferenceEquals(g,null)?-1:g.Id;break;case "M":manager.Clear(x==1);break;case "I":var v=refs.Count==0?null:refs[x%refs.Count];if(!ReferenceEquals(v,null))v.gameObject.activeSelf=false;break;case "K":Pool.ClearPool(x);break;}}catch(NullReferenceException){error="null";}catch(IndexOutOfRangeException){error="index";}catch(ArgumentException){error="duplicate";}catch(InvalidOperationException){error="empty";}results.Add(error+";"+value+";"+string.Join("|",Trace)+";"+Snapshot(manager));}Console.WriteLine(string.Join("\t",results));}}}'''
# Observe callback list order independently of private manager fields, without reflection.
harness=harness.replace('base.OnSpawn();','base.OnSpawn();Verify.Active.Add(this);').replace('base.OnRecycle();','base.OnRecycle();Verify.Active.Remove(this);')
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder);source=folder/'Verify.cs';source.write_text(harness);exe=folder/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),*[str(runtime/n) for n in ('OriginalGameObjectPool.cs','OriginalItemViewRecycle.cs','OriginalItemParticles.cs','OriginalParticlesManager.cs','OriginalParticleRecycleScheduler.cs')]],capture_output=True,text=True,encoding='utf-8')
    assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8')
    assert result.returncode==0,result.stdout+result.stderr
    actual=result.stdout.lstrip('\ufeff').splitlines()
    assert len(actual)==len(expected),(len(actual),len(expected))
    for i,(a,e) in enumerate(zip(actual,expected)):
        if a!=e:
            for j,(av,ev) in enumerate(zip(a.split('\t'),e.split('\t'))):
                assert av==ev,(i,j,cases[i].split('|')[j],av,ev)
report={'passed':True,'native_sequences':len(cases),'native_operations':operation_count,
 'native_outcomes':dict(outcomes),
 'original_rvas':['0x2605a00','0x2605dfc','0x2605c54','0x2d714b8','0x2d72318','0x2d71674','0x2d72514','0x2d71844','0x2d719ec','0x2d71a58','0x2d71f60','0x26912f0','0x1edaa5c','0x1edab0c','0x1edab64','0x2b48aac','0x2b48acc','0x2b48a58'],
 'scope':'Actual native pool/stack creation, prewarm top-up, activation/no-activation spawn, empty growth, recycle/duplicates, reverse manager clear, inactive particle retention, global cache, per-id recycle-all, decrease and available-only clear control flow compared with production C#. Unity allocation/object/transform/destroy, interface dispatch metadata, managed Stack/HashSet/Dictionary/List and fixture callbacks are supplied boundaries. Logging type-name text is intentionally replaced to avoid reflection. Native constructor reflection, full level registration/scheduling, Unity destruction timing, render/simulation and all-pool traversal are outside this differential test.'}
(p.PROJECT/'game_object_pool_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
