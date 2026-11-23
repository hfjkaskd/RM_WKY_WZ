"""Verify the signed release and its source snapshot without modifying either."""

from collections import Counter
from pathlib import Path, PurePosixPath
import argparse
import hashlib
import json
import re
import struct
import zipfile


RELEASE = Path("Builds/Release/1.0.1_101_filtered")
ICON_SOURCE = "Assets/exec-1572350c-db5d-450b-b51c-1c892ca48db7.png"
ICON_SIZES = {120: 36, 160: 48, 240: 72, 320: 96, 480: 144, 640: 192}
RESOURCE_PREFIX = "Assets/Resources/"
RESOURCE_FOLDER_META = "Assets/Resources.meta"
ZIP_UNITY_DATA_PREFIX = "assets/bin/Data/"
CATALOG_ANDROID_PREFIX = "{UnityEngine.AddressableAssets.Addressables.RuntimePath}/Android/"
ZIP_ADDRESSABLES_PREFIX = "assets/aa/"


class VerificationError(RuntimeError):
    pass


def require(condition, message):
    if not condition:
        raise VerificationError(message)


def read_json(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def sha256(path):
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def validate_relative(path):
    relative = PurePosixPath(path)
    require(relative.parts and not relative.is_absolute() and ".." not in relative.parts
            and "\\" not in path and ":" not in path, "Invalid path in the build manifest.")
    return relative


def copied_files(snapshot):
    records = snapshot["copiedFiles"]
    indexed = {record["path"]: record for record in records}
    require(records and len(indexed) == len(records), "Source snapshot has missing or duplicate file records.")
    for path in indexed:
        validate_relative(path)
    return indexed


def changed_files(root, records):
    changes = []
    for relative, record in sorted(records.items()):
        path = root / relative
        if (not path.is_file() or path.stat().st_size != record["bytes"]
                or sha256(path) != record["sha256"]):
            changes.append(relative)
    return changes


def get_artifact_paths(output, manifest):
    files = {}
    for record in manifest["files"]:
        name = record["file"]
        relative = validate_relative(name)
        require(len(relative.parts) == 1 and relative.suffix not in files, "Invalid release artifact list.")
        files[relative.suffix] = (output / name, record)
    require(set(files) == {".aab", ".apk"}, "Expected one AAB and one APK.")
    require(manifest["apk_origin"] == files[".aab"][0].name, "APK origin differs from the verified AAB.")
    return files


def verify_resources(project, snapshot, manifest, records, max_assets, report):
    selection_path = Path(snapshot["resourceSelection"])
    require(sha256(selection_path) == snapshot["selectionSha256"], "The audited selection digest changed.")
    selected = read_json(selection_path)
    if isinstance(selected, dict):
        selected = selected["files"]
    require(len(set(selected)) == len(selected), "The audited selection contains duplicate paths.")
    resources = {path for path in records if path.startswith(RESOURCE_PREFIX)}
    actual = {path.relative_to(project).as_posix(): path.stat().st_size
              for path in (project / "Assets/Resources").rglob("*") if path.is_file()}
    require(actual.keys() == set(selected) - {RESOURCE_FOLDER_META} == resources,
            "Staged Resources differ from the audited selection or source snapshot.")
    require(len(actual) == manifest["resource_file_count"], "Resources count disagrees with the signed build report.")
    require(all(f"{RESOURCE_PREFIX}Levels/level_{level}.json" in actual for level in range(1, 101)),
            "Staged Resources are missing one or more of the 100 release levels.")
    retained_assets = [path for path in actual if not path.endswith(".meta")]
    require(len(retained_assets) <= max_assets, "The staged resource asset count exceeds the release limit.")
    if RESOURCE_FOLDER_META in selected:
        require((project / RESOURCE_FOLDER_META).is_file(), "The Resources folder metadata is missing.")
    report.update({
        "retained_assets": len(retained_assets),
        "retained_files": len(actual),
        "retained_asset_bytes": sum(actual[path] for path in retained_assets),
        "retained_file_bytes": sum(actual.values()),
        "excluded_source_files": len(snapshot["excludedResourceFiles"]),
        "verified_levels": 100,
    })


def packaged_resources(archive, prefix):
    resources = [entry for entry in archive.infolist()
                 if not entry.is_dir() and entry.filename.startswith(prefix + ZIP_UNITY_DATA_PREFIX)]
    require(resources, "A release archive is missing its Unity player data entries.")
    return {
        "entries": len(resources),
        "compressed_bytes": sum(entry.compress_size for entry in resources),
        "uncompressed_bytes": sum(entry.file_size for entry in resources),
    }


def catalog_bundles(archive, prefix):
    addressables = prefix + ZIP_ADDRESSABLES_PREFIX
    catalog = json.loads(archive.read(addressables + "catalog.json"))
    internal_id_prefixes = catalog.get("m_InternalIdPrefixes") or []
    expected = set()
    for internal_id in catalog["m_InternalIds"]:
        if internal_id_prefixes:
            index, separator, suffix = internal_id.rpartition("#")
            if separator and index.isdecimal():
                index = int(index)
                require(index < len(internal_id_prefixes), "Invalid Addressables catalog path prefix index.")
                internal_id = internal_id_prefixes[index] + suffix
        if internal_id.startswith(CATALOG_ANDROID_PREFIX) and internal_id.endswith(".bundle"):
            expected.add(internal_id.removeprefix(CATALOG_ANDROID_PREFIX))

    android = addressables + "Android/"
    packed = Counter(entry.filename.removeprefix(android) for entry in archive.infolist()
                     if entry.filename.startswith(android) and entry.filename.endswith(".bundle"))
    return {
        "referenced": len(expected),
        "packaged": sum(packed.values()),
        "missing": sorted(expected - packed.keys()),
        "extra": sorted(packed.keys() - expected),
        "duplicate_entries": sorted(name for name, count in packed.items() if count > 1),
    }


def verify_catalogs(files, report):
    for extension, prefix in ((".aab", "base/"), (".apk", "")):
        with zipfile.ZipFile(files[extension][0]) as archive:
            report[extension[1:]] = catalog_bundles(archive, prefix)
    require(all(result["referenced"] and not (result["missing"] or result["extra"] or result["duplicate_entries"])
                for result in report.values()),
            "The AAB or APK contains local Addressables bundles that differ from its catalog.")


def png_size(data):
    require(data.startswith(b"\x89PNG\r\n\x1a\n\x00\x00\x00\rIHDR") and len(data) >= 24,
            "A packaged launcher icon is not a PNG.")
    return struct.unpack(">II", data[16:24])


def verify_icons(project, source, previous, manifest, previous_manifest, records, aab, apk, apk_manifest, report):
    current_icon = records[ICON_SOURCE]["sha256"]
    old_icon = copied_files(read_json(previous / "source-snapshot.json"))[ICON_SOURCE]["sha256"]
    compare_previous = current_icon == old_icon
    source_icon_meta = (source / (ICON_SOURCE + ".meta")).read_text(encoding="utf-8-sig")
    require(re.search(r"^guid:\s*" + re.escape(manifest["icon_guid"]) + r"\s*$", source_icon_meta, re.M),
            "The source launcher icon GUID differs from the signed build report.")
    require(sha256(project / ICON_SOURCE) == current_icon,
            "The staged launcher icon differs from the current source image.")

    badging_icons = re.findall(r"^application-icon-(\d+):'([^']+)'\s*$", apk_manifest, re.M)
    icons = [(int(density), path) for density, path in badging_icons if int(density) in ICON_SIZES]
    icon_paths = dict(icons)
    require(len(icons) == len(ICON_SIZES) and set(icon_paths) == set(ICON_SIZES)
            and len(set(icon_paths.values())) == len(ICON_SIZES),
            "The APK manifest does not declare all six launcher icon densities exactly once.")
    report.update({"source_sha256": current_icon, "previous_source_sha256": old_icon,
                   "previous_comparison_required": compare_previous, "densities": []})

    densities = report["densities"]
    for density, size in ICON_SIZES.items():
        path = icon_paths[density]
        apk_icon = apk.read(path)
        require(apk_icon == aab.read("base/" + path), f"AAB and APK icons differ at density {density}.")
        require(png_size(apk_icon) == (size, size), f"The icon has the wrong dimensions at density {density}.")
        densities.append({"density": density, "pixels": size, "path": path,
                          "sha256": hashlib.sha256(apk_icon).hexdigest()})

    if compare_previous:
        previous_artifacts = get_artifact_paths(previous, previous_manifest)
        with (zipfile.ZipFile(previous_artifacts[".aab"][0]) as previous_aab,
              zipfile.ZipFile(previous_artifacts[".apk"][0]) as previous_apk):
            for item in densities:
                path = item["path"]
                first = hashlib.sha256(previous_aab.read("base/" + path)).hexdigest()
                second = hashlib.sha256(previous_apk.read(path)).hexdigest()
                require(first == second == item["sha256"],
                        f"The unchanged source icon packaged differently at density {item['density']}.")
                item["matches_previous"] = True


def verify(project, source, previous, max_package_mib, max_resource_assets, report):
    output = project / RELEASE
    manifest = read_json(output / "release-verification.json")
    snapshot_path = project.parent / "source-snapshot.json"
    require(Path(manifest["resource_snapshot"]).resolve() == snapshot_path, "Build report points to a different source snapshot.")
    snapshot = read_json(snapshot_path)
    require(Path(snapshot["source"]).resolve() == source and Path(snapshot["target"]).resolve() == project,
            "Source and project do not match the snapshot.")

    files = get_artifact_paths(output, manifest)
    verify_catalogs(files, report["addressables"])

    records = copied_files(snapshot)
    report["source_snapshot"] = {"baseline_commit": snapshot["baselineCommit"], "files_rechecked": len(records),
                                 "changes": changed_files(source, records)}
    require(not report["source_snapshot"]["changes"], "One or more copied files changed in the source project.")
    verify_resources(project, snapshot, manifest, records, max_resource_assets, report["resources"])

    max_package_bytes = max_package_mib * 1024 * 1024
    for extension, (path, record) in files.items():
        size = path.stat().st_size
        require(0 < size < max_package_bytes, f"The {extension} package is empty or exceeds the release size limit.")
        require(size == record["bytes"] and sha256(path) == record["sha256"],
                f"The {extension} package differs from the signed build record.")
        report["artifacts"][extension[1:]] = record.copy()

    aab_path, _ = files[".aab"]
    apk_path, apk_record = files[".apk"]
    stem = aab_path.stem.removesuffix("-release")
    with zipfile.ZipFile(output / (stem + "-universal.apks")) as apks:
        require(sum(info.filename == "universal.apk" for info in apks.infolist()) == 1,
                "The bundletool container does not have exactly one universal APK.")
        with apks.open("universal.apk") as universal_apk:
            require(apks.getinfo("universal.apk").file_size == apk_record["bytes"]
                    and hashlib.file_digest(universal_apk, "sha256").hexdigest() == apk_record["sha256"],
                    "The APK differs from bundletool's universal.apk.")
    report["apk_matches_bundletool_universal"] = True

    apk_manifest = (project / "Logs/release-filtered-apk-manifest.log").read_text(encoding="utf-8-sig")
    labels = re.findall(r"^application-label:'([^']+)'\s*$", apk_manifest, re.M)
    require(labels == ["Gemwink Castle"], "The APK application label is not Gemwink Castle.")
    report["application_label"] = labels[0]
    previous_manifest = read_json(previous / "release-verification.json")
    with zipfile.ZipFile(aab_path) as aab, zipfile.ZipFile(apk_path) as apk:
        report["resources"]["packaged_aab_unity_data"] = packaged_resources(aab, "base/")
        report["resources"]["packaged_apk_unity_data"] = packaged_resources(apk, "")
        verify_icons(project, source, previous, manifest, previous_manifest, records, aab, apk, apk_manifest, report["icons"])


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", type=Path, required=True, help="Independent filtered Unity build project.")
    parser.add_argument("--source", type=Path, required=True, help="Authoritative Unity project to read.")
    parser.add_argument("--previous", type=Path, required=True, help="Previous filtered release artifact directory.")
    parser.add_argument("--max-package-mib", type=int, default=200)
    parser.add_argument("--max-resource-assets", type=int, default=2500)
    args = parser.parse_args()
    project, source, previous = (path.resolve() for path in (args.project, args.source, args.previous))
    output = (project / RELEASE).resolve()
    if (project == source or source in project.parents or project in source.parents
            or not output.is_relative_to(project) or output == previous or previous in output.parents
            or not (output / "release-verification.json").is_file()
            or args.max_package_mib <= 0 or args.max_resource_assets <= 0):
        print(json.dumps({"passed": False, "error": "Invalid verification directories, build report, or limits."}))
        return 1

    report = {"passed": False, "max_package_mib": args.max_package_mib,
              "max_resource_assets": args.max_resource_assets, "artifacts": {}, "resources": {}, "icons": {},
              "addressables": {}}
    try:
        verify(project, source, previous, args.max_package_mib, args.max_resource_assets, report)
    except VerificationError as error:
        report["error"] = str(error)
    except (OSError, KeyError, ValueError, zipfile.BadZipFile) as error:
        report["error"] = "Could not read a required verification input (" + type(error).__name__ + ")."
    else:
        report["passed"] = True

    report_path = output / "delivery-verification.json"
    report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    addressables = {kind: {key: len(result[key]) for key in ("missing", "extra", "duplicate_entries")}
                    for kind, result in report["addressables"].items()}
    print(json.dumps({"passed": report["passed"], "report": report_path.name, "addressables": addressables,
                      "source_files_rechecked": report.get("source_snapshot", {}).get("files_rechecked", 0),
                      "retained_resource_assets": report["resources"].get("retained_assets", 0),
                      "icon_densities_checked": len(report["icons"].get("densities", []))}))
    return 0 if report["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
