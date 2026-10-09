#!/usr/bin/env bash
#
# swap-font.sh — repoint every TextMeshPro reference at a new font asset.
#
# OPEN BACKPORT. The whole UI resolves to a single font asset
# (aefad2323caa07c409a285aad750d046 = "Assets/Resources/fonts & materials/
# LiberationSans SDF.asset") across 651 references in 91 scenes/prefabs. The
# other LiberationSans set under "Assets/TextMesh Pro/" is referenced by nothing
# outside TMP Settings, which is why a single GUID replacement is sufficient.
#
# The replacement font asset must already exist as a TMP_FontAsset (.asset).
# A bare .ttf is NOT usable by TextMeshPro — it needs a generated atlas plus
# character/glyph tables, which only the editor's Font Asset Creator produces.
#
# Usage:  tools/swap-font.sh <path-to-new-font-asset>
# Dry run: add --dry-run

set -euo pipefail

OLD_GUID="aefad2323caa07c409a285aad750d046"
ROOT="$(git rev-parse --show-toplevel)"
cd "$ROOT"

DRY=0
[ "${2:-}" = "--dry-run" ] && DRY=1

if [ $# -lt 1 ]; then
	echo "usage: tools/swap-font.sh <path-to-new-font-asset> [--dry-run]" >&2
	exit 2
fi

NEW_ASSET="$1"
if [ ! -f "$NEW_ASSET" ]; then
	echo "error: '$NEW_ASSET' not found." >&2
	echo "  TextMeshPro cannot use a raw .ttf — create a TMP_FontAsset first:" >&2
	echo "    Window > TextMeshPro > Font Asset Creator" >&2
	echo "    Source Font: Assets/Font/NotoSans-Regular.ttf" >&2
	echo "    Atlas: 1024x1024, Dynamic (so unseen glyphs still resolve)" >&2
	exit 2
fi

# Guard: a .ttf/.otf HAS a .meta, so existence alone is not enough. TMP needs a
# real TMP_FontAsset — an asset whose body carries m_SourceFontFileGUID and a
# character table. Reject anything else before touching a single scene.
if [[ "$NEW_ASSET" =~ \.(ttf|otf|ttc)$ ]]; then
	echo "error: '$NEW_ASSET' is a raw font file. TextMeshPro will not render it." >&2
	echo "  Create a TMP font asset from it first:" >&2
	echo "    Window > TextMeshPro > Font Asset Creator" >&2
	echo "    Source Font: $NEW_ASSET" >&2
	echo "    Character Set: ASCII   Atlas: 1024x1024   Sampling: Dynamic" >&2
	exit 2
fi

META="${NEW_ASSET}.meta"
[ -f "$META" ] || { echo "error: no .meta for '$NEW_ASSET' (not imported yet?)" >&2; exit 2; }
NEW_GUID="$(grep -m1 '^guid:' "$META" | awk '{print $2}')"
[ -n "$NEW_GUID" ] || { echo "error: could not read guid from $META" >&2; exit 2; }

if ! grep -q 'm_SourceFontFileGUID' "$NEW_ASSET" 2>/dev/null; then
	echo "error: '$NEW_ASSET' is not a TMP_FontAsset (no m_SourceFontFileGUID)." >&2
	echo "  Point this at a generated Font Asset, not a raw font." >&2
	exit 2
fi

echo "old font guid: $OLD_GUID"
echo "new font guid: $NEW_GUID   ($NEW_ASSET)"
[ "$OLD_GUID" = "$NEW_GUID" ] && { echo "error: that's the same font" >&2; exit 2; }
echo

count_in() { grep -rl "$1" "$2" --include='*.unity' --include='*.prefab' 2>/dev/null | wc -l; }
n_files=$(count_in "$OLD_GUID" Assets)
echo "scenes/prefabs referencing the old font: $n_files"

if [ "$DRY" -eq 1 ]; then
	echo "dry run — nothing written. Re-run without --dry-run to apply."
	exit 0
fi

# 1. every m_fontAsset / m_sharedMaterial-bearing scene+prefab
echo "→ repointing font references"
grep -rl "$OLD_GUID" Assets --include='*.unity' --include='*.prefab' 2>/dev/null \
	| while IFS= read -r f; do sed -i "s/$OLD_GUID/$NEW_GUID/g" "$f"; echo "    $f"; done

# 2. TMP Settings default font (both copies ship in this project)
echo "→ repointing TMP Settings default font"
for s in "Assets/TextMesh Pro/Resources/TMP Settings.asset" "Assets/Resources/TMP Settings.asset"; do
	[ -f "$s" ] && sed -i "s/$OLD_GUID/$NEW_GUID/g" "$s" && echo "    $s"
done

# 3. the old font asset itself + its fallback keep a dangling reference chain;
#    clear the fallback tables so TMP cannot route to the broken pair.
echo "→ clearing fallback tables on the retired fonts"
for f in "Assets/Resources/fonts & materials/LiberationSans SDF.asset" \
	"Assets/Resources/fonts & materials/LiberationSans SDF - Fallback.asset" \
	"Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset" \
	"Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset"; do
	[ -f "$f" ] || continue
	python3 - "$f" <<-'PY'
		import re, sys
		p = sys.argv[1]
		s = open(p).read()
		# replace the whole fallback table body with an empty list
		s = re.sub(r"(m_FallbackFontAssetTable:\n)(?:  - .*\n)+", r"\1  []\n", s)
		open(p, "w").write(s)
		print("    " + p)
	PY
done

echo
echo "done. Verify in the editor, then BUILD (this branch has shipped"
echo "build-only failures that compiled clean)."
