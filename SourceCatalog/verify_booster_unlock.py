"""Execute original ARM64 unlock, AB, EventLock and PrepareButton bodies.

Only metadata, current-user/inventory/time inputs, Unity object/graphic calls
and text/localization boundaries are ports. UI-state branch bodies execute in
the ELF. No original UI click/booster use, production inventory or mobile proof.
"""
from pathlib import Path
import hashlib,json,random,struct,subprocess,sys
ROOT=Path(__file__).resolve().parents[1];BASE=ROOT.parent;sys.path.insert(0,str(BASE/'tools/python'))
from elftools.elf.elffile import ELFFile
from elftools.elf.relocation import RelocationSection
from unicorn import Uc,UC_ARCH_ARM64,UC_MODE_ARM,UC_HOOK_CODE
import unicorn.arm64_const as r
u=Uc(UC_ARCH_ARM64,UC_MODE_ARM);u.mem_map(0,0x6000000);u.mem_map(0x10000000,0x400000);u.mem_map(0x20000000,0x3000)
def put(p,v,n=8):u.mem_write(p,(int(v)&((1<<(n*8))-1)).to_bytes(n,'little'))
def get(p,n=8):return int.from_bytes(u.mem_read(p,n),'little')
def sx(i,v):u.reg_write(getattr(r,'UC_ARM64_REG_X'+str(i)),int(v)&((1<<64)-1))
def x(i):return u.reg_read(getattr(r,'UC_ARM64_REG_X'+str(i)))
def ret(v=0):sx(0,v);u.reg_write(r.UC_ARM64_REG_PC,u.reg_read(r.UC_ARM64_REG_LR))
elf_path=BASE.parent/'RoyalMatch_20260929/apk_unpacked/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so'
with elf_path.open('rb')as stream:
    elf=ELFFile(stream)
    for seg in elf.iter_segments():
        if seg['p_type']=='PT_LOAD':u.mem_write(seg['p_vaddr'],seg.data())
    for section in elf.iter_sections():
        if isinstance(section,RelocationSection):
            for entry in section.iter_relocations():
                if entry['r_info_type']==1027:put(entry['r_offset'],entry['r_addend'])
owner=0x10001000;event=0x10002000;user=0x10003000;basic=0x10004000
user_slot=get(0x53bb4e0);vector_slot=get(0x53df178)
objects={name:0x10100000+i*0x1000 for i,name in enumerate(('countText','add','count','icon','button','collider','ribbon','freeText','levelText','inventory','lockedAsset','lockedSprite','transform'))}
put(owner+0x18,objects['lockedAsset']);put(objects['lockedAsset']+0x20,objects['lockedSprite'])
for offset,name in ((0x28,'countText'),(0x30,'add'),(0x38,'count'),(0x40,'icon'),(0x48,'button'),(0x68,'ribbon'),(0x70,'freeText'),(0x78,'levelText'),(0x98,'inventory')):put(owner+offset,objects[name])
put(objects['button']+0x38,objects['collider']);put(user+0x18,basic)
text_class=0x10110000;put(objects['countText'],text_class);put(text_class+0x518,0x20001000)
states={};sprite=0;trace='';case=None;metadata={};next_meta=0x10200000
def hook(uc,address,size,unused):
    global next_meta,sprite,trace
    if address==0xffd960:
        slot=x(0)
        if slot not in metadata:
            p=next_meta;next_meta+=0x1000;metadata[slot]=p;put(slot,p);put(p+0xe0,1,4);put(p+0xb8,p+0x400)
            if slot==user_slot:put(p+0x400,user)
            if slot==vector_slot:u.mem_write(p+0x400,struct.pack('<6f',0,0,0,1,1,1))
        ret();return
    if address==0xffda64:put(x(0)+0xe0,1,4);ret();return
    if address==0xffda88:raise RuntimeError('Unexpected native null case='+repr(case)+' lr='+hex(u.reg_read(r.UC_ARM64_REG_LR)))
    if address==0x29e27cc:ret(event);return
    if address==0x32bb7e0:ret(case[3]);return
    if address==0x3e49b30 or address==0x3e4944c:states[x(0)]=bool(x(1));ret();return
    if address==0x3e50c6c:sprite=x(1);ret();return
    if address==0x3e4737c:ret(objects['transform']);return
    if address==0x3e4e9f8 or address==0x3e4e4a0:ret();return
    if address==0x13c2588:ret();return
    if address==0x13c2720:trace+='F';ret(case[5]);return
    if address==0x28e6cf4:trace+='C';ret(case[6]);return
    if address==0x3e4d92c:ret(x(0)!=x(1));return
    if address==0x276aa04 or address==0x20001000:ret();return
    if address==0x2653db0:ret(0x10120000);return
    raise RuntimeError(hex(address))
for address in (0xffd960,0xffda64,0xffda88,0x29e27cc,0x32bb7e0,0x3e49b30,0x3e4944c,0x3e50c6c,0x3e4737c,
    0x3e4e9f8,0x3e4e4a0,0x13c2588,0x13c2720,0x28e6cf4,0x3e4d92c,0x276aa04,0x2653db0,0x20001000):
    u.hook_add(UC_HOOK_CODE,hook,begin=address,end=address)
def run(c):
    global states,sprite,trace,case
    case=c;kind,typ,level,group,tutorial,free,count=c;states={};sprite=1;trace=''
    put(basic+0x14,level,4);put(event+0x18,group!=0,1);put(owner+0x80,typ,4);put(owner+0xa0,tutorial,1);put(owner+0xa8,0)
    for i in range(31):sx(i,0)
    u.reg_write(r.UC_ARM64_REG_SP,0x103ff000);u.reg_write(r.UC_ARM64_REG_LR,0x20000000)
    if kind==0:sx(0,typ);address=0x318d4c4
    elif kind==1:sx(0,typ);address=0x318d4e8
    elif kind==2:sx(0,owner);address=0x32be230
    elif kind==3:sx(0,event);sx(1,tutorial);address=0x3268ad4
    else:sx(0,owner);address=0x13c14d4
    u.emu_start(address,0x20000000,count=12000)
    if u.reg_read(r.UC_ARM64_REG_PC)!=0x20000000:raise RuntimeError('Native instruction limit')
    if kind<4:return str(x(0)&0xffffffff)
    flags=int(sprite!=objects['lockedSprite'])
    for i,name in enumerate(('collider','levelText','count','countText','add','ribbon','freeText'),1):flags|=int(states.get(objects[name],False))<<i
    return str(flags)+','+str(count if 'C'in trace else 0)+','+trace
cases=[]
levels=(-2147483648,-1,0,1,4,5,7,8,9,13,14,15,16,17,18,19,20,2147483647)
types=(-2147483648,-1,0,1,2,3,4,5,6,7,8,9,2147483647)
for typ in types:
    cases.append((0,typ,0,0,0,0,0))
    for level in levels:cases.append((1,typ,level,0,0,0,0))
for group in (-2147483648,-1,0,1,2,3,4,2147483647):cases.append((2,0,0,group,0,0,0))
for group in (0,1):
    for level in levels:
        for skip in (0,1):cases.append((3,0,level,group,skip,0,0))
for typ in (4,5,6,7):
    for level in levels:
        for group in (0,1):
            for tutorial in (0,1):
                for free in (0,1):
                    for count in (0,1,65535):cases.append((4,typ,level,group,tutorial,free,count))
expected=[run(c)for c in cases]
editor=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data');exe=BASE/'tools/SpineOfflineBaker/BoosterUnlockHarness.exe'
subprocess.run([str(editor/'NetCoreRuntime/dotnet.exe'),str(editor/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',
    *['-r:'+str(editor/'MonoBleedingEdge/lib/mono/4.5'/n)for n in('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),
    str(ROOT/'SourceCatalog/BoosterUnlockHarness.cs'),str(ROOT/'Assets/Foundation/Runtime/OriginalBoosterUnlock.cs')],check=True)
result=subprocess.run([str(editor/'MonoBleedingEdge/bin/mono.exe'),str(exe)],input='\n'.join('|'.join(map(str,c))for c in cases)+'\n',text=True,encoding='utf-8',capture_output=True,check=True)
actual=[s.lstrip('\ufeff')for s in result.stdout.splitlines()];assert len(actual)==len(cases)
for c,a,e in zip(cases,actual,expected):assert a==e,(c,a,e,[hex(ord(ch))for ch in a[:4]])
report={'passed':True,'cases':len(cases),'native_prepare_button_cases':sum(c[0]==4 for c in cases),'original_arm64_bodies_executed':True,
    'ports':'metadata/current-user/AB value/inventory and free-use queries/Unity graphics/localization/string boundary',
    'ui_state_and_inventory_query_order_compared':True,'original_unlock_levels':[run((0,t,0,0,0,0,0))for t in(4,5,6,7)],
    'production_inventory_booster_use_tooltip_settings_and_ab_provider_verified':False,
    'source_sha256':hashlib.sha256((ROOT/'Assets/Foundation/Runtime/OriginalBoosterUnlock.cs').read_bytes()).hexdigest()}
(ROOT/'booster_unlock_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2))
