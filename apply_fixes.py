#!/usr/bin/env python3
# Run from the project root (the folder that contains "Assets").  Usage:  python3 apply_fixes.py
# Applies the UI polish edits to the C# files. Each file is changed only if ALL of its edits succeed.
import os, sys

ROOT = "Assets/_Project/Scripts/"

def read(path):
    raw = open(path, "rb").read().decode("utf-8")
    crlf = "\r\n" in raw
    return raw.replace("\r\n", "\n"), crlf

def write(path, text, crlf):
    if crlf: text = text.replace("\n", "\r\n")
    open(path, "wb").write(text.encode("utf-8"))

def indent_of(line):
    return line[:len(line) - len(line.lstrip())]

class Fail(Exception): pass

def op_sub(text, old, new, label):
    if old in text:
        if text.count(old) != 1: raise Fail(label + ": text found " + str(text.count(old)) + " times, expected 1")
        return text.replace(old, new), "applied"
    if new in text: return text, "already applied"
    raise Fail(label + ": text not found")

def op_lines(text, start, end, new_block, label, keep_line=False, done=None):
    """Replace from the line containing `start` to the line containing `end` (inclusive) with new_block.
       keep_line=True appends the original start line after new_block (used to insert code above a line)."""
    if done and done in text:
        return text, "already applied"
    lines = text.split("\n")
    si = next((i for i, l in enumerate(lines) if start in l), None)
    if si is None:
        raise Fail(label + ": start line not found")
    ei = next((i for i in range(si, len(lines)) if end in lines[i]), None)
    if ei is None: raise Fail(label + ": end line not found")
    ind = indent_of(lines[si])
    rep = [(ind + l) if l.strip() else "" for l in new_block.split("\n")]
    if keep_line: rep = rep + [lines[si]]
    return "\n".join(lines[:si] + rep + lines[ei + 1:]), "applied"

EDITS = {}

EDITS["UI/HubView.cs"] = [
    ("sub", '"DUNGEON MODE"', '"DUNGEON"', "hub title"),
]
EDITS["UI/Kit/UIKit.cs"] = [
    ("sub", '"Backing (replace sprite)", rt, "pa_panel"', '"Backing (replace sprite)", rt, "pa_plate"', "top nav backing"),
]
EDITS["UI/LegendsView.cs"] = [
    ("sub", '"<size=11>" + label + "</size> " + value', 'label + " " + value', "stat label size"),
]
EDITS["UI/AchievementsView.cs"] = [
    ("lines", 'ParchUI.Txt(body, "Unlocked "', 'ParchUI.Txt(body, "Unlocked "',
     'ParchUI.Counter(body, "Unlocked " + AchievementCatalog.UnlockedCount + "/" + all.Count, 22, 0.74f, 0.795f, 0.965f, 0.87f);',
     "achievements counter", False, 'ParchUI.Counter(body, "Unlocked "'),
]
EDITS["UI/CompendiumView.cs"] = [
    ("lines", 'ParchUI.Txt(body, "Discovered "', '0.55f, 0.80f, 0.955f, 0.865f);',
     'ParchUI.Counter(body, "Discovered " + MetaProgress.PackSeenCount + "/" + PackCatalog.All.Length +\n'
     '                "  |  Cleared " + MetaProgress.PackClearedCount + "/" + PackCatalog.All.Length,\n'
     '                20, 0.56f, 0.795f, 0.965f, 0.875f);', "compendium counter", False, 'ParchUI.Counter(body, "Discovered "'),
    ("lines", 'string god = ', '0.20f, 0.05f, 0.80f, 0.105f);',
     'string god = seen && p.GodRealmChance > 0f ? "God Realm " + Mathf.RoundToInt(p.GodRealmChance * 100f) + "%" : "";\n'
     'float tx1 = cleared ? 0.74f : 0.80f;   // the gold seal on a cleared scroll sits at the right\n'
     '// three lines between the ribbon fold (starts 0.275) and the bottom roller (ends 0.07)\n'
     'ParchUI.Txt(frame, seen ? NodeInfo.EnvName(p.Env) + " / " + p.Theme + (god.Length > 0 ? " - " + god : "") : "Undiscovered",\n'
     '            14, Parch.DarkBrown, TextAnchor.MiddleCenter, 0.20f, 0.205f, tx1, 0.265f);\n'
     'ParchUI.Txt(frame, "BOSS: " + (cleared ? p.Boss : "???"), 14, Parch.DarkBrown, TextAnchor.MiddleCenter, 0.20f, 0.145f, tx1, 0.205f, true);\n'
     'var eq = EquipmentCatalog.ForPack(p.Id);\n'
     'ParchUI.Txt(frame, "EQUIPMENT: " + (cleared && eq != null ? eq.Name : "???"), 13, Parch.DarkBrown, TextAnchor.MiddleCenter, 0.20f, 0.085f, tx1, 0.145f, true);',
     "compendium card text", False, 'float tx1 ='),
]
EDITS["UI/ArmoryView.cs"] = [
    ("lines", 'ParchUI.Txt(body, blessings ? "Blessings: single-use', '0.45f, 0.77f, 0.97f, 0.85f);',
     'if (blessings)\n'
     '    ParchUI.Txt(body, "Blessings: single-use, drain the target\'s Stamina.", 18, Parch.DarkBrown, TextAnchor.MiddleLeft, 0.43f, 0.755f, 0.72f, 0.875f);\n'
     'else\n'
     '    ParchUI.Counter(body, "Found " + found + "/" + count, 20, 0.74f, 0.79f, 0.965f, 0.87f);', "armory counter", False, 'ParchUI.Counter(body, "Found "'),
    ("lines", 'static string Slug(string name)', 'static string Slug(string name)',
     '// Stand-in icons (relic art, in pack order). Put a PNG named after the item slug in Resources/UI/equipment/ to override.\n'
     'static readonly string[] StandIn = {\n'
     '    "relicicons/relic_11_ca", "relicicons/relic_19_lf", "relicicons/relic_18_ce", "relicicons/relic_08_sc", "relicicons/relic_16_aw",\n'
     '    "relicicons/relic_04_bs", "relicicons/relic_17_ch", "relicicons/relic_20_se", "relicicons/relic_24_hc", "relicicons/relic_03_wo",\n'
     '    "relicicons/relic_28_sy", "relicicons/relic_50_hr", "relicicons/relic_41_pp", "relicicons/relic_21_st", "relicicons/relic_22_cw",\n'
     '    "relicicons/relic_25_cv", "relicicons/relic_48_pt", "relicicons/relic_51_ae", "relicicons/relic_32_ps", "relicicons/relic_31_bo" };\n', "armory stand-in list", True, 'static readonly string[] StandIn'),
    ("lines", 'var sp = Resources.Load<Sprite>("UI/equipment/" + Slug(e.Name));', 'var sp = Resources.Load<Sprite>("UI/equipment/" + Slug(e.Name));',
     'var sp = Resources.Load<Sprite>("UI/equipment/" + Slug(e.Name));\n'
     'if (sp == null) { int k = System.Array.IndexOf(EquipmentCatalog.All, e); if (k >= 0 && k < StandIn.Length) sp = UISprites.Get(StandIn[k]); }', "armory icon fallback", False, 'UISprites.Get(StandIn[k])'),
]
EDITS["UI/Parch/ParchUI.cs"] = [
    ("lines", 'public static void TitleBar(', 'public static void TitleBar(',
     '// Greek-key strip: repeated (tiled) at its real height so the pattern is not stretched.\n'
     'public static void Strip(Transform parent, string name, string sprite, float x0, float y0, float x1, float y1)\n'
     '{\n'
     '    var sp = UISprites.Get(sprite);\n'
     '    if (sp == null) return;\n'
     '    var rt = UIFactory.NewRect(parent, name);\n'
     '    UIFactory.Stretch(rt, x0, y0, x1, y1);\n'
     '    var raw = rt.gameObject.AddComponent<RawImage>();\n'
     '    raw.raycastTarget = false;\n'
     '    rt.gameObject.AddComponent<TileStrip>().Setup(sp);\n'
     '}\n', "strip helper", True, 'public static void Strip('),
    ("lines", 'Pic(parent, "KeyStrip"', 'Pic(parent, "KeyStrip"',
     'Strip(parent, "KeyStrip", redStrip ? "ornaments/key_strip_terracotta" : "ornaments/key_strip_brown", 0f, 0.862f, 1f, 0.906f);', "title strip", False, 'Strip(parent, "KeyStrip"'),
    ("lines", 'var lbl = Txt(img.transform, type.ToString()', 'lbl.resizeTextMaxSize = 13;',
     'var lbl = Txt(img.transform, type.ToString(), 20, Parch.DarkBrown, TextAnchor.MiddleCenter, 0.40f, 0.10f, 0.95f, 0.90f, true);\n'
     'lbl.resizeTextForBestFit = true; lbl.resizeTextMinSize = 10; lbl.resizeTextMaxSize = 24;', "badge text", False, 'lbl.resizeTextMaxSize = 24;'),
]

TILE = '''using UnityEngine;
using UnityEngine.UI;

namespace Oddyseus.UI
{
    // Repeats a horizontal strip (Greek key) at its real height so the pattern is not stretched.
    [RequireComponent(typeof(RawImage))]
    public class TileStrip : MonoBehaviour
    {
        RawImage raw;
        float aspect = 16f;

        public void Setup(Sprite sprite)
        {
            raw = GetComponent<RawImage>();
            var tex = sprite.texture;
            tex.wrapMode = TextureWrapMode.Repeat;
            raw.texture = tex;
            aspect = (float)tex.width / Mathf.Max(1, tex.height);
            Apply();
        }

        void OnRectTransformDimensionsChange() { Apply(); }

        void Apply()
        {
            if (raw == null) return;
            Rect r = ((RectTransform)transform).rect;
            if (r.height < 1f) return;
            raw.uvRect = new Rect(0f, 0f, r.width / (r.height * aspect), 1f);
        }
    }
}
'''

def main():
    if not os.path.isdir(ROOT):
        print("Run this from the project root (the folder that contains 'Assets')."); sys.exit(1)
    ok = bad = 0
    for rel, ops in EDITS.items():
        path = ROOT + rel
        if not os.path.isfile(path):
            print("MISSING  " + path); bad += 1; continue
        text, crlf = read(path); notes = []
        try:
            for op in ops:
                if op[0] == "sub": text, n = op_sub(text, op[1], op[2], op[3]); notes.append(op[3] + ": " + n)
                else: text, n = op_lines(text, op[1], op[2], op[3], op[4], op[5], op[6]); notes.append(op[4] + ": " + n)
        except Fail as e:
            print("FAILED   " + rel + "  (" + str(e) + ")  -> file left unchanged"); bad += 1; continue
        write(path, text, crlf); ok += 1
        print("OK       " + rel); [print("           " + n) for n in notes]
    tp = ROOT + "UI/Parch/TileStrip.cs"
    if not os.path.isfile(tp):
        open(tp, "w", newline="\n").write(TILE); print("CREATED  " + tp)
    else: print("exists   " + tp)
    print("\nDone: %d files changed, %d problems." % (ok, bad))

main()
