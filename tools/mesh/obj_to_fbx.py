"""OBJ -> binary FBX via Blender's own writer, then verify the result.

Runs INSIDE Blender:

    blender --background --python obj_to_fbx.py -- \
        <in.obj> <out.fbx> <expected_verts> <expected_tris>

WHY BLENDER DOES THE WRITING: Blender 5.x refuses ASCII FBX outright --
"ASCII FBX files are not supported" -- so nothing that writes FBX 7.4 as text
can produce a file it will open. Writing binary FBX by hand is a much larger
job than ASCII, and Blender's exporter is a known-good implementation of it
that is already on disk.

VERIFIED, NOT ASSUMED. "Exported successfully" is not "Blender can load what
it just wrote", so the file is read back and checked:

  * it is binary (magic "Kaydara FBX Binary")
  * it re-imports, with the expected vertex and polygon counts
  * SIGNED VOLUME IS POSITIVE, i.e. the faces wind outward. Unity is
    left-handed and Blender right-handed, so inside-out is a genuine risk
    and this is what settles it instead of eyeballing a viewport. A sphere
    of radius 10 encloses 4/3.pi.r^3 = 4188.8.
  * shading normals agree with the winding on essentially every triangle
"""
import os
import sys

import bpy

argv = sys.argv[sys.argv.index("--") + 1:]
if len(argv) < 4:
    print("OBJ2FBX: FAIL -- need <obj> <fbx> <verts> <tris>")
    sys.exit(2)

obj_path, fbx_path = argv[0], argv[1]
want_verts, want_tris = int(argv[2]), int(argv[3])


def fail(msg):
    print("OBJ2FBX: FAIL -- %s" % msg)
    sys.exit(1)


bpy.ops.wm.read_factory_settings(use_empty=True)

# Blender 4+ moved the OBJ importer; import_scene.obj raises
# "could not be found" on 5.x.
bpy.ops.wm.obj_import(filepath=obj_path)

meshes = [o for o in bpy.data.objects if o.type == 'MESH']
if not meshes:
    fail("the OBJ produced no mesh")

bpy.ops.export_scene.fbx(filepath=fbx_path, use_selection=False,
                         apply_unit_scale=False,
                         bake_space_transform=False,
                         object_types={'MESH'}, use_mesh_modifiers=False)

if not open(fbx_path, 'rb').read(23).startswith(b'Kaydara FBX Binary'):
    fail("the FBX is not binary -- Blender 5.x will refuse it")

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=fbx_path)
back = [o for o in bpy.data.objects if o.type == 'MESH']
if not back:
    fail("wrote the FBX but cannot re-import it")

verts = sum(len(o.data.vertices) for o in back)
polys = sum(len(o.data.polygons) for o in back)
if verts != want_verts:
    fail("re-imported %d verts, expected %d" % (verts, want_verts))
if polys != want_tris:
    fail("re-imported %d polys, expected %d" % (polys, want_tris))

from mathutils import Vector

me = back[0].data
me.calc_loop_triangles()
volume = 0.0
agree = total = 0
for t in me.loop_triangles:
    a, b, c = (me.vertices[i].co for i in t.vertices)
    volume += a.dot(b.cross(c)) / 6.0
    g = (b - a).cross(c - a)
    if g.length < 1e-9:
        continue
    n = sum((me.vertices[i].normal for i in t.vertices), Vector()) / 3.0
    if n.length < 1e-9:
        continue
    total += 1
    if g.normalized().dot(n.normalized()) > 0:
        agree += 1

xs = [v.co.x for o in back for v in o.data.vertices]
ys = [v.co.y for o in back for v in o.data.vertices]
zs = [v.co.z for o in back for v in o.data.vertices]

print("OBJ2FBX: re-imported %d verts, %d polys" % (verts, polys))
print("OBJ2FBX: bounds x[%.3f,%.3f] y[%.3f,%.3f] z[%.3f,%.3f]"
      % (min(xs), max(xs), min(ys), max(ys), min(zs), max(zs)))
print("OBJ2FBX: signed volume %.2f -> %s"
      % (volume, "OUTWARD, correct" if volume > 0 else "INSIDE OUT"))
print("OBJ2FBX: shading agrees with winding on %d/%d triangles" % (agree, total))

if volume <= 0:
    fail("negative signed volume: the mesh imports inside out")
if total and agree < total * 0.99:
    fail("only %d/%d triangles agree with their shading normals" % (agree, total))

print("OBJ2FBX: wrote %s (%d bytes)" % (fbx_path, os.path.getsize(fbx_path)))
print("OBJ2FBX: OK")