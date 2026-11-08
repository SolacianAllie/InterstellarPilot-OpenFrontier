#!/usr/bin/env python3
"""Turn a painted faction pattern into the triad zone-mask language.

The ship shaders' pattern layer reads `_DecalMap` as a ZONE MASK: R
weights the material's primary colour, G the secondary, B the tertiary.
This tool converts a painted pattern (three designated palette colours
on an alpha-brushed canvas) into exactly that language:
- every pixel near zone colour i becomes a pure channel weight of i;
- a pixel that blends two zone colours (an intended edge) becomes the
  blended channel weights, so the shader's `P*r + S*g + T*b` keeps the
  painter's colour ramp;
- everything opaque is hard-cut: below the alpha threshold the pixel
  keys out to RGB 0 + alpha 0 (the shader ignores that region);
- a background colour can be keyed OUT first (--bg RRGGBB, --bg-tol):
  a sheet painted black-as-empty on a full-alpha canvas becomes alpha-0
  emptiness here, which is what the shaders' ink gate wants;
- pixels that match none of the three colours are REPORTED, never
  invented into a zone (run with --palette N first to see the sheet's
  dominant colours and pick your three).

Weights are solved as a least-squares over the three colours, confined
to the simplex w >= 0, sum w = 1, exactly and vectorised (six face
candidates: each single vertex, each pair), so pure zones come out
binary and blends come out fractional - no iteration, no surprises.

Typical loop after painting a pattern in Krita:
  channelize.py pattern.png --palette 8
  channelize.py pattern.png --colors C0392B,F4D03F,1F618D -o mask.png
"""
import argparse, os, sys

try:
    import numpy as np
    from PIL import Image
except ImportError:
    sys.exit('decals tools need pillow+numpy:  '
             '~/.venvs/decals/bin/pip install pillow numpy   '
             '(use tools/decals/run.sh to pick that venv up)')


def parse_color(text):
    text = text.lstrip('#')
    return int(text[0:2], 16), int(text[2:4], 16), int(text[4:6], 16)


def simplex_weights(colors, rgb, tol):
    """Least-squares weights on the simplex, exact for every pixel.

    colors: 3x3 rows (primary, secondary, tertiary), rgb: Nx3 floats
    in 0..1. Tries each single vertex and each edge (the closed form
    of a 2-var LS along it), keeps the minimal residual per pixel.
    Returns (weights Nx3, residual Nx1, unmapped Nx1 bool)."""
    n = rgb.shape[0]
    best_w = np.zeros((n, 3), dtype=np.float32)
    best_r = np.full(n, np.float32(np.inf), dtype=np.float32)

    def consider(w, r):
        upd = r < best_r
        best_w[upd] = w[upd]
        best_r[upd] = r[upd]

    for i in range(3):
        w = np.zeros((n, 3), dtype=np.float32)
        w[:, i] = 1.0
        consider(w, (((w @ colors) - rgb) ** 2).sum(axis=1))
    for i in range(3):
        for j in range(i + 1, 3):
            d = colors[i] - colors[j]
            dd = float((d * d).sum())
            if dd < 1e-12:
                continue
            a = ((rgb - colors[j]) @ d) / dd
            w = np.zeros((n, 3), dtype=np.float32)
            w[:, i] = np.clip(a, 0.0, 1.0)      # per-pixel mix along the edge
            w[:, j] = 1.0 - w[:, i]
            consider(w, (((w @ colors) - rgb) ** 2).sum(axis=1))

    return best_w, np.sqrt(np.maximum(best_r, 0.0)), (best_r > tol * tol)


def main(argv=None):
    p = argparse.ArgumentParser(
        description=__doc__,
        formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument('input', help='painted pattern sheet (PNG, alpha brush)')
    p.add_argument('-o', '--output',
                   help='triad zone mask PNG (alpha-brushed, hard alpha); '
                        'required whenever --colors is given')
    p.add_argument('--colors', default='',
                   help='comma-separated hex of the THREE painted zone '
                        'colours, faction order primary,secondary,tertiary '
                        '(becomes the R,G,B channel weights)')
    p.add_argument('--palette', type=int, default=0, metavar='N',
                   help='print the N dominant colours of the opaque set '
                        '(useful for choosing --colors), then continue')
    p.add_argument('--alpha-thresh', type=float, default=0.5,
                   help='hard alpha cut between 0 and 1 (default 0.5)')
    p.add_argument('--bg', help='background colour to key OUT (RRGGBB), e.g. '
                   '--bg 000000 when a sheet paints black as empty')
    p.add_argument('--bg-tol', type=float, default=0.12,
                   help='max colour distance from --bg that still keys out '
                        '(default 0.12, same scale as --tol)')
    p.add_argument('--tol', type=float, default=0.12,
                   help='max colour distance from the triad before a pixel '
                        'is reported unmapped (default 0.12)')
    p.add_argument('--scale', type=int, default=1,
                   help='nearest-neighbour integer upscale (default none)')
    p.add_argument('--strict', action='store_true',
                   help='exit 1 when any unmapped pixel exists')
    args = p.parse_args(argv)

    img = Image.open(args.input)
    img = img.convert('RGBA') if img.mode != 'RGBA' else img
    arr = np.asarray(img, dtype=np.float32)
    h, w = arr.shape[:2]
    rgb = arr[:, :, :3]

    opaque = arr[:, :, 3] >= args.alpha_thresh * 255.0
    if args.bg:
        bg = np.array(parse_color(args.bg), dtype=np.float32) / 255.0
        dist = np.sqrt(((rgb / 255.0 - bg[None, None, :]) ** 2).sum(axis=2))
        keyed = dist <= args.bg_tol
        opaque &= ~keyed
        print('%s: bg #%s keys %d px out (painted-empty -> alpha 0)'
              % (os.path.basename(args.input), args.bg.lstrip('#').upper(),
                 int(keyed.sum())))
    fp = rgb[opaque]
    n_opaque = int(opaque.sum())

    if args.palette > 0:
        labels, counts = np.unique(fp.astype(np.uint8).reshape(-1, 3),
                                   axis=0, return_counts=True)
        order = np.argsort(-counts)[:args.palette]
        print('%s: %d opaque px, dominant colours:'
              % (os.path.basename(args.input), n_opaque))
        for k in order:
            c = labels[k]
            print('  #%02X%02X%02X  %6.2f%%'
                  % (c[0], c[1], c[2],
                     100.0 * counts[k] / max(n_opaque, 1)))

    colors = np.array([parse_color(x) for x in args.colors.split(',')],
                      dtype=np.float32) if args.colors else None
    if colors is None:
        if args.palette > 0:
            return 0                        # inspect-only run, done
        p.error('--colors needs exactly three comma-separated RRGGBB '
                'values (or --palette N alone to inspect)')
    if colors.shape != (3, 3):
        p.error('--colors needs exactly three comma-separated RRGGBB values')
    if args.output is None:
        p.error('-o is required when converting (omit --colors to inspect '
                'with --palette alone)')
    if len(np.unique(colors, axis=0)) < 3:
        p.error('--colors carries duplicated paints; zones must be three '
                'distinct colours')

    weights, resid, unmapped = simplex_weights(colors / 255.0, fp / 255.0,
                                               args.tol)

    out = np.zeros((h, w, 4), dtype=np.uint8)
    for ch in range(3):
        out[opaque, ch] = np.clip(weights[:, ch] * 255.0, 0, 255
                                  ).astype(np.uint8)
    out[opaque, 3] = 255
    image = Image.fromarray(out, 'RGBA')
    if args.scale > 1:
        image = image.resize((w * args.scale, h * args.scale),
                             resample=Image.Resampling.NEAREST)
    image.save(args.output)

    name = os.path.basename(args.input)
    if unmapped.any():
        worst_i = int(np.argmax(resid))
        worst_c = fp[worst_i].astype(int)
        print('%s -> %s  (opaque %.2f%%, UNMAPPED %d px, worst distance '
              '%.3f at e.g. #%02X%02X%02X)'
              % (name, os.path.basename(args.output),
                 100.0 * opaque.mean(), int(unmapped.sum()), resid.max(),
                 worst_c[0], worst_c[1], worst_c[2]))
        print('  unmapped px are a paint outside the three zones: pick '
              'different --colors, repaint, or raise --tol %.2f to accept '
              'them clamped to the nearest zone.' % args.tol)
        if args.strict:
            return 1
    else:
        print('%s -> %s  (opaque %.2f%% px; every zone mapped, no stray '
              'paint)' % (name, os.path.basename(args.output),
                          100.0 * opaque.mean()))
    return 0


if __name__ == '__main__':
    sys.exit(main())
