Shader "Mobile/Particles/AdditiveDisabledFog" {
	Properties {
		_MainTex ("Particle Texture", 2D) = "white" {}
	}
	SubShader {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
		ZWrite Off
		Cull Off
		Lighting Off
		Fog { Mode Off }
		Blend SrcAlpha One

		Pass {
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;

			struct appdata {
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};
			struct v2f {
				float4 pos : SV_POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			v2f vert(appdata input)
			{
				v2f output;
				output.pos = UnityObjectToClipPos(input.vertex);
				output.texcoord = TRANSFORM_TEX(input.texcoord, _MainTex);
				output.color = input.color;
				return output;
			}

			fixed4 frag(v2f input) : SV_Target
			{
				return input.color * tex2D(_MainTex, input.texcoord);
			}
			ENDHLSL
		}
	}
}
