"""Compare native UserManager.LevelEnd/RemainingMovesEnd and LevelManager tail.

Actual native BasicUserData progression and UserInventory balance/packing methods
run inside the full dispatch. Feature/event aggregation, database writes, restore
file deletion, analytics/backend, milestone package and Madness validity are typed
boundaries. Session data loading and full event helper implementations are outside
scope. Diagnostic formatting is skipped; production persistence/UI is not proved.
"""
from pathlib import Path
import hashlib,json,random,subprocess,tempfile
USER_SCOPE=__doc__.strip();PROJECT=Path(__file__).resolve().parents[1]
source=PROJECT/'SourceCatalog/verify_account_kernels.py'
exec(compile(source.read_text().split('\ncases=[];expected=[]')[0],str(source),'exec'))
manager,level_owner,session_data,animation,coin,star,cloche,nth,madness,restore,duke,token,token_purchase,episode,new_ux,events,story_data,package,analytics,lantern=[0x10230000+i*0x1000 for i in range(20)]
def user_hook(address):
    a,b,c=x(0),x(1),x(2);missing=v[35]
    if address==0x29e27cc:
        if a not in(31,50,69):return False
        returned(0 if missing=={31:6,50:7,69:8}[a]else{31:cloche,50:duke,69:token}[a]);return True
    if address==0x2aeecb4:u.reg_write(r.UC_ARM64_REG_PC,0x2aeed28);return True
    if address==0x2aeed78:u.reg_write(r.UC_ARM64_REG_PC,0x2aeee68);return True
    if address==0x2aecde4:trace.append('event-info')
    elif address==0x23df9e4:returned(0 if missing==5 else restore);return True
    elif address==0x23e4de0:trace.append('clear-save')
    elif address==0x1f881f8:trace.append('cloche,'+str(si(b)))
    elif address==0x222fef8:trace.append('duke')
    elif address==0x2116ec4:trace.append('token')
    elif address==0x1e18778:trace.append('episode-win')
    elif address==0x1f71a24:trace.append('new-ux')
    elif address==0x25e50d8:trace.append('win-flag');returned(v[33]);return True
    elif address==0x260dea4:assert a==51,a;trace.append('lantern-check')if missing!=28 else None;returned(0 if missing==28 else lantern);return True
    elif address==0x33ea704:trace.append('lantern-seen,'+str(si(a)))
    elif address==0x33fe81c:
        trace.append('nightmare-win')
        if missing==34:put(statics,0)
    elif address==0x28ed5ac:trace.append('story-win,'+str(si(b)))
    elif address==0x1cb5618:trace.append('side-win')
    elif address==0x1e1896c:trace.append('episode-fail')
    elif address==0x3272ed4:trace.append('side-fail')
    elif address==0x27414b4:trace.append('quit-detection')
    elif address==0x3273318:trace.append('fail')
    elif address==0x3386e94:trace.append('stats-fail')
    elif address==0x33890f0:trace.append('stats-win')
    elif address==0x337d258:trace.append('milestone,'+str(si(a)));returned(package);return True
    elif address==0x2aed014:trace.append('package,'+str(si(c)))
    elif address==0x30f6400:assert a==19,a;returned(analytics);return True
    elif address==0x1d16d44:trace.append('milestone-claim')
    elif address==0x151ec38:trace.append('token-purchase')
    elif address==0x2aee654:trace.append('event-win')
    elif address==0x2ae19c8:trace.append('backend,'+str(si(b)))
    elif address==0x32b2f8c:trace.append('madness-valid');returned(v[31]);return True
    elif address==0x1685484:trace.append('try-multiply,'+str(si(a)))
    elif address==0x32732b4:trace.append('event-remaining')
    elif address==0x3dca5c4:
        if missing==22:failure();return True
        trace.append('mission-remaining')
    elif address==0x1d0db50:trace.append('reward,'+str(int(b!=0)))
    else:return False
    returned();return True
extension_hook=user_hook
new_addresses=(0x2aeecb4,0x2aeed78,0x2aecde4,0x23df9e4,0x23e4de0,0x1f881f8,0x222fef8,0x2116ec4,0x1e18778,0x1f71a24,0x25e50d8,0x260dea4,0x33ea704,0x33fe81c,0x28ed5ac,0x1cb5618,0x1e1896c,0x3272ed4,0x27414b4,0x3273318,0x3386e94,0x33890f0,0x337d258,0x2aed014,0x30f6400,0x1d16d44,0x151ec38,0x2aee654,0x2ae19c8,0x32b2f8c,0x1685484,0x32732b4,0x3dca5c4,0x1d0db50)
def adapter(uc,address,size,unused):assert user_hook(address),hex(address)
for address in new_addresses:u.hook_add(UC_HOOK_CODE,adapter,begin=address,end=address)
def setup_user():
    setup();missing=v[35]
    for pointer in(manager,level_owner,session_data,animation,coin,star,cloche,nth,madness,restore,duke,token,token_purchase,episode,new_ux,events,story_data,package,analytics,lantern):u.mem_write(pointer,bytes(0x300))
    if missing==1:put(statics,0)
    if missing==19:put(statics+8,0)
    put(user+0x18,0 if missing==3 else basic);put(user+0x20,0 if missing==4 or v[15]==7 else inventory)
    put(user+0x38,0 if missing==32 else session_data);put(user+0x48,0 if missing==30 else story_data);put(session_data+0x18,0 if missing==2 else active)
    put(active+0x14,v[1],4);put(active+0x3c,v[24],4);put(active+0x50,v[30],4);put(active+0x10,v[36],1)
    put(active+0x60,0 if missing==11 else star);put(star+0x24,v[17],4);put(star+0x28,v[27],4)
    put(active+0x68,0 if missing==31 else cloche);put(cloche+0x28,2,4)
    put(active+0x88,0 if missing==9 else animation);put(animation+0x28,0 if missing==10 else coin)
    put(coin+0x24,v[25],4);put(coin+0x28,v[26],4);put(animation+0x30,0 if missing==12 else nth)
    put(nth+0x24,v[28],4);put(nth+0x28,v[29],4);put(animation+0x40,0 if missing==20 else madness);put(madness+0x28,v[32],4)
    put(manager+0x20,level_owner);put(manager+0x80,events);put(manager+0xa0,events);put(manager+0x188,0 if missing==21 else events)
    put(level_owner+0x10,0 if missing==13 else episode);put(level_owner+0x18,0 if missing==14 else new_ux)
    put(level_owner+0xa8,manager);put(level_owner+0xe0,0 if missing==33 else analytics);put(token+0x20,0 if missing==29 else token_purchase);put(lantern+0x14,v[34],1)
cases=[];expected=[];rng=random.Random(0x2aec860)
values=(-2147483648,-1,0,1,2,3,9,31,14700,14701,2147483647)
for number in range(1800):
    v=[0,rng.choice(values),rng.choice(values),14700,rng.choice((-1,0,1)),rng.randrange(2),rng.choice((0,1,2,5,9,12,15,19)),rng.choice((-1,0,1,2,3)),rng.randrange(2),rng.choice(values),rng.randrange(2),rng.randrange(2),rng.randrange(2),rng.randrange(2),rng.randrange(2),rng.choice((0,0,0,2,3,4,5,6,7,8,9,10,11,12,13)),rng.choice(values),rng.choice(values),slong(rng.getrandbits(64)),0,0,0,0,0,
        rng.choice((0,1,2,3,3,3,4,5,6)),rng.choice(values),rng.choice(values),rng.choice(values),rng.choice(values),rng.choice(values),rng.choice(values),rng.randrange(2),rng.choice(values),rng.choice((0,1,2)),rng.randrange(2),rng.choice((0,0,0,*range(1,23),28,29,30,31,32,33,34)),rng.randrange(2),number%3,rng.randrange(2)]
    if number<120:v[15]=0;v[35]=0;v[24]=3;v[36]=0 # Exercise complete success paths, not only partial-owner failures.
    setup_user();trace=[];kings_reads=0
    call((0x2aec860,0x2aeeb3c,0x1e1c1f8)[v[37]],level_owner if v[37]==2 else manager,0 if v[38]==0 else package)
    state=[si(read(basic+0x14)),si(read(basic+0x18)),si(read(inventory+0x10)),si(read(inventory+0x14)),slong(read(inventory+0x20,8))]
    cases.append(','.join(map(str,v)));expected.append(str(int(fault))+'#'+','.join(map(str,state))+'#'+';'.join(trace))
mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
sources=['SourceCatalog/AccountKernelHarness.cs','SourceCatalog/UserEndHarness.cs','Assets/Foundation/Runtime/OriginalBasicLevelProgress.cs','Assets/Foundation/Runtime/OriginalUserInventoryBalances.cs','Assets/Foundation/Runtime/OriginalUserLevelEnd.cs','Assets/Foundation/Runtime/OriginalLevelEnd.cs','Assets/Foundation/Runtime/OriginalActiveLevelData.cs','Assets/Foundation/Runtime/OriginalSessionValues.cs']
sources+=['Assets/Foundation/Runtime/OriginalLocalAccountLoad.cs','Assets/Foundation/Runtime/OriginalKeyValueStorage.cs']
with tempfile.TemporaryDirectory()as directory:
    exe=Path(directory)/'UserEnd.exe';args=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+','-main:UserEndHarness',*['-r:'+str(mono/'lib/mono/4.5'/path)for path in('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),*[str(PROJECT/path)for path in sources]]
    result=subprocess.run(args,capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    actual=result.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected)
    for index,(a,b)in enumerate(zip(actual,expected)):assert a==b,(index,cases[index],a,b)
report={'passed':True,'cases':len(cases),'scope':USER_SCOPE,'original_rvas':['0x2aec860','0x2aeeb3c','0x1e1c1f8','0x2f82d54','0x28e4f3c','0x28e4bd8','0x28e5320','0x28e5354'],
 'files':[{'path':path,'sha256':hashlib.sha256((PROJECT/path).read_bytes()).hexdigest()}for path in sources]}
(PROJECT/'user_end_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
