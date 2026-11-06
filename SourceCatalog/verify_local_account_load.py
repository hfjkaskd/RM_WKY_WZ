"""Execute native SyncManager local loading and identity/packed-progress decoding.

UserKeyValue reads/BCL parsing, wall clock, home-area parsing and unlimited-booster
string-to-BitArray are typed boundaries. Native field/default/write ordering,
captured references, null/partial failure, Pinata/Madness defaults, RoyalPass and
TeamOffer decoding execute from the ELF. Diagnostic formatting is skipped. SQL
reload/booster parsing are checked separately. Production startup/mobile, home
area contents and complete feature ownership are not proved by this kernel test.
"""
from pathlib import Path
import base64,hashlib,json,random,re,subprocess,tempfile
PROJECT=Path(__file__).resolve().parents[1];LOAD_SCOPE=__doc__.strip()
source=PROJECT/'SourceCatalog/verify_low_match.py'
exec(compile(source.read_text().split('\nfor addr in(')[0],str(source),'exec'))
def call(addr,*args):
    global fault
    fault=False
    for i in range(8):u.reg_write(getattr(r,'UC_ARM64_REG_X'+str(i)),(args[i]if i<len(args)else 0)&0xffffffffffffffff)
    u.reg_write(r.UC_ARM64_REG_SP,0x103ff000);u.reg_write(r.UC_ARM64_REG_LR,0x20000000)
    u.emu_start(addr,0x20000000,count=20000);assert u.reg_read(r.UC_ARM64_REG_PC)==0x20000000
from unicorn import UC_HOOK_MEM_READ,UC_HOOK_MEM_WRITE
def null_access(uc,access,address,size,value,unused):failure();uc.emu_stop()
u.hook_add(UC_HOOK_MEM_READ|UC_HOOK_MEM_WRITE,null_access,begin=0,end=0xff)
metadata=0x10080000
proof=json.loads((PROJECT/'SourceCatalog/local_account_load_native_keys.json').read_text())
literals={int(item['metadata_address'],16):item['entry']['Value']for item in proof['keys']}
keys=[item['entry']['Value']for item in proof['keys']if item['field']!='byte_array_prefix']+['ByteArrayKeyValue12']
literal_pool=0x10100000;strings=0x10180000;next_string=strings
def enc(s):return '~'if s is None else base64.b64encode(s.encode()).decode()
def string(s,literal=False):
    global literal_pool,next_string
    if s is None:return 0
    p=literal_pool if literal else next_string;data=s.encode('utf-16-le')
    if literal:literal_pool+=0x200
    else:next_string+=0x400
    put(p+0x10,len(data)//2,4);u.mem_write(p+0x14,data);return p
def text(p):return bytes(u.mem_read(p+0x14,read(p+0x10)*2)).decode('utf-16-le')if p else None
literal_ptrs={slot:string(value,True)for slot,value in literals.items()};empty=string('',True);old=string('old',True)
user,basic,inventory,identity,log,story,new_basic,new_inventory,other,statics,string_statics=[0x10200000+i*0x1000 for i in range(11)]
for slot,klass_address,storage in((0x53bb4e0,0x1000e000,statics),(0x53a7138,0x1000e400,string_statics)):
    put(read(slot,8),klass_address);put(klass_address+0xe0,1,4);put(klass_address+0x132,1,1);put(klass_address+0xb8,storage)
put(string_statics,empty)
v=[];data=[];reads=0;now=0;bits={}
def slong(n):return(n&0x7fffffffffffffff)-(n&0x8000000000000000)
def mutate():
    if reads==v[2]:
        if v[3]==1:put(user+0x18,new_basic)
        elif v[3]==2:put(user+0x20,new_inventory)
        elif v[3]==3:put(user+0x18,0)
        elif v[3]==4:put(user+0x20,0)
        elif v[3]==5:put(statics,other)
def fetch(key):
    global reads
    trace.append('get,'+key);reads+=1
    if reads==v[1]:failure();return None
    mutate();assert key in keys,(key,trace,hex(x(0)));return data[keys.index(key)]
def number(raw,bits,fallback):
    n=int(raw.strip())if raw is not None and re.fullmatch(r'[+-]?[0-9]+',raw.strip())else None
    return n if n is not None and -(1<<(bits-1))<=n<(1<<(bits-1))else fallback
def parse_bits(raw):
    if not raw:return (192,bytes(24))
    fields=raw.split('\u00a7');result=bytearray(max(24,len(fields)))
    for index,part in enumerate(fields):result[index]=number(part,8,0)&255
    return len(result)*8,bytes(result)
def hook(uc,address,size,unused):
    a,b=x(0),x(1)
    if address==0xffd960:
        if a in literal_ptrs:put(a,literal_ptrs[a]);returned();return
        boundary(uc,address,size,unused);return
    if address in(0xffd90c,0xffda64,0xffda88,0xffda90):boundary(uc,address,size,unused);return
    if address in(0x25e50d8,0x25e5178,0x25e2cc8):
        key=text(a);raw=fetch(key)
        if fault:return
        if address==0x25e2cc8:returned(b if raw is None else string(raw))
        else:returned(number(raw,32 if address==0x25e50d8 else 64,si(b)if address==0x25e50d8 else slong(b)))
        return
    if address==0x29d6ef8:
        trace.append('now')
        if v[0]==8:failure();return
        returned(now);return
    if address==0x337ef10:u.reg_write(r.UC_ARM64_REG_PC,0x337f094);return # Identity diagnostic-only formatting.
    if address==0x28e5648:u.reg_write(r.UC_ARM64_REG_X19,a);u.reg_write(r.UC_ARM64_REG_PC,0x28e56f0);return # Default inventory diagnostic-only formatting.
    if address==0x28ea480:
        raw=fetch('ByteArrayKeyValue12')
        if fault:return
        put(a+0xe0,string(raw));bits[a]=parse_bits(raw);put(a+0xd8,a+0x800);returned();return
    if address==0x3d1f298:
        trace.append('area')
        if v[0]==7:failure();return
        returned();return
    raise AssertionError(hex(address))
for address in(0xffd960,0xffd90c,0xffda64,0xffda88,0xffda90,0x25e50d8,0x25e5178,0x25e2cc8,0x29d6ef8,0x337ef10,0x28e5648,0x28ea480,0x3d1f298):
    u.hook_add(UC_HOOK_CODE,hook,begin=address,end=address)
def basic_state(p):return ','.join(map(str,[si(read(p+0x10)),si(read(p+0x14)),si(read(p+0x18)),slong(read(p+0x28,8)),slong(read(p+0x30,8)),slong(read(p+0x38,8)),si(read(p+0x1c))]))
def inventory_state(p):
    state=[si(read(p+offset))if offset in(0x10,0x14,0x18,0x1c,0x38,0x3c,0x40)else slong(read(p+offset,8))for offset in(0x10,0x14,0x18,0x1c,0x20,0x28,0x30,0x38,0x3c,0x40,0x48,0x50,0x58,0x60,0xb0,0xb8)]
    royal=p+0x68;state.extend([slong(read(royal+i,8))for i in(0,8,16)])
    state.extend([read(royal+0x18,1),si(read(royal+0x1c)),si(read(royal+0x20)),si(read(royal+0x24)),si(read(royal+0x28)),read(royal+0x2c,1),si(read(royal+0x30)),read(royal+0x34,1),read(royal+0x35,1)])
    team=p+0xa0;state.extend([slong(read(team+8,8)),si(read(team)),*[read(team+i,1)for i in(4,5,6,7)]])
    state.append(enc(text(read(p+0xe0,8))));state.append('~'if p not in bits else str(bits[p][0])+':'+base64.b64encode(bits[p][1]).decode())
    return ','.join(map(str,state))
cases=[];expected=[];rng=random.Random(0x337e714)
for index in range(900):
    v=[0 if index<120 else rng.randrange(9),0 if index<120 else rng.choice((0,0,rng.randrange(1,37))),0 if index<180 else rng.choice((0,0,rng.randrange(1,37))),rng.randrange(1,6)]
    now=1700000000000+index*1000
    data=[None if index==0 or rng.randrange(5)==0 else rng.choice(('0','-1','2147483648','-2147483648',str(slong(rng.getrandbits(64))),str(rng.randrange(100000)),'invalid',''))for key in keys]
    for key in('UserName','AppleId','GoogleId','LWM','KNLWM','KNLW'):data[keys.index(key)]=rng.choice((None,'','saved_中文','old'))if index else None
    data[-1]=rng.choice((None,'','-128\u00a7127\u00a7bad\u00a7\u00a70','\u00a7'.join(str(rng.randrange(-150,151))for _ in range(30))))if index else None
    next_string=strings;bits={};reads=0;trace=[]
    for p in(user,basic,inventory,identity,log,story,new_basic,new_inventory,other):u.mem_write(p,bytes(0x300))
    put(statics,0 if v[0]==1 else user)
    for offset,p,missing in((0x10,identity,2),(0x18,basic,3),(0x20,inventory,4),(0x30,log,5),(0x48,story,6)):put(user+offset,0 if v[0]==missing else p)
    for offset,value in((0x10,11),(0x14,22),(0x18,33),(0x1c,77)):put(basic+offset,value,4)
    for offset,value in((0x28,44),(0x30,55),(0x38,66)):put(basic+offset,value)
    put(new_basic+0x14,72,4);put(new_basic+0x18,73,4)
    for p,coin,star,event in((inventory,44,55,66),(new_inventory,84,85,86)):put(p+0x10,coin,4);put(p+0x14,star,4);put(p+0x20,event)
    put(identity+0x20,14);put(identity+0x30,13)
    for p in(identity+0x28,identity+0x38,identity+0x40,log+0x10,story+0x10,story+0x18):put(p,old)
    call(0x337e714)
    state=[int(read(statics,8)!=0)+(int(read(statics,8)==other)),0 if read(user+0x18,8)==0 else 1 if read(user+0x18,8)==basic else 2,0 if read(user+0x20,8)==0 else 1 if read(user+0x20,8)==inventory else 2,0]
    identity_state=','.join(map(str,[enc(text(read(identity+0x28,8))),slong(read(identity+0x30,8)),slong(read(identity+0x20,8)),enc(text(read(identity+0x38,8))),enc(text(read(identity+0x40,8))),enc(text(read(log+0x10,8))),enc(text(read(story+0x10,8))),enc(text(read(story+0x18,8)))]))
    expected.append(str(int(fault))+'#'+','.join(map(str,state))+'#'+basic_state(basic)+'#'+basic_state(new_basic)+'#'+inventory_state(inventory)+'#'+inventory_state(new_inventory)+'#'+identity_state+'#'+';'.join(trace))
    cases.append(','.join(map(str,v))+'|'+str(now)+'|'+','.join(map(enc,data)))
mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
sources=['SourceCatalog/LocalAccountLoadHarness.cs','SourceCatalog/AccountKernelHarness.cs','Assets/Foundation/Runtime/OriginalLocalAccountLoad.cs','Assets/Foundation/Runtime/OriginalKeyValueStorage.cs','Assets/Foundation/Runtime/OriginalBasicLevelProgress.cs','Assets/Foundation/Runtime/OriginalUserInventoryBalances.cs','Assets/Foundation/Runtime/OriginalActiveLevelData.cs']
with tempfile.TemporaryDirectory()as directory:
    exe=Path(directory)/'LocalAccount.exe';args=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+','-main:LocalAccountLoadHarness',*['-r:'+str(mono/'lib/mono/4.5'/p)for p in('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),*[str(PROJECT/p)for p in sources]]
    result=subprocess.run(args,capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    actual=result.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected)
    for index,(a,b)in enumerate(zip(actual,expected)):assert a==b,(index,cases[index],a,b)
report={'passed':True,'cases':len(cases),'scope':LOAD_SCOPE,'original_rvas':['0x337e714','0x337ed84','0x337f0c8','0x337f124','0x30d2110','0x28dc430','0x30cc1e0','0x28e55b8'],
 'files':[{'path':p,'sha256':hashlib.sha256((PROJECT/p).read_bytes()).hexdigest()}for p in sources]}
(PROJECT/'local_account_load_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
