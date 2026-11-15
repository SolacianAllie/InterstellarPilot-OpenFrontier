# Nebula placables

Elite Dangerous' 11 generic bright/dark volumes, plus the 18 named nebulae
that have their own volume pair in ED's `NebulaDatabase`, as raymarched 3D
placables. The shader carries no shape of its own —
`tools/galaxy/nebula_volume_bake.py` is the single source of the silhouette.

The 18 named nebulae are ED's data, not hand-authored fields. The game's
`NebulaDatabase` maps each display name to a volume pair
(`BarnardsLoop_High/Low`, `Orion_High/Low`, `Horsehead_Cluster_High/Low`, …),
and `DJB2("<volume name>_volumetexture")` is the texture UID. That is how the
carve's truncated filenames were recovered. The named bakes keep the game's
RGB and surviving alpha: only BGRA→RGBA, the low-alpha continuum cut, and a
5% edge fade. There is no column normalisation and no card LUT.

Red Spider is deliberately **not** a named prefab: its DB row points at
`Generic_PlanataryNebula_06`, so it has no unique volume. Rosette and
California have no dedicated volume in the database at all (their in-game
visuals reuse a generic type), so they are not in this set either.

## Layout

| Folder | What |
|---|---|
| `Prefabs/` | one prefab per nebula (29: 11 generic + 18 named); all three LODs referenced, `quality` slider picks the uploaded one |
| `Volumes/LOD64/` | 64³ RGBA32, 1 MB each — ED's own low LOD, the mobile default |
| `Volumes/LOD128/` | 128³, 8 MB each — box-downsampled from ED's high LOD |
| `Volumes/LOD256/` | 256³, 64 MB each — ED's own high LOD, desktop/testing only |
| `Cards/` | 512² projections: colour LUT for the five bright volumes, flat-card fallback for the two original named ones |
| `Materials/` | shared `NebulaVolume.mat` + the per-prefab flat-card fallbacks |

## The faint floor: why Barnard was a ball

ED's volumes carry a low-alpha continuum across much of the cube: about half
the voxels are above 1/255, but only ~10% are above 0.08. At `_Density 10`
that haze made a nebula read as a completely filled cube. The shared material
was dropped to `_Density 1`, `_Absorption 1`, `_Brightness 4`, which fixed the
cube but not the ball: the 1–10% continuum still integrates over the full
ray, so Barnard's Loop — a thin shell — rendered as a smooth sphere with its
rim buried in haze.

The named bakes now apply a soft floor scaled to each volume's own bright
core:

```
p  = percentile(alpha, 99.5)
lo = max(0.05, 0.12 * p)
hi = max(0.15, 0.32 * p)
dens = dens * smoothstep(lo, hi, dens)
dens *= edge_guard(5%)
```

The absolute 5%/15% minimum keeps faint planetaries (Lemon Slice, p ≈ 0.17)
from being erased; bright shells (Barnard, p ≈ 0.72) cut at ~0.09/0.23. Below
`lo` is empty, above `hi` is untouched, and the transition is smooth. The
outer 5% of the cube fades so a face can never cut a cloud into a box. The
result is Barnard's shell and rim instead of a filled sphere; the Pleiades
keep their fine wisps. The generic palette keeps its previous normalised
bake — extend `_density_transfer` to `bake_ed` if those ever show the same
ball.

## Quality slider

`GalaxyNebulaVolume.quality` is a 0–2 slider: 0 = 64³, 1 = 128³, 2 = 256³.
Raymarch steps follow the rung (32/48/64) so the added detail is actually
sampled. Only the selected LOD is uploaded. Switching at runtime destroys and
rebuilds the Texture3D, so the high rung hitches on device.

## Regenerating

Run from the repo root:

```
~/.venvs/decals/bin/python tools/galaxy/nebula_volume_bake.py --sizes 64 128 256
~/.venvs/decals/bin/python tools/galaxy/nebula_prefabs.py
```

The default bake set is all 29 prefabs. `nebula_prefabs.py` is idempotent; it
wires the `.bytes` GUIDs into the shared prefab template and skips existing
prefabs unless `--force` is passed. The two originals (`NebulaBarnard`,
`NebulaPleiades`) are hand-authored and never rewritten. Sources are ED's BC7
3D volumes under `/home/deck/ed-assets/carved/textures/` (64³ low and 256³
high; the 128 rung is derived). Set `ED_TEXTURE_ROOT` if the carve moves.

The UIDs come from `DJB2("<name>_volumetexture")`, matched against
`catalog/carved_textures.csv`. The named source table lives in `ED_NAMED`; the
display names are the DB's own. 18 named entries have a dedicated pair; Red
Spider is the one named entry that points at a generic planetary.

## Memory

Every placed instance uploads its own Texture3D — there is no share cache.
A scene with one of each rung costs 1 + 8 + 64 MB of volume textures; dozens
of high-rung instances will not fit a phone. Multiply-placing one prefab
multiplies its cost.
