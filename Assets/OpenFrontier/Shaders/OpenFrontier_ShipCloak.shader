Shader "OpenFrontier/Ship Cloak"
{
	// ---- cloaking hulls: the ship dissolves into bent light ----
	//
	// SHADER NAME -- SPACES ON PURPOSE (house rule, see
	// OpenFrontier_ShipSurface.shader): "OpenFrontier/Ship Cloak" so a
	// designer searching "ship cloak" finds it.
	//
	// WHAT THIS IS
	//
	// OpenFrontier/Ship Surface + a cloak block. Everything else is byte
	// for byte the ship shader's contract: the same slots (_BaseMap,
	// _BumpMap, _OcclusionMap, _MetallicGlossMap, _EmissionMap, the decal
	// layer), the same keyword names (_EMISSION, _METALLICSPECGLOSSMAP,
	// _NORMALMAP, _OCCLUSIONMAP), the same trim list -- so a cloaking
	// ship's material swaps onto this shader with its bindings and flags
	// intact and looks IDENTICAL to its hull material. The only visual
	// difference is what happens when ActiveUnit pushes the cloak values.
	//
	// It exists so the CLOAK OUTLINE becomes material state instead of a
	// second renderer: the hull itself sinks into a bent-image of the
	// background, and owned ships keep a rim of readable structure. No
	// CloakRenderer shell, no CloakOutlineRenderer, no settings toggle.
	//
	// HOW THE SLIDER READS
	//
	// ActiveUnit pushes three uniforms per renderer (MaterialPropertyBlock):
	//
	//   _CloakAmount  0..1 -- 0 decloaked, 1 fully cloaked. This is the
	//      CloakComponent progress under the CloakAlphaPower curve, or the
	//      construction-strobe inverse while a hull is being built.
	//   _CloakFloor   0..1 -- how much hull material survives at amount 1.
	//      This is the CLOAK VISIBILITY floor the video settings already
	//      own: CloakMinAlphaForPlayer (0.3) for owned and allied ships,
	//      CloakMinAlpha (0.07) for enemies, 0 during construction and
	//      external fades. Same numbers the old shell alpha used; they are
	//      not a new tuning surface.
	//   _CloakFriendly  0/1 -- owned/allied within the old outline gate
	//      (IsPlayerOrAllied, and current unit or within 1500u). Only
	//      friendly hulls get the tell rim; enemies dissolve clean.
	//
	// The visible surface then trades itself for the bend:
	//
	//   vis = lerp(1, _CloakFloor, amount)
	//
	// and the fragment outputs lerp(litProfile, bentBackground, 1 - vis).
	// That is why the floor numbers carry over unchanged: they now mean
	// "fraction of the hull's own material left standing", which reads the
	// same way for the player as the old alpha floors did.
	//
	// The bend comes from the hull's own normal map (panel detail warps
	// the screen-UV of _CameraOpaqueTexture -- light following geometry,
	// no extra texture) plus a slow sin ripple. It scales with amount
	// squared: the first tick of cloaking barely bends, full cloak is a
	// standing smear. Owned ships additionally get the tell rim -- the
	// silhouette painted in _CloakTellColor so a friendly ghost is
	// findable in a standing formation.
	//
	// WHY THE SHADER STAYS OPAQUE
	//
	// Blend One Zero, ZWrite On, Geometry queue. A cloaking hull never
	// turns translucent: it swaps its own colour for a sample of the
	// frame behind it, at the rate vis allows. No transparent queue, no
	// sorting, no premultiply stack. Invisible at 100% falls out of the
	// colour converging on bent background, not out of alpha. The thing
	// a transparent shader would have needed is exactly the stack the
	// ship shader refused to carry, and for the same reasons.
	//
	// _CameraOpaqueTexture
	//
	// Bend needs the frame behind the ship. This URP project does NOT
	// request the opaque texture globally (m_OpaqueTexture off on the
	// pipeline asset -- nobody needed a fullscreen copy); instead
	// CloakOpaqueTextureSync flips the requiresColor override on the main
	// camera whenever any cloaking hull asks for it (OpenFrontier
	// namespace counter, written by ActiveUnit on state transitions).
	// Cost when nobody cloaks: one bool compare at 4 Hz. Which is why the
	// shader keeps the scene sample behind a uniform branch -- a decloaked
	// cloak-hull pays stock-Lit cost, not the copy.
	//
	// Sampling the copy when no camera provided it binds a default
	// texture; the bent lookup reads black and the hull visibly darkens
	// while cloaking in that view (reflection probes, cameras without the
	// override). Accepted and documented: the game camera drives what the
	// player sees, and probes are not cloak showcases.
	//
	// SHADOWS / DEPTH
	//
	// ShadowCaster and DepthOnly clip on cloakAmount > 0.02: hulls cast no
	// shadow and take no prepass slot from the first tick of cloaking on.
	// That matches the old shell exactly (a fading hull had no shadows,
	// the shell had no ShadowCaster pass), and a cloaked ship leaving a
	// crisp shadow would betray it -- so the binary clip is the design,
	// not an approximation. Until then an unclipped hull shadows 1:1 with
	// its hull profile.
	//
	// No lightmap keywords, no _CLUSTER_LIGHT_LOOP (Forward renderer --
	// the same documented Forward+ risk as the ship shader), no GBuffer,
	// no DOTS. Shadow/soft-shadow/probe/fog/instancing variants kept, per
	// the ship shader's header. One dynamic branch, zero new variants:
	// the cloak block is uniform-scalar logic, so this stays one shader
	// on every phone the game lives on.
	Properties
	{
		// The five the ships come with + the decal layer. Identical to
		// OpenFrontier/Ship Surface so the hull material swaps over.
		[MainTexture] _BaseMap("Albedo", 2D) = "white" {}
		[MainColor] _BaseColor("Albedo Tint", Color) = (1,1,1,1)

		[Normal] _BumpMap("Normal Map", 2D) = "bump" {}
		_BumpScale("Normal Strength", Float) = 1.0

		_OcclusionMap("Ambient Occlusion", 2D) = "white" {}
		_OcclusionStrength("AO Strength", Range(0.0, 1.0)) = 1.0

		_MetallicGlossMap("Metallic+Smoothness (metal=R, smooth=A)", 2D) = "white" {}
		_Metallic("Metallic", Range(0.0, 1.0)) = 0.0
		_Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5

		[HDR] _EmissionColor("Luminance Color (HDR)", Color) = (0,0,0)
		_EmissionMap("Luminance (Emission) Map", 2D) = "white" {}

		_DecalMap("Decal Overlay (RGBA)", 2D) = "black" {}

		[ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
		[ToggleOff] _EnvironmentReflections("Environment Reflections", Float) = 1.0

		_Cull("Face Culling", Float) = 2.0

		// ---- the cloak ----
		// Pushed per renderer by ActiveUnit; the sliders only exist so the
		// inspector shows what the unit is driving.
		_CloakAmount("Cloak Amount (1 = fully cloaked)", Range(0.0, 1.0)) = 0.0
		_CloakFloor("Cloak Floor (hull left at full cloak)", Range(0.0, 1.0)) = 0.0
		_CloakFriendly("Cloak Friendly Tell", Float) = 0.0

		// The bend, and its material-tuning side. The floor and amount are
		// gameplay state; these are art direction per ship.
		_CloakWarpScale("Cloak Warp Strength", Float) = 0.06
		_CloakShimmerScale("Cloak Shimmer Strength", Float) = 0.02
		_CloakShimmerLines("Cloak Shimmer Lines", Float) = 18.0
		_CloakShimmerSpeed("Cloak Shimmer Speed", Float) = 1.5
		[HDR] _CloakTellColor("Cloak Tell Color (HDR)", Color) = (0.3, 0.8, 1.0, 1.0)
	}

	SubShader
	{
		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
			"UniversalMaterialType" = "Lit"
			"RenderType" = "Opaque"
			"Queue" = "Geometry"
			"IgnoreProjector" = "True"
		}

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"

		// NOTE: Do not ifdef the properties here - SRP batching cannot
		// handle different bulk layouts between passes. Same rule as the
		// ship shader: _Cull and the [ToggleOff] floats stay OUT of the
		// buffer, everything the fragment reads lives in it.
		CBUFFER_START(UnityPerMaterial)
		float4 _BaseMap_ST;
		float4 _DecalMap_ST;
		half4 _BaseColor;
		half4 _EmissionColor;
		half4 _CloakTellColor;
		half _BumpScale;
		half _OcclusionStrength;
		half _Metallic;
		half _Smoothness;
		half _CloakAmount;
		half _CloakFloor;
		half _CloakFriendly;
		half _CloakWarpScale;
		half _CloakShimmerScale;
		half _CloakShimmerLines;
		half _CloakShimmerSpeed;
		CBUFFER_END

		TEXTURE2D(_BaseMap);          SAMPLER(sampler_BaseMap);
		TEXTURE2D(_BumpMap);          SAMPLER(sampler_BumpMap);
		TEXTURE2D(_OcclusionMap);     SAMPLER(sampler_OcclusionMap);
		TEXTURE2D(_MetallicGlossMap); SAMPLER(sampler_MetallicGlossMap);
		TEXTURE2D(_EmissionMap);      SAMPLER(sampler_EmissionMap);
		TEXTURE2D(_DecalMap);         SAMPLER(sampler_DecalMap);
		ENDHLSL

		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode" = "UniversalForward" }

			Blend One Zero
			ZWrite On
			ZTest LEqual
			Cull [_Cull]

			HLSLPROGRAM
			#pragma vertex ShipCloakVertex
			#pragma fragment ShipCloakFragment

			#pragma shader_feature_local _NORMALMAP
			#pragma shader_feature_local _RECEIVE_SHADOWS_OFF
			#pragma shader_feature_local_fragment _EMISSION
			#pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
			#pragma shader_feature_local_fragment _OCCLUSIONMAP
			#pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
			#pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF

			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING _REFLECTION_PROBE_BOX_PROJECTION
			#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"

			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"

			//--------------------------------------
			// GPU Instancing (the ship materials enable it)
			#pragma multi_compile_instancing

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

			struct Attributes
			{
				float4 positionOS   : POSITION;
				float3 normalOS     : NORMAL;
				float4 tangentOS    : TANGENT;
				float2 texcoord     : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float2  uv          : TEXCOORD0;   // _BaseMap space
				float2  decalUV     : TEXCOORD1;   // _DecalMap space (own ST)
				float3  positionWS  : TEXCOORD2;
				float3  normalWS    : TEXCOORD3;
			#ifdef _NORMALMAP
				half4   tangentWS   : TEXCOORD4;   // xyz tangent, w sign
			#endif
			#ifdef _ADDITIONAL_LIGHTS_VERTEX
				half4   fogFactorAndVertexLight : TEXCOORD5; // x fog, yzw vertex light
			#else
				half    fogFactor   : TEXCOORD5;
			#endif
				float3  vertexSH    : TEXCOORD6;   // baked ambient
				float4  positionCS  : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			// ---- the surface, ship edition (identical to Ship Surface) ----
			void InitializeShipSurfaceData(Varyings input, out SurfaceData surfaceData)
			{
				surfaceData = (SurfaceData)0;

				half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

				half4 decal = SAMPLE_TEXTURE2D(_DecalMap, sampler_DecalMap, input.decalUV);
				albedo.rgb = lerp(albedo.rgb, decal.rgb, decal.a);

				surfaceData.albedo = albedo.rgb;
				surfaceData.alpha = 1.0h;

				#ifdef _METALLICSPECGLOSSMAP
				half4 metalSmooth = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, input.uv);
				surfaceData.metallic = metalSmooth.r;
				surfaceData.smoothness = metalSmooth.a * _Smoothness;
				#else
				surfaceData.metallic = _Metallic;
				surfaceData.smoothness = _Smoothness;
				#endif

				surfaceData.specular = half3(0, 0, 0);

				#ifdef _OCCLUSIONMAP
				half occ = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, input.uv).g;
				surfaceData.occlusion = LerpWhiteTo(occ, _OcclusionStrength);
				#else
				surfaceData.occlusion = 1.0h;
				#endif

				#ifdef _NORMALMAP
				surfaceData.normalTS = UnpackNormalScale(
					SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
				#else
				surfaceData.normalTS = half3(0, 0, 1);
				#endif

				#ifdef _EMISSION
				surfaceData.emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb * _EmissionColor.rgb;
				#else
				surfaceData.emission = half3(0, 0, 0);
				#endif
			}

			void InitializeShipInputData(Varyings input, half3 normalTS, out InputData inputData)
			{
				inputData = (InputData)0;

				inputData.positionWS = input.positionWS;

				half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
				#ifdef _NORMALMAP
				float sgn = input.tangentWS.w;
				float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
				half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);
				inputData.tangentToWorld = tangentToWorld;
				inputData.normalWS = TransformTangentToWorld(normalTS, tangentToWorld);
				#else
				inputData.normalWS = input.normalWS;
				#endif
				inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
				inputData.viewDirectionWS = viewDirWS;

				#if defined(MAIN_LIGHT_CALCULATE_SHADOWS) && !defined(_RECEIVE_SHADOWS_OFF)
				inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
				#else
				inputData.shadowCoord = float4(0, 0, 0, 0);
				#endif

				#ifdef _ADDITIONAL_LIGHTS_VERTEX
				inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactorAndVertexLight.x);
				inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
				#else
				inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactor);
				#endif

				inputData.bakedGI = SampleSHPixel(input.vertexSH, inputData.normalWS);

				inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
			}

			Varyings ShipCloakVertex(Attributes input)
			{
				Varyings output = (Varyings)0;

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
				VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

				half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);

				half fogFactor = 0;
				#if !defined(_FOG_FRAGMENT)
				fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
				#endif

				output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
				output.decalUV = TRANSFORM_TEX(input.texcoord, _DecalMap);

				output.normalWS = normalInput.normalWS;
				#ifdef _NORMALMAP
				real sign = input.tangentOS.w * GetOddNegativeScale();
				output.tangentWS = half4(normalInput.tangentWS.xyz, sign);
				#endif

				#ifdef _ADDITIONAL_LIGHTS_VERTEX
				output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
				#else
				output.fogFactor = fogFactor;
				#endif

				output.vertexSH = SampleSHVertex(normalInput.normalWS);

				output.positionWS = vertexInput.positionWS;
				output.positionCS = vertexInput.positionCS;

				return output;
			}

			half4 ShipCloakFragment(Varyings input) : SV_Target0
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				SurfaceData surfaceData;
				InitializeShipSurfaceData(input, surfaceData);

				InputData inputData;
				InitializeShipInputData(input, surfaceData.normalTS, inputData);

				half4 color = UniversalFragmentPBR(inputData, surfaceData);
				color.rgb = MixFog(color.rgb, inputData.fogCoord);

				// ---- the cloak: bend the frame behind the hull, then close it ----
				// Uniform-scalar branch: decloaked hulls pay nothing past the
				// compare, the sampler stays bound but is never read.
				half cloak = saturate(_CloakAmount);
				if (cloak > 0.001h)
				{
					// How much of the hull's own material survives. Drops from 1
					// to the settings floor; the bend eats the remainder.
					half vis = lerp(1.0h, saturate(_CloakFloor), cloak);

					// The bend: panel detail from the hull's own normal map (flat
					// hulls leave (0,0) and the ship bends only via the ripple),
					// quadratic in amount -- gentle start, standing smear at full
					// cloak -- plus a slow two-axis ripple so parked hulls never
					// look like a static mat of background.
					half t = _Time.y * _CloakShimmerSpeed;
					half2 warp = surfaceData.normalTS.xy;
					warp *= _CloakWarpScale * cloak * cloak;
					warp += half2(
						sin(input.uv.y * _CloakShimmerLines + t),
						sin(input.uv.x * _CloakShimmerLines * 0.711h - t * 1.373h)
					) * (_CloakShimmerScale * cloak);

					// The frame behind, displaced. SampleSceneColor clamps and
					// folds the UV itself (bilinear size clamp + stereo).
					half3 bent = SampleSceneColor(inputData.normalizedScreenSpaceUV + warp);

					// Trade the material for the bend. The hull profile was fogged
					// above; the copy is a finished, already-fogged frame -- both
					// sides of the lerp are in the same space, do not fog again.
					color.rgb = lerp(color.rgb, bent, 1.0h - vis);

					// The friendly tell: a rim of structure over the smear, only
					// on hulls ActiveUnit marks as owned/allied-near. Players see
					// their ghost; enemies dissolve clean.
					half rim = 1.0h - saturate(dot(inputData.normalWS, inputData.viewDirectionWS));
					color.rgb += _CloakTellColor.rgb * (_CloakFriendly * rim * rim);
				}

				color.a = 1.0;

				return color;
			}
			ENDHLSL
		}

		Pass
		{
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }

			ZWrite On
			ZTest LEqual
			ColorMask 0
			Cull [_Cull]

			HLSLPROGRAM
			#pragma target 2.0

			#pragma vertex ShipCloakShadowVertex
			#pragma fragment ShipCloakShadowFragment

			#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

			//--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

			float3 _LightDirection;
			float3 _LightPosition;

			struct ShadowAttributes
			{
				float4 positionOS   : POSITION;
				float3 normalOS     : NORMAL;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct ShadowVaryings
			{
				float4 positionCS   : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			float4 GetShipShadowPositionHClip(ShadowAttributes input)
			{
				float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
				float3 normalWS = TransformObjectToWorldNormal(input.normalOS, true);

				#if _CASTING_PUNCTUAL_LIGHT_SHADOW
				float3 lightDirectionWS = normalize(_LightPosition - positionWS);
				#else
				float3 lightDirectionWS = _LightDirection;
				#endif

				float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
				positionCS = ApplyShadowClamping(positionCS);
				return positionCS;
			}

			ShadowVaryings ShipCloakShadowVertex(ShadowAttributes input)
			{
				ShadowVaryings output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionCS = GetShipShadowPositionHClip(input);
				return output;
			}

			half4 ShipCloakShadowFragment(ShadowVaryings input) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(input);
				// Cloaked hulls cast no shadow (see header): any cloaking past
				// a sliver and the silhouette is out of the light's view.
				clip(0.02h - _CloakAmount);
				return 0;
			}
			ENDHLSL
		}

		Pass
		{
			Name "DepthOnly"
			Tags { "LightMode" = "DepthOnly" }

			ZWrite On
			ColorMask R
			Cull [_Cull]

			HLSLPROGRAM
			#pragma target 2.0

			#pragma vertex ShipCloakDepthOnlyVertex
			#pragma fragment ShipCloakDepthOnlyFragment

			//--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing

			struct DepthAttributes
			{
				float4 positionOS   : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct DepthVaryings
			{
				float4 positionCS   : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			DepthVaryings ShipCloakDepthOnlyVertex(DepthAttributes input)
			{
				DepthVaryings output = (DepthVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
				output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
				return output;
			}

			half ShipCloakDepthOnlyFragment(DepthVaryings input) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
				// Same contract as the shadow pass: a cloaking hull is out of
				// the depth prepass too, so depth-driven effects cannot ghost
				// a silhouette the colour pass is erasing.
				clip(0.02h - _CloakAmount);
				return input.positionCS.z;
			}
			ENDHLSL
		}
	}
	FallBack "Hidden/Universal Render Pipeline/FallbackError"
	// No CustomEditor, same as the ship shader: the default inspector
	// lists every row, including the cloak block ActiveUnit drives.
}
