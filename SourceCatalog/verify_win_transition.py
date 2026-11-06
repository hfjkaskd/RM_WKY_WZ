"""Execute native WinFail.Win side effects after the actual state transition.

King animation feature/owner, item-creator lookup, fixed metadata ratio-array
allocation, LifeHelper and Ego owners are explicit typed boundaries. Actual ARM64
order, branching, story gate and null-owner partial failures execute. No King
rendering, life persistence or production application-provider binding is proved.
"""
from pathlib import Path
import hashlib,json,subprocess,tempfile
PROJECT=Path(__file__).resolve().parents[1];SCOPE=__doc__.strip()
source=PROJECT/'SourceCatalog/verify_low_match.py'
exec(compile(source.read_text().split('cases=[];expected=[]')[0],str(source),'exec'))
manager,game_statics,root,game_data,game_ui,king_ui,factory,item_creator,session,level_manager,ego,life,array=[0x100c0000+i*0x1000 for i in range(13)]
# GameContext's actual relocated metadata slot and required static owner chain.
slot=read(0x5351eb0,8);game_class=0x1000f000;put(slot,game_class);put(game_class+0xe0,1,4);put(game_class+0x132,1,1);put(game_class+0xb8,game_statics)
put(game_statics+8,root);put(root+0x20,game_data);put(game_data+0x18,game_ui)
v=[]
def win_hook(uc,address,size,unused):
    a,b=x(0),x(1)
    if address==0x1edbcdc:trace.append('state-win')
    elif address==0x23e743c:trace.append('animation');returned(v[0]);return
    elif address in(0x18c9f68,0x18c9d08):trace.append('happy'if address==0x18c9f68 else'stop')
    elif address==0xffd9d0:put(array+0x18,b,4);returned(array);return
    elif address==0x22bc630:
        trace.append('ratios')
        for i,value in enumerate((10,45,45)):put(array+0x20+4*i,value,4)
    elif address==0x23f0a34:trace.append('update,'+','.join(str(si(read(b+0x20+4*i)))for i in range(read(b+0x18))))
    elif address==0x29e27cc:assert a==3;trace.append('life-owner');returned(0 if v[2]==4 else life);return
    elif address==0x1e1e25c:trace.append('lives,'+str(si(b)))
    elif address==0x32b6638:trace.append('story');return # Run the real native IsStory getter.
    elif address==0x32614e0:trace.append('ego')
    else:raise AssertionError(hex(address))
    returned()
for addr in(0x1edbcdc,0x23e743c,0x18c9f68,0x18c9d08,0xffd9d0,0x22bc630,0x23f0a34,0x29e27cc,0x1e1e25c,0x32b6638,0x32614e0):
    u.hook_add(UC_HOOK_CODE,win_hook,begin=addr,end=addr)
cases=[];expected=[]
for improved in(0,1):
    for story in(0,1):
        for missing in range(8):
            v=[improved,story,missing];trace=[]
            put(manager+0x10,root);put(manager+0x48,0 if missing==2 else factory);put(factory+0x38,0 if missing==3 else item_creator)
            put(game_ui+0xa0,0 if missing==1 else king_ui);put(manager+0x50,0 if missing==5 else session);put(session+0x34,-1 if story else 0,4)
            put(manager+0x30,0 if missing==6 else level_manager);put(level_manager+0x20,0 if missing==7 else ego)
            call(0x1edd734,manager)
            cases.append(','.join(map(str,v)));expected.append(str(int(fault))+'#'+';'.join(trace))
mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
sources=['SourceCatalog/WinTransitionHarness.cs','Assets/Foundation/Runtime/OriginalWinTransition.cs']
with tempfile.TemporaryDirectory()as directory:
    exe=Path(directory)/'WinTransition.exe';args=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',
        *['-r:'+str(mono/'lib/mono/4.5'/p)for p in('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),*[str(PROJECT/p)for p in sources]]
    result=subprocess.run(args,capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    actual=result.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected)
    for i,(a,b)in enumerate(zip(actual,expected)):assert a==b,(i,cases[i],a,b)
report={'passed':True,'cases':len(cases),'scope':SCOPE,'original_rvas':['0x1edd734','0x32b6638'],
    'files':[{'path':p,'sha256':hashlib.sha256((PROJECT/p).read_bytes()).hexdigest()}for p in sources]}
(PROJECT/'win_transition_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
