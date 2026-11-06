"""Execute original packed RestorableData and session snapshot operations."""
import sys
sys.dont_write_bytecode=True
import json,os,random,subprocess,tempfile
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_move_budget.py').read_text().split('cases=[];expected=[]')[0],str(PROJECT/'SourceCatalog/verify_move_budget.py'),'exec'))
emu.hook_del(native_hook);base_hook=hook
before,special,raw,owner,klass_before,klass_special,klass_owner,table=[0x1001a000+i*0x400 for i in range(8)]
for p,k in((before,klass_before),(special,klass_special),(owner,klass_owner)):put(p,k,8)
put(klass_special+0x1b8,0x27b3f48,8)
lengths=[];signed_flags=''
def values_hook(uc,address,size,unused):
    x0,x1,x2=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(3)]
    if address==0xfcd934:
        assert x2 in(2,3),x2
        target=(0x338c800 if x2==2 else 0x338c82c)if x0==before else(0x27b3f48 if x2==2 else 0x27b3f74)if x0==special else(0x20000030 if x2==2 else 0x20000040)
        put(table,target,8);returned(uc,table)
    elif address==0x20000030:returned(uc,lengths[x1]if x1<len(lengths)else 0)
    elif address==0x20000040:returned(uc,x1<len(signed_flags)and signed_flags[x1]=='1')
    elif address==0x231007c:returned(uc)
    else:base_hook(uc,address,size,unused)
emu.hook_add(UC_HOOK_CODE,values_hook)
def wide(rva,*args):
    call(rva,*args);v=emu.reg_read(UC_ARM64_REG_X0);return v-(1<<64)if v>>63 else v
cases=[];expected=[];commands=0
def run(kind,lens,signs,steps):
    global lengths,signed_flags,next_object,commands
    lengths=lens;signed_flags=signs;next_object=0x10030000
    emu.mem_write(before+0x10,bytes(0x30));emu.mem_write(special+0x10,bytes(0x40));emu.mem_write(raw,bytes(0x20))
    call(0x338adc4,before);call(0x27b310c,special);call(0x23e5778,raw,0,owner)
    saved=0;out=[]
    for step in steps:
        commands+=1;f=step.split(',');mode=f[0];i=int(f[1])if len(f)>1 and not(kind=='r'and mode=='a')else 0;v=int(f[2])if len(f)>2 else 0;result='-'
        if kind=='r':
            if mode=='c':call(0x23e5778,raw,i,owner)
            elif mode=='l':call(0x23e5d18,raw,v);saved=v
            elif mode in('w','s','u'):call({'w':0x23e5804,'s':0x23e5998,'u':0x23e59f0}[mode],raw,i,v,int(f[3])if len(f)>3 else 0)
            elif mode in('g','o'):result=str(wide(0x23e5b20,raw,i)if mode=='g'else call(0x23e5a34,raw,i))
            elif mode=='a':
                p=0
                if len(f)>1:
                    a=list(map(int,f[1].split('/')));p=allocate(0x20+len(a)*8);put(p+0x18,len(a))
                    for k,n in enumerate(a):put(p+0x20+8*k,n,8)
                result=str(wide(0x23e578c,raw,p));saved=result if not fault else saved
        elif kind=='b':
            if mode=='d':put(before+0x20,i,1);put(before+0x24,v);put(before+0x28,int(f[3]))
            elif mode=='l':call(0x338c7a8,before,v)
            elif mode=='g':saved=wide(0x338c714,before);result=str(saved)
            elif mode=='q':result=str(call(0x338c6e4,before))
            else:call({'i':0x338c6a8,'a':0x338b130,'u':0x338c6c0,'c':0x338c6dc,'f':0x338c6b4}[mode],before,i)
        else:
            if mode=='d':
                for k,n in enumerate(map(int,f[1:])):put(special+0x10+4*k,n)
            elif mode=='g':saved=wide(0x27b3b74,special);result=str(saved)
            elif mode=='l':call(0x27b3ebc,special,v)
        if fault:result='error'
        fields=','.join(map(str,(read(raw,8)&((1<<63)-1),0)))if kind=='r'else ','.join(map(str,(read(before+0x20,1),signed(read(before+0x24)),signed(read(before+0x28)))))if kind=='b'else ','.join(str(signed(read(special+0x10+4*k)))for k in range(5))
        out.append(result+':'+fields+':'+str(saved))
    cases.append(kind+'#'+','.join(map(str,lens))+'#'+signs+'#'+';'.join(steps));expected.append('|'.join(out))
edges=[-2147483648,-1048577,-1,0,1,4095,4096,8191,8192,1048575,1048576,2147483647]
for n in edges:
    run('b',[1,20,20],'000',[f'i,{n}','u,1','a,0','g','a,-1','g','q',f'u,{n}','q','c','u,5','g','f','g',f'd,1,{n},{n}','g','l,0,-1','g'])
for a in edges:
    for b in(0,4095,4096,8191,8192,-1):run('s',[13,13,12,12,12],'00000',[f'd,{a},{b},{a},{b},{a}','g','l,0,-1','g','l,0,0','g'])
for lens,signs in(([1,20,20],'000'),([13,13,12,12,12],'00000'),([1,12,13,20],'1111'),([0,-1,64,65,2],'10101')):
    for n in(-9223372036854775808,-1,0,1,9223372036854775807):
        steps=['c,-1','g,0','l,0,0']
        for i in range(len(lens)):steps +=[f'w,{i},{n}',f'g,{i}',f'o,{i}',f's,{i},{n},{lens[i]}',f'u,{i},{n},{lens[i]}']
        steps+=['a','g,0','a,1/-1/9223372036854775807','g,1'];run('r',lens,signs,steps)
rng=random.Random(0x23e5778)
for i in range(55):
    lens=[rng.randrange(1,22)for _ in range(4)];signs=''.join(str(rng.randrange(2))for _ in lens)
    steps=['l,0,'+str(rng.randrange(-2**63,2**63))]
    for j in range(12):
        k=rng.randrange(4);steps +=[f'w,{k},{rng.randrange(-2**63,2**63)}',f'g,{k}']
    run('r',lens,signs,steps)
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-session-values-')as directory:
    exe=Path(directory)/'verify.exe';files=('OriginalSessionValues','OriginalLevelSessionCounters')
    p=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-langversion:latest','-out:'+str(exe),str(PROJECT/'SourceCatalog/SessionValuesHarness.cs'),*[str(PROJECT/'Assets/Foundation/Runtime'/(n+'.cs'))for n in files]],capture_output=True,text=True);assert p.returncode==0,p.stdout+p.stderr
    p=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert p.returncode==0,p.stderr[-1800:]
    actual=p.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
    for i,(a,e)in enumerate(zip(actual,expected)):
        if a!=e:
            j=next(j for j,(x,y)in enumerate(zip(a.split('|'),e.split('|')))if x!=y)
            raise AssertionError((i,j,cases[i].split('#')[:3],cases[i].split('#')[3].split(';')[j],a.split('|')[j],e.split('|')[j]))
report={'passed':True,'native_sequences':len(cases),'native_commands':commands,'scope':'Actual ARM64 RestorableData constructor, PrepareData, SetData/GetData, signed magnitude and unsigned helpers, offset and raw replacement; BeforeAfterData constructor, Init, FullReset, Update, AddExtra, Consume, collected amount, packed save/load; SpecialItemCount constructor and packed save/load with original upper-only clamps and short casts. Covers signed long/int boundaries, 64-bit wrapping shift counts, OR accumulation, null PrepareData clearing before failure, 1/20/20 and 13/13/12/12/12 native bit layouts and randomized layouts. CLR allocation and interface resolution are supplied; generic owner length/sign methods use explicit fixture inputs. Full application save/restore and event-derived snapshots remain pending.'}
(PROJECT/'session_values_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
