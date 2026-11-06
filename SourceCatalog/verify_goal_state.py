"""Execute original ARM64 goal state/counting and ordered claim queries vs C#."""
import sys
sys.dont_write_bytecode = True
import verify_board_layout as b
import json, random, subprocess, tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import *
p = b.p
manager, board, claims, goal_list, goal_storage, prereq, flying, claim_list, claim_storage, klass = [0x10010000+i*0x1000 for i in range(10)]
MODEL, UI, ACTIVE, WANT, AWAIT, OUTSTANDING, CLAIM, COLLECT = [0x20000100+i*4 for i in range(8)]
RESTORABLE, RESTORE = 0x20000120, 0x20000124
types = (11, 1, 13, 14, 21, 30, 54, 29)
tuple_base, condition_base, claimer_base, info_base = 0x10030000, 0x10040000, 0x10054000, 0x10068000
trace = []; fault = False; current = 0; mask = 0; condition_goals = 0; tuple_mode = False
claim_data = []; dictionaries = {}; condition_types = {}
signed = lambda x: p.signed(x & 0xffffffff)
def returned(uc, value):
    uc.reg_write(UC_ARM64_REG_X0, value & 0xffffffffffffffff)
    uc.reg_write(UC_ARM64_REG_PC, uc.reg_read(UC_ARM64_REG_LR))
def fail(uc):
    global fault
    fault = True; uc.reg_write(UC_ARM64_REG_PC, 0x20000000)
def boundary(uc, address, size, unused):
    x0, x1, x2, x3, x4, x5 = [uc.reg_read(UC_ARM64_REG_X0+i) for i in range(6)]
    result = None; key = signed(x1)
    if address in (0xffda88, 0xffda90): fail(uc); return
    if address == 0x32123f0: result = board if current != -1 else 0
    elif address == 0x2c1b9a0:
        if key < 0 or key >= p.read(x0+0x18): fail(uc); return
        result = p.read(p.read(x0+0x10, 8)+0x20+key*8, 8)
    elif address == 0x2c1c018:
        result = int(x1 in [p.read(p.read(x0+0x10, 8)+0x20+i*8, 8) for i in range(p.read(x0+0x18))])
    elif address == 0x2c1d0e4:
        arr = p.read(x0+0x10, 8); values = [p.read(arr+0x20+i*8, 8) for i in range(p.read(x0+0x18))]
        result = int(x1 in values)
        if result: values.remove(x1)
        p.put(x0+0x18, len(values))
        for i, value in enumerate(values): p.put(arr+0x20+i*8, value, 8)
    elif address in (0x3c96128, 0x3c961c0, 0x3c97c34, 0x3c97664, 0x3c96364, 0x3c95e00):
        if x0 not in dictionaries: raise AssertionError(('dictionary', hex(address), hex(x0), hex(uc.reg_read(UC_ARM64_REG_LR)), row))
        values = dictionaries[x0]
        if address == 0x3c963d0: result = int(key in values)
        elif address == 0x3c96128:
            if key not in values: fail(uc); return
            result = values[key]
        elif address in (0x3c961c0, 0x3c961d8): values[key] = signed(x2); result = 0
        elif address == 0x3c97c34: p.put(x2, values.get(key, 0)); result = int(key in values)
        elif address == 0x3c97664: result = int(key in values); values.pop(key, None)
        elif address == 0x3c96364: values.clear(); result = 0
        else: result = len(values)
    elif address == 0x1eda1e8: result = int(bool(mask & (1 << key)))
    elif tuple_mode and address == 0x32149fc: result = int(bool(condition_goals & (1 if key == 54 else 2)))
    elif address == 0xfcd934:
        slot = signed(x2)
        if x0 in condition_types:
            target = {13: 0x2ee8460, 14: 0x2ee849c, 21: 0x2ee844c, 30: 0x2ee84ec}.get(condition_types[x0], 0x2ee8454)
        else:
            assert slot in (0,1,2,4,7,8,9,10),('dispatch',hex(x0),hex(x1),slot,hex(uc.reg_read(UC_ARM64_REG_LR)))
            target = {0: ACTIVE, 1: WANT, 2: AWAIT, 4: RESTORABLE, 7: OUTSTANDING, 8: CLAIM, 9: RESTORE, 10: COLLECT}[slot]
        result = info_base + slot*16
        p.put(result, target, 8); p.put(result+8, 0, 8)
    elif address in (ACTIVE, WANT, AWAIT, OUTSTANDING, CLAIM, COLLECT, RESTORABLE, RESTORE):
        ident = (x0-claimer_base)//0x200
        active, want, await_, outstanding = claim_data[ident]
        if address == ACTIVE: result = active
        elif address in (WANT, RESTORABLE): result = int(key == want)
        elif address == AWAIT: result = int(key == await_)
        elif address == OUTSTANDING: result = outstanding if key == want else 0
        else: trace.append({CLAIM:'claim:',COLLECT:'collect:',RESTORE:'restore:'}[address]+str(ident)+':'+str(key)); result = 0
    elif address == MODEL: trace.append('model:'+str(key)+':'+str(signed(x2))); result = 0
    elif address == UI:
        trace.append('ui:'+str(key)+':'+str(signed(x2))+':'+''.join(str(int(bool(v))) for v in (x3,x4,x5))); result = 0
    elif address == 0xffda6c: result = p.allocate()
    elif address == 0x2a2da14: result = 0
    if result is not None: returned(uc, result)
p.emu.hook_add(UC_HOOK_CODE, boundary)

def call(rva, *args):
    global fault
    fault = False
    for i, value in enumerate((*args, 0, 0, 0)): p.emu.reg_write(UC_ARM64_REG_X0+i, value & 0xffffffffffffffff)
    p.emu.reg_write(UC_ARM64_REG_SP, 0x101ff000); p.emu.reg_write(UC_ARM64_REG_LR, 0x20000000)
    try: p.emu.emu_start(rva, 0x20000000, count=300000)
    except Exception: print('Native failure', hex(p.emu.reg_read(UC_ARM64_REG_PC)), row, trace); raise
    assert p.emu.reg_read(UC_ARM64_REG_PC) == 0x20000000, hex(p.emu.reg_read(UC_ARM64_REG_PC))
    return signed(p.emu.reg_read(UC_ARM64_REG_X0))
def tuple_at(index): return tuple_base + index*0x100
def setup_tuple(index, goal_type, target, claim=0):
    owner = tuple_at(index); condition = condition_base+index*0x100
    p.emu.mem_write(owner, bytes(0x80)); p.emu.mem_write(condition, bytes(0x80))
    for offset, value in ((0x10, goal_type), (0x14,target), (0x18,target), (0x1c,target), (0x24,claim)): p.put(owner+offset,value)
    p.put(owner+0x28,condition,8); p.put(condition,klass,8); p.put(condition+0x10,board,8); p.put(condition+0x18,manager,8)
    condition_types[condition] = goal_type
    return owner
def state(owner): return ','.join(str(signed(p.read(owner+o))) for o in (0x14,0x18,0x1c,0x24))
def flags(owner): return ''.join(str(call(rva,owner)) for rva in (0x2ee80d4,0x2ee80f8,0x2ee81a0,0x2ee81d4))
def list_(owner, storage, values):
    p.emu.mem_write(owner, bytes(0x30)); p.emu.mem_write(storage,bytes(0x180))
    p.put(owner+0x10,storage,8); p.put(owner+0x18,len(values)); p.put(storage+0x18,32)
    for i,value in enumerate(values):p.put(storage+0x20+i*8,value,8)
def setup_manager(boards, selected, events, seed):
    global current, dictionaries, claim_data
    p.emu.mem_write(manager, bytes(0x60)); p.emu.mem_write(klass,bytes(0x200))
    current=selected; p.put(board+0x3c,current)
    goal_arrays=[]; limit_arrays=[]
    for bid in range(boards):
        arr=0x10020000+bid*0x200; p.emu.mem_write(arr,bytes(0x180)); p.put(arr+0x18,len(types)); goal_arrays.append(arr)
        for i,kind in enumerate(types):p.put(arr+0x20+i*8,setup_tuple(bid*len(types)+i,kind,(seed+bid*3+i)%5,(seed+i)%3),8)
        la=0x10022000+bid*0x100; lt=0x10023000+bid*0x100
        p.emu.mem_write(la,bytes(0x80)); p.put(la+0x18,1);p.put(la+0x20,lt,8);p.put(lt+0x10,11);p.put(lt+0x14,bid+seed);limit_arrays.append(la)
    list_(goal_list,goal_storage,goal_arrays);list_(0x10025000,0x10026000,limit_arrays)
    p.put(manager+0x18,goal_list,8);p.put(manager+0x20,0x10025000,8)
    p.put(manager+0x38,prereq,8);p.put(manager+0x40,flying,8);p.put(manager+0x48,claims,8)
    dictionaries={prereq:{},flying:{kind:0 for kind in types}};p.dictionaries=dictionaries
    model_event, ui_event=0x10027000,0x10028000
    for event,target in ((model_event,MODEL),(ui_event,UI)):
        p.emu.mem_write(event,bytes(0x60));p.put(event+0x18,target,8)
    p.put(manager+0x28,model_event if events&1 else 0,8);p.put(manager+0x30,ui_event if events&2 else 0,8)
    claim_data=[((seed+i)%2,types[(seed+i)%len(types)],types[(seed+i*2)%len(types)],(seed+i)%4) for i in range(3)]
    list_(claim_list,claim_storage,[]);p.put(claims+0x18,claim_list,8)
    for i in range(3):
        obj=claimer_base+i*0x200;p.emu.mem_write(obj,bytes(0x80));p.put(obj,klass,8)
        if seed&(1<<i):call(0x2ee75d0,claims,obj);call(0x2ee75d0,claims,obj)

rng=random.Random(0x32136cc);cases=[];expected=[];row='';tuple_calls=manager_calls=0
for n in range(180):
    tuple_mode=True;condition_types.clear();trace.clear();p.heap=0x10080000
    kind=(list(range(-2,108))+[-2147483648,2147483647])[n%112]
    target=(-2147483648,-1,0,1,2,36,2147483647)[n%7];mask=(0,1<<12,1<<16,(1<<12)|(1<<16))[n%4];condition_goals=n%4
    owner=setup_tuple(0,kind,target);commands=[];values=[];row='tuple '+str(n)
    operations=[('d',0),('u',0),('i',0),('j',0),('a',n%5-2),('r',0),('s',target),('t',target),('z',0)]
    operations += [(rng.choice('duijrsat'),rng.choice((-2147483648,-2,0,1,17,2147483647))) for _ in range(15)]
    for op,arg in operations:
        rva={'d':0x2ee8070,'u':0x2ee8088,'i':0x2ee80a0,'j':0x2ee80b4,'r':0x2ee80c8,'s':0x2ee83e8,'a':0x2ee83f4,'t':0x2ee83c4,'z':0x2ee8408}[op]
        value=call(rva,owner,arg);assert not fault
        values.append((str(value) if op in 'duijt' else '-')+':'+state(owner)+':'+flags(owner));commands.append(op+','+str(arg));tuple_calls+=5
    cases.append('T#'+','.join(map(str,(kind,target,n%2,mask,condition_goals)))+'#'+';'.join(commands));expected.append('|'.join(values))

mutation_rvas={'d':0x32136cc,'u':0x3213954,'i':0x3213fec,'j':0x3214158,'f':0x3214de8,'a':0x321309c,'r':0x3213170,'c':0x3212fe8,'v':0x3213db8,'s':0x321357c,'t':0x3213f08}
for n in range(180):
    tuple_mode=False;condition_types.clear();trace.clear();p.heap=0x10080000
    boards=1+n%3;selected=n%boards;events=n%4;mask=(n%4)<<12;seed=n%19
    setup_manager(boards,selected,events,seed);commands=[];values=[];row='manager '+str(n)
    operations=[('q',11),('g',11),('a',11),('a',11),('r',11),('f',11),('d',11),('g',11),('u',11),('v',11),('s',0)]
    operations += [(rng.choice('duijfarvsgq'),rng.choice(types+(99,))) for _ in range(30)]
    operations += [('x',0),('k',0),('t',11,-4),('b',0),('q',11),('g',11),('c',0),('b',-1),('q',11),('g',11)]
    for command in operations:
        op,arg=command[:2];value='-';trace.clear();row=(n,command)
        if op=='b':current=arg;p.put(board+0x3c,arg)
        elif op in 'xk':call(0x2ee76b8 if op=='x' else 0x2ee75d0,claims,claimer_base+arg*0x200)
        elif op in mutation_rvas:
            call(mutation_rvas[op],manager,arg,*command[2:]);manager_calls+=1
            if fault:value='E'
        else:
            queries=[(0x3213494,(manager,arg,0)),(0x3213494,(manager,arg,1)),(0x32142a0,(manager,arg)),(0x3214378,(manager,0,arg)),(0x32146cc,(manager,arg,1)),(0x32145d8,(manager,arg)),(0x3214504,(manager,0,arg))] if op=='q' else [(0x32146cc,(manager,arg,0)),(0x3214794,(manager,arg)),(0x32133e4,(manager,arg)),(0x32149fc,(manager,arg)),(0x3214988,(manager,)),(0x3214840,(manager,)),(0x32154fc,(manager,arg)),(0x3214ea4,(manager,arg)),(0x3213d54,(manager,arg))]
            probes=[]
            for rva,args in queries:
                probes.append(call(rva,*args));manager_calls+=1
                if fault:break
            value='E' if fault else ','.join(map(str,probes))
        values.append(value+':'+ '/'.join(state(tuple_at(i)) for i in range(boards*len(types)))+':'+ '/'.join(trace))
        commands.append(','.join(map(str,command)))
    cases.append('M#'+','.join(map(str,(boards,selected,events,mask,seed)))+'#'+';'.join(commands));expected.append('|'.join(values))

with tempfile.TemporaryDirectory() as directory:
    directory=Path(directory);exe=directory/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
    compile_=subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(p.PROJECT/'SourceCatalog/GoalStateHarness.cs'),*[str(runtime/(name+'.cs')) for name in ('OriginalGoalTuple','OriginalGoalClaims','OriginalGoalManager','OriginalGameOperations')]],capture_output=True,text=True,encoding='utf-8')
    assert compile_.returncode==0,compile_.stdout+compile_.stderr
    run=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8')
    assert run.returncode==0,run.stdout+run.stderr
    actual=run.stdout.lstrip('\ufeff').splitlines()
    for i,(managed,native) in enumerate(zip(actual,expected)):
        if managed!=native:
            a=managed.split('|');e=native.split('|');j=next((j for j,(x,y) in enumerate(zip(a,e)) if x!=y),0)
            raise AssertionError((i,cases[i],j,a[j],e[j]))
    assert len(actual)==len(expected)
report={'passed':True,'native_tuple_sequences':180,'native_manager_sequences':180,'native_tuple_method_calls':tuple_calls,'native_manager_method_calls':manager_calls,'scope':'Original ARM64 tuple arithmetic, model/UI count separation, reset/claim/target flags and all five goal conditions, manager lookup/count/display/mutation/notifications, prerequisite/flying tracking and completion checks across one to three boards; actual ordered native claimer selection/outstanding/registration executes. Managed storage, current-board/operation context, claimer callbacks, delegate dispatch, logging/allocation and interface dispatch are supplied boundaries. Manager initialization is exercised by compiled C# but native initialization is not compared; tuple fields are seeded to native constructor state. Goal calculation, trails, HUD rendering, production-board binding and settlement remain pending.'}
(p.PROJECT/'goal_state_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
