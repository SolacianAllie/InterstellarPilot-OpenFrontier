Shader "OpenFrontier/Nebula"
{
	Properties
	{
		_MainTex ("Clouds", 2D) = "black" {}
		_NebulaColor ("Tint", Color) = (1, 1, 1, 1)
		_NebulaIntensity ("Intensity", Float) = 1
	}

	SubShader
	{
		// Queue 2999: below the sun quads (3000) so the sun always draws after the backdrop (AGENTS.md).
		Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent-1" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
		Pass
		{
			Name "AdditiveClouds"
			Blend One One
			ZWrite Off
			Cull Off
			ColorMask RGB

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);

			CBUFFER_START(UnityPerMaterial)
			float4 _MainTex_ST;
			half4  _NebulaColor;
			half   _NebulaIntensity;
			CBUFFER_END

			struct Attributes
			{
				float4 positionOS : POSITION;
				float2 uv         : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv         : TEXCOORD0;
			};

			Varyings vert(Attributes input)
			{
				Varyings output;
				output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
				output.uv = TRANSFORM_TEX(input.uv, _MainTex);
				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				half4 t = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
				half3 rgb = t.rgb * t.a * _NebulaColor.rgb * _NebulaIntensity;
				return half4(rgb, 1);
			}
			ENDHLSL
		}
	}

	Fallback Off
}
