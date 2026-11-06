"""Compare useful-move state and King relief scheduling against actual ARM64."""
import sys
sys.dont_write_bytecode = True
import json, os, random, subprocess, tempfile
from pathlib import Path
PROJECT = Path(__file__).resolve().parents[1]
RECOVERY = PROJECT.parent
sys.path.insert(0, str(RECOVERY/'tools/python'))
from elftools.elf.elffile import ELFFile
from elftools.elf.relocation import RelocationSection
from unicorn import Uc, UC_ARCH_ARM64, UC_MODE_ARM, UC_HOOK_CODE
from unicorn.arm64_const import *

emu = Uc(UC_ARCH_ARM64, UC_MODE_ARM)
emu.mem_map(0, 0x6000000)
emu.mem_map(0x10000000, 0x200000)
emu.mem_map(0x20000000, 0x1000)
def put(address, value, size=4):
    emu.mem_write(address, (value & ((1 << (8*size))-1)).to_bytes(size, 'little'))
def read(address, size=4): return int.from_bytes(emu.mem_read(address, size), 'little')
def signed(value): return value-0x100000000 if value & 0x80000000 else value
lib = RECOVERY.parent/'RoyalMatch_20260929/apk_unpacked/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so'
with lib.open('rb') as stream:
    elf = ELFFile(stream)
    for segment in elf.iter_segments():
        if segment['p_type']=='PT_LOAD': emu.mem_write(segment['p_vaddr'], segment.data())
    for section in elf.iter_sections():
        if isinstance(section, RelocationSection):
            for relocation in section.iter_relocations():
                if relocation['r_info_type']==1027: put(relocation['r_offset'], relocation['r_addend'], 8)

active, king, user, session, moves, game_state, counts, dictionary, set1, set2, caps = [0x10000000+i*0x1000 for i in range(11)]
user_class, king_class, game_class = 0x10010000, 0x10011000, 0x10012000
user_static, king_static = 0x10013000, 0x10014000
for got, slot, klass in ((0x53bb4e0,0x5595c00,user_class),(0x53b5300,0x558f0a0,king_class),(0x5351eb0,0x558d170,game_class)):
    assert read(got,8)==slot, (hex(got),hex(read(got,8)))
    put(slot,klass,8); put(klass+0xe0,1)
put(user_class+0xb8,user_static,8); put(king_class+0xb8,king_static,8)
put(user_static,user,8); put(user+0x38,session,8)
trace=[]; head=[]; fault=False; current=0; allocated=0; clear_counts=[]
metadata={}; metadata_next=0x10090000

def returned(uc,value=0):
    uc.reg_write(UC_ARM64_REG_X0,value & 0xffffffffffffffff)
    uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
def hook(uc,address,size,unused):
    global fault,current,allocated,metadata_next
    x0,x1=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(2)]
    if address==0x2b46b54: trace.append('level')
    elif address==0x260dea4:
        assert x0==98,x0
        trace.append('king,'+str(read(active+0xbc,1)))
        returned(uc,king if head[7] else 0)
    elif address==0x1e2aaac:
        trace.append('start,'+str(read(active+0xbc,1)))
        current=x0; returned(uc,0x10080000+allocated*0x10)
    elif address==0x23e7728: trace.append('win');returned(uc,head[3])
    elif address==0x1eda1d8: trace.append('playing');returned(uc,head[4])
    elif address==0x23e84c8: trace.append('worry');returned(uc,head[5])
    elif address==0x23e7d24:
        trace.append('stop,'+str(x1));put(king+0x58,0,1);put(king+0x62,1,1);returned(uc)
    elif address in (0x3cceac4,0x220c9c0,0x220ec60):
        index={0x3cceac4:0,0x220c9c0:1,0x220ec60:2}[address]
        assert x0==(dictionary,set1,set2)[index],(hex(address),hex(x0))
        clear_counts[index]=0;returned(uc)
    elif address==0xffda78:
        allocated+=1;value=0x10030000+allocated*0x100
        emu.mem_write(value,bytes(0x100));returned(uc,value)
    elif address==0xffd960:
        if x0 not in metadata and read(x0,8)<0x10000000:
            metadata[x0]=metadata_next;metadata_next+=0x200
            put(x0,metadata[x0],8);put(metadata[x0]+0xe0,1)
        returned(uc)
    elif address in (0x231007c,0xffd90c,0xffda64): returned(uc)
    elif address in (0xffda88,0xffda90):
        fault=True;uc.reg_write(UC_ARM64_REG_PC,0x20000000)
emu.hook_add(UC_HOOK_CODE,hook)

def call(rva,*args):
    global fault
    fault=False
    for i,arg in enumerate((*args,0,0,0)): emu.reg_write(UC_ARM64_REG_X0+i,arg & 0xffffffffffffffff)
    emu.reg_write(UC_ARM64_REG_SP,0x101ff000);emu.reg_write(UC_ARM64_REG_LR,0x20000000)
    try: emu.emu_start(rva,0x20000000,count=300000)
    except Exception:
        print('Native failure',hex(emu.reg_read(UC_ARM64_REG_PC)),head,trace);raise
    assert emu.reg_read(UC_ARM64_REG_PC)==0x20000000
    return signed(emu.reg_read(UC_ARM64_REG_X0)&0xffffffff)

cases=[];expected=[];command_count=0
def run(values,commands):
    global head,current,allocated,clear_counts,command_count
    head=values;current=0;allocated=0;missing=head[9]
    emu.mem_write(active,bytes(0x100));emu.mem_write(king,bytes(0x100))
    put(session+0x18,active if head[8] else 0,8)
    put(active+0xbc,head[6],1)
    for offset,value in zip((0xc0,0xc4,0xc8),head[11:14]):put(active+offset,value)
    put(king+0x18,moves if missing!=5 else 0,8);put(moves+0x20,head[1])
    put(king+0x20,game_state if missing!=7 else 0,8)
    put(king_static,caps if missing!=6 else 0,8);put(caps+0x18,head[2])
    put(king+0x58,head[0],1);put(king+0x60,1,1);put(king+0x61,1,1)
    for index,(offset,pointer)in enumerate(((0x40,counts),(0x38,dictionary),(0x48,set1),(0x50,set2)),1):
        put(king+offset,pointer if missing!=index else 0,8)
    put(counts+0x18,head[10])
    for i in range(head[10]):put(counts+0x20+i*4,i+1)
    clear_counts=[1,1,1];output=[]
    for command in commands:
        command_count+=1;trace.clear();result='-'
        if command=='n':
            if current:
                result=str(call(0x23e993c,current))
                if fault:result='error'
            else:result='error'
        else:
            rva={'u':0x2b46b54,'m':0x32ba8b4,'k':0x23e9220,'r':0x32ba8a4,'s':0x32ba8ac,'i':0x32ba87c,'b':0x23e7540}[command]
            call(rva,king if command in 'kb' else active)
            if fault:result='error'
        fields=[read(active+0xbc,1),*[signed(read(active+offset))for offset in (0xc0,0xc4,0xc8)],int(read(king+0xa8,8)!=0),read(king+0x60,1),read(king+0x61,1),read(king+0x58,1),read(king+0x62,1)]
        array='n' if missing==1 else ','.join(str(signed(read(counts+0x20+i*4)))for i in range(head[10]))
        collections=','.join('n' if missing==i+2 else str(v)for i,v in enumerate(clear_counts))
        output.append(result+':'+','.join(map(str,fields))+':'+array+':'+collections+':'+ ';'.join(trace))
    cases.append(','.join(map(str,head))+'#'+commands);expected.append('|'.join(output))

# Every relief gate, boundary and cap edge; unworried paths must skip services.
for worried in (0,1):
    for left in (-2147483648,-1,0,1,3,4,2147483647):
        for flags in range(8):
            for missing in range(8):
                run([worried,left,3,flags&1,(flags>>1)&1,(flags>>2)&1,0,1,1,missing,7,0,0,0],'uuknnnkunnum')
# Required current king/current active level and all data lengths.
for king_exists in (0,1):
    for level_exists in (0,1):
        for length in range(8):
            run([1,1,3,1,1,0,1,king_exists,level_exists,0,length,0,0,0],'umrkmnnu')
# Signed arithmetic wraps exactly as native; maximum uses signed comparison.
for total in (-2147483648,-1,0,2147483646,2147483647):
    for maximum in (-2147483648,-1,0,2,2147483647):
        for streak in (-2147483648,-1,0,2147483646,2147483647):
            run([0,30,3,0,0,0,0,1,1,0,0,total,maximum,streak],'iiiisir')
rng=random.Random(922063)
for i in range(350):
    run([rng.randrange(2),rng.choice((-1,0,1,2,3,4,30)),rng.randrange(5),*[rng.randrange(2)for _ in range(6)],rng.randrange(8),rng.randrange(8),rng.randint(-100,100),rng.randint(-100,100),rng.randint(-100,100)],''.join(rng.choice('umknrsib')for _ in range(25)))

mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
runtime=PROJECT/'Assets/Foundation/Runtime'
with tempfile.TemporaryDirectory(prefix='rm-useful-move-') as directory:
    exe=Path(directory)/'verify.exe'
    compiled=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(PROJECT/'SourceCatalog/UsefulMoveHarness.cs'),str(runtime/'OriginalUsefulMoveState.cs'),str(runtime/'OriginalLevelSessionCounters.cs'),str(runtime/'OriginalSessionValues.cs'),str(runtime/'OriginalKingRelief.cs')],capture_output=True,text=True)
    assert compiled.returncode==0,compiled.stdout+compiled.stderr
    completed=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8')
    assert completed.returncode==0,completed.stdout+completed.stderr
    actual=completed.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected)
    for i,(a,e)in enumerate(zip(actual,expected)):
        assert a==e,(i,cases[i],a,e)
report={'passed':True,'native_sequences':len(cases),'native_commands':command_count,'scope':'Actual ARM64 ItemModel.UsefulMoveMade active-session lookup, UserActiveLevelData useful flag/counter/reset functions, King TryKingRelief request deduplication and iterator creation, CheckBoardDelayed MoveNext and ResetBoardCheckData. Compares per-command state and ordered required-service calls, one-yield timing, signed counter overflow/max updates, all relief gates, missing active level/king/data/move/caps/game-state, partial reset effects and retained coroutine handle after failure. Metadata initialization, allocation, managed collection storage/Clear, scheduler handle and required win/playing/worry/stop analysis/UI services are supplied boundaries. Scheduler here is explicitly manual; real Unity first-yield and box event ordering are checked separately in the box fixture. Full King analysis/UI, session lifecycle/save, audio, formal box production binding and player resolution remain pending.'}
(PROJECT/'useful_move_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(json.dumps(report))
