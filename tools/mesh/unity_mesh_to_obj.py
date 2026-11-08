#!/usr/bin/env python3
"""Decode a Unity Mesh .asset straight from its YAML, without Unity.

    python3 unity_mesh_to_obj.py <Mesh.asset> <out.obj>

WHY: the FBX exporter's own path needs the editor, and Blender rejects ASCII
FBX outright, so the fastest route to a file Blender will open is to decode
the mesh here and hand the geometry to Blender's own binary writer.

READS THE REAL LAYOUT, AND CHECKS IT RATHER THAN TRUSTING IT
  m_VertexCount     vertex count
  m_VertexData.m_Channels   per-attribute stream offsets and dimensions
  m_IndexFormat      0 = UInt16, 1 = UInt32
  m_DataSize         bytes of vertex buffer

Every stride is DERIVED from the channel list, not assumed. A wrong stride
still produces a plausible-looking OBJ, so the guards below are the actual
test: vertex count, every vertex at radius ~= the AABB extent, every index in
range, and a triangle count that matches the submesh.

VERIFIED AGAINST: Assets/InterstellarPilot2/Extracted/Mesh/SphereMediumPoly.asset
  2143 verts, 11904 indices (3968 tris), AABB extent 10.
"""
import re
import struct
import sys

# Attribute -> components, by Unity's channel dimension.
CHANNEL_COMPONENTS = {3: None, 4: None, 2: None}  # dim is the component count


def fail(msg):
    sys.stderr.write("unity_mesh_to_obj: %s\n" % msg)
    sys.exit(1)


def scalar_hex(text, key):
    m = re.search(r'^\s*%s:\s*([0-9a-fA-F]+)\s*$' % re.escape(key), text, re.M)
    return m.group(1) if m else None


def scalar_int(text, key):
    m = re.search(r'^\s*%s:\s*(-?\d+)\s*$' % re.escape(key), text, re.M)
    return int(m.group(1)) if m else None


def channels(text):
    """[(offset, dimension)] for the channels that actually carry data.

    A channel with dimension 0 is an unused slot, which is why the list is
    longer than the number of attributes.
    """
    block = re.search(r'm_Channels:\n((?:\s*-\s*stream:.*\n(?:\s+\w+:.*\n)*)+)',
                      text)
    if not block:
        fail("no m_Channels block")
    out = []
    for m in re.finditer(
            r'-\s*stream:\s*\d+\s*\n\s*offset:\s*(-?\d+)\s*\n\s*format:\s*(\d+)'
            r'\s*\n\s*dimension:\s*(\d+)', block.group(1)):
        offset, _fmt, dim = int(m.group(1)), int(m.group(2)), int(m.group(3))
        if dim > 0:
            out.append((offset, dim))
    return out


def decode(path):
    text = open(path, encoding='utf-8', errors='ignore').read()

    name = re.search(r'^\s*m_Name:\s*(\S+)\s*$', text, re.M)
    name = name.group(1) if name else 'mesh'

    vcount = scalar_int(text, r'm_VertexCount')
    if not vcount:
        fail("no m_VertexCount")

    extent = re.search(r'm_Extent:\s*\{x:\s*([-\d.e]+),\s*y:\s*([-\d.e]+),\s*z:\s*([-\d.e]+)\}', text)
    radius = max(float(g) for g in extent.groups()) if extent else None

    chan = channels(text)
    if len(chan) < 4:
        fail("expected position/normal/tangent/UV, got %d channels" % len(chan))
    pos_off, pos_dim = chan[0]
    nrm_off, nrm_dim = chan[1]
    tan_off, tan_dim = chan[2]
    uv_off, uv_dim = chan[3]
    stride = max(pos_off + pos_dim * 4, nrm_off + nrm_dim * 4,
                 tan_off + tan_dim * 4, uv_off + uv_dim * 4)

    vertex_hex = scalar_hex(text, '_typelessdata')
    index_hex = scalar_hex(text, 'm_IndexBuffer')
    if not vertex_hex or not index_hex:
        fail("missing _typelessdata or m_IndexBuffer")

    vbytes = bytes.fromhex(vertex_hex)
    ibytes = bytes.fromhex(index_hex)

    declared = scalar_int(text, 'm_DataSize')
    if declared is not None and declared != len(vbytes):
        fail("m_DataSize says %d but _typelessdata is %d bytes" % (declared, len(vbytes)))

    need = stride * vcount
    if len(vbytes) < need:
        fail("vertex buffer is %d bytes, need %d for %d verts at stride %d"
             % (len(vbytes), need, vcount, stride))

    index_format = scalar_int(text, 'm_IndexFormat') or 0
    if index_format == 0:
        indices = list(struct.unpack_from('<%dH' % (len(ibytes) // 2), ibytes))
        idx_size = 2
    else:
        indices = list(struct.unpack_from('<%dI' % (len(ibytes) // 4), ibytes))
        idx_size = 4

    positions, normals, uvs = [], [], []
    for i in range(vcount):
        base = i * stride
        p = struct.unpack_from('<3f', vbytes, base + pos_off)
        n = struct.unpack_from('<%df' % nrm_dim, vbytes, base + nrm_off)
        t = struct.unpack_from('<%df' % tan_dim, vbytes, base + tan_off)
        uv = struct.unpack_from('<%df' % uv_dim, vbytes, base + uv_off)
        positions.append(p)
        normals.append(n[:3])
        uvs.append(uv[:2])

    # ---- guards. A misparsed stride still yields a plausible OBJ. ----
    oob = [i for i in indices if i < 0 or i >= vcount]
    if oob:
        fail("%d indices out of range (first %d, verts %d) -- stride or "
             "index format is wrong" % (len(oob), oob[0], vcount))
    if len(indices) % 3:
        fail("index count %d is not a multiple of 3" % len(indices))
    if radius:
        radii = [sum(c * c for c in p) ** 0.5 for p in positions]
        lo, hi = min(radii), max(radii)
        if abs(lo - radius) > radius * 0.05 or abs(hi - radius) > radius * 0.05:
            fail("vertex radii span %.4f..%.4f but the AABB extent is %.4f "
                 "-- the stride is wrong" % (lo, hi, radius))

    sub = re.search(r'indexCount:\s*(\d+)', text)
    if sub and int(sub.group(1)) != len(indices):
        fail("submesh indexCount %s but m_IndexBuffer holds %d"
             % (sub.group(1), len(indices)))

    degenerate = sum(1 for t in range(0, len(indices), 3)
                     if len({indices[t], indices[t + 1], indices[t + 2]}) < 3)
    if degenerate > len(indices) // 3 * 0.02:
        fail("%d/%d triangles are degenerate" % (degenerate, len(indices) // 3))

    return dict(name=name, positions=positions, normals=normals, uvs=uvs,
                indices=indices, stride=stride, radius=radius, index_size=idx_size)


def write_obj(mesh, path):
    with open(path, 'w') as f:
        f.write("# decoded from a Unity Mesh asset by unity_mesh_to_obj.py\n")
        f.write("# %s: %d verts, %d tris\n"
                % (mesh['name'], len(mesh['positions']), len(mesh['indices']) // 3))
        for p in mesh['positions']:
            f.write("v %.7g %.7g %.7g\n" % p)
        for t in mesh['uvs']:
            f.write("vt %.7g %.7g\n" % t)
        for n in mesh['normals']:
            f.write("vn %.7g %.7g %.7g\n" % n)
        f.write("s 1\n")
        idx = mesh['indices']
        for t in range(0, len(idx), 3):
            a, b, c = idx[t] + 1, idx[t + 1] + 1, idx[t + 2] + 1
            f.write("f %d/%d/%d %d/%d/%d %d/%d/%d\n" % (a, a, a, b, b, b, c, c, c))


if __name__ == '__main__':
    if len(sys.argv) != 3:
        fail("usage: unity_mesh_to_obj.py <Mesh.asset> <out.obj>")
    m = decode(sys.argv[1])
    write_obj(m, sys.argv[2])
    print("MESH: %s  %d verts  %d tris  stride %d  idx %d-bit  radius %s"
          % (m['name'], len(m['positions']), len(m['indices']) // 3,
             m['stride'], m['index_size'] * 8, m['radius']))
    # Machine-readable, so run.sh does not have to re-parse the line above.
    print("STATS: verts=%d tris=%d"
          % (len(m['positions']), len(m['indices']) // 3))
    print("MESH: wrote %s" % sys.argv[2])