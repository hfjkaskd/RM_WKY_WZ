"""Execute original ARM64 line score/selection routines against production C#.

Original column/row loops, lightball penalties and below-static admission, highest-score lists,
useful-negative fallback, winning priority and Cell14 shuffle execute from ELF.
Item score/admission, line-winning, grid and RNG are explicit typed boundaries;
this does not differentially prove the concrete goal/scoring owners, full helper
compositions, carried flight, damage or original rendered parity.
"""
from pathlib import Path
import hashlib,json,random,re,sqlite3,subprocess,sys,tempfile
PROJECT=Path(__file__).resolve().parents[1];RECOVERY=PROJECT.parent
sys.path.insert(0,str(RECOVERY/'tools/python'))
from elftools.elf.elffile import ELFFile
from elftools.elf.relocation import RelocationSection
from unicorn import Uc,UC_ARCH_ARM64,UC_MODE_ARM,UC_HOOK_CODE
import unicorn.arm64_const as r
u=Uc(UC_ARCH_ARM64,UC_MODE_ARM);u.mem_map(0,0x6000000);u.mem_map(0x10000000,0x400000);u.mem_map(0x20000000,0x1000)
def put(p,v,n=4):u.mem_write(p,(v&((1<<(8*n))-1)).to_bytes(n,'little'))
def get(p,n=4):return int.from_bytes(u.mem_read(p,n),'little')
def signed(v):v&=0xffffffff;return v-(1<<32)if v&(1<<31)else v
def packed(x,y):return(x&0xffffffff)|((y&0xffffffff)<<32)
lib=RECOVERY.parent/'RoyalMatch_20260929/apk_unpacked/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so'
with lib.open('rb')as stream:
 elf=ELFFile(stream)
 for segment in elf.iter_segments():
  if segment['p_type']=='PT_LOAD':u.mem_write(segment['p_vaddr'],segment.data())
 for section in elf.iter_sections():
  if isinstance(section,RelocationSection):
   for v in section.iter_relocations():
    if v['r_info_type']==1027:put(v['r_offset'],v['r_addend'],8)
owner,data,grid,level,claim,random_owner=[0x10000000+i*0x1000 for i in range(6)]
meta,ctx,klass,statics,point_meta,point_statics=[0x10006000+i*0x1000 for i in range(6)]
point_usage=get(0x53839d0,8)
jelly_usage,jelly_meta,grass_item,grass_class=0x53ca928,0x10035000,0x10036000,0x10037000
put(jelly_meta+0x12c,1,1);put(grass_item,grass_class,8);put(grass_class+0xc8,grass_class+0x200,8)
for p,s in((meta,statics),(point_meta,point_statics)):
 put(p+0x38,ctx,8);put(p+0xe0,1);put(p+0x132,1,1);put(p+0xb8,s,8)
put(ctx,klass,8);put(klass+0x132,1,1);put(klass+0xe0,1);put(klass+0xb8,point_statics,8);put(point_statics,packed(-1,-1),8)
put(owner+0x30,grid,8);put(owner+0x38,random_owner,8);put(owner+0x50,level,8);put(owner+0x48,claim,8);put(claim+0x10,0x1000c000,8)
lists=[0x10010000+i*0x1000 for i in range(2)]
for off,p in zip((0x10,0x90),lists):put(owner+off,p,8);put(p+0x10,p+0x100,8);put(p+0x118,128)
arrays={}
for i,(off,size)in enumerate(((0x98,4),(0xa0,4),(0xb0,1),(0xb8,1),(0xc8,1),(0xa8,4))):
 p=0x10020000+i*0x1000;put(owner+off,p,8);arrays[p]=size
win,win_class,item_class,rng_class=0x10030000,0x10031000,0x10032000,0x10033000
put(owner+0x78,win,8);put(win,win_class,8);put(win_class+0x188,0x20000110,8);put(win_class+0x198,0x20000110,8)
put(item_class+0x378,0x20000120,8);put(item_class+0x3d8,0x20000130,8)
put(random_owner+0x30,0x10034000,8);put(0x10034000,rng_class,8);put(rng_class+0x198,0x20000140,8)
put(0x100f1000,item_class,8);put(item_class+0x40,meta,8);put(0x100f1018,1);put(0x100f2000,meta,8)
cells={};models={};points={};width=height=0;column=False;wins=[];seed=0;draws=[];winning_calls=0;score_queries=0
def next_value(a,b):
 global seed
 assert b>a,(a,b);draws.append(str(a)+','+str(b));seed=(seed*1664525+1013904223)&0xffffffff;return a+seed%(b-a)
def boundary(uc,address,size,unused):
 global winning_calls,score_queries
 x=[uc.reg_read(getattr(r,'UC_ARM64_REG_X'+str(i)))for i in range(6)];value=None
 if address==0xffd960:
  put(x[0],point_meta if x[0]==point_usage else jelly_meta if x[0]==jelly_usage else meta,8);value=0
 elif address in(0xffda64,0xffd90c,0x2a2da14,0x2a2e598,0x2ee6e48,0x2ee72c8,0x1c97168):value=0
 elif address==0xffd9d0:value=0x100f1000
 elif address==0xffda6c:value=0x100f2000
 elif address==0xffda68:value=x[0]
 elif address in(0xffda88,0xffda90,0xffde20):raise AssertionError(('Native error',hex(uc.reg_read(r.UC_ARM64_REG_LR))))
 elif address in(0x1e19848,0x1e19780,0x1e197d8,0x1c96928,0x1c96534,0x18dffec,0x18dfe1c):value=0
 elif address==0x2627eb8:
  element=arrays.get(x[0],4);u.mem_write(x[0]+0x20+x[1]*element,bytes(x[2]*element));value=0
 elif address==0x320a4c0:value=width
 elif address==0x320a4e4:value=0
 elif address==0x320a528:value=height-1
 elif address==0x343b6e8:put(x[0],x[1]);put(x[0]+4,x[2]);value=0
 elif address==0x3207954:value=points.get((signed(x[1]),signed(x[1]>>32)),0)
 elif address==0x343bd78:score_queries+=1;value=cells[x[0]][4]
 elif address==0x1c9763c:value=cells[x[1]][5]
 elif address==0x343bd38:value=int(cells[x[0]][3]!=0)
 elif address in (0x319017c,0x343bd5c):value=x[0]+0x200 if cells[x[0]][3]else 0
 elif address==0x343c67c:value=cells[x[0]][8]
 elif address==0x343c744:value=cells[x[0]][9]
 elif address==0x343c794:value=grass_item if cells[x[0]][9] else 0
 elif address==0x319d004:value=int(cells[x[0]][7]==0)
 elif address==0x20000120:value=models[x[0]][3]
 elif address==0x20000130:value=models[x[0]][6]
 elif address==0x20000110:value=wins[x[1]]
 elif address==0x23e648c:value=0x100f0000
 elif address==0x23e7c7c:winning_calls+=1;value=0
 elif address==0x260e280:
  count=get(x[1]+0x18);value=get(get(x[1]+0x10,8)+0x20+next_value(0,count)*4)
 elif address==0x20000140:value=next_value(signed(x[1]),signed(x[2]))
 elif address==0x12de9a0:
  count=get(x[0]);assert count<14;put(x[0]+8+count*8,x[1],8);put(x[0],count+1);value=0
 elif address==0x12de610:
  assert x[1]<get(x[0]);value=get(x[0]+8+x[1]*8,8)
 elif address==0x12de7cc:
  assert x[1]<get(x[0]);put(x[0]+8+x[1]*8,x[2],8);value=0
 elif address in(0xd00b50,0xd018a0):
  u.mem_write(x[0],bytes(x[2])if address==0xd018a0 else bytes(u.mem_read(x[1],x[2])));value=x[0]
 if value is not None:
  uc.reg_write(r.UC_ARM64_REG_X0,value&0xffffffffffffffff);uc.reg_write(r.UC_ARM64_REG_PC,uc.reg_read(r.UC_ARM64_REG_LR))
hook_source=Path(__file__).read_text().split('def boundary',1)[1].split('hook_source=',1)[0]
for address in set(int(v,16)for v in re.findall(r'0x[0-9a-f]+',hook_source)):u.hook_add(UC_HOOK_CODE,boundary,begin=address,end=address)
def call(address,*args):
 for i in range(8):u.reg_write(getattr(r,'UC_ARM64_REG_X'+str(i)),args[i]if i<len(args)else 0)
 u.reg_write(r.UC_ARM64_REG_SP,0x103ff000);u.reg_write(r.UC_ARM64_REG_LR,0x20000000)
 try:u.emu_start(address,0x20000000,count=2000000)
 except Exception as error:raise AssertionError((hex(address),hex(u.reg_read(r.UC_ARM64_REG_PC)),hex(u.reg_read(r.UC_ARM64_REG_LR))))from error
 assert u.reg_read(r.UC_ARM64_REG_PC)==0x20000000,'Instruction budget exceeded'
 return u.reg_read(r.UC_ARM64_REG_X0)
rng=random.Random(0x1c99380);cases=[];expected=[]
for n in range(250):
 width,height=1+n%9,1+(n//9)%8;column=n%2==0;count=width if column else height;initial_seed=rng.randrange(0x7fffffff);seed=initial_seed;draws=[];winning_calls=0
 for off,length in((0x98,width),(0xa0,height),(0xb0,width),(0xb8,height),(0xc8,width*height),(0xa8,3)):
  p=get(owner+off,8);put(p+0x18,length);u.mem_write(p+0x20,bytes(length*arrays[p]))
 cells={};models={};points={};values=[];wins=[int(n%5==0 and rng.randrange(4)==0)for _ in range(count)]
 for y in range(height):
  for xx in range(width):
   i=len(values);kind=rng.choice((0,1,2,3,4,5,12));score=rng.choice((0,-1,-4569,676,17576,456976,2147483647,-2147483648))if kind else 0
   if n%7==0:kind=1;score=0
   if n%7==1:kind=12;score=456976
   add=int(rng.randrange(5)>0);valid=int(kind!=0 and rng.randrange(5)>0);cell_type=int(rng.randrange(8)>0)
   above=below=0
   if n >= 200:
    above=int(kind==5 and rng.randrange(2)==0)
    below=int(not above and rng.randrange(4)==0)
    if above: score=456976
   if n >= 246:
    # Compare both axes with a lone positive covered cell or a negative lightball over grass.
    kind,score,above,below,add,valid,cell_type = (0,0,0,0,0,0,0)
    if i==0:
     above=int(n<248);below=1-above
     kind,score,add,valid,cell_type=5,456976 if above else -1256720,1,1,1
   value=[i,xx,y,kind,score,add,valid,cell_type,above,below];values.append(value)
   p=0x10100000+i*0x400;cells[p]=value;models[p+0x200]=value;points[xx,y]=p
   put(p+0x18,xx);put(p+0x1c,y);put(p+0xb0,p,8);put(p+0xc0,p,8);put(p+0x10,p,8);put(p+0xa0,value[7]);put(p+0x200,item_class,8)
 trigger=10 if column else 11;put(data+8,trigger);put(data+0x14,0,1);scores=[]
 for i in range(count):
  put(0x100e0000,0,1)
  result=call(0x1c96b94,owner,i,0,0x100e0000,1)if column else call(0x1c97208,owner,i,0,0x100e0000)
  scores.append(str(signed(result))+','+str(get(0x100e0000,1)))
 selected=call(0x1c99380,owner,data,1,0,0,0,1)if column else call(0x1c9a728,owner,data,2,0,0,1)
 expected.append(str(signed(selected))+','+str(signed(selected>>32))+'|'+str(winning_calls)+'|'+';'.join(draws)+'|'+';'.join(scores))
 cases.append(','.join(map(str,(width,height,trigger,initial_seed)))+'|'+','.join(map(str,wins))+'|'+';'.join(','.join(map(str,v))for v in values))
mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
sources=['SourceCatalog/LineExplodeTargetHarness.cs','Assets/Foundation/Runtime/OriginalLineExplodeTargetFinder.cs']
with tempfile.TemporaryDirectory()as folder:
 exe=Path(folder)/'Verify.exe';command=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',
  *['-r:'+str(mono/'lib/mono/4.5'/name)for name in('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),*[str(PROJECT/p)for p in sources]]
 result=subprocess.run(command,capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 actual=result.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
 for i,(a,b)in enumerate(zip(actual,expected)):assert a==b,(i,cases[i],a,b)
methods=[]
with sqlite3.connect(RECOVERY/'code/native/functions.sqlite3')as db:
 for address in(0x1c96b94,0x1c97208,0x1c96fec,0x1c97054,0x1c99380,0x1c9a728,0x1c98b58,0x1c98c30,0x1c98e3c,0x1ed948c):
  name,body=db.execute('select name,body from functions where address=?',(address,)).fetchone();methods.append({'rva':hex(address),'name':name,'body_sha256':hashlib.sha256(body.encode()).hexdigest()})
report={'passed':True,'selection_cases':len(cases),'score_queries':score_queries,'native_methods':methods,'scope':__doc__.strip(),
 'files':[{'path':p,'sha256':hashlib.sha256((PROJECT/p).read_bytes()).hexdigest()}for p in sources]}
(PROJECT/'line_explode_targets_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
