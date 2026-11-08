#!/usr/bin/env python3
"""Every ActiveUnit's Cloak Material must be the asset its own shells reference.

The bug this guards: material identity is a guid, but humans and tools write
about assets by NAME. Mid-migration, the same basename exists in both
InterstellarPilot2/Extracted/Material and OpenFrontier/Material as DIFFERENT
assets (different guids, and after the texture migration, different content).
A field assignment resolved by name can therefore land on the wrong twin and
nobody notices: Unity only alarms on duplicate guids, which these are not.

So the invariant is enforced where the truth actually lives, inside each
prefab:

  For every serialized ActiveUnit, the CloakMaterial guid must describe the
  same asset the unit's own shell renderers carry at slot 0 - matched the
  way FoldMaterialForSlot matches them at runtime: any shell whose slot
  count equals the hull renderer's supplies the fold. A unit whose shells
  are gone (per-ship cleanup) has no shell-derived guid; for those the
  field is the sole source and the pair check is skipped. Either way the
  field material must carry _CloakAmount, or it can never be a fold.

Findings use the same shapes as the runtime probe:
  FAIL  invariant broken - the field describes an asset the unit was not
        authored to wear, or a material that cannot fold. The runtime
        cannot self-heal either.
  WARN  shells fold-capable while no field is set (transitional), one
        unit wearing several different slot-0 folds across LODs, or a
        fold-capable material whose basename says nothing about cloaking.
  INFO  same-named materials across trees, so the twin population stays
        visible while the IP2 reliance is being wound down.
"""
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
ASSETS = os.path.join(PROJECT, "Assets")
AU_GUID_FILE = "OpenFrontierLogic/OpenFrontier.IP/OpenFrontier.IP.Engine/ActiveUnit.cs.meta"


def read(path):
    try:
        with open(path, encoding="utf-8", errors="replace") as f:
            return f.read()
    except OSError:
        return ""


def guid_of(path):
    m = re.search(r"guid: ([0-9a-f]{32})", read(path))
    return m.group(1) if m else None


# ---- asset maps: one walk over Assets, everything keyed by guid -----------

mat_info = {}      # guid -> (basename, relpath)
name_sites = {}    # basename -> [(guid, relpath)]
shader_fold = {}   # shader guid -> carries _CloakAmount

for base, dirs, files in os.walk(ASSETS):
    dirs[:] = [d for d in dirs if d != "Library"]
    for fn in files:
        p = os.path.join(base, fn)
        if fn.endswith(".mat.meta"):
            g = guid_of(p)
            if g:
                rel = os.path.relpath(p[:-5], ASSETS)
                mat_info[g] = (fn[:-9], rel)
                name_sites.setdefault(fn[:-9], []).append((g, rel))
        elif fn.endswith(".shader") and os.path.exists(p + ".meta"):
            g = guid_of(p + ".meta")
            if g:
                shader_fold[g] = "_CloakAmount" in read(p)

# material guid -> shader guid (cheap reread; .mat bodies are small)
mat_shader = {}
for g, (_nm, rel) in mat_info.items():
    m = re.search(r"m_Shader: \{[^}]*guid: ([0-9a-f]{32})", read(os.path.join(ASSETS, rel)))
    if m:
        mat_shader[g] = m.group(1)

# naming convention: a fold-capable material says so. The *Fade names came
# from the old extracted convention and stopped meaning anything once this
# project's cloak became a material property instead of a mesh toggle; after
# the 2026-11 renames every fold reads *Cloaked / *_cloaked / *Cloak*. The
# advisory keeps the population honest the moment someone authors the next
# stray fold into differently-named material.
naming_warns = []
for g, sg in mat_shader.items():
    if sg in shader_fold and shader_fold[sg]:
        nm = mat_info[g][0]
        if "cloak" not in nm.lower():
            naming_warns.append(f"fold-capable material '{nm}' says nothing about cloaking")



def slot_states(guids):
    """[(guid, matname, 'FOLD'|'std'|'?')] for one renderer's material slots."""
    out = []
    for g in guids:
        nm = mat_info.get(g, ("?", "?"))[0]
        sg = mat_shader.get(g)
        if sg is None or sg not in shader_fold:
            out.append((g, nm, "?"))
        else:
            out.append((g, nm, "FOLD" if shader_fold[sg] else "std"))
    return out


# ---- per-prefab parse ------------------------------------------------------

au_guid = guid_of(os.path.join(ASSETS, AU_GUID_FILE))
if not au_guid:
    print(f"FAIL cannot resolve ActiveUnit.cs guid from {AU_GUID_FILE}")
    sys.exit(1)


def field_ids(block, field):
    m = re.search(rf"{field}:\n((?:\s*- \{{fileID: (\d+)\}}\n)+)", block)
    if m:
        return re.findall(r"fileID: (\d+)", m.group(1))
    o = re.search(rf"{field}: \{{fileID: (\d+)\}}", block)
    return [o.group(1)] if o and o.group(1) != "0" else []


fails, warns = [], []
counts = {"hull": 0, "field": 0, "field-sole": 0, "needs-field": 0}

for base, dirs, files in os.walk(ASSETS):
    dirs[:] = [d for d in dirs if d != "Library"]
    for fn in sorted(files):
        if not fn.endswith(".prefab"):
            continue
        path = os.path.join(base, fn)
        text = read(path)
        if f"guid: {au_guid}" not in text:
            continue
        rel = os.path.relpath(path, ASSETS)
        blocks = {
            m.group(2): (m.group(1), m.group(3))
            for m in re.finditer(r"^--- !u!(\d+) &(\d+)\n(.*?)(?=^--- !u!|\Z)", text, re.S | re.M)
        }
        goblocks = {}
        for i, (ty, b) in blocks.items():
            if ty == "1":
                nm = re.search(r"m_Name: (.*)", b)
                goblocks[i] = nm.group(1).strip() if nm else "?"
        rend = {}
        for rid, (ty, b) in blocks.items():
            if ty != "23":
                continue
            g = re.search(r"m_GameObject: \{fileID: (\d+)\}", b)
            rend[rid] = (goblocks.get(g.group(1), "?") if g else "?",
                         re.findall(r"- \{fileID: 2100000, guid: ([0-9a-f]{32})", b))

        for _aid, (ty, b) in sorted(blocks.items()):
            if ty != "114" or f"guid: {au_guid}" not in b:
                continue
            hulls = []
            for i in field_ids(b, "UnitRenderers") or field_ids(b, "UnitRenderer"):
                if i in rend:
                    hulls.append(slot_states(rend[i][1]))
            shells = []
            for i in field_ids(b, "CloakRenderers") or field_ids(b, "CloakRenderer"):
                if i in rend:
                    shells.append(slot_states(rend[i][1]))
            fc = re.search(r"CloakMaterial: \{fileID: 2100000, guid: ([0-9a-f]{32})", b)
            field = fc.group(1) if fc else None

            hull_fold = any(s == "FOLD" for hm in hulls for _, _, s in hm)

            # the fold guids the runtime would adopt: any shell whose slot
            # count equals a hull renderer's, folding at slot 0
            fold_guids = set()
            for hm in hulls:
                for sm in shells:
                    if sm and len(sm) == len(hm) and sm[0][2] == "FOLD":
                        fold_guids.add(sm[0][0])

            if field:
                counts["field"] += 1
                if not hull_fold and not fold_guids:
                    counts["field-sole"] += 1
            elif hull_fold:
                counts["hull"] += 1
            elif fold_guids:
                counts["needs-field"] += 1
                warns.append(f"{rel}: shells carry fold at slot 0 but Cloak Material is unset")

            if field and field in mat_info:
                sg = mat_shader.get(field)
                if sg is not None and sg in shader_fold and not shader_fold[sg]:
                    fails.append(
                        f"{rel}: Cloak Material '{mat_info[field][0]}' lacks _CloakAmount")
            if field and fold_guids and field not in fold_guids:
                what = mat_info.get(field, ("<unknown-asset>",))[0]
                alt = ", ".join(f"{mat_info[g][0]}({g[:8]})" for g in sorted(fold_guids))
                fails.append(
                    f"{rel}: Cloak Material '{what}'({field[:8]}) is not the shells' "
                    f"fold {alt} - mis-assigned twin?")
            elif len(fold_guids) > 1:
                warns.append(
                    f"{rel}: shells fold at slot 0 with {len(fold_guids)} different "
                    f"materials across LODs ({', '.join(sorted(g[:8] for g in fold_guids))})")

# ---- report ----------------------------------------------------------------

if fails:
    for line in fails:
        print("FAIL " + line)
    sys.exit(1)
for line in sorted(warns + naming_warns):
    print("WARN " + line)
twins = sorted(
    nm for nm, sites in name_sites.items()
    if len({os.path.dirname(r) for _, r in sites}) > 1
)
if twins:
    print(f"INFO same-named materials in >1 tree (twin radar): {', '.join(twins)}")
print(
    f"OK {counts['field']} field-based, {counts['field-sole']} field-only, "
    f"{counts['needs-field']} shells-waiting-on-field, 0 misassigned"
)
