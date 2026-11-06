from pathlib import Path
import subprocess
import sys
ROOT=Path(__file__).resolve().parents[1];RECOVERY=ROOT.parent
EDITOR=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data');BAKER=RECOVERY/'tools/SpineOfflineBaker'
RAW=RECOVERY/'assets/RecoveredProject/ExportedProject/Assets/TextAsset';EXE=BAKER/'KingRigHarness.exe'
subprocess.run([str(EDITOR/'NetCoreRuntime/dotnet.exe'),str(EDITOR/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',
    *['-r:'+str(EDITOR/'MonoBleedingEdge/lib/mono/4.5'/n)for n in ('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(EXE),
    str(ROOT/'SourceCatalog/KingRigHarness.cs'),str(ROOT/'SourceCatalog/KingTrackReference.cs'),str(ROOT/'SourceCatalog/KingMeshReference.cs'),str(ROOT/'Assets/Foundation/Runtime/OriginalKingMesh.cs'),str(ROOT/'Assets/Foundation/Runtime/OriginalKingRigData.cs'),str(ROOT/'Assets/Foundation/Runtime/OriginalKingPose.cs'),str(ROOT/'Assets/Foundation/Runtime/OriginalKingAnimationTrack.cs'),
    *[str(p)for p in sorted((BAKER/'src').rglob('*.cs'))]],check=True)
subprocess.run([str(EDITOR/'MonoBleedingEdge/bin/mono.exe'),str(EXE),str(RAW/'King.atlas.txt'),str(RAW/'King.skel.bytes'),
    str(ROOT/'Assets/Resources/Foundation/Animation/OriginalKing.bytes'),str(ROOT/'king_rig_reference_verification.json'),*sys.argv[1:]],check=True)
