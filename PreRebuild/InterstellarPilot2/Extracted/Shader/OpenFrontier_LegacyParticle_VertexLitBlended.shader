// Legacy "Particles/VertexLit Blended" for URP (simplified):
// Blend SrcAlpha OneMinusSrcAlpha, ambient + emission tint per vertex.
// The BiRP original computed full per-vertex fixed-function lighting;
// for sprite-style thrusters the ambient term is the dominant part.
Shader "OpenFrontier/Legacy Particles/VertexLit Blended" {
	Properties {
		[HDR] _Color ("Color", Color) = (1,1,1,1)
		[HDR] _Emission ("Emission", Color) = (0,0,0,0)
		_SpecColor ("Specular Color", Color) = (1,1,1,1)
		_MainTex ("Particle Texture", 2D) = "white" {}
		_InvFade ("Soft Particles Factor", Range(0.01,3.0)) = 1.0
	}
	SubShader {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
		Blend SrcAlpha OneMinusSrcAlpha
		Cull Off
		ZWrite Off

		Pass {
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "OpenFrontierLegacyParticles.cginc"

			fixed4 _Color;
			fixed4 _Emission;

			v2f_legacy_particle vert(appdata_legacy_particle input)
			{
				v2f_legacy_particle output;
				output.pos = UnityObjectToClipPos(input.vertex);
				output.uv = TRANSFORM_TEX(input.uv, _MainTex);
				// Simplified vertex lighting: doubled ambient + emission,
				// multiplied by the particle color and _Color.
				fixed4 lit = _Color * input.color;
				lit.rgb *= UNITY_LIGHTMODEL_AMBIENT.rgb * 2.0 + _Emission.rgb;
				output.color = lit;
				output.screenPos = ComputeScreenPos(output.pos);
				output.eyeDepth = -UnityObjectToViewPos(input.vertex.xyz).z;
				return output;
			}

			fixed4 frag(v2f_legacy_particle input) : SV_Target
			{
				fixed4 col = input.color * tex2D(_MainTex, input.uv);
				return apply_soft_fade(input, col);
			}
			ENDHLSL
		}
	}
}
