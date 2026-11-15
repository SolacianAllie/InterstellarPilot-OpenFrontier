#!/usr/bin/env python3
"""Bake the OpenFrontier nebula placables' RGBA32 volumes, one file per LOD.

The first-generation bake was a pure radial falloff (alpha peaked at the cube
centre and fell off with r, which raymarched as a glowing BALL). This tool
replaces it with ED's own data, in three paths:

Five bright volumes use ED's structure and a card palette. Structure (alpha =
density, wisp for wisp) comes from the game's own BC7 DDS volume: 64^3 "low"
(~300 KB) and 256^3 "high" (19 MB). Colour comes from the existing
Nebula*.png card: a luma -> rgb LUT sampled from the card recolours ED's
teal-blue data into each prefab's palette. The density is ED's OWN - the
companion-ramp gain described under the named path below multiplies the
surviving alpha exactly as the game applies it, and the peak rescale
carries the absolute scale; there is no column normalisation. The cube
faces get a 5% edge guard so no box silhouette can show. The shader
carries no shaping of its own - the bake is the single source of the
shape.

Six volumes keep their own hue. The five DARK volumes are absorption
nebulae (their alpha is the dust, their rgb is only a faint glow at
DARK_EMISSION, and ED's own 18.961x gain is what makes them occlude - the
old DARK_TARGET scaling is retired); plain bright 04 keeps its own
blue-grey hue, also on ED's own gain.

Every ED volume, generic and named, gets the same threefold shaping: the
low-alpha floor cut, ED's own per-volume density gain, and the 5% edge
fade - then the peak rescale for the 8-bit channel.

The 18 named nebulae with a dedicated volume are ED's OWN data, not authored
shapes. The game's NebulaDatabase maps each display name to a High/Low pair;
DJB2("<name>_volumetexture") is the texture UID, which recovers the files
whose carved names lost their leading letters. Barnard's Loop and the
Pleiades were the first two; the other sixteen are the same mapping. Red
Spider is deliberately absent - its DB row points at
Generic_PlanataryNebula_06, so it has no unique volume. The named bakes are
RAW in colour and structure: no column normalisation and no card LUT. The
shaping is threefold:

1. the low-alpha continuum cut (DENSITY_FLOOR, scaled to each volume's own
   core) - ED's alpha carries a 1-10% background around the actual gas, and
   raw the raymarch integrates it over the whole cube, so Barnard's thin
   shell read as a filled sphere with its rim buried in haze;
2. a 5% edge fade so no box silhouette can show;
3. ED's OWN per-volume density gain. Every ED volume texture ships with a
   paired 256x1 RGBA32F transfer slice (same carve hash, R32 ramp + BC7 cube
   share the volume's identity) whose ALPHA channel is the game's density
   gain over raw alpha: 4.957x for sixteen of the named nebulae, 1.0x for
   the translucent Pleiades and Fine Ring, 18.961x for Horsehead's dust.
   The ramp's last bins (249-255) spike to ~192 on under 0.1% of voxels -
   dense-core speckle ED's own render never shows - so the applied gain
   clamps to the bins 0-248 median. The gain pushes per-voxel density past
   1 (alpha 0.75 x 4.96), which the 8-bit alpha channel cannot hold, so the
   bake stores density/K, K = the volume's own peak density, and the
   prefab's component carries `density: K` to restore ED's absolute scale
   in the shader. The ramp's RGB (a dimmed, level-stepped
   version of the volume's own hue) is NOT baked: it is ED's emission
   levelling, whose absolute scale is not recoverable from the carve, so
   each gained prefab carries brightness 4/gain on its GalaxyNebulaVolume
   instead - the validated emission level stays, the opacity becomes ED's.

BC7 decodes as BGRA, so the
channel order is the only colour transform. The authored card functions stay
in this file for reference; the committed Cards/*.png are the flat fallbacks,
and the volume path no longer calls them.

Assets (run from the repo root; .bytes/.png written in place, LFS-tracked):

    ~/.venvs/decals/bin/python tools/galaxy/nebula_volume_bake.py
    ~/.venvs/decals/bin/python tools/galaxy/nebula_volume_bake.py dusk pleiades
    ~/.venvs/decals/bin/python tools/galaxy/nebula_volume_bake.py barnard pleiades --sizes 64 128 256
    ... --sizes 256                              # bake the HQ LOD
    ... --preview /tmp/opencode/neb-previews    # top/side integral PNGs

Output layout: Nebulae/Volumes/LOD<edge>/Nebula<Name>.bytes, cards in
Nebulae/Cards/. Deterministic: fixed seeds, fixed constants.
"""
import argparse
import csv
import json
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
# Bake -> prefab handoff: each named volume's peak density K (see bake_raw).
DENSITY_JSON = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                            'ed_density.json')
NEB_DIR = os.path.join(REPO, 'Assets', 'OpenFrontier', 'Galaxy', 'Nebulae')
CARD_DIR = os.path.join(NEB_DIR, 'Cards')
VOL_DIR = os.path.join(NEB_DIR, 'Volumes')
ED_ROOT = os.environ.get('ED_TEXTURE_ROOT',
                         '/home/deck/ed-assets/carved/textures')
# The carve catalog: sha1 -> carved DDS path. ED ships a 256x1 RGBA32F
# transfer slice beside every volume texture; its row shares the volume's
# own hash, which is the carve filename's last hash token.
ED_CATALOG = os.environ.get('ED_TEXTURE_CATALOG',
                            '/home/deck/ed-assets/catalog/carved_textures.csv')

N = 64                 # default volume edge; --sizes picks the LOD folders
CARD = 512             # card edge
AUTHORED_COLUMN = 0.34  # peak density integral for the two hand-built nebulae
EDGE_GUARD = (0.88, 0.995)  # fade the outer ~5% of every axis
# ED's volumes carry a low-alpha continuum around the real gas. At 5% and
# below it is invisible in isolation, but a raymarch integrates it across the
# whole cube: Barnard's Loop became a red ball with a bright seam rather than
# a thin shell. The floor is the larger of this absolute minimum and a
# fraction of the volume's own bright core, so a dim planetary's halo and a
# bright shell's halo are cut by the same proportion rather than the same
# absolute alpha. Zero below `lo`, identity above `hi`, smooth between. Every
# bake cuts raw alpha.
DENSITY_FLOOR = (0.05, 0.15)
DENSITY_FLOOR_REL = (0.12, 0.32)
LUMA_BINS = 24         # card colour LUT resolution

LUMA = np.array([0.2126, 0.7152, 0.0722], np.float32)

# Every volume ED ships, both native LODs, as paths under ED_ROOT. Note the
# mangled carve names (a leading "ge"/"g" was lost) - copy them verbatim.
# Bright 01's HIGH LOD is the one high file that landed in textures/model.
# A 128 edge is downsampled from the 256 source (ED ships no native 128).
ED_SOURCES = {
	'amethyst': {
		'64': 'texture/eneric_bright_nebula_01_low_volumetexture_tex_e1fd3710__2e342d17_d64.dds',
		'256': 'model/generic_bright_nebula_01_high_volumetexture__aeae3f52_d256.dds',
	},
	'dusk': {
		'64': 'texture/eneric_bright_nebula_02_low_volumetexture_tex_92d79d51__3d42b3cf_d64.dds',
		'256': 'texture/generic_bright_nebula_02_high_volumetexture__5b0da39f_d256.dds',
	},
	'ember': {
		'64': 'texture/eneric_bright_nebula_03_low_volumetexture_tex_43b20392__17d5781b_d64.dds',
		'256': 'texture/generic_bright_nebula_03_high_volumetexture__8f25f399_d256.dds',
	},
	'rose': {
		'64': 'texture/eneric_bright_nebula_05_low_volumetexture_tex_a566d014__18f6a071_d64.dds',
		'256': 'texture/generic_bright_nebula_05_high_volumetexture__63182c2c_d256.dds',
	},
	'veil': {
		'64': 'texture/generic_bright_nebula_04_special_low_volumetexture__93bfffd1_d64.dds',
		'256': 'texture/generic_bright_nebula_04_special_high_volumetexture__a80dda3a_d256.dds',
	},
}

# No card for these: colour comes from the volume's own hue. The dark five are
# absorption nebulae - occlusion first, a thin dust glow second.
ED_RAW = {
	'bright04': {
		'64': 'texture/eneric_bright_nebula_04_low_volumetexture_tex_f48c69d3__8d717023_d64.dds',
		'256': 'texture/generic_bright_nebula_04_high_volumetexture__cb33619e_d256.dds',
	},
	'dark01': {
		'64': 'texture/eric_dark_nebula_01_low_volumetexture_tex_52c36c92__a1e6e602_d64.dds',
		'256': 'texture/neric_dark_nebula_01_high_volumetexture_tex_7a557da0__a9496aed_d256.dds',
	},
	'dark02': {
		'64': 'texture/eric_dark_nebula_02_low_volumetexture_tex_039dd2d3__dd1baf70_d64.dds',
		'256': 'texture/neric_dark_nebula_02_high_volumetexture_tex_467cac01__99be5f11_d256.dds',
	},
	'dark03': {
		'64': 'texture/eric_dark_nebula_03_low_volumetexture_tex_b4783914__8f4d9296_d64.dds',
		'256': 'texture/neric_dark_nebula_03_high_volumetexture_tex_12a3da62__939c7813_d256.dds',
	},
	'dark04': {
		'64': 'texture/eric_dark_nebula_04_low_volumetexture_tex_65529f55__968b6d20_d64.dds',
		'256': 'texture/neric_dark_nebula_04_high_volumetexture_tex_decb08c3__804fb4fc_d256.dds',
	},
	'dark05': {
		'64': 'texture/eric_dark_nebula_05_low_volumetexture_tex_162d0596__6a9c4e71_d64.dds',
		'256': 'texture/neric_dark_nebula_05_high_volumetexture_tex_aaf23724__4724cda6_d256.dds',
	},
}
DARK = ('dark01', 'dark02', 'dark03', 'dark04', 'dark05')
DARK_EMISSION = 0.12   # dust glow, fraction of a bright nebula's emission

# ED'S NAMED NEBULA VOLUMES. The game's NebulaDatabase maps each display name
# to a High/Low volume name; DJB2("<name>_volumetexture") is the texture UID,
# which recovers the files whose carved names lost their leading letters.
# 18 named entries have a dedicated pair. Red Spider is deliberately absent:
# its row points at Generic_PlanataryNebula_06, so it has no unique volume.
# UIDs: BarnardsLoop 5f8f01d3 / 51f3b665; BrightNebula_02 167cb384 / 022975f6.
# These are RAW sources: bake_raw keeps the game's RGB and surviving alpha.
ED_NAMED = {
	'barnard': {
		'64': 'texture/low_volumetexture_tex_51f3b665__eee3bffb_d64.dds',
		'256': 'texture/_high_volumetexture_tex_5f8f01d3__d2718b95_d256.dds',
	},
	'pleiades': {
		'64': 'texture/la_02_low_volumetexture_tex_022975f6__f36b7947_d64.dds',
		'256': 'texture/ula_02_high_volumetexture_tex_167cb384__00e7374e_d256.dds',
	},
	'bowtie': {
		'64': 'other/xture_tex_f646dfa8__af9be05d_d64.dds',
		'256': 'model/texture_tex_8e475376__5f49011c_d256.dds',
	},
	'bubble': {
		'64': 'other/xture_tex_f0f2670a__e435da83_d64.dds',
		'256': 'texture/texture_tex_de63c718__afcd1635_d256.dds',
	},
	'butterfly': {
		'64': 'texture/lumetexture_tex_31fe073f__209e60dd_d64.dds',
		'256': 'model/volumetexture_tex_40e36ded__737422d0_d256.dds',
	},
	'catseye': {
		'64': 'texture/texture_tex_3ee72a8c__126d363d_d64.dds',
		'256': 'texture/metexture_tex_eaf0fada__fcccb6d4_d256.dds',
	},
	'finering': {
		'64': 'texture/metexture_tex_cf773a30__10c7bdbf_d64.dds',
		'256': 'texture/lumetexture_tex_8d82fefe__e2764887_d256.dds',
	},
	'lemonslice': {
		'64': 'texture/volumetexture_tex_2eaa8509__cbdea0b4_d64.dds',
		'256': 'texture/h_volumetexture_tex_d31fa4f7__133047b0_d256.dds',
	},
	'messier78': {
		'64': 'texture/lumetexture_tex_8a26e965__93f774c4_d64.dds',
		'256': 'texture/volumetexture_tex_9e2894d3__1e74a627_d256.dds',
	},
	'ngc6337': {
		'64': 'texture/metexture_tex_2bf92788__d46f6492_d64.dds',
		'256': 'texture/lumetexture_tex_7a429756__499fabbe_d256.dds',
	},
	'orion': {
		'64': 'other/ure_tex_de9ae6c5__5445836b_d64.dds',
		'256': 'other/xture_tex_811c3e33__50734c59_d256.dds',
	},
	'ring': {
		'64': 'other/tex_8afd3e4e__099b4a61_d64.dds',
		'256': 'other/ure_tex_b9c986dc__d3c7890b_d256.dds',
	},
	'veilwest': {
		'64': 'texture/est_low_volumetexture_tex_94229da8__2f0c45c3_d64.dds',
		'256': 'texture/west_high_volumetexture_tex_e79ad176__af664170_d256.dds',
	},
	'flame': {
		'64': 'other/ure_tex_cc4ac923__0dc70e47_d64.dds',
		'256': 'other/xture_tex_24c86c51__e2193f2a_d256.dds',
	},
	'crab': {
		'64': 'other/tex_da185336__77f5d3ab_d64.dds',
		'256': 'other/ure_tex_ec4738c4__29735342_d256.dds',
	},
	'hind': {
		'64': 'other/tex_fdb32141__b79b92fb_d64.dds',
		'256': 'other/ure_tex_833bc82f__949b5d0f_d256.dds',
	},
	'horsehead': {
		'64': 'texture/d_cluster_low_volumetexture_tex_d6e95a92__4098596f_d64.dds',
		'256': 'texture/ad_cluster_high_volumetexture_tex_83392ba0__57c1d716_d256.dds',
	},
	'lbn623': {
		'64': 'texture/texture_tex_690c2c14__f4139cbf_d64.dds',
		'256': 'texture/metexture_tex_59b62d62__11ef23fc_d256.dds',
	},
}

# Readable asset stems. The bake writes Nebula<...>.bytes under these names and
# the prefabs use the same stem, so the folder reads like the sky.
STEM = {
	'barnard': 'NebulaBarnard',
	'pleiades': 'NebulaPleiades',
	'bowtie': 'NebulaBowTie',
	'bubble': 'NebulaBubble',
	'butterfly': 'NebulaButterfly',
	'catseye': 'NebulaCatsEye',
	'finering': 'NebulaFineRing',
	'lemonslice': 'NebulaLemonSlice',
	'messier78': 'NebulaMessier78',
	'ngc6337': 'NebulaNGC6337',
	'orion': 'NebulaOrion',
	'ring': 'NebulaRing',
	'veilwest': 'NebulaVeilWest',
	'flame': 'NebulaFlame',
	'crab': 'NebulaCrab',
	'hind': 'NebulaHind',
	'horsehead': 'NebulaHorsehead',
	'lbn623': 'NebulaLBN623',
}

ED_CARDS = tuple(ED_SOURCES)
ED_ALL = ED_CARDS + tuple(ED_RAW) + tuple(ED_NAMED)

# Retired hand-built volumes. The two named prefabs now carry ED's own data;
# these functions stay only so the flat-card fallbacks can be regenerated.
AUTHORED = ()


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
	"""BC7 DDS (cubic) -> (e,e,e,4) uint8 [z, y, x] RGBA at the file's own res."""
	import texture2ddecoder
	d = open(path, 'rb').read()
	size, flags, h, w, pitch, depth = struct.unpack_from('<6I', d, 4)
	if not (w == h == depth):
		sys.exit(f'{os.path.basename(path)}: expected a cube, got {w}x{h}x{depth}')
	stride = (w // 4) * (h // 4) * 16          # BC7: 16 B per 4x4 block
	data = d[148:148 + stride * depth]         # base mip only
	vol = np.empty((depth, h, w, 4), np.uint8)
	for z in range(depth):
		vol[z] = np.frombuffer(
			texture2ddecoder.decode_bc7(data[z * stride:(z + 1) * stride], w, h),
			np.uint8).reshape(h, w, 4)
	return vol


def to_edge(vol, n):
	"""Box-average a native volume down to edge n (same edge passes through)."""
	e = vol.shape[0]
	if e == n:
		return vol
	if e % n:
		sys.exit(f'cannot downsample {e}^3 to {n}^3')
	f = e // n
	m = vol.reshape(n, f, n, f, n, f, 4).mean(axis=(1, 3, 5), dtype=np.float32)
	return np.clip(np.rint(m), 0, 255).astype(np.uint8)


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


def _density_floor(alpha):
	"""Floor for one named volume, from its own bright core."""
	p = float(np.percentile(alpha[alpha > 0], 99.5))
	return (max(DENSITY_FLOOR[0], DENSITY_FLOOR_REL[0] * p),
	        max(DENSITY_FLOOR[1], DENSITY_FLOOR_REL[1] * p))


_RAMP_CACHE = {}


def ed_gain(src_rel):
	"""ED's per-volume density gain, from the volume's companion slice.

	Every ED volume texture ships with a 256x1 RGBA32F transfer slice carved
	beside it; the two rows share the volume's identity hash (the carve
	filename's last 8-hex token), and one of them is the BC7 cube itself, so
	the row filter demands the ramp shape. Stored channel order is BGRA:
	channel 3 is the gain curve over raw alpha. Its bins 249-255 spike to
	~192 on <0.1% of voxels; the applied gain clamps to the bins 0-248
	median so a lone dense voxel cannot punch a black hole through a ray.
	Volumes whose ramp was not carved fall back to gain 1.
	"""
	key = re.findall(r'[0-9a-f]{8}', os.path.basename(src_rel))[-1]
	if key in _RAMP_CACHE:
		return _RAMP_CACHE[key]
	gain = None
	with open(ED_CATALOG) as f:
		for r in csv.reader(f):
			if (len(r) > 21 and r[0][:8] == key
					and r[7] == 'R32G32B32A32_FLOAT'
					and r[8] == '256' and r[9] == '1'):
				raw = open(r[21], 'rb').read()
				off = 148 if raw[84:88] == b'DX10' else 128
				tex = np.frombuffer(raw, dtype='<f4', count=256 * 4,
				                    offset=off).reshape(256, 4)
				plateau = float(np.median(tex[:249, 3]))
				gain = np.minimum(tex[:, 3], plateau).astype(np.float32)
				break
	if gain is None:
		gain = np.ones(256, np.float32)
	_RAMP_CACHE[key] = gain
	return gain


def ed_gain_plateau(src_rel):
	"""The volume's ED density-gain plateau (bins 0-248 median)."""
	return float(np.median(ed_gain(src_rel)[:249]))


def _density_transfer(dens, floor=None):
	"""Drop a named volume's low-alpha continuum."""
	lo, hi = floor or DENSITY_FLOOR
	return dens * smoothstep(lo, hi, dens)


def bake_raw(vol, floor=None, gain=None, rescale=None):
	"""ED named volume -> (RGBA32 bytes, stats).

	BC7 through texture2ddecoder comes back BGRA; ED's DDS is straight-alpha
	BC7_UNORM, so the swap is the only colour transform. The alpha then loses
	its low-density continuum (a floor scaled to the volume's own core), gets
	ED's own per-volume density gain from the companion ramp (clamped to the
	plateau, indexed by raw alpha - see ed_gain), and the outer 5% fades, so
	the cube's empty half does not raymarch as a ball and the faces do not
	cut the gas. No column normalisation, no LUT: the game's colour and
	surviving density remain.

	ED's gain pushes per-voxel densities past 1 (alpha 0.75 x 4.96), and the
	8-bit alpha channel cannot hold that, so the bake stores density/K with
	K = the volume's own peak density and the GalaxyNebulaVolume component
	carries `density: K` to restore ED's absolute scale in the shader -
	identical tau and emission, and the bytes use the full 0-255 range.
	`rescale` pins K across rungs (always the 256 bake's peak, from
	ed_density.json) so every LOD of a volume restores through one number.
	"""
	alpha = vol[..., 3].astype(np.float32) / 255.0
	if floor is None:
		floor = _density_floor(alpha)
	out = np.empty_like(vol)
	out[..., 0] = vol[..., 2]
	out[..., 1] = vol[..., 1]
	out[..., 2] = vol[..., 0]
	dens = _density_transfer(alpha, floor)
	dens *= _edge_guard(vol.shape[0])
	if gain is not None:
		dens *= gain[np.clip((alpha * 255).round().astype(int), 0, 255)]
	k = float(rescale) if rescale is not None else max(float(dens.max()), 1e-6)
	peak_column = float(dens.sum(axis=0).max() / vol.shape[0])
	dens /= k
	out[..., 3] = np.clip(dens * 255, 0, 255)
	n = vol.shape[0]
	return out, {'peak_column': peak_column,
	             'nonzero': float((dens > 1 / 255).mean()),
	             'clipped': float((dens >= 1).mean()),
	             'floor': floor,
	             'gain': (float(np.median(gain)) if gain is not None else 1.0),
	             'peak_density': k}


def bake_ed(name, vol, card_png, gain=None, rescale=None):
	"""ED volume + optional card palette, on the faithful density path.

	Identical shaping to bake_raw: the low-alpha floor cut, ED's own
	per-volume density gain from the companion ramp (this is what the
	game's generic brights and darks actually render at - the old column
	normalisation to TARGET_COLUMN / DARK_TARGET was a stand-in for the
	then-unrecovered gain and is retired), and the 5% edge fade. Colour
	keeps each prefab's identity: the card LUT, or the volume's own hue -
	the dark five glow at DARK_EMISSION and let the density do the real
	work. The peak rescale (density/K, K on the component) carries the
	8-bit channel, as in bake_raw.
	"""
	n = vol.shape[0]
	raw = vol[..., 3].astype(np.float32) / 255.0
	floor = _density_floor(raw)
	# Same order as the named bakes: cut the continuum, fade the faces,
	# then ED's own gain.
	dens = _density_transfer(raw, floor) * _edge_guard(n)
	dark = name in DARK
	if gain is not None:
		dens *= gain[np.clip((raw * 255).round().astype(int), 0, 255)]

	alpha = dens                     # colour shaping reads the true scale
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

	k = float(rescale) if rescale is not None else max(float(dens.max()), 1e-6)
	peak_column = float(dens.sum(axis=0).max() / n)
	dens /= k
	out = np.zeros((n, n, n, 4), np.uint8)
	out[..., :3] = np.clip(rgb * 255, 0, 255)
	out[..., 3] = np.clip(dens * 255, 0, 255)
	return out, {'peak_column': peak_column,
	             'nonzero': float((dens > 1 / 255).mean()),
	             'floor': floor,
	             'gain': (float(np.median(gain)) if gain is not None else 1.0),
	             'peak_density': k}


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


def _edge_guard(n):
	"""1 at the core, 0 at the faces: no box silhouette can show."""
	axis = (np.arange(n, dtype=np.float32) / (n - 1)) * 2 - 1
	g = 1 - smoothstep(EDGE_GUARD[0], EDGE_GUARD[1], np.abs(axis))
	return g[:, None, None] * g[None, :, None] * g[None, None, :]


def _stamp(dens, p, sigma, amp):
	"""Add one gaussian blob at continuous -1..1 coordinates (in place)."""
	n = dens.shape[0]
	step = 2.0 / (n - 1)
	c = (np.asarray(p, np.float64) + 1.0) / step
	r = int(np.ceil(2.6 * sigma / step))
	lo = np.maximum(np.floor(c).astype(int) - r, 0)
	hi = np.minimum(np.floor(c).astype(int) + r + 1, n)
	if np.any(hi <= lo):
		return
	ax = [(np.arange(lo[i], hi[i], dtype=np.float32) - c[i]) * step
	      for i in range(3)]
	gz, gy, gx = np.meshgrid(ax[0], ax[1], ax[2], indexing='ij')
	dens[lo[0]:hi[0], lo[1]:hi[1], lo[2]:hi[2]] += amp * np.exp(
		-(gx * gx + gy * gy + gz * gz) / (2 * sigma * sigma))


def bake_authored(name, n):
	"""Authored nebula -> a REAL 3D field (n^3 RGBA32, stats).

	Barnard's Loop is a broken, noise-warped bubble shell - a ring from above,
	an arc from the side - with its own interior haze, so no view is a card
	extrusion. The Pleiades are a bright core with filaments that each point
	in their own 3D direction. Same face margin and optical budget as the ED
	bakes so the whole palette sits in one brightness family.
	"""
	xs = np.linspace(-1, 1, n, dtype=np.float32)
	Z, Y, X = np.meshgrid(xs, xs, xs, indexing='ij')
	n3 = trivol((n, n, n))
	if name == 'barnard':
		# ellipsoidal shell, warped by noise so the rim is lumpy; a wedge is
		# missing (the Loop is an arc, not a closed ring) and the rim is
		# brighter along one side
		r = np.sqrt((X / 0.82) ** 2 + (Y / 0.66) ** 2 + (Z / 0.60) ** 2)
		r += (n3 - 0.5) * 0.22
		shell = np.exp(-((r - 1.0) ** 2) / (2 * 0.11 ** 2))
		# azimuth lives in xy only, so the mask is a 2D slice broadcast along
		# z; the wrapped distance keeps the seam at theta=+-pi continuous
		cy, cx = np.meshgrid(xs, xs, indexing='ij')
		theta = np.arctan2(cy, cx)
		dth = np.abs(np.arctan2(np.sin(theta + 2.15),
		                        np.cos(theta + 2.15)))
		gap = 1 - 0.85 * np.exp(-(dth / 0.85) ** 2)
		wave = 0.5 + 0.5 * np.clip(np.cos(theta - 0.6), 0, 1)
		dens = (shell * (0.2 + 0.8 * n3 ** 1.5)
		        * gap[None, :, :] * wave[None, :, :])
		dens += 0.07 * np.exp(-(r * 1.9) ** 2) * n3 ** 2   # interior haze
		lo, hi = (92, 20, 16), (255, 118, 82)      # deep red H-alpha
	else:
		# core, a faint envelope, and sixteen curved filaments stamped as
		# overlapping gaussian blobs. The strands start all over the cluster
		# and take their own directions, so nothing radiates from the core -
		# a shared origin is what made the first pass read as a starfish.
		rr = np.sqrt(X * X + Y * Y + Z * Z)
		dens = np.exp(-(rr / 0.11) ** 2) + 0.5 * np.exp(-(rr / 0.05) ** 2)
		dens += 0.24 * np.exp(-(rr / 0.48) ** 2)
		rng = np.random.default_rng(5407)
		for _ in range(16):
			d = rng.normal(size=3)
			d /= max(np.linalg.norm(d), 1e-6)
			start = d * rng.uniform(0.02, 0.26)
			u = rng.normal(size=3)
			u /= np.linalg.norm(u)
			u = 0.7 * u + 0.5 * d          # outward bias, not radial
			u /= np.linalg.norm(u)
			v = rng.normal(size=3)
			v -= u * np.dot(u, v)
			v /= max(np.linalg.norm(v), 1e-6)
			w = np.cross(u, v)
			length = rng.uniform(0.15, 0.55)
			bend = rng.uniform(-0.35, 0.35)
			bend2 = rng.uniform(-0.3, 0.3)
			steps = max(int(length / 0.015), 5)
			sig = rng.uniform(0.02, 0.045)
			amp = rng.uniform(0.18, 0.5)
			for k in range(steps + 1):
				t = k / steps
				s = t * length
				p = (start + u * s + v * (bend * s * s)
				     + w * (bend2 * s * s * 0.7))
				_stamp(dens, p, sig * (1 - 0.35 * t), amp * (1 - 0.5 * t))
		dens *= 0.45 + 0.55 * n3
		lo, hi = (88, 126, 222), (228, 242, 255)   # blue reflection nebula

	dens *= _edge_guard(n)
	peak = dens.sum(axis=0).max() / n            # same column unit as bake_ed
	if peak > 0:
		dens *= min(AUTHORED_COLUMN / peak, 6.0)
	dens = np.clip(dens, 0, 1)
	t = dens / max(float(dens.max()), 1e-6)
	rgb = _ramp(t, lo, hi, 0.75)
	out = np.zeros((n, n, n, 4), np.uint8)
	out[..., :3] = np.clip(rgb * 255, 0, 255)
	out[..., 3] = np.clip(dens * 255, 0, 255)
	return out, {'peak_column': float(dens.sum(axis=0).max() / n),
	             'nonzero': float((dens > 1 / 255).mean()),
	             'clipped': float((dens >= 1).mean())}


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
	ap.add_argument('--sizes', type=int, nargs='+', default=[N],
	                help='volume edges to bake: 64 128 256 (default 64); '
	                     '128/256 come from ED high LODs')
	ap.add_argument('--preview', metavar='DIR',
	                help='also write top/side integral PNGs (scratch)')
	args = ap.parse_args()
	names = args.names or list(ED_ALL + AUTHORED)

	# Previous bake's peak densities pin K across rungs (and re-runs): every
	# LOD of a volume must restore through the prefab's one `density` number.
	peaks = {}
	if os.path.isfile(DENSITY_JSON):
		with open(DENSITY_JSON, encoding='utf-8') as f:
			peaks = json.load(f)
	bad = [n for n in names if n not in ED_ALL + AUTHORED]
	if bad:
		sys.exit(f'unknown nebula name(s): {", ".join(bad)}')
	sizes = sorted(set(args.sizes), reverse=True)
	for n in sizes:
		if n < 16 or n > 256 or n % 16:
			sys.exit(f'--sizes: {n} is not a 16..256 cube edge')

	for name in names:
		stem = STEM.get(name, f'Nebula{name.capitalize()}')
		card_path = os.path.join(CARD_DIR, f'{stem}.png')
		srcs = ED_SOURCES.get(name) or ED_RAW.get(name) or ED_NAMED.get(name)
		if srcs is None:
			sys.exit(f'{name}: no source table')
		vols = {}
		floor = None
		if name in ED_NAMED:
			# Derive the floor from ED's high LOD for every rung, so 64/128/256
			# cut at the same absolute alpha even though the 64 source is a
			# separate bake.
			high = srcs.get('256')
			if high is not None:
				high_full = os.path.join(ED_ROOT, high)
				if high_full not in vols:
					vols[high_full] = decode_ed_dds(high_full)
				floor = _density_floor(
					vols[high_full][..., 3].astype(np.float32) / 255.0)
		for n in sizes:
			path = srcs.get(str(n)) or srcs.get('256')
			if path is None:
				sys.exit(f'{name}: no source for {n}^3')
			full = os.path.join(ED_ROOT, path)
			if not os.path.isfile(full):
				sys.exit(f'{name}: ED volume not found: {full}\n'
				         '(the carve lives at /home/deck/ed-assets)')
			if full not in vols:
				vols[full] = decode_ed_dds(full)   # one decode per source
			vol = to_edge(vols[full], n)
			if name in ED_NAMED:
				out, stats = bake_raw(vol, floor, ed_gain(path), rescale=peaks.get(stem))
				how = 'ED named volume, floor cut + ED gain, peak rescaled'
			else:
				out, stats = bake_ed(name, vol,
				                     card_path if name in ED_SOURCES else None,
				                     ed_gain(path), peaks.get(stem))
				how = ('ED volume, card palette, floor cut + ED gain, peak rescaled'
				       if name in ED_SOURCES
				       else 'ED volume, own hue, floor cut + ED gain, peak rescaled')
			if peaks.get(stem) is None:
				peaks[stem] = stats['peak_density']
			dst = os.path.join(VOL_DIR, f'LOD{n}')
			os.makedirs(dst, exist_ok=True)
			save_bytes(os.path.join(dst, f'{stem}.bytes'), out)
			print(f'{name:9s} {n:3d}^3 -> {stem}.bytes [{how}]  peak column '
			      f'{stats["peak_column"]:.2f} cu, gas voxels '
			      f'{stats["nonzero"] * 100:.0f}%'
			      + (f', floor {stats["floor"][0]:.2f}-{stats["floor"][1]:.2f}'
			         if 'floor' in stats else '')
			      + (f', ED gain {stats["gain"]:.2f}x'
			         if 'gain' in stats else '')
			      + (f', peak density {stats["peak_density"]:.2f}'
			         if 'peak_density' in stats else '')
			      + (f', clipped {stats["clipped"] * 100:.1f}%'
			         if 'clipped' in stats else ''))
			if args.preview:
				os.makedirs(args.preview, exist_ok=True)
				save_preview(os.path.join(args.preview, f'{stem}-{n}'), out)

	if peaks:
		with open(DENSITY_JSON, 'w', encoding='utf-8') as f:
			json.dump(peaks, f, indent=1, sort_keys=True)
			f.write('\n')
		print(f'{len(peaks)} peak densities -> '
		      f'{os.path.relpath(DENSITY_JSON, REPO)}')


if __name__ == '__main__':
	main()
