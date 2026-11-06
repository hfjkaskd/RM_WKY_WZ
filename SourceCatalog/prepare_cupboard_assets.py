"""Rebind preserved cupboard prefabs to the owned Unity components."""
from hashlib import sha256
import json
from pathlib import Path
import re
from uuid import uuid4

PROJECT = Path(__file__).resolve().parents[1]
CATALOG = PROJECT / "SourceCatalog"
RAW = PROJECT.parent / "assets/RecoveredProject/ExportedProject/Assets"
PREFABS = PROJECT / "Assets/Resources/Foundation/Prefabs"
RUNTIME = PROJECT / "Assets/Foundation/Runtime"
SETTINGS = PROJECT / "Assets/Foundation/Settings"


def digest(path):
    return sha256(path.read_bytes()).hexdigest()


def guid(path):
    return re.search(r"(?m)^guid: ([a-f0-9]{32})$", path.read_text(encoding="utf-8"))[1]


def insert_once(content, text, insertion):
    if content.count(text) != 1:
        raise ValueError(f"Expected one serialized field: {text}")
    return content.replace(text, insertion + text)


def main():
    preflight = json.loads((CATALOG / "cupboard_preflight.json").read_text(encoding="utf-8-sig"))
    for entry in preflight["original_sources"] + preflight["configured_targets_before"]:
        path = PROJECT / entry["path"]
        if digest(path) != entry["sha256"]:
            raise ValueError(f"Source changed after preflight: {path}")

    outputs = {}
    added = []
    bindings = {}
    def add_meta(path, importer):
        meta = Path(str(path) + ".meta")
        if meta.exists():
            raise FileExistsError(meta)
        value = uuid4().hex
        outputs[meta] = f"fileFormatVersion: 2\nguid: {value}\n{importer}:\n  externalObjects: {{}}\n"
        added.append(str(meta))
        return value

    new_scripts = (
        "OriginalCupboardBoardItem", "OriginalCupboardItemView", "OriginalCupboardBurstParticles",
        "OriginalCupboardDoorParticles", "OriginalSortedParticleSystem3d", "OriginalItemParticles3d",
        "OriginalCupboardPresentation",
    )
    for script in new_scripts:
        script_path = RUNTIME / (script + ".cs")
        assert script_path.is_file(), script_path
        added.append(str(script_path))
        bindings[script] = add_meta(script_path, "MonoImporter")
    for script in ("OriginalRootTransform", "OriginalAutoShadowParticles", "OriginalAutoShadowEmitParticles"):
        bindings[script] = guid(RUNTIME / (script + ".cs.meta"))

    profiles = {
        "CupboardItemPrefab": ("OriginalCupboardItem", {
            "30ecf5a5cd7626d02c835684ad7b8844": ("OriginalCupboardItemView", 1),
            "98d7c11aef979f39dccadbc0ed1b4acf": ("OriginalRootTransform", 1),
        }),
        "CupboardItemDoorExplodeParticles": ("OriginalCupboardDoorParticles", {
            "241d43ff1f9c726e582f44c1c9c6c4b7": ("OriginalCupboardDoorParticles", 1),
            "2d87e5a9b3627818ea9e4181e529a3d9": ("OriginalItemParticles3d", 2),
            "4b0ff5378608313711680db6681004bb": ("OriginalSortedParticleSystem3d", 4),
            "91ebf7488625aa01d5397cc750621300": ("OriginalAutoShadowParticles", 1),
        }),
        "CupboardItemSingleExplodeParticles": ("OriginalCupboardPlateParticles", {
            "7723fd5b0ac9e0fe06ebcb1a85ee2897": ("OriginalCupboardBurstParticles", 1),
            "21903a5c3e515a9fb608419393885fd0": ("OriginalAutoShadowEmitParticles", 2),
        }),
        "CupboardItemExplodeParticles": ("OriginalCupboardBreakParticles", {
            "3c3b2a03685dd6978c50f93894fba56b": ("OriginalCupboardBurstParticles", 1),
            "21903a5c3e515a9fb608419393885fd0": ("OriginalAutoShadowEmitParticles", 3),
        }),
    }
    report_prefabs = []
    for original, (name, scripts) in profiles.items():
        source = RAW / "GameObject" / (original + ".prefab")
        destination = PREFABS / (name + ".prefab")
        if destination.exists():
            raise FileExistsError(destination)
        content = source.read_text(encoding="utf-8")
        for old, (script, count) in scripts.items():
            if content.count("guid: " + old) != count:
                raise ValueError(f"Unexpected script count in {source}: {script}")
            content = content.replace("guid: " + old, "guid: " + bindings[script])
        if name == "OriginalCupboardItem":
            extra = ("  idleState: CupboardIdleAnimation\n  firstTapState: CupboardFirstTapAnimation\n"
                     "  commonTapState: CupboardCommonTapAnimation\n  cupboardSpeed: 0.9\n  plateSpeed: 0.8\n")
            for field, states in {
                "shakeStates": [f"CupboardShakeAnimation{i}" for i in (1, 2)],
                "plateStates": [f"PlateAnimation{i}" for i in range(1, 6)],
                "plateTapStates": [f"PlateTapAnimation{i}" for i in range(1, 6)],
                "alternatePlateTapStates": [f"PlateTapAnimation{i}_2" for i in range(1, 6)],
            }.items():
                extra += f"  {field}:\n" + "".join(f"  - {state}\n" for state in states)
            content = insert_once(content, "  doorView:", extra)
        if name == "OriginalCupboardPlateParticles":
            content = content.replace("  mushroomParticles:", "  poolId: 49\n  particles:")
        if name == "OriginalCupboardBreakParticles":
            content = content.replace("  baseParticles:", "  poolId: 50\n  particles:")
        outputs[destination] = content
        added.append(str(destination))
        add_meta(destination, "PrefabImporter")
        report_prefabs.append({"source": str(source), "destination": str(destination),
                               "source_sha256": digest(source), "scripts": scripts})

    foundation = PROJECT / "Assets/Foundation/Prefabs/Foundation.prefab"
    fields = {
        "cupboardPrefabPath": "OriginalCupboardItem", "cupboardDoorPath": "OriginalCupboardDoorParticles",
        "cupboardPlatePath": "OriginalCupboardPlateParticles", "cupboardBreakPath": "OriginalCupboardBreakParticles",
    }
    path_fields = "".join(f"  {key}: Foundation/Prefabs/{value}\n" for key, value in fields.items())
    outputs[foundation] = insert_once(foundation.read_text(encoding="utf-8"), "  grassPrefabPath:", path_fields)

    goal = SETTINGS / "GoalUiAssets.asset"
    icon = "Recovered/Sprite/Items-CupboardItem-cupboard_goal"
    if not (PROJECT / "Assets/Resources" / (icon + ".asset")).is_file():
        raise FileNotFoundError(icon)
    goal_row = (
        f"  - goalType: 15\n    iconPath: {icon}\n    shadowPath:\n"
        "    shadowPosition: {x: 0, y: -0.04}\n    shadowScale: {x: 1, y: 1}\n"
        "    shadowAlpha: 1\n    iconScale: 1\n    iconOffset: {x: 0, y: 0}\n"
    )
    outputs[goal] = insert_once(goal.read_text(encoding="utf-8"), "  - goalType: 21\n", goal_row)
    audio = SETTINGS / "GameSfxAssets.asset"
    local_clips = ("cupboard_container_break", "cupboard_door_break", "cupboard_plate_1", "cupboard_plate_2", "cupboard_plate3")
    alternate = ("cupboard_cover_touchfeedback", "cupboard_plate_touchfeedback",
                 "cupboard_plate2_touchfeedback", "cupboard_only1plate_touchfeedback")
    bundle = "Recovered/AssetBundles/gameplayitemsfx/royal/scenes/game/audio/bundle/gameplayitemsfx/"
    clip_paths = ["Recovered/AudioClip/" + name for name in local_clips] + [bundle + name for name in alternate]
    audio_yaml = audio.read_text(encoding="utf-8")
    rows = ""
    for type_id, path in enumerate(clip_paths, 98):
        if f"  - Type: {type_id}\n" in audio_yaml or not (PROJECT / "Assets/Resources" / (path + ".wav")).is_file():
            raise ValueError(f"Unexpected clip configuration: {type_id} {path}")
        rows += f"  - Type: {type_id}\n    ResourcePath: {path}\n"
    outputs[audio] = insert_once(audio_yaml, "  fallHitPaths:\n", rows)
    for path, value in outputs.items():
        path.write_text(value, encoding="utf-8", newline="\n")
    evidence = {"source_preflight": "SourceCatalog/cupboard_preflight.json", "prefabs": report_prefabs,
                "added": added, "updated": [str(path) for path in (foundation, goal, audio)]}
    (CATALOG / "cupboard_asset_authoring.json").write_text(json.dumps(evidence, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
