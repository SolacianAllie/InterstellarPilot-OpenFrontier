// Legacy "Particles/Additive (Soft)" for URP: Blend One OneMinusSrcColor
// (soft additive / screen blend), no x2 boost.
Shader "OpenFrontier/Legacy Particles/Additive (Soft)" {
	Properties {
		[HDR] _TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
		_MainTex ("Particle Texture", 2D) = "white" {}
		_InvFade ("Soft Particles Factor", Range(0.01,3.0)) = 1.0
	}
	SubShader {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
		Blend One OneMinusSrcColor
		Cull Off
		ZWrite Off

		Pass {
			HLSLPROGRAM
			#pragma vertex vert_legacy_particle
			#pragma fragment frag
			#include "OpenFrontierLegacyParticles.cginc"

			fixed4 frag(v2f_legacy_particle input) : SV_Target
			{
				fixed4 col = input.color * tex2D(_MainTex, input.uv);
				col.a = saturate(col.a);
				return apply_soft_fade(input, col);
			}
			ENDHLSL
		}
	}
}
