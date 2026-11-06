"""Compare active Load/name selection and three-color predicates to actual ARM64."""
import sys
sys.dont_write_bytecode=True
import base64,json,os,random,subprocess,tempfile,unicodedata
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_move_budget.py').read_text().split('cached=json.loads')[0],str(PROJECT/'SourceCatalog/verify_move_budget.py'),'exec'))
emu.hook_del(native_hook)
base_hook=hook
manager,progress=0x10008000,0x1000a000
three_group=0;group_delta=0;found_version=0;override_version=0
literals=json.loads((PROJECT.parent/'code/metadata/stringliteral.json').read_text())
literal_values={int(s['address'],16):s['value']for s in literals}
literal_slots=(0x5608098,0x5623c20,0x56336a0,0x56236b0,0x5618958,0x5616fe8,0x561c600,0x5614788,0x5615520,0x5621db8,0x561fbe8,0x5621938,0x560ff40,0x5622838)
for i,slot in enumerate(literal_slots):
    value=literal_values[slot];p=0x10180000+i*0x100;raw=value.encode('utf-16-le')
    put(p+0x10,len(raw)//2);emu.mem_write(p+0x14,raw);put(slot,p,8)

def encoded(value):return '~'if value is None else base64.b64encode(value.encode()).decode()
def snapshot_ab():return 'ab,'+','.join(map(str,(signed(read(active+0x14)),signed(read(active+0x20)),read(active+0x10,1),signed(read(active+0x3c)))))

def name_hook(uc,address,size,unused):
    global three_group
    x0,x1,x2=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(3)]
    if address==0x29e27cc and x0==87:returned(uc,manager if flags&256 else 0)
    elif address==0x32c00f0:
        trace.append(snapshot_ab())
        if flags&1024:fail(uc)
        else:
            value=three_group;three_group=signed((three_group+group_delta)&0xffffffff);returned(uc,value)
    elif address==0x283fcdc:
        trace.append('version,'+str(signed(x0&0xffffffff)))
        if flags&2048:fail(uc)
        else:put(x1,override_version);put(x1+4,0);returned(uc,found_version)
    elif address==0x1d77a98:
        units=bytes(emu.mem_read(x0+0x14,read(x0+0x10)*2));index=-1
        for i in range(len(units)//2):
            if int.from_bytes(units[2*i:2*i+2],'little')==x1:index=i
        returned(uc,index)
    elif address==0x1d6cc9c:returned(uc,read(x0+0x14+2*x1,2))
    elif address==0x2044854:returned(uc,unicodedata.category(chr(x0))=='Nd')
    elif address==0x1027570:
        p=allocate(0x18+2*x0);put(p+0x10,x0);returned(uc,p)
    elif address==0x22f9920:emu.mem_write(x0,bytes(emu.mem_read(x1,x2)));returned(uc)
    elif address==0x2653e44:
        value=signed(read(x0));returned(uc,string(('-'if value<0 else '')+format(abs(value),'02d')))
    elif address in(0x1d6812c,0x1d72d80):
        values=(x0,x1)if address==0x1d6812c else(x0,x1,x2)
        returned(uc,string(''.join(string_value(p)or''for p in values)))
    elif address==0x1d72f48:
        returned(uc,string(''.join(string_value(read(x0+0x20+8*i,8))or''for i in range(read(x0+0x18)))))
    elif address==0x2a2da14:returned(uc)
    else:base_hook(uc,address,size,unused)
emu.hook_add(UC_HOOK_CODE,name_hook)
cases=[];expected=[];commands=0
real_config=json.loads((PROJECT/'SourceCatalog/slb_move_config_snapshot.json').read_text())
real=[real_config[k]for k in('c','d','f','g','h')]
synthetic=[['160v-1','0','230v2147483647','0'],['75v0','0'],['111v-2147483648','0'],['111v-2','0'],['211v22','0']]

def run_load(head,profile,steps):
    global flags,next_object,data,stored_text,three_group,group_delta,found_version,override_version,commands,fault,forced_group,duke_mode
    flags,slb_group,three_group,group_delta,forced,user_type,found_version,override_version=head
    forced_group=-1;duke_mode=0;data=profile;next_object=0x10030000;dictionaries.clear()
    emu.mem_write(user,bytes(0xb000));emu.mem_write(user_static,bytes(0x3000))
    put(user_static,user if flags&2 else 0,8);put(user+0x38,session if flags&4 else 0,8);put(session+0x18,active if flags&8 else 0,8)
    put(user+0x40,ab if flags&16 else 0,8);put(user+0x18,progress if flags&512 else 0,8);put(progress+0x18,user_type);put(manager+0x22,forced,1)
    put(booster+0x18,slb_group);stored_text=string('stored')if profile is not None else 0
    if profile is not None:
        for i,values in enumerate(profile):put(config+0x10+8*i,array(values),8)
    output=[]
    for step in steps:
        commands+=1;trace.clear();mode,*args=step;a=args[0]if args else 0;result='-';fault=False
        put(0x1001c000,-777);put(0x1001c008,string('seed'),8)
        if mode=='l':call(0x32b6768,active,a,string(args[1]),*args[2:])
        elif mode=='n':
            result_pointer=call(0x32b6948,active,string(a));result=encoded(string_value(result_pointer))if not fault else '-'
        elif mode=='t':
            result_pointer=call(0x32b6ddc,string(a));result=encoded(string_value(result_pointer))if not fault else '-'
        elif mode=='v':result=str(call(0x221e868,a,0x1001c000,0x1001c008))
        elif mode=='s':result=str(call(0x283d348,*args))
        elif mode=='x':
            result_pointer=call(0x283d444);result=encoded(string_value(result_pointer))if not fault else '-'
        elif mode in('u','h','d','e','j'):result=str(call({'u':0x283cf7c,'h':0x283ec44,'d':0x283eca4,'e':0x283eda4,'j':0x283ee24}[mode],a))
        elif mode=='z':call(0x32b6760,active)
        elif mode=='f':
            for bit,offset in((0,0x10),(1,0x11),(2,0x29),(3,0x39)):put(active+offset,(a>>bit)&1,1)
        elif mode=='b':put(active+0x34,a);put(active+0x2c,args[1])
        elif mode=='g':three_group=a;put(booster+0x18,args[1])
        elif mode=='m':call(0x32b6dd4,active,a)
        if fault:result='error'
        version=signed(read(0x1001c000));extension=encoded(string_value(read(0x1001c008,8)))
        fields=[signed(read(active+o))for o in(0x14,0x20,0x24)]
        tail=[encoded(string_value(read(active+0x18,8))),read(active+0x10,1),read(active+0x11,1),signed(read(active+0x3c)),three_group,';'.join(trace)]
        output.append(':'.join(map(str,(result,version,extension,*fields,*tail))))
    arrays='~'if profile is None else ';'.join('~'if values is None else ','.join(encode(s)for s in values)for values in profile)
    serialized=';'.join(','.join([mode,*[encoded(a)if(mode in('n','t')or(mode=='l'and i==1))else str(a)for i,a in enumerate(args)]])for mode,*args in steps)
    cases.append(','.join(map(str,head))+'#'+arrays+'#'+serialized);expected.append('|'.join(output))

valid_flags=2|4|8|16|32|256|512
names=(None,'','plain','00001v02_L','_L','level_','alpha_beta_v12_L','160v9_L','160v9x_L','160vÙ¡Ù¢_L','160vï¼‘ï¼’_L','v12_L','160v_L')
for three in(-1,0,2,3,4,5,6):
    for slb_group in(0,3,4,6,7,8):
        for force in(0,1):
            steps=[]
            for n,name in enumerate(names):
                steps.extend((('f',n%4),('b',0,n%3),('l',160,name,n-3,n%2,(n>>1)%2),('n',name),('z',)))
            steps.extend(('v',n)for n in(1,74,75,110,111,159,160,210,211,230,231,2147483647,-2147483648))
            run_load([valid_flags,slb_group,three,0,force,0,1,-2],real,steps)
for flag in(0,2,14,30,46,valid_flags&~32,valid_flags&~256,valid_flags&~512,valid_flags|64,valid_flags|128,valid_flags|1024,valid_flags|2048):
    for force in(0,1):
        steps=[('l',160,'old_L',30,0,0),('b',0,0),('f',0),('l',211,'new_L',-10,1,1),('n','next_L'),('f',1),('l',1,'admin_L',17,0,0),('s',160,0),('x',),('v',211)]
        run_load([flag,8,5,0,force,0,1,7],real,steps)
for group in range(8):
    for user_type in(0,1,-1):
        steps=[]
        for n in(-2147483648,-1,0,1,38,39,49,74,109,110,111,112,121,122,201,202,231,232,261,262,301,302,1001,1002,1101,1102,2147483647):
            steps.extend((('d',n),('e',n),('j',n),('s',n,0)))
        for league in range(-2,33):steps.append(('s',211,league))
        steps.extend((('u',),('h',),('x',)))
        run_load([valid_flags,3,group,0,0,user_type,0,777],None,steps)
for group in(3,4,6,7,8):
    for level in range(1,233):
        run_load([valid_flags,group,0,0,0,0,0,0],real,[('l',level,str(level)+'v99_L',30,0,0),('v',level),('m',19)])
for value in names:
    run_load([valid_flags,0,0,0,0,0,0,0],None,[('t',value)])
for group in(3,4,6,7,8):
    run_load([valid_flags,group,0,0,0,0,0,0],synthetic,[('l',n,'prefixv88_L',30,0,0)for n in(75,111,160,211,230)])
rng=random.Random(0x32b6768)
for i in range(100):
    steps=[]
    for j in range(25):
        mode=rng.choice(('l','n','t','v','s','x','u','h','d','e','j','z','f','b','g','m'))
        n=rng.choice((-2147483648,-1,0,1,39,49,111,160,211,230,2147483647))
        if mode=='l':step=(mode,n,rng.choice(names),n,rng.randrange(2),rng.randrange(2))
        elif mode in('n','t'):step=(mode,rng.choice(names))
        elif mode in('s','b','g'):step=(mode,n,rng.randrange(-2,12))
        elif mode=='f':step=(mode,rng.randrange(16))
        else:step=(mode,n)
        steps.append(step)
    run_load([rng.choice((valid_flags,valid_flags,valid_flags|1024,valid_flags|2048)),rng.randrange(10),rng.randrange(8),rng.randrange(2),rng.randrange(2),rng.randrange(2),rng.randrange(2),rng.choice((-2147483648,-2,0,1,7,2147483647))],rng.choice((None,real,synthetic)),steps)
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-active-load-')as directory:
    exe=Path(directory)/'verify.exe'
    files=('OriginalMoveBudgetAdjustment','OriginalSlbMoveConfig','OriginalActiveLevelData','OriginalActiveLevelInitialization','OriginalLevelSessionCounters','OriginalSessionValues','OriginalThreeColorLevelNames')
    compiled=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-langversion:latest','-out:'+str(exe),str(PROJECT/'SourceCatalog/ActiveLevelLoadHarness.cs'),*[str(PROJECT/'Assets/Foundation/Runtime'/(n+'.cs'))for n in files]],capture_output=True,text=True)
    assert compiled.returncode==0,compiled.stdout+compiled.stderr
    completed=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8')
    assert completed.returncode==0,completed.stdout+completed.stderr
    actual=completed.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
    for i,(a,e)in enumerate(zip(actual,expected)):assert a==e,(i,cases[i],a[:3500],e[:3500])
report={'passed':True,'native_sequences':len(cases),'native_commands':commands,'scope':'Actual ARM64 UserActiveLevelData.Load/Reset/GetFixedLevelName/StripVersionSegment, SLB name override/default version using recovered cache and actual three-color ShouldReport/IsEnabled/IsHardExcluded/GetExtension/GetDifficulty/IsExtreme/tutorial-level predicate. Compares Load write order, old admin flag, retained data on exceptions, Unicode decimal version suffixes, exact original literal/table mappings, missing contexts, repeated mutable AB reads and all tested SLB level/group boundaries. CLR strings/formatting/metadata/allocation/dictionary and existing AB group/version-entry data access are supplied boundaries; diagnostic logs omitted. Full native constructor/session Start/Restart/save/event snapshots, three-color config cache, episode/three-color/welcome-back move preprocessing and formal playable context remain pending.'}
(PROJECT/'active_level_load_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report))
