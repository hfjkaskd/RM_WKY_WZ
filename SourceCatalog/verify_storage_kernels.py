"""Compare actual ARM64 KeyValueTable/UserKeyValue increment/DatabaseService save lifecycle.

SQLite commands, generic Find/Delete, sync SQL builders, corruption processing,
service lookup and BCL string/number conversion are typed boundaries. Native
row/flag construction, null/default selection, return comparisons, increment
overflow, dirty/commit/begin ordering, frame modulo and re-reading the connection
execute from the original ELF. This does not verify complete SQLite ORM, account
loading, corruption repair, all tables, mobile storage or production startup.
"""
from pathlib import Path
import base64,hashlib,json,random,re,subprocess,tempfile
PROJECT=Path(__file__).resolve().parents[1];STORAGE_SCOPE=__doc__.strip()
source=PROJECT/'SourceCatalog/verify_low_match.py'
exec(compile(source.read_text().split('cases=[];expected=[]')[0],str(source),'exec'))
from unicorn import UC_HOOK_MEM_READ,UC_HOOK_MEM_WRITE
def null_access(uc,access,address,size,value,unused):
    failure();uc.emu_stop()
u.hook_add(UC_HOOK_MEM_READ|UC_HOOK_MEM_WRITE,null_access,begin=0,end=0xff)
db,connection,other,boxed,strings=0x10200000,0x10201000,0x10202000,0x10203000,0x10210000
v=[];raw=None;value=fallback=0;next_string=strings
def enc(s):return '~'if s is None else base64.b64encode(s.encode()).decode()
def string(s):
    global next_string
    if s is None:return 0
    data=s.encode('utf-16-le');p=next_string;next_string+=0x200
    put(p+0x10,len(data)//2,4);u.mem_write(p+0x14,data);return p
def text(p):return bytes(u.mem_read(p+0x14,read(p+0x10)*2)).decode('utf-16-le')if p else None
def signed64(n):return(n&0x7fffffffffffffff)-(n&0x8000000000000000)
def sql_fail():
    if v[2]==3:failure();return True
    return False
def storage_hook(uc,address,size,unused):
    a,b,c=x(0),x(1),x(2)
    if address==0xffda6c:
        u.mem_write(boxed+0x10,bytes(u.mem_read(b,24)));returned(boxed);return
    if address in(0x2653db0,0x2654ec4):returned(string(str(si(read(a))if address==0x2653db0 else signed64(read(a,8)))));return
    if address in(0x26544c4,0x2655c48):
        s=text(a);bits=32 if address==0x26544c4 else 64
        number=int(s.strip())if s is not None and re.fullmatch(r'[+-]?[0-9]+',s.strip())else None
        valid=number is not None and -(1<<(bits-1))<=number<(1<<(bits-1))
        put(b,number if valid else 0,bits//8);returned(int(valid));return
    if address==0x1fdfcf4:
        if read(a+0x10,8)==0:failure();return
        trace.append('find')
        if sql_fail():return
        out=x(8);put(out,string(None if v[8]==0 else 'Coins'if v[8]==1 else ''));put(out+8,string(raw));put(out+16,v[6],4);put(out+20,v[7],4);returned();return
    if address==0x32190d0:
        row=b+0x10;trace.append('insert,'+enc(text(read(row,8)))+','+enc(text(read(row+8,8)))+','+str(si(read(row+16)))+','+str(si(read(row+20))))
        if sql_fail():return
        returned(v[1]);return
    if address in(0x1fdf0a4,0x25d269c,0x25d27cc):
        put(a+0x1c,1,1)
        if read(a+0x10,8)==0:failure();return
        trace.append('delete'if address==0x1fdf0a4 else ('synced,'if address==0x25d269c else'blocked,')+str(si(c)))
        if sql_fail():return
        returned(v[1]);return
    if address==0x3218950:
        trace.append('commit,'+str(int(a==other)))
        if v[2]==4:failure();return
        if v[2]==7:put(db+0x10,other)
    elif address==0x32181c0:
        trace.append('begin,'+str(int(a==other)))
        if v[2]==5:failure();return
    elif address==0x25d22a4:
        trace.append('clean')
        if v[2]==6:failure();return
    elif address==0x3e42614:returned(v[5]);return
    elif address==0x25e4f5c:returned(0 if v[2]==1 else db);return
    else:raise AssertionError(hex(address))
    returned()
for address in(0xffda6c,0x2653db0,0x2654ec4,0x26544c4,0x2655c48,0x1fdfcf4,0x32190d0,0x1fdf0a4,0x25d269c,0x25d27cc,0x3218950,0x32181c0,0x25d22a4,0x3e42614,0x25e4f5c):
    u.hook_add(UC_HOOK_CODE,storage_hook,begin=address,end=address)
cases=[];expected=[];rng=random.Random(0x25d4a44)
rv=(0x25d4a44,0x25d4bf0,0x25d4e00,0x25d4b44,0x25d4cf4,0x25d4f04,0x25d4928,0x25d9834,0x25d98a8,0x25d98cc,0x25d49b4,0x25d2234,0x25d21f8,0x25d2268,0x25d2284,0x25d23ec,0x25e5100)
values=(-9223372036854775808,-2147483649,-2147483648,-1,0,1,61,2147483647,2147483648,9223372036854775807)
for n in range(1360):
    v=[n%17,rng.choice((-1,0,1,2)),rng.randrange(8),rng.randrange(2),rng.choice((0,-1,1,61,67,2147483647)),rng.choice((0,1,60,61,122,-61,-2147483648,2147483647)),rng.choice((-1,0,1,2)),rng.choice((-1,0,1,2)),rng.randrange(3),rng.randrange(2)]
    value=rng.choice(values);fallback=rng.choice(values);raw=rng.choice((None,'','0','  +17\t','-1','12x','1,000','2147483648','-2147483648','9223372036854775807','-9223372036854775808','9223372036854775808','四','a\0b','中文🚀'))
    next_string=strings;u.mem_write(db,bytes(0x100));put(db+0x10,0 if v[2]==2 else connection);put(db+0x18,v[4],4);put(db+0x1c,v[3],1)
    owner=0 if v[2]==1 else db;key=string('Coins');payload=string(raw);trace=[]
    mode=v[0]
    if mode==0:result=call(rv[mode],owner,key,payload,v[9])
    elif mode in(1,2):result=call(rv[mode],owner,key,value if mode==2 else si(value),v[9])
    elif mode==3:
        result=call(rv[mode],owner,key,string('fallback'));result=enc(text(x(0)))if not fault else'0'
    elif mode in(4,5):result=call(rv[mode],owner,key,fallback if mode==5 else si(fallback));result=signed64(x(0))if mode==5 and not fault else result
    elif mode in(6,7,10):result=call(rv[mode],owner,key)
    elif mode in(8,9):result=call(rv[mode],owner,key,v[9])
    elif mode==16:result=call(rv[mode],key,si(value),si(fallback))
    else:call(rv[mode],owner);result=0
    if fault:result=0
    dirty=0 if v[2]==1 else read(db+0x1c,1)
    cases.append(','.join(map(str,v))+'|'+str(value)+'|'+str(fallback)+'|'+enc(raw))
    expected.append(str(int(fault))+'#'+str(dirty)+','+str(result)+'#'+';'.join(trace))
mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
sources=['SourceCatalog/StorageKernelHarness.cs','Assets/Foundation/Runtime/OriginalKeyValueStorage.cs']
with tempfile.TemporaryDirectory()as directory:
    exe=Path(directory)/'StorageKernel.exe';args=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',*['-r:'+str(mono/'lib/mono/4.5'/p)for p in('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),*[str(PROJECT/p)for p in sources]]
    result=subprocess.run(args,capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    actual=result.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected)
    for i,(a,b)in enumerate(zip(actual,expected)):assert a==b,(i,cases[i],a,b)
report={'passed':True,'cases':len(cases),'scope':STORAGE_SCOPE,'original_rvas':[hex(a)for a in rv]+['0x25ce920'],
 'files':[{'path':p,'sha256':hashlib.sha256((PROJECT/p).read_bytes()).hexdigest()}for p in sources]}
(PROJECT/'storage_kernels_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
