"""Compare clock quantization and late-update accumulation with native ARM64."""
import sys
sys.dont_write_bytecode=True
import json, struct, random, subprocess, tempfile, os, types
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
# Load the native image/runtime boundaries without running unrelated board-layout tests.
exec(compile((PROJECT/'SourceCatalog/verify_move_budget.py').read_text().split('cases=[];expected=[]')[0],str(PROJECT/'SourceCatalog/verify_move_budget.py'),'exec'))
native_clock_call=call
p=types.SimpleNamespace(emu=emu,put=put,read=read,PROJECT=PROJECT,mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')))
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import UC_ARM64_REG_S0,UC_ARM64_REG_PC,UC_ARM64_REG_LR
delta_bits=0
def boundary(uc,address,size,data):
    if address==0x3e424ec:
        uc.reg_write(UC_ARM64_REG_S0,delta_bits);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE,boundary)
bits=lambda value:struct.unpack('<I',struct.pack('<f',value))[0]
rng=random.Random(3181334)
inputs=[0,1,0x80000000,0x7f800000,0xff800000,0x7fc00000,0x7f7fffff,0xff7fffff]
inputs += [bits(x) for x in (1/60,1/30,0.00009,-0.00009,0.123456,-0.123456,214748.36,-214748.36)]
inputs += [rng.randrange(2**32) for _ in range(256)]
expected=[];commands=[]
for delta_bits in inputs:
    native_clock_call(0x318d1c4,0x10000000);expected.append(str(p.emu.reg_read(UC_ARM64_REG_S0)));commands.append('Q,'+str(delta_bits))
owner=0x10000000;klass=0x10001000
p.emu.mem_write(owner,bytes(0x2000));p.put(owner,klass,8);p.put(klass+0x1c8,0x318d1c4,8)
for _ in range(512):
    delta_bits=bits(rng.uniform(0,0.1));native_clock_call(0x318d334,owner)
    expected.append(str(p.read(owner+0x10))+','+str(p.read(owner+0x14)));commands.append('A,'+str(delta_bits))
harness=r'''using System;using Whitebox.Foundation;class Verify{static uint Bits(float value){return BitConverter.ToUInt32(BitConverter.GetBytes(value),0);}static void Main(){var c=new OriginalLevelClock();string line;while((line=Console.ReadLine())!=null){var a=line.Split(',');float d=BitConverter.ToSingle(BitConverter.GetBytes(uint.Parse(a[1])),0);if(a[0]=="Q")Console.WriteLine(Bits(OriginalLevelClock.QuantizeDelta(d)));else{c.ManualLateUpdate(d);Console.WriteLine(c.FrameCount+","+Bits(c.Time));}}}}'''
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder);source=folder/'Verify.cs';source.write_text(harness);exe=folder/'Verify.exe'
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),str(p.PROJECT/'Assets/Foundation/Runtime/OriginalLevelClock.cs')],capture_output=True,text=True,encoding='utf-8')
    assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(commands)+'\n',capture_output=True,text=True,encoding='utf-8')
    assert result.returncode==0,result.stdout+result.stderr
    actual=result.stdout.replace('\ufeff','').splitlines()
    assert actual==expected,next(((commands[i],a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e),'count mismatch')
report={'passed':True,'native_delta_cases':len(inputs),'native_late_update_steps':512,'original_rvas':['0x318d1c4','0x318d334'],
 'scope':'Native float bit comparison for quantized delta (including negative/nonfinite/overflow inputs), accumulated clock and frame count. Unity delta getter supplied. Unity callback ordering and replay clock are not covered.'}
(p.PROJECT/'level_clock_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
