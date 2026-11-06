"""Explicit static prefab repair from preserved native goal slots; no runtime UI authoring."""
from pathlib import Path
import hashlib,json,re
ROOT=Path(__file__).resolve().parent.parent
SOURCE=ROOT.parent/'assets/RecoveredProject/ExportedProject/Assets/GameObject/LevelFailDialog.prefab'
TARGET=ROOT/'Assets/Resources/Foundation/Prefabs/CoreLevelFailDialog.prefab'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def blocks(s):return {re.search(r'&(-?\d+)\n',b)[1]:b for b in re.split(r'(?=^--- !u!)',s,flags=re.M)[1:]}
def field(b,n):return re.search(r'^  '+n+r': \{fileID: (-?\d+)',b,re.M)[1]
def change(b,n,value):
    b,count=re.subn(r'(^  '+n+r': ).*',lambda m:m[1]+value,b,flags=re.M);assert count==1,(n,count);return b
raw=blocks(SOURCE.read_text());text=TARGET.read_text();dest=blocks(text)
before=sha(TARGET);native=raw['114119337346424409']
def array(name):return re.findall(r'fileID: (\d+)',re.search(r'^  '+name+r':\n((?:  - .*\n)+)',native,re.M)[1])
def subtree(t):
    b=raw[t];yield t;go=field(b,'m_GameObject');yield go
    for c in re.findall(r'  - component: \{fileID: (\d+)',raw[go]):
        if c!=t:yield c
    section=b[b.index('  m_Children:'):b.index('  m_Father:')]
    for c in re.findall(r'fileID: (\d+)',section):yield from subtree(c)
root_id=field(native,'goalParent');ids=list(subtree(root_id));assert len(ids)==len(set(ids))
assert not (set(ids)&set(dest)), 'Only repair the variant which removed the original goal subtree.'
parent=field(dest['1136518645586112313'],'m_Father')
child_section=dest[parent][dest[parent].index('  m_Children:'):dest[parent].index('  m_Father:')]
assert re.findall(r'fileID: (\d+)',child_section)==['1136518645586112313']
dest[parent]=dest[parent].replace('  m_Children:\n','  m_Children:\n  - {fileID: '+root_id+'}\n')
for i in ids:
    assert raw[i].startswith(('--- !u!1 ','--- !u!4 ','--- !u!212 ')),raw[i][:50]
    dest[i]=raw[i]
dest[root_id]=change(dest[root_id],'m_Father','{fileID: '+parent+'}')
script_guid=(ROOT/'Assets/Foundation/Runtime/CoreLevelFailDialog.cs.meta').read_text().split('guid: ')[1].splitlines()[0]
owner=next(k for k,b in dest.items()if 'guid: '+script_guid in b)
extra='  retryLabel: {fileID: 5165621530758521443}\n  goalParent: {fileID: '+root_id+'}\n'
for target,source in [('goalSlots','goals'),('icons','icons'),('shadows','shadows'),('goalResults','goalResults'),('goalShadows','goalShadows')]:
    extra+='  '+target+':\n'+''.join('  - {fileID: '+i+'}\n'for i in array(source))
for n in ['checkIcon','redCrossIcon','checkShadow','redCrossShadow']:
    extra+=re.search(r'^  '+n+r': .*\n',native,re.M)[0]
guid=(ROOT/'Assets/Foundation/Settings/GoalUiAssets.asset.meta').read_text().split('guid: ')[1].splitlines()[0]
extra+='  goalAssets: {fileID: 11400000, guid: '+guid+', type: 2}\n  goalDistances:\n  - 0\n  - 0.9\n  - 0.8\n  - 0.66\n  fourGoalScale: 1.1\n  checkShadowY: -0.025\n  crossShadowY: -0.011\n'
dest[owner]+=extra
# Recover original nested sorting keys by exact original object path. Include
# standard Button canvases, whose Graphic child replaces the source renderer.
def maps(bs):
    transforms={field(b,'m_GameObject'):k for k,b in bs.items()if b.startswith(('--- !u!4 ','--- !u!224 '))}
    def path(go):
        parts=[];t=transforms[go]
        while field(bs[t],'m_Father')!='0':
            g=field(bs[t],'m_GameObject');parts.append(re.search(r'^  m_Name: (.*)',bs[g],re.M)[1]);t=field(bs[t],'m_Father')
        return '/'.join(reversed(parts))
    return transforms,path
raw_ts,raw_path=maps(raw);_,dst_path=maps(dest)
keys={}
for k,b in raw.items():
    if not b.startswith(('--- !u!23 ','--- !u!212 ')):continue
    go=field(b,'m_GameObject');t=raw_ts[go];groups=[]
    while t!='0':
        g=field(raw[t],'m_GameObject')
        for c in re.findall(r'  - component: \{fileID: (\d+)',raw[g]):
            if raw[c].startswith('--- !u!210 '):groups.append(int(re.search(r'm_SortingOrder: (-?\d+)',raw[c])[1]))
        t=field(raw[t],'m_Father')
    keys[raw_path(go)]=tuple(reversed(groups))+(int(re.search(r'm_SortingOrder: (-?\d+)',b)[1]),)
ordered=[]
for k,b in dest.items():
    if not b.startswith(('--- !u!23 ','--- !u!212 ','--- !u!223 ')):continue
    path=dst_path(field(b,'m_GameObject'))
    if b.startswith('--- !u!223 '):assert path.endswith('/Graphic');path=path[:-8]
    assert path in keys,path
    ordered.append((keys[path],k))
ordered.sort(key=lambda pair:pair[0])
for order,(_,k)in enumerate(ordered):
    dest[k]=change(dest[k],'m_SortingOrder',str(order));dest[k]=change(dest[k],'m_SortingLayerID','2046106761')
TARGET.write_text(text[:text.index('--- !u!')]+''.join(dest.values()),encoding='utf-8',newline='\n')
# Reproducible source paths for explicit Unity asset authoring.
source_paths=ROOT/'Assets/Foundation/Editor/CoreUiSourcePaths.cs';s=source_paths.read_text();marker='    }\n}'
addition=''
for target,source in [('goalSlots','goals'),('icons','icons'),('shadows','shadows'),('goalResults','goalResults'),('goalShadows','goalShadows')]:
    paths=[raw_path(field(raw[i],'m_GameObject'))for i in array(source)]
    addition+='        public static readonly string[] '+target+' = { '+', '.join(json.dumps(p)for p in paths)+' };\n'
assert s.count(marker)==1;source_paths.write_text(s.replace(marker,addition+marker),encoding='utf-8',newline='\n')
proof={'source':str(SOURCE.resolve()),'source_sha256':sha(SOURCE),'target':str(TARGET.resolve()),'before_sha256':before,'after_sha256':sha(TARGET),
 'copied_static_blocks':ids,'native_methods':['0x134cd24','0x13504bc','0x13506a0','0x1350ab8'],
 'native_float_addresses':{'distances_2_3_4':'0x3fcd2c4','scale_default_four':'0x3fbe7e0','check_shadow_y':'0x3fb2940','cross_shadow_y':'0x3fcd1a8'},
 'scope':'Restore original four static goal slots, sprites, relative sorting keys and native ordinary-board layout inputs. Runtime fills goal state. No peripheral slider/lives/shop restored; no full rendered parity claim.'}
(ROOT/'SourceCatalog/core_fail_goals_authoring.json').write_text(json.dumps(proof,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'copied_blocks':len(ids),'render_layers':len(ordered),'target_sha256':proof['after_sha256']}))
