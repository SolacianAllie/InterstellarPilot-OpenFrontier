// Open Frontier galaxy star billboards.
//
// One merged mesh of camera-facing quads (vertex shader billboard, the
// proven technique): COLOR carries the premultiplied tint, TEXCOORD0
// the -1..1 corner, TEXCOORD1.x the quad size in MODEL units.
//
// The size is compensated by the object's world scale per vertex, so
// the dressing root's x3000 scale applies to POSITIONS and SIZES alike
// and no parent transform can silently distort the billboards.
//
// Near fade: the far layers (hero mesh) shrink away as their centre
// approaches the camera and the local shell (GalaxyLocalStars, same
// shader with its OWN material, _NearFadeEnd = 0 = no fade) supplies
// the near field -- the two tiers compose into one sky. The faded tier
// must fade BEFORE the shell's stars appear or the near field doubles.
//
// Profile: soft gaussian core + wider halo, edge-safe (zero at the
// quad border -- no additive square edges at any brightness).
//
// NO depth-based fades anywhere: _CameraDepthTexture reads ~0 behind
// empty space on this stack (the soft-particle doctrine), and these
// quads live where there is no depth to read.
Shader "OpenFrontier/GalaxyStars"
{
	Properties
	{
		_Intensity ("Intensity", Float) = 1
		_NearFadeStart ("Near Fade Start (world units, 0 disables)", Float) = 60
		_NearFadeEnd ("Near Fade End (world units)", Float) = 140
		_Softness ("Core Softness", Range(3, 30)) = 10
	}

	SubShader
	{
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

		Pass
		{
			Name "BillboardAdditive"

			Blend One One
			ZWrite Off
			ZTest LEqual
			Cull Off

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			half _Intensity;
			float _NearFadeStart;
			float _NearFadeEnd;
			half _Softness;

			struct Attributes
			{
				float4 positionOS : POSITION; // quad centre, model space
				float2 corner : TEXCOORD0;    // -1..1 corner
				float4 color : COLOR;         // premultiplied tint
				float4 data : TEXCOORD1;      // x = size (model units), y = alpha
			};

			struct Varyings
			{
				float4 positionHCS : SV_POSITION;
				float2 corner : TEXCOORD0;
				half4 color : COLOR;
			};

			Varyings vert(Attributes input)
			{
				Varyings output;
				float3 centerWS = TransformObjectToWorld(input.positionOS.xyz);
				float3 centerVS = TransformWorldToView(centerWS);
				float distanceVS = max(-centerVS.z, 1e-3);

				// Object-scale compensation: size is authored in MODEL
				// units; the world->view offset must carry the object's
				// world scale or a re-scaled dressing silently changes
				// star sizes (the old scene's x3000 lesson).
				float3x3 rot = (float3x3)GetObjectToWorldMatrix();
				float sx = length(mul(rot, float3(1, 0, 0)));
				float sy = length(mul(rot, float3(0, 1, 0)));
				float sz = length(mul(rot, float3(0, 0, 1)));
				float worldScale = max(sx, max(sy, sz));
				float sizeVS = input.data.x * worldScale;

				// Near fade: uniform-gated (a material with
				// _NearFadeEnd = 0 never fades -- the local shell).
				float fade = 1.0;
				if (_NearFadeEnd > 0.0)
				{
					fade = saturate(
						(distanceVS - _NearFadeStart)
						/ max(_NearFadeEnd - _NearFadeStart, 1e-3));
				}

				float2 cornerVS = input.corner * sizeVS;
				float3 positionVS = centerVS + float3(cornerVS, 0.0);
				output.positionHCS = mul(UNITY_MATRIX_P, float4(positionVS, 1.0));
				output.corner = input.corner;
				output.color = input.color * input.data.y * fade;
				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				float d2 = dot(input.corner, input.corner);
				float s = _Softness;
				float s2 = s * 0.28;
				// Soft gaussian core + wider halo; the edge values are
				// subtracted so the quad border is exactly zero.
				float a = (exp(-d2 * s) - exp(-s)) * 1.10
					+ (exp(-d2 * s2) - exp(-s2)) * 0.18;
				a = max(a, 0.0);
				return half4(input.color.rgb * a * _Intensity, 1.0);
			}
			ENDHLSL
		}
	}

	Fallback Off
}
