"""Run original ARM64 match-sound dispatch, counter and resource selection."""
import sys
sys.dont_write_bytecode=True
import hashlib,json,os,subprocess,tempfile
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_sfx.py').read_text().split('cases=[];expected=[];commands=0')[0],str(PROJECT/'SourceCatalog/verify_sfx.py'),'exec'))
assets,arr,owner,rnd,vtable,klass,static=[0x10020000+i*0x1000 for i in range(7)]
slot=read(0x53b30b8,8);put(slot,klass,8);put(klass+0xe0,1);put(klass+0xb8,static,8)
put(assets+0x48,owner,8);put(rnd,vtable,8);put(vtable+0x198,0x2d70ac4,8)
cases=[];expected=[]
for flags in range(32):
 for swap in (0,1):
  for counter in (-2147483648,-2,-1,0,4,5,6,2147483647):
   for count in (0,1,6):
    for choice in (-1,0,5,6,2147483647):
     times.clear();trace.clear();random_value=choice;resolve_id=1;now=float_bits(1);current_type=0
     put(static,0 if flags&1 else assets,8);put(assets+0x20,0 if flags&2 else arr,8);put(arr+0x18,count)
     put(owner+0x30,0 if flags&8 else rnd,8);put(game+0x10,0 if flags&4 else audio,8)
     put(game+0x20,counter);put(game+0x24,bool(flags&16),1);put(audio+0x58,source,8);put(audio+0x48,0,1);put(audio+0xa1,0,1)
     call(0x2180304,game,swap)
     cases.append(','.join(map(str,(flags,swap,counter,count,choice))))
     expected.append(('error'if fault else'-')+','+str(signed(read(game+0x20)))+':'+ ';'.join(trace))
# Actual GetClip jump table: six match array entries and swipe field.
for index,typ in enumerate((*range(247,253),254)):
 for i in range(6):put(arr+0x20+i*8,clip_pointer(i+1),8)
 put(arr+0x18,6);put(assets+0x20,arr,8);put(assets+0x28,clip_pointer(7),8)
 call(0x1ee9054,assets,typ);assert not fault
 cases.append('-1,'+str(typ));expected.append(str(pointer_id(emu.reg_read(UC_ARM64_REG_X0))))
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-match-sfx-') as directory:
 exe=Path(directory)/'verify.exe'
 sources=[PROJECT/'SourceCatalog/MatchSfxHarness.cs']+[PROJECT/'Assets/Foundation/Runtime'/(n+'.cs')for n in ('OriginalSfxPlayback','OriginalApplicationRandom','OriginalLevelSeedRandom','OriginalGameSfxAssets')]
 result=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),*map(str,sources)],capture_output=True,text=True)
 assert result.returncode==0,result.stdout+result.stderr
 result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8-sig')
 assert result.returncode==0,result.stdout+result.stderr
 actual=result.stdout.splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
 for i,(a,e)in enumerate(zip(actual,expected)):assert a==e,(i,cases[i],a,e)
report={'passed':True,'native_dispatch_cases':len(cases)-7,'native_clip_cases':7,'scope':'Actual ARM64 PlayMatchExplode, GetRandomSfxTypeForMatchExplode, RandomManager dispatch, AudioManager ordinary one-shot path and GetClip match/swipe jump-table cases. Compares counter mutation, overflow/clamping, missing global assets/array/audio/random, no-game mute, swap bypass of array count, invalid random output and ordered time/random/load/play traces. CLR metadata/storage, cached random-owner lookup, Unity random/time/object-null/playback and resource transport are supplied boundaries. Full native audio/context lifecycle remains pending.','resource_sha256':{}}
for name in [f'match_explode{i}'for i in range(1,7)]+['match_item_swipe']:
 original=RECOVERY/'assets/RecoveredProject/ExportedProject/Assets/AudioClip'/(name+'.wav')
 recovered=PROJECT/'Assets/Resources/Recovered/AudioClip'/(name+'.wav')
 digest=hashlib.sha256(original.read_bytes()).hexdigest();assert digest==hashlib.sha256(recovered.read_bytes()).hexdigest()
 report['resource_sha256'][name]=digest
(PROJECT/'match_sfx_native_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(json.dumps(report))
