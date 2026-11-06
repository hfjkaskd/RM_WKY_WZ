"""Execute original ARM64 area parser/control flow and compare actual runtime C#.

Storage rows, config objects/lengths, asset availability, published area count,
managed allocation and BCL string/base64/byte/SQL boundaries are supplied.
Original captured/re-read user state, initial/legacy/NextArea/server-limit/
missing-bundle branches, task construction, model publication and call order
execute from ELF. Only diagnostic formatting/logging is skipped. This is not
complete account startup, download/Home rendering, all area model methods or
mobile execution proof. A separate Unity check uses real configs and SQLite.
"""
from pathlib import Path
import sys,json,hashlib,itertools,random,subprocess,tempfile,base64,struct
PROJECT=Path(__file__).resolve().parents[1];BASE=PROJECT.parent
sys.path.insert(0,str(BASE/'tools/python'))
from elftools.elf.elffile import ELFFile
from elftools.elf.relocation import RelocationSection
from unicorn import Uc,UC_ARCH_ARM64,UC_MODE_ARM,UC_HOOK_CODE
import unicorn.arm64_const as r
u=Uc(UC_ARCH_ARM64,UC_MODE_ARM);u.mem_map(0,0x6000000);u.mem_map(0x10000000,0x400000);u.mem_map(0x20000000,0x1000)
def put(p,v,n=8):u.mem_write(p,(int(v)&((1<<(8*n))-1)).to_bytes(n,'little'))
def read(p,n=8):return int.from_bytes(u.mem_read(p,n),'little')
def signed(v):return (int(v)&0xffffffff)-0x100000000 if int(v)&0x80000000 else int(v)&0xffffffff
def x(i):return u.reg_read(getattr(r,'UC_ARM64_REG_X'+str(i)))
def returned(v=0):u.reg_write(r.UC_ARM64_REG_X0,v&0xffffffffffffffff);u.reg_write(r.UC_ARM64_REG_PC,u.reg_read(r.UC_ARM64_REG_LR))
with(BASE.parent/'RoyalMatch_20260929/apk_unpacked/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so').open('rb')as stream:
 elf=ELFFile(stream)
 for s in elf.iter_segments():
  if s['p_type']=='PT_LOAD':u.mem_write(s['p_vaddr'],s.data())
 for s in elf.iter_sections():
  if isinstance(s,RelocationSection):
   for e in s.iter_relocations():
    if e['r_info_type']==1027:put(e['r_offset'],e['r_addend'])
area,second,user,user_static,home,container,config,helper,builder_class,string_static=[0x10001000+i*0x1000 for i in range(10)]
user_location=read(0x53bb4e0);string_location=read(0x53a7138)
strings={read(0x53c3b88):'Area',read(0x539e428):'NextArea'}
metadata=0x10200000;constant_heap=0x10100000;heap=0x10300000;v=[];trace=[];calls=0;fault=False;dirty=False;rows={};builders={};queries={}
primitive_mode=False;byte_lists={}
def alloc(size=0x100):
 global heap
 p=heap;heap+=(size+15)&~15;assert heap<0x103e0000;u.mem_write(p,bytes(size));return p
def string(value,constant=False):
 global constant_heap
 if value is None:return 0
 data=value.encode('utf-16-le');size=0x14+len(data)+2
 if constant:p=constant_heap;constant_heap+=(size+15)&~15;u.mem_write(p,bytes(size))
 else:p=alloc(size)
 put(p+0x10,len(data)//2,4);u.mem_write(p+0x14,data);return p
empty=string('',True)
def text(p):return None if not p else bytes(u.mem_read(p+0x14,read(p+0x10,4)*2)).decode('utf-16-le')
def array(length,step=8):
 if length<0:fail();return 0
 p=alloc(0x20+step*length);put(p+0x18,length,4);return p
def bytes_array(data):p=array(len(data),1);u.mem_write(p+0x20,data);return p
def fail():
 global fault
 fault=True;u.reg_write(r.UC_ARM64_REG_PC,0x20000000)
def step(label):
 global calls
 trace.append(label);calls+=1
 if calls==v[10]:fail();return False
 return True
def bits(p):
 assert p
 result=0
 for i in range(read(p+0x18,4)):
  if read(p+0x24+i*8,1):result|=1<<(i&31)
 return signed(result)
def hook(uc,address,size,unused):
 global metadata,dirty
 a,b,c=x(0),x(1),x(2)
 if address==0xffd960:
  if a in strings:put(a,string(strings[a],True))
  else:
   put(a,metadata);put(metadata+0xe0,1,4)
   if a==user_location:put(metadata+0xb8,user_static);put(user_static,user)
   if a==string_location:put(metadata+0xb8,string_static);put(string_static,empty)
   metadata+=0x200
 elif address in(0xffd90c,0xffda64,0x2a2b5fc):pass
 elif address in(0xffda88,0xffda90,0xffdaac):fail();return
 elif address==0xffda78:p=alloc();put(p,builder_class);returned(p);return
 elif address==0xffd9d0:
  p=array(signed(b))
  if not fault:returned(p)
  return
 elif address in(0x3d1f34c,0x3d1f568,0x3d1c6f8):
  if not step('user'):return
  return
 elif address==0x3d1bbec:
  if not step('new'):return
 elif address==0x2608558:
  assert a==0
  if not step('home'):return
  put(home+0x10,0 if v[9]==2 else container);returned(home);return
 elif address==0x3d1be0c:
  if not step('load:'+str(signed(b))):return
  put(config+0x28,0 if v[9]==4 else array(v[4]));returned(0 if v[9]==3 else config);return
 elif address==0x30f6400:
  assert a==8
  if not step('assets'):return
  returned(0 if v[9]==5 else helper);return
 elif address==0x3d1b190:
  identity=signed(b)
  if not step('has:'+str(identity)):return
  returned(int(v[8]==2 or v[8]==1 and identity<=1));return
 elif address==0x3d1dfc4:
  if not step('playable'):return
  returned(v[7]);return
 elif address==0x25e2cc8:
  key=text(a)
  if not step('get:'+key):return
  if key=='Area'and v[11]==1:put(user+0x28,second)
  returned(string(rows[key][0])if key in rows else b);return
 elif address==0x25e3900:
  key,value=text(a),text(b);dirty=True
  if not step('set:'+key+':'+value):return
  rows[key]=[value,0,1];returned(1);return
 elif address==0x25e5094:
  key=text(a);dirty=True
  if not step('delete:'+key):return
  found=key in rows;rows.pop(key,None);returned(int(found));return
 elif address==0x25e5240:
  key=text(a);assert b==1;dirty=True
  if not step('blocked:'+key+':0'):return
  if key in rows:rows[key][2]=0
  if not step('synced:'+key+':1'):return
  if key in rows:rows[key][1]=1
  returned(int(key in rows));return
 elif address==0x1d72154:returned(int(text(a)in(None,'')));return
 elif address==0x2053c4c:
  try:data=base64.b64decode(''.join(text(a).split()),validate=True)
  except Exception:fail();return
  returned(bytes_array(data));return
 elif address==0x1d74d2c:
  fields=text(a).split(chr(b));p=array(len(fields))
  for i,value in enumerate(fields):put(p+0x20+i*8,string(value))
  returned(p);return
 elif address==0x26540e0:
  try:value=int(text(a));assert -0x80000000<=value<0x80000000
  except Exception:fail();return
  returned(value);return
 elif address==0x20bb960:
  if not a:fail();return
  p=alloc();queries[p]=(a,max(0,signed(b)),None);returned(p);return
 elif address==0x20bbc70:
  source,start,_=queries[a];p=alloc();queries[p]=(source,start,max(0,signed(b)));returned(p);return
 elif address==0x20bc784:
  source,start,count=queries[a];length=read(source+0x18,4);data=bytes(u.mem_read(source+0x20,length))[start:start+count];returned(bytes_array(data));return
 elif address==0x2047a88:
  if not a or read(a+0x18,4)<4:fail();return
  returned(read(a+0x20,4));return
 elif address==0x1cc3cd4:builders[a]=''
 elif address in(0x1cc5fb4,0x1cc5eb4):builders[a]+=str(signed(b))if address==0x1cc5fb4 else chr(b);returned(a);return
 elif address==0x20000610:returned(string(builders[a]));return
 elif address==0x3d1f12c:u.reg_write(r.UC_ARM64_REG_PC,0x3d1f1ac);return # SaveRealAreaData diagnostics only.
 elif address==0x3d1f718:u.reg_write(r.UC_ARM64_REG_PC,0x3d1f7c4);return # Delete-success diagnostic only.
 elif address==0x28e3b14:u.reg_write(r.UC_ARM64_REG_X23,read(0x53c3b88));u.reg_write(r.UC_ARM64_REG_PC,0x28e3c2c);return # UpdateAreaData diagnostic only.
 elif address==0x28e3a10:
  if primitive_mode:return
  data=struct.pack('<iii',signed(read(a+0x10,4)),signed(read(a+0x18,4)),bits(read(a+0x20)));returned(string(base64.b64encode(data).decode()));return
 elif address==0x20476c0:returned(bytes_array(struct.pack('<i',signed(a))));return
 elif address==0x1f0bfc4:byte_lists[a]=b''
 elif address==0x1f0ca84:byte_lists[a]+=bytes(u.mem_read(b+0x20,read(b+0x18,4)))
 elif address==0x1f0e2e0:returned(bytes_array(byte_lists[a]));return
 elif address==0x2053114:returned(string(base64.b64encode(bytes(u.mem_read(a+0x20,read(a+0x18,4)))).decode()));return
 elif address==0xd02590:
  exponent=signed(a);value=2.0**exponent if exponent<128 else float('inf');u.reg_write(r.UC_ARM64_REG_S0,int.from_bytes(struct.pack('<f',value),'little'));returned();return
 else:raise AssertionError(hex(address))
 returned()
hooks=(0xffd960,0xffd90c,0xffda64,0xffda88,0xffda90,0xffdaac,0xffda78,0xffd9d0,0x3d1f34c,0x3d1f568,0x3d1c6f8,0x3d1bbec,0x2608558,0x3d1be0c,0x30f6400,0x3d1b190,0x3d1dfc4,0x25e2cc8,0x25e3900,0x25e5094,0x25e5240,0x1d72154,0x2053c4c,0x1d74d2c,0x26540e0,0x20bb960,0x20bbc70,0x20bc784,0x2047a88,0x1cc3cd4,0x1cc5fb4,0x1cc5eb4,0x20000610,0x3d1f12c,0x3d1f718,0x28e3b14,0x28e3a10,0x2a2b5fc,0xd02590)
hooks+=(0x20476c0,0x1f0bfc4,0x1f0ca84,0x1f0e2e0,0x2053114)
for a in hooks:u.hook_add(UC_HOOK_CODE,hook,begin=a,end=a)
def snapshot(p):
 tasks=read(p+0x20);values='-'if not tasks else'/'.join(str(signed(read(tasks+0x20+i*8,4)))+':'+str(read(tasks+0x24+i*8,1))for i in range(read(tasks+0x18,4)))
 return','.join(str(signed(read(p+offset,4)))for offset in(0x10,0x14,0x18))+','+values
def row(key):return '~'if key not in rows else','.join(map(str,rows[key]))
cases=[]
for op,identity,length,missing,fail_at in itertools.product(range(4),(-1,0,1,4),(0,6,33),range(6),range(11)):
 cases.append((op,identity,3,-1,length,1,1,2,1,missing,fail_at,0))
rng=random.Random(0x3d1f298)
for i in range(600):cases.append((rng.randrange(4),rng.choice((-3,0,1,2,8)),rng.randrange(5),rng.randrange(-2**31,2**31),rng.choice((0,1,6,13,33)),rng.randrange(6),rng.randrange(5),rng.choice((1,4,166)),rng.randrange(3),rng.randrange(6),rng.randrange(12),rng.randrange(2)))
expected=[]
for v in cases:
 heap=0x10300000;trace=[];calls=0;fault=False;dirty=False;rows={};builders={};queries={}
 for p,identity in((area,v[1]),(second,12)):
  u.mem_write(p,bytes(0x100));put(p+0x10,identity,4);put(p+0x14,9,4);put(p+0x18,11,4)
  tasks=array(2);put(tasks+0x20,4,4);put(tasks+0x24,1,1);put(tasks+0x28,2,4);put(p+0x20,tasks)
 put(user+0x28,0 if v[9]==1 else area);put(builder_class+0x168,0x20000610)
 payload=struct.pack('<iii',v[1],v[2],v[3]);stored=None if v[5]==0 else base64.b64encode(payload).decode()if v[5]==1 else'!'if v[5]==2 else base64.b64encode(payload[:4]).decode()if v[5]==3 else base64.b64encode(payload+bytes(4)).decode()if v[5]==4 else''
 next_value=None if v[6]==0 else str(v[1]+2)+'§'+str(v[3])+'§'+str(v[2])if v[6]==1 else'bad§4§2'if v[6]==2 else'2§3'if v[6]==3 else'2§3§4§99'
 if stored is not None:rows['Area']=[stored,0,1]
 if next_value is not None:rows['NextArea']=[next_value,0,1]
 args=(0,0,0,0)if v[0]in(0,2)else(0 if v[9]==1 else area,v[1],v[3],v[2])
 for i in range(8):u.reg_write(getattr(r,'UC_ARM64_REG_X'+str(i)),args[i]if i<len(args)else 0)
 u.reg_write(r.UC_ARM64_REG_SP,0x103ff000);u.reg_write(r.UC_ARM64_REG_LR,0x20000000)
 try:u.emu_start((0x3d1f298,0x3d1eeb8,0x3d1c674,0x3d1f5b4)[v[0]],0x20000000,count=50000)
 except Exception as error:raise RuntimeError((v,hex(u.reg_read(r.UC_ARM64_REG_PC))))from error
 assert u.reg_read(r.UC_ARM64_REG_PC)==0x20000000,(v,hex(u.reg_read(r.UC_ARM64_REG_PC)))
 expected.append(str(int(fault))+'#'+snapshot(area)+'#'+snapshot(second)+'#'+str(int(dirty))+'#'+row('Area')+'#'+row('NextArea')+'#'+';'.join(trace))
mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
sources=['SourceCatalog/AreaDataHarness.cs']+['Assets/Foundation/Runtime/'+name+'.cs'for name in('OriginalAreaDataParser','OriginalUserAreaData','OriginalAreaConfig','OriginalAreaConfigContainer','OriginalKeyValueStorage')]
with tempfile.TemporaryDirectory()as directory:
 exe=Path(directory)/'Area.exe';command=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',*['-r:'+str(mono/'lib/mono/4.5'/p)for p in('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),*[str(PROJECT/p)for p in sources]]
 result=subprocess.run(command,capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(','.join(map(str,case))for case in cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 actual=result.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected)
 for i,(a,b)in enumerate(zip(actual,expected)):assert a==b,(i,cases[i],a,b)
primitive_mode=True;primitive_cases=[];primitive_expected=[]
for mode,length,mask in itertools.product(('B','E'),(-1,0,1,6,13,30,31,32,33,64,128,129),(-2147483648,-1,0,1,5,2147483647)):
 primitive_cases.append(','.join(map(str,(mode,length,mask,-2147483648,2147483647))))
for length,index in itertools.product((-1,0,1,3,4,5,12),(-2147483648,-3,0,1,4,100,2147483647)):primitive_cases.append('G,'+str(length)+','+str(index)+',0,0')
def primitive_call(address,*args):
 global fault
 fault=False
 for i in range(8):u.reg_write(getattr(r,'UC_ARM64_REG_X'+str(i)),args[i]if i<len(args)else 0)
 u.reg_write(r.UC_ARM64_REG_SP,0x103ff000);u.reg_write(r.UC_ARM64_REG_LR,0x20000000);u.emu_start(address,0x20000000,count=50000)
 assert u.reg_read(r.UC_ARM64_REG_PC)==0x20000000
 return x(0)
for case in primitive_cases:
 mode,length,mask,identity,status=case.split(',');length,mask,identity,status=map(int,(length,mask,identity,status));heap=0x10300000;byte_lists={};queries={};fault=False
 if mode=='G':
  data=0 if length<0 else bytes_array(bytes((i*17+3)&255 for i in range(length)));index_pointer=0x1000b000;put(index_pointer,mask,4)
  value=primitive_call(0x3d1f7d0,data,index_pointer);output=str(0 if fault else signed(value))+','+str(signed(read(index_pointer,4)))
 else:
  tasks=0 if length<0 else array(length)
  if tasks:
   for i in range(length):put(tasks+0x20+i*8,i+11,4);put(tasks+0x24+i*8,int(bool((mask&0xffffffff)&(1<<(i&31)))),1)
  put(area+0x10,identity,4);put(area+0x18,status,4);put(area+0x20,tasks)
  if mode=='B':
   first=primitive_call(0x28e377c,area)
   if fault:output='0,0'
   else:
    other=primitive_call(0x3d1f850,tasks);output=str(signed(first))+','+str(0 if fault else signed(other))
  else:
   encoded=primitive_call(0x28e3a10,area);output=''if fault else text(encoded)
 primitive_expected.append(str(int(fault))+'#'+output)
with tempfile.TemporaryDirectory()as directory:
 exe=Path(directory)/'Primitives.exe';command=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+','-main:AreaDataPrimitiveHarness',*['-r:'+str(mono/'lib/mono/4.5'/p)for p in('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),*[str(PROJECT/p)for p in sources+['SourceCatalog/AreaDataPrimitiveHarness.cs']]]
 result=subprocess.run(command,capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(primitive_cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 actual=result.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(primitive_expected)
 for i,(a,b)in enumerate(zip(actual,primitive_expected)):assert a==b,(i,primitive_cases[i],a,b)
report={'passed':True,'cases':len(cases),'primitive_cases':len(primitive_cases),'primitive_scope':'Actual ARM64 GetTaskValue OR/32-bit shift wrapping, ldexpf/FCVTZS/add task packing including overflow/Infinity, 12-byte/base64 encoding, null/short byte input and index mutation before failure. BCL byte-list/base64/Skip/Take/ToInt32 are supplied boundaries.','scope':__doc__.strip(),'original_rvas':['0x3d1f298','0x3d1eeb8','0x3d1c674','0x3d1f5b4','0x3d1f1d8','0x3d1f7d0','0x3d1f900','0x3d1effc','0x3d1f6a8','0x28e3a80','0x28e377c','0x3d1f850','0x28e38e8','0x28e3a10'],'files':[{'path':p,'sha256':hashlib.sha256((PROJECT/p).read_bytes()).hexdigest()}for p in sources+['SourceCatalog/AreaDataPrimitiveHarness.cs','SourceCatalog/verify_area_data.py']]}
(PROJECT/'area_data_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
