from pathlib import Path
import argparse, hashlib, json, os, re, subprocess, sys, zipfile, shutil

sys.stdout.reconfigure(encoding='utf-8')
parser = argparse.ArgumentParser(description='Build the filtered current source as signed AAB, then derive its APK.')
parser.add_argument('--project', required=True, type=Path)
parser.add_argument('--signing-directory', required=True, type=Path)
parser.add_argument('--max-package-mib', type=int, default=200)
args = parser.parse_args()
root = args.project.resolve()
(root / 'Logs').mkdir(exist_ok=True)
editor = Path(r'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor')
android = editor / 'Data/PlaybackEngines/AndroidPlayer'
signing_source = args.signing_directory.resolve()
signing = root / 'Logs/ReleaseSigningFiltered'
output = root / 'Builds/Release/1.0.1_101_filtered'
output.mkdir(parents=True, exist_ok=True)
signing.mkdir(parents=True, exist_ok=True)
keystore = signing / 'release.jks'
assert not keystore.exists()
shutil.copy2(signing_source / 'Alfanium Indonesia Digital.jks', keystore)
assert keystore.read_bytes() == (signing_source / 'Alfanium Indonesia Digital.jks').read_bytes()
raw = (signing_source / 'readme.txt').read_text(encoding='utf-8-sig').strip()
passwords = list(dict.fromkeys([raw] + re.findall(r'[\x21-\x7e]+', raw)))
env = os.environ.copy()
keytool = android / 'OpenJDK/bin/keytool.exe'
for candidate in passwords:
    env['GEMWINK_KEYSTORE_PASSWORD'] = candidate
    result = subprocess.run([str(keytool), '-J-Duser.language=en', '-list', '-v', '-keystore', str(keystore),
                             '-storepass:env', 'GEMWINK_KEYSTORE_PASSWORD'], cwd=signing, env=env,
                            capture_output=True, text=True, encoding='utf-8', errors='replace')
    if result.returncode == 0:
        aliases = re.findall(r'^Alias name:\s*(.+)$', result.stdout, re.M)
        assert aliases == ['key0'] and 'PrivateKeyEntry' in result.stdout
        certificate = re.findall(r'SHA256:\s*([0-9A-F:]+)', result.stdout)[0]
        assert certificate == '43:99:14:58:E7:A3:5B:9E:1A:B8:5A:1D:4A:BF:AE:5A:B4:73:CA:9C:B4:10:14:FD:40:18:41:A6:8D:96:68:B6'
        break
else:
    raise RuntimeError('Supplied signing credentials could not open the keystore.')
env.update(GEMWINK_KEYSTORE_PATH=str(keystore), GEMWINK_KEY_ALIAS='key0', GEMWINK_KEY_PASSWORD=candidate)

def run_logged(name, command):
    name = name.replace('release-', 'release-filtered-', 1)
    result = subprocess.run([str(x) for x in command], cwd=root, env=env,
                            capture_output=True, text=True, encoding='utf-8', errors='replace')
    text = result.stdout + result.stderr
    for secret in passwords:
        if secret: text = text.replace(secret, '[REDACTED]')
    (root / 'Logs' / (name + '.log')).write_text(text, encoding='utf-8')
    print(name + ': exit ' + str(result.returncode), flush=True)
    if result.returncode:
        raise RuntimeError(name + ' failed; see its redacted log.')
    return text

assert 'guid: ab54b39b81dbfb24b8cdeffcfd5e851d' in (root / 'ProjectSettings/ProjectSettings.asset').read_text(encoding='utf-8-sig')

# Refuse a full-source build if the resource selection was not applied.
snapshot = json.loads((root.parent / 'source-snapshot.json').read_text(encoding='utf-8'))
selection_bytes = Path(snapshot['resourceSelection']).read_bytes()
assert hashlib.sha256(selection_bytes).hexdigest() == snapshot['selectionSha256'], 'Resource selection changed after preparation.'
selected = json.loads(selection_bytes)
if isinstance(selected, dict):
    selected = selected['files']
actual = {p.relative_to(root).as_posix() for p in (root / 'Assets/Resources').rglob('*') if p.is_file()}
assert actual == set(selected) - {'Assets/Resources.meta'}, 'Build Resources do not match the audited selection.'
assert all((root / f'Assets/Resources/Levels/level_{n}.json').is_file() for n in range(1, 101))
assert len([p for p in actual if not p.endswith('.meta')]) < 5000, 'Unexpected resource count; review the selection.'

# Editor-only checks are copied into the isolated build project, never the source Assets.
editor_check = root / 'Assets/Editor/GemwinkFilteredRelease.cs'
assert not editor_check.exists()
editor_check.parent.mkdir(parents=True, exist_ok=True)
shutil.copy2(root / 'Tools/Release/GemwinkFilteredRelease.cs', editor_check)
editor_check.with_suffix('.cs.meta').write_text('fileFormatVersion: 2\nguid: c4bfc26ab42c41d1a954a5b82efb153a\n', encoding='utf-8')

# Recover the actual private key before spending time on a Unity build.
probe = signing / 'signing-probe.jar'
with zipfile.ZipFile(probe, 'w') as archive:
    archive.writestr('META-INF/MANIFEST.MF', 'Manifest-Version: 1.0\n\n')
    archive.writestr('probe.txt', 'Signing verification only.\n')
run_logged('release-signing-probe', [android / 'OpenJDK/bin/jarsigner.exe', '-keystore', keystore,
    '-storepass:env', 'GEMWINK_KEYSTORE_PASSWORD', '-keypass:env', 'GEMWINK_KEY_PASSWORD', probe, 'key0'])

aab = output / 'GemwinkCastle-1.0.1-101-release.aab'
assert not aab.exists(), 'Existing AAB would be overwritten; choose a new output.'
log = root / 'Logs/release-filtered-aab-1.0.1-101.log'
startup = subprocess.STARTUPINFO()
startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
startup.wShowWindow = subprocess.SW_HIDE
process = subprocess.Popen([str(editor / 'Unity.exe'), '-batchmode', '-nographics', '-quit',
    '-projectPath', str(root), '-buildTarget', 'Android', '-executeMethod', 'GemwinkFilteredRelease.BuildAab',
    '-customBuildPath', str(aab), '-logFile', str(log)], env=env, cwd=root,
    startupinfo=startup, creationflags=subprocess.CREATE_NO_WINDOW)
print('Unity release AAB build started; PID=' + str(process.pid), flush=True)
(root / 'Logs/release-filtered-build-state.json').write_text(json.dumps({'pid': process.pid, 'aab': str(aab), 'stage': 'unity-aab'}))
exit_code = process.wait()
print('Unity release build exit=' + str(exit_code), flush=True)
build_log = log.read_text(encoding='utf-8-sig', errors='replace')
if (exit_code or not aab.is_file() or 'Obfuscation failed' in build_log
        or '[AutoBuild_Android] Release obfuscation verified.' not in build_log
        or '[GemwinkFilteredRelease] Resource preflight passed:' not in build_log):
    raise RuntimeError('Release AAB did not pass Unity and Obfuz build checks.')

assert aab.stat().st_size < args.max_package_mib * 1024 * 1024, 'AAB exceeded the package-size gate; inspect its resource report.'
java = android / 'OpenJDK/bin/java.exe'
bundletool = android / 'Tools/bundletool-all-1.17.2.jar'
run_logged('release-bundle-validate', [java, '-jar', bundletool, 'validate', '--bundle=' + str(aab)])
verify = run_logged('release-aab-signature', [android / 'OpenJDK/bin/jarsigner.exe', '-verify', '-verbose', '-certs', aab])
assert 'jar verified.' in verify
aab_certificate = run_logged('release-aab-certificate', [keytool, '-J-Duser.language=en', '-printcert', '-jarfile', aab])
assert certificate in aab_certificate
manifest = run_logged('release-aab-manifest', [java, '-jar', bundletool, 'dump', 'manifest', '--bundle=' + str(aab), '--module=base'])
assert 'com.bangsawan.gemwinkcastle' in manifest and 'versionCode="101"' in manifest and 'versionName="1.0.1"' in manifest
assert 'android:debuggable="true"' not in manifest

# APK is generated only from the signed AAB, with the identical supplied key.
password_file = signing / 'bundletool-password.txt'
apks = output / 'GemwinkCastle-1.0.1-101-universal.apks'
password_file.write_text(candidate, encoding='utf-8')
try:
    run_logged('release-aab-to-apk', [java, '-jar', bundletool, 'build-apks', '--bundle=' + str(aab),
        '--output=' + str(apks), '--mode=universal', '--ks=' + str(keystore), '--ks-key-alias=key0',
        '--ks-pass=file:' + str(password_file), '--key-pass=file:' + str(password_file)])
finally:
    password_file.unlink(missing_ok=True)
apk = output / 'GemwinkCastle-1.0.1-101-release.apk'
with zipfile.ZipFile(apks) as archive:
    apk.write_bytes(archive.read('universal.apk'))
apksigner = android / 'SDK/build-tools/34.0.0/lib/apksigner.jar'
apk_sig = run_logged('release-apk-signature', [java, '-jar', apksigner, 'verify', '--verbose', '--print-certs', apk])
assert certificate.replace(':', '').lower() in apk_sig.lower()
apk_manifest = run_logged('release-apk-manifest', [android / 'SDK/build-tools/34.0.0/aapt.exe', 'dump', 'badging', apk])
assert "name='com.bangsawan.gemwinkcastle'" in apk_manifest
assert "versionCode='101'" in apk_manifest and "versionName='1.0.1'" in apk_manifest
assert 'application-debuggable' not in apk_manifest
assert "native-code: 'arm64-v8a'" in apk_manifest
assert apk.stat().st_size < args.max_package_mib * 1024 * 1024, 'APK exceeded the package-size gate.'
files = [{'file': p.name, 'bytes': p.stat().st_size, 'sha256': hashlib.sha256(p.read_bytes()).hexdigest()} for p in (aab, apk)]
(output / 'release-verification.json').write_text(json.dumps({'package': 'com.bangsawan.gemwinkcastle',
    'version': '1.0.1', 'versionCode': 101, 'certificate_sha256': certificate, 'apk_origin': aab.name,
    'resource_snapshot': str(root.parent / 'source-snapshot.json'), 'resource_file_count': len(actual),
    'icon_guid': 'ab54b39b81dbfb24b8cdeffcfd5e851d', 'files': files}, indent=2), encoding='utf-8')
print('AAB and AAB-derived APK verified.', flush=True)
