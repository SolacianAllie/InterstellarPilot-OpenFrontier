Shader "OpenFrontier/Ship Surface"
{
	// ---- ships: URP Lit rebuilt for our hulls, with a decal overlay ----
	//
	// SHADER NAME -- SPACES ON PURPOSE (same rule as the planet shaders,
	// see OpenFrontier_PlanetGasGiant.shader): the picker searches the
	// declared name, so this is "OpenFrontier/Ship Surface" and a user
	// looking for "ship surface" finds it.
	//
	// WHAT IT IS
	//
	// Unity's URP /Lit (UniversalForward + ShadowCaster + DepthOnly),
	// rewritten with our ships' texture set in mind and trimmed to what
	// a ship hull actually needs on the phones this game lives on. It
	// reads the same slots stock Lit does -- _BaseMap, _BumpMap,
	// _OcclusionMap, _MetallicGlossMap, _EmissionMap -- with the same
	// keyword NAMES, so a ship material that already carries those
	// bindings and flags (the extracted ones do) only needs its shader
	// changed in the inspector. Nothing is rebound, nothing is re-tuned.
	//
	// THE MAPPED CONVENTIONS -- MEASURED, NOT ASSUMED
	//
	// Four ships' texture sets were measured pixel-by-pixel (Drake,
	// Creon, Thunder, Flyer) before this was written:
	//
	//   - The metallic/roughness files are NAMED as glTF packs
	//     ("Drake_Roughness+Metallic", "Creon_Metallic+Roughness_2")
	//     and are NOT that. Their RGB is one identical channel - a hard,
	//     near-binary mask that tracks metal panels (90% saturated on
	//     Drake, mean 0.114) - and their ALPHA is a soft, nearly
	//     gradient-free field (mean 0.43 on Drake, 99% mid-tone, tiny
	//     gradients). That is exactly Unity's own packing: METALLIC in
	//     red, SMOOTHNESS in alpha, which is what URP Lit expects and
	//     where the migrated materials already bound them. The names
	//     mislead; the pixels do not. This shader keeps Unity's reading
	//     of the slot and does not follow the filenames.
	//   - The occlusion maps are flat greyscale, so the .g channel stock
	//     Lit samples is identical to the others; stock's reading is
	//     kept for parity.
	//   - "Luminance" maps (Drake GI, Thunder Luminance, Flyer
	//     Luminance_GI) are the ship's self-lit parts - windows, engine
	//     strips, console glow. They are EMISSION, and the extracted
	//     materials already bind them to _EmissionMap with an _EMISSION
	//     keyword; this shader reads the same slot and multiplies by
	//     _EmissionColor (HDR) exactly like stock.
	//
	// No size roll, no per-unit scaling: the hull is whatever scale its
	// active unit prefab ships with (see the planet-sizing doctrine in
	// Unit.cs). Nothing in this shader overrides transforms.
	//
	// THE DECAL LAYER
	//
	// _DecalMap is a straightforward ON TOP overlay, not a decal-projection
	// system: one RGBA image, its own tiling/offset (_DecalMap_ST), same
	// UV0 space as the hull. ALPHA is the brush; the RGB underneath it is
	// a MULTIPLY filter, not absolute paint: where alpha is 0 the hull
	// shows untouched (the lerped filter is white and the artwork's
	// transparent-region RGB is ignored), where it is 1 the decal RGB
	// multiplies the albedo underneath - white decals erase themselves,
	// tinted ones darken and colour the hull panels they cover. Livery
	// drawn this way shades the hull instead of covering it. The default
	// texture is white, which is inert by definition.
	//
	// _DecalPaintStrength is the escape hatch from multiply's one
	// blindness: hull x white = hull, so under multiply a WHITE livery
	// mark cannot exist -- there is no paint lighter than the hull. The
	// slider lifts a texel from the multiplied result toward the decal's
	// own colour, weighted by the same alpha brush: 0 keeps pure
	// shading, 1 repaints the texel outright, and the useful middle
	// paints the mark while the hull's dirt still grimes through it from
	// underneath. The layer's whole cost stays one sample and two
	// lerps - no pass, no variant, no stencil - because this game
	// runs on phones where the cheapest feature is the one that lives.
	//
	// It overlays ALBEDO only. It does not push normals, occlusion or
	// smoothness: livery is paint on the hull, and painted hulls still
	// reflect the panel geometry underneath. If a decal ever needs to
	// fake its own bump, that is a second sampler and a decision, not
	// an accident.
	//
	// THE EMISSIVE DECAL LAYER
	//
	// _DecalEmissionMap is the glow companion to _DecalMap: same tiling
	// (_DecalMap_ST), same decal UV interpolator. RGB is the lit
	// artwork, its ALPHA is the per-part mask on top of that, and
	// _DecalEmissionColor (HDR) scales it so a faction's glow can be
	// retuned without repainting. It adds to _EmissionMap's luminance,
	// never replaces it. Pixel alignment with the paint decal is the
	// contract: an emissive stripe sits exactly where the decal sheet
	// drew it, so this layer costs one extra sample and, because it
	// rides the layer-switch keyword (_DECALEMISSION), nothing at all
	// on the faction materials whose decals do not glow.
	//
	// WHAT WAS TRIMMED FROM STOCK, AND WHY (mobile-first)
	//
	// The project renderer is FORWARD (OpenFrontier-URP-Renderer,
	// m_RenderingMode: 0) with NO renderer features (m_RendererFeatures:
	// []), and ships are strictly dynamic meshes (the sector's
	// LightingData bakes static shells only). So:
	//
	//   - No transparent / clip / premultiply machinery. Ship hulls are
	//     opaque ALWAYS: ActiveUnit swaps to a separate cloak renderer
	//     for fades and construction (ActiveUnitRenderMode.Fade hides
	//     unitRenderer and shows CloakRenderer - NamedFrame set up in
	//     ActiveUnit.cs). The hull material never turns transparent, so
	//     the shader refuses to carry that stack. The transparent
	//     extracted materials (the *Fade ones) are cloak/effects
	//     materials and belong on a different shader anyway.
	//   - No lightmap/mesh-bake keywords (LIGHTMAP_ON family,
	//     SHADOWMASK, legacy lightmaps): ships spawn at runtime and are
	//     never baked; ambient arrives as SH off the normal.
	//   - No _CLUSTER_LIGHT_LOOP: the pipeline is Forward. NOTE: if the
	//     URP renderer is ever switched to Forward+, ships go dark
	//     silently - re-add then, do not debug it twice.
	//   - No GBuffer/Meta/2D passes: deferred is not in use and ships
	//     are not lightmapped.
	//   - No DOTS instancing, no debug-display variants.
	//
	// Sample count check (what survives): main light shadows x3,
	// additional lights x2 (+ vertex form), additional shadows, the
	// four soft-shadow qualities Unity 6 picks by pipeline setting, and
	// the two reflection-probe keywords. That is the stock Lit
	// behaviour a combat scene leans on, minus the tree it does not.
	Properties
	{
		// The five the ships come with + the decal layer. Names, ordering
		// and defaults match stock Lit for every slot Lit also has, so
		// swapping an existing ship material onto this shader keeps its
		// bindings and keywords.
		[MainTexture] _BaseMap("Albedo", 2D) = "white" {}
		[MainColor] _BaseColor("Albedo Tint", Color) = (1,1,1,1)

		[Normal] _BumpMap("Normal Map", 2D) = "bump" {}
		_BumpScale("Normal Strength", Float) = 1.0

		_OcclusionMap("Ambient Occlusion", 2D) = "white" {}
		_OcclusionStrength("AO Strength", Range(0.0, 1.0)) = 1.0

		// Unity packing: metallic in R, smoothness in alpha - measured on
		// the ship packs (see header). The bare scalars are the no-map
		// fallback and scale the map the same way stock Lit does.
		_MetallicGlossMap("Metallic+Smoothness (metal=R, smooth=A)", 2D) = "white" {}
		_Metallic("Metallic", Range(0.0, 1.0)) = 0.0
		_Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5

		// The luminance maps bind here on the migrated materials.
		[HDR] _EmissionColor("Luminance Color (HDR)", Color) = (0,0,0)
		_EmissionMap("Luminance (Emission) Map", 2D) = "white" {}

		// The decal layer: RGBA, its own tiling. Alpha is the brush;
		// RGB is a multiply filter (white = no paint). Default white =
		// inert by definition, however needs no-op honestly.
		_DecalMap("Decal Overlay (RGBA, multiply)", 2D) = "white" {}

		// How far a decal texel stops SHADING and starts PAINTING.
		// 0 keeps the multiply overlay only (whites vanish into the
		// hull). 1 lets the decal's own colour take the texel over.
		// Mid values paint the mark while the hull's dirt and panel
		// wear still shade through from underneath - the balance
		// between stencilled-on wear and fresh livery.
		_DecalPaintStrength("Decal Paint Strength", Range(0.0, 1.0)) = 0.0

		// The emissive decal companion: same _DecalMap tiling, pixel-aligned
		// artwork; RGBA where rgb glows and a gates it. HDR color for the
		// per-material retune. Default black + toggle off = zero cost
		// and zero light on factions that do not use glowing decals.
		[HDR] _DecalEmissionColor("Decal Glow Color (HDR)", Color) = (1,1,1,1)
		_DecalEmissionMap("Decal Glow Map (RGB col, A mask)", 2D) = "black" {}
		[Toggle(_DECALEMISSION)] _DecalEmissionToggle("Emissive Decals", Float) = 0.0

		[ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
		[ToggleOff] _EnvironmentReflections("Environment Reflections", Float) = 1.0

		_Cull("Face Culling", Float) = 2.0
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

		// Shared input: declared ONCE so every pass sees the identical
		// UnityPerMaterial layout (SRP batcher requires per-material
		// buffers to match across passes, and this is the cheapest way
		// to guarantee it).
		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
		// Explicit so the forward pass alone never has to prove the
		// include chain found it.
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"

		// NOTE: Do not ifdef the properties here - SRP batching cannot
		// handle different bulk layouts between passes.
		// _Cull and the [ToggleOff] floats are deliberately NOT here:
		// _Cull is consumed by the render state, the toggles drive
		// keywords only - stock Lit keeps them out of the buffer too,
		// and stray cbuffer members are how layouts drift apart.
		CBUFFER_START(UnityPerMaterial)
		float4 _BaseMap_ST;
		float4 _DecalMap_ST;
		half4 _BaseColor;
		half4 _EmissionColor;
		half4 _DecalEmissionColor;
		half _BumpScale;
		half _OcclusionStrength;
		half _Metallic;
		half _Smoothness;
		half _DecalPaintStrength;
		CBUFFER_END

		TEXTURE2D(_BaseMap);          SAMPLER(sampler_BaseMap);
		TEXTURE2D(_BumpMap);          SAMPLER(sampler_BumpMap);
		TEXTURE2D(_OcclusionMap);     SAMPLER(sampler_OcclusionMap);
		TEXTURE2D(_MetallicGlossMap); SAMPLER(sampler_MetallicGlossMap);
		TEXTURE2D(_EmissionMap);      SAMPLER(sampler_EmissionMap);
		TEXTURE2D(_DecalMap);         SAMPLER(sampler_DecalMap);
		TEXTURE2D(_DecalEmissionMap); SAMPLER(sampler_DecalEmissionMap);
		ENDHLSL

		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode" = "UniversalForward" }

			// Plain render-state commands, mirroring stock Lit's opaque
			// forward pass.
			Blend One Zero
			ZWrite On
			ZTest LEqual
			Cull [_Cull]

			HLSLPROGRAM
			#pragma vertex ShipSurfaceVertex
			#pragma fragment ShipSurfaceFragment

			// Material keywords -- same names stock Lit uses, so the
			// extracted ship materials carry theirs across a swap.
			#pragma shader_feature_local _NORMALMAP
			#pragma shader_feature_local _RECEIVE_SHADOWS_OFF
			#pragma shader_feature_local_fragment _EMISSION
			#pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
			#pragma shader_feature_local_fragment _OCCLUSIONMAP
			#pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
			#pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
			// Emissive decals: on per material ([Toggle] drawer). Off by
			// default so faction hulls that never glow pay nothing.
			#pragma shader_feature_local_fragment _DECALEMISSION

			// Pipeline keywords, trimmed to what a Hull forward pass can
			// be asked for by the project's setup (see header).
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

			// ---- the surface, ship edition ----
			// One sample per map, one overlay, straight SurfaceData out.
			void InitializeShipSurfaceData(Varyings input, out SurfaceData surfaceData)
			{
				surfaceData = (SurfaceData)0;

				half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

				// The decal layer, in two halves. Base: the multiply filter
				// (alpha is the brush, RGB the filter; white erases itself,
				// alpha 0 ignores the artwork's transparent-region RGB) -
				// dirt, panel wear and tint washes ride through here. Then
				// _DecalPaintStrength lifts the result toward the texel's
				// own colour: whites become paint instead of vanishing,
				// while the multiplied residue underneath stays as the
				// wear showing through them. Dark marks are the same in
				// both halves, so stencils and black icons read identically
				// at every strength.
				half4 decal = SAMPLE_TEXTURE2D(_DecalMap, sampler_DecalMap, input.decalUV);
				half3 shaded = albedo.rgb * lerp(half3(1.0h, 1.0h, 1.0h), decal.rgb, decal.a);
				albedo.rgb = lerp(shaded, decal.rgb, decal.a * _DecalPaintStrength);

				surfaceData.albedo = albedo.rgb;
				surfaceData.alpha = 1.0h; // opaque hulls, always

				// Metallic in R, smoothness in alpha (measured packing).
				#ifdef _METALLICSPECGLOSSMAP
				half4 metalSmooth = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, input.uv);
				surfaceData.metallic = metalSmooth.r;
				surfaceData.smoothness = metalSmooth.a * _Smoothness;
				#else
				surfaceData.metallic = _Metallic;
				surfaceData.smoothness = _Smoothness;
				#endif

				// Glossy highlights' albedo fallback (Lit parity).
				surfaceData.specular = half3(0, 0, 0); // metallic workflow

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

				// Luminance = emission, stock slot and math.
				#ifdef _EMISSION
				surfaceData.emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb * _EmissionColor.rgb;
				#else
				surfaceData.emission = half3(0, 0, 0);
				#endif

				// Emissive decals: pixel-aligned with the paint decal (same
				// decalUV). RGB glows, its alpha gates it, the HDR color
				// retunes per faction. ADDS to the luminance above, never
				// replaces it.
				#ifdef _DECALEMISSION
				half4 decalGlow = SAMPLE_TEXTURE2D(_DecalEmissionMap, sampler_DecalEmissionMap, input.decalUV);
				surfaceData.emission += decalGlow.rgb * decalGlow.a * _DecalEmissionColor.rgb;
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

				// Shadow coordinate in the fragment: ships are few, big
				// and dynamic, and this saves the interpolator the
				// stock shader needs for cascade quality.
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

				// Ships are never lightmapped; their ambient is the SH
				// probe interpolator, computed in the vertex.
				inputData.bakedGI = SampleSHPixel(input.vertexSH, inputData.normalWS);

				inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
			}

			Varyings ShipSurfaceVertex(Attributes input)
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

				// Baked ambient: SH only, ships are never lightmapped.
				output.vertexSH = SampleSHVertex(normalInput.normalWS);

				output.positionWS = vertexInput.positionWS;
				output.positionCS = vertexInput.positionCS;

				return output;
			}

			half4 ShipSurfaceFragment(Varyings input) : SV_Target0
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				SurfaceData surfaceData;
				InitializeShipSurfaceData(input, surfaceData);

				InputData inputData;
				InitializeShipInputData(input, surfaceData.normalTS, inputData);

				half4 color = UniversalFragmentPBR(inputData, surfaceData);
				color.rgb = MixFog(color.rgb, inputData.fogCoord);
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

			#pragma vertex ShipShadowVertex
			#pragma fragment ShipShadowFragment

			// Directional vs punctual normal-bias formulas.
			#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

			//--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

			// Shadow-caster geometry parameters, written by
			// ShadowUtils.SetupShadowCasterConstantBuffer.
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

			// Opaque hulls: no alpha test in the shadow pass. The decal
			// does not shape the silhouette (paint is inside the hull).
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

			ShadowVaryings ShipShadowVertex(ShadowAttributes input)
			{
				ShadowVaryings output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionCS = GetShipShadowPositionHClip(input);
				return output;
			}

			half4 ShipShadowFragment(ShadowVaryings input) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(input);
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

			#pragma vertex ShipDepthOnlyVertex
			#pragma fragment ShipDepthOnlyFragment

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

			DepthVaryings ShipDepthOnlyVertex(DepthAttributes input)
			{
				DepthVaryings output = (DepthVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
				output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
				return output;
			}

			half ShipDepthOnlyFragment(DepthVaryings input) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
				return input.positionCS.z;
			}
			ENDHLSL
		}
	}
	FallBack "Hidden/Universal Render Pipeline/FallbackError"
	// No CustomEditor on purpose: the default inspector lists every
	// property above in declaration order. Stock Lit's custom GUI
	// would happily hide the decal and luminance rows, and a feature a
	// designer cannot see is a feature that does not exist.
}
