"""Execute original ARM64 remaining-session orchestration and reward arithmetic.

Session RemainingMovesEnd and its real BeforeAfterData writes execute from the
ELF. Event-owner calls are typed trace boundaries. Separate native executions
cover Madness remaining rewards, both Lightning reward multipliers, Clash
remaining-special weighting/overflow and AnimationData mission difficulty
multipliers. Context/metadata, event validity, score sink, dictionary iterator
and event-owner calls are explicit boundaries; no account persistence, active
event presentation, production win trigger or original rendered parity claim.
"""
from pathlib import Path
import hashlib,json,random,sqlite3,struct,subprocess,sys,tempfile
PROJECT=Path(__file__).resolve().parents[1];BASE=PROJECT.parent
sys.path.insert(0,str(BASE/'tools/python'))
from elftools.elf.elffile import ELFFile
from elftools.elf.relocation import RelocationSection
from unicorn import Uc,UC_ARCH_ARM64,UC_MODE_ARM,UC_HOOK_CODE
import unicorn.arm64_const as r
u=Uc(UC_ARCH_ARM64,UC_MODE_ARM);u.mem_map(0,0x6000000);u.mem_map(0x10000000,0x400000);u.mem_map(0x20000000,0x1000)
def put(p,v,n=8):u.mem_write(p,(v&((1<<(8*n))-1)).to_bytes(n,'little'))
def read(p,n=4):return int.from_bytes(u.mem_read(p,n),'little')
def signed(v):return v-0x100000000 if v&0x80000000 else v
def x(i):return u.reg_read(getattr(r,'UC_ARM64_REG_X'+str(i)))
with (BASE.parent/'RoyalMatch_20260929/apk_unpacked/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so').open('rb')as stream:
 elf=ELFFile(stream)
 for s in elf.iter_segments():
  if s['p_type']=='PT_LOAD':u.mem_write(s['p_vaddr'],s.data())
 for s in elf.iter_sections():
  if isinstance(s,RelocationSection):
   for v in s.iter_relocations():
    if v['r_info_type']==1027:put(v['r_offset'],v['r_addend'])
session,animation,coin,madness,daily,propeller,clash,mission,items,special_ids,super_owner,madness_owner=[0x10000000+i*0x1000 for i in range(12)]
metadata_next=0x10100000;mode='';trace=[];fault=False;valid=False;super_value=0;shown=0;pair_index=0;pairs=[];missing=0
def returned(value=0):u.reg_write(r.UC_ARM64_REG_X0,value&0xffffffffffffffff);u.reg_write(r.UC_ARM64_REG_PC,u.reg_read(r.UC_ARM64_REG_LR))
def boundary(uc,address,size,unused):
 global metadata_next,fault,pair_index
 a,b,c,d=x(0),x(1),x(2),x(3);si=lambda v:signed(v&0xffffffff)
 if address==0xffd960:
  if not 0x10100000<=read(a,8)<0x10300000:
   put(a,metadata_next);put(metadata_next+0xe0,1,4);put(metadata_next+0x132,1,1);metadata_next+=0x200
 elif address in(0xffda64,0xffd90c):pass
 elif address in(0xffda88,0xffda90):fault=True;u.reg_write(r.UC_ARM64_REG_PC,0x20000000);return
 elif address==0x29e27cc:
  assert a in(9,29),a;returned(madness_owner if a==9 else super_owner);return
 elif address==0x222dcc8:
  if mode=='s':trace.append('super,'+str(super_value))
  returned(super_value);return
 elif address==0x1e2399c:trace.append('icon');returned(shown);return
 elif address==0x32b38e4 and mode=='s':trace.append(f'madness,{si(b)},{si(c)}')
 elif address==0x338df68 and mode=='s':trace.append(f'lightning,{si(b)}')
 elif address==0x32b4cf8:trace.append(f'propeller,{si(b)}')
 elif address==0x338d10c:trace.append(f'clash,{si(b)}')
 elif address==0x338d14c and mode=='s':assert b==items;trace.append(f'specials,{c&1}')
 elif address==0x32b9df4:trace.append('lightballs')
 elif address==0x338b92c and mode=='s':trace.append(f'mission,{si(b)},{(c&255)-256 if c&128 else c&255},{si(d)}')
 elif address==0x32b2f8c:trace.append('valid');returned(int(valid));return
 elif address==0x338d704:trace.append(f'inc,{si(b)}')
 elif address==0x338c878:trace.append('valid');returned(int(valid));return
 elif address==0x338cea0:trace.append(f'score,{si(b)}')
 elif address==0x32b4094:trace.append(f'mission,{si(b)},{si(c)}')
 elif address==0x3c965cc:pair_index=0
 elif address==0x347b0c0:
  if pair_index>=len(pairs):returned(0);return
  key,value=pairs[pair_index];pair_index+=1;put(a+0x10,key,4);put(a+0x14,value,4);returned(1);return
 elif address==0x347b1b8:pass
 else:raise AssertionError(hex(address))
 returned()
addresses=(0xffd960,0xffda64,0xffd90c,0xffda88,0xffda90,0x29e27cc,0x222dcc8,0x1e2399c,
 0x32b38e4,0x338df68,0x32b4cf8,0x338d10c,0x338d14c,0x32b9df4,0x338b92c,
 0x32b2f8c,0x338d704,0x338c878,0x338cea0,0x32b4094,0x3c965cc,0x347b0c0,0x347b1b8)
for address in addresses:
 # Native tested entry points must execute, not be intercepted by their
 # session-protocol boundaries in independent arithmetic modes.
 def dispatch(uc,addr,size,unused):
  if (addr==0x32b38e4 and mode=='m')or(addr==0x338df68 and mode=='d')or(addr==0x338d14c and mode=='c')or(addr==0x338b92c and mode=='a'):return
  boundary(uc,addr,size,unused)
 u.hook_add(UC_HOOK_CODE,dispatch,begin=address,end=address)
def call(address,*args):
 global fault
 fault=False
 for i in range(8):u.reg_write(getattr(r,'UC_ARM64_REG_X'+str(i)),(args[i]if i<len(args)else 0)&0xffffffffffffffff)
 u.reg_write(r.UC_ARM64_REG_SP,0x103ff000);u.reg_write(r.UC_ARM64_REG_LR,0x20000000)
 u.emu_start(address,0x20000000,count=100000);assert u.reg_read(r.UC_ARM64_REG_PC)==0x20000000,hex(u.reg_read(r.UC_ARM64_REG_PC))
cases=[];expected=[];counts={};rng=random.Random(0x32b9bf0)
def run(kind,v):
 global mode,trace,valid,super_value,shown,pairs,missing
 v=list(map(int,v))
 mode=kind;trace=[];state='-';counts[kind]=counts.get(kind,0)+1
 if kind=='s':
  n,extra,diff,super_value,shown,event_type,before,after,consume,missing=v
  put(session+0x88,0 if missing==8 else animation);put(session+0x30,diff,1);put(session+0x50,-77,4);put(session+0xd8,special_ids)
  put(animation+0x28,0 if missing==9 else coin);put(animation+0x40,0 if missing==1 else madness);put(animation+0x88,0 if missing==2 else daily)
  put(animation+0x90,0 if missing==3 else propeller);put(animation+0xe8,0 if missing==4 else clash);put(animation+0xc8,mission)
  put(coin+0x24,before,4);put(coin+0x28,after,4);put(coin+0x20,consume,1);put(special_ids+0x18,3,4);put(special_ids+0x1c,0,4);put(madness_owner+0x18,event_type,4)
  call(0x32b9bf0,session,n,extra,items)
  state=f'{signed(read(coin+0x28))},{read(coin+0x20,1)},{signed(read(session+0x50))},{read(special_ids+0x18)}'
 elif kind=='m':
  n,diff,valid,after,consume=v;put(madness+0x28,after,4);put(madness+0x20,consume,1);call(0x32b38e4,madness,n,diff)
  state=f'{signed(read(madness+0x28))},{read(madness+0x20,1)}'
 elif kind in('d','l'):
  n,super_value=v;call(0x338df68 if kind=='d'else 0x338e00c,daily,n)
 elif kind=='a':
  typ,diff,n=v;put(animation+0xc8,mission);call(0x338b92c,animation,typ,diff,n)
 elif kind=='c':
  valid,super_value,*other=v;pairs=list(zip((3,2,4,5,99),other[5:]));
  for offset,value in zip((0x58,0x54,0x5c,0x60,0x84),other[:5]):put(clash+offset,value,4)
  call(0x338d14c,clash,0 if super_value==2 else items,int(super_value==1))
 else:raise AssertionError(kind)
 cases.append(kind+','+','.join(map(str,v)));expected.append(f'{int(fault)}#{state}#'+ ';'.join(trace))
edge=(-2147483648,-7,-1,0,1,2,3,24,25,2147483647)
for n in range(220):
 run('s',[rng.choice(edge),rng.choice(edge),rng.choice((-128,-1,0,1,2,3,127)),n%2,(n//2)%2,(1,3,4)[n%3],13,rng.choice(edge),n%2,(0,0,0,1,2,3,4,8,9)[n%9]])
for n in range(160):run('m',[rng.choice(edge),rng.choice((-128,-1,0,1,2,3,127)),n%2,rng.choice(edge),n%3==0])
for kind in('d','l'):
 for n in edge:
  for super_flag in(0,1):run(kind,[n,super_flag])
for n in range(150):run('a',[rng.choice((0,2,3,4,5,99)),rng.choice((-128,-1,0,1,2,3,127)),rng.choice(edge)])
for n in range(160):run('c',[n%5!=0,n%3,*[rng.choice(edge)for _ in range(10)]])
mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
sources=['SourceCatalog/RemainingSessionHarness.cs','Assets/Foundation/Runtime/OriginalRemainingSessionEnd.cs','Assets/Foundation/Runtime/OriginalRemainingEventRewards.cs','Assets/Foundation/Runtime/OriginalSessionValues.cs']
with tempfile.TemporaryDirectory()as folder:
 exe=Path(folder)/'Session.exe';command=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',
  *['-r:'+str(mono/'lib/mono/4.5'/name)for name in('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),*[str(PROJECT/p)for p in sources]]
 result=subprocess.run(command,capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 actual=result.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected)
 for i,(a,b)in enumerate(zip(actual,expected)):assert a==b,(i,cases[i],a,b)
report={'passed':True,'cases':len(cases),'case_counts':counts,'scope':__doc__.strip(),
 'files':[{'path':p,'sha256':hashlib.sha256((PROJECT/p).read_bytes()).hexdigest()}for p in sources]}
(PROJECT/'remaining_session_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
