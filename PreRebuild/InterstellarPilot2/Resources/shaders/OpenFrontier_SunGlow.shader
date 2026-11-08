Shader "OpenFrontier/Sun Glow" {
	Properties {
		_BaseMap ("Texture", 2D) = "white" {}
		[HDR] _Color ("Color", Color) = (1, 1, 1, 1)
	}
	SubShader {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		ZWrite Off
		Cull Off
		Lighting Off
		Fog { Mode Off }
		// Additive: the sun adds light over the starfield.
		Blend SrcAlpha One

		Pass {
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _BaseMap;
			float4 _BaseMap_ST;
			float4 _Color;

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};
			struct v2f {
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			v2f vert(appdata input)
			{
				v2f output;
				output.pos = UnityObjectToClipPos(input.vertex);
				output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
				return output;
			}

			float4 frag(v2f input) : SV_Target
			{
				return tex2D(_BaseMap, input.uv) * _Color;
			}
			ENDHLSL
		}
	}
}
