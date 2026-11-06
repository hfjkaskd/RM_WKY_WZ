"""Execute native AnimationData construction and record its ownership/defaults."""
import sys
sys.dont_write_bytecode=True
import hashlib,json,sqlite3
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_move_budget.py').read_text().split('cases=[];expected=[]')[0],str(PROJECT/'SourceCatalog/verify_move_budget.py'),'exec'))
emu.hook_del(native_hook);base_hook=hook
root=0x10020000;next_object=0x10030000;allocations=[];writes=[];dicts=[]
def ownership_hook(uc,address,size,unused):
 x0,x1=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(2)]
 if address==0xffda78:
  p=allocate(0x100);allocations.append(p);returned(uc,p)
 elif address==0xffd90c:
  if root+0x10<=x0<=root+0x128:writes.append((x0-root,x1))
  returned(uc)
 elif address==0x3c95768:dicts.append(x0);returned(uc) # CLR dictionary constructor, empty default capacity.
 elif address==0x231007c:returned(uc)
 else:base_hook(uc,address,size,unused)
emu.hook_add(UC_HOOK_CODE,ownership_hook)
emu.mem_write(root,bytes(0x140));call(0x338a648,root);assert not fault
order=[0x10,0x18,0x20,0x28,0x30,0x38,0x40,0x48,0x58,0x60,0x68,0x88,0x90,0xa0,0x70,0x78,0x80,0x98,0xa8,0xb0,0xb8,0xc0,0xc8,0xd0,0xd8,0xe0,0xe8,0xf0,0x50,0xf8,0x100,0x108,0x110,0x118,0x120,0x128]
assert [o for o,p in writes]==order,writes
assert len(allocations)==36 and len(set(allocations))==36
assert [p for o,p in writes]==allocations
assert dicts==[read(root+o,8)for o in order[-4:]]
for o in order[:32]:
 p=read(root+o,8)
 if o==0x100:assert read(p+0x10,1)==0 and read(p+0x14)==0
 else:
  assert read(p+0x10,8)==0 and read(p+0x18,8)==p,(hex(o),'restore owner')
  assert read(p+0x20,1)==0 and read(p+0x24)==0 and read(p+0x28)==0
  if o==0xc8:assert all(read(p+k)==0 for k in range(0x2c,0x38,4))
  elif o in(0x40,0x48,0xa0):assert signed(read(p+0x2c))==-1
  elif o==0x88:assert signed(read(p+0x38))==-1
assert read(root+0x130,2)==0
db=sqlite3.connect(PROJECT.parent/'code/native/functions.sqlite3')
native_name,body=db.execute('select name,body from functions where address=0x338a648').fetchone()
evidence={'constructor_rva':'0x338a648','name':native_name,'disassembly_sha256':hashlib.sha256(body.encode()).hexdigest(),
 'construction_field_order':[hex(o)for o in order], 'native_snapshot_count':32,'native_dictionary_count':4,
 'sentinels':{'madness_event_id':-1,'royal_pass_event_id':-1,'pinata_event_id':-1,'daily_step':-1},
 'notes':['All 32 snapshots are distinct. RestorableData owns the snapshot itself; InviteFriends has no restorable config.',
 'Base snapshot counts/consume and Mission/Invite counters start zero. Two AnimationData flags start false.',
 'Native executes original constructors and field writes, including inlined derived setup; CLR object allocation and empty enum/int dictionary constructors are supplied.',
 'Runtime constructor uses the same order and creates concrete snapshots; shared StartSnapshots aliases the animation object. Its typed application context is resolved only by later operations.',
 'Runtime ownership/defaults, deferred resolution, seven restore bindings and all-concrete Load/Start/Restart are checked by ActiveLevelStartVerification in Unity.']}
(PROJECT/'SourceCatalog/animation_ownership_evidence.json').write_text(json.dumps(evidence,indent=2)+'\n')
report={'passed':True,'native_constructors_executed':1,'native_owned_snapshots':32,'native_owned_dictionaries':4,
 'scope':'Actual ARM64 AnimationData constructor and concrete/inline snapshot constructors; verifies original allocation/store order, distinct objects, self-owned restore configs, derived sentinels, zero counters and consume/multiplier flags. CLR allocation and empty Dictionary enum/int constructors are supplied boundaries. Runtime constructor integration and dependency binding are checked separately in Unity; application composition and playable loop remain incomplete.'}
(PROJECT/'animation_ownership_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
