# Galaxy render — build spec (OpenFrontier / Unity URP, mobile-first)

Companion to `/home/deck/ed-assets/docs/GALAXY_RENDER_FINDINGS.md` (evidence).
This file is the build order. Every claim about ED is cited there; this file
only says what we build.

Goal: one galaxy model, rendered in two contexts — the **map** (external disc
view, ortho `MAP` camera) and the **sky** (first-person inside a system,
`Galaxy Camera`) — without a second asset set and without per-frame CPU.

Non-goals: star/planet generation, gameplay, nebula volume RE (already done).

---

## 1. Architecture

```
GalaxyBlueprint (ScriptableObject, existing)
  ├─ density map / analytic spiral        (existing)
  ├─ palette, star counts, haze, twinkle  (existing)
  ├─ NEW: background material settings    (band gain, dust gain, glow)
  └─ seed
        │
        ├─► GalaxyBackgroundBaker  (CPU, once per reseed / per jump)
        │     └─ GalaxyBackground.png / RenderTexture  N²  (512/1024/2048)
        │
        ├─► GalaxyInstanceCloud (existing, 12k billboards)
        ├─► GalaxySkyStars (the 3D star catalog: real positions, apparent
        │     magnitude from camera distance - explorable)
        └─► GalaxySky (per-system hook: galacticPosition + the Sky Observer
              anchor the future skybox capture docks at)
```

The background texture is **generated, never shipped** — matching ED. It is the
single source for the band and the dust, so the two can never disagree.

**The explorable miniature** (2026-11-15, user's call): the Galaxy scene is a
miniature galaxy you can FLY through, not a backdrop observed from one spot.
Every layer is correct from any camera position - the band projects through
the plane from the camera ray, the dust and glow tap the same rays, the cloud
is world-anchored, and the star catalog is real 3D content. That is what makes
the capture story work: the gameplay skybox will be rendered FROM this scene
later (a capture camera at a system's galactic position), the way ED renders
its map and sky from one galaxy model. One model, N captures.

---

## 2. Layer stack

Draw order low → high. "Queue" is the material `renderQueue`; shader tag stays
`Transparent` (Unity 6 rejects bare integers in Queue tags).

| # | Layer | Shader | Queue | Blend | Depth | Geometry |
|---|---|---|---:|---|---|---|
| 0 | Background band | `OpenFrontier/Galaxy/Band` | 2970 | `One One` (add) | ZWrite off, ZTest LEqual | one screen-covering quad (see §4) |
| 1 | Core glow | `OpenFrontier/Galaxy/Glow` | 2975 | `One One` | ZWrite off | quad at galactic centre |
| 2 | Dust lanes | `OpenFrontier/Galaxy/Dust` | 2976 | `SrcAlpha OneMinusSrcAlpha` (darken) | ZWrite off | one screen-covering quad |
| 3 | Density haze | `OpenFrontier/Galaxy/Density` | 2972 | `One One` | ZWrite off | disc quad, map layer 30 |
| 3b | Local dark dust cards | `OpenFrontier/Galaxy/DustCard` | 2979 | `SrcAlpha OneMinusSrcAlpha` | ZWrite off | instanced camera-facing quads |
| 4 | Star cloud | `OpenFrontier/Galaxy/Cloud` (existing) | 2990 | `SrcAlpha One` | ZWrite off | instanced billboards |
| 5 | Map star sprites | `OpenFrontier/Galaxy/MapStar` | 2991 | `SrcAlpha One` | ZWrite off | instanced sheet quads, **map layer 30** (§10) |
| 6 | Nebula volumes | `OpenFrontier/GalaxyNebulaVolume` (existing) | 2999 | `One OneMinusSrcAlpha` | ZWrite off | unit cube |
| 7 | Star catalog | `OpenFrontier/Galaxy/SkyStar` | 3000 | `One One` (add) | ZWrite off | instanced quads at **real 3D positions**, **sky layer 31** (§10) |
| 8 | Local stars / BH | `OpenFrontier/Galaxy/SkyStar` variant | 3001 | `SrcAlpha One` | ZWrite off | instanced quads (deferred) |

ED draws one catalog twice: textured `StarMapStar` sprites from the
`systemmap_stars` sheet in the galaxy map, a procedural star in the sky. Layer
5 (restored 2026-11-15) and layer 7 are those two presentations. Each is drawn
on its presentation layer: the MAP camera includes layer 30 and masks layer 31,
the in-sky Galaxy Camera includes layer 31 and masks layer 30. Game cameras
never draw a star twice; the Scene view includes both layers unless its
culling mask is narrowed, which is useful for inspection but is not a game
presentation.

**Editor preview (2026-11-15).** Every layer component is `[ExecuteAlways]`, so
the Scene view draws the real 3D stack without play mode. The screen-space
band/dust quads are the exception: they write clip space and are hidden from
the Scene view, material preview and reflection cameras, because an editor
camera that sees them gets a giant camera-facing sheet instead of the galaxy.
The 3D cloud, density, glow and both star catalogs still preview in the Scene
view. An **orthographic** Scene view (toolbar toggle, or
`OpenFrontier/Galaxy/Frame Galaxy (Scene View)`) is useful for inspecting the
map's 3D stack, but it does not include the screen-space band/dust; the full
map presentation is the Game view's `Preview Map Camera`. A perspective Scene
view likewise shows the sky's 3D cloud and stars, but the sky band/dust are
only in the Game view's `Preview Sky Camera`. The game cameras pick the
presentation by layer mask, not by projection, so a Scene view that includes
both layers is showing the two catalogs together for inspection. Inspector
edits to `GalaxyBlueprint` / `GalaxyQualitySettings` debounce into a live
rebake/reseed (`GalaxyEditPreview`, `EditorApplication.delayCall`, so it fires
even when the Scene view does not repaint). Hidden layer children the backdrop
builds are `HideAndDontSave`; they are rebuilt on enable and never enter the
scene file.

The scene's `MAP` camera ships disabled and sized for a 50-unit view, so the
map presentation has no working camera until something configures it. Two
menus cover that without play mode: `Preview Map Camera (Game View)` enables
MAP as a top-down ortho camera at 3x disc radius framing the whole disc (and
disables the Galaxy Camera so only one base camera renders), while `Preview
Sky Camera (Game View)` returns to the perspective in-disc view. Both edit the
scene and can be undone.

Rules:
- **Dust must be alpha-blended and drawn above the band**, before the cloud.
  Additive sprites cannot darken — this is why ED inverts the band texture
  rather than adding a second one.
- Billboard orientation happens in clip space (the existing
  `OpenFrontier_GalaxyCloud.shader` pattern), so there is no second draw per
  camera. The star catalog is the one deliberate camera split, and it is a
  gate, not a duplicate: only one presentation is live per camera.
- No `_Time` anywhere. Animate on the `_OpenFrontierGalaxyTime` global pushed
  from `Time.unscaledTime` (the stage boots at `timeScale 0`).

---

## 3. Background bake (`GalaxyBackgroundBaker`)

Runs on CPU once per reseed, off the render path. Output: one `Texture2D`
(N×N, `RGBAHalf`, no mips unless the rung asks for them).

Inputs: the blueprint's density field (painted map or `ProceduralDensity()`),
palette, plus a small amount of clump noise.

**Shipped form (updated during build):** `RGBA32` sRGB **with trilinear mips**
(the band is sampled at grazing angles and minifies hard at zoom-out) — the
`RGBAHalf` draft was optimistic for mobile: 4 bytes/texel at 512² is 1 MB, at
2048² is 16 MB, and the bake never needs HDR headroom. The colour is stored
STRAIGHT (not premultiplied): `out = float4(col, a)` and the band shader
multiplies by `a` at draw time, which keeps the dust's Rec.709 read of the
colour clean.

```
for each texel (u,v) in N²:
    p  = disc coords from (u,v)          // -1..1, radius mask
    d  = density(p)                       // existing field, 0..1
    arm = armPhase(p)                     // spiral angle term
    col = palette(d, arm, radius(p))      // core / arm / edge lerp
    // bandGain is a material multiplier, not a bake multiplier: applying
    // it in both places squared it and changed the dust/glow source too.
    a   = 1 - smoothstep(0.94, 1.0, radius(p))  // geometric coverage mask
    out = float4(col, a)                  // straight alpha; RGB is the signal
```

- `Y` channel for the dust is just `luminance(out.rgb)` computed in the shader;
  do **not** store a second texture. ED ships a colour + Y pair because its
  baker is a GPU pipeline; we can afford one `dot()`.
- Resolution comes from the quality rung (512/1024/2048). On mobile, bake at
  512 and let the rung's bilinear upscale do the rest.
- Bake time budget: ≤ 8 ms at 512² on a mid phone. If it exceeds that, move the
  per-texel loop to a tiny compute/`Graphics.Blit` pass — but only if measured.

---

## 4. Band projection (`OpenFrontier/Galaxy/Band`)

ED's model, reconstructed for the **map**, with a smooth **sky** companion. Both
use ONE screen-covering quad, so the old closed sphere's double fragment (near
and far shell, `Cull Off`) is gone. The vertex shader writes clip space
directly; the fragment reconstructs the camera ray through
`UNITY_MATRIX_I_VP`.

**Map (orthographic):** exact ray/plane intersection, because the camera is
above the disc and every pixel has a well-defined plane hit.

```
farWS = mul(UNITY_MATRIX_I_VP, float4(ndc, farZ, 1)).xyz / w
ro  = camera + (farWS - camera projected onto the camera plane)
rd  = camera forward                                 // galaxy space
t   = -ro.y / rd.y                                   // discard t <= 0
hit = ro.xz + rd.xz * t
uv  = hit / (2 * galaxyRadius) + 0.5
r01 = length(hit) / galaxyRadius
disc = 1 - smoothstep(0.96, 1.0, r01)                // rim mask
```

**Sky (perspective):** a plane seen from inside is not a band. A downward ray
hits the disc centre, a horizontal ray runs to infinity, and the zenith always
shows the core. ED's `vs_DistantMilkyWay` instead builds a field of camera-facing
billboards whose colours follow galactic longitude and radius. The smooth
sky backdrop is therefore a **cylindrical** sampling of the baked disc,
rotated so the bake's core lands on the observer's true core direction:

```
lat  = asin(clamp(rd.y, -1, 1))                     // galaxy-local
lon  = atan2(rd.z, rd.x)
coreLon = atan2(toCore.z, toCore.x)                  // toCore = normalize(-ro)
coreU   = frac(coreLon / (2*pi) + 0.5)
u    = frac(lon / (2*pi) + 0.5 + (0.5 - coreU))     // longitude -> texture x
v    = saturate(0.5 + lat / (2 * skyBandHalfAngle))  // latitude -> texture y
fade = 1 - smoothstep(0.72, 1.0, |lat| / skyBandHalfAngle)
```

At the horizon `v = 0.5`; the baked disc's central row (core and arms) wraps
around the galactic equator. Latitude selects the radial slice and fades the
band out by `_SkyBandHalfAngle` (0.30 rad, a ~34-degree band). There is no
zenith core and no infinite plane hit. The realtime billboard cloud still
supplies the three-dimensional detail and parallax; this quad is only the
smooth substrate.

The sky texture is sampled at LOD 3.5 and its contrast is compressed
(`skyLum = lerp(0.22, lum, 0.45)`) before the cream tint. One top-down row
wrapped around 360 degrees otherwise reads as parallel brushed-metal folds;
the map keeps the full-resolution bake and its warm/blue palette. A broad
warm `_SkyGlowColor` base and a `_CoreColour` core bump carry the smooth
veil, and the 3D cloud carries the small-scale structure.

Fragment (both modes):
```
float4 bg = SAMPLE_TEXTURE2D_LOD(_BandTex, sampler_Band, uv, ortho ? 0 : 3.5);
float  lum = dot(bg.rgb, float3(0.2125, 0.7154, 0.0721));
float  coverage = smoothstep(0.001, 0.035, lum);         // Rec.709 luminance

if (ortho)
    col = bg.rgb * _BandTint * _BandGain * coverage * disc * fade;
else
{
    float skyLum = lerp(0.22, lum, 0.45);
    float skyAdd = _SkyGlowColor * (_SkyGlowGain * bandGlow * fade)
                 + _CoreColour * (pow(coreDot, 96) * _SkyGlowGain * 8);
    col = _BandTint * skyLum * _BandGain * coverage * fade + skyAdd;
}
```

Uniforms: `_BandTex`, `_BandTint`, `_BandGain`, `_GalaxyRadius`,
`_SkyBandHalfAngle` (sky band half-width, radians), `_SkyGlowColor`,
`_SkyGlowGain`, `_CoreColour` (set from the blueprint by C#), `_FadeNearRange`,
`_FadeNearRadius`, `_FadeFarRange`, `_FadeFarRadius`.

**Distance fade is mandatory, not decoration.** ED ships four parameters for it
(`gAlphaFadeRange`, `gAlphaFadeRadius`, `gAlphaFadeRangeFar`,
`gAlphaFadeRadiusFar`) and the plane must fade as the camera approaches it:
when you get close to the disc the ray/plane intersection runs to infinity and
a finite texture smears. Implement:

```
float d      = |camera height above the plane|          // map mode only
float nearF  = smoothstep(_FadeNearRange, _FadeNearRadius, d)   // 0 close, 1 far
float farF   = 1 - smoothstep(_FadeFarRange, _FadeFarRadius, d)
float bandA  = nearF * farF
```

Defaults to start with: near 20 / 400 world units, far 40k / 90k (tune against
the map's zoom range). As `bandA` falls, the billboard cloud must already be
drawn underneath — this is the handoff the eye sees as "the plane fades away
when you get close".

Why a screen quad: the inverse-VP reconstruction is the one cost, and URP
defines `UNITY_MATRIX_I_VP` for every camera. The old sphere was ordinary
scene geometry, but it was a closed mesh drawn with `Cull Off`, so every pixel
got the band twice and the dust alpha twice. The map plane's one failure mode —
`viewDir.y ≈ 0` (camera exactly in the plane) — is handled by the map
`_FadeNearRange`; the sky does not intersect a plane at all, so it has no
degenerate ray and no zenith core.

---

## 5. Dust (`OpenFrontier/Galaxy/Dust`)

Same grid, same texture, same UV. Output extinction. The map samples the
bake at LOD 0 and the sky at LOD 3.5; both use the lane response below:

```
float y = dot(bg.rgb, float3(0.2125, 0.7154, 0.0721));
float dust = (1 - smoothstep(0.06, 0.28, y * _DustContrast))
           * smoothstep(0.005, 0.04, y);
dust *= _DustStrength * fade * (1 - smoothstep(0.96, 1.0, r01));
if (!ortho) dust *= 0.15;                    // sky veil, not the map mask
return float4(_DustColour.rgb, saturate(dust));
```

`_DustContrast` default 1.3; `_DustStrength` 0.82; `_DustColour` near-black
with a slight warm bias (dust reddens). The map keeps the full lane mask; the
sky keeps only a 15% veil. At the map strength the wrapped arms turn the whole
band brown and the warm core reads green by contrast, which is not ED's sky.
This is the whole dust lane system - no second texture, no painted lane mask.
If the painted density map already has rifts, they show up here automatically
because the band is darker there.

---

## 6. Glow (`OpenFrontier/Galaxy/Glow`)

The shipped `GalaxyGlow` is analytic: two unrolled 24-step kernels plus the
centre term, i.e. **50 `exp` instructions**, not 24 texture taps. Our
rebuild approximates the radial shape with 12 taps (phone) / 24 taps
(desktop), as a shader `#pragma multi_compile`, and samples the baked band at
each step:

```
float3 glow = 0;
for (i = 0; i < TAPS; i++) {
    float r = (i + 0.5) / TAPS;
    float3 s = SAMPLE_TEXTURE2D(_BandTex, sampler_Band, centre + dir * r * _GlowRadius).rgb;
    glow += s * exp2(-r * _GlowFalloff) * (1 - r);
}
return float4(glow * _GlowGain / TAPS, 1);
```

`_GlowFalloff` 3.0, `_GlowGain` 2.4, `_GlowRadius` 0.18 of the disc. Additive.
This is what makes the bulge bloom without a screen-space pass; the URP bloom
then spreads it further.

---

## 6b. Local dust cards (`OpenFrontier/Galaxy/DustCard`)

ED's cheap-cloud mechanism, and the single most useful mobile win here: instead
of marching three nebula volumes per pixel, draw 24–60 camera-facing textured
cards with a noise map. That is literally what `LocalDarkNebulaDust_Forward`
does (`pLocalNebulaTexture` + `pNoiseMapTexture`, plus a `_ReducedSize` variant
because even ED found it worth halving).

Per-instance: `POSITION` (galaxy-local), `_CardSize`, `_CardRotation`,
`_CardColour`, `_CardSeed`.

```hlsl
float2 uv  = i.uv * 2 - 1;
float  n   = SAMPLE_TEXTURE2D(_NoiseMap, sampler_Noise, i.uv * _NoiseScale + _CardSeed).r;
float  sil = SAMPLE_TEXTURE2D(_DustTex, sampler_Dust, i.uv).a;
float  a   = saturate(sil * (0.55 + 0.45 * n)) * i.colour.a;
float3 c   = _DustColour.rgb * (1 - n * 0.35);
return float4(c, a * _DustCardStrength);
```

- Alpha-blended, no depth write, sorted back-to-front approximately by
  galaxy-local Z. Because the camera is inside the cloud, exact sorting is not
  needed — ED does not sort them either (the effect is alpha blended and
  depth-tested).
- The noise map is one 256² tiling texture shared by every card. Do not give
  each card its own texture.
- This layer is what makes close-up dust read as cloud rather than as a flat
  band, and it costs one draw call.

## 7. Quality rungs

Keep ED's ladder shape (three rungs, Ultra aliases High for the background).
Our names: `Mobile` / `Desktop` / `Hero`.

| Knob | Mobile (= ED Low) | Desktop (= ED Medium) | Hero (= ED High) |
|---|---:|---:|---:|
| Background texture | 512 | 1024 | 2048 |
| Band mesh grid | 16×16 | 32×32 | 64×64 |
| Glow taps | 12 | 24 | 24 |
| Star cloud instances | 8000 | 12000 | 16000 |
| Star cloud brightness | 1.5 | 1.3 | 1.0 |
| Star cloud size | 1.3 | 1.15 | 1.0 |
| Map star sprites (ED StarInstanceCount) | 2000 | 2000 | 4000 |
| Sky catalog stars | 2500 | 5000 | 8000 |
| Nebula volumes (ED) | 10 | 20 | 30 |
| Nebula steps, low/high-res (ED) | 25/128 | 30/110 | 35/138 |
| **Nebula LOD ceiling (ours)** | **64³** | **128³** | **256³** |
| **Nebula steps, size ramp (ours)** | **6–24** | **10–64** | **16–128** |
| **Nebula volumes drawn (ours)** | **10** | **20** | **all** |
| Local dust cards | 24 | 40 | 60 |
| Local dust brightness | 0.13 | 0.033 | 0.0 |
| MW billboards to RT | **yes** | no | no |
| Dust layer | on | on | on |
| Bloom | Off/HighPass | HighPass | Custom |

Notes:
- The **inverse** brightness/size scaling is deliberate: fewer sprites must be
  individually brighter and larger to read as the same band. Do not "fix" it.
- `LocalDustBrightness` is a compensation term for having only 10 of 30 nebula
  volumes and 24 / 40 / 60 dust cards; at Hero it must be exactly 0.
- The **local dust cards** are the close-up cloud layer, not a stand-in for
  the volumes. ED's own `LocalDarkNebulaDust` is a textured card with a noise
  map and a half-res variant; the volumes run on Mobile too (1 MB, ≤24 steps),
  so the cards add near-field structure rather than replacing them.
- The shipped volumetric path is `GalaxyNebulaVolume` + `NebulaQuality` +
  `GalaxyNebulaField`. The rung is a **maximum resolution**, not a fixed
  choice: each volume's authored `quality` slider is a second, per-instance
  ceiling, and the field swaps the uploaded LOD by projected size - near
  volumes the allowed ceiling, mid-size at most 128³, distant and undrawn
  volumes 64³ - with a 0.75 shrink factor at both thresholds so a volume
  hovering at a boundary does not rebuild its texture every tick. Volumes
  parented under a field start at 64³ (1 MB) and refine on the first ranking
  tick; a volume with no field keeps its authored ceiling. The component
  derives the occupied box from alpha; the field ranks volumes 4×/s, draws
  the top N and ramps steps with projected size; the shader discards a ray
  that misses the occupied box and stops at 0.4% transmittance.
- The prefab's `volumeMedium`/`volumeHigh` references are pulled into any
  build that places it: 73 MB per placed prefab, 88% of it the 256³ rung.
  Ship LOD64 only for Mobile or move the high rungs to a desktop-only
  bundle. `NebulaVolumeBuildCheck` prices the enabled scenes at build time.
- The offscreen-RT path is a Low-quality optimisation: render the MW billboard
  layer once into a half-res RT and blit it with `ReducedSizeGalaxyBillboardsQuad`
  semantics. Only build it if the Mobile profile actually misses frame time
  with live billboards — it is a cache, not a feature.

---

## 8. Sky / per-system hook (shipped shape)

The draft's `SystemSky` ScriptableObject and per-system uniform pushes are
obsolete: the explorable miniature renders the sky from wherever the CAMERA
is, so there is nothing to push and no re-bake on a jump. (Our bake is
position-independent; ED re-bakes because its band texture is built from the
observer's point of view - the ray-plane projection does that work at draw
time instead.) What ships is the hook the capture needs:

```csharp
// Assets/OpenFrontier/Scripts/Content/Galaxy/GalaxySky.cs
public class GalaxySky : MonoBehaviour   // on the cloud GO - galaxy-local axes
{
    public Vector3 galacticPosition;    // the system's place in the galaxy
    public Transform observerAnchor;    // child "Sky Observer" at that position
    public void ApplyPosition();        // ContextMenu: place/move the anchor
}
```

`GalaxySky` is the DOCKING POINT of the future capture: the gameplay skybox
will be rendered FROM this scene - a capture camera placed at the system's
galactic position inside the miniature, rendering what the sky looks like
there into a cubemap/panorama. The scene must therefore stay correct from any
point, which every layer already is (camera-agnostic band projection, real 3D
catalog, world-anchored cloud). `galacticPosition` is expressed on the cloud's
local axes - the galaxy's own frame, tilt included.

Deferred from the original §8 (rendering-only scope): the local-star and
black-hole astronomy layers (`gLocalStarDirectionSize*`, `gDirectionAndSizeBH*`)
and the blueprint's per-sky gain block. When generation lands, the catalog's
`GalaxySkyStars.ReseedAllRandom()` is the per-jump entry point.

---

## 9. Shader/texture manifest

| Asset | Type | Source |
|---|---|---|
| `OpenFrontier_GalaxyBand.shader` | new | §4 |
| `OpenFrontier_GalaxyDust.shader` | new | §5 |
| `OpenFrontier_GalaxyGlow.shader` | new | §6 |
| `OpenFrontier_GalaxyDustCard.shader` | new | §6b |
| `OpenFrontier_GalaxySkyStar.shader` | new | §10 |
| `GalaxySkyStar.mat` | new | dedicated procedural sky-star material; queue 3000; keeps the shader from being stripped and gives the inspector a real asset |
| `OpenFrontier_GalaxyCloud.shader` | existing | keep |
| `OpenFrontier_GalaxyNebulaVolume.shader` | existing | keep |
| `GalaxyStarSprites.png` | existing | ED `systemmap_stars` repack |
| `GalaxyBrightSprites.png` | existing | ED `lensflarehq` repack |
| `GalaxyBackground.png` | **generated** | `GalaxyBackgroundBaker`, not in git |

---

## 10. Sky star shader (procedural, magnitude-driven)

ED ships two pixel-shader variants of the same star: the procedural variant is
pure arithmetic, while `_1_0_0_0` (and the other `SkyboxLocalStar` local
variants) declare **Texture2D resources and sample them** — the earlier
"texture-free `_0_0_0_0`" note was wrong. Build the arithmetic shape for the
real 3D catalog (it is the cheaper and the more faithful default for points
that are not a flat star sheet), but keep the textured variants in mind when
comparing the sheet sprites. Match ED's curve:

```
// per-instance: pos (3D, galaxy-local), absMag, colour, seed
float appMag = absMag + 5.0 * log10(max(dist, 1.0) / _TenParsecDistance);
float t     = saturate((_DimmestMag - appMag) / (_DimmestMag - _CappedMag));
float radius = lerp(_SizeDimmest, _SizeCapped, pow(t, _StarRadPow));
float alpha  = pow(t, _StarAlphaPow) * (appMag < _SuperBrightMag ? _SuperBrightPow : 1);
alpha = max(alpha, _MinVisAlpha) * _DimOverride;

float r  = length(uv);              // uv in [-1,1]
float core  = pow(saturate(1 - r), _StarRadPow);
float halo  = smoothstep(_HaloStart, 1.0, r) * _HaloStrength;
float spike = max(0, 1 - abs(uv.x) * _SpikeSharp) * max(0, 1 - abs(uv.y) * _SpikeSharp) * _SpikeStrength;
float ring  = smoothstep(_RingStart, 1.0, r) * _RingStrength;
float3 col  = _StarColour * (core + halo + spike + ring) * alpha;
```

Defaults from ED's immediates: radius clamp `[10, 40]` px, curve exponents
`_StarRadPow ≈ 2`, `_StarAlphaPow ≈ 1.4`, `_SuperBrightPow 3`, halo start 0.55,
spike start 0.15, ring start 0.85. Billboards face the camera in clip space at
the star's REAL position (pixel diameter through `clip.w`): the stars
parallax and can be approached - flying at one, it brightens through the
modulus and grows toward the 40 px clamp. There is no infinity forcing.

The catalog has **two presentations**, exactly ED's split, and the split is a
CAMERA LAYER mask, not an ortho/perspective test:

- **Sky (layer 31)** - `GalaxySkyStars` + `OpenFrontier_GalaxySkyStar`:
  the procedural star above. The in-sky Galaxy Camera includes layer 31 and
  masks out the map layers.
- **Map (layer 30)** - `GalaxyMapStars` +
  `OpenFrontier_GalaxyMapStar`: textured sprites sampled from the
  `systemmap_stars` sheet (repacked as `GalaxyStarSprites.png`, 8×4 cells of
  64 px). Placement follows the same density CDF, so the sprites sit on the
  same arms; per-star cell, size (8-22 px), warm/cool tint and alpha jitter
  come from the seed. Queue 2991, `SrcAlpha One`. The MAP camera includes
  layer 30 and masks out layer 31.

The density disc uses the same layer-30 gate, so it is map-only without an
`unity_OrthoParams` discard. Camera distance never switches a layer off: ED's
map shows its star catalog at every zoom, and the sky catalog parallaxes as
the player flies.

The miniature compresses the real 10 pc into `_TenParsecDistance` (default
1000 world units) - the one authored lie in an otherwise real distance modulus
(5 mag per decade). It is a component knob on `GalaxySkyStars`, tuned so the
apparent-magnitude spread across the disc reads like ED's sky: a resolved
local volume over an unresolved far galaxy that belongs to the band. Do not
set it to a physically literal 10 pc at this scale (a few units): the default
sky camera sits thousands of units out, so every star would land past the
magnitude cap and the layer would collapse into uniform faint dots.

Colour: temperature by `colourIndex`, passed through the same Rec.709 luma so
the band and stars agree.

Per-instance blend state is real in ED (`pBlendOp`, `pSrcBlend`, `pDestBlend`
are part of the star parameter block). We ship one blend: `One One` additive
with alpha premultiplied into the colour in the shader - order-independent, so
the catalog never fights the cloud or the nebula volumes for sort order. A
`SrcAlpha OneMinusSrcAlpha` variant stays reserved for a future map
focus/selection star, as a material variant rather than a runtime switch.

---

## 11. Build order

1. `GalaxyBackgroundBaker` + `OpenFrontier_GalaxyBand.shader` → band visible in
   the map and in the sky from the same texture. **This is the milestone that
   proves the model.**
2. Dust (inversion) → lanes appear for free.
3. Glow (12/24 taps) → bulge bloom.
4. `GalaxySky` → the system's galactic position + the Sky Observer anchor the
   future skybox capture docks at (local stars / black holes deferred).
5. `OpenFrontier_GalaxySkyStar.shader` → the 3D star catalog: real positions,
   apparent magnitude from camera distance.
6. Quality rungs wired to a single `GalaxyQuality` enum; Hero aliases Desktop
   for the background, exactly as ED's Ultra aliases High.
7. Optional: offscreen-RT MW cache for Mobile, only if measured.

## 12. Verification

- Harness green (`bash tools/harness/check.sh`), then
  `grep -in "shader error" Logs/Editor.log` after every shader edit — a failed
  shader reimport silently keeps the last good version.
- Mobile profile: band + dust + glow must cost ≤ 2 draw calls and ≤ 2 MB of
  texture at 512². Count them in the frame debugger, not by eye.
- Visual: at Sol the band should be a thin, bright, dust-rifted strip with the
  core toward Sagittarius; from the rim it should fade to a faint arc. If the
  band is uniformly bright in every direction, the plane projection is wrong
  (the UV is not following the galactic plane).
- Explorable check: fly the camera INSIDE the disc. Nearby catalog stars
  resolve and grow toward the 40 px clamp; the band stays a cylindrical strip;
  from high above, the same stars form the map's field. If far-rim stars are
  visible but nearby ones are not, the modulus scale is off (raise/lower
  `tenParsecDistance`).
- No `_Time` in any new shader; no per-frame `Find*`; no managed allocation in
  `Update`.

---

## 13. Implementation status (2026-11-15)

Build order §11 steps 1–5 are landed (step 4 in its minimal shape); 6–7 are
landed. What exists, and where each decision moved off the draft:

| Spec piece | Status | Shaped as |
|---|---|---|
| §3 Background bake | **landed** | `GalaxyBackgroundBaker.Bake` — RGBA32 sRGB + mips (see §3 note) |
| §4 Band | **landed** | `OpenFrontier_GalaxyBand.shader` — one screen-covering quad; per-fragment ray through `UNITY_MATRIX_I_VP`; ortho/sky branch on `unity_OrthoParams` (see §4 note) |
| §5 Dust | **landed** | `OpenFrontier_GalaxyDust.shader` — same texture inverted, Rec.709 luma, one quad |
| §6 Glow | **landed** | `OpenFrontier_GalaxyGlow.shader` — GLOW_12/GLOW_24 keywords, quad at the core; ED's verified 50-`exp` analytic kernel is the shape reference |
| §6b Dust cards | **landed** | `OpenFrontier_GalaxyDustCard.shader` + `GalaxyDustCards` — silhouette is **procedural** (ED's `pLocalNebulaTexture` bytes were never carved, findings §7.9); noise map is a generated 256² tiling value-noise |
| §7 Quality rungs | **landed** | `GalaxyQualitySettings` — every ED XML knob as a named field; `ApplyPreset` rewrites only the preset block, artistic knobs survive a rung change (the future graphics UI's contract) |
| §8 GalaxySky | **landed (minimal)** | `GalaxySky` — galacticPosition + the "Sky Observer" anchor the future skybox capture docks at; no uniforms to push, no re-bake (the bake is position-independent); local stars / black holes stay deferred (rendering-only scope) |
| §10 Sky stars | **landed (rewritten 2026-11-15, layer-gated 2026-11-16)** | the arithmetic star profile driving a REAL 3D catalog — apparent magnitude from camera distance (real modulus, `_TenParsecDistance` stands in for 10 pc), real positions, layer 31 (sky camera); `GalaxySkyStars` generates the catalog from the blueprint's density CDF. The shipped `SkyboxLocalStar` family is textured; this rebuild uses the arithmetic shape for 3D points. |
| §2 layer 3 Density haze | **landed** | `OpenFrontier_GalaxyDensity.shader` — in-plane disc quad, map layer 30 (not an ortho discard); ED's `{50.0, 1.2}` immediates stay unresolved (findings §7.3), gain defaults 1.05 |
| §2 layer 5 Map stars | **landed (restored 2026-11-15)** | `GalaxyMapStars` + `OpenFrontier_GalaxyMapStar.shader` — ED's `StarMapStar`: `systemmap_stars` sheet sprites (8×4 cells), density-CDF placement, queue 2991, map layer 30; quality knob `mapStarSprites` = 2000/2000/4000 |
| Nebula field placement | **landed (2026-11-15)** | installer scatters the 29 real ED volume prefabs under a `Nebulae` container with `GalaxyNebulaField` (density-weighted, seeded); existing volumes are never moved - hand placement wins |
| Nebula LOD ladder | **landed (2026-11-15)** | `GalaxyNebulaVolume.SetLodLevel` + `GalaxyNebulaField.DesiredLod` - quality slider = per-volume ceiling, platform rung = global ceiling, upload swaps 64³/128³/256³ by projected size with shrink hysteresis; undrawn volumes drop to 64³ |
| Editor preview | **landed (2026-11-15)** | `[ExecuteAlways]` on backdrop, cloud, sky/map stars, dust cards, nebula field + volumes; `GalaxyEditPreview` debounces asset-driven rebake/reseed; `Frame Galaxy (Scene View)` + `Preview Map/Sky Camera (Game View)` give the map and sky presentations in one click, in or out of play |

### 2026-11-16 corrections (pre-Unity validation)

- Band and dust now draw **one screen-covering quad each**; the old closed
  sphere was `Cull Off`, so the band was added twice per pixel and the dust
  alpha was applied twice. Their vertex shaders write far-plane clip space and
  the fragment reconstructs the ray through `UNITY_MATRIX_I_VP`.
- The sky-star magnitude curve now maps the **bright cap to t=1** and the dim
  bound to t=0. The previous fraction had the opposite sign, making faint stars
  large and bright stars vanish. `_TenParsecDistance` defaults to **1000** world
  units so the default sky camera can resolve the bright end.
- Map-star cell selection skips `GalaxyStarSprites` cells **9 and 13** (the two
  square nebula frames). The cloud still uses them, but its circular tile fade
  keeps them soft; at map-sprite size they read as pasted clouds.
- `_BandGain` is a **material** multiplier only. It was also baked into the
  shared texture, squaring the knob and changing the dust/glow source.
- `GalaxySkyStar.mat` is the dedicated sky material (queue 3000), so the
  procedural shader survives build stripping; `GalaxySkyStars` assigns it.
- 2026-11-16 sky pass: the perspective branch is a **cylindrical longitude/
  latitude** sample around the galactic pole, rotated to the observer's true
  core direction, not a ray/plane intersection. `_SkyBandHalfAngle` is 0.30
  rad (~34°). The sky samples the bake at LOD 3.5, compresses its luminance
  contrast and tints it cream; the map keeps the full-resolution warm/blue
  bake. A warm procedural base glow plus a core bump carries the veil, and the
  3D cloud carries the detail.
- The sky dust is deliberately **15% of the map's** alpha and is sampled at
  LOD 3.5. At the map's 0.82 strength the wrapped arms turn the whole band
  brown and the warm core reads green by contrast. The map dust is unchanged.
- `PreviewSkyCamera` now pitches the authored camera toward the galactic plane
  ahead (preserving position/yaw) so the first sky view includes the band, the
  near 3D starfield and the disc instead of only a tan strip at the bottom.
- Sky-star edges fade to exactly `r = 1.0`; the previous `r = 1.0..1.18` fade
  never reached the quad's edge and clipped each sprite into a rounded square.
  The quality asset now drives size 1.6..12 px, radius power 3, alpha power
  1.6, halo 0.35, ring 0.12; the sprite is round and varied, not a uniform dot.
- The band/dust quads write clip space, so they are hidden from the editor
  Scene view, material preview and reflection cameras via URP's
  `RenderPipelineManager.beginCameraRendering` (before culling);
  only `CameraType.Game` (MAP, Galaxy and a Game camera's preview) re-enables
  them. The Scene view now shows the 3D cloud, density, glow and star
  catalogs without a giant camera-facing band sheet; use the Game view's
  Preview Map/Sky Camera for the full band/dust presentation.

Installer: `Assets/Editor/GalaxyRenderInstaller.cs` (menu
`OpenFrontier/Galaxy/Install ED Renderer`) — idempotent, wires cloud +
backdrop + catalog stars + map star sprites + the 29 nebula volumes + cards +
`GalaxySky` under the scene's `hand/` and authors `GalaxyQuality.asset` /
`GalaxyMapStar.mat` on first run. Re-run it after the 2026-11-15 restructure:
it adds `GalaxySky`, `GalaxyMapStars` and the `Nebulae` field to the existing
cloud, and never moves volumes it already placed.

Verified 2026-11-16 (batch import): `bash tools/harness/check.sh` green;
`grep -c "error CS"` = 0 and `grep -ic "shader error"` = 0 after the final
import. Native play captures `30-sky-native`, `31-angled-map-native` and
`32-map-native`, plus aimed sky views `50`-`54`, were reviewed against the
warm spiral, dense starfield and cylindrical band checks.

Still open (not blocking the PC presentation):
1. Measure the mobile rung's band + dust + glow cost on the weakest target;
   the sky dust/band work is two screen quads with one bake sample each.
2. Optional: user screenshots for tint/histogram calibration (findings §8.4)
   and the canonical `ed-assets` C30/C31 review, which stays untouched until
   approved.
3. The finite-disc rim is visible when looking outward along the plane from
   inside the disc (`52-sky-inplane-outward`); that is the galaxy edge, not a
   culling artifact.
