Shader "OpenFrontier/PlanetaryRing"
{
	// A planetary ring: a sheet of dust, lit and seen from both faces.
	//
	// DUST ONLY. The rocks were a second layer blended in on UV2, and they
	// are deliberately absent here rather than disabled -- every property,
	// sample and branch that existed only to serve them is gone, so what is
	// left is the dust's own behaviour and nothing to mentally account for.
	// They are not gone from the design: the second UV channel and the
	// second tangent frame are exactly what bringing them back would need,
	// and neither is expensive. See the notes on FrameFromUV in git history
	// (11cc1324) for why UV2 needs a frame of its own.
	//
	// EVERYTHING THE TEXTURES READ IS UV-BASED
	//
	// The band fade is driven by V, not by object-space radius. V is a clean
	// monotonic radial ramp on this mesh -- measured at 0.0 across all 512
	// inner-edge loops and 1.0 across all 512 outer-edge loops, 0
	// non-monotonic steps in 1024 -- so a fade in V is the same fade, made
	// independent of how the mesh is scaled or placed.
	//
	// The one exception is the planet shadow, which reads object space, and
	// that is shading rather than texturing: it asks whether a sphere
	// occludes this point, which is geometry, not a property of what is
	// painted on the surface. Baking it into a texture would freeze one
	// particular sun position into the albedo.
	//
	// VOLUMETRIC DUST
	//
	// A flat sheet lit from one side is either lit or black, and black is
	// the problem: the underside of a ring should glow with the light that
	// came through the dust from the far side. Three terms:
	//
	//   scatter    a Henyey-Greenstein phase lobe. Real dust forward-
	//              scatters -- it is much brighter looking toward the sun
	//              through the sheet than away from it -- and HG is the
	//              function that describes that, with one parameter. The
	//              previous pow() lobe is a Blinn stand-in: it peaks in the
	//              right place but is symmetric and far too narrow at the
	//              back.
	//   rim        pow(1 - dot(N, V)). A thin sheet is nearly edge-on over
	//              most of its area, so this carries much of the read.
	//   ambient    a colour, not a light, so the unlit side has body rather
	//              than dropping to black. URP's bakedGI is zero here, so
	//              nothing else would put anything in the shadow.
	//
	// MOBILE FIRST: no shadow receiving from the map, no additional lights,
	// one sample per texture, and every branch is on a uniform so it stays
	// uniform-coherent.

	Properties
	{
		[MainTexture] _DustAlbedo ("Dust Albedo (RGB, A = density)", 2D) = "black" {}
		[Normal] _DustNormals ("Dust Normals (tangent space)", 2D) = "bump" {}
		_DustSpecMap ("Dust Spec Mask (R = detail gloss)", 2D) = "black" {}
		[HDR] _DustColor ("Dust Tint", Color) = (1, 1, 1, 1)
		_DustSmoothness ("Dust Smoothness", Range(0, 1)) = 0.08
		_DustDetailSmoothness ("Dust Detail Smoothness", Range(0, 1)) = 0.15
		_DustNormalStrength ("Dust Normal Strength", Range(0, 2)) = 1

		// ---- coverage ----
		_AlphaScale ("Alpha Scale", Range(0, 1)) = 1
		_AlphaPower ("Alpha Power", Range(0.25, 4)) = 1
		// Thin the dust at grazing angles. A flat sheet is all edge-on, so
		// without this the band ends in a wall instead of fading.
		_AlphaGrazing ("Alpha Grazing Falloff", Range(0, 2)) = 0.6

		// ---- volumetric ----
		[HDR] _DustAmbient ("Dust Ambient", Color) = (0.04, 0.04, 0.05, 1)
		_ScatterStrength ("Forward Scatter Strength", Range(0, 4)) = 1.5
		// HG anisotropy. Positive = forward scattering, which is what dust
		// does. 0 is isotropic; negative would back-scatter like a retroreflector.
		_ScatterG ("Forward Scatter Bias", Range(-0.9, 0.9)) = 0.6
		[HDR] _ScatterColor ("Scatter Colour", Color) = (1, 0.94, 0.84, 1)
		_RimStrength ("Rim Strength", Range(0, 4)) = 0.5
		_RimPower ("Rim Power", Range(0.5, 16)) = 3
		// Wraps the dust's diffuse terminator so it fades out across it
		// instead of snapping off at it.
		_DustWrap ("Diffuse Wrap", Range(0, 1)) = 0.6

		// ---- planet shadow ----
		// A shadow map cannot reach this ring. The project's shadow distance
		// is 3000 units over 4 cascades, and a ring that reads as a ring is
		// legible far beyond that -- and cascades spend their resolution near
		// the camera, which is the one region the ring does not need.
		//
		// So the occlusion is computed instead of sampled. The light's
		// direction is exact at any range (GetMainLight returns it
		// unconditionally; only shadowAttenuation is bounded), and whether a
		// sphere hides a point is geometry.
		//
		// OBJECT units, and the planet's centre is the object's origin --
		// which is true by construction, because the mesh is an annulus
		// centred on it. That is why this does not need the planet's world
		// position as a property: the offset from the centre is positionOS.
		// 0 disables it entirely.
		_PlanetRadius ("Planet Radius (object units, 0 = off)", Float) = 0
		_ShadowStrength ("Planet Shadow Strength", Range(0, 1)) = 1
		// Penumbra tangent, i.e. tan of the star's angular radius. The real
		// Sun is about 0.27 degrees, so 0.005 is in the right order. A fixed
		// softness instead would read as a stencil cut with scissors, and a
		// zero one is wrong for every real star.
		_PlanetShadowSoftness ("Shadow Penumbra Tangent", Float) = 0.005

		// ---- band edges, in V (0 = inner edge, 1 = outer edge) ----
		_Edges ("Edges (x=inner start, y=inner fade, z=outer start, w=outer fade)", Vector) = (0, 0.08, 0.92, 0.08)
		_EdgeFadeStrength ("Edge Fade Strength", Range(0, 1)) = 1

		// 0 final | 1 dust albedo | 2 uv luma | 3 magenta
		_AlbedoDebug ("Albedo Debug", Range(0, 3)) = 0
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
			// strength and darkens toward the background instead of fading
			// into it, which is a black fringe on every soft edge.
			//
			// Cull off: a ring is seen from both faces, and a back-facing
			// annulus lit by its own geometric normal is black.
			Blend One OneMinusSrcAlpha
			ZWrite Off
			ZTest LEqual
			Cull [_Cull]

			HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			// No multi_compile_fog, and no MixFog. The bound URP asset
			// carries no fog settings and no shader in this project ever
			// assigns inputData.fogCoord, so the planet shader's MixFog reads
			// a zeroed field and does nothing. Copying it would pay for a fog
			// variant per pixel to reproduce dead code, and a ring is in space.

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			// One sampler state per texture. Sharing one across three
			// textures means the last one set silently sets wrap and filter
			// for all the others.
			TEXTURE2D(_DustAlbedo);   SAMPLER(sampler_DustAlbedo);
			TEXTURE2D(_DustNormals);   SAMPLER(sampler_DustNormals);
			TEXTURE2D(_DustSpecMap);   SAMPLER(sampler_DustSpecMap);

			// Every material property belongs here, textures included by way
			// of their _ST, or the SRP Batcher drops to per-draw setup.
			CBUFFER_START(UnityPerMaterial)
				float4 _DustAlbedo_ST;
				float4 _DustNormals_ST;
				float4 _DustSpecMap_ST;
				float4 _DustColor;
				float4 _DustAmbient;
				float4 _ScatterColor;
				float4 _Edges;
				float _DustSmoothness;
				float _DustDetailSmoothness;
				float _DustNormalStrength;
				float _AlphaScale;
				float _AlphaPower;
				float _AlphaGrazing;
				float _ScatterStrength;
				float _ScatterG;
				float _RimStrength;
				float _RimPower;
				float _DustWrap;
				float _PlanetRadius;
				float _ShadowStrength;
				float _PlanetShadowSoftness;
				float _EdgeFadeStrength;
				float _AlbedoDebug;
				float _Cull;
			CBUFFER_END

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 tangentOS : TANGENT;
				float2 uv1 : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv1 : TEXCOORD0;
				float3 positionWS : TEXCOORD1;
				float3 positionOS : TEXCOORD2;
				half3 normalWS : TEXCOORD3;
				half4 tangentWS : TEXCOORD4;
			};

			// SV_IsFrontFace is INPUT only. Declaring it in Varyings, which
			// vert() writes, is an illegal output semantic on Vulkan. It goes
			// in the fragment input, with the varyings nested as
			// `interpolated`, which is how URP does it in SpeedTree8Passes.hlsl.
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
				// RAW uv, not TRANSFORM_TEX'd: the tiling that makes the band
				// look right is a property of the surface, not of any one
				// map, and all three share it.
				output.uv1 = input.uv1;
				return output;
			}

			// Henyey-Greenstein phase function, unnormalised so that g = 0
			// returns exactly 1 and _ScatterStrength means the same thing at
			// every bias. Dividing through by 4*PI is left out deliberately:
			// it is a constant, and folding it into the strength property
			// would only make the default value harder to reason about.
			half PhaseHG(half cosTheta, half g)
			{
				half g2 = g * g;
				// Clamped because g near 1 drives the denominator to zero
				// along the forward direction, where the lobe is meant to
				// spike but not to infinity.
				half d = max(1.0 + g2 - 2.0 * g * cosTheta, 1e-4);
				return (1.0 - g2) / (d * sqrt(d));
			}

			// Whether the planet hides this point from the star. Analytic, so
			// it is exact at any distance -- see the note on _PlanetRadius.
			//
			// For a directional light the planet's shadow is an infinite
			// cylinder of the planet's radius along the light direction, so
			// there is no quadratic to solve: a point is occluded exactly when
			// it is behind the planet and within the planet's radius of the
			// light axis.
			half PlanetShadow(float3 positionOS, half3 lightDirOS)
			{
				if (_PlanetRadius <= 0.0)
				{
					return 1.0;
				}
				// Signed distance along the light axis. lightDirOS points TOWARD
				// the star, so a point in front of the planet has
				// positionOS . L > 0 and a point behind it has < 0.
				//
				// Only a point BEHIND can be occluded, and the sign is worth
				// being sure of: the ray to the star runs from positionOS
				// toward lightDirOS, so it can only reach the sphere if the
				// point is already on the far side. Checking this against a
				// star in +X, planet radius 20: (-50, 0, 0) is behind and on
				// axis, shadowed; (-50, 30, 0) is 30 off axis, lit; (50, 0, 0)
				// is in front and on axis, lit. Guarding on `>=` here is what
				// keeps it that way -- the opposite guard shadows the sunlit
				// face and is not obviously wrong by eye.
				half axial = dot(positionOS, lightDirOS);
				if (axial >= 0.0)
				{
					return 1.0;
				}
				// Perpendicular distance from that axis, i.e. from the line
				// running through the planet's centre toward the star. Taken
				// as a difference of squares rather than length(P - L*axial)
				// so it stays non-negative under rounding.
				half perp = sqrt(max(dot(positionOS, positionOS) - axial * axial, 0.0));
				// The penumbra widens with distance behind the occluder,
				// because that is what a star's angular diameter does. A
				// constant softness would look cut out with scissors.
				// -axial is the distance behind the centre, hence the negate.
				half soft = _PlanetShadowSoftness * (-axial) + _PlanetRadius * 0.01;
				return 1.0 - smoothstep(_PlanetRadius, _PlanetRadius + soft, perp);
			}

			// Radial fade in V. V is 0 at the inner edge and 1 at the outer on
			// this mesh, verified rather than assumed, so the same four
			// numbers mean "where the band starts and stops" without carrying
			// the object's scale.
			half EdgeFade(float v)
			{
				half inner = saturate((v - _Edges.x) * rcp(max(1e-4, _Edges.y)));
				half outer = 1.0 - saturate((v - _Edges.z) * rcp(max(1e-4, _Edges.w)));
				return saturate(inner) * saturate(outer);
			}

			half4 frag(FragmentInput input) : SV_Target
			{
				Varyings v = input.interpolated;

				// ---- one sample each ----
				float2 uv = TRANSFORM_TEX(v.uv1, _DustAlbedo);
				half4 dustTex = SAMPLE_TEXTURE2D(_DustAlbedo, sampler_DustAlbedo, uv);
				half4 dustSpec = SAMPLE_TEXTURE2D(_DustSpecMap, sampler_DustSpecMap,
					TRANSFORM_TEX(v.uv1, _DustSpecMap));
				half3 dustN = UnpackNormal(SAMPLE_TEXTURE2D(_DustNormals,
					sampler_DustNormals, TRANSFORM_TEX(v.uv1, _DustNormals)));

				// ---- staged bisect ----
				// Returned BEFORE the lighting with alpha 1, so what is on
				// screen is the value under test and nothing else. Bounded at
				// BOTH ends: written as `> 0` alone, every stage matches, they
				// all look identical to stage 1, and the later ones become
				// unreachable and report nothing.
				if (_AlbedoDebug > 0.5 && _AlbedoDebug < 1.5)
					return half4(dustTex.rgb, 1.0);
				if (_AlbedoDebug > 1.5 && _AlbedoDebug < 2.5)
					return half4(v.uv1.x, v.uv1.y, 0.0, 1.0);
				if (_AlbedoDebug > 2.5)
					return half4(1.0, 0.0, 1.0, 1.0);   // magenta: reached the end

				// ---- normal ----
				// Flip the geometric normal on back faces, or the underside of
				// the ring is lit from behind and comes out black.
				half3 geoNormalWS = normalize(v.normalWS)
					* IS_FRONT_VFACE(input.facing, 1.0, -1.0);
				half3 tangentWS = normalize(v.tangentWS.xyz);
				half3 bitangentWS = cross(geoNormalWS, tangentWS) * v.tangentWS.w;
				half3 shadedNormalWS = normalize(TransformTangentToWorld(
					dustN * _DustNormalStrength,
					half3x3(tangentWS, bitangentWS, geoNormalWS)));

				SurfaceData surfaceData = (SurfaceData)0;
				surfaceData.albedo = dustTex.rgb * _DustColor.rgb;
				// Dust is a dielectric: constant zero metallic.
				surfaceData.metallic = 0.0;
				surfaceData.smoothness = saturate(
					_DustSmoothness + dustSpec.r * _DustDetailSmoothness);
				// The real normal travels in inputData.normalWS.
				// UniversalFragmentPBR never reads normalTS -- InitializeBRDFData
				// uses only albedo, metallic, specular, smoothness and alpha --
				// so this is left flat rather than carrying a tangent-space
				// value nothing consumes.
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

				// The zero-argument overload on purpose: it returns direction
				// and colour without touching the shadow map. The map cannot
				// reach this far anyway, so sampling it here would be a fetch
				// spent on a guaranteed 1.0.
				Light mainLight = GetMainLight();

				// ---- the volumetric part ----
				half3 lightDir = mainLight.direction;          // surface -> light
				half3 viewDir = inputData.viewDirectionWS;      // surface -> camera

				// Wrapped diffuse. Without it the terminator is a hard line
				// and the dust looks like a solid sheet, which is the opposite
				// of volumetric.
				half wrap = 1.0 / max(1e-3, 1.0 + _DustWrap);
				half ndl = dot(shadedNormalWS, lightDir);
				half diffuse = saturate((ndl + _DustWrap) * wrap);

				// Forward scattering through the sheet. cosTheta is between
				// the direction the light is travelling and the direction to
				// the camera, so it peaks when you are looking toward the star
				// through the dust -- which is exactly "the light hitting the
				// light side is visible on the underside too".
				half scatter = PhaseHG(saturate(dot(-lightDir, viewDir)), _ScatterG);

				// The limb term carries much of the volumetric read, because a
				// thin sheet is nearly edge-on over most of its area.
				half rim = pow(1.0 - saturate(dot(shadedNormalWS, viewDir)), _RimPower);

				surfaceData.emission = mainLight.color * diffuse
					* (_ScatterColor.rgb * (_ScatterStrength * scatter + _RimStrength * rim));

				half4 color = UniversalFragmentPBR(inputData, surfaceData);

				// ---- planet shadow ----
				// Applied to the lit result rather than through inputData:
				// shadowMask only feeds BakedShadow() for lightmaps, and with
				// no lightmaps in this project it is ignored entirely -- a hook
				// that silently does nothing. shadowCoord cannot be used
				// either, since the map does not reach here.
				if (_PlanetRadius > 0.0)
				{
					// doNormalize passed explicitly rather than left to the
					// default argument: URP declares the parameter but nothing
					// in its library calls the one-argument form, so relying on
					// default-argument support here would be the only place in
					// the project doing so. The cast to float3x3 inside drops
					// the translation, so this is a direction transform even
					// though the planet's centre is not at the object origin
					// in general -- on this mesh it is, by construction.
					half3 lightDirOS = TransformWorldToObjectDir(lightDir, true);
					half shadow = PlanetShadow(v.positionOS, lightDirOS);
					shadow = lerp(1.0, shadow, _ShadowStrength);
					// Scales both the direct term and the scatter above it,
					// which is right: dust in the planet's shadow receives no
					// direct star and should not glow with transmission.
					color.rgb *= shadow;
				}

				// Ambient, added after the shadow so the unlit side is not
				// simply black. URP's bakedGI is zero here, so with no
				// additional lights there is nothing else that would.
				color.rgb += _DustAmbient.rgb;

				// ---- coverage ----
				half alpha = pow(saturate(dustTex.a), _AlphaPower) * _AlphaScale;
				// A flat sheet seen edge-on is all edge-on; without this the
				// band ends in a wall rather than fading.
				alpha *= 1.0 - _AlphaGrazing
					* pow(1.0 - saturate(dot(geoNormalWS, viewDir)), 2.0);
				alpha *= lerp(1.0, EdgeFade(v.uv1.y), _EdgeFadeStrength);
				color.a = saturate(alpha);

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
