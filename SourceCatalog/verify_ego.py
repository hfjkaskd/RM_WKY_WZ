"""Actual ARM64 coin spending, Ego continuation/state and item coroutine vs C#.

UpdateCoins storage/event transport, current user/home/AB/episode/offer/extra
move/audio/save/charger/grid/creator/coroutine-host owners are typed boundaries.
SpendCoins, HasEnoughCoins, package indexing/getters, Ego flow/state, item
coroutine and gameplay waits execute in the ELF. No production dialog binding,
AB catalog implementation, real rendered item throw or mobile parity is proved.
"""
from pathlib import Path
import hashlib,itertools,json,random,struct,subprocess,tempfile
PROJECT=Path(__file__).resolve().parents[1];SCOPE=__doc__.strip()
source=PROJECT/'SourceCatalog/verify_low_match.py'
exec(compile(source.read_text().split('cases=[];expected=[]')[0],str(source),'exec'))
ego,inventory,other,user_statics,user,session_data,session,animation,snapshot,home_statics,home,home_data,home_panel,coin_view,ego_statics,packages,extra,state,moves,audio,level,episode,offer,multiplier,charger,save,grid,factory,creator,iterator,cell_list,event,klass=[0x10100000+i*0x1000 for i in range(33)]
package_addresses=[0x10200000+i*0x1000 for i in range(5)]
cell_addresses=[0x10210000+i*0x1000 for i in range(4)]
prefix,name_ptr,joined_ptr,wait=0x10320000,0x10321000,0x10322000,0x10323000
dialog,winfail,interface_cell,interface_type,winfail_class,interface_table=0x10324000,0x10325000,0x10326000,0x10327000,0x10328000,0x10329000
invite=0x1032a000
v=[];routine=0;wait_kind='';strings={prefix:'Ego-'};spent=False
def fbits(value):return struct.unpack('<I',struct.pack('<f',value))[0]
def maybe_fail(number):
    if v[1]==number:failure();return True
    return False
def hook(uc,address,size,unused):
    global routine,wait_kind,spent
    a,b,c=x(0),x(1),x(2);missing=v[1]
    if address==0x28e4db0:
        value=max(0,si(b));put(a+0x10,value,4)
        if maybe_fail(1):return
        trace.extend(('write,'+str(value),'updated'));returned();return
    if address==0x20000400:
        trace.append('spent');spent=True
        if v[22]:put(user+0x20,other)
    elif address==0x2fb02b0:
        if maybe_fail(3):return
        trace.append('home')
    elif address==0x20000410:
        trace.append('snapshot,'+str(si(b)));put(snapshot+0x24,b,4);put(snapshot+0x28,b,4)
    elif address==0x30f6400:
        assert a==19,a
        if missing==7:returned(0);return
        returned(level);return
    elif address==0x1d10f3c:trace.append('spending,'+str(si(b))+','+strings[c]+','+str(si(x(4))))
    elif address==0x1ca509c:
        if maybe_fail(22):return
        trace.append('configure,'+str(si(b)))
    elif address==0x2657978:returned(min(si(a),si(b)));return
    elif address==0x2653db0:strings[name_ptr]=str(si(read(a)));returned(name_ptr);return
    elif address==0x1d6812c:
        assert a==prefix;strings[joined_ptr]=strings[a]+strings[b];returned(joined_ptr);return
    elif address==0x29e27cc:
        if a==14:returned(0 if missing==14 else offer);return
        if a==2:returned(0 if(missing==13 and v[0]in(3,10,11))or(missing==23 and v[0]==4)else level);return
        if a==61:returned(0 if missing==24 else multiplier);return
        raise AssertionError(('user unit',a))
    elif address==0x1f79824:trace.append('offer,'+str(b)+','+str(c))
    elif address==0x1e18a10:trace.append('episode-accept')
    elif address==0x1e1881c:trace.append('episode-first')
    elif address==0x1ee57bc:trace.append('play');put(state+0x18,1,4)
    elif address==0x1edb0bc:trace.append('fail');put(state+0x18,4,4)
    elif address==0x326158c:
        if maybe_fail(20):return
        trace.append('has-additional');returned(v[11]);return
    elif address==0x32615c0:trace.append('additional');returned(v[12]);return
    elif address==0x32612d0:trace.append('additional-available');returned(v[11]);return
    elif address==0x32613c8:trace.append('seen')
    elif address==0x1ca4e54:trace.append('coin-shop,0');return
    elif address==0x32b118c:trace.append('invite-price,'+str(si(b)));return # Execute original snapshot setter.
    elif address==0x32baa54:
        if maybe_fail(26):return
        trace.append('remaining,'+str(si(b)))
    elif address==0x1eda0f4:trace.append('moves,'+str(si(b)));put(moves+0x10,b,4)
    elif address==0x3e17838:trace.append('sound,'+str(b)+','+format(struct.unpack('<f',struct.pack('<I',u.reg_read(r.UC_ARM64_REG_S0)))[0],'.2f'))
    elif address==0x3e15e40:trace.append('resume')
    elif address==0x260dea4:
        if a==92:returned(0 if missing==15 else charger);return
        assert a==2,a;trace.append('grid');returned(0 if missing==18 else grid);return
    elif address==0x344f034:trace.append('charger')
    elif address==0x23df7bc:returned(save if v[21]else 0);return
    elif address==0x23e00a4:
        if maybe_fail(17):return
        trace.append('save')
    elif address==0xffda78:
        value=iterator if u.reg_read(r.UC_ARM64_REG_LR)==0x1ca57d8 else wait
        u.mem_write(value,bytes(0x200));returned(value);return
    elif address in(0x231007c,0x3e495b0):pass
    elif address==0x1e2aaac:
        if maybe_fail(21):return
        trace.append('start-coroutine');routine=a
    elif address==0x1ee1c9c:trace.append('op-start,'+str(b));put(state+0x100,read(state+0x100)+1,4)
    elif address==0x1ee1d14:trace.append('op-finish,'+str(b));put(state+0x100,read(state+0x100)-1,4)
    elif address==0x320fd10:trace.append('pattern')
    elif address in(0x320dddc,0x320e2e4,0x320e728):
        trace.append('cells,'+str({0x320dddc:1,0x320e2e4:2,0x320e728:3}[address]));returned(0 if missing==28 else cell_list);return
    elif address==0x2c1b9a0:returned(0 if missing==29 and b==0 else cell_addresses[b]);return
    elif address==0x319e1dc:
        index=cell_addresses.index(a);trace.append('jelly,'+str(index)+','+str(b));returned(index%2);return
    elif address==0x320dbc4:
        index=cell_addresses.index(b-0x200);trace.append('rocket,'+str(index)+','+str(c));returned(10 if index%2==0 else 20);return
    elif address==0x1c91f34:trace.append('create,'+str(cell_addresses.index(b)if b else-1)+','+str(c))
    elif address==0x3e49528:
        assert u.reg_read(r.UC_ARM64_REG_S0)==fbits(.5);wait_kind='u'
    elif address==0x12de578:
        assert u.reg_read(r.UC_ARM64_REG_S0)==fbits(.5);wait_kind='g';return # Run original constructor.
    elif address==0x32037e4:u.reg_write(r.UC_ARM64_REG_S0,fbits(.25))
    elif address==0x23e34a4:returned(v[5]);return
    elif address==0x23e3470:returned(v[12]);return
    elif address==0x23e34d8:returned(v[9]);return
    elif address==0x23df9e4:returned(level);return
    elif address==0x20000500:trace.append('clear,'+str(b))
    elif address==0x1347458:u.reg_write(r.UC_ARM64_REG_PC,0x20000000);return # Stop before unrelated dialog UI cleanup.
    else:raise AssertionError(hex(address))
    returned()
addresses=(0x28e4db0,0x20000400,0x2fb02b0,0x20000410,0x30f6400,0x1d10f3c,0x1ca509c,0x2657978,0x2653db0,0x1d6812c,0x29e27cc,0x1f79824,0x1e18a10,0x1e1881c,0x1ee57bc,0x1edb0bc,0x326158c,0x32615c0,0x32612d0,0x32613c8,0x1ca4e54,0x32b118c,0x32baa54,0x1eda0f4,0x3e17838,0x3e15e40,0x260dea4,0x344f034,0x23df7bc,0x23e00a4,0xffda78,0x231007c,0x3e495b0,0x1e2aaac,0x1ee1c9c,0x1ee1d14,0x320fd10,0x320dddc,0x320e2e4,0x320e728,0x2c1b9a0,0x319e1dc,0x320dbc4,0x1c91f34,0x3e49528,0x12de578,0x32037e4,0x23e34a4,0x23e3470,0x23e34d8,0x23df9e4,0x20000500,0x1347458)
for address in addresses:u.hook_add(UC_HOOK_CODE,hook,begin=address,end=address)
def setup():
    missing=v[1]
    for p in(ego,inventory,other,user_statics,user,session_data,session,animation,snapshot,home_statics,home,home_data,home_panel,coin_view,ego_statics,packages,extra,state,moves,audio,level,episode,offer,multiplier,charger,save,grid,factory,creator,iterator,cell_list,event,klass,*package_addresses,*cell_addresses):u.mem_write(p,bytes(0x500))
    for address in(0x57435db,0x573963a,0x573963f,0x573dc55,0x573dc56,0x573dc57,0x573dc5a,0x573dc5b,0x573dc5c,0x573dc5d,0x573e83e):put(address,1,1)
    for i,(got,storage)in enumerate(((0x534ab58,0),(0x53649d8,0),(0x53bb4e0,user_statics),(0x53cc258,home_statics),(0x5377db8,ego_statics),(0x5351eb0,0),(0x53b3c40,0),(0x53922c0,0))):
        cls=0x10380000+i*0x200;put(read(got,8),cls);put(cls+0xe0,1,4);put(cls+0x132,1,1);put(cls+0xb8,storage)
    put(user_statics,user);put(user+0x20,0 if missing==5 else inventory);put(user+0x38,session_data)
    put(session_data+0x18,0 if missing==27 else session);put(session_data+0x20,0 if missing==4 else animation)
    put(animation+0x28,0 if missing==6 else snapshot);put(snapshot,klass);put(klass+0x1b8,0x20000410)
    put(snapshot+0x24,-99,4);put(snapshot+0x28,-98,4);put(inventory+0x10,v[2],4);put(other+0x10,777,4)
    put(inventory+0xc8,event if v[14]else 0);put(event+0x18,0x20000400)
    put(home_statics+8,home if v[13]or missing==2 else 0);put(home+0x20,0 if missing==2 else home_data);put(home_data+0x18,home_panel);put(home_panel+0x68,coin_view)
    put(ego_statics,0 if missing==8 else packages);put(packages+0x18,v[20],4)
    for index,p in enumerate(package_addresses):
        put(packages+0x20+8*index,0 if missing==9 else p);put(p+0x10,v[3],4);put(p+0x14,v[10],1)
        put(p+0x18,p+0x200);put(p+0x218,3,4)
        for offset,value in enumerate(v[15:18]):put(p+0x220+offset,value,1)
    for offset,pointer in((0x10,factory),(0x18,0 if missing==10 else state),(0x20,moves),(0x28,0 if missing==12 else audio),(0x30,0 if missing==11 else extra)):put(ego+offset,pointer)
    initial_step=-1 if v[6]==0 and v[8]else v[6]
    put(ego+0x38,initial_step,4);put(ego+0x3c,v[8],1);put(ego+0x40,v[7],4);put(ego+0x44,v[9]==1,1)
    put(session+0x4c,v[5],4);put(session+0x88,animation);put(animation+0x100,0 if missing==25 else invite)
    put(level+0x10,episode);put(level+0x20,creator);put(factory+0x40,0 if missing==19 else creator)
    put(state+0x18,v[19],4);put(moves+0x10,17,4);put(cell_list+0x18,v[18],4)
    for i,p in enumerate(cell_addresses):put(p+0x18,p+0x200)
    put(read(0x5359200,8),prefix)
    put(dialog+0x188,0 if missing==31 else ego);put(winfail,winfail_class);put(winfail_class+0x12a,1,2);put(winfail_class+0xb0,interface_table)
    put(interface_table,interface_type);put(interface_table+8,0,4);put(interface_cell,interface_type);put(winfail_class+0x138,0x20000500)

base=[3,0,2000,900,0,0,0,0,0,0,5,0,0,0,1,0,0,0,2,3,5,1,0]
vectors=[]
edge=(-2147483648,-1,0,1,899,900,2000,2147483647)
for coin,price,missing,home_visible,event_set,swap in itertools.product(edge,edge,range(8),(0,1),(0,1),(0,1)):
    a=base.copy();a[0]=0;a[1]=missing;a[2]=coin;a[3]=price;a[13]=home_visible;a[14]=event_set;a[22]=swap;vectors.append(a)
rng=random.Random(0x1ca53e0)
for i in range(700):
    a=base.copy();a[0]=rng.choice((1,2,3,3,3,4,5,6,7,8,9));a[1]=rng.randrange(30)
    a[2]=rng.choice(edge);a[3]=rng.choice(edge);a[4]=rng.randrange(2);a[5]=rng.choice((-2147483648,-1,0,1,4,2147483647));a[6]=rng.choice((-2147483648,-1,0,1,2,3,2147483647))
    if a[1]==11 and a[6]==0:a[6]=-1
    a[7]=rng.randrange(4);a[8]=rng.randrange(2);a[9]=rng.choice((0,1,2));a[10]=rng.choice((0,5,255));a[11]=rng.randrange(2);a[12]=rng.choice((-2147483648,-7,0,3,2147483647));a[13]=rng.randrange(2);a[14]=rng.randrange(2)
    a[15:18]=[rng.randrange(3)for _ in range(3)];a[18]=rng.randrange(3);a[19]=rng.randrange(7);a[20]=rng.choice((0,5,5,5));a[21]=rng.randrange(2);a[22]=rng.randrange(2);vectors.append(a)
for rockets,tnt,balls,cells,missing in itertools.product(range(3),range(3),range(3),range(3),(0,18,19,28,29)):
    a=base.copy();a[0]=7;a[1]=missing;a[15:19]=[rockets,tnt,balls,cells];vectors.append(a)
for free,coin,additional in itertools.product((0,1),(0,899,900,2000),(-7,0,3)):
    a=base.copy();a[4]=free;a[2]=coin;a[11]=1;a[12]=additional;vectors.append(a)
for mode,missing,coin in itertools.product((10,11,12),(0,5,10,11,12,13,14,15,17,21,27,30,31),(899,2000)):
    a=base.copy();a[0]=mode;a[1]=missing;a[2]=coin
    if missing==11:a[6]=-1
    vectors.append(a)
for step in(-2147483648,-3,-2,-1,0,1,2147483647):
    a=base.copy();a[0]=13;a[6]=step;vectors.append(a)
cases=[];expected=[]
for v in vectors:
    setup();trace=[];routine=0;wait_kind='';spent=False;result=-1
    mode=v[0]
    try:
        if mode==0:strings[joined_ptr]='fixture';result=call(0x28e4f98,inventory,v[3],joined_ptr,0,v[7])
        elif mode==1:result=call(0x28e4da0,inventory,v[3])
        elif mode==2:result=call(0x1ca5374,ego)
        elif mode==3:call(0x1ca53e0,ego,v[4])
        elif mode==4:call(0x1ca4d64,ego)
        elif mode==5:call(0x1ca5840,ego,v[4])
        elif mode==6:call(0x1ca58b0,ego)
        elif mode==7:routine=call(0x1ca5794,ego,0 if v[20]==0 else package_addresses[0]if v[1]!=9 else 0)
        elif mode==8:call(0x1ca596c,ego)
        elif mode==9:call(0x1ca5824,ego)
        elif mode==13:result=call(0x1ca5e80,ego)
        elif mode in(10,11,12):
            for register,pointer in((19,dialog),(20,0 if v[1]==30 else winfail),(25,read(0x53649d8,8)),(26,interface_cell)):u.reg_write(getattr(r,'UC_ARM64_REG_X'+str(register)),pointer)
            call({10:0x1346f20,11:0x1347368,12:0x1346f6c}[mode])
        if mode in(7,8)and not fault and routine:
            for _ in range(1000):
                value=call(0x1e29c14,routine)
                if fault or not value:break
                trace.append('yield,'+wait_kind)
                if wait_kind=='g':
                    for _ in range(2):trace.append('poll,'+str(call(0x12de540,wait)))
    except Exception as error:raise AssertionError((v,trace,hex(u.reg_read(r.UC_ARM64_REG_PC))))from error
    if fault:result=-1
    values=[result,si(read(inventory+0x10)),si(read(snapshot+0x24)),si(read(snapshot+0x28)),si(read(ego+0x38)),si(read(ego+0x40)),read(ego+0x44,1),si(read(state+0x18)),si(read(moves+0x10)),si(read(session+0x4c)),si(read(state+0x100))]
    if v[1]==6:values[2:4]=[-99,-98]
    expected.append(str(int(fault))+'#'+','.join(map(str,values))+'#'+';'.join(trace));cases.append(','.join(map(str,v)))
mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
sources=['SourceCatalog/EgoHarness.cs',*['Assets/Foundation/Runtime/'+name+'.cs'for name in('OriginalUserInventoryBalances','OriginalCoinSpending','OriginalEgoPackage','OriginalEgoManager','OriginalGameplayWait','OriginalEgoDialogGameActions')]]
with tempfile.TemporaryDirectory()as directory:
    exe=Path(directory)/'Ego.exe';command=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',*['-r:'+str(mono/'lib/mono/4.5'/p)for p in('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),*[str(PROJECT/p)for p in sources]]
    result=subprocess.run(command,capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    actual=result.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
    for i,(a,b)in enumerate(zip(actual,expected)):assert a==b,(i,cases[i],a,b)
report={'passed':True,'cases':len(cases),'scope':SCOPE,'original_rvas':['0x28e4f98','0x28e4da0','0x1ca4f68','0x1ca5020','0x1ca5374','0x1ca53e0','0x1ca4d64','0x1ca5840','0x1ca58b0','0x1ca596c','0x1ca5794','0x1e29c14','0x12de540','0x12de578','0x1ca5824','0x1ca4edc','0x32b118c','0x1ca5e80'],
 'on_close_core_ranges':['0x1346f20..0x1346f68','0x1347368..0x13473bc','0x1346f6c..0x1346fbc','0x13474b0..0x1347524'],
    'files':[{'path':p,'sha256':hashlib.sha256((PROJECT/p).read_bytes()).hexdigest()}for p in sources]}
(PROJECT/'ego_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
