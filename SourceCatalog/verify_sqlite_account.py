"""Real Windows OS SQLite persistence and captured native database schema check.

Executes project runtime storage/account adapters with actual SQL, independent
connection visibility, commit/reopen/rollback and native sync flags. The original
captured database is read-only; writes target only a temporary copy. No database
contents or identifiers are exported. Mobile transports, full database migration,
account constructor defaults and production startup are not verified.
"""
from pathlib import Path
import hashlib,json,shutil,sqlite3,subprocess,tempfile
PROJECT=Path(__file__).resolve().parents[1];BASE=PROJECT.parent
captured=next((BASE.parent/'RoyalMatch_20260929/filesystem/data/data/com.dreamgames.royalmatch/app_pFiles').glob('U_*'))
source=sqlite3.connect(captured.resolve().as_uri()+'?mode=ro',uri=True)
schema=source.execute('pragma table_info(KeyValue)').fetchall();sql=source.execute('select sql from sqlite_master where name=?',('KeyValue',)).fetchone()[0]
source.close();original_hash=hashlib.sha256(captured.read_bytes()).hexdigest()
mono=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data/MonoBleedingEdge')
sources=['SourceCatalog/SqliteAccountHarness.cs','Assets/Foundation/Runtime/OriginalKeyValueStorage.cs','Assets/Foundation/Runtime/OriginalAccountStorage.cs','Assets/Foundation/Runtime/OriginalWindowsSqliteConnection.cs']
sources+=['Assets/Foundation/Runtime/OriginalNativeSqliteConnection.cs','Assets/Foundation/Runtime/OriginalNativeSqliteApi.cs']
with tempfile.TemporaryDirectory()as directory:
    directory=Path(directory);file=directory/'actual.sqlite';copy=directory/'captured.sqlite';shutil.copy2(captured,copy)
    exe=directory/'SqliteAccount.exe';args=[str(mono.parent/'NetCoreRuntime/dotnet.exe'),str(mono.parent/'DotNetSdkRoslyn/csc.dll'),'-nologo','-nostdlib+',*['-r:'+str(mono/'lib/mono/4.5'/p)for p in('mscorlib.dll','System.dll','System.Core.dll')],'-out:'+str(exe),*[str(PROJECT/p)for p in sources]]
    result=subprocess.run(args,capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    result=subprocess.run([str(mono/'bin/mono.exe'),str(exe),str(file),str(copy)],capture_output=True,text=True,encoding='utf-8');assert result.returncode==0,result.stdout+result.stderr
    checks=int(result.stdout.lstrip('\ufeff').strip());db=sqlite3.connect(file)
    actual_schema=db.execute('pragma table_info(KeyValue)').fetchall();assert actual_schema==schema,(actual_schema,schema)
    account=dict(db.execute('select Key,Value from KeyValue where Key in (?,?,?)',('Level','Coins','Stars')));assert account=={'Level':'2','Coins':'178','Stars':'1'},account
    assert all(row==(0,1)for row in db.execute('select Synced,SyncBlocked from KeyValue where Key in (?,?,?)',('Level','Coins','Stars')))
    db.close()
assert hashlib.sha256(captured.read_bytes()).hexdigest()==original_hash
report={'passed':True,'checks':checks,'scope':__doc__.strip(),'native_captured_schema_sql':sql,'schema_columns':schema,'original_capture_unchanged':True,
 'files':[{'path':p,'sha256':hashlib.sha256((PROJECT/p).read_bytes()).hexdigest()}for p in sources],
 'api_sources':['https://www.sqlite.org/c3ref/open.html','https://www.sqlite.org/c3ref/bind_blob.html','https://www.sqlite.org/c3ref/step.html','https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/custom-versions']}
(PROJECT/'sqlite_account_storage_verification.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
