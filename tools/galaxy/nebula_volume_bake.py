#!/usr/bin/env python3
"""Bake the OpenFrontier nebula placables' 64^3 RGBA32 volumes.

The first-generation bake was a pure radial falloff (alpha peaked at the cube
centre and fell off with r, which raymarched as a glowing BALL). This tool
replaces it with two data paths:

Five ED nebulae (amethyst/dusk/ember/rose/veil) bake from ELITE DANGEROUS'S
OWN 64^3 low-detail volumes - BC7-compressed DDS 3D textures in the game
carve (/home/deck/ed-assets, ~300 KB each; ED also ships 256^3 high LODs at
19 MB - out of the mobile budget). Structure (alpha = density, wisp for wisp)
comes from ED's volume; colour comes from the existing Nebula*.png card: a
luma -> rgb LUT sampled from the card recolours ED's teal-blue data into each
prefab's palette. ED's alpha is column-normalised so every volume integrates
to TARGET_COLUMN cube units through its densest vein, and the cube faces get
a 5% edge guard so no box silhouette can show. The shader carries no shaping
of its own - the bake is the single source of the shape.

The rest of ED's palette bakes with no card: the five DARK volumes are
absorption nebulae (their alpha is the dust, their rgb is only a faint glow
at DARK_EMISSION, and the column is normalised UP to DARK_TARGET so they
actually occlude), and plain bright 04 keeps its own blue-grey hue with the
density left at its natural brightness. Together with the five card bakes
that is every nebula volume ED ships as a placeable.

Two nebulae are AUTHORED (ED ships no named-nebula textures - the carve holds
only generic bright/dark volumes): Barnard's Loop is a broken elliptical ring
in deep red, the Pleiades a compact blue wisp cluster. Their cards are
synthesized here, saved next to the volumes so the flat-card fallback assets
stay in sync, and baked as a wavy noisy SLAB whose xy silhouette is the card:

    density(x,y,z) = card_luma(x,y) * card_alpha(x,y)   (xy silhouette)
                     * gauss(z - wavy(x,y), sigma)      (slab thickness)
                     * value_noise(x,y,z)               (wispy breakup)

Assets (run from the repo root; .bytes/.png written in place, LFS-tracked):

    ~/.venvs/decals/bin/python tools/galaxy/nebula_volume_bake.py
    ~/.venvs/decals/bin/python tools/galaxy/nebula_volume_bake.py dusk pleiades
    ... --preview /tmp/opencode/neb-previews    # top/side integral PNGs

Deterministic: fixed seeds, fixed constants.
"""
import argparse
import os
import re
import struct
import sys

try:
	import numpy as np
	from PIL import Image
except ImportError:
	sys.exit('nebula bake needs pillow+numpy: '
	         '~/.venvs/decals/bin/pip install pillow numpy '
	         '(use ~/.venvs/decals/bin/python to run this tool)')

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
NEB_DIR = os.path.join(REPO, 'Assets', 'OpenFrontier', 'Galaxy', 'Nebulae')
ED_DIR = os.environ.get('ED_TEXTURE_DIR',
                        '/home/deck/ed-assets/carved/textures/texture')

N = 64                 # volume edge; must match GalaxyNebulaVolume.size
CARD = 512             # card edge
TARGET_COLUMN = 0.46   # peak density integral, cube units (old ball: ~0.52)
SIGMA_Z = 0.26         # authored slabs: gaussian sigma, cube units
WAVE = 0.12            # authored slabs: mid-plane displacement, cube units
FACE_FADE = (0.38, 0.495)   # authored slabs: kill density at the cube z faces
BODY_FLOOR = 0.12      # authored slabs: base density so the body isn't hollow
EDGE_GUARD = (0.88, 0.995)  # ED volumes: fade the outer ~5% of every axis
LUMA_BINS = 24         # card colour LUT resolution

LUMA = np.array([0.2126, 0.7152, 0.0722], np.float32)

ED_SOURCES = {
	# prefab    ED low-detail volume (64^3 BC7 DDS in the carve)
	'amethyst': 'eneric_bright_nebula_01_low_volumetexture_tex_e1fd3710__2e342d17_d64.dds',
	'dusk':     'eneric_bright_nebula_02_low_volumetexture_tex_92d79d51__3d42b3cf_d64.dds',
	'ember':    'eneric_bright_nebula_03_low_volumetexture_tex_43b20392__17d5781b_d64.dds',
	'rose':     'eneric_bright_nebula_05_low_volumetexture_tex_a566d014__18f6a071_d64.dds',
	'veil':     'generic_bright_nebula_04_special_low_volumetexture__93bfffd1_d64.dds',
}

# No card for these: colour comes from the volume's own hue. The dark five are
# absorption nebulae - occlusion first, a thin dust glow second.
ED_RAW = {
	'bright04': 'eneric_bright_nebula_04_low_volumetexture_tex_f48c69d3__8d717023_d64.dds',
	'dark01': 'eric_dark_nebula_01_low_volumetexture_tex_52c36c92__a1e6e602_d64.dds',
	'dark02': 'eric_dark_nebula_02_low_volumetexture_tex_039dd2d3__dd1baf70_d64.dds',
	'dark03': 'eric_dark_nebula_03_low_volumetexture_tex_b4783914__8f4d9296_d64.dds',
	'dark04': 'eric_dark_nebula_04_low_volumetexture_tex_65529f55__968b6d20_d64.dds',
	'dark05': 'eric_dark_nebula_05_low_volumetexture_tex_162d0596__6a9c4e71_d64.dds',
}
DARK = ('dark01', 'dark02', 'dark03', 'dark04', 'dark05')
DARK_EMISSION = 0.12   # dust glow, fraction of a bright nebula's emission
DARK_TARGET = 0.55     # dark columns are scaled UP to this optical depth

ED_CARDS = tuple(ED_SOURCES)
ED_ALL = ED_CARDS + tuple(ED_RAW)
AUTHORED = ('barnard', 'pleiades')


# ---------------------------------------------------------------- noise ----
def smoothstep(a, b, x):
	t = np.clip((x - a) / (b - a), 0, 1)
	return t * t * (3 - 2 * t)


def trivol(shape, seeds=(5, 11), weights=(0.65, 0.35)):
	"""Two-octave 3D value noise in [0,1] (trilinear lattice interpolation)."""
	def vnoise(res, rng):
		g = rng.random((res + 1,) * 3).astype(np.float32)
		axis = [np.arange(n, dtype=np.float32) / (n - 1) * res for n in shape]
		Z, Y, X = np.meshgrid(*axis, indexing='ij')
		out = np.zeros(shape, np.float32)
		for cz in (0, 1):
			for cy in (0, 1):
				for cx in (0, 1):
					wz = np.where(cz, Z % 1, 1 - Z % 1)
					wy = np.where(cy, Y % 1, 1 - Y % 1)
					wx = np.where(cx, X % 1, 1 - X % 1)
					wz = np.where(Z >= res, float(cz), wz)
					wy = np.where(Y >= res, float(cy), wy)
					wx = np.where(X >= res, float(cx), wx)
					iz = np.minimum(np.floor(Z).astype(int) + cz, res)
					iy = np.minimum(np.floor(Y).astype(int) + cy, res)
					ix = np.minimum(np.floor(X).astype(int) + cx, res)
					out += g[iz, iy, ix] * (wz * wy * wx)
		return out
	rng = np.random.default_rng(seeds[0])
	n = vnoise(seeds[0], rng) * weights[0]
	rng = np.random.default_rng(seeds[0] + 1)
	n += vnoise(seeds[1], rng) * weights[1]
	return n / (weights[0] + weights[1])


def trinoise2d(size, res, seed):
	"""2D value noise in [0,1] via PIL bilinear upscale (card authoring)."""
	rng = np.random.default_rng(seed)
	g = (rng.random((res + 2, res + 2)) * 255).astype(np.uint8)
	img = Image.fromarray(g, 'L').resize((size, size), Image.BILINEAR)
	return np.asarray(img, np.float32) / 255.0


# --------------------------------------------------------- ED volume path ----
def decode_ed_dds(path):
	"""64^3 BC7 DDS -> (64,64,64,4) uint8 [z, y, x] RGBA."""
	import texture2ddecoder
	d = open(path, 'rb').read()
	size, flags, h, w, pitch, depth = struct.unpack_from('<6I', d, 4)
	if (w, h, depth) != (N, N, N):
		sys.exit(f'{os.path.basename(path)}: expected {N}^3, got {w}x{h}x{depth}')
	stride = (w // 4) * (h // 4) * 16          # BC7: 16 B per 4x4 block
	data = d[148:148 + stride * depth]         # base mip only
	vol = np.empty((depth, h, w, 4), np.uint8)
	for z in range(depth):
		vol[z] = np.frombuffer(
			texture2ddecoder.decode_bc7(data[z * stride:(z + 1) * stride], w, h),
			np.uint8).reshape(h, w, 4)
	return vol


def card_lut(card_png):
	"""Luma -> rgb LUT from a card: the palette identity of a prefab."""
	img = np.asarray(Image.open(card_png).convert('RGBA'), np.float32) / 255.0
	mask = img[..., 3] > 0.6
	luma = (img[..., :3] @ LUMA)[mask]
	rgb = img[..., :3][mask]
	bins = np.clip((luma * LUMA_BINS).astype(int), 0, LUMA_BINS - 1)
	lut = np.zeros((LUMA_BINS, 3), np.float32)
	count = np.zeros(LUMA_BINS, np.float32)
	for b in range(LUMA_BINS):
		sel = bins == b
		if sel.any():
			lut[b] = rgb[sel].mean(axis=0)
			count[b] = sel.sum()
	# fill empty bins from the nearest populated one
	idx = np.where(count > 0)[0]
	if len(idx) == 0:
		sys.exit(f'{card_png}: no opaque pixels for the colour LUT')
	for b in range(LUMA_BINS):
		if count[b] == 0:
			lut[b] = lut[idx[np.abs(idx - b).argmin()]]
	return lut, idx


def bake_ed(name, dds_name, card_png=None):
	"""ED volume structure + optional card palette -> (RGBA32 bytes, stats)."""
	vol = decode_ed_dds(os.path.join(ED_DIR, dds_name))
	alpha = vol[..., 3].astype(np.float32) / 255.0

	# edge guard: no box silhouette even if ED's data touches a face
	axis = (np.arange(N, dtype=np.float32) / (N - 1)) * 2 - 1
	guard = 1 - smoothstep(EDGE_GUARD[0], EDGE_GUARD[1], np.abs(axis))
	guard3 = guard[:, None, None] * guard[None, :, None] * guard[None, None, :]
	alpha *= guard3

	dark = name in DARK
	dens = alpha

	# column normalise: card bakes keep ED's natural brightness (capped at the
	# optical budget), dark dust is scaled UP until it actually occludes.
	peak = dens.sum(axis=0).max() / N
	if peak > 0:
		if dark:
			dens *= DARK_TARGET / peak
		else:
			dens *= min(TARGET_COLUMN / peak, 1.0)

	intensity = vol[..., :3].astype(np.float32) / 255.0 @ LUMA
	if card_png and os.path.isfile(card_png):
		# voxel colour: ED intensity -> the card's palette
		lut, idx = card_lut(card_png)
		lo, hi = np.percentile(intensity[alpha > 0.02], (2, 98)) \
			if (alpha > 0.02).any() else (0.0, 1.0)
		inorm = np.clip((intensity - lo) / max(hi - lo, 1e-4), 0, 1)
		binc = np.clip((inorm * (LUMA_BINS - 1)).round().astype(int),
		               0, LUMA_BINS - 1)
		rgb = lut[binc]
	else:
		# no card: the volume's own hue. Bright 04 sells its colour at full
		# strength; the dark five are dust, so their glow rides the density
		# at DARK_EMISSION and the absorption term does the real work.
		src = vol[..., :3].astype(np.float32) / 255.0
		sel = alpha > 0.02
		hue = src[sel].mean(axis=0) if sel.any() else np.full(3, 0.6, np.float32)
		hue = np.clip(hue / max(float(hue.max()), 1e-6), 0, 1)
		if dark:
			t = dens / max(float(dens.max()), 1e-6)
			rgb = (t[..., None] ** 0.8) * hue * DARK_EMISSION
		else:
			lo, hi = np.percentile(intensity[alpha > 0.02], (2, 98)) \
				if (alpha > 0.02).any() else (0.0, 1.0)
			inorm = np.clip((intensity - lo) / max(hi - lo, 1e-4), 0, 1)
			rgb = inorm[..., None] * hue

	out = np.zeros((N, N, N, 4), np.uint8)
	out[..., :3] = np.clip(rgb * 255, 0, 255)
	out[..., 3] = np.clip(dens * 255, 0, 255)
	stats = {'peak_column': float(dens.sum(axis=0).max() / N),
	         'nonzero': float((dens > 1 / 255).mean())}
	return out, stats


# ---------------------------------------------------------- authored path ----
def _ramp(dens, lo, hi, gamma):
	t = np.clip(dens, 0, 1) ** gamma
	c = np.empty(dens.shape + (3,), np.float32)
	for i in range(3):
		c[..., i] = lo[i] + (hi[i] - lo[i]) * t
	return c


def card_barnard():
	"""Barnard's Loop: a broken elliptical ring, deep red, faint."""
	size = CARD
	yy, xx = np.mgrid[0:size, 0:size].astype(np.float32)
	dens = np.zeros((size, size), np.float32)
	for (a, b, cx, cy, phi, t0, t1, sigma, amp) in (
			(200, 158, 256, 252, 0.50, -0.55 * np.pi, 1.45 * np.pi, 22, 1.00),
			(164, 130, 268, 244, 0.55, 0.15 * np.pi, 1.25 * np.pi, 10, 0.35),
	):
		tt = np.linspace(t0, t1, 260)
		px = cx + a * np.cos(tt) * np.cos(phi) - b * np.sin(tt) * np.sin(phi)
		py = cy + a * np.cos(tt) * np.sin(phi) + b * np.sin(tt) * np.cos(phi)
		# arc-length taper: fade the last 12% of each end, brightness wave
		taper = np.minimum(smoothstep(0, 0.12, (tt - t0) / (t1 - t0)),
		                   smoothstep(0, 0.12, (t1 - tt) / (t1 - t0)))
		wave = 0.55 + 0.45 * np.clip(np.sin(tt * 1.3 + 0.6), 0, 1)
		for lo in range(0, size, 64):
			hi = min(lo + 64, size)
			dx = xx[lo:hi, :, None] - px[None, None, :]
			dy = yy[lo:hi, :, None] - py[None, None, :]
			d2 = dx * dx + dy * dy
			nearest = d2.min(axis=2)
			along = d2.argmin(axis=2)          # nearest arc sample per pixel
			dens[lo:hi] += amp * np.exp(-nearest / (sigma * sigma)) \
				* taper[along] * wave[along]
	dens *= 0.7 + 0.3 * trinoise2d(size, 12, 7301)
	rgb = _ramp(dens, (108, 26, 20), (255, 122, 86), 0.7)
	alpha = np.clip(dens * 240, 0, 255)
	return rgb, alpha.astype(np.uint8)


def card_pleiades():
	"""The Pleiades: a compact blue wisp cluster with streaks and knots."""
	size = CARD
	yy, xx = np.mgrid[0:size, 0:size].astype(np.float32)
	rng = np.random.default_rng(5407)
	dens = np.zeros((size, size), np.float32)

	def gauss(cx, cy, sx, sy, ang, amp):
		ca, sa = np.cos(ang), np.sin(ang)
		u = (xx - cx) * ca + (yy - cy) * sa
		v = -(xx - cx) * sa + (yy - cy) * ca
		dens[...] += amp * np.exp(-(u * u / (2 * sx * sx)
		                            + v * v / (2 * sy * sy)))

	dens += 1.00 * np.exp(-((xx - 256) ** 2 + (yy - 256) ** 2) / (2 * 26 ** 2))
	dens += 0.80 * np.exp(-((xx - 256) ** 2 + (yy - 256) ** 2) / (2 * 10 ** 2))
	for i in range(6):
		ang = i * np.pi / 6 + rng.uniform(-0.22, 0.22)
		ln = rng.uniform(60, 115)
		dist = rng.uniform(35, 75)
		cx = 256 + np.cos(ang) * dist
		cy = 256 + np.sin(ang) * dist
		gauss(cx, cy, ln * 0.5, rng.uniform(9, 15), ang, rng.uniform(0.45, 0.8))
		# knot at the streak's far end
		ke = rng.uniform(0.4, 0.75)
		gauss(cx + np.cos(ang) * ln * ke, cy + np.sin(ang) * ln * ke,
		      9, 9, ang, 0.5)
	dens *= 0.75 + 0.25 * trinoise2d(size, 16, 5407)
	rgb = _ramp(dens, (104, 142, 228), (225, 240, 255), 0.8)
	alpha = np.clip(dens * 255, 0, 255)
	return rgb, alpha.astype(np.uint8)


def bake_slab(rgb, alpha):
	"""Authored card -> wavy noisy slab volume (64^3 RGBA32, stats)."""
	full = np.concatenate([np.clip(rgb * 255, 0, 255).astype(np.uint8),
	                       alpha[..., None]], axis=2)
	down = Image.fromarray(full, 'RGBA').resize((N, N), Image.BILINEAR)
	c = np.asarray(down, np.float32) / 255.0
	rgb64, a64 = c[..., :3], c[..., 3]

	# xy structure: the card's veins are 5-20 px and an 8x bilinear mean
	# washes them out, so density samples a BLOCK-PERCENTILE of the full-res
	# luma (85th: filaments survive, haze does not), contrast-stretched
	# against the card's own gas range.
	lum512 = (rgb * LUMA).sum(axis=-1)
	blocks = lum512.reshape(N, CARD // N, N, CARD // N)
	luma_p = np.percentile(blocks, 85, axis=(1, 3))
	gas = a64 > 0.5
	lo, hi = np.percentile(luma_p[gas], (5, 97))
	lum = np.clip((luma_p - max(lo, 1e-4)) / max(hi - lo, 1e-4), 0, 1)

	env = a64                                   # the card's round cutout
	dens_xy = env * (BODY_FLOOR + (1 - BODY_FLOOR) * lum ** 0.9)

	xs = np.arange(N, dtype=np.float32)
	Z, Y, X = [g / (N - 1) for g in np.meshgrid(xs, xs, xs, indexing='ij')]
	wavy = trinoise2d(N, 6, 42) * 2 - 1
	zz = Z - 0.5 + WAVE * wavy
	gz = np.exp(-zz * zz / (2 * SIGMA_Z * SIGMA_Z))
	gz *= 1 - smoothstep(FACE_FADE[0], FACE_FADE[1], np.abs(zz))
	n3 = trivol((N, N, N))
	xy = (np.rint(Y * (N - 1)).astype(int) * N
	      + np.rint(X * (N - 1)).astype(int))       # flat card-slice index
	dens = dens_xy.ravel()[xy] * gz * (0.42 + 0.58 * n3)

	column = dens.sum(axis=0) / N              # cube units
	peak = column.max()
	if peak > 0:
		dens *= min(TARGET_COLUMN / peak, 1.0) / 1.0

	rgbv = rgb64.reshape(-1, 3)[xy] * env.ravel()[xy][..., None]
	out = np.zeros((N, N, N, 4), np.uint8)
	out[..., 0] = np.clip(rgbv[..., 0] * 255, 0, 255)
	out[..., 1] = np.clip(rgbv[..., 1] * 255, 0, 255)
	out[..., 2] = np.clip(rgbv[..., 2] * 255, 0, 255)
	out[..., 3] = np.clip(dens * 255, 0, 255)
	return out, {'peak_column': float(dens.sum(axis=0).max() / N),
	             'nonzero': float((dens > 1 / 255).mean())}


# --------------------------------------------------------------- plumbing ----
def save_bytes(path, out):
	with open(path, 'wb') as f:
		f.write(out.tobytes())   # x fastest, then y, then z


def save_card(path, rgb, alpha):
	rgba = np.concatenate([np.clip(rgb, 0, 255).astype(np.uint8),
	                       alpha[..., None]], axis=2)
	Image.fromarray(rgba, 'RGBA').save(path)


def save_preview(path, out):
	"""Column integrals: what the map (top) and in-sky (side) cameras see."""
	col = out[..., 3].astype(np.float32).sum(axis=0)      # (y, x)
	side = out[..., 3].astype(np.float32).sum(axis=1)     # (y, z)
	for img in (col, side):
		img /= max(img.max(), 1e-6)
	px = 256
	for name, img in (('top', col), ('side', side)):
		Image.fromarray((img * 255).astype(np.uint8), 'L') \
			.resize((px, px), Image.NEAREST).save(f'{path}-{name}.png')


def main():
	ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
	ap.add_argument('names', nargs='*',
	                help=f"nebulae to bake; default all: {ED_ALL + AUTHORED}")
	ap.add_argument('--preview', metavar='DIR',
	                help='also write top/side integral PNGs (scratch)')
	args = ap.parse_args()
	names = args.names or list(ED_ALL + AUTHORED)
	bad = [n for n in names if n not in ED_ALL + AUTHORED]
	if bad:
		sys.exit(f'unknown nebula name(s): {", ".join(bad)}')

	for name in names:
		stem = f'Nebula{name.capitalize()}'
		card_path = os.path.join(NEB_DIR, f'{stem}.png')
		if name in AUTHORED:
			rgb, alpha = (card_barnard if name == 'barnard' else card_pleiades)()
			save_card(card_path, rgb, alpha)
			out, stats = bake_slab(rgb, alpha)
			how = 'slab'
		else:
			dds = ED_SOURCES.get(name) or ED_RAW[name]
			if not os.path.isfile(os.path.join(ED_DIR, dds)):
				sys.exit(f'{name}: ED volume not found: {ED_DIR}/{dds}\n'
				         '(the carve lives at /home/deck/ed-assets)')
			out, stats = bake_ed(name, dds,
			                     card_path if name in ED_SOURCES else None)
			how = 'ed volume' if name in ED_SOURCES else 'ed volume, own hue'
		save_bytes(os.path.join(NEB_DIR, f'{stem}.bytes'), out)
		line = (f'{name:9s} -> {stem}.bytes [{how}]  peak column '
		        f'{stats["peak_column"]:.2f} cu, gas voxels '
		        f'{stats["nonzero"] * 100:.0f}%')
		if args.preview:
			save_preview(os.path.join(args.preview, stem), out)
			line += f'  previews in {args.preview}'
		print(line)


if __name__ == '__main__':
	main()
