"""Decode original King keyframe data outside Unity; no sampled mesh frames."""
from pathlib import Path
import hashlib, json, subprocess
ROOT=Path(__file__).resolve().parents[1]
RECOVERY=ROOT.parent
EDITOR=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data')
BAKER=RECOVERY/'tools/SpineOfflineBaker'
RAW=RECOVERY/'assets/RecoveredProject/ExportedProject/Assets/TextAsset'
OUTPUT=ROOT/'Assets/Resources/Foundation/Animation/OriginalKing.bytes'
OUTPUT.parent.mkdir(parents=True,exist_ok=True)
EXE=BAKER/'ExportKingRig.exe'
subprocess.run([str(EDITOR/'NetCoreRuntime/dotnet.exe'),str(EDITOR/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',
    *['-r:'+str(EDITOR/'MonoBleedingEdge/lib/mono/4.5'/n)for n in ('mscorlib.dll','System.dll','System.Core.dll')],
    '-out:'+str(EXE),str(ROOT/'SourceCatalog/ExportKingRig.cs'),*[str(p)for p in sorted((BAKER/'src').rglob('*.cs'))]],check=True)
subprocess.run([str(EDITOR/'MonoBleedingEdge/bin/mono.exe'),str(EXE),str(RAW/'King.atlas.txt'),str(RAW/'King.skel.bytes'),str(OUTPUT)],check=True)
Path(str(OUTPUT)+'.meta').write_text('fileFormatVersion: 2\nguid: 41a4d990c8784810bd96350e8fe34ce1\nTextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
report={'original_skeleton_sha256':hashlib.sha256((RAW/'King.skel.bytes').read_bytes()).hexdigest(),
        'original_atlas_sha256':hashlib.sha256((RAW/'King.atlas.txt').read_bytes()).hexdigest(),
        'export_sha256':hashlib.sha256(OUTPUT.read_bytes()).hexdigest(),'bytes':OUTPUT.stat().st_size,
        'scale':0.007142857,'mesh_frame_baking':False,'third_party_runtime_in_assets':False}
(ROOT/'SourceCatalog/king_data_export.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report,indent=2))
