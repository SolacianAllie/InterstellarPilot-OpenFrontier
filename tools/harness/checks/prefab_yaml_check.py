#!/usr/bin/env python3
"""Structural check over every .prefab in the project.

Unity's YAML is unusual in ways a stock loader dislikes: it opens with
`%YAML 1.1` / `%TAG` directives that must be honoured, documents are tagged
`!u!<classId>` and anchored `&<fileID>`, and the interesting data is the
mapping under the class name.

What is checked, and why each one matters here:

  * the file parses at all -- a truncated or hand-mangled prefab
  * the file still carries Unity's `%YAML 1.1` / `%TAG` prologue -- the
    per-document parse below re-adds a fresh prologue to every document,
    so a file that has LOST the directives parses fine here and is
    rejected by Unity outright ("File may be corrupted or was serialized
    with a newer version of Unity"). First fired 2026-11-07: an asset
    sweep dropped the prologue on 11 rewritten prefabs; Unity failed
    exactly those and the offline parse passed all of them.
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

# Deliberate, documented known problems: each entry is (relative path, needles).
# A finding whose file matches the path AND contains every needle is printed
# every run (never swallowed) but does not gate the check. Fail-closed in both
# directions: a DIFFERENT problem on the same file, or a problem on any other
# file, still fails; an entry that stops matching (its problem was fixed, or
# its message drifted) is reported so the entry cannot rot invisibly.
#
# ComponentTradeScreen: 9eb88df3 dedented a mis-indented m_Children block that
# 98ec3b45 had broken, and 42b1ad05 (2026-10-29) deliberately reverted it-
# "the forked-in fixes are not part of the right baseline until the lost
# week's content is recovered". The broken YAML is a TOMBSTONE of that lost
# content; fixing the file piecemeal now would clobber the eventual proper
# recovery. The needles pin the exact break (line 14, column 15) so any new
# or different damage on this file still fails.
KNOWN_ISSUES = [
    # Tombstone moved to PreRebuild/InterstellarPilot2/Extracted/GameObject/
    # in the 2026-11-08 blank-slate migration, and this check only walks
    # Assets/ -- so a live entry would print STALE forever. Re-add the entry
    # below (with its needles) if/when the tombstone returns under Assets;
    # the full narrative stays in AGENTS.md's tombstone trap.
    # ('Assets/InterstellarPilot2/Extracted/GameObject/ComponentTradeScreen.prefab',
    #  ['YAML parse error', 'line 14, column 15']),
]
KNOWN_HITS = set()


def known_issue(finding):
    for path, needles in KNOWN_ISSUES:
        if finding.startswith(path + ':') and all(n in finding for n in needles):
            KNOWN_HITS.add(path)
            return True
    return False


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

# Unity's `stripped` document marker (see parse_documents): `stripped` after
# the fileID on the first line of a document.
STRIPPED_RE = re.compile(r'^!u!\d+ &\d+ stripped\s*$')

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
        # Unity's nested-prefab output re-serialises child transforms it only
        # carries for modification bookkeeping as `--- !u!4 &<fileID> stripped`.
        # The `stripped` token makes the root a tagged scalar instead of a
        # mapping, which libyaml refuses outright, and there is nothing inside
        # one to check: the body is the source-object pointer it derives from,
        # and the raw-text regex checks above keep its fileID in the unique-id
        # sweep regardless. First exercised here by real editor session work
        # (nav-light chips nested on the Pioneer/Ranger, 2026-11-07).
        if STRIPPED_RE.match(part.split('\n', 1)[0]):
            continue
        doc = yaml.load(PROLOGUE + '--- ' + part, Loader=Loader)
        if doc:
            docs.append(doc)
    return docs


def analyse(path, text):
    """Structural findings for one prefab. Returns a list of strings."""
    rel = os.path.relpath(path, PROJECT)
    findings = []

    # The per-document parse below prepends a fresh PROLOGUE to every
    # document, which silently heals a file that has lost its own
    # directives -- offline. Unity does not heal it: the loader refuses
    # the whole asset. So check the file header itself.
    if not text.startswith(PROLOGUE):
        findings.append(
            '%s: missing %%YAML/%%TAG prologue -- Unity rejects the file '
            '("may be corrupted or was serialized with a newer version")' % rel)

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

    gated, knowns = [], []
    for f in findings:
        (knowns if known_issue(f) else gated).append(f)

    for f in gated:
        print(f)
    for f in knowns:
        print('  KNOWN (not gating): %s' % f)
    for path, _ in KNOWN_ISSUES:
        if path not in KNOWN_HITS:
            print('  STALE KNOWN ISSUE entry: %s no longer matches any '
                  'finding - the problem was fixed, or the file changed; '
                  'remove or update the entry.' % path)
    if gated:
        print('\n%d prefab structural problem(s) across %d prefabs'
              % (len(gated), total))
        return 1
    print('  prefabs: %d structurally sound (%d known issue(s) suppressed)'
          % (total, len(knowns)))
    return 0


if __name__ == '__main__':
    sys.exit(main())