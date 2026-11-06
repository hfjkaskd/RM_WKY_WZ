"""Execute native BasicUserData progression and UserInventory balance/packing code.

Real native level/league updates, arithmetic, clamping, delegates and Madness
packing execute. UserKeyValue storage writes, feature gates, leaderboard/mission,
analytics, side-progression/story helpers are explicit typed boundaries. Native
diagnostic formatting is skipped. This does not prove database loading, real
save files, event providers or production account startup composition.
"""
from pathlib import Path
import hashlib,json,random,subprocess,tempfile
PROJECT=Path(__file__).resolve().parents[1];SCOPE=__doc__.strip()
source=PROJECT/'SourceCatalog/verify_low_match.py'
exec(compile(source.read_text().split('cases=[];expected=[]')[0],str(source),'exec'))
basic,inventory,active,statics,user,level_statics,progress,stats=[0x10200000+i*0x1000 for i in range(8)]
for got,storage,klass_address in((0x53bb4e0,statics,0x1000e000),(0x536b340,level_statics,0x1000e400)):
    put(read(got,8),klass_address);put(klass_address+0xe0,1,4);put(klass_address+0x132,1,1);put(klass_address+0xb8,storage)
v=[];kings_reads=0;extension_hook=None
def slong(value):return (value&0x7fffffffffffffff)-(value&0x8000000000000000)
def hook(uc,address,size,unused):
    global kings_reads
    if extension_hook is not None and extension_hook(address):return
    a,b,c=x(0),x(1),x(2);missing=v[15]
    if address==0x2f82674:
        u.reg_write(r.UC_ARM64_REG_X23,read(0x53649d8,8));u.reg_write(r.UC_ARM64_REG_X24,0x103f0000);u.reg_write(r.UC_ARM64_REG_PC,0x2f82748);return
    if address==0x2f8287c:
        u.reg_write(r.UC_ARM64_REG_X23,0x103f0000);u.reg_write(r.UC_ARM64_REG_PC,0x2f82948);return
    if address in(0x28e4e1c,0x28e4c44):
        u.reg_write(r.UC_ARM64_REG_X24,0x103f0000);u.reg_write(r.UC_ARM64_REG_PC,0x28e4ee8 if address==0x28e4e1c else 0x28e4d10);return
    if address==0x28e5408:
        u.reg_write(r.UC_ARM64_REG_X20,a);u.reg_write(r.UC_ARM64_REG_X24,0x103f0000);u.reg_write(r.UC_ARM64_REG_PC,0x28e54dc);return
    if address==0x28e5648:u.reg_write(r.UC_ARM64_REG_X19,a);u.reg_write(r.UC_ARM64_REG_PC,0x28e56f0);return
    if address==0x29e27cc:
        if a in(7,8,16,28):
            trace.append('get,'+str(a));returned(0 if missing=={7:2,8:3,16:4,28:5}[a]else 0x10210000+a*0x100);return
        assert a==1,a;returned(0x10210000);return
    if address==0x28e68fc:trace.append('mark-side')
    elif address==0x1685500:trace.append('can-multiply');returned(v[8]);return
    elif address==0x1687e1c:trace.append('multiplier');returned(v[9]);return
    elif address==0x1ca6468:
        value=v[10]if kings_reads==0 else v[11];kings_reads+=1;trace.append('kings,'+str(value));returned(value);return
    elif address in(0x23f3708,0x2404f68,0x282b9d8):
        index={0x23f3708:12,0x2404f68:13,0x282b9d8:14}[address];trace.append({12:'battle,',13:'treasure,',14:'weekly,'}[index]+str(v[index]));returned(v[index]);return
    elif address in(0x25db268,0x25dbbd4):trace.append(('my-level,'if address==0x25db268 else'league-score,')+','.join(str(si(x(i)))for i in range(5)))
    elif address==0x2f832e4:trace.append('story')
    elif address==0x3dc1158:trace.append('mission,'+str(si(a))+','+str(si(b)))
    elif address==0x3389234:trace.append('crowns,'+str(si(b)))
    elif address==0x2af1b64:trace.append('level-changed,'+str(si(b)))
    elif address==0x25e50ac:
        ret=u.reg_read(r.UC_ARM64_REG_LR)
        if ret in(0x2f82774,0x2f82974):trace.append('clear-win')
        elif ret in(0x2f82784,0x2f82984):trace.append('clear-other')
        else:
            label,fail={0x2f827a0:('level',9),0x2f82998:('league',10),0x28e4d24:('star',12)}[ret];trace.append(label+','+str(si(b)))
            if missing==fail:failure();return
    elif address==0x25e514c:
        ret=u.reg_read(r.UC_ARM64_REG_LR);label,fail=('coin',11)if ret==0x28e4efc else('event',13)
        assert ret in(0x28e4efc,0x28e54f0),hex(ret);trace.append(label+','+str(slong(b)))
        if missing==fail:failure();return
    elif address==0x1ff92e4:trace.append('transaction,'+str(si(a)))
    elif address==0x20000600:trace.append('coin-updated')
    elif address==0x20000610:trace.append('stars-updated')
    else:raise AssertionError(hex(address))
    returned()
addresses=(0x2f82674,0x2f8287c,0x28e4e1c,0x28e4c44,0x28e5408,0x28e5648,0x29e27cc,0x28e68fc,0x1685500,0x1687e1c,0x1ca6468,0x23f3708,0x2404f68,0x282b9d8,0x25db268,0x25dbbd4,0x2f832e4,0x3dc1158,0x3389234,0x2af1b64,0x25e50ac,0x25e514c,0x1ff92e4,0x20000600,0x20000610)
for address in addresses:u.hook_add(UC_HOOK_CODE,hook,begin=address,end=address)
def setup():
    for pointer in(basic,inventory,active,statics,user,level_statics,progress,stats):u.mem_write(pointer,bytes(0x300))
    put(basic+0x14,v[1],4);put(basic+0x18,v[2],4);put(level_statics+0x10,v[3],4)
    put(active+0x34,v[4],4);put(active+0x39,v[5],1);put(active+0x2c,v[6],4);put(active+0x30,v[7],1)
    put(statics,0 if v[15]==6 else user);put(statics+8,0 if v[15]==8 else stats);put(user+0x20,0 if v[15]==7 else inventory)
    put(inventory+0x10,v[16],4);put(inventory+0x14,v[17],4);put(inventory+0x20,v[18]);put(progress,v[21],4);put(progress+4,v[22],4);put(progress+8,v[23],4)
    for offset,target in((0xd0,0x20000600),(0xc0,0x20000610)):
        event=inventory+offset+0x100;put(inventory+offset,event);put(event+0x18,target)
cases=[];expected=[];rng=random.Random(0x2f82d54)
values=(-2147483648,-14701,-1,0,1,2,3,5,9,10,12,15,19,31,14700,14701,2147483647)
for number in range(1500):
    mode=number%7;v=[mode,rng.choice(values),rng.choice(values),rng.choice((1,14700,2147483647)),rng.choice((-1,0,1)),rng.randrange(2),rng.choice(values),rng.choice((-1,0,1,2,3,4)),rng.randrange(2),rng.choice(values),rng.randrange(2),rng.randrange(2),rng.randrange(2),rng.randrange(2),rng.randrange(2),rng.randrange(14),rng.choice(values),rng.choice(values),slong(rng.getrandbits(64)),rng.choice(values),rng.randrange(2),rng.choice(values),rng.choice(values),rng.choice(values)]
    if number<14:v[18]=0;v[21]=v[22]=v[23]=0 # Include the native zero->default event inventory path.
    setup();trace=[];kings_reads=0;packed=0
    if mode==0:call(0x2f82d54,basic,0 if v[15]==1 else active)
    elif mode in(1,2,3):call({1:0x28e4db0,2:0x28e4bd8,3:0x28e4f3c}[mode],inventory,v[19],v[20])
    elif mode==4:call(0x2f96544,progress,v[18]);packed=slong(x(0))if not fault else 0
    elif mode==5:call(0x28e5354,inventory,(v[21]&0xffffffff)|((v[22]&0xffffffff)<<32),v[23])
    else:
        call(0x28e5320,inventory);trace.append('madness,'+str(si(x(0)))+','+str(si(x(0)>>32))+','+str(si(x(1))))
    state=[si(read(basic+0x14)),si(read(basic+0x18)),si(read(inventory+0x10)),si(read(inventory+0x14)),slong(read(inventory+0x20,8)),packed]
    cases.append(','.join(map(str,v)));expected.append(str(int(fault))+'#'+','.join(map(str,state))+'#'+';'.join(trace))
mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
sources=['SourceCatalog/AccountKernelHarness.cs','Assets/Foundation/Runtime/OriginalBasicLevelProgress.cs','Assets/Foundation/Runtime/OriginalUserInventoryBalances.cs','Assets/Foundation/Runtime/OriginalActiveLevelData.cs']
sources+=['Assets/Foundation/Runtime/OriginalLocalAccountLoad.cs','Assets/Foundation/Runtime/OriginalKeyValueStorage.cs']
with tempfile.TemporaryDirectory()as directory:
    exe=Path(directory)/'AccountKernel.exe';args=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',*['-r:'+str(mono/'lib/mono/4.5'/path)for path in('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),*[str(PROJECT/path)for path in sources]]
    result=subprocess.run(args,capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    actual=result.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected)
    for index,(a,b)in enumerate(zip(actual,expected)):assert a==b,(index,cases[index],a,b)
report={'passed':True,'cases':len(cases),'scope':SCOPE,'original_rvas':['0x2f82d54','0x2f825d4','0x2f827f8','0x28e4db0','0x28e4bd8','0x28e4f3c','0x28e5320','0x28e5354','0x28e5390','0x28e55b8','0x2f9645c','0x2f96544','0x2f965e0'],
 'files':[{'path':path,'sha256':hashlib.sha256((PROJECT/path).read_bytes()).hexdigest()}for path in sources]}
(PROJECT/'account_kernels_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
