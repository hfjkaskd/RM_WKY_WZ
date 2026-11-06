"""Compare native unlimited-booster string/BitArray restoration.

Actual native initialization, signed-byte array construction, length selection
and bit update loops execute. Storage/key serialization, BCL Split/TryParse,
allocation and BitArray constructors/setters are typed boundaries. This does
not restore booster use timers or prove production/mobile account composition.
"""
from pathlib import Path
import base64,hashlib,json,random,re,subprocess,tempfile
PROJECT=Path(__file__).resolve().parents[1];BOOSTER_SCOPE=__doc__.strip()
source=PROJECT/'SourceCatalog/verify_low_match.py'
exec(compile(source.read_text().split('\nfor addr in(')[0],str(source),'exec'))
metadata=0x10080000;inventory=0x10200000;string_pool=0x10210000;allocation=0x10300000;next_string=string_pool;arrays={};raw=None
def string(value):
    global next_string
    if value is None:return 0
    data=value.encode('utf-16-le');p=next_string;next_string+=max(0x100,len(data)+0x30)
    put(p+0x10,len(data)//2,4);u.mem_write(p+0x14,data);return p
def text(p):return bytes(u.mem_read(p+0x14,read(p+0x10)*2)).decode('utf-16-le')if p else None
def allocate():
    global allocation
    p=allocation;allocation+=0x1000;u.mem_write(p,bytes(0x1000));return p
def hook(uc,address,size,unused):
    a,b,c=x(0),x(1),x(2)
    if address in(0xffd960,0xffd90c,0xffda64,0xffda88,0xffda90):boundary(uc,address,size,unused);return
    if address==0xffda78:returned(allocate());return
    if address==0xffd9d0:
        p=allocate();put(p+0x18,b,4);returned(p);return
    if address==0x25d0324:returned(0x102f0000);return
    if address==0x25e2cc8:returned(string(raw));return
    if address==0x1d72154:returned(int(not text(a)));return
    if address==0x1d74d2c:
        assert b==0xa7
        parts=text(a).split('\u00a7');p=allocate();put(p+0x18,len(parts),4)
        for index,part in enumerate(parts):put(p+0x20+8*index,string(part))
        returned(p);return
    if address==0x2617ba0:
        value=text(a);number=int(value.strip())if re.fullmatch(r'[+-]?[0-9]+',value.strip())else None
        valid=number is not None and -128<=number<=127;put(b,number if valid else 0,1);returned(int(valid));return
    if address==0x2657780:returned(max(si(a),si(b)));return
    if address in(0x32ef658,0x32ef660):arrays[a]=[bool(c)if address==0x32ef660 else False]*si(b);returned();return
    if address==0x32ef9dc:
        index=si(b);assert 0<=index<len(arrays[a]);arrays[a][index]=bool(c);returned();return
    raise AssertionError(hex(address))
for address in(0xffd960,0xffd90c,0xffda64,0xffda88,0xffda90,0xffda78,0xffd9d0,0x25d0324,0x25e2cc8,0x1d72154,0x1d74d2c,0x2617ba0,0x2657780,0x32ef658,0x32ef660,0x32ef9dc):
    u.hook_add(UC_HOOK_CODE,hook,begin=address,end=address)
cases=[];expected=[];rng=random.Random(0x2d7b268)
parts=('','-128','127','128','-129','0','-1','invalid',' +7 ','١','0x7f','四')
for index in range(420):
    raw=None if index==0 else ''if index==1 else '\u00a7'.join(rng.choice(parts)if rng.randrange(3)==0 else str(rng.randrange(-128,128))for _ in range(rng.choice((1,2,3,23,24,25,32,64))))
    arrays={};allocation=0x10300000;next_string=string_pool;u.mem_write(inventory,bytes(0x200));fault=False
    for i in range(8):u.reg_write(getattr(r,'UC_ARM64_REG_X'+str(i)),inventory if i==0 else 0)
    u.reg_write(r.UC_ARM64_REG_SP,0x103ff000);u.reg_write(r.UC_ARM64_REG_LR,0x20000000)
    u.emu_start(0x28ea480,0x20000000,count=100000);assert u.reg_read(r.UC_ARM64_REG_PC)==0x20000000 and not fault
    values=arrays[read(inventory+0xd8,8)];out=bytearray((len(values)+7)//8)
    for bit,enabled in enumerate(values):
        if enabled:out[bit//8]|=1<<(bit%8)
    expected.append(str(len(values))+':'+base64.b64encode(out).decode());cases.append('~'if raw is None else base64.b64encode(raw.encode()).decode())
mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
sources=['SourceCatalog/LocalBoosterInventoryHarness.cs','SourceCatalog/AccountKernelHarness.cs','Assets/Foundation/Runtime/OriginalLocalAccountLoad.cs','Assets/Foundation/Runtime/OriginalKeyValueStorage.cs','Assets/Foundation/Runtime/OriginalBasicLevelProgress.cs','Assets/Foundation/Runtime/OriginalUserInventoryBalances.cs','Assets/Foundation/Runtime/OriginalActiveLevelData.cs']
with tempfile.TemporaryDirectory()as directory:
    exe=Path(directory)/'BoosterLoad.exe';args=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+','-main:LocalBoosterInventoryHarness',*['-r:'+str(mono/'lib/mono/4.5'/p)for p in('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),*[str(PROJECT/p)for p in sources]]
    result=subprocess.run(args,capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    actual=result.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected)
    for index,(a,b)in enumerate(zip(actual,expected)):assert a==b,(index,cases[index],a,b)
report={'passed':True,'cases':len(cases),'scope':BOOSTER_SCOPE,'original_rvas':['0x28ea480','0x2d7b268','0x2d7b2d0','0x2d7b3bc','0x2d7b1dc'],
 'files':[{'path':p,'sha256':hashlib.sha256((PROJECT/p).read_bytes()).hexdigest()}for p in sources]}
(PROJECT/'local_booster_inventory_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
