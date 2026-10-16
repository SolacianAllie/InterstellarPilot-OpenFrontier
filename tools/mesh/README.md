# Mesh export: Unity Mesh .asset -> binary FBX Blender will open

```bash
bash tools/mesh/run.sh <Mesh.asset> [out.fbx]
# defaults to <asset dir>/../<name>.fbx
```

No Unity, no editor, no Autodesk SDK.

## Why this route exists

Three separate things were measured on this machine, not assumed:

1. **Blender 5.x refuses ASCII FBX.** Its importer reports
   `ASCII FBX files are not supported`. Anything writing FBX 7.4 as text
   cannot produce a file Blender will open.
2. **Unity's FBX exporter writes ASCII by default**, so its output is refused
   for the same reason.
3. **Unity's exporter is broken here anyway.** `Logs/Editor.log` records
   `DllNotFoundException: libxml2.so.2` at
   `Autodesk.Fbx.NativeMethods..cctor` → `ModelExporter.ExportObjects`. The
   Autodesk FBX SDK wants that soname; the distro ships `libxml2.so.16`, and
   pointing one at the other is an ABI mismatch, not a fix.

So `unity_mesh_to_obj.py` decodes Unity's own mesh serialisation and
`obj_to_fbx.py` runs **inside Blender**, using Blender's own exporter to write
the binary FBX. Writing binary FBX by hand is a much larger job than ASCII, and
a known-good implementation is already on disk.

## What each half checks

The decoder derives the vertex stride from the asset's own `m_Channels` list
rather than assuming 48, then checks:

- every vertex within 5% of the AABB extent radius — **the check that catches a
  misparsed stride**, which otherwise yields a plausible-looking mesh
- every index inside the vertex buffer
- index count a multiple of 3, and equal to the submesh's `indexCount`
- under 2% degenerate triangles

The Blender half treats "exported successfully" as unproven:

- the file starts with the `Kaydara FBX Binary` magic
- it **re-imports**, with the same vertex and polygon counts
- **signed volume is positive**, so the faces wind outward. Unity is
  left-handed and Blender right-handed, so inside-out is a real risk; signed
  volume settles it instead of eyeballing a viewport. A sphere of radius 10
  encloses 4/3·π·10³ = 4188.8.
- shading normals agree with the winding on ≥99% of triangles

## Layout

| file | runs in | does |
|---|---|---|
| `unity_mesh_to_obj.py` | plain python | decodes the `.asset`, verifies it, writes an OBJ |
| `obj_to_fbx.py` | inside Blender | OBJ → binary FBX, then re-imports and verifies |
| `run.sh` | shell | orchestrates the two |

The OBJ is only the hand-off. The FBX is the deliverable.

## Known limitation

This reads Unity's *current* mesh serialisation: `_typelessdata` /
`m_VertexBuffer` + `m_IndexBuffer`, 16- or 32-bit indices, four float32
attributes. A different layout — compressed meshes, quantised attributes,
more or fewer channels — is not handled, and the guards are what make that a
loud failure rather than a wrong mesh. It fails rather than guessing.