"""Execute native FillFourColors/ContainsSpecialItem/RemainingMovesStarted.

Actual native array/list element mutations, weight retention, availability reset,
match/special membership and complete per-set end-phase loop run. Managed
allocation, list/dictionary storage and empty quota ownership are supplied at
typed boundaries. The independent Unity cases exercise these methods on the
actual formal board with actual weighted refill and low-match state.
"""
from pathlib import Path
import hashlib,json,random,subprocess,sys,tempfile
PROJECT=Path(__file__).resolve().parents[1]
SCOPE=__doc__.strip()
source=PROJECT/'SourceCatalog/verify_low_match.py'
exec(compile(source.read_text().split('cases=[];expected=[]')[0],str(source),'exec'))
set_owner=0x100a0000;array=0x100a1000;creator=0x100a2000;heap=0x10100000
def allocate(n=0x100):
    global heap
    p=heap;heap+=n;u.mem_write(p,bytes(n));return p
def make_list(p,values):
    data=allocate(0x400);put(p+0x10,data);put(data+0x18,64,4);put(p+0x18,len(values),4)
    for i,v in enumerate(values):put(data+0x20+8*i,v)
def contents(p):return [read(read(p+0x10,8)+0x20+8*i,8)for i in range(read(p+0x18))]
def integer_contents(p):return [si(read(read(p+0x10,8)+0x20+4*i))for i in range(read(p+0x18))]
def set_hook(uc,address,size,unused):
    a,b,c=x(0),x(1),x(2)
    if address==0xffda78:returned(allocate());return
    if address==0x231007c:returned();return
    if address==0x314e86c:
        data=allocate(0x400);put(data+0x18,64,4);put(a+0x10,data);put(a+0x18,0,4);returned();return
    if address==0x315057c:
        vals=integer_contents(a)
        if si(b)in vals:vals.remove(si(b));result=1
        else:result=0
        put(a+0x18,len(vals),4)
        for i,v in enumerate(vals):put(read(a+0x10,8)+0x20+4*i,v,4)
        returned(result);return
    if address in(0x2c1b9a0,0x314eddc):
        count=read(a+0x18);i=si(b)
        if not 0<=i<count:failure();return
        returned(read(read(a+0x10,8)+0x20+i*(8 if address==0x2c1b9a0 else 4),8 if address==0x2c1b9a0 else 4));return
    if address==0x3c963d0:returned(0);return # Real empty quota owner in these cases.
    if address==0x3c96364:returned();return
    raise AssertionError(hex(address))
for addr in(0xffda78,0x231007c,0x314e86c,0x315057c,0x2c1b9a0,0x314eddc,0x3c963d0,0x3c96364):u.hook_add(UC_HOOK_CODE,set_hook,begin=addr,end=addr)
rng=random.Random(0x23f0d00);cases=[];expected=[]
for n in range(600):
    mode=n%2;can_fall=(n//2)%2;ids=[rng.choice((1,2,3,4,5,6,10,20,30,40,50,70))for _ in range(n%7)]
    weights=[rng.choice((1,1,2,3,2147483647))for _ in ids]
    heap=0x10100000;u.mem_write(set_owner,bytes(0x200));lst=allocate();put(set_owner+0x18,lst);put(set_owner+0x38,can_fall,1);put(set_owner+0x40,allocate());put(set_owner+0x50,0,1)
    entries=[]
    for a,b in zip(ids,weights):p=allocate();put(p+0x10,a,4);put(p+0x14,b,4);entries.append(p)
    make_list(lst,entries);put(creator+0x20,array);put(array+0x18,1,4);put(array+0x20,set_owner)
    for _ in range(2):
        call(0x1910830,set_owner)if mode==0 else call(0x23f0d00,creator)
        if fault:break
    failed=fault;elems=contents(lst)
    snapshot=','.join(str(si(read(p+0x10)))for p in elems)+'/'+','.join(str(si(read(p+0x14)))for p in elems)+'/'+','.join(str(read(p+0x18,1))for p in elems)+'/'+str(read(set_owner+0x50,1))+','+str(si(read(set_owner+0x54)))+','+str(si(read(set_owner+0x88)))
    special=call(0x1910c68,set_owner);assert not fault
    cases.append(str(mode)+'|'+str(can_fall)+'|'+','.join(str(a)+':'+str(b)for a,b in zip(ids,weights)))
    expected.append(str(int(failed))+'#'+snapshot+'#'+str(special))
mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
sources=['SourceCatalog/RemainingItemCreationHarness.cs','Assets/Foundation/Runtime/OriginalRemainingItemCreation.cs','Assets/Foundation/Runtime/OriginalLevelSetSelection.cs','Assets/Foundation/Runtime/OriginalConfigData.cs','Assets/Foundation/Runtime/OriginalParsedLevelSet.cs','Assets/Foundation/Runtime/OriginalLevelRules.cs','Assets/Foundation/Runtime/OriginalTiledItemTypes.cs']
with tempfile.TemporaryDirectory()as directory:
    exe=Path(directory)/'EndSets.exe';args=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',
        *['-r:'+str(mono/'lib/mono/4.5'/p)for p in('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),*[str(PROJECT/p)for p in sources]]
    result=subprocess.run(args,capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    actual=result.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected)
    for i,(a,b)in enumerate(zip(actual,expected)):assert a==b,(i,cases[i],a,b)
report={'passed':True,'cases':len(cases),'scope':SCOPE,'original_rvas':['0x1910830','0x1910c68','0x23f0d00'],
    'files':[{'path':p,'sha256':hashlib.sha256((PROJECT/p).read_bytes()).hexdigest()}for p in sources]}
(PROJECT/'remaining_item_creation_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
