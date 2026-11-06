"""Actual ARM64 loss-flow, out-of-moves banner and Ego action vs production C#.

External session fail/refund, account/event/King/audio, resource instantiation,
UiDialog rendering/lifecycle and FlowManager.Push/Clear are recorded boundaries.
The original loss order, event callback deferral, offer selection and banner
league/portal/bundle decisions execute in the ELF. No production scene binding,
actual failure persistence, rendered popup or mobile parity is established.
"""
from pathlib import Path
import hashlib,itertools,json,struct,subprocess,tempfile
PROJECT=Path(__file__).resolve().parents[1];SCOPE=__doc__.strip()
source=PROJECT/'SourceCatalog/verify_low_match.py'
exec(compile(source.read_text().split('cases=[];expected=[]')[0],str(source),'exec'))
manager,audio,ego,session,moves,level,flow,lava,ancient,game_statics,game_root,game_data,top,king,user_statics,user,user_data,league,dialog_manager,assets,dialog,bundle,action,callback,cls=[0x10100000+i*0x1000 for i in range(25)]
v=[];created=0;callbacks=[]

def hook(uc,address,size,unused):
    global created
    a,b,c=x(0),x(1),x(2);missing=v[1]
    # Skip diagnostic allocation/boxing/logging only, preserving real dispatch.
    skips={0x1ede544:0x1ede5ec,0x1ede704:0x1ede770,0x1ede8ac:0x1ede920}
    if address in skips:
        # Restore the metadata-cell pointers loaded inside diagnostic blocks;
        # subsequent real dispatch still reads their actual initialized values.
        cells={0x1ede544:{25:0x534ab58,23:0x53e3018,22:0x538cd78},
               0x1ede704:{20:0x534ab58,21:0x53e3018},
               0x1ede8ac:{22:0x53649d8,20:0x5359a78,23:0x536d610}}[address]
        for register,got in cells.items():u.reg_write(getattr(r,'UC_ARM64_REG_X'+str(register)),read(got,8))
        u.reg_write(r.UC_ARM64_REG_PC,skips[address]);return
    if address==0x30f6400:assert a==14;trace.append('flow');returned(0 if missing==6 else flow);return
    if address==0x29e27cc:
        if a in(34,68):trace.append('lava-owner'if a==34 else'ancient-owner');returned(0 if missing==(8 if a==34 else 9)else lava if a==34 else ancient)
        elif a==2:
            if missing==12:failure();return
            returned(level)
        elif a==5:
            if missing==13:failure();return
            returned(league)
        else:raise AssertionError(('user-unit',a))
        return
    if address==0xffda78:
        delegate=u.reg_read(r.UC_ARM64_REG_LR)in(0x1ede94c,0x1ede9bc)
        returned(callback if delegate else action if missing!=10 else 0);return
    if address==0x2044a78:
        assert b==manager and c==callback+0x100,(hex(b),hex(c))
        callbacks.append((a,c));returned();return
    if address==0x3e15f20:trace.append('stop,'+str(b))
    elif address==0x18ca2b0:trace.append('sad')
    elif address==0x1ca4d64:trace.append('ego')
    elif address==0x32b9e70:trace.append('no-move-session')
    elif address==0x32ba36c:trace.append('fail-session')
    elif address==0x1e1be38:trace.append('no-move-end')
    elif address==0x1e1bf08:trace.append('level-end')
    elif address==0x1631718:trace.append('increase-ui')
    elif address==0x3e17838:
        value=struct.unpack('<f',struct.pack('<I',u.reg_read(r.UC_ARM64_REG_S0)))[0];trace.append('sound,'+str(b)+','+format(value,'.2f'))
    elif address==0x1cb5e00:trace.append('lava,'+str(b));returned(v[2]);return
    elif address==0x20e9f74:trace.append('ancient,'+str(c)+','+str(x(3)));returned(v[3]);return
    elif address==0x38c6aa4:trace.append('offer-gate');returned(v[4]);return
    elif address in(0x3d520b8,0x1595914,0x1595624,0x38c5b14,0x1594d38):
        created={0x3d520b8:1,0x1595914:2,0x1595624:3,0x38c5b14:4,0x1594d38:5}[address]
        trace.append({1:'new-duke,'+str(b),2:'new-banner',3:'new-no-move',4:'new-offer,'+str(b)+','+str(c),5:'new-fail,'+str(b)}[created])
    elif address==0x35a2448:trace.append('push,'+str(created))
    elif address==0x2002494:
        if missing==1:failure();return
        trace.append('orientation,'+str(a))
    elif address==0x2002270:
        if missing==2:failure();return
        trace.append('landscape');returned(v[2]);return
    elif address in(0x1630900,0x13bd09c):
        if missing==(3 if address==0x1630900 else 4):failure();return
        trace.append('top-sorting'if address==0x1630900 else'bottom-sorting')
    elif address in(0x158a88c,0x158a8ac):trace.append('flow');return
    elif address==0x35a2030:trace.append('clear,'+str(b))
    elif address==0x158a8dc:
        created=6;trace.append('new-ego,'+str(si(read(action+0x30)))+','+str(read(action+0x34,1))+','+str(si(read(action+0x38))));return
    elif address in(0x15956fc,0x1594af0):trace.append('session');return
    elif address in(0x15957ec,0x1594bdc):trace.append('league-downloaded');return
    elif address==0x359f9f8:returned();return # Base DialogAction owner binding is a boundary.
    elif address==0x1e1d6c8:trace.append('portal,'+str(v[3]));returned(v[3]);return
    elif address==0x1e19344:trace.append('hard-bundle');returned(v[4]);return
    elif address==0x1e122bc:
        if missing==14:failure();return
        trace.append('load-bundle');returned(bundle if v[6]else 0);return
    elif address in(0x292ce98,0x292cf28):
        if missing==15:failure();return
        trace.append('default'if address==0x292ce98 else'league-banner');returned(0 if missing==16 else dialog);return
    elif address==0x134aef8:
        if missing==17:failure();return
        assert b==bundle;trace.append('initialize')
    elif address==0x1345fc4:
        if missing==17:failure();return
        trace.append('init,'+str(si(b))+','+str(c)+','+str(si(u.reg_read(r.UC_ARM64_REG_S0))))
    elif address==0x25cb31c:
        if missing==18:failure();return
        trace.append('show')
    elif address==0x20000300:trace.append('complete')
    else:raise AssertionError(hex(address))
    returned()

addresses=(0x1ede544,0x1ede704,0x1ede8ac,0x30f6400,0x29e27cc,0xffda78,0x2044a78,0x3e15f20,0x18ca2b0,0x1ca4d64,0x32b9e70,0x32ba36c,0x1e1be38,0x1e1bf08,0x1631718,0x3e17838,0x1cb5e00,0x20e9f74,0x38c6aa4,0x3d520b8,0x1595914,0x1595624,0x38c5b14,0x1594d38,0x35a2448,0x2002494,0x2002270,0x1630900,0x13bd09c,0x158a88c,0x158a8ac,0x35a2030,0x158a8dc,0x15956fc,0x15957ec,0x1594af0,0x1594bdc,0x359f9f8,0x1e1d6c8,0x1e19344,0x1e122bc,0x292ce98,0x292cf28,0x134aef8,0x1345fc4,0x25cb31c,0x20000300)
for address in addresses:u.hook_add(UC_HOOK_CODE,hook,begin=address,end=address)

def setup():
    missing=v[1]
    for address in(manager,audio,ego,session,moves,level,flow,lava,ancient,game_statics,game_root,game_data,top,king,user_statics,user,user_data,league,dialog_manager,assets,dialog,bundle,action,callback,cls):u.mem_write(address,bytes(0x400))
    for address in range(0x573ec8a,0x573ec91):put(address,1,1)
    for address in(0x573b267,0x573b2a2,0x573b2a3,0x573b2a8,0x573b2a9,0x573963a,0x573963e):put(address,1,1)
    for got,statics in((0x5353668,0),(0x534ab58,0),(0x5351eb0,game_statics),(0x53bb4e0,user_statics),(0x53649d8,0),(0x536b340,0)):
        put(read(got,8),cls);put(cls+0xe0,1,4);put(cls+0x132,1,1)
        # Each static-owning class needs its own descriptor.
        if statics:
            descriptor=statics+0x400;put(read(got,8),descriptor);put(descriptor+0xe0,1,4);put(descriptor+0x132,1,1);put(descriptor+0xb8,statics)
    put(manager+0x40,0 if missing==1 else audio);put(manager+0x58,0 if missing==3 else ego)
    put(manager+0x50,0 if missing==4 else session);put(manager+0x20,moves);put(manager+0x30,0 if missing==5 else level)
    put(game_statics+8,game_root);put(game_root+0x20,game_data);put(game_data+0x18,0 if missing==7 and v[0]!=7 else top);put(top+0xa0,0 if missing==2 else king);put(game_data+0x20,level)
    put(user_statics,0 if missing==11 else user);put(user+0x38,user_data);put(user_data+0x18,0 if missing==4 else session)
    put(session+0x2c,v[2],4);put(league+0x38,v[5],1)
    put(manager+0x20,0 if missing==15 else dialog_manager)if v[0]>=5 else None
    put(dialog_manager+0x30,assets);put(assets+0x20,0x10130000)
    put(manager,cls);put(cls+0x1b8,0x20000300);put(cls+0x208,0x1594a18)
    put(manager+0x70,0 if missing==6 else flow)
    # ScriptMetadataMethod at relocated cell90103664 identifies the actual
    # WinFailManager.ShowFailDialog callback (MethodAddress32369384).
    put(read(0x53b9168,8),callback+0x100);put(callback+0x100,0x1edeae8)

vectors=[]
for mode,missing,lava_active,ancient_active,offer,repeat in itertools.product(range(5),range(11),(0,1),(0,1),(0,1),(0,1,3)):
    vectors.append([mode,missing,lava_active,ancient_active,offer,repeat,0])
for missing,index,portal,hard,downloaded,loaded in itertools.product((0,4,11,12,13,14,15,16,17,18),(-12,0,1,2,5,9,12,19,2147483647),(0,1),(0,1),(0,1),(0,1,2)):
    vectors.append([5,missing,index,portal,hard,downloaded,loaded])
banner_vectors=[item for item in vectors if item[0]==5]
for index,item in enumerate(banner_vectors):
    for origin,add,bits in ((0,0,1045220557),(3,1,-2147483648),(-2147483648,index%2,2143289344)):
        vectors.append([6,*item[1:],origin,add,bits])
for missing,landscape in itertools.product(range(11),(0,1)):vectors.append([7,missing,landscape,0,0,0,0])
cases=[];expected=[]
for v in vectors:
    setup();trace=[];created=0;callbacks=[]
    if v[0]in(5,6):trace.append('type,'+str(call(0x159562c if v[0]==5 else 0x1594a20,manager)))
    if v[0]==6:
        u.reg_write(r.UC_ARM64_REG_S0,v[9]&0xffffffff);call(0x158a8fc,manager,v[7],v[8]);assert not fault
    try:call((0x1ede230,0x1ede420,0x1ede668,0x1ede7f0,0x1edeae8,0x1595634,0x1594a28,0x158a754)[v[0]],manager)
    except Exception as error:raise AssertionError((v,trace,hex(u.reg_read(r.UC_ARM64_REG_PC)),hex(u.reg_read(r.UC_ARM64_REG_LR))))from error
    if v[0]==3 and not fault and v[5]and(v[2]or v[3]):
        assert len(callbacks)==(1 if v[2]else 2)
        for _ in range(v[5]):
            trace.append('callback');call(read(callbacks[-1][1],8),manager)
            if fault:break
    cases.append(','.join(map(str,v)));expected.append(str(int(fault))+'#'+';'.join(trace))
mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
sources=['SourceCatalog/LossFlowHarness.cs','Assets/Foundation/Runtime/OriginalLossFlowCoordinator.cs','Assets/Foundation/Runtime/OriginalOutOfMovesBannerAction.cs','Assets/Foundation/Runtime/OriginalEgoDialogAction.cs','Assets/Foundation/Runtime/OriginalOutOfMovesContinuation.cs','Assets/Foundation/Runtime/OriginalActiveLevelData.cs']
with tempfile.TemporaryDirectory()as directory:
    exe=Path(directory)/'LossFlow.exe'
    command=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',*['-r:'+str(mono/'lib/mono/4.5'/p)for p in('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),*[str(PROJECT/p)for p in sources]]
    result=subprocess.run(command,capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    actual=result.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
    for i,(a,b)in enumerate(zip(actual,expected)):assert a==b,(i,cases[i],a,b)
report={'passed':True,'cases':len(cases),'loss_cases':sum(v[0]<5 for v in vectors),'banner_cases':sum(v[0]==5 for v in vectors),'ego_cases':sum(v[0]==6 for v in vectors),'continuation_cases':sum(v[0]==7 for v in vectors),'scope':SCOPE,'original_rvas':['0x1ede230','0x1ede420','0x1ede668','0x1ede7f0','0x1edeae8','0x159562c','0x1595634','0x15958a4','0x158a8fc','0x1594a20','0x1594a28','0x1594cbc','0x158a754'],
    'files':[{'path':p,'sha256':hashlib.sha256((PROJECT/p).read_bytes()).hexdigest()}for p in sources]}
(PROJECT/'loss_flow_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
