"""Native match-special creation, wait and particle-transfer control flow."""
import sys
sys.dont_write_bytecode=True
import verify_board_layout as b
import json, random, struct, subprocess, tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import *
p=b.p
creator,group,model,view,model_class,view_class,factory,item_creator,state_manager,session,root,user_data,animation,transform,front,back,snapshot,mediator=[0x10010000+i*0x1000 for i in range(18)]
GET_VIEW,GET_TYPE,SORT,PLAY,UNDER,BRIDGE=[0x20000100+i*4 for i in range(6)]
cell=lambda i:0x10030000+i*0x100
cell_id=lambda pointer:(pointer-cell(0))//0x100
bits=lambda f:struct.unpack('<I',struct.pack('<f',f))[0]
number=lambda n:struct.unpack('<f',struct.pack('<I',n))[0]
normal=lambda n:'nan' if number(n)!=number(n) else str(n)
trace=[];freezes=[];current_cell=0;gets=missing_view=0;active_view=1;playing=started=auto=factory_null=honey=roof=remove_on_animation=spread=difficulty=0
item_type=3;iterator=wait=bridge_lr=0;fault='';delta=level_time=0

def resolve(slot):
    if slot not in b.resolved_slots:
        p.emu.reg_write(UC_ARM64_REG_X0,slot);b.boundary(p.emu,0xffd960,4,None)
    return p.read(slot,8)

def boundary(uc,address,size,unused):
    global iterator,wait,bridge_lr,fault,gets,active_view,current_cell,item_type
    x0,x1,x2,x3,x4,x5=[uc.reg_read(r) for r in (UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_X3,UC_ARM64_REG_X4,UC_ARM64_REG_X5)]
    value=None
    if address in (0xd00b50,0xd018a0):uc.mem_write(x0,bytes(uc.mem_read(x1,x2)));value=x0
    elif address==0xd01fa0:uc.mem_write(x0,bytes([x1&255])*x2);value=x0
    elif address==0x23f0154:
        trace.append('create:'+str(x1)+':'+str(cell_id(x2)))
        item_type={10:3,20:2,30:2,40:4,50:5}[x1]
        current_cell=x2;value=0 if factory_null else model
    elif address==GET_VIEW:
        gets+=1;value=0 if gets==missing_view else view
    elif address==GET_TYPE:value=item_type
    elif address==0x3e473b8:value=view
    elif address==0x3e4737c:value=transform
    elif address==0x3e49b30:active_view=x1;trace.append('active:'+str(x1));value=0
    elif address==0x31933d4:i=cell_id(x0);freezes[i]+=1;trace.append('freeze:'+str(i));value=0
    elif address==0x31932f8:i=cell_id(x0);freezes[i]-=1;trace.append('unfreeze:'+str(i));value=0
    elif address==0x1e2aaac:
        iterator=x0;trace.append('start');bridge_lr=uc.reg_read(UC_ARM64_REG_LR)
        uc.reg_write(UC_ARM64_REG_LR,BRIDGE);uc.reg_write(UC_ARM64_REG_PC,0x1c92df8);return
    elif address==BRIDGE:
        trace.append('initial:'+str(x0)+':'+('w' if p.read(iterator+0x18,8)==wait else '-'))
        uc.reg_write(UC_ARM64_REG_LR,bridge_lr);value=0
    elif address==0x12de578:wait=x0;trace.append('wait:'+normal(uc.reg_read(UC_ARM64_REG_S0)))
    elif address==0x3e495b0:value=0
    elif address==0x1eda1d8:value=playing
    elif address==0x32b65d8:value=started
    elif address==0x27b2a68:trace.append('increment:'+str(x0)+':'+str(x1));value=0
    elif address==0x3191dd4:
        i=cell_id(x0);trace.append('position:'+str(i))
        for r,v in zip((UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2),(float(i),float(i+2),0.)):uc.reg_write(r,bits(v))
        value=0
    elif address==0x32b982c:trace.append('record:'+str(x2)+':'+str(x3)+':'+str(x4));value=0
    elif address==0x32b9ab0:trace.append('madness:'+str(x1)+':'+str(x2)+':'+str(x3)+':'+str(x5));value=0
    elif address==0x2ae7458:value=animation
    elif address==0x338b92c:trace.append('mission:'+str(x1)+':'+str((x2&255)-256 if x2&128 else x2&255)+':'+str(x3));value=0
    elif address==0x32ba8b4:trace.append('useful');value=0
    elif address in (0x2b45df4,0x2b45968):value=current_cell
    elif address==SORT:trace.append('sort:'+str(p.signed(x1&0xffffffff))+':'+str(p.signed(x1>>32))+':'+str(x2));value=0
    elif address==PLAY:
        trace.append('animation:'+normal(uc.reg_read(UC_ARM64_REG_S0))+':'+str(x1))
        if remove_on_animation:current_cell=0
        value=0
    elif address==UNDER:trace.append('under:'+str(x1));value=0
    elif address==0x27b4744c:raise AssertionError('Wrong address')
    elif address==0x2b4744c:trace.append('honey');value=honey
    elif address==0x2b474d8:trace.append('roof');value=roof
    elif address==0x319e620:trace.append('above:'+str(cell_id(x0)));value=int(spread>0)
    elif address==0x1c92b04:trace.append('appear');value=front
    elif address==0x27b4f1c and iterator:
        trace.append('particles:'+str(x2));value=0
    elif address==0x2a2e598:trace.append('missing-cell');value=0
    elif address==0x3205e3c:trace.append('time');uc.reg_write(UC_ARM64_REG_S0,level_time);value=0
    elif address==0x32037e4:uc.reg_write(UC_ARM64_REG_S0,delta);value=0
    elif address==0x3e3b754:value=int(x0==x1)
    elif address==0x2b51474:trace.append('snapshot');value=snapshot
    elif address==0x260a220:trace.append('spawn:'+str(x1)+':'+str(x2));value=back
    elif address==0x21aaab0:trace.append('back-init');value=0
    elif address==0x21aab84:trace.append('back-play');value=0
    elif address==0x2b511c0:trace.append('clear:'+str(x2)+':'+str(x3));value=0
    elif address==0xffda88:
        fault='null';uc.reg_write(UC_ARM64_REG_PC,0x20000000);return
    elif address==0xffde20:
        fault='cast';uc.reg_write(UC_ARM64_REG_PC,0x20000000);return
    if value is not None:
        uc.reg_write(UC_ARM64_REG_X0,value&0xffffffffffffffff);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,boundary)

def call(rva,*args):
    for i,v in enumerate(args):p.emu.reg_write(UC_ARM64_REG_X0+i,v&0xffffffffffffffff)
    p.emu.reg_write(UC_ARM64_REG_SP,0x101ff000);p.emu.reg_write(UC_ARM64_REG_LR,0x20000000)
    p.emu.emu_start(rva,0x20000000,count=200000)
    assert p.emu.reg_read(UC_ARM64_REG_PC)==0x20000000,hex(p.emu.reg_read(UC_ARM64_REG_PC))
    return p.emu.reg_read(UC_ARM64_REG_X0)

def snapshot_state(value):
    return ','.join(map(str,[value,p.read(model+0xb1,1),active_view,gets,':'.join(map(str,freezes)),fault]))+';'+'|'.join(trace)

def base_setup(layer):
    p.emu.mem_write(creator,bytes(0x30000));p.heap=0x10100000
    p.put(model,model_class,8);p.put(view,view_class,8);p.put(model+0x38,mediator,8);p.put(model+0xb1,1,1)
    p.put(model_class+0x418,GET_VIEW,8);p.put(model_class+0x378,GET_TYPE,8)
    p.put(view_class+0x208,SORT,8);p.put(view_class+0x388,PLAY,8);p.put(view_class+0x308,UNDER,8)
    for slot,klass,inherits in [(0x5594190,model_class,0x10044000),(0x5594198,view_class,0x10045000)]:
        target=resolve(slot);p.put(target+0x12c,1,1);p.put(klass+0x12c,2,1);p.put(klass+0xc8,inherits,8);p.put(inherits,target,8)
    statics=0x10046000;p.put(resolve(0x55940a8)+0xb8,statics,8);p.put(statics+8,layer)
    p.put(creator+0x18,factory,8);p.put(factory+0x38,item_creator,8);p.put(creator+0x30,state_manager,8)
    statics=0x10047000;p.put(resolve(0x5595c00)+0xb8,statics,8);p.put(statics,root,8);p.put(root+0x38,user_data,8);p.put(user_data+0x18,session,8);p.put(session+0x30,difficulty,1)
    # Generic logger type chain on the native missing-cell path.
    info=resolve(0x55c1248);p.put(info+0x38,0x10048000,8);p.put(0x10048000,0x10049000,8);p.put(0x10049000+0x132,1,1);p.put(0x10049000+0xe0,1);p.put(0x10049000+0xb8,0x1004a000,8)

rng=random.Random(0x1c92df8);cases=[];expected=[]
for pattern in list(range(-12,20))+[-2147483648,2147483647]:
    cases.append('P,'+str(pattern));expected.append(str(call(0x1c91e28,pattern)&0xffffffff))
for n in range(96):
    target=bits(rng.choice((0.,.17,.2,-1.,float('inf'),float('nan'))));deltas=[bits(rng.choice((0.,.0166,.0333,.1,-.01,float('nan')))) for _ in range(16)]
    owner=0x10050000;p.emu.mem_write(owner,bytes(0x100));p.put(owner+0x10,target)
    cases.append(','.join(map(str,['W',target,*deltas])))
    for delta in deltas:expected.append(str(call(0x12de540,owner)))

for n in range(64):
    trace=[];p.emu.mem_write(model,bytes(0x100));p.put(model+0xb1,1,1)
    commands=[(bits(rng.choice((0.,.17,1.,-2.,float('nan'),float('inf')))),rng.randrange(3)) for _ in range(16)]
    cases.append(','.join(map(str,['A',*[value for pair in commands for value in pair]])))
    for level_time,operation in commands:
        if operation<2:call(0x27b4320,model,operation)
        active=call(0x27b4368,model);swappable=call(0x27b4310,model);special=call(0x27b4318,model)
        call(0x27b4348,model);duration=normal(p.emu.reg_read(UC_ARM64_REG_S0))
        near=call((0x27ba90c,0x27b5958,0x1f3de38,0x1c9df74)[n%4],model)
        expected.append(','.join(map(str,[active,swappable,special,near,duration]))+';'+'|'.join(trace))

for n in range(256):
    trace=[];freezes=[0]*4;gets=0;active_view=1;iterator=wait=0;fault='';current_cell=0;item_type=3
    pattern=[0,1,2,3,4,5,6,7,8,-1][n%10];count=n%15
    playing=n%2;started=n//2%2;factory_null=int(n%19==0);honey=n//4%2;roof=n//8%2;spread=n//16%3-1
    missing_view=[0,0,0,0,1,2,3,4,5,6,7,8][n//10%12]
    layer=rng.choice((231352099,0,-12));difficulty=rng.choice((-2,0,1,2));level_time=bits(rng.uniform(0,20));remove_on_animation=int(n%29==0)
    duration=bits(rng.choice((.2,.3,0.,float('nan'))));ratio=bits(rng.choice((.85,.5,1.)))
    auto=n//3%2;deltas=[bits(rng.choice((.0166,.1,.2,0.))) for _ in range(4)];lose_cell=int(n%17==0)
    wrong_model=int(n%23==0)
    cases.append(','.join(map(str,['C',pattern,count,playing,started,factory_null,honey,roof,spread,missing_view,layer,difficulty,level_time,remove_on_animation,duration,ratio,auto,*deltas,lose_cell,wrong_model])))
    base_setup(layer)
    if wrong_model:p.put(model_class+0x12c,0,1)
    creation_delay=bits(number(duration)*number(ratio));static=0x1004b000;p.put(resolve(0x5594188)+0xb8,static,8);p.put(static,creation_delay)
    p.put(group,pattern);p.put(group+4,3);p.put(group+8,count)
    for i in range(count):p.put(group+0x10+i*8,cell(i%4),8)
    for i in range(4):p.put(cell(i)+0x18,i);p.put(cell(i)+0x1c,i+2)
    call(0x1c91994,creator,group,cell(0),auto);expected.append(snapshot_state('created'))
    p.emu.mem_write(group,bytes(152))
    for step in range(7):
        value='-'
        if iterator and not fault:
            if step<4:
                delta=deltas[step];value='w'+str(call(0x12de540,wait))
            else:
                if step==4 and lose_cell:current_cell=0
                moved=call(0x1c92df8,iterator)
                if not fault:value=str(moved)+':'+('-' if p.read(iterator+0x18,8)==0 else 'w')
        expected.append(snapshot_state(value))

for source in (0,1):
    for above in (0,1):
        for under in (0,1):
            iterator=0;trace=[];fault='';p.emu.mem_write(view,bytes(0x100));p.put(view+0x30,factory,8);p.put(view+0x68,under,1)
            cases.append(','.join(map(str,['T',source,above,under])))
            call(0x27b4f1c,view,front if source else 0,above)
            expected.append(str(int(p.read(view+0x90,8)!=0))+';'+'|'.join(trace))
            for step in range(2):
                call(0x27b4dcc,view);expected.append(str(int(p.read(view+0x90,8)!=0))+';'+'|'.join(trace))

with tempfile.TemporaryDirectory() as folder:
    exe=Path(folder)/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
    files=('OriginalSpecialItemCreator.cs','OriginalSpecialBoardItem.cs','OriginalSpecialCreationParticleTransfer.cs','OriginalGameplayWait.cs','OriginalMatchSnapshot.cs','OriginalMatchItemSorting.cs','OriginalMatchMergeAnimation.cs','OriginalTiledItemTypes.cs')
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(p.PROJECT/'SourceCatalog/SpecialItemCreatorHarness.cs'),*[str(runtime/f) for f in files]],capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    actual=result.stdout.replace('\ufeff','').splitlines()
    assert actual==expected,next(((i,a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e),(len(actual),len(expected)))
report={'passed':True,'native_pattern_probes':34,'native_wait_sequences':96,'native_wait_polls':1536,'native_activation_sequences':64,'native_activation_observations':1024,'native_near_match_capability_probes':1024,'native_creation_sequences':256,'native_creation_observations':2048,'native_particle_transfer_paths':8,
 'scope':'Actual ARM64 match-special creator, CreateItem constructor/MoveNext, gameplay wait, pattern mapping, activation and SpecialItemView creation-particle transfer versus production C#. Compares ordered calls, source snapshot lifetime, freeze counts, activation, view lookups, initial wait, extra yield, static blocking/spreading branches, session/playing/auto flags and missing cell/view/null-factory outcomes. Unity object/particle factory, record sinks, module data and scheduling are supplied boundaries. Concrete special model factories/animation and full playable lifecycle remain unconnected; no rendered-original parity is asserted.'}
(p.PROJECT/'special_item_creator_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
