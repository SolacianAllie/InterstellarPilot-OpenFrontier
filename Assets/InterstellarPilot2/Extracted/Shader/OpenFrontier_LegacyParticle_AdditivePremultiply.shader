// Legacy "Particles/Additive" for premultiplied-alpha textures.
//
// The original declares Blend SrcAlpha One, which is correct for a texture
// whose colour has NOT already been multiplied into its alpha. Unity marks
// such a texture with alphaIsTransparency, and a material whose _MainTex
// carries that flag has its colour multiplied in already - so multiplying
// by alpha a second time in the blend state erases it.
//
// For a soft additive glow the damage is cumulative rather than obvious: a
// wisp at alpha 0.1 is added at a hundredth of its own brightness and simply
// does not appear, while the dense cores survive. That is the appearance of
// an effect that spawns and draws nothing, which is how the ship trails went
// missing.
//
// Blend One One is the additive form for premultiplied data - the colour is
// already scaled, so it only has to be added. This is the additive
// counterpart of OpenFrontier/Legacy Particles/Alpha Blended Premultiply,
// which already exists and is used by the laser materials.
//
// Only use this on a material whose texture is flagged
// alphaIsTransparency. A straight-alpha texture on this shader will look
// too bright, because it is no longer being multiplied down by its alpha.
Shader "OpenFrontier/Legacy Particles/Additive Premultiply" {
	Properties {
		[HDR] _TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
		_MainTex ("Particle Texture", 2D) = "white" {}
		_InvFade ("Soft Particles Factor", Range(0.01,3.0)) = 1.0
	}
	SubShader {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
		Blend One One
		Cull Off
		ZWrite Off

		Pass {
			HLSLPROGRAM
			#pragma vertex vert_legacy_particle
			#pragma fragment frag
			#include "OpenFrontierLegacyParticles.cginc"

			fixed4 frag(v2f_legacy_particle input) : SV_Target
			{
				fixed4 col = 2.0f * input.color * tex2D(_MainTex, input.uv);
				return apply_soft_fade(input, col);
			}
			ENDHLSL
		}
	}
}
