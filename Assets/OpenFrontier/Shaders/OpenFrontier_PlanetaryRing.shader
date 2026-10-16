Shader "OpenFrontier/PlanetaryRing"
{
	// A planetary ring: a sheet of dust with rocks embedded in it.
	//
	// TWO UV CHANNELS, TWO TEXTURE SETS
	//
	//   UV1 (TEXCOORD0) -> the DUST.   UV2 (TEXCOORD1) -> the ROCKS.
	//
	// They are separate channels because they want different layouts, and one
	// layout cannot serve both: dust wants a smooth band, rocks want
	// near-isotropic texels. An earlier version read the mesh UV for both and
	// the rocks came out as radial streaks.
	//
	// UNITY NAMES THESE UV0 AND UV1; BLENDER AND 3DS MAX CALL THEM UV1 AND
	// UV2. "UV1" here means the FIRST channel, which is TEXCOORD0.
	//
	// THE ROCKS REQUIRE UV2 TO EXIST ON THE MESH
	//
	// A ModelImporter with generateSecondaryUV off will NOT invent one, so if
	// the mesh has no second channel every rock sample reads the same texel and
	// the belt becomes one repeated smudge. Debug stage 2 shows exactly that.
	//
	// ALPHA: ROCKS CUTOUT, DUST REGULAR
	//
	//   rocks  alpha 1. Opaque, always visible, whatever the lighting does.
	//          A rock silhouette is a hard edge via _RockCutout.
	//   dust   its own alpha, alpha blended, and softened at grazing angles so
	//          the band thins out instead of ending in a wall.
	//
	// Both are in ONE transparent pass with premultiplied blending, so the
	// rocks land at full strength and the dust blends under them. The cost of
	// not writing depth is that two rocks do not occlude one another; on a
	// sheet of scattered small rocks that reads fine, and a second material
	// for the rocks would double the draw calls for it.
	//
	// MAKING THE DUST FEEL VOLUMETRIC
	//
	// A flat sheet lit from one side is either lit or black, and black is the
	// problem -- the underside of a ring should glow with the light that came
	// through the dust from the far side. Three terms do that:
	//
	//   transmission  pow(saturate(dot(V, -L)), p). Largest when the camera
	//                 is on the dark side and the light is behind the dust,
	//                 which IS "the light hitting the light side is visible on
	//                 the underside too".
	//   rim           pow(1 - dot(N, V)). A thin sheet is nearly edge-on over
	//                 most of its area, so this is most of the effect.
	//   ambient floor a colour, not a light, so the unlit side has body rather
	//                 than dropping to black.
	//
	// All three are scaled by how much DUST there is (1 - rock), so a rock
	// never glows and the belt reads as solid against the dust around it.
	//
	// MOBILE FIRST: no shadow receiving, no additional lights, one sample per
	// texture, and every branch is on a uniform so it is uniform-coherent.

	Properties
	{
		// ---- dust, UV1 ----
		[MainTexture] _DustAlbedo ("Dust Albedo (RGB, A = coverage)", 2D) = "black" {}
		[Normal] _DustNormals ("Dust Normals (tangent space)", 2D) = "bump" {}
		_DustSpecMap ("Dust Spec Mask (R = detail/gloss)", 2D) = "black" {}
		[HDR] _DustColor ("Dust Tint", Color) = (1, 1, 1, 1)
		_DustSmoothness ("Dust Smoothness", Range(0, 1)) = 0.08
		_DustDetailSmoothness ("Dust Detail Smoothness", Range(0, 1)) = 0.15
		_DustNormalStrength ("Dust Normal Strength", Range(0, 2)) = 1

		// ---- rocks, UV2 ----
		_RockAlbedo ("Rock Albedo (RGB, A = coverage)", 2D) = "black" {}
		[Normal] _RockNormals ("Rock Normals (tangent space)", 2D) = "bump" {}
		_RockMask ("Rock Mask  R = metallic, A = smoothness", 2D) = "black" {}
		[HDR] _RockColor ("Rock Tint", Color) = (1, 1, 1, 1)
		_RockNormalStrength ("Rock Normal Strength", Range(0, 2)) = 1
		// Unity builds Mikk tangents from UV0. On a ring whose UV2 is
		// deliberately rotated away from UV1 -- which is what counteracting
		// the circle's distortion does -- that imported frame is the wrong
		// one for the rock normal map, by a median 40 degrees. Measured on
		// this mesh: UV1's tangent is purely tangential (radial component
		// 0.012), UV2's is a 45 degree diagonal (0.674). Decoding the rock
		// map in the UV1 frame points its normals somewhere arbitrary and
		// lights up as slivers.
		//
		// "From UV2" derives the frame from the uv that actually indexes the
		// map, so the two cannot drift apart. Costs two derivative
		// instructions and no texture fetch.
		[Enum(From UV2, 1, From Imported Tangents, 0)]
		_RockTangentSource ("Rock Tangent Source", Float) = 1
		_RockMetallicScale ("Rock Metallic Scale", Range(0, 1)) = 1
		_RockSmoothnessScale ("Rock Smoothness Scale", Range(0, 1)) = 0.6
		// Hard silhouette for the rocks. Above this coverage a rock is solid,
		// below it there is no rock and the dust shows through.
		_RockCutout ("Rock Cutout", Range(0, 1)) = 0.5
		// How much the rim glow picks the rocks out of the dust.
		_RockRimResistance ("Rock Rim Resistance", Range(0, 1)) = 1

		// ---- what turns dust into rock ----
		[Enum(Rock Alpha, 0, Mask Texture, 1, Vertex Alpha, 2, All Rock, 3)]
		_BlendMaskSource ("Blend Mask Source", Float) = 0
		_BlendMask ("Blend Mask (R)", 2D) = "white" {}
		[Enum(Lerp, 0, Smoothstep, 1)] _BlendShape ("Blend Shape", Float) = 0
		_BlendFloor ("Blend Floor", Range(0, 1)) = 0
		_BlendRange ("Blend Range", Range(0.001, 1)) = 0.3

		// ---- making the dust volumetric ----
		[HDR] _DustAmbient ("Dust Ambient", Color) = (0.04, 0.04, 0.05, 1)
		_ScatterStrength ("Dust Transmission Strength", Range(0, 4)) = 1.2
		_ScatterPower ("Dust Transmission Power", Range(0.5, 16)) = 4
		[HDR] _ScatterColor ("Dust Transmission Colour", Color) = (1, 0.94, 0.84, 1)
		_RimStrength ("Dust Rim Strength", Range(0, 4)) = 0.6
		_RimPower ("Dust Rim Power", Range(0.5, 16)) = 3
		// Wraps the dust's diffuse terminator, so it fades out over the
		// terminator instead of snapping off at it.
		_DustWrap ("Dust Wrap", Range(0, 1)) = 0.6

		// ---- overall ----
		_AlphaScale ("Alpha Scale", Range(0, 1)) = 1
		_AlphaPower ("Alpha Power", Range(0.25, 4)) = 1
		// Thin the dust at grazing angles. A flat sheet seen edge-on is all
		// edge-on, so without this the band ends in a wall instead of fading.
		_AlphaGrazing ("Alpha Grazing Falloff", Range(0, 2)) = 0.6
		// x = inner start, y = inner fade, z = outer end, w = outer fade.
		// OBJECT space. Default strength 0, i.e. OFF: it multiplies alpha, and
		// a fade that is subtly wrong does not dim the ring, it erases it.
		_Edges ("Edges (x=inner, y=inner fade, z=outer, w=outer fade)", Vector) = (0, 1, 10000, 1)
		_EdgeFadeStrength ("Edge Fade Strength", Range(0, 1)) = 0
		// 0 final | 1 dust albedo | 2 rock albedo | 3 uv2 luma | 4 lit, alpha 1
		_AlbedoDebug ("Albedo Debug", Range(0, 4)) = 0
		[Enum(Off, 0, Back, 1, Front, 2)] _Cull ("Culling", Float) = 0
	}

	SubShader
	{
		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
			"RenderType" = "Transparent"
			"Queue" = "Transparent"
			"DisableBatching" = "True"
		}

		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode" = "UniversalForward" }

			// PREMULTIPLIED: One / OneMinusSrcAlpha, and the fragment
			// premultiplies to match. Under the ordinary SrcAlpha form a
			// half-transparent pixel contributes its full lit colour at half
			// strength and darkens toward the background instead of fading into
			// it, which is a black fringe on every soft edge in the ring.
			//
			// Cull off: a ring is seen from both faces and a back-facing annulus
			// lit by its own geometric normal is black.
			Blend One OneMinusSrcAlpha
			ZWrite Off
			ZTest LEqual
			Cull [_Cull]

			HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			// No multi_compile_fog, and no MixFog. The bound URP asset carries
			// no fog settings at all, and no shader in this project ever
			// assigns inputData.fogCoord, so the planet shader's MixFog reads a
			// zeroed field and does nothing. Copying it would pay for a fog
			// variant on every ring pixel to reproduce dead code, and a ring
			// is in space. Dropped deliberately -- do not "restore" it without
			// also populating fogCoord, or it will still not fog.

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			// One sampler state per texture. Sharing one sampler across six
			// textures means the last one set silently sets wrap and filter for
			// all the others.
			TEXTURE2D(_DustAlbedo);   SAMPLER(sampler_DustAlbedo);
			TEXTURE2D(_DustNormals);   SAMPLER(sampler_DustNormals);
			TEXTURE2D(_DustSpecMap);   SAMPLER(sampler_DustSpecMap);
			TEXTURE2D(_RockAlbedo);    SAMPLER(sampler_RockAlbedo);
			TEXTURE2D(_RockNormals);   SAMPLER(sampler_RockNormals);
			TEXTURE2D(_RockMask);      SAMPLER(sampler_RockMask);
			TEXTURE2D(_BlendMask);     SAMPLER(sampler_BlendMask);

			// Every material property belongs here, textures included by way of
			// their _ST, or the SRP Batcher drops to per-draw setup.
			CBUFFER_START(UnityPerMaterial)
				float4 _DustAlbedo_ST;
				float4 _DustNormals_ST;
				float4 _DustSpecMap_ST;
				float4 _RockAlbedo_ST;
				float4 _RockNormals_ST;
				float4 _RockMask_ST;
				float4 _BlendMask_ST;
				float4 _DustColor;
				float4 _RockColor;
				float4 _DustAmbient;
				float4 _ScatterColor;
				float4 _Edges;
				float _DustSmoothness;
				float _DustDetailSmoothness;
				float _DustNormalStrength;
				float _RockNormalStrength;
				float _RockTangentSource;
				float _RockMetallicScale;
				float _RockSmoothnessScale;
				float _RockCutout;
				float _RockRimResistance;
				float _BlendMaskSource;
				float _BlendShape;
				float _BlendFloor;
				float _BlendRange;
				float _ScatterStrength;
				float _ScatterPower;
				float _RimStrength;
				float _RimPower;
				float _DustWrap;
				float _AlphaScale;
				float _AlphaPower;
				float _AlphaGrazing;
				float _EdgeFadeStrength;
				float _AlbedoDebug;
				float _Cull;
			CBUFFER_END

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 tangentOS : TANGENT;
				// UV1: the dust. UV2: the rocks. Separate channels because they
				// want separate layouts.
				float2 uv1 : TEXCOORD0;
				float2 uv2 : TEXCOORD1;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv1 : TEXCOORD0;
				float2 uv2 : TEXCOORD1;
				float3 positionWS : TEXCOORD2;
				float3 positionOS : TEXCOORD3;
				half3 normalWS : TEXCOORD4;
				half4 tangentWS : TEXCOORD5;
			};

			// SV_IsFrontFace is INPUT only. Declaring it in Varyings, which
			// vert() writes, is an illegal output semantic on Vulkan. It goes in
			// the fragment input, with the varyings nested as `interpolated`,
			// which is how URP does it in SpeedTree8Passes.hlsl.
			struct FragmentInput
			{
				Varyings interpolated;
				FRONT_FACE_TYPE facing : FRONT_FACE_SEMANTIC;
			};

			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;
				VertexPositionInputs posInputs =
					GetVertexPositionInputs(input.positionOS.xyz);
				VertexNormalInputs normInputs =
					GetVertexNormalInputs(input.normalOS, input.tangentOS);

				output.positionCS = posInputs.positionCS;
				output.positionWS = posInputs.positionWS;
				output.positionOS = input.positionOS.xyz;
				output.normalWS = normInputs.normalWS;
				output.tangentWS = half4(normInputs.tangentWS, input.tangentOS.w);

				// RAW uv, not TRANSFORM_TEX'd: each set carries its own _ST so
				// the dust and the rocks tile independently while still agreeing
				// on where they are.
				output.uv1 = input.uv1;
				output.uv2 = input.uv2;
				return output;
			}

			// Radial fade, OBJECT space. A ring's radius is a property of the
			// ring; measuring it in world space ties a local effect to wherever
			// the system happens to sit, and this did -- with the outer end at
			// 10000 units, any world further than that from the origin
			// multiplied alpha by exactly zero and the ring drew nothing.
			half EdgeFade(float3 positionOS)
			{
				half d = length(positionOS);
				half inner = saturate((d - _Edges.x) * rcp(max(1e-4, _Edges.y)));
				half outer = 1.0 - saturate((d - _Edges.z) * rcp(max(1e-4, _Edges.w)));
				return saturate(inner) * saturate(outer);
			}

			// A tangent frame derived from the uv that indexes the map, so the
			// two cannot disagree. Cheaper than carrying a second set of
			// imported tangents and, unlike them, guaranteed consistent.
			//
			// The uv must be RAW here, not TRANSFORM_TEX'd. Tiling is an
			// affine scale of both axes, which leaves the frame's direction
			// unchanged, so either works -- but raw makes that independence
			// explicit instead of accidental.
			//
			// The guards are not defensive noise. A degenerate uv makes both
			// vectors collapse to zero, and normalizing a zero vector is NaN,
			// which reaches the framebuffer as a black or white speck rather
			// than as an error anyone can find.
			void FrameFromUV(float3 positionWS, float2 uv, float3 normalWS,
			                 out float3 tangentOut, out float3 bitangentOut)
			{
				float3 dp1 = ddx(positionWS);
				float3 dp2 = ddy(positionWS);
				float2 duv1 = ddx(uv);
				float2 duv2 = ddy(uv);

				// Project the position derivatives into the surface plane, then
				// solve for the directions that map d(uv) onto them.
				float3 dp2perp = cross(dp2, normalWS);
				float3 dp1perp = cross(normalWS, dp1);
				float3 T = dp2perp * duv1.x + dp1perp * duv2.x;
				float3 B = dp2perp * duv1.y + dp1perp * duv2.y;

				float inv = rcp(max(abs(dot(dp1, dp2perp)), 1e-8));
				T *= inv;
				B *= inv;

				float lenT = length(T);
				float lenB = length(B);
				// Clamp before the divide: rcp(0) is the infinity that turns
				// into the NaN above, and max() is cheaper than a branch.
				T *= rcp(max(lenT, 1e-8));
				B *= rcp(max(lenB, 1e-8));

				// Gram-Schmidt against the geometric normal. T and B are not
				// orthogonal to each other and not perpendicular to N once
				// the map's V axis is skewed relative to the surface.
				T = normalize(T - normalWS * dot(normalWS, T));
				float3 Bortho = cross(normalWS, T);
				// Keep the original B's handedness. A mirrored map needs its
				// bitangent flipped or the bumps light from the wrong side.
				Bortho *= (dot(B, Bortho) < 0.0) ? -1.0 : 1.0;

				// Flag a collapsed frame so the caller can fall back rather
				// than shade from NaN.
				tangentOut = (min(lenT, lenB) > 1e-8) ? T : float3(1, 0, 0);
				bitangentOut = (min(lenT, lenB) > 1e-8) ? Bortho : float3(0, 0, 1);
			}

			half4 frag(FragmentInput input) : SV_Target
			{
				Varyings v = input.interpolated;

				// ---- one sample each; every channel needed below reuses these ----
				float2 uvDust = TRANSFORM_TEX(v.uv1, _DustAlbedo);
				float2 uvRock = TRANSFORM_TEX(v.uv2, _RockAlbedo);

				half4 dustTex = SAMPLE_TEXTURE2D(_DustAlbedo, sampler_DustAlbedo, uvDust);
				half4 rockTex = SAMPLE_TEXTURE2D(_RockAlbedo, sampler_RockAlbedo, uvRock);
				half4 dustSpec = SAMPLE_TEXTURE2D(_DustSpecMap, sampler_DustSpecMap,
					TRANSFORM_TEX(v.uv1, _DustSpecMap));
				half4 rockMask = SAMPLE_TEXTURE2D(_RockMask, sampler_RockMask,
					TRANSFORM_TEX(v.uv2, _RockMask));
				half3 dustN = UnpackNormal(SAMPLE_TEXTURE2D(_DustNormals,
					sampler_DustNormals, TRANSFORM_TEX(v.uv1, _DustNormals)));
				half3 rockN = UnpackNormal(SAMPLE_TEXTURE2D(_RockNormals,
					sampler_RockNormals, TRANSFORM_TEX(v.uv2, _RockNormals)));

				// ---- how much rock is there here ----
				// Source 0 reuses the rock fetch, so the default costs nothing.
				half rock;
				if (_BlendMaskSource < 0.5)
				{
					rock = rockTex.a;
				}
				else if (_BlendMaskSource < 1.5)
				{
					rock = SAMPLE_TEXTURE2D(_BlendMask, sampler_BlendMask,
						TRANSFORM_TEX(v.uv2, _BlendMask)).r;
				}
				else if (_BlendMaskSource < 2.5)
				{
					rock = 1.0;      // no vertex colour in this version
				}
				else
				{
					rock = 1.0;
				}

				// Thresholded: two genuinely different materials should meet at
				// a decided edge, not sit in a permanent smear.
				rock = saturate((rock - _BlendFloor) * rcp(max(1e-4, _BlendRange)));
				if (_BlendShape > 0.5)
				{
					rock = rock * rock * (3.0 - 2.0 * rock);
				}

				half3 albedoDebug = lerp(dustTex.rgb * _DustColor.rgb,
					rockTex.rgb * _RockColor.rgb, rock);

				// ---- staged bisect ----
				// Returned BEFORE the lighting with alpha 1, so what is on
				// screen is the value under test and nothing else. Bounded at
				// BOTH ends: written as `> 0` alone, every stage matches, they
				// all look identical to stage 1, and the later ones become
				// unreachable and report nothing.
				if (_AlbedoDebug > 0.5 && _AlbedoDebug < 1.5)
					return half4(dustTex.rgb, 1.0);
				if (_AlbedoDebug > 1.5 && _AlbedoDebug < 2.5)
					return half4(rockTex.rgb, 1.0);
				// Stage 3 exists for one reason: UV2. If this is a single flat
				// colour across the whole ring, the mesh has no second UV
				// channel and every rock sample is reading the same texel.
				if (_AlbedoDebug > 2.5 && _AlbedoDebug < 3.5)
					return half4(v.uv2.x, v.uv2.y, 0.0, 1.0);
				if (_AlbedoDebug > 3.5)
					return half4(1.0, 0.0, 1.0, 1.0);   // magenta: reached the end

				// ---- normals ----
				// TWO FRAMES, because two UV channels are two tangent spaces.
				//
				// The dust normal map is indexed by UV1 and the imported tangent
				// was built from UV1, so those agree and the imported frame is
				// used unchanged. The rock map is indexed by UV2, whose frame on
				// this mesh is rotated ~40 degrees away from the imported one,
				// so it gets a frame derived from UV2 itself.
				//
				// Blend in WORLD space. The earlier version lerped in tangent
				// space, which is only valid when both maps share a frame --
				// here they do not, so there is no common space to lerp in.
				// Transforming first and lerping the world normals is the
				// correct order, and normalize() afterwards covers the fact
				// that the two results are not exactly unit length.
				//
				// Flip the geometric normal on back faces, or the underside of
				// the ring is lit from behind and comes out black.
				half3 geoNormalWS = normalize(v.normalWS)
					* IS_FRONT_VFACE(input.facing, 1.0, -1.0);

				half3 importedT = normalize(v.tangentWS.xyz);
				half3 importedB = cross(geoNormalWS, importedT) * v.tangentWS.w;
				half3 dustNormalWS = normalize(TransformTangentToWorld(
					dustN * _DustNormalStrength,
					half3x3(importedT, importedB, geoNormalWS)));

				half3 rockT, rockB;
				FrameFromUV(v.positionWS, v.uv2, geoNormalWS, rockT, rockB);

				// The A/B switch: From Imported Tangents reproduces the old,
				// visibly wrong behaviour so the difference is attributable.
				half3 rockTsel = lerp(importedT, rockT, _RockTangentSource);
				half3 rockBsel = lerp(importedB, rockB, _RockTangentSource);
				half3 rockNormalWS = normalize(TransformTangentToWorld(
					rockN * _RockNormalStrength,
					half3x3(rockTsel, rockBsel, geoNormalWS)));

				half3 shadedNormalWS = normalize(
					lerp(dustNormalWS, rockNormalWS, rock));

				SurfaceData surfaceData = (SurfaceData)0;
				surfaceData.albedo = albedoDebug;
				// Dust is a dielectric: constant 0 metallic. The rocks are the
				// metallic half of the pair, which is why they need their own
				// channel and their own set.
				surfaceData.metallic = saturate(rockMask.r * _RockMetallicScale) * rock;
				surfaceData.smoothness = lerp(
					saturate(_DustSmoothness + dustSpec.r * _DustDetailSmoothness),
					saturate(rockMask.a * _RockSmoothnessScale),
					rock);
				// The real normal travels in inputData.normalWS; UniversalFragmentPBR
				// never reads normalTS (InitializeBRDFData uses only albedo,
				// metallic, specular, smoothness and alpha), so this is left flat
				// rather than carrying a tangent-space value from a frame that no
				// longer exists in one piece.
				surfaceData.normalTS = half3(0, 0, 1);
				surfaceData.occlusion = 1.0;

				InputData inputData = (InputData)0;
				inputData.positionWS = v.positionWS;
				inputData.normalWS = shadedNormalWS;
				inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(v.positionWS);
				inputData.shadowCoord = TransformWorldToShadowCoord(v.positionWS);
				inputData.vertexLighting = half3(0, 0, 0);
				inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(v.positionCS);
				inputData.shadowMask = half4(1, 1, 1, 1);

				Light mainLight = GetMainLight(inputData.shadowCoord);

				// ---- the volumetric part ----
				half3 lightDir = mainLight.direction;          // surface -> light
				half3 viewDir = inputData.viewDirectionWS;      // surface -> camera
				half ndl = dot(shadedNormalWS, lightDir);

				// Wrapped diffuse for the dust. Without it the terminator is a
				// hard line and the dust looks like a solid sheet, which is the
				// opposite of volumetric.
				half wrap = 1.0 / max(1e-3, 1.0 + _DustWrap);
				half dustDiffuse = saturate((ndl + _DustWrap) * wrap);
				// Only the dust scatters; a rock gets the ordinary PBR term.
				half dustness = 1.0 - rock;

				// Light that came THROUGH the sheet from the lit side to the
				// side being looked at. Largest when the camera is on the dark
				// side and the light is beyond the dust, which is exactly "the
				// light hitting the light side is visible on the underside too".
				half transmission = pow(saturate(dot(viewDir, -lightDir)), _ScatterPower);

				// A thin sheet is nearly edge-on over most of its area, so the
				// limb term carries most of the volumetric read.
				half rim = pow(1.0 - saturate(dot(shadedNormalWS, viewDir)), _RimPower);

				// Rocks resist the glow, or the belt turns into fog.
				rim *= lerp(1.0, _RockRimResistance, rock);

				half3 volumetric = dustness * mainLight.color * dustDiffuse
					* (_DustColor.rgb * _ScatterStrength * transmission * _ScatterColor.rgb
					   + _DustColor.rgb * _RimStrength * rim);

				surfaceData.emission = volumetric + _DustAmbient.rgb * dustness;

				half4 color = UniversalFragmentPBR(inputData, surfaceData);

				// ---- coverage ----
				// Rocks are OPAQUE: alpha 1, so they are always visible whatever
				// the lighting does. The cutout is a hard silhouette, so a rock
				// either is there or it is not -- there is no half-faded rock.
				half dustAlpha = pow(saturate(dustTex.a), _AlphaPower);
				// A flat sheet seen edge-on is all edge-on; without this the band
				// ends in a wall rather than fading.
				dustAlpha *= 1.0 - _AlphaGrazing
					* pow(1.0 - saturate(dot(geoNormalWS, viewDir)), 2.0);
				dustAlpha *= lerp(1.0, EdgeFade(v.positionOS), _EdgeFadeStrength);

				half alpha = lerp(saturate(dustAlpha * _AlphaScale), 1.0, rock);
				color.a = alpha;

				// Premultiply, to match Blend One / OneMinusSrcAlpha above.
				color.rgb *= color.a;
				return color;
			}
			ENDHLSL
		}
	}

	// No fallback SubShader. The project is URP-only -- the graphics settings
	// bind a UniversalRenderPipelineAsset -- so a built-in CGPROGRAM pass here
	// is unreachable code that Unity still compiles.
	Fallback Off
}