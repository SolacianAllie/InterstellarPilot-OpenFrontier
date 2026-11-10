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
	// body fill, PANEL LINES traced bright, and a strong OUTER SILHOUETTE
	// contour - light that behaves like a solid object, never an X-ray.
	// It never touches gameplay ships or their materials:
	// OpenFrontier/Ship Surface keeps flying the hulls; this shader paints
	// curated copies of the SAME hull meshes. The look is procedural:
	//
	//   - BODY FILL: a very low constant plus a damped relief shading
	//     (fixed never-seen light vector), so the silhouette reads but
	//     stays dark - the lines carry the picture.
	//   - PANEL EDGES: the ships' own NORMAL MAPS are the blueprint.
	//     Panel seams are normal discontinuities, so a 4-tap edge trace
	//     over the bump map lights every seam and speckles every
	//     greeble (the reference look's line detail). Gated on
	//     _NORMALMAP + _HOLOEDGES; ships without a bump bound fall back
	//     to fill + contour, smooth but whole.
	//   - SILHOUETTE CONTOUR: an inverted-hull pass - the mesh grown
	//     along its normals, back faces only. The ring pokes past the
	//     object's own silhouette; everything else hides behind the
	//     solid depth. The fresnel-rim approach committed earlier
	//     (d4121fbe) produced blotchy wash on bevels instead of contour,
	//     so fresnel is demoted to a subtle floor (RimStrength default
	//     0.35) and the contour pass owns the outline.
	//   - SCANLINES + SWEEP: faint world-Y energy field the hull rotates
	//     through (world Y chosen over UV: a field the hull moves
	//     through reads as a scan; UV lines would whirl with the ship).
	//   - FLICKER: a cheap sum-of-sines amplitude wobble.
	//
	// WHY THREE PASSES -- the measured saga (2026-11-10)
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
	//           sections genuinely past the silhouette still show.
	//   PASS 3 "Outline":    inverted hull, Cull Front, tested against the
	//           same depth - the ring outside the silhouette shows, the
	//           rest fails behind the solid.
	//
	// Order-independent and deterministic on any hull. Cross-object too:
	// each object's prepass runs before its own colour, so two ships in
	// one rig occlude each other correctly.
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
	// keep _BumpScale above 0 (the edge tracer reads the scaled
	// normals - 0 flattens the lines away), and the normal map needs
	// mesh tangents (FBX "Generate Tangents" default is on).
	//
	// RENDER SHAPE (mobile-first)
	//
	// Unlit Core.hlsl only - no Lighting.hlsl, no realtime lights, no
	// shadow caster (a hologram casts no shadow by definition) and no
	// pipeline keyword forest. Additive over the loading screens'
	// near-black backgrounds - adding is the honest blend for light
	// that arrives, and it makes RT-side black edges invisible. Three
	// passes cost two extra vertex-only runs (prepass, outline) plus
	// four texture taps when edges are on - a loading-screen-scoped
	// spend, and the keyword set stays deliberately tiny (_NORMALMAP,
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

		// The old fill weights, damped for the line-art balance.
		_RimPower("Rim Power", Range(0.25, 8)) = 2.4
		_RimStrength("Rim Strength", Range(0, 4)) = 0.35
		_Relief("Relief Depth", Range(0, 1)) = 0.25

		_ScanDensity("Scanlines per Metre", Float) = 6.0
		_ScanStrength("Scanline Strength", Range(0, 1)) = 0.25
		_SweepSpeed("Sweep Speed", Float) = 0.35
		_SweepStrength("Sweep Strength", Range(0, 2)) = 0.5
		_Flicker("Flicker", Range(0, 0.5)) = 0.08

		// The line-art pair: panel seams traced from the normal map.
		[Toggle(_HOLOEDGES)] _EdgeToggle("Normal-Map Edges", Float) = 1.0
		_EdgeStrength("Edge Strength", Range(0, 8)) = 2.0

		// The outline ring: metres of world-space normal growth.
		_OutlineWidth("Silhouette Outline Width", Float) = 0.05
		_OutlineStrength("Silhouette Outline Strength", Range(0, 4)) = 1.0

		// Same keyword name, same slot pair as stock Lit, so extracted
		// materials swap into this shader with nothing rebound.
		[Toggle(_NORMALMAP)] _NormalMapToggle("Normal Map", Float) = 0.0
		_BumpMap("Normal Map", 2D) = "bump" {}
		_BumpScale("Normal Scale", Range(0, 2)) = 1.0

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
		half _Relief;
		half _ScanDensity;
		half _ScanStrength;
		half _SweepSpeed;
		half _SweepStrength;
		half _Flicker;
		half _EdgeStrength;
		half _OutlineWidth;
		half _OutlineStrength;
		half _BumpScale;
		float4 _BumpMap_TexelSize;
		CBUFFER_END

		TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);

		// The one fixed relief vector: not a light, tuned once here,
		// identical for every hull and every curated variant.
		static const half3 kReliefLightDir = normalize(half3(-0.35, 0.85, 0.45));

		// Pair-seam pickup for the edge tracer.
		static const half kEdgeGain = 16.0;

		// Shader ground the loading rigs advance: see the header's TIME
		// CONTRACT. Script side: HoloSpinner.HoloTimeId.
		half _OpenFrontierHoloTime;
		ENDHLSL

		// ---- PASS 1: solid depth (untagged -> SRPDefaultUnlit) ----
		//
		// The self-occlusion referendum, written BEFORE any colour exists
		// (see the header's WHY THREE PASSES). ColourMask 0: nothing is
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
				half3 n = (half3)geometryNormal;

				// The scan field's Y (the hull turns through this
				// fixed-space field - see the header).
				float scanY = input.positionWS.y;

				#if defined(_NORMALMAP)
					float3x3 tangentToW = float3x3(
						normalize(input.tangentWS.xyz),
						cross(geometryNormal, input.tangentWS.xyz) * input.tangentWS.w,
						geometryNormal);
					half3 nTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv0), _BumpScale);
					n = normalize(mul((half3x3)tangentToW, nTS));

					#if defined(_HOLOEDGES)
						// The blueprint tracer: panel seams are normal
						// discontinuities, so neighbour-tap deltas light
						// every seam - and speckle every greeble - with
						// four samples on ship-sized triangles.
						float2 texel = _BumpMap_TexelSize.xy;
						half3 nX = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv0 + float2(texel.x, 0.0)), _BumpScale)
								 - UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv0 - float2(texel.x, 0.0)), _BumpScale);
						half3 nY = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv0 + float2(0.0, texel.y)), _BumpScale)
								 - UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv0 - float2(0.0, texel.y)), _BumpScale);
						half edges = saturate((length(nX) + length(nY)) * kEdgeGain) * _EdgeStrength;
					#else
						half edges = 0;
					#endif

					// Tangent-space X deflection tips the sampled field:
					// scanlines conform over bevels and panel steps.
					scanY += nTS.x * 0.35f;
				#else
					half edges = 0;
				#endif

				// Damped fill: silhouette presence only, lines carry the
				// picture (the reference look's balance).
				half relief = smoothstep((half)0.10, (half)0.95, dot(n, kReliefLightDir)) * _Relief;
				half rim = pow((half)saturate(1.0 - saturate(dot(n, viewDir))), _RimPower) * _RimStrength;

				// The reading field: thin bright bands every 1/_ScanDensity
				// metre of world height.
				half scan = smoothstep((half)0.90, (half)1.0, (half)frac(scanY * (float)_ScanDensity)) * _ScanStrength;

				// The announcement band: period 4 m of world height,
				// drifting upward at _SweepSpeed bands per second.
				float bandPhase = frac(scanY * 0.25f - (float)_OpenFrontierHoloTime * (float)_SweepSpeed);
				half band = smoothstep((half)0.86, (half)1.0, (half)(1.0 - abs(bandPhase - 0.5) * 2.0)) * _SweepStrength;

				// Sum-of-sines wobble; cheap, self-cancelling over time.
				half flicker = 1.0 - _Flicker * (half)(0.5 + 0.5 * sin((float)_OpenFrontierHoloTime * 13.7f) * sin((float)_OpenFrontierHoloTime * 31.3f + 1.9f));

				// The base film keeps the silhouette present even when
				// every effect finishes a trough together.
				half energy = (half)0.06 + relief + rim + edges + scan + band;

				return half4(_BaseColor.rgb * (_Intensity * energy * flicker), 1.0);
			}
			ENDHLSL
		}

		// ---- PASS 3: the silhouette contour ----
		//
		// Inverted hull: the mesh grown along its normals, back faces
		// only. The ring that pokes past the object's own silhouette
		// shows against empty depth; everything else fails behind the
		// solid from PASS 1. Metres, not object scale, so one width
		// serves ships of every size.
		Pass
		{
			Name "Outline"

			Blend One One
			ZWrite Off
			ZTest LEqual
			Cull Front

			HLSLPROGRAM
			#pragma vertex HoloOutlineVertex
			#pragma fragment HoloOutlineFragment

			#pragma multi_compile_instancing

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS   : NORMAL;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings HoloOutlineVertex(Attributes input)
			{
				Varyings output = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
				float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
				// Grow along the WORLD normal: metres, identical meaning
				// on every ship regardless of authored import scale.
				positionWS += TransformObjectToWorldDir(input.normalOS) * (float)_OutlineWidth;
				output.positionCS = TransformWorldToHClip(positionWS);
				return output;
			}

			half4 HoloOutlineFragment(Varyings input) : SV_TARGET
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
