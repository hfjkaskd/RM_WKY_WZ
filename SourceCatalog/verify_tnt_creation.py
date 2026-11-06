"""TNT creation/idle sorting and fuse control versus original ARM64."""
import sys
sys.dont_write_bytecode=True
import verify_board_layout as b
import json,re,random,struct,subprocess,tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import *
p=b.p
view,klass,model,modelklass,fuse,fuseklass,cell,item,fall,fallklass,group,blocker,sorting_manager=[0x10010000+i*0x1000 for i in range(13)]
objs={0x10030000:'b',0x10031000:'a',0x10032000:'reset',0x10033000:'g',0x10034000:'f0',0x10035000:'f1',0x10036000:'f2'}
base,anim,reset,sorting_group,f0,f1,f2=objs
objs.update({0x10037000:'light',0x10038000:'mask'})
GET_CELL,IS_CROSS,GET_ATLAS,GET_FALSE,RECYCLE,GET_SORT=[0x20000100+i*4 for i in range(6)]
bits=lambda v:struct.unpack('<I',struct.pack('<f',v))[0]
trace=[];has_cell=True;touch=incoming=cross=state_hash=state_time=0;playing=1;fault='';active={};layers={};orders={}
def resolve(slot):
    if slot not in b.resolved_slots:p.emu.reg_write(UC_ARM64_REG_X0,slot);b.boundary(p.emu,0xffd960,4,None)
    return p.read(slot,8)
def boundary(uc,address,size,data):
    global playing,fault
    x0,x1,x2=[uc.reg_read(r) for r in (UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2)];value=None
    if address==GET_CELL:trace.append('cell');value=cell if has_cell else 0
    elif address==GET_ATLAS:value=0xffffffff
    elif address==GET_FALSE:value=0
    elif address==GET_SORT:uc.reg_write(UC_ARM64_REG_X1,0);value=layers[sorting_group]|((orders[sorting_group]&0xffffffff)<<32)
    elif address==IS_CROSS:trace.append('cross');value=cross
    elif address==RECYCLE:trace.append('recycle-fuse');value=0
    elif address in (0x3e473b8,0x3e4737c):value=x0
    elif address==0x3e49b30:active[x0]=x1;trace.append('active:'+objs[x0]+':'+str(x1));value=0
    elif address==0x3e49490:value=active[anim]
    elif address==0x3e4e4a0:trace.append('local:'+objs[x0]+':'+':'.join(str(uc.reg_read(r)) for r in (UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2)));value=0
    elif address==0x3e50f78:value=0
    elif address in (0x3e4e2c8,0x3e4e958,0x3e4e568):
        for i,reg in enumerate((UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2,UC_ARM64_REG_S3)):uc.reg_write(reg,bits(1. if address==0x3e4e958 or address==0x3e4e568 and i==3 else 0.))
        value=0
    elif address in (0x3e4e368,0x3e4e5f0,0x3e4e9f8,0x3e5104c,0x141b5d8,0x141b658,0x141b4d8,0x141b558):value=0
    elif address==0x1e3f44c:trace.append('state:'+str(x1));uc.mem_write(uc.reg_read(UC_ARM64_REG_X8),bytes(36));value=0
    elif address==0x1e3ed48:value=state_hash
    elif address==0x1e3ed58:uc.reg_write(UC_ARM64_REG_S0,state_time);value=0
    elif address==0x1e3fe50:trace.append('animation:'+str(p.signed(x1&0xffffffff))+':'+str(x2)+':'+str(uc.reg_read(UC_ARM64_REG_S0)));value=0
    elif address==0x21805c0:trace.append('sound:'+str(x1)+':'+str(uc.reg_read(UC_ARM64_REG_S0)));value=0
    elif address==0x38db1c4:trace.append('playing:'+str(playing));value=playing
    elif address==0x38db708:playing=1;trace.append('fuse-play');value=0
    elif address==0x38db868:playing=0;trace.append('fuse-stop');value=0
    elif address in (0x3b4e368,0x3e3ebc0):layers[x0]=p.signed(x1&0xffffffff);trace.append('layer:'+objs[x0]+':'+str(layers[x0]));value=0
    elif address in (0x3b4e3e8,0x3e3ec40):orders[x0]=p.signed(x1&0xffffffff);trace.append('order:'+objs[x0]+':'+str(orders[x0]));value=0
    elif address==0x3440db8:trace.append('touch');value=touch
    elif address==0x319dc64:trace.append('incoming');value=incoming
    elif address==0x319017c:value=item
    elif address==0x27b4dcc:trace.append('decrease');value=0
    elif address in (0x2b4acdc,0x2b4ad2c):
        enabled=int(address==0x2b4ad2c);active[reset]=enabled;trace.append('active:reset:'+str(enabled));value=0
    elif address==0xffda88:fault='null';uc.reg_write(UC_ARM64_REG_PC,0x20000000);return
    if value is not None:uc.reg_write(UC_ARM64_REG_X0,value&0xffffffffffffffff);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,boundary)
def call(rva,*args):
    for i,v in enumerate(args):p.emu.reg_write(UC_ARM64_REG_X0+i,v&0xffffffffffffffff)
    p.emu.reg_write(UC_ARM64_REG_SP,0x101ff000);p.emu.reg_write(UC_ARM64_REG_LR,0x20000000)
    p.emu.emu_start(rva,0x20000000,count=200000)
    assert p.emu.reg_read(UC_ARM64_REG_PC)==0x20000000,hex(p.emu.reg_read(UC_ARM64_REG_PC))
    return p.emu.reg_read(UC_ARM64_REG_X0)
def interface(klass,slot,methods,storage):
    p.put(klass+0x12a,1,2);p.put(klass+0xb0,storage,8);p.put(storage,resolve(slot),8);p.put(storage+8,0)
    for i,method in methods.items():p.put(klass+0x138+i*16,method,8)
def setup(y):
    p.emu.mem_write(view,bytes(0x20000))
    p.put(view,klass,8);p.put(view+0x28,sorting_group,8);p.put(view+0x50,sorting_manager,8);p.put(view+0x58,1,1)
    p.put(view+0x38,0x10044000,8);p.put(view+0x60,model,8);p.put(view+0x70,base,8);p.put(view+0x78,anim,8);p.put(view+0xc0,model,8);p.put(view+0xd0,fuse,8)
    p.put(view+0xb8,0x10037000,8);p.put(view+0xa8,0x10038000,8);p.put(view+0xb0,0x10039000,8);p.put(view+0xa0,0x1003a000,8)
    p.put(klass+0x208,0x27b8bcc,8);p.put(klass+0x3a8,0x27b9098,8);p.put(klass+0x2e8,GET_FALSE,8);p.put(klass+0x2f8,GET_FALSE,8);p.put(klass+0x1b8,GET_ATLAS,8)
    p.put(klass+0x218,GET_SORT,8)
    p.put(model,modelklass,8);interface(modelklass,0x558e108,{3:GET_CELL},0x10040000)
    p.put(fuse,fuseklass,8);p.put(fuse+0x30,0x10041000,8)
    p.put(fuse+0x38,f0,8);p.put(fuse+0x40,f1,8);p.put(fuse+0x48,f2,8)
    interface(fuseklass,0x558e5d0,{0:0x27b7c0c,1:0x27b7c48,2:0x27b7c64,3:0x27b7c70,4:0x27b7c78},0x10042000)
    p.put(fuseklass+0x1b8,RECYCLE,8)
    p.put(cell+0x1c,y);p.put(cell+0xc0,blocker,8);p.put(item+0x40,fall,8);p.put(fall,fallklass,8)
    interface(fallklass,0x558df78,{9:IS_CROSS},0x10043000)
for slot,offsets in ((0x55940a8,{8:231352099,12:151353909,20:487648401}),(0x5595120,{0:111,4:222})):
    owner=0x10050000+(slot&0xfff);p.put(resolve(slot)+0xb8,owner,8)
    for offset,value in offsets.items():p.put(owner+offset,value)
# Compare serialized TNT explosion/shake data to the actual native prepare.
call(0x27b58ec,model)
asset=(p.PROJECT/'Assets/Foundation/Settings/TntViewAssets.asset').read_text()
fields=('Radius','RingInterval','ExplodeDelay','Delay','Duration','MinVibrate','MidVibrate','MaxVibrate')
values=[float(re.search(r'^ +'+field+r': ([^\n]+)',asset,re.M)[1]) for field in fields]
assert p.read(model+0xc0)==int(values[0])
for offset,value in zip(range(0xc4,0xe0,4),values[1:]):assert p.read(model+offset)==bits(value),(hex(offset),value,p.read(model+offset))
assert p.read(model+0xe0,1)==0 and p.read(model+0xe1,1)==1 and p.read(model+0xe4)==0
vector_statics=0x10060000;p.put(resolve(0x5595e08)+0xb8,vector_statics,8)
for i,value in enumerate((0.,0.,0.,1.,1.,1.)):p.put(vector_statics+i*4,bits(value))
quat_statics=0x10061000;p.put(resolve(0x5591e28)+0xb8,quat_statics,8)
for i,value in enumerate((0.,0.,0.,1.)):p.put(quat_statics+i*4,bits(value))
cases=[];expected=[];rng=random.Random(0x27b811c)
for n in range(128):
    y=rng.choice((0,1,5,11));touch=n%2;incoming=n//2%2;cross=n//4%2
    has_cell=True;playing=1;active={base:1,anim:0,reset:1};layers={sorting_group:231352099};orders={sorting_group:1000};setup(y)
    commands=[['F'],['U',0,bits(0.)],['P',bits(rng.choice((0.,.4,-1.,float('nan')))),n%2]]
    for i,t in enumerate((.4,.40000004,.5,.50000006,.9,1.,float('nan'),float('inf'))):
        if i==2 and n%3==0:commands.append(['C',0])
        if i==4:commands.append(['N',n%2])
        commands.append(['U',222 if n%7==0 else 0,bits(t)])
        commands.append(['S',231352099,2000+i,n//8%2])
    commands.extend([['C',1],['T'],['B'],['U',111,bits(.45)],['U',111,bits(.6)],['U',111,bits(1.)],['L'],['U',222,bits(.8)],['U',222,bits(1.4)],['T'],['E'],['H'],['V'],['R']])
    cases.append(';'.join([','.join(map(str,[y,touch,incoming,cross])),*[','.join(map(str,c)) for c in commands]]))
    for command in commands:
        trace=[];fault='';op=command[0]
        if op=='P':p.emu.reg_write(UC_ARM64_REG_S0,command[1]);call(0x27b8d9c,view,command[2])
        elif op=='U':state_hash,state_time=command[1:];call(0x27b811c,view)
        elif op=='S':call(0x27b8bcc,view,(command[1]&0xffffffff)|((command[2]&0xffffffff)<<32),command[3])
        elif op=='C':has_cell=bool(command[1])
        elif op=='N':call(0x27b985c,view,command[1])
        else:call({'F':0x27b9790,'E':0x27b9850,'T':0x27b9098,'H':0x27b87e0,'V':0x27b8890,'B':0x27b8f18,'L':0x27b925c,'R':0x27b8944}[op],view)
        expected.append(','.join(map(str,[active[base],active[anim],playing,p.read(view+0xdd,1),layers[sorting_group],orders[sorting_group],fault]))+';'+'|'.join(trace))
with tempfile.TemporaryDirectory() as folder:
    exe=Path(folder)/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(p.PROJECT/'SourceCatalog/TntCreationHarness.cs'),*[str(runtime/f) for f in ('OriginalTntItemSpriteView.cs','OriginalTntFuseParticles.cs','OriginalTntViewAssets.cs')]],capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    actual=result.stdout.replace('\ufeff','').splitlines()
    assert actual==expected,next(((i,a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e),(len(actual),len(expected)))
report={'passed':True,'native_sequences':128,'native_observations':len(expected),'scope':'Actual ARM64 TNT creation, animator progress/idle/fall watch, normal/booster/lightball state dispatch, stop, under-static sorting, exact recycle subset and fuse Play/Stop/three-renderer ordering versus production C#. Ordered current-cell/static/incoming/cross-fall queries, threshold boundaries including NaN/infinity, sorting locks, mask visibility, recycle pose and sound dispatch are compared. Serialized explosion/shake config is also checked against native PrepareConfig. Component/Animator/particle storage, module metadata, common Hide/Show and creation-particle transfer are supplied boundaries. Full real prefab/model/creator/pool integration is checked separately in Unity; TNT explosion/combo damage, full fall motion, audio playback and original rendered parity remain pending.'}
(p.PROJECT/'tnt_creation_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
