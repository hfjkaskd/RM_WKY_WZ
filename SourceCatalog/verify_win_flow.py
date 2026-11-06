"""Run original ARM64 win coordination, level-end dispatch and three dialog actions.

Account, tutorial/download/multiplier/analytics, audio/coroutine host, UiDialog
instantiation/lifecycle and remaining-manager calls are explicit typed boundaries.
Real native UpdateLevelWinData and session league-kind getters execute. Native
diagnostic logging and IL2CPP interface type checks are skipped; no rendered
result dialog, user persistence or production startup binding is established.
"""
from pathlib import Path
import hashlib,itertools,json,struct,subprocess,tempfile
PROJECT=Path(__file__).resolve().parents[1];SCOPE=__doc__.strip()
source=PROJECT/'SourceCatalog/verify_low_match.py'
exec(compile(source.read_text().split('cases=[];expected=[]')[0],str(source),'exec'))
manager,game_statics,game_root,game_data,top,king,session,level,goals,audio,reactivation,token,flow,listener,dialog_manager,assets,dialog,iterator,remaining,user_statics,user,user_data,tutorial,analytics,league=[0x10100000+i*0x1000 for i in range(25)]
for got,statics in((0x5351eb0,game_statics),(0x53bb4e0,user_statics)):
    cls=0x1000e000+(got==0x53bb4e0)*0x400;put(read(got,8),cls);put(cls+0xe0,1,4);put(cls+0x132,1,1);put(cls+0xb8,statics)
actions=[0x10140000+i*0x1000 for i in range(3)];allocation_index=0;speed_reads=0;v=[]
def hook(uc,address,size,unused):
    global allocation_index,speed_reads
    a,b,c=x(0),x(1),x(2);mode,missing=v[0],v[4]
    if address==0x1ede098:u.reg_write(r.UC_ARM64_REG_PC,0x1ede0f4);return
    if address in(0x133f11c,0x133f1ac):u.reg_write(r.UC_ARM64_REG_PC,0x133f148 if address==0x133f11c else 0x133f1d8);return
    if address==0x29e27cc:
        returned((0 if missing==2 else token)if a==69 else(0 if missing==5 else reactivation)if a==56 else(0 if missing==3 else token)if a==79 else(0 if missing==5 else league));return
    if address==0x2116ec0:trace.append('token')
    elif address==0x1e18b58:
        value=v[2]if speed_reads==0 else v[3];speed_reads+=1;trace.append('speed,'+str(value));returned(value);return
    elif address==0x18c9f68:trace.append('happy')
    elif address==0x32146cc:trace.append('goal,'+str(si(b))+','+str(c));returned(v[5]);return
    elif address==0x27427e8:
        trace.append('reactivation')
        if missing==6:put(manager+0x50,0)
    elif address==0x32b8920:trace.append('session,'+str(si(c)))
    elif address==0x1e1bf08:
        if mode==1:return
        trace.append('level-end')
    elif address==0x3e19100:trace.append('fade');returned(iterator);return
    elif address==0x1e2aaac:trace.append('game-coroutine');returned(0x10180000);return
    elif address==0x30f6400:
        if a==14:trace.append('flow-owner');returned(0 if missing==9 else flow);return
        assert a==17,a;returned(0 if missing==1 else listener);return
    elif address==0xffda78:
        if mode==0:
            index=allocation_index;allocation_index+=1;returned(0 if missing==10+index else actions[index]);return
        returned(0x10180000);return
    elif address in(0x18d0540,0x1118bcc,0x133f288):trace.append('new,'+str((0x18d0540,0x1118bcc,0x133f288).index(address)+1))
    elif address==0x35a2448:trace.append('push,'+str(actions.index(b)+1))
    elif address==0x32ba87c:trace.append('useless')
    elif address==0x1e1c03c:trace.append('download')
    elif address==0x3d0f7f0:trace.append('tutorial')
    elif address==0x211512c:trace.append('multiplier')
    elif address==0x1d0c53c:trace.append('analytics')
    elif address==0x2aec860:trace.append('user-end')
    elif address==0x2c9862c:trace.append('disable')
    elif address==0x2c9863c:trace.append('enable')
    elif address==0x3e49528:trace.append('wait,'+format(struct.unpack('<f',struct.pack('<I',u.reg_read(r.UC_ARM64_REG_S0)))[0],'.1f'))
    elif address==0x292ce98:
        label='logo'if mode==2 else'remaining'if mode==3 else'default';trace.append('instantiate-'+label)
        returned(0 if missing==(3 if mode==4 else 5)else dialog);return
    elif address==0x25cb31c:trace.append('show')
    elif address==0x18d0730:trace.append('play')
    elif address==0x260dea4:
        assert a==33,a;trace.append('remaining-owner')if missing!=1 else None;returned(0 if missing==1 else remaining);return
    elif address==0x2183430:trace.append('should-send');returned(v[1]);return
    elif address==0x21834cc:trace.append('immediate')
    elif address==0x20000300:trace.append('complete')
    elif address==0x1630900:trace.append('sorting')
    elif address==0x292cf28:trace.append('instantiate-league');returned(0 if missing==4 else dialog);return
    elif address==0x1e19344:trace.append('hard-bundle');returned(v[2]);return
    elif address==0x1e122bc:trace.append('popup-bundle');returned(0);return
    elif address==0x111e1cc:trace.append('inject,'+str(si(c)))
    elif address==0x133f198:trace.append('league-downloaded');return
    else:raise AssertionError(hex(address))
    returned()
addresses=(0x1ede098,0x133f11c,0x133f1ac,0x29e27cc,0x2116ec0,0x1e18b58,0x18c9f68,0x32146cc,0x27427e8,0x32b8920,0x1e1bf08,0x3e19100,0x1e2aaac,0x30f6400,0xffda78,0x18d0540,0x1118bcc,0x133f288,0x35a2448,0x32ba87c,0x1e1c03c,0x3d0f7f0,0x211512c,0x1d0c53c,0x2aec860,0x2c9862c,0x2c9863c,0x3e49528,0x292ce98,0x25cb31c,0x18d0730,0x260dea4,0x2183430,0x21834cc,0x20000300,0x1630900,0x292cf28,0x1e19344,0x1e122bc,0x111e1cc,0x133f198)
for address in addresses:u.hook_add(UC_HOOK_CODE,hook,begin=address,end=address)
def setup():
    mode,missing=v[0],v[4]
    for address in(manager,game_statics,game_root,game_data,top,king,session,level,goals,audio,reactivation,token,flow,listener,dialog_manager,assets,dialog,iterator,remaining,user_statics,user,user_data,tutorial,analytics,league):u.mem_write(address,bytes(0x200))
    put(manager+0x50,0 if mode==0 and missing==1 else session);put(session+0x28,v[1],1)
    put(manager+0x18,0 if mode==0 and missing==4 else goals);put(manager+0x20,0x10190000)
    put(manager+0x30,0 if mode==0 and missing==7 else level);put(manager+0x40,0 if mode==0 and missing==8 else audio)
    put(game_statics+8,game_root);put(game_root+0x20,game_data);put(game_data+0x18,0 if(mode==4 and missing==2)or(mode==3 and missing==4)else top)
    put(top+0xa0,0 if missing==3 else king)
    put(user_statics,user);put(user+0x38,user_data);put(user_data+0x18,0 if mode in(1,4)and missing==1 else session)
    put(session+0x10,v[1]if mode==1 else 0,1);put(session+0xbc,v[2],1);put(session+0x3c,v[3],4)
    put(level+0xc8,0 if missing==2 else tutorial);put(level+0xe0,0 if missing==4 else analytics);put(level+0xa8,0 if missing==5 else user)
    put(manager+0x20,0 if mode in(2,3)and missing==2 else dialog_manager);put(dialog_manager+0x30,0 if mode in(2,3)and missing==3 else assets)
    put(iterator+0x20,manager);put(iterator+0x10,0,4);put(session+0x2c,v[1],4);put(session+0x30,v[5],1);put(league+0x38,v[3],1)
    klass=0x101a0000;put(manager,klass);put(klass+0x1b8,0x20000300)
cases=[];expected=[]
vectors=[]
for bonus,before,after,missing,coins in itertools.product((0,1),(0,1),(0,1),range(13),(-9,0,71)):
    vectors.append([0,bonus,before,after,missing,coins])
for admin,useful,state,missing in itertools.product((0,1),(0,1),(0,2,3,4),range(6)):vectors.append([1,admin,useful,state,missing,0])
for missing in(0,1,2,3,5):vectors.append([2,0,0,0,missing,0])
for send,missing in itertools.product((0,1),range(6)):vectors.append([3,send,0,0,missing,0])
for index,hard,downloaded,missing,difficulty in itertools.product((-12,-1,0,1,2,5,9,10,12,15,19,2147483647),(0,1),(0,1),range(6),(-1,0,3)):
    vectors.append([4,index,hard,downloaded,missing,difficulty])
for v in vectors:
    setup();trace=[];allocation_index=speed_reads=0
    mode=v[0]
    if mode==2:
        for _ in range(3):
            call(0x18d054c,iterator)
            if fault:break
    else:call((0x1eddefc,0x1e1bf08,0,0x1118a64,0x133ef58)[mode],manager if mode!=1 else level)
    cases.append(','.join(map(str,v)));expected.append(str(int(fault))+'#'+';'.join(trace))
mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
sources=['SourceCatalog/WinFlowHarness.cs','Assets/Foundation/Runtime/OriginalWinFlowCoordinator.cs','Assets/Foundation/Runtime/OriginalLevelEnd.cs','Assets/Foundation/Runtime/OriginalWinDialogActions.cs','Assets/Foundation/Runtime/OriginalActiveLevelData.cs']
with tempfile.TemporaryDirectory()as directory:
    exe=Path(directory)/'WinFlow.exe';args=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',
        *['-r:'+str(mono/'lib/mono/4.5'/p)for p in('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),*[str(PROJECT/p)for p in sources]]
    result=subprocess.run(args,capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    actual=result.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
    for i,(a,b)in enumerate(zip(actual,expected)):assert a==b,(i,cases[i],a,b)
report={'passed':True,'cases':len(cases),'scope':SCOPE,'original_rvas':['0x1eddefc','0x1edea24','0x1e1bf08','0x18d054c','0x1118a64','0x133ef58','0x133f218','0x32b65b8','0x32b66d0','0x32b6700','0x32b6730'],
    'files':[{'path':p,'sha256':hashlib.sha256((PROJECT/p).read_bytes()).hexdigest()}for p in sources]}
(PROJECT/'win_flow_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
