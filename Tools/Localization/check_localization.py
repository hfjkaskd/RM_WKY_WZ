"""Run actual production localization/codec C# against the shipped language table.

Does not launch Unity, alter assets or call a server. Requires the Unity editor
toolchain. The framework GetText method, row/table classes and vector struct are
extracted verbatim; the binary codec and CoreLocalization compile from source.
"""
from pathlib import Path
import argparse
import json
import subprocess
import sys

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'Tools/OfflineSync'))
from source_blocks import block
sys.path.insert(0, str(Path(__file__).parent))
from update_language_table import get_overrides


def read(relative):
    return (ROOT / relative).read_text(encoding='utf-8-sig')


def main():
    sys.stdout.reconfigure(encoding='utf-8')
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--unity-editor', type=Path, required=True)
    args = parser.parse_args()
    fixture = Path(__file__).with_name('localization_fixture.cs.txt').read_text(encoding='utf-8')
    definitions = read('Assets/Game/Scripts/ConfigData/ConfigDefinitions.cs')
    fixture = fixture.replace('// PRODUCTION_VECTOR', block(definitions, 'public struct vector2'))
    fixture = fixture.replace('// PRODUCTION_LANGUAGE_ROW', block(definitions, 'public sealed class LanguageConfig'))
    fixture = fixture.replace('// PRODUCTION_LANGUAGE_TABLE', block(read('Assets/Game/Scripts/ConfigData/ConfigTables.cs'), 'public sealed class TblLanguage'))
    fixture = fixture.replace('// PRODUCTION_GET_TEXT', block(read('Assets/BizzaWZ/Common/Framework/Runtime/Module/Localization/LanguageUtils.cs'), 'public static string GetText('))
    rows = []
    for key, values in sorted(get_overrides().items()):
        quoted = [json.dumps(value, ensure_ascii=True) for value in [key, *[values[locale] for locale in ('en-US', 'pt-BR', 'id-ID')]]]
        rows.append('new Expected(' + ', '.join(quoted) + '),')
    fixture = fixture.replace('// REVIEWED_OVERRIDES', '\n'.join(rows))
    output = ROOT / 'Temp/LocalizationFix/core-checks'
    output.mkdir(parents=True, exist_ok=True)
    generated = output / 'LocalizationChecks.cs'
    generated.write_text(fixture, encoding='utf-8')
    data = args.unity_editor / 'Data'
    shims = data / 'NetStandard/compat/2.1.0/shims/netfx'
    refs = [data / 'NetStandard/ref/2.1.0/netstandard.dll']
    refs += [shims / name for name in ('mscorlib.dll', 'System.dll', 'System.Core.dll')]
    exe = output / 'LocalizationChecks.exe'
    rsp = output / 'localization.rsp'
    sources = [generated, ROOT / 'Assets/Foundation/Runtime/CoreLocalization.cs', ROOT / 'Assets/Game/Scripts/ConfigData/SimpleConfigBinary.cs']
    rsp.write_text('\n'.join(['-nologo', '-utf8output', '-nostdlib+', '-langversion:latest', '-target:exe',
                             '-define:BIZZA_REAL_WITHDRAW', f'-out:"{exe}"',
                             *[f'-r:"{path}"' for path in refs], *[f'"{path}"' for path in sources]]), encoding='utf-8')
    for phase, command in (
        ('compile', [str(data / 'NetCoreRuntime/dotnet.exe'), str(data / 'DotNetSdkRoslyn/csc.dll'), '@' + str(rsp)]),
        ('results', [str(data / 'MonoBleedingEdge/bin/mono.exe'), str(exe), str(ROOT / 'Assets/Game/Resources/ConfigAssets/TableBin/Table01/tbllanguage.bytes')]),
    ):
        result = subprocess.run(command, cwd=ROOT, capture_output=True, text=True, encoding='utf-8', errors='replace')
        text = result.stdout + result.stderr
        (output / (phase + '.log')).write_text(text, encoding='utf-8')
        print(text, end='')
        if result.returncode:
            raise SystemExit(result.returncode)


if __name__ == '__main__': main()
