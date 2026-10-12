Shader "SpaceUnity/PlanetAtmosphere" {
	Properties {
		_AtmoColor ("Atmosphere Color", Vector) = (0.5,0.5,1,1)
		_Size ("Size", Float) = 0.1
		_Falloff ("Falloff", Float) = 5
		_Transparency ("Transparency", Float) = 15
	}
	SubShader {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
		ZWrite Off
		Cull Front
		Lighting Off
		Fog { Mode Off }
		Blend SrcAlpha One

		Pass {
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			fixed4 _AtmoColor;
			float _Size;
			float _Falloff;
			float _Transparency;

			struct appdata {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};
			struct v2f {
				float4 pos : SV_POSITION;
				float3 normalDir : TEXCOORD0;
				float3 viewDir : TEXCOORD1;
			};

			v2f vert(appdata input)
			{
				v2f output;
				// expand the shell slightly
				float3 pos = input.vertex.xyz * (1.0 + _Size * 0.1);
				output.pos = UnityObjectToClipPos(float4(pos, 1));
				output.normalDir = UnityObjectToWorldNormal(input.normal);
				float3 worldPos = mul(unity_ObjectToWorld, input.vertex).xyz;
				output.viewDir = normalize(_WorldSpaceCameraPos - worldPos);
				return output;
			}

			fixed4 frag(v2f input) : SV_Target
			{
				// back faces of the shell -> rim seen through the planet silhouette
				float fres = saturate(dot(normalize(input.normalDir), normalize(input.viewDir)));
				fres = pow(fres, _Falloff);
				return _AtmoColor * fres * (_Transparency * 0.1);
			}
			ENDHLSL
		}
	}
}
