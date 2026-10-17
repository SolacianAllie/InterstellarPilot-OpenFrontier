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
	// THE BANDS DRIFT, AND THE DRIFT TABLE IS THE "NORMAL" MAP
	//
	// The *_normal.png that ships with the gas giants is not a normal map.
	// Measured on the four of them against the terrain set:
	//
	//     texture                            red between-band   within
	//     gas_giant_01_normal                      56.5          7.2
	//     gas_giant_06_normal                      73.2          2.4
	//     dwarf_gas_giant_01_normal                59.0          4.2
	//     cloudless_gas_giant_01_normal            22.4          2.1
	//     alpine_01_normal                          0.5          5.6
	//     barren_01_normal                          1.1          1.6
	//
	// Red's between-band variance is 3x to 30x its within-band variance, so
	// it holds ONE number per latitude band, constant across it. The terrain
	// maps have the ratio inverted, below 1, which is what a genuine
	// normal map looks like. Green is flat in all of them (0.7 to 1.7).
	//
	// And that is the tell: a surface banded by latitude has its height
	// gradient in LATITUDE, which in a tangent-space normal map is the green
	// channel. Green carries nothing here. Red carries everything, in the axis
	// a banded planet should have none in.
	//
	// Remapped to -1..1 the per-band values straddle zero -- -0.81 to +0.40 on
	// gas_giant_01 -- so some bands run one way and some the other. That is
	// differential rotation, and it is the whole effect: each band slides
	// along its own latitude at its own rate.
	//
	// The blue channel is 0 in EVERY planet normal map here, terrain included,
	// because Unity's UnpackNormal takes the UnpackNormalMapRGorAG path, which
	// rebuilds Z from red and green and ignores blue entirely. So the zero
	// blue is the project's convention, not something special about gas
	// giants, and it is not where the animation data lives. Alpha is 255 with
	// about 1% of texels slightly under, scattered across every row: noise.
	//
	// WHY THE BANDS TEAR AT THEIR EDGES, AND WHAT SMOOTHS IT
	//
	// The drift steps between adjacent bands, so the texture is offset by two
	// different amounts on the two sides of a boundary and the content jumps
	// there. _BandSmoothing softens it by averaging the table either side of
	// the band edge. Real zonal flow shears gradually across a band and only
	// steps at the jet boundary, so a little smoothing is closer to the truth
	// than none -- but a large value smears the difference between adjacent
	// bands, which is the part that was worth having.
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
		// Named _Normals because the existing materials bind that slot, and
		// because renaming it would break every gas giant material on disk.
		// It is a drift table, not a normal map.
		_Normals ("Band Drift Table (R = signed drift per band)", 2D) = "black" {}
		_BandDriftSpeed ("Band Drift Speed", Range(-0.1, 0.1)) = 0.004
		// Mip level the drift table is read at, and the fix for the bands
		// looking smeared with glowing edges.
		//
		// The drift steps between adjacent bands, so two fragments either side
		// of a boundary are sampling the albedo at very different places in the
		// texture -- 0.6 of its width apart after a minute at the old speed. The
		// GPU picks a mip from the screen-space UV derivative, so a quad that
		// straddles a boundary reports a derivative of half a texture width and
		// selects the smallest mip. The table has 47 bands in 256 rows, about
		// 5 rows each, so once the planet is more than a few hundred pixels
		// across, MOST quads straddle a boundary and the whole planet goes
		// blurry. The bright edges are a displaced band landing somewhere else
		// in the albedo.
		//
		// Reading the table at a higher mip blurs the drift field itself, which
		// turns the steps into a gradient, and a gradient has no boundary to
		// straddle. Costs nothing -- same texture, same sampler, a lod bias.
		// The table is 256 rows tall, so lod 3 is 32 rows: well under the 94
		// rows that 47 bands would need to stay distinct.
		_BandDriftLOD ("Band Drift Smoothing", Range(0, 6)) = 3
		// Vertical shear on top of the along-band slide. Real jets lean, and
		// without this the bands move like sliding rings on a turntable.
		_BandShear ("Band Shear", Range(0, 1)) = 0.3
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

		// 0 final | 1 albedo | 2 drifting albedo | 3 band drift
		_AlbedoDebug ("DEBUG Surface Bisect", Range(0, 3)) = 0
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
			TEXTURE2D(_Normals);   SAMPLER(sampler_Normals);

			CBUFFER_START(UnityPerMaterial)
				float4 _MainTex_ST;
				float4 _Normals_ST;
				float4 _AtmoColor;
				float4 _SunRimColor;
				float4 _SunRimDeep;
				float4 _TwilightColor;
				float _BandDriftSpeed;
				float _BandDriftLOD;
				float _BandShear;
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
				float _AlbedoDebug;
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
				half4 tangentWS : TEXCOORD3;
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
				output.tangentWS = half4(normInputs.tangentWS, input.tangentOS.w);
				output.uv = input.uv;
				return output;
			}

			// The signed drift rate of the band this texel is in, remapped to
			// -1..1.
			//
			// Sampled on the ALBEDO's uv, not its own _ST, so the table's bands
			// land exactly on the albedo's bands. Giving the table its own
			// tiling would silently slide one relative to the other and the
			// drift would animate bands that are not the ones you can see.
			//
			// Smoothing is a MIP BIAS on the table, not an extra tap. Blurring
			// the drift field turns the step between bands into a gradient, and
			// a gradient has no boundary for a pixel quad to straddle -- which
			// was what was driving the mip selection to the top of the chain
			// and smearing the planet. An extra tap would not have fixed that:
			// two samples of a step function still step.
			half BandDrift(float2 uvT)
			{
				return SAMPLE_TEXTURE2D_LOD(_Normals, sampler_Normals, uvT,
					_BandDriftLOD).r * 2.0 - 1.0;
			}

			half4 frag(FragmentInput input) : SV_Target
			{
				Varyings v = input.interpolated;

				float2 uvT = TRANSFORM_TEX(v.uv, _MainTex);
				half drift = BandDrift(uvT);

				// The slide is in TEXTURE space, applied after the tiling, so
				// _BandDriftSpeed means the same number however the material is
				// tiled. Doing it before TRANSFORM_TEX would scale the drift by
				// the tiling too and the rate would change with it.
				float2 bandUV = uvT + float2(
					drift * _Time.y * _BandDriftSpeed,
					// Shear leans the band vertically as it slides, so the flow
					// is not a set of rings turning like a turntable. Signed by
					// the drift so a band sliding backwards leans the other way,
					// which is what keeps the two consistent at a jet boundary.
					drift * _Time.y * _BandDriftSpeed * _BandShear * 0.5);

				half4 albedoTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, bandUV);

				if (_AlbedoDebug > 0.5 && _AlbedoDebug < 1.5)
					return half4(albedoTex.rgb, 1.0);
				// Stage 2 is the same sample with NO drift, so the two can be
				// compared side by side and the motion attributed to the drift
				// rather than to anything else that changed.
				if (_AlbedoDebug > 1.5 && _AlbedoDebug < 2.5)
					return half4(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvT).rgb, 1.0);
				// Stage 3 paints the drift itself: red = one way, blue = the
				// other, and the bands read as horizontal stripes because that
				// is what it is. If this shows structure other than bands, the
				// table is not the per-band drift it was measured to be.
				if (_AlbedoDebug > 2.5)
					return half4(drift * 0.5 + 0.5, 1.0 - drift * 0.5 - 0.5, 0.0, 1.0);

				// GEOMETRIC normal only. The band table is not a normal map, so
				// there is nothing to perturb with, and the atmosphere below
				// keys off the geometric normal regardless -- feeding a
				// normal-mapped one in is what used to make haze appear on the
				// night side and made the limb blotchy.
				half3 geoNormalWS = normalize(v.normalWS);
				half3 viewDirWS = GetWorldSpaceNormalizeViewDir(v.positionWS);
				half3 bitangentWS = cross(geoNormalWS, v.tangentWS.xyz) * v.tangentWS.w;

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
