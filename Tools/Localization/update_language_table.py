"""Merge reviewed project translations into the existing BZC2 table.

Other keys and locales are preserved. The JSON source is also consumed by the
Unity table exporter, so re-exporting the original workbook keeps these fixes.
Run with --apply to write; without it, require both outputs to be up to date.
"""
from pathlib import Path
import argparse
import io
import json
import re
import struct

ROOT = Path(__file__).resolve().parents[2]
TABLE = ROOT / 'Assets/Game/Resources/ConfigAssets/TableBin/Table01/tbllanguage.bytes'
SOURCE = ROOT / 'Assets/Game/Localization/LanguageOverrides.json'
LOCALES = ('en-US', 'pt-BR', 'id-ID')


def read_table(data):
    stream = io.BytesIO(data)
    def integer():
        return struct.unpack('<i', stream.read(4))[0]
    def raw():
        length = shift = 0
        while True:
            byte = stream.read(1)[0]
            length |= (byte & 127) << shift
            if byte < 128: break
            shift += 7
        return stream.read(length).decode('utf-8')
    def nullable():
        return raw() if stream.read(1) == b'\x01' else None
    assert stream.read(4) == b'BZC2' and integer() == 1
    assert raw() == 'tbllanguage'
    rows = {}
    for _ in range(integer()):
        key = nullable()
        assert key and key not in rows
        values = {}
        for _ in range(integer()):
            lang = nullable()
            values[lang] = nullable()
        rows[key] = values
    assert not stream.read(), 'Unexpected trailing table data'
    return rows


def write_table(rows):
    stream = io.BytesIO()
    def integer(value): stream.write(struct.pack('<i', value))
    def raw(value):
        data = value.encode('utf-8'); length = len(data)
        while length >= 128:
            stream.write(bytes([(length & 127) | 128])); length >>= 7
        stream.write(bytes([length])); stream.write(data)
    def nullable(value):
        stream.write(b'\x00' if value is None else b'\x01')
        if value is not None: raw(value)
    stream.write(b'BZC2'); integer(1); raw('tbllanguage'); integer(len(rows))
    for key, values in rows.items():
        nullable(key); integer(len(values))
        for lang, value in values.items(): nullable(lang); nullable(value)
    return stream.getvalue()


def get_overrides():
    overrides = {}
    for path in sorted(Path(__file__).parent.glob('*-translations.json')):
        for key, row in json.loads(path.read_text(encoding='utf-8-sig')).items():
            assert all(isinstance(row.get(lang), str) and row[lang].strip() for lang in LOCALES), (path, key)
            formats = [set(re.findall(r'\{([^{}]+)\}', row[lang])) for lang in LOCALES]
            assert formats[0] == formats[1] == formats[2], (key, formats)
            if key in overrides: assert overrides[key] == row, ('Conflicting translation', key)
            overrides[key] = row
    return overrides


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--apply', action='store_true')
    args = parser.parse_args()
    rows = read_table(TABLE.read_bytes())
    overrides = get_overrides()
    for key, values in overrides.items(): rows.setdefault(key, {}).update(values)
    source = {'rows': [dict(key=key, english=value['en-US'], portuguese=value['pt-BR'], indonesian=value['id-ID'])
                       for key, value in sorted(overrides.items())]}
    text = json.dumps(source, ensure_ascii=False, indent=2) + '\n'
    data = write_table(rows)
    assert read_table(data) == rows
    if args.apply:
        SOURCE.parent.mkdir(parents=True, exist_ok=True)
        SOURCE.write_text(text, encoding='utf-8')
        TABLE.write_bytes(data)
    else:
        assert SOURCE.read_text(encoding='utf-8-sig') == text, 'Run with --apply to update JSON source'
        assert TABLE.read_bytes() == data, 'Run with --apply to update binary table'
    print(f'PASS {len(rows)} table keys; {len(overrides)} reviewed three-language overrides; round trip preserves all locales')


if __name__ == '__main__': main()
