#!/usr/bin/env python3
"""One MonoBehaviour per file, filename == class name, always.

Unity registers exactly ONE MonoScript per .cs file, named after the file.
A MonoBehaviour declared in a file of a different name therefore has no
MonoScript at all: AddComponent<T>() and the inspector both refuse it, and
it serialises as `m_Script: {fileID: 0}` -- a missing script on the prefab.

That is perfectly valid C#, so no compiler sees it. It fails only once the
component is actually put on a prefab, several steps from the mistake.
SavedHostItemUI was declared inside SavedHostItemsUI.cs and the transfer
tool silently produced an empty row while reporting success.

A class whose base chain cannot be resolved is NOT reported. A plugin
MonoBehaviour, or anything behind a conditional compile, is a guess, and a
checker that cries wolf gets ignored and then catches nothing.
"""
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.abspath(os.path.join(HERE, '..', '..', '..'))

# Unity classes a MonoBehaviour may derive from, at the root of a chain.
ROOTS = ('MonoBehaviour', 'ScriptableObject', 'EditorWindow', 'ScriptableRendererFeature')

CLASS_RE = re.compile(
    r'^\s*(?:\[[^\]]*\]\s*)*'
    r'(?:public|internal|private|protected|abstract|sealed|static|partial|\s)*'
    r'class\s+([A-Za-z_]\w*)\s*(?::\s*([^{]*))?',
    re.M)

GENERIC_RE = re.compile(r'class\s+[A-Za-z_]\w*\s*<')


def uncomment(text):
    """Blank out comments and string literals so a `class` inside one of
    them is not read as a declaration."""
    text = re.sub(r'/\*.*?\*/', ' ', text, flags=re.S)
    text = re.sub(r'//[^\n]*', ' ', text)
    text = re.sub(r'@"(?:[^"]|"")*"', '""', text, flags=re.S)
    text = re.sub(r'"(?:\\.|[^"\\\n])*"', '""', text)
    return text


def first_base(bases):
    """The first type named in a base list, generics stripped."""
    if not bases:
        return None
    raw = bases.split(',')[0].strip()
    raw = re.sub(r'\[.*?\]', '', raw)          # attribute on a base
    raw = raw.split('<')[0].strip()
    raw = raw.rsplit('.', 1)[-1].strip()        # drop a namespace qualifier
    return raw or None


def main():
    files = []
    for root, dirs, names in os.walk(os.path.join(PROJECT, 'Assets')):
        dirs[:] = [d for d in dirs if d not in ('.git',)]
        for n in names:
            if n.endswith('.cs'):
                files.append(os.path.join(root, n))

    # class name -> base name, across the whole project
    bases = {}
    parsed = {}
    for f in files:
        text = uncomment(open(f, encoding='utf-8', errors='ignore').read())
        found = []
        for m in CLASS_RE.finditer(text):
            name, blist = m.group(1), m.group(2)
            if name not in bases:
                bases[name] = first_base(blist)
            found.append((name, first_base(blist)))
        parsed[f] = (text, found)

    def is_unity_derived(name, seen=None):
        """True only when the chain provably reaches a Unity root type.

        The chain ends either at a root, or at a name with no project base --
        and an unresolvable base is NOT a root, so it stops rather than
        guessing. That is what keeps a plugin-derived component out of the
        report.
        """
        seen = set() if seen is None else seen
        if name in seen:
            return False
        seen.add(name)
        if name in ROOTS:
            return True
        base = bases.get(name)
        if not base:
            return False
        if base in bases:
            return is_unity_derived(base, seen)
        return base in ROOTS

    findings = []
    for f, (text, found) in sorted(parsed.items()):
        stem = os.path.splitext(os.path.basename(f))[0]
        for name, _base in found:
            if name == stem:
                continue
            # a generic class is not a MonoBehaviour Unity can attach anyway
            if re.search(r'class\s+%s\s*<' % re.escape(name), text):
                continue
            if is_unity_derived(name):
                findings.append((f, name))

    for f, name in findings:
        print('%s: MonoBehaviour-derived class %r has no MonoScript -- the '
              'file is %s' % (os.path.relpath(f, PROJECT), name,
                              os.path.basename(f)))
    if findings:
        print('\n%d MonoBehaviour(s) in a file of a different name' % len(findings))
        return 1
    print('  monoscript: %d .cs, one MonoBehaviour per file' % len(files))
    return 0


if __name__ == '__main__':
    sys.exit(main())