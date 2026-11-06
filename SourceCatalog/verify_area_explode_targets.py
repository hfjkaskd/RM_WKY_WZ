"""Execute original ARM64 TNT area loops/selection against production C#.

Cell scores, goal-cap admission and area-winning decisions are shared boundaries.
The area traversal, cover and underlay rules, candidate lists and RNG execute from ELF.
"""
from pathlib import Path
import hashlib,json,random,re,sqlite3,struct,subprocess,sys,tempfile
PROJECT=Path(__file__).resolve().parents[1];RECOVERY=PROJECT.parent
sys.path.insert(0,str(RECOVERY/'tools/python'))
from elftools.elf.elffile import ELFFile
from elftools.elf.relocation import RelocationSection
from unicorn import Uc,UC_ARCH_ARM64,UC_MODE_ARM,UC_HOOK_CODE
import unicorn.arm64_const as r
u=Uc(UC_ARCH_ARM64,UC_MODE_ARM);u.mem_map(0,0x6000000);u.mem_map(0x10000000,0x400000);u.mem_map(0x20000000,0x1000)
def put(p,v,n=4):u.mem_write(p,(v&((1<<(n*8))-1)).to_bytes(n,'little'))
def read(p,n=4):return int.from_bytes(u.mem_read(p,n),'little')
def signed(v):v&=0xffffffff;return v-(1<<32)if v&(1<<31)else v
def packed(x,y):return (x&0xffffffff)|((y&0xffffffff)<<32)
def unpacked(v):return signed(v),signed(v>>32)
lib=RECOVERY.parent/'RoyalMatch_20260929/apk_unpacked/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so'
with lib.open('rb')as stream:
 elf=ELFFile(stream)
 for segment in elf.iter_segments():
  if segment['p_type']=='PT_LOAD':u.mem_write(segment['p_vaddr'],segment.data())
 for section in elf.iter_sections():
  if isinstance(section,RelocationSection):
   for v in section.iter_relocations():
    if v['r_info_type']==1027:put(v['r_offset'],v['r_addend'],8)
owner,data,level,claim,scorer,grid,random_owner= [0x10000000+i*0x1000 for i in range(7)]
meta,ctx,klass,statics=0x10007000,0x10008000,0x10009000,0x1000a000
put(meta+0x38,ctx,8);put(ctx,klass,8);put(klass+0x132,1,1);put(klass+0xe0,1);put(klass+0xb8,statics,8)
put(meta+0xe0,1);put(meta+0xb8,statics,8)
put(statics,packed(-1,-1),8) # Already initialized CellPoint.Null static, not a result adjustment.
jelly_usage,jelly_meta,grass_item,grass_class=0x53ca928,0x10035000,0x10036000,0x10037000
put(jelly_meta+0x12c,1,1)
put(grass_item,grass_class,8);put(grass_class+0xc8,grass_class+0x200,8)
honey_class,honey_ready,honey_pending=0x10038000,0x10039000,0x1003a000
put(honey_class+0x4d8,0x2b52784,8);put(honey_class+0x4c8,0x2b527a8,8)
for cover in (honey_ready,honey_pending):
 put(cover,honey_class,8);put(cover+0x38,cover,8);put(cover+0x50,1)
put(owner+0x50,level,8);put(owner+0x48,claim,8);put(claim+0x10,scorer,8);put(owner+0x30,grid,8);put(owner+0x38,random_owner,8)
put(owner+0xc8,0x1000b000,8);put(0x1000b018,72)
item_class=0x1000c000;put(item_class+0x378,0x20000100,8)
put(0x100f1000,item_class,8);put(item_class+0x40,meta,8);put(0x100f1018,1);put(0x100f2000,meta,8)
lists=[0x10010000+i*0x1000 for i in range(3)]
for offset,p in zip((0x18,0x20,0x28),lists):
 put(owner+offset,p,8);put(p+0x10,p+0x100,8);put(p+0x118,256)
cells={};models={};points={};ordered=[];iterator_index=-1;random_value=0;draws=[];winning_calls=0
def boundary(uc,address,size,unused):
 global iterator_index,winning_calls
 x=[uc.reg_read(getattr(r,'UC_ARM64_REG_X'+str(i)))for i in range(6)];result=None
 if address==0xffd960:put(x[0],jelly_meta if x[0]==jelly_usage else meta,8);result=0
 elif address==0xffd9d0:result=0x100f1000 # Framework log object-array allocation.
 elif address==0xffda6c:result=0x100f2000 # Framework boxing for the debug log only.
 elif address==0xffda68:result=x[0] # Compatible box/array store type check at the logging boundary.
 elif address in (0xffd90c,0xffda64,0x2627eb8,0x2ee6e48,0x1c97168,0x2a2da14):result=0
 elif address in (0xffda88,0xffda90):raise AssertionError(('native error',hex(uc.reg_read(r.UC_ARM64_REG_LR))))
 elif address in (0x1e19780,0x1e19848,0x1e197d8,0x319e378,0x319e1dc,0x319dfe0,0x319dedc,0x319e248,0x2ee72c8):result=0
 elif address==0x318ec2c:iterator_index=-1;result=0
 elif address==0x318ecc8:
  iterator_index+=1;result=ordered[iterator_index]if iterator_index<len(ordered)else 0;put(owner+0x68,result,8)
 elif address==0x3195f9c:result=cells[x[0]][5]
 elif address in (0x319017c,0x343bd5c):result=x[0]+0x200 if cells[x[0]][3]else 0
 elif address==0x343c67c:result=int(cells[x[0]][8]!=0)
 elif address==0x343c6cc:result=(0,honey_ready,honey_pending)[cells[x[0]][8]]
 elif address==0x343c744:result=cells[x[0]][9]
 elif address==0x343c794:result=grass_item if cells[x[0]][9] else 0
 elif address==0x318ce80:result=int(x[0]==honey_pending)
 elif address==0x343bd38:result=int(cells[x[0]][3]!=0)
 elif address==0x343bd78:result=cells[x[0]][4]
 elif address==0x2b47ce8:result=int(0<models[x[0]][3]<=5)
 elif address==0x20000100:result=models[x[0]][3]
 elif address==0x3207954:result=points.get(unpacked(x[1]),0)
 elif address==0x343b6e8:put(x[0],x[1]);put(x[0]+4,x[2]);result=0
 elif address==0x1c9763c:result=cells[x[1]][6]
 elif address==0x20000110:result=cells[points[unpacked(x[1])]][7]
 elif address==0x23e648c:result=0x100f0000
 elif address==0x23e7c7c:winning_calls+=1;result=0
 elif address==0x260e21c:
  count=read(x[1]+0x18);assert count>0;draws.append(count);result=read(read(x[1]+0x10,8)+0x20+(random_value%count)*8,8)
 elif address==0x20000000:return
 if result is not None:
  uc.reg_write(r.UC_ARM64_REG_X0,result&0xffffffffffffffff);uc.reg_write(r.UC_ARM64_REG_PC,uc.reg_read(r.UC_ARM64_REG_LR))
# Enter Python only at explicit native/framework boundaries. Hooking every
# ARM instruction adds minutes without observing any additional boundary.
hook_source=Path(__file__).read_text(encoding='utf-8').split('def boundary',1)[1].split('# Enter Python',1)[0]
for address in set(int(value,16) for value in re.findall(r'0x[0-9a-f]+',hook_source)):
 u.hook_add(UC_HOOK_CODE,boundary,begin=address,end=address)
# WinCalculationData's virtual area-winning boundary is supplied separately;
# actual score loops and highest-candidate selection execute from the ELF.
win,win_class=0x1000d000,0x1000e000;put(owner+0x78,win,8);put(win,win_class,8);put(win_class+0x1a8,0x20000110,8)
def call(address,*args):
 for i in range(8):u.reg_write(getattr(r,'UC_ARM64_REG_X'+str(i)),args[i]&0xffffffffffffffff if i<len(args)else 0)
 u.reg_write(r.UC_ARM64_REG_SP,0x103ff000);u.reg_write(r.UC_ARM64_REG_LR,0x20000000)
 try:u.emu_start(address,0x20000000,count=2000000)
 except Exception as error:raise AssertionError((hex(address),hex(u.reg_read(r.UC_ARM64_REG_PC)),hex(u.reg_read(r.UC_ARM64_REG_LR))))from error
 assert u.reg_read(r.UC_ARM64_REG_PC)==0x20000000,'Instruction budget exceeded'
 return u.reg_read(r.UC_ARM64_REG_X0)
rng=random.Random(0x1c95804);cases=[];expected=[];score_queries=0
if '--trace' in sys.argv:
 def trace(uc,address,size,unused):
  if n!=14:return
  if address==0x1c96028:
   cp=uc.reg_read(r.UC_ARM64_REG_X27)
   print('candidate',cells[cp][:5],*[signed(uc.reg_read(getattr(r,'UC_ARM64_REG_X'+str(i)))) for i in (19,21,25,28)],flush=True)
  else:
   print('lists',[[unpacked(read(read(p+0x10,8)+0x20+i*8,8))for i in range(read(p+0x18))]for p in lists],flush=True)
 for address in (0x1c96028,0x1c9647c):u.hook_add(UC_HOOK_CODE,trace,begin=address,end=address)
for n in range(214):
 cells={};models={};points={};ordered=[];draws=[];winning_calls=0;random_value=rng.randrange(0x7fffffff)
 width,height=(1+n%9,1+(n//9)%8) if n < 210 else (1,1)
 values=[]
 for y in range(height):
  for xx in range(width):
   i=len(values);kind=rng.choice([0,1,2,3,4,5,12]);score=rng.choice([0,-1,-4569,456976,17576,2147483647,-2147483648])
   if n%8==0:kind=1;score=0
   if n%8==1:kind=12;score=456976
   normal=int(n%5!=0 or rng.randrange(4)>0);add=int(rng.randrange(5)>0);winning=int(n%4==0 and rng.randrange(6)==0)
   above=below=0
   if n >= 160:
    above=rng.choice((0,0,0,1,2))
    below=int(above==0 and rng.randrange(4)==0)
    if above==1:score=456976
    if below and kind==5:score=-1256720
   if n >= 210:
    kind,score,above,below=(
        (5,456976,1,0),(5,456976,2,0),(5,-1256720,0,1),(1,0,2,0)
    )[n-210]
    normal,add,winning=1,1,0
   value=[i,xx,y,kind,score,normal,add,winning,above,below];values.append(value)
   pointer=0x10100000+i*0x400;cells[pointer]=value;models[pointer+0x200]=value;points[(xx,y)]=pointer;ordered.append(pointer)
   put(pointer+0x18,xx);put(pointer+0x1c,y);put(pointer+0xb0,pointer,8);put(pointer+0xc0,pointer,8);put(pointer+0x10,pointer,8)
   put(pointer+0x200,item_class,8)
 scores=[]
 for value in values:
  result=signed(call(0x1c9ba28,owner,packed(value[1],value[2]),0,13,0x1000f000));positive=read(0x1000f000,1)
  scores.append(str(result)+','+str(positive));score_queries+=1
 put(data+8,13);put(data+0x14,0,1)
 selected=unpacked(call(0x1c95804,owner,data,0,0,0,1))
 cases.append(str(random_value)+'|'+';'.join(','.join(map(str,v))for v in values))
 expected.append(str(selected[0])+','+str(selected[1])+'|'+str(winning_calls)+'|'+','.join(map(str,draws))+'|'+';'.join(scores))
mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
with tempfile.TemporaryDirectory()as folder:
 exe=Path(folder)/'Verify.exe'
 sources=['SourceCatalog/AreaExplodeTargetHarness.cs','Assets/Foundation/Runtime/OriginalAreaExplodeTargetFinder.cs']
 command=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',*["-r:"+str(mono/'lib/mono/4.5'/name)for name in ('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),*[str(PROJECT/name)for name in sources]]
 result=subprocess.run(command,capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 actual=result.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
 for i,(a,e)in enumerate(zip(actual,expected)):assert a==e,(i,cases[i],a,e)
methods=[]
with sqlite3.connect(str(RECOVERY/'code/native/functions.sqlite3'))as db:
 for address in (0x1c95804,0x1c9ba28,0x1c97054,0x1c9d160,0x2b44060,0x2b52784,0x2b527a8):
  name,body=db.execute('select name,body from functions where address=?',(address,)).fetchone();methods.append({'rva':hex(address),'name':name,'body_sha256':hashlib.sha256(body.encode()).hexdigest()})
report={'passed':True,'selection_cases':len(cases),'score_queries':score_queries,'methods':methods,'scope':__doc__.strip()+' The fixtures cover sparse and non-normal cells, overflow, grass below Lightballs, available and pending honey covers, useful-negative candidates and one-candidate RNG consumption.'}
report['production_source_sha256']=hashlib.sha256((PROJECT/'Assets/Foundation/Runtime/OriginalAreaExplodeTargetFinder.cs').read_bytes()).hexdigest()
report['harness_source_sha256']=hashlib.sha256((PROJECT/'SourceCatalog/AreaExplodeTargetHarness.cs').read_bytes()).hexdigest()
(PROJECT/'area_explode_targets_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
