// Open Frontier: ED's DistantMilkyWay, rebuilt.
//
// One generated square galaxy texture supplies both presentations. In map
// mode every fragment intersects its own camera ray with the galactic plane;
// in sky mode the camera ray is converted to galactic longitude/latitude and
// the bake's central row is sampled as a cylindrical band. The geometry is
// one screen-covering quad whose fragment reconstructs the ray through
// UNITY_MATRIX_I_VP; the old closed sphere was drawn with Cull Off and added
// the whole band twice.
//
// Map mode (ortho): true camera height, ED's near/far plane fades. This is
// the "plane fades away when you get close" behaviour; the billboard cloud
// is already underneath.
// Sky mode (perspective): a finite cylindrical band around the galactic pole,
// rotated so its core is the observer's true core direction. No distance fade;
// the latitude fade is the rim. The sky sample is mip-blurred and its contrast
// compressed because one top-down row wrapped around 360 degrees otherwise
// reads as parallel cloth folds; the 3D cloud carries the detail.
//
// Additive, no depth write, drawn first in the transparent galaxy stack.
Shader "OpenFrontier/Galaxy/Band" {
	Properties {
		_BandTex ("Galaxy Background", 2D) = "black" {}
		[HDR] _BandTint ("Band Tint", Color) = (1,1,1,1)
		_BandGain ("Band Gain", Float) = 1
		_GalaxyRadius ("Galaxy Radius (local)", Float) = 10000
		_SkyBandHalfAngle ("Sky Band Half Angle (radians)", Float) = 0.30
		_SkyGlowColor ("Sky Band Glow Colour", Color) = (1, 0.78, 0.55, 1)
		_SkyGlowGain ("Sky Band Glow Gain", Float) = 0.35
		_FadeNearRange ("Near Fade Start", Float) = 20
		_FadeNearRadius ("Near Fade End", Float) = 400
		_FadeFarRange ("Far Fade Start", Float) = 40000
		_FadeFarRadius ("Far Fade End", Float) = 90000
	}
	SubShader {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
		Blend One One
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
			float4 _BandTint;
			float _BandGain;
			float _GalaxyRadius;
			float _SkyBandHalfAngle;
			float4 _SkyGlowColor;
			float _SkyGlowGain;
			float4 _CoreColour;
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
				// One screen-covering quad, exactly once. The old proxy was a
				// closed sphere with Cull Off, so every pixel got the band
				// twice (and the dust alpha twice).
				o.positionCS = float4(v.positionOS.xy, GALAXY_FAR_Z, 1.0);
				o.uv = v.uv;
				return o;
			}

			float4 frag(Varyings i) : SV_Target
			{
				float3 camWS = GetCameraPositionWS();
				bool ortho = unity_OrthoParams.w > 0.5;
				float3 fwdWS = -unity_CameraToWorld._m02_m12_m22;

				// Reconstruct this pixel's camera ray. For the ortho map the
				// inverse-VP far point still gives the parallel-ray offset;
				// for the sky it gives the perspective direction.
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

				float h = ro.y;
				float denom = rd.y;
				float2 uv;
				float fade = 1.0;
				float disc = 1.0;
				float3 skyAdd = float3(0.0, 0.0, 0.0);

				if (ortho)
				{
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

					float r01 = length(hit) / max(_GalaxyRadius, 1e-4);
					uv = hit / (2.0 * _GalaxyRadius) + 0.5;
					disc = 1.0 - smoothstep(0.96, 1.0, r01);
				}
				else
				{
					// The sky uses a cylinder around the galactic pole so the
					// band rings the horizon and the disc never turns into an
					// overhead plane. Longitude selects the baked disc's
					// radial row; latitude selects the radial sample.
					float halfAngle = max(_SkyBandHalfAngle, 0.05);
					float latitude = asin(clamp(rd.y, -1.0, 1.0));
					float longitude = atan2(rd.z, rd.x);

					// The baked map's core sits at u=0.5, but the sky's raw
					// longitude does not put it in the direction of the
					// galactic centre. Rotate the sample so the core lands
					// exactly where the observer sees it; without this the
					// band is roughly a quarter-turn out of phase and the
					// bright core appears off to one side.
					float roLen = length(ro);
					float3 toCore = roLen > 1e-3 ? (-ro / roLen) : float3(0.0, 0.0, 1.0);
					float coreLongitude = atan2(toCore.z, toCore.x);
					float coreU = frac(coreLongitude * (1.0 / 6.28318530718) + 0.5);
					float u = frac(longitude * (1.0 / 6.28318530718) + 0.5 + (0.5 - coreU));
					float v = saturate(0.5 + latitude / (2.0 * halfAngle));
					uv = float2(u, v);

					float bandT = abs(latitude) / halfAngle;
					fade = 1.0 - smoothstep(0.72, 1.0, bandT);

					// The central row is structurally right but too dark and
					// streaked on its own to read as a Milky Way. A broad warm
					// glow carries the band; the texture supplies dust/arm
					// modulation. The core bump uses the true centre direction
					// rather than the texture's arbitrary u.
					float bandGlow = exp(-pow(bandT * 1.8, 2.0));
					float coreDot = saturate(dot(rd, toCore));
					skyAdd = _SkyGlowColor.rgb * (_SkyGlowGain * bandGlow * fade);
					skyAdd += _CoreColour.rgb * (pow(coreDot, 96.0) * _SkyGlowGain * 8.0);
				}

				// The sky texture is deliberately sampled through its mips:
				// the top-down bake's high frequencies become brushed-metal
				// streaks when one row is stretched around a cylinder. The
				// contrast is also compressed here: the map keeps the bake's
				// full arm/core signal, but the sky's smooth backdrop should
				// be a warm veil with only a hint of the arms. The 3D cloud
				// supplies the real small-scale detail.
				float4 bg = SAMPLE_TEXTURE2D_LOD(_BandTex, sampler_BandTex, uv, ortho ? 0.0 : 3.5);
				// RGB is the signal; A is only a geometric mask. Derive the
				// coverage from Rec.709 luminance (ED's own constants) so the
				// band survives wherever the baked colour is faint. The old
				// alpha multiply made the band quadratic in density and left
				// only the core readable; the old 0.005..0.08 window then cut
				// the faint outer arms a second time.
				float lum = dot(bg.rgb, float3(0.2125, 0.7154, 0.0721));
				float coverage = smoothstep(0.001, 0.035, lum);
				// The map keeps the bake's own warm/blue palette. The sky uses
				// luminance only and a cream tint: the bake's blue star knots
				// turn green when a blurred core is stretched across a huge
				// perspective band, and the 3D cloud supplies the real colour
				// variance. ED's smooth backdrop is a warm veil, not a lit
				// galaxy photo. Raising the floor keeps the wrapped arms from
				// reading as parallel cloth folds.
				float skyLum = lerp(0.22, lum, 0.45);
				float3 texCol = ortho ? bg.rgb * _BandTint.rgb : _BandTint.rgb * skyLum;
				float3 col = texCol * _BandGain * coverage * disc * fade;
				col += skyAdd;
				return float4(col, 1.0);
			}
			ENDHLSL
		}
	}
}
