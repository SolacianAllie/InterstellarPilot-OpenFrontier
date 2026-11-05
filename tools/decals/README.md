# tools/decals — the 4K-preserved decal workbench

The ship shaders read decals as one alpha-brushed RGBA sheet riding the
hull's UV0 space (`_DecalMap`, multiplied/lifted per material). This
directory is the pipeline that builds those sheets whole-ship at a
time — marks are made **manually in Krita**, the deterministic tools
here handle everything else. The information-preservation contract is
enforced by architecture, not discipline:

- the albedo is **never a write target** — it is a locked reference the
  shader composites at runtime;
- the AI only ever writes **inside a mask**, and each generation is a
  small crop (the "considered" area), never the whole 4K canvas;
- generated marks pass through quantize before touching a sheet:
  flat palette colours, keyed background, hard alpha, 1:1 placement.

## The four tools (all stdlib-free — Pillow + numpy via run.sh)

| tool | does |
|---|---|
| `census.py` | every mark in a sheet → exact bbox + the generation window it needs (1K/1.5K/2K+); optional per-mark crop PNGs |
| `quantize.py` | AI patch → decal language (chroma-key out, snap to palette, hard alpha, nearest upscale) |
| `place.py` | write a marsk into a sheet at exact coordinates, refusing overlaps, never touching the source |
| `preview.py` | composite decal+albedo with the exact shader math (multiply + Decal Paint Strength) as a QA image |

```bash
tools/decals/run.sh census.py sheet.png --json sheet.census.json --crops census/
tools/decals/run.sh quantize.py generated.png --palette 0A2472,FFFFFF,151515 -o mark.png
tools/decals/run.sh place.py --sheet sheet.png --mark mark.png --x 1200 --y 2400 --check-empty --output out.png
tools/decals/run.sh preview.py --albedo hull_Color.png --decal out.png --out preview.png
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
2. In Krita: 4096 doc — albedo layer (locked, reference), decal layer
   (the sheet), paint each mark where it belongs (own layer is handy;
   it composites into 4K coordinates at 1:1).
3. Walk the generated patch through `quantize.py` → `place.py`.
4. `preview.py` per sheet — compare against the shader's actual look.
5. Commit the sheet. `preview.py` outputs never enter a commit.

