"""Execute original ARM64 LevelManager.LevelLoad and required move providers."""
import sys
sys.dont_write_bytecode=True
import base64,hashlib,json,os,random,struct,subprocess,tempfile,unicodedata
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_move_budget.py').read_text().split('cached=json.loads')[0],str(PROJECT/'SourceCatalog/verify_move_budget.py'),'exec'))
emu.hook_del(native_hook)
base_move_hook=hook
load,episode,strategy,league,gift,gift_progress,progress,manager=0x1000b000,0x1000c000,0x1000d000,0x1000e000,0x1000f000,0x10018000,0x1000a000,0x10008000
literal_values={int(s['address'],16):s['value']for s in json.loads((PROJECT.parent/'code/metadata/stringliteral.json').read_text())}
literal_slots=(0x5608098,0x5623c20,0x56336a0,0x56236b0,0x5618958,0x5616fe8,0x561c600,0x5614788,0x5615520,0x5621db8,0x561fbe8,0x5621938,0x560ff40,0x5622838,0x5610af8)
for i,slot in enumerate(literal_slots):
    raw=literal_values[slot].encode('utf-16-le');p=0x10180000+i*0x100;put(p+0x10,len(raw)//2);emu.mem_write(p+0x14,raw);put(slot,p,8)
empty_method,empty_type,empty_class,empty_static,empty_array=[0x101a0000+i*0x1000 for i in range(5)]
put(0x55c1220,empty_method,8);put(empty_method+0x38,empty_type,8);put(empty_type,empty_class,8);put(empty_class+0x132,1,1);put(empty_class+0xe0,1);put(empty_class+0xb8,empty_static,8);put(empty_static,empty_array,8)
put(strategy,0x10019000,8)
for slot,target in((0,0x2500c30),(2,0x2500c38)):
    put(0x1001a000+slot*16,target,8);put(0x1001a008+slot*16,0,8)
load_flags=0;user_level=0;resource_number=0;three_mode=0;three_result=0;gift_fail=0;league_text=None;native_calls=0

def encoded(s):return '~'if s is None else base64.b64encode(s.encode()).decode()
def boolfield(a):return read(a,1)!=0
def fault_or_return(uc,condition,value=0):
    if condition:fail(uc)
    else:returned(uc,value)

def composition_hook(uc,address,size,unused):
    x0,x1,x2,x3,x4=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(5)]
    if address==0x1e1a0cc:
        trace.append('resources,'+str(signed(read(load+0x9c)))+','+str(read(load+0x28,1)))
        if load_flags&1024:fail(uc)
        else:put(load+0x30,parser if load_flags&2 else 0,8);put(load+0x9c,resource_number);returned(uc)
    elif address==0x1c21ee0:trace.append('force');fault_or_return(uc,load_flags&2048,load_flags&256!=0)
    elif address==0x1c1ea9c:trace.append('name');fault_or_return(uc,load_flags&8192,string('fixturev99_L'))
    elif address==0x1e1aa40:uc.reg_write(UC_ARM64_REG_PC,0x1e1ab58) # Omit only the missing-parser diagnostic.
    elif address==0xfcd934 and x0==strategy:
        assert x2 in(0,2),x2
        returned(uc,0x1001a000+x2*16)
    elif address==0x29e27cc and x0==5:trace.append('league');returned(uc,league if load_flags&128 else 0)
    elif address==0x29e27cc and x0==87:returned(uc,manager if flags&256 else 0)
    elif address==0x283d900:
        trace.append('three,'+','.join(map(str,(signed(x0&0xffffffff),signed(x1&0xffffffff),x2,x3,x4))))
        fault_or_return(uc,load_flags&16384,three_result)
    elif address==0x283fcdc:trace.append('version,'+str(signed(x0&0xffffffff)));put(x1,0,8);fault_or_return(uc,flags&2048,0)
    elif address==0x32c00f0:
        trace.append('ab,'+','.join(map(str,(signed(read(active+0x14)),signed(read(active+0x20)),read(active+0x10,1),signed(read(active+0x3c))))));fault_or_return(uc,flags&1024,0)
    elif address==0x25d4b1c and x0==read(0x5610af8,8):trace.append('league_storage');returned(uc,string(league_text))
    elif address==0x1d74d2c:
        text=string_value(x0)
        if text is None:fail(uc)
        else:returned(uc,array(text.split(chr(x1))))
    elif address==0x26540e0:
        success,number=try_int(string_value(x0));fault_or_return(uc,not success,number)
    elif address in(0x29cf3fc,0x29cf474,0x29cf6c8,0x29cf698):
        stage,offset,label={0x29cf3fc:(1,0x24,'start'),0x29cf474:(2,0x20,'tier'),0x29cf6c8:(3,0x1c,'date'),0x29cf698:(4,0x28,'reward')}[address]
        value=signed(x1&0xffffffff);trace.append(label+','+str(value));put(gift_progress+offset,value,1 if stage==4 else 4);fault_or_return(uc,gift_fail==stage)
    elif address==0x25e50ac:trace.append('churn,'+str(signed(x1&0xffffffff)));fault_or_return(uc,gift_fail==5,1)
    elif address==0x1d77a98:
        raw=bytes(emu.mem_read(x0+0x14,read(x0+0x10)*2));found=-1
        for i in range(len(raw)//2):
            if int.from_bytes(raw[2*i:2*i+2],'little')==x1:found=i
        returned(uc,found)
    elif address==0x1d6cc9c:returned(uc,read(x0+0x14+2*x1,2))
    elif address==0x2044854:returned(uc,unicodedata.category(chr(x0))=='Nd')
    elif address==0x1027570:p=allocate(0x18+2*x0);put(p+0x10,x0);returned(uc,p)
    elif address==0x22f9920:emu.mem_write(x0,bytes(emu.mem_read(x1,x2)));returned(uc)
    elif address==0x2653e44:
        value=signed(read(x0));returned(uc,string(('-'if value<0 else'')+format(abs(value),'02d')))
    elif address in(0x1d6812c,0x1d72d80):returned(uc,string(''.join(string_value(p)or''for p in((x0,x1)if address==0x1d6812c else(x0,x1,x2)))))
    elif address==0x1d72f48:returned(uc,string(''.join(string_value(read(x0+0x20+8*i,8))or''for i in range(read(x0+0x18)))))
    elif address==0x2a2da14:returned(uc)
    else:base_move_hook(uc,address,size,unused)
emu.hook_add(UC_HOOK_CODE,composition_hook)
metadata=PROJECT.parent.parent/'RoyalMatch_20260929/apk_unpacked/base/assets/bin/Data/Managed/Metadata/global-metadata.dat'
raw_metadata=metadata.read_bytes();tiers={};tier_evidence={}
for key,offset,digest in(('tier1',0xa41630,'DB59009EE5BEEFBBA740BE33BC67C7D48C2A90C837CC8F7F4D8256B43CBF971F'),('tier2',0xa414b0,'CA7E76EF277D462C0BBBBC6100313F9CC81A2B944CC42D43D5CC6B042557B795'),('tier3',0xa40a08,'6A32DC2C8739087997E603EFBA4AAFA9061F8357090DF59BE2A256A923D4428F')):
    raw=raw_metadata[offset:offset+100];assert hashlib.sha256(raw).hexdigest().upper()==digest;tiers[key]=list(struct.unpack('<25i',raw));tier_evidence[key]={'metadata_offset':hex(offset),'sha256':digest}
assert tiers==json.loads((PROJECT/'SourceCatalog/welcome_back_move_tiers_snapshot.json').read_text())
real_slb=json.loads((PROJECT/'SourceCatalog/slb_move_config_snapshot.json').read_text());data=[real_slb[k]for k in('c','d','f','g','h')]
cases=[];expected=[];commands=0

def run(h,csv,steps,tier_mode=0):
    global flags,load_flags,user_level,resource_number,three_mode,three_result,gift_fail,league_text,next_object,stored_text,commands,data,duke_mode,forced_group
    flags,load_flags,number,user_level,raw_moves,three_mode,three_result,league_index,user_league,tier,start,difficulty,bonus,reactivation,slb_group,admin,online,resource_number,gift_fail,churn,randomized=h
    league_text=csv;next_object=0x10030000;dictionaries.clear();duke_mode=0;forced_group=-1
    emu.mem_write(user,bytes(0x9000));emu.mem_write(load,bytes(0x5000));emu.mem_write(progress,bytes(0x100));emu.mem_write(user_static,bytes(0x3000));emu.mem_write(gift_progress,bytes(0x100))
    put(strategy,0x10019000,8);put(episode+0x10,strategy if load_flags&8 else 0,8)
    def contexts():
        put(user_static,user if flags&2 else 0,8);put(user+0x18,progress if flags&512 else 0,8);put(user+0x38,session if flags&4 else 0,8);put(session+0x18,active if flags&8 else 0,8);put(user+0x40,ab if flags&16 else 0,8);put(progress+0x14,user_level);put(progress+0x18,user_league)
    contexts();put(manager+0x22,three_mode,1);put(load+0x10,episode if load_flags&4 else 0,8);put(load+0x30,parser if load_flags&1 else 0,8);put(load+0x9c,number);put(load+0x28,load_flags&512!=0,1);put(load+0x67,randomized,1);put(load+0xd8,manager if load_flags&16 else 0,8);put(load+0xb8,gift if load_flags&32 else 0,8);put(parser+0x10,raw_moves)
    for offset,value in((0x14,77),(0x20,-7),(0x24,-8),(0x3c,5),(0x2c,league_index)):put(active+offset,value)
    for offset,value in((0x10,admin),(0x11,online),(0x28,bonus),(0x29,reactivation),(0x30,difficulty)):put(active+offset,value,1)
    put(active+0x18,string('prior'),8);put(move_static,77);put(booster+0x18,slb_group)
    stored_text=string('stored')
    for i,values in enumerate(data):put(config+0x10+8*i,array(values),8)
    put(gift+0x10,gift_progress if load_flags&64 else 0,8);put(gift+0x28,1,8);put(gift+0x34,churn);put(gift_progress+0x20,tier);put(gift_progress+0x24,start);put(gift_progress+0x1c,42);put(gift_progress+0x28,1,1)
    arrays=[tiers[k]for k in('tier1','tier2','tier3')]
    if tier_mode in(1,2,3):arrays[tier_mode-1]=None
    elif tier_mode==4:arrays=[[],[],[]]
    for i,values in enumerate(arrays):
        pointer=0
        if values is not None:
            pointer=allocate(0x20+4*len(values));put(pointer+0x18,len(values))
            for j,value in enumerate(values):put(pointer+0x20+4*j,value)
        put(gift+0x38+8*i,pointer,8)
    output=[]
    for mode,*args in steps:
        trace.clear();result='-';a=args[0]if args else 0;commands+=1
        if mode in('l','w','a','r','c','e'):
            answer=call({'l':0x1e1a8fc,'w':0x283bea4,'a':0x283be08,'r':0x283bcb0,'c':0x283db2c,'e':0x2500c38}[mode],*({'l':(load,),'w':(gift,),'a':(gift,),'r':(gift,a),'c':(),'e':(strategy,a)}[mode]))
            if mode in('w','a','c','e'):result=str(answer)
        elif mode=='g':
            call(0x1e14688,league,a);pair=emu.reg_read(UC_ARM64_REG_X0);result=str(signed(pair&0xffffffff))+','+str(signed(pair>>32))
        elif mode=='p':call(0x1e12130,league,string(a))
        elif mode=='u':user_level=a;contexts()
        elif mode=='i':put(active+0x2c,a)
        elif mode=='b':put(active+0x28,a,1);put(active+0x30,args[1],1)
        elif mode=='n':put(load+0x9c,a)
        elif mode=='f':flags=a;contexts()
        if mode not in('u','i','b','n','f')and fault:result='error'
        fields=[signed(read(load+0x9c)),read(load+0x28,1),read(load+0x30,8)!=0,signed(read(active+0x14)),signed(read(active+0x20)),signed(read(active+0x24)),encoded(string_value(read(active+0x18,8))),read(active+0x10,1),read(active+0x11,1),signed(read(active+0x3c)),signed(read(move_static)),signed(read(gift_progress+0x20)),signed(read(gift_progress+0x24)),signed(read(gift_progress+0x1c)),read(gift_progress+0x28,1),signed(read(gift+0x34)),read(gift+0x28,8)!=0]
        fields=[int(x)if isinstance(x,bool)else x for x in fields];output.append(':'.join(map(str,[result,*fields,';'.join(trace)])))
    tier_text=';'.join('~'if a is None else','.join(map(str,a))for a in arrays)
    slb_text=';'.join(','.join(encode(s)for s in a)for a in data)
    step_text=';'.join(','.join([mode,*[encoded(a)if mode=='p'else str(a)for a in args]])for mode,*args in steps)
    cases.append(','.join(map(str,h))+'#'+encoded(csv)+'#'+tier_text+'#'+slb_text+'#'+step_text);expected.append('|'.join(output))

valid=1|2|4|8|16|32|256|512
normal_load=1|2|4|8|16|32|64|128
def head():return[valid,normal_load,1,1,30,0,22,0,1,0,0,0,0,0,0,0,0,1,0,7,0]
for missing in(0,1,2,4,8,16,32,64,128):
    for mode in(0,1):
        for force in(0,256,512,1024,2048,8192,16384):
            h=head();h[1]=(normal_load&~missing)|force;h[5]=mode;run(h,'10:-2,20:3',[('l',),('w',),('l',)])
for baseflags in(0,1,2,6,14,30,valid&~1,valid&~2,valid&~4,valid&~8,valid&~16,valid&~32,valid&~256,valid&~512,valid|64,valid|128,valid|1024,valid|2048):
    for mode in(0,1):
        h=head();h[0]=baseflags;h[5]=mode;h[2]=162;h[3]=162;h[17]=162;h[14]=3;run(h,'10:-2,20:3',[('l',),('w',)])
for tier in(-1,0,1,2,3,4):
    for difficulty in(-128,-1,0,1,2,3,127):
        h=head();h[9]=tier;h[10]=100;h[3]=100;h[11]=difficulty
        run(h,'10:-2,20:3',[('w',),('u',99),('w',),('u',101),('w',),('u',124),('w',),('u',125),('w',),('a',),('u',126),('a',)])
for stage in range(6):
    h=head();h[9]=3;h[3]=125;h[10]=100;h[18]=stage;run(h,None,[('a',),('w',),('r',0),('r',1)])
for tier_mode in range(5):
    for tier in(1,2,3):
        h=head();h[9]=tier;h[3]=100;h[10]=100;h[11]=3;run(h,'10:-2',[('w',),('b',1,3),('w',),('u',126),('w',)],tier_mode)
for text in(None,'','1','1:2','1:2:3','1:2,3:4','1:2,broken','1:2,3:bad',' 1 : -2 ,3:+4','2147483648:2','-2147483648:2147483647'):
    h=head();h[7]=1
    run(h,text,[('g',n)for n in(-2147483648,-1,0,1,2,3,2147483647)]+[('c',),('e',30),('p','1:2,3:4'),('g',3),('c',),('l',)])
for group,level in((3,162),(4,75),(6,111),(7,150),(8,212)):
    for raw in(19,20,21,30,2147483647):
        h=head();h[2]=h[3]=h[17]=level;h[14]=group;h[4]=raw;run(h,'10:-2',[('l',),('l',)])
rng=random.Random(0x1e1a8fc)
for i in range(55):
    h=head();h[2]=rng.choice((-1,1,20,162));h[3]=rng.choice((1,20,100,124,125));h[4]=rng.choice((-2147483648,-1,0,19,20,21,30,2147483647));h[5]=rng.randrange(2);h[6]=rng.randrange(0,40);h[7]=rng.randrange(2);h[8]=rng.choice((0,1,2,3));h[9]=rng.randrange(5);h[10]=rng.choice((0,100));h[11]=rng.randrange(-1,4);h[12]=rng.randrange(2);h[13]=rng.randrange(2);h[15]=rng.randrange(2);h[16]=rng.randrange(2);h[17]=rng.choice((-1,1,20,162));h[20]=rng.randrange(2)
    run(h,'10:-2,20:3',[('w',),('l',),('u',125),('w',),('i',1),('c',),('l',)])
    if i%20==0:print('native sequence progress',len(cases),flush=True)
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-level-load-')as directory:
    exe=Path(directory)/'verify.exe';files=('OriginalLevelLoad','OriginalLeagueLevelMoves','OriginalWelcomeBackMoves','OriginalThreeColorMoves','OriginalThreeColorMoveConfig','OriginalThreeColorLevelNames','OriginalActiveLevelData','OriginalActiveLevelInitialization','OriginalLevelSessionCounters','OriginalSessionValues','OriginalMoveBudgetAdjustment','OriginalSlbMoveConfig')
    compiled=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-langversion:latest','-out:'+str(exe),str(PROJECT/'SourceCatalog/LevelLoadHarness.cs'),*[str(PROJECT/'Assets/Foundation/Runtime'/(n+'.cs'))for n in files]],capture_output=True,text=True);assert compiled.returncode==0,compiled.stdout+compiled.stderr
    completed=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert completed.returncode==0,completed.stdout+completed.stderr
    actual=completed.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
    for i,(a,e)in enumerate(zip(actual,expected)):
        if a!=e:
            rows_a,rows_e=a.split('|'),e.split('|');j=next(j for j,(x,y)in enumerate(zip(rows_a,rows_e))if x!=y)
            raise AssertionError((i,j,cases[i].split('#')[0],cases[i].split('#')[4].split(';')[j],rows_a[j],rows_e[j]))
report={'passed':True,'native_sequences':len(cases),'native_commands':commands,'welcome_back_tier_evidence':tier_evidence,'scope':'Actual ARM64 LevelManager.LevelLoad/ShouldForceReloadLevel with real active Load/name/SLB post-adjustment, native episode interface dispatch to SemiDynamicDifficulty.GetUpdatedMoveCount/SetLevelNumber, LeagueManager.ParseLevelAndMoves/GetLeagueLevel and current league offset, WelcomeBackGiftManager.IsMoveGiftActive/GetExtraMoveCount/ResetGiftData/SetChurnPeriod. Verifies resource reload decisions, write/exception ordering, previous active flags, move sums/overflow, recovered SLB data and metadata-hash-verified three 25-entry tier tables, hard first-level floors, bonus exclusion, exactly-25 expiry reset, partial setter failures, league malformed cache retention and randomized branches. Resource selection, parser force/name transport, three-color preprocessing (separately ARM64 verified), CLR runtime services, AppKeyValue and progress setter/persistence are supplied boundaries; diagnostic logs omitted. Formal application providers, complete constructor/session Start/Restart/save lifecycle and playable damage/resolution/fall/refill/HUD/settlement remain pending.'}
(PROJECT/'level_load_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report))
