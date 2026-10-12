// Legacy "Particles/Multiply" for URP: Blend Zero SrcColor, rgb premultiplied
// by alpha and lerped against white so low alpha leaves the frame untouched.
Shader "OpenFrontier/Legacy Particles/Multiply" {
	Properties {
		[HDR] _TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
		_MainTex ("Particle Texture", 2D) = "white" {}
		_InvFade ("Soft Particles Factor", Range(0.01,3.0)) = 1.0
	}
	SubShader {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
		Blend Zero SrcColor
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
				col.rgb *= col.a;
				col = lerp(fixed4(1,1,1,1), col, col.a);
				return apply_soft_fade(input, col);
			}
			ENDHLSL
		}
	}
}
