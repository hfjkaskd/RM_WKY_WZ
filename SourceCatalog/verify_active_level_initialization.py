"""Actual ARM64 active constructor, ResetValues, counters and reset ordering."""
import sys
sys.dont_write_bytecode=True
import json,os,random,subprocess,tempfile
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_move_budget.py').read_text().split('cases=[];expected=[]')[0],str(PROJECT/'SourceCatalog/verify_move_budget.py'),'exec'))
emu.hook_del(native_hook)
base_hook=hook
animation,coin,extra,inventory,basic_class,restart_class,king=[0x1001a000+i*0x1000 for i in range(7)]
put(basic_class+0x1b8,0x338c6a8,8)
pref_count=0;missing=0;failure=0
put(0x574f288,0x20000010,8)
literal=json.loads((PROJECT.parent/'code/metadata/stringliteral.json').read_text())
assert next(v['value']for v in literal if int(v['address'],16)==0x5618dd0)=='SLTC'

def oracle_hook(uc,address,size,unused):
    global pref_count
    x0,x1,x2=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(3)]
    if address==0xffda78:
        p=allocate(0x200);put(p,basic_class,8);returned(uc,p)
    elif address in(0x314c024,0x2c1b430):
        p=allocate(0x20);put(x0+0x10,p,8);returned(uc)
    elif address in(0x3c9bdd8,0x3c95768):dictionaries[x0]={};returned(uc)
    elif address in(0x3c9c9ec,0x3c96364):dictionaries[x0].clear();returned(uc)
    elif address==0x2627eb8:emu.mem_write(x0+0x20+8*x1,bytes(8*x2));returned(uc)
    elif address==0x231007c:returned(uc)
    elif address==0x338c6a8 and x0 in(coin,extra):
        trace.append(('coin'if x0==coin else'extra')+','+str(signed(x1&0xffffffff)))
        if failure==(5 if x0==coin else 6):fail(uc)
    elif address==0x29e27cc:
        assert x0 in(29,50),x0
        trace.append('booster'if x0==29 else'duke');returned(uc,0 if missing==(5 if x0==29 else 6)else(booster if x0==29 else duke))
    elif address in(0x222d854,0x222ec04):
        is_duke=address==0x222ec04;trace.append('duke_progress'if is_duke else'booster_progress')
        if failure==(2 if is_duke else 1):fail(uc)
        else:returned(uc,head[7 if is_duke else 6])
    elif address==0x3e41f48:
        trace.append('get')
        if failure==3:fail(uc)
        else:returned(uc,pref_count)
    elif address==0x20000010:
        v=signed(x1&0xffffffff);trace.append('set,'+str(v))
        if failure==4:fail(uc)
        else:pref_count=v;returned(uc,1)
    elif address==0x260dea4:assert x0==98;returned(uc,king)
    elif address==0x23e9220:
        trace.append('king,'+str(read(active+0xbc,1)))
        if failure==7:fail(uc)
        else:returned(uc)
    else:base_hook(uc,address,size,unused)
init_hook = emu.hook_add(UC_HOOK_CODE,oracle_hook)
cases=[];expected=[];commands=0
def snapshot(p):return 'null'if not p else ','.join(map(str,(read(p+0x20,1),signed(read(p+0x24)),signed(read(p+0x28)))))
def state():
    r=read(active+0xa0,8);s=read(active+0x90,8);m=read(active+0x98,8);arr=read(m+0x10,8)
    values=[signed(read(active+o,n))if n==4 else signed(read(active+o,1)|0xffffff00)if read(active+o,1)>127 and o==0x5c else read(active+o,n)for o,n in((0x3c,4),(0xac,4),(0x40,1),(0xcc,4),(0x5c,1),(0xbc,1),(0xc0,4),(0xc4,4),(0xc8,4))]
    values.append(read(read(active+0xe8,8)+0x18))
    values += [snapshot(read(active+o,8))for o in(0x60,0x68,0x70,0x78,0x80)]+[snapshot(0 if missing==3 else coin),snapshot(0 if missing==4 else extra)]
    values += [len(dictionaries[read(r+o,8)])for o in(0x10,0x18)]
    values += [''.join(str(read(r+0x1f+i,1))if i<=3 else'0'for i in range(1,9)),''.join(str(read(r+0x1f+i,1))if 4<=i<=7 else'0'for i in range(1,9))]
    values += [','.join(str(signed(read(arr+0x20+4*(i-1))))if 1<=i<=5 else'-1'for i in range(8))]
    values += [len(dictionaries[read(s+o,8)])for o in(0x10,0x18)]+[signed(read(s+o))for o in(0x20,0x24,0x28,0x2c)]+[pref_count]
    return ':'.join(map(str,values))
def dirty():
    for i,p in enumerate([read(active+o,8)for o in(0x60,0x68,0x70,0x78,0x80)]+[0 if missing==3 else coin,0 if missing==4 else extra]):
        if p:put(p+0x20,1,1);put(p+0x24,100+i);put(p+0x28,200+i)
    for o,v,n in((0x3c,5,4),(0xac,9,4),(0x40,1,1),(0xcc,88,4),(0x5c,-8,1),(0xbc,0,1),(0xc0,4,4),(0xc4,9,4),(0xc8,2,4)):put(active+o,v,n)
    l=read(active+0xe8,8);a=allocate(0x40);put(a+0x18,4);put(l+0x10,a,8);put(l+0x18,read(l+0x18)+2)
    r=read(active+0xa0,8);dictionaries[read(r+0x10,8)][4]=4;dictionaries[read(r+0x18,8)][5]=5
    for i in range(7):put(r+0x20+i,1,1)
    s=read(active+0x90,8);dictionaries[read(s+0x10,8)][1]=1;dictionaries[read(s+0x18,8)][2]=3
    for i in range(4):put(s+0x20+4*i,11+i)
    m=read(active+0x98,8)
    for i in range(1,7):call(0x21a4028,m,i,i*11)
def run(h,steps):
    global head,missing,failure,pref_count,next_object,commands
    head=h;missing,failure,story,difficulty,coins,stars,b,d,pref_count=h;next_object=0x10030000;dictionaries.clear()
    emu.mem_write(active,bytes(0x200));emu.mem_write(animation,bytes(0x100));emu.mem_write(coin,bytes(0x100));emu.mem_write(extra,bytes(0x100));put(coin,basic_class,8);put(extra,basic_class,8)
    put(user_static,user,8);put(user+0x20,0 if missing==1 else inventory,8);put(inventory+0x10,coins);put(inventory+0x14,stars)
    put(animation+0x28,0 if missing==3 else coin,8);put(animation+0x30,0 if missing==4 else extra,8)
    call(0x32b6324,active,0 if missing==2 else animation);assert not fault
    put(active+0x34,story);put(active+0x30,difficulty,1)
    out=[]
    for step in steps:
        commands+=1;trace.clear();result='-';fields=step.split(',');mode=fields[0];v=int(fields[1])if len(fields)>1 else 0
        if mode=='d':dirty()
        elif mode in('r','t','c','u','i','z','s'):
            call({'r':0x32b7060,'t':0x32b8894,'c':0x32b8904,'u':0x32ba8b4,'i':0x32ba87c,'z':0x32ba8a4,'s':0x32ba8ac}[mode],active)
        elif mode in('x','m'):call(0x21a3f58 if mode=='x'else 0x21a4028,read(active+0x98,8),v,int(fields[2])if len(fields)>2 else 0)
        elif mode=='p':call(0x27b2874,read(active+0x90,8))
        elif mode in('n','h'):call(0x32b1428 if mode=='n'else 0x32b17c8,read(active+0xa0,8),v,int(fields[2])if len(fields)>2 else 0)
        if fault:result='error'
        out.append(result+':'+state()+':'+ ';'.join(trace))
    cases.append(','.join(map(str,h))+'#'+';'.join(steps));expected.append('|'.join(out))
for missing_value in range(7):
    for fail_value in range(8):
        for story in(0,-1):
            run([missing_value,fail_value,story,3,1001,12,7,9,2147483647],['q','d','r','r','c','u','i','z','u','s','x','p'])
for difficulty in(-128,-1,0,1,2,3,127):
    for story in(-2147483648,-1,0,2147483647):
        run([0,0,story,difficulty,-2147483648,2147483647,-1,0,-2147483648],['d','t','r','i','r','c'])
rng=random.Random(0x32b7060)
for i in range(40):
    h=[0,0,-1 if i%2 else 0,rng.randrange(-128,128),rng.randrange(-2**31,2**31),rng.randrange(-2**31,2**31),rng.randrange(-2**31,2**31),rng.randrange(-2**31,2**31),rng.randrange(-2**31,2**31)]
    steps=['q']+[rng.choice(['d','r','t','x','p','u','i','z','s','c','n,'+str(rng.randrange(-1,10)),'h,'+str(rng.randrange(-1,10))+','+str(rng.randrange(2)),'m,'+str(rng.randrange(-1,9))+','+str(rng.choice([-2147483648,-1,1,2147483647]))])for _ in range(20)]
    run(h,steps)
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-active-init-')as directory:
    exe=Path(directory)/'verify.exe';files=('OriginalSessionValues','OriginalLevelSessionCounters','OriginalActiveLevelInitialization','OriginalActiveLevelData','OriginalMoveBudgetAdjustment','OriginalSlbMoveConfig','OriginalUsefulMoveState')
    compiled=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-langversion:latest','-out:'+str(exe),str(PROJECT/'SourceCatalog/ActiveLevelInitializationHarness.cs'),*[str(PROJECT/'Assets/Foundation/Runtime'/(n+'.cs'))for n in files]],capture_output=True,text=True);assert compiled.returncode==0,compiled.stdout+compiled.stderr
    completed=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert completed.returncode==0,completed.stdout+completed.stderr
    actual=completed.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
    for i,(a,e)in enumerate(zip(actual,expected)):
        if a!=e:
            j=next(j for j,(x,y)in enumerate(zip(a.split('|'),e.split('|')))if x!=y)
            raise AssertionError((i,j,[ord(c)for c in a[:5]],cases[i].split('#')[0],cases[i].split('#')[1].split(';')[j],a.split('|')[j],e.split('|')[j]))
report={'passed':True,'native_sequences':len(cases),'native_commands':commands,'scope':'Actual ARM64 UserActiveLevelData constructor, ResetValues, IncrementStoryLevelTryCount, ResetCumulativeProgress, shared useful/useless counters and King callback ordering; BeforeAfter Init, MatchItemCounts constructor/reset/increment/query, SpecialItemCounts constructor/reset, LevelRestartData constructor/reset/unlimited/animation flags. Missing inventory/animation/snapshot/managers and progress/PlayerPrefs/Init/King failures preserve original partial writes. CLR allocation/collections/Array.Clear, user inventory/manager progress, PlayerPrefs persistence and King relief implementation are supplied boundaries. SetStartValues and Start/Restart wrappers, full AnimationData/event initialization/save and formal production composition/playable loop remain pending.'}
(PROJECT/'active_level_initialization_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
