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
	// An unlit, additive shell for the curated "Hologram" ship prefabs the
	// loading screens show. It never touches gameplay ships or their
	// materials: OpenFrontier/Ship Surface keeps flying the hulls, and this
	// shader paints curated copies of the SAME hull meshes as projected
	// holograms. The whole look is procedural - no albedo, no emission map
	// to bind - because a hologram is geometry speaking, not texture:
	//
	//   - BASE FILM: a low constant so the silhouette never fully
	//     vanishes between effects.
	//   - RELIEF: a FIXED light direction no renderer ever sees, folded
	//     in as a smoothstepped N . L. It is not a light - every hull
	//     receives the same constant vector - so panels and greebles
	//     gain depth definition from the ship's own normal map at the
	//     cost of one dot product.
	//   - RIM: fresnel off the (normal-perturbed) normal, so bevels,
	//     hardpoints and chamfers catch brightness at grazing views
	//     instead of the hull reading as one smooth blob.
	//   - SCANLINES: bright line bands sampled on WORLD Y. World Y is
	//     the deliberate choice over UV space: the hull ROTATES through
	//     a stationary line field (the rig's HoloSpinner is always
	//     turning), and a field the hull moves through is what reads as
	//     a scan; UV lines would whirl with the ship instead. The bumped
	//     normal tips the sampling so lines visibly wrap over panels
	//     rather than painting a flat grid on a silhouette.
	//   - SWEEP: one slow bright band drifting up the same world-Y
	//     field - the classic hologram announcement band.
	//   - FLICKER: a cheap sum-of-sines amplitude wobble.
	//
	// TIME CONTRACT -- THE TRAP THIS SHADER EXISTS TO AVOID
	//
	// Unity's shader _Time follows Time.timeScale. Loading screens run
	// while gameplay world state is frozen (the legacy LoadingScreenUI
	// set timeScale = 0 under itself), so any effect animated on _Time
	// freezes silently in exactly the place freeze is most likely.
	// Nothing here reads _Time: everything animates on the
	// _OpenFrontierHoloTime GLOBAL, which HoloSpinner (on the rig's ship
	// holder) pushes every frame from Time.unscaledTime. The same
	// unscaled contract the legacy LoadingWidgetUI honoured by
	// animating on realtimeSinceStartup while the world sat paused.
	// No HoloSpinner instance? The global stays 0 and the hologram
	// loses its drift - visible, never silent.
	//
	// NORMAL MAPS -- depth information, not decoration
	//
	// Reads _BumpMap on UV0, the same slot and the same _NORMALMAP
	// keyword NAME stock Lit uses, so an extracted ship material
	// carries its bump binding and keyword straight across a shader
	// swap. Normals live on materials, so one material cannot serve
	// every ship: the PARENT material ("Hologram Ship", in
	// Resources/Materials) holds every tuning, and per-ship copies are
	// MATERIAL VARIANTS of it that override only the normal map
	// (rarely the colour). Tuning the parent retunes the entire
	// hologram fleet. Without a bump binding the shader_feature strips
	// to flat geometric normals - a plain ship looks smooth, never
	// broken. The keyword set is deliberately just _NORMALMAP:
	// build cost lives in keyword combinations, and every knob below
	// is a uniform the variants set for free.
	//
	// RENDER SHAPE (mobile-first)
	//
	// Unlit Core.hlsl only - no Lighting.hlsl, no realtime lights, no
	// shadow caster (a hologram casts no shadow by definition) and no
	// pipeline keyword forest. Additive over the loading screens'
	// near-black backgrounds - adding is the honest blend for light
	// that arrives, and it makes RT-side black edges invisible.
	//
	// SELF-OCCLUSION -- ZWrite ON, deliberately (2026-11-10)
	//
	// Additive WITHOUT depth is the X-ray trap: every part of the ship
	// adds over every other part, and a complex hull reads as
	// fluorescent gauze - wing panels visible through the fuselage,
	// engine channel guts glowing through the nose (measured on the
	// first curated ship: "ship is visible through itself").
	// ZWrite On costs one depth write and buys nearest-shell-wins: the
	// near hull hides the far hull. It still READS as light, because
	// blending stays additive - only the depth behaves solid. Interior
	// detail then shows exactly where it truly can - through open
	// cockpit glass and engine mouths - and exhaust sections that
	// genuinely stick past the silhouette still show. One curation
	// note: near-coincident inner liners (cockpit pods directly behind
	// shell panels) can faintly double-add at openings - fade or strip
	// the liner per ship if it glares; a code fix would be machinery
	// for one mesh. The vestigial DepthOnly pass below matters even
	// less now, kept to complete the URP pass table for the day a
	// depth-prepassing camera looks at a rig. The ACCENT
	// material ("Hologram Ship Accent", a variant of the parent)
	// serves curated highlights (wing strips, engine channels) that
	// deserve their own finer shimmer.
	//
	// No CustomEditor on purpose (same rule as Ship Surface): the
	// default inspector lists every property in declaration order - a
	// knob a curator cannot see is a knob that does not exist.

	Properties
	{
		[HDR] _BaseColor("Hologram Colour", Color) = (0.25, 0.85, 1.0, 1.0)
		_Intensity("Overall Intensity", Range(0, 4)) = 1.3

		_RimPower("Rim Power", Range(0.25, 8)) = 2.4
		_RimStrength("Rim Strength", Range(0, 4)) = 1.7
		_Relief("Relief Depth", Range(0, 1)) = 0.45

		_ScanDensity("Scanlines per Metre", Float) = 6.0
		_ScanStrength("Scanline Strength", Range(0, 1)) = 0.4
		_SweepSpeed("Sweep Speed", Float) = 0.35
		_SweepStrength("Sweep Strength", Range(0, 2)) = 0.8
		_Flicker("Flicker", Range(0, 0.5)) = 0.08

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
		// Explicit: UnityPerMaterial's copy of UnpackNormalScale must not
		// depend on whoever reorganises Core.hlsl next release. Core.hlsl
		// also includes Packing.hlsl today (verified line 25), but the
		// house rule is that no pass ever has to prove the chain.
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

		// NOTE: do not ifdef the properties here - SRP batching cannot
		// handle different bulk layouts between passes (house rule from
		// Ship Surface). _NormalMapToggle drives the keyword only and
		// _Cull is consumed by the render state; both stay out of the
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
		half _BumpScale;
		CBUFFER_END

		TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);

		// The one fixed relief vector: not a light, tuned once here,
		// identical for every hull and every curated variant.
		static const half3 kReliefLightDir = normalize(half3(-0.35, 0.85, 0.45));

		// Shader ground the loading rigs advance: see the header's TIME
		// CONTRACT. Script side: HoloSpinner.HoloTimeId.
		// (Declared as a uniform here so the compiler never constant-
		// folds it away when someone screenshots an idle第一时间 rig.)
		half _OpenFrontierHoloTime;
		ENDHLSL

		Pass
		{
			Name "Forward"
			Tags { "LightMode" = "UniversalForward" }

			Blend One One
			ZWrite On
			ZTest LEqual
			Cull [_Cull]

			HLSLPROGRAM
			#pragma vertex HoloVertex
			#pragma fragment HoloFragment

			// Material keywords - just the one, _NORMALMAP (stock Lit's
			// name). Everything visual is a uniform, so per-ship material
			// variants cost buffer writes, never variant combinations.
			#pragma shader_feature_local_fragment _NORMALMAP

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
					// Tangent-space X deflection tips the sampled field:
					// scanlines conform over bevels and panel steps.
					scanY += nTS.x * 0.35f;
				#endif

				// Depth definition from the never-seen fixed light.
				half relief = smoothstep((half)0.10, (half)0.95, dot(n, kReliefLightDir)) * _Relief;

				// Silhouette and bevels catch the grazing view.
				half rim = pow((half)saturate(1.0 - saturate(dot(n, viewDir))), _RimPower) * _RimStrength;

				// The reading field: thin bright bands every 1/_ScanDensity
				// metre of world height, duty cycle tuned at smoothstep.
				half scan = smoothstep((half)0.90, (half)1.0, (half)frac(scanY * (float)_ScanDensity)) * _ScanStrength;

				// The announcement band: period 4 m of world height,
				// drifting upward at _SweepSpeed bands per second.
				float bandPhase = frac(scanY * 0.25f - (float)_OpenFrontierHoloTime * (float)_SweepSpeed);
				half band = smoothstep((half)0.86, (half)1.0, (half)(1.0 - abs(bandPhase - 0.5) * 2.0)) * _SweepStrength;

				// Sum-of-sines wobble; cheap, self-cancelling over time.
				half flicker = 1.0 - _Flicker * (half)(0.5 + 0.5 * sin((float)_OpenFrontierHoloTime * 13.7f) * sin((float)_OpenFrontierHoloTime * 31.3f + 1.9f));

				// The base film keeps the silhouette present even when
				// every effect finishes a trough together.
				half energy = (half)0.12 + relief + rim + scan + band;

				return half4(_BaseColor.rgb * (_Intensity * energy * flicker), 1.0);
			}
			ENDHLSL
		}

		// Vestigial depth pass: the URP pass table stays complete (ship
		// holograms take no part in depth-based effects today; keep what
		// would not surprise a future depth-prepassing camera). Writes no
		// depth: additive transparent hides nothing.
		Pass
		{
			Name "DepthOnly"
			Tags { "LightMode" = "DepthOnly" }

			ZWrite Off
			ColorMask 0
			Cull [_Cull]

			HLSLPROGRAM
			#pragma target 2.0

			#pragma vertex HoloDepthVertex
			#pragma fragment HoloDepthFragment

			#pragma multi_compile_instancing

			struct DepthAttributes
			{
				float4 positionOS : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct DepthVaryings
			{
				float4 positionCS : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			DepthVaryings HoloDepthVertex(DepthAttributes input)
			{
				DepthVaryings output = (DepthVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
				output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
				return output;
			}

			half HoloDepthFragment(DepthVaryings input) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
				return input.positionCS.z;
			}
			ENDHLSL
		}
	}
	FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
