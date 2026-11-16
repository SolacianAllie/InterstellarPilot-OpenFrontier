// Open Frontier: ED's DistantMilkyWayDust, rebuilt.
//
// The same generated galaxy texture as the band, inverted: the dust lane
// system is the dark side of the band, so the two can never disagree. Alpha
// blended (additive cannot darken) and drawn above the band, below the
// billboard cloud. No second texture and no painted lane mask.
//
// Projection and fade are identical to OpenFrontier/Galaxy/Band; see that
// shader for the map/sky modes. The band and dust each use ONE screen-
// covering quad, so the lanes get one alpha pass per pixel.
Shader "OpenFrontier/Galaxy/Dust" {
	Properties {
		_BandTex ("Galaxy Background", 2D) = "black" {}
		_DustColour ("Dust Colour", Color) = (0.045,0.028,0.022,1)
		_DustStrength ("Dust Strength", Range(0,1)) = 0.82
		_DustContrast ("Dust Contrast", Range(0.5,3)) = 1.3
		_GalaxyRadius ("Galaxy Radius (local)", Float) = 10000
		_SkyBandHalfAngle ("Sky Band Half Angle (radians)", Float) = 0.30
		_FadeNearRange ("Near Fade Start", Float) = 20
		_FadeNearRadius ("Near Fade End", Float) = 400
		_FadeFarRange ("Far Fade Start", Float) = 40000
		_FadeFarRadius ("Far Fade End", Float) = 90000
	}
	SubShader {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
		Blend SrcAlpha OneMinusSrcAlpha
		Cull Off
		ZWrite Off
		ZTest LEqual

		Pass {
			HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			TEXTURE2D(_BandTex);
			SAMPLER(sampler_BandTex);

			float4x4 _WorldToGalaxy;
			float4 _DustColour;
			float _DustStrength;
			float _DustContrast;
			float _GalaxyRadius;
			float _SkyBandHalfAngle;
			float _FadeNearRange;
			float _FadeNearRadius;
			float _FadeFarRange;
			float _FadeFarRadius;

			struct Attributes
			{
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			#if UNITY_REVERSED_Z
				#define GALAXY_FAR_Z 0.0
			#else
				#define GALAXY_FAR_Z 1.0
			#endif

			Varyings vert(Attributes v)
			{
				Varyings o;
				// Exactly one screen-covering fragment per pixel; the old
				// closed sphere double-drew the dust and made the lanes read
				// like a flat black veil.
				o.positionCS = float4(v.positionOS.xy, GALAXY_FAR_Z, 1.0);
				o.uv = v.uv;
				return o;
			}

			float4 frag(Varyings i) : SV_Target
			{
				float3 camWS = GetCameraPositionWS();
				bool ortho = unity_OrthoParams.w > 0.5;
				float3 fwdWS = -unity_CameraToWorld._m02_m12_m22;

				float2 ndc = i.uv * 2.0 - 1.0;
				float4 farClip = float4(ndc, GALAXY_FAR_Z, 1.0);
				float4 farWS4 = mul(UNITY_MATRIX_I_VP, farClip);
				float3 farWS = farWS4.xyz / max(farWS4.w, 1e-6);

				float3 roWS = camWS;
				float3 rdWS;
				if (ortho)
				{
					float3 offset = farWS - camWS;
					offset -= fwdWS * dot(offset, fwdWS);
					roWS = camWS + offset;
					rdWS = fwdWS;
				}
				else
				{
					rdWS = normalize(farWS - camWS);
				}

				float3 ro = mul(_WorldToGalaxy, float4(roWS, 1.0)).xyz;
				float3 rd = normalize(mul((float3x3)_WorldToGalaxy, rdWS));

				float2 uv;
				float fade = 1.0;
				float r01 = 1.0;

				if (ortho)
				{
					float h = ro.y;
					float denom = rd.y;
					if (abs(denom) < 1e-5) discard;
					float t = -h / denom;
					if (t <= 0.0) discard;
					float2 hit = ro.xz + rd.xz * t;

					float d = abs(h);
					float nearT = saturate((d - _FadeNearRange) / max(_FadeNearRadius - _FadeNearRange, 1e-3));
					nearT = nearT * nearT * (3.0 - 2.0 * nearT);
					float farT = saturate((d - _FadeFarRange) / max(_FadeFarRadius - _FadeFarRange, 1e-3));
					farT = farT * farT * (3.0 - 2.0 * farT);
					fade = nearT * (1.0 - farT);

					r01 = length(hit) / max(_GalaxyRadius, 1e-4);
					uv = hit / (2.0 * _GalaxyRadius) + 0.5;
				}
				else
				{
					// Same cylindrical sky framing as the band: longitude
					// selects the baked disc's radial row, latitude selects
					// the radial sample. The sample is rotated so the core
					// longitude stays registered with the band's core.
					float halfAngle = max(_SkyBandHalfAngle, 0.05);
					float latitude = asin(clamp(rd.y, -1.0, 1.0));
					float longitude = atan2(rd.z, rd.x);
					float roLen = length(ro);
					float3 toCore = roLen > 1e-3 ? (-ro / roLen) : float3(0.0, 0.0, 1.0);
					float coreLongitude = atan2(toCore.z, toCore.x);
					float coreU = frac(coreLongitude * (1.0 / 6.28318530718) + 0.5);
					float u = frac(longitude * (1.0 / 6.28318530718) + 0.5 + (0.5 - coreU));
					float v = saturate(0.5 + latitude / (2.0 * halfAngle));
					uv = float2(u, v);
					fade = 1.0 - smoothstep(0.72, 1.0, abs(latitude) / halfAngle);
					r01 = 0.0; // latitude fade is the disc gate in the sky
				}

				// The sky sample is deliberately softer than the map's: the
				// baked disc's arms read as brushed-metal streaks when they
				// are wrapped across 360 degrees of longitude at full detail.
				float4 bg = SAMPLE_TEXTURE2D_LOD(_BandTex, sampler_BandTex, uv, ortho ? 0.0 : 3.5);
				float y = dot(bg.rgb, float3(0.2125, 0.7154, 0.0721)); // Rec.709, ED's own
				// ED's shader is 1 - y, but a straight inversion dims the
				// whole disc. A smoothstep centred on the lane luminance
				// deepens the dark gaps while leaving the bright arms
				// untouched - the map reads as dust lanes, not as a dirty
				// veil. _DustContrast moves the threshold.
				// Narrow the lane response: the arms are bright, the lanes
				// are dark. A wide inversion dims the whole disc instead of
				// cutting lanes through it.
				float dust = 1.0 - smoothstep(0.06, 0.28, y * _DustContrast);
				// Gate on luminance, not alpha: the baked alpha is a
				// geometric mask, and ED's dust is the inverse of the band's
				// own luminance. Black outside the disc stays black.
				dust *= smoothstep(0.005, 0.04, y);
				dust *= _DustStrength * fade * (1.0 - smoothstep(0.96, 1.0, r01));
				// In-sky dust is a veil, not the map's lane mask. At the
				// map strength the alpha pass turns the whole band brown
				// and the warm core reads green by contrast; ED's sky
				// keeps only the broad dark lanes. The map is unchanged.
				dust *= ortho ? 1.0 : 0.15;
				return float4(_DustColour.rgb, saturate(dust));
			}
			ENDHLSL
		}
	}
}
