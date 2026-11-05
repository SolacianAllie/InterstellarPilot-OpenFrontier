# tools/decals — the 4K-preserved decal workbench

The ship shaders read decals as one alpha-brushed RGBA sheet riding the
hull's UV0 space (`_DecalMap`, multiplied/lifted per material). This
directory is the pipeline that builds those sheets whole-ship at a
time — marks are made **manually in Krita**, the deterministic tools
here handle everything else. The information-preservation contract is
enforced by architecture, not discipline:

- the albedo is **never a write target** — it is a locked reference the
  shader composites at runtime;
- every mark lives **inside alpha** — a brush texel writes, nothing else
  changes, and marks land in 4K coordinates at 1:1;
- marks enter a sheet only in its language: flat palette colours, keyed
  background, hard alpha (`quantize`), and the pattern layer reads as a
  triad zone mask — R/G/B weights the shader recolours from the
  material's three faction colours (`channelize`). Each faction
  colour's own alpha is **ink translucency**: partial alpha paints its
  zone at half ink (hull through the paint), alpha 0 erases the zone
  (8-digit hex everywhere in `preview.py`).

## The five tools (all stdlib-free — Pillow + numpy via run.sh)

| tool | does |
|---|---|
| `census.py` | every mark in a sheet → exact bbox + the generation window it needs (1K/1.5K/2K+); optional per-mark crop PNGs |
| `channelize.py` | painted pattern → the triad zone-mask the shader reads: three design colours become R/G/B weights, zone edges keep their blend as fractional weights, stray paint is reported, hard alpha |
| `quantize.py` | baked-colour artwork → decal language (chroma-key out, snap to palette, hard alpha, nearest upscale) — the superpower brand layer's language |
| `place.py` | write a mark into a sheet at exact coordinates, refusing overlaps, never touching the source |
| `preview.py` | composite pattern(+brand)+albedo with the exact shader math (triad select, multiply, both paint strengths) as a QA image |

```bash
tools/decals/run.sh census.py sheet.png --json sheet.census.json --crops census/
tools/decals/run.sh channelize.py pattern.png --palette 8
tools/decals/run.sh channelize.py pattern.png --colors C0392B,F4D03F,1F618D -o mask.png
tools/decals/run.sh quantize.py artwork.png --palette 0A2472,FFFFFF,151515 -o mark.png
tools/decals/run.sh place.py --sheet sheet.png --mark mark.png --x 1200 --y 2400 --check-empty --output out.png
tools/decals/run.sh preview.py --albedo hull_Color.png --decal mask.png \
  --primary C0392B --secondary F4D03F --tertiary 1F618D \
  --brand brand.png --out preview.png
```

## The AI half — withdrawn (2026-11-05, same day it landed)

Two engines tried, then removed by decision after real painting runs:

1. **ComfyUI + ROCm torch** — HIP initializes fine, memory blits fine,
   but *every compute kernel silently returns garbage* (2×2 ones-matmul
   reads back 0.0). Reproduced identically on host and in the Arch box,
   across torch-rocm 7.2, 6.4, and AMD's TheRock nightly built natively
   for gfx1033 — suspected SteamOS-kernel kfd scheduling; unfixable
   from userland.
2. **stable-diffusion.cpp (Vulkan, `~/sdcpp`)** — genuinely fast
   (512² window, 16 steps, ~56 s per inpaint) and 512² runs completed
   fine, but **1024² windows permanently wedge the Vulkan device**:
   `Failed to allocate pinned memory` → `device lost`, unrecoverable
   inside the session even with the process under the cgroup cap —
   and one uncapped run froze the whole desktop before the cap existed.

Everything was deleted (~/ComfyUI 16 G incl. checkpoint/controlnet,
~/sdcpp 3.2 G, uv caches 42 G — ~61 GB back on the SSD). What survives
is `tools/decals` itself: **marks are made manually in Krita, then
quantize → place → preview exactly as below**. The v2 batcher idea is
parked with it. Mirror URLs and every engine record are retrievable
from git history (`7a07f28c`, `33a39d1d`) if a lighter engine (tiny
distilled SD, or a non-APU machine) is ever worth another attempt;
`~/.venvs/decals` (Pillow+numpy) stays — the four tools depend on it.

Licence note for a commercial game (if ever revived): SD 1.5 =
OpenRAIL-M; SDXL = Stability's ≤$1M clause; FLUX dev = non-commercial.

## The working loop per sheet

1. `census.py` the sheet (JSON + crops). ~97% of existing marks fit a
   1024² window (measured; see census numbers in git history).
2. In Krita: 4096 doc — albedo layer (locked, reference), pattern
   layer, paint every mark in exactly one of the three faction zone
   colours (`channelize.py --palette N` lists a sheet's dominant
   colours when choosing them). An edge may blend two zone colours —
   the shader ramps it for free.
3. `channelize.py pattern.png --colors A,B,C -o mask.png` — stray
   paints are reported, never invented into a zone.
4. The superpower brand is a second, separate sheet in the old decal
   language (plain painted colours); it binds straight into
   `_PowerDecalMap` with no conversion.
5. `preview.py` per sheet with the material's real triad and
   strengths — compare against the shader's actual look.
6. Commit the sheet. `preview.py` outputs never enter a commit.

