"""Original match particle emission, seed pairing and native capacity thresholds."""
import sys
sys.dont_write_bytecode=True
import verify_board_layout as b
import json,random,struct,subprocess,tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_LR,UC_ARM64_REG_PC,UC_ARM64_REG_SP,UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2
p=b.p
owner=0x10000000;parts=0x10001000;shadows=0x10002000;shadow_base=0x10003000;ps_base=0x10010000
bits=lambda f:struct.unpack('<I',struct.pack('<f',f))[0]
trace=[];counts=[];maxima=[];seeds=[];playing=[];enabled=1;random_bits=0;error=''
particle=lambda i:ps_base+0x100*i
ident=lambda pointer:(pointer-ps_base)//0x100
point=lambda values:','.join(map(str,values))

def boundary(uc,address,size,unused):
    global error
    x0,x1,x2=[uc.reg_read(r) for r in (UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2)]
    value=None
    if address in (0xffda88,0xffda90):error='null' if address==0xffda88 else 'index';uc.reg_write(UC_ARM64_REG_PC,0x20000000);return
    if address in (0xd00b50,0xd018a0):uc.mem_write(x0,bytes(uc.mem_read(x1,x2)));value=x0
    elif address==0xd01fa0:uc.mem_write(x0,bytes([x1&255])*x2);value=x0
    elif address==0x38dbacc:trace.append('emission:'+str(ident(x0)));value=x0
    elif address==0x38dd2e8:trace.append('enabled:'+str(ident(p.read(x0,8)))+':'+str(enabled));value=enabled
    elif address==0x38db1c4:i=ident(x0);trace.append('playing:'+str(i)+':'+str(playing[i]));value=playing[i]
    elif address==0x362ead8:
        assert uc.reg_read(UC_ARM64_REG_S0)==bits(0.) and uc.reg_read(UC_ARM64_REG_S1)==bits(4294967296.)
        trace.append('random');uc.reg_write(UC_ARM64_REG_S0,random_bits);value=0
    elif address==0x38db300:i=ident(x0);seeds[i]=x1&0xffffffff;trace.append('seed:'+str(i)+':'+str(seeds[i]));value=0
    elif address==0x38dbaa4:trace.append('main:'+str(ident(x0)));value=x0
    elif address==0x38dd1e8:i=ident(p.read(x0,8));trace.append('max:'+str(i)+':'+str(maxima[i]));value=maxima[i]
    elif address==0x38db23c:i=ident(x0);trace.append('count:'+str(i)+':'+str(counts[i]));value=counts[i]
    elif address==0x38dd260:i=ident(p.read(x0,8));maxima[i]=p.signed(x1&0xffffffff);trace.append('set-max:'+str(i)+':'+str(maxima[i]));value=0
    elif address==0x38df524:
        for i,r in enumerate((UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2)):p.put(x0+i*4,uc.reg_read(r))
        value=0
    elif address==0x38df538:p.put(x0+12,x1,1);value=0
    elif address==0x38db9b8:
        i=ident(x0);n=p.signed(x2&0xffffffff);trace.append('emit:'+str(i)+':'+str(n)+':'+str(p.read(x1+12,1))+':'+point([p.read(x1+o) for o in (0,4,8)]))
        counts[i]=min(maxima[i],counts[i]+n);value=0
    elif address==0x2d74718:
        trace.append('shadow:'+str((x0-shadow_base)//0x100)+':'+str(x1)+':'+point([uc.reg_read(r) for r in (UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2)]));value=0
    if value is not None:uc.reg_write(UC_ARM64_REG_X0,value&0xffffffffffffffff);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,boundary)

def call(rva,extra,kind,position):
    for r,v in ((UC_ARM64_REG_X0,owner),(UC_ARM64_REG_X1,extra),(UC_ARM64_REG_X2,kind),(UC_ARM64_REG_SP,0x101ff000),(UC_ARM64_REG_LR,0x20000000)):p.emu.reg_write(r,v&0xffffffffffffffff)
    for r,v in zip((UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2),position):p.emu.reg_write(r,v)
    p.emu.emu_start(rva,0x20000000,count=100000)
    assert p.emu.reg_read(UC_ARM64_REG_PC)==0x20000000

rng=random.Random(0x21a869c);cases=[];expected=[]
for n in range(384):
    p.emu.mem_write(owner,bytes(0x20000));trace=[];error=''
    enabled=int(n%7!=0);extra=n%2;blast=(n//2)%2;kind=[-1,0,1,2,3,4,5,6,7,2147483647][n%10]
    random_bits=bits(rng.choice((0.,1.,12345678.,4294967040.,4294967296.,-1.,-2147483904.,float('nan'),float('inf'))))
    counts=[rng.randrange(0,120) for _ in range(12)];maxima=[rng.choice((0,1,4,10,100,counts[i],counts[i]+1,counts[i]+4,counts[i]+10)) for i in range(12)]
    # Unity's stored particle counts do not exceed its current capacity.
    counts=[min(a,c) for a,c in zip(counts,maxima)];seeds=[0]*12;playing=[int(rng.random()<.5) for _ in range(12)]
    initial_counts=list(counts);initial_max=list(maxima)
    position=[bits(rng.uniform(-10,10)) for _ in range(3)]
    missing=0 if n<336 else (n%6)+1
    p.put(owner+0x30,particle(0) if missing!=1 else 0,8);p.put(owner+0x38,particle(1) if missing!=2 else 0,8)
    p.put(owner+0x40,parts if missing!=3 else 0,8);p.put(owner+0x48,shadows if missing!=4 else 0,8);p.put(owner+0x58,4)
    p.put(parts+0x18,0 if missing==5 else 5);p.put(shadows+0x18,5)
    for i in range(5):p.put(parts+0x20+8*i,particle(i+2),8);p.put(shadows+0x20+8*i,shadow_base+0x100*i if missing!=6 else 0,8);p.put(shadow_base+0x100*i+0x18,particle(i+7),8)
    cases.append(';'.join([','.join(map(str,[enabled,extra,blast,kind,random_bits,missing,*position])),point(initial_counts),point(initial_max),point(playing)]))
    call(0x21a8b88 if blast else 0x21a8618,extra,kind,position)
    expected.append(error+';'+ '|'.join(trace)+';'+point(counts)+';'+point(maxima)+';'+point(seeds))
harness=r'''using System;using System.Collections.Generic;using UnityEngine;using Whitebox.Foundation;
namespace UnityEngine{
public class MonoBehaviour{}public struct Vector3{public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}}
public static class Random{public static float Value;public static float Range(float a,float b){if(a!=0||b!=4294967296f)throw new Exception("Random range");Verify.Trace.Add("random");return Value;}}
public class ParticleSystem{public int Id,Count,Max;public bool Playing,Enabled;public uint Seed;
public bool isPlaying{get{Verify.Trace.Add("playing:"+Id+":"+(Playing?1:0));return Playing;}}public uint randomSeed{set{Seed=value;Verify.Trace.Add("seed:"+Id+":"+value);}}
public EmissionModule emission{get{Verify.Trace.Add("emission:"+Id);return new EmissionModule{P=this};}}public struct EmissionModule{public ParticleSystem P;public bool enabled{get{Verify.Trace.Add("enabled:"+P.Id+":"+(P.Enabled?1:0));return P.Enabled;}}}
public MainModule main{get{Verify.Trace.Add("main:"+Id);return new MainModule{P=this};}}public struct MainModule{public ParticleSystem P;public int maxParticles{get{Verify.Trace.Add("max:"+P.Id+":"+P.Max);return P.Max;}set{P.Max=value;Verify.Trace.Add("set-max:"+P.Id+":"+value);}}}
public int particleCount{get{Verify.Trace.Add("count:"+Id+":"+Count);return Count;}}public struct EmitParams{public Vector3 position;public bool applyShapeToPosition;}
public void Emit(EmitParams emit,int n){Verify.Trace.Add("emit:"+Id+":"+n+":"+(emit.applyShapeToPosition?1:0)+":"+Verify.Point(emit.position));Count=Math.Min(Max,Count+n);}}
}
namespace Whitebox.Foundation{public abstract class OriginalItemParticles:MonoBehaviour{protected virtual void Awake(){}public abstract int GetPoolId();}public class OriginalAutoShadowEmitParticles{public ParticleSystem system;public int Id;public void Init(){throw new Exception("Awake is outside this test");}public void EmitShadows(Vector3 p,int n){Verify.Trace.Add("shadow:"+Id+":"+n+":"+Verify.Point(p));}}}
class Verify{public static List<string> Trace=new List<string>();static float Float(string s){return BitConverter.ToSingle(BitConverter.GetBytes(uint.Parse(s)),0);}static uint Bits(float f){return BitConverter.ToUInt32(BitConverter.GetBytes(f),0);}public static string Point(Vector3 p){return Bits(p.x)+","+Bits(p.y)+","+Bits(p.z);}
static string Values(ParticleSystem[] ps,int field){var result=new string[ps.Length];for(int i=0;i<ps.Length;i++)result[i]=field==0?ps[i].Count.ToString():field==1?ps[i].Max.ToString():ps[i].Seed.ToString();return string.Join(",",result);}
static void Main(){string line;while((line=Console.ReadLine())!=null){Trace.Clear();var parts=line.Split(';');var a=parts[0].Split(',');var counts=Array.ConvertAll(parts[1].Split(','),int.Parse);var maxima=Array.ConvertAll(parts[2].Split(','),int.Parse);var playing=Array.ConvertAll(parts[3].Split(','),int.Parse);var ps=new ParticleSystem[12];for(int i=0;i<12;i++)ps[i]=new ParticleSystem{Id=i,Count=counts[i],Max=maxima[i],Playing=playing[i]==1,Enabled=a[0]=="1"};int missing=int.Parse(a[5]);var effect=new OriginalMatchExplodeParticles{lightRing=missing==1?null:ps[0],blastDots=missing==2?null:ps[1],blastParts=missing==3?null:new ParticleSystem[missing==5?0:5],shadowParticles=missing==4?null:new OriginalAutoShadowEmitParticles[5]};for(int i=0;i<5;i++){if(effect.blastParts!=null&&effect.blastParts.Length>i)effect.blastParts[i]=ps[i+2];if(effect.shadowParticles!=null)effect.shadowParticles[i]=missing==6?null:new OriginalAutoShadowEmitParticles{Id=i,system=ps[i+7]};}Random.Value=Float(a[4]);var position=new Vector3(Float(a[6]),Float(a[7]),Float(a[8]));string error="";try{if(a[2]=="1")effect.PlayBlastParticles(position,a[1]=="1",int.Parse(a[3]));else effect.Play(position,a[1]=="1",int.Parse(a[3]));}catch(NullReferenceException){error="null";}catch(IndexOutOfRangeException){error="index";}Console.WriteLine(error+";"+string.Join("|",Trace)+";"+Values(ps,0)+";"+Values(ps,1)+";"+Values(ps,2));}}}'''
harness=harness.replace('Random.Value','UnityEngine.Random.Value')
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder);source=folder/'Verify.cs';source.write_text(harness);exe=folder/'Verify.exe'
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),str(p.PROJECT/'Assets/Foundation/Runtime/OriginalMatchExplodeParticles.cs')],capture_output=True,text=True,encoding='utf-8')
    assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8')
    assert result.returncode==0,result.stdout+result.stderr
    actual=result.stdout.replace('\ufeff','').splitlines()
    assert len(actual)==len(expected),(len(actual),len(expected))
    assert actual==expected,next(((i,cases[i],a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e),'count mismatch')
report={'passed':True,'native_playback_cases':384,'original_rvas':['0x21a8618','0x21a869c','0x21a8b88','0x21a8b5c'],
 'scope':'Full native Play and PlayBlastParticles control flow versus production component with supplied Unity particle-module/array storage, application random values and shadow-emission boundary. Compares every module/count read, capacity change, shape/position/count emission, paired seed, disabled emission and null/index fault path. No Unity engine particle simulation, Awake shadow-clone lifecycle, fade-out or global particle manager/pool behavior is asserted.'}
(p.PROJECT/'match_particles_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
