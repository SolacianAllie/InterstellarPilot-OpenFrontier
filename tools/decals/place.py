#!/usr/bin/env python3
"""Place a quantized mark into a decal sheet at exact sheet coordinates.

The sheet's contract with the shader is: alpha is the brush, RGB is a
multiply filter, marks never touch pixels they do not cover. So place()
is strictly additive: the sheet is copied, the mark is nearest-scaled to
its target size and composited under a max-alpha rule, and with
--check-empty the tool refuses to land a mark where the sheet already
has paint (a mark landing on a mark is almost always a mis-paste).

Always writes to 'output' - the source sheet is treated as read-only so
a bad paste is a file-swap away from undone, never lost.

  place.py --sheet Magnus_Decal_X.png --mark mark.png \
           --x 1200 --y 2400 --output out.png
"""
import argparse, os, sys

try:
    import numpy as np
    from PIL import Image
except ImportError:
    sys.exit('decals tools need pillow+numpy:  '
             '~/.venvs/decals/bin/pip install pillow numpy   '
             '(use tools/decals/run.sh to pick that venv up)')


def load(path):
    img = Image.open(path).convert('RGBA')
    return np.asarray(img, dtype=np.uint8)


def main(argv=None):
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--sheet', required=True, help='decal sheet (read as source)')
    p.add_argument('--mark', required=True, help='quantized mark patch')
    p.add_argument('--x', type=int, required=True, help='sheet-space top-left')
    p.add_argument('--y', type=int, required=True)
    p.add_argument('--scale', type=int, default=1,
                   help='nearest scale for the mark patch (default 1:1)')
    p.add_argument('--check-empty', action='store_true',
                   help='refuse if the target area has any existing alpha')
    p.add_argument('--output', required=True, help='written sheet (never the source)')
    args = p.parse_args(argv)

    if os.path.abspath(args.output) == os.path.abspath(args.sheet):
        sys.exit('place.py refuses to write onto the source sheet - '
                 'point --output somewhere else (in-place edits are how '
                 'a bad paste disappears).')

    sheet, mark = load(args.sheet), load(args.mark)
    if args.scale > 1:
        m = Image.fromarray(mark, 'RGBA')
        mark = np.asarray(m.resize((mark.shape[1] * args.scale,
                                    mark.shape[0] * args.scale),
                                   resample=Image.Resampling.NEAREST),
                          dtype=np.uint8)
    mh, mw, sh, sw = mark.shape[0], mark.shape[1], sheet.shape[0], sheet.shape[1]
    x0, y0 = args.x, args.y
    x1, y1 = x0 + mw, y0 + mh
    if x0 < 0 or y0 < 0 or x1 > sw or y1 > sh:
        sys.exit('mark %dx%d at (%d,%d) does not fit a %dx%d sheet'
                 % (mw, mh, x0, y0, sw, sh))
    region = sheet[y0:y1, x0:x1]
    if args.check_empty and (mark[:, :, 3] > 0).any() and (region[:, :, 3] > 0).any():
        sys.exit('target area (%d,%d..%d,%d) already has alpha - '
                 'place.py would be adding a mark onto a mark. '
                 'Pick an empty spot or drop --check-empty.'
                 % (x0, y0, x1, y1))
    out = sheet.copy()
    ma = mark[:, :, 3] > 0
    out[y0:y1, x0:x1, :3][ma] = mark[:, :, :3][ma]
    out[y0:y1, x0:x1, 3][ma] = np.maximum(region[:, :, 3][ma],
                                           mark[:, :, 3][ma])
    tmp = args.output + '.tmp'
    Image.fromarray(out, 'RGBA').save(tmp, format='PNG')
    os.replace(tmp, args.output)
    ink = int(ma.sum())
    print('placed %dx%d mark at (%d,%d) -> %s  (%d px of new ink)'
          % (mw, mh, x0, y0, os.path.basename(args.output), ink))
    return 0


if __name__ == '__main__':
    sys.exit(main())
