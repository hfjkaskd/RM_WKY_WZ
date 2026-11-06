"""Exercise actual move/state/WinFail/loss and main-queue runtime composition.

Uses a fixture accepted swap and explicit UiDialog close completion. Goal/auto
models, DisableCounter, session/account/event/King/audio/orientation owners and
prefab rendering remain fixtures. The banner animation, Ego purchase/cancel,
actual failure persistence and production FoundationController are not bound.
"""
from pathlib import Path
import hashlib,json,subprocess,tempfile
PROJECT=Path(__file__).resolve().parents[1]
sources=['SourceCatalog/LossQueueHarness.cs',*['Assets/Foundation/Runtime/'+name+'.cs'for name in
    ('OriginalMoveManager','OriginalGameOperations','OriginalGameStateManager','OriginalWinFailManager','OriginalFlowExecutor','OriginalLossFlowCoordinator','OriginalOutOfMovesBannerAction','OriginalEgoDialogAction','OriginalOutOfMovesContinuation','OriginalActiveLevelData')]]
mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
with tempfile.TemporaryDirectory()as directory:
    exe=Path(directory)/'LossQueue.exe'
    command=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',*['-r:'+str(mono/'lib/mono/4.5'/p)for p in('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),*[str(PROJECT/p)for p in sources]]
    result=subprocess.run(command,capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(mono/'bin/mono.exe'),str(exe)],capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    assert result.stdout.lstrip('\ufeff').strip()=='passed:6',result.stdout
report={'passed':True,'scenarios':6,'scope':__doc__.strip(),'files':[{'path':p,'sha256':hashlib.sha256((PROJECT/p).read_bytes()).hexdigest()}for p in sources]}
(PROJECT/'loss_queue_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
