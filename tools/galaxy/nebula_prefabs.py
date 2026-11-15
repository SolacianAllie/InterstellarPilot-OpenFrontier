#!/usr/bin/env python3
"""Generate the named-nebula prefabs from the baked .bytes assets.

`nebula_volume_bake.py` writes the volumes; this tool wires one prefab and one
TextAsset .meta per named nebula. The prefab body is the same cube + shared
NebulaVolume.mat + GalaxyNebulaVolume component as the two hand-authored
originals (NebulaBarnard, NebulaPleiades), so there is one template and no
per-prefab material drift.

Every run also writes the per-instance optical fields (density / absorption /
brightness) into ALL Nebula*.prefab files on one rule: density = the baked
peak, absorption 1, brightness 4/ED-gain. Hand-tuned values in the editor do
not survive a regeneration; the fields are generated data, like the scales.

The GUIDs are deterministic (MD5 of the repo-relative asset path), so a
regenerated checkout gets the same references and the .bytes need no editor
round-trip before the prefabs can be imported. Existing Barnard/Pleiades
prefabs and metas are left alone; --force rewrites the generated ones.

Run after the bake:

    ~/.venvs/decals/bin/python tools/galaxy/nebula_prefabs.py
    ~/.venvs/decals/bin/python tools/galaxy/nebula_prefabs.py --force
"""
import argparse
import hashlib
import json
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(os.path.dirname(HERE))
NEB = os.path.join(REPO, 'Assets', 'OpenFrontier', 'Galaxy', 'Nebulae')
VOL = os.path.join(NEB, 'Volumes')
PRE = os.path.join(NEB, 'Prefabs')
TEMPLATE = os.path.join(PRE, 'NebulaBarnard.prefab')
LODS = (('volumeLow', 64), ('volumeMedium', 128), ('volumeHigh', 256))

# Display name and initial cube edge (world units) for each named volume.
# The scale is the volume's bounding box, not the visible cloud's diameter:
# the gas occupies only part of the cube, so these are deliberately generous.
# Edit the prefab transform in the editor to taste.
DISPLAY = {
	'barnard': ('Nebula Barnard', 260),
	'pleiades': ('Nebula Pleiades', 60),
	'bowtie': ('Nebula Bow-Tie', 60),
	'bubble': ('Nebula Bubble', 100),
	'butterfly': ('Nebula Butterfly', 70),
	'catseye': ("Nebula Cat's Eye", 40),
	'finering': ('Nebula Fine Ring', 40),
	'lemonslice': ('Nebula Lemon Slice', 50),
	'messier78': ('Nebula Messier 78', 120),
	'ngc6337': ('Nebula NGC 6337', 40),
	'orion': ('Nebula Orion', 220),
	'ring': ('Nebula Ring', 40),
	'veilwest': ('Nebula Veil West', 260),
	'flame': ('Nebula Flame', 160),
	'crab': ('Nebula Crab', 90),
	'hind': ('Nebula Hind', 50),
	'horsehead': ('Nebula Horsehead', 200),
	'lbn623': ('Nebula LBN 623', 140),
}

# The two originals were hand-authored and are referenced by the scene. Never
# rewrite them silently; their GUIDs already exist.
ORIGINALS = ('NebulaBarnard', 'NebulaPleiades')

# Per-instance optical multipliers, written into every prefab's
# GalaxyNebulaVolume block next to the quality field. Every ED volume -
# named and generic - carries the game's own per-volume density gain
# baked into the .bytes (ed_gain in the bake tool), stored as
# density/peak with `density` restoring the peak (ed_density.json), so
# they all march at absorption 1 with brightness 4/gain: ED's opacity
# lands in the data and the emission level the volume's own colour gives
# at brightness 4 stays. The shared NebulaVolume.mat stays the neutral
# 1/1/4 base for anything these fields do not cover.
BASE_BRIGHTNESS = 4.0

OPTICAL_FIELDS = ('density', 'absorption', 'brightness')


def apply_optics(path, values):
	"""Write the per-instance optical fields into a prefab's
	GalaxyNebulaVolume block, replacing any that already exist. Idempotent,
	and touches nothing else - names, scales and GUIDs stay put."""
	lines = open(path, encoding='utf-8').read().splitlines(keepends=True)
	block = end = None
	for i, line in enumerate(lines):
		if block is None:
			if ('m_EditorClassIdentifier: OpenFrontier.Content::'
					'OpenFrontier.IP.Engine.GalaxyNebulaVolume' in line):
				block = i
		elif line.startswith('--- !u!'):
			end = i
			break
	if block is None:
		sys.exit(f'{os.path.basename(path)}: no GalaxyNebulaVolume block')
	if end is None:
		end = len(lines)
	out, inserted = [], False
	for line in lines[block:end]:
		field = line.split(':', 1)[0].strip()
		if field in OPTICAL_FIELDS:
			continue                      # re-written right after quality
		out.append(line)
		if not inserted and field == 'quality':
			for name, value in values.items():
				out.append(f'  {name}: {value:.6g}\n')
			inserted = True
	if not inserted:
		sys.exit(f'{os.path.basename(path)}: no quality line to anchor on')
	with open(path, 'w', encoding='utf-8', newline='') as f:
		f.write(''.join(lines[:block] + out + lines[end:]))


def optics_pass(bake):
	"""Write per-instance optics into every Nebula*.prefab.

	One rule covers all 29 now: density = the volume's baked peak
	(ed_density.json), absorption = 1 (ED's own optical depth is in the
	data), brightness = 4/gain (the gain multiplies emission too, so this
	holds the level the volume's own colour gives at brightness 4).
	"""
	named = {bake.STEM[k]: k for k in bake.STEM}
	for key in tuple(bake.ED_SOURCES) + tuple(bake.ED_RAW):
		named[f'Nebula{key.capitalize()}'] = key
	peaks = {}
	if os.path.isfile(bake.DENSITY_JSON):
		with open(bake.DENSITY_JSON, encoding='utf-8') as f:
			peaks = json.load(f)
	done = 0
	for fname in sorted(os.listdir(PRE)):
		if not fname.endswith('.prefab'):
			continue
		stem = fname[:-len('.prefab')]
		path = os.path.join(PRE, fname)
		key = named.get(stem)
		if key is None:
			sys.exit(f'{stem}: no ED volume mapping for this prefab stem')
		srcs = (bake.ED_NAMED.get(key) or bake.ED_SOURCES.get(key)
		        or bake.ED_RAW.get(key))
		if srcs is None:
			sys.exit(f'{key}: no volume source table')
		if stem not in peaks:
			sys.exit(f'{stem}: no baked peak density in '
			         f'{bake.DENSITY_JSON} - run nebula_volume_bake.py first')
		gain = bake.ed_gain_plateau(srcs['256'])
		values = {'density': peaks[stem], 'absorption': 1.0,
		          'brightness': BASE_BRIGHTNESS / gain}
		apply_optics(path, values)
		print(f'{stem:18s} density {peaks[stem]:g}  absorption 1  '
		      f'brightness {values["brightness"]:.6g}  '
		      f'[ED gain {gain:.3f}x, peak {peaks[stem]:.2f}]')
		done += 1
	print(f'{done} prefab(s) optics set')


def import_bake():
	sys.path.insert(0, HERE)
	import nebula_volume_bake
	return nebula_volume_bake


def guid_for(rel):
	"""Stable 32-hex Unity GUID from the repo-relative asset path."""
	return hashlib.md5(rel.encode('utf-8')).hexdigest()


def read_guid(meta):
	m = re.search(r'^guid: ([0-9a-f]{32})$', open(meta, encoding='utf-8').read(),
	              re.M)
	if not m:
		sys.exit(f'{meta}: no GUID')
	return m.group(1)


def write_bytes_meta(path, guid):
	text = (
		'fileFormatVersion: 2\n'
		f'guid: {guid}\n'
		'TextScriptImporter:\n'
		'  externalObjects: {}\n'
		'  userData: \n'
		'  assetBundleName: \n'
		'  assetBundleVariant: \n')
	with open(path, 'w', encoding='utf-8', newline='\n') as f:
		f.write(text)


def write_prefab_meta(path, guid):
	text = (
		'fileFormatVersion: 2\n'
		f'guid: {guid}\n'
		'PrefabImporter:\n'
		'  externalObjects: {}\n'
		'  userData: \n'
		'  assetBundleName: \n'
		'  assetBundleVariant: \n')
	with open(path, 'w', encoding='utf-8', newline='\n') as f:
		f.write(text)


def make_prefab(template, name, scale, guids):
	text = template
	text = text.replace('m_Name: Nebula Barnard', f'm_Name: {name}')
	text = text.replace(
		'm_LocalScale: {x: 260, y: 260, z: 260}',
		f'm_LocalScale: {{x: {scale}, y: {scale}, z: {scale}}}')
	for field, lod in LODS:
		pat = re.compile(
			rf'^  {field}: \{{fileID: 4900000, guid: [0-9a-f]{{32}}, type: 3\}}$',
			re.M)
		text, n = pat.subn(
			f'  {field}: {{fileID: 4900000, guid: {guids[lod]}, type: 3}}',
			text)
		if n != 1:
			sys.exit(f'{field}: template has {n} matches, expected 1')
	return text


def main():
	ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
	ap.add_argument('--force', action='store_true',
	                help='rewrite generated prefabs/metas that already exist')
	args = ap.parse_args()

	bake = import_bake()
	if not os.path.isfile(TEMPLATE):
		sys.exit(f'template missing: {TEMPLATE}')
	template = open(TEMPLATE, encoding='utf-8').read()

	missing = []
	made = []
	for key, (name, scale) in DISPLAY.items():
		stem = bake.STEM[key]
		if stem in ORIGINALS:
			continue
		guids = {}
		for field, lod in LODS:
			bytes_path = os.path.join(VOL, f'LOD{lod}', f'{stem}.bytes')
			meta_path = bytes_path + '.meta'
			if not os.path.isfile(bytes_path):
				missing.append(os.path.relpath(bytes_path, REPO))
				continue
			if not os.path.isfile(meta_path):
				write_bytes_meta(meta_path, guid_for(
					os.path.relpath(bytes_path, REPO).replace(os.sep, '/')))
			guids[lod] = read_guid(meta_path)
		if len(guids) != 3:
			continue

		prefab_path = os.path.join(PRE, f'{stem}.prefab')
		meta_path = prefab_path + '.meta'
		if os.path.isfile(prefab_path) and not args.force:
			print(f'{stem}: prefab exists, left alone')
			continue
		text = make_prefab(template, name, scale, guids)
		with open(prefab_path, 'w', encoding='utf-8', newline='\n') as f:
			f.write(text)
		if not os.path.isfile(meta_path) or args.force:
			write_prefab_meta(meta_path, guid_for(
				os.path.relpath(prefab_path, REPO).replace(os.sep, '/')))
		made.append((stem, name, scale))

	for stem, name, scale in made:
		print(f'{stem:18s} {name:24s} cube edge {scale}')
	print(f'{len(made)} prefab(s) generated')

	# Per-instance optics run every time (idempotent): named volumes get
	# brightness 4/ED-gain, generics the measured absorption. This covers
	# the hand-authored originals and the generic prefabs too - only the
	# three optical fields are touched.
	optics_pass(bake)

	if missing:
		print('missing baked volumes (run the bake first):')
		for p in missing:
			print('  ' + p)
		return 1
	return 0


if __name__ == '__main__':
	sys.exit(main())
