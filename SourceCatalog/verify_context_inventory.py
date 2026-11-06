"""Execute native LevelContext ctor to recover permanent registration order."""
import sys
sys.dont_write_bytecode=True
import hashlib,json,os,re,sqlite3,subprocess,tempfile
from pathlib import Path
PROJECT=Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_move_budget.py').read_text().split('cases=[];expected=[]')[0],str(PROJECT/'SourceCatalog/verify_move_budget.py'),'exec'))
emu.hook_del(native_hook);base_hook=hook
database=sqlite3.connect(RECOVERY/'code/native/functions.sqlite3')
ctor=0x3206098
ctors={target:name for target,name in database.execute('select c.target,f.name from calls c join functions f on f.address=c.target where c.caller=? and f.name like ?',(ctor,'%.ctor')) if target not in (0x3f40b30,0x231007c)}
owner,enum_array=0x10020000,0x10021000
names={};order=[];heap=0x10040000;table_count=None
dump=(RECOVERY/'code/metadata/dump.cs').read_text(encoding='utf-8')
enum=dump.split('public enum LevelContextId ',1)[1].split('\n}',1)[0]
expected_count=len(re.findall('public const LevelContextId ',enum));put(enum_array+0x18,expected_count)
def hook(uc,address,size,unused):
 global heap,table_count
 x0,x1,x2=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(3)]
 if address in ctors:names[x0]=ctors[address];returned(uc)
 elif address==0xffda78:
  p=heap;heap+=0x800;emu.mem_write(p,bytes(0x800));put(p,x0,8);returned(uc,p)
 elif address==0x261458c:returned(uc,0x10022000)
 elif address==0x2301314:returned(uc,enum_array)
 elif address==0x3f40b30:table_count=x1;returned(uc)
 elif address==0x3f40bf8:assert x1 in names,hex(x1);order.append((x1,names[x1]));returned(uc)
 elif address==0x231007c:returned(uc)
 else:base_hook(uc,address,size,unused)
for a in [*ctors,0xffda78,0x261458c,0x2301314,0x3f40b30,0x3f40bf8,0x231007c,0xffd960,0xffd90c,0xffda64,0xffda88,0xffda90]:emu.hook_add(UC_HOOK_CODE,hook,begin=a,end=a)
call(ctor,owner);assert not fault;assert table_count==expected_count==112
units=[]
for pointer,name in order:
 getter_name=name.removesuffix('.ctor')+'get_Id'
 address=database.execute('select address from functions where name=?',(getter_name,)).fetchone()
 assert address,getter_name
 unit_id=call(address[0],pointer);assert not fault
 units.append({'id':unit_id,'type':name.split('$$')[0],'get_id_rva':hex(address[0])})
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-context-inventory-')as folder:
 exe=Path(folder)/'verify.exe';source=Path(folder)/'Verify.cs'
 source.write_text('using System;using Whitebox.Foundation; class V{static void Main(){Console.Write(OriginalLevelUnitRegistration.UnitCount+":");for(int i=0;i<OriginalLevelUnitRegistration.PermanentUnitCount;i++){if(i>0)Console.Write(",");Console.Write(OriginalLevelUnitRegistration.GetPermanentUnitId(i));}}}')
 result=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(source),str(PROJECT/'Assets/Foundation/Runtime/OriginalContextContainer.cs')],capture_output=True,text=True,encoding='utf-8-sig');assert result.returncode==0,result.stdout+result.stderr
 result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],capture_output=True,text=True,encoding='utf-8-sig');assert result.returncode==0,result.stderr
 expected=str(table_count)+':'+','.join(str(v['id'])for v in units);assert result.stdout==expected,(result.stdout,expected)
report={'passed':True,'unit_table_count':table_count,'native_permanent_registrations':units,'scope':'Actual ARM64 LevelContext constructor registration calls in execution order, followed by each concrete native unit get_Id. Unit constructors, CLR metadata/allocation, Enum.GetNames sizing and ContextContainer.Add are supplied boundaries; this proves permanent order/table count, not construction/binding of every original manager. C# registration catalog uses explicit numeric protocol identities without enum reflection. Complete application/context activation and all actual unit factories remain pending.'}
(PROJECT/'context_inventory_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'passed':True,'table_count':table_count,'registration_ids':[v['id']for v in units]}))
