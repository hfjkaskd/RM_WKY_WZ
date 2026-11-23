"""Regression for real fall scheduling/motion with an in-memory presentation boundary.

Compiles the production manager, planner, grid, mediation, item availability and
motion together. The harness substitutes prefab views, clock/audio, constructors,
refill choice and the absent ice-level counter. It does not exercise Unity
rendering, matching/explosions, or rerun the historical native ARM64 oracle.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import tempfile


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project', type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument('--output-dir', type=Path)
    parser.add_argument('--mono-root', type=Path)
    parser.add_argument('--planner-source', type=Path, help='Optional old planner for reproducing the regression.')
    parser.add_argument('--harness', type=Path, help='Defaults to SourceCatalog/DiagonalFallHarness.cs.')
    args = parser.parse_args()
    root = args.project.resolve()
    output = args.output_dir or Path(tempfile.mkdtemp(prefix='rm-diagonal-fall-'))
    output.mkdir(parents=True, exist_ok=True)
    version_text = (root / 'ProjectSettings/ProjectVersion.txt').read_text()
    version = re.search(r'^m_EditorVersion: (.+)$', version_text, re.M).group(1).strip()
    mono = args.mono_root or Path(os.environ.get(
        'ROYAL_MONO_ROOT', f'C:/Program Files/Unity/Hub/Editor/{version}/Editor/Data/MonoBleedingEdge'))
    names = (
        'OriginalCellMediation', 'OriginalGridIterator', 'OriginalCellGrid',
        'OriginalCellFallState', 'OriginalFillingCell', 'OriginalFallRefillColumns',
        'OriginalFallTargetPlanner', 'OriginalFallManager', 'OriginalFallGridStrategies',
        'OriginalBoardLayout', 'OriginalBoardFallContext', 'OriginalFillingItemPlacement',
        'OriginalFallComponentState', 'OriginalFallMotion', 'OriginalItemSwapAvailability',
        'OriginalItemFallAvailability', 'OriginalStaticFallQueries', 'OriginalCellTouchRules',
    )
    sources = [root / 'Assets/Foundation/Runtime' / (name + '.cs') for name in names]
    if args.planner_source:
        sources[names.index('OriginalFallTargetPlanner')] = args.planner_source.resolve()
    harness = args.harness or root / 'SourceCatalog/DiagonalFallHarness.cs'
    exe = output / 'diagonal-fall.exe'
    compiled = subprocess.run([
        str(mono / 'bin/mono.exe'), str(mono / 'lib/mono/4.5/mcs.exe'),
        '-langversion:latest', '-out:' + str(exe), str(harness), *map(str, sources),
    ], capture_output=True, text=True, encoding='utf-8-sig')
    if compiled.returncode:
        raise RuntimeError(compiled.stdout + compiled.stderr)
    run = subprocess.run([str(mono / 'bin/mono.exe'), str(exe)],
                         capture_output=True, text=True, encoding='utf-8-sig')
    lines = run.stdout.splitlines()
    passed = sum(line.startswith('PASS ') for line in lines)
    failed = [line for line in lines if line.startswith('FAIL ')]
    report = {
        'passed': run.returncode == 0 and passed > 0 and not failed,
        'assertions_passed': passed,
        'failures': failed,
        'output': run.stdout,
        'errors': run.stderr,
        'boundary': __doc__.strip(),
        'source_hashes': {str(source): hashlib.sha256(source.read_bytes()).hexdigest()
                          for source in [harness, *sources]},
    }
    report_path = output / 'diagonal-fall-report.json'
    report_path.write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print(f'Assertions passed: {passed}; failed: {len(failed)}; report: {report_path}')
    for line in failed:
        print(line)
    if not report['passed']:
        raise SystemExit(1)


if __name__ == '__main__':
    main()
