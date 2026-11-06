"""Execute native ordered goal-trail routing and item-view collection branches."""
import sys
sys.dont_write_bytecode=True
import verify_item_goal_delivery as b
import json, random, struct, subprocess, tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import *

manager, entries, storage, view, view_class, delegate, delegate_class, factory = [0x10020000+i*0x1000 for i in range(8)]
claim_base, claim_class, info, effect, effect_class, transform, level_statics = [0x10030000+i*0x1000 for i in range(7)]
SUBGOAL, INIT, TYPE = 0x20000400, 0x20000420, 0x20000440
b.put(manager+0x18,entries,8);b.put(entries+0x10,storage,8)
b.put(view,view_class,8);b.put(view_class+0x368,0x2b4b8b0,8)
b.put(view+0x60,delegate,8);b.put(delegate,delegate_class,8)
b.put(b.board_class+0x1a8,SUBGOAL,8)
b.put(effect,effect_class,8);b.put(effect_class+0x1a8,INIT,8)
b.put(b.level_class+0xb8,level_statics,8)
head=[]; data=[]; registry=[]; mutated=False
bits=lambda v:format(v&0xffffffff,'08x')
def position(uc):return ','.join(bits(uc.reg_read(UC_ARM64_REG_S0+i)) for i in range(3))
def sync():
    b.put(entries+0x18,len(registry))
    for i,value in enumerate(registry):b.put(storage+0x20+i*8,value,8)
def mutate(phase,ident):
    global mutated
    if mutated or head[14]!=phase or ident!=0:return
    mutated=True;b.trace.append('mutate,'+str(phase))
    if phase==1 and claim_base in registry:registry.remove(claim_base)
    if phase==2 and claim_base+0x100 in registry:registry.remove(claim_base+0x100)
    if phase==3 and claim_base+0x300 not in registry:registry.append(claim_base+0x300)
    sync()
def hook(uc,address,size,unused):
    x0,x1,x2=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(3)]
    goal=b.signed(x1&0xffffffff)
    if address==0x260dea4:
        assert x0==111,x0;b.returned(uc,manager if head[12] else 0)
    elif address==0x1e19144:b.trace.append('M');b.returned(uc,head[6])
    elif address==0x1e18cd4:b.trace.append('W')
    elif address==0x2b4b9f8:b.trace.append('R')
    elif address==SUBGOAL:b.trace.append('board,'+str(goal));b.returned(uc,head[7])
    elif address==0x260a220:
        b.trace.append('spawn,'+str(x1)+','+str(x2));b.returned(uc,effect if head[10] else 0)
    elif address==0x3e4737c:b.returned(uc,transform if head[11] else 0)
    elif address==0x3e4e368:b.trace.append('pos,'+position(uc));b.returned(uc)
    elif address==INIT:b.trace.append('init,'+str(goal)+','+str(x2));b.returned(uc)
    elif address==TYPE:b.returned(uc,head[0])
    elif address==0x2c1b9a0:
        assert x0==entries and 0<=x1<len(registry),(hex(x0),x1,registry)
        b.returned(uc,registry[x1])
    elif address==0xfcd934:
        slot=x2
        assert slot in (0,1,3,12),slot
        target=TYPE if x0==delegate else 0x20000200+slot*0x10
        b.put(info+slot*0x20,target,8);b.put(info+slot*0x20+8,0,8);b.returned(uc,info+slot*0x20)
    elif address in (0x20000200,0x20000210,0x20000230,0x200002c0):
        ident=(x0-claim_base)//0x100;active,want,await_,result=data[ident]
        if address==0x20000200:b.trace.append('a,'+str(ident));mutate(1,ident);b.returned(uc,active)
        elif address==0x20000210:b.trace.append('w,'+str(ident)+','+str(goal));mutate(3,ident);b.returned(uc,int(goal==want))
        elif address==0x20000230:b.trace.append('c,'+str(ident)+','+str(goal));mutate(2,ident);b.returned(uc,int(goal==await_))
        else:b.trace.append('trail,'+str(ident)+','+str(goal)+','+position(uc));b.returned(uc,result)
b.emu.hook_add(UC_HOOK_CODE,hook)

cases=[];expected=[]
def run(mode,values,rows):
    global head,data,registry,mutated
    head=values;data=rows;mutated=False;b.trace.clear()
    b.values=[0,0,0,head[2],head[3],head[4]]
    registry=[claim_base+i*0x100 for i in range(4)if(head[13]&(1<<i))]
    for i in range(4):b.put(claim_base+i*0x100,claim_class,8)
    sync();b.put(level_statics+0x14,head[5],1)
    b.put(b.game_statics+8,b.game,8);b.put(b.game+0x20,b.ui,8);b.put(b.ui+0x38,b.board if head[8] else 0,8)
    b.put(view+0x30,factory if head[9] else 0,8)
    for i,value in enumerate((head[15]*.25,head[16]*.5,head[17]*.125)):
        b.emu.reg_write(UC_ARM64_REG_S0+i,struct.unpack('<I',struct.pack('<f',value))[0])
    if mode=='T':value=b.call(0x2ee7710,manager,head[0]);result='yes'if value else'no'
    elif mode=='V':value=b.call(0x2b4b7c4,view);result='effect'if value==effect else'none'
    else:value=b.call(0x2b4b8b0,view,head[0],head[1]);result='effect'if value==effect else'none'
    b.trace.append('error'if b.fault else result)
    cases.append(mode+'#'+','.join(map(str,head))+'#'+';'.join(','.join(map(str,row))for row in rows))
    expected.append(';'.join(b.trace))

rng=random.Random(17153)
empty=[[0,-999,-999,0]for _ in range(4)]
# Every collection gate and missing board/factory/spawn/transform/claim-manager.
for flags in range(32):
    for failure in range(7):
        h=[11,flags&1,flags&1,(flags>>1)&1,(flags>>2)&1,(flags>>3)&1,(flags>>4)&1,1,1,1,1,1,1,0,0,5,-9,2]
        if failure>0:h[6+failure]=0
        run('C',h,empty)
# Exhaust priority: awaiting trails precede wanting goals; false trail result
# returns immediately, and callbacks may alter the registry while it is read.
for seed in range(600):
    h=[rng.choice((1,11,55)),seed%2,seed%2,0,1,0,seed%2,1,1,1,1,1,1,rng.randrange(16),rng.randrange(4),rng.randrange(-20,21),rng.randrange(-20,21),rng.randrange(-20,21)]
    rows=[[rng.randrange(2),rng.choice((1,11,55,-999)),rng.choice((1,11,55,-999)),rng.randrange(2)]for _ in range(4)]
    run('T'if seed%2 else'C',h,rows)
for item_type in range(-4,121):
    run('V',[item_type,0,1,0,1,0,0,1,1,1,1,1,1,0,0,-4,7,0],empty)

with tempfile.TemporaryDirectory(prefix='rm-view-collection-')as directory:
    exe=Path(directory)/'verify.exe'
    names=('OriginalGoalClaims','OriginalGoalTrailCollection','OriginalItemViewGoalCollection','OriginalItemGoalServices','OriginalGameUiProgress','OriginalGoalTypes','OriginalLevelRules','OriginalTiledItemTypes')
    compiled=subprocess.run([str(b.mono/'bin/mono.exe'),str(b.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(b.PROJECT/'SourceCatalog/ItemViewCollectionHarness.cs'),*[str(b.runtime/(n+'.cs'))for n in names]],capture_output=True,text=True)
    assert compiled.returncode==0,compiled.stdout+compiled.stderr
    completed=subprocess.run([str(b.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8')
    assert completed.returncode==0,completed.stdout+completed.stderr
    actual=completed.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected)
    for i,(a,e)in enumerate(zip(actual,expected)):assert a==e,(i,cases[i],a,e)
report={'passed':True,'native_cases':len(cases),'scope':'Actual ARM64 ordered GoalClaimManager.TryCollectWithTrail including pending-trail priority, wanting fallback, false-result short circuit and mutation of claimer registry during callbacks. Actual ItemView item-type overload and explicit-goal collection, actual LevelManager.ShouldCollectToProgress control flow, water exclusion, multi-board subgoal test, pool 175/389 selection, spawn-position-init order and returned effect. Covers all collection-gate combinations, missing required services/effect/transform, signed item-type bounds and exact source float positions. Managed registry/list storage, interface/virtual dispatch, metadata initialization, level flag getters, board membership, claimer trail visuals, pooled effect and Unity transform storage are supplied boundaries. Actual normal-first-level no-progress collection is separately exercised with real empty claim registry in the Unity box fixture. Real trail/progress effect prefabs, active-level/GameContext composition, box audio/session/King relief and formal player resolution remain pending.'}
(b.PROJECT/'item_view_collection_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(json.dumps(report))
