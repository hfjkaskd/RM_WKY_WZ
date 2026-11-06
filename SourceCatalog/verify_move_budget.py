"""Differential native ARM64 SLB cache/lookup and fresh move budget verification."""
import sys
sys.dont_write_bytecode = True
import base64, json, os, random, re, subprocess, tempfile
from pathlib import Path
PROJECT = Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_useful_move.py').read_text().split('active, king,')[0], str(PROJECT/'SourceCatalog/verify_useful_move.py'), 'exec'))
user, session, active, duke, ab, booster, config, parser = [0x10000000+i*0x1000 for i in range(8)]
user_class, slb_class, move_class = 0x10010000, 0x10011000, 0x10012000
user_static, slb_static, move_static = 0x10013000, 0x10014000, 0x10015000
for slot, klass, static in ((0x5595c00,user_class,user_static),(0x5593f58,slb_class,slb_static),(0x5590918,move_class,move_static)):
    put(slot,klass,8);put(klass+0xe0,1);put(klass+0x132,1,1);put(klass+0xb8,static,8)
emu.reg_write(UC_ARM64_REG_TPIDR_EL0,0x1001f000)
metadata_next=0x10090000; next_object=0; flags=0; forced_group=-1; duke_mode=0; data=None; fault=False; trace=[]; dictionaries={}

def allocate(size):
    global next_object
    value=next_object;next_object=(next_object+size+15)&~15
    assert next_object < 0x10080000
    emu.mem_write(value,bytes(size));return value

def string(value):
    if value is None:return 0
    raw=value.encode('utf-16-le');p=allocate(0x18+len(raw));put(p+0x10,len(raw)//2);emu.mem_write(p+0x14,raw);return p

def string_value(pointer):
    if not pointer:return None
    return bytes(emu.mem_read(pointer+0x14,read(pointer+0x10)*2)).decode('utf-16-le')

def array(values):
    if values is None:return 0
    p=allocate(0x20+8*len(values));put(p+0x18,len(values))
    for i,value in enumerate(values):put(p+0x20+8*i,string(value),8)
    return p

def try_int(value):
    if value is None:return False,0
    # CLR integer parser is a supplied runtime boundary, not SLB behavior.
    value=value.strip(' \t\r\n\v\f').rstrip('\0')
    if not re.fullmatch(r'[+-]?[0-9]+',value):return False,0
    number=int(value);return (-2147483648 <= number <= 2147483647, number if -2147483648 <= number <= 2147483647 else 0)

def returned(uc,value=0):
    uc.reg_write(UC_ARM64_REG_X0,value&0xffffffffffffffff);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))

def fail(uc):
    global fault
    fault=True;uc.reg_write(UC_ARM64_REG_PC,0x20000000)

def hook(uc,address,size,unused):
    global metadata_next
    x0,x1,x2,x3,x4=[uc.reg_read(UC_ARM64_REG_X0+i) for i in range(5)]
    if address==0xffd960:
        if not 0x10000000 <= read(x0,8) < 0x10200000:
            p=metadata_next;metadata_next+=0x400
            put(p+0xe0,1);put(p+0x132,1,1);put(p+0x38,p+0x200,8);put(p+0x200,p,8);put(p+0xb8,p+0x300,8);put(x0,p,8)
        returned(uc)
    elif address in (0xffd90c,0xffda64,0x2a2b5fc):returned(uc)
    elif address==0xffda78:returned(uc,allocate(0x100))
    elif address==0xffd9d0:
        p=allocate(0x20+8*x1);put(p+0x18,x1);returned(uc,p)
    elif address==0xffda68:returned(uc,x0)
    elif address==0xffda6c:
        p=allocate(0x20);put(p+0x10,read(x1));returned(uc,p)
    elif address==0x261458c:returned(uc,0x1001e000)
    elif address in (0xffda88,0xffda90,0xffda50):fail(uc)
    elif address==0x29e27cc:
        if x0==50:trace.append('getduke');returned(uc,duke if flags&1 else 0)
        elif x0==29:returned(uc,booster if flags&32 else 0)
        else:raise AssertionError(('UserContext',x0))
    elif address==0x22301a8 and duke_mode!=0:trace.append('duke');returned(uc,duke_mode==1)
    elif address==0x32be678:trace.append('oneab') # Execute the original constant-false AB body too.
    elif address==0x222d464:
        trace.append('abvalues')
        if flags&64:fail(uc)
        else:
            if forced_group>=0:put(booster+0x18,forced_group)
            returned(uc)
    elif address==0x25d4b1c:
        trace.append('storage')
        if flags&128:fail(uc)
        else:returned(uc,stored_text)
    elif address==0x260b858:returned(uc,config if data is not None else 0)
    elif address in(0x3cdeaa8,0x3cdeac4):dictionaries[x0]={};returned(uc)
    elif address==0x3cdf524:dictionaries[x0][signed(x1&0xffffffff)]=(signed(x2&0xffffffff),signed((x2>>32)&0xffffffff));returned(uc)
    elif address==0x3ce10c0:
        entry=dictionaries[x0].get(signed(x1&0xffffffff));put(x2,0,8)
        if entry is not None:put(x2,entry[0]);put(x2+4,entry[1])
        returned(uc,entry is not None)
    elif address==0x1d72154:returned(uc,not x0 or read(x0+0x10)==0)
    elif address==0x1d771a8:returned(uc,string_value(x0).find(chr(x1)))
    elif address==0x1d74444:returned(uc,string(string_value(x0)[x1:x1+x2]))
    elif address==0x1d76954:returned(uc,string(string_value(x0)[x1:]))
    elif address==0x26544c4:
        success,value=try_int(string_value(x0));put(x1,value);returned(uc,success)
    elif address==0x290ccd8:returned(uc,0x1001d000)
    elif address==0x265fbf0:
        value=bytes(emu.mem_read(x0,x1*2)).decode('utf-16-le');success,number=try_int(value);put(x4,number);returned(uc,success)
native_hook = emu.hook_add(UC_HOOK_CODE,hook)

def call(rva,*args):
    global fault
    fault=False
    for i,arg in enumerate((*args,0,0,0)):emu.reg_write(UC_ARM64_REG_X0+i,arg&0xffffffffffffffff)
    emu.reg_write(UC_ARM64_REG_SP,0x101ff000);emu.reg_write(UC_ARM64_REG_LR,0x20000000)
    try:emu.emu_start(rva,0x20000000,count=500000)
    except Exception:
        print('Native fault',hex(emu.reg_read(UC_ARM64_REG_PC)),trace);raise
    assert emu.reg_read(UC_ARM64_REG_PC)==0x20000000
    return signed(emu.reg_read(UC_ARM64_REG_X0)&0xffffffff)

def encode(s):return '~' if s is None else base64.b64encode(s.encode()).decode()
def dict_text(pointer):
    if not pointer:return 'null'
    return '/'.join(','.join(map(str,(k,*v)))for k,v in sorted(dictionaries[pointer].items()))
cases=[];expected=[];commands=0

def run(head,profile,steps):
    global flags,forced_group,duke_mode,data,next_object,fault,commands,stored_text
    flags,group,forced_group,duke_mode=head;data=profile;next_object=0x10030000
    dictionaries.clear();emu.mem_write(user,bytes(0x8000));emu.mem_write(user_static,bytes(0x3000))
    put(user_static,user if flags&2 else 0,8);put(user+0x38,session if flags&4 else 0,8);put(session+0x18,active if flags&8 else 0,8)
    put(user+0x40,ab if flags&16 else 0,8);put(booster+0x18,group)
    stored_text=string('stored')if profile is not None else 0
    if profile is not None:
        for i,values in enumerate(profile):put(config+0x10+8*i,array(values),8)
    output=[]
    for step in steps:
        commands+=1;trace.clear();mode,*args=step;a=args[0]if args else 0;result='-';fault=False
        if mode=='a':result=str(call(0x1eda6d8,a))
        elif mode=='q':result=str(call(0x221e5f8,a))
        elif mode=='e':
            answer=call(0x221e66c,a,0x1001c000);result=','.join(map(str,(answer,signed(read(0x1001c000)),signed(read(0x1001c004)))))
        elif mode=='k':
            pointer=call(0x221eaf8,a);result=dict_text(pointer)if not fault else '-'
        elif mode=='g':put(booster+0x18,a)
        elif mode=='n':put(active+0x14,a)
        elif mode=='m':call(0x32b6dd4,active,a)
        elif mode in('p','l','z'):result=str(call({'p':0x1c20058,'l':0x1c200b4,'z':0x1c26790}[mode],parser))
        elif mode=='r':
            old=duke_mode;duke_mode=0;result=str(call(0x22301a8,duke));duke_mode=old
        elif mode=='t':put(move_static,a)
        elif mode=='v':
            answer=call(0x221e2d0,string(a),0x1001c000,0x1001c004);result=','.join(map(str,(answer,signed(read(0x1001c000)),signed(read(0x1001c004)))))
        if fault:result='error'
        output.append(result+':'+':'.join(map(str,(signed(read(move_static)),signed(read(booster+0x18)),signed(read(active+0x14)),signed(read(active+0x20)))))+':'+','.join(trace))
    arrays='~' if profile is None else ';'.join('~'if values is None else ','.join(encode(s)for s in values)for values in profile)
    serialized=';'.join(','.join([mode,*[encode(a)if mode=='v'else str(a)for a in args]])for mode,*args in steps)
    cases.append(','.join(map(str,head))+'#'+arrays+'#'+serialized);expected.append('|'.join(output))

cached=json.loads((PROJECT.parent/'configs/live_20261008/server_cached/app_SLBMoveConfig.json').read_text())['value']
real=[cached[k]for k in('c','d','f','g','h')]
synthetic=[['160v2','4','160v03','12','230v1','2147483647','broken','9'],['75v01','-10','230v-2','-2147483648'],['111v2','7','231v99','6'],['111v2','2','230v3','3'],['211v1','-7','230v2','-2147483648']]
malformed=[['odd'],None,[],['bad','1','111vno','2','111v3','bad','111v4','5'],['211v+02',' 8 ','230v3','-1']]
edges=(-2147483648,-1,0,1,19,20,21,30,2147483647)
for flags_value in(0,1,2,7,15,31,47,63,63|64,63|128):
    for group in(-1,0,3,4,5,6,7,8,9):
        for mode in(0,1,2):
            steps=[('t',77),('r',),('m',-13),('p',),('l',),('z',),('n',160)]
            steps += [('a',a)for a in edges]
            steps += [('e',n)for n in(-2147483648,74,75,110,111,159,160,210,211,230,231,2147483647)]
            steps += [('k',group),('q',160),('g',8),('n',230),('a',30),('k',8)]
            run([flags_value,group,-1,mode],synthetic,steps)
for profile in(None,real,synthetic,malformed):
    for group in range(11):
        steps=[]
        for n in range(1,233):steps.extend((('n',n),('a',30),('e',n)))
        steps.extend(('k',g)for g in(-1,3,4,6,7,8,9))
        run([63,0,group,0],profile,steps)
tokens=(None,'','1','v1','1v','1V2','1v2','001v02','1v-2','-1v+3',' 1 v 2 ','1v2v3','2147483648v1','1v2147483648','2147483647v-2147483648','1vbad','bad v1')
run([63,3,-1,0],None,[('v',token)for token in tokens])
rng=random.Random(0x1eda6d8)
for i in range(120):
    steps=[]
    for j in range(40):
        mode=rng.choice(('a','q','e','k','g','n','m','p','l','z','t'))
        value=rng.choice(edges)if mode in('a','m','t')else rng.randrange(-2,242)
        steps.append((mode,value))
    run([rng.choice((63,63,63|64,63|128,47)),rng.randrange(11),-1,rng.randrange(3)],rng.choice((None,real,synthetic,malformed)),steps)
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-move-budget-')as directory:
    exe=Path(directory)/'verify.exe'
    compiled=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-langversion:latest','-out:'+str(exe),str(PROJECT/'SourceCatalog/MoveBudgetHarness.cs'),*[str(PROJECT/'Assets/Foundation/Runtime'/(n+'.cs'))for n in('OriginalSlbMoveConfig','OriginalMoveBudgetAdjustment')]],capture_output=True,text=True)
    assert compiled.returncode==0,compiled.stdout+compiled.stderr
    completed=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8')
    assert completed.returncode==0,completed.stdout+completed.stderr
    actual=completed.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
    for i,(a,e)in enumerate(zip(actual,expected)):assert a==e,(i,cases[i],a[:3000],e[:3000])
report={'passed':True,'native_sequences':len(cases),'native_commands':commands,'recovered_config_entries':{k:len(v)//2 for k,v in cached.items()},'scope':'Actual ARM64 MoveManager.GetMoveRemoveAmount and static unclamped adjustment, SuperDuke.ShouldRemoveOneMove and current constant-false AB body, SLB GetMoveAdjustment/TryGetEntry/cache range/group dispatch/EnsureCacheLoaded/BuildGroupDictionary/token parser/GetCachedData, active move update and fresh LevelParser move getters. Exercises recovered October 8 config across all 232 tested level numbers and AB groups, cache reuse, missing contexts, thrown AB/storage services, signed overflow, 20-move clamp, malformed pairs/tokens and duplicate overwrite. CLR metadata/allocation/string/integer parsing/dictionary runtime, AB initialization and AppKeyValue/JsonUtility transport are supplied boundaries; diagnostic logs omitted. Full active session Load/name normalization, episode/three-color/welcome-back preprocessing, saved-level providers, persistent/download lifecycle and formal playable context remain pending.'}
(PROJECT/'move_budget_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report))
