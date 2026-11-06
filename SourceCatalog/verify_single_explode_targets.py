"""Original ARM64 target selection, rule lookup and incoming reservations vs runtime C#."""
from pathlib import Path
import hashlib, json, random, sqlite3, struct, subprocess, sys, tempfile
sys.path.insert(0, str(Path(__file__).resolve().parents[2] / 'tools/python'))
from elftools.elf.elffile import ELFFile
from elftools.elf.relocation import RelocationSection
from unicorn import Uc, UC_ARCH_ARM64, UC_MODE_ARM, UC_HOOK_CODE
from unicorn.arm64_const import UC_ARM64_REG_X0, UC_ARM64_REG_X1, UC_ARM64_REG_X2, UC_ARM64_REG_X8, UC_ARM64_REG_SP, UC_ARM64_REG_LR, UC_ARM64_REG_PC

PROJECT=Path(__file__).resolve().parents[1]; RECOVERY=PROJECT.parent
lib=RECOVERY.parent/'RoyalMatch_20260929/apk_unpacked/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so'
emu=Uc(UC_ARCH_ARM64,UC_MODE_ARM)
emu.mem_map(0,0x6000000);emu.mem_map(0x10000000,0x200000);emu.mem_map(0x20000000,0x1000)
def put(a,v,size=4):emu.mem_write(a,(v&((1<<(size*8))-1)).to_bytes(size,'little'))
def read(a,size=4):return int.from_bytes(emu.mem_read(a,size),'little')
def signed(v):v&=0xffffffff;return v-0x100000000 if v>=0x80000000 else v
with lib.open('rb') as stream:
    elf=ELFFile(stream)
    for segment in elf.iter_segments():
        if segment['p_type']=='PT_LOAD':emu.mem_write(segment['p_vaddr'],segment.data())
    for section in elf.iter_sections():
        if isinstance(section,RelocationSection):
            for relocation in section.iter_relocations():
                if relocation['r_info_type']==1027:put(relocation['r_offset'],relocation['r_addend'],8)

owner,data,previous,model,model_class=0x10000000,0x10001000,0x10002000,0x10003000,0x10004000
incoming,extra,incoming_set,extra_set=0x10005000,0x10006000,0x10007000,0x10008000
cell_base,list_base,meta,ctx,klass,statics=0x10010000,0x10030000,0x10080000,0x10081000,0x10082000,0x10083000
put(meta+0x38,ctx,8);put(ctx,klass,8);put(klass+0x132,1,1);put(klass+0xe0,1);put(klass+0xb8,statics,8)
put(meta+0xe0,1);put(meta+0xb8,statics,8)
put(model,model_class,8);put(model_class+0x378,0x20000100,8);put(model_class+0x368,0x20000110,8)
put(model+0x78,incoming,8);put(model+0x80,extra,8);put(incoming+0x10,incoming_set,8);put(extra+0x10,extra_set,8)
put(model+0x68,3)
lists={};cells={};bucket_keys=[];enum_index=-1;random_values=[];random_used=0;trace=[];sets={};item_type=0
goal_left=0
put(model+0x48,0x100a0000,8)

def boundary(uc,address,size,unused):
    global enum_index,random_used
    x0,x1,x2=[uc.reg_read(r) for r in (UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2)]
    result=None
    if address==0xffd960:put(x0,meta,8);result=0
    elif address in (0xffd90c,0xffda64,0x31743a0,0x2a2da14):result=0
    elif address in (0xffda88,0xffda90):raise AssertionError('Native error at '+hex(uc.reg_read(UC_ARM64_REG_LR)))
    elif address==0x1275108:
        enum_index=-1;uc.mem_write(uc.reg_read(UC_ARM64_REG_X8),bytes(48));result=0
    elif address==0x3174330:enum_index+=1;result=int(enum_index<len(bucket_keys))
    elif address==0x31743a4:
        key=bucket_keys[enum_index];uc.reg_write(UC_ARM64_REG_X1,list_base+enum_index*0x100);result=key&0xffffffff
    elif address==0x1ed9438:
        count=signed(x2)-signed(x1);result=random_values[random_used]%count+signed(x1);random_used+=1;trace.append('N'+str(signed(x2))+':'+str(result))
    elif address==0x2c1b9a0:result=lists[x0][signed(x1)]
    elif address==0x2c1d3c0:lists[x0].pop(signed(x1));put(x0+0x18,len(lists[x0]));result=0
    elif address==0x343c39c:trace.append('R'+str(cells[x0][0]));result=cells[x0][4]
    elif address==0x343caa4:
        value=cells[x0];trace.append('D'+str(value[0])+':'+str(len(lists[x2])));result=0 if value[5]<0 else cell_base+value[5]*0x100
    elif address==0x3bf937c:put(x0,x1,1);put(x0+8,x2,8);result=0
    elif address in (0x220d588,0x220cc0c):
        values=sets.setdefault(x0,set());value=signed(x1)
        if address==0x220d588:result=int(value not in values);values.add(value)
        else:result=int(value in values);values.discard(value)
        put(x0+0x20,len(values))
    elif address==0x20000100:result=item_type
    elif address==0x20000110:result=3
    elif address==0x2b46d88:result=goal_left
    elif address==0x2b45df4:result=cell_base
    elif address==0x29e27cc:result=0x100a0000
    elif address in (0x32072dc,0x1e19780,0x1e19848):result=0 # No inactive item helpers are substituted for active ones.
    if result is not None:
        uc.reg_write(UC_ARM64_REG_X0,result&0xffffffffffffffff);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
emu.hook_add(UC_HOOK_CODE,boundary)
def call(rva,*args):
    for reg,v in zip((UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2),args):emu.reg_write(reg,v&0xffffffffffffffff)
    emu.reg_write(UC_ARM64_REG_SP,0x101ff000);emu.reg_write(UC_ARM64_REG_LR,0x20000000)
    try:emu.emu_start(rva,0x20000000,count=100000)
    except Exception as error:raise AssertionError((hex(rva),hex(emu.reg_read(UC_ARM64_REG_PC)),hex(emu.reg_read(UC_ARM64_REG_LR)))) from error
    assert emu.reg_read(UC_ARM64_REG_PC)==0x20000000,hex(emu.reg_read(UC_ARM64_REG_PC))
    return signed(emu.reg_read(UC_ARM64_REG_X0))

rng=random.Random(0x1c97808);cases=[];expected=[]
for n in range(192):
    point=(rng.randrange(5),rng.randrange(5));has_previous=n%3==0
    prekeys=[rng.choice([0,456976,182788,-4569,-1])] if n%2 else []
    values=[]
    for i in range(n%25):
        values.append([i,rng.randrange(5),rng.randrange(5),rng.choice([0,456976,182788,-4569,-1,2147483647,-2147483648]),rng.randrange(-2,4),i if i%7 else -1])
    if values and n%4==0:values[0][1:3]=point
    random_values=[rng.randrange(0x7fffffff) for _ in range(200)];random_used=0;trace=[]
    bucket_keys=sorted(set(signed(-v) for v in prekeys)|set(signed(-v[3]) for v in values))
    lists={list_base+i*0x100:[] for i in range(len(bucket_keys))};cells={}
    for value in values:
        pointer=cell_base+value[0]*0x100;cells[pointer]=value;put(pointer+0x18,value[1]);put(pointer+0x1c,value[2]);put(pointer+0xb0,pointer,8)
        lists[list_base+bucket_keys.index(signed(-value[3]))*0x100].append(pointer)
    for pointer,lst in lists.items():put(pointer+0x18,len(lst))
    put(owner+0x70,0x10090000,8);put(owner+0x38,0x10091000,8);put(data,point[0]);put(data+4,point[1]);put(data+8,8)
    answers=[]
    for query in range(3):
        keep=call(0x1c97808,owner,data,previous if has_previous else 0);target=emu.reg_read(UC_ARM64_REG_X1)
        answers.append(str(keep)+','+str(-1 if target==0 else (target-cell_base)//0x100))
    cases.append('|'.join(['S',','.join(map(str,(*point,int(has_previous),3))),','.join(map(str,prekeys)),';'.join(','.join(map(str,v)) for v in values),','.join(map(str,random_values))]))
    expected.append(';'.join(answers)+'|'+','.join(trace))

rule_count=0
for item_type in list(range(-3,119))+[-2147483648,2147483647]+[rng.randint(-2147483648,2147483647) for _ in range(64)]:
    trigger=rng.choice(list(range(-1,39))+[-2147483648,2147483647]);guided=rng.randrange(-3,12)
    score=call(0x2b45368,item_type);typ=call(0x318d028,trigger);single=call(0x2b4432c,guided);obstacle=call(0x2b47a38,model)
    cases.append('R|'+','.join(map(str,(item_type,trigger,guided))));expected.append(','.join(map(str,(score,typ,single,obstacle))));rule_count+=1

container_steps=0
for n in range(64):
    sets={};emu.mem_write(incoming_set,bytes(0x100));emu.mem_write(extra_set,bytes(0x100));emu.mem_write(extra+0x18,bytes(12))
    operations=[];answers=[]
    for j in range(48):
        operation=rng.randrange(2);identity=rng.randrange(-3,12);trigger=rng.choice([8,10,11,13,30,35])
        operations.append((operation,identity,trigger));put(data+0xc,identity);put(data+8,trigger)
        call(0x2b466e4 if operation==0 else 0x2b46728,model,data)
        answers.append(','.join(map(str,(call(0x318ce80,incoming),call(0x2b46578,model),call(0x318cdc0,extra,1),call(0x318cdc0,extra,2),call(0x318cdc0,extra,3)))))
    cases.append('C|'+';'.join(','.join(map(str,v)) for v in operations));expected.append(';'.join(answers));container_steps+=len(operations)

score_cases=0
for kind,item_type in [(0,1),(1,2),(1,3),(1,4),(1,5),(2,6)]:
    for match_type in (range(1,7) if kind==0 else [0]):
        for goal_left in [-1,0,1,2,3,10]:
            for selected in range(2):
                for prepared in range(2):
                    for has_view in range(2):
                        put(model+0x18,match_type);put(model+0x279,selected,1);put(model+0xb2,prepared,1)
                        score=call(0x21a5670 if kind==0 else 0x27b4378 if kind==1 else 0x2b4629c,model,0)
                        cases.append('T|'+','.join(map(str,(kind,item_type,match_type,goal_left,selected,prepared,has_view))));expected.append(str(score));score_cases+=1

mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
with tempfile.TemporaryDirectory() as folder:
    exe=Path(folder)/'Verify.exe'
    sources=['SourceCatalog/SingleExplodeTargetHarness.cs',*[f'Assets/Foundation/Runtime/{name}.cs' for name in ('OriginalIncomingExploders','OriginalItemExplodeScores','OriginalExplodeTargetRules','OriginalSingleExplodeTargetFinder','OriginalFirstBoardExplodeTargets')]]
    command=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',*["-r:"+str(mono/'lib/mono/4.5'/name) for name in ('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),*[str(PROJECT/name) for name in sources]]
    r=subprocess.run(command,capture_output=True,text=True,encoding='utf8');assert r.returncode==0,r.stdout+r.stderr
    r=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf8');assert r.returncode==0,r.stdout+r.stderr
    actual=r.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
    for i,(a,e) in enumerate(zip(actual,expected)):assert a==e,(i,cases[i],a,e)
db=sqlite3.connect(RECOVERY/'code/native/functions.sqlite3');methods=[]
for rva in (0x1c97808,0x2b45368,0x318d028,0x2b4432c,0x2b47a38,0x2b466e4,0x2b46728,0x2b46578,0x318cc60,0x318cd10,0x318cdc0,0x21a5670,0x21a428c,0x21a4698,0x27b4378,0x2b4629c):
    name,body=db.execute('select name,body from functions where address=?',(rva,)).fetchone();methods.append({'rva':hex(rva),'name':name,'sha256':hashlib.sha256(body.encode()).hexdigest()})
report={'passed':True,'selector_cases':192,'selector_queries':576,'rule_cases':rule_count,'reservation_sequences':64,'reservation_operations':container_steps,'first_board_score_cases':score_cases,'methods':methods,'scope':'Actual ARM64 FindSingleTargetCellForExploder vs compiled production C#, including repeated queries, stale empty zero buckets retaining targets, self-point avoidance, negative/overflow scores, exhausted/multi-hit targets and redirect boundary. Framework sorted-dictionary/list/tuple operations, cell remaining/redirect services and deterministic regular RNG samples are supplied boundaries. Actual native rule lookup and incoming container/model bodies are executed; duplicate/mismatched-trigger removal behavior is preserved. Native match/special/box score bodies and match calculator/ShouldCalculateScore execute with the admitted first-board inactive helper context; original goal-count and context/service lookups are supplied boundaries. Does not verify active static/item scoring helpers, combo/win selectors or propeller flight/activation.'}
(PROJECT/'single_explode_targets_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
