"""Actual ARM64 DailyLightningRush startup, resource ownership and save/restore."""
import sys
sys.dont_write_bytecode=True
import json,os,random,subprocess,tempfile
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_move_budget.py').read_text().split('cases=[];expected=[]')[0],str(PROJECT/'SourceCatalog/verify_move_budget.py'),'exec'))
emu.hook_del(native_hook);base_hook=hook
obj,manager,progress,pools,owner,old,new,klass,manager_class,bundle_class,ancestors,entry=[0x10020000+i*0x400 for i in range(12)]
put(manager,manager_class,8);put(manager_class+0x248,0x20000020,8)
put(bundle_class+0xc8,ancestors,8);put(bundle_class+0x12c,1,1)
h=[];resolves=progress_reads=step_reads=0
def record(uc,label):
    trace.append(label)
    if len(trace)==h[1]:fail(uc);return False
    return True
def resolve(uc,label,p):
    global resolves
    if record(uc,label):resolves+=1;returned(uc,0 if resolves==h[0]else p)
def result(uc,label,value=0):
    if record(uc,label):returned(uc,value)
def daily_hook(uc,address,size,unused):
    global progress_reads,step_reads
    x0,x1,x2=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(3)]
    if address==0x338d330:resolve(uc,'manager',manager)
    elif address==0x29e27cc:assert x0==62;resolve(uc,'manager',manager)
    elif address==0x338d398:resolve(uc,'pools',pools)
    elif address==0x1f8cedc:
        progress_reads+=1;result(uc,'progress',h[2 if progress_reads==1 else 12])
    elif address==0x20000020:result(uc,'downloaded',h[3])
    elif address==0x30f6400:assert x0==27;resolve(uc,'owner',owner)
    elif address==0x3665764:
        target=read(0x5592610,8);put(target+0x12c,1,1);put(ancestors,0 if h[11]else target,8);put(new,bundle_class,8)
        resolve(uc,'bundle',new)
    elif address==0x365eb20:assert x0==new;result(uc,'load',h[4])
    elif address==0x2f858c4:
        step_reads+=1;result(uc,'step',h[5 if step_reads==1 else 6])
    elif address in(0x23f1d80,0x23f1d9c,0x23f1db8):
        if record(uc,{0x23f1d80:'create',0x23f1d9c:'clear',0x23f1db8:'recycle'}[address]):
            if address==0x23f1d9c and h[13]:put(obj+0x30,0,8)
            returned(uc)
    elif address==0x365eed8:result(uc,'unload,'+str(1 if x0==old else 2)+','+str(x1))
    elif address==0x1e18f34:result(uc,'table',h[8])
    elif address in(0x338d660,0x338d95c):uc.reg_write(UC_ARM64_REG_PC,0x338d640 if address==0x338d660 else 0x338d950) # Skip only diagnostic Log.Error via the real stack epilog.
    elif address==0xffde20:fail(uc) # Actual native wrong-type cast failure.
    elif address==0x231007c:returned(uc)
    elif address==0xfcd934:
        assert x0==obj and x2 in(2,3),(hex(x0),x2)
        put(entry,0x338e2f0 if x2==2 else 0x338e31c,8);returned(uc,entry)
    else:base_hook(uc,address,size,unused)
emu.hook_add(UC_HOOK_CODE,daily_hook)
cases=[];expected=[];commands=0
def state():
    p=read(obj+0x30,8)
    return ','.join(map(str,[read(obj+0x20,1),signed(read(obj+0x24)),signed(read(obj+0x28)),signed(read(obj+0x38)),signed(read(obj+0x3c)),signed(read(obj+0x40)),int(read(obj+0x48,8)!=0),read(obj+0x50,1),0 if not p else 1 if p==old else 2]))
def run(head,steps):
    global h,resolves,progress_reads,step_reads,next_object,commands,fault
    h=head[:];next_object=0x10030000;emu.mem_write(obj,bytes(0x100));put(obj,klass,8)
    call(0x338ae10,obj);out=[]
    for command in steps:
        commands+=1;trace.clear();resolves=progress_reads=step_reads=0;fault=False;v=command.split(',');mode=v[0];n=int(v[1])if len(v)>1 else 0;result_text='-'
        put(manager+0x28,0 if h[9]else progress,8)
        put(user_static,0 if h[10]==1 else user,8);put(user+0x38,0 if h[10]==2 else session,8);put(session+0x18,0 if h[10]==3 else active,8);put(active+0x30,h[7],1)
        if mode=='d':
            for o,value in((0x20,1),(0x24,101),(0x28,202),(0x38,55),(0x3c,7),(0x40,-9),(0x50,1)):put(obj+o,value,1 if o in(0x20,0x50)else 4)
            put(obj+0x30,old,8);put(obj+0x48,pools,8)
        elif mode in('i','r'):call(0x338d3dc if mode=='i'else 0x338d7b8,obj,n)
        elif mode=='l':call(0x338e238,obj,n)
        elif mode=='g':
            call(0x338e1a4,obj);value=emu.reg_read(UC_ARM64_REG_X0);result_text=str(value-(1<<64)if value>>63 else value)
        elif mode in('v','n'):result_text=str(call(0x338da00 if mode=='v'else 0x338d704,obj,n))
        elif mode=='h':h[n]=int(v[2])
        elif mode=='b':put(obj+0x24,n);put(obj+0x28,int(v[2]));put(obj+0x20,0,1)
        if fault:result_text='error'
        out.append(result_text+':'+state()+':'+ ';'.join(trace))
    cases.append(','.join(str(int(value))for value in head)+'#'+';'.join(steps));expected.append('|'.join(out))
def head():return[0,0,1,1,1,2,2,1,0,0,0,0,1,0]
for flags in range(16):
    for dirty in(0,1):
        a=head();a[2]=a[12]=flags&1;a[3]=(flags>>1)&1;a[4]=(flags>>2)&1;a[8]=(flags>>3)&1
        run(a,(['d']if dirty else[])+['g','i,1','v','g','r,2','g','n,0','g','n,-1','g','l,-1','g','i,3'])
for branch in range(4):
    for missing,failure in([(i,0)for i in range(1,9)]+[(0,i)for i in range(1,24)]):
        a=head();a[0]=missing;a[1]=failure;a[2]=a[12]=branch!=1;a[3]=branch!=2;a[4]=branch!=3
        run(a,['d','i,2','g','r,3','g','l,0','g','v','n,5'])
for flag in(9,10,11,13):
    for value in(1,2,3):
        a=head();a[flag]=value;run(a,['d','i,3','g','r,1','g','l,-1','g','h,2,0','i,2','g'])
for first,second in((55,55),(55,2),(2,55),(2,3)):
    a=head();a[5]=first;a[6]=second;run(a,['d','i,1','g','i,2','g'])
for difficulty in(-2147483648,-128,-2,-1,0,1,2,3,4,127,2147483647):
    a=head();a[7]=-128
    run(a,[f'i,{difficulty}','g','n,0','g','n,2147483647','g','n,-2147483648','g','h,12,0',f'i,{difficulty}','g','l,-1','g'])
rng=random.Random(0x338d3dc)
for i in range(70):
    a=head();a[5]=rng.choice([-2147483648,-1,0,1,55,2147483647]);a[6]=rng.choice([-2147483648,-1,0,1,55,2147483647]);a[7]=rng.randrange(-128,128);a[12]=i%2
    run(a,['d',f'i,{rng.randrange(-2**31,2**31)}','g',f'b,{rng.randrange(-2**31,2**31)},{rng.randrange(-2**31,2**31)}','g',f'l,{rng.randrange(-2**63,2**63)}','g','n,0','g','h,2,0','i,1','g','h,2,1','i,2','g'])
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-daily-lightning-')as directory:
    exe=Path(directory)/'verify.exe';files=('OriginalSessionValues','OriginalLevelSessionCounters','OriginalActiveLevelInitialization','OriginalActiveLevelData','OriginalMoveBudgetAdjustment','OriginalSlbMoveConfig','OriginalDailyLightningRushLevelData')
    p=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-langversion:latest','-out:'+str(exe),str(PROJECT/'SourceCatalog/DailyLightningRushHarness.cs'),*[str(PROJECT/'Assets/Foundation/Runtime'/(n+'.cs'))for n in files]],capture_output=True,text=True);assert p.returncode==0,p.stdout+p.stderr
    p=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert p.returncode==0,p.stderr[-2000:]
    actual=p.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
    for i,(a,e)in enumerate(zip(actual,expected)):
        if a!=e:
            aa=a.split('|');ee=e.split('|');j=next(j for j,(x,y)in enumerate(zip(aa,ee))if x!=y)
            raise AssertionError((i,j,cases[i],aa[j],ee[j]))
report={'passed':True,'native_sequences':len(cases),'native_commands':commands,'scope':'Actual ARM64 DailyLightningRushLevelData constructor, Init, InitForRestore, IsEventValid, IncrementCount, 1/24/24 signed snapshot save/load and active-level difficulty recovery. Compares native ordered manager/download/bundle/pool/progress calls and all modeled field writes, ordinal missing dependencies and callback failures, wrong bundle type, zero-count consume flag, changing step reads, stale-bundle retention, inactive recycle/clear/unload, successful and failed loads, dirty presentation state, signed and packed edges. Only diagnostic Log.Error construction/tail call is omitted. CLR allocation/interface/type checks, actual application/user managers/progress, bundle loading/unloading and item-pool implementation are explicit supplied boundaries; formal application/scene composition and playable loop remain incomplete.'}
(PROJECT/'daily_lightning_rush_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
