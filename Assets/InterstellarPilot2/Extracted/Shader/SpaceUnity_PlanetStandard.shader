Shader "SpaceUnity/PlanetStandard" {
	Properties {
		_MainTex ("_MainTex", 2D) = "black" {}
		_Normals ("_Normals", 2D) = "black" {}
		_SpecMap ("_SpecMap", 2D) = "black" {}
		// City lights: an orthographic night-lights plate. Visible on the night
		// side only, and off the water.
		_Lights ("_Lights", 2D) = "black" {}
		_LightScale ("_LightScale", Float) = 1
		_Emission ("_Emission", 2D) = "black" {}
		_EmissionScale ("_EmissionScale", Float) = 1
		// _AlbedoDebug returns the RAW _MainTex sample, untouched and before any
		// lighting, cloud, haze or fog. One number settles "is the colour missing
		// in the texture, or is the shader taking it away":
		//   0  normal render
		//   1  the raw albedo texture, full stop
		//   2  after UniversalFragmentPBR, before the cloud deck
		//   3  after the cloud deck, before the aerial-perspective lerp
		//   4  the finished pixel
		//
		// The raw plate was confirmed good, so this walks the pipeline forward
		// one stage at a time and the first stage that loses the colour names
		// the term that took it.
		//
		// Delete this property and the one-line branch in frag() when the cause
		// is known. Nothing else depends on it.
		_AlbedoDebug ("DEBUG Surface Bisect", Range(0, 4)) = 0
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
		_SunRimDeep ("Sun Rim Deep Color", Color) = (1,0.30,0.12,1)
		_HazeStrength ("Haze Strength", Range(0, 1)) = 0.6
		_HazeSunStrength ("Haze Sun Strength", Range(0, 2)) = 0.8
		_HazeFalloff ("Haze Falloff", Range(0.5, 8)) = 2.5
		_HazeSunPower ("Haze Sun Power", Range(0.5, 6)) = 1.3
		// How warm the twilight zone gets at the SIDE-ON viewing angle, where the
		// sun is perpendicular to the view and this term peaks. Was a hardcoded
		// 0.55, which capped hazeSun so low the tint came out neutral.
		_HazeSunView ("Haze Sun Side-On", Range(0, 1)) = 0.85
		[Header(SpecMap)]
		_WaterFloor ("Water Floor", Range(0, 1)) = 0.32
		_WaterRange ("Water Range", Range(0.02, 1)) = 0.2
		// Where CITY LIGHTS may appear. A second gate on the same spec map, on
		// the G channel, with its OWN threshold.
		//
		// Not _WaterFloor, because that gate does not close. Measured on
		// alpine_02_specular.png against night_lights.png, as the share of
		// city-light energy surviving over water (spec G > 0.4):
		//
		//   _WaterFloor 0.32 / _WaterRange 0.20   0.1110  <- 11% still leaks
		//   _CityLightFloor 0.25 / _CityLightRange 0.10  0.0000  <- none
		//
		// The reason is the arithmetic: waterMask reaches 1.0 only at spec G 0.52,
		// and the water in this plate tops out at 0.518 -- so the ocean mask never
		// fully closes and a strip of lights always survives on it.
		//
		// G rather than A on purpose. They are the same channel at correlation
		// 0.9937, but this plate's Standalone and Android imports are both at
		// textureCompression 1, and DXT1/BC1 stores no alpha, so .a reads back
		// as 1.0 everywhere. G cannot be stripped that way, and it is the channel
		// waterMask already uses, so there is one water channel, not two.
		_CityLightFloor ("City Light Floor", Range(0, 1)) = 0.25
		_CityLightRange ("City Light Range", Range(0.02, 1)) = 0.1
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
		// Normal and specular maps for the deck itself. Both sets already exist
		// on disk and were never wired up. The normals are real data; the
		// specular maps are FLAT -- every pixel identical, measured std 0.000
		// with G = 0.110 on all nine -- so they give a uniform sheen and no
		// variation until real ones are authored. Read as data, not colour.
		_CloudNormals ("Cloud Normals", 2D) = "bump" {}
		_CloudSpecular ("Cloud Specular", 2D) = "black" {}
		_CloudNormalStrength ("Cloud Normal Strength", Range(0, 3)) = 1
		_CloudSmoothness ("Cloud Smoothness", Range(0, 2)) = 1
		// Gain, applied BEFORE the strength. Capped at 1 on purpose: above 1 it
		// stops being sharpness and starts inventing shadow the map does not
		// describe -- it overdrives a texel the artist left at 0.3 into 0.75.
		// At 1 the map says what it means, and strength alone decides how dark
		// that reads.
		_CloudShadowSharpness ("Cloud Shadow Sharpness", Range(0.25, 1)) = 1
		// Depth, and deliberately overdrivable to 2. Above 1 the densest
		// authored shadows saturate to solid black rather than going negative
		// -- see the saturate in the frag.
		_CloudShadowStrength ("Cloud Shadow Strength", Range(0, 2)) = 1.5
		// How much of the EMISSION survives the cloud deck above it. 1 is fully
		// clear, 0 is fully blocked.
		//
		// This is a separate dial from _CloudShadowStrength on purpose. That one
		// is applied to the LIT term only, so that the terrain goes dark under a
		// cloud while city lights and magma keep burning through it -- right for
		// the surface being shadowed, and wrong for the emission, which sits
		// UNDER the deck and has to pass through it.
		//
		// At zero coverage this is exactly 1.0, so a planet with no cloud is
		// untouched by this term.
		//
		// It is SQUARED, which is what makes it visible. Straight lerp at 0.3 was
		// barely readable: under cloud the eye sees a mix of the dimmed lights and
		// the deck's own bright colour, so dimming the lights moves that mix
		// toward something already bright and the change compresses.
		//
		// Squaring puts the extra bite where coverage is highest, which is exactly
		// where the eye is looking for it:
		//
		//   1.0 clear      1.000    nothing under any cloud
		//   0.5 half       0.500    half covered is half as bright
		//   0.5 full       0.250    fully covered is a quarter -- a 4x cut
		//
		// At 0.3 squared it is 0.09 at full cloud, which is too much: the lights
		// vanish under the deck and the map reads as having no cities at all in
		// the overcast regions.
		_CloudEmissionThrough ("Cloud Emission Through", Range(0, 1)) = 0.5
		// Pan and drift are a UV offset added before the parallax term, so all
		// four cloud maps move together. Drift is UV per second off _Time.
		// Cloud altitude in PLANET RADII, for projecting the shadow. A cloud at
		// height H casts its shadow H/tan(elevation) away from the sun. Measured
		// across the shipped maps, this is the one thing the art does NOT contain:
		// correlating each shadow alpha against its own cloud alpha gives a best
		// offset of exactly (0,0) at correlation 1.000 on 11 of 13 comparable
		// pairs, so the shadow maps are undisplaced copies of the clouds. They
		// are not pre-offset, so supplying the offset is not double-counting.
		// Earth-like H/R is 0.0016, which at a 30 degree sun is under one texel
		// of a 2048 map -- real, and invisible. It only reads on small bodies
		// with a proportionally thick atmosphere, or at a very low sun.
		_CloudShadowHeight ("Cloud Shadow Height", Range(0, 0.05)) = 0.002
		_CloudPan ("Cloud Pan", Vector) = (0,0,0,0)
		_CloudDrift ("Cloud Drift", Vector) = (0,0,0,0)
		[Header(Twilight)]
		_TwilightColor ("Twilight Color", Color) = (1.0,0.62,0.30,1)
		_TwilightStrength ("Twilight Strength", Range(0, 1)) = 0.55
		_TwilightDim ("Twilight Dimming", Range(0, 1)) = 0.3
		_TwilightSharpness ("Twilight Sharpness", Range(0.01, 1)) = 0.13
		// How far PAST the terminator the twilight band reaches. 0 is the old
		// behaviour -- the band stopped dead on the terminator, which is why it
		// read as a stripe painted on the day side rather than as light in air.
		//
		// In cosZ units, so it can be read straight off the sun's elevation:
		//
		//   0.05   2.9 deg past   the old gate, which is what that did anyway
		//   0.105  6.0 deg past   the end of civil twilight
		//   0.25  14.5 deg past   the end of nautical twilight   <- default
		//   0.31  18.0 deg past   the end of astronomical twilight
		//   1.0   90 deg past     the whole night side, which is not a twilight
		//          -- it is a sunset that never finishes
		//
		// Note the band is ALREADY 90 degrees wide on the day side, from the
		// sub-solar point down to where the sun-path air mass passes 7 (cosZ
		// 0.115, sun 6.6 degrees up). That half is not a choice and should not
		// be touched: the air really does have to cross seven atmospheres by
		// then. Only the night side is a dial.
		_TwilightDepth ("Twilight Night Depth", Range(0.01, 1)) = 0.25
		// 0.6, not 0.3. Measured at the terminator band peak (geoNdL 0.10,
		// where twilight is 1.0) the haze amount is exactly this value, and at
		// 0.3 the band was lerping 20 percent of a surface that had already
		// been dimmed to 70 -- which is what made it read as dark grey rather
		// than orange. The colour was never the problem; the tint at that
		// point is already (0.98, 0.38, 0.18).
		_TwilightHaze ("Twilight Haze", Range(0, 1)) = 0.6
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
			
			TEXTURE2D(_SpecMap);    SAMPLER(sampler_SpecMap);
			TEXTURE2D(_Lights);     SAMPLER(sampler_Lights);
			TEXTURE2D(_Clouds);     SAMPLER(sampler_Clouds);
			TEXTURE2D(_CloudShadows); SAMPLER(sampler_CloudShadows);
			TEXTURE2D(_CloudNormals); SAMPLER(sampler_CloudNormals);
			TEXTURE2D(_CloudSpecular); SAMPLER(sampler_CloudSpecular);
			TEXTURE2D(_Emission);   SAMPLER(sampler_Emission);

			CBUFFER_START(UnityPerMaterial)
				float4 _MainTex_ST;
				float _LightScale;
				float _EmissionScale;
				float _AlbedoDebug;
				float4 _AtmoColor;
				float4 _SunRimColor;
				float4 _SunRimDeep;
				float _HazeStrength;
				float _HazeSunStrength;
				float _HazeFalloff;
				float _HazeSunPower;
				float _HazeSunView;
				float _WaterFloor;
				float _WaterRange;
				float _CityLightFloor;
				float _CityLightRange;
				float _WaterSmoothness;
				float _LandSmoothness;
				float _LandDetailSmoothness;
				float _CloudCoverage;
				float _CloudSharpness;
				float _CloudBrightness;
				float _CloudParallax;
				float _CloudWrap;
				float _CloudNormalStrength;
				float _CloudSmoothness;
				float _CloudShadowSharpness;
				float _CloudShadowStrength;
				float _CloudEmissionThrough;
				float _CloudShadowHeight;
				float4 _CloudPan;
				float4 _CloudDrift;
				float4 _TwilightColor;
				float _TwilightStrength;
				float _TwilightDim;
				float _TwilightSharpness;
				float _TwilightDepth;
				float _TwilightHaze;
				float4 _Clouds_ST;
				float4 _CloudShadows_ST;
				float4 _CloudNormals_ST;
				float4 _CloudSpecular_ST;
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

				// Returned here, before the lighting, so what is on screen is the
				// texture and nothing else. Grey here means the plate or its
				// import; coloured here and grey in the render means the shader
				// is taking the colour away.
				// Bounded at BOTH ends, and the upper bound is the point: this used
				// to be written as > 0.5 alone, which matched 2, 3 and 4 as well,
				// so every stage of the bisect below returned the raw albedo and
				// they all looked identical to stage 1. The later branches were
				// unreachable and reported nothing at all.
				if (_AlbedoDebug > 0.5 && _AlbedoDebug < 1.5)
					return half4(albedoTex.rgb, 1.0);
				half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_Normals, sampler_Normals, input.uv));
				half3 lightsTex = SAMPLE_TEXTURE2D(_Lights, sampler_Lights, input.uv).rgb;
				half4 emissionTex = SAMPLE_TEXTURE2D(_Emission, sampler_Emission, input.uv);
				// Sampled up here rather than down with the SurfaceData, because the
				// water placement needs to be known before the smoothness block.
				// Defaults to black, and a black spec map gives waterMask 0 and
				// landMask 0 -- matte terrain, the safe reading of a missing map
				// rather than a fully mirrored planet.
				half4 specTex = SAMPLE_TEXTURE2D(_SpecMap, sampler_SpecMap, input.uv);

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

				// Geometric sun angle for this sample, and where the water is. Both
				// are needed before the emission block. Neither may use the
				// normal-mapped normal: whether a place is on the night side, and
				// whether it is under water, are both properties of the sphere.
				float geoNdL = dot(geoNormalWS, mainLight.direction);

				// Night side gate for the city lights, on the GEOMETRIC normal and
				// never the normal-mapped one: a settlement is not unlit because a
				// hill happens to be in the way. Measured earlier, a bump taking the
				// map-based ndl from -0.60 to +0.15 took that mask from 1.000 to 0.000
				// and punched the lights a hole exactly where the relief was steepest.
				float nightMask = saturate(-geoNdL * 2.0);

				float waterMask = saturate((specTex.g - _WaterFloor) * rcp(max(1e-3, _WaterRange)));

				// The city-lights gate. Separate from waterMask above and measured
				// in the property block: waterMask leaves 11% of the city's light
				// energy sitting on the ocean, this leaves none.
				float cityLightAvoid = saturate((specTex.g - _CityLightFloor)
				                               * rcp(max(1e-3, _CityLightRange)));


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

				// Straddle the terminator instead of stopping dead on it.
				//
				// WHAT THIS REPLACED, and why it was wrong for the look: the gate was
				// saturate(cosZ * 12.0 + 0.6), whose stated intent was to "hold it
				// right up to the terminator, then kill it over the couple of
				// degrees past". It did not. Measured, it is already down to 0.600
				// AT cosZ = 0 -- the terminator itself -- and reaches 0 at
				// cosZ = -0.05, which is 2.9 degrees of night side. So the band
				// faded over its last 2.9 degrees of DAY side as well, and what
				// read as a hard cut was the tail of that fade.
				//
				// It could not simply be loosened, because rcp(cosZ) has no meaning
				// past zero -- that is the entire reason the gate was there. max()
				// keeps airMass pinned at its floor and twilight saturated at 1.0,
				// so on the night side this term is free to do all the work.
				//
				// The old gate also deliberately kept the night side BLUE, on the
				// reasoning that the sun is gone there and only ambient is left.
				// That is a defensible choice and it is being reversed on purpose:
				// twilight on the night side is real -- it is the shadow of the
				// planet falling across its own atmosphere -- and with the band
				// confined to the day half, the terminator reads as an edge rather
				// than as the place the light bends.
				twilight *= 1.0 - saturate(-cosZ * rcp(max(1e-3, _TwilightDepth)));

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

				// _Emission only: anything glowing on its own terms -- lava, magma,
				// glowing crystal. It emits all the way round because it does not
				// come from the sun, so nothing gates it, and its alpha is the
				// intensity with _EmissionScale as the gain above it.
				//
				// There are no CITY LIGHTS here. That system was removed wholesale
				// to start again; nothing of it remains, and the _Lights plate is
				// City lights come from the _Lights plate, only on the night side and
				// never over water, because these plates are orthographic night lights
				// and their street grids bleed well past every coastline.
				half3 emission = lightsTex * _LightScale * nightMask
				                              * (1.0 - cityLightAvoid)
				               + emissionTex.rgb * emissionTex.a * _EmissionScale;
							   + emissionTex.rgb * emissionTex.a * _EmissionScale;


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
				// TWO coordinates, because the deck and its shadow are not in the
				// same place.
				//
				// Pan and drift are shared: the clouds drift, so what they cast has
				// to drift with them or the shadow detaches from its own cloud.
				//
				// Parallax is NOT shared. It is the view-dependent term, which is
				// the whole reason it exists -- it is what makes the deck read as
				// being at altitude rather than painted on. A shadow is a
				// projection down ONTO the ground; sliding it with the camera
				// makes the shadow move across a stationary surface, which reads
				// as exactly the flat decal the parallax was added to fix. The
				// shadow belongs to the surface plane, so it gets the surface UV.
				float2 cloudDriftUV = input.uv
				                    + _CloudPan.xy
				                    + _CloudDrift.xy * _Time.y;
				float2 cloudUV = cloudDriftUV
				               + viewTangent.xy * _CloudParallax;
				half4 cloudTex = SAMPLE_TEXTURE2D(_Clouds, sampler_Clouds,
				                                  TRANSFORM_TEX(cloudUV, _Clouds));
				half3 cloudNTS = UnpackNormalScale(
					SAMPLE_TEXTURE2D(_CloudNormals, sampler_CloudNormals,
					                  TRANSFORM_TEX(cloudUV, _CloudNormals)),
					_CloudNormalStrength);
				half3 cloudSpecTex = SAMPLE_TEXTURE2D(_CloudSpecular, sampler_CloudSpecular,
				                                      TRANSFORM_TEX(cloudUV, _CloudSpecular)).rgb;
				// PROJECTED along the sun's rays. The shadow belongs to the surface
				// plane, so it is sampled at cloudDriftUV with no parallax AND no
				// longer directly under the cloud: a cloud at height H throws its
				// shadow H/tan(elevation) away from the sun.
				//
				// Direction: the sun projected into this sample's tangent frame, then
				// negated, because the shadow falls on the far side from the sun.
				// Magnitude: H/tan(elev) in world units, converted to UV by dividing
				// by the arc a full 0..1 wrap covers, 2*pi*R. With H given in planet
				// radii that is just /2*pi.
				//
				// sinElev is clamped because tan -> 0 at the terminator and the
				// offset would run to infinity; at 0.08 the shadow is about 12x the
				// cloud height away, which is already well past a plausible throw.
				float sinElev = max(0.08, dot(geoNormalWS, mainLight.direction));
				float2 shadowThrow = normalize(
					float2(dot(mainLight.direction, input.tangentWS.xyz),
					        dot(mainLight.direction, bitangentWS))
					+ 1e-6) * (-_CloudShadowHeight * rcp(sinElev) * 0.15915494);
				half4 shadowTex = SAMPLE_TEXTURE2D(_CloudShadows, sampler_CloudShadows,
					                                   TRANSFORM_TEX(cloudDriftUV + shadowThrow,
					                                             _CloudShadows));

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
				// THE EMISSION IS UNDER THE DECK, so it is occluded by it. The
				// deck composite further down is untouched and still HEAD's; this
				// only scales what reaches the camera through the cloud in between.
				//
				// lerp is exactly 1.0 at zero coverage, so an unclouded planet is
				// bit-identical to having no deck at all.
				// Square, so the cut is concentrated under the thickest cloud --
				// see the property block for why a straight lerp read as no change.
				float throughLerp = lerp(1.0, _CloudEmissionThrough, cloudCov);
				surfaceData.emission = emission * throughLerp * throughLerp;
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
				// Gain first, then the strength. The order matters: strength alone
				// cannot make a faint map visible. Measured alpha means across the
				// shipped shadow maps: clouds_medium 0.371, clouds_locked 0.201, but
				// clouds_aerial_shadow only 0.012 with 1.4% of texels above 0.3.
				//
				// The saturate around the whole expression is REQUIRED, not tidiness.
				// _CloudShadowStrength goes to 2, so 1 - a*strength goes NEGATIVE
				// wherever a*strength exceeds 1 -- on clouds_nuke, whose alpha mean
				// is 0.794, that is most of the map. A negative cloudShadow
				// multiplied into max(litTerm, 0) subtracts from the emission term,
				// which is exactly what the separation just above exists to
				// prevent: city lights and magma are meant to keep burning under a
				// cloud. Saturating turns "overdriven" into "solid black shadow"
				// instead of "shadow that eats the city lights".
				float cloudShadow = saturate(
					1.0 - saturate(shadowTex.a * _CloudShadowSharpness)
					        * _CloudShadowStrength);
				color.rgb = emissionTerm + max(litTerm, 0.0) * cloudShadow;
				half3 afterLit = color.rgb;          // bisect capture

				// The deck itself. Clouds are a high-albedo layer sitting in the
				// sky, so they take the sun's colour and are lit with a wrapped
				// terminator -- they scatter forward and stay visible a little
				// past where the ground has gone dark, which is most of what
				// sells them as being above it rather than painted on.
				// Lit off the deck's OWN normal, not the surface's. A cloud mass has
				// relief of its own; lighting it from geoNormalWS made the deck perfectly
				// smooth, which is most of why it read as painted on rather than above.
				//
				// The tangent frame is built on geoNormalWS, not the normal-mapped
				// normal: the deck sits above the terrain, so terrain relief must not
				// tilt it, same rule the haze and the cloud day/night gate already use.
				float3 cloudNormalWS = normalize(TransformTangentToWorld(cloudNTS,
					half3x3(input.tangentWS.xyz, bitangentWS, geoNormalWS)));
				float cloudLight = saturate(dot(cloudNormalWS, mainLight.direction) * _CloudWrap
				                           + (1.0 - _CloudWrap));
				
				// Specular. Read as data like every other map in this shader: G is the
				// gloss mask, scaled by _CloudSmoothness. The shipped cloud specular maps
				// are flat at G = 0.110, so this is a uniform sheen until real ones exist.
				// A thick cloud deck is not very glossy, hence the low default.
				half cloudGloss = _CloudSmoothness * saturate(cloudSpecTex.g);
				half3 cloudHalf = normalize(mainLight.direction + viewDirWS);
				half cloudSpec = pow(saturate(dot(cloudNormalWS, cloudHalf)),
					                      exp2(2.0 + 10.0 * cloudGloss)) * cloudGloss;
				half3 cloudColour = cloudTex.rgb * (sunColour * _CloudBrightness * cloudLight)
					                  + sunColour * cloudSpec;
				color.rgb = lerp(color.rgb, cloudColour, cloudCov);
				half3 afterDeck = color.rgb;         // bisect capture

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
				// Night-side bound UNCHANGED at -0.15, so this does not wrap any
				// further than before. The day-side edge moves from 0.25 to 0.10 so
				// the window reaches full strength closer to the terminator: at geoNdL
				// 0 it went from 0.316 to 0.588, and the band peak went from 0.68
				// to 1.0. Nothing past -0.15 is affected at all.
				float twilightLit = smoothstep(-0.15, 0.10, geoNdL);

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
				// Peaks at 1.0 when the sun is perpendicular to the view, i.e. the
				// 90 degree side-on case, and falls off as the sun swings round behind
				// the camera. The ceiling was a hardcoded 0.55, which is most of why
				// the twilight zone never read as warm from the side.
				float lowForUs = saturate(_HazeSunView
				                        - dot(mainLight.direction, viewDirWS) * 1.5);
				float hazeSun = saturate(lowSun * lowForUs);

				// TWO stages, not one. A single lerp from the air's cyan to the rim's
				// orange passes straight through grey, and at the side-on ceiling that
				// produced (0.71, 0.69, 0.62) -- R minus B of +0.09, which is neutral.
				// The twilight zone could then never look orange no matter how far
				// _TwilightHaze was pushed, because it was lerping toward a colour with
				// no chroma left in it. Routing through the deep sunset colour keeps the
				// chroma up at every step: at that same 0.55 it is (0.80, 0.57, 0.47),
				// R minus B of +0.33. Squared on the second stage, because the deep
				// end is only reached right at the terminator.
				half3 hazeTint = lerp(lerp(_AtmoColor.rgb, _SunRimColor.rgb, saturate(hazeSun)),
					                  _SunRimDeep.rgb, saturate(hazeSun * hazeSun));
				// The band term as well as the limb term. hazeTint is already
				// the sunset colour wherever the sun is low, so letting a slice
				// of it in across the terminator is what makes the blue visibly
				// bend to red instead of only doing it at the silhouette.
				float hazeAmt = saturate(grazing * hazeLit
					* (_HazeStrength + _HazeSunStrength * hazeSun)
					+ twilight * _TwilightHaze * twilightLit);
				color.rgb = lerp(color.rgb, hazeTint, hazeAmt);



				half3 afterHaze = color.rgb;         // bisect capture

				if (_AlbedoDebug > 1.5)
				{
					if (_AlbedoDebug < 2.5) return half4(afterLit, 1.0);
					if (_AlbedoDebug < 3.5) return half4(afterDeck, 1.0);
					return half4(afterHaze, 1.0);
				}

				color.rgb = MixFog(color.rgb, inputData.fogCoord);
				return color;
			}
			ENDHLSL
		}
	}
	FallBack "Universal Render Pipeline/Lit"
}
