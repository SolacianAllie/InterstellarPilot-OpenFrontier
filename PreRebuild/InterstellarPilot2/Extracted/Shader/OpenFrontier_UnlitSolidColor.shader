Shader "OpenFrontier/Unlit Solid Color" {
	Properties {
		// Settable directly as hex in the inspector color field (e.g. 4D4A48).
		// Declared as Color so linear color-space projects convert it correctly
		// (a Vector-typed property would arrive unconverted and display too bright).
		_Color ("Overlay Color", Color) = (0.302, 0.29, 0.282, 1)
	}
	SubShader {
		Tags { "Queue"="Overlay" "IgnoreProjector"="True" "RenderType"="Transparent" }
		ZWrite Off
		Cull Off
		Lighting Off
		Fog { Mode Off }
		Blend SrcAlpha OneMinusSrcAlpha

		Pass {
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			fixed4 _Color;

			struct appdata {
				float4 vertex : POSITION;
			};
			struct v2f {
				float4 pos : SV_POSITION;
			};

			v2f vert(appdata input)
			{
				v2f output;
				output.pos = UnityObjectToClipPos(input.vertex);
				return output;
			}

			fixed4 frag(v2f input) : SV_Target
			{
				return _Color;
			}
			ENDHLSL
		}
	}
}
