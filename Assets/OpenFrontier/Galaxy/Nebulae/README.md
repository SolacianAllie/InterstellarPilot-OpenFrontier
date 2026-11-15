# Nebula placables

Every nebula volume Elite Dangerous ships (11 generic bright/dark volumes),
plus Barnard's Loop and the Pleiades authored, as raymarched 3D placables.
The shader carries no shape of its own — `tools/galaxy/nebula_volume_bake.py`
is the single source of the silhouette.

## Layout

| Folder | What |
|---|---|
| `Prefabs/` | one prefab per nebula; all three LODs referenced, `quality` slider picks the uploaded one |
| `Volumes/LOD64/` | 64³ RGBA32, 1 MB each — ED's own low LOD, the mobile default |
| `Volumes/LOD128/` | 128³, 8 MB each — box-downsampled from ED's high LOD |
| `Volumes/LOD256/` | 256³, 64 MB each — ED's own high LOD, desktop/testing only |
| `Cards/` | 512² projections: colour LUT for the five bright volumes, silhouette + flat fallback for the two authored ones |
| `Materials/` | shared `NebulaVolume.mat` + the per-prefab flat-card fallbacks |

## Quality slider

`GalaxyNebulaVolume.quality` is a 0–2 slider: 0 = 64³, 1 = 128³, 2 = 256³.
Raymarch steps follow the rung (32/48/64) so the added detail is actually
sampled. Only the selected LOD is uploaded. Switching at runtime destroys and
rebuilds the Texture3D, so the high rung hitches on device.

## Regenerating

Run from the repo root:

```
~/.venvs/decals/bin/python tools/galaxy/nebula_volume_bake.py --sizes 64 128 256
~/.venvs/decals/bin/python tools/galaxy/nebula_volume_bake.py dusk pleiades --sizes 256
```

Sources are ED's BC7 3D volumes under `/home/deck/ed-assets/carved/textures/`
(64³ low and 256³ high; the 128 rung is derived). Set `ED_TEXTURE_ROOT` if the
carve moves.

## Memory

Every placed instance uploads its own Texture3D — there is no share cache.
A scene with one of each rung costs 1 + 8 + 64 MB of volume textures; dozens
of high-rung instances will not fit a phone. Multiply-placing one prefab
multiplies its cost.
