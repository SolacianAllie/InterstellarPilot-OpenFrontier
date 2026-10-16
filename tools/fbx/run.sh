#!/usr/bin/env bash
#
# Verify FbxMeshWriter without Unity: compile it standalone, write fixture
# meshes, then parse the result with an independent validator.
#
#   bash tools/fbx/run.sh
#
# The validator is written without reference to the writer, so a bug shared
# between them cannot hide. It checks the two things that fail SILENTLY in an
# FBX -- the bitwise-complement marker on each face's last index, and *N array
# headers counting scalars rather than elements -- plus index range, layer
# wiring, and whether the winding agrees with the shading normals.

set -uo pipefail
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$HERE"

red()   { printf '\033[31m%s\033[0m\n' "$*"; }
green() { printf '\033[32m%s\033[0m\n' "$*"; }

rm -rf out
mkdir -p out

# Judged on the exit code, not by grepping the output: `grep -q error` also
# matches the failure it was meant to detect, so a broken build reported as
# a pass and vice versa.
if ! dotnet build -v q --nologo > "$HERE/.build.log" 2>&1; then
  red "fbx: the writer does not compile outside Unity"
  grep -E "error" "$HERE/.build.log" | head -5 | sed 's/^/  /'
  exit 1
fi

if ! dotnet run --no-build -v q 2>&1 | sed 's/^/  /'; then
  red "fbx: generating fixtures failed"
  exit 1
fi

if python3 validate_fbx.py out/sphere.fbx out/cube.fbx | sed 's/^/  /'; then
  green "  fbx: writer verified"
  exit 0
fi

red "  fbx: the written FBX failed validation"
exit 1
