"""Check source GameObjects retain transforms and own every restored component."""
from pathlib import Path
import hashlib
import json
import re

ROOT = Path(__file__).resolve().parent.parent
SOURCE = ROOT.parent / "assets/RecoveredProject/ExportedProject/Assets/GameObject"
TARGET = ROOT / "Assets/Resources/Recovered/GameObject"


def blocks(path):
    text = path.read_text(encoding="utf-8-sig").replace("\r\n", "\n")
    return {int(m[2]): (int(m[1]), m[3]) for m in re.finditer(
        r"^--- !u!(\d+) &(-?\d+)\n(.*?)(?=^--- !u!|\Z)", text, re.M | re.S)}


def verify(name):
    path = TARGET / (name + ".prefab")
    current, source = blocks(path), blocks(SOURCE / (name + ".prefab"))
    owners, references = {}, {}
    for identity, (kind, body) in current.items():
        if kind == 1:
            references[identity] = [int(v) for v in re.findall(r"^  - component: \{fileID: (-?\d+)\}$", body, re.M)]
            assert len(set(references[identity])) == len(references[identity]), (name, identity, "duplicate")
        else:
            owner = re.search(r"^  m_GameObject: \{fileID: (-?\d+)\}$", body, re.M)
            if owner:
                owners[identity] = int(owner[1])
        if kind in (4, 224):
            assert source[identity] == (kind, body), (name, identity, "source transform changed")
    for component, owner in owners.items():
        assert component in references[owner], (name, component, "missing owner reference")
    for owner, components in references.items():
        for component in components:
            assert owners.get(component) == owner, (name, component, "wrong component owner")
    source_objects = {i for i, (k, _) in source.items() if k == 1}
    assert source_objects == set(references), (name, "source hierarchy changed")
    return {"prefab": name, "game_objects": len(references), "components": len(owners),
            "source_transforms_unchanged": True,
            "sha256": hashlib.sha256(path.read_bytes()).hexdigest()}


if __name__ == "__main__":
    results = [verify(name) for name in ("RocketItemPrefab", "TntItemPrefab", "LightballItemPrefab", "PropellerItemPrefab", "TntItemExplodeParticles",
                                       "LightballItemBgPrefab", "LightballUseParticles", "LightballEffectedItemExplodeParticles", "LightballRayPrefab", "LightballItemExplodeParticles", "RocketItemParticles")]
    report = {"passed": True, "prefabs": results,
              "scope": "Offline serialized two-way component ownership, duplicate checks, source GameObject identity and unchanged source Transform blocks. Does not replace Unity import or gameplay verification."}
    (ROOT / "special_prefab_bindings_verification.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, indent=2))
