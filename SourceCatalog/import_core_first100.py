"""Import only original ordinary levels1–100 and author their configured catalog.

Payloads are copied byte-for-byte from the recovered decoded configurations.
No layout/goal/item is substituted to make an unsupported level appear playable.
The content audit describes dependencies, not completed gameplay support.
"""
from pathlib import Path
import collections,hashlib,json,uuid

ROOT=Path(__file__).resolve().parents[1];RECOVERY=ROOT.parent
rows=json.loads((RECOVERY/'configs/level_index.json').read_text(encoding='utf-8'))
rows=[v for v in rows if v['name'].isdigit()and 1<=int(v['name'])<=100]
assert len(rows)==100 and {int(v['name'])for v in rows}==set(range(1,101))
rows.sort(key=lambda v:int(v['name']))
namespace=uuid.UUID('d9373058-d259-497a-a5fe-3d15d15fb442')
manifest=[];all_ids=collections.Counter();first_id={};static_first={};goal_first={}
yaml=['%YAML 1.1','%TAG !u! tag:unity3d.com,2011:','--- !u!114 &11400000','MonoBehaviour:',
    '  m_ObjectHideFlags: 0','  m_CorrespondingSourceObject: {fileID: 0}','  m_PrefabInstance: {fileID: 0}',
    '  m_PrefabAsset: {fileID: 0}','  m_GameObject: {fileID: 0}','  m_Enabled: 1','  m_EditorHideFlags: 0',
    '  m_Script: {fileID: 11500000, guid: 116e9c8120164a01a9ce4287f0cc4b37, type: 3}',
    '  m_Name: CoreLevelsFirst100','  m_EditorClassIdentifier: ','  levels:']
for row in rows:
    number=int(row['name']);source=RECOVERY/row['decoded'];data=source.read_bytes();level=json.loads(data)
    assert level['Name'].startswith(f'{number:05d}') and level['Move']>0
    grid=level['Grid'];assert len(grid['Items'])==grid['Width']*grid['Height']
    target=ROOT/f'Assets/Resources/Levels/level_{number}.json'
    if target.exists():assert target.read_bytes()==data,('Existing level differs from recovered source',number)
    else:target.write_bytes(data)
    meta=target.with_suffix('.json.meta')
    if not meta.exists():meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid5(namespace,str(number)).hex+'\nTextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',encoding='utf-8')
    item_counts=collections.Counter(grid['Items']);all_ids.update(item_counts)
    for key in item_counts:first_id.setdefault(key,number)
    static_counts=collections.Counter()
    for cell in grid.get('Cells',[]):
        for key,value in cell.items():
            if key not in ('_absent_fields','X','Y') and value not in (0,None,False,[],{}):static_counts[key]+=1;static_first.setdefault(key,number)
    goals=[v['Goal']for v in level.get('Goals',[])]
    for goal in goals:goal_first.setdefault(goal,number)
    manifest.append({'number':number,'original_name':level['Name'],'resource_path':f'Levels/level_{number}',
        'decoded_source':row['decoded'],'raw_source':row['raw'],'sha256':hashlib.sha256(data).hexdigest(),
        'moves':level['Move'],'width':grid['Width'],'height':grid['Height'],'goal_types':goals,
        'raw_item_ids':dict(sorted(item_counts.items())),'nonzero_cell_fields':dict(sorted(static_counts.items())),
        'scroll':bool(level.get('Scroll',False)),'shifter_boards':len(level.get('ShifterGrids',[])),
        'water_cells':(level.get('WaterData')or{}).get('WaterReceivableCellCount',0)})
    yaml.extend([f'  - number: {number}',f"    originalName: {level['Name']}",f'    resourcePath: Levels/level_{number}'])
asset=ROOT/'Assets/Foundation/Settings/CoreLevelsFirst100.asset';asset.write_text('\n'.join(yaml)+'\n',encoding='utf-8')
asset.with_suffix('.asset.meta').write_text('fileFormatVersion: 2\nguid: b64942340dc34b45be4db362d5152713\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',encoding='utf-8')
report={'requested_first_level':1,'requested_last_level':100,'peripheral_systems_in_scope':False,'original_payloads_imported':100,
    'all100_levels_playable':False,'formal_production_win_fail_end_retry_next_level_bound':False,
    'formal_account_and_area_startup_removed':True,'all_existing_peripheral_startup_removed':False,'raw_item_first_appearance':dict(sorted(first_id.items())),
    'nonzero_cell_field_first_appearance':dict(sorted(static_first.items())),'goal_first_appearance':dict(sorted(goal_first.items())),
    'levels':manifest,'scope':__doc__.strip()+' Current formal board still rejects unsupported item/static/water/shifter compositions and is gated to the first level. Formal account/area initialization is removed under the new no-peripherals scope; session presentation snapshots use serialized settings. Existing booster inventory/purchase UI dependencies still require core-only composition. Runtime catalog load and content availability do not prove full gameplay/visual parity.'}
(ROOT/'SourceCatalog/core_first100_manifest.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'imported':100,'range':[1,100],'unique_raw_item_ids':len(all_ids),'first_item_appearance':report['raw_item_first_appearance'],
    'first_nonzero_cell_fields':report['nonzero_cell_field_first_appearance'],'first_goal_appearance':report['goal_first_appearance'],'all100_levels_playable':False},indent=2))
