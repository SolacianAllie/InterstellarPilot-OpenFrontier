Shader "Hovl/Particles/Electricity" {
	Properties {
		_MainTexture ("Main Texture", 2D) = "white" {}
		_Dissolveamount ("Dissolve amount", Range(0, 1)) = 0.332
		_Mask ("Mask", 2D) = "white" {}
		_Color ("Color", Vector) = (0.5,0.5,0.5,1)
		_Emission ("Emission", Float) = 6
		_RemapXYFresnelZW ("Remap XY/Fresnel ZW", Vector) = (-10,10,2,2)
		_Speed ("Speed", Vector) = (0.189,0.225,-0.2,-0.05)
		_Opacity ("Opacity", Range(0, 1)) = 1
		[MaterialToggle] _Usedepth ("Use depth?", Float) = 0
		_Depth ("Depth", Float) = 0.15
		[Enum(Cull Off,0, Cull Front,1, Cull Back,2)] _CullMode ("Culling", Float) = 2
	}
	SubShader {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
		ZWrite Off
		Cull [_CullMode]
		Lighting Off
		Fog { Mode Off }
		Blend SrcAlpha One

		Pass {
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma shader_feature _USEDEPTH_ON
			#include "UnityCG.cginc"
			#include "UnityStandardUtils.cginc"

			sampler2D _MainTexture;
			float4 _MainTexture_ST;
			sampler2D _Mask;
			float4 _Mask_ST;
			fixed4 _Color;
			float _Dissolveamount;
			float _Emission;
			float4 _RemapXYFresnelZW;
			float4 _Speed;
			float _Opacity;
			float _Usedepth;
			float _Depth;
			UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

			struct appdata {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};
			struct v2f {
				float4 pos : SV_POSITION;
				fixed4 color : COLOR;
				float2 uvMain : TEXCOORD0;
				float2 uvMask : TEXCOORD1;
				float fresnel : TEXCOORD2;
				float4 screenPos : TEXCOORD3;
				float eyeDepth : TEXCOORD4;
			};

			v2f vert(appdata input)
			{
				v2f output;
				output.pos = UnityObjectToClipPos(input.vertex);
				output.uvMain = TRANSFORM_TEX(input.texcoord, _MainTexture);
				output.uvMask = TRANSFORM_TEX(input.texcoord, _Mask);
				output.color = input.color;
				// fresnel remapped into [_RemapXYFresnelZW.z, _RemapXYFresnelZW.w]
				float3 worldNormal = UnityObjectToWorldNormal(input.normal);
				float3 worldPos = mul(unity_ObjectToWorld, input.vertex).xyz;
				float3 viewDir = normalize(_WorldSpaceCameraPos - worldPos);
				float fres = 1.0 - saturate(dot(worldNormal, viewDir));
				output.fresnel = lerp(_RemapXYFresnelZW.z, _RemapXYFresnelZW.w, fres);
				output.screenPos = ComputeScreenPos(output.pos);
				output.eyeDepth = -UnityObjectToViewPos(input.vertex.xyz).z;
				return output;
			}

			fixed4 frag(v2f input) : SV_Target
			{
				// scrolling main texture (xy speed) and mask (zw speed)
				float2 uvMain = input.uvMain + frac(_Time.y * _Speed.xy);
				float2 uvMask = input.uvMask + frac(_Time.y * _Speed.zw);
				fixed4 mainTex = tex2D(_MainTexture, uvMain);
				fixed mask = tex2D(_Mask, uvMask).r;

				float dissolve = saturate((mask - (1.0 - _Dissolveamount) * input.fresnel) / max(_Dissolveamount * input.fresnel, 1e-5));
				dissolve = saturate(dissolve);

				fixed4 col = mainTex * _Color * _Emission * input.color;
				col.a = mainTex.a * _Opacity * dissolve * input.color.a;

				#ifdef _USEDEPTH_ON
				float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, input.screenPos.xy / input.screenPos.w));
				float fade = saturate((sceneZ - input.eyeDepth) / max(_Depth, 1e-4));
				col.a *= fade;
				#endif

				col.rgb *= col.a;
				return col;
			}
			ENDHLSL
		}
	}
}
