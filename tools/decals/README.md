# tools/decals — the 4K-preserved decal workbench

The ship shaders read decals as one alpha-brushed RGBA sheet riding the
hull's UV0 space (`_DecalMap`, multiplied/lifted per material). This
directory is the pipeline that builds those sheets whole-ship at a time
using a LOCAL diffusion model for the marks and deterministic tools for
everywhere else. The information-preservation contract is enforced by
architecture, not discipline:

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

## The AI half — as built on this Steam Deck (2026-11-05)

tl;dr: **ComfyUI/ROCm is parked.** HIP computes return garbage on this
APU no matter the stack (see the story in `comfy.sh`). The working engine
is **stable-diffusion.cpp's prebuilt Vulkan binary** — and for the decal
batcher that's the *better* engine anyway: one CLI call per mark, no
python stack, no server.

- binary: `~/sdcpp/` (`sd-cli`, `sd-server`, `libggml-vulkan.so` —
  commit `58bb3c1`, the `Linux-Ubuntu-26.04-vulkan` release zip; runs on
  the host directly, no distrobox, no glibc complaints)
- device: `AMD Custom GPU 0932 (RADV VANGOGH)` — uma, fp16 dot2
- model: the same checkpoint works —
  `~/ComfyUI/repo/models/checkpoints/sd-v1-5-inpainting.ckpt` (4.0 G;
  the original `runwayml` HF org was deleted 2024-08; hash-identical
  mirrors are `stable-diffusion-v1-5/stable-diffusion-inpainting` and
  `Comfy-Org/stable-diffusion-v1-5-archive`)
- measured on this APU: 512² txt2img 8 steps **~41 s total** (1.88 s/it
  + ~8.6 s VAE decode); 256² mask-inpaint 10 steps **~30 s**; extrapolate
  1024² ≈ 2.5–4 min/hero mark — overnight batch is comfortable
- mask convention verified: **white = generate**, black = preserved
  (the preserved side survives within a feathered ~5% brightness drift —
  final "AI never erases" is enforced deterministically at
  place-time by compositing original pixels outside the mask)
- inpaint shape: `sd-cli -M img_gen -m <ckpt> -i hull_crop.png
  --mask white_is_generate.png -p "..." --steps 10 --strength 0.9
  -o out.png` (`--mask-mode`, `dilate_erode`, `inpaint_padding` exist
  as model-args if needed)
- interactive painting outside Krita: `tools/decals/sdserver.sh` runs
  `sd-server` inside a **cgroup cap** (soft throttle 4.5 GB, hard kill
  5.25 GB) at `http://127.0.0.1:1234` — *always use the capped launcher*:
  an uncapped 1024²-window generation OOM-freezes the whole desktop
  session (happened 2026-11-05; on the capped server the same runaway
  kills only the job/process). Keep painter windows ≤512²; the model
  loads automatically in the UI.

Licence note for a commercial game: check each checkpoint's licence on
its download page (SD 1.5 = OpenRAIL-M; SDXL = Stability's ≤$1M clause;
FLUX dev = non-commercial — skip it).

### Krita + AI Diffusion plugin (optional, NOT the batch path)

The plugin speaks **ComfyUI only** — it cannot talk to sd.cpp, and
ComfyUI's ROCm path is parked above (Krita's plugin would fall back to
CPU-turtle). If in-Krita generation is ever wanted, revisit when either
AMD fixes the APU or the plugin grows a llama.cpp/sd.cpp backend:
install the plugin zip from `github.com/Acly/krita-ai-diffusion`
(Tools ▸ Scripts ▸ Import Python Plugin from File...), point it at a
ComfyUI, and re-read this section's history first.

## The working loop per sheet

1. `census.py` the sheet (JSON + crops). ~97% of existing marks fit a
   1024² window (measured; see census numbers in git history).
2. In Krita: 4096 doc — albedo layer (locked, reference), decal layer
   (the sheet), draw masks where a mark goes. Generate per mask; the
   result writes back into 4K coordinates at 1:1.
3. Walk the generated patch through `quantize.py` → `place.py`.
4. `preview.py` per sheet — compare against the shader's actual look.
5. Commit the sheet. `preview.py` outputs never enter a commit.

