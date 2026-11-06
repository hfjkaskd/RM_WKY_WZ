"""Actual Cell14 index/add and by-value difficulty shuffle native checks."""
import sys
sys.dont_write_bytecode=True
import json,os,random,subprocess,tempfile
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_move_budget.py').read_text().split('cases=[];expected=[]')[0],str(PROJECT/'SourceCatalog/verify_move_budget.py'),'exec'))
emu.hook_del(native_hook);base_hook=hook
group,manager,rnd,klass=[0x10020000+i*0x1000 for i in range(4)];storage=group+8;random_call=0x1002f000
put(manager+0x38,rnd,8);put(rnd,klass,8);put(klass+0x198,random_call,8)
def i32(v):return signed(v&0xffffffff)
class ClrRandom:
 def __init__(self,seed):
  self.a=[0]*56;mj=i32(161803398-(2147483647 if seed==-2147483648 else abs(seed)));self.a[55]=mj;mk=1
  for i in range(1,55):
   ii=21*i%55;self.a[ii]=mk;mk=i32(mj-mk)
   if mk<0:mk+=2147483647
   mj=self.a[ii]
  for _ in range(4):
   for i in range(1,56):
    self.a[i]=i32(self.a[i]-self.a[1+(i+30)%55])
    if self.a[i]<0:self.a[i]+=2147483647
  self.i=0;self.j=21
 def next(self,lo,hi):
  self.i=self.i+1 if self.i<55 else 1;self.j=self.j+1 if self.j<55 else 1
  v=i32(self.a[self.i]-self.a[self.j])
  if v==2147483647:v-=1
  if v<0:v+=2147483647
  self.a[self.i]=v;return int(v*(1.0/2147483647)*(hi-lo))+lo
random_source=None
def hook(uc,address,size,unused):
 x0,x1,x2=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(3)]
 if address==0x12de690:uc.reg_write(UC_ARM64_REG_PC,0x12de74c) # Omit only invalid-index log formatting.
 elif address==0x12de850:uc.reg_write(UC_ARM64_REG_PC,0x12de90c)
 elif address==0x260dea4:assert x0==0;returned(uc,manager)
 elif address==random_call:returned(uc,random_source.next(i32(x1),i32(x2)))
 elif address==0xd00b50:emu.mem_write(x0,bytes(emu.mem_read(x1,x2)));returned(uc,x0)
 else:base_hook(uc,address,size,unused)
for a in [0xffd960,0xffd90c,0xffda64,0xffda88,0xffda90,0xffda50,0x12de690,0x12de850,0x260dea4,random_call,0xd00b50]:emu.hook_add(UC_HOOK_CODE,hook,begin=a,end=a)
def cell(ident):return 0 if ident<0 else 0x10040000+ident*0x100
def ident(pointer):return -1 if pointer==0 else (pointer-0x10040000)//0x100
def format_storage():return ','.join(map(str,[read(storage)]+[ident(read(storage+8+i*8,8))for i in range(14)]))
def init(count):
 emu.mem_write(group,bytes(160))
 for i in range(count):call(0x12de9a0,storage,cell(i));assert not fault
cases=[];expected=[];commands=0
rng=random.Random(0x12de610)
for count in range(21):
 for seq in range(8):
  init(count);rows=[];output=[]
  for j in range(30):
   mode=rng.randrange(3);index=rng.choice([-2147483648,-1,0,1,5,13,14,15,2147483647]);value=rng.randrange(-1,30)
   args=(storage,index)if mode==0 else(storage,index,cell(value))if mode==1 else(storage,cell(value))
   answer=call((0x12de610,0x12de7cc,0x12de9a0)[mode],*args);assert not fault
   result=str(ident(emu.reg_read(UC_ARM64_REG_X0)))if mode==0 else'-'
   output.append(result+':'+format_storage());rows.append([mode,index,value]);commands+=1
  cases.append('0,'+str(count)+'#'+';'.join(','.join(map(str,r))for r in rows));expected.append('|'.join(output))
cell_cases=len(cases)
for count in range(21):
 for seed in (0,1,371,-371,2147483647,-2147483648):
  for repeat in (1,2,4):
   init(count);put(manager+0x28,0);random_source=ClrRandom(seed)
   for _ in range(repeat):call(0x12df454,group);assert not fault
   answer=call(0x1ed98c8,manager,0,1000000);assert not fault
   cases.append(','.join(map(str,(1,count,seed,repeat)))+'#');expected.append(format_storage()+':'+str(read(manager+0x28))+','+str(answer))
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-cell-shuffle-')as folder:
 exe=Path(folder)/'verify.exe';runtime=PROJECT/'Assets/Foundation/Runtime'
 result=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-langversion:latest','-out:'+str(exe),str(PROJECT/'SourceCatalog/CellShuffleHarness.cs'),str(runtime/'OriginalMatchSnapshot.cs'),str(runtime/'OriginalLevelRandomSource.cs'),str(runtime/'OriginalLevelRandomMatchCells.cs')],capture_output=True,text=True,encoding='utf-8-sig');assert result.returncode==0,result.stdout+result.stderr
 result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8-sig');assert result.returncode==0,result.stdout+result.stderr
 actual=result.stdout.splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
 for i,(a,e)in enumerate(zip(actual,expected)):assert a==e,(i,cases[i],a,e)
report={'passed':True,'native_cell_sequences':cell_cases,'native_cell_commands':commands,'native_shuffle_cases':len(cases)-cell_cases,'scope':'Actual ARM64 Cell14 get/set/Add, MatchGroup.ShuffleCells, LevelRandomManager.ShuffleCellsBasedOnDifficulty and GetDifficultyNext. Compares all fourteen inline fields, capacity rather than count indexing, null/invalid indexes, unchanged Count on set, append-before-increment including overflow past capacity, outer inline-value independence and difficulty draw count/next sample over repeated shuffles and signed seeds. CLR metadata/write barriers/memcpy and LevelContext owner lookup supplied; invalid-index diagnostic formatting/logging omitted on both sides. Native System.Random.Next receives a supplied classic subtractive CLR stream; C# uses actual Mono System.Random. This verifies shuffle orchestration/state consumption, not the full native random-owner/seed lifecycle.'}
(PROJECT/'cell_shuffle_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
