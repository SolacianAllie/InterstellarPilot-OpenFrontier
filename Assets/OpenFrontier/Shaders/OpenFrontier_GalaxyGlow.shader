// Open Frontier: ED's GalaxyGlow, rebuilt.
//
// Twenty-four (desktop) or twelve (phone) radial taps of the generated band
// texture around the core, accumulated with exp2 falloff. A bounded
// camera-facing quad at the galaxy centre, not a screen-space pass: the
// bulge blooms without touching the frame buffer, and a later URP bloom can
// spread it further. Additive and depth-tested.
//
// The tap loop is a shader keyword (GLOW_12 / GLOW_24), so the count is
// compile-time and the mobile variant really is twelve samples.
Shader "OpenFrontier/Galaxy/Glow" {
	Properties {
		_BandTex ("Galaxy Background", 2D) = "black" {}
		_GlowCenter ("Glow Center (world)", Vector) = (0,0,0,1)
		_GlowSize ("Glow Diameter (world)", Float) = 4500
		[HDR] _CoreColour ("Core Colour", Color) = (1,0.9,0.74,1)
		_GlowGain ("Glow Gain", Float) = 2.2
		_GlowFalloff ("Glow Falloff", Float) = 3
		_GlowRadius ("Tap Radius (UV)", Range(0.02,0.5)) = 0.18
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
			#pragma multi_compile _ GLOW_12 GLOW_24
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			TEXTURE2D(_BandTex);
			SAMPLER(sampler_BandTex);

			float4 _GlowCenter;
			float _GlowSize;
			float4 _CoreColour;
			float _GlowGain;
			float _GlowFalloff;
			float _GlowRadius;

			#if defined(GLOW_24)
				#define GLOW_TAPS 24
			#elif defined(GLOW_12)
				#define GLOW_TAPS 12
			#else
				#define GLOW_TAPS 4
			#endif

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

			Varyings vert(Attributes v)
			{
				Varyings o;
				float4 clip = mul(UNITY_MATRIX_VP, float4(_GlowCenter.xyz, 1.0));
				clip.xy += v.positionOS.xy * _GlowSize
					* float2(UNITY_MATRIX_P._m00, UNITY_MATRIX_P._m11);
				if (clip.w <= 0.0)
				{
					// Core behind the camera: collapse instead of flipping
					// the billboard through the screen.
					o.positionCS = float4(2.0, 2.0, 2.0, 1.0);
					o.uv = 0.0;
					return o;
				}
				o.positionCS = clip;
				o.uv = v.uv;
				return o;
			}

			float4 frag(Varyings i) : SV_Target
			{
				float2 p = i.uv * 2.0 - 1.0;
				float r = length(p);
				float2 dir = p / max(r, 1e-5);

				float3 acc = 0.0;
				[unroll]
				for (int t = 0; t < GLOW_TAPS; t++)
				{
					float rr = (t + 0.5) / GLOW_TAPS;
					float2 uv = 0.5 + dir * rr * _GlowRadius;
					float3 s = SAMPLE_TEXTURE2D(_BandTex, sampler_BandTex, uv).rgb;
					acc += s * exp2(-rr * _GlowFalloff) * (1.0 - rr);
				}

				float mask = 1.0 - smoothstep(0.55, 1.0, r);
				float3 col = acc * (_GlowGain / GLOW_TAPS) * mask * _CoreColour.rgb;
				return float4(col, 1.0);
			}
			ENDHLSL
		}
	}
}
