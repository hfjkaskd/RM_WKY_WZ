"""Differential ThreeColorMoveConfigHelper and move preprocessing against ARM64."""
import sys
sys.dont_write_bytecode = True
import base64,hashlib,json,os,random,subprocess,tempfile
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_move_budget.py').read_text().split('cached=json.loads')[0],str(PROJECT/'SourceCatalog/verify_move_budget.py'),'exec'))
emu.hook_del(native_hook)
base_hook=hook
three_class,three_static,manager=0x10016000,0x10017000,0x10008000
put(0x5595048,three_class,8);put(three_class+0xb8,three_static,8);put(three_class+0xe0,1)
literal_values={int(s['address'],16):s['value']for s in json.loads((PROJECT.parent/'code/metadata/stringliteral.json').read_text())}
for i,a in enumerate((0x561ef28,0x561ef30)):
    raw=literal_values[a].encode('utf-16-le');p=0x10180000+i*0x100;put(p+0x10,len(raw)//2);emu.mem_write(p+0x14,raw);put(a,p,8)
profiles=[];stored=None;version=0;group=0;ignore=0;offset=0;configured=0;league_minimum=0

def profile_pointer(index):
    profile=profiles[index]
    if profile is None:return 0
    p=allocate(0x40);put(p+0x10,string(profile[0]),8)
    for i,values in enumerate(profile[1:]):put(p+0x18+8*i,array(values),8)
    return p

def three_hook(uc,address,size,unused):
    global stored,version
    x0,x1,x2=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(3)]
    if address==0x29e27cc and x0==87:
        trace.append('manager');returned(uc,manager if flags&1 else 0)
    elif address==0x32c00f0:
        trace.append('ab')
        if flags&32:fail(uc)
        else:returned(uc,group)
    elif address==0x25d4b1c:
        trace.append('read')
        if flags&64:fail(uc)
        else:returned(uc,string(stored))
    elif address==0x25d4ccc:
        trace.append('version')
        if flags&64:fail(uc)
        else:returned(uc,version)
    elif address==0x25d4a18:
        value=string_value(x1);trace.append('write,'+value)
        if flags&2048:fail(uc)
        else:stored=value;returned(uc)
    elif address==0x25d4bc4:
        value=signed(x1&0xffffffff);trace.append('writeversion,'+str(value))
        if flags&4096:fail(uc)
        else:version=value;returned(uc)
    elif address==0x25d499c:
        key=string_value(x0);is_config=key=='ThreeColorMoveConfig';assert is_config or key=='ThreeColorMoveConfigVersion',key
        trace.append('delete'if is_config else'deleteversion')
        if flags&(8192 if is_config else 16384):fail(uc)
        else:
            if is_config:stored=None
            else:version=0
            returned(uc)
    elif address==0x260b858:
        value=string_value(x0);trace.append('json,'+value)
        if flags&128:fail(uc)
        else:returned(uc,profile_pointer(int(value)))
    elif address in(0x3f9afc0,0x3f9afdc):dictionaries[x0]={};returned(uc)
    elif address==0x3f9ba3c:dictionaries[x0][signed(x1&0xffffffff)]=(signed(x2&0xffffffff),signed((x2>>32)&0xffffffff));returned(uc)
    elif address==0x3f9d5d8:
        entry=dictionaries[x0].get(signed(x1&0xffffffff));put(x2,0,8)
        if entry is not None:put(x2,entry[0]);put(x2+4,entry[1])
        returned(uc,entry is not None)
    elif address==0x283db2c:
        trace.append('offset')
        if flags&256:fail(uc)
        else:returned(uc,offset)
    elif address==0x283c7e8:
        trace.append('configured,'+str(signed(x0&0xffffffff)))
        if flags&512:fail(uc)
        else:returned(uc,configured)
    elif address==0x283cb88:
        trace.append('minimum,'+str(signed(x0&0xffffffff)))
        if flags&1024:fail(uc)
        else:returned(uc,league_minimum)
    elif address==0x283fb6c:uc.reg_write(UC_ARM64_REG_PC,0x283fbe8) # Omit diagnostic formatting after the two actual DeleteKey calls.
    elif address==0x283fbec:uc.reg_write(UC_ARM64_REG_PC,0x283fbf0)
    elif address==0x2a3257c:returned(uc)
    else:base_hook(uc,address,size,unused)
emu.hook_add(UC_HOOK_CODE,three_hook)
cases=[];expected=[];commands=0

def run(head,values,steps):
    global flags,group,ignore,offset,configured,league_minimum,stored,version,profiles,next_object,commands
    flags,group,ignore,league,offset,configured,league_minimum,initial,version=head
    profiles=values;stored=None if initial<0 else str(initial);next_object=0x10030000;dictionaries.clear()
    emu.mem_write(user,bytes(0x9000));emu.mem_write(user_static,bytes(0x3000));emu.mem_write(three_static,bytes(0x30))
    def contexts():
        put(user_static,user if flags&2 else 0,8);put(user+0x40,ab if flags&4 else 0,8);put(user+0x38,session if flags&8 else 0,8);put(session+0x18,active if flags&16 else 0,8);put(active+0x2c,league);put(manager+0x23,ignore,1)
    contexts();output=[]
    for mode,*args in steps:
        commands+=1;trace.clear();result='-';a=args[0]if args else 0
        if mode in('e','v'):
            answer=call(0x283fcdc if mode=='e'else 0x283fe14,a,0x1001c000)
            result=','.join(map(str,(answer,signed(read(0x1001c000)),signed(read(0x1001c004)))))if mode=='e'else','.join(map(str,(answer,signed(read(0x1001c000)))))
        elif mode in('a','m','x','c','h','d'):
            answer=call({'a':0x283daa0,'m':0x283dac4,'x':0x283dc48,'c':0x283ef70,'h':0x283efbc,'d':0x283fac4}[mode],a)
            if mode not in('h','d'):result=str(answer)
        elif mode=='k':
            p=call(0x283fe4c,a);result=dict_text(p)if not fault else '-'
        elif mode=='u':call(0x283f07c,a,string(None if args[1]<0 else str(args[1])))
        elif mode=='b':stored=None if a<0 else str(a)
        elif mode=='g':group=a
        elif mode=='i':ignore=a;contexts()
        elif mode=='q':flags=a;contexts()
        elif mode in('r','p','f','s'):result=str(call({'r':0x283d9cc,'p':0x283da1c,'f':0x283d900,'s':0x283dcc0}[mode],*args))
        elif mode=='t':
            answer=call(0x283fc04,string(a),0x1001c000,0x1001c004);result=','.join(map(str,(answer,signed(read(0x1001c000)),signed(read(0x1001c004)))))
        elif mode=='n':result=str(call(0x283f968,string(a)))
        elif mode=='j':result=str(call(0x283f62c,profile_pointer(a)))
        elif mode=='l':call(0x283f518,profile_pointer(a))
        if mode not in('b','g','i','q')and fault:result='error'
        output.append(result+':'+('~'if stored is None else stored)+':'+str(version)+':'+str(group)+':'+str(ignore)+':'+','.join(trace))
    serialized=[]
    for profile in values:
        serialized.append('~'if profile is None else ';'.join([encode(profile[0]),*['~'if v is None else ','.join(encode(s)for s in v)for v in profile[1:]]]))
    steps_text=';'.join(','.join([mode,*[encode(a)if mode in('t','n')else str(a)for a in args]])for mode,*args in steps)
    cases.append(','.join(map(str,head))+'#'+'!'.join(serialized)+'#'+steps_text);expected.append('|'.join(output))

real=json.loads((PROJECT/'SourceCatalog/three_color_move_config_snapshot.json').read_text())
values=[None,[None,None,None,None,None],[None,real['b'],None,None,None],['17',['1v2','-9','160v-1','3','1v7','-12'],['1v3','2'],['1v4','-2147483648'],['1v5','2147483647']],['8',['odd'],[],['broken','2','1v2','bad','1v-2','-2'],None],['-3',[],None,None,None],[' 20 ',['1v0','-2'],['-1v+3',' 7 '],None,['2147483647v-2147483648','-1']]]
valid=31
for initial in range(-1,len(values)):
    for group_value in(-1,0,2,3,4,5,6):
        steps=[('c',),('k',group_value),('e',1),('v',1),('a',1),('m',10),('x',)]
        steps += [('p',1,n)for n in(-2147483648,-1,0,9,10,16,17,18,30,2147483647)]
        steps += [('i',1),('e',1),('m',9),('i',0),('b',6),('e',1),('c',),('u',2,6),('e',1),('m',10),('u',2,3),('e',1),('d',),('e',1),('c',)]
        run([valid,group_value,0,0,0,0,10,initial,1],values,steps)
for flags_value in(0,1,3,7,15,30,27,29,23,valid|32,valid|64,valid|128,valid|256,valid|512,valid|1024,valid|2048,valid|4096,valid|8192,valid|16384):
    for league in(0,1):
        for ignore_value in(0,1):
            steps=[('e',1),('m',10),('c',),('u',5,3),('e',1),('f',1,30,0,0,0),('f',1,30,1,0,0),('d',),('q',valid),('e',1),('m',10),('c',)]
            run([flags_value,2,ignore_value,league,2,-3,13,1,0],values,steps)
for group_value in range(8):
    for league in(0,1):
        steps=[]
        for n in(-2147483648,-31,-1,0,1,9,10,11,19,20,21,29,30,31,41,2147483647):
            for randomize,negative,bonus in((0,0,0),(1,0,0),(0,1,0),(0,0,1)):
                steps.extend((('r',n,randomize,negative,bonus),('f',1,n,randomize,negative,bonus)))
        run([valid,group_value,0,league,3,-5,12,3,0],values,steps)
tokens=(None,'','1','v1','1v','1V2','1v2','001v02','1v-2','-1v+3',' 1 v 2 ','1v2v3','2147483648v1','1v2147483648','2147483647v-2147483648','1vbad','bad v1')
run([valid,2,0,0,0,0,10,-1,0],values,[('t',t)for t in tokens]+[('n',t)for t in(None,'','0','-1','1',' +17 ','2147483647','2147483648','bad')]+[('j',i)for i in range(len(values))]+[('s',a,b)for a in(-2147483648,-1,0,1,31,2147483647)for b in(-2147483648,-1,0,1,5,6,7,10,2147483647)])
rng=random.Random(0x283d900)
run([valid,2,0,0,0,0,10,-1,0],values,[('s',rng.randint(-2147483648,2147483647),rng.randint(-2147483648,2147483647))for _ in range(300)])
for i in range(90):
    steps=[]
    for j in range(30):
        mode=rng.choice(('e','a','m','k','c','u','g','i','r','p','f','l','h','d','b'))
        n=rng.choice((-2147483648,-1,0,1,10,17,30,160,2147483647))
        if mode=='u':step=(mode,n,rng.randrange(-1,len(values)))
        elif mode=='g':step=(mode,rng.randrange(8))
        elif mode=='i':step=(mode,rng.randrange(2))
        elif mode in('l','b'):step=(mode,rng.randrange(len(values)))
        elif mode=='r':step=(mode,n,rng.randrange(2),rng.randrange(2),rng.randrange(2))
        elif mode=='f':step=(mode,1,n,rng.randrange(2),rng.randrange(2),rng.randrange(2))
        elif mode=='p':step=(mode,1,n)
        elif mode in('c','h','d'):step=(mode,)
        else:step=(mode,n)
        steps.append(step)
    run([valid,rng.randrange(8),rng.randrange(2),rng.randrange(2),rng.randrange(-5,6),rng.randrange(-5,6),rng.randrange(5,20),rng.randrange(-1,len(values)),rng.randrange(3)],values,steps)
    if i%30==0:print('native sequence progress',len(cases),flush=True)
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-three-moves-')as directory:
    exe=Path(directory)/'verify.exe';files=('OriginalThreeColorMoveConfig','OriginalThreeColorMoves','OriginalThreeColorLevelNames','OriginalActiveLevelData','OriginalActiveLevelInitialization','OriginalLevelSessionCounters','OriginalSessionValues','OriginalSlbMoveConfig','OriginalMoveBudgetAdjustment')
    compiled=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-langversion:latest','-out:'+str(exe),str(PROJECT/'SourceCatalog/ThreeColorMovesHarness.cs'),*[str(PROJECT/'Assets/Foundation/Runtime'/(n+'.cs'))for n in files]],capture_output=True,text=True)
    assert compiled.returncode==0,compiled.stdout+compiled.stderr
    completed=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert completed.returncode==0,completed.stdout+completed.stderr
    actual=completed.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
    for i,(a,e)in enumerate(zip(actual,expected)):
        if a!=e:
            rows_a,rows_e=a.split('|'),e.split('|')
            mismatch=next(j for j,(x,y)in enumerate(zip(rows_a,rows_e))if x!=y)
            raise AssertionError((i,mismatch,cases[i].split('#')[2].split(';')[mismatch],rows_a[mismatch],rows_e[mismatch]))
report={'passed':True,'native_sequences':len(cases),'native_commands':commands,'scope':'Actual ARM64 ThreeColorMoveConfigHelper cache build/load, invalidation, version read/update and write order, entry/version/move/minimum lookup, ignore flag ordering, token/minimum parser, duplicate and malformed pairs; actual ThreeColorLevelsManager multiplier table, integer-overflow/float-ceiling scale, reduction/config adjustment/league final clamp. Includes recovered October 8 b-empty snapshot, unknown groups, cache retention after invalidation/external storage changes, missing contexts, injected boundary failures and randomized sequences. CLR strings/integer parsing/dictionary/metadata/allocation, JsonUtility transport and AppKeyValue transport are supplied boundaries; diagnostic logs omitted. LeagueManager current offset and separate ThreeColorLeagueLevelsConfigHelper offset/minimum are required supplied boundaries. Formal application storage/AB assignment, level selection, constructor/session Start/Restart, episode/welcome-back/save startup and full playable resolution remain pending.'}
report['snapshot_source_sha256']=hashlib.sha256((PROJECT.parent/'configs/live_20261008/server_cached/app_ThreeColorMoveConfig.json').read_bytes()).hexdigest()
report['original_multiplier_table']='ELF 0x4044360: group 2=5, 3=6, 4=7, 5=6'
(PROJECT/'three_color_moves_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report))
