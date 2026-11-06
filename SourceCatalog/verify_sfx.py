"""Execute actual native one-shot throttling, random dispatch and box clip cases."""
import sys
sys.dont_write_bytecode=True
import hashlib,json,os,random,re,struct,subprocess,tempfile
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
# Reuse only the ELF mapping/relocations and byte helpers, without running its
# useful-state cases. The executable source is always the original ARM64 ELF.
loader=(PROJECT/'SourceCatalog/verify_useful_move.py').read_text().split('active, king,')[0]
exec(compile(loader,str(PROJECT/'SourceCatalog/verify_useful_move.py'),'exec'))
audio,game,dictionary,source,box,library,clip_array,alt_dictionary=[0x10000000+i*0x1000 for i in range(8)]
put(audio+0x78,dictionary,8);put(box+0x28,clip_array,8);put(box+0x78,alt_dictionary,8)
put(clip_array+0x18,2);put(0x10008040,library,8)
trace=[];times={};fault=False;now=0;resolve_id=0;clip_id=0;current_type=0;random_value=0;float_value=0;alt_mask=0
metadata_next=0x10090000
def bits(v):return format(v&0xffffffff,'08x')
def float_bits(v):return struct.unpack('<I',struct.pack('<f',v))[0]
def clip_pointer(ident):return 0 if ident==0 else 0x10050000+(ident&0xff)*0x100
def pointer_id(pointer):return 0 if pointer==0 else (pointer-0x10050000)//0x100
def returned(uc,value=0):
    uc.reg_write(UC_ARM64_REG_X0,value&0xffffffffffffffff);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
def hook(uc,address,size,unused):
    global fault,metadata_next
    x0,x1,x2,x3=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(4)]
    typ=signed(x1&0xffffffff)
    if address==0xffd960:
        if not 0x10000000<=read(x0,8)<0x10200000:
            put(x0,metadata_next,8);put(metadata_next+0xe0,1);metadata_next+=0x200
        returned(uc)
    elif address in (0xffda64,0xffd90c):returned(uc)
    elif address in (0xffda88,0xffda90):fault=True;uc.reg_write(UC_ARM64_REG_PC,0x20000000)
    elif address==0x3c9fea4:returned(uc,int(typ in times))
    elif address==0x3c9fbf0:uc.reg_write(UC_ARM64_REG_S0,times[typ]);returned(uc)
    elif address==0x3c9fc88:times[typ]=uc.reg_read(UC_ARM64_REG_S0);returned(uc)
    elif address==0x3ca172c:put(x2,times.get(typ,0));returned(uc,int(typ in times))
    elif address==0x3e4249c:
        trace.append('time,'+bits(now));uc.reg_write(UC_ARM64_REG_S0,now);returned(uc)
    elif address==0x3e12aa4:trace.append('load,'+str(signed(x0&0xffffffff)));returned(uc,clip_pointer(resolve_id))
    elif address==0x3e3b754:returned(uc,int(x0==0 or pointer_id(x0)==255))
    elif address in (0x3e17920,0x3e17adc):
        # Only diagnostic formatting/logging is a boundary. The executed code
        # has already committed its timestamp and taken Unity null equality.
        trace.append('missing,'+str(current_type));uc.reg_write(UC_ARM64_REG_PC,0x3e179c0 if address==0x3e17920 else 0x3e17b7c)
    elif address==0x1d64940:trace.append('play,'+str(pointer_id(x1)));returned(uc)
    elif address==0x362eb18:
        trace.append('int,'+str(signed(x0&0xffffffff))+','+str(signed(x1&0xffffffff)));returned(uc,random_value)
    elif address==0x362ead8:
        trace.append('float,'+bits(uc.reg_read(UC_ARM64_REG_S0))+','+bits(uc.reg_read(UC_ARM64_REG_S1)))
        uc.reg_write(UC_ARM64_REG_S0,float_value);returned(uc)
    elif address==0x260a0a4:assert x0==library;returned(uc,box)
    elif address==0x3c9e3f0:
        types=(34,35,37,38);ident=typ if typ in types and alt_mask&(1<<types.index(typ)) else 0
        put(x2,clip_pointer(ident),8);returned(uc,int(ident!=0))
emu.hook_add(UC_HOOK_CODE,hook)
def call(rva,*args):
    global fault
    fault=False
    for i,arg in enumerate((*args,0,0,0)):emu.reg_write(UC_ARM64_REG_X0+i,arg&0xffffffffffffffff)
    emu.reg_write(UC_ARM64_REG_SP,0x101ff000);emu.reg_write(UC_ARM64_REG_LR,0x20000000)
    try:emu.emu_start(rva,0x20000000,count=200000)
    except Exception:
        print('Native fault',hex(emu.reg_read(UC_ARM64_REG_PC)),trace);raise
    assert emu.reg_read(UC_ARM64_REG_PC)==0x20000000
    return signed(emu.reg_read(UC_ARM64_REG_X0)&0xffffffff)

cases=[];expected=[];commands=0
def run(head,rows):
    global now,resolve_id,clip_id,current_type,commands,random_value
    times.clear();put(audio+0x48,head[0],1);put(audio+0xa1,head[1],1)
    put(game+0x24,head[2],1);put(audio+0x58,source if head[3] else 0,8);put(game+0x10,audio if head[4] else 0,8)
    put(game+0x18,0x10009000 if len(head)>5 and head[5]else 0,8)
    output=[]
    for mode,typ,time_value,offset,given,resolved in rows:
        commands+=1;trace.clear();now=time_value;resolve_id=resolved;clip_id=given;current_type=typ
        emu.reg_write(UC_ARM64_REG_S0,offset)
        rva={'c':0x3e17770,'p':0x3e17838,'e':0x3e17a04,'g':0x21805c0,'h':0x21805ec,'f':0x218022c,'l':0x3e18f78}[mode]
        if mode=='f':random_value=given
        value=call(rva,game if mode in 'ghf' else audio,typ,clip_pointer(given))
        result='error' if fault else str(value)if mode=='c'else bits(emu.reg_read(UC_ARM64_REG_S0))if mode=='l'else'-'
        output.append(result+':'+ ';'.join(trace))
    cases.append(','.join(map(str,head))+'#'+';'.join(','.join((m,str(t),bits(tm),bits(of),str(g),str(r)))for m,t,tm,of,g,r in rows));expected.append('|'.join(output))

f=float_bits
for flags in range(32):
    for clip in (0,-1,33):
        rows=[]
        for mode in ('p','g','e','h'):
            for moment in (0,.02,.039999,.04,.08,.12):
                rows.append((mode,33,f(moment),f(.04),clip,clip))
        run([flags&1,(flags>>1)&1,(flags>>2)&1,(flags>>3)&1,(flags>>4)&1],rows)
rng=random.Random(17838)
for seed in range(160):
    run([rng.randrange(2)for _ in range(5)],[(rng.choice('cpegh'),rng.choice((33,34,35,36,37,38,-1,2147483647)),rng.choice([f(v)for v in (-1,0,.01,.04,1,float('nan'),float('inf'))]),rng.choice([f(v)for v in (-.1,0,.04,1,float('nan'))]),rng.choice((0,-1,33,38)),rng.choice((0,-1,34,37)))for i in range(20)])
audio_cases=len(cases);audio_commands=commands
for flags in range(64):
 for choice in(-1,0,1,2):
  head=[(flags>>i)&1 for i in range(6)];rows=[]
  for moment in(0,.099999,.1,.100001,.14,.2,.200002,float('nan'),float('inf')):
   rows.extend((('f',146,f(moment),f(0),choice,46),('l',146,f(moment),f(0),0,0),('l',147,f(moment),f(0),0,0)))
  run(head,rows)
for typ in(146,147):
 for moment in(0,.05,.1,1,-1,float('nan')):
  for after in(.1,.100001,.15,1.05,1.100001,float('nan')):
   run([0,0,0,1,1,1],[('p',typ,f(moment),f(0),0,46),('f',146,f(after),f(0),1,47),('l',typ,f(after),f(0),0,0)])
fall_cases=len(cases)-audio_cases;fall_commands=commands-audio_commands
fall_array=0x1000a000;put(0x10008018,fall_array,8);put(fall_array+0x18,2)
for typ in(146,147):
 for first in(0,46):
  for second in(0,47):
   put(fall_array+0x20,clip_pointer(first),8);put(fall_array+0x28,clip_pointer(second),8);call(0x1ee9054,0x10008000,typ);assert not fault
   cases.append('F#'+','.join(map(str,(typ,first,second))));expected.append(str(pointer_id(emu.reg_read(UC_ARM64_REG_X0))))
for mode in 'ixbf':
    for i in range(45):
        lo,hi=rng.randint(-50,50),rng.randint(-50,50);random_value=rng.randrange(2);float_value=f(rng.uniform(-20,20));trace.clear()
        if mode=='f':
            lo,hi=bits(f(lo*.25)),bits(f(hi*.5));emu.reg_write(UC_ARM64_REG_S0,int(lo,16));emu.reg_write(UC_ARM64_REG_S1,int(hi,16));call(0x2d70af8,0)
            result=bits(emu.reg_read(UC_ARM64_REG_S0))
        else:
            value=call({'i':0x2d70ac4,'x':0x2d70ab8,'b':0x2d70ad4}[mode],0,hi if mode=='x'else lo,hi);result=str(value)
        cases.append('R#'+','.join((mode,str(lo),str(hi),str(random_value),bits(float_value))));expected.append(result+':'+ ';'.join(trace))
for typ in range(33,40):
    for alt_mask in range(16):
        for original in (0,1):
            put(clip_array+0x20,clip_pointer(original),8);put(clip_array+0x28,clip_pointer(original+1),8)
            value=call(0x1ee9054,0x10008000,typ)
            assert not fault
            cases.append('B#'+','.join(map(str,(typ,original,original+1,alt_mask))));expected.append(str(pointer_id(emu.reg_read(UC_ARM64_REG_X0))))
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-sfx-')as directory:
    exe=Path(directory)/'verify.exe'
    names=('OriginalSfxPlayback','OriginalApplicationRandom','OriginalLevelSeedRandom','OriginalBoxViewAssets','OriginalGameSfxAssets')
    compiled=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(PROJECT/'SourceCatalog/SfxHarness.cs'),*[str(PROJECT/'Assets/Foundation/Runtime'/(n+'.cs'))for n in names]],capture_output=True,text=True)
    assert compiled.returncode==0,compiled.stdout+compiled.stderr
    completed=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8')
    assert completed.returncode==0,completed.stdout+completed.stderr
    actual=completed.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
    for i,(a,e)in enumerate(zip(actual,expected)):assert a==e,(i,cases[i],a,e)
report={'passed':True,'native_audio_sequences':audio_cases,'native_audio_commands':audio_commands,'native_fall_sequences':fall_cases,'native_fall_commands':fall_commands,'native_fall_clip_cases':8,'native_random_dispatch_cases':180,'native_box_clip_cases':224,'scope':'Actual ARM64 AudioManager CanPlayAudio, GetClipLastPlayedTime and both ordinary PlaySound overloads through GameSfx PlaySfx and PlayFallHit. Compares ordered time/random/clip/play/log traces, both fall-clip timestamp gates at .1 seconds, .04 playback throttle, mute/override/no-game/source/audio/random flags, exact edges, NaN/infinity, null/destroyed clips. Actual application RandomManager dispatch and GameSfxAssets fall/box cases; dictionary storage, metadata, item-library lookup, Resources/bundle transport, Unity time/object-null/PlayOneShot and diagnostic logging are supplied boundaries. Native audio preference/context/music/looping/download lifecycle and full player loop remain pending.'}
paths=re.findall(r'(?:  - |    Path: )(Recovered/(?:AudioClip|AssetBundles)/[^\r\n]+)',(PROJECT/'Assets/Foundation/Settings/BoxViewAssets.asset').read_text())
assert len(paths)==6
paths+=['Recovered/AudioClip/fall_hit_1','Recovered/AudioClip/fall_hit_2']
report['original_resource_sha256']={}
for path in paths:
    recovered=PROJECT/'Assets/Resources'/(path+'.wav')
    original=RECOVERY/'assets/RecoveredProject/ExportedProject/Assets'/(path.removeprefix('Recovered/')+'.wav')
    digest=hashlib.sha256(recovered.read_bytes()).hexdigest()
    assert digest==hashlib.sha256(original.read_bytes()).hexdigest(),path
    report['original_resource_sha256'][path]=digest
(PROJECT/'sfx_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report))
