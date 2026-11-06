from hashlib import sha256
import json
from pathlib import Path
import random
import sqlite3
import struct
import sys

ROOT=Path(__file__).resolve().parents[1]
if len(sys.argv)!=2:raise SystemExit('Usage: verify_dialog_tween_clock.py OUTPUT_DIRECTORY')
WORK=Path(sys.argv[1]).resolve()
WORK.mkdir(parents=True,exist_ok=True)
sys.path.insert(0,str(ROOT.parent/'tools/python'))
from elftools.elf.elffile import ELFFile
from elftools.elf.relocation import RelocationSection
from unicorn import Uc,UC_ARCH_ARM64,UC_MODE_ARM,UC_HOOK_CODE
import unicorn.arm64_const as r
machine=Uc(UC_ARCH_ARM64,UC_MODE_ARM)
machine.mem_map(0,0x6000000)
machine.mem_map(0x10000000,0x400000)
machine.mem_map(0x20000000,0x1000)
with (ROOT.parent.parent/'RoyalMatch_20260929/apk_unpacked/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so').open('rb') as stream:
    elf=ELFFile(stream)
    for segment in elf.iter_segments():
        if segment['p_type']=='PT_LOAD':machine.mem_write(segment['p_vaddr'],segment.data())
    for section in elf.iter_sections():
        if isinstance(section,RelocationSection):
            for item in section.iter_relocations():
                if item['r_info_type']==1027:machine.mem_write(item['r_offset'],struct.pack('<Q',item['r_addend']))
def put(address,value,n=8):machine.mem_write(address,int(value).to_bytes(n,'little'))
def read(address,n=8):return int.from_bytes(machine.mem_read(address,n),'little')
def fbits(value):return struct.unpack('<I',struct.pack('<f',value))[0]
def return_(value=None):
    if value is not None:machine.reg_write(r.UC_ARM64_REG_X0,value)
    machine.reg_write(r.UC_ARM64_REG_PC,machine.reg_read(r.UC_ARM64_REG_LR))
metadata=0x10100000
delta=0
trace=[]
manual=[]
level_class,level_static,level,clock,clock_class,interface=range(0x10000000,0x10006000,0x1000)
level_slot=read(0x53f0418)
interface_slot=read(0x533fdc8)
def hook(uc,address,size,unused):
    global metadata
    if address==0xffd960:
        slot=uc.reg_read(r.UC_ARM64_REG_X0)
        if slot in (level_slot,interface_slot):return_();return
        put(slot,metadata);put(metadata+0xe0,1,4);put(metadata+0xb8,metadata+0x200);metadata+=0x400
        return_()
    elif address in (0xffd90c,0xffda64):return_()
    elif address in (0xffda88,0xffda90):raise AssertionError(hex(address))
    elif address==0x3e424ec:
        trace.append('Unity.deltaTime');uc.reg_write(r.UC_ARM64_REG_S0,delta);return_()
    elif address==0x32037e4:trace.append('LevelContext.DeltaTime')
    elif address==0x318d1c4:trace.append('DeltaTimeManager.DeltaTime')
    elif address==0x1d795f0:
        manual.append((uc.reg_read(r.UC_ARM64_REG_S0),uc.reg_read(r.UC_ARM64_REG_S1)));return_()
machine.hook_add(UC_HOOK_CODE,hook)
put(level_slot,level_class);put(level_class+0xb8,level_static);put(level_class+0xe0,1,4)
put(level_static,level);put(level+0x18,clock);put(clock,clock_class)
put(interface_slot,interface);put(clock_class+0x12a,1,2);put(clock_class+0xb0,clock_class+0x400)
put(clock_class+0x400,interface);put(clock_class+0x408,0,4);put(clock_class+0x148,0x318d1c4)
def call(address):
    machine.reg_write(r.UC_ARM64_REG_SP,0x103ff000)
    machine.reg_write(r.UC_ARM64_REG_LR,0x20000000)
    machine.emu_start(address,0x20000000,count=30000)
    assert machine.reg_read(r.UC_ARM64_REG_PC)==0x20000000
    return machine.reg_read(r.UC_ARM64_REG_S0)

inputs=[0,1/30,1/60,1/90,1/120,1/300,.00005,.0034501,.123456]
inputs += [random.Random(4581+i).uniform(0,.1) for i in range(256)]
samples=[]
for value in inputs:
    delta=fbits(value)
    quantized=call(0x318d1c4)
    for active in (False,True):
        put(level_static+8,int(active),1)
        trace.clear();manual.clear()
        actual=call(0x38ffc88)
        expected=quantized if active else delta
        assert manual==[(expected,expected)],(value,active,manual,expected)
        assert ('LevelContext.DeltaTime' in trace)==active,(value,active,trace,manual,hex(read(0x53f0418)),read(level_static+8,1))
        assert ('DeltaTimeManager.DeltaTime' in trace)==active,(value,active,trace)
        samples.append({'unity_delta_bits':delta,'level_active':active,'manual_delta_bits':manual[0][0],
                        'trace':list(trace)})
with sqlite3.connect(ROOT.parent/'code/native/functions.sqlite3') as db:
    methods={}
    for address in (0x38ffc34,0x38ffc88,0x32037e4,0x318d1c4,0x25cb6cc,0x25c7bf8,0x25c8170):
        name,body=db.execute('select name,body from functions where address=?',(address,)).fetchone()
        methods[hex(address)]={'name':name,'body_sha256':sha256(body.encode()).hexdigest()}
        (WORK/('native_'+hex(address)[2:]+'.txt')).write_text(body,encoding='utf-8')
report={'passed':True,'native_manual_update_cases':len(samples),'runtime_scope':'Original ARM64 TweenManager.GetDeltaTime/ManualUpdate, LevelContext clock dispatch and DeltaTimeManager quantization execute from libil2cpp. Unity Time getter, IL2CPP metadata and final DOTween.ManualUpdate transport are supplied ports.',
        'native_methods':methods,'samples':samples}
(WORK/'native_clock_comparison.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({'passed':True,'native_manual_update_cases':len(samples),'example_60fps_active':samples[5]},indent=2))
