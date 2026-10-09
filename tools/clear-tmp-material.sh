#!/usr/bin/env bash
# clear-tmp-material.sh — zero m_sharedMaterial on TextMeshPro components only.
#
# Why: the new Noto font asset keeps its material and atlas EMBEDDED (local
# sub-assets, fileID -4744098847162023832 / 8707631093939876025). An embedded
# sub-asset cannot be referenced from another file, so every TMP component that
# still points m_sharedMaterial at the old LiberationSans material must have
# that field cleared — otherwise the font asset says Noto while the material
# says LiberationSans, and text renders blank. That is the exact symptom being
# fixed, so a font swap that leaves materials behind would change nothing.
#
# Only MonoBehaviour blocks carrying the TextMeshProUGUI script guid are
# touched. m_sharedMaterial is also a field on Image/Button/Toggle/RawImage, and
# zeroing those would strip every sprite in the UI.
set -euo pipefail

TMP_SCRIPT="f4688fdb7df04437aeb418b961361dc5"   # TextMeshProUGUI
ROOT="$(git rev-parse --show-toplevel)"
cd "$ROOT"

python3 - "$TMP_SCRIPT" <<'PY'
import os, sys, re
tmp = sys.argv[1]
changed_files = 0
changed_blocks = 0
scanned = 0

for root, dirs, files in os.walk('Assets'):
    for fn in files:
        if not fn.endswith(('.unity', '.prefab')):
            continue
        p = os.path.join(root, fn)
        try:
            s = open(p, errors='ignore').read()
        except OSError:
            continue
        scanned += 1
        # split into YAML documents on "--- !u!"
        parts = re.split(r'(?m)^(--- !u!\d+ &\d+)$', s)
        out = [parts[0]]
        dirty = False
        for i in range(1, len(parts), 2):
            header, body = parts[i], parts[i + 1]
            if tmp in body and re.search(r'(?m)^  m_sharedMaterial: \{fileID: (?!0\b)\d+', body):
                new = re.sub(r'(?m)^(  m_sharedMaterial: )\{fileID: \d+(, guid: [a-f0-9]{32}, type: \d+)?\}',
                             r'\g<1>{fileID: 0}', body)
                if new != body:
                    changed_blocks += 1
                    dirty = True
                out.append(header); out.append(new)
            else:
                out.append(header); out.append(body)
        if dirty:
            open(p, 'w').write(''.join(out))
            changed_files += 1

print(f"scanned {scanned} scenes/prefabs")
print(f"cleared m_sharedMaterial on {changed_blocks} TMP component(s) across {changed_files} file(s)")
PY
