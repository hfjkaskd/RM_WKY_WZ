"""Create a release copy from current source and the audited Resources selection.

The source project is never trimmed. Existing target import caches are allowed,
but Assets/Packages/ProjectSettings must not already exist in the target.
"""
from pathlib import Path
import argparse
import datetime
import hashlib
import json
import shutil
import subprocess


def digest(path):
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", required=True, type=Path)
    parser.add_argument("--target", required=True, type=Path)
    parser.add_argument("--selection", required=True, type=Path)
    args = parser.parse_args()
    source, target = args.source.resolve(), args.target.resolve()
    if source == target or source in target.parents or target in source.parents:
        raise RuntimeError("Source and build copy must be independent directories.")
    if any((target / name).exists() for name in ("Assets", "Packages", "ProjectSettings")):
        raise RuntimeError("Target already contains project source; use a fresh build directory.")
    selected = json.loads(args.selection.read_text(encoding="utf-8-sig"))
    if isinstance(selected, dict):
        selected = selected["files"]
    selected = set(selected)
    if not selected or any((not path.startswith("Assets/Resources/") and path != "Assets/Resources.meta") or ".." in Path(path).parts for path in selected):
        raise RuntimeError("Invalid Resources selection.")
    for path in selected:
        if not (source / path).is_file():
            raise FileNotFoundError(source / path)

    def git(*values):
        return subprocess.check_output(["git", "-C", str(source), *values]).decode("utf-8").strip()

    baseline = git("rev-parse", "HEAD")
    status = git("status", "--short", "--untracked-files=all")
    folders = ("Assets", "Packages", "ProjectSettings", "_Bizza_BuildTemplate", "_Bizza_Tools", "FunctionTools")
    files, excluded = [], []
    for folder in folders:
        for path in sorted((source / folder).rglob("*")):
            if not path.is_file():
                continue
            relative = path.relative_to(source).as_posix()
            if relative.startswith("Assets/Resources/") and relative not in selected:
                excluded.append(relative)
                continue
            destination = target / relative
            destination.parent.mkdir(parents=True, exist_ok=True)
            before = digest(path)
            shutil.copy2(path, destination)
            if digest(destination) != before or digest(path) != before:
                raise RuntimeError("Source changed or copy mismatch: " + relative)
            files.append({"path": relative, "bytes": path.stat().st_size, "sha256": before})
    # Retain the exact inputs needed to repeat this selection in the build copy.
    tooling = source / "Tools/Release"
    if tooling.is_dir():
        shutil.copytree(tooling, target / "Tools/Release")
    saved_selection = target.parent / "verified-resource-selection.json"
    saved_selection.write_text(json.dumps(sorted(selected), ensure_ascii=False, indent=2), encoding="utf-8")
    record = {
        "createdUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
        "source": str(source), "target": str(target), "baselineCommit": baseline,
        "sourceGitStatus": status,
        "resourceSelection": str(saved_selection),
        "resourceAuditSelection": str(args.selection.resolve()),
        "selectionSha256": digest(saved_selection),
        "resourceSelectionPolicy": "Verified first-100-level baseline plus current dependency closure; every retained file copied from current source.",
        "copiedFiles": files, "excludedResourceFiles": excluded,
        "copiedBytes": sum(item["bytes"] for item in files),
    }
    (target.parent / "source-snapshot.json").write_text(json.dumps(record, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({"target": str(target), "files": len(files), "bytes": record["copiedBytes"], "excludedResourceFiles": len(excluded)}), flush=True)


if __name__ == "__main__":
    main()
