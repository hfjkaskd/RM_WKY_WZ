"""Conservative preflight of authored TMP rectangles and the framework test cases.

Uses real font advances, minimum auto-size, margins, wrapping and bold spacing.
This is not a substitute for the Unity PreviewScene mesh/render verification.
"""
from pathlib import Path
import json
import re
import sys

ROOT = Path(__file__).resolve().parents[2]


def value(text, field, default=""):
    match = re.search(r"^  " + re.escape(field) + r": ([^\n]*)", text, re.M)
    return match[1] if match else default


def vector(text, field, default=(0, 0)):
    raw = value(text, field)
    return tuple(float(n) for n in re.findall(r"[xyzw]: (-?[\d.]+)", raw)) or default


class Prefab:
    def __init__(self, path):
        self.path = path
        data = (ROOT / path).read_text(encoding="utf-8-sig")
        parts = re.split(r"^--- !u!(\d+) &(-?\d+)\s*\n", data, flags=re.M)
        self.docs = {parts[i + 1]: (parts[i], parts[i + 2]) for i in range(1, len(parts), 3)}
        self.transforms = {}
        for file_id, (kind, body) in self.docs.items():
            if kind == "224":
                go = re.search(r"m_GameObject: \{fileID: (\d+)\}", body)[1]
                self.transforms[go] = file_id

    def size(self, file_id, axis, seen=None):
        seen = set() if seen is None else seen
        if file_id in seen or file_id not in self.docs:
            return None
        seen.add(file_id)
        body = self.docs[file_id][1]
        low, high = vector(body, "m_AnchorMin"), vector(body, "m_AnchorMax")
        delta = vector(body, "m_SizeDelta")[axis]
        parent = re.search(r"m_Father: \{fileID: (\d+)\}", body)[1]
        span = high[axis] - low[axis]
        if span == 0 and delta > 0:
            return delta
        parent_size = (1440, 2560)[axis] if parent == "0" else self.size(parent, axis, seen)
        if parent_size is None:
            return None
        result = span * parent_size + delta
        if result > 0:
            return result
        # LayoutGroup-controlled children have their authored sizeDelta set to zero.
        if parent in self.docs:
            parent_go = re.search(r"m_GameObject: \{fileID: (\d+)\}", self.docs[parent][1])[1]
            for kind, component in self.docs.values():
                if kind != "114" or "m_GameObject: {fileID: " + parent_go + "}" not in component:
                    continue
                if value(component, "m_ChildControl" + ("Width", "Height")[axis]) == "1":
                    return parent_size
        return None

    def text_info(self, file_id):
        body = self.docs[file_id][1]
        go = re.search(r"m_GameObject: \{fileID: (\d+)\}", body)[1]
        rect = self.transforms[go]
        margin = vector(body, "m_margin", (0, 0, 0, 0))
        width, height = self.size(rect, 0), self.size(rect, 1)
        if width is not None:
            width -= margin[0] + margin[2]
        if height is not None:
            height -= margin[1] + margin[3]
        return body, width, height


class Font:
    def __init__(self, guid):
        paths = {
            "956a82d70bfe4c809008bfb6da242101": "Assets/BizzaWZ/Final/RoyalMatchReskin/SettingsMikado.asset",
            "79e4b5fe7d2626c4d9cf93d088b9c03d": "Assets/Resources/Recovered/MonoBehaviour/MikadoBlack SDF.asset",
            "7cc23ba99c7035347900a2e939f7ab60": "Assets/BizzaWZ/Common/Framework/Res/Fonts/MainFont_Simple.asset",
        }
        data = (ROOT / paths[guid]).read_text(encoding="utf-8-sig")
        self.point_size = float(re.search(r"m_PointSize: ([\d.]+)", data)[1])
        self.line_height = float(re.search(r"m_LineHeight: ([\d.]+)", data)[1])
        self.bold_spacing = float(value(data, "boldSpacing", "0"))
        self.widths = {}
        glyphs = {}
        for glyph in re.finditer(r"^  - m_Index: (\d+)\n([\s\S]*?)(?=^  - m_Index:|^  m_CharacterTable:)", data, re.M):
            width = re.search(r"m_HorizontalAdvance: ([\d.]+)", glyph[2])
            if width:
                glyphs[int(glyph[1])] = float(width[1]) / self.point_size
        for character in re.finditer(r"m_Unicode: (\d+)\n    m_GlyphIndex: (\d+)", data):
            self.widths[chr(int(character[1]))] = glyphs[int(character[2])]
        if guid == "7cc23ba99c7035347900a2e939f7ab60":
            from fontTools.ttLib import TTFont
            with TTFont(ROOT / "Assets/BizzaWZ/Common/Framework/Res/Fonts/COOKIERUN BLACK.OTF") as source:
                units = source["head"].unitsPerEm
                for codepoint, name in source.getBestCmap().items():
                    self.widths.setdefault(chr(codepoint), source["hmtx"].metrics[name][0] / units)

    def width(self, text, size, bold):
        # Unsupported symbols conservatively use a full em; Unity checks the actual fallback.
        advance = sum(self.widths.get(c, 1.0) for c in text) * size
        if bold and text:
            advance += max(0, len(text) - 1) * self.bold_spacing * size * 0.01
        return advance

    def measure(self, text, size, width, wrapping, bold):
        text = re.sub(r"<br\s*/?>", "\n", text)
        text = re.sub(r"<[^>]*>", "", text).replace("\r", "")
        lines = []
        for paragraph in text.split("\n"):
            if not wrapping:
                lines.append(paragraph)
                continue
            line = ""
            for word in paragraph.split(" "):
                proposed = word if not line else line + " " + word
                if line and self.width(proposed, size, bold) > width:
                    lines.append(line)
                    line = word
                else:
                    line = proposed
            lines.append(line)
        return max((self.width(line, size, bold) for line in lines), default=0), len(lines) * size * self.line_height / self.point_size


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    cases = json.loads((ROOT / "Tools/Localization/framework-text-cases.json").read_text(encoding="utf-8"))["cases"]
    prefabs, fonts, results = {}, {}, []
    for case in cases:
        prefab = prefabs.setdefault(case["prefab"], Prefab(case["prefab"]))
        body, width, height = prefab.text_info(case["textFileId"])
        font_guid = re.search(r"m_fontAsset: \{[^\n]*guid: (\w+)", body)[1]
        if font_guid not in fonts:
            fonts[font_guid] = Font(font_guid)
        font = fonts[font_guid]
        auto = value(body, "m_enableAutoSizing") == "1"
        size = float(value(body, "m_fontSizeMin" if auto else "m_fontSize"))
        wrapping = value(body, "m_enableWordWrapping") == "1"
        bold = int(value(body, "m_fontStyle", "0")) & 1
        for language, text in case["texts"].items():
            measured = font.measure(text, size, width, wrapping, bold) if width and height else None
            passed = measured is not None and measured[0] <= width + 0.5 and measured[1] <= height + 0.5
            result = dict(prefab=case["prefab"], scenario=case["scenario"], textFileId=case["textFileId"], language=language,
                          minimumSize=size, width=width, height=height, measured=measured, passed=passed)
            results.append(result)
            if not passed:
                print("REVIEW", json.dumps(result, ensure_ascii=False))
    out = ROOT / "Logs/LocalizationFix/framework-static-fit.json"
    out.write_text(json.dumps({"scope": "Conservative authored glyph/rectangle preflight, not Unity rendered results", "cases": results}, ensure_ascii=False, indent=2), encoding="utf-8")
    print("Framework text fit:", sum(r["passed"] for r in results), "/", len(results), "cases fit the minimum authored font size.")
    return 0 if all(r["passed"] for r in results) else 1


if __name__ == "__main__":
    raise SystemExit(main())
