"""Execute actual ARM64 PossibleMatch struct/finder and initialization retry decision.

Cell mediation, current item virtual capabilities, cell/static/incoming
eligibility, neighbor access, native metadata and match-existence query are
supplied ports. Original full finder loops, relative chain rules, required
cell/neighbor result, struct validation, by-value iterator reset and the
0/1/2 retry/lightball decision execute from ELF. This is not all runtime
shuffle animation/event/lifecycle behavior or full production startup proof.
"""
from pathlib import Path
import sys,json,hashlib,itertools,random,subprocess,tempfile
PROJECT=Path(__file__).resolve().parents[1];BASE=PROJECT.parent
sys.path.insert(0,str(BASE/'tools/python'))
from elftools.elf.elffile import ELFFile
from elftools.elf.relocation import RelocationSection
from unicorn import Uc,UC_ARCH_ARM64,UC_MODE_ARM,UC_HOOK_CODE
import unicorn.arm64_const as r
u=Uc(UC_ARCH_ARM64,UC_MODE_ARM);u.mem_map(0,0x6000000);u.mem_map(0x10000000,0x400000);u.mem_map(0x20000000,0x3000)
def put(p,v,n=8):u.mem_write(p,(int(v)&((1<<(8*n))-1)).to_bytes(n,'little'))
def read(p,n=8):return int.from_bytes(u.mem_read(p,n),'little')
def si(v):return (int(v)&0xffffffff)-0x100000000 if int(v)&0x80000000 else int(v)&0xffffffff
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
manager,iterator,strategy,strategy_class,item_class,statics,directions,out,match_manager=[0x10001000+i*0x1000 for i in range(9)]
cells=[0x10010000+i*0x1000 for i in range(9)];meds=[0x10020000+i*0x1000 for i in range(9)];items=[0x10030000+i*0x1000 for i in range(9)];neighbors=[0x10040000+i*0x1000 for i in range(9)];static_items=[0x10050000+i*0x1000 for i in range(9)]
neighbor_slot=read(0x53b2db8);metadata=0x10200000;trace=[];calls=0;fault=False;v=[];rows=[]
def fail():
 global fault
 fault=True;u.reg_write(r.UC_ARM64_REG_PC,0x20000000)
def step(label):
 global calls
 trace.append(label);calls+=1
 if calls==v[5]:fail();return False
 if calls==v[6]:
  rows[v[7]][v[8]]=v[9]
  if v[8]==7:put(items[v[7]]+0x18,v[9],4)
 return True
def result(label,value=0):
 if step(label):returned(value)
def neighbor(i,d):
 if d<0 or d>7:fail();return 0
 xx=i%3+(-1,0,1,1,1,0,-1,-1)[d];yy=i//3+(1,1,1,0,-1,-1,-1,0)[d]
 return cells[xx+yy*3]if 0<=xx<3 and 0<=yy<3 else 0
def value(label,i,field):
 if step(label+','+str(i)):returned(rows[i][field])
def hook(uc,address,size,unused):
 global metadata
 a,b=x(0),x(1)
 if address==0xffd960:
  put(a,metadata);put(metadata+0xe0,1,4)
  if a==neighbor_slot:put(metadata+0xb8,statics)
  metadata+=0x200;returned();return
 if address in(0xffd90c,0xffda64):returned();return
 if address in(0xffda88,0xffda90):fail();return
 if address==0x20001000:result('first',cells[0]if v[10]else 0);return
 if address==0x20001010:
  i=cells.index(b);result('next,'+str(i),cells[i+1]if i+1<v[10]else 0);return
 if address==0x343bd38:value('has',meds.index(a),0);return
 if address in(0x343bd5c,0x319017c):
  i=meds.index(a)if address==0x343bd5c else cells.index(a)
  if step('item,'+str(i)):returned(items[i]if rows[i][1]else 0)
  return
 if address==0x20001300:value('special',items.index(a),11);return
 if address==0x20001310:value('match',items.index(a),10);return
 if address==0x20001320:value('swap',items.index(a),12);return
 if address==0x20001330:value('type',items.index(a),8);return
 if address==0x318ec68:value('enter',cells.index(a),2);return
 if address==0x319dcb0:value('accept',cells.index(a),3);return
 if address==0x319cd50:value('special-cell',cells.index(a),4);return
 if address==0x3440db8:value('blocking',static_items.index(a),5);return
 if address==0x3440fa4:assert b==6;value('chain',static_items.index(a),6);return
 if address==0x31932c8:
  p=neighbor(neighbors.index(a),si(b))
  if not fault:returned(p)
  return
 if address==0x12e1470:assert a==match_manager;result('any,'+str(b),int(v[4]!=0));return
 raise AssertionError(hex(address))
for a in(0xffd960,0xffd90c,0xffda64,0xffda88,0xffda90,0x20001000,0x20001010,0x343bd38,0x343bd5c,0x319017c,0x20001300,0x20001310,0x20001320,0x20001330,0x318ec68,0x319dcb0,0x319cd50,0x3440db8,0x3440fa4,0x31932c8,0x12e1470):u.hook_add(UC_HOOK_CODE,hook,begin=a,end=a)
def call(address,a,b=0,c=0,d=0):
 global fault
 fault=False
 for i in range(9):u.reg_write(getattr(r,'UC_ARM64_REG_X'+str(i)),(a,b,c,d)[i]if i<4 else out if i==8 else 0)
 u.reg_write(r.UC_ARM64_REG_SP,0x103ff000);u.reg_write(r.UC_ARM64_REG_LR,0x20000000);u.emu_start(address,0x20000000,count=30000)
 assert u.reg_read(r.UC_ARM64_REG_PC)==0x20000000,hex(u.reg_read(r.UC_ARM64_REG_PC))
 return si(x(0))
def cell_id(p):return -1 if not p else cells.index(p)
def match_state(p):return ','.join(map(str,(cell_id(read(p)),si(read(p+8,4)),read(p+12,1),*[cell_id(read(p+i))for i in(16,24,32,40)])))
def native(head,data):
 global v,rows,trace,calls
 v=head;rows=[data[i*13:i*13+13]for i in range(9)];trace=[];calls=0
 for p in(manager,iterator,strategy,strategy_class,item_class,out):u.mem_write(p,bytes(0x600))
 put(statics+8,directions);put(directions+0x18,4,4)
 for i,d in enumerate((1,3,5,7)):put(directions+0x20+i*4,d,4)
 put(strategy,strategy_class);put(strategy_class+0x178,0x20001000);put(strategy_class+0x188,0x20001010)
 put(iterator,strategy);put(iterator+16,cells[0]if v[10]else 0);u.mem_write(manager+0x88,bytes(u.mem_read(iterator,24)))
 put(manager+0x28,0 if v[11]else match_manager)
 for offset,fn in((0x558,0x20001300),(0x548,0x20001310),(0x448,0x20001320),(0x378,0x20001330)):put(item_class+offset,fn)
 for i,p in enumerate(cells):
  put(p+0x10,meds[i]);put(p+0x20,neighbors[i]);put(p+0xc0,static_items[i]);put(p+0xa0,data[117+i],4);put(items[i],item_class);put(items[i]+0x18,rows[i][7],4)
 p=0 if v[1]<0 else cells[v[1]];result_value=-99;struct_value='-1,0,0,-1,-1,-1,-1'
 if v[0]==0:result_value=call(0x1ee5c64,iterator)
 elif v[0]==1:result_value=call(0x1ee5e98,p)
 elif v[0]==2:call(0x1ee5f94,p,v[2],v[3])
 elif v[0]==3:call(0x1ee6068,p,v[2],v[4],v[3])
 elif v[0]==4:result_value=call(0x1ee6c20,manager)
 else:
  put(out+8,v[4],4);put(out+12,v[11],1)
  for i,d in enumerate((1,3,5,7)):
   if v[10]&(1<<i):call(0x1ee5b88,out,cells[i],d)
  call(0x1ee5b88,out,p,v[3]);count=call(0x1ee5c34,out);valid=call(0x1ee5bd4,out,int(v[2]!=0));result_value=count*10+valid
 if fault:result_value=-99
 if v[0]in(2,3,5)and not fault:struct_value=match_state(out)
 return str(int(fault))+'#'+str(result_value)+'#'+str(read(manager+0xa4,1))+'#'+struct_value+'#'+';'.join(trace)
rng=random.Random(0x1ee5c64);cases=[]
def standard():return [z for i in range(9)for z in(1,1,1,1,0,0,0,rng.randrange(1,5),1,0,1,0,1)]+[1]*9
for mode in range(6):
 for _ in range(700):
  data=[z for i in range(9)for z in(*[rng.randrange(2)for j in range(7)],rng.randrange(5),rng.choice((1,2,3,4,5,6)),0,rng.randrange(2),rng.randrange(2),rng.randrange(2))]+[rng.randrange(3)for i in range(9)]
  cases.append(([mode,rng.randrange(-1,9),rng.randrange(5),rng.choice((1,3,5,7)),rng.randrange(4),0,0,0,0,0,rng.randrange(10)if mode!=5 else rng.randrange(16),rng.randrange(2)],data))
for mode,stage in itertools.product(range(5),range(1,75)):
 cases.append(([mode,4,2,1,2,stage,0,0,0,0,9,0],standard()))
for mode,stage,field in itertools.product((0,2,3,4),range(1,40),(0,1,2,3,7,11)):
 cases.append(([mode,4,2,1,2,0,stage,rng.randrange(9),field,0,9,0],standard()))
for direction,target,mask,special,accept in itertools.product(range(-3,12),(-1,0,1,2,3,4,5),range(16),range(2),range(2)):
 cases.append(([5,4,accept,direction,target,0,0,0,0,0,mask,special],standard()))
expected=[native(*case)for case in cases]
mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
sources=['SourceCatalog/PossibleMoveHarness.cs','Assets/Foundation/Runtime/OriginalPossibleMoveFinder.cs','Assets/Foundation/Runtime/OriginalGridIterator.cs']
with tempfile.TemporaryDirectory()as directory:
 exe=Path(directory)/'Possible.exe';command=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',*['-r:'+str(mono/'lib/mono/4.5'/p)for p in('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),*[str(PROJECT/p)for p in sources]]
 result=subprocess.run(command,capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(','.join(map(str,h))+'|'+','.join(map(str,d))for h,d in cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 actual=result.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected)
 for i,(a,b)in enumerate(zip(actual,expected)):assert a==b,(i,cases[i],a,b)
report={'passed':True,'cases':len(cases),'scope':__doc__.strip(),'original_rvas':['0x1ee5b88','0x1ee5bd4','0x1ee5c34','0x1ee5c64','0x1ee5e98','0x1ee5f94','0x1ee6068','0x1ee6c20'],'files':[{'path':p,'sha256':hashlib.sha256((PROJECT/p).read_bytes()).hexdigest()}for p in sources+['SourceCatalog/verify_possible_moves.py']]}
(PROJECT/'possible_moves_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
