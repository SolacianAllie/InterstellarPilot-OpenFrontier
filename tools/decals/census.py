#!/usr/bin/env python3
"""Census the marks in a decal sheet.

Every isolated mark (connected alpha==255 component) gets an exact
bounding box in sheet coordinates, classified by the generation window
it would need (1K / 1.5K / 2K+). The census is what the quantize/place
loop and any future batch runner plan against.

Output: a JSON census {sheet, size, marks:[{id,bbox,area,fit}]} and,
with --crops, the per-mark crop windows (bbox + margin) saved as PNGs,
ready to drop into Krita or hand to a ComfyUI job.
"""
import argparse, json, os, sys, collections

try:
    import numpy as np
    from PIL import Image
except ImportError:
    sys.exit('decals tools need pillow+numpy:  '
             '~/.venvs/decals/bin/pip install pillow numpy   '
             '(use tools/decals/run.sh to pick that venv up)')


def mark_census(alpha, stride=8, thresh=128, min_px=200):
    """Connected alpha components at `stride`, refined to exact bboxes."""
    h, w = alpha.shape
    gw, gh = max(1, w // stride), max(1, h // stride)
    grid = (alpha[::stride, ::stride] > thresh).astype(np.uint8)[:gh, :gw]
    seen = np.zeros_like(grid, dtype=bool)
    marks = []
    for sy in range(gh):
        for sx in range(gw):
            if not grid[sy, sx] or seen[sy, sx]:
                continue
            seen[sy, sx] = True
            q = collections.deque([(sx, sy)])
            x0s = x1s = sx; y0s = y1s = sy; cells = 0
            while q:
                cx, cy = q.popleft(); cells += 1
                x0s = min(x0s, cx); x1s = max(x1s, cx)
                y0s = min(y0s, cy); y1s = max(y1s, cy)
                for dy in (-1, 0, 1):
                    for dx in (-1, 0, 1):
                        nx, ny = cx + dx, cy + dy
                        if 0 <= nx < gw and 0 <= ny < gh and grid[ny, nx] and not seen[ny, nx]:
                            seen[ny, nx] = True
                            q.append((nx, ny))
            # refine to exact bbox at full res (pad the window a little)
            px0 = max(0, x0s * stride - stride)
            py0 = max(0, y0s * stride - stride)
            px1 = min(w, (x1s + 1) * stride + stride)
            py1 = min(h, (y1s + 1) * stride + stride)
            win = alpha[py0:py1, px0:px1] > thresh
            ys, xs = np.nonzero(win)
            if xs.size == 0:
                continue
            bx0, bx1 = px0 + xs.min(), px0 + xs.max() + 1
            by0, by1 = py0 + ys.min(), py0 + ys.max() + 1
            area = int(win.sum())
            if area < min_px:
                continue
            bw, bh = bx1 - bx0, by1 - by0
            side = max(bw, bh)
            fit = '1K' if side <= 1024 else ('1.5K' if side <= 1536 else
                 ('2K' if side <= 2048 else '2K+'))
            marks.append({'bbox': [int(bx0), int(by0), int(bx1), int(by1)],
                          'area': area, 'fit': fit})
    marks.sort(key=lambda m: (m['bbox'][1], m['bbox'][0]))
    for i, m in enumerate(marks):
        m['id'] = 'mark_%03d' % i
    return marks


def load_alpha(path):
    img = Image.open(path).convert('RGBA')
    a = np.asarray(img, dtype=np.uint8)[:, :, 3]
    return img, a


def main(argv=None):
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('sheets', nargs='+', help='decal sheet PNG(s)')
    p.add_argument('--json', help='write census JSON here (one file per sheet if several)')
    p.add_argument('--crops', help='directory to write per-mark crop PNGs into')
    p.add_argument('--margin', type=int, default=96,
                   help='context margin around each crop (default 96)')
    p.add_argument('--stride', type=int, default=8)
    p.add_argument('--min-px', type=int, default=200,
                   help='ignore components below this full-res pixel area')
    args = p.parse_args(argv)

    any_wrote = False
    for path in args.sheets:
        img, alpha = load_alpha(path)
        marks = mark_census(alpha, stride=args.stride, min_px=args.min_px)
        bins = {}
        for m in marks:
            bins[m['fit']] = bins.get(m['fit'], 0) + 1
        cover = sum(m['area'] for m in marks) / alpha.size
        print('%s: %d marks  coverage=%.1f%%  fit=%s'
              % (os.path.basename(path), len(marks), 100 * cover,
                 ', '.join('%s=%d' % kv for kv in sorted(bins.items())) or 'none'))
        if args.json:
            jpath = args.json
            if len(args.sheets) > 1:
                base = os.path.splitext(os.path.basename(path))[0]
                jpath = '%s_%s.json' % (os.path.splitext(args.json)[0], base)
            doc = {'sheet': path, 'width': img.width, 'height': img.height,
                   'stride': args.stride, 'min_px': args.min_px, 'marks': marks}
            with open(jpath, 'w') as f:
                json.dump(doc, f, indent=1)
            print('  census -> %s' % jpath)
            any_wrote = True
        if args.crops:
            os.makedirs(args.crops, exist_ok=True)
            base = os.path.splitext(os.path.basename(path))[0]
            w, h = img.width, img.height
            for m in marks:
                x0, y0, x1, y1 = m['bbox']
                cx0, cy0 = max(0, x0 - args.margin), max(0, y0 - args.margin)
                cx1, cy1 = min(w, x1 + args.margin), min(h, y1 + args.margin)
                crop = img.crop((cx0, cy0, cx1, cy1))
                op = os.path.join(args.crops, '%s_%s.png' % (base, m['id']))
                crop.save(op)
                m['crop'] = op
            print('  %d crops -> %s (margin %d)' % (len(marks), args.crops, args.margin))
    return 0 if (marks or not args.json) else 0


if __name__ == '__main__':
    sys.exit(main())
