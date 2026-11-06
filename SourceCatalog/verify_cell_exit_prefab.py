"""Verify the restored Box RootTransform against original serialized data."""
from pathlib import Path
import sys,re,json,hashlib
PROJECT=Path(__file__).resolve().parents[1];BASE=PROJECT.parent
sys.path.insert(0,str(BASE/'tools/git_api'))
from dulwich.repo import Repo
relative='Assets/Resources/Recovered/GameObject/BoxItemPrefab.prefab'
def blocks(s):return {int(m.group(1)):m.group(0).strip()for m in re.finditer(r'--- !u!\d+ &(\d+)\n.*?(?=--- !u!|\Z)',s,re.S)}
current=(PROJECT/relative).read_text(encoding='utf-8-sig');new=blocks(current)
raw=blocks((BASE/'assets/RecoveredProject/ExportedProject/Assets/GameObject/BoxItemPrefab.prefab').read_text(encoding='utf-8-sig'))
with Repo(str(PROJECT))as repo:
 head=(sys.argv[1]if len(sys.argv)>1 else 'a9fc60de88f74cfa2b8d5750d65264864c8eedae').encode()
 _,sha=repo[repo[head].tree].lookup_path(repo.object_store.__getitem__,relative.encode());old=blocks(repo[sha].data.decode('utf-8-sig'))
added=set(new)-set(old);removed=set(old)-set(new);changed={i for i in set(new)&set(old)if new[i]!=old[i]}
assert added=={114061268324235159}and not removed and changed=={1730242686860875,114099118695303065},(added,removed,changed)
assert new[114061268324235159]==raw[114061268324235159].replace('98d7c11aef979f39dccadbc0ed1b4acf','137d98524eb1485da60ad90960078210')
assert new[1730242686860875].replace('  - component: {fileID: 114061268324235159}\n','')==old[1730242686860875]
assert new[114099118695303065].replace('  rootTransform: {fileID: 114061268324235159}\n','')==old[114099118695303065]
assert 'rootTransform: {fileID: 114061268324235159}'in raw[114099118695303065]
report={'passed':True,'comparison_head':head.decode(),'prefab':relative,'sha256':hashlib.sha256((PROJECT/relative).read_bytes()).hexdigest(),'unchanged_blocks':len(old)-len(changed),'added_original_root_component':114061268324235159,'changed_reference_owners':sorted(changed),'scope':'Exact original RootTransform component/GO/transform references restored with the project runtime script GUID. All prior layout, renderer, sprite, sorting, transform and hierarchy blocks are preserved; only root component registration and Box view reference change. No rendered-original parity claim.'}
(PROJECT/'SourceCatalog/cell_exit_prefab_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
