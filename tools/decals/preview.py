#!/usr/bin/env python3
"""Preview a decal sheet exactly the way the ship shaders will read it.

This composites decal-over-albedo with the same math as
OpenFrontier/Ship Surface and /Ship Cloak - BOTH layers, in order.

The pattern sheet (--decal, _DecalMap) is a triad ZONE MASK: its R/G/B
weight the material's three colours, alpha stays the brush. The superpower
brand (--brand, _PowerDecalMap) is the old baked-colour layer over it.

    triad  = clamp(P.primary*r + P.secondary*g + P.tertiary*b)  # zone select
    shaded = hull tint x lerp(1, layer.rgb, layer.a)            # multiply pass
    out    = lerp(shaded, layer.rgb, layer.a * strength)         # paint lift

at the strengths the materials actually carry (0.5 defaults, both ranges
capped there). It is a QA tool: cheap, deterministic, runs before Unity so
alignment and readability problems do not cost an editor round trip.
Emission (the _DecalEmissionMap layer) is added the same way when
--emission is given. Math runs in sRGB space - a visual approximation of
the engine's linear-light result, not a LUT.
"""
import argparse, os, sys

try:
    import numpy as np
    from PIL import Image
except ImportError:
    sys.exit('decals tools need pillow+numpy:  '
             '~/.venvs/decals/bin/pip install pillow numpy   '
             '(use tools/decals/run.sh to pick that venv up)')


def load_f(path):
    img = Image.open(path).convert('RGBA')
    a = np.asarray(img, dtype=np.float32) / 255.0
    return a[:, :, :3], a[:, :, 3]


def main(argv=None):
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--albedo', required=True, help='hull colour map')
    p.add_argument('--decal', required=True,
                   help='pattern sheet (_DecalMap): R/G/B triad zone mask, '
                        'alpha brush')
    p.add_argument('--brand', help='superpower brand sheet (_PowerDecalMap), '
                   'if the material binds one')
    p.add_argument('--emission', help='the _DecalEmissionMap texture, if wired')
    p.add_argument('--tint', default='FFFFFF', help='_BaseColor tint as RRGGBB')
    p.add_argument('--primary', default='FFFFFF',
                   help='Pattern Primary (R zones) as RRGGBB')
    p.add_argument('--secondary', default='FFFFFF',
                   help='Pattern Secondary (G zones) as RRGGBB')
    p.add_argument('--tertiary', default='FFFFFF',
                   help='Pattern Tertiary (B zones) as RRGGBB')
    p.add_argument('--strength', type=float, default=0.5,
                   help='Pattern Paint Strength the material carries (capped 0.5)')
    p.add_argument('--brand-strength', type=float, default=0.5,
                   help='Superpower Paint Strength (capped 0.5)')
    p.add_argument('--emission-color', default='FFFFFF', help='Decal Glow Color RRGGBB')
    p.add_argument('--out', required=True, help='preview PNG to write')
    p.add_argument('--width', type=int, default=1600,
                   help='preview downscale width (full res with --region)')
    p.add_argument('--region', help='x,y,w,h in sheet pixels - full-res crop')
    args = p.parse_args(argv)

    strength = min(max(args.strength, 0.0), 0.5)
    tint = np.array([int(args.tint[i:i + 2], 16) for i in (0, 2, 4)],
                    dtype=np.float32) / 255.0

    hull, _ = load_f(args.albedo)
    decal_rgb, decal_a = load_f(args.decal)
    if hull.shape[:2] != decal_a.shape[:2]:
        sys.exit('albedo %s and decal %s differ in size - sheets ride the '
                 'hull\'s UV space and must match'
                 % (args.albedo, args.decal))

    h, w = decal_a.shape[:2]
    hullt = hull * tint[None, None, :]

    def parse_rgb(text):
        return np.array([int(text[i:i + 2], 16) for i in (0, 2, 4)],
                        dtype=np.float32) / 255.0

    primary, secondary, tertiary = (parse_rgb(x) for x in
                                    (args.primary, args.secondary, args.tertiary))
    # triad = clamp(P*r + S*g + T*b)  -- the zone select, zones exclusive,
    # edge weights blending exactly as the shader sums them
    triad = np.clip(primary[None, None, :] * decal_rgb[:, :, 0:1]
                    + secondary[None, None, :] * decal_rgb[:, :, 1:2]
                    + tertiary[None, None, :] * decal_rgb[:, :, 2:3], 0.0, 1.0)
    # shaded = hull * lerp(1, layer.rgb, layer.a)   -- the multiply pass
    shaded = hullt * ((1.0 - decal_a[:, :, None]) + triad * decal_a[:, :, None])
    # out = lerp(shaded, layer.rgb, layer.a * strength)  -- the paint lift
    out = shaded * (1.0 - decal_a * strength)[:, :, None] \
        + triad * (decal_a * strength)[:, :, None]

    if args.brand:
        brand_rgb, brand_a = load_f(args.brand)
        if brand_a.shape[:2] != (h, w):
            sys.exit('brand sheet %s differs in size from the pattern '
                     'sheet - both ride the hull\'s UV space'
                     % args.brand)
        bst = min(max(args.brand_strength, 0.0), 0.5)
        shaded = out * ((1.0 - brand_a[:, :, None]) + brand_rgb * brand_a[:, :, None])
        out = shaded * (1.0 - brand_a * bst)[:, :, None] \
            + brand_rgb * (brand_a * bst)[:, :, None]

    if args.emission:
        er, ea = load_f(args.emission)
        ec = np.array([int(args.emission_color[i:i + 2], 16) for i in (0, 2, 4)],
                      dtype=np.float32) / 255.0
        glow = er * ea[:, :, None] * ec[None, None, :]
        out = np.clip(out + glow * np.where(ea[:, :, None] > 0, 1.0, 0.0), 0, 1)

    if args.region:
        x, y, rw, rh = [int(v) for v in args.region.split(',')]
    else:
        x, y, rw, rh = 0, 0, w, h
    crop = Image.fromarray((np.clip(out[y:y + rh, x:x + rw], 0, 1) * 255).astype(np.uint8))
    if rw > args.width and not args.region:
        crop = crop.resize((args.width, int(rh * args.width / rw)),
                           resample=Image.Resampling.LANCZOS)
    crop.save(args.out)
    ink = (decal_a > 0).mean()
    ink_brand = ((brand_a > 0).mean() if args.brand else 0.0)
    print('%s: %dx%d @pattern %.2f%s, ink %.2f%%%s -> %s'
          % (os.path.basename(args.out), w, h, strength,
             ' @brand %.2f' % bst if args.brand else '',
             100 * ink,
             ' (brand ink %.2f%%)' % (100 * ink_brand) if args.brand else '',
             args.out))
    return 0


if __name__ == '__main__':
    sys.exit(main())
