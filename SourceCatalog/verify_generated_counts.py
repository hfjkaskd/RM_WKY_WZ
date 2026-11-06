"""Actual ARM64 ItemCreator clear/restore generated-count orchestration.

CLR dictionary/enumerator, saved-loader/pair, element lists, classification,
goal-limit/target/prerequisite and ratio-recalculation calls are supplied
ports. Native capture/re-read, traversal, arithmetic, disabling and partial
failure state execute from ELF. Real generation/set/goal/predefined owners
and decoded saved-count access are separately tested in Unity. This is not
full binary save parsing or production restore/startup/mobile proof.
Only unsupported-tiled-ID diagnostic formatting is skipped.
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
owner,goal,predefined,created_dict,initial_dict,restore,loader0,loader1,set_array,set0,set1,list0,list1,list2=[0x10001000+i*0x1000 for i in range(14)]
list_ids={list0:0,list1:1,list2:2};lists={};dicts={};enumerators={};metadata=0x10200000;trace=[];calls=0;fault=False;head=[];prerequisites=9;current_loader=0;pairs=[]
def fail():
 global fault
 fault=True;u.reg_write(r.UC_ARM64_REG_PC,0x20000000)
def sync_list(p,ids):
 lists[p]=list(ids);put(p+0x18,len(ids),4)
 for i,value in enumerate(ids):put(p+0x100+i*0x20+0x10,value,4);put(p+0x100+i*0x20+0x18,1,1)
def step(label):
 global calls,current_loader
 trace.append(label);calls+=1
 if calls==head[1]:fail();return False
 if calls==head[2]:
  kind=head[3]
  if kind==1:current_loader=1
  elif kind==2:put(owner+0x20,0)
  elif kind==3:put(owner+0x20,set_array);put(set_array+0x18,0,4)
  elif kind==4:
   p=read(set0+0x18);i=len(lists[p]);lists[p].append(25);put(p+0x18,len(lists[p]),4);put(p+0x100+i*0x20+0x10,25,4);put(p+0x100+i*0x20+0x18,1,1)
  elif kind==5:put(set0+0x18,list2)
  elif kind==6:dicts[created_dict].clear()
 return True
def result(label,value=0):
 if step(label):returned(value)
def hook(uc,address,size,unused):
 global metadata,prerequisites
 a,b,c=x(0),x(1),x(2)
 if address==0xffd960:put(a,metadata);put(metadata+0xe0,1,4);metadata+=0x200;returned();return
 if address in(0xffd90c,0xffda64):returned();return
 if address in(0xffda88,0xffda90):fail();return
 if address==0x190f59c:uc.reg_write(r.UC_ARM64_REG_PC,0x190f65c);return
 if address==0x3212fe8:
  assert a==goal
  if step('clear'):prerequisites=0;returned()
  return
 if address==0x3c96364:dicts[a].clear();returned();return
 if address==0x3c965cc:enumerators[x(8)]=[list(dicts[a].items()),0];returned();return
 if address==0x347b0c0:
  values,index=enumerators[a]
  if index==len(values):returned();return
  key,value=values[index];put(a+0x10,key,4);put(a+0x14,value,4);enumerators[a][1]+=1;returned(1);return
 if address==0x347b1b8:returned();return
 if address==0x3c961d8:
  key=si(b)
  if key in dicts[a]:fail();return
  dicts[a][key]=si(c);returned();return
 if address==0x3c97c34:
  key=si(b);put(c,dicts[a].get(key,0),4);returned(int(key in dicts[a]));return
 if address==0x3c96128:
  if si(b)not in dicts[a]:fail();return
  returned(dicts[a][si(b)]);return
 if address==0x3c961c0:dicts[a][si(b)]=si(c);returned();return
 if address==0x23df9e4:
  if step('restore'):returned(0 if head[0]==4 else restore)
  return
 if address==0x23e3a6c:assert a in(loader0,loader1);result('length,'+str(int(a==loader1)),len(pairs[int(a==loader1)])//2);return
 if address==0x23e3b70:
  assert a in(loader0,loader1);ident=int(a==loader1)
  if not step('pair,'+str(ident)+','+str(b)):return
  values=pairs[ident]
  if b*2+1>=len(values):fail();return
  returned(values[b*2]);u.reg_write(r.UC_ARM64_REG_X1,values[b*2+1]&0xffffffffffffffff);return
 if address in(0x191798c,0x1917948):returned(si(read(a+(8 if address==0x191798c else 0),8)));return
 if address==0x2b452f8:result('obstacle,'+str(si(a)),int(si(a)in(7,13,8)));return
 if address in(0x32145d8,0x32142a0):assert a==goal;result(('limit,'if address==0x32145d8 else'target,')+str(si(b)),head[4]if address==0x32145d8 else head[5]);return
 if address==0x2c1b9a0:
  ident=list_ids[a]
  if not step('element,'+str(ident)+','+str(b)):return
  if b>=len(lists[a]):fail();return
  returned(a+0x100+b*0x20);return
 if address==0x23eb28c:assert a==owner;result('calculate');return
 raise AssertionError(hex(address))
for a in(0xffd960,0xffd90c,0xffda64,0xffda88,0xffda90,0x190f59c,0x3212fe8,0x3c96364,0x3c965cc,0x347b0c0,0x347b1b8,0x3c961d8,0x3c97c34,0x3c96128,0x3c961c0,0x23df9e4,0x23e3a6c,0x23e3b70,0x191798c,0x1917948,0x2b452f8,0x32145d8,0x32142a0,0x2c1b9a0,0x23eb28c):u.hook_add(UC_HOOK_CODE,hook,begin=a,end=a)
# Native Current is re-read at each call; synchronize its loader field after
# the restore getter callback, including callbacks that switch the singleton.
def current_hook(uc,address,size,unused):
 if address in(0x23eb008,0x23eb054):put(restore+0x20,loader0 if current_loader==0 else loader1)
u.hook_add(UC_HOOK_CODE,current_hook,begin=0x23eb008,end=0x23eb008);u.hook_add(UC_HOOK_CODE,current_hook,begin=0x23eb054,end=0x23eb054)
def call(address):
 global fault
 fault=False
 for i in range(8):u.reg_write(getattr(r,'UC_ARM64_REG_X'+str(i)),owner if i==0 else 0)
 u.reg_write(r.UC_ARM64_REG_SP,0x103ff000);u.reg_write(r.UC_ARM64_REG_LR,0x20000000)
 try:u.emu_start(address,0x20000000,count=20000)
 except Exception as error:raise AssertionError((hex(address),hex(u.reg_read(r.UC_ARM64_REG_PC)),head,trace))from error
 assert u.reg_read(r.UC_ARM64_REG_PC)==0x20000000,hex(u.reg_read(r.UC_ARM64_REG_PC))
def dictionary(data):return dict(zip(data[::2],data[1::2]))
def native(h,existing,initial,saved,commands):
 global head,trace,calls,dicts,enumerators,prerequisites,current_loader,pairs
 head=h;trace=[];calls=0;prerequisites=9;current_loader=0;pairs=[saved,[7,3,13,-2147483648,8,0,7,2147483647]];enumerators={};dicts={created_dict:dictionary(existing),initial_dict:dictionary(initial)}
 for p in(owner,predefined,set_array,set0,set1,list0,list1,list2):u.mem_write(p,bytes(0x800))
 put(owner+0x110,0 if h[0]==1 else goal);put(owner+0x118,0 if h[0]==2 else created_dict);put(owner+0x100,0 if h[0]==3 else predefined);put(predefined+0x18,initial_dict)
 put(owner+0x20,0 if h[0]==5 else set_array);put(set_array+0x18,1 if h[0]==6 else 2,4);put(set_array+0x20,0 if h[0]==6 else set0);put(set_array+0x28,set1)
 put(set0+0x18,0 if h[0]==7 else list0);put(set1+0x18,list1);put(set0+0x50,1,1);put(set1+0x50,1,1);sync_list(list0,[25,1,25,41]);sync_list(list1,[13,25]);sync_list(list2,[25,1])
 for command in commands:
  call(0x23ead94 if command=='C'else 0x23eaf58)
  if fault:break
 flags=[]
 for p in(set0,set1):
  elements=read(p+0x18)
  flags.append(('-1,'if not elements else str(list_ids[elements])+','+''.join(str(read(elements+0x100+i*0x20+0x18,1))for i in range(len(lists[elements]))))+','+str(read(p+0x50,1)))
 return str(int(fault))+'#'+str(prerequisites)+'#'+'/'.join(str(k)+','+str(v)for k,v in dicts[created_dict].items())+'#'+'/'.join(flags)+'#'+';'.join(trace)
cases=[]
for missing,limit,target in itertools.product(range(8),(0,1,3,5),(0,1,3,5)):
 for commands in('C','U','CU','UCU'):
  cases.append(([missing,0,0,0,limit,target],[7,11,13,-4,99,2],[1,90,7,2],[7,3,7,5,13,-2147483648],commands))
for stage in range(1,80):
 cases.append(([0,stage,0,0,3,5],[7,11],[1,90,7,2],[7,3,7,5,13,5],'CU'))
for stage,mutation in itertools.product(range(1,45),range(1,7)):
 cases.append(([0,0,stage,mutation,3,5],[7,11],[1,90,7,2],[7,3,7,5,13,5],'CUU'))
rng=random.Random(0x23eaf58)
for _ in range(400):
 existing=[7,si(rng.getrandbits(32)),13,si(rng.getrandbits(32))];initial=[1,rng.randrange(100),7,rng.randrange(6)]
 saved=[rng.choice((1,7,8,13)),si(rng.getrandbits(32)),rng.choice((1,7,8,13)),rng.randrange(6)]
 cases.append(([rng.randrange(8),rng.randrange(80),rng.randrange(45),rng.randrange(7),rng.randrange(6),rng.randrange(6)],existing,initial,saved,rng.choice(('C','U','CU','UCU'))))
expected=[native(*case)for case in cases]
mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
sources=['SourceCatalog/GeneratedCountHarness.cs','Assets/Foundation/Runtime/OriginalGeneratedItemCountState.cs','Assets/Foundation/Runtime/OriginalTiledItemTypes.cs','Assets/Foundation/Runtime/OriginalGoalTypes.cs','Assets/Foundation/Runtime/OriginalLevelRules.cs']
with tempfile.TemporaryDirectory()as directory:
 exe=Path(directory)/'Counts.exe';args=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',*['-r:'+str(mono/'lib/mono/4.5'/p)for p in('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),*[str(PROJECT/p)for p in sources]]
 result=subprocess.run(args,capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 lines=['|'.join([','.join(map(str,a))for a in case[:4]]+[case[4]])for case in cases]
 result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(lines)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 actual=result.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected)
 for i,(a,b)in enumerate(zip(actual,expected)):assert a==b,(i,cases[i],a,b)
report={'passed':True,'cases':len(cases),'scope':__doc__.strip(),'original_rvas':['0x23ead94','0x23eaf58'],'files':[{'path':p,'sha256':hashlib.sha256((PROJECT/p).read_bytes()).hexdigest()}for p in sources+['SourceCatalog/verify_generated_counts.py']]}
(PROJECT/'generated_counts_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
