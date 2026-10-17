Shader "OpenFrontier/PlanetGasGiant"
{
	// A gas giant: banded albedo with differential band drift, and the same
	// atmosphere the rocky planet shader has.
	//
	// WHY A SEPARATE SHADER RATHER THAN PlanetStandard WITH A FLAG
	//
	// PlanetStandard spends most of its per-pixel budget on things a gas giant
	// does not have: a spec map split into land and water masks, city lights
	// with a growth threshold, a land-detail normal, an emission map, and a
	// separate cloud deck with its own normals, specular and projected
	// shadows. A gas giant has none of those. A flag would leave every one of
	// them sampled, branched around and paid for.
	//
	// WHAT THIS SHADER IS
	//
	// A banded albedo and the atmosphere. That is the whole of it: one texture
	// sample for _MainTex, UniversalFragmentPBR over it, and the aerial
	// perspective and twilight block below.
	//
	// It was more than that for a while. The gas giants ship a *_normal.png
	// that is not a normal map -- red is one value per latitude band, 96.7% of
	// its variance is between rows, and it straddles zero when remapped, which
	// is differential rotation -- and green is a height field. Both were built
	// out: the bands drifted, and green became a tangent-space normal.
	//
	// The drift is gone because it could not be rendered, not because the data
	// was wrong. R holds one number per band, so applying it means translating
	// the albedo by a STEP function of latitude, and the two halves of that
	// cannot both be had: the sample point moves far faster across the screen
	// than the surface does, so it aliases; and blurring the field to stop that
	// reconstructs a coarse grid, whose cells each shear the texture differently.
	// The aliasing rate is the visible motion rate -- they are the same number,
	// and no LOD, speed or amplitude separates them, because a planet this
	// close never minifies and so never wanted the blur in the first place.
	//
	// Recorded here because the analysis was expensive and the conclusions are
	// not recoverable from the stripped code: if band motion is wanted later,
	// the data supports it and the technique does not.
	//
	// ATMOSPHERE
	//
	// The aerial-perspective and twilight block is carried across from
	// PlanetStandard essentially intact, and the property NAMES are identical
	// on purpose: the atmosphere shell (OpenFrontier/PlanetAtmosphere) reads
	// _AtmoColor and _SunRimColor from the same material, so the inside and
	// outside halves of the atmosphere cannot disagree about where the sunset
	// is. Renaming anything here would split that in half again.
	//
	// There is no cloud deck. On a gas giant the bands ARE the cloud; adding a
	// separate deck above them would double the atmosphere and put the storms
	// in the wrong place.

	Properties
	{
		_MainTex ("Banded Albedo", 2D) = "black" {}

		// Dielectric roughness for the PBR pass. This is the one part of the
		// old band machinery that was never about the bands.
		_BandSmoothness ("Band Smoothness", Range(0, 1)) = 0.25

		// ---- atmosphere, names fixed by PlanetStandard ----
		[Header(AerialPerspective)]
		_AtmoColor ("Atmosphere Color", Color) = (0.35, 0.78, 1, 1)
		_SunRimColor ("Sun Rim Color", Color) = (1, 0.62, 0.30, 1)
		_SunRimDeep ("Sun Rim Deep Color", Color) = (1, 0.30, 0.12, 1)
		_HazeStrength ("Haze Strength", Range(0, 1)) = 0.6
		_HazeSunStrength ("Haze Sun Strength", Range(0, 2)) = 0.8
		_HazeFalloff ("Haze Falloff", Range(0.5, 8)) = 2.5
		_HazeSunPower ("Haze Sun Power", Range(0.5, 6)) = 1.3
		_HazeSunAngle ("Haze Sun Angle", Range(0, 180)) = 45
		_HazeSunSpread ("Haze Sun Spread", Range(1, 180)) = 35

		[Header(Twilight)]
		_TwilightColor ("Twilight Color", Color) = (1.0, 0.62, 0.30, 1)
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

			ZWrite On
			ZTest LEqual
			Cull Back

			HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			// Deliberately NO multi_compile_fog and NO MixFog. The bound URP
			// asset carries no fog settings and no shader in this project ever
			// assigns inputData.fogCoord, so PlanetStandard's MixFog reads a
			// zeroed field and does nothing. Copying it would pay for a fog
			// variant on every planet pixel to reproduce dead code.

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			TEXTURE2D(_MainTex);   SAMPLER(sampler_MainTex);

			CBUFFER_START(UnityPerMaterial)
				float4 _MainTex_ST;

				float4 _AtmoColor;
				float4 _SunRimColor;
				float4 _SunRimDeep;
				float4 _TwilightColor;
				float _BandSmoothness;
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
			CBUFFER_END

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 tangentOS : TANGENT;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 positionWS : TEXCOORD1;
				half3 normalWS : TEXCOORD2;
			};

			// SV_IsFrontFace is INPUT only; declaring it in Varyings, which
			// vert() writes, is an illegal output semantic on Vulkan.
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
				output.normalWS = normInputs.normalWS;
				output.uv = input.uv;
				return output;
			}



			half4 frag(FragmentInput input) : SV_Target
			{
				Varyings v = input.interpolated;

				float2 uvT = TRANSFORM_TEX(v.uv, _MainTex);

				half4 albedoTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvT);


				// Two normals, deliberately.
				//
				// geoNormalWS is the sphere's own and stays geometric. The
				// atmosphere below keys off it regardless of relief, because
				// feeding a perturbed one in is what used to make haze appear on
				// the night side and made the limb blotchy -- the terminator has
				// to be where the GEOMETRY puts it or the sunset lands in the
				// wrong place.
				//
				// shadingNormalWS adds the relief built from G, and only that one
				// goes to the lighting. This is the first normal-mapped lighting in
				// this shader, so it is worth knowing it is here.
				half3 geoNormalWS = normalize(v.normalWS);
				half3 viewDirWS = GetWorldSpaceNormalizeViewDir(v.positionWS);

				InputData inputData = (InputData)0;
				inputData.positionWS = v.positionWS;
				inputData.normalWS = geoNormalWS;
				inputData.viewDirectionWS = viewDirWS;
				inputData.shadowCoord = TransformWorldToShadowCoord(v.positionWS);
				inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(v.positionCS);
				inputData.shadowMask = half4(1, 1, 1, 1);

				Light mainLight = GetMainLight();

				float geoNdL = dot(geoNormalWS, mainLight.direction);
				float cosZ = geoNdL;
				// Airmass through an exponential atmosphere goes as 1/cos of the
				// angle to the normal: 1 looking straight down, unbounded at the
				// limb. The 0.06 floor keeps it finite there.
				float airMass = rcp(max(0.06, cosZ));
				float twilight = saturate((airMass - 1.0) * _TwilightSharpness);
				// How far PAST the terminator the twilight band reaches. Without
				// this the band wraps right around the night side, which is not
				// twilight, it is daylight.
				twilight *= 1.0 - saturate(-cosZ * rcp(max(1e-3, _TwilightDepth)));
				half3 sunTint = lerp(half3(1, 1, 1), _TwilightColor.rgb,
					saturate(twilight * _TwilightStrength))
				              * lerp(1.0, _TwilightDim, twilight);
				half3 sunColour = mainLight.color * sunTint;

				SurfaceData surfaceData = (SurfaceData)0;
				surfaceData.albedo = albedoTex.rgb;
				surfaceData.metallic = 0.0;
				surfaceData.smoothness = _BandSmoothness;
				// UniversalFragmentPBR never reads normalTS -- InitializeBRDFData
				// takes only albedo, metallic, specular, smoothness and alpha --
				// and there is no normal map here anyway.
				surfaceData.normalTS = half3(0, 0, 1);
				surfaceData.occlusion = 1.0;
				surfaceData.alpha = 1.0;
				surfaceData.clearCoatMask = 0.0;
				surfaceData.clearCoatSmoothness = 0.0;

				half4 color = UniversalFragmentPBR(inputData, surfaceData);

				// Twilight rides on the lit term, which is exactly why it is
				// applied here rather than by tinting mainLight: the light
				// URP hands UniversalFragmentPBR is fetched inside it and never
				// passed in, so the only separable place is the result.
				color.rgb *= sunTint;

				// ---- aerial perspective ----
				// The atmosphere shell only ever draws OUTSIDE the silhouette,
				// so the air stops dead at the planet's edge and the atmosphere
				// reads as a sticker. This is the inside half: the view ray
				// crosses a lot of air near the limb and that air scatters
				// light back over the surface. A lerp toward a tint, so the
				// bands stay visible underneath.
				float mu = saturate(dot(geoNormalWS, viewDirWS));  // 1 face-on, 0 at the limb
				float grazing = pow(saturate(1.0 - mu), _HazeFalloff);

				// Judged on the sphere's own terminator, so haze cannot leak
				// onto the night side.
				float hazeLit = smoothstep(0.0, 0.35, geoNdL);
				// ...but the twilight band sits ON the terminator rather than on
				// the daylit side of it, so it gets a window that straddles it.
				float twilightLit = smoothstep(-0.15, 0.10, geoNdL);

				float lowSun = pow(saturate(1.0 - geoNdL), _HazeSunPower);
				// Angle between the sun and the view ray in degrees: 0 is
				// looking straight into the sun, 90 is the sun square across the
				// view, 180 is the night side. One acos per pixel, which is
				// noise beside UniversalFragmentPBR's few hundred instructions.
				float sunAngle = degrees(acos(saturate(dot(mainLight.direction, viewDirWS))));
				float lowForUs = saturate((sunAngle - _HazeSunAngle)
				                         * rcp(max(1.0, _HazeSunSpread)));
				float hazeSun = saturate(lowSun * lowForUs);

				// TWO stages, not one. A single lerp from the air's cyan to the
				// rim's orange passes through grey, and the twilight zone then
				// cannot look orange however far _TwilightHaze is pushed,
				// because it would be lerping toward a colour with no chroma
				// left in it. Routing through the deep sunset colour keeps the
				// chroma up at every step. Squared on the second stage, since
				// the deep end is only reached right at the terminator.
				half3 hazeTint = lerp(lerp(_AtmoColor.rgb, _SunRimColor.rgb, saturate(hazeSun)),
				                      _SunRimDeep.rgb, saturate(hazeSun * hazeSun));
				// The band term as well as the limb term, so the blue visibly
				// bends to red across the terminator instead of only at the
				// silhouette.
				float hazeAmt = saturate(grazing * hazeLit
					* (_HazeStrength + _HazeSunStrength * hazeSun)
					+ twilight * _TwilightHaze * twilightLit);
				color.rgb = lerp(color.rgb, hazeTint, hazeAmt);

				return color;
			}
			ENDHLSL
		}
	}
}
