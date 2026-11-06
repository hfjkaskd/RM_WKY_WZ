"""Execute the actual ARM64 restart-event snapshots and compare managed ports."""
import sys
sys.dont_write_bytecode=True
import json,os,random,subprocess,tempfile
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_move_budget.py').read_text().split('cases=[];expected=[]')[0],str(PROJECT/'SourceCatalog/verify_move_budget.py'),'exec'))
emu.hook_del(native_hook);base_hook=hook
obj,manager,propeller,pools,klass,entry=[0x10020000+i*0x1000 for i in range(6)]
put(klass+0x1b8,0x338c6a8,8)
kind=0;h=[];resolves=0;manager_resolves=0
def record(uc,label):
    trace.append(label)
    if len(trace)==h[1]:fail(uc);return False
    return True
def resolve(uc,label,p):
    global resolves,manager_resolves
    if not record(uc,label):return
    resolves+=1
    if label=='manager':
        manager_resolves+=1
        put(manager+(0x50 if kind==0 else 0x18),h[6]+(manager_resolves if h[12]else 0))
        if kind==0:put(manager+0x18,h[7])
    returned(uc,0 if resolves==h[0]else p)
def result(uc,label,value=0):
    if record(uc,label):returned(uc,value)
def event_hook(uc,address,size,unused):
    global resolves
    x0,x1,x2=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(3)]
    if address in(0x32b2eb4,0x32b518c,0x32b4560):resolve(uc,'manager',manager)
    elif address==0x32b2f1c:resolve(uc,'pools',pools)
    elif address==0x29e27cc:assert x0==66;resolve(uc,'propeller',propeller)
    elif address==0x32b45f8:
        if record(uc,'ab'):
            resolves+=1;put(user+0x40,0 if resolves==h[0]else ab,8)
    elif address==0x32bb7e0:assert x1 in(224,225);result(uc,'ab,'+str(x1),h[10 if x1==224 else 11])
    elif address==0x1e2399c:result(uc,'show',h[2])
    elif address==0x274b0a4:result(uc,'active',h[2])
    elif address==0x1f7bdf0:result(uc,'playable',h[2])
    elif address in(0x274d684,0x1f7e400):result(uc,'tutorial',h[3])
    elif address==0x2752294:result(uc,'safe',h[4])
    elif address==0x1f7c1f8:result(uc,'final',h[4])
    elif address in(0x1e22efc,0x274f758,0x1f7b4c4):result(uc,'count',h[5])
    elif address==0x1f7a004:result(uc,'id',read(manager+0x18))
    elif address==0x1f81948:result(uc,'progress',h[8])
    elif address==0x1e18f34:result(uc,'table',h[9])
    elif address==0x23f1d64:result(uc,'create,'+str(signed(x1&0xffffffff)))
    elif address==0x338c6a8 and x0==obj and kind<3:record(uc,'init,'+str(signed(x1&0xffffffff)))
    elif address==0x231007c:returned(uc)
    elif address==0x39ae3f0:dictionaries[x0]={};returned(uc)
    elif address==0xfcd934:
        assert x0==obj and x2 in(2,3),(x0,x2)
        targets=[(0x32b3c38,0x32b3c68),(0x32b5688,0x32b56b4),(0x32b4c20,0x32b4c4c),(0x338c800,0x338c82c)]
        put(entry,targets[kind][x2-2],8);returned(uc,entry)
    else:base_hook(uc,address,size,unused)
emu.hook_add(UC_HOOK_CODE,event_hook)
cases=[];expected=[];commands=0
def state():
    d=read(obj+0x30,8)if kind==0 else 0
    return ','.join(map(str,[read(obj+0x20,1),signed(read(obj+0x24)),signed(read(obj+0x28)),signed(read(obj+0x2c))if kind<3 else-1,len(dictionaries[d])if d else-1,read(obj+0x2c,1)if kind==3 else 0]))
def wide(rva,*args):
    call(rva,*args);v=emu.reg_read(UC_ARM64_REG_X0);return v-(1<<64)if v>>63 else v
def run(k,head,steps):
    global kind,h,resolves,manager_resolves,next_object,commands,fault
    kind=k;h=head;next_object=0x10030000;dictionaries.clear();emu.mem_write(obj,bytes(0x100));put(obj,klass,8);put(user_static,user,8);put(user+0x40,ab,8)
    call([0x32b3c78,0x32b56c4,0x32b4c5c,0x32b1168][kind],obj)
    out=[]
    for step in steps:
        commands+=1;trace.clear();resolves=manager_resolves=0;fault=False;v=step.split(',');mode=v[0];n=int(v[1])if len(v)>1 else 0;result_text='-'
        if mode=='i':call([0x32b2fd8,0x32b51fc,0x32b45c8,0x32b1104][kind],obj,n,int(v[2])if len(v)>2 else 0)
        elif mode=='g':result_text=str(wide([0x32b3b18,0x32b5578,0x32b4b10,0x338c714][kind],obj))
        elif mode=='l':call([0x32b3bbc,0x32b561c,0x32b4bb4,0x338c7a8][kind],obj,n)
        elif mode in('a','u'):call(0x338b130 if mode=='a'else 0x338c6c0,obj,n)
        elif mode=='d':put(obj+0x20,1,1);put(obj+0x24,n);put(obj+0x28,int(v[2]))
        elif mode=='v':result_text=str(call([0x32b2f8c,0x32b5448,0x32b4ac0][kind],obj))
        elif mode=='s':result_text=str(call(0x32b5264,obj,n))
        elif mode=='r':call(0x32b322c,obj)
        elif mode=='x':
            p=read(obj+0x30,8)
            if p:dictionaries[p][42]=7
        elif mode in('c','t','q'):
            answer=call({'c':0x32b1120,'t':0x32b113c,'q':0x32b1150}[mode],obj,n)
            if mode=='q':result_text=str(answer)
        if fault:result_text='error'
        out.append(result_text+':'+state()+':'+ ';'.join(trace))
    cases.append(str(k)+'#'+','.join(map(str,h))+'#'+';'.join(steps));expected.append('|'.join(out))
def head():return[0,0,1,0,0,17,123,3,1,0,0,0,0]
for k in range(3):
    for missing in range(13):
        for failure in range(27):
            a=head();a[0]=missing;a[1]=failure
            run(k,a,['d,101,202','i,0,2','a,5','i,1,3','g','v','l,-1','g','v']+(['r','x','i,0,-1','x','r']if k==0 else['s,0','s,1']if k==1 else[]))
for k in range(3):
    for flags in range(32):
        for variant in((0,0),(2,0),(0,2),(2,2),(-1,3)):
            a=head();a[2]=flags&1;a[3]=(flags>>1)&1;a[4]=(flags>>2)&1;a[8]=(flags>>3)&1;a[9]=(flags>>4)&1;a[10:12]=variant
            run(k,a,['i,1,0','g','i,0,1','v','i,1,2','g','l,0','v'])
rng=random.Random(0x32b2fd8)
edges=[-2147483648,-8388609,-4194305,-2097153,-8192,-1,0,1,8191,2097151,4194303,8388607,2147483647]
for k in range(3):
    for i in range(80):
        a=head();a[5]=rng.choice(edges);a[6]=rng.choice(edges);a[7]=rng.choice([-1,0,1,2,3,4]);a[10:12]=rng.choice([(0,0),(2,0),(0,2)]);a[12]=i%2
        steps=['d,'+str(rng.choice(edges))+','+str(rng.choice(edges)),'g','i,0,'+str(rng.choice(edges)),'a,'+str(rng.choice(edges)),'g','i,1,3','g','l,'+str(rng.randrange(-2**63,2**63)),'g','v']
        if k==0:steps+=['x','r','x','r','g']
        run(k,a,steps)
for n in edges:run(3,head(),['i,'+str(n),'g','c,'+str(n),'q','g','t,'+str(n),'q','g','l,-1','q','i,0','q'])
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-restart-events-')as directory:
    exe=Path(directory)/'verify.exe';files=('OriginalSessionValues','OriginalLevelSessionCounters','OriginalActiveLevelInitialization','OriginalActiveLevelData','OriginalMoveBudgetAdjustment','OriginalSlbMoveConfig','OriginalActiveLevelStart','OriginalRestartEventSnapshots')
    p=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-langversion:latest','-out:'+str(exe),str(PROJECT/'SourceCatalog/RestartEventSnapshotsHarness.cs'),*[str(PROJECT/'Assets/Foundation/Runtime'/(n+'.cs'))for n in files]],capture_output=True,text=True);assert p.returncode==0,p.stdout+p.stderr
    p=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert p.returncode==0,p.stderr[-2000:]
    actual=p.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
    for i,(a,e)in enumerate(zip(actual,expected)):
        if a!=e:
            aa=a.split('|');ee=e.split('|');j=next(j for j,(x,y)in enumerate(zip(aa,ee))if x!=y)
            raise AssertionError((i,j,cases[i],aa[j],ee[j]))
report={'passed':True,'native_sequences':len(cases),'native_commands':commands,'scope':'Actual ARM64 Madness/RoyalPass/Pinata constructors, Init, event validity, signed save/load and Madness InitAfterRestore; RoyalPass ShouldSetData, AB 224/225 remaining-collectables predicate, DukesTreats Init/CollectTreat/ReduceCooldown/GetTreatCount. All manager resolves, callbacks, pool calls and virtual Init ordering are compared, including ordinal missing dependencies, callback exceptions, retained partial state, changing manager identities, tutorial/final-reward/restart/table gates, signed/packed edges and per-exploder dictionary replacement. Manager lifecycle/progress/AB storage, item-pool creation and CLR allocations/interface dispatch are explicit supplied boundaries; full application ownership/composition and playable scene remain incomplete.'}
(PROJECT/'restart_event_snapshots_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
