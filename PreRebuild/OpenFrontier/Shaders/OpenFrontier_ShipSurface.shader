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
	// THE DECAL LAYERS -- faction pattern, then superpower brand
	//
	// Two plain ALPHA OVERLAYS, in order. ALPHA is the coverage: where
	// a mark's alpha says ink, the hull is replaced by the mark's
	// colour; partial alpha leaves hull showing through. There is no
	// strength slider and no multiply leg any more - the amounts live
	// in the sheet (and the colours') own alpha, authored per texel,
	// so the global slider and its cap went with the old language. No
	// pass, no stencil - two samples and a couple of lerps, because
	// this game runs on phones where the cheapest feature is the one
	// that lives.
	//
	// LAYER 1 (the faction PATTERN): _DecalMap is a ZONE MASK, not
	// paint. R, G and B are pure channel weights (primary / secondary
	// / tertiary zones) recoloured from the material's triad
	// (_DecalPrimary/_DecalSecondary/_DecalTertiary), so ONE pattern
	// sheet serves every faction and each material picks its three
	// colours. Each colour's own alpha carries the ink: opaque paints
	// its zones solid, a partial alpha paints them translucently (hull
	// shows through the ink), alpha 0 empties the zone - the two
	// scales multiply, sheet coverage x ink opacity. Zones are painted
	// exclusive; a pixel blending two channels is an intended edge
	// blend (the shader sums the channel weights, so a zone boundary
	// colour-ramps for free), and anything that would sum past white
	// is saturated, never doubled. That same ink gate is what keeps an
	// unbound layer inert: the default texture is opaque BLACK, which
	// selects no zone at all, so black-in-black alpha draws nothing.
	// channelize.py in tools/decals builds sheets in this language from
	// three painted palette colours.
	//
	// LAYER 2 (the SUPERPOWER BRAND) is the same overlay a second
	// time: _PowerDecalMap, its own tiling (_PowerDecalMap_ST), the
	// sheet's own baked colours, alpha the amount, landing on top of
	// the faction pattern. It has no channel gate of its own, so its
	// inert contract is the _POWERDECAL toggle (same shape as
	// _DECALEMISSION below): off = unbound slot that must not be
	// sampled, inert AND free.
	//
	// LAYER 3 (the WEAR) is the grime pass: _DecalDirtMap, same decal
	// UV space as the pattern, RGB the dirt colour, A the amount. It
	// lands after both of the above a ONE plain alpha overlay, gated
	// to the painted ink alone (pattern ink, or the brand's amount
	// where it covers) - the hull underneath never grimes, and
	// translucent paint takes proportionally less dirt. Its inert
	// contract is the _DECALDIRT toggle, same shape as the other two.
	//
	// All of these layers overlay ALBEDO only. They do not push normals,
	// occlusion or smoothness: livery is paint on the hull, and painted
	// hulls still reflect the panel geometry underneath. If a decal
	// ever needs to fake its own bump, that is a second sampler and a
	// decision, not an accident.
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

		// LAYER 1 -- faction pattern. Not paint, a ZONE MASK: RGB are
		// pure channel weights (R primary / G secondary / B tertiary
		// zones), sheet alpha the ink coverage, and the composite is
		// a PLAIN ALPHA OVERLAY - no strength slider, the amounts live
		// in the sheet. The three colours below recolour one shared
		// pattern sheet per material; each colour's own alpha is INK
		// TRANSLUCENCY (opaque = solid, partial = hull through the
		// ink, 0 = zone erased). The default opaque-BLACK sheet
		// selects no zone at all, so an unbound layer is perfectly
		// inert by construction.
		_DecalMap("Decal Pattern Mask (R/G/B zones, A ink)", 2D) = "black" {}
		_DecalPrimary("Pattern Primary (R zones)", Color) = (1,1,1,1)
		_DecalSecondary("Pattern Secondary (G zones)", Color) = (1,1,1,1)
		_DecalTertiary("Pattern Tertiary (B zones)", Color) = (1,1,1,1)

		// LAYER 2 -- superpower brand over the pattern: the same plain
		// alpha overlay a second time (RGB the baked paint, A the
		// amount), its own tiling. Its inert contract is the TOGGLE:
		// off = the slot is unbound and must not be sampled - with no
		// strength slider left to gate it, off is how an empty brand
		// stays inert AND free.
		_PowerDecalMap("Superpower Brand (RGBA, overlay)", 2D) = "black" {}
		[Toggle(_POWERDECAL)] _PowerDecalToggle("Superpower Brand", Float) = 0.0

		// LAYER 3 -- the wear pass: _DecalDirtMap is grime over BOTH
		// decal layers. RGB is the dirt colour, A the dirtiness,
		// sampled in the pattern's decal space (decalUV -- a dirt
		// sheet paints against the same maker canvas as a decal
		// sheet; no ST of its own). It composites after everything as
		// a plain alpha overlay, but gated to ONLY the painted ink:
		// the hull below stays clean, and translucent ink takes dirt
		// at its own weight. Same inert contract as the two layers
		// above: toggle off = unbound and not sampled.
		_DecalDirtMap("Ship Decal Dirt (RGBA, overlay)", 2D) = "black" {}
		[Toggle(_DECALDIRT)] _DecalDirtToggle("Ship Decal Dirt", Float) = 0.0

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
		float4 _PowerDecalMap_ST;
		half4 _BaseColor;
		half4 _EmissionColor;
		half4 _DecalEmissionColor;
		half4 _DecalPrimary;
		half4 _DecalSecondary;
		half4 _DecalTertiary;
		half _BumpScale;
		half _OcclusionStrength;
		half _Metallic;
		half _Smoothness;
		CBUFFER_END

		TEXTURE2D(_BaseMap);          SAMPLER(sampler_BaseMap);
		TEXTURE2D(_BumpMap);          SAMPLER(sampler_BumpMap);
		TEXTURE2D(_OcclusionMap);     SAMPLER(sampler_OcclusionMap);
		TEXTURE2D(_MetallicGlossMap); SAMPLER(sampler_MetallicGlossMap);
		TEXTURE2D(_EmissionMap);      SAMPLER(sampler_EmissionMap);
		TEXTURE2D(_DecalMap);         SAMPLER(sampler_DecalMap);
		TEXTURE2D(_PowerDecalMap);    SAMPLER(sampler_PowerDecalMap);
		TEXTURE2D(_DecalDirtMap);     SAMPLER(sampler_DecalDirtMap);
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
			#pragma shader_feature_local_fragment _POWERDECAL
			// Wear over the decals, same per-material toggle contract:
			// off = unbound slot sampled by nobody.
			#pragma shader_feature_local_fragment _DECALDIRT

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
				float2  powerUV     : TEXCOORD7;   // _PowerDecalMap space (own ST)
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

				// The decal layers as plain ALPHA OVERLAYS: where a
				// mark's alpha says ink, the hull is REPLACED by the
				// mark's colour, partial alpha leaving hull through -
				// no strength slider, the amounts live in the sheet.
				// The pattern mask's RGB selects the faction triad
				// (edges blending as summed weights, saturate guarding
				// overlaps) and each colour's own alpha rides its
				// channel as INK TRANSLUCENCY: sheet coverage x ink
				// opacity gates the overlay, which also keeps the
				// opaque-BLACK default texture perfectly inert (black
				// selects no zone). The superpower brand is the same
				// overlay a second time in the sheet's own baked
				// colours, riding its _POWERDECAL toggle: unbound slot
				// = off = nothing sampled, free. The wear pass lands
				// over both in the same language, gated to the ink they
				// put down; see below.
				half4 mask = SAMPLE_TEXTURE2D(_DecalMap, sampler_DecalMap, input.decalUV);
				half3 triad = saturate(_DecalPrimary.rgb * mask.r
					+ _DecalSecondary.rgb * mask.g + _DecalTertiary.rgb * mask.b);
				half zoneA = saturate(_DecalPrimary.a * mask.r
					+ _DecalSecondary.a * mask.g + _DecalTertiary.a * mask.b);
				half ink = saturate(mask.a * zoneA);
				albedo.rgb = lerp(albedo.rgb, triad, ink);

				#ifdef _POWERDECAL
				half4 brand = SAMPLE_TEXTURE2D(_PowerDecalMap, sampler_PowerDecalMap, input.powerUV);
				albedo.rgb = lerp(albedo.rgb, brand.rgb, brand.a);
				// The brand joins the wear gate: dirt lands over its ink too.
				ink = max(ink, brand.a);
				#endif

				// The dirt lands last: a plain overlay over whatever the
				// two layers painted, gated by the ink they put down -
				// livery wears, bare hull does not.
				#ifdef _DECALDIRT
				half4 dirt = SAMPLE_TEXTURE2D(_DecalDirtMap, sampler_DecalDirtMap, input.decalUV);
				albedo.rgb = lerp(albedo.rgb, dirt.rgb, dirt.a * ink);
				#endif

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

				// Emissive decals: pixel-aligned with the pattern decal
				// (same decalUV and tiling). RGB glows, its alpha gates
				// it, the HDR color retunes per faction. ADDS to the
				// luminance above, never replaces it. The superpower
				// brand does not glow - deliberate: a layer earns its
				// glow with a sample, and no brand sheet has asked yet.
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
				output.powerUV = TRANSFORM_TEX(input.texcoord, _PowerDecalMap);

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
