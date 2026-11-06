"""Compare original shadow rotation/scaling and per-frame color updates."""
import sys
sys.dont_write_bytecode=True
import verify_board_layout as b
import json,math,random,struct,subprocess,tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2,UC_ARM64_REG_LR,UC_ARM64_REG_PC,UC_ARM64_REG_SP,UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2,UC_ARM64_REG_D0
p=b.p
owner=0x10000000;ps=0x10001000;transform=0x10002000;array=0x10003000;zero=0x10004000
bits=lambda f:struct.unpack('<I',struct.pack('<f',f))[0]
number=lambda n:struct.unpack('<f',struct.pack('<I',n))[0]
normal=lambda n:'nan' if math.isnan(number(n)) else str(n)
angles=[];scale=[];color=0;sim_space=0;mode=0;gets=sets=0

def boundary(uc,address,size,unused):
    global color,gets,sets
    x0,x1,x2=[uc.reg_read(r) for r in (UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2)]
    value=None
    if address==0x3e4737c:assert x0==ps;value=transform
    elif address in (0x3e4e538,0x3e4e958,0x3e4f9b4):
        assert x0==transform
        for r,v in zip((UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2),angles if address==0x3e4e538 else scale):uc.reg_write(r,v)
        value=0
    elif address==0x38dbaa4:assert x0==ps;value=ps
    elif address==0x38dcfe8:assert p.read(x0,8)==ps;value=sim_space
    elif address==0x38dd170:assert p.read(x0,8)==ps;value=mode
    elif address==0xd021c0:
        a,c=[number(uc.reg_read(r)) for r in (UC_ARM64_REG_S0,UC_ARM64_REG_S1)]
        uc.reg_write(UC_ARM64_REG_S0,bits(math.atan2(a,c)));value=0
    elif address==0xd01bd0:
        angle=number(uc.reg_read(UC_ARM64_REG_S0));p.put(x0,bits(math.sin(angle)));p.put(x1,bits(math.cos(angle)));value=0
    elif address==0x38db4f0:assert x0==ps and x1==array;gets+=1;value=1
    elif address==0x38df464:assert x0==array+0x20;value=color
    elif address==0x38dbd74:assert x0==array+0x20;color=x1&0xffffffff;value=0
    elif address==0x38db3e4:assert (x0,x1,x2)==(ps,array,1);sets+=1;value=0
    elif address==0xd019c0:
        v=struct.unpack('<d',struct.pack('<Q',uc.reg_read(UC_ARM64_REG_D0)))[0]
        fraction,integral=math.modf(v);uc.mem_write(x0,struct.pack('<d',integral));uc.reg_write(UC_ARM64_REG_D0,struct.unpack('<Q',struct.pack('<d',fraction))[0]);value=0
    if value is not None:uc.reg_write(UC_ARM64_REG_X0,value);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,boundary)

def call(rva,position=None):
    for r,v in ((UC_ARM64_REG_X0,owner),(UC_ARM64_REG_X1,0),(UC_ARM64_REG_X2,0),(UC_ARM64_REG_SP,0x101ff000),(UC_ARM64_REG_LR,0x20000000)):p.emu.reg_write(r,v)
    if position is not None:
        for r,v in zip((UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2),position):p.emu.reg_write(r,v)
    p.emu.emu_start(rva,0x20000000,count=100000)
    assert p.emu.reg_read(UC_ARM64_REG_PC)==0x20000000
    return [p.emu.reg_read(r) for r in (UC_ARM64_REG_S0,UC_ARM64_REG_S1,UC_ARM64_REG_S2)]

p.emu.mem_write(owner,bytes(0x8000));p.put(owner+0x18,ps,8);p.put(owner+0x30,array,8);p.put(array+0x18,1)
slot=0x5595e08
if slot not in b.resolved_slots:p.emu.reg_write(UC_ARM64_REG_X0,slot);b.boundary(p.emu,0xffd960,4,None)
p.put(p.read(slot,8)+0xb8,zero,8)
rng=random.Random(0x2d74bcc);cases=[];expected=[]
for i in range(240):
    position=[bits(rng.uniform(-12,12)) for _ in range(3)]
    angles=[bits(rng.choice((0,30,90,180,270,360,-30,1e-20))) for _ in range(3)]
    scale=[bits(rng.choice((.5,1,1.3,-2,3))) for _ in range(3)]
    sim_space=i%3;mode=(i//3)%3
    result=call(0x2d74804,position);result=call(0x2d74a10,result)
    cases.append('P;'+','.join(map(str,[sim_space,mode,*position,*angles,*scale])))
    expected.append(','.join(normal(v) for v in result))
for i in range(320):
    alpha=bits(rng.choice((.02,.05,.1,.5,0,-.1,1.,float('nan'),float('inf'))))
    color=rng.getrandbits(32) if i%3 else 0xff223344
    p.put(owner+0x20,alpha);initial=color;gets=sets=0
    for frame in range(16):
        call(0x2d74bcc)
        expected.append(str(color))
    assert gets==sets==16
    cases.append('C;'+str(alpha)+','+str(initial))
harness=r'''using System;using Whitebox.Foundation;using UnityEngine;
namespace UnityEngine{public struct Vector3{public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}}public struct Color32{public byte r,g,b,a;public Color32(byte r,byte g,byte b,byte a){this.r=r;this.g=g;this.b=b;this.a=a;}}}
class Verify{static float Float(string s){return BitConverter.ToSingle(BitConverter.GetBytes(uint.Parse(s)),0);}static string Bits(float v){return float.IsNaN(v)?"nan":BitConverter.ToUInt32(BitConverter.GetBytes(v),0).ToString();}
static Vector3 Point(string[] a,int i){return new Vector3(Float(a[i]),Float(a[i+1]),Float(a[i+2]));}static void Main(){string line;while((line=Console.ReadLine())!=null){var p=line.Split(';');var a=p[1].Split(',');
if(p[0]=="P"){var math=new OriginalParticleShadowMath();var position=Point(a,2);if(a[0]=="0")position=math.ArrangeRotation(position,Point(a,5));if(a[0]!="1"&&(a[1]=="0"||a[1]=="1"))position=math.ArrangeScale(position,Point(a,8));Console.WriteLine(Bits(position.x)+","+Bits(position.y)+","+Bits(position.z));}
else{uint n=uint.Parse(a[1]);var color=new Color32((byte)n,(byte)(n>>8),(byte)(n>>16),(byte)(n>>24));float alpha=Float(a[0]);for(int i=0;i<16;i++){color=OriginalParticleShadowMath.UpdateColor(color,alpha);Console.WriteLine((uint)color.r|((uint)color.g<<8)|((uint)color.b<<16)|((uint)color.a<<24));}}}}}'''
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder);source=folder/'Verify.cs';source.write_text(harness);exe=folder/'Verify.exe'
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),str(p.PROJECT/'Assets/Foundation/Runtime/OriginalParticleShadowMath.cs')],capture_output=True,text=True,encoding='utf-8')
    assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8')
    assert result.returncode==0,result.stdout+result.stderr
    actual=result.stdout.replace('\ufeff','').splitlines()
    assert len(actual)==len(expected),(len(actual),len(expected))
    assert actual==expected,next(((i,a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e),'count mismatch')
report={'passed':True,'native_position_cases':240,'native_color_sequences':320,'native_color_frame_comparisons':5120,
 'original_rvas':['0x2d74804','0x2d74a10','0x2d74bcc'],
 'scope':'Original ARM64 rotation order/sign, scale mode selection, finite vector float bits and per-frame shadow color versus production math. Color cases include NaN/infinite/zero/negative alpha and unchanged over-ten-step colors. Native host atan2/sincos/modf and Unity transform/module/color storage are supplied boundaries. Clone initialization, renderer/material output and Unity particle simulation are not covered by this report.'}
(p.PROJECT/'particle_shadows_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
