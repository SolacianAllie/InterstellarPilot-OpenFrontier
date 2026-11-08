Shader "OpenFrontier/Planet Gas Giant"
{
	// A gas giant: a banded atmosphere lit by one distant star.
	//
	// SHADER NAME -- SPACES ON PURPOSE
	//
	// This is "OpenFrontier/Planet Gas Giant" rather than
	// "OpenFrontier/PlanetGasGiant", and that is a findability decision,
	// not a style one. Unity's shader picker searches the DECLARED name,
	// not the filename and not the folder, so a user looking for a gas
	// giant types "gas giant" with a space. Against the run-together name
	// that search matches nothing, and the shader reads as absent even
	// though it is right there in Assets/OpenFrontier/Shaders/.
	//
	// So every planet shader is declared with spaced words and grouped
	// under OpenFrontier/:
	//
	//   OpenFrontier/Planet Gas Giant    (this file)
	//   OpenFrontier/Planet Surface      (SpaceUnity_PlanetStandard.shader)
	//   OpenFrontier/Planet Atmosphere
	//   OpenFrontier/Planetary Ring
	//
	// Two of them used to be SpaceUnity/... , which is the original game's
	// namespace, not ours. The FILENAMES were left alone -- renaming those
	// would orphan the GUIDs and is not needed to be findable.
	//
	// ONE WARNING. ActiveUnitPlanet.cs and ActiveUnitPlanetRing.cs match
	// the RING shader by material.shader.name against a hardcoded string.
	// Renaming a shader that is matched that way does not error: the
	// comparison just stops matching, and the effect vanishes silently. If
	// you rename one of these four, grep for its old name in
	// OpenFrontierLogic/ before you assume you are done.
	//
	// FROM SCRATCH, from two textures only --
	// gas_giant_06_diffuse and gas_giant_06_normal. Nothing here descends
	// from the earlier OpenFrontier/PlanetGasGiant, and none of its
	// features are carried over.
	//
	// WHAT THE TWO TEXTURES ACTUALLY ARE
	//
	// The diffuse is good. 2048x1024, a real Jupiter-like belt-and-zone
	// albedo in equirectangular layout: dark brown belts, cream zones,
	// turbulent shear along the boundaries, a couple of oval storms, and
	// a near-uniform grey at both poles. It tiles in U almost perfectly --
	// column 0 against column -1 differs by 0.0067 against an interior
	// step of 0.0059, so it is 1.1x the background noise rather than a
	// seam. Its alpha is a flat 1.0 everywhere and carries nothing, which
	// is worth knowing because the .meta has alphaIsTransparency: 1.
	//
	// The normal map is not a normal map. This is measured, not guessed,
	// and it is why there is no bump in this shader:
	//
	//   - Its blue channel is 0. The mean is 0.0002 and the 99th
	//     percentile is 0. Only 0.34% of texels have any blue in them at
	//     all. A tangent-space normal map stores Z, and Z must be near 1
	//     for a surface facing the viewer; decoding 0 gives Z = -1, so
	//     every normal in the image points AWAY from the camera.
	//   - Decoded, the mean vector length is 1.176, not 1.0. A normal map
	//     is unit length by construction, so these are not normals.
	//   - It is not derived from the diffuse either. Correlation of its
	//     red channel against the gradient of the diffuse's luminance is
	//     -0.075, and green against the same is 0.020. A normal map
	//     authored alongside its albedo would track it.
	//   - It is heavily quantised: 88 distinct 8-bit levels in red and
	//     green, 9 in blue. Individual rows hold as few as 13 distinct
	//     values.
	//   - It has hard horizontal seams. Red's row-mean jumps hard at rows
	//     183, 325-329, 695 and 837-841, where the median step is 0.0048
	//     and the worst is 0.0758.
	//
	// Visually it is orange and green stripes. Its green channel is
	// recognisably a crater field, which belongs to a rocky body, not to
	// a planet made of cloud. The whole gas-giant set is affected --
	// gas_giant_01_normal has a single distinct blue level, i.e. a
	// constant zero -- so this is a property of the source set and not of
	// one bad export.
	//
	// The honest reading is that this file is not usable as a normal map,
	// and this shader does NOT use it as one -- not for lighting, and not
	// for relief either, which was tried and died on its own demo.
	//
	// Decoding remains refused for the reason that started this section:
	// normals pointing away from the camera read as a planet lit from
	// behind, with a soft terminator in the wrong place, disagreeing with
	// the atmosphere shell's terminator, which is correct.
	//
	// The RELIEF ATTEMPT, for the record. Green is measurably a smooth
	// height field (mean step 0.020 over 16 texels, p95 0.059, worst
	// 0.19; U-wrap seam 0.0023 against an interior column step of
	// 0.0022; worst row-mean jump 0.008 against red's 0.076), so it was
	// converted by gradient -- the only honest way for data like this --
	// and shipped as surface relief riding the deck's motion. It was
	// seen in play and removed the same session on one word of verdict:
	// "awful". The full autopsy is under THE RELIEF below. What survives
	// of it here is the measurements, kept because they are facts about
	// the file, not because the feature exists.
	//
	// The cheap-planet note survives: there is no terrain to bump, the
	// bands belong in the albedo, and the reference is one extra texture
	// sample behind the deck, costing nothing for what it buys.
	//
	// THE BANDS ARE ALBEDO, AND THAT IS PHYSICAL
	//
	// Jupiter's belts and zones are colour differences in the cloud deck:
	// ammonia cirrus and ammonium hydrosulfide at different altitudes,
	// seen from above. They emit no light of their own. So the bands are
	// multiplied by the lighting and they vanish at the terminator. This
	// shader does that, which is most of why it reads as a gas giant.
	//
	// WHICH WAY IS THE SUN
	//
	// Read through GetMainLight(), not from a global. SunBillboard.cs
	// writes _SunDirectionWorld, and that write is only reached when both
	// WorldCamera.Resolve() and cachedLight succeed -- on every other
	// frame the global keeps whatever it last held, which is not this
	// scene's light. This project has already been bitten by exactly that:
	// the planet atmosphere shader stopped reading it, because its rim
	// wrapped right around the planet while the surface was correctly in
	// shadow. _MainLightPosition is the same vector GetMainLight() hands
	// out as .direction, so reading it through there makes the planet's
	// terminator and the atmosphere shell's terminator the same line by
	// construction rather than two systems each guessing. See
	// OpenFrontier_PlanetAtmosphere.shader.
	//
	// THE ATMOSPHERE, PORTED FROM THE NORMAL PLANET
	//
	// The terminator and the limb were two bespoke terms here (a wrapped
	// N.L, a limb-darkening curve, an emissive rim glow). They are now the
	// SAME atmosphere the rocky planets use, lifted out of
	// SpaceUnity_PlanetStandard.shader ("OpenFrontier/Planet Surface")
	// with its property names, ranges and defaults intact:
	//
	//   _AtmoColor, _SunRimColor, _SunRimDeep, _Haze*     the air
	//   _Twilight*                                          the light
	//
	// Two reasons to port rather than invent again. ONE VOCABULARY:
	// "Atmosphere Color" on this material means what it means on every
	// other planet and on the atmosphere shell, which deliberately shares
	// these same names -- the surface and the shell have to agree about
	// the colour of the air, or one of them is lying. And the old rim glow
	// was EMISSIVE: a painted edge that brightened the night side, which
	// is precisely the term the normal shader's own comments say belongs
	// on the shell instead.
	//
	// What the port brings:
	//
	//   - TWILIGHT is the terminator now. The light itself reddens and
	//     dims as the air mass along the sun's own path grows (1/cosZ),
	//     so a warm band straddles the geometric terminator. The old wrap
	//     was a cheaper lie about the same boundary: it lit a fixed
	//     distance PAST the terminator instead of bending the light AT it.
	//   - AERIAL PERSPECTIVE is the limb. Airmass through the view ray
	//     (1/cos of the angle between the view and the surface normal) is
	//     what concentrates the wash at the horizon rather than laying a
	//     flat tint over the whole disc, and it is a lerp, not a
	//     replace, so the deck and its motion stay visible underneath.
	//
	// Everything keys off the GEOMETRIC normal, which the port's comments
	// insist on for two reasons that both apply here: the terminator is a
	// property of the SPHERE (a bump on the night side tilting toward the
	// sun reads as lit), and airmass is a property of the VIEW (feeding
	// terrain relief into it makes the wash follow every ridge instead of
	// the limb). This shader has no normal map at all, so the interpolated
	// vertex normal IS the geometric one and the port is exact rather than
	// approximate.
	//
	// SINCE THAT PORT, THE AIR LEARNED THE STAR'S COLOUR: the haze tint
	// multiplies the main light's colour taken as HUE ONLY -- normalized
	// by its brightest channel, guarded against black -- which is the
	// atmosphere shell's own doctrine for this very colour (see its
	// lightHue). The deck and the sheen already light from mainLight.color
	// through sunColour, so the air was the one term left painting a white
	// sun's _AtmoColor under any star: the observation the fix answered.
	// Amounts stay light-unaware on purpose -- hue belongs in the tint,
	// brightness belongs in the existing factors. The planet surface
	// carries the same five lines; keep the two in step if either moves.
	//
	// WHAT A GAS GIANT'S ATMOSPHERE WANTS
	//
	// The port brought the rocky planets' atmosphere over COMPLETE -- the
	// machinery is air under one star either way, and it stays untouched.
	// What needed fixing was what the material was feeding it:
	// GasGiant.mat's atmosphere values predate this shader and were
	// authored for the OLD bespoke terminator and limb glow, on whose
	// units these property names do not agree. Those numbers have been
	// reset to the source's tuned set and then hearded toward what a
	// cloud planet wants:
	//
	//   - _HazeStrength 1 -> 0.45, _HazeFalloff 1.36 -> 2.5. The old pair
	//     leaned a broad flat wash across half the disc, which on a rocky
	//     horizon reads as distance but on a bright cream deck reads as a
	//     sticker at the edge. Air over a gas giant reads with a THIN
	//     limb: concentrated tightly at the rim, half as present, with
	//     the deck and its streaming visible underneath.
	//   - _AtmoColor tan -> muted steel blue (0.45, 0.66, 0.87). The tan
	//     was indistinguishable from the deck behind it, so the limb
	//     hinted nothing at all -- tinting atmosphere a colour your
	//     surface already is is how you paint NO atmosphere. The full-
	//     cyan rocky default (0.35, 0.78, 1) swings the other way: it is
	//     an ocean world's air. The weak cool blue over a warm deck is
	//     what makes a limb read as air on a cream-and-brown planet.
	//   - _SunRimDeep primary red -> (1, 0.30, 0.12), the source's.
	//     The two-stage haze tint routes through this colour to keep
	//     chroma up as the sun drops; a channel-flat red is a paint-box
	//     red, not a sunset, and at the deep end it split the warm path
	//     instead of deepening it.
	//   - The whole _Twilight set back at the source's numbers: Strength
	//     0.55, Dim 0.30, Sharpness 0.13, Haze 0.6 (Depth was already
	//     0.25). The material's 1, 0.55, 0.2 and 0.018 were authored
	//     against terms that do not exist here -- in the ported units
	//     its _TwilightHaze of 0.018 meant the air does not bend to red
	//     across the terminator AT ALL, which is the twilight looking
	//     like it is broken. Every other planet gets the tuned band; a
	//     gas giant gets the same band, and if it over-flattens the
	//     belts at dusk it is a slider away in the material, not a code
	//     change.
	//
	// Kept at the source's values because the gas giant wants the same
	// geometry: _HazeSunStrength, _HazeSunAngle, _HazeSunSpread,
	// _HazeSunPower and _TwilightDepth, plus both rim colours that were
	// already right. One vocabulary means one meaning per slider, not
	// one value per planet.
	//
	// WHAT THE SPEC MAP IS
	//
	// gas_giant_01_specular.png, measured, not assumed:
	//
	//   - R is entirely zero. One distinct 8-bit level across 2048x1024,
	//     mean 0.000. B is entirely zero too. So the land-mask reading
	//     the normal shader uses (landMask = saturate(specTex.r)) has
	//     NOTHING to read in this file -- half of that shader's spec
	//     system cannot exist here, and porting it would be porting a
	//     channel that is uniformly off.
	//   - G is the data: mean 0.246, range 0.09-0.345, 48 levels, and
	//     never below 0.09 -- it has no black in it anywhere.
	//   - G is a gloss mask, and specifically an INVERSION of its own
	//     planet's albedo: correlation -0.897 against gas_giant_01's
	//     diffuse luminance. Gloss over the dark belts. That is what a
	//     cloud-top sheen map is.
	//
	// The pairing caveat, measured rather than waved at: this plate was
	// authored against planet 01, and the albedo on GasGiant.mat is
	// planet 06. Correlation against THAT diffuse is -0.409, so there is
	// real alignment but not the near-identity it has with its own
	// planet. The sheen will therefore favour some of 06's bands and not
	// others. There is no 06 specular plate in the set (gas_giant_06 has
	// diffuse and normal only), and the alternative plate,
	// pd_gas_giant_01_specular, is worse aligned still (-0.314), so this
	// one stays bound. Flagged because "the sheen lights the wrong
	// bands" is a bug report waiting to be filed otherwise.
	//
	// Because G never leaves 0.09-0.345, it is remapped through a floor
	// and a range before use -- the same threshold-pair mechanism the
	// normal shader applies to this very channel for its water mask.
	// Used raw, gloss would live in a narrow slice of the lobe's exponent
	// range AND the lobe's amplitude could never exceed 0.345, so the
	// sheen slider would do nothing visible at any setting.
	//
	// The U wrap is clean: seam 0.005 against an interior column step of
	// 0.246, so this plate tiles like the deck does.
	//
	// THE RELIEF, TRIED AND REMOVED
	//
	// For one session, green was a height field converted to surface
	// slopes: texture-space gradient over a 16-texel step, one height and
	// two slope taps per each of the deck's two flow phases (six samples,
	// riding the same displaced coordinates and triangle weight as the
	// albedo, so the relief drifted with its deck), landing on a tangent
	// frame solved per pixel from screen derivatives -- no tangent
	// attribute, because the solve needs none. The solve was verified
	// numerically before being written (20,000 randomized quads plus
	// mirrored, flipped and sheared layouts, all |cos| = 1.0 against the
	// true frame); the one thing it cannot pin is the frame's GLOBAL
	// sign, which belongs to the mesh's UV layout, so the strength
	// slider took a negative half to answer it per mesh. The gain was
	// calibrated on the measured field: 0.02 strength, about 3 degrees
	// of tilt typical at the default, ~25 on the rarest rims.
	//
	// It read badly anyway, and not at the margins. Three real reasons,
	// deducible from the measurements that had looked like good news:
	//
	//   1. Green is a CRATER field (rocky, see above). Rims and dents
	//      are the one character a fluid cloud deck must not read as --
	//      the same reason the header originally said "the bands belong
	//      in the albedo".
	//   2. The relief did not come from THIS deck. Its gradient's
	//      correlation with the diffuse's own gradient is +0.009 --
	//      nothing -- so the lighting disagreed with the albedo it sat
	//      on, swampy rather than revealing.
	//   3. The field slope stats that made the conversion possible
	//      (mean 0.020/16texels) also mean most of the plate is FLAT:
	//      the midfield got nothing from the whole cost, and the
	//      occasional rim slopes arrived as isolated dark/bright streaks
	//      rather than as a coherent surface.
	//
	// Verdict known and recorded: "it looks awful". Removed in the commit
	// after the one that added it, lighting back on the geometric normal,
	// everything else about the session kept. DO NOT RE-ADD without a
	// fresh conversation about where a relief plate that actually
	// matches a gas deck would come from -- the conversion machinery was
	// fine; the data was not.
	//
	// THE DECK MOVES
	//
	// A fluid surface that never moves reads as glass. The deck gets two
	// kinds of motion, and all of them are the same displacement applied to
	// the ALBEDO's U coordinate before it is sampled -- never to the mesh:
	//
	//   1. BAND OFFSET. Where red is high, the deck is pushed east; where
	//      red is low, west. Belt-versus-zone shear, sourced from the
	//      reference's own band structure, authored into the texture.
	//   2. STREAM. The offset amount ramps monotonically west to east over
	//      time and wraps, and the wrap is hidden by a two-phase cross-
	//      fade: two deck samples, each ramping at its own phase half a
	//      cycle apart, blended with triangle weights that are exactly
	//      zero when that phase snaps. The deck therefore streams ONE
	//      direction forever -- no back-and-forth, no snapping, and the
	//      offsets stay bounded within +/-_BandDispStrength per cycle, so
	//      an overnight session cannot tear the deck apart. Flow map's
	//      standard tax: the two deck copies stay exactly one ramp-span
	//      apart wherever red sits away from neutral, which reads as a
	//      faint motion-trail of width up to ~_BandDispStrength.
	//
	// NO ROTATION TERM HERE, DELIBERATELY. Rotation belongs to the engine:
	// ActiveUnitPlanet.Rotate() spins the planet mesh every FixedUpdate at
	// PlanetRotation deg/s scaled by the unit's RotationRateMultiplier, and
	// ActiveUnitPlanetClouds spins a separate cloud shell at its own rate.
	// A texture-space rotation in this shader would be a THIRD one, synced
	// to neither: on a real planet the surface texture would creep against
	// the spinning mesh -- a slow beat between two rates -- and buy nothing.
	// Do not add a Zonal Jet back. A menu backdrop that needs to turn gets
	// RotateObject on its mesh instead: free, and it keeps rotation in one
	// place.
	//
	// Both terms add into one float that offsets the deck's U. No C# needs
	// to know any of this exists: animation is shader-time on _Time.y,
	// which continues while the game is paused behind a PauseWhenShown
	// screen -- deliberately, because a backdrop planet that keeps
	// breathing while every gameplay sim is stopped is what a menu wants,
	// and it costs no script work. Strictness check: _Time.y is unscaled,
	// so a throttle of timeScale does not slow it; you will see that as
	// the clouds crawling during pause. It is cosmetic, and it is the
	// mobile answer: no CPU cost reachable from anywhere.
	//
	// ON PRECISION (wrote this down so it does not bite the next editor).
	// The surviving ramp moves the sample coordinate by about
	// 2 * _BandDispSpeed / 60 ~ 1.3e-3 UV per frame at the default --
	// comfortably above fp16's ulp (5e-4), so half would be tolerable
	// today. Everything on the coordinate path is float anyway: it costs
	// nothing, and the trap this section exists to prevent is that a
	// step far below half's ulp per frame does not animate at all -- it
	// rounds to the same value frame after frame and the motion freezes
	// into stair-steps. Mobile compilers promote to half aggressively, so
	// float here is a contract, not a preference.
	//
	// The deck's ALBEDO texture must wrap-tile in U for this to be safe:
	// it does, at 1.1x interior noise (measured above). Wrap repeat is
	// already the asset's import setting.
	//
	// The BANDS ARE ALBEDO section above still governs the look: the
	// lighting and the whole atmosphere apply AFTER the deck displacement,
	// so they survive the motion untouched, and the haze is a lerp rather
	// than a replace, so the moving deck stays visible under it.
	//
	// MOBILE FIRST
	//
	// Five texture samples: the deck twice (two cross-fade phases), the
	// band reference once (static -- it drives the motion, it must not
	// ride it), and the spec mask twice at the deck's two displaced
	// coordinates. The atmosphere costs NO texture fetches -- one acos
	// and about a dozen pow/rcp/smoothstep/lerp -- and the sheen adds
	// one pow on top of the plate taps above it. No tangent attribute
	// and no tangent interpolator, no shadow caster and no depth pass:
	// the mesh is a 2143-vertex sphere, and a planet is never close
	// enough to the camera for self-shadowing to matter.
	//
	// No parallax and no Fresnel of its own. The sheen is the one
	// specular this surface gets -- the deck's own cloud lobe from the
	// normal planet, driven by the spec mask. The rim this shader used to
	// paint itself is emissive by nature and belongs to the atmosphere
	// shell (OpenFrontier/Planet Atmosphere); the in-surface half is what
	// is below. What is left is the texture, its motion, the light, and
	// the air.

	Properties
	{
		[MainTexture] _Albedo ("Cloud Deck (RGB)", 2D) = "white" {}

		// ---- band motion (the deck moves) ----
		//
		// The deck displaces from gas_giant_06_normal's RED channel, read
		// as a reference value at each pixel, never as a lighting normal.
		// _BandNeutral decides which red value reads as "no displacement",
		// _BandSoftness dilutes the file's 88 quantisation levels and its
		// hard row seams into that same remap, and the two sliders set
		// how far the deck shifts per cycle (_BandDispStrength) and how
		// fast those cycles run so the shear streams continuously in one
		// direction rather than wobbling back and forth (_BandDispSpeed).
		// Rotation is the engine's job -- see NO ROTATION TERM in the
		// header.
		// _BandDispSpeed is CYCLES PER SECOND: one cycle slides a band
		// 2 * _BandDispStrength from west to east, so the visible speed
		// of a band scales with both sliders and with its own red value.
		[NoScaleOffset] _BandRef ("Band Reference (R: gas_giant_06_normal)", 2D) = "black" {}
		_BandDispStrength ("Band Displacement", Range(0, 0.06)) = 0.012
		_BandDispSpeed ("Band Stream Speed", Range(0, 0.5)) = 0.04
		_BandSoftness ("Band Softness", Range(1, 8)) = 3
		_BandNeutral ("Band Neutral", Range(0, 1)) = 0.6

		// ---- the sheen ----
		//
		// The cloud deck's own specular, ported from the cloud term of
		// OpenFrontier/Planet Surface. What is ported is the LOBE, not
		// the water/land machinery around it: a gas giant is a cloud deck
		// all the way down, and this plate's land channel is flat zero
		// anyway (see WHAT THE SPEC MAP IS in the header).
		//
		// [NoScaleOffset] because this plate has to stay registered with
		// the deck -- it is the same equirectangular layout of the same
		// planet, and a tile or offset here would slide the sheen off
		// the bands it belongs to.
		[NoScaleOffset] _SpecMap ("Specular Mask (G: gas_giant_01_specular)", 2D) = "black" {}
		// The remap of G. Floor 0.09 is the file's measured minimum and
		// range 0.26 its measured span, so the default maps the whole
		// plate across 0..1.
		_SpecFloor ("Sheen Floor", Range(0, 1)) = 0.09
		_SpecRange ("Sheen Range", Range(0.01, 1)) = 0.26
		// Gloss, and with it both how bright the sheen can get and how
		// tight it is (the lobe's exponent is exp2(2 + 10 * gloss), so
		// this one number sets size and strength together -- the same
		// double duty _CloudSmoothness does in the source).
		//
		// Kept low deliberately: gloss peaks over the DARK belts, and at
		// 1.0 the lobe would out-shine the cloud it sits on and read as
		// a wet highlight rather than as haze on cloud tops.
		_SpecGloss ("Sheen Gloss", Range(0, 2)) = 0.25

		// ---- the atmosphere ----
		//
		// Ported from OpenFrontier/Planet Surface
		// (SpaceUnity_PlanetStandard.shader), names, ranges and defaults
		// intact on purpose: one vocabulary with the rocky planets and
		// with the atmosphere shell, which deliberately shares these same
		// names. GasGiant.mat's carried values predate this shader and
		// were authored for the OLD bespoke terminator/limb glow whose
		// units no longer exist, so they have been reset to the source's
		// tuned set and then suited to a cloud planet -- what changed and
		// why is in WHAT A GAS GIANT'S ATMOSPHERE WANTS, in the header.
		//
		// _Haze* is the AIR over the surface you are looking at: airmass
		// through the view ray, concentrated at the limb, tinted by how
		// low the sun is. _Twilight* is the LIGHT at the terminator,
		// reddened and dimmed by the air mass along the sun's own path.
		_AtmoColor ("Atmosphere Color", Color) = (0.35,0.78,1,1)
		_SunRimColor ("Sun Rim Color", Color) = (1,0.62,0.30,1)
		_SunRimDeep ("Sun Rim Deep Color", Color) = (1,0.30,0.12,1)
		_HazeStrength ("Haze Strength", Range(0, 1)) = 0.6
		_HazeSunStrength ("Haze Sun Strength", Range(0, 2)) = 0.8
		_HazeFalloff ("Haze Falloff", Range(0.5, 8)) = 2.5
		_HazeSunPower ("Haze Sun Power", Range(0.5, 6)) = 1.3
		_HazeSunAngle ("Haze Sun Angle", Range(0, 180)) = 45
		_HazeSunSpread ("Haze Sun Spread", Range(1, 180)) = 35
		_TwilightColor ("Twilight Color", Color) = (1.0,0.62,0.30,1)
		_TwilightStrength ("Twilight Strength", Range(0, 1)) = 0.55
		_TwilightDim ("Twilight Dimming", Range(0, 1)) = 0.3
		_TwilightSharpness ("Twilight Sharpness", Range(0.01, 1)) = 0.13
		_TwilightDepth ("Twilight Night Depth", Range(0.01, 1)) = 0.25
		_TwilightHaze ("Twilight Haze", Range(0, 1)) = 0.6
	}

	SubShader
	{
		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
			"RenderType" = "Opaque"
			"Queue" = "Geometry"
		}

		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode" = "UniversalForward" }

			// The planet is a solid body and the sky is behind it, so this
			// is an opaque pass writing depth. Cull Back: a sphere is
			// only ever seen from outside, and drawing the far side first
			// would be overdraw for nothing.
			ZWrite On
			ZTest LEqual
			Cull Back

			HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			TEXTURE2D(_Albedo);   SAMPLER(sampler_Albedo);
			TEXTURE2D(_BandRef);  SAMPLER(sampler_BandRef);
			// _SpecMap is [NoScaleOffset] as well, so no _ST for it either.
			TEXTURE2D(_SpecMap);  SAMPLER(sampler_SpecMap);

			// Every material property belongs here, textures included by way
			// of their _ST, or the SRP Batcher drops to per-draw setup.
			// _BandRef is [NoScaleOffset], so it declares no _ST.
			CBUFFER_START(UnityPerMaterial)
				float4 _Albedo_ST;
				float4 _AtmoColor;
				float4 _SunRimColor;
				float4 _SunRimDeep;
				float4 _TwilightColor;
				float _HazeStrength;
				float _HazeSunStrength;
				float _HazeFalloff;
				float _HazeSunPower;
				float _HazeSunAngle;
				float _HazeSunSpread;
				float _TwilightStrength;
				float _TwilightDim;
				float _TwilightSharpness;
				float _TwilightDepth;
				float _TwilightHaze;
				float _BandDispStrength;
				float _BandDispSpeed;
				float _BandSoftness;
				float _BandNeutral;
				float _SpecFloor;
				float _SpecRange;
				float _SpecGloss;
			CBUFFER_END

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float2 uv : TEXCOORD0;
				// No TANGENT. There is no normal map (see the header), so
				// nothing needs a tangent frame, and carrying one costs an
				// attribute and an interpolator for nothing.
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 normalWS : TEXCOORD0;
				float3 positionWS : TEXCOORD1;
				half2 uv : TEXCOORD2;
			};

			Varyings vert(Attributes input)
			{
				Varyings output;
				VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
				VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

				output.positionCS = positionInputs.positionCS;
				output.normalWS = normalInputs.normalWS;
				output.positionWS = positionInputs.positionWS;
				output.uv = TRANSFORM_TEX(input.uv, _Albedo);
				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				// ---- band motion (continuous stream) ----
				// The deck's longitude offset comes from the band
				// reference's RED channel sampled at the pixel's own
				// latitude, and that offset then STREAMS: it ramps
				// monotonically one direction over a bounded cycle and
				// wraps. The wrap is hidden by a two-phase cross-fade --
				// two samples of the deck, each at its own phase half a
				// cycle apart, blended by triangle weights that are
				// exactly zero for whichever phase is snapping. Per band
				// this reads as constant one-way motion with no
				// back-and-forth, and bands whose red value differs
				// stream at different rates -- differential shear
				// without any per-cycle bounce. Documented in THE DECK
				// MOVES and in the properties block.
				//
				// Triangle weight, not smoothstep: a weight must be
				// EXACTLY zero at its phase's snap (p = 0 or 1) or the
				// snap shows as a one-texel flicker every cycle.
				// 1 - |2p - 1| hits 0 at both ends and 1 mid-cycle; the
				// other phase's weight is 1 - weightA exactly, because
				// the two ramps are always exactly one span apart.
				//
				// Everything on the sample-coordinate path is float. The
				// ramp's own step is comfortable at the default speed
				// (2 * _BandDispSpeed / 60 ~ 1.3e-3 UV, above half's
				// 5e-4 ulp), but the trap is that a step below the ulp
				// does not animate at all -- it rounds to the same value
				// every frame -- and Band Stream Speed is a slider someone
				// will turn down. Float by contract. See ON PRECISION.
				float bandPhase = _Time.y * _BandDispSpeed;
				float phaseA = frac(bandPhase);
				float phaseB = frac(bandPhase + 0.5);
				float rampA = 2.0 * phaseA - 1.0;   // -1 .. +1, monotonic in time
				float rampB = 2.0 * phaseB - 1.0;
				float weightA = 1.0 - abs(rampA);   // deckB's weight is 1 - weightA
				half  bandField  = pow(SAMPLE_TEXTURE2D(_BandRef, sampler_BandRef, input.uv).r, (half)_BandSoftness);
				half  bandOffset = bandField - (half)_BandNeutral;
				half  bandAmp    = bandOffset * (half)_BandDispStrength;
				float deckUA = float(input.uv.x) + (float)bandAmp * rampA;
				float deckUB = float(input.uv.x) + (float)bandAmp * rampB;

				// The cloud deck. TWO samples of the albedo, at the two
				// displaced coordinates built above. The spec mask samples
				// through these same two coordinates further down, so it
				// moves as one with the deck it belongs to.
				float2 flowUvA = float2(deckUA, input.uv.y);
				float2 flowUvB = float2(deckUB, input.uv.y);
				half3 deckA = SAMPLE_TEXTURE2D(_Albedo, sampler_Albedo, flowUvA).rgb;
				half3 deckB = SAMPLE_TEXTURE2D(_Albedo, sampler_Albedo, flowUvB).rgb;
				half3 albedo = lerp(deckB, deckA, (half)weightA);

				// The deck goes to the light exactly as sampled: no pivot remap
				// and no tint. A gas giant's bands are what the plate says
				// they are.

				// This is a sphere, so the interpolated normal is unit
				// length apart from float error -- normalise it anyway
				// rather than trust it, because the terminator and the
				// whole atmosphere hang off this and a mesh that ever
				// ships a scaled or unnormal stream would move them.
				half3 N = normalize(input.normalWS);
				half3 V = normalize(GetCameraPositionWS() - input.positionWS);

				// Through GetMainLight(), deliberately. See WHICH WAY IS THE
				// SUN in the header -- _SunDirectionWorld is stale on every
				// frame the sun billboard's write is not reached.
				Light mainLight = GetMainLight();
				half3 L = normalize(mainLight.direction);
				// Both dots in FLOAT: every atmosphere term below is a
				// function of these two, and near the terminator (ndotl at
				// 0) a half's ulp is a visible fraction of the twilight
				// band's width.
				float ndotl = dot((float3)N, (float3)L);
				float ndotv = saturate(dot((float3)N, (float3)V));

				// ---- twilight: the LIGHT, reddening ----
				// Ported from OpenFrontier/Planet Surface; THE ATMOSPHERE
				// in the header says why. Air mass along the sun's path
				// goes as 1/cosZ: 1 looking straight down the sun's rays
				// at the sub-solar point, unbounded at the horizon, and
				// that is exactly the quantity that makes the band appear
				// when you look side on. max() pins it on the night side,
				// where 1/cosZ has no meaning at all, and the second line
				// keeps the band off the far night side without stopping
				// it dead at the terminator -- twilight is the shadow of
				// the planet falling across its own air, so it exists
				// just past the boundary too.
				float airMass = rcp(max(0.06, ndotl));
				float twilight = saturate((airMass - 1.0) * _TwilightSharpness);
				twilight *= 1.0 - saturate(-ndotl * rcp(max(1e-3, _TwilightDepth)));

				// Multiplicative on purpose: real twilight darkens and
				// reddens at the same time, so it scales the light rather
				// than tinting the surface -- a lerp toward the sunset
				// colour would BRIGHTEN the terminator. _TwilightColor is
				// authored as a MULTIPLIER (1.0 passes a channel
				// untouched, 0.3 passes 30% of it) and the dimming is
				// _TwilightDim's separate job, so the band does not hit
				// full sunset hue and full brightness in the same pixel.
				half3 sunColour = mainLight.color
					* lerp(half3(1, 1, 1), _TwilightColor.rgb, saturate(twilight * _TwilightStrength))
					* lerp(1.0, _TwilightDim, twilight);

				// ---- the lit deck ----
				// Plain Lambert, no wrap. The terminator's softness used
				// to come from a wrapped N.L; it is the twilight band's
				// job now, the way the normal planet does it, so there
				// is only ONE opinion in this shader about where the
				// terminator is. The wrap's argument against a second
				// gate still stands and is worth keeping in mind: any
				// smoothstep on ndotl has to keep its two edges
				// DISTINCT, because equal edges divide by zero per
				// pixel. The two below do.
				//
				// shadowAttenuation is free -- GetMainLight() has already
				// fetched it -- and a ring or a moon between the planet
				// and the sun casting into the disc is a real thing to
				// want.
				half3 color = albedo * sunColour * saturate(ndotl) * mainLight.shadowAttenuation;

				// ---- the sheen: the cloud deck's own specular ----
				// Ported from OpenFrontier/Planet Surface, where the same
				// lobe is applied to ITS cloud deck (cloudGloss /
				// cloudSpec there). A gas giant is a cloud deck all the way
				// down, so it is that term which ports -- see WHAT THE SPEC
				// MAP IS in the header for the measurements, including why
				// the water/land half of that system could not.
				//
				// Gloss is G, read as data like every other plate in this
				// shader, remapped through the floor/range pair because the
				// file's G never leaves 0.09-0.345. The lobe's exponent is
				// exp2(2 + 10 * gloss): a tight pinpoint at full gloss, a
				// broad sheen near zero -- the whole range a cloud top has.
				//
				// The mask rides the flow: sampled at the deck's two
				// displaced coordinates and blended by the same triangle
				// weight as the deck, so the sheen streams WITH its deck
				// instead of sliding underneath it by half its span.
				// Remapping after the blend is exact -- the floor/range
				// pair is affine, and affine maps commute with lerps.
				half specG = lerp(
					SAMPLE_TEXTURE2D(_SpecMap, sampler_SpecMap, flowUvB).g,
					SAMPLE_TEXTURE2D(_SpecMap, sampler_SpecMap, flowUvA).g,
					(half)weightA);
				float sheenMask = saturate((specG - _SpecFloor) * rcp(max(1e-3, _SpecRange)));
				half  cloudGloss = (half)_SpecGloss * (half)sheenMask;

				// Additive, and in SUN colour, like the source: this is the
				// star reflecting off cloud tops, so it follows the sun and
				// cannot survive on the night side.
				//
				// Shadowed as well, which is a DEVIATION from the source and
				// deliberately so. There the cloud deck is a separate layer
				// floating above the terrain and is lit unshadowed. Here the
				// deck IS the surface, so a ring or moon between the planet
				// and the sun falls across the cloud tops and their sheen
				// alike; leaving this one unshadowed would put a bright
				// highlight inside a dark band and read as the shadow
				// glowing.
				half3 halfVec = normalize(mainLight.direction + V);
				half  sheen = pow(saturate(dot(N, halfVec)), exp2(2.0 + 10.0 * cloudGloss)) * cloudGloss;
				color += sunColour * sheen * mainLight.shadowAttenuation;

				// ---- aerial perspective: the inside half of the air ----
				// The atmosphere shell only ever draws OUTSIDE the
				// silhouette, so the air stops dead at the planet's edge
				// and the atmosphere reads as a sticker rather than as air
				// sitting on top. This is the inside: near the limb the
				// view ray crosses a lot of atmosphere before it lands,
				// and that air scatters light back over the deck. Airmass
				// through it goes as 1/cos of the angle between the view
				// ray and the surface normal, which is what concentrates
				// the wash at the horizon instead of laying a flat tint
				// over the whole disc.
				//
				// mu is ndotv: 1 face-on, 0 at the silhouette.
				float grazing = pow(saturate(1.0 - ndotv), _HazeFalloff);

				// Two different terminator windows, on purpose: aerial
				// perspective STOPS at the terminator, because it is
				// light coming back off lit air, while the sunset glow
				// STRADDLES it, because it sits on the boundary.
				//
				// (And both smoothsteps keep distinct edges, which is the
				// divide-by-zero trap the old terminator notes warned
				// about: equal edges make the hardware divide by zero, per
				// pixel, on a surface that can cover most of the screen.)
				float hazeLit = smoothstep(0.0, 0.35, ndotl);
				float twilightLit = smoothstep(-0.15, 0.10, ndotl);

				// The sun being low is what makes the limb glow warm, and
				// the ramp is measured in DEGREES off the sun's axis: 0 is
				// looking into the sun, 90 is square across the view, 180
				// is the night side. One acos per pixel -- the source
				// shader calls that worth naming against a PBR call a few
				// lines earlier, and with no PBR here it is the single
				// most expensive instruction in this pass.
				float sunAngle = degrees(acos(saturate(dot(mainLight.direction, V))));
				float lowForUs = saturate((sunAngle - _HazeSunAngle) * rcp(max(1.0, _HazeSunSpread)));
				float lowSun = pow(saturate(1.0 - ndotl), _HazeSunPower);
				float hazeSun = saturate(lowSun * lowForUs);

				// TWO stages, not one: a single lerp from the air's cyan to
				// the rim's orange passes through grey at the side-on
				// ceiling -- measured at (0.71, 0.69, 0.62), R minus B of
				// +0.09, i.e. neutral -- and the twilight could then never
				// look orange no matter how far _TwilightHaze was pushed.
				// Routing through the deep sunset colour keeps the chroma
				// up at every step. The second stage is squared because
				// the deep end is only reached right at the terminator.

				// The air scatters THE STAR's light, so the star's hue
				// rides on the scattered colour. Same doctrine the atmosphere
				// shell applies to this very colour (its lightHue lines):
				// take the main light's colour as HUE ONLY, normalised by the
				// brightest channel, guarded against black -- URP sets
				// _MainLightColor black when there is no main light, and
				// letting that through would black the air out. Hue, never
				// brightness: a red star tints its air red without the rim
				// brightening or dimming with the star's intensity, the same
				// split the shell keeps. The amounts below stay untouched.
				float3 lightRGB = (float3)mainLight.color;
				float colorMax = max(lightRGB.r, max(lightRGB.g, lightRGB.b));
				half3 lightHue = (half3)((colorMax > 1e-4)
					? lightRGB / colorMax : float3(1.0, 1.0, 1.0));

				half3 hazeTint = lerp(lerp(_AtmoColor.rgb, _SunRimColor.rgb, saturate(hazeSun)),
				                      _SunRimDeep.rgb, saturate(hazeSun * hazeSun)) * lightHue;

				// The twilight band as well as the limb term: hazeTint is
				// already the sunset colour wherever the sun is low, so
				// letting a slice of it in across the terminator is what
				// makes the air visibly bend to red instead of only doing
				// it at the silhouette. A lerp, never a replace -- the deck
				// and its motion stay visible underneath it.
				float hazeAmt = saturate(grazing * hazeLit
					* (_HazeStrength + _HazeSunStrength * hazeSun)
					+ twilight * _TwilightHaze * twilightLit);
				color = lerp(color, hazeTint, hazeAmt);

				// The texture's alpha is a flat 1.0 across all 2048x1024
				// texels and carries no data, so this pass is opaque and
				// does not read it.
				return half4(color, 1.0h);
			}
			ENDHLSL
		}
	}
}