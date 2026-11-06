"""Original ARM64 goal/limit parsing, pruning, claim preparation and raw board rows."""
import sys
sys.dont_write_bytecode=True
import verify_board_layout as b
import json,random,subprocess,tempfile
from pathlib import Path
from unicorn import UC_HOOK_CODE
from unicorn.arm64_const import *
p=b.p
parser,goal_list,limit_list,claim_list,start_list,end_list=[0x10000000+i*0x1000 for i in range(6)]
goal_storage,limit_storage,claim_storage,start_storage,end_storage=[0x10010000+i*0x1000 for i in range(5)]
dictionaries={};orders={};free={};fault=False;goal_index=limit_index=curtain_index=0;row=None
goal_manager,claim_manager,prereq_dict,flying_dict,empty_list,empty_storage,log_runtime,log_statics=[0x10060000+i*0x1000 for i in range(8)]
MODEL_EVENT,UI_EVENT=0x20000200,0x20000204
init_notifications=[]
def s(value):return p.signed(value&0xffffffff)
def ret(uc,value):
 uc.reg_write(UC_ARM64_REG_X0,value&0xffffffffffffffff);uc.reg_write(UC_ARM64_REG_PC,uc.reg_read(UC_ARM64_REG_LR))
def fail(uc):
 global fault
 fault=True;uc.reg_write(UC_ARM64_REG_PC,0x20000000)
def boundary(uc,address,size,unused):
 global goal_index,limit_index,curtain_index
 x0,x1,x2=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(3)];result=None;key=s(x1)
 if address in (0xffda88,0xffda90,0xffda50):fail(uc);return
 if address==0x1c1f5f4:result=row['water']
 elif address==0x1c26104:result=row['water_boards']
 elif address==0x1c1eca0:result=row['height']
 elif address==0x1c1ec14:result=row['width']
 elif address==0x1622adc:result=int(row['scroll'])
 elif address==0x1c25fcc:
  index=key+row['width']*s(x2)
  if index<0 or index>=len(row['items']):fail(uc);return
  result=row['items'][index]
 elif address in (0x162175c,0x1622674,0x1624864):result=len(row[{0x162175c:'goals',0x1622674:'limits',0x1624864:'curtains'}[address]])
 elif address in (0x1621684,0x162259c,0x162478c):
  if address==0x1621684:goal_index=key
  elif address==0x162259c:limit_index=key
  else:curtain_index=key
  result=0
 elif address in (0x2e02d4c,0x2e0542c,0x2e03be0):uc.reg_write(UC_ARM64_REG_X1,0);result=0
 elif address in (0x19175dc,0x1917620,0x1917664,0x19176ac):result=row['goals'][goal_index][{0x19175dc:0,0x1917620:1,0x1917664:2,0x19176ac:3}[address]]
 elif address in (0x191a1f8,0x191a23c,0x191a280):result=row['limits'][limit_index][{0x191a1f8:0,0x191a23c:1,0x191a280:2}[address]]
 elif address==0x191827c:result=row['curtains'][curtain_index][0]
 elif address==0x1917f9c:result=len(row['curtains'][curtain_index][1])
 elif address==0x1917f3c:result=row['curtains'][curtain_index][1][key]
 elif address==0x1918054:
  values=row['curtains'][curtain_index][2];result=0 if values is None else values[key]
 elif address in (0x2ee82b4,0x2ee8318,0x2ee8218):result=0 # Required goal-condition context binding, not goal/tuple arithmetic.
 elif address==0x2c1b4a0:
  storage=p.allocate(0x1000);p.put(storage+0x18,128);p.put(x0+0x10,storage,8);p.put(x0+0x18,0);result=0
 elif address in (0x2c1c668,0x3c9ccac):
  output=uc.reg_read(UC_ARM64_REG_X8);uc.mem_write(output,bytes(24 if address==0x2c1c668 else 40));p.put(output,x0,8);result=0
 elif address==0x2b3560c:
  owner=p.read(x0,8);index=p.read(x0+8)
  if index<p.read(owner+0x18):
   p.put(x0+0x10,p.read(p.read(owner+0x10,8)+0x20+index*8,8),8);p.put(x0+8,index+1);result=1
  else:p.put(x0+0x10,0,8);result=0
 elif address==0x347c108:
  owner=p.read(x0,8);index=p.read(x0+8);keys=[key for key in orders.get(owner,[])if key is not None]
  if index<len(keys):
   key=keys[index];p.put(x0+0x10,key);p.put(x0+0x18,dictionaries[owner][key],8);p.put(x0+8,index+1);result=1
  else:result=0
 elif address in (0x2b35608,0x347c224):result=0
 elif address==0x3c9c47c:result=len(dictionaries[x0])
 elif address==0x3c96364:dictionaries[x0].clear();result=0
 elif address==0x3c961c0:dictionaries[x0][key]=s(x2);result=0
 elif address in (0x1d68d90,0x1d73260,0x1d6812c):result=log_statics+0x100
 elif address==0xfcd644:result=log_runtime
 elif address==0x2a2da14:result=0
 elif address==MODEL_EVENT:init_notifications.append('m,'+str(key)+','+str(s(x2)));result=0
 elif address==UI_EVENT:
  flags=''.join(str(int(bool(uc.reg_read(UC_ARM64_REG_X0+i))))for i in(3,4,5))
  init_notifications.append('u,'+str(key)+','+str(s(x2))+','+flags);result=0
 elif address==0x2c1b9a0:
  if key<0 or key>=p.read(x0+0x18):fail(uc);return
  result=p.read(p.read(x0+0x10,8)+0x20+key*8,8)
 elif address==0x341ed38:
  if key<0 or key>=p.read(x0+0x18):fail(uc);return
  storage=p.read(x0+0x10,8)+0x20+key*12
  result=p.read(storage,8);uc.reg_write(UC_ARM64_REG_X1,p.read(storage+8))
 elif address in (0x3c9c858,0x3c9ca58,0x3c9c7a4,0x3c9ddd0,0x3c9e3f0):
  values=dictionaries[x0]
  if address==0x3c9c858:
   if key in values:fail(uc);return
   values[key]=x2;slots=orders.setdefault(x0,[]);freed=free.setdefault(x0,[])
   if freed:slots[freed.pop()]=key
   else:slots.append(key)
   result=0
  elif address==0x3c9ca58:result=int(key in values)
  elif address==0x3c9c7a4:
   if key not in values:fail(uc);return
   result=values[key]
  elif address==0x3c9ddd0:
   result=int(key in values)
   if result:
    slots=orders[x0];index=slots.index(key);slots[index]=None;free[x0].append(index);del values[key]
  else:p.put(x2,values.get(key,0),8);result=int(key in values)
 elif address in (0x2a2e598,):result=0
 elif address==0xffda6c:result=p.allocate()
 if result is not None:ret(uc,result)
p.emu.hook_add(UC_HOOK_CODE,boundary)
def call(rva,*args):
 global fault
 fault=False
 for i,value in enumerate((*args,0,0,0)):p.emu.reg_write(UC_ARM64_REG_X0+i,value&0xffffffffffffffff)
 p.emu.reg_write(UC_ARM64_REG_SP,0x101ff000);p.emu.reg_write(UC_ARM64_REG_LR,0x20000000)
 try:p.emu.emu_start(rva,0x20000000,count=1000000)
 except Exception:print('Native failed',hex(p.emu.reg_read(UC_ARM64_REG_PC)),row);raise
 assert p.emu.reg_read(UC_ARM64_REG_PC)==0x20000000,hex(p.emu.reg_read(UC_ARM64_REG_PC))
 return s(p.emu.reg_read(UC_ARM64_REG_X0))
def list_(owner,storage):
 p.emu.mem_write(owner,bytes(0x40));p.emu.mem_write(storage,bytes(0x1000));p.put(storage+0x18,128);p.put(owner+0x10,storage,8)
def list_values(owner):
 storage=p.read(owner+0x10,8);return[p.read(storage+0x20+i*8,8)for i in range(p.read(owner+0x18))]
def csv(values):return ','.join(map(str,values))
def encode(row):
 curtains=';'.join(str(y)+'/'+csv(goals)+'/'+('-'if targets is None else csv(targets))for y,goals,targets in row['curtains'])
 return '#'.join((csv((row['width'],row['height'],int(row['scroll']),row['water'],row['water_boards'],int(row.get('reset',True)))),csv(row['items']),';'.join(map(csv,row['goals'])),';'.join(map(csv,row['limits'])),curtains))
def native(row_):
 global row,dictionaries,orders,free
 row=row_;dictionaries={};orders={};free={};p.dictionaries=dictionaries;p.heap=0x10080000
 p.emu.mem_write(parser,bytes(0x200))
 for owner,storage in zip((goal_list,limit_list,claim_list,start_list,end_list),(goal_storage,limit_storage,claim_storage,start_storage,end_storage)):list_(owner,storage)
 p.put(parser+0x60,claim_list,8);p.put(parser+0x70,start_list,8);p.put(parser+0x78,end_list,8)
 call(0x1c206b0,parser,goal_list);status='E'if fault else'1'
 call(0x1c20adc,parser,limit_list)
 if fault:status+='E'
 goals=[]
 for owner in list_values(goal_list):
  records=[]
  for key in orders.get(owner,[]):
   if key is None:continue
   value=dictionaries[owner][key]
   records.append(csv([key,*[s(p.read(value+offset))for offset in (0x10,0x14,0x18,0x1c,0x24)],p.read(value+0x20,1)]))
  goals.append(';'.join(records))
 limits=[]
 for owner in list_values(limit_list):
  limits.append(';'.join(csv((key,s(p.read(dictionaries[owner][key]+0x10)),s(p.read(dictionaries[owner][key]+0x14))))for key in orders.get(owner,[])if key is not None))
 claims=[];storage=p.read(claim_list+0x10,8)
 for i in range(p.read(claim_list+0x18)):claims.append(csv(s(p.read(storage+0x20+i*12+offset))for offset in(0,4,8)))
 rows=[]
 for y in range(-1,row['height']+1):
  value=call(0x1c25b68,parser,y);rows.append('E'if fault else str(value))
 initialized=[];init_notifications.clear()
 if status=='1':
  p.emu.mem_write(goal_manager,bytes(0x80));p.emu.mem_write(claim_manager,bytes(0x80));list_(empty_list,empty_storage)
  p.put(claim_manager+0x18,empty_list,8);p.put(goal_manager+0x48,claim_manager,8)
  p.put(goal_manager+0x38,prereq_dict,8);p.put(goal_manager+0x40,flying_dict,8);dictionaries[prereq_dict]={};dictionaries[flying_dict]={}
  p.emu.mem_write(log_runtime,bytes(0x200));p.put(log_runtime+0x132,1,1);p.put(log_runtime+0xe0,1);p.put(log_runtime+0xb8,log_statics,8);p.put(log_statics,0,8)
  # The observed native GOT load points to the Log metadata slot. Its +0x38
  # runtime-class indirection must exist before the native debug-only lookup.
  log_slot=p.read(0x53d5e30,8)
  if log_slot not in b.resolved_slots:
   p.emu.reg_write(UC_ARM64_REG_X0,log_slot);b.boundary(p.emu,0xffd960,4,None)
  p.put(p.read(log_slot,8)+0x38,log_statics+0x200,8);p.put(log_statics+0x200,log_runtime,8)
  for offset,target in((0x28,MODEL_EVENT),(0x30,UI_EVENT)):
   event=p.allocate();p.put(event+0x18,target,8);p.put(goal_manager+offset,event,8)
  call(0x3212444,goal_manager,goal_list,limit_list,int(row.get('reset',True)));assert not fault,('initialization failed',row)
  for values in list_values(p.read(goal_manager+0x18,8)):
   for i in range(p.read(values+0x18)):
    t=p.read(values+0x20+i*8,8);initialized.append(csv(s(p.read(t+offset))for offset in(0x10,0x14,0x18,0x1c,0x24)))
 return '#'.join((status,str(p.read(parser+0x16,1)),str(call(0x1c2545c,parser)),'|'.join(goals),'|'.join(limits),';'.join(claims),','.join(rows),';'.join(initialized),';'.join(init_notifications)))
rng=random.Random(0x1c206b0);rows=[]
original=json.loads((p.PROJECT/'Assets/Resources/Levels/level_1.json').read_text(encoding='utf-8-sig'))
rows.append({'width':original['Grid']['Width'],'height':original['Grid']['Height'],'scroll':False,'water':0,'water_boards':0,'items':original['Grid']['Items'],'goals':[(g['Goal'],g['Count'],int(g.get('IsFromSettings',False)),g.get('BoardId',0))for g in original['Goals']],'limits':[],'curtains':[]})
for n in range(120):
 width=1+n%4;height=12;scroll=n%3==1;water=4 if n%3==2 else 0
 items=[rng.choice((1,2,3,4,5,6,21,0))for _ in range(width*height)]
 for y in range(height):items[y*width]=1
 if scroll or water:
  for y in(3,4,8,9):items[y*width]=17 if n%2 else 320
  if n%5==0:items[11*width]=12
 boards=3 if scroll or water else 1;goals=[];limits=[]
 for board_id in range(boards):
  for kind in (11,13,22,14,54,30,29,21):goals.append((kind,rng.choice((-2,0,0,1,3,36)),n%2,board_id))
  limits.extend(((11,n-10,board_id),(1,3,board_id)))
 curtains=[]
 for bid,y in enumerate((1,6,10)if boards==3 else(1,)):
  kinds=[rng.choice((11,1,13,22,14,54,21,100))for _ in range(n%5)]
  targets=None if n%7==0 else[rng.choice((-3,0,1,2,7))for _ in kinds]
  curtains.append((y,kinds,targets))
 rows.append({'width':width,'height':height,'scroll':scroll,'water':water,'water_boards':boards,'items':items,'goals':goals,'limits':limits,'curtains':curtains,'reset':n%2==0})
for variant in range(12):
 base={'width':1,'height':5,'scroll':False,'water':0,'water_boards':0,'items':[1]*5,'goals':[(11,3,0,0)],'limits':[],'curtains':[]}
 if variant==0:base['goals']=[]
 elif variant==1:base['goals']=[(11,0,0,-1)]
 elif variant==2:base['goals'] *= 2
 elif variant==3:base['limits']=[(11,3,2)]
 elif variant==4:base['limits']=[(11,3,0),(11,5,0)]
 elif variant==5:base['curtains']=[(8,[11],[2])]
 elif variant==6:base['curtains']=[(-1,[11],[0])]
 elif variant==7:base['curtains']=[(0,[11,11,1],[2,3,4])]
 elif variant==8:base['goals']=[(21,0,0,0)]
 elif variant==9:base['goals']=[(13,0,0,0),(22,-1,0,0)]
 elif variant==10:base['water']=1;base['water_boards']=0
 else:base['goals']=[(11,0,0,0),(1,0,0,2)];base['curtains']=[(1,[1],[3])]
 rows.append(base)
cases=[];expected=[]
for i,row_ in enumerate(rows):
 cases.append(encode(row_));expected.append(native(row_))
with tempfile.TemporaryDirectory()as directory:
 directory=Path(directory);exe=directory/'Verify.exe';runtime=p.PROJECT/'Assets/Foundation/Runtime'
 names=('OriginalConfigData','OriginalLevelRules','OriginalBoardLayout','OriginalCellMediation','OriginalGridIterator','OriginalCellGrid','OriginalGoalTuple','OriginalLevelGoalPreparation','OriginalGoalManager','OriginalGoalClaims','OriginalGameOperations')
 result=subprocess.run([str(p.mono/'bin/mono.exe'),str(p.mono/'lib/mono/4.5/mcs.exe'),'-out:'+str(exe),str(p.PROJECT/'SourceCatalog/LevelGoalPreparationHarness.cs'),*[str(runtime/(name+'.cs'))for name in names]],capture_output=True,text=True,encoding='utf-8')
 assert result.returncode==0,result.stdout+result.stderr
 result=subprocess.run([str(p.mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
 actual=result.stdout.lstrip('\ufeff').splitlines()
 for i,(a,e)in enumerate(zip(actual,expected)):assert a==e,(i,cases[i],a,e)
 assert len(actual)==len(expected)
report={'passed':True,'native_cases':len(cases),'first_level':'00001v02_L','first_level_result':expected[0],'scope':'Actual ARM64 ParseGoals, ParseLimits, GetBoardCount, two-pass prerequisite pruning, BonusLevel coin flag, ApplyGoalClaims, CollectGoalClaims, GoalTuple constructor/factory and claim/target arithmetic, cached raw CalculateBoardRows/GetBoardIdForRow and prerequisite mapping, followed by actual GoalManager.InitWithGoalsAndLimits reset/retain paths and ordered model/UI initial notifications. Covers fixed first level, multiple boards, raw separators, water counts, zero/negative goals, repeated claims, missing Targets, settings flags, duplicate keys and invalid board indices. Decoded FlatBuffers/water-data getters, condition-context binding, managed storage/allocation/enumeration/delegates and logging are supplied boundaries. Unity formal-board composition, HUD, trails and full resolution remain pending.'}
(p.PROJECT/'level_goal_preparation_native_verification.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
