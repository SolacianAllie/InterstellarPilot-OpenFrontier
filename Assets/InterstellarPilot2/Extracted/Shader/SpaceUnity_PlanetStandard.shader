Shader "SpaceUnity/PlanetStandard" {
	Properties {
		_MainTex ("_MainTex", 2D) = "black" {}
		_Normals ("_Normals", 2D) = "black" {}
		_Lights ("_Lights", 2D) = "black" {}
		_SpecMap ("_SpecMap", 2D) = "black" {}
		_LightScale ("_LightScale", Float) = 1
		_Emission ("_Emission", 2D) = "black" {}
		_EmissionScale ("_EmissionScale", Float) = 1
		[Header(AerialPerspective)]
		// Deliberately the SAME property names as the atmosphere shell
		// (OpenFrontier/Planet Atmosphere): _AtmoColor and _SunRimColor.
		//
		// They used to be called _HazeColor/_HazeSunColor, which split one
		// concept across two shaders under two names. Setting "Atmosphere
		// Color" on the shell then moved only the outer rim and the wash over
		// the surface silently kept its own default, so there was no way to
		// colour the atmosphere as a whole.
		//
		// These still have to be given the same values on both materials.
		// A shader global cannot do it: several planets can be on screen at
		// once, each with its own air, so the colour has to live on the
		// material rather than being shared.
		_AtmoColor ("Atmosphere Color", Color) = (0.35,0.78,1,1)
		_SunRimColor ("Sun Rim Color", Color) = (1,0.62,0.30,1)
		_HazeStrength ("Haze Strength", Range(0, 1)) = 0.6
		_HazeSunStrength ("Haze Sun Strength", Range(0, 2)) = 0.8
		_HazeFalloff ("Haze Falloff", Range(0.5, 8)) = 2.5
		_HazeSunPower ("Haze Sun Power", Range(0.5, 6)) = 1.3
		[Header(SpecMap)]
		_WaterFloor ("Water Floor", Range(0, 1)) = 0.32
		_WaterRange ("Water Range", Range(0.02, 1)) = 0.2
		_WaterSmoothness ("Water Smoothness", Range(0, 1)) = 0.92
		_LandSmoothness ("Land Smoothness", Range(0, 1)) = 0.12
		_LandDetailSmoothness ("Land Detail Smoothness", Range(0, 1)) = 0.3
		[Header(Clouds)]
		_Clouds ("Clouds", 2D) = "black" {}
		_CloudShadows ("Cloud Shadows", 2D) = "black" {}
		_CloudCoverage ("Cloud Coverage", Range(0, 1)) = 0.5
		_CloudSharpness ("Cloud Sharpness", Range(0.5, 8)) = 1
		_CloudBrightness ("Cloud Brightness", Range(0, 3)) = 1
		_CloudParallax ("Cloud Parallax", Range(-0.1, 0.1)) = 0.008
		_CloudWrap ("Cloud Wrap Light", Range(0, 1)) = 0.7
		_CloudShadowStrength ("Cloud Shadow Strength", Range(0, 1)) = 0.5
		[Header(Twilight)]
		_TwilightColor ("Twilight Color", Color) = (1.0,0.62,0.30,1)
		_TwilightStrength ("Twilight Strength", Range(0, 1)) = 0.55
		_TwilightDim ("Twilight Dimming", Range(0, 1)) = 0.3
		_TwilightSharpness ("Twilight Sharpness", Range(0.01, 1)) = 0.13
		_TwilightHaze ("Twilight Haze", Range(0, 1)) = 0.1
	}
	SubShader {
		Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
		LOD 200

		Pass {
			Name "ForwardLit"
			Tags { "LightMode"="UniversalForward" }

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma multi_compile _ _ADDITIONAL_LIGHTS
			#pragma multi_compile _ _SHADOWS_SOFT

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			TEXTURE2D(_MainTex);    SAMPLER(sampler_MainTex);
			TEXTURE2D(_Normals);    SAMPLER(sampler_Normals);
			TEXTURE2D(_Lights);     SAMPLER(sampler_Lights);
			TEXTURE2D(_SpecMap);    SAMPLER(sampler_SpecMap);
			TEXTURE2D(_Clouds);     SAMPLER(sampler_Clouds);
			TEXTURE2D(_CloudShadows); SAMPLER(sampler_CloudShadows);
			TEXTURE2D(_Emission);   SAMPLER(sampler_Emission);

			CBUFFER_START(UnityPerMaterial)
				float4 _MainTex_ST;
				float _LightScale;
				float _EmissionScale;
				float4 _AtmoColor;
				float4 _SunRimColor;
				float _HazeStrength;
				float _HazeSunStrength;
				float _HazeFalloff;
				float _HazeSunPower;
				float _WaterFloor;
				float _WaterRange;
				float _WaterSmoothness;
				float _LandSmoothness;
				float _LandDetailSmoothness;
				float4 _Clouds_ST;
				float4 _CloudShadows_ST;
				float _CloudCoverage;
				float _CloudSharpness;
				float _CloudBrightness;
				float _CloudParallax;
				float _CloudWrap;
				float _CloudShadowStrength;
				float4 _TwilightColor;
				float _TwilightStrength;
				float _TwilightDim;
				float _TwilightSharpness;
				float _TwilightHaze;
			CBUFFER_END

			struct Attributes {
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 tangentOS : TANGENT;
				float2 uv : TEXCOORD0;
			};
			struct Varyings {
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 positionWS : TEXCOORD1;
				float3 normalWS : TEXCOORD2;
				float4 tangentWS : TEXCOORD3;
			};

			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;
				VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
				VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
				output.positionCS = posInputs.positionCS;
				output.positionWS = posInputs.positionWS;
				output.normalWS = normInputs.normalWS;
				output.tangentWS = float4(normInputs.tangentWS, input.tangentOS.w);
				output.uv = TRANSFORM_TEX(input.uv, _MainTex);
				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				half4 albedoTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
				half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_Normals, sampler_Normals, input.uv));
				half3 lightsTex = SAMPLE_TEXTURE2D(_Lights, sampler_Lights, input.uv).rgb;
				half3 emissionTex = SAMPLE_TEXTURE2D(_Emission, sampler_Emission, input.uv).rgb;

				float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
				float3 bitangentWS = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
				float3 normalWS = normalize(TransformTangentToWorld(normalTS,
					half3x3(input.tangentWS.xyz, bitangentWS, input.normalWS)));

				// Geometric normal. The haze and the clouds both need it and
				// the normal map must not touch either: the haze's day/night
				// gate is a property of the sphere, and the cloud deck is a
				// layer in the sky that terrain relief cannot tilt.
				float3 geoNormalWS = normalize(input.normalWS);

				InputData inputData = (InputData)0;
				inputData.positionWS = input.positionWS;
				inputData.normalWS = normalWS;
				inputData.viewDirectionWS = viewDirWS;
				inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
				inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

				Light mainLight = GetMainLight(inputData.shadowCoord);
				float ndl = dot(normalWS, mainLight.direction);

				// Twilight -- the light itself reddening, which is what a
				// terminator actually looks like and what was missing.
				//
				// Everything else here faded to black at the terminator
				// because mainLight.color is uniform across the planet. But
				// the sunlight reaching the terminator has crossed a very long
				// path through the atmosphere and arrives both heavily
				// reddened and much dimmer. That is why a sunset on the ground
				// is red: it is the LIGHT that is red, not a tint over the sky.
				//
				// Air mass along the sun's path is the secant/zine
				// approximation for a spherical atmosphere: 1 looking straight
				// down the sun's rays at the sub-solar point, and unbounded at
				// the horizon. rcp(cosZ) is all that costs, and it is exactly
				// the quantity that makes the band appear when you look side on.
				//
				// cosZ is taken on the geometric normal -- this is a property
				// of the sphere and the altitude of the sun over it, not of
				// whatever relief happens to be under the pixel.
				float cosZ = dot(geoNormalWS, mainLight.direction);
				float airMass = rcp(max(0.06, cosZ));
				float twilight = saturate((airMass - 1.0) * _TwilightSharpness);

				// Hold it right up to the terminator, then kill it over the
				// couple of degrees past, where the sun is gone and only the
				// night side's ambient is left to tint -- that should stay
				// blue, not go orange.
				//
				// The first attempt gated on saturate(cosZ * 6.0), which was
				// too tight: it peaked the twilight at zenith 80 and dropped
				// it to 0.10 by 89, i.e. the deepest red sat just BEFORE the
				// horizon instead of on it. Measured, the profile now holds
				// at full right through to the terminator.
				twilight *= saturate(cosZ * 12.0 + 0.6);

				// Multiplicative on purpose. A lerp toward the sunset colour
				// would BRIGHTEN the terminator; real twilight darkens and
				// reddens at the same time, so it has to scale the light.
				// _SunRimColor is reused rather than adding a second colour --
				// it is already "the colour of the sun in the atmosphere" and
				// the haze uses it, so the sky and the light stay coherent.
				//
				// Strength and dimming are SEPARATE. Driving both from the
				// same twilight meant the core of the band hit the full
				// sunset hue AND 45% brightness, which read as orange paint
				// rather than as warm light.
				//
				// _TwilightColor is its OWN colour, not _SunRimColor. That
				// reuse was a mistake and it showed: _SunRimColor is authored
				// as an ABSOLUTE colour for the sky, and using it as a
				// multiplier means any saturated value silently becomes a
				// channel kill. Planet-Molten-Volcanica has it at (1, 0, 0),
				// which turned the band into (1.000, 0.450, 0.450) -- green and
				// blue both halved, i.e. magenta, which is the pink band in
				// the screenshot rather than a reddened terminator.
				//
				// _TwilightColor is authored as a MULTIPLIER, not an absolute
				// colour: 1.0 passes a channel untouched, 0.3 passes 30% of
				// it. So (1.0, 0.62, 0.30) means "red full, green at 62%,
				// blue at 30%", which is what a reddened sun actually is.
				// Normalising it to unit luminance was tried and reverted --
				// it pushed R/B from 1.63 to 3.33, i.e. more saturated, the
				// opposite of what this band is for. Darkening is the
				// separate job of _TwilightDim.
				half3 sunTint = lerp(half3(1, 1, 1), _TwilightColor.rgb,
				                    saturate(twilight * _TwilightStrength))
				              * lerp(1.0, _TwilightDim, twilight);
				half3 sunColour = mainLight.color * sunTint;

				// Two independent emitters, and they are deliberately NOT the same:
//
//   _Lights   - city lights. Only visible on the night side, so they
//               read as settlements lit by their own power rather than
//               as part of the lit surface.
//   _Emission - anything glowing on its own terms (lava, magma, glowing
//               crystal, running lights). Emits all the way round,
//               because it does not come from the sun. Keep this one
//               masked by hand if a feature should only show on the day
//               side - baking a small negative into the albedo is not a
//               substitute, and doing the mask here keeps it free.
//
// Both are plain maps rather than HDR keywords: at planet scale the
// glow has to survive being minified into a few pixels, and a keyword
// swap cannot be authored per feature.
float nightMask = saturate(-ndl * 2.0);
half3 emission = lightsTex * _LightScale * nightMask
			   + emissionTex * _EmissionScale;

				// Sampled before the SurfaceData so the spec block can read it.
				// Defaults to black, and a black spec map gives waterMask 0 and
				// landMask 0 -- matte terrain, which is the safe reading of a
				// missing map rather than a fully mirrored planet.
				half4 specTex = SAMPLE_TEXTURE2D(_SpecMap, sampler_SpecMap, input.uv);

				// Cloud layer. The art convention in *_clouds_diffuse.png is
				// near-white RGB with the COVERAGE IN ALPHA, and the matching
				// *_clouds_shadow_diffuse.png has RGB = 0 with everything in
				// alpha too -- so both masks are read from .a, not .rgb.
				//
				// Parallax: the deck sits above the surface, so it slides
				// against the terrain as the camera moves. The offset is taken
				// in the surface tangent frame, which is the frame the sphere
				// UVs live in, so it stays consistent as the planet rotates.
				float3 viewTangent = float3(dot(viewDirWS, input.tangentWS.xyz),
				                             dot(viewDirWS, bitangentWS), 0.0);
				float2 cloudUV = input.uv + viewTangent.xy * _CloudParallax;
				half4 cloudTex = SAMPLE_TEXTURE2D(_Clouds, sampler_Clouds,
				                                  TRANSFORM_TEX(cloudUV, _Clouds));
				half4 shadowTex = SAMPLE_TEXTURE2D(_CloudShadows, sampler_CloudShadows,
				                                   TRANSFORM_TEX(cloudUV, _CloudShadows));

				// _CloudCoverage is the midpoint and _CloudSharpness the edge.
				// Default 0.5 / 1.0 is a straight pass through of the authored
				// alpha; raising sharpness gives the hard-edged storm look.
				float cloudCov = saturate((cloudTex.a - _CloudCoverage + 0.5) * _CloudSharpness);

				SurfaceData surfaceData = (SurfaceData)0;
				surfaceData.albedo = albedoTex.rgb;
				surfaceData.metallic = 0.0;

				// Spec map convention, measured off the maps rather than
				// guessed. Checked on alpine_02, ocean_05, desert_04 and
				// snow_01, which all agree, and cross-checked against each
				// map's own diffuse so land and water could be separated:
				//
				//   G = WATER.  Mean 0.47 over lakes, 0.21 over land, and it
				//       binarises the lake mask at 3.5% mismatch. Tops out
				//       near 0.50-0.55 rather than 1.0, hence the remap.
				//
				//   A = a near-duplicate of G.  Correlation 0.9937, identical
				//       binarisation quality, mean difference +0.004. It adds
				//       nothing and is not read. It never was.
				//
				//   R = LAND / TERRAIN.  Mean 0.32 over land, 0.08 over
				//       water, and bimodal: zero in the valleys and ridge
				//       lines, high on the plateaus. A terrain variation
				//       mask, NOT snow or ice as an earlier note here
				//       claimed.
				//
				//   B = 0 in every map.
				//
				// These must be imported as LINEAR. They are data, not
				// colour, and the whole set defaulted to sRGB, which left
				// the GPU seeing water at 0.21 and land at 0.05 instead of
				// 0.50 and 0.24 -- putting both below _WaterFloor and
				// rendering the ocean matte. All 162 are now Linear.
				float waterMask = saturate((specTex.g - _WaterFloor) * rcp(max(1e-3, _WaterRange)));
				float landMask  = saturate(specTex.r);

				// Smoothness is what actually drives the look, for the reason
				// in the F0 note below.
				//
				// Measured, the old formula (R*0.6+0.15) gave:
				//     ocean 0.15 (matte)   rock 0.15   snow 0.70 (a mirror)
				// i.e. exactly backwards, and 0.70 on terrain is polished
				// chrome. That is the hard white blobs in the screenshot.
				//
				// Correctly read it gives:
				//     ocean 0.86 (mirror)  rock 0.12 (matte)  snow 0.39
				float landSmoothness = _LandSmoothness + landMask * _LandDetailSmoothness;
				surfaceData.smoothness = lerp(landSmoothness, _WaterSmoothness, waterMask);

				// F0 -- normal-incidence reflectance.
				//
				// NOTE: surfaceData.specular is NOT assigned, and that is
				// deliberate. URP only reads it when the _SPECULAR_SETUP
				// variant is enabled; this shader never enables it, so
				// InitializeBRDFData takes the #else branch and hardcodes
				//     brdfSpecular = lerp(kDielectricSpec, albedo, metallic)
				// with metallic = 0, i.e. a flat F0 of 0.04 for every texel.
				// Assigning specular here did nothing at all for as long as
				// that variant was off -- it was a line that looked like it
				// controlled reflectance and controlled nothing.
				//
				// 0.04 is also the right answer physically: terrain is
				// dielectric, and the sea reads bright toward the limb
				// because of the grazing-angle Fresnel rise, which URP
				// already applies through grazingTerm. Raising F0 over the
				// water on top of that is what turns the ocean into a mirror.
				// If per-texel F0 is ever genuinely wanted it needs
				// #pragma multi_compile _ _SPECULAR_SETUP and the keyword set
				// on the material, which also changes how diffuse is derived.
				surfaceData.normalTS = normalTS;
				surfaceData.emission = emission;
				surfaceData.occlusion = 1.0;
				surfaceData.alpha = 1.0;
				surfaceData.clearCoatMask = 0.0;
				surfaceData.clearCoatSmoothness = 0.0;

				half4 color = UniversalFragmentPBR(inputData, surfaceData);

				// Cloud shadows.
				//
				// These have to dim the LIGHTING and nothing else, so that city
				// lights and magma keep burning under a cloud. URP's
				// CalculateLightingColor is
				//
				//     lightingColor = (mainLight + additionalLights + GI) * albedo
				//                     + emissionColor
				//
				// so emission is exactly additive and separable, and the lit
				// term is never negative. Subtracting the emission we already
				// know and putting it straight back is therefore exact rather
				// than a clamp.
				//
				// Note this cannot be done by scaling mainLight.color: the
				// local Light that UniversalFragmentPBR receives is never
				// passed in, it calls GetMainLight itself. Scaling surfaceData
				// .albedo instead would work but would also dim the ambient and
				// IBL, which a cloud deck only partly blocks.
				half3 emissionTerm = surfaceData.emission;
				// Twilight rides on the lit term, so it never touches the
				// emission: magma and city lights keep burning through the
				// band instead of being dimmed with everything else.
				half3 litTerm = (color.rgb - emissionTerm) * sunTint;
				float cloudShadow = 1.0 - saturate(shadowTex.a) * _CloudShadowStrength;
				color.rgb = emissionTerm + max(litTerm, 0.0) * cloudShadow;

				// The deck itself. Clouds are a high-albedo layer sitting in the
				// sky, so they take the sun's colour and are lit with a wrapped
				// terminator -- they scatter forward and stay visible a little
				// past where the ground has gone dark, which is most of what
				// sells them as being above it rather than painted on.
				float cloudLight = saturate(dot(geoNormalWS, mainLight.direction) * _CloudWrap
				                           + (1.0 - _CloudWrap));
				half3 cloudColour = cloudTex.rgb * (sunColour * _CloudBrightness * cloudLight);
				color.rgb = lerp(color.rgb, cloudColour, cloudCov);

				// Aerial perspective -- the other half of the atmosphere.
				//
				// The shell only ever draws OUTSIDE the silhouette, so the air
				// stops dead at the planet's edge. That hard boundary is why the
				// atmosphere reads as a sticker rather than as air sitting on top
				// of the world. This is the inside half: looking at the surface
				// near the limb, the view ray crosses a lot of atmosphere before
				// it lands, and that air scatters light back over the terrain.
				//
				// Airmass through an exponential atmosphere goes as 1/cos of the
				// angle between the view ray and the surface normal -- 1 looking
				// straight down, unbounded at the limb. That is what concentrates
				// the wash at the horizon instead of laying a flat tint over the
				// whole disc.
				//
				// Note this is NOT the fresnel rim that used to live here. That
				// one was emissive and drew a glowing edge, which is why it moved
				// onto the shell. This is in-scattering over the terrain: a lerp
				// toward a tint, so the surface stays visible underneath.
				//
				// EVERYTHING below keys off the GEOMETRIC normal, not the
				// normal-mapped one. input.normalWS is the interpolated vertex
				// normal of the sphere; normalWS has the _Normals map folded
				// into it. That distinction is the whole point of this block:
				//
				//  - the terminator is a property of the SPHERE. With the
				//    normal-mapped ndl, any bump on the night side whose
				//    perturbed normal tilts toward the sun reads as lit, so
				//    haze appeared on the dark side of the planet.
				//  - airmass is a property of the VIEW, not of the terrain.
				//    Feeding the normal map into mu made the wash follow
				//    every ridge and crater, so the limb read as a blotchy
				//    band instead of the clean arc in the reference.
				float geoNdL = dot(geoNormalWS, mainLight.direction);

				float mu = saturate(dot(geoNormalWS, viewDirWS));  // 1 face-on, 0 at the limb
				float grazing = pow(saturate(1.0 - mu), _HazeFalloff);

				// Lit or not, judged on the sphere's own terminator so the
				// haze cannot leak onto the night side.
				float hazeLit = smoothstep(0.0, 0.35, geoNdL);
				// ...but the twilight band needs the old wide window, because
				// it sits ON the terminator rather than on the daylit side of
				// it. Same split as the atmosphere shell's dayAir / dayArc:
				// aerial perspective stops at the terminator, the sunset glow
				// straddles it. The clouds keep wrapping regardless -- that is
				// _CloudWrap, a separate term, and it is the one that is
				// supposed to reach past the terminator.
				float twilightLit = smoothstep(-0.15, 0.25, geoNdL);

				// The sunrise arc washes the surface too, not just the shell, and it
				// has to be driven the SAME way the shell drives it or the two
				// halves will disagree about where the sunset is.
				//
				// Keyed on the geometric ndl: the sun being low at this point
				// is what makes its light arrive reddened. The global dot(V, -L)
				// that used to drive this is nearly constant around the visible
				// limb, so it could not produce a gradient along the rim at all
				// -- it could only switch the whole limb warm or not.
				float lowSun = pow(saturate(1.0 - geoNdL), _HazeSunPower);
				// and only when the sun is low for US, not just somewhere on
				// the planet; otherwise a sun directly behind the camera puts
				// the terminator exactly on the visible limb and the wash
				// covers everything.
				float lowForUs = saturate(0.55 - dot(mainLight.direction, viewDirWS) * 1.5);
				float hazeSun = saturate(lowSun * lowForUs);

				half3 hazeTint = lerp(_AtmoColor.rgb, _SunRimColor.rgb, saturate(hazeSun));
				// The band term as well as the limb term. hazeTint is already
				// the sunset colour wherever the sun is low, so letting a slice
				// of it in across the terminator is what makes the blue visibly
				// bend to red instead of only doing it at the silhouette.
				float hazeAmt = saturate(grazing * hazeLit
					* (_HazeStrength + _HazeSunStrength * hazeSun)
					+ twilight * _TwilightHaze * twilightLit);
				color.rgb = lerp(color.rgb, hazeTint, hazeAmt);

				color.rgb = MixFog(color.rgb, inputData.fogCoord);
				return color;
			}
			ENDHLSL
		}
	}
	FallBack "Universal Render Pipeline/Lit"
}
