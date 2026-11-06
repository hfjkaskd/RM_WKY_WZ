"""Run native DefaultLightballStrategy.ExplodeTouching against production C#."""
import sys
sys.dont_write_bytecode = True
import hashlib, json, random, sqlite3, struct, subprocess, tempfile
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[2] / "tools/python"))
from types import SimpleNamespace
from unicorn import Uc, UC_ARCH_ARM64, UC_MODE_ARM, UC_HOOK_CODE
from elftools.elf.elffile import ELFFile
from elftools.elf.relocation import RelocationSection
from unicorn.arm64_const import UC_ARM64_REG_X0, UC_ARM64_REG_X1, UC_ARM64_REG_X2, UC_ARM64_REG_LR, UC_ARM64_REG_SP, UC_ARM64_REG_PC

p = SimpleNamespace(PROJECT=Path(__file__).resolve().parents[1])
p.RECOVERY=p.PROJECT.parent
p.mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
p.emu=Uc(UC_ARCH_ARM64, UC_MODE_ARM)
p.emu.mem_map(0,0x6000000);p.emu.mem_map(0x10000000,0x200000);p.emu.mem_map(0x20000000,0x1000)
p.read=lambda a,size=4:int.from_bytes(p.emu.mem_read(a,size),'little')
p.put=lambda a,v,size=4:p.emu.mem_write(a,(v&((1<<(size*8))-1)).to_bytes(size,'little'))
lib=p.RECOVERY.parent/'RoyalMatch_20260929/apk_unpacked/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so'
with lib.open('rb') as stream:
    elf=ELFFile(stream)
    for segment in elf.iter_segments():
        if segment['p_type']=='PT_LOAD':p.emu.mem_write(segment['p_vaddr'],segment.data())
    for section in elf.iter_sections():
        if isinstance(section,RelocationSection):
            for relocation in section.iter_relocations():
                if relocation['r_info_type']==1027:p.put(relocation['r_offset'],relocation['r_addend'],8)
owner, cell, data = 0x10000000, 0x10001000, 0x10002000
statics, directions, neighbors, array = 0x10003000, 0x10004000, 0x10005000, 0x10006000
trace = []
def boundary(uc, address, size, unused):
    if address == 0xffd960:
        pointer=uc.reg_read(UC_ARM64_REG_X0)
        p.put(pointer,0x1000a000,8);p.put(0x1000a000+0xe0,1);p.put(0x1000a000+0xb8,statics,8)
        uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
    elif address == 0x319d7a4:
        target = uc.reg_read(UC_ARM64_REG_X0)
        value = uc.reg_read(UC_ARM64_REG_X1)
        trace.append(str(target) + ':' + bytes(uc.mem_read(value, 56)).hex())
        # The native callee receives a value copy, even if it changes that copy.
        uc.mem_write(value, bytes([0xff]) * 56)
        uc.reg_write(UC_ARM64_REG_X0, 0)
        uc.reg_write(UC_ARM64_REG_PC, uc.reg_read(UC_ARM64_REG_LR))
p.emu.hook_add(UC_HOOK_CODE, boundary)
slot = p.read(0x53b2db8, 8)
p.put(slot,0x1000a000,8);p.put(0x1000a000+0xe0,1);p.put(0x1000a000+0xb8,statics,8)
p.put(statics + 8, directions, 8)
p.put(directions + 0x18, 4)
# This is the actual original initializer blob, not the proposed C# array.
field_hash='49C16B6F0EF802F3AAF7FC52DC1E6925199EDDC6E255028182BBA4E236F8F855'
metadata=lib.parents[3]/'base/assets/bin/Data/Managed/Metadata/global-metadata.dat'
with metadata.open('rb') as stream:
    stream.seek(0xa40748); touching_blob=stream.read(16)
assert hashlib.sha256(touching_blob).hexdigest().upper()==field_hash
with (p.RECOVERY/'code/metadata/script.json').open(encoding='utf-8') as stream:
    for line in stream:
        if line.strip().startswith('"Address":'): field_address=int(line.split(':',1)[1].strip().rstrip(','))
        if field_hash in line: break
    else: raise AssertionError('Native initializer field is missing')
# CellNeighbors..cctor 0x31956b8 loads this specific metadata field.
assert p.read(0x5397568,8)==field_address
for i, direction in enumerate(struct.unpack('<4i',touching_blob)):
    p.put(directions + 0x20 + i * 4, direction)
p.put(cell + 0x20, neighbors, 8)
p.put(neighbors + 0x18, array, 8)
p.put(array + 0x18, 8)
rng = random.Random(0x1f45b3c)
cases, expected = [], []
for n in range(128):
    x, y = rng.randint(-30, 30), rng.randint(-30, 30)
    # Every direction can be absent or share a neighbor. Repeated hits survive.
    targets = [rng.choice((0, 0x10007000, 0x10008000, 0x10009000)) for _ in range(8)]
    for i, target in enumerate(targets):
        p.put(array + 0x20 + i * 8, target, 8)
    p.put(cell + 0x18, x); p.put(cell + 0x1c, y)
    payload = bytearray(rng.randrange(256) for _ in range(56))
    payload[0x14] = n % 2; payload[0x15] = (n // 2) % 2
    payload = bytes(payload)
    p.emu.mem_write(data, payload)
    for reg, value in ((UC_ARM64_REG_X0, owner), (UC_ARM64_REG_X1, cell), (UC_ARM64_REG_X2, data),
                       (UC_ARM64_REG_SP, 0x101ff000), (UC_ARM64_REG_LR, 0x20000000)):
        p.emu.reg_write(reg, value)
    trace.clear()
    p.emu.emu_start(0x1f45b3c, 0x20000000, count=100000)
    assert p.emu.reg_read(UC_ARM64_REG_PC) == 0x20000000
    cases.append('|'.join((str(x), str(y), ','.join(map(str, targets)), payload.hex())))
    expected.append('|'.join(trace))

harness = r'''using System; using System.Collections.Generic; using Whitebox.Foundation;
namespace Whitebox.Foundation {
 public sealed class OriginalCellMediation {public OriginalCellMediation(OriginalGridCell cell){}}
 public sealed class OriginalCellStaticExplosionServices {}
 public sealed partial class OriginalDefaultLightballStrategy {
  private readonly OriginalCellStaticExplosionServices statics = new OriginalCellStaticExplosionServices();
  public void Run(OriginalGridCell cell, OriginalExplodeData data){ExplodeTouching(cell,data);}
 }
 public partial class OriginalGridCell {public long Id;}
 public static class OriginalCellNearMatchExplosion {
  public static void ExplodeCurrentItemByNearMatch(OriginalGridCell cell,OriginalExplodeData data,OriginalCellStaticExplosionServices services){
   byte[] bytes=Verify.Bytes(data);
   Verify.Trace.Add(cell.Id+":"+BitConverter.ToString(bytes).Replace("-","").ToLowerInvariant());
  }
 }
}
class Verify {
 public static byte[] Payload; public static List<string> Trace=new List<string>();
 static int Int(int at){return BitConverter.ToInt32(Payload,at);}
 static float Float(int at){return BitConverter.ToSingle(Payload,at);}
 static OriginalExplodeData Read(){var d=new OriginalExplodeData(Int(8),Int(12),new OriginalCellPoint(Int(0),Int(4)),Int(16),Payload[20]!=0,Payload[21]!=0,Int(24),Int(40));d.LastTargetItemIdentifier=new OriginalItemIdentifier{ItemType=Int(28),StaticItemType=Int(32),ItemGroupId=Int(36)};d.IncomingX=Float(44);d.IncomingY=Float(48);d.IncomingZ=Float(52);return d;}
 public static byte[] Bytes(OriginalExplodeData d){var b=new byte[56];var values=new[]{d.Point.X,d.Point.Y,d.Trigger,d.Id,d.MatchType};for(int i=0;i<5;i++)Array.Copy(BitConverter.GetBytes(values[i]),0,b,i*4,4);b[20]=(byte)(d.CanSpreadJelly?1:0);b[21]=(byte)(d.IsHorizontalRocket?1:0);b[22]=Payload[22];b[23]=Payload[23];values=new[]{d.ComboId,d.LastTargetItemIdentifier.ItemType,d.LastTargetItemIdentifier.StaticItemType,d.LastTargetItemIdentifier.ItemGroupId,d.ExploderIndex};for(int i=0;i<5;i++)Array.Copy(BitConverter.GetBytes(values[i]),0,b,24+i*4,4);var floats=new[]{d.IncomingX,d.IncomingY,d.IncomingZ};for(int i=0;i<3;i++)Array.Copy(BitConverter.GetBytes(floats[i]),0,b,44+i*4,4);return b;}

 static void Main(){string line;while((line=Console.ReadLine())!=null){var a=line.Split('|');int x=int.Parse(a[0]),y=int.Parse(a[1]);var ids=Array.ConvertAll(a[2].Split(','),long.Parse);Payload=new byte[56];for(int i=0;i<56;i++)Payload[i]=Convert.ToByte(a[3].Substring(i*2,2),16);var cell=new OriginalGridCell(x,y,1);for(int i=0;i<8;i++)if(ids[i]!=0)cell.Neighbors.Set(i,new OriginalGridCell(0,0,1){Id=ids[i]});Trace.Clear();new OriginalDefaultLightballStrategy().Run(cell,Read());Console.WriteLine(string.Join("|",Trace));}}
}'''
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder); source=folder/'Verify.cs'; source.write_text(harness); exe=folder/'Verify.exe'
    runtime=p.PROJECT/'Assets/Foundation/Runtime'
    names=('OriginalExplodeData', 'OriginalGridIterator', 'OriginalLightballTouching')
    result=subprocess.run([str(p.mono/'bin/mono.exe'), str(p.mono/'lib/mono/4.5/mcs.exe'), '-out:'+str(exe), str(source),
                           *[str(runtime/(name+'.cs')) for name in names]], capture_output=True, text=True, encoding='utf-8')
    assert result.returncode == 0, result.stdout+result.stderr
    result=subprocess.run([str(p.mono/'bin/mono.exe'), str(exe)], input='\n'.join(cases)+'\n', capture_output=True, text=True, encoding='utf-8')
    assert result.returncode == 0, result.stdout+result.stderr
    actual=result.stdout.lstrip('\ufeff').splitlines()
    assert actual == expected, next(((i,cases[i],a,e) for i,(a,e) in enumerate(zip(actual,expected)) if a!=e),'count differs')
db=sqlite3.connect(p.RECOVERY/'code/native/functions.sqlite3')
body=db.execute('select body from functions where address=?',(0x1f45b3c,)).fetchone()[0]
report={'passed':True,'native_cases':len(cases),'original_rva':'0x1f45b3c','native_body_sha256':hashlib.sha256(body.encode()).hexdigest(),
        'original_initializer_rva':'0x31955ec','original_field_metadata_offset':'0xa40748','original_field_sha256':field_hash.lower(),
        'scope':'Actual ARM64 Lightball touching dispatch vs compiled production C#: cardinal order, absent/repeated neighbors, origin point and unchanged per-hit payload. Real CellNeighbors.Get executes. Metadata/neighbor storage and concrete near-match damage are boundary supplied; this does not claim obstacle damage, lightball animation or rendered parity.'}
(p.PROJECT/'lightball_touching_verification.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report))
