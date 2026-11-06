"""Compare native item goal identities and ordered collect/progress dispatch."""
import sys
sys.dont_write_bytecode = True
import json, os, random, re, subprocess, tempfile
from pathlib import Path
PROJECT = Path(__file__).resolve().parents[1]
RECOVERY = PROJECT.parent
sys.path.insert(0, str(RECOVERY / 'tools/python'))
from elftools.elf.elffile import ELFFile
from elftools.elf.relocation import RelocationSection
from unicorn import Uc, UC_ARCH_ARM64, UC_MODE_ARM, UC_HOOK_CODE
from unicorn.arm64_const import *

lib = RECOVERY.parent / 'RoyalMatch_20260929/apk_unpacked/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so'
emu = Uc(UC_ARCH_ARM64, UC_MODE_ARM)
emu.mem_map(0, 0x6000000)
emu.mem_map(0x10000000, 0x200000)
emu.mem_map(0x20000000, 0x1000)
def put(address, value, size=4): emu.mem_write(address, (value & ((1 << (size*8))-1)).to_bytes(size, 'little'))
def read(address, size=4): return int.from_bytes(emu.mem_read(address, size), 'little')
def signed(value): return value - 0x100000000 if value & 0x80000000 else value
with lib.open('rb') as stream:
    elf = ELFFile(stream)
    for segment in elf.iter_segments():
        if segment['p_type'] == 'PT_LOAD': emu.mem_write(segment['p_vaddr'], segment.data())
    for section in elf.iter_sections():
        if isinstance(section, RelocationSection):
            for relocation in section.iter_relocations():
                if relocation['r_info_type'] == 1027: put(relocation['r_offset'], relocation['r_addend'], 8)

item, goals, game, ui, story, board, board_class, settings = [0x10000000+i*0x1000 for i in range(8)]
game_class, level_class, game_statics = 0x10010000, 0x10011000, 0x10012000
game_slot, level_slot = read(0x5351eb0, 8), read(0x536b340, 8)
assert game_slot == 0x558d170 and level_slot == 0x558f6b0, (hex(game_slot), hex(level_slot))
put(game_slot, game_class, 8); put(level_slot, level_class, 8)
put(game_class+0xe0, 1); put(level_class+0xe0, 1); put(game_class+0xb8, game_statics, 8)
put(board, board_class, 8); put(board_class+0x1f8, 0x20000100, 8)
put(item+0x60, goals, 8)
trace = []; values = []; fault = False; mode = ''

def returned(uc, value=0):
    uc.reg_write(UC_ARM64_REG_X0, value & 0xffffffffffffffff)
    uc.reg_write(UC_ARM64_REG_PC, uc.reg_read(UC_ARM64_REG_LR))
def hook(uc, address, size, unused):
    global fault
    x0, x1, x2 = [uc.reg_read(UC_ARM64_REG_X0+i) for i in range(3)]
    goal = signed(x1 & 0xffffffff)
    if address == 0x3213954:
        trace.append('u,'+str(goal))
        if values[1] != -999: put(item+0x20, values[1])
        returned(uc)
    elif address == 0x2b46d54:
        trace.append('R')
        if values[2] != -999: put(item+0x20, values[2])
    elif address in (0x1e18cf4, 0x1e18d50, 0x1e18dac):
        index = {0x1e18cf4:3,0x1e18d50:4,0x1e18dac:5}[address]
        trace.append({3:'S',4:'F',5:'P'}[index]); returned(uc, values[index])
    elif address == 0x11538a0:
        trace.append('s,'+str(goal)); returned(uc)
    elif address == 0x20000100:
        assert x2 in (0, 0x10008000), hex(x2)
        trace.append('b,'+str(goal)+','+str(int(x2 != 0)))
        returned(uc)
    elif address in (0xffda88, 0xffda90):
        fault = True; uc.reg_write(UC_ARM64_REG_PC, 0x20000000)
    elif address in (0xffd960, 0xffda64, 0xffd90c): returned(uc)
emu.hook_add(UC_HOOK_CODE, hook)

def call(rva, *args):
    global fault
    fault = False
    for i, arg in enumerate((*args, 0, 0, 0)):
        emu.reg_write(UC_ARM64_REG_X0+i, arg & 0xffffffffffffffff)
    emu.reg_write(UC_ARM64_REG_X8, settings)
    emu.reg_write(UC_ARM64_REG_SP, 0x101ff000); emu.reg_write(UC_ARM64_REG_LR, 0x20000000)
    try: emu.emu_start(rva, 0x20000000, count=300000)
    except Exception:
        print('Native fault',hex(emu.reg_read(UC_ARM64_REG_PC)),mode,values,trace); raise
    assert emu.reg_read(UC_ARM64_REG_PC) == 0x20000000
    return signed(emu.reg_read(UC_ARM64_REG_X0) & 0xffffffff)

cases = []; expected = []
probes = list(range(-64, 181)) + [-2147483648, 2147483647]
rng = random.Random(7143)
probes += [rng.randint(-2147483648,2147483647) for _ in range(100)]
for mode, rva in (('I',0x2b452a0),('M',0x2b452c4),('S',0x2b45064),('P',0x2b45614)):
    for value in probes:
        cases.append(mode+';'+str(value)); expected.append(str(call(rva,value)))
mapping_cases = len(cases)
runtime = PROJECT/'Assets/Foundation/Runtime'
tiled_ids = sorted(set(map(int,re.findall(r'case (\d+):',(runtime/'OriginalTiledItemTypes.cs').read_text()))))
for value in tiled_ids:
    call(0x23ec140,value,0)
    assert not fault, value
    cases.append('T;'+str(value)); expected.append(str(read(settings+0x10)))
tiled_cases = len(tiled_ids)

dispatch = []
for mode in ('C','G'):
    for flags in range(8):
        for sinks in range(4):
            for delegate in (0,1):
                dispatch.append((mode,[1,-999,-999,flags&1,(flags>>1)&1,(flags>>2)&1,sinks&1,(sinks>>1)&1,1,1,delegate]))
for goal in range(-6,116):
    dispatch.append(('C',[goal,-999,-999,1,0,1,1,1,1,1,1]))
for seed in range(500):
    dispatch.append(('C',[rng.choice(probes[:247]),rng.choice((-999,1,11,23,105)),rng.choice((-999,6,11,55)),
        *[rng.randrange(2) for _ in range(8)]]))
for mode, values in dispatch:
    trace.clear(); put(item+0x20,values[0]); put(game_statics+8, game if values[8] else 0,8)
    put(game+0x20,ui if values[9] else 0,8)
    put(ui+0x28,story if values[6] else 0,8); put(ui+0x38,board if values[7] else 0,8)
    delegate = 0x10008000 if values[10] else 0
    if mode == 'C': call(0x2b46c80,item,delegate)
    else: call(0x162c0e8,ui,values[0],delegate)
    trace.append('error' if fault else 'ok')
    cases.append(mode+';'+','.join(map(str,values))); expected.append(';'.join(trace))

mono = Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-item-goals-') as directory:
    exe = Path(directory)/'verify.exe'
    names = ('OriginalGoalTypes','OriginalItemGoalServices','OriginalGameUiProgress','OriginalLevelRules','OriginalTiledItemTypes')
    compile_ = subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),
        str(PROJECT/'SourceCatalog/ItemGoalDeliveryHarness.cs'),*[str(runtime/(n+'.cs'))for n in names]],capture_output=True,text=True)
    assert compile_.returncode == 0, compile_.stdout+compile_.stderr
    result = subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8')
    assert result.returncode == 0, result.stdout+result.stderr
    actual = result.stdout.lstrip('\ufeff').splitlines()
    assert len(actual) == len(expected), (len(actual),len(expected), result.stderr)
    for index, (a,e) in enumerate(zip(actual,expected)):
        assert a == e, (index,cases[index],a,e)
report = {'passed':True,'native_mapping_cases':mapping_cases,'native_tiled_settings_cases':tiled_cases,
    'native_dispatch_cases':len(dispatch),'scope':'Actual ARM64 item/match/static goal mapping and collect-particle gate, full GetSettingsForTileId goal selection, CollectAsGoalUi and TryIncrementStoryLevelProgress followed by actual GameUi.IncrementProgress branching. Covers all supported tiled IDs, signed bounds, all story/final/portal branches, missing story/board/current-game/UI, opaque IMultipleBoardProgressDelegate object identity handoff and ordered notification/context reads including goal mutations during UI notification and context resolution. GoalManager notification/storage, level-context flag getters, metadata/class initialization and board/story sinks are supplied boundaries; target presentation methods are not invoked by these dispatch methods. Actual GoalManager composition and formal item goal identities are separately checked in Unity. Animated goal trails/HUD, story/board presentation, production game-context progress binding and full playable resolution remain pending.'}
(PROJECT/'item_goal_delivery_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(json.dumps(report))
