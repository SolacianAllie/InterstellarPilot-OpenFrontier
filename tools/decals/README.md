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

## The AI half — on this Steam Deck

Editor: **Krita** (host) + **AI Diffusion plugin** (install through
Krita's Python plugin manager or the Release zip from
`github.com/Acly/krita-ai-diffusion`). Its settings panel is where the
backend URL (`http://127.0.0.1:8188`) and the per-document inpaint model
get picked.

Backend: **ComfyUI inside the existing Arch distrobox** (host stays
pristine; SteamOS survives updates):

```bash
distrobox enter <box>
sudo pacman -S --needed python git
git clone --recursive https://github.com/comfyanonymous/ComfyUI
cd ComfyUI && python -m venv venv && . venv/bin/activate
pip install torch torchvision --index-url https://download.pytorch.org/whl/rocm6.2
pip install -r requirements.txt
# Deck's APU (gfx1033 → RDNA2 family override; gfx1041 etc. need 11.0.0):
export HSA_OVERRIDE_GFX_VERSION=10.3.0
python main.py              # host ports are shared — 8188 is on 127.0.0.1:8188 on the host
```

Models go under `ComfyUI/models/checkpoints/` etc.:

- an **SD 1.5 inpainting** checkpoint — the iteration model (the ENTIRE
  original `runwayml/...` repo has been pulled from Hugging Face at
  times; use an actively mirrored equivalent search result)
- a **ControlNet canny or lineart** model in `models/controlnet/` —
  locks silhouette to drawn guides when wanted
- an SDXL inpaint checkpoint for hero marks only (its native box is
  1536–2048 px and each run is several times an SD generation)

Licence note for a commercial game: check each checkpoint's licence on
its download page (SD 1.5 = OpenRAIL-M; SDXL = Stability's ≤$1M clause;
FLUX dev = non-commercial — skip it).

## The working loop per sheet

1. `census.py` the sheet (JSON + crops). ~97% of existing marks fit a
   1024² window (measured; see census numbers in git history).
2. In Krita: 4096 doc — albedo layer (locked, reference), decal layer
   (the sheet), draw masks where a mark goes. Generate per mask; the
   result writes back into 4K coordinates at 1:1.
3. Walk the generated patch through `quantize.py` → `place.py`.
4. `preview.py` per sheet — compare against the shader's actual look.
5. Commit the sheet. `preview.py` outputs never enter a commit.

