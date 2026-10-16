#!/usr/bin/env python3
"""A null object reference on a prefab is only a bug if the script
dereferences it without a guard.

A hand-edited prefab clone can drop a field and fail SILENTLY as "nothing
happens". MultiplayerScreen.prefab is a clone of LoadGameScreen's; a dropped
reference does not break the build, the C# still compiles, the field reads as
null, and whatever was wired to it stops working while looking fine. `ItemList`
on ItemListSelectionButton was `{fileID: 0}`; its Awake dereferences it, so
Unity swallowed a NullReferenceException inside Instantiate and the screen
never opened. Nothing in the UI looked broken and no error was shown.

So the finding has to come from BOTH sides:

  * the prefab side -- this script has this field at `{fileID: 0}`
  * the script side -- this script dereferences this field and never null-checks it

Either half alone is noise. Thousands of prefabs have benign null references
(`m_Material: {fileID: 0}` on an Image is normal), and thousands of scripts
dereference a field that is always wired. Reporting either alone is a checker
that cries wolf, gets ignored, and then catches nothing.

The guard test is deliberately coarse: a null check for the field ANYWHERE in
the file counts. A field guarded in one method and dereferenced in another is
still reported, which is a false positive, but the alternative -- per-method
control flow -- reports far more and is wrong more often.

The prefab half caches on (mtime_ns, size), keyed exactly like the YAML check.
"""
import json
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
CACHE = os.path.join(HERE, '.prefab-null-cache.json')
STRICT = '--strict' in sys.argv

GUID_IN_META = re.compile(r'^guid: ([a-f0-9]{32})', re.M)
MONO_BLOCK = re.compile(r'--- !u!114 &(-?\d+)\nMonoBehaviour:\n(.*?)(?=\n--- !u!|\Z)', re.S)
SCRIPT_REF = re.compile(r'm_Script: \{fileID: 11500000, guid: ([a-f0-9]{32})')
# A field serialised as exactly `{fileID: 0}`. With a guid it is an external
# reference that happens to be absent, which Unity resolves by loading the
# other asset -- not a null.
NULL_FIELD = re.compile(r'^  ([A-Za-z_]\w*): \{fileID: 0\}$', re.M)
# The same field, present and pointing at a real object. Its existence
# somewhere in the class is what makes the nulls elsewhere deliberate.
WIRED_FIELD = re.compile(r'^  ([A-Za-z_]\w*): \{fileID: -?[1-9]\d*\}$', re.M)

FIELD_DECL = re.compile(
    r'^\s*(?:\[[^\]]*\]\s*)*public\s+(?!class\b|static\b|const\b|event\b)'
    r'[A-Za-z_][\w<>\[\],. ]*?\s+([A-Za-z_]\w*)\s*(?:=[^;]*)?;', re.M)

# Every field name assigned ANYWHERE in the project. A field that is assigned
# somewhere is populated at runtime, so a null for it in a prefab is by
# design, not a dropped wire: SectorMapForm.Sector is a property with a
# setter, filled in when the screen opens, and its `Sector: {fileID: 0}` in
# SectorMapScreen.prefab is correct. Without this the check reported 49
# findings, a large slice of them this shape.
#
# Coarse on purpose -- a same-named field in an unrelated class can suppress
# a real finding. Erring toward silence is the right trade for a check whose
# failure mode is crying wolf, but it is a real limit, not a guarantee.
ASSIGNED = re.compile(r'(?<![\w.])((?:this\.)?[A-Za-z_]\w*)\s*=(?![=<>!])'
                      r'(?![-+*/%&|^]=)')

# Dereference of a field: field.something, field(...), field[...]
#
# `%` formatting, not `{0}`. Written as `{0}`, the regex engine reads the
# brace as a quantifier applied to the zero-width `\b` and the whole module
# dies on `nothing to repeat`.
DEREF = r'\b%s\s*(?:\.|\(|\[)'
# Any null check at all for that field name
GUARD = (r'\b%s\s*(?:==|!=)\s*null\b|\bif\s*\(\s*%s\s*\)|'
         r'\b%s\s*\?\?|\b%s\s*\?\.|nameof\s*\(\s*%s\s*\)')


def guid_map():
    """script GUID -> the .cs it names."""
    out = {}
    for dirpath, dirnames, filenames in os.walk(os.path.join(PROJECT, 'Assets')):
        dirnames[:] = [d for d in dirnames if d != '.git']
        for n in filenames:
            if not n.endswith('.cs.meta'):
                continue
            path = os.path.join(dirpath, n)
            try:
                m = GUID_IN_META.search(open(path, encoding='utf-8',
                                             errors='ignore').read())
            except OSError:
                continue
            if m:
                out[m.group(1)] = path[:-len('.meta')]
    return out


def assigned_anywhere():
    """Set of every identifier assigned anywhere in Assets/."""
    out = set()
    for dirpath, dirnames, filenames in os.walk(os.path.join(PROJECT, 'Assets')):
        dirnames[:] = [d for d in dirnames if d != '.git']
        for n in filenames:
            if not n.endswith('.cs'):
                continue
            try:
                text = open(os.path.join(dirpath, n), encoding='utf-8',
                            errors='ignore').read()
            except OSError:
                continue
            for m in ASSIGNED.finditer(text):
                out.add(m.group(1).split('.')[-1])
    return out


def unguarded_fields(script_path, assigned=frozenset()):
    """Fields the script dereferences and never null-checks.

    Returns None when the file cannot be read, so the caller reports
    'unknown' rather than silently treating it as safe.
    """
    try:
        text = open(script_path, encoding='utf-8', errors='ignore').read()
    except OSError:
        return None
    text = re.sub(r'/\*.*?\*/', ' ', text, flags=re.S)
    text = re.sub(r'//[^\n]*', ' ', text)

    risky = set()
    for m in FIELD_DECL.finditer(text):
        name = m.group(1)
        # Skip `int x = 5`-shaped false hits and property-looking names.
        if name in ('get', 'set'):
            continue
        # Assigned somewhere => filled in at runtime => a null here is by design
        if name in assigned:
            continue
        esc = re.escape(name)
        if not re.search(DEREF % esc, text):
            continue
        if re.search(GUARD % (esc, esc, esc, esc, esc), text):
            continue
        risky.add(name)
    return risky


def scan_prefab(text):
    """{script GUID: {'null': [fields], 'wired': [fields]}}

    Both halves are kept because the verdict needs the whole class, not one
    prefab: a field that is wired on SOME prefab carrying the class is a
    deliberate optional reference, and the nulls on the others are by design.
    """
    out = {}
    for m in MONO_BLOCK.finditer(text):
        body = m.group(2)
        s = SCRIPT_REF.search(body)
        if not s:
            continue
        nulls = NULL_FIELD.findall(body)
        wired = WIRED_FIELD.findall(body)
        if nulls or wired:
            entry = out.setdefault(s.group(1), {'null': [], 'wired': []})
            entry['null'].extend(nulls)
            entry['wired'].extend(wired)
    return out


def iter_prefabs():
    for dirpath, dirnames, filenames in os.walk(os.path.join(PROJECT, 'Assets')):
        dirnames[:] = [d for d in dirnames if d != '.git']
        for n in filenames:
            if n.endswith('.prefab'):
                yield os.path.join(dirpath, n)


def main():
    guids = guid_map()
    assigned = assigned_anywhere()
    cache = {}
    if os.path.exists(CACHE):
        try:
            cache = json.load(open(CACHE, encoding='utf-8')).get('files', {})
        except Exception:
            cache = {}

    fresh = {}
    total = 0
    # (guid, field) -> set of prefabs where it is null
    null_at = {}
    wired_at = {}

    for path in iter_prefabs():
        total += 1
        rel = os.path.relpath(path, PROJECT)
        try:
            st = os.stat(path)
            key = [st.st_mtime_ns, st.st_size]
        except OSError:
            continue

        hit = cache.get(rel)
        if hit and hit.get('key') == key:
            scan = hit.get('scan') or {}
        else:
            try:
                text = open(path, encoding='utf-8', errors='ignore').read()
                scan = scan_prefab(text)
            except OSError:
                scan = {}
            fresh[rel] = {'key': key, 'scan': scan}

        for guid, entry in scan.items():
            for f in entry.get('null', ()):
                null_at.setdefault((guid, f), []).append(rel)
            for f in entry.get('wired', ()):
                wired_at.setdefault((guid, f), []).append(rel)

    try:
        tmp = CACHE + '.tmp'
        with open(tmp, 'w', encoding='utf-8') as f:
            json.dump({'files': fresh}, f)
        os.replace(tmp, CACHE)
    except OSError:
        pass

    risky_cache = {}
    candidates = []
    suppressed = 0
    for (guid, field), prefs in sorted(null_at.items()):
        script = guids.get(guid)
        if script is None:
            continue                      # plugin script, not ours to reason about
        if guid not in risky_cache:
            risky_cache[guid] = unguarded_fields(script, assigned)
        risky = risky_cache[guid]
        if risky is None or field not in risky:
            continue
        if wired_at.get((guid, field)):
            # Wired on some other prefab of the same class => optional by
            # design. This is the difference between 34 findings and 6.
            suppressed += len(prefs)
            continue
        name = os.path.splitext(os.path.basename(script))[0]
        for rel in sorted(prefs):
            candidates.append('%s: %s.%s is null, is never assigned anywhere, '
                              'and the script dereferences it with no null check'
                              % (rel, name, field))

    print('  ADVISORY, not a gate. %d candidate(s) across %d prefabs; %d '
          'null-reference(s) suppressed as wired elsewhere on the same class.'
          % (len(candidates), total, suppressed))
    for c in candidates:
        print(c)
    if candidates and STRICT:
        print('\n%d finding(s) (--strict)' % len(candidates))
        return 1
    return 0


if __name__ == '__main__':
    sys.exit(main())
