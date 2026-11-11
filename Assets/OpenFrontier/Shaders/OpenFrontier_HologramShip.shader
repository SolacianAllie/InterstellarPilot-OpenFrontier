Shader "OpenFrontier/Hologram Ship"
{
	// ---- holograms: the loading screens' spinning ship projection ----
	//
	// SHADER NAME -- SPACES ON PURPOSE (same rule as the planet and ship
	// shaders): the picker searches the declared name, so this is
	// "OpenFrontier/Hologram Ship" and a curator typing "hologram" finds it.
	//
	// WHAT IT IS
	//
	// The curated "Hologram" ship prefab projection for the loading
	// screens, matched to the agreed reference look (2026-11-10): a DIM
	// body fill, PANEL LINES traced bright, a CONTOUR LINE along the
	// mesh edge, and a SCAN FIELD crawling upward - light that behaves
	// like a solid object, never an X-ray.
	// It never touches gameplay ships or their materials:
	// OpenFrontier/Ship Surface keeps flying the hulls; this shader paints
	// curated copies of the SAME hull meshes. The look is procedural:
	//
	//   - BODY FILL: a very low constant, so the silhouette reads but
	//     stays dark - the lines carry the picture. There is NO
	//     directional shading of any kind: the pass is unlit and reads
	//     no light, no ambient, no probe - the ONLY direction it knows
	//     is the camera's. An earlier draft folded a fixed
	//     "never-seen light vector" into the fill; measured on the
	//     first curated ship, its up-bias reproduced scene sun/sky
	//     lighting ("rim seems based too much on scene lighting"), so
	//     the term is gone (2026-11-10): depth information lives in
	//     the traced EDGES and the fresnel is view-only, by
	//     construction incapable of reacting to the scene.
	//   - PANEL EDGES: the ships' own NORMAL MAPS are the blueprint.
	//     Panel seams are normal discontinuities, so a 4-tap edge trace
	//     over the bump map lights every seam and speckles every
	//     greeble (the reference look's line detail). Gated on
	//     _NORMALMAP + _HOLOEDGES; ships without a bump bound fall back
	//     to fill + rim, smooth but whole. 2026-11-10, live-measured:
	//     the mip-0 pin that fixed the view-fade also exposed the
	//     PER-TEXEL grain the compressed normal map ships - and the
	//     greeble grain of a repainted bump - every texel boundary
	//     carrying a delta that, at gain, read two to five times
	//     above the 0.06 body fill: a hull that read as sandpaper.
	//     The tracer gates the raw delta BEFORE the gain - real
	//     plate seams sit an order of magnitude steeper and survive
	//     at full brightness, the grain does not. 2026-11-10, by
	//     request: the floor became the material's "Normal Edge
	//     Noise Removal" slider (the first baked 0.03 kept as its
	//     default; knee 0.05 stays a constant in HLSLINCLUDE). The
	//     knob family was ONE system this day ("Normal Edge
	//     Strength" stays the strength's display name): the fold
	//     DETECTOR that briefly sat next to it was CUT per user
	//     direction - see the EDGE LINES bullet below.
	//     All
	//     bump taps pin
	//     mip 0
	//     EXPLICITLY (2026-11-10): implicit derivative mip selection
	//     climbed levels at grazing camera angles, blurring the normals
	//     and starving the deltas - measured as whole regions fading
	//     with the view and inverting when the camera crossed sides.
	//     With a fixed LOD the tracer ignores the camera entirely; the
	//     cost is possible minification shimmer when a ship renders
	//     small/far - loading rigs show ships large, and a real
	//     small-ship need earns a clamped-LOD knob later. The rim rides
	//     the UNPERTURBED hull normal for the same reason.
	//   - SCANLINES: a faint world-Y energy field crawling UPWARD on
	//     the holo clock (_ScanDrift lines/s; world Y chosen over UV:
	//     UV lines would whirl with the ship). Measured 2026-11-10: a
	//     world-Y field does not move by itself, and the rig's spin is
	//     YAW-only - a yaw turn changes no world Y - so the original
	//     static field sat as frozen horizontal rings, motionless both
	//     in space and on the hull. The crawl IS the scanlines' motion;
	//     _ScanDrift 0 restores the frozen field. The field is
	//     WORLD-SPACE PURE (2026-11-10, by request): an earlier
	//     tangent-space tip folded the bump's greeble normals INTO the
	//     banding - the scan broke into speckle tracing the normals;
	//     only the tracer speaks bump now, the scan does not.
	//   - RIM: a view-only fresnel floor on the UNPERTURBED hull normal
	//     - the camera is the only direction this shader knows. Two
	//     measured lessons live here: a rim riding bumped normals
	//     relocated glare with the view (orange-peel rings), and the
	//     fresnel-rim-as-silhouette committed in d4121fbe rendered as
	//     blotchy wash on bevels - so the rim stays a floor (RimStrength
	//     default 0.35). _RimOffset (born 2026-11-10 by request,
	//     retired the same day by user call - not needed after the
	//     retuning) is gone: RimPower alone shapes the fresnel,
	//     exactly as it read before the knob existed.
	//   - EDGE LINES (2026-11-10): CUT, per user direction - the look
	//     now rides PURELY the rimlight, the normal-map tracer and
	//     the outline ring. The geometric fold detector (screen-space
	//     hull-normal derivative; its "Edge Detection
	//     Strength/Sensitivity" knobs), the Pass-4 screen-space depth
	//     detector and its RenderGraph feature are the same day's
	//     removals. The bump TRACER stays (it reads normal MAPS - the
	//     one system that is not a screen-space derivative); its
	//     full history below. The detector family's measured saga
	//     (born, inverted knob, retired twice) stays written because
	//     it could be invited back.
	//
	// WHY TWO PASSES -- the measured saga (2026-11-10)
	//
	// Attempt 1 (additive, ZWrite Off) X-rayed: every hull part added
	// light over every other part. Attempt 2 (ZWrite On) still X-rayed,
	// and the reason is worth keeping: with additive blending, a far
	// fragment that rasterizes FIRST has already added its light - a
	// nearer fragment can win the depth test afterwards, but a depth
	// write cannot RECALL light already in the framebuffer. Per-pixel
	// raster order is luck, and the open hulls (cockpit, engine
	// channels) roll it constantly. So depth only works if it exists
	// BEFORE any colour is written:
	//
	//   PASS 1 "Solid Depth" (untagged -> SRPDefaultUnlit): ColourMask 0,
	//           ZWrite On - the hull writes solid depth and nothing else.
	//   PASS 2 "Forward":    additive colour, ZTest LEqual against that
	//           solid depth, ZWrite Off - interiors show ONLY through
	//           true openings (cockpit glass, engine mouths) and exhaust
	//           sections genuinely past the silhouette still show. The
	//           silhouette emphasis rides the rim on the mesh itself;
	//           the OUTLINE ring (PASS 3) draws OUTSIDE it.
	//
	// A third pass served the silhouette for one day and died twice.
	// First death: as a second UNTAGGED pass it was never drawn at all
	// - URP's forward draw matches only the FIRST pass per LightMode
	// name, so the second SRPDefaultUnlit never issued a draw call
	// (measured: the width knob repainted and nothing happened).
	// Tagged "UniversalForwardOnly" it rendered - and that fix is
	// worth keeping for ANY future third pass: one pass per LightMode
	// name, distinct tags per pass, and SRPDefaultUnlit ->
	// UniversalForward -> UniversalForwardOnly mirrors the declared
	// order under either reading of the draw contract. SECOND death:
	// once visible it measured BAD live - a band floating BEYOND the
	// mesh, splitting at split-vertex creases on faceted hulls - and
	// the pass was retired. (Its in-fragment fold-glow successor was
	// cut in turn the same day - see the EDGE LINES bullet.) Cross-object
	// depth holds as ever: each object's prepass runs before its own
	// colour, so two ships in one rig occlude each other correctly.
	//
	// 2026-11-10, Stage A by request: the ring RETURNS with the tag
	// fix and every lesson above - PASS 3 "Outline Hull"
	// (UniversalForwardOnly tag, extrusion along vertex normals,
	// masked to the silhouette's outside by PASS 1's solid depth).
	// The crease gap (the one honest limit: faceted hulls split the
	// ring at their split-vertex creases) earned TWO connectors the
	// same day - pivot-radial growth (connected, but the thickness
	// varied with the pivot relationship) and then the winner: the
	// ring ALWAYS inflates along BAKED SMOOTHED NORMALS -
	// HoloOutlineNormalsBaker welds the ships' vertices by position
	// at import, averages the normals each position saw, and bakes
	// the result into vertex COLOR (alpha 0.5 marks data), so the
	// old even-width extrusion connects every 90 degree face. No
	// knob: the grow mode came out by user call the hour it shipped
	// (the bake IS the behavior); a mesh without a valid bake falls
	// back to its own vertex normals - the classical hull, whose
	// crease gap is that mesh's honest state until it is baked. The
	// same day the ring's strength ceiling was raised 4 -> 8 to
	// match Normal Edge Strength - equal sliders, equal brightness
	// (the tint is the same _BaseColor on both systems). The depth+normals prepass
	// with a Sobel feature remains what interior part-boundary
	// lines would need if the ring ever reads thin again - not
	// installed, by the user's call.
	//
	// DETECTOR stage, RETIRED (2026-11-10): after the mesh ring came
	// back (above), the screen-space detector was BUILT by request -
	// PASS 4 wrote the hull's eye-depth-in-metres into the private R16F
	// texture of a RenderGraph EdgeOutlineFeature (Roberts-cross apply
	// after the transparents; _CameraDepthTexture cannot see a
	// transparent queue - copied after opaques only - so the feature
	// rendered its own depth). It fought four separate walls in one
	// session - blit include order (Core.hlsl BEFORE Blit.hlsl),
	// per-pass self-containment (an HLSLPROGRAM shares NOTHING across
	// passes), the renderer-feature map's id domain (local file id,
	// never an instance id), handle casts only at execution - and the
	// fourth made it LIVE, after which the user cut it the same day:
	// still not worth the failure surface for a loading-screen-only
	// look. THE MESH RING (PASS 3) IS THE SHIPPING SILHOUETTE. Limits
	// stand as measured: the ring splits at faceted hulls'
	// split-vertex creases; unpainted folds are invisible to every
	// cheap system (art-side: paint or harden normals).
	//
	// TIME CONTRACT -- THE TRAP THIS SHADER EXISTS TO AVOID
	//
	// Unity's shader _Time follows Time.timeScale. Loading screens run
	// while gameplay world state is frozen (the legacy LoadingScreenUI
	// set timeScale = 0 under itself), so any effect animated on _Time
	// freezes silently in exactly the place freeze is most likely.
	// Nothing here reads _Time: everything animates on the
	// _OpenFrontierHoloTime GLOBAL, which HoloSpinner (on the rig's ship
	// holder) pushes every frame from Time.unscaledTime. No HoloSpinner
	// instance? The global stays 0 and the hologram loses its drift -
	// visible, never silent.
	//
	// NORMAL MAPS AND MATERIALS
	//
	// Reads _BumpMap on UV0 with the same slot and the same _NORMALMAP
	// keyword NAME stock Lit uses, so an extracted ship material carries
	// its bump binding and keyword straight across a shader swap.
	// Normals live on materials, so one material cannot serve every
	// ship: the PARENT material ("Hologram Ship", in
	// Resources/Materials) holds every tuning, and per-ship copies are
	// MATERIAL VARIANTS of it overriding the normal map (and rarely the
	// colour). Tuning the parent retunes the entire fleet. Two notes:
	// the bump scale is FIXED at 1 - the "Normal Scale" knob came out
	// on 2026-11-10 (unreadable tuning; the tracer reads raw bump
	// deltas) - and the normal map needs mesh tangents (FBX
	// "Generate Tangents" default is on).
	//
	// RENDER SHAPE (mobile-first)
	//
	// Unlit Core.hlsl only - no Lighting.hlsl, no realtime lights, no
	// shadow caster (a hologram casts no shadow by definition) and no
	// pipeline keyword forest. Additive over the loading screens'
	// near-black backgrounds - adding is the honest blend for light
	// that arrives, and it makes RT-side black edges invisible. Two
	// passes cost one extra vertex-only run (the solid-depth prepass),
	// four texture taps when the tracer is on - a loading-screen-scoped
	// spend, and the keyword set
	// stays deliberately tiny (_NORMALMAP,
	// _HOLOEDGES): build cost lives in keyword combinations, every other
	// knob is a uniform variants set for free. The ACCENT material
	// ("Hologram Ship Accent", a variant of the parent) serves curated
	// highlights and may switch _HOLOEDGES off for a cleaner shimmer.
	//
	// No CustomEditor on purpose (same rule as Ship Surface): the
	// default inspector lists every property in declaration order - a
	// knob a curator cannot see is a knob that does not exist.

	Properties
	{
		[HDR] _BaseColor("Hologram Colour", Color) = (0.25, 0.85, 1.0, 1.0)
		_Intensity("Overall Intensity", Range(0, 4)) = 1.0

		// The grazing-surface shading, damped for the line-art balance.
		// VIEW-ONLY by definition: fresnel needs nothing but the camera.
		_RimPower("Rim Power", Range(0.25, 8)) = 2.4
		_RimStrength("Rim Strength", Range(0, 4)) = 0.35

		_ScanDensity("Scanlines per Metre", Float) = 6.0
		_ScanStrength("Scanline Strength", Range(0, 1)) = 0.25
		// Lines per second the field crawls UPWARD on the HOLO clock -
		// the unscaled global, never _Time: loading screens stop time.
		_ScanDrift("Scanline Drift Speed", Float) = 3.0

		// ONE LINE SYSTEM (2026-11-10): the NORMAL-MAP PANEL-EDGE
		// tracer - the seams the ships' bump MAPS paint (line work
		// living in art). The geometric fold detector that used to
		// run beside it (screen-space normal derivative with its
		// Strength/Sensitivity knobs) is CUT the same day with the
		// rest of the screen-space machinery: the hull's lines ride
		// traced seams + the rim + the outline ring (PASS 3).
		[Toggle(_HOLOEDGES)] _EdgeToggle("Normal-Map Edges", Float) = 1.0
		_EdgeStrength("Normal Edge Strength", Range(0, 8)) = 2.0
		// The gate above which a traced bump delta counts as a seam
		// rather than grain. 0 = the tracer raw; higher cleans the
		// per-texel noise the mip-0 pin exposed (compression
		// artifacts, repaint grain). Default 0.03 = the first baked
		// gate. Past ~0.08-0.1 fine painted seams start gating out
		// WITH the noise - slide while watching the hull.
		_EdgeNoiseFloor("Normal Edge Noise Removal", Range(0, 0.15)) = 0.03

		// The OUTLINE HULL (PASS 3, 2026-11-10, Stage A by request):
		// a ring of light OUTSIDE the silhouette - the screen-space
		// outline look with no post machinery. Strength 0 = dormant;
		// width lives in OBJECT units (same camera-independence
		// doctrine as the mip-0 pin). The ring is masked by PASS 1's
		// solid depth, so it shows only outside the hull. It ALWAYS
		// inflates along the baked smoothed normals (see below) -
		// hard-edged hulls stay connected.
		_OutlineWidth("Outline Width", Range(0.001, 1.0)) = 0.06
		// Ceiling matches Normal Edge Strength (both 0-8, 2026-11-10
		// by request): the ring is the same _BaseColor tint as the
		// traced seams, so equal slider values reach equal brightness.
		_OutlineStrength("Outline Strength", Range(0, 8)) = 0.0
		// The ring's growth is NOT a knob (2026-11-10, by user call
		// the hour the mode knob shipped): it ALWAYS inflates along
		// the baked smoothed normal from Assets/Editor/
		// HoloOutlineNormalsBaker.cs - vertices welded by position
		// at import, crease-averaged, vertex COLOR rgb = n*0.5+0.5,
		// alpha 0.5 marks data present. Every copy of a crease
		// vertex shares one direction, so the shells connect across
		// 90-degree faces while the width stays the old even
		// per-vertex-normal band. Meshes without a valid bake fall
		// back to their own vertex normals - the classical hull,
		// whose crease gap is that mesh's honest state until it is
		// baked. (Pivot-grow and the mode knob retired; saga in the
		// header.)

		// Same keyword name, same slot pair as stock Lit, so extracted
		// materials swap into this shader with nothing rebound.
		[Toggle(_NORMALMAP)] _NormalMapToggle("Normal Map", Float) = 0.0
		_BumpMap("Normal Map", 2D) = "bump" {}

		// Exposed for curated hollow models (cockpit domes, engine
		// nozzles) where the interior reads as a mess of echo geometry.
		_Cull("Face Culling", Float) = 2.0
	}

	SubShader
	{
		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
			"RenderType" = "Transparent"
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"PreviewType" = "Plane"
		}

		// Shared input: declared ONCE so every pass sees the identical
		// UnityPerMaterial layout (SRP batching requires per-material
		// buffers to match across passes).
		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
		// Explicit: UnpackNormalScale and _BumpMap_TexelSize consumers must
		// not depend on whoever reorganises Core.hlsl next release. Core.hlsl
		// also includes Packing.hlsl today (verified line 25), but the
		// house rule is that no pass ever has to prove the chain.
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

		// NOTE: do not ifdef the properties here - SRP batching cannot
		// handle different bulk layouts between passes (house rule from
		// Ship Surface). _NormalMapToggle/_EdgeToggle drive keywords only
		// and _Cull is consumed by the render state; they stay out of the
		// buffer the same way stock Lit keeps its toggles out.
		CBUFFER_START(UnityPerMaterial)
		half4 _BaseColor;
		half _Intensity;
		half _RimPower;
		half _RimStrength;
		half _ScanDensity;
		half _ScanStrength;
		half _ScanDrift;
		half _EdgeStrength;
		half _EdgeNoiseFloor;
		half _OutlineWidth;
		half _OutlineStrength;
		float4 _BumpMap_TexelSize;
		CBUFFER_END

		TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);

		// Pair-seam pickup for the edge tracer.
		static const half kEdgeGain = 16.0;

		// Noise gate for the tracer (2026-11-10, baked; promoted to
		// the material's "Normal Edge Noise Removal" slider the same
		// day): the mip-0 pin (which fixed the view-fade) also exposed
		// the PER-TEXEL grain the compressed normal map ships with -
		// and the greeble grain of a repainted bump: every texel
		// boundary carried a delta around 0.005-0.02, which at
		// kEdgeGain lands 0.1-0.3 energy, two to five times the 0.06
		// body fill - measured live as a hull that read as sandpaper.
		// Real plate seams are an order of magnitude steeper, so the
		// gate is applied to the raw delta BEFORE the gain. The knee
		// is the one fixed graded span above the floor.
		static const half kEdgeNoiseKnee = 0.05;

		// Shader ground the loading rigs advance: see the header's TIME
		// CONTRACT. Script side: HoloSpinner.HoloTimeId.
		half _OpenFrontierHoloTime;
		ENDHLSL

		// ---- PASS 1: solid depth (untagged -> SRPDefaultUnlit) ----
		//
		// The self-occlusion referendum, written BEFORE any colour exists
		// (see the header's WHY TWO PASSES). ColourMask 0: nothing is
		// drawn, depth becomes solid. ZWrite On is the point of the pass.
		Pass
		{
			Name "Solid Depth"

			ColorMask 0
			ZWrite On
			ZTest LEqual
			Cull [_Cull]

			HLSLPROGRAM
			#pragma vertex HoloDepthVertex
			#pragma fragment HoloDepthFragment

			#pragma multi_compile_instancing

			struct Attributes
			{
				float4 positionOS : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings HoloDepthVertex(Attributes input)
			{
				Varyings output = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
				output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
				return output;
			}

			half HoloDepthFragment(Varyings input) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
				return 0;
			}
			ENDHLSL
		}

		// ---- PASS 2: the projection itself ----
		//
		// Additive colour, tested against the solid depth from PASS 1 -
		// interiors survive ONLY through true openings, exhaust sections
		// genuinely past the silhouette survive, and no raster order can
		// X-ray the hull. Depth already solid: this pass never writes it.
		Pass
		{
			Name "Forward"
			Tags { "LightMode" = "UniversalForward" }

			Blend One One
			ZWrite Off
			ZTest LEqual
			Cull [_Cull]

			HLSLPROGRAM
			#pragma vertex HoloVertex
			#pragma fragment HoloFragment

			// Material keywords - the pair: _NORMALMAP (stock Lit's name,
			// bindings survive the swap) and the edge tracer's
			// _HOLOEDGES. Everything visual is a uniform, so per-ship
			// material variants cost buffer writes, never combinations.
			#pragma shader_feature_local_fragment _NORMALMAP
			#pragma shader_feature_local_fragment _HOLOEDGES

			#pragma multi_compile_instancing

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS   : NORMAL;
				float4 tangentOS  : TANGENT;
				float2 uv0        : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				float2 uv0        : TEXCOORD1;
				float3 normalWS   : TEXCOORD2;
				float4 tangentWS  : TEXCOORD3;	// xyz tangent, w bitangent sign
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings HoloVertex(Attributes input)
			{
				Varyings output = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
				output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
				output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
				output.normalWS = TransformObjectToWorldNormal(input.normalOS);
				output.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
				output.uv0 = input.uv0;
				return output;
			}

			half4 HoloFragment(Varyings input) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				float3 viewDir = GetWorldSpaceNormalizeViewDir(input.positionWS);
				float3 geometryNormal = normalize(input.normalWS);

				// The scan field's Y (the hull turns through this
				// fixed-space field - see the header).
				float scanY = input.positionWS.y;

				// The bump's only consumer: the seam tracer below. The
				// gate needs BOTH keywords - _NORMALMAP is stock Lit's
				// name (an extracted material's bindings survive the
				// shader swap), _HOLOEDGES arms the tracer itself.
				// 2026-11-10, by request: the tangent-space X tip that
				// folded the greeble normals INTO the scan field is
				// gone - the bands broke into speckle tracing the bump
				// (measured on Ranger's hull); the scan overlay now
				// reads pure world Y and the bump owns the lines only.
				#if defined(_NORMALMAP) && defined(_HOLOEDGES)
					// The blueprint tracer: panel seams are normal
					// discontinuities, so neighbour-tap deltas light
					// every seam - and speckle every greeble - with
					// four samples on ship-sized triangles. All four
					// taps pin mip 0 EXPLICITLY (see header): the
					// tracer is camera-independent by construction.
					float2 texel = _BumpMap_TexelSize.xy;
					half3 nX = UnpackNormalScale(SAMPLE_TEXTURE2D_LOD(_BumpMap, sampler_BumpMap, input.uv0 + float2(texel.x, 0.0), 0.0), 1.0)
							 - UnpackNormalScale(SAMPLE_TEXTURE2D_LOD(_BumpMap, sampler_BumpMap, input.uv0 - float2(texel.x, 0.0), 0.0), 1.0);
					half3 nY = UnpackNormalScale(SAMPLE_TEXTURE2D_LOD(_BumpMap, sampler_BumpMap, input.uv0 + float2(0.0, texel.y), 0.0), 1.0)
							 - UnpackNormalScale(SAMPLE_TEXTURE2D_LOD(_BumpMap, sampler_BumpMap, input.uv0 - float2(0.0, texel.y), 0.0), 1.0);
					half edges = saturate(smoothstep(_EdgeNoiseFloor, _EdgeNoiseFloor + kEdgeNoiseKnee, length(nX) + length(nY)) * kEdgeGain) * _EdgeStrength;
				#else
					half edges = 0;
				#endif

				// View-only rim, on the UNPERTURBED hull normal: fresnel
				// spikes riding bumped normals (orange-peel rings)
				// relocated glare with the camera - measured alongside
				// the mip fade. Geometry-level rim is angle-stable;
				// detail belongs to the tracer above. The camera is the
				// only direction this shader knows - no world light
				// exists to react to.
				//
				// _RimOffset (born 2026-11-10 by request, retired the
				// same day by user call - not needed after the
				// retuning) used to slide the band's inner border
				// BEFORE the power remap. Gone: RimPower alone shapes
				// the fresnel, exactly as it read before the knob
				// existed (0 was the identity anyway).
				half rimEdge = (half)saturate(1.0 - saturate(dot(geometryNormal, viewDir)));
				half rim = pow(rimEdge, _RimPower) * _RimStrength;

				// The reading field: thin bright bands every 1/_ScanDensity
				// metre of world height, crawled UPWARD by the holo clock
				// at _ScanDrift lines per second. The clock IS the motion:
				// the rig spins yaw-only and yaw moves no world Y, so a
				// fixed field sat as unmoving horizontal rings (measured
				// 2026-11-10). The clock is the UNSCALED global - loading
				// screens stop time; the field must outlive the freeze.
				half scan = smoothstep((half)0.90, (half)1.0, (half)frac(scanY * (float)_ScanDensity - (float)_OpenFrontierHoloTime * (float)_ScanDrift)) * _ScanStrength;

				// The base film keeps the hull readable even when the
				// scan field sits dark between its bands. No relief term:
				// directional shading was measured out (see the header).
				// and the scanned band glow all multiply the base colour.
				half energy = (half)0.06 + rim + edges + scan;

				return half4(_BaseColor.rgb * (_Intensity * energy), 1.0);
			}
			ENDHLSL
		}

		// ---- PASS 3: the outline hull (2026-11-10, Stage A by request) ----
		//
		// The inverted hull: vertices extrude outward along their
		// normals, backfaces only draw, and PASS 1's solid depth masks
		// everything that would land OVER the ship - the pass paints
		// exactly one glowing ring OUTSIDE the silhouette. Tag
		// UniversalForwardOnly - the rule the WHY TWO PASSES saga paid
		// for: URP's forward draw matches ONE pass per LightMode name
		// (a second untagged pass never issues a draw call - measured),
		// and SRPDefaultUnlit -> UniversalForward ->
		// UniversalForwardOnly mirrors the declared order. Additive
		// like PASS 2, ZWrite Off: strength 0 adds nothing, killing
		// the pass without a branch anywhere. Width is OBJECT units
		// (see the property note). The split-vertex caveat stands:
		// smooth-shaded ships keep one continuous shell; faceted
		// hulls split the ring at their creases.
		Pass
		{
			Name "Outline Hull"
			Tags { "LightMode" = "UniversalForwardOnly" }

			Blend One One
			ZWrite Off
			ZTest LEqual
			Cull Front
			ColorMask RGB

			HLSLPROGRAM
			#pragma vertex OutlineVertex
			#pragma fragment OutlineFragment

			#pragma multi_compile_instancing

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 colorOS : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings OutlineVertex(Attributes input)
			{
				Varyings output = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
				// The ring ALWAYS inflates along the baked smoothed
				// normal (Assets/Editor/HoloOutlineNormalsBaker.cs:
				// position-welded, crease-averaged, vertex COLOR
				// rgb = n*0.5+0.5). Every copy of a crease vertex
				// shares one direction, so the shells connect across
				// 90-degree faces while the width stays the old even
				// per-vertex-normal band. Data present ONLY when
				// alpha sits on the 0.5 marker - a mesh without the
				// bake reads white (a=1) or zero (a=0) and falls
				// back to its own vertex normal (classical hull; the
				// classical crease gap is that mesh's honest state
				// until it is baked).
				float3 dir = input.normalOS;
				if (abs(input.colorOS.a - (float)0.5) < (float)0.02)
				{
					float3 baked = input.colorOS.rgb * (float)2.0 - (float)1.0;
					float bakedLen = length(baked);
					if (bakedLen > (float)0.2)
					{
						dir = baked / bakedLen;
					}
				}
				float3 inflated = input.positionOS.xyz + normalize(dir) * _OutlineWidth;
				output.positionCS = TransformObjectToHClip(inflated);
				return output;
			}

			half4 OutlineFragment(Varyings input) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
				return half4(_BaseColor.rgb * (_Intensity * _OutlineStrength), 1.0);
			}
			ENDHLSL
		}
	}
	FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
