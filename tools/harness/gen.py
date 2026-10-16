#!/usr/bin/env python3
"""Generate one .csproj per .asmdef, so the whole project can be compiled
outside the editor.

Two rules make this trustworthy, and both are load-bearing:

1.  ONE PROJECT PER ASMDEF, WITH THAT ASMDEF'S REAL REFERENCES.
    Compiling everything into everything cannot see the two error classes
    that have actually bitten: a script using a type from an assembly it
    does not reference, and a call that exists in a stub but not in the
    real engine. References are DIRECT only, because Unity's asmdef
    references are not transitive here.

2.  REFERENCE UNITY'S OWN DLLS, NOT A STUB AND NOT EACH OTHER.
    Every cross-assembly reference resolves to the DLL Unity itself built
    into Library/ScriptAssemblies, so there are no <ProjectReference>
    elements, no build ordering to get wrong, and no stub that can drift
    out of step with the editor.

    The cost of (2) is documented in README.md: a NEW type in assembly A
    cannot be used from assembly B until Unity has rebuilt
    Library/ScriptAssemblies, because that DLL is the editor's build, not
    ours. That shows up as CS0103 on a type you can see on disk. The
    editor rebuild clears it.

Run with --check to print what it would do and exit non-zero if the
generated set no longer matches the asmdefs on disk.
"""
import argparse
import glob
import json
import os
import re
import sys
import xml.etree.ElementTree as ET

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.abspath(os.path.join(HERE, '..', '..'))
BUILD = os.path.join(HERE, 'build')

# Unity's own managed DLLs. Referencing these rather than a stub is what
# makes a wrong API call a compile error instead of a runtime surprise.
UNITY_ENGINE = os.path.expanduser(
    '~/Unity/Hub/Editor/6000.6.4f1/Editor/Data/Managed/UnityEngine')
NETSTANDARD = os.path.expanduser(
    '~/Unity/Hub/Editor/6000.6.4f1/Editor/Data/NetStandard/ref/2.1.0/netstandard.dll')
SCRIPT_ASSEMBLIES = os.path.join(PROJECT, 'Library', 'ScriptAssemblies')

# The platform build-support modules. AGENTS.md: an Editor/ FOLDER inside
# an asmdef compiles into that asmdef's editor assembly, which lacks
# references to these, so `using UnityEditor.Android;` fails there with
# CS0246. Loose scripts under Assets/Editor go to
# Assembly-CSharp-Editor, which does have them.
PLATFORM_SUPPORT = [
    'UnityEditor.Android.Extensions.dll',
    'UnityEditor.Android.dll',
    'UnityEditor.Android.Extensions.Android.dll',
]

LANG_VERSION = '9.0'


def fail(msg):
    sys.stderr.write('gen.py: %s\n' % msg)
    sys.exit(1)


def find_asmdefs():
    out = []
    for path in sorted(glob.glob(os.path.join(PROJECT, 'Assets', '**', '*.asmdef'),
                              recursive=True)):
        try:
            data = json.load(open(path, encoding='utf-8'))
        except Exception as e:
            fail('%s: %s' % (path, e))
        out.append((path, data))
    return out


def asmdef_root(path):
    return os.path.dirname(path)


def sources_for(root, exclude_roots=()):
    """Every .cs under root, minus anything owned by a nested asmdef."""
    all_cs = glob.glob(os.path.join(root, '**', '*.cs'), recursive=True)
    keep = []
    for f in all_cs:
        if any(f.startswith(e + os.sep) for e in exclude_roots):
            continue
        keep.append(f)
    return sorted(keep)


def resolve_reference(name):
    """asmdef reference name -> a real DLL path, or None if unresolvable."""
    cand = os.path.join(SCRIPT_ASSEMBLIES, name + '.dll')
    if os.path.exists(cand):
        return cand
    hits = glob.glob(os.path.join(PROJECT, 'Assets', '**', name + '.dll'),
                    recursive=True)
    if hits:
        return hits[0]
    return None


# Where a bare DLL name from a Bee response file can live. ScriptAssemblies
# is Unity's own build of every project and package assembly, so most names
# land there; package caches and Assets/Plugins cover the rest.
_SEARCH_DIRS = [
    SCRIPT_ASSEMBLIES,
    os.path.join(PROJECT, 'Assets'),
    os.path.join(PROJECT, 'Library', 'PackageCache'),
    UNITY_ENGINE,
    os.path.dirname(UNITY_ENGINE),
]

_dll_index = None


def dll_index():
    """Bare DLL name -> path, built once per run."""
    global _dll_index
    if _dll_index is None:
        idx = {}
        for d in _SEARCH_DIRS:
            if not os.path.isdir(d):
                continue
            for p in glob.glob(os.path.join(d, '**', '*.dll'), recursive=True):
                idx.setdefault(os.path.basename(p), p)
        _dll_index = idx
    return _dll_index


def bee_rsp(assembly):
    """Unity's own response file for an assembly, if it has one.

    This is the authoritative reference list. Deriving it from the asmdef
    JSON instead means modelling Unity's precompiled-assembly rules by hand,
    and getting that wrong is invisible: an assembly that uses
    UnityEngine.UI without listing it compiles here and fails in the editor.

    There are several response files per assembly, one per build graph, and
    they are NOT interchangeable: the Android player ones carry
    UNITY_ANDROID and PLATFORM_ANDROID and no UNITY_EDITOR, so compiling with
    them includes #if UNITY_ANDROID blocks the editor never compiles and
    reports errors the editor does not have. Always take the editor's.
    """
    hits = sorted(glob.glob(os.path.join(PROJECT, 'Library', 'Bee', 'artifacts',
                                         '*', assembly + '.rsp')))
    if not hits:
        return None
    for h in hits:
        try:
            head = open(h, encoding='utf-8', errors='ignore').read(65536)
        except OSError:
            continue
        if 'UNITY_EDITOR' in head:
            return h
    return hits[0]


def unity_refs_for(assembly, own_name):
    """(dll paths, define constants) exactly as Unity passes them.

    Unity passes its own .ref.dll reference assemblies, not the built
    DLLs. Resolving by basename therefore misses nearly everything, and the
    miss is SILENT: a dropped reference simply becomes a CS0246 or CS0234
    on a type the editor compiles fine. So a `.ref.dll` is resolved on its
    stem, against the .ref.dll sitting beside the response file and then
    against the real DLL.
    """
    rsp = bee_rsp(assembly)
    if not rsp:
        return None, None

    index = dict(dll_index())
    dag = os.path.dirname(rsp)
    for p in glob.glob(os.path.join(dag, '*.ref.dll')):
        index.setdefault(os.path.basename(p)[:-len('.ref.dll')], p)

    def resolve(name):
        # Prefer the REAL built DLL over Unity's .ref.dll reference assembly.
        # The .ref.dll files are keyed by STEM (no .dll suffix), so checking
        # the stem first let every one of them shadow the ScriptAssemblies
        # copy. Note: that ordering was NOT the cause of the CS0234 chased
        # here -- with either reference in place the error persisted, and the
        # real cause was a wrong `using` namespace in the source. Preferring
        # the built DLL is still what this harness wants (a reference
        # assembly can differ from what the editor actually compiles against),
        # but it did not fix that error and should not be credited with it.
        stem = name[:-len('.ref.dll')] if name.endswith('.ref.dll') else name
        return index.get(stem + '.dll') or index.get(stem)

    dlls, defines, missing = [], [], []
    for line in open(rsp, encoding='utf-8', errors='ignore'):
        line = line.strip()
        if line.startswith('-r:'):
            v = line[3:].strip().strip('"')
            name = os.path.basename(v)
            if not name or name == own_name + '.dll':
                continue
            hit = resolve(name)
            if hit:
                dlls.append(hit)
            else:
                missing.append(name)
        elif line.startswith('-define:'):
            for d in line[8:].split(';'):
                if d.strip():
                    defines.append(d.strip())
    for m in sorted(set(missing)):
        if m.endswith('.ref.dll'):
            sys.stderr.write('gen.py: WARNING unresolvable reference: %s\n' % m)
    return dedupe(dlls), defines


def unity_refs():
    refs = []
    for p in sorted(glob.glob(os.path.join(UNITY_ENGINE, '*.dll'))):
        refs.append(p)
    if os.path.exists(NETSTANDARD):
        refs.append(NETSTANDARD)
    return refs


def platform_refs():
    out = []
    for d in (UNITY_ENGINE, os.path.dirname(UNITY_ENGINE)):
        for n in PLATFORM_SUPPORT:
            p = os.path.join(d, n)
            if os.path.exists(p):
                out.append(p)
    # also look in the editor root
    for n in PLATFORM_SUPPORT:
        for p in glob.glob(os.path.expanduser(
                '~/Unity/Hub/Editor/6000.6.4f1/Editor/Data/Managed/**/' + n),
                recursive=True):
            out.append(p)
    seen, uniq = set(), []
    for p in out:
        if p not in seen:
            seen.add(p)
            uniq.append(p)
    return uniq


AMBIENT = (
    'UnityEngine.UI.dll',
    'Unity.TextMeshPro.dll',
    'Unity.InputSystem.dll',
    'Unity.RenderPipelines.Core.Runtime.dll',
    'Unity.RenderPipelines.Universal.Runtime.dll',
    'Unity.RenderPipelines.Core.ShaderLibrary.dll',
    'Unity.Mathematics.dll',
    'Unity.Collections.dll',
    'Unity.Burst.dll',
)


def ambient_refs():
    """The precompiled assemblies Unity makes available to every assembly.

    Deliberately generous. A csproj that is too broad costs nothing but a
    few milliseconds; a csproj that is too narrow fails to compile code the
    editor compiles fine. The strict check that a script uses a type from an
    assembly it does not DECLARE is asmdef-refs.py's job, not the
    csproj's -- and that split is the point.
    """
    idx = dll_index()
    out = []
    for n in AMBIENT:
        p = idx.get(n)
        if p:
            out.append(p)
    return out


def dedupe(paths):
    seen, out = set(), []
    for p in paths:
        if p and p not in seen:
            seen.add(p)
            out.append(p)
    return out


def write_csproj(path, name, sources, refs, defines=()):
    ns = 'http://schemas.microsoft.com/developer/msbuild/2003'
    ET.register_namespace('', ns)
    proj = ET.Element('Project', {'ToolsVersion': '4.0',
                                  'Sdk': 'Microsoft.NET.Sdk'})
    prop = ET.SubElement(proj, 'PropertyGroup')
    for k, v in (
            ('Configuration', 'Debug'),
            ('Platform', 'AnyCPU'),
            ('OutputType', 'Library'),
            ('TargetFramework', 'netstandard2.1'),
            ('LangVersion', LANG_VERSION),
            ('AllowUnsafeBlocks', 'true'),
            ('Nullable', 'disable'),
            ('EnableDefaultCompileItems', 'false'),
            ('GenerateAssemblyInfo', 'false'),
            ('Deterministic', 'true'),
            ('AssemblyName', name),
            ('RootNamespace', name),
            ('EnableSourceLink', 'false'),
            ('DefineConstants', ';'.join(defines)),
            ('NoWarn', '0169;0414;0649;0067;0108;0114;0162;0219;1591;0672'),
    ):
        ET.SubElement(prop, k).text = v

    items = ET.SubElement(proj, 'ItemGroup')
    for s in sources:
        ET.SubElement(items, 'Compile', {'Include': os.path.relpath(s, os.path.dirname(path))})

    rgrp = ET.SubElement(proj, 'ItemGroup')
    for r in refs:
        e = ET.SubElement(rgrp, 'Reference', {'Include': os.path.basename(r)})
        ET.SubElement(e, 'HintPath').text = os.path.relpath(r, os.path.dirname(path))
        ET.SubElement(e, 'Private').text = 'false'

    ET.indent(proj, space='  ')
    os.makedirs(os.path.dirname(path), exist_ok=True)
    ET.ElementTree(proj).write(path, encoding='utf-8', xml_declaration=True)


def generate():
    asmdefs = find_asmdefs()
    if not asmdefs:
        fail('no .asmdef found under Assets/ -- gen.py paths are wrong')

    roots = [asmdef_root(p) for p, _ in asmdefs]
    unresolved = set()
    projects = []

    for path, data in asmdefs:
        name = data.get('name') or fail('%s has no name' % path)
        root = asmdef_root(path)
        srcs = sources_for(root, [r for r in roots if r != root])
        bee, defines = unity_refs_for(name, name)
        # Unity's rsp is the best record of what this assembly actually needs,
        # but it can be older than the source. Union it with the ambient set
        # and the asmdef's own declarations so a stale rsp cannot hide a
        # reference the editor has and we do not.
        refs = list(bee or []) + ambient_refs() + unity_refs()
        for r in data.get('references', []):
            hit = resolve_reference(r)
            if hit:
                refs.append(hit)
            else:
                unresolved.add(r)
        refs = dedupe(refs)
        safe = re.sub(r'[^A-Za-z0-9._-]', '_', name)
        out = os.path.join(BUILD, safe, safe + '.csproj')
        write_csproj(out, name, srcs, refs, defines)
        projects.append((name, out, len(srcs)))

    # Loose scripts: anything under Assets that no asmdef claims. Unity
    # compiles these into Assembly-CSharp, which sees every assembly. Without
    # this project they are in no csproj at all, and check.py's stale-file
    # guard fires -- which is exactly how that guard earned its keep.
    asmdef_cs = set()
    for r in roots:
        for f in glob.glob(os.path.join(r, '**', '*.cs'), recursive=True):
            asmdef_cs.add(os.path.normpath(f))
    loose = [f for f in glob.glob(os.path.join(PROJECT, 'Assets', '**', '*.cs'),
                                  recursive=True)
             if os.path.normpath(f) not in asmdef_cs
             and not f.startswith(os.path.join(PROJECT, 'Assets', 'Editor') + os.sep)]
    if loose:
        bee, defines = unity_refs_for('Assembly-CSharp', 'Assembly-CSharp')
        refs = dedupe((bee or []) + ambient_refs() + unity_refs() + platform_refs())
        # Assembly-CSharp is predefined: Unity references EVERY auto-referenced
        # assembly into it, not a hand-picked list.
        for path, data in asmdefs:
            if data.get('autoReferenced', True):
                hit = resolve_reference(data.get('name', ''))
                if hit:
                    refs.append(hit)
        refs = dedupe(refs)
        out = os.path.join(BUILD, 'Assembly-CSharp', 'Assembly-CSharp.csproj')
        write_csproj(out, 'Assembly-CSharp', sorted(loose), refs, defines or ())
        projects.append(('Assembly-CSharp', out, len(loose)))

    # Assets/Editor is NOT an asmdef. Unity compiles loose scripts there into
    # Assembly-CSharp-Editor, which references every game assembly plus the
    # platform build-support modules.
    editor_dir = os.path.join(PROJECT, 'Assets', 'Editor')
    if os.path.isdir(editor_dir):
        srcs = sources_for(editor_dir)
        bee, defines = unity_refs_for('Assembly-CSharp-Editor',
                                      'Assembly-CSharp-Editor')
        refs = dedupe((bee or []) + ambient_refs() + unity_refs() + platform_refs())
        # Assembly-CSharp-Editor also sees every game assembly, plus the
        # platform build-support modules -- which is why a platform build
        # callback must live in Assets/Editor and not in an asmdef Editor/
        # subfolder. An asmdef's generated editor assembly lacks these and
        # `using UnityEditor.Android;` fails there with CS0246.
        for path, data in asmdefs:
            if data.get('autoReferenced', True):
                hit = resolve_reference(data.get('name', ''))
                if hit:
                    refs.append(hit)
        refs = dedupe(refs)
        out = os.path.join(BUILD, 'Assembly-CSharp-Editor',
                           'Assembly-CSharp-Editor.csproj')
        write_csproj(out, 'Assembly-CSharp-Editor', srcs, refs, defines or ())
        projects.append(('Assembly-CSharp-Editor', out, len(srcs)))

    for r in sorted(unresolved):
        sys.stderr.write('gen.py: WARNING unresolved asmdef reference: %s\n' % r)

    return projects


def check():
    """A green run that compiled nothing is this harness's signature failure,
    so these two counts are asserted rather than assumed."""
    asmdefs = find_asmdefs()
    roots = [asmdef_root(p) for p, _ in asmdefs]
    asmdef_cs = set()
    for r in roots:
        for f in glob.glob(os.path.join(r, '**', '*.cs'), recursive=True):
            asmdef_cs.add(os.path.normpath(f))
    loose = [f for f in glob.glob(os.path.join(PROJECT, 'Assets', '**', '*.cs'),
                                  recursive=True)
             if os.path.normpath(f) not in asmdef_cs
             and not f.startswith(os.path.join(PROJECT, 'Assets', 'Editor') + os.sep)]
    on_disk = len(asmdefs)
    on_disk += 1 if loose else 0
    on_disk += 1 if os.path.isdir(os.path.join(PROJECT, 'Assets', 'Editor')) else 0
    generated = len(glob.glob(os.path.join(BUILD, '*', '*.csproj')))

    if generated == 0:
        fail('no csproj generated -- gen.py paths are wrong, and a green '
             'run would mean nothing compiled')
    if generated != on_disk:
        fail('%d csproj generated but %d asmdefs (+Editor) on disk -- '
             'refusing to report success' % (generated, on_disk))

    # every .cs under Assets must be in SOME project
    covered = set()
    for p in glob.glob(os.path.join(BUILD, '*', '*.csproj')):
        tree = ET.parse(p)
        for c in tree.iter():
            if c.tag.endswith('Compile') and 'Include' in c.attrib:
                covered.add(os.path.normpath(
                    os.path.join(os.path.dirname(p), c.attrib['Include'])))
    orphans = []
    for f in glob.glob(os.path.join(PROJECT, 'Assets', '**', '*.cs'), recursive=True):
        if os.path.normpath(f) not in covered:
            orphans.append(f)
    if orphans:
        for o in orphans[:20]:
            sys.stderr.write('gen.py: STALE -- never compiled by any project: %s\n' % o)
        sys.stderr.write('gen.py: %d .cs file(s) in no csproj\n' % len(orphans))
        sys.exit(2)

    print('  %d projects, %d .cs files all covered' % (generated, len(covered)))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--check', action='store_true',
                    help='verify the generated set matches the asmdefs on disk')
    args = ap.parse_args()

    if args.check:
        check()
        return

    projects = generate()
    print('  generated %d csproj:' % len(projects))
    for name, out, n in sorted(projects):
        print('    %-46s %4d .cs' % (name, n))
    check()


if __name__ == '__main__':
    main()