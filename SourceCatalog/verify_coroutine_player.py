"""Native CoroutinePlayer and RoyalCoroutine lifecycle versus production C#."""
import sys
sys.dont_write_bytecode=True
import verify_board_layout as b
import json,random,struct,subprocess,tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import *
p=b.p
player,mono,wrapper,inner,klass,iface,storage,array,complete,killed,handle,token=[0x10010000+i*0x1000 for i in range(12)]
MOVE,CURRENT,COMPLETE,KILLED,AFTER_KILL=[0x20000100+i*4 for i in range(5)]
trace=[];heap=0x10100000;iterator=0;wait=0;inside=0;bridge_return=0;bridge_value=0
bits=lambda f:struct.unpack('<I',struct.pack('<f',f))[0]

def alloc():
    global heap
    value=heap;heap+=0x100;p.emu.mem_write(value,bytes(0x100));return value
def boundary(uc,address,size,unused):
    global iterator,wait,bridge_return,bridge_value
    x0,x1=uc.reg_read(UC_ARM64_REG_X0),uc.reg_read(UC_ARM64_REG_X1);value=None
    if address==0xffda78:value=alloc()
    elif address==0x3e4c364:
        assert x0==mono;iterator=x1;trace.append('host-start');value=handle
    elif address==0x3e4c5e0:assert x0==mono and x1==handle;trace.append('host-stop');value=0
    elif address==0x2c1b9a0:
        assert x0==storage and x1<p.read(storage+0x18);value=p.read(array+0x20+x1*8,8)
    elif address==0x2c1d0e4:
        assert x0==storage and x1==wrapper
        value=int(p.read(storage+0x18)>0)
        if value:p.put(storage+0x18,p.read(storage+0x18)-1)
    elif address==MOVE:
        assert x0==inner;trace.append('inner:'+str(inside))
        if inside in (2,3):
            bridge_return=uc.reg_read(UC_ARM64_REG_LR);bridge_value=int(inside==2)
            uc.reg_write(UC_ARM64_REG_LR,AFTER_KILL);uc.reg_write(UC_ARM64_REG_X0,wrapper)
            uc.reg_write(UC_ARM64_REG_PC,0x2d7cd20);return
        value=int(inside==1)
    elif address==AFTER_KILL:
        uc.reg_write(UC_ARM64_REG_LR,bridge_return);value=bridge_value
    elif address==CURRENT:assert x0==inner;trace.append('current');value=token
    elif address==COMPLETE:trace.append('complete');value=0
    elif address==KILLED:trace.append('kill');value=0
    elif address==0x3e49528:wait=x0;trace.append('wait:'+str(uc.reg_read(UC_ARM64_REG_S0)));value=0
    elif address in (0xffd90c,0x231007c):value=0
    if value is not None:uc.reg_write(UC_ARM64_REG_X0,value);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,boundary)

def call(rva,*args):
    for r,v in zip((UC_ARM64_REG_X0,UC_ARM64_REG_X1,UC_ARM64_REG_X2),args+(0,)*(3-len(args))):p.emu.reg_write(r,v)
    p.emu.reg_write(UC_ARM64_REG_SP,0x101ff000);p.emu.reg_write(UC_ARM64_REG_LR,0x20000000)
    p.emu.emu_start(rva,0x20000000,count=100000)
    assert p.emu.reg_read(UC_ARM64_REG_PC)==0x20000000
    return p.emu.reg_read(UC_ARM64_REG_X0)

def snapshot(moved):
    cur=p.read(iterator+0x18,8)
    kind='-' if cur==0 else 'w' if cur==wait else 'v'
    return str(moved)+','+str(p.read(storage+0x18))+','+str(int(p.read(wrapper+0x40,8)!=0))+','+kind+';'+','.join(trace)

rng=random.Random(0x2d7d2dc);cases=[];expected=[]
for case in range(96):
    p.emu.mem_write(player,bytes(0xc000));heap=0x10100000;p.heap=0x10040000;trace=[];wait=0
    slot=0x558ded8
    if slot not in b.resolved_slots:p.emu.reg_write(UC_ARM64_REG_X0,slot);b.boundary(p.emu,0xffd960,4,None)
    p.put(klass+0x12a,1,2);p.put(klass+0xb0,iface,8);p.put(iface,p.read(slot,8),8);p.put(iface+8,20)
    p.put(klass+0x278,MOVE,8);p.put(klass+0x288,CURRENT,8);p.put(inner,klass,8)
    p.put(player+0x10,mono,8);p.put(player+0x18,storage,8);p.put(storage+0x10,array,8);p.put(array+0x18,128)
    p.put(wrapper+0x10,inner,8);p.put(wrapper+0x18,complete if case%2 else 0,8);p.put(wrapper+0x20,killed if case%3 else 0,8)
    p.put(complete+0x18,COMPLETE,8);p.put(killed+0x18,KILLED,8)
    delay=bits([0.,.02,-.1,float('nan'),float('inf')][case%5]);p.put(wrapper+0x38,delay)
    call(0x2d7cae8,player,wrapper);expected.append(snapshot(2))
    commands=[rng.choice((1,1,1,0,2,3,4,5,6)) for _ in range(18)]
    if case<24:commands=[1]*5+[0]+[1]*12
    elif case<48:commands=[1]*3+[4]+[1]*14
    cases.append(str(delay)+','+str(case%2)+','+str(int(case%3!=0))+';'+','.join(map(str,commands)))
    for command in commands:
        moved=2
        if command<4:inside=command;moved=call(0x2d7d2dc,iterator)
        elif command==4:call(0x2d7cc4c,player,handle)
        elif command==5:call(0x2d7cd20,wrapper)
        else:call(0x2d7cc4c,player,0x7777)
        expected.append(snapshot(moved))

harness=r'''using System;using System.Collections;using System.Collections.Generic;using Whitebox.Foundation;using UnityEngine;
namespace UnityEngine{public class Coroutine{public IEnumerator Iterator;}public class WaitForSeconds{public WaitForSeconds(float seconds){Verify.Trace.Add("wait:"+BitConverter.ToUInt32(BitConverter.GetBytes(seconds),0));}}public class MonoBehaviour{public Coroutine StartCoroutine(IEnumerator value){Verify.Trace.Add("host-start");return new Coroutine{Iterator=value};}public void StopCoroutine(Coroutine value){Verify.Trace.Add("host-stop");}}}
class Inner:IEnumerator{public int Command;public OriginalRoyalCoroutine Wrapper;public object Current{get{Verify.Trace.Add("current");return Verify.Token;}}public bool MoveNext(){Verify.Trace.Add("inner:"+Command);if(Command==2||Command==3)Wrapper.Kill();return Command==1||Command==2;}public void Reset(){throw new NotSupportedException();}}
class Verify{public static List<string> Trace=new List<string>();public static object Token=new object();static float Float(string s){return BitConverter.ToSingle(BitConverter.GetBytes(uint.Parse(s)),0);}static string Snapshot(int moved,OriginalCoroutinePlayer p,OriginalRoyalCoroutine wrapper,IEnumerator it){var cur=it.Current;return moved+","+p.Count+","+(wrapper.Coroutine==null?0:1)+","+(cur==null?"-":cur is WaitForSeconds?"w":"v")+";"+string.Join(",",Trace);}static void Main(){string line;while((line=Console.ReadLine())!=null){Trace.Clear();var a=line.Split(';');var h=a[0].Split(',');var inner=new Inner();var wrapper=OriginalRoyalCoroutine.Create(inner).SetStartDelay(Float(h[0]));inner.Wrapper=wrapper;if(h[1]=="1")wrapper.SetOnComplete(()=>Trace.Add("complete"));if(h[2]=="1")wrapper.SetOnKill(()=>Trace.Add("kill"));var player=new OriginalCoroutinePlayer(new MonoBehaviour());player.StartCoroutine(wrapper);var handle=wrapper.Coroutine;var it=handle.Iterator;Console.WriteLine(Snapshot(2,player,wrapper,it));foreach(var text in a[1].Split(',')){int command=int.Parse(text),moved=2;if(command<4){inner.Command=command;moved=it.MoveNext()?1:0;}else if(command==4)player.StopCoroutine(handle);else if(command==5)wrapper.Kill();else player.StopCoroutine(new Coroutine());Console.WriteLine(Snapshot(moved,player,wrapper,it));}}}}'''
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder);source=folder/'Verify.cs';source.write_text(harness);exe=folder/'Verify.exe'
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),str(p.PROJECT/'Assets/Foundation/Runtime/OriginalCoroutinePlayer.cs')],capture_output=True,text=True,encoding='utf-8')
    assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8')
    assert result.returncode==0,result.stdout+result.stderr
    actual=result.stdout.lstrip('\ufeff').splitlines()
    assert len(actual)==len(expected),(len(actual),len(expected))
    assert actual==expected,next(((i//19,i%19,cases[i//19],a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e),'count mismatch')
report={'passed':True,'native_lifecycle_cases':len(cases),'native_observations':len(expected),
 'original_rvas':['0x2d7cae8','0x2d7cc4c','0x2d7ce60','0x2d7cbd8','0x2d7d2dc','0x2d7d25c','0x2d7cd20'],
 'scope':'Actual native player registration, handle assignment, reverse stop lookup, RoyalCoroutine iterator, delay allocation, completion and clear/kill order versus production C#. Includes empty completion, external cancel, missing handle, self-kill from inner MoveNext returning true/false, duplicate kill, and post-completion resumes. Compares host start/stop, inner current access, completion/kill callbacks, current yield object, registered count and handle clearing. Unity host execution timing and wait scheduling, allocation, delegate/interface metadata, inner enumerator and managed list storage are boundaries. Immediate first resume and deactivated-view merge cancellation are covered separately in Unity. KillAll traversal/logging and arbitrary user callback mutation are not asserted.'}
(p.PROJECT/'coroutine_player_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
