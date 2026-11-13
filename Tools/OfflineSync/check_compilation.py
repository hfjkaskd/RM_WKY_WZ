"""Compile changed assemblies with Unity-generated arguments; write only to Temp.

Run after Unity has imported this project with Android selected.
"""
from pathlib import Path
import argparse
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[2]


def main():
    sys.stdout.reconfigure(encoding='utf-8')
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--unity-editor', type=Path, required=True)
    args = parser.parse_args()
    candidates = list((ROOT / 'Library/Bee/artifacts').glob('*E*.dag/Assembly-CSharp.rsp'))
    if not candidates: raise SystemExit('Import the project in Unity first.')
    directory = max(candidates, key=lambda p: p.stat().st_mtime).parent
    out = ROOT / 'Temp/OfflineSyncChecks/compilation'
    out.mkdir(parents=True, exist_ok=True)
    data = args.unity_editor / 'Data'
    for mode in ('editor', 'android'):
        names = ['Bizza.Common.DeviceInfo', 'Bizza.Common.UI', 'Whitebox.Foundation', 'Assembly-CSharp'] + (['Whitebox.Foundation.Editor', 'Assembly-CSharp-Editor'] if mode == 'editor' else [])
        for name in names:
            source = (directory / (name + '.rsp')).read_text(encoding='utf-8-sig')
            assert '-define:UNITY_ANDROID' in source, 'Select Android before importing.'
            assert '-define:USER_VERIFICATION_SDK' not in source, 'Apply the disabled authorization macro in Unity first.'
            lines = []
            for line in source.splitlines():
                if line.startswith(('-out:','-refout:')): continue
                if mode == 'android' and line.startswith('-define:UNITY_EDITOR'): continue
                for dep in ('Bizza.Common.DeviceInfo', 'Bizza.Common.UI', 'Whitebox.Foundation', 'Assembly-CSharp'):
                    if line.startswith('-r:') and re.search('/'+re.escape(dep)+r'(\.ref)?\.dll"?$',line):
                        line = '-r:"' + str(out/(dep+'-'+mode+'.dll')) + '"'
                lines.append(line)
            lines.append('-out:"' + str(out/(name+'-'+mode+'.dll')) + '"')
            rsp = out/(name+'-'+mode+'.rsp')
            rsp.write_text('\n'.join(lines),encoding='utf-8')
            result = subprocess.run([str(data/'NetCoreRuntime/dotnet.exe'),str(data/'DotNetSdkRoslyn/csc.dll'),'@'+str(rsp)],
                                    cwd=ROOT,capture_output=True,text=True,encoding='utf-8',errors='replace')
            output = result.stdout+result.stderr
            (out/(name+'-'+mode+'.log')).write_text(output,encoding='utf-8')
            print(('PASS ' if result.returncode == 0 else 'FAIL ')+mode+' '+name)
            if result.returncode:
                print(output)
                raise SystemExit(result.returncode)


if __name__ == '__main__': main()
