Shader "OpenFrontier/Particles/Unlit (NoFog)" {
	Properties {
		_BaseMap ("Base Map", 2D) = "white" {}
		// HDR-capable: the inspector shows the intensity slider, so the
		// material's base color can exceed 1 and drive bloom directly.
		[HDR] _BaseColor ("Base Color", Color) = (1,1,1,1)
		_Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
		[HDR] _EmissionColor ("Color", Color) = (0,0,0)
		_EmissionMap ("Emission", 2D) = "white" {}

		// Particle specific
		_SoftParticlesNearFadeDistance ("Soft Particles Near Fade", Float) = 0.0
		_SoftParticlesFarFadeDistance ("Soft Particles Far Fade", Float) = 1.0
		_CameraNearFadeDistance ("Camera Near Fade", Float) = 1.0
		_CameraFarFadeDistance ("Camera Far Fade", Float) = 2.0

		// Hidden properties - generic
		_Surface ("__surface", Float) = 0.0
		_Blend ("__mode", Float) = 0.0
		_Cull ("__cull", Float) = 2.0
		[ToggleUI] _AlphaClip ("__clip", Float) = 0.0
		[HideInInspector] _BlendOp ("__blendop", Float) = 0.0
		[HideInInspector] _SrcBlend ("__src", Float) = 1.0
		[HideInInspector] _DstBlend ("__dst", Float) = 0.0
		[HideInInspector] _SrcBlendAlpha ("__srcA", Float) = 1.0
		[HideInInspector] _DstBlendAlpha ("__dstA", Float) = 0.0
		[HideInInspector] _ZWrite ("__zw", Float) = 1.0
		[HideInInspector] _AlphaToMask ("__alphaToMask", Float) = 0.0

		// Particle specific
		_ColorMode ("_ColorMode", Float) = 0.0
		[HideInInspector] _BaseColorAddSubDiff ("_ColorMode", Vector) = (0,0,0,0)
		[ToggleOff] _FlipbookBlending ("__flipbookblending", Float) = 0.0
		[ToggleUI] _SoftParticlesEnabled ("__softparticlesenabled", Float) = 0.0
		[ToggleUI] _CameraFadingEnabled ("__camerafadingenabled", Float) = 0.0
		[HideInInspector] _SoftParticleFadeParams ("__softparticlefadeparams", Vector) = (0,0,0,0)
		[HideInInspector] _CameraFadeParams ("__camerafadeparams", Vector) = (0,0,0,0)

		// Editmode props
		_QueueOffset ("Queue offset", Float) = 0.0

		// Obsolete
		[HideInInspector] _Mode ("mode", Float) = 0
		[HideInInspector] _Color ("color", Color) = (1,1,1,1)
	}
	SubShader {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }

		BlendOp[_BlendOp]
		Blend[_SrcBlend][_DstBlend], [_SrcBlendAlpha][_DstBlendAlpha]
		ZWrite[_ZWrite]
		Cull[_Cull]
		ColorMask RGBA

		Pass {
			// No LightMode tag: renders as SRPDefaultUnlit under URP and receives NO fog.
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma shader_feature_local _SOFTPARTICLES_ON
			#pragma shader_feature_local_fragment _ALPHATEST_ON
			#include "UnityCG.cginc"

			sampler2D _BaseMap;
			float4 _BaseMap_ST;
			half4 _BaseColor;
			half _Cutoff;
			half4 _EmissionColor;
			sampler2D _EmissionMap;
			half4 _SoftParticleFadeParams;
			half4 _CameraFadeParams;
			half _SoftParticlesEnabled;
			half _CameraFadingEnabled;
			half _AlphaToMask;
			UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				half4 color : COLOR;
			};
			struct v2f {
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				half4 color : COLOR;
				float4 screenPos : TEXCOORD1;
				float eyeDepth : TEXCOORD2;
			};

			v2f vert(appdata input)
			{
				v2f output;
				output.pos = UnityObjectToClipPos(input.vertex);
				output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
				output.color = input.color * _BaseColor;
				output.screenPos = ComputeScreenPos(output.pos);
				output.eyeDepth = -UnityObjectToViewPos(input.vertex.xyz).z;
				return output;
			}

			half4 frag(v2f input) : SV_Target
			{
				half4 albedo = tex2D(_BaseMap, input.uv);
				half4 col = albedo * input.color;
				col.rgb += _EmissionColor.rgb * tex2D(_EmissionMap, input.uv).rgb;

				#if _ALPHATEST_ON
				clip(col.a - _Cutoff);
				#endif

				#ifdef _SOFTPARTICLES_ON
				// identical formula to URP Particles.hlsl so material params behave the same
				float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, input.screenPos.xy / input.screenPos.w);
				float sceneZ = LinearEyeDepth(rawDepth);
				float fade = saturate(_SoftParticleFadeParams.y * ((sceneZ - _SoftParticleFadeParams.x) - input.eyeDepth));
				col.a *= fade;
				#endif

				if (_CameraFadingEnabled > 0.5)
				{
					half camFade = saturate((input.eyeDepth - _CameraFadeParams.x) * _CameraFadeParams.y);
					col.a *= camFade;
				}

				return col;
			}
			ENDHLSL
		}
	}
	FallBack "Universal Render Pipeline/Particles/Unlit"
}
