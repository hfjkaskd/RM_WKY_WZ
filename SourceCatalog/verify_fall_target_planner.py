"""Differential ARM64 fall targeting, including optional settled Unity board snapshots."""
import argparse
import sys
sys.dont_write_bytecode=True
import json,os,random,subprocess,tempfile,sqlite3,hashlib
from pathlib import Path
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--observations', type=Path)
args = parser.parse_args()
PROJECT=Path(__file__).resolve().parents[1]
exec(compile((PROJECT/'SourceCatalog/verify_move_budget.py').read_text().split('cases=[];expected=[]')[0],str(PROJECT/'SourceCatalog/verify_move_budget.py'),'exec'))
emu.hook_del(native_hook);base_hook=hook
root,grid,board_manager,board,state,ice,filldict,left,right,entry=0x10020000,0x10021000,0x10021200,0x10021400,0x10021600,0x10021800,0x10021a00,0x10022000,0x10022200,0x10022500
cells=[];items=[];cell_index={};item_index={}
width=0;height=0
h=[];c=[];v=[];slots=[];positions=[];held=[];incoming=[];queues=[];disabled=set();pending=set();stacks={left:[],right:[]}
filling_map={};throw_count=0
def ci(p):return -1 if not p else cell_index[p]
def ii(p):return 0 if not p else item_index[p]
def cp(i):return 0 if i<0 else cells[i]
def ip(i):return 0 if not i else items[i-1]
def record(uc,label):
 global throw_count
 trace.append(label);throw_count+=1
 if throw_count==h[9]:fail(uc);return False
 return True
def result(uc,label,value=0):
 if record(uc,label):returned(uc,value)
def setslot(cell,style,item):
 if item==0:return
 old=slots[cell][style]
 if old and old!=item:return
 if old==item:return
 slots[cell][style]=item
 previous=positions[item-1][style]
 if previous>=0:slots[previous][style]=0
 positions[item-1][style]=cell
def clearslot(cell,style):
 item=slots[cell][style]
 if item:positions[item-1][style]=-1
 slots[cell][style]=0
def neighbor(cell,d):
 x,y=c[cell][:2];dx=[-1,0,1,1,1,0,-1,-1][d];dy=[1,1,1,0,-1,-1,-1,0][d]
 x+=dx;y+=dy
 return y*width+x if 0<=x<width and 0<=y<height else -1
def fill_ready(i):return c[i][0]not in disabled and c[i][0]not in pending and not incoming[i]and bool(c[i][13])
def cell_from_mediation(p):return cell_index[p-0x100]
def item_from_mediation(p):return item_index[p-0x100]-1
def planner_hook(uc,address,size,unused):
 x0,x1,x2=[uc.reg_read(UC_ARM64_REG_X0+i)for i in range(3)]
 if address==0x319d024:result(uc,'blocked,'+str(ci(x0)),c[ci(x0)][6])
 elif address==0x20000010:result(uc,'available,'+str(ii(x0))+','+str(ci(x1)),v[ii(x0)-1][1])
 elif address==0x20000020:result(uc,'can_cross,'+str(ii(x0)),v[ii(x0)-1][2])
 elif address==0x20000030:result(uc,'special,'+str(ii(x0)),v[ii(x0)-1][3])
 elif address==0x20000040:result(uc,'cross_blocking,'+str(ii(x0)),v[ii(x0)-1][4])
 elif address==0x20000050:returned(uc,1<=c[ci(x0)][4]<=4)
 elif address==0x1e19144:result(uc,'multiple_board',h[1])
 elif address==0x320819c:returned(uc,board)
 elif address==0x3207c54:result(uc,'board_cross,'+str(signed(x1&0xffffffff)),h[2])
 elif address==0x29e27cc:assert x0==2;result(uc,'level',ice)
 elif address==0x1e19848:assert x1==51;result(uc,'ice',h[4])
 elif address==0x260df1c:assert x0==49;result(uc,'ice_helper',0 if h[8]else ice)
 elif address==0x2b91c48:result(uc,'above,'+str(signed(x1&0xffffffff))+','+str(signed(x1>>32)),h[5])
 elif address==0x2b92e6c:result(uc,'under,'+str(ci(x1)),cp(h[6]))
 elif address==0x31a0590:result(uc,'cross_freeze,'+str(ci(x0)),c[ci(x0)][7])
 elif address==0x2b92cf0:result(uc,'bottom,'+str(signed(x1&0xffffffff)),h[7])
 elif address==0x343ccfc:result(uc,'incoming,'+str(ci(x0-0x350)),c[ci(x0-0x350)][8])
 elif address==0x3196c8c:returned(uc,c[ci(x0)][3]==h[0])
 elif address in(0x343bd38,0x34487c0,0x3448800):returned(uc,slots[cell_from_mediation(x0)][{0x343bd38:0,0x34487c0:1,0x3448800:2}[address]]!=0)
 elif address in(0x343bd5c,0x34487a4,0x34487e4):returned(uc,ip(slots[cell_from_mediation(x0)][{0x343bd5c:0,0x34487a4:1,0x34487e4:2}[address]]))
 elif address in(0x319017c,0x319c48c,0x319c4a8):returned(uc,ip(slots[ci(x0)][{0x319017c:0,0x319c48c:1,0x319c4a8:2}[address]]))
 elif address in(0x2b45968,0x2b45a7c,0x2b45b90):returned(uc,cp(positions[item_from_mediation(x0)][{0x2b45968:0,0x2b45a7c:1,0x2b45b90:2}[address]]))
 elif address==0x2b45c94:returned(uc,positions[item_from_mediation(x0)][2]>=0)
 elif address==0x344866c:setslot(cell_from_mediation(x0),2,ii(x1));returned(uc)
 elif address==0x343d1f8:returned(uc,fill_ready(ci(x0)))
 elif address==0x343d69c:returned(uc,ii(x1)in queues[ci(x0)])
 elif address==0x343d2dc:incoming[ci(x0)].add(ii(x1));returned(uc)
 elif address==0x343d498:
  i=ci(x0);item=ii(x1)
  if item not in incoming[i]and item not in queues[i]:queues[i].append(item)
  returned(uc)
 elif address==0x343d1ec:held[ci(x0)]=0;put(x0+0xe0,0,8);returned(uc)
 elif address==0x220ca20:returned(uc,signed(x1&0xffffffff)in pending)
 elif address==0x3cceb30:returned(uc,signed(x1&0xffffffff)in filling_map)
 elif address==0x3cce87c:returned(uc,cp(filling_map[signed(x1&0xffffffff)]))
 elif address==0x16d0fa4:stacks[x0].clear();put(x0+0x18,0);returned(uc)
 elif address==0x16d14a4:stacks[x0].append(x1);put(x0+0x18,len(stacks[x0]));returned(uc)
 elif address==0x16d1420:
  a=stacks[x0].pop();put(x0+0x18,len(stacks[x0]));returned(uc,a)
 elif address==0xffda68:
  assert x1==read(0x558e268,8),(hex(x1),hex(read(0x558e268,8)))
  returned(uc,x0 if x0 and v[ii(x0)-1][0]!=0 else 0)
 elif address==0xfcd934:
  assert x0 in item_index and x2 in(0,1),(hex(x0),x2)
  put(entry,0x20000060 if x2==0 else 0x20000070,8);returned(uc,entry)
 elif address==0x20000060:
  if record(uc,'multiple,'+str(ii(x0))+','+str(ci(x1))):setslot(ci(x1),2,ii(x0));returned(uc)
 elif address==0x20000070:result(uc,'disable,'+str(ii(x0))+','+str(ci(x1)),v[ii(x0)-1][5])
 elif address==0xffde20:fail(uc)
 else:base_hook(uc,address,size,unused)
emu.hook_add(UC_HOOK_CODE,planner_hook)
def initialize(head,cell_data,item_data):
 global h,c,v,slots,positions,held,incoming,queues,disabled,pending,filling_map,next_object
 global cells,items,cell_index,item_index,width,height
 h=head[:];c=[x[:]for x in cell_data];v=[x[:]for x in item_data];next_object=0x10070000
 width=max(cell[0]for cell in c)+1;height=max(cell[1]for cell in c)+1
 assert width*height==len(c)<=192 and 0<len(v)<=256
 cells=[0x10030000+i*0x400 for i in range(len(c))]
 items=[0x10100000+i*0x400 for i in range(len(v))]
 cell_index={p:i for i,p in enumerate(cells)};item_index={p:i+1 for i,p in enumerate(items)}
 emu.mem_write(root,bytes(0x3000));emu.mem_write(cells[0],bytes(len(c)*0x400));emu.mem_write(items[0],bytes(len(v)*0x400))
 slots=[[0,0,0]for _ in cells];positions=[[-1,-1,-1]for _ in items];held=[x[12]for x in c];incoming=[set()for _ in cells];queues=[[]for _ in cells];disabled=set();pending=set();filling_map={};stacks[left]=[];stacks[right]=[]
 put(root+0x18,0x10022400,8);put(root+0x28,state,8);put(root+0x30,grid,8);put(root+0x40,left,8);put(root+0x48,right,8);put(grid+0xf0,board_manager,8)
 put(board_manager+0x3c,h[0]);put(board+0x38,0 if h[10]else filldict,8);put(state+0x24,h[3],1)
 # Resolve class metadata before constructing native class membership tables.
 for slot in(0x558cd50,0x558e268,0x558e7e0,0x558e7f0):call(0xffd960,slot)
 fill_type=read(0x558cd50,8);fake_type=read(0x558e7e0,8);master_type=read(0x558e7f0,8)
 for target in(fill_type,fake_type,master_type):put(target+0x12c,1,1)
 for k in range(4):
  klass=0x10060000+k*0x1000;emu.mem_write(klass,bytes(0x900));put(klass+0x478,0x20000010,8);put(klass+0x608,0x20000020,8);put(klass+0x558,0x20000030,8);put(klass+0x778,0x20000040,8)
  put(klass+0x12c,1,1);put(klass+0xc8,klass+0x800,8);put(klass+0x800,fake_type if k==2 else master_type if k==3 else 0,8)
 for filling in(0,1):
  klass=0x10065000+filling*0x1000;put(klass+0x238,0x20000050,8);put(klass+0x12c,1,1);put(klass+0xc8,klass+0x800,8);put(klass+0x800,fill_type if filling else 0,8)
 for i,p in enumerate(items):put(p,0x10060000+v[i][0]*0x1000,8);put(p+0x38,p+0x100,8)
 for i,p in enumerate(cells):
  a=c[i];put(p,0x10066000 if a[4]else 0x10065000,8);put(p+0x10,p+0x100,8);put(p+0x20,p+0x200,8);put(p+0x200+0x18,p+0x280,8);put(p+0x280+0x18,8);put(p+0x200+0x20,cp(neighbor(i,1)),8) # CellNeighbors.UpperCell is also read directly by the native cross-candidate search.
  for d in range(8):put(p+0x280+0x20+d*8,cp(neighbor(i,d)),8)
  put(p+0x18,a[0]);put(p+0x1c,a[1]);put(p+0x28,a[5]);put(p+0x30,a[14],1);put(p+0x68,a[3]);put(p+0xa0,a[2]);put(p+0xb0,p+0x350,8);put(p+0xe0,a[12]and ip(a[12]),8)
  if a[4]and a[3]==h[0]:filling_map[a[0]]=i
  for style,column in((0,10),(1,9),(2,11)):
   if a[column]:setslot(i,style,a[column])
def state_text():
 result=[]
 for i,p in enumerate(cells):
  mask=sum(1<<(item-1)for item in queues[i]if item)
  result.append(','.join(map(str,[*slots[i],read(p+0x30,1),held[i]if c[i][4]else 0,int(bool(c[i][4])and fill_ready(i)),mask])))
 result+= [','.join(map(str,a))for a in positions]
 return '/'.join(result)
cases=[];expected=[];commands=0
def run(head,cell_data,item_data,steps):
 global throw_count,commands,fault
 initialize(head,cell_data,item_data);out=[]
 for command in steps:
  commands+=1;trace.clear();throw_count=0;fault=False;parts=command.split(',');mode=parts[0];a=int(parts[1])if len(parts)>1 else 0;b=int(parts[2])if len(parts)>2 else 0;result_text='-'
  if mode in('n','p','s','x'):
   r=call({'n':0x32115cc,'p':0x3211684,'s':0x3211768,'x':0x3211d28}[mode],root,cp(a),cp(b),int(parts[3])if len(parts)>3 else 0)
   result_text=str(ii(emu.reg_read(UC_ARM64_REG_X0))if mode in('n','x')and not fault else r)
  elif mode=='f':
   put(entry+0x100,0,8);call(0x3210e48,root,cp(a),entry+0x100);result_text=str(ci(read(entry+0x100,8)))
  elif mode=='c':call(0x3211494,root,cp(a))
  elif mode=='r':result_text=str(call(0x3211f24,root,cp(a)))
  elif mode=='q':result_text=str(call(0x3210d84,root,a))
  elif mode=='d':disabled.add(a)
  elif mode=='e':disabled.discard(a);pending.add(a)
  elif mode=='z':clearslot(a,2)
  elif mode=='t':setslot(a,2,b)
  elif mode=='h':h[a]=b;put(state+0x24,h[3],1);put(board_manager+0x3c,h[0]);put(board+0x38,0 if h[10]else filldict,8)
  if fault:result_text='error'
  out.append(result_text+':'+state_text()+':'+ ';'.join(trace))
 cases.append(','.join(map(str,head))+'#'+'/'.join(','.join(map(str,a))for a in cell_data)+'#'+'/'.join(','.join(map(str,a))for a in item_data)+'#'+';'.join(steps));expected.append('|'.join(out))
 return out
def world():
 cells_data=[[i%3,i//3,1,0,0,0,0,0,0,0,0,0,0,1,1]for i in range(12)]
 return [0,0,1,0,0,0,-1,0,0,0,0],cells_data,[[0,1,1,0,0,0]for _ in range(6)]
# Empty vertical columns, filling/board/freeze and item reservation combinations.
for fill in(0,1):
 for current,next_i,target in((0,0,0),(1,1,0),(1,1,1),(0,1,0),(1,0,0),(1,1,2)):
  for kind in range(4):
   for gate in range(6):
    head,cd,iv=world();cd[10][4]=fill;cd[7][9:12]=[next_i,current,target];iv[0][0]=kind
    if gate==1:cd[7][5]=1
    if gate==2:cd[4][6]=1
    if gate==3:iv[0][1]=0
    if gate==4:cd[7][3]=1
    if gate==5:cd[10][13]=0
    run(head,cd,iv,['n,7,1','p,7,1','f,1','f,4','z,1','f,1','s,1,7','c,1','x,1,4,0','x,1,4,2','q,1'])
# Cross-side ordering, special preference and above-item height reservation.
for flags in range(128):
 head,cd,iv=world();cd[3][9:12]=[1,1,0];cd[5][9:12]=[2,2,0]
 cd[4][6]=(flags>>0)&1;cd[3][5]=(flags>>1)&1;cd[5][5]=(flags>>2)&1
 iv[0][3]=(flags>>3)&1;iv[1][3]=(flags>>4)&1;iv[0][2]=(flags>>5)&1;iv[1][2]=(flags>>6)&1
 run(head,cd,iv,['c,1','z,1','x,1,1,0','x,1,1,2','c,4'])
# Native ice and multiple-item blockers, master/fake side membership.
for flags in range(256):
 head,cd,iv=world();head[1]=flags&1;head[2]=(flags>>1)&1;head[3]=(flags>>2)&1;head[4]=1;head[5]=(flags>>3)&1;head[6]=0;head[7]=(flags>>4)&1
 cd[1][7]=(flags>>5)&1;cd[0][7]=(flags>>6)&1;cd[4][10]=1;iv[0][0]=2+(flags>>7);iv[0][4]=1
 cd[0][8]=1;cd[2][5]=1
 run(head,cd,iv,['s,1,4','s,1,-1','s,-1,4','h,8,1','s,1,4','h,8,0','c,1'])
# Lower filling cells actually consume upper-board held and reserved items.
# Exercise queue-first versus incoming-first order, frozen partial transfers,
# duplicate held references and targets already owned by the current board.
for flags in range(128):
 head,cd,iv=world();cd[4][4]=1;cd[4][13]=(flags>>0)&1
 cd[7][3]=1;cd[7][4]=1;cd[7][12]=1;cd[7][9:11]=[3,3]
 cd[10][3]=1;cd[10][4]=1;cd[10][12]=1 if flags&2 else 2;cd[10][9:11]=[4,4]
 cd[7][5]=(flags>>2)&1;cd[10][5]=(flags>>3)&1;iv[2][1]=(flags>>4)&1
 if flags&32:cd[0][11]=3
 iv[2][0]=(flags>>6)&1
 run(head,cd,iv,['r,4','q,1','e,1','f,4','z,4','f,4','q,1','f,1'])
 run(head,cd,iv,['e,1','f,4','r,4','q,1','z,4','f,4','f,1','n,7,4'])
# Callback failure must preserve native target flags and queue partial writes.
for throw_at in range(1,24):
 head,cd,iv=world();head[9]=throw_at;head[4]=1;head[5]=1;head[6]=0;cd[10][4]=1;cd[7][10]=1;cd[7][9]=1;iv[0][0]=1
 run(head,cd,iv,['f,1','c,1','s,1,7','r,10','q,1','n,-1,0','p,-1,0','x,1,-1,0'])
rng=random.Random(0x3210e48)
for seed in range(180):
 head,cd,iv=world();head[0]=rng.randrange(2);head[1]=rng.randrange(2);head[4]=rng.randrange(2);head[5]=rng.randrange(2);head[6]=rng.randrange(-1,12);head[7]=rng.randrange(2)
 for a in cd:
  a[2]=rng.randrange(2);a[3]=rng.randrange(3);a[5]=int(rng.random()<.12);a[6]=int(rng.random()<.18);a[7]=rng.randrange(2);a[8]=rng.randrange(2)
 for a in iv:a[:]=[rng.randrange(4),rng.randrange(2),rng.randrange(2),rng.randrange(2),rng.randrange(2),rng.randrange(2)]
 for j in range(6):
  for col in(9,10,11):cd[rng.randrange(12)][col]=j+1
 for i in range(9,12):
  if rng.random()<.5:cd[i][4]=1;cd[i][12]=rng.randrange(7);cd[i][13]=rng.randrange(2)
 steps=['q,-1','q,0','q,1','q,2']
 for _ in range(9):
  a=rng.randrange(12);b=rng.randrange(-1,12);mode=rng.choice(['f','c','n','p','s','x'])
  steps.append(f'{mode},{a}'+(f',{b}'if mode in('n','p','s','x')else'')+(f',{rng.choice([0,2])}'if mode=='x'else''))
 steps+=['e,1','f,1','d,1','f,1','h,10,1','q,1'];run(head,cd,iv,steps)


def observed_openings(path):
    report = json.loads(path.read_bytes())
    assert report['passed'], 'A completed Unity opening run is required.'
    observed = []
    for level in report['levels']:
        snapshot = level.get('fall_snapshot') or {}
        if not snapshot.get('cells'):
            continue
        board_width, board_height = snapshot['width'], snapshot['height']
        state = snapshot['cells']
        assert len(state) == board_width * board_height
        board_id = snapshot['active_board']
        head = [board_id, 0, 1, int(snapshot['won']), 0, 0, -1, 0, 0, 0, 0]
        layout, models, vacancies = [], [], []
        for index, cell in enumerate(state):
            item_id = 0
            if cell['tiled_id']:
                models.append([0, int(cell['item_available']), int(cell['item_can_cross']),
                               int(cell['item_is_special']), 0, 0])
                item_id = len(models)
            elif cell['cell_type'] == 1:
                vacancies.append(index)
            layout.append([
                index % board_width, index // board_width, cell['cell_type'], cell['board_id'], cell['fill_type'],
                int(cell['frozen']), int(cell['blocked']), 0, 0, item_id, item_id, item_id, 0,
                int(cell['refill_ready']), int(cell['should_find_cross']),
            ])
        assert len(vacancies) == level['empty_after'], level['level']
        for vacancy in vacancies:
            straight = run(head, layout, models, [f'f,{vacancy}'])[0]
            filling = int(straight.split(':', 1)[0])
            straight_target = slots[vacancy][2]
            cross_checked = bool(read(cells[vacancy] + 0x30, 1)) and straight_target == 0
            straight_blocker = None
            for action in trace:
                operation, *args = action.split(',')
                if operation == 'blocked' and layout[int(args[0])][6]:
                    blocked_at = int(args[0])
                elif operation == 'available' and not models[int(args[0]) - 1][1]:
                    blocked_at = positions[int(args[0]) - 1][0]
                else:
                    continue
                straight_blocker = {'cell': layout[blocked_at][:2], 'tiled_id': state[blocked_at]['tiled_id']}
                break
            cross_target = 0
            if cross_checked:
                run(head, layout, models, [f'f,{vacancy}', f'c,{vacancy}'])
                cross_target = slots[vacancy][2]
            filling_ready = filling >= 0 and bool(layout[filling][13])
            point = layout[vacancy][:2]
            assert cross_checked == state[vacancy]['should_find_cross'], (level['level'], point)
            assert not (filling_ready or straight_target or cross_target), (level['level'], point)

            opened = [cell.copy() for cell in layout]
            removed_boxes = []
            for index in range(vacancy + board_width, len(opened), board_width):
                if state[index]['tiled_id'] in (21, 22) and not state[index]['item_available']:
                    opened[index][9:12] = [0, 0, 0]
                    removed_boxes.append(opened[index][:2])
            assert removed_boxes, (level['level'], point, 'Expected a box in the vertical path.')
            opened_straight = run(head, opened, models, [f'f,{vacancy}'])[0]
            opened_filling = int(opened_straight.split(':', 1)[0])
            opened_target = slots[vacancy][2]
            opened_ready = opened_filling >= 0 and bool(opened[opened_filling][13])
            assert opened_ready or opened_target, (level['level'], point, 'Expected an open straight path.')
            observed.append({
                'level': level['level'],
                'cell': point,
                'straight_blocker': straight_blocker,
                'ready_filling_cell': None,
                'straight_candidate': None,
                'cross_checked': cross_checked,
                'cross_candidate': None,
                'after_removing_column_boxes': {
                    'removed_boxes': removed_boxes,
                    'ready_filling_cell': opened[opened_filling][:2] if opened_ready else None,
                    'straight_candidate': opened[positions[opened_target - 1][0]][:2] if opened_target else None,
                },
            })
    assert observed, 'No settled vacancies were captured.'
    return observed


observations = observed_openings(args.observations) if args.observations else None
mono=Path(os.environ.get('ROYAL_MONO_ROOT','C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge'))
with tempfile.TemporaryDirectory(prefix='rm-fall-target-')as directory:
 exe=Path(directory)/'verify.exe';files=('OriginalCellMediation','OriginalGridIterator','OriginalCellFallState','OriginalFillingCell','OriginalFallRefillColumns','OriginalFallTargetPlanner')
 p=subprocess.run([str(mono/'bin/mono.exe'),str(mono/'lib/mono/4.5/mcs.exe'),'-langversion:latest','-out:'+str(exe),str(PROJECT/'SourceCatalog/FallTargetPlannerHarness.cs'),*[str(PROJECT/'Assets/Foundation/Runtime'/(n+'.cs'))for n in files]],capture_output=True,text=True);assert p.returncode==0,p.stdout+p.stderr
 p=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],input='\n'.join(cases)+'\n',capture_output=True,text=True,encoding='utf-8');assert p.returncode==0,p.stderr[-2000:]
 actual=p.stdout.lstrip('\ufeff').splitlines();assert len(actual)==len(expected),(len(actual),len(expected))
 for i,(a,e)in enumerate(zip(actual,expected)):
  if a!=e:
   aa=a.split('|');ee=e.split('|');j=next(j for j,(x,y)in enumerate(zip(aa,ee))if x!=y)
   raise AssertionError((i,j,cases[i],aa[j],ee[j]))
report={'passed':True,'native_sequences':len(cases),'native_commands':commands,'scope':'Actual ARM64 FallManager straight-target/filling search, GetNextItemOfCell, IsFallPossibleFromCellToCell, ShouldFindCross, multiple-item target guard, complete left/right stack traversal and candidate selection, top-board remaining-item queue transfer and current-board filling lookup. Compares concrete bidirectional mediation and target/cross-flag/queue partial state, ordered availability/static/board/ice/virtual callbacks, unknown/missing cells and ice/dictionary boundaries, all ice/master/fake side gates, special-item preference, board/freeze/reservation conditions and callback failures. Actual item availability/static blockers/ice helper, board feature guards and multiple-item behavior, queue bookkeeping, CLR/type/interface/storage are supplied boundaries. Per-item fall motion, full FallManager ManualUpdate/application composition and formal resolution/refill/HUD/settlement remain incomplete.'}
output = 'fall_target_planner_native_verification.json'
if observations is not None:
 report['observed_opening_cells'] = observations
 report['opening_scope'] = 'Captured Unity item availability and cell topology are supplied to both planners. Original ARM64 and the C# planner are compared for straight and eligible cross searches on each vacancy. A separate copied layout tests straight supply after removing the blocking column boxes. Original device rendering and the original path into this settled state require separate evidence.'
 output = 'fall_target_observed_openings_verification.json'
(PROJECT/output).write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
