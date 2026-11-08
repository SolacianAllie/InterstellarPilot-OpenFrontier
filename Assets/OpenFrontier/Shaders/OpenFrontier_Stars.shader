Shader "OpenFrontier/Stars"
{
	Properties
	{
		_MainTex ("Stars", 2D) = "black" {}
		_StarsColor ("Tint", Color) = (1, 1, 1, 1)
		_StarsIntensity ("Intensity", Float) = 1
	}

	SubShader
	{
		Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Background" "RenderType" = "Opaque" }
		Pass
		{
			Name "ForwardUnlitStars"
			Cull Front
			ZWrite On

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);

			CBUFFER_START(UnityPerMaterial)
			float4 _MainTex_ST;
			half4  _StarsColor;
			half   _StarsIntensity;
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
				return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _StarsColor * _StarsIntensity;
			}
			ENDHLSL
		}
	}

	Fallback Off
}
