# Nebula placables

Elite Dangerous' 11 generic bright/dark volumes, plus its own Barnard's Loop
and Pleiades volumes, as raymarched 3D placables. The shader carries no shape
of its own — `tools/galaxy/nebula_volume_bake.py` is the single source of the
silhouette.

The two named nebulae are ED's data, not hand-authored fields. The game's
`NebulaDatabase` maps each display name to a volume pair
(`BarnardsLoop_High/Low`; the Pleiades use `BrightNebula_02_High/Low`), and
`DJB2("<volume name>_volumetexture")` is the texture UID. That is how the
carve's truncated filenames were recovered. They bake raw: the game's own RGB
and alpha, only BGRA→RGBA, no border guard and no column normalisation. That
is as close to the shipped BC7 DDS as an RGBA32 Texture3D gets.

## Layout

| Folder | What |
|---|---|
| `Prefabs/` | one prefab per nebula; all three LODs referenced, `quality` slider picks the uploaded one |
| `Volumes/LOD64/` | 64³ RGBA32, 1 MB each — ED's own low LOD, the mobile default |
| `Volumes/LOD128/` | 128³, 8 MB each — box-downsampled from ED's high LOD |
| `Volumes/LOD256/` | 256³, 64 MB each — ED's own high LOD, desktop/testing only |
| `Cards/` | 512² projections: colour LUT for the five bright volumes, flat-card fallback for the two named ones |
| `Materials/` | shared `NebulaVolume.mat` + the per-prefab flat-card fallbacks |

## The faint floor is the volume, not the shape

ED's volumes carry a low-alpha haze across much of the cube: about half the
voxels are above 1/255, but only ~10% are above 0.08. At `_Density 10` that
haze is what makes a nebula read as a completely filled cube. The shared
material starts at `_Density 1`, `_Absorption 1`, `_Brightness 4`: dense
cores still clip bright, and the space between the veins stays mostly
transparent. Raise density only if the long map view needs it.

## Quality slider

`GalaxyNebulaVolume.quality` is a 0–2 slider: 0 = 64³, 1 = 128³, 2 = 256³.
Raymarch steps follow the rung (32/48/64) so the added detail is actually
sampled. Only the selected LOD is uploaded. Switching at runtime destroys and
rebuilds the Texture3D, so the high rung hitches on device.

## Regenerating

Run from the repo root:

```
~/.venvs/decals/bin/python tools/galaxy/nebula_volume_bake.py --sizes 64 128 256
~/.venvs/decals/bin/python tools/galaxy/nebula_volume_bake.py barnard pleiades --sizes 64 128 256
```

The default set is all 13 prefabs. Sources are ED's BC7 3D volumes under
`/home/deck/ed-assets/carved/textures/` (64³ low and 256³ high; the 128 rung
is derived). Set `ED_TEXTURE_ROOT` if the carve moves.

The other 17 named volumes in ED's `NebulaDatabase` map the same way; add
their high/low paths to `ED_NAMED` when a prefab needs one. The UIDs come
from `DJB2("<name>_volumetexture")`, matched against
`catalog/carved_textures.csv`.

## Memory

Every placed instance uploads its own Texture3D — there is no share cache.
A scene with one of each rung costs 1 + 8 + 64 MB of volume textures; dozens
of high-rung instances will not fit a phone. Multiply-placing one prefab
multiplies its cost.
