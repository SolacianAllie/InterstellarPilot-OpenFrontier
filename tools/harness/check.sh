#!/usr/bin/env bash
#
# Offline verification for Open Frontier. Catches 99% of what would otherwise
# wait for an editor refocus.
#
#   bash tools/harness/check.sh            # build + check
#   bash tools/harness/check.sh --regen    # regenerate the csproj set first
#   HARNESS_JOBS=12 bash tools/harness/check.sh
#
# This lives in the repo rather than /tmp because /tmp is wiped. It was
# originally in /tmp/opencode/harness and lost twice to a power cut and a
# /tmp clear, which cost a rebuild of gen.py from scratch both times.
#
# WHY IT IS FAST (4.6s warm, 16.6s cold in the original; if it reads 70s
# again, one of these has been lost):
#   1. CSafeLoader, not SafeLoader. Same parser, compiled: 6.7s -> 0.9s
#      over 200 prefabs.
#   2. The prefab check caches on (mtime_ns, size) and stores the MESSAGES,
#      never just the verdict. Caching the boolean would make a second run
#      of a failing check print "0 failures" and quietly invert the check.
#   3. The projects build in parallel (xargs -P 6). They are independent:
#      no generated csproj has a ProjectReference, because each references
#      Unity's own DLLs out of Library/ScriptAssemblies rather than each
#      other, so there is no ordering to respect. Capped at 6 because the
#      editor is usually open and starving it to save three seconds is a
#      bad trade.
#   4. --no-restore on every build. NuGet restore runs whether anything
#      changed or not: 853ms against 521ms.

set -uo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT="$(cd "$HERE/../.." && pwd)"
BUILD="$HERE/build"
CHECKS="$HERE/checks"
JOBS="${HARNESS_JOBS:-6}"

REGEN=0
for arg in "$@"; do
  case "$arg" in
    --regen) REGEN=1 ;;
    -h|--help) sed -n '2,12p' "${BASH_SOURCE[0]}"; exit 0 ;;
    *) echo "unknown option: $arg" >&2; exit 2 ;;
  esac
done

cd "$PROJECT"

red()   { printf '\033[31m%s\033[0m\n' "$*"; }
green() { printf '\033[32m%s\033[0m\n' "$*"; }
head1() { printf '\n\033[1m%s\033[0m\n' "$*"; }

FAILED=0
note_fail() { FAILED=1; red "  FAIL: $*"; }

# ---------------------------------------------------------------- generate
GEN=0
if [ "$REGEN" = 1 ] || [ ! -d "$BUILD" ]; then
  GEN=1
  head1 "generating projects"
  python3 "$HERE/gen.py" || exit 1
fi
# A regenerated csproj set invalidates the restore, so it is redone rather
# than reused. Deciding this from `$BUILD` alone would skip the restore on a
# fresh clone, because generating creates the directory first.
if [ "$REGEN" = 1 ]; then
  rm -f "$BUILD/.restored"
fi

# gen.py --check is the guard against a green run that compiled almost
# nothing: it asserts the generated count matches the .asmdef files on disk,
# and that every .cs under Assets is in some csproj. Both were verified by
# breaking gen.py on purpose. If either fires, do NOT work around it -- the
# harness has stopped seeing your code.
head1 "guards"
if ! python3 "$HERE/gen.py" --check; then
  note_fail "gen.py --check (project count / stale .cs)"
fi

# ------------------------------------------------------------------- build
head1 "building $JOBS at a time"
if [ ! -f "$BUILD/.restored" ]; then
  # Restore is only needed when the csproj set changes, which is what
  # --regen and a cold build are. After this, --no-restore is correct.
  for p in "$BUILD"/*/; do
    n="$(basename "$p")"
    dotnet restore "$p$n.csproj" -v q --nologo >/dev/null 2>&1
  done
  touch "$BUILD/.restored"
fi

RESULTS="$(mktemp)"
WORKER="$(mktemp)"
trap 'rm -f "$RESULTS" "$WORKER"' EXIT

# The build step lives in a file, not in an inline `sh -c` string. Inline,
# $0 is the shell itself rather than the build directory, and a quoting slip
# there fails in a way that looks like a compile error.
cat > "$WORKER" <<'WORKER_EOF'
#!/usr/bin/env bash
build_root="$1"; name="$2"
out=$(dotnet build "$build_root/$name/$name.csproj" --no-restore -v q --nologo 2>&1)
if printf '%s' "$out" | grep -q 'Build succeeded'; then
  echo "PASS $name"
else
  echo "FAIL $name"
  printf '%s\n' "$out" \
    | grep -oE 'error CS[0-9]+: [^[]*' | sort -u | head -8 | sed 's|^|    |'
fi
WORKER_EOF
chmod +x "$WORKER"

for p in "$BUILD"/*/; do
  printf '%s\n' "$(basename "$p")"
done | xargs -P "$JOBS" -I{} "$WORKER" "$BUILD" {} > "$RESULTS"

NPASS=0; NFAIL=0
while read -r line; do
  case "$line" in
    PASS*) NPASS=$((NPASS+1)); green "  $line" ;;
    *) NFAIL=$((NFAIL+1)); red "$line" ;;
  esac
done < "$RESULTS"

# A zero-count build is this harness's signature failure mode: paths wrong,
# nothing compiled, and every test that did run passed.
if [ "$NPASS" = 0 ]; then
  note_fail "no project built at all -- gen.py paths are wrong"
elif [ "$NFAIL" -gt 0 ]; then
  note_fail "$NFAIL of $((NPASS+NFAIL)) projects failed to compile"
else
  green "  $NPASS projects, $(grep -ho '<Compile Include' "$BUILD"/*/*.csproj | wc -l | tr -d ' ') .cs files"
fi

# ------------------------------------------------------------------ checks
head1 "checks"
for c in "$CHECKS"/*.py; do
  [ -e "$c" ] || continue
  name="$(basename "$c" .py)"
  if out="$(python3 "$c" 2>&1)"; then
    printf '%s\n' "$out" | sed '/^$/d' | sed 's/^/  /'
  else
    printf '%s\n' "$out" | sed '/^$/d' | sed 's/^/  /'
    note_fail "$name"
  fi
done

# --------------------------------------------------------------- fbx writer
# FbxMeshWriter is pure C# with no UnityEngine in it precisely so it can be
# checked here: compiled standalone against synthetic meshes, then the output
# is parsed by an independent validator. It is the only test of that writer,
# and the two things it guards -- the bitwise-complement marker on each face's
# last index, and *N counting scalars rather than elements -- both fail
# SILENTLY in the file rather than loudly here.
FBX_RUN="$PROJECT/tools/fbx/run.sh"
if [ -f "$FBX_RUN" ]; then
  head1 "fbx writer"
  if bash "$FBX_RUN"; then
    :
  else
    note_fail "fbx writer verification"
  fi
fi

# ------------------------------------------------------------------ verdict
head1 ""
if [ "$FAILED" = 0 ]; then
  green "ALL CHECKS PASSED"
  echo
  echo "  A clean run is necessary but not sufficient: the harness does not"
  echo "  define every symbol the editor defines, and it cannot compile HLSL."
  echo "  Verify with:  grep -c \"error CS\" Logs/Editor.log   (after the last"
  echo "  import line)   and   grep \"Shader error\" Logs/Editor.log"
  exit 0
fi
red "CHECKS FAILED"
exit 1