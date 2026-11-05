#!/usr/bin/env python3
"""Turn an AI-generated patch into a decal-ready mark.

Takes what the model produced (soft RGB anywhere, usually art on a solid
chroma background) and returns the only form a sheet accepts: flat
palette colours, keyed background, hard alpha, optional nearest-neighbour
upscale. Nothing here re-renders the artwork - it locks it into the
sheet's language.

Typical loop after a Krita/Comfy generation:
  quantize.py generated.png --key FF00FF \
      --palette 0A2472,FFFFFF,151515 --scale 2 -o mark.png
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


def hex_list(text):
    return np.array([tuple(c) for c in ([] if not text else
                     [parse_color(x) for x in text.split(',')])],
                    dtype=np.float32) if text.strip() else None


def main(argv=None):
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('input', help='AI output patch (PNG)')
    p.add_argument('-o', '--output', required=True)
    p.add_argument('--key', default='magenta',
                   help="background colour to key out: 'magenta', or a hex "
                        "RRGGBB, or 'alpha' when the input already carries "
                        "its own alpha (default magenta)")
    p.add_argument('--key-tol', type=int, default=64,
                   help='per-channel tolerance around the key colour')
    p.add_argument('--palette', default='',
                   help='comma-separated hex colours; every kept pixel '
                        'snaps to its nearest palette entry')
    p.add_argument('--scale', type=int, default=1,
                   help='nearest-neighbour integer upscale (default none)')
    args = p.parse_args(argv)

    img = Image.open(args.input)
    img = img.convert('RGBA') if img.mode != 'RGBA' else img
    arr = np.asarray(img, dtype=np.float32)
    h, w = arr.shape[:2]
    rgb, alpha = arr[:, :, :3], arr[:, :, 3]

    keymode = args.key.lower()
    if keymode == 'alpha':
        a = alpha
    elif keymode == 'magenta':
        key = np.array((255, 0, 255), dtype=np.float32)
    else:
        key = parse_color(keymode)
        key = np.array(key, dtype=np.float32)
    if keymode != 'alpha':
        dist = np.abs(rgb - key[None, None, :]).max(axis=2)
        a = (dist > args.key_tol).astype(np.float32) * 255.0
        # pixels that are neither near-key nor near-opaque in alpha: keep
        a = np.maximum(a, (alpha > 127).astype(np.float32) * 255.0
                        * np.where(dist > args.key_tol, 1.0, 0.0))

    palette = hex_list(args.palette)
    if palette is not None:
        d2 = ((rgb[:, :, None, :] - palette[None, None, :, :]) ** 2).sum(axis=3)
        snap = palette[d2.argmin(axis=2)]
        rgb = snap

    out = np.zeros((h, w, 4), dtype=np.uint8)
    out[:, :, :3] = np.clip(rgb, 0, 255).astype(np.uint8)
    out[:, :, 3] = np.clip(a, 0, 255).astype(np.uint8)
    image = Image.fromarray(out, 'RGBA')
    if args.scale > 1:
        image = image.resize((w * args.scale, h * args.scale),
                            resample=Image.Resampling.NEAREST)
    image.save(args.output)
    kept = (out[:, :, 3] > 0).mean()
    print('%s: %dx%d -> %s%s  (kept %.2f%% px)'
          % (os.path.basename(args.input), w, h,
             os.path.basename(args.output),
             ' x%d' % args.scale if args.scale > 1 else '', 100 * kept))
    if kept > 0.5 and keymode != 'alpha':
        print('  WARNING: over half the patch survived the key - the key '
              'colour probably does not match this render.')
    return 0


if __name__ == '__main__':
    sys.exit(main())
