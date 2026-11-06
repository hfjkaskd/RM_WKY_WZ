"""Rebind original end-level particle prefabs to owned Unity components offline."""
from pathlib import Path
import re,uuid,json,hashlib
PROJECT=Path(__file__).resolve().parents[1]
ORIGINAL=PROJECT.parent/'assets/RecoveredProject/ExportedProject/Assets'
def meta(path,kind='DefaultImporter'):
 p=Path(str(path)+'.meta')
 if not p.exists():p.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n'+kind+':\n  externalObjects: {}\n',encoding='utf8')
 return re.search(r'guid: (\w+)',p.read_text(encoding='utf8'))[1]
def run():
 runtime=PROJECT/'Assets/Foundation/Runtime'
 for name in ['OriginalRemainingMovesItemReplacer','OriginalRemainingMovesReplacementServices','OriginalRemainingMovesParticles','OriginalRemainingMovesHitParticles']:
  meta(runtime/(name+'.cs'),'MonoImporter')
 evidence=[]
 for name in ['RemainingMovesParticles','RemainingMovesHitParticles']:
  source=ORIGINAL/'GameObject'/(name+'.prefab');s=source.read_text(encoding='utf8')
  scripts=re.findall(r'm_Script: \{fileID: 11500000, guid: (\w+), type: 3\}',s)
  assert len(scripts)==1,scripts
  s=s.replace(scripts[0],meta(runtime/('Original'+name+'.cs'),'MonoImporter'))
  target=PROJECT/'Assets/Resources/Foundation/Prefabs'/('Original'+name+'.prefab')
  target.write_text(s,encoding='utf8');meta(target,'PrefabImporter')
  evidence.append({'name':name,'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest()})
 foundation=PROJECT/'Assets/Foundation/Prefabs/Foundation.prefab'
 s=foundation.read_text(encoding='utf8')
 if '  remainingMovesPrefabPath:' not in s:
  s=s.replace('  tntRocketComboUsePrefabPath:', '  remainingMovesPrefabPath: Foundation/Prefabs/OriginalRemainingMovesParticles\n  remainingMovesHitPrefabPath: Foundation/Prefabs/OriginalRemainingMovesHitParticles\n  tntRocketComboUsePrefabPath:')
 if '  remainingMovesPrewarmCount:' not in s:s=s.replace('  remainingMovesHitPrefabPath: Foundation/Prefabs/OriginalRemainingMovesHitParticles\n','  remainingMovesHitPrefabPath: Foundation/Prefabs/OriginalRemainingMovesHitParticles\n  remainingMovesPrewarmCount: 10\n')
 foundation.write_text(s,encoding='utf8')
 audio=PROJECT/'Assets/Foundation/Settings/GameSfxAssets.asset';s=audio.read_text(encoding='utf8')
 for id,name in [(306,'remaining_item_land1'),(307,'remaining_item_land2'),(308,'remaining_move_throw1')]:
  if f'  - Type: {id}\n' not in s:s+=f'  - Type: {id}\n    ResourcePath: Recovered/Resources/ui/{name}\n'
 audio.write_text(s,encoding='utf8')
 print(json.dumps(evidence))
if __name__=='__main__':run()
