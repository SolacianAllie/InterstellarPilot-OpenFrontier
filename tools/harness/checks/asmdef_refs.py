#!/usr/bin/env python3
"""A script must declare, in its asmdef, every project assembly it uses.

Unity's asmdef references are NOT transitive here. OpenFrontier.IP.Multiplayer
referenced OpenFrontier.IP, which references
OpenFrontier.IP.SavedGames.V2.Model, and the editor still refused with CS0234
on a namespace the second assembly plainly provides. An assembly can only use
types from assemblies it DIRECTLY references.

This is invisible offline by design: the generated csproj is deliberately
generous with references so it compiles, which is exactly why the strict
declaration check has to live here instead.

Only namespaces that exactly one project assembly can supply are reported.
A namespace two assemblies both live in is genuinely ambiguous, and a
finding that cannot be right is worse than no finding.
"""
import json
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.abspath(os.path.join(HERE, '..', '..', '..'))

# The WHOLE dotted namespace, not its first segment. Capturing `[A-Za-z_]\w*`
# and then requiring `;` matches `using UnityEngine;` and nothing else, which
# is a check that cannot fail -- verified by injecting the exact missing
# reference AGENTS.md documents and watching it pass.
#
# `using static` and the alias form (`using Foo = Bar.Baz;`) are excluded:
# neither is a namespace import.
USING_RE = re.compile(
    r'^\s*(?:global\s+)?using\s+(?!static\s)(?![\w\s.]*=)([A-Za-z_][\w.]*)'
    r'\s*;', re.M)
NAMESPACE_RE = re.compile(r'^\s*namespace\s+([A-Za-z_][\w.]*)', re.M)


def uncomment(text):
    text = re.sub(r'/\*.*?\*/', ' ', text, flags=re.S)
    text = re.sub(r'//[^\n]*', ' ', text)
    return text


def project_assemblies():
    """{assembly name: (root dir, declared reference names)}"""
    out = {}
    for dirpath, dirnames, filenames in os.walk(os.path.join(PROJECT, 'Assets')):
        for n in filenames:
            if not n.endswith('.asmdef'):
                continue
            data = json.load(open(os.path.join(dirpath, n), encoding='utf-8'))
            name = data.get('name')
            if name:
                out[name] = (dirpath, set(data.get('references', [])))
    return out


def owner(root, asmdefs):
    """The assembly whose directory contains this file.

    The equality case matters: a .cs sitting directly in the asmdef's own
    directory is owned by it. Testing only `startswith(r + os.sep)` skipped
    every such file, and the check passed while doing nothing -- found by
    putting the probe in the one place it did not look.
    """
    best = None
    for name, (r, _refs) in asmdefs.items():
        if root == r or root.startswith(r + os.sep):
            if best is None or len(r) > len(asmdefs[best][0]):
                best = name
    return best


def main():
    asmdefs = project_assemblies()
    if not asmdefs:
        print('  asmdef-refs: no asmdefs found under Assets/', file=sys.stderr)
        return 1

    # namespace -> assemblies that declare it EXACTLY.
    #
    # Exactly, and not by parent. `using UnityEngine;` must not be attributed
    # to UnityUIExtensions just because it declares
    # `namespace UnityEngine.UI.Extensions` -- attributing parents made this
    # report 1173 findings, led by "uses namespace UnityEngine from
    # UnityUIExtensions", which is a checker that cries wolf and then gets
    # ignored. Exact matching still catches the real case, because the
    # assembly that fails to declare a reference always uses the full
    # namespace it needs.
    #
    # Loose scripts are not counted: only asmdef assemblies participate, so
    # the `namespace UnityEngine.UI` in Assembly-CSharp cannot shadow Unity's
    # own.
    ns_owners = {}
    for name, (root, _refs) in sorted(asmdefs.items()):
        for dirpath, dirnames, filenames in os.walk(root):
            for n in filenames:
                if not n.endswith('.cs'):
                    continue
                text = uncomment(open(os.path.join(dirpath, n),
                                      encoding='utf-8', errors='ignore').read())
                for m in NAMESPACE_RE.finditer(text):
                    ns_owners.setdefault(m.group(1), set()).add(name)

    findings = []
    files = 0
    for name, (root, refs) in sorted(asmdefs.items()):
        for dirpath, dirnames, filenames in os.walk(root):
            for fn in filenames:
                if not fn.endswith('.cs'):
                    continue
                path = os.path.join(dirpath, fn)
                if owner(dirpath, asmdefs) != name:
                    continue
                files += 1
                text = uncomment(open(path, encoding='utf-8', errors='ignore').read())
                for m in USING_RE.finditer(text):
                    ns = m.group(1)
                    owners = ns_owners.get(ns)
                    if not owners or len(owners) != 1:
                        continue
                    other = next(iter(owners))
                    if other == name or other in refs:
                        continue
                    findings.append((path, ns, name, other))

    for path, ns, src, needed in findings:
        print('%s: uses namespace %s from %s, which %s does not reference '
              '-- CS0234 in the editor' % (os.path.relpath(path, PROJECT),
                                           ns, needed, src))
    if findings:
        print('\n%d undeclared project reference(s) across %d .cs'
              % (len(findings), files))
        return 1
    print('  asmdef-refs: %d .cs, every project namespace used is declared'
          % files)
    return 0


if __name__ == '__main__':
    sys.exit(main())