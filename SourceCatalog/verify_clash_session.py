"""Actual ARM64 Clash startup/restore/resource lifecycle and item/combo scoring."""
import sys
sys.dont_write_bytecode=True
import json,os,random,subprocess,tempfile
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_move_budget.py').read_text().split('cases=[];expected=[]')[0],str(PROJECT/'SourceCatalog/verify_move_budget.py'),'exec'))
emu.hook_del(native_hook);base_hook=hook
obj,manager,helper,config,pools,owner,old,new,dialog,asset,component,klass,manager_class,bundle_class,ancestors,entry=[0x10020000+i*0x200 for i in range(16)]
put(manager,manager_class,8);put(manager_class+0x248,0x20000020,8);put(bundle_class+0xc8,ancestors,8);put(bundle_class+0x12c,1,1)
config_calls=[0x3f7eca4,0x3f7ed10,0x3f7ed80,0x3f7edf0,0x3f7ee60,0x3f7eed0,0x3f7ef40,0x3f7efb0,0x3f7f020,0x3f7f090,0x3f7f100,0x3f7f170,0x3f7f1e0,0x3f7ec4c]
h=[];resolves=0
def record(uc,label):
    trace.append(label)
    if len(trace)==h[1]:fail(uc);return False
    return True
def result(uc,label,value=0):
    if record(uc,label):returned(uc,value)
def resolve(uc,label,p):
    global resolves
    if record(uc,label):resolves+=1;returned(uc,0 if resolves==h[0]else p)
def clash_hook(uc,address,size,unused):
    x0,x1,x2=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(3)]
    if address==0x29e27cc:assert x0==74;resolve(uc,'manager',manager)
    elif address==0x338c834:resolve(uc,'pools',pools)
    elif address==0x20f8fc8:result(uc,'in_event',h[2])
    elif address==0x20000020:result(uc,'downloaded',h[3])
    elif address==0x20f9070:result(uc,'progress',h[5])
    elif address==0x3c683e4:result(uc,'locked',h[6])
    elif address==0x1687e1c:result(uc,'multiplier',h[7])
    elif address==0x30f6400:assert x0==27;resolve(uc,'owner',owner)
    elif address==0x3665764:
        target=read(0x5592610,8);put(target+0x12c,1,1);put(ancestors,0 if h[13]else target,8)
        put(new,bundle_class,8);put(dialog,bundle_class,8)
        game=uc.reg_read(UC_ARM64_REG_LR)==0x338cca0;assert game or uc.reg_read(UC_ARM64_REG_LR)==0x338cdbc
        resolve(uc,'bundle,'+('game'if game else'dialog'),new if game else dialog)
    elif address==0x365eb20:assert x0==new;result(uc,'load',h[4])
    elif address==0x3a269b0:assert x0==new;result(uc,'asset',0 if h[11]else asset)
    elif address==0x260527c:assert x0==asset;result(uc,'component',0 if h[12]else component)
    elif address==0x23f1dd4:result(uc,'create,'+str(int(x1!=0)))
    elif address==0x23f1e30:
        if record(uc,'clear'):
            if h[14]:put(obj+0x40,0,8)
            returned(uc)
    elif address==0x365eed8:result(uc,'unload,'+str(1 if x0==old else 2 if x0==new else 3)+','+str(x1))
    elif address==0x3c6a750:result(uc,'score,'+str(signed(x1&0xffffffff))+','+str(signed(x2&0xffffffff)))
    elif address in config_calls:result(uc,'config,'+str(config_calls.index(address)),signed(h[8]+config_calls.index(address)*7))
    elif address==0x338c928:
        if h[15]==4:put(user+0x38,0,8)
        elif h[15]==5:put(session+0x18,0,8)
    elif address==0xffde20:fail(uc)
    elif address==0x231007c:returned(uc)
    elif address==0xfcd934:
        assert x0==obj and x2 in(2,3)
        put(entry,0x338c800 if x2==2 else 0x338c82c,8);returned(uc,entry)
    else:base_hook(uc,address,size,unused)
emu.hook_add(UC_HOOK_CODE,clash_hook)
cases=[];expected=[];commands=0
def state():
    b=read(obj+0x40,8)
    return ','.join(map(str,[read(obj+0x20,1),signed(read(obj+0x24)),signed(read(obj+0x28)),int(read(obj+0x30,8)!=0),int(read(obj+0x38,8)!=0),0 if not b else 1 if b==old else 2,signed(read(obj+0x48)),read(obj+0x4c,1),*[signed(read(obj+o))for o in range(0x50,0x8c,4)]]))
def run(head,steps):
    global h,resolves,next_object,commands,fault
    h=head[:];next_object=0x10030000;emu.mem_write(obj,bytes(0x100));put(obj,klass,8);call(0x338ae24,obj);out=[]
    for command in steps:
        commands+=1;trace.clear();resolves=0;fault=False;v=command.split(',');mode=v[0];n=int(v[1])if len(v)>1 else 0;result_text='-'
        put(manager+0x38,0 if h[9]else helper,8);put(manager+0x30,0 if h[10]else config,8)
        put(user_static,0 if h[15]==1 else user,8);put(user+0x38,0 if h[15]==2 else session,8);put(session+0x18,0 if h[15]==3 else active,8);put(active+0x30,h[16],1);put(active+0xd0,h[17],1)
        if mode=='d':
            put(obj+0x20,1,1);put(obj+0x24,101);put(obj+0x28,202);put(obj+0x30,manager,8);put(obj+0x38,helper,8);put(obj+0x40,old,8);put(obj+0x48,-7);put(obj+0x4c,1,1)
            for i,o in enumerate(range(0x50,0x88,4)):put(obj+o,-101-i)
        elif mode=='i':call(0x338c958,obj,n,int(v[2]),int(v[3]))
        elif mode=='l':call(0x338c8d0,obj,n)
        elif mode=='g':
            call(0x338c714,obj);x=emu.reg_read(UC_ARM64_REG_X0);result_text=str(x-(1<<64)if x>>63 else x)
        elif mode in('v','a','c'):
            result_text=str(call({'v':0x338c878,'a':0x338ce2c,'c':0x338cf00}[mode],obj,n,int(v[2])if len(v)>2 else 0))
        elif mode in('m','r'):call(0x338d10c if mode=='m'else 0x338ce20,obj,n)
        elif mode=='h':h[n]=int(v[2])
        if fault:result_text='error'
        out.append(result_text+':'+state()+':'+ ';'.join(trace))
    cases.append(','.join(str(int(x))for x in head)+'#'+';'.join(steps));expected.append('|'.join(out))
def head():return[0,0,1,1,1,1,0,3,11,0,0,0,0,0,0,0,1,1]
for flags in range(32):
    a=head();a[2]=flags&1;a[3]=(flags>>1)&1;a[4]=(flags>>2)&1;a[5]=(flags>>3)&1;a[6]=(flags>>4)&1
    run(a,['v','d','i,1,1,0','g','v','a,1','a,2','a,3','a,4','a,5','c,1,17','c,2,9','c,5,9','m,3','g','r','g','i,2,1,1','g','l,-1','g'])
for branch in range(4):
    for missing,failure in([(x,0)for x in range(1,8)]+[(0,x)for x in range(1,29)]):
        a=head();a[0]=missing;a[1]=failure;a[2]=branch!=1;a[3]=branch!=2;a[4]=branch!=3
        run(a,['d','i,3,1,0','g','i,-1,1,1','g','a,1','c,1,7','l,-1','g'])
for flag in(9,10,11,12,13,14,15):
    for value in range(1,6)if flag==15 else(1,):
        a=head();a[flag]=value;run(a,['d','i,1,1,0','a,2','g','l,-1','g','h,2,0','i,0,0,1','g'])
for difficulty in(-128,-1,0,1,2,3,4,127):
    for multiplier in(-2147483648,-1,0,1,3,2147483647):
        a=head();a[7]=multiplier;a[8]=2147483640
        run(a,[f'i,{difficulty},1,0']+[f'a,{x}'for x in(-1,0,1,2,3,4,5,6,2147483647)]+[f'c,{x},2147483647'for x in range(-1,13)]+['m,-2147483648','g','r','g','i,2,0,1','g'])
rng=random.Random(0x338c958)
for i in range(35):
    a=head();a[7]=rng.randrange(-2**31,2**31);a[8]=rng.randrange(-2**31,2**31);a[16]=rng.randrange(-128,128);a[17]=i%2
    run(a,['d','l,'+str(rng.randrange(-2**63,2**63)),'g','a,'+str(rng.randrange(-2**31,2**31)),'c,'+str(rng.randrange(-2**31,2**31))+','+str(rng.randrange(-2**31,2**31)),'m,'+str(rng.randrange(-2**31,2**31)),'g','h,6,1','a,1','i,-1,1,0','a,1','g'])
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-clash-session-')as directory:
    exe=Path(directory)/'verify.exe';files=('OriginalSessionValues','OriginalLevelSessionCounters','OriginalActiveLevelInitialization','OriginalActiveLevelData','OriginalMoveBudgetAdjustment','OriginalSlbMoveConfig','OriginalActiveLevelStart','OriginalDailyLightningRushLevelData','OriginalClashOfChampionsLevelData')
    p=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-langversion:latest','-out:'+str(exe),str(PROJECT/'SourceCatalog/ClashSessionHarness.cs'),*[str(PROJECT/'Assets/Foundation/Runtime'/(n+'.cs'))for n in files]],capture_output=True,text=True);assert p.returncode==0,p.stdout+p.stderr
    p=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert p.returncode==0,p.stderr[-2000:]
    actual=p.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
    for i,(a,e)in enumerate(zip(actual,expected)):
        if a!=e:
            aa=a.split('|');ee=e.split('|');j=next(j for j,(x,y)in enumerate(zip(aa,ee))if x!=y)
            raise AssertionError((i,j,cases[i],aa[j],ee[j]))
report={'passed':True,'native_sequences':len(cases),'native_commands':commands,'scope':'Actual ARM64 ClashOfChampionsLevelData constructor, Init, full gameplay bundle/pool lifecycle, base unsigned 1/20/20 save and override LoadSaveData, CanIncreaseScore, item/combo/remaining-move scoring, local-score-before-snapshot ordering and round reset. Compares native field/ownership writes and ordered manager/download/bundle/asset/component/pool/multiplier/lock/config/local-score calls, missing dependencies, callback failure partial state, invalid bundle type, null component forwarding, two current-level reads, retained restored total/round-reset total and signed overflow/jump-table edges. Actual manager/helper/config/MultiplierUtil behavior, bundle asset loading/unloading, ItemFactory pools and CLR runtime/type/interface handling remain explicit supplied boundaries. Collect-view presentation, remaining-special-item settlement, full AnimationData/application ownership and formal playable loop remain incomplete.'}
(PROJECT/'clash_session_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
