Shader "Outlined/OutlineOnly" {
	Properties {
		_OutlineColor ("Outline Color", Vector) = (0,0,0,1)
		_Outline ("Outline width", Range(0, 0.03)) = 0.005
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		// Inverted-hull outline pass
		Pass {
			Cull Front
			ZWrite On

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			fixed4 _OutlineColor;
			float _Outline;

			struct appdata {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};
			struct v2f {
				float4 pos : SV_POSITION;
			};

			v2f vert(appdata input)
			{
				v2f output;
				float3 pos = input.vertex.xyz + normalize(input.normal) * _Outline;
				output.pos = UnityObjectToClipPos(float4(pos, 1));
				return output;
			}

			fixed4 frag(v2f input) : SV_Target
			{
				return _OutlineColor;
			}
			ENDHLSL
		}
	}
}
