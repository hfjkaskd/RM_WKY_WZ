from pathlib import Path
import json,re,sys,struct
ROOT=Path(__file__).resolve().parents[1];RAW=ROOT.parent/'assets/RecoveredProject/ExportedProject/Assets'
text=(RAW/'Royal/Scenes/Game/GameScene.unity').read_text(encoding='utf8')
blocks={int(m[2]):(int(m[1]),m[0])for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)\n[\s\S]*?(?=^--- !u!|\Z)',text,re.M)}
scripts={}
base=RAW/'Scripts/Assembly-CSharp/Royal/Scenes/Game/Ui/Bottom'
for p in base.rglob('*.cs.meta'):
    if p.stem[:-3]in ('BottomUi','InGameBoosterManager','InGameBoosterButton','SettingsDialog'):
        scripts[re.search(r'guid: (\w+)',p.read_text())[1]]=p.stem[:-3]
report={'components':{},'hierarchy':[]}
for id,(kind,b)in blocks.items():
    if kind!=114:continue
    script=re.search(r'm_Script: \{fileID: \d+, guid: (\w+)',b)
    if script and script[1]in scripts:
        name=scripts[script[1]];report['components'][str(id)]={'type':name,'yaml':b}
        if name=='BottomUi':
            go=int(re.search(r'm_GameObject: \{fileID: (\d+)\}',b)[1]);root=int(re.search(r'component: \{fileID: (\d+)\}',blocks[go][1])[1])
def walk(t):
    b=blocks[t][1];go=int(re.search(r'm_GameObject: \{fileID: (\d+)\}',b)[1]);g=blocks[go][1]
    report['hierarchy'].append({'transform':t,'go':go,'name':re.search(r'm_Name: (.*)',g)[1],
        'active':int(re.search(r'm_IsActive: (\d+)',g)[1]),'position':re.search(r'm_LocalPosition: (.*)',b)[1],
        'parent':int(re.search(r'm_Father: \{fileID: (\d+)\}',b)[1]),
        'components':[(int(x),blocks[int(x)][0])for x in re.findall(r'component: \{fileID: (\d+)\}',g)]})
    for child in re.findall(r'^  - \{fileID: (\d+)\}',b,re.M):walk(int(child))
walk(root)
report['root_transform']=root
sys.path.insert(0,str(ROOT.parent/'tools/python'))
from elftools.elf.elffile import ELFFile
with(ROOT.parent.parent/'RoyalMatch_20260929/apk_unpacked/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so').open('rb')as f:
    elf=ELFFile(f)
    for s in elf.iter_segments():
        addr=0x4053060
        if s['p_type']=='PT_LOAD'and s['p_vaddr']<=addr<s['p_vaddr']+s['p_filesz']:
            f.seek(s['p_offset']+addr-s['p_vaddr']);report['booster_unlock_levels']=list(struct.unpack('<4i',f.read(16)))
    report['native_constants']={}
    for label,addr in {'new_lock_scale':0x3fb2544,'old_lock_scale':0x3fb2580,'new_lock_y':0x3fb26e8,'bottom_y_offset':0x3fcd434}.items():
        for s in elf.iter_segments():
            if s['p_type']=='PT_LOAD'and s['p_vaddr']<=addr<s['p_vaddr']+s['p_filesz']:
                f.seek(s['p_offset']+addr-s['p_vaddr']);report['native_constants'][label]={'address':hex(addr),'float':struct.unpack('<f',f.read(4))[0]}
(ROOT/'SourceCatalog/bottom_scene_inspection.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print(json.dumps({'components':{k:v['type']for k,v in report['components'].items()},'hierarchy_objects':len(report['hierarchy']),
    'booster_unlock_levels':report['booster_unlock_levels'],'native_constants':report['native_constants'],
    'root_children':[n for n in report['hierarchy']if n['parent']==root]},ensure_ascii=False,indent=2))
