"""Native MatchItemView highlight activation/ratio/table versus production state."""
import sys
sys.dont_write_bytecode=True
import verify_board_layout as b
import json,struct,subprocess,tempfile,itertools
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import *
p=b.p
view,renderer,model,klass,pairs=[0x10010000+i*0x1000 for i in range(5)]
GET_TYPE=0x20000100
alpha=0;active=False;match_type=0;trace=[]
def bits(x):return struct.unpack('<I',struct.pack('<f',x))[0]
def resolve(slot):
 if slot not in b.resolved_slots:p.emu.reg_write(UC_ARM64_REG_X0,slot);b.boundary(p.emu,0xffd960,4,None)
 return p.read(slot,8)
def boundary(uc,address,size,data):
 global alpha,active
 x0,x1=[uc.reg_read(r)for r in (UC_ARM64_REG_X0,UC_ARM64_REG_X1)];value=None
 if address==0x3e473b8:value=renderer
 elif address==0x3e49b30:active=bool(x1);trace.append('active:'+str(int(active)));value=0
 elif address==0x3e50edc:
  trace.append('get')
  for r,v in zip((UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2,UC_ARM64_REG_S3),(bits(.1),bits(.3),bits(.7),alpha)):uc.reg_write(r,v)
  value=0
 elif address==0x3e50f78:
  assert [uc.reg_read(r)for r in (UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2)]==[bits(.1),bits(.3),bits(.7)]
  alpha=uc.reg_read(UC_ARM64_REG_S3);trace.append('set:'+str(alpha));value=0
 elif address==GET_TYPE:value=match_type&0xffffffff
 elif address==0xffda88:raise AssertionError('Unexpected native null surface')
 if value is not None:uc.reg_write(UC_ARM64_REG_X0,value);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,boundary)
def call(rva,*args):
 for i,v in enumerate((*args,0,0)):p.emu.reg_write(UC_ARM64_REG_X0+i,v&0xffffffffffffffff)
 p.emu.reg_write(UC_ARM64_REG_SP,0x101ff000);p.emu.reg_write(UC_ARM64_REG_LR,0x20000000)
 p.emu.emu_start(rva,0x20000000,count=200000);assert p.emu.reg_read(UC_ARM64_REG_PC)==0x20000000
p.put(view+0x80,renderer,8);p.put(model,klass,8);p.put(klass+0x12a,1,2);p.put(klass+0xb0,pairs,8);p.put(pairs,resolve(0x558e108),8);p.put(pairs+8,0);p.put(klass+0x158,GET_TYPE,8)
cases=[];expected=[]
ratios=[-float('inf'),-2.,-.0011,-.001,-.000999,-0.,0.,.000999,.001,.0011,.25,1.,2.,float('inf'),float('nan')]
ops=[('H',0),('H',1),('M',0),('G',0)]+[('R',bits(r))for r in ratios]
for suppressed,highlighted,match_type,bound,(op,arg) in itertools.product((False,True),(False,True),(-2147483648,-1,0,1,2,3,4,5,6,2147483647),(False,True),ops):
 p.put(view+0x68,int(suppressed),1);p.put(view+0xa8,int(highlighted),1);p.put(view+0x98,model if bound else 0,8);alpha=bits(.2);active=highlighted;trace=[];result=''
 if op=='H':call(0x21a6ae8,view,arg)
 elif op=='R':p.emu.reg_write(UC_ARM64_REG_S0,arg);call(0x21a74f8,view)
 elif op=='M':call(0x21a76e4,view);result=str(p.emu.reg_read(UC_ARM64_REG_S0))
 elif op=='G':call(0x21a76a8,view);result=str(p.emu.reg_read(UC_ARM64_REG_S0))
 # The C# surface combines get/color-with-alpha/set as one alpha property.
 # Native get is retained as evidence here, but omitted for setter comparison.
 trace=[t for i,t in enumerate(trace)if not(t=='get' and i+1<len(trace) and trace[i+1].startswith('set:'))]
 cases.append(','.join(map(str,(op,int(suppressed),int(highlighted),match_type,int(bound),arg))))
 expected.append('|'.join(trace)+'#'+','.join(map(str,(p.read(view+0xa8,1),int(active),alpha,result))))
with tempfile.TemporaryDirectory() as folder:
 exe=Path(folder)/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
 result=subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(p.PROJECT/'SourceCatalog/MatchHighlightHarness.cs'),str(runtime/'OriginalMatchHighlight.cs')],capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 result=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 actual=result.stdout.lstrip('\ufeff').splitlines();assert actual==expected,next(((i,cases[i],a,e)for i,(a,e)in enumerate(zip(actual,expected))if a!=e),(len(actual),len(expected)))
report={'passed':True,'native_cases':len(cases),'scope':'Actual ARM64 MatchItemView Highlight/SetHighlightRatio/GetHighlightRatio/GetMaxHighlightRatio versus production inline state, including original five-color float table, absent view delegate, arbitrary match IDs, suppression flag input, retained alpha on disable, strict epsilon, signed zero, infinities and NaN. Unity GameObject and renderer RGB/alpha storage plus interface dispatch are supplied boundaries. Surface alpha setter combines native color get/preserve RGB/set. Full base static-flag setter/sorting lifecycle is not claimed; real configured renderer and press/cancel/input are separately exercised in Unity.'}
(p.PROJECT/'match_highlight_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
