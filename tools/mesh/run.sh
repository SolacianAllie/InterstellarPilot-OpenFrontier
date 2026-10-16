#!/usr/bin/env bash
#
# Export a Unity Mesh .asset to a binary FBX that Blender will open, without
# opening Unity.
#
#   bash tools/mesh/run.sh <Mesh.asset> [out.fbx]
#
# WHY NOT THE UNITY EDITOR, AND WHY NOT AN ASCII WRITER
#
#   * Blender 5.x refuses ASCII FBX: "ASCII FBX files are not supported".
#     Anything that writes FBX 7.4 as text cannot produce a usable file here,
#     which includes Unity's own exporter -- its default format is ASCII.
#   * Unity's exporter is broken on this machine regardless: Editor.log
#     records `DllNotFoundException: libxml2.so.2`. The Autodesk FBX SDK wants
#     that soname, the distro ships libxml2.so.16, and pointing one at the
#     other is an ABI mismatch rather than a fix.
#
# So the mesh is decoded here from Unity's own serialisation and Blender writes
# the FBX with its own exporter. Both halves verify themselves: the decoder
# checks vertex radii against the AABB extent (the check that catches a
# misparsed stride), and the writer re-imports what it wrote and confirms the
# signed volume is positive, which is what settles inside-out.

set -uo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

red()   { printf '\033[31m%s\033[0m\n' "$*"; }
green() { printf '\033[32m%s\033[0m\n' "$*"; }

ASSET="${1:-}"
OUT="${2:-}"
if [ -z "$ASSET" ]; then
  echo "usage: bash tools/mesh/run.sh <Mesh.asset> [out.fbx]" >&2
  exit 2
fi
[ -f "$ASSET" ] || { red "no such file: $ASSET"; exit 2; }

if [ -z "$OUT" ]; then
  NAME="$(basename "$ASSET" | sed 's/\.asset$//')"
  OUT="$(dirname "$ASSET")/../$NAME.fbx"
fi
mkdir -p "$(dirname "$OUT")"

OBJ="${OUT%.fbx}.obj"
SUMMARY="$(python3 "$HERE/unity_mesh_to_obj.py" "$ASSET" "$OBJ")" || {
  red "decoding the mesh failed"
  echo "$SUMMARY"
  exit 1
}
echo "$SUMMARY" | sed 's/^/  /'

VERTS="$(echo "$SUMMARY" | sed -n 's/^STATS: verts=\([0-9]*\).*/\1/p')"
TRIS="$(echo "$SUMMARY"  | sed -n 's/^STATS:.* tris=\([0-9]*\)$/\1/p')"
if [ -z "$VERTS" ] || [ -z "$TRIS" ]; then
  red "could not read the vertex/triangle counts back from the decoder"
  exit 1
fi

if ! command -v blender >/dev/null 2>&1; then
  red "blender is not on PATH -- the binary FBX has to come from its exporter"
  exit 1
fi

if blender --background --python "$HERE/obj_to_fbx.py" -- \
     "$OBJ" "$OUT" "$VERTS" "$TRIS" 2>&1 \
     | grep -E "OBJ2FBX|Error" | sed 's/^/  /'; then
  green "  wrote $OUT"
else
  red "  the FBX failed verification"
  exit 1
fi