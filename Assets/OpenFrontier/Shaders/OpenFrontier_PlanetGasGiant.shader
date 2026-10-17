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
	// and this shader does NOT use it as one. Adding a bump here would mean
	// shipping lighting that is wrong in a way that is very hard to see
	// and expensive to diagnose: normals pointing away from the camera
	// read as a planet lit from behind, with a soft terminator in the
	// wrong place, and it would disagree with the atmosphere shell's
	// terminator, which is correct.
	//
	// But it IS read -- as DATA, not as directions. See THE DECK MOVES
	// below for what its red channel is asked to do and why that ask is
	// safe where lighting normals were not.
	//
	// A gas giant is also the one planet where this is nearly free. There
	// is no terrain to bump: you are looking at the top of a fluid, so
	// the bands belong in the albedo, not in the normal. MOBILE FIRST --
	// the file is one extra texture sample behind the deck, and it costs
	// nothing for what it buys.
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
	// THE TERMINATOR IS NOT A HARD EDGE
	//
	// Saturn's limb goes dark abruptly -- an actual photosphere in
	// silhouette, and you can see the rings' shadow on the cloud tops.
	// Jupiter does not. Its terminator is soft over several degrees,
	// because there is a deep atmosphere and light entering the limb
	// scatters many times before it reaches cloud level and scatters again
	// on the way out. So this wraps the diffuse, and the wrap amount is a
	// property rather than a constant, because it is the difference
	// between the two gas giants a player is most likely to see.
	//
	// Wrapping is what you get for free when the terminator is soft, so
	// this is not a fudge on top of one. For an atmosphere of optical
	// depth tau lit without absorption, the surface brightness falls off
	// roughly as exp(-tau / mu) in the sun's direction, and mu -> 0 at
	// the limb. Normalising so the sub-solar point stays at 1 gives a
	// curve that stays bright across most of the disc and then falls,
	// which is the observed shape.
	//
	// THE DECK MOVES
	//
	// A fluid surface that never moves reads as glass. The deck gets two
	// kinds of motion, and both are the same displacement applied to the
	// ALBEDO's U coordinate before it is sampled -- never to the mesh:
	//
	//   1. BAND SHEAR. Where red is high, the deck is pushed east; where
	//      red is low, west. Belt-versus-zone shear, sourced from the
	//      reference's own band structure, authored into the texture.
	//   2. THE CONVEYOR. The whole deck streams east continuously at
	//      _BandStreamSpeed, and the shear pattern RIDES that stream:
	//      the reference is sampled at u + drift while the deck content
	//      is taken at u - drift, so the shear travels with the flow
	//      instead of pulling the content away from a frozen frame.
	//
	// Two earlier designs are recorded because both failed visibly, for
	// reasons worth more than their lines of code:
	//
	//   - A sin() wobble oscillated: bands slid back and forth. A fluid
	//     that keeps reversing reads as a motor, not weather.
	//   - A two-phase flow-map cross-fade (two deck samples, triangle
	//     weights) never reversed either -- and still read as the
	//     texture "resetting". Its weighted-mean displacement is
	//     identically ZERO at every instant, so the strong band edges,
	//     which dominate perception, slide half a span and then
	//     collapse back to where they started, twice per cycle. Only
	//     noise-like content can hide that collapse; a banded deck
	//     cannot.
	//
	// The conveyor has neither failure, and this is why:
	//
	//   - Every pixel's sampled coordinate runs at
	//     -_BandStreamSpeed * (1 - _BandDispStrength * dRef/du), which
	//     keeps its sign for as long as _BandDispStrength * the
	//     reference's local U gradient stays under 1. Measured over the
	//     composite field, that holds for 99% of texels at the default
	//     strength; the rest are single-texel quantisation spikes,
	//     under one screen pixel wide at planet scale. If a hard zoom
	//     ever shows local counterflow at a band edge, that is this
	//     threshold -- lower _BandDispStrength.
	//   - The shear pattern moves WITH the content, so the offset
	//     between any two latitudes is a difference of two reference
	//     values travelling together: bounded by the reference's own
	//     amplitude FOREVER. Bands never drift apart. That bound is
	//     what a plain monotone drift lacks -- it shears the deck to
	//     death over hours -- and it is why the pattern rides rather
	//     than stands.
	//   - The reference's argument and the deck's separate at twice the
	//     stream speed, so the shear morphology slowly transits the
	//     disc: a whole pattern lap every 1/_BandStreamSpeed seconds,
	//     about 17 minutes at the default. Bands continuously rearrange
	//     their turbulence against each other, with no clock a viewer
	//     could learn.
	//
	// One measured caveat, deliberately not hidden. The reference has a
	// U-wrap seam: column 0 against column 2047 differs by 12.3x its
	// interior column step in red (8.8x after the pow). That seam rides
	// the conveyor and crosses the disc once per pattern lap. On the
	// deck it is a shear step of _BandDispStrength * the seam delta --
	// about a tenth of a deck texel at 2048, under the footprint of one
	// screen pixel on any planet MIP the game actually shows. It is
	// written down because "there is a seam and it moves" should never
	// have to be discovered in a bug report.
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
	// The drift accumulates: deckU advances per frame by speed/60 --
	// about 1.7e-5 UV at the default -- and runs away through whole
	// texture periods within the hour. In half, whose ulp near 1.0 is
	// 0.0005, that per-frame step would vanish into rounding: the
	// animation would only crawl in half-ulp stair-steps, and the
	// running counter would exhaust a half's exponent range long before
	// a session ended. Everything on the coordinate path is therefore
	// float, and only the RESULT goes back to the sampler. The same
	// reasoning makes _Time.y * speed float. This is a mobile GPU
	// contract, not a style preference; mobile compilers promote to
	// half aggressively.
	//
	// Both the ALBEDO and the band reference must wrap-tile in U for
	// this to be safe, because deckU's counter runs through whole
	// texture periods over a session. The albedo does, at 1.1x interior
	// noise (measured above). The reference does NOT: its column seam
	// is 12.3x its own interior column step -- see the caveat in THE
	// DECK MOVES for what that costs once sheared and mip-reduced,
	// which is under a screen pixel. Wrap repeat is already both
	// assets' import setting.
	//
	// The BANDS ARE ALBEDO section above still governs the look: contrast,
	// terminator and limb all apply AFTER the deck displacement, so they
	// survive the motion untouched.
	//
	// MOBILE FIRST
	//
	// Two texture samples -- the deck, and the band reference that
	// steers it -- and about a dozen ALU operations over what a static
	// planet needs; no tangent attribute and no tangent interpolator,
	// no shadow caster and no depth pass --
	// the mesh is a 2143-vertex sphere, and a planet is never close enough
	// to the camera for self-shadowing to matter. No parallax, no rim, no
	// Fresnel, no specular: all of those are per-pixel work on a surface
	// that fills a large part of the screen, and none of them is what
	// makes a gas giant look like a gas giant.

	Properties
	{
		[MainTexture] _Albedo ("Cloud Deck (RGB)", 2D) = "white" {}
		[HDR] _Tint ("Band Tint", Color) = (1, 1, 1, 1)

		// ---- light ----
		// How far past the terminator light still reaches. 0 is a hard
		// edge (a body with a visible surface, Saturn's look); higher is
		// softer and further around the limb.
		//
		// The value is in units of N.L, so it runs 0 to 1 and the full
		// range covers "terminator in one pixel" to "glow wraps half way
		// round the planet". Above about 0.5 it starts to look lit where
		// it should be night.
		_TerminatorSoftness ("Terminator Softness", Range(0, 1)) = 0.22
		// Remap the diffuse itself. The texture is an albedo map and
		// nothing about it is normalised: measured over the whole image
		// it runs 0.06 to 0.75 per channel with a mean near 0.42, so the
		// belts are dark and the zones are mid rather than the zones
		// being white. Raising contrast here separates the belts from
		// the zones without re-authoring the texture.
		_BandContrast ("Band Contrast", Range(0.5, 4)) = 1.15
		// Where the contrast pivots. 0.42 is the texture's own mean, so
		// 1.0 is a pure contrast change that neither brightens nor
		// darkens the planet overall.
		_BandPivot ("Band Pivot", Range(0.1, 0.8)) = 0.42

		// ---- band motion (the deck streams) ----
		//
		// The deck displaces from gas_giant_06_normal's RED channel, read
		// as a reference value at each pixel, never as a lighting normal.
		// _BandNeutral decides which red value reads as "no displacement"
		// and _BandSoftness dilutes the file's 88 quantisation levels and
		// its hard row seams into that same remap.
		//
		// Motion is a conveyor. _BandStreamSpeed is UV PER SECOND -- the
		// deck simply travels east; at 0.001 a 2048-wide lap takes about
		// 17 minutes, or half a texel per second. _BandDispStrength
		// scales how far a latitude's shear deviates it from the
		// conveyor; because the reference rides the stream with the
		// deck, bands shear against each other only within the
		// reference's own amplitude, so nothing resets and nothing
		// tears, however long the session runs. Full reasoning in THE
		// DECK MOVES.
		[NoScaleOffset] _BandRef ("Band Reference (R: gas_giant_06_normal)", 2D) = "black" {}
		_BandDispStrength ("Band Shear", Range(0, 0.06)) = 0.012
		_BandStreamSpeed ("Band Stream Speed", Range(0, 0.01)) = 0.001
		_BandSoftness ("Band Softness", Range(1, 8)) = 3
		_BandNeutral ("Band Neutral", Range(0, 1)) = 0.6

		// ---- the limb ----
		// How much darker the planet gets toward its own edge.
		//
		// Real limb darkening: a deep atmosphere is optically thicker
		// along a grazing path, so less light comes back out. On a
		// planet this size you see it as the disc having a soft dark
		// ring inside the edge, which is most of what separates a gas
		// giant from a painted ball.
		//
		// 1 is off. This multiplies the lit colour only, so it cannot
		// brighten anything.
		_LimbDarkening ("Limb Darkening", Range(1, 4)) = 1.6
		// The faint warm glow just inside the lit edge, where sunlight
		// passes through a lot of atmosphere and comes back scattered.
		// Kept small and tinted with the star rather than added flat,
		// so it follows the sun and cannot survive on the night side.
		[HDR] _LimbGlowColor ("Limb Glow Colour", Color) = (1, 0.82, 0.6, 1)
		_LimbGlowStrength ("Limb Glow Strength", Range(0, 2)) = 0.35

		// ---- what is behind it ----
		// Starlight, and nothing else. There is no ambient probe on a
		// planet in open space, so the night side has to be lifted by
		// hand or it is a black hole in the middle of the sector map.
		//
		// Deliberately NOT a tinted ambient: a constant of any colour is
		// the same on every surface whatever is lighting it, so it cannot
		// darken where the planet's shadow falls and it does not move
		// when the sun does. This is neutral, and small. See the note on
		// the ring shader's _TransmitStrength for the same argument
		// about the unlit side of a planet.
		_Ambient ("Starlight", Range(0, 0.5)) = 0.03
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

			// Every material property belongs here, textures included by way
			// of their _ST, or the SRP Batcher drops to per-draw setup.
			// _BandRef is [NoScaleOffset], so it declares no _ST.
			CBUFFER_START(UnityPerMaterial)
				float4 _Albedo_ST;
				float4 _Tint;
				float4 _LimbGlowColor;
				float _TerminatorSoftness;
				float _BandContrast;
				float _BandPivot;
				float _LimbDarkening;
				float _LimbGlowStrength;
				float _Ambient;
				float _BandDispStrength;
				float _BandStreamSpeed;
				float _BandSoftness;
				float _BandNeutral;
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

			// The lit part of the atmosphere, as a fraction of full.
			//
			// Wrapped rather than clipped: saturate((N.L + w) / (1 + w)).
			// At w = 0 this is exactly saturate(N.L), the ordinary
			// Lambert term. As w rises the lit region extends past the
			// geometric terminator and the edge of the disc softens, which
			// is the shape a deep atmosphere actually produces.
			//
			// Divide rather than subtract so the sub-solar point stays at
			// exactly 1 for every w. Subtracting w instead would darken
			// the whole planet as the terminator softened, which reads as
			// the sun dimming rather than as the shadow spreading.
			half WrapDiffuse(half ndotl, half softness)
			{
				return saturate((ndotl + softness) / (1.0h + softness));
			}

			// Limb darkening, as a multiplier on 1 at the sub-solar point.
			//
			// N.V is 1 dead centre and 0 at the silhouette, so the term
			// that grows toward the edge is (1 - N.V). It is used with a
			// zero at the centre: _LimbDarkening is 1 for "off", and the
			// result is 1 at ndotv = 1 for every value of it, so this can
			// only ever darken and never brightens or lifts the whole
			// disc.
			//
			// Saturating rather than driving to zero at the very edge is
			// deliberate. A planet's atmosphere is optically THICKEST at
			// the silhouette, so the last few degrees before the edge
			// should be the brightest part of the limb, not the darkest;
			// the glow term below sits in that same few degrees. Letting
			// the darkening run all the way to zero fights it and the two
			// cancel at the edge, which is where the eye looks.
			half EvaluateLimbShade(half ndotv, half strength)
			{
				return 1.0h / (1.0h + (strength - 1.0h) * (1.0h - ndotv));
			}

			half4 frag(Varyings input) : SV_Target
			{
				// ---- band motion (conveyor + riding shear) ----
				//   drift = _BandStreamSpeed * _Time.y
				//   deckU = u - drift
				//       + _BandDispStrength * (pow(ref(u + drift).r, softness) - neutral)
				//
				// The deck content is taken at u - drift: every pixel
				// streams east forever at the conveyor rate. The shear
				// reference is taken at u + drift: it rides the flow
				// ahead of the content, so a latitude's shear changes
				// only as the pattern transits, and the offset between
				// two latitudes is a difference of two reference values
				// travelling together -- bounded, never accumulating.
				// The per-pixel rate of deckU is
				//   -_BandStreamSpeed * (1 - _BandDispStrength * dRef/du),
				// which keeps its sign unless the reference's local U
				// gradient defeats it -- measured to be single-texel
				// quantisation spikes only, below one screen pixel at
				// planet scale. See THE DECK MOVES.
				//
				// No phase, no cross-fade, no wrap: there is nothing in
				// this motion that can reset, and that is the whole
				// point. deckU's counter grows unbounded -- the REPEAT
				// sampler absorbs it and the albedo is U-seamless -- so
				// the sampler never sees a snap, and that is why the
				// fp32 contract matters. Do not half this. See ON
				// PRECISION.
				float drift = _BandStreamSpeed * _Time.y;
				half  bandAmount = pow(SAMPLE_TEXTURE2D(_BandRef, sampler_BandRef, float2(float(input.uv.x) + drift, input.uv.y)).r, (half)_BandSoftness);
				half  bandOffset = bandAmount - (half)_BandNeutral;
				float deckU = float(input.uv.x) - drift + (float)bandOffset * _BandDispStrength;

				// The cloud deck. ONE deck sample plus the band reference
				// above -- that is this shader's whole texture budget.
				half3 albedo = SAMPLE_TEXTURE2D(_Albedo, sampler_Albedo, float2(deckU, input.uv.y)).rgb;

				// Band contrast about a pivot, which is what separates the
				// belts from the zones. Multiplicative about a pivot and
				// not an additive lift, so a dark belt stays dark.
				albedo = (albedo - _BandPivot) * _BandContrast + _BandPivot;

				albedo *= _Tint.rgb;

				// This is a sphere, so the interpolated normal is unit
				// length apart from float error -- normalise it anyway
				// rather than trust it, because the whole terminator hangs
				// off this and a mesh that ever ships a scaled or unnormal
				// stream would move it.
				half3 N = normalize(input.normalWS);
				half3 V = normalize(GetCameraPositionWS() - input.positionWS);

				// Through GetMainLight(), deliberately. See WHICH WAY IS THE
				// SUN in the header -- _SunDirectionWorld is stale on every
				// frame the sun billboard's write is not reached.
				Light mainLight = GetMainLight();
				half3 L = normalize(mainLight.direction);
				half ndotl = dot(N, L);
				half ndotv = saturate(dot(N, V));

				// ---- the terminator ----
				half diffuseTerm = WrapDiffuse(ndotl, (half)_TerminatorSoftness);
				// Shadows: the main light's shadow attenuation. A planet
				// this size is not going to shadow-map itself usefully, but
				// a ring or a moon between it and the sun can cast into
				// this, and the term is free because GetMainLight() has
				// already fetched it.
				diffuseTerm *= mainLight.shadowAttenuation;

				// No second lit-side gate here, and that is deliberate.
				// The obvious one to reach for is
				// smoothstep(-_TerminatorSoftness, _TerminatorSoftness,
				// ndotl), and it is wrong twice over:
				//
				//   - At softness 0 -- a legal value of the slider, and the
				//     one that gives a hard terminator -- its two edges are
				//     equal and smoothstep divides by (edge1 - edge0). That
				//     is a divide by zero, undefined on the hardware, and a
				//     divide per pixel on a surface that can cover most of
				//     the screen.
				//   - Replacing it with saturate(ndotl) trades that for a
				//     subtler bug: it is 0 for every N.L below zero, which
				//     is exactly where the wrap is still contributing, so it
				//     cancels the terminator softening completely and the
				//     slider silently does nothing.
				//
				// The wrap is already the gate, on its own: it reaches zero
				// at N.L = -softness and is clamped there, so no part of
				// the night side is ever lit. What survives is a thin arc
				// from -softness to 0, which is forward-scattered twilight
				// just past the geometric terminator -- the thing you see on
				// a real gas giant, and the reason the rim below does not
				// need gating of its own.

				// ---- the limb ----
				// Darker toward the edge, on the lit side only, because
				// the light leaving at a grazing angle came through more
				// atmosphere. LimbShade is 1 at the sub-solar point for
				// every _LimbDarkening, so this is a pure darkening.
				half limbShade = EvaluateLimbShade(ndotv, (half)_LimbDarkening);

				half3 color = albedo * mainLight.color * diffuseTerm * limbShade;

				// The glow band just inside the lit edge. Tinted with the
				// star rather than added flat, so it takes the star's
				// colour, and gated by diffuseTerm so it cannot appear
				// where the planet is in shadow.
				//
				// Concentrated at the edge by (1 - N.V)^3: N.V is 1 at the
				// centre and 0 at the limb, so this is 0 across most of the
				// disc and rises only in the last few degrees before the
				// silhouette. Cubed because the effect is a thin shell, and
				// a square would put a visible ring a few pixels wide
				// inside the edge.
				half rim = pow(1.0h - ndotv, 3.0h);
				color += _LimbGlowColor.rgb * mainLight.color * rim * diffuseTerm * _LimbGlowStrength;

				// Starlight, last and smallest. Neutral and constant, and
				// only because there is no ambient probe out here; see the
				// note on _Ambient in the properties for why it is not a
				// tinted constant.
				color += albedo * _Ambient;

				// The texture's alpha is a flat 1.0 across all 2048x1024
				// texels and carries no data, so this pass is opaque and
				// does not read it.
				return half4(color, 1.0h);
			}
			ENDHLSL
		}
	}
}