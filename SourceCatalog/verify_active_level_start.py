"""Execute the original ARM64 Start/Restart/SetStartValues orchestration."""
import sys
sys.dont_write_bytecode=True
import json,os,random,struct,subprocess,tempfile
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_active_level_initialization.py').read_text().split('for missing_value in range(7):')[0],str(PROJECT/'SourceCatalog/verify_active_level_initialization.py'),'exec'))
emu.hook_del(init_hook);reset_hook=oracle_hook
simple_slots=[16,24,32,40,48,56,80,88,96,104,112,120,128,136,168,176,184,192,208,216,224,240,248,264]
event_slots=[64,72,144,152,160,200,232,256]
objects={s:0x10020000+i*0x100 for i,s in enumerate(sorted(simple_slots+event_slots))}
objects[40]=coin;objects[48]=extra
labels={p:s for s,p in objects.items()if s in simple_slots or s==152}
archery_manager,streak_data,rank_data,interface_entry,unit_object=0x10023000,0x10023100,0x10023200,0x10023300,0x10024000
start_missing=0;failure_stage=0;unit_cloche_calls=0;can_play_calls=0;all_head=[]
def record(uc,value):
    trace.append(value)
    if len(trace)==failure_stage:fail(uc);return False
    return True
def result(uc,label,value=0):
    if record(uc,label):returned(uc,value)
def start_hook(uc,address,size,unused):
    global unit_cloche_calls,can_play_calls,pref_count
    x0,x1,x2,x3=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(4)]
    if address==0x32b7244:
        if start_missing==6:put(user+0x20,0,8)
    elif address==0x29e27cc:
        if not record(uc,'unit,'+str(x0)):return
        assert x0 in(29,50,31,63,79,57,45,66),x0
        if x0==31:
            unit_cloche_calls+=1;returned(uc,0 if start_missing==unit_cloche_calls and start_missing<=2 else unit_object)
        else:
            absent={63:3,79:4,57:10,45:11,66:14}.get(x0,-1)
            returned(uc,0 if start_missing==absent else archery_manager if x0==45 else unit_object)
    elif address in(0x222d854,0x222ec04,0x210555c):
        code={0x222d854:29,0x222ec04:50,0x210555c:63}[address]
        result(uc,'progress,'+str(code),head[6]if code==29 else head[7]if code==50 else 13)
    elif address==0x1f881dc:result(uc,'cloche',11)
    elif address==0x1f88c5c:result(uc,'save_cloche,'+str(signed(x1&0xffffffff)))
    elif address==0x2113afc:
        can_play_calls+=1;result(uc,'can_play',all_head[12]if can_play_calls==1 else all_head[13])
    elif address==0x21148d4:result(uc,'selected',all_head[14])
    elif address==0x1687b78:assert x0==0;result(uc,'multiply',all_head[11])
    elif address==0x28e7944:
        if record(uc,'remaining,'+str(x1)):
            if start_missing==x1+3:put(user+0x20,0,8)
            returned(uc,all_head[15+x1-4])
    elif address==0x3e4263c:
        if record(uc,'clock'):uc.reg_write(UC_ARM64_REG_S0,struct.unpack('<I',struct.pack('<f',42.25))[0]);returned(uc)
    elif address==0x338c6a8 and x0 in labels:record(uc,'init,'+str(labels[x0])+','+str(signed(x1&0xffffffff)))
    elif address==0x32b2fd8:result(uc,'madness,'+str(x1)+','+str(signed(x2&0xffffffff)))
    elif address in(0x32b51fc,0x32b45c8):result(uc,('royal'if address==0x32b51fc else'pinata')+','+str(x1))
    elif address==0x338c958:result(uc,'clash,'+str(signed((x1&255)|0xffffff00)if x1&128 else x1)+','+str(x2)+','+str(x3))
    elif address==0x1f81948:result(uc,'propeller_progress',all_head[20])
    elif address==0x1e18f34:result(uc,'table',all_head[21])
    elif address==0x20f5610:result(uc,'balloon',all_head[19])
    elif address==0x20eb13c:result(uc,'archery_progress',all_head[22])
    elif address==0x20eb22c:result(uc,'icon',all_head[25])
    elif address==0x3e41f48:result(uc,'get',pref_count)
    elif address==0x20000010:
        if record(uc,'set,'+str(signed(x1&0xffffffff))):pref_count=signed(x1&0xffffffff);returned(uc,1)
    elif address==0xfcd934 and x0 in(objects[144],objects[152]):
        assert x2 in(2,3),x2
        target=0x338c82c if x2==3 else 0x338c660 if x0==objects[152]else 0x32b4f80
        put(interface_entry,target,8);returned(uc,interface_entry)
    elif address in(0x32b6f3c,0x32b7ad8):uc.reg_write(UC_ARM64_REG_PC,0x32b703c if address==0x32b6f3c else 0x32b7bd8) # Omit only the post-initialization diagnostic Log.Debug argument building.
    else:reset_hook(uc,address,size,unused)
emu.hook_add(UC_HOOK_CODE,start_hook)
cases=[];expected=[];commands=0
def number(p):return signed(read(p))
def small(p):v=read(p,1);return v-256 if v>127 else v
def value(p):return ','.join(map(str,[read(p+0x20,1),number(p+0x24),number(p+0x28)]))
def nullable(slot):return start_missing==1000+slot
def full_state():
    a=active;p=objects[144];m=objects[200];inv=objects[256];ar=objects[152]
    core=[number(a+0x3c),number(a+0xac),read(a+0x40,1),number(a+0xcc),small(a+0x5c),read(a+0xbc,1),number(a+0xc0),number(a+0xc4),number(a+0xc8),read(read(a+0xe8,8)+0x18),
        *[number(a+o)for o in(0x4c,0x54,0x58,0xb0,0xb4,0xb8)],format(struct.unpack('<f',bytes(emu.mem_read(a+0xa8,4)))[0],'g'),read(a+0xd0,1),read(animation+0x131,1),*[read(a+o,1)for o in range(0xe0,0xe8)],pref_count]
    owned='/'.join(value(read(a+o,8))for o in(0x60,0x68,0x70,0x78,0x80))
    r=read(a+0xa0,8);s=read(a+0x90,8);mat=read(read(a+0x98,8)+0x10,8)
    counters=[len(dictionaries[read(r+o,8)])for o in(0x10,0x18)]+[len(dictionaries[read(s+o,8)])for o in(0x10,0x18)]+[number(s+o)for o in(0x20,0x24,0x28,0x2c)]+[number(mat+0x20+4*i)for i in range(5)]
    snapshots='/'.join(str(slot)+'='+value(objects[slot])for slot in sorted(simple_slots))
    prop='null'if nullable(144)else value(p)+','+str(number(p+0x2c))+','+str(number(p+0x30))
    mission='null'if nullable(200)else ','.join(map(str,[read(m+0x20,1),*[number(m+o)for o in(0x24,0x28,0x2c,0x30,0x34)]]))
    invite='null'if nullable(256)else str(read(inv+0x10,1))+','+str(number(inv+0x14))
    arch='null'if nullable(152)else value(ar)+','+str(number(ar+0x2c))+','+str(number(ar+0x30))+','+str(read(ar+0x34,1))
    return ':'.join([','.join(map(str,core)),owned,','.join(map(str,counters)),snapshots,prop,mission,invite,arch])
def dirty_start():
    for p in [read(active+o,8)for o in(0x60,0x68,0x70,0x78,0x80)]+[objects[s]for s in simple_slots]+[objects[144],objects[152]]:
        put(p+0x20,1,1);put(p+0x24,101);put(p+0x28,202)
    for o,v in((0x3c,5),(0xac,9),(0xcc,88),(0xc0,4),(0xc4,9),(0xc8,2),(0x4c,7),(0x54,3),(0x58,4),(0xb0,5),(0xb4,6),(0xb8,7)):put(active+o,v)
    for o,v in((0x40,1),(0x5c,-8),(0xbc,0),(0xd0,1)):put(active+o,v,1)
    for o in range(0xe0,0xe8):put(active+o,1,1)
    put(active+0xa8,struct.unpack('<I',struct.pack('<f',8.5))[0]);put(animation+0x131,1,1)
    l=read(active+0xe8,8);arr=allocate(0x28);put(arr+0x18,1);put(l+0x10,arr,8);put(l+0x18,read(l+0x18)+1)
    r=read(active+0xa0,8);dictionaries[read(r+0x10,8)][4]=4;dictionaries[read(r+0x18,8)][5]=5
    s=read(active+0x90,8);dictionaries[read(s+0x10,8)][1]=1;dictionaries[read(s+0x18,8)][2]=3
    for i in range(4):put(s+0x20+4*i,11+i)
    for i in range(1,6):call(0x21a4028,read(active+0x98,8),i,i*11)
    m=objects[200];put(m+0x20,1,1)
    for o in(0x24,0x28,0x2c,0x30,0x34):put(m+o,5)
    put(objects[256]+0x10,1,1);put(objects[256]+0x14,55)
    put(objects[152]+0x2c,3);put(objects[152]+0x30,9);put(objects[152]+0x34,1,1)
def run_start(h,steps):
    global head,all_head,start_missing,failure_stage,pref_count,next_object,commands,unit_cloche_calls,can_play_calls
    all_head=h;head=h[:9];start_missing=h[9];failure_stage=h[10];pref_count=h[8];next_object=0x10030000;dictionaries.clear()
    emu.mem_write(active,bytes(0x200));emu.mem_write(animation,bytes(0x200))
    for p in objects.values():emu.mem_write(p,bytes(0x100));put(p,basic_class,8)
    for p in(objects[144],objects[152]):call(0x338adc4,p)
    put(user_static,user,8);put(user+0x20,inventory,8);put(user+0x38,session,8);put(session+0x18,0 if start_missing==16 else active,8);put(inventory+0x10,h[4]);put(inventory+0x14,h[5])
    for slot,p in objects.items():put(animation+slot,0 if nullable(slot)or(start_missing==15 and slot not in(40,48))else p,8)
    put(archery_manager+0x40,0 if start_missing==12 else streak_data,8);put(streak_data+0x28,h[23],8)
    put(archery_manager+0x58,0 if start_missing==13 else rank_data,8);put(rank_data+0x20,h[24])
    call(0x32b6324,active,0 if start_missing==5 else animation);put(active+0x34,h[2]);put(active+0x30,h[3],1);out=[]
    for step in steps:
        commands+=1;trace.clear();unit_cloche_calls=0;can_play_calls=0;fault_before=False;result_text='-';put(user+0x20,inventory,8)
        if step=='d':dirty_start()
        elif step in('b','r'):call(0x32b6ed0 if step=='b'else 0x32b7a6c,active)
        elif step.startswith('v,'):call(0x32b7244,active,int(step[2:]))
        elif step.startswith('flags,'):
            f=step.split(',');put(active+0x28,int(f[1]),1);put(active+0x39,int(f[2]),1)
        else:
            f=step.split(',');mode=f[0];args=list(map(int,f[1:]))
            if mode in('pi','pm','pr','pl','pg'):
                answer=call({'pi':0x32b4ce4,'pm':0x32b4cf8,'pr':0x32b4d9c,'pl':0x32b4eac,'pg':0x32b4e18}[mode],objects[144],*args)
                if mode=='pg':raw_result=emu.reg_read(UC_ARM64_REG_X0);result_text=str(raw_result-(1<<64)if raw_result>>63 else raw_result)
            elif mode in('al','ag','ai'):
                call({'al':0x338c5c4,'ag':0x338c500,'ai':0x338c4e8}[mode],objects[152],*args)
                if mode=='ag':raw_result=emu.reg_read(UC_ARM64_REG_X0);result_text=str(raw_result-(1<<64)if raw_result>>63 else raw_result)
            elif mode=='mul':result_text=str(call(0x338c6f0,*args))
        if fault:result_text='error'
        trace_text=';'.join(trace);out.append(result_text+':'+full_state()+':'+trace_text)
    cases.append(','.join(map(str,h))+'#'+';'.join(steps));expected.append('|'.join(out))
def header():return[0,0,0,0,1001,7,9,12,2147483647,0,0,0,1,1,3,10,0,-1,20,17,1,0,1,5<<20,4,1]
for absent in([0]+list(range(1,16))+[1000+s for s in simple_slots+event_slots]):
    h=header();h[9]=absent;run_start(h,['d','b','r','v,0','v,1','v,2'])
for stage in range(1,60):
    h=header();h[10]=stage;run_start(h,['d','b','r','v,1','v,2'])
for story in(-1,0):
    for mode in range(4):
        for multiplied in(0,1):
            for first,second in((0,0),(0,1),(1,0),(1,1)):
                h=header();h[2]=story;h[11]=multiplied;h[12]=first;h[13]=second
                run_start(h,['d',f'flags,{mode&1},{mode>>1}','b','r'])
for difficulty in(-128,-1,0,1,2,3,127):
    for progress,table_game in((0,0),(1,0),(1,1)):
        h=header();h[3]=difficulty;h[20]=progress;h[21]=table_game
        run_start(h,['d','v,-1','v,1','r'])
rng=random.Random(0x32b7244)
for i in range(40):
    h=header();h[2]=rng.choice((-2147483648,-1,0,1,2147483647));h[3]=rng.randrange(-128,128)
    for k in(11,12,13,20,21,22,25):h[k]=rng.randrange(2)
    for k in(14,15,16,17,18,19,24):h[k]=rng.choice((-2147483648,-1,0,1,2,10,2147483647))
    h[23]=rng.randrange(-2**63,2**63);run_start(h,['d','b','r','v,3'])
for missing_context in(0,14,16):
    for difficulty in(-2147483648,-128,-2,-1,0,1,2,3,4,127,2147483647):
        h=header();h[9]=missing_context;h[3]=difficulty if -128<=difficulty<=127 else 3
        run_start(h,['d',f'mul,{difficulty}',f'pi,-1,{difficulty}','pm,2147483647','pg','pl,-1','pg','pr','al,-1','ag','ai,-1,-2147483648,2147483647,0','ag','al,0','ag'])
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-active-start-')as directory:
    exe=Path(directory)/'verify.exe';files=('OriginalSessionValues','OriginalLevelSessionCounters','OriginalActiveLevelInitialization','OriginalActiveLevelData','OriginalMoveBudgetAdjustment','OriginalSlbMoveConfig','OriginalActiveLevelStart')
    p=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-langversion:latest','-out:'+str(exe),str(PROJECT/'SourceCatalog/ActiveLevelStartHarness.cs'),*[str(PROJECT/'Assets/Foundation/Runtime'/(n+'.cs'))for n in files]],capture_output=True,text=True);assert p.returncode==0,p.stdout+p.stderr
    p=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert p.returncode==0,p.stderr[-2000:]
    actual=p.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
    for i,(a,e)in enumerate(zip(actual,expected)):
        if a!=e:
            j=next(j for j,(x,y)in enumerate(zip(a.split('|'),e.split('|')))if x!=y)
            raise AssertionError((i,j,cases[i].split('#')[0],cases[i].split('#')[1].split(';')[j],a.split('|')[j],e.split('|')[j]))
report={'passed':True,'native_sequences':len(cases),'native_commands':commands,'scope':'Actual ARM64 UserActiveLevelData.Start/Restart/SetStartValues with actual ResetValues, owned counters/dictionaries, snapshot base Init, mission/invite in-place resets, PropellerRush Init/TryIncrementCount/Reset and save/load with current-level difficulty recovery, difficulty multiplier table, Archery field capture/virtual Init and six-field packed save/load. Compares ordered required calls and partial state for every missing manager/inventory/snapshot and 59 callback-failure positions, first/restart/other origins, mutable multiplier eligibility reads, story/bonus/table gates, independent four inventory resolutions, time/collection/counter reset order and random signed/packed edges. Native diagnostic Log.Debug tail only is omitted. CLR allocation/collections/metadata, application manager/progress/inventory/PlayerPrefs/time reads, MultiplierUtil.CanMultiply and Madness/RoyalPass/Pinata/Clash/derived virtual event Init implementations are explicit required boundary fixtures. Full real AnimationData/event initialization and application composition, save/restore beyond restored data, formal playable resolution and visual parity remain pending. No fixture installed in production; SDK unchanged.'}
(PROJECT/'active_level_start_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
