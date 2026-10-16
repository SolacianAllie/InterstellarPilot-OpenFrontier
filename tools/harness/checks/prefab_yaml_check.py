#!/usr/bin/env python3
"""Structural check over every .prefab in the project.

Unity's YAML is unusual in ways a stock loader dislikes: it opens with
`%YAML 1.1` / `%TAG` directives that must be honoured, documents are tagged
`!u!<classId>` and anchored `&<fileID>`, and the interesting data is the
mapping under the class name.

What is checked, and why each one matters here:

  * the file parses at all -- a truncated or hand-mangled prefab
  * every document has a unique fileID -- a duplicate silently binds one
    object's data to another
  * every `m_Component` and `m_GameObject` reference resolves inside the
    file -- `{fileID: 0}` is Unity's own encoding for null and is fine, but
    a reference to a fileID that is not there is a dangling reference
  * every `m_Script` resolves -- `{fileID: 0}` here is a MISSING SCRIPT,
    which is not null, it is a component Unity cannot load

PERFORMANCE. The original took 53s, which was not the compiler. Three
things fixed it and all three are load-bearing:

  * `yaml.load_all(text, Loader=CSafeLoader)` -- same parser, compiled
    instead of interpreted: 6.7s -> 0.9s over 200 prefabs. Falls back to
    SafeLoader if libyaml is missing, and NEVER fails on that account.
  * a cache on (mtime_ns, size) in checks/.prefab-yaml-cache.json: 12.9s
    -> 142ms on any run where nothing under Assets/ was touched, which is
    nearly every run.
  * THE CACHE STORES THE MESSAGES, NOT THE VERDICT. Caching the boolean
    would make a second run of a failing check print "0 failures" and
    quietly invert the check -- which is this harness's signature failure
    mode, so the distinction is the whole point of storing messages.
"""
import json
import os
import re
import sys

try:
    import yaml
    try:
        from yaml import CSafeLoader as Loader
    except ImportError:
        from yaml import SafeLoader as Loader
except ImportError:
    print('  prefabs: PyYAML not available, skipped', file=sys.stderr)
    sys.exit(0)

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
CACHE = os.path.join(HERE, '.prefab-yaml-cache.json')

HEADER_RE = re.compile(r'^--- !u!(\d+) &(-?\d+)', re.M)

# An INTERNAL reference is exactly `{fileID: N}` -- no guid, no type. Anything
# with a guid points at another asset and is perfectly valid, so the absence
# of `guid:` is what makes a reference checkable at all. Matching loosely with
# `component.*?fileID.*?(\d+)` instead spans across intervening keys and
# reported HudScreen's `m_fontAsset: {fileID: 11400000, guid: ...}` as a
# dangling m_Component: a false positive on a healthy prefab.
COMPONENT_RE = re.compile(r'component: \{fileID: (-?\d+)\}')
GAMEOBJ_RE = re.compile(r'm_GameObject: \{fileID: (-?\d+)\}')
# m_Script normally carries a guid. `{fileID: 0}` with NO guid is a component
# Unity cannot load at all -- a missing script, not a null reference.
MISSING_SCRIPT_RE = re.compile(r'm_Script: \{fileID: 0\}')

MONOBEHAVIOUR_ID = '114'

# Unity's documents are tagged `!u!<classId>`, which the `%TAG` directive
# expands to `tag:unity3d.com,2011:<classId>`. Without a constructor for
# that prefix PyYAML fails EVERY prefab with "could not determine a
# constructor", which reads as 2310 findings and means nothing at all.
yaml.add_multi_constructor(
    'tag:unity3d.com,2011:',
    lambda loader, suffix, node: loader.construct_mapping(node, deep=True),
    Loader=Loader)


def iter_prefabs():
    for dirpath, dirnames, filenames in os.walk(os.path.join(PROJECT, 'Assets')):
        dirnames[:] = [d for d in dirnames if d != '.git']
        for n in filenames:
            if n.endswith('.prefab'):
                yield os.path.join(dirpath, n)


PROLOGUE = '%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'


def parse_documents(text):
    """Parse every document in a prefab.

    PyYAML scopes tag handles per document, so `load_all` on the whole file
    resolves `!u!1` in the first document and then fails with "undefined tag
    handle" on the second -- which is every document after the first, i.e.
    almost the whole file. Each document is therefore re-parsed with its own
    directive prologue.
    """
    docs = []
    parts = re.split(r'(?m)^--- ', text)
    for part in parts[1:]:
        doc = yaml.load(PROLOGUE + '--- ' + part, Loader=Loader)
        if doc:
            docs.append(doc)
    return docs


def analyse(path, text):
    """Structural findings for one prefab. Returns a list of strings."""
    rel = os.path.relpath(path, PROJECT)
    findings = []

    m = HEADER_RE.search(text)
    if not m:
        return ['%s: no `--- !u!<class> &<fileID>` document header' % rel]

    try:
        docs = parse_documents(text)
    except Exception as e:
        return ['%s: YAML parse error: %s' % (rel, str(e).replace('\n', ' ')[:160])]
    if not docs:
        return ['%s: parses to zero documents' % rel]

    # Unique anchors, straight off the text: two objects sharing a fileID is
    # how a hand-edited clone loses a field without looking wrong.
    ids = HEADER_RE.findall(text)
    seen = set()
    for _cls, fid in ids:
        if fid in seen:
            findings.append('%s: duplicate fileID &%s -- two objects share one id'
                            % (rel, fid))
            break
        seen.add(fid)

    # Reference integrity is a whole-file property: a fileID resolves against
    # every object in the prefab, not just the one it appears next to.
    for cid in COMPONENT_RE.findall(text):
        if cid != '0' and cid not in seen:
            findings.append('%s: m_Component points at fileID %s, which is not '
                            'in this prefab' % (rel, cid))
    for gid in GAMEOBJ_RE.findall(text):
        if gid != '0' and gid not in seen:
            findings.append('%s: m_GameObject points at fileID %s, which is not '
                            'in this prefab' % (rel, gid))
    n_missing = len(MISSING_SCRIPT_RE.findall(text))
    if n_missing:
        findings.append('%s: %d component(s) with m_Script fileID 0 -- a MISSING '
                        'SCRIPT, not a null reference' % (rel, n_missing))

    for i, doc in enumerate(docs):
        if not isinstance(doc, dict):
            findings.append('%s: document %d is %s, not a mapping'
                            % (rel, i, type(doc).__name__))
            continue
        if not any(isinstance(v, dict) for v in doc.values()):
            findings.append('%s: document %d has no mapping body' % (rel, i))

    return findings


def main():
    cache = {}
    if os.path.exists(CACHE):
        try:
            cache = json.load(open(CACHE, encoding='utf-8')).get('files', {})
        except Exception:
            cache = {}

    findings = []
    fresh = {}
    total = 0
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
            # Re-emit the cached messages. Skipping this is the inversion this
            # cache is warned about: storing messages is no help if a warm run
            # reads them and does not print them, because then a second run of
            # a FAILING check reports "0 failures" and ALL CHECKS PASSED.
            # Caught the first time it was written, by running cold then warm.
            fresh[rel] = hit
            findings.extend(hit.get('messages') or [])
            continue

        try:
            text = open(path, encoding='utf-8', errors='ignore').read()
        except OSError as e:
            msgs = ['%s: unreadable: %s' % (rel, e)]
        else:
            msgs = analyse(path, text)
        fresh[rel] = {'key': key, 'messages': msgs}
        findings.extend(msgs)

    try:
        tmp = CACHE + '.tmp'
        with open(tmp, 'w', encoding='utf-8') as f:
            json.dump({'files': fresh}, f)
        os.replace(tmp, CACHE)
    except OSError:
        pass

    for f in findings:
        print(f)
    if findings:
        print('\n%d prefab structural problem(s) across %d prefabs'
              % (len(findings), total))
        return 1
    print('  prefabs: %d structurally sound' % total)
    return 0


if __name__ == '__main__':
    sys.exit(main())