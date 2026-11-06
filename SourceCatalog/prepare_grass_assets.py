"""Author the scoped grass assets from the preserved Unity export."""
from hashlib import sha256
import json
from pathlib import Path
import re
from uuid import uuid4

PROJECT = Path(__file__).resolve().parents[1]
CATALOG = PROJECT / "SourceCatalog"
SOURCE = PROJECT.parent / "assets/RecoveredProject/ExportedProject/Assets"
RUNTIME = PROJECT / "Assets/Foundation/Runtime"
SETTINGS = PROJECT / "Assets/Foundation/Settings"
PREFABS = PROJECT / "Assets/Resources/Foundation/Prefabs"
FOUNDATION = PROJECT / "Assets/Foundation/Prefabs/Foundation.prefab"


def digest(path):
    return sha256(path.read_bytes()).hexdigest()


def script_guid(name):
    return re.search(r"(?m)^guid: ([a-f0-9]{32})$", (RUNTIME / (name + ".cs.meta")).read_text())[1]


def new_asset(outputs, path, content, importer, main_object_id=None):
    meta = Path(str(path) + ".meta")
    if path.exists() or meta.exists():
        raise FileExistsError(path)
    guid = uuid4().hex
    outputs[path] = content
    outputs[meta] = f"fileFormatVersion: 2\nguid: {guid}\n{importer}:\n  externalObjects: {{}}\n"
    if main_object_id is not None:
        outputs[meta] += f"  mainObjectFileID: {main_object_id}\n"
    return guid


def replace_once(content, original, replacement):
    if content.count(original) != 1:
        raise ValueError(f"Expected one serialized field or row: {original}")
    return content.replace(original, replacement)


def main():
    preflight = json.loads((CATALOG / "grass_preflight.json").read_text(encoding="utf-8"))
    for record in preflight["original_sources"]:
        path = PROJECT / record["path"]
        if digest(path) != record["sha256"]:
            raise ValueError(f"Asset differs from the migration baseline: {path}")

    raw_settings = SOURCE / "Resources/GrassItemAssets.asset"
    source_data = json.loads((CATALOG / "grass_source_data.json").read_text(encoding="utf-8"))["data"]
    original_settings = raw_settings.read_text(encoding="utf-8")
    outputs = {}
    evidence = []
    bindings = {
        "GrassItemPrefab": ("OriginalGrassItem", {
            "ce02f998afbb123f11028c567b15ebf1": ("OriginalGrassItemView", 1),
        }),
        "GrassItemExplodeParticles": ("OriginalGrassExplodeParticles", {
            "0cbd796c41a9f778c01e9555d895bce3": ("OriginalGrassExplodeParticles", 1),
            "21903a5c3e515a9fb608419393885fd0": ("OriginalAutoShadowEmitParticles", 6),
        }),
        "GrassItemTapParticles": ("OriginalGrassTapParticles", {
            "5ac0b00aabc8e6477461c592b5cb8c10": ("OriginalGrassTapParticles", 1),
            "91ebf7488625aa01d5397cc750621300": ("OriginalAutoShadowParticles", 2),
        }),
    }
    for original, (owned, scripts) in bindings.items():
        source = SOURCE / "GameObject" / (original + ".prefab")
        content = source.read_text(encoding="utf-8")
        for old_guid, (component, count) in scripts.items():
            if content.count("guid: " + old_guid) != count:
                raise ValueError(f"Unexpected script binding count in {source}")
            content = content.replace("guid: " + old_guid, "guid: " + script_guid(component))
        destination = PREFABS / (owned + ".prefab")
        new_asset(outputs, destination, content, "PrefabImporter")
        evidence.append({"source": str(source), "sha256": digest(source),
                         "destination": str(destination), "script_bindings": scripts})

    sprite_root = "Recovered/Sprite/"

    def sprite_paths(key):
        result = []
        for value in source_data[key]:
            path = sprite_root + value["name"]
            if not (PROJECT / "Assets/Resources" / (path + ".asset")).is_file():
                raise FileNotFoundError(path)
            result.append(path)
        return result

    grass_script = script_guid("OriginalGrassViewAssets")
    settings = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {grass_script}, type: 3}}
  m_Name: GrassViewAssets
  m_EditorClassIdentifier:
"""
    sprite_fields = {"lightBasePaths": "lightBases", "darkBasePaths": "darkBases", "patchPaths": "sprites"}
    for field, key in sprite_fields.items():
        settings += f"  {field}:\n" + "".join(f"  - {path}\n" for path in sprite_paths(key))
    settings += f"  shadowPath: {sprite_paths('sprites')[0]}\n  shadowAlphas:\n  - 0.6\n  - 0.9\n"
    settings_guid = new_asset(outputs, SETTINGS / "GrassViewAssets.asset", settings, "NativeFormatImporter", 11400000)

    foundation = FOUNDATION.read_text(encoding="utf-8")
    presentation_guid = script_guid("OriginalLevelPresentation")
    presentation = next(block for block in foundation.split("\n--- ") if f"guid: {presentation_guid}" in block)
    fields = (
        f"  grassAssets: {{fileID: 11400000, guid: {settings_guid}, type: 2}}\n"
        "  grassPrefabPath: Foundation/Prefabs/OriginalGrassItem\n"
        "  grassExplodePrefabPath: Foundation/Prefabs/OriginalGrassExplodeParticles\n"
        "  grassTapPrefabPath: Foundation/Prefabs/OriginalGrassTapParticles\n"
    )
    foundation = replace_once(foundation, presentation, replace_once(presentation, "  boxAssets:", fields + "  boxAssets:"))
    gem = """  - TiledId: 5
    PrefabPath: Recovered/GameObject/MatchItemPrefab
    SpritePath: Recovered/Sprite/Items-MatchItem-gem
    ShadowPath: Recovered/Sprite/Items-MatchItem-shadow_gem
    MatchPose:
      BaseOffset: {x: 0, y: 0, z: 0}
      ShadowParentOffset: {x: 0, y: -0.03, z: 0}
      ShadowColor: {r: 1, g: 1, b: 1, a: 0.33}
"""
    outputs[FOUNDATION] = replace_once(foundation, "  - TiledId: 21\n", gem + "  - TiledId: 21\n")

    goal = source_data["goal"]
    goal_row = f"""  - goalType: {goal['type']}
    iconPath: {sprite_root + goal['icon']['name']}
    shadowPath: {sprite_root + goal['shadow']['name']}
"""
    for field in ("shadowPosition", "shadowScale", "shadowAlpha", "iconScale", "iconOffset"):
        goal_row += re.search(rf"(?m)^    {field}: .+$", original_settings)[0] + "\n"
    goal_asset = SETTINGS / "GoalUiAssets.asset"
    outputs[goal_asset] = replace_once(goal_asset.read_text(encoding="utf-8"), "  - goalType: 21\n", goal_row + "  - goalType: 21\n")

    audio = SETTINGS / "GameSfxAssets.asset"
    audio_yaml = audio.read_text(encoding="utf-8").rstrip() + "\n"
    audio_bundle = "Recovered/AssetBundles/gameplayitemsfx/royal/scenes/game/audio/bundle/gameplayitemsfx/"
    clips = {164: "Recovered/AudioClip/grass_clear", 165: audio_bundle + "grass_clear2", 166: audio_bundle + "grass_feedback"}
    clip_rows = ""
    for clip, path in clips.items():
        if f"  - Type: {clip}\n" in audio_yaml or not (PROJECT / "Assets/Resources" / (path + ".wav")).is_file():
            raise ValueError(f"Unexpected audio configuration for {clip}: {path}")
        clip_rows += f"  - Type: {clip}\n    ResourcePath: {path}\n"
    outputs[audio] = replace_once(audio_yaml, "  fallHitPaths:\n", clip_rows + "  fallHitPaths:\n")

    for path, content in outputs.items():
        path.write_text(content, encoding="utf-8", newline="\n")
    report = {
        "baseline": preflight["baseline_commit"],
        "raw_settings": {"path": str(raw_settings), "sha256": digest(raw_settings)},
        "prefabs": evidence,
        "gem_pose": "MatchItemView.Init 0x21a5094, type 5 branch 0x21a5524",
        "grass_shadow_alpha": "GrassViewData.Init 0x187cfbc",
        "added": [str(path) for path in outputs if path not in (FOUNDATION, goal_asset, audio)],
        "updated": [str(path) for path in (FOUNDATION, goal_asset, audio)],
        "source_preserved": True,
    }
    (CATALOG / "grass_asset_authoring.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
