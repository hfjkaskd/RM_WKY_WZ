"""Decision-loop differential: actual original ARM64 vs compiled production C#.

Original external presentation/account/flow routines are identical recorded call
boundaries on both sides. This does not validate their implementations or bind them.
"""
from pathlib import Path
import hashlib,json,random,struct,subprocess,sys,tempfile
PROJECT=Path(__file__).resolve().parents[1];RECOVERY=PROJECT.parent
sys.path.insert(0,str(RECOVERY/'tools/python'))
from elftools.elf.elffile import ELFFile
from elftools.elf.relocation import RelocationSection
from unicorn import Uc,UC_ARCH_ARM64,UC_MODE_ARM,UC_HOOK_CODE
import unicorn.arm64_const as r
u=Uc(UC_ARCH_ARM64,UC_MODE_ARM);u.mem_map(0,0x6000000);u.mem_map(0x10000000,0x200000);u.mem_map(0x20000000,0x1000)
def put(p,v,n=4):u.mem_write(p,(v&((1<<(n*8))-1)).to_bytes(n,'little'))
def read(p,n=4):return int.from_bytes(u.mem_read(p,n),'little')
def fput(p,v):u.mem_write(p,struct.pack('<f',v))
def fbits(v):return struct.unpack('<I',struct.pack('<f',v))[0]
def signed(v):v&=0xffffffff;return v-(1<<32)if v&(1<<31)else v
lib=RECOVERY.parent/'RoyalMatch_20260929/apk_unpacked/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so'
with lib.open('rb')as stream:
 elf=ELFFile(stream)
 for segment in elf.iter_segments():
  if segment['p_type']=='PT_LOAD':u.mem_write(segment['p_vaddr'],segment.data())
 for section in elf.iter_sections():
  if isinstance(section,RelocationSection):
   for v in section.iter_relocations():
    if v['r_info_type']==1027:put(v['r_offset'],v['r_addend'],8)
owner,state,goals,moves,auto,duke=[0x10000000+i*0x1000 for i in range(6)]
slots={};heap=0x10080000;trace=[];clock=0;mask=0;checks=False;complete=False;fast=True;token=False
def hook(uc,a,size,unused):
 global heap,mask
 x0,x1=[uc.reg_read(getattr(r,'UC_ARM64_REG_X'+str(i)))for i in range(2)];value=None
 if a==0xffd960:
  if x0 not in slots:
   slots[x0]=heap;heap+=0x800;put(slots[x0]+0xe0,1);put(slots[x0]+0x12c,1,1);put(slots[x0]+0xb8,slots[x0]+0x200,8)
  put(x0,slots[x0],8);value=0
 elif a in [0xffd90c,0xffda64]:value=x0
 elif a in [0xffda88,0xffde20]:raise AssertionError(('Native exception',hex(a),hex(uc.reg_read(r.UC_ARM64_REG_LR))))
 elif a==0x3205e3c:uc.reg_write(r.UC_ARM64_REG_S0,fbits(clock));value=0
 elif a==0x1e18b58:value=int(fast)
 elif a==0x1ee51b4:value=int(mask!=0)
 elif a==0x1ee517c:value=int((mask&(1<<x1))!=0)
 elif a==0x32155a8:value=int(checks)
 elif a==0x3214840:value=int(complete)
 elif a==0x22300bc:value=int(token)
 elif a==0x1edc738:mask&=~(1<<x1);value=0
 elif a==0x1edd734:put(state+0x18,2);trace.append('win');value=0
 elif a==0x1ede364:put(state+0x18,3);value=0
 elif a in [0x1eddefc,0x2183d20,0x1ede230,0x1ede420,0x1ede668,0x1ede7f0,0x163174c,0x1edec08]:
  trace.append({0x1eddefc:'winflow',0x2183d20:'send',0x1ede230:'extra',0x1ede420:'ego',0x1ede668:'nomove',0x1ede7f0:'fail',0x163174c:'spend',0x1edec08:'stopfade'}[a]);value=0
 elif a==0x1eddcb8:
  n=read(owner+0x78)+1;put(owner+0x78,n);trace.append('warn:'+str(n)+':'+str(read(state+0x18)));fput(owner+0x74,clock);uc.reg_write(r.UC_ARM64_REG_PC,0x1edde10);return
 elif a==0x1eddbd8:
  trace.append('spend');uc.reg_write(r.UC_ARM64_REG_PC,0x1edde10);return
 elif a==0x344f940:value=0 # Bonus-only charger boundary, unchanged first-board exclusion.
 elif a==0x260dea4:value=duke
 if value is not None:uc.reg_write(r.UC_ARM64_REG_X0,value);uc.reg_write(r.UC_ARM64_REG_PC,uc.reg_read(r.UC_ARM64_REG_LR))
u.hook_add(UC_HOOK_CODE,hook)
def invoke(address,*args):
 for i,v in enumerate(args):u.reg_write(getattr(r,'UC_ARM64_REG_X'+str(i)),v)
 u.reg_write(r.UC_ARM64_REG_SP,0x101ff000);u.reg_write(r.UC_ARM64_REG_LR,0x20000000);u.emu_start(address,0x20000000,count=20000)
 assert u.reg_read(r.UC_ARM64_REG_PC)==0x20000000,hex(u.reg_read(r.UC_ARM64_REG_PC))
def native(case):
 global trace,clock,mask,checks,complete,fast,token
 config,*commands=case.split(';');st,bonus,fast,token=map(int,config.split(','));trace=[];clock=0;mask=0;checks=complete=False
 u.mem_write(owner,bytes(0x100));u.mem_write(state,bytes(0x100));u.mem_write(moves,bytes(0x100));u.mem_write(auto,bytes(0x100));put(owner+0x10,state,8);put(owner+0x18,goals,8);put(owner+0x20,moves,8);put(owner+0x38,auto,8);put(owner+0x50,moves,8);put(owner+0x60,duke,8);put(owner+0x6a,1,1);put(state+0x10,state+0x80,8);put(state+0x18,st);put(moves+0x28,bonus,1)
 for cmd in commands:
  v=cmd.split(',')
  if v[0]=='t':clock=float(v[1]);mask=int(v[2]);put(auto+0x10,int(v[3]),1);checks=v[4]=='1';invoke(0x1edd990,owner)
  elif v[0]=='g':complete=v[1]=='1';invoke(0x1edd92c,owner,11,0 if complete else 1)
  elif v[0]=='m':invoke(0x1edd6a0,owner,int(v[1])&0xffffffff,0)
  elif v[0]=='s':put(state+0x18,int(v[1]))
  elif v[0]=='c':invoke(0x1edec84,owner,1)
 return ','.join(map(str,[read(state+0x18),read(owner+0x6a,1),read(owner+0x7c,1),read(owner+0x68,1),mask]))+'|'+'/'.join(trace)
cases=[
 '1,0,1,0;m,0;g,1;t,1,0,0,0;t,1.7,0,0,1;t,1.8,0,0,1',
 '1,0,1,0;m,0;t,1,1,0,0;t,1.1,0,0,0;t,1.31,0,0,0',
 '1,0,1,0;g,1;t,1,2097152,0,1;t,1.1,0,0,1;t,1.201,0,0,1',
 '1,0,1,0;g,1;t,1,32,0,1;t,1.3,0,1,1;t,1.6,0,0,1',
 '1,0,1,1;m,0;t,1,0,0,0;t,2,0,0,0',
 '1,0,0,0;g,1;t,1,1,0,0;t,1.1,0,0,0;t,1.31,0,0,0',
 '1,1,0,0;m,0;t,1,0,0,0',
 '2,0,1,0;t,1,3,0,0;t,11.01,3,0,0;t,11.02,0,0,1',
 '3,0,1,0;t,1,4,0,0;t,11.01,4,0,0;t,22,4,0,0',
 '5,0,1,1;t,1,0,0,0', '4,0,1,0;t,1,0,0,0', '6,0,1,0;t,1,0,0,0',
 '1,0,1,0;m,0;c;t,1,0,0,0;g,1;t,2,0,0,1'
]
rng=random.Random(160021)
for _ in range(160):
 state_value=rng.randrange(7);bonus=rng.randrange(2);fast_value=rng.randrange(2);token_value=rng.randrange(2);cmds=[f'{state_value},{bonus},{fast_value},{token_value}'];now=0
 for j in range(15):
  k=rng.randrange(10)
  if k==0:cmds.append('m,'+str(rng.choice([0,1,-1])))
  elif k==1:cmds.append('g,'+str(rng.randrange(2)))
  elif k==2:cmds.append('s,'+str(rng.randrange(7)))
  elif k==3:cmds.append('c')
  else:
   now+=rng.choice([.016,.1,.2,.21,1,10.01]);ops=rng.choice([0,0,1,2,3,4,32,2097152]);cmds.append(f't,{now:.5f},{ops},{rng.randrange(2)},{rng.randrange(2)}')
 cases.append(';'.join(cmds))
data=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data');mono=data/'MonoBleedingEdge';runtime=PROJECT/'Assets/Foundation/Runtime/OriginalWinFailManager.cs'
with tempfile.TemporaryDirectory(prefix='rm_win_fail_')as temp:
 exe=Path(temp)/'WinFailHarness.exe';cmd=[str(data/'NetCoreRuntime/dotnet.exe'),str(data/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',*[f'-r:{mono/"lib/mono/4.5"/x}'for x in ['mscorlib.dll','System.dll','System.Core.dll']],'-out:'+str(exe),str(PROJECT/'SourceCatalog/WinFailHarness.cs'),str(runtime)]
 result=subprocess.run(cmd,capture_output=True,text=True);assert result.returncode==0,result.stdout+result.stderr
 result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf8');assert result.returncode==0,result.stdout+result.stderr
 actual=result.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(cases),(len(actual),len(cases))
 for i,(case,value)in enumerate(zip(cases,actual)):
  try:expected=native(case)
  except Exception as e:raise AssertionError((i,case,trace,hex(u.reg_read(r.UC_ARM64_REG_X19))))from e
  assert value==expected,(i,case,expected,value)
report={'passed':True,'cases':len(cases),'source_sha256':hashlib.sha256(runtime.read_bytes()).hexdigest(),'original_rvas':['0x1edd990','0x1edd6a0','0x1edd92c','0x1eddeac','0x1edec84'],'scope':'Decision gates/state flags/call order versus actual original ARM64, with identical recorded presentation/account/flow call boundaries. Includes last-move goal priority, .2s operation/check gates, pending automatic matches, out-of-moves/extra token, normal/bonus, timeout recovery, clear. Does not verify omitted Win/Flow/Session/UI implementations or establish production binding.'}
(PROJECT/'win_fail_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf8');print(json.dumps(report,indent=2))
