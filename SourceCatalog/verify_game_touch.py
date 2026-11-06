"""GameTouchListener and arithmetic grid hit testing versus original ARM64."""
import sys
sys.dont_write_bytecode=True
import verify_board_layout as b
import json,struct,math,random,subprocess,tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import *
p=b.p
listener,move,booster,gridman,uiman,level,cameraman,camera,grid,outpoint,vecstatic,gamestatic,game,ui,ui2,boosterui=[0x10010000+i*0x1000 for i in range(16)]
cells=[0x10060000+i*0x100 for i in range(96)]
trace=[];fault=False;world=(0.,0.);ui_touch=selected=can_move=deselect=down=held=up=ios=False
bits=lambda v:struct.unpack('<I',struct.pack('<f',v))[0]
value=lambda v:struct.unpack('<f',struct.pack('<I',v))[0]
f32=lambda v:value(bits(v))
def resolve(slot):
 if slot not in b.resolved_slots:p.emu.reg_write(UC_ARM64_REG_X0,slot);b.boundary(p.emu,0xffd960,4,None)
 return p.read(slot,8)
def cellid(address):return 0 if address==0 else cells.index(address)+1
def boundary(uc,address,size,data):
 global fault,selected
 x0,x1,x2=[uc.reg_read(r) for r in (UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2)];result=None
 if address==0x2c99b18:trace.append('ui');result=int(ui_touch)
 elif address==0x2666914:
  trace.append('mouse');result=0
  uc.reg_write(UC_ARM64_REG_S0,bits(world[0]));uc.reg_write(UC_ARM64_REG_S1,bits(world[1]));uc.reg_write(UC_ARM64_REG_S2,0)
 elif address==0x3e433e8:
  assert uc.reg_read(UC_ARM64_REG_S2)==0;trace.append('project');result=0
  uc.reg_write(UC_ARM64_REG_S0,bits(world[0]));uc.reg_write(UC_ARM64_REG_S1,bits(world[1]));uc.reg_write(UC_ARM64_REG_S2,0)
 elif address in (0x2666960,0x266699c,0x2667e20):
  name,state={0x2666960:('down',down),0x266699c:('held',held),0x2667e20:('up',up)}[address];assert x0==0;trace.append(name);result=int(state)
 elif address==0x1eda16c:trace.append('can');result=int(can_move)
 elif address==0x31950b4:
  x,y=p.signed(x1&0xffffffff),p.signed((x1>>32)&0xffffffff);trace.append('cell:'+str(x)+':'+str(y));result=cells[x+y*12] if 0<=x<12 and 0<=y<8 else 0
 elif address==0x2d78224:result=int(ios)
 elif address==0x1e35b34:trace.append('should:'+str(cellid(x1)));result=int(deselect)
 elif address==0x13c0a90:trace.append('deselect');selected=False;p.put(booster+0x88,0);result=0
 elif address==0x1ee1ebc:trace.append('cancel');result=0
 elif address==0x1ee1df0:
  trace.append('touch:'+str(cellid(x1)))
  if not x1:fault=True;uc.reg_write(UC_ARM64_REG_PC,0x20000000);return
  result=0
 elif address==0x1e3c950:trace.append('swap:'+str(cellid(x1))+':'+str(p.signed(x2&0xffffffff))+':'+str(p.signed(x2>>32)));result=0
 elif address==0x1e3cc6c:trace.append('tap:'+str(cellid(x1)));result=0
 elif address==0xd025c0:
  arg=value(uc.reg_read(UC_ARM64_REG_S0));uc.reg_write(UC_ARM64_REG_S0,bits(math.atan(arg)));result=0
 elif address==0x1e2b3c8 and p.read(listener+0x48)==0:trace.append('enable-error');result=0
 elif address==0xffda88:fault=True;uc.reg_write(UC_ARM64_REG_PC,0x20000000);return
 if result is not None:uc.reg_write(UC_ARM64_REG_X0,result&0xffffffffffffffff);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,boundary)
p.put(resolve(0x5595df0)+0xb8,vecstatic,8);p.put(vecstatic,0,8)
p.put(resolve(0x558d170)+0xb8,gamestatic,8);p.put(gamestatic+8,game,8);p.put(game+0x20,ui,8);p.put(ui+0x20,ui2,8);p.put(ui2+0x18,boosterui,8)
for i,c in enumerate(cells):p.put(c+0x18,(i%12)|((i//12)<<32),8)
def call(rva,*args,xy=None):
 for i,v in enumerate((*args,0,0,0)):p.emu.reg_write(UC_ARM64_REG_X0+i,v&0xffffffffffffffff)
 if xy is not None:p.emu.reg_write(UC_ARM64_REG_S0,xy[0]);p.emu.reg_write(UC_ARM64_REG_S1,xy[1])
 p.emu.reg_write(UC_ARM64_REG_SP,0x101ff000);p.emu.reg_write(UC_ARM64_REG_LR,0x20000000)
 p.emu.emu_start(rva,0x20000000,count=200000)
 assert p.emu.reg_read(UC_ARM64_REG_PC)==0x20000000,hex(p.emu.reg_read(UC_ARM64_REG_PC))
 return p.emu.reg_read(UC_ARM64_REG_X0)
def setup():
 p.emu.mem_write(listener,bytes(0x100));p.put(listener+0x10,cameraman,8);p.put(listener+0x18,move,8);p.put(listener+0x20,booster,8);p.put(listener+0x28,gridman,8);p.put(listener+0x50,uiman,8);p.put(listener+0x58,level,8)
 p.put(cameraman+0x18,camera,8);p.put(gridman+0x40,grid,8);p.put(gridman+0x60,0);p.put(gridman+0x64,0);p.put(gridman+0x68,0,1);p.put(booster+0x88,0)
cases=[];expected=[];rng=random.Random(0x1e3c868)
def cmd(op,x=0.,y=0.,*rest):return ','.join(map(str,(op,bits(x),bits(y),*rest)))
# Exact limits, 30/60-degree boundaries, signed zero, overflow and unordered inputs.
angles=[(x,y) for x in (-1.,0.,1.) for y in (-1.,0.,1.)]
for degrees in (29.99999,30.,30.00001,59.99999,60.,60.00001):
 for signx in (-1.,1.):
  for signy in (-1.,1.):angles.append((signx,signy*f32(math.tan(math.radians(degrees)))))
angles += [(float('nan'),1.),(1.,float('nan')),(float('inf'),1.),(1.,float('inf')),(float('inf'),float('inf'))]
for x,y in angles:cases.append(cmd('T',3.,4.,52)+';'+cmd('G',x,y))
for n in range(96):
 x,y=rng.choice(((0.,0.),(3.,4.),(-.5,4.),(11.49,7.49)))
 commands=[cmd('M',x,y,1,1,1,0,0,1,0)]
 # UI blocking, deselection and disabled input must preserve selection state.
 commands += [cmd('M',x+.1,y,0,1,0,1,0,1,0),'D',cmd('M',x+.2,y,1,1,1,0,0,1,0),'D','E','C,1']
 dx,dy=rng.choice(((.49999997,0.),(.5,0.),(.50000006,0.),(.6,.6),(0.,-.5),(float('nan'),0.)))
 commands += [cmd('M',f32(x+dx),f32(y+dy),0,1,0,0,0,1,0),cmd('M',x,y,0,0,1,0,0,1,0)]
 commands += [cmd('P',x,y),cmd('U',f32(x+.50000006),y),'C,0',cmd('V',x,y+.8),cmd('U',x,y)]
 commands += [cmd('M',x,y,1,1,1,0,1,1,n%2),cmd('M',x,y,1,1,1,0,0,0,0),'S','E']
 # Explicit signal precedence and both Clear branches with a live selection.
 commands += [cmd('T',x,y,52),'C,'+str(n%2),cmd('M',x,y,n%2,n//2%2,n//4%2,0,0,1,0)]
 cases.append(';'.join(commands))
cases.append(cmd('G',1.,0.))
cases.append(cmd('T',3.,4.,0)+';'+cmd('V',float('nan'),4.)+';'+cmd('U',3.,4.)+';C,1;S')
input_sequences=len(cases)
for line in cases:
 setup();selected=False;extra=''
 for command in line.split(';'):
  f=command.split(',');op=f[0];trace=[];fault=False;extra='';xy=tuple(map(int,f[1:3])) if op in ('M','P','V','U','T','G') else None
  if op=='M':
   world=tuple(value(v) for v in xy);down,held,up,ui_touch,selected,can_move,deselect=map(lambda v:bool(int(v)),f[3:]);p.put(booster+0x88,int(selected));call(0x1e3c2c8,listener)
  elif op=='G':
   ok=call(0x1e3c868,listener,outpoint,xy=xy)
   if not fault:point=p.read(outpoint,8);extra=str(ok&1)+':'+str(p.signed(point&0xffffffff))+':'+str(p.signed(point>>32))
  elif op=='T':call(0x1e3c810,listener,0 if f[3]=='0' else cells[int(f[3])-1],xy=xy)
  elif op in ('P','V','U'):call({'P':0x1e3c540,'V':0x1e3c5a0,'U':0x1e3c680}[op],listener,xy=xy)
  else:call({'D':0x1e2a8c0,'E':0x1e2b3c8,'C':0x1e3c2c0,'S':0x1e3c77c}[op],listener,int(f[1]) if op=='C' else 0)
  expected.append(','.join(map(str,(p.signed(p.read(listener+0x48)),p.read(listener+0x30,1),p.read(listener+0x34),p.read(listener+0x38),cellid(p.read(listener+0x40,8)),int(fault),extra)))+';'+'|'.join(trace))
input_observations=len(expected)
limits=[-float('inf'),-2147483648.,-1.5,-.65000004,-.65,-.6499999,-.50000006,-.5,-.49999997,-0.,0.,.49999997,.5,.50000006,7.5,8.499999,8.5,8.500001,8.649999,8.65,8.650001,2147483648.,float('inf'),float('nan')]
for correction in (0,1):
 for ios in (0,1):
  for i,x in enumerate(limits):
   y=limits[(i*7)%len(limits)];result=call(0x343b7a4,correction,xy=(bits(x),bits(y)))
   cases.append('POINT;'+','.join(map(str,(bits(x),bits(y),correction,ios))));expected.append(str(p.signed(result&0xffffffff))+','+str(p.signed(result>>32)))
for n in range(128):
 setup();ox,oy=rng.choice(((-4.,-3.5),(0.,0.),(.5,-.5)));x,y=rng.uniform(-5,13),rng.uniform(-5,10);correction=n%2;ios=n//2%2
 p.put(gridman+0x60,bits(ox));p.put(gridman+0x64,bits(oy));p.put(gridman+0x68,correction,1);trace=[]
 result=call(0x320cfa0,gridman,xy=(bits(x),bits(y)));cases.append('WORLD;'+','.join(map(str,(bits(x),bits(y),bits(ox),bits(oy),correction,ios))));expected.append(str(cellid(result))+';'+'|'.join(trace))
with tempfile.TemporaryDirectory() as folder:
 exe=Path(folder)/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
 result=subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(p.PROJECT/'SourceCatalog/GameTouchHarness.cs'),str(runtime/'OriginalGameTouchListener.cs'),str(runtime/'OriginalCellGridHitTest.cs')],capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 result=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 actual=result.stdout.lstrip('\ufeff').splitlines();assert actual==expected,next(((i,a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e),(len(actual),len(expected)))
report={'passed':True,'native_input_sequences':input_sequences,'native_input_observations':input_observations,'native_point_cases':96,'native_world_lookup_cases':128,'scope':'Actual ARM64 GameTouchListener manual input precedence, UI/booster/move guards, down/move/up/start/stop, Clear subset, nested disable/enable, half-cell distance and 30/60-degree angle dead zone versus production C#. Preserves large-release/UI-block/Clear state, ordered and unordered float behavior. Actual CellPoint rounding, strict iOS edge bands and translated grid lookup also compared. Camera/Input engine reads, context services, debug formatting and atanf are supplied boundaries; no full level touch/resolution or original rendered parity claim.'}
(p.PROJECT/'game_touch_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
