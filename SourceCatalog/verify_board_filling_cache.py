"""Compare highest-filling cache and incoming-item cross-fall gates with ARM64."""
import sys
sys.dont_write_bytecode = True
import json, os, random, subprocess, tempfile
from pathlib import Path
PROJECT = Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_move_budget.py').read_text().split('cases=[];expected=[]')[0], str(PROJECT/'SourceCatalog/verify_move_budget.py'), 'exec'))
emu.hook_del(native_hook)
base_hook = hook
board, manager, grid, dictionary, entry = 0x10020000, 0x10021000, 0x10022000, 0x10023000, 0x10024000
cell_ptrs = [0x10030000 + i * 0x400 for i in range(20)]
index_of = {p:i for i,p in enumerate(cell_ptrs)}
cache = {}; enumeration = []; cursor = 0; data = []
def cache_hook(uc, address, size, unused):
    global enumeration, cursor
    x0,x1,x2 = [uc.reg_read(UC_ARM64_REG_X0+i) for i in range(3)]
    if address == 0x31950b4:
        x,y = signed(x1 & 0xffffffff), signed(x1 >> 32)
        returned(uc, 0 if x < 0 or x >= 4 or y < 0 or y >= 5 or data[y*4+x]&3 == 3 else cell_ptrs[y*4+x])
    elif address == 0x20000010: returned(uc, data[index_of[x0]]&3 in (1,2))
    elif address == 0x3cce930:
        column = signed(x1 & 0xffffffff)
        if column in cache: fail(uc)
        else: cache[column] = index_of[x2]; returned(uc)
    elif address == 0x3cced84:
        enumeration = list(cache.items()); cursor = 0; returned(uc)
    elif address == 0x3173e70:
        if cursor >= len(enumeration): returned(uc,0)
        else:
            key, i = enumeration[cursor]; cursor += 1
            put(x0+0x10,key); put(x0+0x18,cell_ptrs[i],8); returned(uc,1)
    elif address == 0x3173f8c: returned(uc)
    elif address == 0xffde20: fail(uc)
    else: base_hook(uc,address,size,unused)
for a in (0xffd960,0xffd90c,0xffda64,0xffda88,0xffda90,0xffda50,0x343b6e8,0x31950b4,0x20000010,0x3cce930,0x3cced84,0x3173e70,0x3173f8c,0xffde20):
    # CellPoint constructor is executed, not replaced by a hook boundary.
    if a != 0x343b6e8: emu.hook_add(UC_HOOK_CODE,cache_hook,begin=a,end=a)
put(board+0x10,0,8); put(board+0x20,0xffffffff,8); call(0x3207b00,board)
fill_type = read(read(0x53e7838,8),8); put(fill_type+0x12c,1,1)
for kind in (0,1,2):
    klass = 0x10050000 + kind*0x1000
    put(klass+0x238,0x20000010,8); put(klass+0x12c,1,1); put(klass+0xc8,klass+0x800,8); put(klass+0x800,fill_type if kind == 1 else 0,8)
cases=[]; expected=[]
def run(h,values):
    global data,cache
    data=values; cache={}
    put(board+0x10,((h[1]&0xffffffff)<<32)|(h[0]&0xffffffff),8)
    put(board+0x20,(((h[1]+h[3]-1)&0xffffffff)<<32)|((h[0]+h[2]-1)&0xffffffff),8)
    put(board+0x38,dictionary,8); put(board+0x58,manager,8); put(manager+0x40,grid,8)
    for i,p in enumerate(cell_ptrs):
        put(p,0x10050000+(data[i]&3 if data[i]&3 != 3 else 0)*0x1000,8)
        put(p+0xe8,p+0x200,8); put(p+0x220,int(bool(data[i]&4)))
    commands=['c',*[f'x{i}' for i in range(-2,7)],'c','x1']; out=[]
    for command in commands:
        answer=call(0x3207b00,board) if command=='c' else call(0x3207c54,board,int(command[1:]))
        result='error' if fault else 'ok' if command=='c' else str(answer)
        out.append(result+':'+ '/'.join(f'{k},{v}' for k,v in cache.items()))
    cases.append(','.join(map(str,h))+'#'+','.join(map(str,data))+'#'+';'.join(commands)); expected.append('|'.join(out))
for mask in range(256):
    run([0,0,4,5], [1|(((mask>>(i%8))&1)<<2) if i>=16 else 1 if i>=12 and mask&1 else 0 for i in range(20)])
rng=random.Random(0x3207b00)
for _ in range(256):
    run([rng.randrange(-1,4),rng.randrange(-1,4),rng.randrange(0,6),rng.randrange(0,7)], [rng.randrange(8) for _ in range(20)])
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-board-fill-') as folder:
    exe=Path(folder)/'verify.exe'; runtime=PROJECT/'Assets/Foundation/Runtime'
    files=['OriginalBoardFallContext','OriginalBoardLayout','OriginalCellGrid','OriginalGridIterator','OriginalFallGridStrategies','OriginalFillingCell','OriginalCellMediation']
    result=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-langversion:latest','-out:'+str(exe),str(PROJECT/'SourceCatalog/BoardFillingCacheHarness.cs'),*[str(runtime/(f+'.cs')) for f in files]],capture_output=True,text=True,encoding='utf-8-sig')
    assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8-sig'); assert result.returncode==0,result.stderr
    actual=result.stdout.lstrip('\ufeff').splitlines(); assert len(actual)==len(expected)
    for i,(a,e) in enumerate(zip(actual,expected)): assert a==e,(i,cases[i],a,e)
report={'passed':True,'native_sequences':len(cases),'native_commands':len(cases)*12,'scope':'Actual ARM64 Board.CacheFillingCells, Board.CanCrossFall and CellPoint constructor. Highest filling source per column, inclusive region scan, non-cleared duplicate cache and partial mutation on null/invalid cast, incoming-item gates in own and adjacent columns. Grid lookup, virtual IsFillingCell, CLR dictionary/enumerator and type metadata are supplied boundaries. Runtime context delegation, real item/static queries, automatic planner and motion are integration-verified in Unity, not established by this narrow native test.'}
(PROJECT/'board_filling_cache_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
