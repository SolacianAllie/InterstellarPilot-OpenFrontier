#!/usr/bin/env python3
"""Cloakify the legacy fade/ghost shell materials.

Every unit that still fades to a grey ghost (URP Lit alpha shells) gets its
ghost material re-shaded onto OpenFrontier/Ship Cloak, carrying that unit's
hull surface recipe - the arrangement the Magnus family already has
(MaterialMagnusCloaked = Magnus surface recipe + cloak shader + transparent
blend + DepthOnly/SHADOWCASTER passes disabled).

The transform is derived from the Magnus pair and PROVEN by round-tripping
MaterialMagnusStandard through it and diffing against MaterialMagnusCloaked:
the only acceptable differences are the fold-art texture GUIDs (the user's
dedicated Magnus shell art) - everything else must match char for char.

Ghost files keep their names and GUIDs, so every shell renderer in every
prefab that referenced the fade material now references the cloak material -
no prefab surgery, no reference churn.

Property cards (the "- _Name:" spans) are copied VERBATIM from the hull when
a property exists there, so formatting (inline scalars vs. multi-line texture
bodies) always matches what Unity itself serialized.
"""
import os, re, sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
MAGNUS_HULL = os.path.join(ROOT, "Assets/OpenFrontier/Material/Ships/Magnus/MaterialMagnusStandard.mat")
MAGNUS_CLOAK = os.path.join(ROOT, "Assets/OpenFrontier/Material/Ships/Magnus/MaterialMagnusCloaked.mat")

# OpenFrontier/Ship Cloak
CLOAK_SHADER_GUID = "c9f2d8847ba14e6d9b03a55f21c76e04"

# Forced transparent state every fold-shell material carries
# (read off MaterialMagnusCloaked exactly as the editor saved it).
FORCED_FLOATS = {
    "_DstBlend": "10",
    "_DstBlendAlpha": "10",
    "_Mode": "2",
    "_Surface": "1",
    "_ZWrite": "0",
}

EXTRA_INVALID = ["_ALPHABLEND_ON", "_ALPHAPREMULTIPLY_ON", "_DETAIL_MULX2", "_SURFACE_TYPE_TRANSPARENT"]

PROP_SECTIONS = ("m_TexEnvs", "m_Floats", "m_Colors")

def parse_cards(path):
    """Return (sections, keywords, lines).

    sections: section name -> list of cards; card = {'name': prop, 'lines': [verbatim]}
    keywords: {'m_ValidKeywords': [...], 'm_InvalidKeywords': [...]}
    """
    with open(path) as fh:
        lines = fh.read().splitlines()
    sections = {}
    keywords = {}
    current_section = None
    kw_current = None
    card = None
    for line in lines:
        m = re.match(r"^    (m_TexEnvs|m_Floats|m_Colors):$", line)
        if m:
            if current_section and card is not None:
                sections[current_section].append(card); card = None
            keywords_flushed = True
            current_section = m.group(1)
            sections[current_section] = []
            continue
        if re.match(r"^  m_ValidKeywords:$|  m_InvalidKeywords:$", line) and line.startswith("  m_"):
            if current_section and card is not None:
                sections[current_section].append(card); card = None
            kw_current = line.strip()[:-1]
            keywords[kw_current] = []
            current_section = None
            continue
        if kw_current and line.startswith("  - "):
            keywords[kw_current].append(line[4:].strip())
            continue
        if kw_current and not line.startswith("  - "):
            kw_current = None
        m = re.match(r"^    - ([A-Za-z_][A-Za-z0-9_]*):(.*)$", line)
        if m and current_section:
            if card is not None:
                sections[current_section].append(card)
            card = {"name": m.group(1), "lines": [line], "inline": m.group(2).strip()}
            continue
        if card and line.startswith("        "):
            card["lines"].append(line)
            continue
        if card is not None and not line.startswith("        ") and not line.startswith("    - "):
            if current_section:
                sections[current_section].append(card)
            card = None
            current_section = None
    if card is not None and current_section:
        sections[current_section].append(card)
    return sections, keywords, lines

def cloakify(hull_path, name, ghost_path=None):
    hull_sections, hull_kw, _ = parse_cards(hull_path)
    cloak_sections, _, template_lines = parse_cards(MAGNUS_CLOAK)
    # Textures are per-mesh art: the ghost's own non-null textures win (their
    # UVs pair with the ghost mesh), the hull only fills slots the ghost left
    # null. Colors and floats stay hull-based - the ghost's carry the legacy
    # fade tint that must not leak into the fold.
    ghost_cards = {}
    ghost_sectionsm = {}
    if ghost_path and os.path.exists(ghost_path):
        ghost_sectionsm, _, _ = parse_cards(ghost_path)
        for sec, cards in ghost_sectionsm.items():
            for card in cards:
                ghost_cards[card["name"]] = card

    def texture_guid(card):
        for row in card["lines"][1:]:
            m = re.search(r"m_Texture: \{fileID: (\d+), guid: ([0-9a-f]+)", row)
            if m:
                return m.group(2)
        return None

    valid = list(hull_kw.get("m_ValidKeywords", []))
    invalid = sorted(dict.fromkeys(hull_kw.get("m_InvalidKeywords", []) + EXTRA_INVALID))

    hull_cards = {}
    for sec, cards in hull_sections.items():
        for card in cards:
            hull_cards[card["name"]] = card

    out = []
    emitted = set()
    i = 0
    while i < len(template_lines):
        line = template_lines[i]
        if line.startswith("  m_Name: ") and i + 1 < len(template_lines) and template_lines[i + 1].startswith("  m_Shader:"):
            out.append("  m_Name: " + name)
            i += 1
            continue
        if line.startswith("  m_Shader: "):
            out.append("  m_Shader: {fileID: 4800000, guid: " + CLOAK_SHADER_GUID + ", type: 3}")
            i += 1
            continue
        if line.startswith("  m_ValidKeywords:"):
            out.append(line)
            out.extend("  - " + k for k in valid)
            while i + 1 < len(template_lines) and template_lines[i + 1].startswith("  - "):
                i += 1
            i += 1
            continue
        if line.startswith("  m_InvalidKeywords:"):
            out.append(line)
            out.extend("  - " + k for k in invalid)
            while i + 1 < len(template_lines) and template_lines[i + 1].startswith("  - "):
                i += 1
            i += 1
            continue
        m = re.match(r"^    (m_TexEnvs|m_Floats|m_Colors):$", line)
        if m:
            current_section = m.group(1)
            out.append(line)
            for tc in cloak_sections[current_section]:
                pname = tc["name"]
                src = hull_cards.get(pname)
                if current_section == "m_TexEnvs" and ghost_cards:
                    # per-mesh textures: ghost art wins, hull fills nulls
                    gcard = ghost_cards.get(pname)
                    if gcard is not None and texture_guid(gcard):
                        src = gcard
                    elif gcard is None and (src is None or not texture_guid(src)):
                        src = tc  # neither ghost nor hull has art: keep template (nulls for built-ins)
                emit = list((src if src is not None else tc)["lines"])
                if current_section == "m_Floats" and pname in FORCED_FLOATS:
                    emit = [re.sub(r"^(\s+- " + re.escape(pname) + r":).*$",
                                   r"\1 " + FORCED_FLOATS[pname], emit[0])] + emit[1:]
                out.extend(emit)
                emitted.add(pname)
            # hull card-props the cloak template lacks: keep their cards, the
            # shader ignores extra keys and the editor re-serializes cleanly.
            for hcard in hull_sections.get(current_section, []):
                if hcard["name"] in emitted:
                    continue
                out.extend(hcard["lines"])
                emitted.add(hcard["name"])
            # ghost-only texture cards too (per-mesh art survives the schema change)
            if current_section == "m_TexEnvs":
                for gcard in ghost_sectionsm.get(current_section, []):
                    if gcard["name"] in emitted:
                        continue
                    out.extend(gcard["lines"])
                    emitted.add(gcard["name"])
            # skip the template's own property rows, they were rebuilt above
            while i + 1 < len(template_lines) and (
                    template_lines[i + 1].startswith("    - ") or template_lines[i + 1].startswith("        ")):
                i += 1
            i += 1
            continue
        out.append(line)
        i += 1
    return "\n".join(out) + "\n"

def main():
    gen = cloakify(MAGNUS_HULL, "MaterialMagnusCloaked")
    ref = open(MAGNUS_CLOAK).read()
    gl, rl = gen.splitlines(), ref.splitlines()
    print(f"round-trip lines: gen {len(gl)} ref {len(rl)}")
    ok = True
    diffs = 0
    for i in range(max(len(gl), len(rl))):
        x = gl[i] if i < len(gl) else "<EOF>"
        y = rl[i] if i < len(rl) else "<EOF>"
        if x != y:
            ok = False
            diffs += 1
            if diffs <= 20:
                print(f"  L{i+1}:\n   gen {x!r}\n   ref {y!r}")
    print("ROUND TRIP:", "OK" if ok else f"MISMATCH ({diffs} lines)")
    return 0 if ok else 1

if __name__ == "__main__":
    sys.exit(main())
