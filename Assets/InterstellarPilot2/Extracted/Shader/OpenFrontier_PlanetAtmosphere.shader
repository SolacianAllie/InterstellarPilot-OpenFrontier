Shader "OpenFrontier/Planet Atmosphere" {
	Properties {
		_AtmoColor ("Atmosphere Color", Vector) = (0.5,0.5,1,1)
		_Size ("Size", Float) = 0.1
		_Falloff ("Falloff", Float) = 5
		_Transparency ("Transparency", Float) = 15
		[Header(OpenFrontierDayNight)]
		_TerminatorColor ("Terminator Color", Color) = (1,0.55,0.3,1)
		_TerminatorBoost ("Terminator Boost", Range(0, 3)) = 0.8
		_TerminatorFalloff ("Terminator Falloff", Range(1, 16)) = 4
		_NightGlow ("Night Glow", Range(0, 0.3)) = 0.03
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
			fixed4 _TerminatorColor;
			float _TerminatorBoost;
			float _TerminatorFalloff;
			float _NightGlow;
			// Set per frame by SunBillboard: world direction TOWARDS the sun.
			float4 _SunDirectionWorld;

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
				float3 n = normalize(input.normalDir);
				float3 v = normalize(input.viewDir);
				float3 l = (dot(_SunDirectionWorld.xyz, _SunDirectionWorld.xyz) < 0.0001) ? float3(0, 1, 0) : normalize(_SunDirectionWorld.xyz);
				// limb brightening (as before): thickest looking along the surface
				float fres = pow(saturate(dot(n, v)), _Falloff);
				// day-night scattering: 1 on the day side, ~0 on the night side,
				// smooth transition through the terminator
				float ndl = dot(n, l);
				float day = smoothstep(-0.2, 0.3, ndl);
				// sunset band hugging the terminator line
				float terminator = pow(saturate(1.0 - abs(ndl)), _TerminatorFalloff);
				fixed3 tint = lerp(_AtmoColor.rgb, _TerminatorColor.rgb, terminator * _TerminatorColor.a);
				float glow = day * (1.0 + terminator * _TerminatorBoost) + _NightGlow;
				return fixed4(tint * glow * fres * (_Transparency * 0.1), 1);
			}
			ENDHLSL
		}
	}
}
