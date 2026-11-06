"""Compare real ARM64 main FlowExecuter queue and callbacks with the runtime port.

Typed action virtuals/delegates, CLR type equality, LinkedList storage and the
home auto-delay owner are boundaries. Native Push/Add/replace/remove/advance,
completion, counters, next-mode, clear and priority instructions execute.
Diagnostic-only formatting is skipped. Full application/subflow composition and
rendered UiDialog ownership are outside this check.
"""
from pathlib import Path
import hashlib,json,random,subprocess,tempfile
PROJECT=Path(__file__).resolve().parents[1];SCOPE=__doc__.strip()
source=PROJECT/'SourceCatalog/verify_low_match.py'
exec(compile(source.read_text().split('cases=[];expected=[]')[0],str(source),'exec'))
owner,main,auto,next_list,next_mode,pause,on_push,flow,delegate=[0x10200000+i*0x1000 for i in range(9)]
actions=[0x10210000+i*0x1000 for i in range(8)];classes=[0x10220000+i*0x1000 for i in range(4)]
lists={};nodes={};next_node=0x10230000;configs=[];delay=False;trace=[]
def action_id(pointer):return actions.index(pointer)
def sync_list(pointer):
    values=lists[pointer];put(pointer+0x10,values[0]if values else 0);put(pointer+0x18,len(values),4)
def node_for(value):
    global next_node
    result=next_node;next_node+=0x40;put(result+0x28,value);nodes[result]=0;return result
def native_complete(pointer):
    trace.append('complete,'+str(action_id(pointer)));u.reg_write(r.UC_ARM64_REG_PC,0x359fd84)
def hook(uc,address,size,unused):
    a,b,c=x(0),x(1),x(2)
    # All omitted ranges are native diagnostics only. Preserve live list/node
    # registers assigned inside those ranges before rejoining native code.
    if address==0x35a0a68:
        u.reg_write(r.UC_ARM64_REG_X20,read(x(21)+0x20,8));u.reg_write(r.UC_ARM64_REG_PC,0x35a0b74);return
    if address in(0x35a0708,0x35a08e4):u.reg_write(r.UC_ARM64_REG_PC,0x35a07d4);return
    if address==0x35a0e78:u.reg_write(r.UC_ARM64_REG_PC,0x35a0ef4);return
    if address==0x35a106c:
        u.reg_write(r.UC_ARM64_REG_X25,x(20)+0x28);u.reg_write(r.UC_ARM64_REG_X20,1);u.reg_write(r.UC_ARM64_REG_PC,0x35a1118);return
    if address==0x35a18ac:u.reg_write(r.UC_ARM64_REG_PC,0x35a1938);return
    if address==0x35a17a0:u.reg_write(r.UC_ARM64_REG_PC,0x35a17fc);return
    if address==0x2310854:returned(read(a,8));return
    if address==0x261dca0:returned(a==b);return
    if address==0xffda78:returned(delegate);return
    if address==0x2044a78:
        put(a+0x18,0x35a1398);put(a+0x40,b);return returned()
    if address==0x359fb14:put(a+0x10,b);return returned()
    if address==0x359fbb0:put(a+0x10,0);return returned()
    if address==0x30f6400:assert a==14,a;returned(flow);return
    if address==0x35a1170:returned(configs[action_id(b)][6]);return
    if address==0x39e3988:returned(delay);return
    if address==0x35a11d0:trace.append('delay,'+str(action_id(b)));return returned()
    if address==0x20000400:trace.append('replaced,'+str(action_id(a)));return returned()
    if 0x20000500<=address<=0x20000570:
        index=action_id(a);config=configs[index]
        if address==0x20000500:returned(config[1]);return
        if address==0x20000510:returned(config[2]);return
        if address==0x20000520:returned(config[3]);return
        if address==0x20000530:
            trace.append('execute,'+str(index))
            if config[4]:native_complete(a);return
        elif address==0x20000540:
            trace.append('cancel,'+str(index)+','+str(action_id(b)))
            if config[5]:native_complete(a);return
        elif address==0x20000550:native_complete(a);return
        elif address==0x20000560:trace.extend(('replace,'+str(index)+','+str(action_id(b)),'replaced,'+str(index)))
        elif address==0x20000570:trace.append('clear,'+str(index))
        return returned()
    if address in(0x2b60ff4,0x2b61160):
        node=node_for(b);nodes[node]=a
        if address==0x2b60ff4:lists[a].insert(0,node)
        else:lists[a].append(node)
        sync_list(a);returned(node);return
    if address==0x2b610c0:
        nodes[b]=a;lists[a].insert(0,b);sync_list(a);return returned()
    if address in(0x2b61704,0x2b61778,0x2b616b8):
        node=lists[a].pop(0 if address==0x2b61704 else -1)if address!=0x2b616b8 else b
        if address==0x2b616b8:lists[a].remove(node)
        nodes[node]=0;sync_list(a);return returned()
    if address==0x2b60eec:returned(lists[a][-1]if lists[a]else 0);return
    if address==0x2b5e2b4:
        values=lists[nodes[a]];index=values.index(a)+1;returned(values[index]if index<len(values)else 0);return
    if address==0x2b6129c:
        for node in lists[a]:nodes[node]=0
        lists[a].clear();sync_list(a);return returned()
    if address==0x2b615d8:
        output=x(8);u.mem_write(output,bytes(0x28));put(output,a);return returned()
    if address==0x2b353b4:
        values=lists[read(a,8)];index=read(a+8)
        if index<len(values):put(a+0x18,read(values[index]+0x28,8));put(a+8,index+1,4);returned(1)
        else:returned(0)
        return
    if address==0x2b35548:return returned()
    raise AssertionError(hex(address))
addresses=(0x35a0a68,0x35a0708,0x35a08e4,0x35a0e78,0x35a106c,0x35a18ac,0x35a17a0,0x2310854,0x261dca0,0xffda78,0x2044a78,0x359fb14,0x359fbb0,0x30f6400,0x35a1170,0x39e3988,0x35a11d0,0x20000400,
    *range(0x20000500,0x20000580,0x10),0x2b60ff4,0x2b61160,0x2b610c0,0x2b61704,0x2b61778,0x2b616b8,0x2b60eec,0x2b5e2b4,0x2b6129c,0x2b615d8,0x2b353b4,0x2b35548)
for address in addresses:u.hook_add(UC_HOOK_CODE,hook,begin=address,end=address)
def setup():
    global lists,nodes,next_node,delay
    lists={main:[],auto:[],next_list:[]};nodes={};next_node=0x10230000;delay=False
    for pointer in(owner,main,auto,next_list,next_mode,pause,on_push,flow,delegate,*actions,*classes):u.mem_write(pointer,bytes(0x300))
    for offset,pointer in((0x10,main),(0x18,auto),(0x20,next_list),(0x28,next_mode),(0x30,pause),(0x38,on_push)):put(owner+offset,pointer)
    for index,pointer in enumerate(actions):
        put(pointer,classes[configs[index][0]]);event=pointer+0x200;put(pointer+0x18,event);put(event+0x18,0x20000400);put(event+0x40,pointer)
    for pointer in classes:
        for offset,target in((0x178,0x20000500),(0x1d8,0x20000510),(0x1e8,0x20000520),(0x198,0x20000530),(0x1a8,0x20000540),(0x1b8,0x20000550),(0x1c8,0x20000560),(0x1f8,0x20000570)):put(pointer+offset,target)
cases=[];expected=[];rng=random.Random(0x35a0934)
for number in range(120):
    configs=[[i%4,rng.randrange(4),rng.randrange(2),rng.randrange(2),rng.randrange(5)==0,rng.randrange(2),rng.randrange(5)==0]for i in range(8)]
    operations=[(3,0),(0,0),(0,1),(0,4),(4,0),(1,0),(0,2),(0,3),(2,0),(10,1),(0,7),(2,0),(7,0),(0,5),(7,0),(0,6),(8,0),(8,0),(2,0)]
    for _ in range(60):
        command=rng.choices(range(11),(9,3,5,2,2,1,1,2,2,2,2))[0];operations.append((command,rng.randrange(8)if command==0 else rng.randrange(2)))
    setup();trace=[]
    for command,value in operations:
        address={0:0x35a0934,1:0x35a05b0,2:0x20000550,3:0x35a16f0,4:0x35a170c,5:0x35a1744,6:0x35a1728,7:0x35a1760,8:0x35a1828,9:0x35a03c4}.get(command)
        if command==10:delay=bool(value)
        elif command==2:
            pointer=read(owner+0x40,8)
            if pointer:call(address,pointer)
        else:
            try:call(address,owner,actions[value]if command==0 else value)
            except Exception as error:raise RuntimeError((number,command,value,hex(u.reg_read(r.UC_ARM64_REG_PC)),trace[-8:]))from error
        assert not fault,(number,command,value,trace)
        current=read(owner+0x40,8)
        trace.append('state,'+','.join(map(str,(int(si(read(pause+0x10))>0),action_id(current)if current else-1,len(lists[main]),len(lists[auto]),len(lists[next_list])))))
    cases.append(';'.join(','.join(str(int(item))for item in config)for config in configs)+'|'+(';'.join(str(a)+','+str(b)for a,b in operations)))
    expected.append(';'.join(trace))
mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
sources=['SourceCatalog/FlowExecutorHarness.cs','Assets/Foundation/Runtime/OriginalFlowExecutor.cs']
with tempfile.TemporaryDirectory()as directory:
    exe=Path(directory)/'FlowExecutor.exe';args=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',
        *['-r:'+str(mono/'lib/mono/4.5'/p)for p in('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),*[str(PROJECT/p)for p in sources]]
    result=subprocess.run(args,capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    actual=result.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
    for i,(a,b)in enumerate(zip(actual,expected)):
        if a!=b:
            aa=a.split(';');bb=b.split(';');different=next(j for j in range(min(len(aa),len(bb)))if aa[j]!=bb[j])if aa[:min(len(aa),len(bb))]!=bb[:min(len(aa),len(bb))]else min(len(aa),len(bb))
            raise AssertionError((i,different,aa[max(0,different-5):different+5],bb[max(0,different-5):different+5],cases[i]))
report={'passed':True,'scenarios':len(cases),'operations':len(cases)*len(operations),'scope':SCOPE,
    'original_rvas':['0x35a0934','0x35a09b0','0x35a0d48','0x35a0f4c','0x35a05b4','0x35a1398','0x35a03c4','0x35a16f0','0x35a170c','0x35a1728','0x35a1744','0x35a1760','0x35a1828'],
    'files':[{'path':p,'sha256':hashlib.sha256((PROJECT/p).read_bytes()).hexdigest()}for p in sources]}
(PROJECT/'flow_executor_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
