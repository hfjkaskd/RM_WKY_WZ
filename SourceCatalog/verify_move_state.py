"""Differential sequence tests of actual ARM64 move/state bodies and callbacks."""
import sys
sys.dont_write_bytecode=True
import json,os,random,subprocess,tempfile
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_useful_move.py').read_text().split('active, king,')[0],str(PROJECT/'SourceCatalog/verify_useful_move.py'),'exec'))
move,state,container,array,touch,services=[0x10000000+i*0x1000 for i in range(6)]
CHANGE,SECOND,COMPLETE=0x20000100,0x20000120,0x20000140
delegates={0x30:0x10010000,0x38:0x10010100,0x40:0x10010200}
for offset,pointer in delegates.items():put(pointer+0x18,{0x30:CHANGE,0x38:SECOND,0x40:COMPLETE}[offset],8)
metadata_next=0x10090000;allocated=0;frame=0;head=[];trace=[];fault=False;listeners={};touch_handlers=[];swap_handlers=[];complete_handlers=[]
bind_targets={0x55e3218:0x1eda8dc,0x55e3220:0x1eda914,0x55e3228:0x1eda6bc}
setters={0:0x1ee5690,1:0x1ee57bc,2:0x1edbcdc,3:0x1ede364,4:0x1edb0bc,5:0x1ee5870,6:0x1edd5f4}
def returned(uc,value=0):
    uc.reg_write(UC_ARM64_REG_X0,value&0xffffffffffffffff);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
def hook(uc,address,size,unused):
    global metadata_next,allocated,fault
    x0,x1,x2=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(3)]
    if address==0xffd960:
        if not 0x10000000<=read(x0,8)<0x10200000:
            if x0 in bind_targets:value=bind_targets[x0]
            else:
                value=metadata_next;metadata_next+=0x400
                put(value+0xe0,1);put(value+0x132,1,1);put(value+0x38,value+0x200,8);put(value+0x200,value,8);put(value+0xb8,value+0x300,8)
            put(x0,value,8)
        returned(uc)
    elif address in (0xffd90c,0xffda64,0x2a2da14):returned(uc)
    elif address==0xffda78:
        allocated+=1;value=0x10030000+allocated*0x100;emu.mem_write(value,bytes(0x100));returned(uc,value)
    elif address in (0x145d444,0x1468df8,0x145b488):
        put(x0+0x18,x2,8);put(x0+0x40,x1,8);returned(uc)
    elif address==0x260dea4:
        assert x0 in (3,15),x0;returned(uc,(state if head[0] else 0)if x0==15 else(touch if head[1] else 0))
    elif address in (0x1eda3d4,0x1eda484,0x1eda534):
        label,targets={0x1eda3d4:('t',touch_handlers),0x1eda484:('w',swap_handlers),0x1eda534:('c',complete_handlers)}[address]
        targets.append(read(x1+0x18,8));trace.append('bind,'+label);returned(uc)
    elif address in (CHANGE,SECOND,COMPLETE):
        offset={CHANGE:0x30,SECOND:0x38,COMPLETE:0x40}[address]
        for _ in range(listeners[offset]):
            if address==CHANGE:trace.append('move,'+','.join(map(str,(signed(x1&0xffffffff),signed(x2&0xffffffff),signed(read(move+0x24)),signed(read(move+0x28)),signed(read(move+0x2c))))))
            elif address==SECOND:trace.append('second,'+','.join(map(str,(signed(x1&0xffffffff),signed(read(move+0x20)),signed(read(move+0x24))))))
            else:trace.append('complete,'+','.join(map(str,(signed(x1&0xffffffff),signed(read(move+0x20)),signed(read(move+0x24))))))
            if head[3]&{CHANGE:1,SECOND:2,COMPLETE:4}[address]:fault=True;uc.reg_write(UC_ARM64_REG_PC,0x20000000);return
        returned(uc)
    elif address==0x3205d7c:returned(uc,frame)
    elif address==0x30f6400:assert x0 in (14,37),x0;returned(uc,services if head[2] else 0)
    elif address==0x2670cfc:trace.append('profile,'+str(read(state+0x18)));returned(uc)
    elif address==0x35a2030:trace.append('flow,'+str(x1)+','+str(read(state+0x18)));returned(uc)
    elif address in (0xffda88,0xffda90):fault=True;uc.reg_write(UC_ARM64_REG_PC,0x20000000)
emu.hook_add(UC_HOOK_CODE,hook)
def call(rva,*args):
    global fault
    fault=False
    for i,arg in enumerate((*args,0,0,0)):emu.reg_write(UC_ARM64_REG_X0+i,arg&0xffffffffffffffff)
    emu.reg_write(UC_ARM64_REG_SP,0x101ff000);emu.reg_write(UC_ARM64_REG_LR,0x20000000)
    try:emu.emu_start(rva,0x20000000,count=200000)
    except Exception:
        print('Native fault',hex(emu.reg_read(UC_ARM64_REG_PC)),head,trace);raise
    assert emu.reg_read(UC_ARM64_REG_PC)==0x20000000
    return signed(emu.reg_read(UC_ARM64_REG_X0)&0xffffffff)
def add_listeners():
    for offset,pointer in delegates.items():
        listeners[offset]=(listeners[offset]+1)if read(move+offset,8)else 1;put(move+offset,pointer,8)
def invoke(targets,*args):
    for target in targets:
        call(target,move,*args)
        if fault:return
cases=[];expected=[];command_count=0
def run(values,steps):
    global head,allocated,frame,command_count,fault
    head=values;allocated=0;frame=0;emu.mem_write(move,bytes(0x8000))
    put(state+0x10,container,8);put(container+0x10,array,8);put(array+0x18,28);put(container+0x18,-1)
    for targets in(touch_handlers,swap_handlers,complete_handlers):targets.clear()
    for offset in delegates:listeners[offset]=0
    add_listeners();out=[]
    for step in steps:
        command_count+=1;trace.clear();fault=False;result='-';mode,*args=step;a,b,c=(list(args)+[0,0,0])[:3]
        if mode=='b':call(0x1eda228,move)
        elif mode=='s':call(0x1eda130,move,a,b,c)
        elif mode=='m':call(0x1eda0f4,move,a)
        elif mode=='i':call(0x1eda060,move,a,b)
        elif mode=='r':call(0x1eda088,move,a)
        elif mode=='z':call(0x1eda128,move,a)
        elif mode=='d':call(0x1eda200,move)
        elif mode=='t':invoke(touch_handlers,0)
        elif mode=='w':invoke(swap_handlers,0,0,a)
        elif mode=='c':invoke(complete_handlers,a)
        elif mode=='e':call(0x1eda6a0,move,a)
        elif mode=='h':result=str(call(0x1eda0e4,move))
        elif mode=='u':result=str(call(0x1eda16c,move))
        elif mode=='q':call(0x1eda5e4,move,a)
        elif mode=='l':add_listeners()
        elif mode=='x':
            for targets in(touch_handlers,swap_handlers,complete_handlers):targets.clear()
        elif mode=='f':frame=a
        elif mode=='A':call(0x1ee1c9c,state,a)
        elif mode=='R':call(0x1edc738,state,a)
        elif mode in 'PNJK':call({'P':0x1ee5ac0,'N':0x1ee5ad0,'J':0x1ee5ae8,'K':0x1ee5af8}[mode],state)
        elif mode=='V':call(0x1ee5b14,state,a);call(0x1ee5b30,state,b)
        elif mode=='C':call(0x1ee5648,state,a)
        elif mode=='H':call(0x1ee5974,state)
        elif mode=='B':call(0x1ee5738,state)
        elif mode=='S':call(setters[a],state)
        if fault:result='error'
        snapshot=[*[signed(read(move+offset))for offset in(0x20,0x24,0x28,0x2c)],read(state+0x18),signed(read(state+0x1c)),call(0x1ee5b08,state),read(state+0x24,1),read(state+0x25,1),signed(read(state+0x28)),signed(read(array+0x20+6*4)),signed(read(array+0x20+2*4)),call(0x1ee5b3c,state)]
        out.append(result+':'+','.join(map(str,snapshot))+':'+ ';'.join(trace))
    cases.append(','.join(map(str,head))+'#'+';'.join(','.join(map(str,step))for step in steps));expected.append('|'.join(out))

edges=(-2147483648,-1,0,1,3,30,2147483647)
for flags in range(8):
    for budget in edges:
        for game_state in range(7):
            run([flags&1,(flags>>1)&1,(flags>>2)&1,0],[('b',),('s',budget,budget,budget),('S',game_state),('u',),('A',6),('u',),('R',6),('t',),('w',0),('w',1),('c',13),('e',0),('z',budget),('u',),('d',),('h',),('q',0),('e',7),('q',1),('t',),('l',),('w',1),('c',10)])
for throws in range(8):
    run([1,1,1,throws],[('b',),('s',30,30,0),('t',),('w',1),('c',1),('d',),('q',1),('t',),('l',),('b',),('w',1),('e',9)])
rng=random.Random(0x1eda130)
for seed in range(220):
    steps=[('b',),('m',rng.choice(edges))]
    for i in range(55):
        mode=rng.choice(('s','i','r','z','d','t','w','c','e','h','u','q','l','x','b','f','A','R','P','N','J','K','V','C','H','B','S'))
        if mode=='s':step=(mode,*[rng.choice(edges)for _ in range(3)])
        elif mode=='i':step=(mode,rng.choice(edges),rng.randrange(5))
        elif mode in('A','R'):step=(mode,rng.choice((2,6)))
        elif mode=='S':step=(mode,rng.randrange(7))
        elif mode=='V':step=(mode,rng.randrange(2),rng.randrange(2))
        elif mode in('q','C','w'):step=(mode,rng.randrange(2))
        elif mode=='f':step=(mode,rng.choice(edges))
        else:step=(mode,rng.choice(edges))
        steps.append(step)
    run([rng.randrange(2),rng.randrange(2),rng.randrange(2),rng.randrange(8)],steps)
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-move-state-')as directory:
    exe=Path(directory)/'verify.exe'
    names=('OriginalMoveManager','OriginalGameStateManager','OriginalGameOperations')
    compiled=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(PROJECT/'SourceCatalog/MoveStateHarness.cs'),*[str(PROJECT/'Assets/Foundation/Runtime'/(n+'.cs'))for n in names]],capture_output=True,text=True)
    assert compiled.returncode==0,compiled.stdout+compiled.stderr
    completed=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8')
    assert completed.returncode==0,completed.stdout+completed.stderr
    actual=completed.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
    for i,(a,e)in enumerate(zip(actual,expected)):assert a==e,(i,cases[i],a,e)
report={'passed':True,'native_sequences':len(cases),'native_commands':command_count,'scope':'Actual ARM64 MoveManager Bind registration and original bound tap/swap/completion callback bodies, move/time initialization/spending/reset/query/increment/events, eligibility state/operation-6 gate and Clear lifetime. Actual GameState state setters, StartLevel profiling and quit-flow order, Home activation clear, owned operation Clear, special/shifter sum and retained shifting flags, game-stable frame boundary. Compares per-command storage, eligibility and callback traces including integer overflow, signed budgets, negative timed-level conventions, repeated bindings, retained touch subscriptions after exit Clear, and handler exceptions after committed state. Context lookup, metadata/allocation/delegate storage/registration, application profiling/flow services and frame getter are supplied boundaries; diagnostic state logging is omitted. Full application/Home bind lifecycle, active-level adjusted/saved start provider, King analysis/UI and complete player resolution remain pending.'}
(PROJECT/'move_state_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report))
