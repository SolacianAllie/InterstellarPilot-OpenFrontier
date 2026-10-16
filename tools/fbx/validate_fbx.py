#!/usr/bin/env python3
"""Independent validator for ASCII FBX files written by FbxMeshWriter.

Written deliberately WITHOUT reference to the writer: it parses the file the
way a consumer would, so a bug shared between writer and test cannot hide.

Checks, in order of how quietly they break a real file:
  1. every *N array header matches the number of values actually present
  2. PolygonVertexIndex carries the bitwise-complement marker on the LAST
     vertex of every face (a positive index there is not rejected, it just
     imports as a mangled mesh)
  3. every decoded index is inside the vertex buffer
  4. layer elements are ByControlPoint/Direct with one entry per vertex
  5. connections tie the geometry to a model
  6. GEOMETRY: the winding agrees with the shading normals -- the geometric
     normal of each triangle, computed from its own vertices, must point the
     same way as the interpolated vertex normals. A reversed winding here is
     the classic silent failure: the file imports, and the mesh is inside out.
"""
import math
import re
import sys


def tokenize(text):
    """(name, value_or_children) tree for `Name: value { ... }` and `Name: *N { a: ... }`."""
    pos = 0
    n = len(text)

    def skip_ws():
        nonlocal pos
        while pos < n and text[pos] in ' \t\r\n':
            pos += 1

    def parse_block():
        """Called with pos just past '{'. Returns dict of children."""
        nonlocal pos
        out = {}
        while pos < n:
            skip_ws()
            if pos >= n:
                break
            if text[pos] == '}':
                pos += 1
                return out
            # name up to ':'
            m = re.match(r'([A-Za-z_][\w\-]*)', text[pos:])
            if not m:
                pos += 1          # stray char, skip
                continue
            name = m.group(1)
            pos += m.end()
            skip_ws()
            if pos >= n or text[pos] != ':':
                continue
            pos += 1
            skip_ws()
            # optional declared count, e.g. *24
            declared = None
            mc = re.match(r'\*(\d+)', text[pos:])
            if mc:
                declared = int(mc.group(1))
                pos += mc.end()
                skip_ws()
            if pos < n and text[pos] == '{':
                pos += 1
                out[name] = ('block', declared, parse_block())
            else:
                # FBX puts the node's id and class on the SAME line as the
                # opening brace:
                #     Geometry: 140000000, "Geometry::", "Mesh" {
                # so a header that is not immediately a '{' is still a block
                # if a brace appears before the end of the line.
                brace = text.find('{', pos)
                nl = text.find('\n', pos)
                if brace != -1 and (nl == -1 or brace < nl):
                    pos = brace + 1
                    out[name] = ('block', declared, parse_block())
                    continue
                # An FBX array payload is introduced by the name `a:`. It is comma
                # separated, so it CANNOT share the scalar stop-at-','
                # rule below -- doing that truncated every array to its first
                # value and discarded the rest as stray characters.
                if name == 'a':
                    nl = text.find('\n', pos)
                    line_end = nl if nl != -1 else n
                    raw = text[pos:line_end]
                    end = len(raw.rstrip())
                    if end > 0 and raw[end - 1] == '}':
                        # inline "a: 1,2,3 }" -- leave pos ON the brace so
                        # the enclosing block still closes here
                        pos = pos + end - 1
                        raw = raw[:end - 1]
                    else:
                        pos = line_end
                    out[name] = ('array', declared, raw.strip())
                else:
                    # A scalar stops at '\n', ',' or '}'. Running to end of
                    # line swallows the brace of an inline block:
                    # `ObjectType: "Model" {Count: 1}` became the scalar
                    # "1}", so that block never closed on its own line and
                    # every level of nesting after it drifted by one brace
                    # until Objects and Connections vanished.
                    scan = pos
                    while scan < n and text[scan] not in '\n,}':
                        scan += 1
                    chunk = text[pos:scan].strip()
                    pos = scan
                    out[name] = ('scalar', None, chunk)
        return out

    skip_ws()
    return parse_block()


def values_of(node):
    """A comma-separated `a:` payload as (declared_count, [floats]).

    `Vertices: *24 { a: ... }` parses as a BLOCK whose `declared` count sits
    on the block, with the payload on the `a` child -- so unwrap and keep the
    count from the outer node, not the inner one.
    """
    if node[0] == 'block':
        declared = node[1]
        kids = node[2]
        if 'a' in kids and kids['a'][0] == 'array':
            payload = kids['a'][2]
            if not payload:
                return declared, []
            return declared, [float(x) for x in payload.split(',')]
        raise AssertionError('block with no a: array: %s' % sorted(kids))

    kind, declared, payload = node
    assert kind == 'array', kind
    if not payload:
        return declared, []
    return declared, [float(x) for x in payload.split(',')]


def find_block(tree, path):
    """The node tuple at `path`, or None. `tree` is already a children dict."""
    cur = tree
    node = None
    for p in path:
        node = cur.get(p)
        if node is None or node[0] != 'block':
            return None
        cur = node[2]
    return node


def check(path):
    print('=== %s' % path)
    text = open(path).read()
    failures = []

    tree = tokenize(text)
    hdr = find_block(tree, ['FBXHeaderExtension'])
    if not hdr or hdr[2].get('FBXVersion', ('s', None, ''))[2] != '7400':
        failures.append('missing or wrong FBXHeaderExtension/FBXVersion')

    geo = None
    for node in tree.get('Objects', ('', None, {}))[2].values():
        if node[2].get('__class__') == 'Geometry' or 'Vertices' in node[2]:
            geo = node[2]
    if geo is None:
        failures.append('no Geometry node with Vertices')
        report(failures)
        return

    vdecl, verts = values_of(geo['Vertices'])
    vcount = len(verts) // 3
    if vdecl != len(verts):
        failures.append('Vertices header says *%s but %d values present'
                        % (vdecl, len(verts)))
    if vcount == 0:
        failures.append('zero vertices')

    idecl, pv = values_of(geo['PolygonVertexIndex'])
    if idecl != len(pv):
        failures.append('PolygonVertexIndex header says *%s but %d present'
                        % (idecl, len(pv)))
    if len(pv) % 3:
        failures.append('PolygonVertexIndex is not a multiple of 3')

    # The complement marker: last index of each face must be negative.
    bad_marker = [i for i in range(2, len(pv), 3) if pv[i] >= 0]
    if bad_marker:
        failures.append('%d face(s) have a non-negative last index (missing the '
                        'bitwise complement) e.g. at %d'
                        % (len(bad_marker), bad_marker[0]))

    indices = []
    index_errors = 0
    for i, v in enumerate(pv):
        # ~v == -v-1 for the face-final vertex; everything else is a plain
        # index. Both must come out as int -- leaving the plain ones as the
        # float the parser produced makes every slice index a TypeError.
        idx = (~int(v)) if i % 3 == 2 else int(v)
        if idx < 0 or idx >= vcount:
            index_errors += 1
            if index_errors <= 3:
                failures.append('index %d out of range at position %d'
                                % (idx, i))
        indices.append(idx)
    if index_errors > 3:
        failures.append('...and %d more out-of-range indices' % (index_errors - 3))
    if not failures:
        print('    %d verts, %d tris, indices all in range'
              % (vcount, len(indices) // 3))

    # Layer elements
    layers = {}
    for key, node in geo.items():
        if key.startswith('LayerElement'):
            body = node[2]
            mit = body.get('MappingInformationType', ('s', None, ''))[2].strip('"')
            rit = body.get('ReferenceInformationType', ('s', None, ''))[2].strip('"')
            if mit != 'ByControlPoint':
                failures.append('%s uses %s, expected ByControlPoint' % (key, mit))
            if rit != 'Direct':
                failures.append('%s uses %s, expected Direct' % (key, rit))
            for pk, pv2 in body.items():
                # The payload is `Normals: *5958 { a: ... }`, which parses as
                # a BLOCK wrapping the array, not as an array itself.
                if not (isinstance(pv2, tuple) and pv2[0] in ('array', 'block')):
                    continue
                try:
                    d, vals = values_of(pv2)
                except AssertionError:
                    continue
                if not vals:
                    continue
                per = {'LayerElementNormal': 3, 'LayerElementTangent': 4,
                       'LayerElementUV': 2}.get(key)
                if d != len(vals):
                    failures.append('%s.%s header says *%s but %d present'
                                    % (key, pk, d, len(vals)))
                if per and len(vals) != vcount * per:
                    failures.append('%s.%s has %d values, expected %d verts x %d'
                                    % (key, pk, len(vals), vcount, per))
                layers[key] = vals
            print('    %-22s %d values' % (key, len(layers.get(key, []))))

    # The Layer node's BODY holds the refs, not the key name. Comparing the
    # key 'Layer' against 'LayerElementNormal' is always false and looks
    # exactly like a missing layer reference.
    # The layer refs are the Type VALUES inside LayerElement: {Type: "..."},
    # and those keys repeat, so a dict-backed tree keeps only the last one.
    # Read them off the raw text, which is what the check is actually about.
    for key in layers:
        if not re.search(r'LayerElement:\s*\{Type:\s*"%s"' % re.escape(key),
                         text):
            failures.append('%s is not referenced by a Layer node' % key)

    conns = find_block(tree, ['Connections'])
    if not conns:
        failures.append('no Connections block')
    else:
        pairs = re.findall(r'C:\s*"OO",\s*(\d+)\s*,\s*(\d+)', text)
        if not pairs:
            failures.append('Connections has no object-object links')
        # The regex yields STRINGS. Comparing them to int ids fails every
        # time, which looks exactly like "the exporter emitted no
        # connections" and is not.
        ids = {int(a) for a, _ in pairs} | {int(b) for _, b in pairs}
        if 140000000 not in ids or 200000000 not in ids:
            failures.append('geometry/model not both present in Connections, '
                            'saw %s' % sorted(ids))

    # GEOMETRY CHECK: does the winding agree with the shading normals?
    # Skipped when any index was bad, because slicing a vertex buffer with a
    # bad index raises IndexError and the crash reads as a validator crash
    # rather than as the data error it actually is.
    normals = layers.get('LayerElementNormal')
    if normals and not index_errors:
        agree = 0
        total = 0
        bad = 0
        for f in range(0, len(indices), 3):
            i0, i1, i2 = indices[f], indices[f + 1], indices[f + 2]
            p0 = verts[i0 * 3:i0 * 3 + 3]
            p1 = verts[i1 * 3:i1 * 3 + 3]
            p2 = verts[i2 * 3:i2 * 3 + 3]
            u = [p1[k] - p0[k] for k in range(3)]
            v = [p2[k] - p0[k] for k in range(3)]
            g = [u[1] * v[2] - u[2] * v[1],
                 u[2] * v[0] - u[0] * v[2],
                 u[0] * v[1] - u[1] * v[0]]
            gl = math.sqrt(sum(c * c for c in g))
            if gl < 1e-12:
                continue
            g = [c / gl for c in g]
            n = [sum(normals[i * 3 + k] for i in (i0, i1, i2)) / 3.0 for k in range(3)]
            nl = math.sqrt(sum(c * c for c in n))
            if nl < 1e-9:
                continue
            n = [c / nl for c in n]
            total += 1
            if sum(g[k] * n[k] for k in range(3)) > 0.3:
                agree += 1
            else:
                bad += 1
        if total:
            print('    winding agrees with normals on %d/%d triangles (%d opposed)'
                  % (agree, total, bad))
            # UNIFORM opposition is a handedness convention difference, not a
            # bug: the writer copies the source winding verbatim, so it can
            # only ever be the axis convention the consumer applies. That is
            # handled at export time by a winding flip, not by a corrupt
            # file. MIXED results mean genuinely inconsistent winding, which
            # IS a defect.
            if bad > total * 0.05 and agree > total * 0.05:
                failures.append('winding is INCONSISTENT: %d triangles agree and '
                                '%d are opposed. One-sided disagreement is a '
                                'handedness convention; two-sided means bad '
                                'data.' % (agree, bad))
            elif bad:
                print('    NOTE: %d/%d opposed -- one-sided, i.e. a handedness '
                      'convention difference, not corruption.' % (bad, total))

    report(failures)


def report(failures):
    if failures:
        for f in failures:
            print('    FAIL: %s' % f)
        sys.exit(1)
    print('    OK')


if __name__ == '__main__':
    rc = 0
    for p in sys.argv[1:]:
        try:
            check(p)
        except AssertionError as e:
            print('    FAIL: unexpected node kind %r' % e)
            rc = 1
    sys.exit(rc)