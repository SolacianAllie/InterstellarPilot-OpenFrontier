// Open Frontier galaxy backdrop: the BAKED disk face.
//
// One unlit additive quad carrying the texture GalaxyBaker rendered
// from GalaxyModel.Params (editor-time). All of the photo's continuous
// structure -- core glow, arm haze, dust extinction, rim fade -- is
// already correct IN the texture (dust darkened the core inside the
// same buffer), so this shader only adds: sample, tint, one blend.
//
// Queue 2990: draws BEFORE the star billboards (3000) and any gameplay
// transparents (3000+). The old sun/nebula queue doctrine applies at
// this scale too: backdrop first, everything that must sort over it
// later.
Shader "OpenFrontier/GalaxyBackdrop"
{
	Properties
	{
		[NoScaleOffset] _BaseMap ("Disk Texture (baked)", 2D) = "black" {}
		_Tint ("Tint", Color) = (1, 1, 1, 1)
		_Intensity ("Intensity", Float) = 1
	}

	SubShader
	{
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

		Pass
		{
			Name "AdditiveFace"

			Blend One One
			ZWrite Off
			ZTest LEqual
			Cull Off

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			TEXTURE2D(_BaseMap);
			SAMPLER(sampler_BaseMap);

			half4 _Tint;
			half _Intensity;

			struct Attributes
			{
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionHCS : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			Varyings vert(Attributes input)
			{
				Varyings output;
				output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
				output.uv = input.uv;
				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				half3 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;
				half3 light = tex * _Tint.rgb * _Intensity;
				return half4(light, 1.0);
			}
			ENDHLSL
		}
	}

	Fallback Off
}
