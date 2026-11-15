#!/usr/bin/env python3
"""Generate the named-nebula prefabs from the baked .bytes assets.

`nebula_volume_bake.py` writes the volumes; this tool wires one prefab and one
TextAsset .meta per named nebula. The prefab body is the same cube + shared
NebulaVolume.mat + GalaxyNebulaVolume component as the two hand-authored
originals (NebulaBarnard, NebulaPleiades), so there is one template and no
per-prefab material drift.

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
	if missing:
		print('missing baked volumes (run the bake first):')
		for p in missing:
			print('  ' + p)
		return 1
	return 0


if __name__ == '__main__':
	sys.exit(main())
