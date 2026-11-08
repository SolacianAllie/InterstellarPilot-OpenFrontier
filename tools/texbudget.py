#!/usr/bin/env python3
"""Texture memory census: what the importers actually produce.

Disk PNG size never reaches the GPU - runtime memory is the importer's
output: (capped size) x (compression format) + mips. This walks every
texture in Assets/, reads its TextureImporter .meta (importer base +
explicit per-target overrides), opens the source with Pillow for true
dimensions, and estimates the runtime footprint per audience:

  - mobile-MB: what an Android build resolves (ASTC 6x6 for the plain
    Compressed class, the ETC2-family 8bpp for the legacy CompressedLQ
    class, ASTC 4x4 for HQ)
  - desk-MB: what the desktop/editor resolves (BC1/BC7-family, modelled
    as 8bpp for Compressed and HQ, DXT-LQ for CompressedLQ)

All numbers assume mipmaps and count the +33% mip chain. True format
resolution happens inside Unity; the classes are what bounds it.

--fix-lq reports every texture that still carries the rip-era
CompressedLQ class (any textureCompression: 2 line in the meta) under
--under, with before/after memory estimates for flipping it to the
plain Compressed class. --apply writes the flip. No repainting
involved - and on Android ASTC 6x6 is a quality STEP UP from ETC-LQ.
"""
import argparse
import os
import re
import sys

try:
    from PIL import Image
except ImportError:
    sys.exit('decals tools need pillow+numpy:  '
             '~/.venvs/decals/bin/pip install pillow numpy '
             '(use tools/decals/run.sh to pick that venv up)')

# bits per pixel of the formats Unity can emit for a square texture.
BPP = {
    'rgba32': 32.0,           # uncompressed: 4 bytes/px
    'astc4x4': 8.0,           # the "high quality" ASTC step
    'astc6x6': 128.0 / 36.0,  # 3.56 - Unity's mobile "Normal" default
    'astc8x8': 2.0,           # one quality step down, quarter of 6x6
    'etc2': 8.0,
}

COMP = {'0': 'uncomp', '1': 'compr', '2': 'low', '3': 'high'}

META_KEYS = ('maxTextureSize', 'textureCompression', 'textureCompressionFormat',
             'crunchedCompression', 'compressionQuality', 'mipMaps',
             'overridden', 'buildTarget')


def parse_meta(path):
    """Regex-parse a texture .meta: importer base + truly overridden blocks."""
    all_flags = {}
    lines = open(path, 'r', encoding='utf-8', errors='replace').readlines()
    for line in lines:
        key, _, val = line.strip().partition(':')
        if key in META_KEYS:
            all_flags[key] = val.strip()
    platforms = []
    entry = None
    for line in lines:
        stripped = line.strip()
        if stripped == 'platformSettings:':
            entry, in_platform = None, True
            continue
        if not entry:
            continue
        key, _, val = stripped.partition(':')
        key = key.strip()
        if key == 'buildTarget':
            entry = {'buildTarget': val.strip(), 'overridden': '0'}
            platforms.append(entry)
            continue
        if key in META_KEYS:
            entry[key] = val.strip()
        if key == 'overridden' and val.strip() != '1':
            platforms.pop()
            entry = None
    return all_flags, [p for p in platforms if p.get('overridden') == '1']


def footprint(width, height, cap, bpp, mip=True):
    side_w = min(width, cap)
    side_h = min(height, cap)
    mb = (side_w * side_h) * (bpp / 8.0) / (1024.0 * 1024.0)
    return mb * (4.0 / 3.0) if mip else mb


def bpp_for(platform, entry):
    """Best-effort bits/px for an importer block on a given target.

    True output format is resolved inside Unity, but the classes map
    cheaply: Compressed = the platform's normal compressed default
    (ASTC 6x6 on Android, BC-family on desktop), CompressedLQ = that
    platform's lowest-quality legacy format (ETC2-family 8bpp), HQ =
    the highest-quality one (ASTC 4x4 / BC7, both 8bpp). Uncompressed
    is RGBA32 in every case.
    """
    comp = entry.get('textureCompression', '1')
    crunch = entry.get('crunchedCompression', '0')
    if comp == '0':
        return BPP['rgba32']
    if comp == '2':   # LQ: ETC2-family on mobile, DXT on desktop
        return BPP['etc2']
    if comp == '3':   # HQ: ASTC 4x4 / BC7 - 8bpp either way
        return BPP['astc4x4']
    if platform == 'Android' or platform == 'iOS':
        return BPP['astc6x6']
    return BPP['astc4x4']   # desktop Compressed default ~BC-quality 8bpp


def settings_for(flags, overrides, target):
    """Resolve the importer settings a target actually sees."""
    for o in overrides:
        if target in o.get('buildTarget', ''):
            return dict(flags), o
    return dict(flags), None


def fixed_bpp(platform, entry):
    """bpp the same block would carry after the CompressedLQ flip."""
    fixed = dict(entry)
    if fixed.get('textureCompression') == '2':
        fixed['textureCompression'] = '1'
    return bpp_for(platform, fixed)


def collect(args, limit_paths=None):
    """Walk texture metas, build per-target census rows."""
    rows = []
    for base, _, files in os.walk(args.root):
        for name in files:
            if not name.lower().endswith('.meta'):
                continue
            texpath = os.path.join(base, name[:-5])
            if not texpath.lower().endswith(('.png', '.jpg', '.tga', '.tif',
                                             '.tiff', '.exr', '.psd')):
                continue
            rel = os.path.relpath(texpath, args.root)
            if limit_paths is not None and not any(s in rel for s in limit_paths):                continue
            try:
                with Image.open(texpath) as im:
                    width, height = im.size
            except Exception:
                continue
            flags, overrides = parse_meta(os.path.join(base, name))
            base_flags, _ = settings_for(flags, overrides, 'DefaultTexturePlatform')
            cap = int(base_flags.get('maxTextureSize', '0') or 0) or 4096
            lows = [o for o in overrides if o.get('textureCompression') == '2']
            is_low = flags.get('textureCompression') == '2' or bool(lows)
            row = {
                'path': rel,
                'metapath': os.path.join(base, name),
                'shipped': '/Resources/' in texpath or '/Material/' in texpath,
                'w': width, 'h': height,
                'est': {},
                'comp': {},
                'is_low': is_low or bool(lows),
            }
            for target, tag in (('Android', 'mob'), ('Standalone', 'desk')):
                fflags, over = settings_for(flags, overrides, target)
                entry = over or fflags
                mip = entry.get('mipMaps', fflags.get('mipMaps', '1')) == '1'
                entry_cap = int(entry.get('maxTextureSize', fflags.get('maxTextureSize', str(cap)))
                                or cap) or cap
                est = footprint(width, height, entry_cap, bpp_for(target, entry), mip)
                fixed = footprint(width, height, entry_cap, fixed_bpp(target, entry), mip)
                row['est'][tag] = est
                row['est'].setdefault(tag + 'fixed', fixed)
                row['comp'][tag] = COMP.get(entry.get('textureCompression', '?'), '?')
                row['maxsz'] = entry_cap
            rows.append(row)
    return rows


def report_groups(rows):
    import collections
    groups = collections.defaultdict(lambda: {'mob': 0.0, 'desk': 0.0, 'n': 0})
    for r in rows:
        parent = '/'.join(r['path'].split('/')[:4])
        g = groups[parent]
        g['mob'] += r['est']['mob']
        g['desk'] += r['est']['desk']
        g['n'] += 1
    print('%-46s %9s %9s %5s' % ('directory', 'mobile-MB', 'desk-MB', 'n'))
    for gname, g in sorted(groups.items(), key=lambda kv: -kv[1]['mob']):
        print('%-46s %9.1f %9.1f %5d' % (gname, g['mob'], g['desk'], g['n']))


def report_top(rows, top):
    def desk_of(path):
        for r in rows:
            if r['path'] == path:
                return r['est']['desk']
        return float('nan')
    mob_rows = sorted([r for r in rows if 'mob' in r['est']], key=lambda r: -r['est']['mob'])[:top]
    tail = [r for r in rows if r not in mob_rows]
    print('   mobile-MB    desk-MB   disk MB  set           wxh        ship  path')
    for r in mob_rows:
        print('%10.1f   %10.1f %9.1f  %s/%s %5dx%-5d %-4s  %s'
              % (r['est']['mob'], r['est']['desk'],
                 os.path.getsize(r['metapath'][:-5]) / (1024.0 * 1024.0),
                 r['comp']['mob'], r['maxsz'], r['w'], r['h'],
                 'R' if r['shipped'] else '-', r['path']))
    if tail:
        print('  (+%d smaller textures: %.1f MB mobile / %.1f MB desk)'
              % (len(tail),
                 sum(r['est']['mob'] for r in tail),
                 sum(r['est']['desk'] for r in tail)))


def report_fix_lq(rows, apply):
    """About to flip CompressedLQ -> Compressed: what changes where."""
    lq = [r for r in rows if r['is_low']]
    os.makedirs('/tmp/opencode', exist_ok=True)
    before = sum(r['est']['mob'] for r in lq), sum(r['est']['desk'] for r in lq)
    after = sum(r['est']['mobfixed'] for r in lq), sum(r['est']['deskfixed'] for r in lq)
    print('CompressedLQ textures under scope: %d' % len(lq))
    print('mobile  before %7.1f MB -> after %7.1f MB  (saves %5.1f)'
          % (before[0], after[0], before[0] - after[0]))
    print('desktop before %7.1f MB -> after %7.1f MB  (saves %5.1f)'
          % (before[1], after[1], before[1] - after[1]))
    raw_files = 0
    raw_lines = 0
    for r in rows:
        hits = len(re.findall(rb"textureCompression: 2(\r?\n)",
                              open(r['metapath'], 'rb').read()))
        if hits:
            raw_files += 1
            raw_lines += hits
    print('raw regex matches the write pass would flip: %d lines across %d metas' % (raw_lines, raw_files))
    if not apply:
        print('(dry run - pass --apply to write the metas)')
        return
    written = 0
    for r in lq:
        path = r['metapath']
        text = open(path, 'rb').read()
        new, count = re.subn(rb'textureCompression: 2(\r?\n)', rb'textureCompression: 1\1', text)
        if count:
            with open(path, 'wb') as fh:
                fh.write(new)
            written += 1
    print('flipped textureCompression 2 -> 1 in %d metas' % written)
    log = '/tmp/opencode/texbudget_fixlq_paths.txt'
    with open(log, 'w', encoding='utf-8') as fh:
        fh.write('\n'.join(sorted(r['path'] for r in lq)))
        fh.write('\n')
    print('changed paths listed in %s' % log)


def main():
    p = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    p.add_argument('--root', default='Assets')
    p.add_argument('--under', default=None, help='only report path substrings')
    p.add_argument('--top', type=int, default=24)
    p.add_argument('--groups', action='store_true', help='roll costs up per directory')
    p.add_argument('--fix-lq', action='store_true',
                   help='report (or with --apply, flip) CompressedLQ textures')
    p.add_argument('--apply', action='store_true', help='write the --fix-lq flip')
    args = p.parse_args()

    scope = [args.under] if args.under else None
    rows = collect(args, scope)

    if args.fix_lq:
        report_fix_lq(rows, args.apply)
        return

    if args.groups:
        report_groups(rows)
        return

    print('%d textures | mobile estimate %.1f MB | desktop/editor estimate %.1f MB'
          % (len(rows), sum(r['est'].get('mob', 0) for r in rows),
             sum(r['est'].get('desk', 0) for r in rows)))
    print()
    print('   mobile-MB    desk-MB   disk MB  set           wxh        ship  path')
    mob_rows = sorted([r for r in rows if 'mob' in r['est']], key=lambda r: -r['est']['mob'])[:args.top]
    for r in mob_rows:
        print('%10.1f   %10.1f %9.1f  %s/%s/%s %5dx%-5d %-4s  %s'
              % (r['est']['mob'], r['est']['desk'],
                 os.path.getsize(os.path.join(args.root, r['path'])) / (1024.0 * 1024.0),
                 r['comp']['mob'], r['maxsz'], r['w'], r['h'],
                 'R' if r['shipped'] else '-', r['path']))
    shown = {r['path'] for r in mob_rows}
    tail = [r for r in rows if r['path'] not in shown]
    if tail:
        print('  (+%d smaller textures: %.1f MB mobile / %.1f MB desk)'
              % (len(tail),
                 sum(r['est']['mob'] for r in tail),
                 sum(r['est']['desk'] for r in tail)))


if __name__ == '__main__':
    main()
